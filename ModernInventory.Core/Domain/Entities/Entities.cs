using System;
using System.Collections.Generic;

namespace ModernInventory.Core.Domain.Enums
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
        CRV, // Cash Receipt Voucher
        CPV, // Cash Payment Voucher
        BRV, // Bank Receipt Voucher
        BPV, // Bank Payment Voucher
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

namespace ModernInventory.Core.Domain.Entities
{
    using ModernInventory.Core.Domain.Enums;

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

    public class UserSession
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string UserId { get; set; } = string.Empty;
        public string BusinessId { get; set; } = string.Empty;
        public string Token { get; set; } = Guid.NewGuid().ToString("N");
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime ExpiresAt { get; set; } = DateTime.UtcNow.AddDays(30);
        public bool RememberMe { get; set; } = true;
        public bool IsRevoked { get; set; } = false;
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
        public decimal TaxRate { get; set; } = 18.00m; // Pakistani standard GST default
    }

    public class StockAdjustment : BaseEntity
    {
        public string ProductId { get; set; } = string.Empty;
        public StockAdjustmentType Type { get; set; } = StockAdjustmentType.ADDITION;
        public decimal QuantityChanged { get; set; } = 0.00m;
        public decimal CostPerUnit { get; set; } = 0.00m;
        public decimal PreviousQuantity { get; set; } = 0.00m;
        public decimal NewQuantity { get; set; } = 0.00m;
        public string Reason { get; set; } = string.Empty;
        public DateTime AdjustmentDate { get; set; } = DateTime.UtcNow;

        // Convenient aliases for Android / Domain alignment
        public decimal QuantityDelta { get => QuantityChanged; set => QuantityChanged = value; }
        public StockAdjustmentType AdjustmentType { get => Type; set => Type = value; }
        public DateTime Date { get => AdjustmentDate; set => AdjustmentDate = value; }
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
        public string Notes { get; set; } = string.Empty;
    }

    public class Employee : BaseEntity
    {
        public string Name { get; set; } = string.Empty;
        public string Designation { get; set; } = string.Empty;
        public string Cnic { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public decimal MonthlySalary { get; set; } = 0.00m;
        public DateTime JoiningDate { get; set; } = DateTime.UtcNow;
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

    public class Supplier : BaseEntity
    {
        public string Name { get; set; } = string.Empty;
        public string CompanyName { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Cnic { get; set; } = string.Empty;
        public string Ntn { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public decimal OpeningBalance { get; set; } = 0.00m;
        public decimal CurrentPayableBalance { get; set; } = 0.00m;
        public string Notes { get; set; } = string.Empty;
    }

    public class Purchase : BaseEntity
    {
        public string BillNumber { get; set; } = string.Empty;
        public string SupplierId { get; set; } = string.Empty;
        public string SupplierName { get; set; } = string.Empty;
        public string SupplierPhone { get; set; } = string.Empty;
        public DateTime PurchaseDate { get; set; } = DateTime.UtcNow;
        public decimal Subtotal { get; set; } = 0.00m;
        public decimal TaxAmount { get; set; } = 0.00m;
        public decimal GrandTotal { get; set; } = 0.00m;
        public decimal PaidAmount { get; set; } = 0.00m;
        public decimal DueBalance { get; set; } = 0.00m;
        public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.CASH;
        public string Notes { get; set; } = string.Empty;
        public List<PurchaseItem> Items { get; set; } = new();
    }

    public class PurchaseItem
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string PurchaseId { get; set; } = string.Empty;
        public string ProductId { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public decimal Quantity { get; set; } = 0.00m;
        public decimal UnitCost { get; set; } = 0.00m;
        public decimal TotalCost { get; set; } = 0.00m;
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
        public List<VoucherEntry> Entries { get; set; } = new();
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

    public class Expense : BaseEntity
    {
        public string Category { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public decimal Amount { get; set; } = 0.00m;
        public DateTime ExpenseDate { get; set; } = DateTime.UtcNow;
        public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.CASH;
        public string? AccountHeadId { get; set; }
        public string Notes { get; set; } = string.Empty;
    }

    public class BankAccount : BaseEntity
    {
        public string BankName { get; set; } = string.Empty;
        public string AccountTitle { get; set; } = string.Empty;
        public string AccountNumber { get; set; } = string.Empty;
        public string BranchCode { get; set; } = string.Empty;
        public string Iban { get; set; } = string.Empty;
        public decimal OpeningBalance { get; set; } = 0.00m;
        public decimal CurrentBalance { get; set; } = 0.00m;
        public bool IsActive { get; set; } = true;
    }

    public class BankTransaction : BaseEntity
    {
        public string BankAccountId { get; set; } = string.Empty;
        public string TransactionType { get; set; } = "DEPOSIT"; // DEPOSIT, WITHDRAWAL, TRANSFER
        public decimal Amount { get; set; } = 0.00m;
        public DateTime TransactionDate { get; set; } = DateTime.UtcNow;
        public string ChequeNumber { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
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

    public class LocalBackupRecord
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string BusinessId { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public long FileSizeBytes { get; set; } = 0;
        public string Checksum { get; set; } = string.Empty;
        public string EntityCountsJson { get; set; } = "{}";
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
