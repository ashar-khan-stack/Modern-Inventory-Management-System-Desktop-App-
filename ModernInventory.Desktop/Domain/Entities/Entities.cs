using System;
using System.Collections.Generic;

namespace ModernInventory.Desktop.Domain.Enums
{
    public enum SyncStatus
    {
        Synced,
        PendingCreate,
        PendingUpdate,
        PendingDelete,
        SyncFailed
    }

    public enum SyncOperationType
    {
        CREATE,
        UPDATE,
        DELETE
    }

    public enum AccountType
    {
        ASSET,
        LIABILITY,
        EQUITY,
        REVENUE,
        EXPENSE
    }

    public enum VoucherType
    {
        CRV, // Cash Receipt
        CPV, // Cash Payment
        BRV, // Bank Receipt
        BPV, // Bank Payment
        JV   // Journal Voucher
    }

    public enum PaymentMethod
    {
        CASH,
        BANK,
        CREDIT,
        SPLIT
    }

    public enum StockAdjustmentType
    {
        ADDITION,
        DAMAGE,
        THEFT,
        EXPIRED,
        AUDIT_CORRECTION
    }

    public enum PartyType
    {
        CUSTOMER,
        EMPLOYEE,
        SUPPLIER,
        NONE
    }
}

namespace ModernInventory.Desktop.Domain.Entities
{
    using ModernInventory.Desktop.Domain.Enums;

    public abstract class BaseEntity
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string BusinessId { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public bool IsDeleted { get; set; } = false;
        public SyncStatus SyncStatus { get; set; } = SyncStatus.PendingCreate;
    }

    public class BusinessProfile
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string BusinessName { get; set; } = string.Empty;
        public string OwnerName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string NtnNumber { get; set; } = string.Empty;
        public string StrnNumber { get; set; } = string.Empty;
        public string Currency { get; set; } = "PKR";
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }

    public class User
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string BusinessId { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public string Salt { get; set; } = string.Empty;
        public string SecurityQuestionsJson { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }

    public class Category : BaseEntity
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }

    public class Product : BaseEntity
    {
        public string CategoryId { get; set; } = string.Empty;
        public string Sku { get; set; } = string.Empty;
        public string Barcode { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Unit { get; set; } = "Pcs";
        public decimal CostPrice { get; set; } = 0.00m;
        public decimal SalePrice { get; set; } = 0.00m;
        public decimal StockQuantity { get; set; } = 0.00m;
        public decimal MinStockAlert { get; set; } = 0.00m;
        public decimal TaxRate { get; set; } = 18.00m;
    }

    public class Customer : BaseEntity
    {
        public string Name { get; set; } = string.Empty;
        public string CompanyName { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Cnic { get; set; } = string.Empty;
        public string Ntn { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public decimal CreditLimit { get; set; } = 0.00m;
        public decimal OpeningBalance { get; set; } = 0.00m;
        public decimal CurrentBalance { get; set; } = 0.00m;
    }

    public class Employee : BaseEntity
    {
        public string Name { get; set; } = string.Empty;
        public string Designation { get; set; } = string.Empty;
        public string Cnic { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public decimal MonthlySalary { get; set; } = 0.00m;
        public DateTime JoiningDate { get; set; }
        public decimal AdvanceBalance { get; set; } = 0.00m;
        public bool IsActive { get; set; } = true;
    }

    public class Sale : BaseEntity
    {
        public string InvoiceNumber { get; set; } = string.Empty;
        public string CustomerId { get; set; } = string.Empty;
        public DateTime SaleDate { get; set; } = DateTime.UtcNow;
        public decimal Subtotal { get; set; } = 0.00m;
        public decimal DiscountAmount { get; set; } = 0.00m;
        public decimal TaxAmount { get; set; } = 0.00m;
        public decimal GrandTotal { get; set; } = 0.00m;
        public decimal PaidAmount { get; set; } = 0.00m;
        public decimal DueBalance { get; set; } = 0.00m;
        public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.CASH;
        public string Notes { get; set; } = string.Empty;
        public List<SaleItem> Items { get; set; } = new();
    }

    public class SaleItem
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string SaleId { get; set; } = string.Empty;
        public string ProductId { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public string Unit { get; set; } = "Pcs";
        public decimal Quantity { get; set; } = 0.00m;
        public decimal UnitPrice { get; set; } = 0.00m;
        public decimal UnitCost { get; set; } = 0.00m;
        public decimal Discount { get; set; } = 0.00m;
        public decimal TaxRate { get; set; } = 18.00m;
        public decimal TaxAmount { get; set; } = 0.00m;
        public decimal TotalPrice { get; set; } = 0.00m;
    }

    public class AccountHead : BaseEntity
    {
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public AccountType Type { get; set; } = AccountType.ASSET;
        public bool IsSystem { get; set; } = false;
        public string Description { get; set; } = string.Empty;
    }

    public class Voucher : BaseEntity
    {
        public string VoucherNumber { get; set; } = string.Empty;
        public VoucherType VoucherType { get; set; } = VoucherType.CRV;
        public DateTime Date { get; set; } = DateTime.UtcNow;
        public decimal TotalAmount { get; set; } = 0.00m;
        public string ReferenceType { get; set; } = string.Empty;
        public string? ReferenceId { get; set; }
        public string Narration { get; set; } = string.Empty;
    }

    public class VoucherEntry
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string VoucherId { get; set; } = string.Empty;
        public string AccountHeadId { get; set; } = string.Empty;
        public decimal DebitAmount { get; set; } = 0.00m;
        public decimal CreditAmount { get; set; } = 0.00m;
        public PartyType PartyType { get; set; } = PartyType.NONE;
        public string? PartyId { get; set; }
        public string Notes { get; set; } = string.Empty;
    }

    public class SyncQueueItem
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string BusinessId { get; set; } = string.Empty;
        public string EntityType { get; set; } = string.Empty;
        public string EntityId { get; set; } = string.Empty;
        public SyncOperationType OperationType { get; set; } = SyncOperationType.CREATE;
        public string PayloadJson { get; set; } = string.Empty;
        public int RetryCount { get; set; } = 0;
        public DateTime? LastAttemptAt { get; set; }
        public string? LastError { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
