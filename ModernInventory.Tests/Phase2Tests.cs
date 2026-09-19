using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using ModernInventory.Core.Application.DTOs;
using ModernInventory.Core.Application.Repositories;
using ModernInventory.Core.Application.Services;
using ModernInventory.Core.Domain.Entities;
using ModernInventory.Core.Domain.Enums;
using ModernInventory.Core.Infrastructure.Database;
using Xunit;

namespace ModernInventory.Tests
{
    public class Phase2InventoryTests : IDisposable
    {
        private readonly SqliteConnection _connection;
        private readonly AppDbContext _context;
        private readonly IProductRepository _productRepo;
        private readonly ICategoryRepository _categoryRepo;
        private readonly IStockAdjustmentRepository _stockAdjustmentRepo;
        private readonly IInventoryService _inventoryService;
        private readonly IDashboardService _dashboardService;

        private const string TestBusinessA = "biz-corp-alpha";
        private const string TestBusinessB = "biz-corp-beta";

        public Phase2InventoryTests()
        {
            _connection = new SqliteConnection("Data Source=:memory:");
            _connection.Open();

            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite(_connection)
                .Options;

            _context = new AppDbContext(options);
            _context.Database.EnsureCreated();

            _productRepo = new ProductRepository(_context);
            _categoryRepo = new CategoryRepository(_context);
            _stockAdjustmentRepo = new StockAdjustmentRepository(_context);
            _inventoryService = new InventoryService(_context, _productRepo, _categoryRepo, _stockAdjustmentRepo);
            _dashboardService = new DashboardService(_context);
        }

        public void Dispose()
        {
            _context.Dispose();
            _connection.Dispose();
        }

        [Fact]
        public async Task Test01_CreateCategory_SuccessAndSyncQueueRecorded()
        {
            var category = await _inventoryService.AddCategoryAsync(TestBusinessA, new CreateCategoryDto
            {
                Name = "Beverages & Drinks",
                Description = "Cold drinks and juices"
            });

            Assert.NotNull(category);
            Assert.Equal("Beverages & Drinks", category.Name);
            Assert.Equal(TestBusinessA, category.BusinessId);
            Assert.False(category.IsDeleted);

            // Verify Sync Queue
            var syncItem = await _context.SyncQueue
                .FirstOrDefaultAsync(q => q.EntityId == category.Id && q.EntityType == nameof(Category));
            Assert.NotNull(syncItem);
            Assert.Equal(SyncOperationType.CREATE, syncItem.OperationType);
        }

        [Fact]
        public async Task Test02_DuplicateCategoryName_ThrowsInvalidOperationException()
        {
            await _inventoryService.AddCategoryAsync(TestBusinessA, new CreateCategoryDto
            {
                Name = "Electronics"
            });

            // Same name, same business -> Must fail
            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                await _inventoryService.AddCategoryAsync(TestBusinessA, new CreateCategoryDto
                {
                    Name = "electronics" // Case-insensitive check
                });
            });
        }

        [Fact]
        public async Task Test03_DuplicateCategoryAcrossDifferentBusinesses_Allowed()
        {
            var catA = await _inventoryService.AddCategoryAsync(TestBusinessA, new CreateCategoryDto
            {
                Name = "Hardware"
            });

            var catB = await _inventoryService.AddCategoryAsync(TestBusinessB, new CreateCategoryDto
            {
                Name = "Hardware"
            });

            Assert.NotNull(catA);
            Assert.NotNull(catB);
            Assert.NotEqual(catA.Id, catB.Id);
            Assert.Equal(TestBusinessA, catA.BusinessId);
            Assert.Equal(TestBusinessB, catB.BusinessId);
        }

        [Fact]
        public async Task Test04_UpdateCategory_Success()
        {
            var category = await _inventoryService.AddCategoryAsync(TestBusinessA, new CreateCategoryDto
            {
                Name = "Old Category Name",
                Description = "Old desc"
            });

            var updated = await _inventoryService.UpdateCategoryAsync(TestBusinessA, new UpdateCategoryDto
            {
                Id = category.Id,
                Name = "New Category Name",
                Description = "Updated desc"
            });

            Assert.Equal("New Category Name", updated.Name);
            Assert.Equal("Updated desc", updated.Description);

            // Verify Sync Queue UPDATE
            var syncUpdate = await _context.SyncQueue
                .FirstOrDefaultAsync(q => q.EntityId == category.Id && q.OperationType == SyncOperationType.UPDATE);
            Assert.NotNull(syncUpdate);
        }

        [Fact]
        public async Task Test05_SoftDeleteCategory_Success()
        {
            var category = await _inventoryService.AddCategoryAsync(TestBusinessA, new CreateCategoryDto
            {
                Name = "To Delete"
            });

            var deleted = await _inventoryService.DeleteCategoryAsync(category.Id, TestBusinessA);
            Assert.True(deleted);

            var inDb = await _context.Categories.FindAsync(category.Id);
            Assert.NotNull(inDb);
            Assert.True(inDb.IsDeleted);

            var activeList = await _inventoryService.GetCategoriesAsync(TestBusinessA);
            Assert.DoesNotContain(activeList, c => c.Id == category.Id);
        }

        [Fact]
        public async Task Test06_CreateProduct_WithInitialStock_CreatesAdjustmentAtomically()
        {
            var cat = await _inventoryService.AddCategoryAsync(TestBusinessA, new CreateCategoryDto { Name = "Spices" });

            var product = await _inventoryService.AddProductAsync(TestBusinessA, new CreateProductDto
            {
                Name = "National Black Pepper 50g",
                Sku = "SKU-PEPPER-01",
                Barcode = "896400012345",
                CategoryId = cat.Id,
                Unit = "Pack",
                CostPrice = 120.00m,
                SalePrice = 160.00m,
                TaxRate = 18.00m,
                MinStockAlert = 10.00m,
                InitialStock = 50.00m
            });

            Assert.NotNull(product);
            Assert.Equal(50.00m, product.StockQuantity);
            Assert.Equal("National Black Pepper 50g", product.Name);

            // Check initial StockAdjustment
            var adjustment = await _context.StockAdjustments
                .FirstOrDefaultAsync(s => s.ProductId == product.Id && s.BusinessId == TestBusinessA);
            Assert.NotNull(adjustment);
            Assert.Equal(StockAdjustmentType.ADDITION, adjustment.Type);
            Assert.Equal(50.00m, adjustment.QuantityChanged);
            Assert.Equal(120.00m, adjustment.CostPerUnit);
            Assert.Equal(0.00m, adjustment.PreviousQuantity);
            Assert.Equal(50.00m, adjustment.NewQuantity);

            // Check sync queue has both product and adjustment
            var productSync = await _context.SyncQueue.FirstOrDefaultAsync(q => q.EntityId == product.Id && q.EntityType == nameof(Product));
            var adjustSync = await _context.SyncQueue.FirstOrDefaultAsync(q => q.EntityId == adjustment.Id && q.EntityType == nameof(StockAdjustment));

            Assert.NotNull(productSync);
            Assert.NotNull(adjustSync);
        }

        [Fact]
        public async Task Test07_SkuUniqueness_EnforcedWithinBusiness()
        {
            await _inventoryService.AddProductAsync(TestBusinessA, new CreateProductDto
            {
                Name = "Product 1",
                Sku = "UNIQUE-SKU-100",
                CostPrice = 10,
                SalePrice = 15
            });

            // Same SKU in same business -> Must fail
            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                await _inventoryService.AddProductAsync(TestBusinessA, new CreateProductDto
                {
                    Name = "Product 2",
                    Sku = "unique-sku-100",
                    CostPrice = 20,
                    SalePrice = 25
                });
            });

            // Same SKU in different business -> Allowed
            var prodB = await _inventoryService.AddProductAsync(TestBusinessB, new CreateProductDto
            {
                Name = "Product In Biz B",
                Sku = "UNIQUE-SKU-100",
                CostPrice = 10,
                SalePrice = 15
            });
            Assert.NotNull(prodB);
        }

        [Fact]
        public async Task Test08_BarcodeUniqueness_EnforcedWithinBusiness()
        {
            await _inventoryService.AddProductAsync(TestBusinessA, new CreateProductDto
            {
                Name = "Product Barcode 1",
                Barcode = "1234567890128",
                CostPrice = 10,
                SalePrice = 15
            });

            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                await _inventoryService.AddProductAsync(TestBusinessA, new CreateProductDto
                {
                    Name = "Product Barcode 2",
                    Barcode = "1234567890128",
                    CostPrice = 20,
                    SalePrice = 25
                });
            });
        }

        [Fact]
        public async Task Test09_UpdateProduct_PreservesStockQuantity()
        {
            var product = await _inventoryService.AddProductAsync(TestBusinessA, new CreateProductDto
            {
                Name = "Original Product",
                Sku = "SKU-ORIG",
                CostPrice = 100,
                SalePrice = 150,
                InitialStock = 25
            });

            var updated = await _inventoryService.UpdateProductAsync(TestBusinessA, new UpdateProductDto
            {
                Id = product.Id,
                Name = "Updated Product Name",
                Sku = "SKU-MODIFIED",
                CostPrice = 110,
                SalePrice = 165,
                TaxRate = 18,
                MinStockAlert = 5
            });

            Assert.Equal("Updated Product Name", updated.Name);
            Assert.Equal("SKU-MODIFIED", updated.Sku);
            Assert.Equal(25.00m, updated.StockQuantity); // Stock not tampered by product update
        }

        [Fact]
        public async Task Test10_AdjustStock_Addition_IncreasesStock()
        {
            var product = await _inventoryService.AddProductAsync(TestBusinessA, new CreateProductDto
            {
                Name = "Tape Rolls",
                CostPrice = 50,
                SalePrice = 80,
                InitialStock = 10
            });

            var adjustment = await _inventoryService.AdjustStockAsync(TestBusinessA, new StockAdjustmentRequestDto
            {
                ProductId = product.Id,
                Type = StockAdjustmentType.ADDITION,
                Quantity = 15,
                Reason = "Purchased from supplier"
            });

            Assert.Equal(10.00m, adjustment.PreviousQuantity);
            Assert.Equal(25.00m, adjustment.NewQuantity);

            var reloaded = await _inventoryService.GetProductByIdAsync(product.Id, TestBusinessA);
            Assert.Equal(25.00m, reloaded!.StockQuantity);
        }

        [Fact]
        public async Task Test11_AdjustStock_Damage_ReducesStock()
        {
            var product = await _inventoryService.AddProductAsync(TestBusinessA, new CreateProductDto
            {
                Name = "Glass Bottles",
                CostPrice = 200,
                SalePrice = 300,
                InitialStock = 20
            });

            var adjustment = await _inventoryService.AdjustStockAsync(TestBusinessA, new StockAdjustmentRequestDto
            {
                ProductId = product.Id,
                Type = StockAdjustmentType.DAMAGE,
                Quantity = 4,
                Reason = "Broken during offloading"
            });

            Assert.Equal(20.00m, adjustment.PreviousQuantity);
            Assert.Equal(16.00m, adjustment.NewQuantity);

            var reloaded = await _inventoryService.GetProductByIdAsync(product.Id, TestBusinessA);
            Assert.Equal(16.00m, reloaded!.StockQuantity);
        }

        [Fact]
        public async Task Test12_AdjustStock_Theft_ReducesStock()
        {
            var product = await _inventoryService.AddProductAsync(TestBusinessA, new CreateProductDto
            {
                Name = "Memory Cards 64GB",
                CostPrice = 1000,
                SalePrice = 1500,
                InitialStock = 10
            });

            var adjustment = await _inventoryService.AdjustStockAsync(TestBusinessA, new StockAdjustmentRequestDto
            {
                ProductId = product.Id,
                Type = StockAdjustmentType.THEFT,
                Quantity = 2,
                Reason = "Missing from display counter"
            });

            Assert.Equal(10.00m, adjustment.PreviousQuantity);
            Assert.Equal(8.00m, adjustment.NewQuantity);
        }

        [Fact]
        public async Task Test13_AdjustStock_Expired_ReducesStock()
        {
            var product = await _inventoryService.AddProductAsync(TestBusinessA, new CreateProductDto
            {
                Name = "Milk Pack 1L",
                CostPrice = 240,
                SalePrice = 280,
                InitialStock = 15
            });

            var adjustment = await _inventoryService.AdjustStockAsync(TestBusinessA, new StockAdjustmentRequestDto
            {
                ProductId = product.Id,
                Type = StockAdjustmentType.EXPIRED,
                Quantity = 5,
                Reason = "Past expiration date"
            });

            Assert.Equal(15.00m, adjustment.PreviousQuantity);
            Assert.Equal(10.00m, adjustment.NewQuantity);
        }

        [Fact]
        public async Task Test14_NegativeStockPrevention_ThrowsInvalidOperationException()
        {
            var product = await _inventoryService.AddProductAsync(TestBusinessA, new CreateProductDto
            {
                Name = "Limited Item",
                CostPrice = 500,
                SalePrice = 750,
                InitialStock = 5
            });

            // Trying to damage/reduce 8 items when only 5 exist
            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                await _inventoryService.AdjustStockAsync(TestBusinessA, new StockAdjustmentRequestDto
                {
                    ProductId = product.Id,
                    Type = StockAdjustmentType.DAMAGE,
                    Quantity = 8,
                    Reason = "Excess reduction"
                });
            });

            // Product stock should remain unchanged
            var reloaded = await _inventoryService.GetProductByIdAsync(product.Id, TestBusinessA);
            Assert.Equal(5.00m, reloaded!.StockQuantity);
        }

        [Fact]
        public async Task Test15_AuditCorrection_StockAdjustment()
        {
            var product = await _inventoryService.AddProductAsync(TestBusinessA, new CreateProductDto
            {
                Name = "Screws Box",
                CostPrice = 50,
                SalePrice = 80,
                InitialStock = 100
            });

            // Net change +15 from audit count
            var adjustment = await _inventoryService.AdjustStockAsync(TestBusinessA, new StockAdjustmentRequestDto
            {
                ProductId = product.Id,
                Type = StockAdjustmentType.AUDIT_CORRECTION,
                Quantity = 15,
                Reason = "Physical audit count revealed 15 extra"
            });

            Assert.Equal(100.00m, adjustment.PreviousQuantity);
            Assert.Equal(115.00m, adjustment.NewQuantity);
        }

        [Fact]
        public async Task Test16_StockHistoryTracking_ReturnsChronologicalLogs()
        {
            var product = await _inventoryService.AddProductAsync(TestBusinessA, new CreateProductDto
            {
                Name = "Cement Bag 50kg",
                Sku = "CEM-50",
                CostPrice = 1200,
                SalePrice = 1450,
                InitialStock = 100
            });

            await _inventoryService.AdjustStockAsync(TestBusinessA, new StockAdjustmentRequestDto
            {
                ProductId = product.Id,
                Type = StockAdjustmentType.DAMAGE,
                Quantity = 3,
                Reason = "Rain damaged"
            });

            await _inventoryService.AdjustStockAsync(TestBusinessA, new StockAdjustmentRequestDto
            {
                ProductId = product.Id,
                Type = StockAdjustmentType.ADDITION,
                Quantity = 50,
                Reason = "Restock from distributor"
            });

            var history = await _inventoryService.GetStockHistoryAsync(TestBusinessA, product.Id);
            Assert.Equal(3, history.Count); // Initial + Damage + Restock
            Assert.All(history, h => Assert.Equal(product.Id, h.ProductId));
        }

        [Fact]
        public async Task Test17_LowStockDetection_AndExclusion()
        {
            var lowProduct = await _inventoryService.AddProductAsync(TestBusinessA, new CreateProductDto
            {
                Name = "Low Stock Item",
                CostPrice = 10,
                SalePrice = 20,
                MinStockAlert = 10,
                InitialStock = 5 // Low
            });

            var normalProduct = await _inventoryService.AddProductAsync(TestBusinessA, new CreateProductDto
            {
                Name = "Normal Stock Item",
                CostPrice = 10,
                SalePrice = 20,
                MinStockAlert = 5,
                InitialStock = 50 // Normal
            });

            var lowStockList = await _inventoryService.GetLowStockProductsAsync(TestBusinessA);
            Assert.Contains(lowStockList, p => p.Id == lowProduct.Id);
            Assert.DoesNotContain(lowStockList, p => p.Id == normalProduct.Id);
        }

        [Fact]
        public async Task Test18_SearchProducts_ByNameSkuBarcode_AndTenantIsolation()
        {
            var p1 = await _inventoryService.AddProductAsync(TestBusinessA, new CreateProductDto
            {
                Name = "Shan Biryani Masala",
                Sku = "SHAN-BIR-01",
                Barcode = "896400055112",
                CostPrice = 90,
                SalePrice = 120
            });

            var p2 = await _inventoryService.AddProductAsync(TestBusinessA, new CreateProductDto
            {
                Name = "National Kheer Mix",
                Sku = "NAT-KHEER-02",
                Barcode = "896400099443",
                CostPrice = 110,
                SalePrice = 140
            });

            // Product in Business B with similar name
            var pBizB = await _inventoryService.AddProductAsync(TestBusinessB, new CreateProductDto
            {
                Name = "Shan Korma Masala",
                Sku = "SHAN-KOR-01",
                Barcode = "896400077889",
                CostPrice = 90,
                SalePrice = 120
            });

            // Search by Name (case-insensitive)
            var nameResults = await _inventoryService.SearchProductsAsync(TestBusinessA, "biryani");
            Assert.Single(nameResults);
            Assert.Equal(p1.Id, nameResults[0].Id);

            // Search by SKU
            var skuResults = await _inventoryService.SearchProductsAsync(TestBusinessA, "NAT-KHEER");
            Assert.Single(skuResults);
            Assert.Equal(p2.Id, skuResults[0].Id);

            // Search by Barcode
            var barcodeResults = await _inventoryService.SearchProductsAsync(TestBusinessA, "896400055112");
            Assert.Single(barcodeResults);
            Assert.Equal(p1.Id, barcodeResults[0].Id);

            // Search for "Shan" in Business A should NOT return Business B's product
            var shanResults = await _inventoryService.SearchProductsAsync(TestBusinessA, "Shan");
            Assert.Single(shanResults);
            Assert.Equal(p1.Id, shanResults[0].Id);
        }

        [Fact]
        public async Task Test19_InventoryValuation_CalculatesAccurately()
        {
            // Product 1: 10 units @ 100 cost = 1000 cost val, 150 retail = 1500 retail val
            await _inventoryService.AddProductAsync(TestBusinessA, new CreateProductDto
            {
                Name = "Item 1",
                CostPrice = 100.00m,
                SalePrice = 150.00m,
                InitialStock = 10.00m,
                MinStockAlert = 5.00m
            });

            // Product 2: 20 units @ 50 cost = 1000 cost val, 80 retail = 1600 retail val
            await _inventoryService.AddProductAsync(TestBusinessA, new CreateProductDto
            {
                Name = "Item 2",
                CostPrice = 50.00m,
                SalePrice = 80.00m,
                InitialStock = 20.00m,
                MinStockAlert = 25.00m // Low stock
            });

            var valuation = await _inventoryService.GetStockValuationAsync(TestBusinessA);

            Assert.Equal(2, valuation.TotalProductCount);
            Assert.Equal(30.00m, valuation.TotalStockQuantity);
            Assert.Equal(2000.00m, valuation.TotalCostValuation);
            Assert.Equal(3100.00m, valuation.TotalRetailValuation);
            Assert.Equal(1100.00m, valuation.PotentialProfit);
            Assert.Equal(1, valuation.LowStockCount);

            // Verify DashboardService metrics also match
            var dashboard = await _dashboardService.GetDashboardMetricsAsync(TestBusinessA);
            Assert.Equal(2, dashboard.TotalProductsCount);
            Assert.Equal(2000.00m, dashboard.TotalStockValuation);
            Assert.Equal(1, dashboard.LowStockCount);
        }

        [Fact]
        public async Task Test20_Validations_NegativePricesAndInvalidTax_Rejected()
        {
            // Negative cost price
            await Assert.ThrowsAsync<ArgumentException>(async () =>
            {
                await _inventoryService.AddProductAsync(TestBusinessA, new CreateProductDto
                {
                    Name = "Bad Product",
                    CostPrice = -10,
                    SalePrice = 20
                });
            });

            // Negative sale price
            await Assert.ThrowsAsync<ArgumentException>(async () =>
            {
                await _inventoryService.AddProductAsync(TestBusinessA, new CreateProductDto
                {
                    Name = "Bad Product",
                    CostPrice = 10,
                    SalePrice = -5
                });
            });

            // Tax rate > 100%
            await Assert.ThrowsAsync<ArgumentException>(async () =>
            {
                await _inventoryService.AddProductAsync(TestBusinessA, new CreateProductDto
                {
                    Name = "Bad Product",
                    CostPrice = 10,
                    SalePrice = 20,
                    TaxRate = 120
                });
            });
        }
    }
}
