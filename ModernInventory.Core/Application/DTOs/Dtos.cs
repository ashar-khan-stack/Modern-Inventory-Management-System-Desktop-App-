using System;
using System.Collections.Generic;

namespace ModernInventory.Core.Application.DTOs
{
    public class RegisterRequestDto
    {
        public string BusinessName { get; set; } = string.Empty;
        public string OwnerName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string SecurityQuestion { get; set; } = string.Empty;
        public string SecurityAnswer { get; set; } = string.Empty;
        public string Currency { get; set; } = "PKR";
    }

    public class AuthResultDto
    {
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public string? Token { get; set; }
        public string? UserId { get; set; }
        public string? BusinessId { get; set; }
        public string? FullName { get; set; }
        public string? BusinessName { get; set; }
        public string? Email { get; set; }
    }

    public class DashboardSummaryDto
    {
        public decimal TodaySales { get; set; } = 0.00m;
        public decimal MonthlySales { get; set; } = 0.00m;
        public decimal TotalCostOfGoodsSold { get; set; } = 0.00m;
        public decimal TotalExpenses { get; set; } = 0.00m;
        public decimal NetProfit { get; set; } = 0.00m;
        public decimal TotalReceivables { get; set; } = 0.00m;
        public decimal TotalPayables { get; set; } = 0.00m;
        public decimal CashBalance { get; set; } = 0.00m;
        public decimal BankBalance { get; set; } = 0.00m;
        public int LowStockCount { get; set; } = 0;
        public int TotalProductsCount { get; set; } = 0;
        public decimal TotalStockValuation { get; set; } = 0.00m;
        public int TotalCustomersCount { get; set; } = 0;
        public int TotalSalesCount { get; set; } = 0;
        public List<RecentActivityDto> RecentActivities { get; set; } = new();
    }

    public class RecentActivityDto
    {
        public string Id { get; set; } = string.Empty;
        public string ActivityType { get; set; } = string.Empty; // "Sale", "Purchase", "Expense", "Voucher"
        public string Title { get; set; } = string.Empty;
        public decimal Amount { get; set; } = 0.00m;
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public string Status { get; set; } = string.Empty;
    }
}
