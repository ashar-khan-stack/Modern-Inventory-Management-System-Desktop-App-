using System;
using System.Collections.Generic;
using ModernInventory.Core.Domain.Enums;

namespace ModernInventory.Core.Application.DTOs
{
    public class AccountHeadDto
    {
        public string Id { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public AccountType Type { get; set; } = AccountType.ASSET;
        public bool IsSystem { get; set; } = false;
        public string Description { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    public class CreateAccountHeadDto
    {
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public AccountType Type { get; set; } = AccountType.ASSET;
        public string Description { get; set; } = string.Empty;
    }

    public class VoucherEntryDto
    {
        public string Id { get; set; } = string.Empty;
        public string AccountHeadId { get; set; } = string.Empty;
        public string AccountHeadName { get; set; } = string.Empty;
        public decimal DebitAmount { get; set; } = 0.00m;
        public decimal CreditAmount { get; set; } = 0.00m;
        public PartyType PartyType { get; set; } = PartyType.NONE;
        public string? PartyId { get; set; }
        public string Notes { get; set; } = string.Empty;
    }

    public class CreateVoucherEntryDto
    {
        public string AccountHeadId { get; set; } = string.Empty;
        public decimal DebitAmount { get; set; } = 0.00m;
        public decimal CreditAmount { get; set; } = 0.00m;
        public PartyType PartyType { get; set; } = PartyType.NONE;
        public string? PartyId { get; set; }
        public string Notes { get; set; } = string.Empty;
    }

    public class VoucherDto
    {
        public string Id { get; set; } = string.Empty;
        public string VoucherNumber { get; set; } = string.Empty;
        public VoucherType VoucherType { get; set; } = VoucherType.CRV;
        public DateTime Date { get; set; } = DateTime.UtcNow;
        public decimal TotalAmount { get; set; } = 0.00m;
        public string ReferenceType { get; set; } = string.Empty;
        public string? ReferenceId { get; set; }
        public string Narration { get; set; } = string.Empty;
        public List<VoucherEntryDto> Entries { get; set; } = new();
        public DateTime CreatedAt { get; set; }
    }

    public class CreateVoucherDto
    {
        public VoucherType VoucherType { get; set; } = VoucherType.CRV;
        public DateTime Date { get; set; } = DateTime.UtcNow;
        public string ReferenceType { get; set; } = string.Empty;
        public string? ReferenceId { get; set; }
        public string Narration { get; set; } = string.Empty;
        public List<CreateVoucherEntryDto> Entries { get; set; } = new();
    }

    public class ExpenseDto
    {
        public string Id { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public decimal Amount { get; set; } = 0.00m;
        public DateTime ExpenseDate { get; set; } = DateTime.UtcNow;
        public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.CASH;
        public string? AccountHeadId { get; set; }
        public string Notes { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    public class CreateExpenseDto
    {
        public string Category { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public decimal Amount { get; set; } = 0.00m;
        public DateTime ExpenseDate { get; set; } = DateTime.UtcNow;
        public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.CASH;
        public string? AccountHeadId { get; set; }
        public string Notes { get; set; } = string.Empty;
    }

    public class BankAccountDto
    {
        public string Id { get; set; } = string.Empty;
        public string BankName { get; set; } = string.Empty;
        public string AccountTitle { get; set; } = string.Empty;
        public string AccountNumber { get; set; } = string.Empty;
        public string BranchCode { get; set; } = string.Empty;
        public string Iban { get; set; } = string.Empty;
        public decimal OpeningBalance { get; set; } = 0.00m;
        public decimal CurrentBalance { get; set; } = 0.00m;
        public bool IsActive { get; set; } = true;
    }

    public class CreateBankAccountDto
    {
        public string BankName { get; set; } = string.Empty;
        public string AccountTitle { get; set; } = string.Empty;
        public string AccountNumber { get; set; } = string.Empty;
        public string BranchCode { get; set; } = string.Empty;
        public string Iban { get; set; } = string.Empty;
        public decimal OpeningBalance { get; set; } = 0.00m;
    }

    public class LedgerItemDto
    {
        public string Id { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public string VoucherNumber { get; set; } = string.Empty;
        public string AccountName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Debit { get; set; } = 0.00m;
        public decimal Credit { get; set; } = 0.00m;
        public decimal RunningBalance { get; set; } = 0.00m;
    }

    public class ClientPositionDto
    {
        public string CustomerId { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string CompanyName { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public decimal OpeningBalance { get; set; } = 0.00m;
        public decimal TotalSales { get; set; } = 0.00m;
        public decimal TotalPaid { get; set; } = 0.00m;
        public decimal OutstandingBalance { get; set; } = 0.00m;
    }

    public class MonthlyPositionDto
    {
        public string MonthName { get; set; } = string.Empty;
        public decimal SalesRevenue { get; set; } = 0.00m;
        public decimal PurchasesCost { get; set; } = 0.00m;
        public decimal ExpensesAmount { get; set; } = 0.00m;
        public decimal NetCashFlow { get; set; } = 0.00m;
    }

    public class MonthlyPnLDto
    {
        public string MonthName { get; set; } = string.Empty;
        public decimal GrossRevenue { get; set; } = 0.00m;
        public decimal CostOfGoodsSold { get; set; } = 0.00m;
        public decimal GrossProfit { get; set; } = 0.00m;
        public decimal OperatingExpenses { get; set; } = 0.00m;
        public decimal NetProfitLoss { get; set; } = 0.00m;
        public decimal ProfitMarginPercent { get; set; } = 0.00m;
    }
}
