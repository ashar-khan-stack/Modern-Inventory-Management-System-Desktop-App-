using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ModernInventory.Core.Application.DTOs;
using ModernInventory.Core.Application.Repositories;
using ModernInventory.Core.Domain.Entities;
using ModernInventory.Core.Infrastructure.Database;

namespace ModernInventory.Core.Application.Services
{
    #region Supplier Service

    public interface ISupplierService
    {
        Task<List<SupplierListItemDto>> GetSuppliersAsync(string businessId, string? searchTerm = null);
        Task<Supplier?> GetSupplierByIdAsync(string id, string businessId);
        Task<Supplier> AddSupplierAsync(string businessId, CreateSupplierDto dto);
        Task<Supplier> UpdateSupplierAsync(string businessId, UpdateSupplierDto dto);
        Task<bool> DeleteSupplierAsync(string id, string businessId);
    }

    public class SupplierService : ISupplierService
    {
        private readonly AppDbContext _context;
        private readonly ISupplierRepository _supplierRepo;

        public SupplierService(AppDbContext context, ISupplierRepository supplierRepo)
        {
            _context = context;
            _supplierRepo = supplierRepo;
        }

        public async Task<List<SupplierListItemDto>> GetSuppliersAsync(string businessId, string? searchTerm = null)
        {
            var suppliers = await _supplierRepo.SearchAsync(businessId, searchTerm ?? string.Empty);
            return suppliers.Select(s => new SupplierListItemDto
            {
                Id = s.Id,
                Name = s.Name,
                CompanyName = s.CompanyName,
                Phone = s.Phone,
                Email = s.Email,
                Address = s.Address,
                OpeningBalance = s.OpeningBalance,
                CurrentPayableBalance = s.CurrentPayableBalance,
                Notes = s.Notes,
                CreatedAt = s.CreatedAt
            }).ToList();
        }

        public async Task<Supplier?> GetSupplierByIdAsync(string id, string businessId)
        {
            return await _supplierRepo.GetByIdAsync(id, businessId);
        }

        public async Task<Supplier> AddSupplierAsync(string businessId, CreateSupplierDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Name))
            {
                throw new ArgumentException("Supplier name is required.", nameof(dto.Name));
            }

            var supplier = new Supplier
            {
                BusinessId = businessId,
                Name = dto.Name.Trim(),
                CompanyName = dto.CompanyName?.Trim() ?? string.Empty,
                Phone = dto.Phone?.Trim() ?? string.Empty,
                Email = dto.Email?.Trim() ?? string.Empty,
                Cnic = dto.Cnic?.Trim() ?? string.Empty,
                Ntn = dto.Ntn?.Trim() ?? string.Empty,
                Address = dto.Address?.Trim() ?? string.Empty,
                OpeningBalance = dto.OpeningBalance,
                CurrentPayableBalance = dto.OpeningBalance,
                Notes = dto.Notes?.Trim() ?? string.Empty
            };

            await _supplierRepo.AddAsync(supplier);
            return supplier;
        }

        public async Task<Supplier> UpdateSupplierAsync(string businessId, UpdateSupplierDto dto)
        {
            var supplier = await _supplierRepo.GetByIdAsync(dto.Id, businessId);
            if (supplier == null) throw new InvalidOperationException("Supplier not found.");

            supplier.Name = dto.Name.Trim();
            supplier.CompanyName = dto.CompanyName?.Trim() ?? string.Empty;
            supplier.Phone = dto.Phone?.Trim() ?? string.Empty;
            supplier.Email = dto.Email?.Trim() ?? string.Empty;
            supplier.Cnic = dto.Cnic?.Trim() ?? string.Empty;
            supplier.Ntn = dto.Ntn?.Trim() ?? string.Empty;
            supplier.Address = dto.Address?.Trim() ?? string.Empty;
            supplier.Notes = dto.Notes?.Trim() ?? string.Empty;

            await _supplierRepo.UpdateAsync(supplier);
            return supplier;
        }

        public async Task<bool> DeleteSupplierAsync(string id, string businessId)
        {
            await _supplierRepo.SoftDeleteAsync(id, businessId);
            return true;
        }
    }

    #endregion

    #region Purchase Service

    public interface IPurchaseService
    {
        Task<List<PurchaseListItemDto>> GetPurchasesHistoryAsync(string businessId, DateTime? startDate = null, DateTime? endDate = null, string? supplierId = null, string? searchTerm = null);
        Task<Purchase?> GetPurchaseByIdAsync(string id, string businessId);
        Task<Purchase> CreatePurchaseAsync(string businessId, CreatePurchaseDto dto);
    }

    public class PurchaseService : IPurchaseService
    {
        private readonly AppDbContext _context;
        private readonly IPurchaseRepository _purchaseRepo;
        private readonly ISupplierRepository _supplierRepo;
        private readonly IProductRepository _productRepo;

        public PurchaseService(
            AppDbContext context,
            IPurchaseRepository purchaseRepo,
            ISupplierRepository supplierRepo,
            IProductRepository productRepo)
        {
            _context = context;
            _purchaseRepo = purchaseRepo;
            _supplierRepo = supplierRepo;
            _productRepo = productRepo;
        }

        public async Task<List<PurchaseListItemDto>> GetPurchasesHistoryAsync(
            string businessId,
            DateTime? startDate = null,
            DateTime? endDate = null,
            string? supplierId = null,
            string? searchTerm = null)
        {
            var purchases = await _purchaseRepo.GetPurchasesHistoryAsync(businessId, startDate, endDate, supplierId, searchTerm);
            return purchases.Select(p => new PurchaseListItemDto
            {
                Id = p.Id,
                BillNumber = p.BillNumber,
                SupplierName = p.SupplierName,
                PurchaseDate = p.PurchaseDate,
                GrandTotal = p.GrandTotal,
                DueBalance = p.DueBalance
            }).ToList();
        }

        public async Task<Purchase?> GetPurchaseByIdAsync(string id, string businessId)
        {
            return await _purchaseRepo.GetPurchaseWithItemsAsync(id, businessId);
        }

        public async Task<Purchase> CreatePurchaseAsync(string businessId, CreatePurchaseDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.SupplierId)) throw new ArgumentException("Supplier ID is required.");
            if (dto.Items == null || !dto.Items.Any()) throw new ArgumentException("Purchase must have at least one item.");

            var supplier = await _supplierRepo.GetByIdAsync(dto.SupplierId, businessId);
            if (supplier == null) throw new InvalidOperationException("Supplier not found.");

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                decimal subtotal = 0;
                var purchaseItems = new List<PurchaseItem>();

                foreach (var itemDto in dto.Items)
                {
                    var product = await _productRepo.GetByIdAsync(itemDto.ProductId, businessId);
                    if (product == null || product.IsDeleted) throw new InvalidOperationException($"Product '{itemDto.ProductName}' not found.");
                    if (itemDto.Quantity <= 0) throw new InvalidOperationException($"Invalid quantity for product '{product.Name}'.");

                    decimal lineTotal = itemDto.Quantity * itemDto.UnitCost;
                    subtotal += lineTotal;

                    purchaseItems.Add(new PurchaseItem
                    {
                        Id = Guid.NewGuid().ToString(),
                        ProductId = product.Id,
                        ProductName = product.Name,
                        Quantity = itemDto.Quantity,
                        UnitCost = itemDto.UnitCost,
                        TotalCost = lineTotal
                    });

                    // Update Stock
                    decimal previousQuantity = product.StockQuantity;
                    product.StockQuantity += itemDto.Quantity;
                    product.UpdatedAt = DateTime.UtcNow;

                    // Create StockMovement
                    var movement = new StockAdjustment
                    {
                        Id = Guid.NewGuid().ToString(),
                        BusinessId = businessId,
                        ProductId = product.Id,
                        Type = ModernInventory.Core.Domain.Enums.StockAdjustmentType.ADDITION,
                        QuantityChanged = itemDto.Quantity,
                        CostPerUnit = itemDto.UnitCost,
                        PreviousQuantity = previousQuantity,
                        NewQuantity = product.StockQuantity,
                        Reason = $"Purchase: {dto.BillNumber}",
                        AdjustmentDate = dto.PurchaseDate,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };
                    await _context.Set<StockAdjustment>().AddAsync(movement);
                }

                decimal grandTotal = subtotal - dto.Discount + dto.Tax;
                decimal dueBalance = grandTotal - dto.PaidAmount;

                var purchase = new Purchase
                {
                    Id = Guid.NewGuid().ToString(),
                    BusinessId = businessId,
                    BillNumber = await _purchaseRepo.GenerateNextPurchaseNumberAsync(businessId),
                    SupplierId = supplier.Id,
                    SupplierName = supplier.Name,
                    SupplierPhone = supplier.Phone,
                    PurchaseDate = dto.PurchaseDate,
                    Subtotal = subtotal,
                    TaxAmount = dto.Tax,
                    GrandTotal = grandTotal,
                    PaidAmount = dto.PaidAmount,
                    DueBalance = dueBalance,
                    Notes = dto.Notes,
                    Items = purchaseItems
                };

                await _purchaseRepo.AddAsync(purchase);

                // Update Supplier Payable
                if (dueBalance > 0)
                {
                    supplier.CurrentPayableBalance += dueBalance;
                    supplier.UpdatedAt = DateTime.UtcNow;
                    await _supplierRepo.UpdateAsync(supplier);
                }

                // SyncQueue
                await _context.Set<SyncQueueItem>().AddAsync(new SyncQueueItem
                {
                    BusinessId = businessId,
                    EntityType = nameof(Purchase),
                    EntityId = purchase.Id,
                    OperationType = ModernInventory.Core.Domain.Enums.SyncOperationType.CREATE,
                    PayloadJson = System.Text.Json.JsonSerializer.Serialize(purchase),
                    CreatedAt = DateTime.UtcNow
                });

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return purchase;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
    }

    #endregion
}
