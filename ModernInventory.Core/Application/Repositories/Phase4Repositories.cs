using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ModernInventory.Core.Domain.Entities;
using ModernInventory.Core.Infrastructure.Database;

namespace ModernInventory.Core.Application.Repositories
{
    #region Supplier Repository

    public interface ISupplierRepository : IRepository<Supplier>
    {
        Task<Supplier?> GetByNameAsync(string name, string businessId);
        Task<List<Supplier>> SearchAsync(string businessId, string searchTerm);
        Task<decimal> GetTotalPayablesAsync(string businessId);
    }

    public class SupplierRepository : Repository<Supplier>, ISupplierRepository
    {
        public SupplierRepository(AppDbContext context) : base(context)
        {
        }

        public async Task<Supplier?> GetByNameAsync(string name, string businessId)
        {
            if (string.IsNullOrWhiteSpace(name)) return null;
            var clean = name.Trim();
            return await _dbSet
                .Where(s => s.BusinessId == businessId && !s.IsDeleted && s.Name == clean)
                .FirstOrDefaultAsync();
        }

        public async Task<List<Supplier>> SearchAsync(string businessId, string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
            {
                return await GetAllAsync(businessId);
            }

            var clean = searchTerm.Trim().ToLowerInvariant();
            return await _dbSet
                .Where(s => s.BusinessId == businessId && !s.IsDeleted &&
                    (s.Name.ToLower().Contains(clean) ||
                     s.Phone.ToLower().Contains(clean) ||
                     s.CompanyName.ToLower().Contains(clean)))
                .OrderBy(s => s.Name)
                .ToListAsync();
        }

        public async Task<decimal> GetTotalPayablesAsync(string businessId)
        {
            var balances = await _dbSet
                .Where(s => s.BusinessId == businessId && !s.IsDeleted && s.CurrentPayableBalance > 0)
                .Select(s => s.CurrentPayableBalance)
                .ToListAsync();
            return balances.Sum();
        }
    }

    #endregion

    #region Purchase Repository

    public interface IPurchaseRepository : IRepository<Purchase>
    {
        Task<Purchase?> GetPurchaseWithItemsAsync(string purchaseId, string businessId);
        Task<List<Purchase>> GetPurchasesHistoryAsync(string businessId, DateTime? startDate = null, DateTime? endDate = null, string? supplierId = null, string? searchTerm = null);
        Task<string> GenerateNextPurchaseNumberAsync(string businessId);
    }

    public class PurchaseRepository : Repository<Purchase>, IPurchaseRepository
    {
        public PurchaseRepository(AppDbContext context) : base(context)
        {
        }

        public async Task<Purchase?> GetPurchaseWithItemsAsync(string purchaseId, string businessId)
        {
            var purchase = await _dbSet
                .Where(p => p.Id == purchaseId && p.BusinessId == businessId && !p.IsDeleted)
                .FirstOrDefaultAsync();

            if (purchase != null)
            {
                purchase.Items = await _context.Set<PurchaseItem>()
                    .Where(i => i.PurchaseId == purchaseId)
                    .ToListAsync();
            }

            return purchase;
        }

        public async Task<List<Purchase>> GetPurchasesHistoryAsync(
            string businessId,
            DateTime? startDate = null,
            DateTime? endDate = null,
            string? supplierId = null,
            string? searchTerm = null)
        {
            var query = _dbSet.Where(p => p.BusinessId == businessId && !p.IsDeleted);

            if (startDate.HasValue) query = query.Where(p => p.PurchaseDate >= startDate.Value);
            if (endDate.HasValue) query = query.Where(p => p.PurchaseDate <= endDate.Value);
            if (!string.IsNullOrWhiteSpace(supplierId)) query = query.Where(p => p.SupplierId == supplierId);

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var term = searchTerm.Trim().ToLowerInvariant();
                query = query.Where(p => p.BillNumber.ToLower().Contains(term) ||
                                         p.SupplierName.ToLower().Contains(term) ||
                                         p.Notes.ToLower().Contains(term));
            }

            var purchases = await query.OrderByDescending(p => p.PurchaseDate).ToListAsync();
            
            var purchaseIds = purchases.Select(p => p.Id).ToList();
            var allItems = await _context.Set<PurchaseItem>()
                .Where(i => purchaseIds.Contains(i.PurchaseId))
                .ToListAsync();

            var itemsByPurchaseId = allItems.GroupBy(i => i.PurchaseId).ToDictionary(g => g.Key, g => g.ToList());
            foreach (var p in purchases)
            {
                if (itemsByPurchaseId.TryGetValue(p.Id, out var items)) p.Items = items;
            }

            return purchases;
        }

        public async Task<string> GenerateNextPurchaseNumberAsync(string businessId)
        {
            var yearMonth = DateTime.UtcNow.ToString("yyyyMM");
            var prefix = $"PUR-{yearMonth}-";

            var lastPurchase = await _dbSet
                .Where(p => p.BusinessId == businessId && p.BillNumber.StartsWith(prefix))
                .OrderByDescending(p => p.BillNumber)
                .Select(p => p.BillNumber)
                .FirstOrDefaultAsync();

            int nextSeq = 1;
            if (!string.IsNullOrEmpty(lastPurchase))
            {
                var parts = lastPurchase.Split('-');
                if (parts.Length == 3 && int.TryParse(parts[2], out int parsed)) nextSeq = parsed + 1;
            }

            return $"{prefix}{nextSeq:D4}";
        }
    }

    #endregion
}
