using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ModernInventory.Core.Domain.Entities;
using ModernInventory.Core.Infrastructure.Database;

namespace ModernInventory.Core.Application.Repositories
{
    public interface IProductRepository : IRepository<Product>
    {
        Task<Product?> GetBySkuAsync(string sku, string businessId);
        Task<Product?> GetByBarcodeAsync(string barcode, string businessId);
        Task<bool> IsSkuUniqueAsync(string sku, string businessId, string? excludeProductId = null);
        Task<bool> IsBarcodeUniqueAsync(string barcode, string businessId, string? excludeProductId = null);
        Task<List<Product>> SearchAsync(string businessId, string searchTerm);
        Task<List<Product>> GetLowStockAsync(string businessId);
    }

    public class ProductRepository : Repository<Product>, IProductRepository
    {
        public ProductRepository(AppDbContext context) : base(context)
        {
        }

        public async Task<Product?> GetBySkuAsync(string sku, string businessId)
        {
            if (string.IsNullOrWhiteSpace(sku)) return null;
            var cleanSku = sku.Trim();
            return await _dbSet
                .Where(p => p.BusinessId == businessId && !p.IsDeleted && p.Sku.ToLower() == cleanSku.ToLower())
                .FirstOrDefaultAsync();
        }

        public async Task<Product?> GetByBarcodeAsync(string barcode, string businessId)
        {
            if (string.IsNullOrWhiteSpace(barcode)) return null;
            var cleanBarcode = barcode.Trim();
            return await _dbSet
                .Where(p => p.BusinessId == businessId && !p.IsDeleted && p.Barcode.ToLower() == cleanBarcode.ToLower())
                .FirstOrDefaultAsync();
        }

        public async Task<bool> IsSkuUniqueAsync(string sku, string businessId, string? excludeProductId = null)
        {
            if (string.IsNullOrWhiteSpace(sku)) return true;
            var cleanSku = sku.Trim().ToLower();
            var query = _dbSet.Where(p => p.BusinessId == businessId && !p.IsDeleted && p.Sku.ToLower() == cleanSku);
            if (!string.IsNullOrEmpty(excludeProductId))
            {
                query = query.Where(p => p.Id != excludeProductId);
            }
            return !(await query.AnyAsync());
        }

        public async Task<bool> IsBarcodeUniqueAsync(string barcode, string businessId, string? excludeProductId = null)
        {
            if (string.IsNullOrWhiteSpace(barcode)) return true; // Empty barcodes are allowed
            var cleanBarcode = barcode.Trim().ToLower();
            var query = _dbSet.Where(p => p.BusinessId == businessId && !p.IsDeleted && p.Barcode.ToLower() == cleanBarcode);
            if (!string.IsNullOrEmpty(excludeProductId))
            {
                query = query.Where(p => p.Id != excludeProductId);
            }
            return !(await query.AnyAsync());
        }

        public async Task<List<Product>> SearchAsync(string businessId, string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
            {
                return await GetAllAsync(businessId);
            }

            var clean = searchTerm.Trim().ToLower();
            return await _dbSet
                .Where(p => p.BusinessId == businessId && !p.IsDeleted &&
                    (p.Name.ToLower().Contains(clean) ||
                     p.Sku.ToLower().Contains(clean) ||
                     p.Barcode.ToLower().Contains(clean)))
                .OrderBy(p => p.Name)
                .ToListAsync();
        }

        public async Task<List<Product>> GetLowStockAsync(string businessId)
        {
            return await _dbSet
                .Where(p => p.BusinessId == businessId && !p.IsDeleted && p.StockQuantity <= p.MinStockAlert)
                .OrderBy(p => p.StockQuantity)
                .ToListAsync();
        }
    }

    public interface ICategoryRepository : IRepository<Category>
    {
        Task<bool> IsNameUniqueAsync(string name, string businessId, string? excludeCategoryId = null);
        Task<Dictionary<string, int>> GetProductCountsByCategoryAsync(string businessId);
    }

    public class CategoryRepository : Repository<Category>, ICategoryRepository
    {
        public CategoryRepository(AppDbContext context) : base(context)
        {
        }

        public async Task<bool> IsNameUniqueAsync(string name, string businessId, string? excludeCategoryId = null)
        {
            if (string.IsNullOrWhiteSpace(name)) return false;
            var cleanName = name.Trim().ToLower();
            var query = _dbSet.Where(c => c.BusinessId == businessId && !c.IsDeleted && c.Name.ToLower() == cleanName);
            if (!string.IsNullOrEmpty(excludeCategoryId))
            {
                query = query.Where(c => c.Id != excludeCategoryId);
            }
            return !(await query.AnyAsync());
        }

        public async Task<Dictionary<string, int>> GetProductCountsByCategoryAsync(string businessId)
        {
            return await _context.Products
                .Where(p => p.BusinessId == businessId && !p.IsDeleted && !string.IsNullOrEmpty(p.CategoryId))
                .GroupBy(p => p.CategoryId)
                .Select(g => new { CategoryId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(g => g.CategoryId, g => g.Count);
        }
    }

    public interface IStockAdjustmentRepository : IRepository<StockAdjustment>
    {
        Task<List<StockAdjustment>> GetHistoryForProductAsync(string productId, string businessId);
        Task<List<StockAdjustment>> GetAllHistoryAsync(string businessId);
    }

    public class StockAdjustmentRepository : Repository<StockAdjustment>, IStockAdjustmentRepository
    {
        public StockAdjustmentRepository(AppDbContext context) : base(context)
        {
        }

        public async Task<List<StockAdjustment>> GetHistoryForProductAsync(string productId, string businessId)
        {
            return await _dbSet
                .Where(s => s.BusinessId == businessId && s.ProductId == productId && !s.IsDeleted)
                .OrderByDescending(s => s.AdjustmentDate)
                .ToListAsync();
        }

        public async Task<List<StockAdjustment>> GetAllHistoryAsync(string businessId)
        {
            return await _dbSet
                .Where(s => s.BusinessId == businessId && !s.IsDeleted)
                .OrderByDescending(s => s.AdjustmentDate)
                .ToListAsync();
        }
    }
}
