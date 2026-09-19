using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ModernInventory.Core.Application.DTOs;
using ModernInventory.Core.Application.Repositories;
using ModernInventory.Core.Domain.Entities;
using ModernInventory.Core.Domain.Enums;
using ModernInventory.Core.Infrastructure.Database;

namespace ModernInventory.Core.Application.Services
{
    public interface IInventoryService
    {
        // Categories
        Task<List<CategoryListItemDto>> GetCategoriesAsync(string businessId, string? searchTerm = null);
        Task<Category?> GetCategoryByIdAsync(string id, string businessId);
        Task<Category> AddCategoryAsync(string businessId, CreateCategoryDto dto);
        Task<Category> UpdateCategoryAsync(string businessId, UpdateCategoryDto dto);
        Task<bool> DeleteCategoryAsync(string id, string businessId);

        // Products
        Task<List<ProductListItemDto>> GetProductsAsync(string businessId, string? categoryId = null, string? searchTerm = null, bool lowStockOnly = false);
        Task<Product?> GetProductByIdAsync(string id, string businessId);
        Task<Product?> GetProductBySkuAsync(string sku, string businessId);
        Task<Product?> GetProductByBarcodeAsync(string barcode, string businessId);
        Task<Product> AddProductAsync(string businessId, CreateProductDto dto);
        Task<Product> UpdateProductAsync(string businessId, UpdateProductDto dto);
        Task<bool> DeleteProductAsync(string id, string businessId);

        // Search & Barcode Lookup
        Task<List<ProductListItemDto>> SearchProductsAsync(string businessId, string query);
        Task<ProductListItemDto?> LookupByBarcodeAsync(string businessId, string barcode);

        // Stock & Adjustments
        Task<StockAdjustment> AdjustStockAsync(string businessId, StockAdjustmentRequestDto dto);
        Task<List<StockMovementHistoryDto>> GetStockHistoryAsync(string businessId, string? productId = null);
        Task<List<ProductListItemDto>> GetLowStockProductsAsync(string businessId);
        Task<InventoryValuationSummaryDto> GetStockValuationAsync(string businessId);
    }

    public class InventoryService : IInventoryService
    {
        private readonly AppDbContext _context;
        private readonly IProductRepository _productRepo;
        private readonly ICategoryRepository _categoryRepo;
        private readonly IStockAdjustmentRepository _stockAdjustmentRepo;

        public InventoryService(
            AppDbContext context,
            IProductRepository productRepo,
            ICategoryRepository categoryRepo,
            IStockAdjustmentRepository stockAdjustmentRepo)
        {
            _context = context;
            _productRepo = productRepo;
            _categoryRepo = categoryRepo;
            _stockAdjustmentRepo = stockAdjustmentRepo;
        }

        #region Category Management

        public async Task<List<CategoryListItemDto>> GetCategoriesAsync(string businessId, string? searchTerm = null)
        {
            var categories = await _categoryRepo.GetAllAsync(businessId);
            var productCounts = await _categoryRepo.GetProductCountsByCategoryAsync(businessId);

            var query = categories.AsEnumerable();
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var term = searchTerm.Trim().ToLowerInvariant();
                query = query.Where(c => c.Name.ToLowerInvariant().Contains(term) ||
                                         c.Description.ToLowerInvariant().Contains(term));
            }

            return query.Select(c => new CategoryListItemDto
            {
                Id = c.Id,
                Name = c.Name,
                Description = c.Description,
                ProductCount = productCounts.TryGetValue(c.Id, out var count) ? count : 0,
                CreatedAt = c.CreatedAt,
                UpdatedAt = c.UpdatedAt
            }).OrderBy(c => c.Name).ToList();
        }

        public async Task<Category?> GetCategoryByIdAsync(string id, string businessId)
        {
            return await _categoryRepo.GetByIdAsync(id, businessId);
        }

        public async Task<Category> AddCategoryAsync(string businessId, CreateCategoryDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Name))
            {
                throw new ArgumentException("Category name cannot be empty.", nameof(dto.Name));
            }

            var cleanName = dto.Name.Trim();
            var isUnique = await _categoryRepo.IsNameUniqueAsync(cleanName, businessId);
            if (!isUnique)
            {
                throw new InvalidOperationException($"A category with name '{cleanName}' already exists.");
            }

            var category = new Category
            {
                Id = Guid.NewGuid().ToString(),
                BusinessId = businessId,
                Name = cleanName,
                Description = dto.Description?.Trim() ?? string.Empty,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                IsDeleted = false,
                SyncStatus = SyncStatus.PendingCreate
            };

            await _categoryRepo.AddAsync(category);
            return category;
        }

        public async Task<Category> UpdateCategoryAsync(string businessId, UpdateCategoryDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Name))
            {
                throw new ArgumentException("Category name cannot be empty.", nameof(dto.Name));
            }

            var category = await _categoryRepo.GetByIdAsync(dto.Id, businessId);
            if (category == null)
            {
                throw new KeyNotFoundException($"Category with ID '{dto.Id}' not found.");
            }

            var cleanName = dto.Name.Trim();
            var isUnique = await _categoryRepo.IsNameUniqueAsync(cleanName, businessId, excludeCategoryId: dto.Id);
            if (!isUnique)
            {
                throw new InvalidOperationException($"Another category with name '{cleanName}' already exists.");
            }

            category.Name = cleanName;
            category.Description = dto.Description?.Trim() ?? string.Empty;
            category.UpdatedAt = DateTime.UtcNow;

            await _categoryRepo.UpdateAsync(category);
            return category;
        }

        public async Task<bool> DeleteCategoryAsync(string id, string businessId)
        {
            var category = await _categoryRepo.GetByIdAsync(id, businessId);
            if (category == null) return false;

            await _categoryRepo.SoftDeleteAsync(id, businessId);
            return true;
        }

        #endregion

        #region Product Management

        public async Task<List<ProductListItemDto>> GetProductsAsync(
            string businessId,
            string? categoryId = null,
            string? searchTerm = null,
            bool lowStockOnly = false)
        {
            var productsQuery = _context.Products.Where(p => p.BusinessId == businessId && !p.IsDeleted);

            if (!string.IsNullOrEmpty(categoryId))
            {
                productsQuery = productsQuery.Where(p => p.CategoryId == categoryId);
            }

            if (lowStockOnly)
            {
                productsQuery = productsQuery.Where(p => p.StockQuantity <= p.MinStockAlert);
            }

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var term = searchTerm.Trim().ToLower();
                productsQuery = productsQuery.Where(p =>
                    p.Name.ToLower().Contains(term) ||
                    p.Sku.ToLower().Contains(term) ||
                    p.Barcode.ToLower().Contains(term));
            }

            var products = await productsQuery.OrderBy(p => p.Name).ToListAsync();
            var categories = await _context.Categories
                .Where(c => c.BusinessId == businessId && !c.IsDeleted)
                .ToDictionaryAsync(c => c.Id, c => c.Name);

            return products.Select(p => new ProductListItemDto
            {
                Id = p.Id,
                CategoryId = p.CategoryId,
                CategoryName = !string.IsNullOrEmpty(p.CategoryId) && categories.TryGetValue(p.CategoryId, out var catName) ? catName : "Uncategorized",
                Sku = p.Sku,
                Barcode = p.Barcode,
                Name = p.Name,
                Unit = p.Unit,
                CostPrice = p.CostPrice,
                SalePrice = p.SalePrice,
                StockQuantity = p.StockQuantity,
                MinStockAlert = p.MinStockAlert,
                TaxRate = p.TaxRate,
                UpdatedAt = p.UpdatedAt
            }).ToList();
        }

        public async Task<Product?> GetProductByIdAsync(string id, string businessId)
        {
            return await _productRepo.GetByIdAsync(id, businessId);
        }

        public async Task<Product?> GetProductBySkuAsync(string sku, string businessId)
        {
            return await _productRepo.GetBySkuAsync(sku, businessId);
        }

        public async Task<Product?> GetProductByBarcodeAsync(string barcode, string businessId)
        {
            return await _productRepo.GetByBarcodeAsync(barcode, businessId);
        }

        public async Task<Product> AddProductAsync(string businessId, CreateProductDto dto)
        {
            // Validations
            if (string.IsNullOrWhiteSpace(dto.Name))
            {
                throw new ArgumentException("Product name cannot be empty.", nameof(dto.Name));
            }

            if (dto.CostPrice < 0)
            {
                throw new ArgumentException("Cost price cannot be negative.", nameof(dto.CostPrice));
            }

            if (dto.SalePrice < 0)
            {
                throw new ArgumentException("Sale price cannot be negative.", nameof(dto.SalePrice));
            }

            if (dto.TaxRate < 0 || dto.TaxRate > 100)
            {
                throw new ArgumentException("Tax rate must be between 0 and 100.", nameof(dto.TaxRate));
            }

            if (dto.MinStockAlert < 0)
            {
                throw new ArgumentException("Minimum stock alert cannot be negative.", nameof(dto.MinStockAlert));
            }

            if (dto.InitialStock < 0)
            {
                throw new ArgumentException("Initial stock quantity cannot be negative.", nameof(dto.InitialStock));
            }

            var cleanSku = dto.Sku?.Trim() ?? string.Empty;
            if (!string.IsNullOrEmpty(cleanSku))
            {
                var isSkuUnique = await _productRepo.IsSkuUniqueAsync(cleanSku, businessId);
                if (!isSkuUnique)
                {
                    throw new InvalidOperationException($"A product with SKU '{cleanSku}' already exists.");
                }
            }

            var cleanBarcode = dto.Barcode?.Trim() ?? string.Empty;
            if (!string.IsNullOrEmpty(cleanBarcode))
            {
                var isBarcodeUnique = await _productRepo.IsBarcodeUniqueAsync(cleanBarcode, businessId);
                if (!isBarcodeUnique)
                {
                    throw new InvalidOperationException($"A product with Barcode '{cleanBarcode}' already exists.");
                }
            }

            var productId = Guid.NewGuid().ToString();
            var product = new Product
            {
                Id = productId,
                BusinessId = businessId,
                Name = dto.Name.Trim(),
                Sku = cleanSku,
                Barcode = cleanBarcode,
                CategoryId = dto.CategoryId?.Trim() ?? string.Empty,
                Unit = string.IsNullOrWhiteSpace(dto.Unit) ? "Pcs" : dto.Unit.Trim(),
                CostPrice = dto.CostPrice,
                SalePrice = dto.SalePrice,
                TaxRate = dto.TaxRate,
                MinStockAlert = dto.MinStockAlert,
                StockQuantity = dto.InitialStock > 0 ? dto.InitialStock : 0.00m,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                IsDeleted = false,
                SyncStatus = SyncStatus.PendingCreate
            };

            await _context.Products.AddAsync(product);

            // Add Product CREATE to Sync Queue
            await _context.SyncQueue.AddAsync(new SyncQueueItem
            {
                BusinessId = businessId,
                EntityType = nameof(Product),
                EntityId = product.Id,
                OperationType = SyncOperationType.CREATE,
                PayloadJson = JsonSerializer.Serialize(product),
                CreatedAt = DateTime.UtcNow
            });

            // If initial stock was provided, create initial stock adjustment record atomically
            if (dto.InitialStock > 0)
            {
                var initialAdjustment = new StockAdjustment
                {
                    Id = Guid.NewGuid().ToString(),
                    BusinessId = businessId,
                    ProductId = product.Id,
                    Type = StockAdjustmentType.ADDITION,
                    QuantityChanged = dto.InitialStock,
                    CostPerUnit = dto.CostPrice,
                    PreviousQuantity = 0.00m,
                    NewQuantity = dto.InitialStock,
                    Reason = "Initial Opening Stock",
                    AdjustmentDate = DateTime.UtcNow,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    IsDeleted = false,
                    SyncStatus = SyncStatus.PendingCreate
                };

                await _context.StockAdjustments.AddAsync(initialAdjustment);

                await _context.SyncQueue.AddAsync(new SyncQueueItem
                {
                    BusinessId = businessId,
                    EntityType = nameof(StockAdjustment),
                    EntityId = initialAdjustment.Id,
                    OperationType = SyncOperationType.CREATE,
                    PayloadJson = JsonSerializer.Serialize(initialAdjustment),
                    CreatedAt = DateTime.UtcNow
                });
            }

            await _context.SaveChangesAsync();
            return product;
        }

        public async Task<Product> UpdateProductAsync(string businessId, UpdateProductDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Name))
            {
                throw new ArgumentException("Product name cannot be empty.", nameof(dto.Name));
            }

            if (dto.CostPrice < 0)
            {
                throw new ArgumentException("Cost price cannot be negative.", nameof(dto.CostPrice));
            }

            if (dto.SalePrice < 0)
            {
                throw new ArgumentException("Sale price cannot be negative.", nameof(dto.SalePrice));
            }

            if (dto.TaxRate < 0 || dto.TaxRate > 100)
            {
                throw new ArgumentException("Tax rate must be between 0 and 100.", nameof(dto.TaxRate));
            }

            if (dto.MinStockAlert < 0)
            {
                throw new ArgumentException("Minimum stock alert cannot be negative.", nameof(dto.MinStockAlert));
            }

            var product = await _productRepo.GetByIdAsync(dto.Id, businessId);
            if (product == null)
            {
                throw new KeyNotFoundException($"Product with ID '{dto.Id}' not found.");
            }

            var cleanSku = dto.Sku?.Trim() ?? string.Empty;
            if (!string.IsNullOrEmpty(cleanSku))
            {
                var isSkuUnique = await _productRepo.IsSkuUniqueAsync(cleanSku, businessId, excludeProductId: dto.Id);
                if (!isSkuUnique)
                {
                    throw new InvalidOperationException($"Another product with SKU '{cleanSku}' already exists.");
                }
            }

            var cleanBarcode = dto.Barcode?.Trim() ?? string.Empty;
            if (!string.IsNullOrEmpty(cleanBarcode))
            {
                var isBarcodeUnique = await _productRepo.IsBarcodeUniqueAsync(cleanBarcode, businessId, excludeProductId: dto.Id);
                if (!isBarcodeUnique)
                {
                    throw new InvalidOperationException($"Another product with Barcode '{cleanBarcode}' already exists.");
                }
            }

            product.Name = dto.Name.Trim();
            product.Sku = cleanSku;
            product.Barcode = cleanBarcode;
            product.CategoryId = dto.CategoryId?.Trim() ?? string.Empty;
            product.Unit = string.IsNullOrWhiteSpace(dto.Unit) ? "Pcs" : dto.Unit.Trim();
            product.CostPrice = dto.CostPrice;
            product.SalePrice = dto.SalePrice;
            product.TaxRate = dto.TaxRate;
            product.MinStockAlert = dto.MinStockAlert;
            product.UpdatedAt = DateTime.UtcNow;

            // Note: StockQuantity is deliberately preserved to prevent direct unauthorized tampering
            await _productRepo.UpdateAsync(product);
            return product;
        }

        public async Task<bool> DeleteProductAsync(string id, string businessId)
        {
            var product = await _productRepo.GetByIdAsync(id, businessId);
            if (product == null) return false;

            await _productRepo.SoftDeleteAsync(id, businessId);
            return true;
        }

        public async Task<List<ProductListItemDto>> SearchProductsAsync(string businessId, string query)
        {
            return await GetProductsAsync(businessId, searchTerm: query);
        }

        public async Task<ProductListItemDto?> LookupByBarcodeAsync(string businessId, string barcode)
        {
            if (string.IsNullOrWhiteSpace(barcode)) return null;

            var product = await _productRepo.GetByBarcodeAsync(barcode, businessId);
            if (product == null) return null;

            var categoryName = "Uncategorized";
            if (!string.IsNullOrEmpty(product.CategoryId))
            {
                var cat = await _context.Categories.FindAsync(product.CategoryId);
                if (cat != null) categoryName = cat.Name;
            }

            return new ProductListItemDto
            {
                Id = product.Id,
                CategoryId = product.CategoryId,
                CategoryName = categoryName,
                Sku = product.Sku,
                Barcode = product.Barcode,
                Name = product.Name,
                Unit = product.Unit,
                CostPrice = product.CostPrice,
                SalePrice = product.SalePrice,
                StockQuantity = product.StockQuantity,
                MinStockAlert = product.MinStockAlert,
                TaxRate = product.TaxRate,
                UpdatedAt = product.UpdatedAt
            };
        }

        #endregion

        #region Stock Adjustments & History

        public async Task<StockAdjustment> AdjustStockAsync(string businessId, StockAdjustmentRequestDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.ProductId))
            {
                throw new ArgumentException("Product ID is required for stock adjustment.", nameof(dto.ProductId));
            }

            if (dto.Quantity <= 0)
            {
                throw new ArgumentException("Adjustment quantity must be greater than zero.", nameof(dto.Quantity));
            }

            // Using transaction for atomicity: Product Update + StockAdjustment Record + SyncQueue
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var product = await _context.Products
                    .Where(p => p.Id == dto.ProductId && p.BusinessId == businessId && !p.IsDeleted)
                    .FirstOrDefaultAsync();

                if (product == null)
                {
                    throw new KeyNotFoundException($"Product '{dto.ProductId}' not found.");
                }

                var previousQuantity = product.StockQuantity;
                decimal newQuantity;

                switch (dto.Type)
                {
                    case StockAdjustmentType.ADDITION:
                        newQuantity = previousQuantity + dto.Quantity;
                        break;

                    case StockAdjustmentType.DAMAGE:
                    case StockAdjustmentType.THEFT:
                    case StockAdjustmentType.EXPIRED:
                        if (previousQuantity - dto.Quantity < 0)
                        {
                            throw new InvalidOperationException($"Cannot reduce stock by {dto.Quantity}. Current stock is {previousQuantity}. Negative stock is not permitted.");
                        }
                        newQuantity = previousQuantity - dto.Quantity;
                        break;

                    case StockAdjustmentType.AUDIT_CORRECTION:
                        // Audit correction quantity delta
                        // If reason or client intends a target count, quantity is the net change
                        // If delta would result in negative stock, block it
                        newQuantity = previousQuantity + dto.Quantity;
                        if (newQuantity < 0)
                        {
                            throw new InvalidOperationException($"Audit correction resulted in negative stock ({newQuantity}). Operation cancelled.");
                        }
                        break;

                    default:
                        throw new ArgumentOutOfRangeException(nameof(dto.Type), $"Unsupported adjustment type: {dto.Type}");
                }

                // Update product stock
                product.StockQuantity = newQuantity;
                product.UpdatedAt = DateTime.UtcNow;
                product.SyncStatus = SyncStatus.PendingUpdate;

                // Create adjustment record
                var adjustment = new StockAdjustment
                {
                    Id = Guid.NewGuid().ToString(),
                    BusinessId = businessId,
                    ProductId = product.Id,
                    Type = dto.Type,
                    QuantityChanged = dto.Quantity,
                    CostPerUnit = dto.CostPerUnit > 0 ? dto.CostPerUnit : product.CostPrice,
                    PreviousQuantity = previousQuantity,
                    NewQuantity = newQuantity,
                    Reason = dto.Reason?.Trim() ?? string.Empty,
                    AdjustmentDate = dto.Date ?? DateTime.UtcNow,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    IsDeleted = false,
                    SyncStatus = SyncStatus.PendingCreate
                };

                await _context.StockAdjustments.AddAsync(adjustment);

                // Enqueue SyncQueue for both StockAdjustment (CREATE) and Product (UPDATE)
                await _context.SyncQueue.AddAsync(new SyncQueueItem
                {
                    BusinessId = businessId,
                    EntityType = nameof(StockAdjustment),
                    EntityId = adjustment.Id,
                    OperationType = SyncOperationType.CREATE,
                    PayloadJson = JsonSerializer.Serialize(adjustment),
                    CreatedAt = DateTime.UtcNow
                });

                await _context.SyncQueue.AddAsync(new SyncQueueItem
                {
                    BusinessId = businessId,
                    EntityType = nameof(Product),
                    EntityId = product.Id,
                    OperationType = SyncOperationType.UPDATE,
                    PayloadJson = JsonSerializer.Serialize(product),
                    CreatedAt = DateTime.UtcNow
                });

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return adjustment;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<List<StockMovementHistoryDto>> GetStockHistoryAsync(string businessId, string? productId = null)
        {
            var adjustmentsQuery = _context.StockAdjustments.Where(s => s.BusinessId == businessId && !s.IsDeleted);

            if (!string.IsNullOrEmpty(productId))
            {
                adjustmentsQuery = adjustmentsQuery.Where(s => s.ProductId == productId);
            }

            var adjustments = await adjustmentsQuery
                .OrderByDescending(s => s.AdjustmentDate)
                .ThenByDescending(s => s.CreatedAt)
                .ToListAsync();

            var productIds = adjustments.Select(a => a.ProductId).Distinct().ToList();
            var products = await _context.Products
                .Where(p => productIds.Contains(p.Id))
                .ToDictionaryAsync(p => p.Id, p => new { p.Name, p.Sku });

            return adjustments.Select(a =>
            {
                var prodInfo = products.TryGetValue(a.ProductId, out var prod) ? prod : null;
                return new StockMovementHistoryDto
                {
                    Id = a.Id,
                    ProductId = a.ProductId,
                    ProductName = prodInfo?.Name ?? "Unknown Product",
                    Sku = prodInfo?.Sku ?? "-",
                    Type = a.Type,
                    QuantityChanged = a.QuantityChanged,
                    CostPerUnit = a.CostPerUnit,
                    PreviousQuantity = a.PreviousQuantity,
                    NewQuantity = a.NewQuantity,
                    Reason = a.Reason,
                    AdjustmentDate = a.AdjustmentDate,
                    CreatedAt = a.CreatedAt
                };
            }).ToList();
        }

        public async Task<List<ProductListItemDto>> GetLowStockProductsAsync(string businessId)
        {
            return await GetProductsAsync(businessId, lowStockOnly: true);
        }

        public async Task<InventoryValuationSummaryDto> GetStockValuationAsync(string businessId)
        {
            var products = await _context.Products
                .Where(p => p.BusinessId == businessId && !p.IsDeleted)
                .Select(p => new
                {
                    p.StockQuantity,
                    p.CostPrice,
                    p.SalePrice,
                    p.MinStockAlert
                })
                .ToListAsync();

            return new InventoryValuationSummaryDto
            {
                TotalProductCount = products.Count,
                TotalStockQuantity = products.Sum(p => p.StockQuantity),
                TotalCostValuation = products.Sum(p => p.StockQuantity * p.CostPrice),
                TotalRetailValuation = products.Sum(p => p.StockQuantity * p.SalePrice),
                LowStockCount = products.Count(p => p.StockQuantity <= p.MinStockAlert),
                OutOfStockCount = products.Count(p => p.StockQuantity <= 0)
            };
        }

        #endregion
    }
}
