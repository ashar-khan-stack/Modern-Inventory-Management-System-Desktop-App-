using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ModernInventory.Core.Domain.Entities;
using ModernInventory.Core.Infrastructure.Database;

namespace ModernInventory.Core.Application.Repositories
{
    #region Customer Repository

    public interface ICustomerRepository : IRepository<Customer>
    {
        Task<Customer?> GetByPhoneAsync(string phone, string businessId);
        Task<Customer?> GetByCnicAsync(string cnic, string businessId);
        Task<List<Customer>> SearchAsync(string businessId, string searchTerm);
        Task<decimal> GetTotalReceivablesAsync(string businessId);
        Task<bool> IsPhoneUniqueAsync(string phone, string businessId, string? excludeId = null);
        Task<int> GetInvoicesCountAsync(string customerId, string businessId);
        Task<decimal> GetTotalPurchasesAmountAsync(string customerId, string businessId);
    }

    public class CustomerRepository : Repository<Customer>, ICustomerRepository
    {
        public CustomerRepository(AppDbContext context) : base(context)
        {
        }

        public async Task<Customer?> GetByPhoneAsync(string phone, string businessId)
        {
            if (string.IsNullOrWhiteSpace(phone)) return null;
            var cleanPhone = phone.Trim();
            return await _dbSet
                .Where(c => c.BusinessId == businessId && !c.IsDeleted && c.Phone == cleanPhone)
                .FirstOrDefaultAsync();
        }

        public async Task<Customer?> GetByCnicAsync(string cnic, string businessId)
        {
            if (string.IsNullOrWhiteSpace(cnic)) return null;
            var cleanCnic = cnic.Trim();
            return await _dbSet
                .Where(c => c.BusinessId == businessId && !c.IsDeleted && c.Cnic == cleanCnic)
                .FirstOrDefaultAsync();
        }

        public async Task<List<Customer>> SearchAsync(string businessId, string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
            {
                return await GetAllAsync(businessId);
            }

            var clean = searchTerm.Trim().ToLowerInvariant();
            return await _dbSet
                .Where(c => c.BusinessId == businessId && !c.IsDeleted &&
                    (c.Name.ToLower().Contains(clean) ||
                     c.Phone.ToLower().Contains(clean) ||
                     c.Email.ToLower().Contains(clean) ||
                     c.CompanyName.ToLower().Contains(clean) ||
                     c.Cnic.ToLower().Contains(clean)))
                .OrderBy(c => c.Name)
                .ToListAsync();
        }

        public async Task<decimal> GetTotalReceivablesAsync(string businessId)
        {
            var balances = await _dbSet
                .Where(c => c.BusinessId == businessId && !c.IsDeleted && c.CurrentBalance > 0)
                .Select(c => c.CurrentBalance)
                .ToListAsync();
            return balances.Sum();
        }

        public async Task<bool> IsPhoneUniqueAsync(string phone, string businessId, string? excludeId = null)
        {
            if (string.IsNullOrWhiteSpace(phone)) return true;
            var clean = phone.Trim().ToLowerInvariant();
            var query = _dbSet.Where(c => c.BusinessId == businessId && !c.IsDeleted && c.Phone.ToLower() == clean);
            if (!string.IsNullOrEmpty(excludeId))
            {
                query = query.Where(c => c.Id != excludeId);
            }
            return !(await query.AnyAsync());
        }

        public async Task<int> GetInvoicesCountAsync(string customerId, string businessId)
        {
            return await _context.Sales
                .Where(s => s.BusinessId == businessId && s.CustomerId == customerId && !s.IsDeleted)
                .CountAsync();
        }

        public async Task<decimal> GetTotalPurchasesAmountAsync(string customerId, string businessId)
        {
            var totals = await _context.Sales
                .Where(s => s.BusinessId == businessId && s.CustomerId == customerId && !s.IsDeleted)
                .Select(s => s.GrandTotal)
                .ToListAsync();
            return totals.Sum();
        }
    }

    #endregion

    #region Employee Repository

    public interface IEmployeeRepository : IRepository<Employee>
    {
        Task<Employee?> GetByCnicAsync(string cnic, string businessId);
        Task<Employee?> GetByPhoneAsync(string phone, string businessId);
        Task<List<Employee>> SearchAsync(string businessId, string searchTerm);
        Task<List<Employee>> GetActiveEmployeesAsync(string businessId);
        Task<bool> IsCnicUniqueAsync(string cnic, string businessId, string? excludeId = null);
    }

    public class EmployeeRepository : Repository<Employee>, IEmployeeRepository
    {
        public EmployeeRepository(AppDbContext context) : base(context)
        {
        }

        public async Task<Employee?> GetByCnicAsync(string cnic, string businessId)
        {
            if (string.IsNullOrWhiteSpace(cnic)) return null;
            var cleanCnic = cnic.Trim();
            return await _dbSet
                .Where(e => e.BusinessId == businessId && !e.IsDeleted && e.Cnic == cleanCnic)
                .FirstOrDefaultAsync();
        }

        public async Task<Employee?> GetByPhoneAsync(string phone, string businessId)
        {
            if (string.IsNullOrWhiteSpace(phone)) return null;
            var cleanPhone = phone.Trim();
            return await _dbSet
                .Where(e => e.BusinessId == businessId && !e.IsDeleted && e.Phone == cleanPhone)
                .FirstOrDefaultAsync();
        }

        public async Task<List<Employee>> SearchAsync(string businessId, string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
            {
                return await GetAllAsync(businessId);
            }

            var clean = searchTerm.Trim().ToLowerInvariant();
            return await _dbSet
                .Where(e => e.BusinessId == businessId && !e.IsDeleted &&
                    (e.Name.ToLower().Contains(clean) ||
                     e.Designation.ToLower().Contains(clean) ||
                     e.Phone.ToLower().Contains(clean) ||
                     e.Cnic.ToLower().Contains(clean)))
                .OrderBy(e => e.Name)
                .ToListAsync();
        }

        public async Task<List<Employee>> GetActiveEmployeesAsync(string businessId)
        {
            return await _dbSet
                .Where(e => e.BusinessId == businessId && !e.IsDeleted && e.IsActive)
                .OrderBy(e => e.Name)
                .ToListAsync();
        }

        public async Task<bool> IsCnicUniqueAsync(string cnic, string businessId, string? excludeId = null)
        {
            if (string.IsNullOrWhiteSpace(cnic)) return true;
            var clean = cnic.Trim().ToLowerInvariant();
            var query = _dbSet.Where(e => e.BusinessId == businessId && !e.IsDeleted && e.Cnic.ToLower() == clean);
            if (!string.IsNullOrEmpty(excludeId))
            {
                query = query.Where(e => e.Id != excludeId);
            }
            return !(await query.AnyAsync());
        }
    }

    #endregion

    #region Sale Repository

    public interface ISaleRepository : IRepository<Sale>
    {
        Task<Sale?> GetSaleWithItemsAsync(string saleId, string businessId);
        Task<List<Sale>> GetSalesHistoryAsync(string businessId, DateTime? startDate = null, DateTime? endDate = null, string? customerId = null, string? searchTerm = null);
        Task<string> GenerateNextInvoiceNumberAsync(string businessId);
        Task<List<SaleItem>> GetSaleItemsBySaleIdAsync(string saleId);
        Task<List<SaleItem>> GetSaleItemsForProductAsync(string productId, string businessId);
        Task<List<Sale>> GetRecentSalesAsync(string businessId, int limit = 10);
    }

    public class SaleRepository : Repository<Sale>, ISaleRepository
    {
        public SaleRepository(AppDbContext context) : base(context)
        {
        }

        public async Task<Sale?> GetSaleWithItemsAsync(string saleId, string businessId)
        {
            var sale = await _dbSet
                .Where(s => s.Id == saleId && s.BusinessId == businessId && !s.IsDeleted)
                .FirstOrDefaultAsync();

            if (sale != null)
            {
                sale.Items = await _context.SaleItems
                    .Where(i => i.SaleId == saleId)
                    .ToListAsync();
            }

            return sale;
        }

        public async Task<List<Sale>> GetSalesHistoryAsync(
            string businessId,
            DateTime? startDate = null,
            DateTime? endDate = null,
            string? customerId = null,
            string? searchTerm = null)
        {
            var query = _dbSet.Where(s => s.BusinessId == businessId && !s.IsDeleted);

            if (startDate.HasValue)
            {
                query = query.Where(s => s.SaleDate >= startDate.Value);
            }

            if (endDate.HasValue)
            {
                query = query.Where(s => s.SaleDate <= endDate.Value);
            }

            if (!string.IsNullOrWhiteSpace(customerId))
            {
                query = query.Where(s => s.CustomerId == customerId);
            }

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var term = searchTerm.Trim().ToLowerInvariant();
                query = query.Where(s => s.InvoiceNumber.ToLower().Contains(term) ||
                                         s.Notes.ToLower().Contains(term));
            }

            var sales = await query.OrderByDescending(s => s.SaleDate).ToListAsync();

            // Load items for each sale
            var saleIds = sales.Select(s => s.Id).ToList();
            var allItems = await _context.SaleItems
                .Where(i => saleIds.Contains(i.SaleId))
                .ToListAsync();

            var itemsBySaleId = allItems.GroupBy(i => i.SaleId).ToDictionary(g => g.Key, g => g.ToList());
            foreach (var s in sales)
            {
                if (itemsBySaleId.TryGetValue(s.Id, out var items))
                {
                    s.Items = items;
                }
            }

            return sales;
        }

        public async Task<string> GenerateNextInvoiceNumberAsync(string businessId)
        {
            var yearMonth = DateTime.UtcNow.ToString("yyyyMM");
            var prefix = $"INV-{yearMonth}-";

            var lastInvoice = await _dbSet
                .Where(s => s.BusinessId == businessId && s.InvoiceNumber.StartsWith(prefix))
                .OrderByDescending(s => s.InvoiceNumber)
                .Select(s => s.InvoiceNumber)
                .FirstOrDefaultAsync();

            int nextSeq = 1;
            if (!string.IsNullOrEmpty(lastInvoice))
            {
                var parts = lastInvoice.Split('-');
                if (parts.Length == 3 && int.TryParse(parts[2], out int parsed))
                {
                    nextSeq = parsed + 1;
                }
            }

            return $"{prefix}{nextSeq:D4}";
        }

        public async Task<List<SaleItem>> GetSaleItemsBySaleIdAsync(string saleId)
        {
            return await _context.SaleItems
                .Where(i => i.SaleId == saleId)
                .ToListAsync();
        }

        public async Task<List<SaleItem>> GetSaleItemsForProductAsync(string productId, string businessId)
        {
            var saleIds = await _dbSet
                .Where(s => s.BusinessId == businessId && !s.IsDeleted)
                .Select(s => s.Id)
                .ToListAsync();

            return await _context.SaleItems
                .Where(i => i.ProductId == productId && saleIds.Contains(i.SaleId))
                .ToListAsync();
        }

        public async Task<List<Sale>> GetRecentSalesAsync(string businessId, int limit = 10)
        {
            return await _dbSet
                .Where(s => s.BusinessId == businessId && !s.IsDeleted)
                .OrderByDescending(s => s.SaleDate)
                .Take(limit)
                .ToListAsync();
        }
    }

    #endregion
}
