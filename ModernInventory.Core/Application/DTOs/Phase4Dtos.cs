using System;
using System.Collections.Generic;
using ModernInventory.Core.Domain.Enums;

namespace ModernInventory.Core.Application.DTOs
{
    #region Supplier DTOs

    public class CreateSupplierDto
    {
        public string Name { get; set; } = string.Empty;
        public string CompanyName { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Cnic { get; set; } = string.Empty;
        public string Ntn { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public decimal OpeningBalance { get; set; } = 0.00m;
        public string Notes { get; set; } = string.Empty;
    }

    public class UpdateSupplierDto
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string CompanyName { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Cnic { get; set; } = string.Empty;
        public string Ntn { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string Notes { get; set; } = string.Empty;
    }

    public class SupplierListItemDto
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string CompanyName { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public decimal OpeningBalance { get; set; } = 0.00m;
        public decimal CurrentPayableBalance { get; set; } = 0.00m;
        public string Notes { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    #endregion

    #region Purchase DTOs

    public class CreatePurchaseDto
    {
        public string SupplierId { get; set; } = string.Empty;
        public string BillNumber { get; set; } = string.Empty;
        public DateTime PurchaseDate { get; set; } = DateTime.UtcNow;
        public decimal Discount { get; set; } = 0.00m;
        public decimal Tax { get; set; } = 0.00m;
        public decimal PaidAmount { get; set; } = 0.00m;
        public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.CASH;
        public string Notes { get; set; } = string.Empty;
        public List<CreatePurchaseItemDto> Items { get; set; } = new();
    }

    public class CreatePurchaseItemDto
    {
        public string ProductId { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public decimal Quantity { get; set; } = 0.00m;
        public decimal UnitCost { get; set; } = 0.00m;
    }

    public class PurchaseListItemDto
    {
        public string Id { get; set; } = string.Empty;
        public string BillNumber { get; set; } = string.Empty;
        public string SupplierName { get; set; } = string.Empty;
        public DateTime PurchaseDate { get; set; }
        public decimal GrandTotal { get; set; } = 0.00m;
        public decimal DueBalance { get; set; } = 0.00m;
    }

    #endregion
}
