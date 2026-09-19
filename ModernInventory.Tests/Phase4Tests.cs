using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using ModernInventory.Core.Application.DTOs;
using ModernInventory.Core.Application.Repositories;
using ModernInventory.Core.Application.Services;
using ModernInventory.Core.Domain.Entities;
using ModernInventory.Core.Infrastructure.Database;
using Xunit;

namespace ModernInventory.Tests
{
    public class Phase4Tests : IDisposable
    {
        private readonly SqliteConnection _connection;
        private readonly AppDbContext _context;

        public Phase4Tests()
        {
            _connection = new SqliteConnection("DataSource=:memory:");
            _connection.Open();
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite(_connection)
                .Options;
            _context = new AppDbContext(options);
            _context.Database.EnsureCreated();
        }

        public void Dispose()
        {
            _context.Dispose();
            _connection.Dispose();
        }

        [Fact]
        public async Task Test_Purchase_Success()
        {
            var supplierRepo = new SupplierRepository(_context);
            var productRepo = new ProductRepository(_context);
            var purchaseRepo = new PurchaseRepository(_context);
            var service = new PurchaseService(_context, purchaseRepo, supplierRepo, productRepo);

            var businessId = "biz1";
            var supplier = new Supplier { BusinessId = businessId, Name = "Sup 1" };
            await supplierRepo.AddAsync(supplier);
            var product = new Product { BusinessId = businessId, Name = "Prod 1", StockQuantity = 10 };
            await productRepo.AddAsync(product);
            await _context.SaveChangesAsync();

            var dto = new CreatePurchaseDto
            {
                SupplierId = supplier.Id,
                PurchaseDate = DateTime.UtcNow,
                PaidAmount = 100,
                Items = new List<CreatePurchaseItemDto>
                {
                    new CreatePurchaseItemDto { ProductId = product.Id, ProductName = product.Name, Quantity = 5, UnitCost = 100 }
                }
            };

            var purchase = await service.CreatePurchaseAsync(businessId, dto);

            Assert.NotNull(purchase);
            Assert.Equal(500, purchase.Subtotal);
            Assert.Equal(400, purchase.DueBalance);
            
            var updatedProduct = await productRepo.GetByIdAsync(product.Id, businessId);
            Assert.NotNull(updatedProduct);
            Assert.Equal(15, updatedProduct.StockQuantity);

            var updatedSupplier = await supplierRepo.GetByIdAsync(supplier.Id, businessId);
            Assert.NotNull(updatedSupplier);
            Assert.Equal(400, updatedSupplier.CurrentPayableBalance);
        }

        [Fact]
        public async Task Test_PurchaseFailure_RollsBackEverything()
        {
            var supplierRepo = new SupplierRepository(_context);
            var productRepo = new ProductRepository(_context);
            var purchaseRepo = new PurchaseRepository(_context);
            
            var service = new PurchaseService(_context, purchaseRepo, supplierRepo, productRepo);

            var businessId = "biz2";
            var supplier = new Supplier { BusinessId = businessId, Name = "Sup 2" };
            await supplierRepo.AddAsync(supplier);
            var product = new Product { BusinessId = businessId, Name = "Prod 2", StockQuantity = 10 };
            await productRepo.AddAsync(product);
            await _context.SaveChangesAsync();

            var dto = new CreatePurchaseDto
            {
                SupplierId = supplier.Id,
                Items = new List<CreatePurchaseItemDto>
                {
                    new CreatePurchaseItemDto { ProductId = product.Id, ProductName = product.Name, Quantity = 5, UnitCost = 100 },
                    new CreatePurchaseItemDto { ProductId = "invalid_id", ProductName = "Invalid", Quantity = 1, UnitCost = 10 }
                }
            };

            await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreatePurchaseAsync(businessId, dto));

            // Verify rollback
            _context.ChangeTracker.Clear();
            var updatedProduct = await productRepo.GetByIdAsync(product.Id, businessId);
            Assert.NotNull(updatedProduct);
            Assert.Equal(10, updatedProduct.StockQuantity); // Stock unchanged

            var purchases = await purchaseRepo.GetAllAsync(businessId);
            Assert.Empty(purchases); // No purchase created

            var updatedSupplier = await supplierRepo.GetByIdAsync(supplier.Id, businessId);
            Assert.NotNull(updatedSupplier);
            Assert.Equal(0, updatedSupplier.CurrentPayableBalance); // Supplier balance unchanged
        }
    }
}
