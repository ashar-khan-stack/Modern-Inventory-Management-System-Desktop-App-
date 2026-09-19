using System;
using System.Collections.Generic;
using ModernInventory.Core.Domain.Enums;

namespace ModernInventory.Core.Application.DTOs
{
    #region Customer DTOs

    public class CreateCustomerDto
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
        public string Notes { get; set; } = string.Empty;
    }

    public class UpdateCustomerDto
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string CompanyName { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Cnic { get; set; } = string.Empty;
        public string Ntn { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public decimal CreditLimit { get; set; } = 0.00m;
        public string Notes { get; set; } = string.Empty;
    }

    public class CustomerListItemDto
    {
        public string Id { get; set; } = string.Empty;
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
        public int InvoicesCount { get; set; } = 0;
        public decimal TotalPurchasedAmount { get; set; } = 0.00m;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class CustomerTransactionHistoryItemDto
    {
        public string Id { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public string TransactionType { get; set; } = string.Empty; // "Sale Invoice", "Payment"
        public string ReferenceNumber { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Amount { get; set; } = 0.00m;
        public decimal Paid { get; set; } = 0.00m;
        public decimal DueBalance { get; set; } = 0.00m;
        public string PaymentMethod { get; set; } = string.Empty;
    }

    #endregion

    #region Employee DTOs

    public class CreateEmployeeDto
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

    public class UpdateEmployeeDto
    {
        public string Id { get; set; } = string.Empty;
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

    public class EmployeeListItemDto
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Designation { get; set; } = string.Empty;
        public string Cnic { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public decimal MonthlySalary { get; set; } = 0.00m;
        public DateTime JoiningDate { get; set; }
        public decimal AdvanceBalance { get; set; } = 0.00m;
        public bool IsActive { get; set; } = true;
        public string StatusDisplay => IsActive ? "Active" : "Inactive";
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    #endregion

    #region Sales & POS DTOs

    public class CreateSaleItemDto
    {
        public string ProductId { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public string Unit { get; set; } = "Pcs";
        public decimal Quantity { get; set; } = 1.00m;
        public decimal UnitPrice { get; set; } = 0.00m;
        public decimal Discount { get; set; } = 0.00m;
        public decimal TaxRate { get; set; } = 18.00m;
    }

    public class CreateSaleDto
    {
        public string? CustomerId { get; set; }
        public string CustomerName { get; set; } = "Walk-in Customer";
        public DateTime SaleDate { get; set; } = DateTime.UtcNow;
        public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.CASH;
        public decimal OrderDiscount { get; set; } = 0.00m;
        public decimal PaidAmount { get; set; } = 0.00m;
        public string Notes { get; set; } = string.Empty;
        public List<CreateSaleItemDto> Items { get; set; } = new();
    }

    public class CartItemDto
    {
        public string ProductId { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public string Sku { get; set; } = string.Empty;
        public string Barcode { get; set; } = string.Empty;
        public string Unit { get; set; } = "Pcs";
        public decimal AvailableStock { get; set; } = 0.00m;
        public decimal UnitCost { get; set; } = 0.00m;
        public decimal UnitPrice { get; set; } = 0.00m;
        public decimal Quantity { get; set; } = 1.00m;
        public decimal Discount { get; set; } = 0.00m;
        public decimal TaxRate { get; set; } = 18.00m;

        public decimal LineSubtotal => Quantity * UnitPrice;
        public decimal TaxableAmount => Math.Max(0, LineSubtotal - Discount);
        public decimal TaxAmount => Math.Round(TaxableAmount * (TaxRate / 100m), 2, MidpointRounding.AwayFromZero);
        public decimal TotalPrice => TaxableAmount + TaxAmount;
    }

    public class SaleListItemDto
    {
        public string Id { get; set; } = string.Empty;
        public string InvoiceNumber { get; set; } = string.Empty;
        public string CustomerId { get; set; } = string.Empty;
        public string CustomerName { get; set; } = "Walk-in Customer";
        public DateTime SaleDate { get; set; }
        public decimal Subtotal { get; set; } = 0.00m;
        public decimal DiscountAmount { get; set; } = 0.00m;
        public decimal TaxAmount { get; set; } = 0.00m;
        public decimal GrandTotal { get; set; } = 0.00m;
        public decimal PaidAmount { get; set; } = 0.00m;
        public decimal DueBalance { get; set; } = 0.00m;
        public PaymentMethod PaymentMethod { get; set; }
        public string Notes { get; set; } = string.Empty;
        public int ItemCount { get; set; } = 0;
        public string Status => DueBalance <= 0 ? "Paid" : (PaidAmount > 0 ? "Partial" : "Unpaid");
    }

    public class SaleDetailDto
    {
        public string Id { get; set; } = string.Empty;
        public string InvoiceNumber { get; set; } = string.Empty;
        public string CustomerId { get; set; } = string.Empty;
        public string CustomerName { get; set; } = "Walk-in Customer";
        public string CustomerPhone { get; set; } = string.Empty;
        public string CustomerAddress { get; set; } = string.Empty;
        public DateTime SaleDate { get; set; }
        public decimal Subtotal { get; set; } = 0.00m;
        public decimal DiscountAmount { get; set; } = 0.00m;
        public decimal TaxAmount { get; set; } = 0.00m;
        public decimal GrandTotal { get; set; } = 0.00m;
        public decimal PaidAmount { get; set; } = 0.00m;
        public decimal DueBalance { get; set; } = 0.00m;
        public PaymentMethod PaymentMethod { get; set; }
        public string Notes { get; set; } = string.Empty;
        public List<SaleItemDetailDto> Items { get; set; } = new();
    }

    public class SaleItemDetailDto
    {
        public string Id { get; set; } = string.Empty;
        public string ProductId { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public string Unit { get; set; } = "Pcs";
        public decimal Quantity { get; set; } = 0.00m;
        public decimal UnitPrice { get; set; } = 0.00m;
        public decimal UnitCost { get; set; } = 0.00m;
        public decimal Discount { get; set; } = 0.00m;
        public decimal TaxRate { get; set; } = 0.00m;
        public decimal TaxAmount { get; set; } = 0.00m;
        public decimal TotalPrice { get; set; } = 0.00m;
    }

    public class SalesSummaryDto
    {
        public int TotalInvoicesCount { get; set; } = 0;
        public int TodayInvoicesCount { get; set; } = 0;
        public decimal TotalRevenue { get; set; } = 0.00m;
        public decimal TodayRevenue { get; set; } = 0.00m;
        public decimal TotalPaid { get; set; } = 0.00m;
        public decimal TotalDueReceivables { get; set; } = 0.00m;
        public decimal CashSalesAmount { get; set; } = 0.00m;
        public decimal CreditSalesAmount { get; set; } = 0.00m;
        public decimal BankSalesAmount { get; set; } = 0.00m;
    }

    #endregion
}
