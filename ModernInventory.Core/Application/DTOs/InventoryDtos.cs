using System;
using System.Collections.Generic;
using ModernInventory.Core.Domain.Enums;

namespace ModernInventory.Core.Application.DTOs
{
    public class CreateCategoryDto
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }

    public class UpdateCategoryDto
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }

    public class CategoryListItemDto
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int ProductCount { get; set; } = 0;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class CreateProductDto
    {
        public string Name { get; set; } = string.Empty;
        public string Sku { get; set; } = string.Empty;
        public string Barcode { get; set; } = string.Empty;
        public string CategoryId { get; set; } = string.Empty;
        public string Unit { get; set; } = "Pcs";
        public decimal CostPrice { get; set; } = 0.00m;
        public decimal SalePrice { get; set; } = 0.00m;
        public decimal TaxRate { get; set; } = 18.00m; // Default Pakistani GST
        public decimal MinStockAlert { get; set; } = 0.00m;
        public decimal InitialStock { get; set; } = 0.00m;
    }

    public class UpdateProductDto
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Sku { get; set; } = string.Empty;
        public string Barcode { get; set; } = string.Empty;
        public string CategoryId { get; set; } = string.Empty;
        public string Unit { get; set; } = "Pcs";
        public decimal CostPrice { get; set; } = 0.00m;
        public decimal SalePrice { get; set; } = 0.00m;
        public decimal TaxRate { get; set; } = 18.00m;
        public decimal MinStockAlert { get; set; } = 0.00m;
    }

    public class ProductListItemDto
    {
        public string Id { get; set; } = string.Empty;
        public string CategoryId { get; set; } = string.Empty;
        public string CategoryName { get; set; } = "Uncategorized";
        public string Sku { get; set; } = string.Empty;
        public string Barcode { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Unit { get; set; } = "Pcs";
        public decimal CostPrice { get; set; } = 0.00m;
        public decimal SalePrice { get; set; } = 0.00m;
        public decimal StockQuantity { get; set; } = 0.00m;
        public decimal MinStockAlert { get; set; } = 0.00m;
        public decimal TaxRate { get; set; } = 18.00m;
        public bool IsLowStock => StockQuantity <= MinStockAlert;
        public decimal StockValue => StockQuantity * CostPrice;
        public decimal RetailValue => StockQuantity * SalePrice;
        public string Status => IsLowStock ? (StockQuantity <= 0 ? "Out of Stock" : "Low Stock") : "In Stock";
        public DateTime UpdatedAt { get; set; }
    }

    public class StockAdjustmentRequestDto
    {
        public string ProductId { get; set; } = string.Empty;
        public StockAdjustmentType Type { get; set; } = StockAdjustmentType.ADDITION;
        public StockAdjustmentType AdjustmentType { get => Type; set => Type = value; }
        public decimal Quantity { get; set; } = 0.00m;
        public decimal QuantityDelta { get => Quantity; set => Quantity = value; }
        public decimal CostPerUnit { get; set; } = 0.00m;
        public string Reason { get; set; } = string.Empty;
        public DateTime? Date { get; set; }
        public DateTime? AdjustmentDate { get => Date; set => Date = value; }
    }

    public class StockMovementHistoryDto
    {
        public string Id { get; set; } = string.Empty;
        public string ProductId { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public string Sku { get; set; } = string.Empty;
        public StockAdjustmentType Type { get; set; }
        public StockAdjustmentType AdjustmentType => Type;
        public string TypeDisplay => Type switch
        {
            StockAdjustmentType.ADDITION => "Stock Addition (+)",
            StockAdjustmentType.DAMAGE => "Damaged Stock (-)",
            StockAdjustmentType.THEFT => "Theft / Loss (-)",
            StockAdjustmentType.EXPIRED => "Expired Stock (-)",
            StockAdjustmentType.AUDIT_CORRECTION => "Audit Correction",
            _ => Type.ToString()
        };
        public decimal QuantityChanged { get; set; }
        public decimal QuantityDelta => QuantityChanged;
        public decimal CostPerUnit { get; set; }
        public decimal PreviousQuantity { get; set; }
        public decimal NewQuantity { get; set; }
        public string Reason { get; set; } = string.Empty;
        public DateTime AdjustmentDate { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class InventoryValuationSummaryDto
    {
        public int TotalProductCount { get; set; } = 0;
        public decimal TotalStockQuantity { get; set; } = 0.00m;
        public decimal TotalCostValuation { get; set; } = 0.00m;
        public decimal TotalRetailValuation { get; set; } = 0.00m;
        public decimal PotentialProfit => TotalRetailValuation - TotalCostValuation;
        public int LowStockCount { get; set; } = 0;
        public int OutOfStockCount { get; set; } = 0;
    }
}
