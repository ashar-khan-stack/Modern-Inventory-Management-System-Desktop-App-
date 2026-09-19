using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using ModernInventory.Core.Application.DTOs;
using ModernInventory.Core.Domain.Entities;
using ModernInventory.Core.Domain.Enums;
using ModernInventory.Core.Infrastructure.Database;

namespace ModernInventory.Core.Application.Services
{
    public interface IDashboardService
    {
        Task<DashboardSummaryDto> GetDashboardMetricsAsync(string businessId);
    }

    public class DashboardService : IDashboardService
    {
        private readonly AppDbContext _context;

        public DashboardService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<DashboardSummaryDto> GetDashboardMetricsAsync(string businessId)
        {
            var today = DateTime.UtcNow.Date;
            var startOfMonth = new DateTime(today.Year, today.Month, 1, 0, 0, 0, DateTimeKind.Utc);

            // Fetch Real Database Sales
            var salesQuery = _context.Sales.Where(s => s.BusinessId == businessId && !s.IsDeleted);
            var todaySalesList = await salesQuery
                .Where(s => s.SaleDate >= today)
                .Select(s => s.GrandTotal)
                .ToListAsync();
            var todaySales = todaySalesList.Sum();

            var monthlySalesList = await salesQuery
                .Where(s => s.SaleDate >= startOfMonth)
                .Select(s => s.GrandTotal)
                .ToListAsync();
            var monthlySales = monthlySalesList.Sum();

            // Fetch Real COGS from SaleItems
            var monthlySaleIds = await salesQuery
                .Where(s => s.SaleDate >= startOfMonth)
                .Select(s => s.Id)
                .ToListAsync();

            var saleItems = await _context.SaleItems
                .Where(i => monthlySaleIds.Contains(i.SaleId))
                .Select(i => new { i.Quantity, i.UnitCost })
                .ToListAsync();
            var cogs = saleItems.Sum(i => i.Quantity * i.UnitCost);

            // Fetch Real Expenses
            var expensesList = await _context.Expenses
                .Where(e => e.BusinessId == businessId && !e.IsDeleted && e.ExpenseDate >= startOfMonth)
                .Select(e => e.Amount)
                .ToListAsync();
            var monthlyExpenses = expensesList.Sum();

            var netProfit = monthlySales - cogs - monthlyExpenses;

            // Fetch Customer Receivables
            var receivablesList = await _context.Customers
                .Where(c => c.BusinessId == businessId && !c.IsDeleted)
                .Select(c => c.CurrentBalance)
                .ToListAsync();
            var receivables = receivablesList.Sum();

            // Fetch Supplier Payables
            var payablesList = await _context.Purchases
                .Where(p => p.BusinessId == businessId && !p.IsDeleted)
                .Select(p => p.DueBalance)
                .ToListAsync();
            var payables = payablesList.Sum();

            // Products & Stock Counts and Valuation
            var activeProducts = await _context.Products
                .Where(p => p.BusinessId == businessId && !p.IsDeleted)
                .Select(p => new { p.StockQuantity, p.CostPrice, p.MinStockAlert })
                .ToListAsync();

            var totalProducts = activeProducts.Count;
            var lowStockCount = activeProducts.Count(p => p.StockQuantity <= p.MinStockAlert);
            var totalStockValuation = activeProducts.Sum(p => p.StockQuantity * p.CostPrice);

            // Bank Accounts Balances
            var bankAccountsList = await _context.BankAccounts
                .Where(b => b.BusinessId == businessId && !b.IsDeleted && b.IsActive)
                .Select(b => b.CurrentBalance)
                .ToListAsync();
            var bankBalance = bankAccountsList.Sum();

            // Cash Balance from Cash Account or Vouchers
            var cashBalance = 0.00m;
            var cashAccount = await _context.AccountHeads
                .FirstOrDefaultAsync(a => a.BusinessId == businessId && a.Code == "1001" && !a.IsDeleted);

            if (cashAccount != null)
            {
                var cashEntries = await _context.VoucherEntries
                    .Where(e => e.AccountHeadId == cashAccount.Id)
                    .Select(e => new { e.DebitAmount, e.CreditAmount })
                    .ToListAsync();
                cashBalance = cashEntries.Sum(e => e.DebitAmount - e.CreditAmount);
            }

            var customersCount = await _context.Customers.CountAsync(c => c.BusinessId == businessId && !c.IsDeleted);
            var salesCount = await salesQuery.CountAsync();

            // Recent Activities
            var recentSales = await salesQuery
                .OrderByDescending(s => s.SaleDate)
                .Take(5)
                .Select(s => new RecentActivityDto
                {
                    Id = s.Id,
                    ActivityType = "Sale",
                    Title = $"Invoice #{s.InvoiceNumber}",
                    Amount = s.GrandTotal,
                    Timestamp = s.SaleDate,
                    Status = s.DueBalance > 0 ? $"Due: PKR {s.DueBalance:N2}" : "Paid"
                })
                .ToListAsync();

            return new DashboardSummaryDto
            {
                TodaySales = todaySales,
                MonthlySales = monthlySales,
                TotalCostOfGoodsSold = cogs,
                TotalExpenses = monthlyExpenses,
                NetProfit = netProfit,
                TotalReceivables = receivables,
                TotalPayables = payables,
                CashBalance = cashBalance,
                BankBalance = bankBalance,
                LowStockCount = lowStockCount,
                TotalProductsCount = totalProducts,
                TotalStockValuation = totalStockValuation,
                TotalCustomersCount = customersCount,
                TotalSalesCount = salesCount,
                RecentActivities = recentSales
            };
        }
    }

    public interface IBackupService
    {
        Task<LocalBackupRecord> CreateBackupAsync(string businessId, string destinationDirectory, string dbFilePath);
        Task<bool> VerifyBackupAsync(string backupFilePath);
        Task<bool> RestoreBackupAsync(string businessId, string backupFilePath, string targetDbPath);
        Task<List<LocalBackupRecord>> GetBackupHistoryAsync(string businessId);
    }

    public class BackupService : IBackupService
    {
        private readonly AppDbContext _context;

        public BackupService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<LocalBackupRecord> CreateBackupAsync(string businessId, string destinationDirectory, string dbFilePath)
        {
            if (!Directory.Exists(destinationDirectory))
            {
                Directory.CreateDirectory(destinationDirectory);
            }

            var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
            var fileName = $"ModernInventory_Backup_{timestamp}.db";
            var backupFilePath = Path.Combine(destinationDirectory, fileName);

            // Execute copy (or SQLite VACUUM INTO)
            File.Copy(dbFilePath, backupFilePath, true);

            var fileInfo = new FileInfo(backupFilePath);
            string checksum;
            using (var sha256 = SHA256.Create())
            await using (var stream = new FileStream(backupFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, 4096, useAsync: true))
            {
                var hashBytes = await sha256.ComputeHashAsync(stream);
                checksum = BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
            }

            var entityCounts = new Dictionary<string, int>
            {
                { "Products", await _context.Products.CountAsync(p => p.BusinessId == businessId && !p.IsDeleted) },
                { "Customers", await _context.Customers.CountAsync(c => c.BusinessId == businessId && !c.IsDeleted) },
                { "Sales", await _context.Sales.CountAsync(s => s.BusinessId == businessId && !s.IsDeleted) },
                { "Purchases", await _context.Purchases.CountAsync(p => p.BusinessId == businessId && !p.IsDeleted) }
            };

            var record = new LocalBackupRecord
            {
                Id = Guid.NewGuid().ToString(),
                BusinessId = businessId,
                FileName = fileName,
                FilePath = backupFilePath,
                FileSizeBytes = fileInfo.Length,
                Checksum = checksum,
                EntityCountsJson = JsonSerializer.Serialize(entityCounts),
                CreatedAt = DateTime.UtcNow
            };

            await _context.LocalBackups.AddAsync(record);
            await _context.SaveChangesAsync();

            return record;
        }

        public async Task<bool> VerifyBackupAsync(string backupFilePath)
        {
            if (!File.Exists(backupFilePath))
            {
                return false;
            }

            SqliteConnection? conn = null;
            try
            {
                var connectionString = new SqliteConnectionStringBuilder
                {
                    DataSource = backupFilePath,
                    Mode = SqliteOpenMode.ReadOnly,
                    Pooling = false
                }.ToString();

                conn = new SqliteConnection(connectionString);
                await conn.OpenAsync();

                await using var cmd = conn.CreateCommand();
                cmd.CommandText = "PRAGMA integrity_check;";
                var result = (string?)await cmd.ExecuteScalarAsync();

                return result?.Trim().ToLowerInvariant() == "ok";
            }
            catch
            {
                return false;
            }
            finally
            {
                if (conn != null)
                {
                    try
                    {
                        conn.Close();
                        SqliteConnection.ClearPool(conn);
                    }
                    catch
                    {
                        // Ignore cleanup exceptions
                    }
                    finally
                    {
                        conn.Dispose();
                    }
                }
            }
        }

        public async Task<bool> RestoreBackupAsync(string businessId, string backupFilePath, string targetDbPath)
        {
            bool isValid = await VerifyBackupAsync(backupFilePath);
            if (!isValid)
            {
                throw new InvalidOperationException("Backup verification failed. The file is corrupt or not a valid SQLite database.");
            }

            // Create pre-restore safety snapshot
            var preRestoreDir = Path.Combine(Path.GetDirectoryName(targetDbPath) ?? ".", "pre_restore_snapshots");
            if (!Directory.Exists(preRestoreDir))
            {
                Directory.CreateDirectory(preRestoreDir);
            }

            var snapshotPath = Path.Combine(preRestoreDir, $"PreRestore_{DateTime.UtcNow:yyyyMMdd_HHmmss}.db");
            if (File.Exists(targetDbPath))
            {
                File.Copy(targetDbPath, snapshotPath, true);
            }

            // Clear connection pools before replacing target database file
            SqliteConnection.ClearAllPools();

            // Copy verified backup over target database file
            File.Copy(backupFilePath, targetDbPath, true);
            return true;
        }

        public async Task<List<LocalBackupRecord>> GetBackupHistoryAsync(string businessId)
        {
            return await _context.LocalBackups
                .Where(b => b.BusinessId == businessId)
                .OrderByDescending(b => b.CreatedAt)
                .ToListAsync();
        }
    }

    public interface ISyncQueueService
    {
        Task EnqueueAsync(string businessId, string entityType, string entityId, SyncOperationType opType, object payload);
        Task<List<SyncQueueItem>> GetPendingQueueAsync(string businessId, int limit = 50);
        Task MarkProcessedAsync(string queueItemId);
        Task RecordFailureAsync(string queueItemId, string errorMessage);
        Task<int> GetPendingCountAsync(string businessId);
    }

    public class SyncQueueService : ISyncQueueService
    {
        private readonly AppDbContext _context;

        public SyncQueueService(AppDbContext context)
        {
            _context = context;
        }

        public async Task EnqueueAsync(string businessId, string entityType, string entityId, SyncOperationType opType, object payload)
        {
            var item = new SyncQueueItem
            {
                Id = Guid.NewGuid().ToString(),
                BusinessId = businessId,
                EntityType = entityType,
                EntityId = entityId,
                OperationType = opType,
                PayloadJson = JsonSerializer.Serialize(payload),
                CreatedAt = DateTime.UtcNow
            };

            await _context.SyncQueue.AddAsync(item);
            await _context.SaveChangesAsync();
        }

        public async Task<List<SyncQueueItem>> GetPendingQueueAsync(string businessId, int limit = 50)
        {
            return await _context.SyncQueue
                .Where(q => q.BusinessId == businessId)
                .OrderBy(q => q.CreatedAt)
                .Take(limit)
                .ToListAsync();
        }

        public async Task MarkProcessedAsync(string queueItemId)
        {
            var item = await _context.SyncQueue.FirstOrDefaultAsync(q => q.Id == queueItemId);
            if (item != null)
            {
                _context.SyncQueue.Remove(item);
                await _context.SaveChangesAsync();
            }
        }

        public async Task RecordFailureAsync(string queueItemId, string errorMessage)
        {
            var item = await _context.SyncQueue.FirstOrDefaultAsync(q => q.Id == queueItemId);
            if (item != null)
            {
                item.RetryCount++;
                item.LastAttemptAt = DateTime.UtcNow;
                item.LastError = errorMessage;
                await _context.SaveChangesAsync();
            }
        }

        public async Task<int> GetPendingCountAsync(string businessId)
        {
            return await _context.SyncQueue.CountAsync(q => q.BusinessId == businessId);
        }
    }
}
