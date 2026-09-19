using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using ModernInventory.Core.Application.DTOs;
using ModernInventory.Core.Application.Repositories;
using ModernInventory.Core.Application.Services;
using ModernInventory.Core.Domain.Entities;
using ModernInventory.Core.Domain.Enums;
using ModernInventory.Core.Infrastructure.Database;
using ModernInventory.Core.Infrastructure.Security;
using Xunit;

namespace ModernInventory.Tests
{
    public class Phase1CoreTests : IDisposable
    {
        private readonly SqliteConnection _connection;
        private readonly AppDbContext _context;
        private readonly IPasswordHasher _passwordHasher;
        private readonly IAuthService _authService;
        private readonly IDashboardService _dashboardService;
        private readonly ISyncQueueService _syncQueueService;
        private readonly IBackupService _backupService;

        public Phase1CoreTests()
        {
            _connection = new SqliteConnection("Data Source=:memory:");
            _connection.Open();

            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite(_connection)
                .Options;

            _context = new AppDbContext(options);
            _context.Database.EnsureCreated();

            _passwordHasher = new PasswordHasher();
            _authService = new AuthService(_context, _passwordHasher);
            _dashboardService = new DashboardService(_context);
            _syncQueueService = new SyncQueueService(_context);
            _backupService = new BackupService(_context);
        }

        public void Dispose()
        {
            _context.Dispose();
            _connection.Dispose();
        }

        [Fact]
        public void Test01_PasswordHasher_GeneratesUniqueSaltsAndVerifiesCorrectly()
        {
            string password = "StrongPassword@123";

            var (hash1, salt1) = _passwordHasher.HashPassword(password);
            var (hash2, salt2) = _passwordHasher.HashPassword(password);

            Assert.NotEmpty(hash1);
            Assert.NotEmpty(salt1);
            Assert.NotEqual(salt1, salt2); // Salts must be unique
            Assert.NotEqual(hash1, hash2); // Hashes differ because of different salts

            Assert.True(_passwordHasher.VerifyPassword(password, hash1, salt1));
            Assert.False(_passwordHasher.VerifyPassword("WrongPassword", hash1, salt1));
        }

        [Fact]
        public void Test02_PasswordHasher_RejectsInvalidInputAndMalformedHashes()
        {
            Assert.Throws<ArgumentException>(() => _passwordHasher.HashPassword(""));
            Assert.False(_passwordHasher.VerifyPassword("password", "invalid_base64_hash", "invalid_base64_salt"));
            Assert.False(_passwordHasher.VerifyPassword("", "hash", "salt"));
        }

        [Fact]
        public async Task Test03_AuthService_RegistersUser_CreatesBusinessAndDefaultChartOfAccounts()
        {
            var req = new RegisterRequestDto
            {
                BusinessName = "Lahore Tech Hub",
                OwnerName = "Muhammad Ali",
                Email = "ali@lahoretech.pk",
                Phone = "+92 300 1234567",
                Password = "SecretPassword123!",
                SecurityQuestion = "What is your primary phone?",
                SecurityAnswer = "+923001234567",
                Currency = "PKR"
            };

            var result = await _authService.RegisterAsync(req);

            Assert.True(result.Success);
            Assert.NotEmpty(result.Token!);
            Assert.NotEmpty(result.BusinessId!);

            var business = await _context.Businesses.FindAsync(result.BusinessId);
            Assert.NotNull(business);
            Assert.Equal("Lahore Tech Hub", business.BusinessName);
            Assert.Equal("PKR", business.Currency);

            var accounts = await _context.AccountHeads.Where(a => a.BusinessId == result.BusinessId).ToListAsync();
            Assert.True(accounts.Count >= 9, "Pakistani Chart of Accounts must be automatically seeded.");
            Assert.Contains(accounts, a => a.Code == "1001" && a.Name == "Cash in Hand");
            Assert.Contains(accounts, a => a.Code == "4001" && a.Name == "Sales Revenue");
        }

        [Fact]
        public async Task Test04_AuthService_EnforcesUniqueEmailAndCaseInsensitivity()
        {
            var req = new RegisterRequestDto
            {
                BusinessName = "Karachi Mart",
                OwnerName = "Bilal Ahmed",
                Email = "Bilal@KarachiMart.com",
                Password = "Password@456"
            };
            await _authService.RegisterAsync(req);

            var duplicateReq = new RegisterRequestDto
            {
                BusinessName = "Karachi Mart 2",
                OwnerName = "Bilal Ahmed",
                Email = "bilal@karachimart.com", // lower case
                Password = "Password@456"
            };
            var duplicateResult = await _authService.RegisterAsync(duplicateReq);

            Assert.False(duplicateResult.Success);
            Assert.Contains("already exists", duplicateResult.ErrorMessage);
        }

        [Fact]
        public async Task Test05_AuthService_ValidatesLogin_AndRejectsInvalidCredentials()
        {
            var req = new RegisterRequestDto
            {
                BusinessName = "Rawalpindi Auto Store",
                OwnerName = "Tariq Khan",
                Email = "tariq@rawalpindiauto.pk",
                Password = "AutoStorePassword#1"
            };
            await _authService.RegisterAsync(req);

            // Valid Login
            var loginResult = await _authService.LoginAsync("tariq@rawalpindiauto.pk", "AutoStorePassword#1", rememberMe: true);
            Assert.True(loginResult.Success);
            Assert.NotEmpty(loginResult.Token!);

            // Invalid Password
            var invalidPassResult = await _authService.LoginAsync("tariq@rawalpindiauto.pk", "WrongPassword#999");
            Assert.False(invalidPassResult.Success);

            // Non-existent Email
            var unknownEmailResult = await _authService.LoginAsync("nobody@random.com", "Password@123");
            Assert.False(unknownEmailResult.Success);
        }

        [Fact]
        public async Task Test06_AuthService_SessionManagement_ValidatesAndRevokesSession()
        {
            var req = new RegisterRequestDto
            {
                BusinessName = "Peshawar Carpet Palace",
                OwnerName = "Rashid Khan",
                Email = "rashid@carpetpalace.pk",
                Password = "CarpetPassword#2"
            };
            var reg = await _authService.RegisterAsync(req);

            // Validate Active Session
            var valResult = await _authService.ValidateSessionAsync(reg.Token!);
            Assert.True(valResult.Success);
            Assert.Equal("rashid@carpetpalace.pk", valResult.Email);

            // Logout
            await _authService.LogoutAsync(reg.Token!);

            // Re-validate Revoked Session
            var postLogoutResult = await _authService.ValidateSessionAsync(reg.Token!);
            Assert.False(postLogoutResult.Success);
        }

        [Fact]
        public async Task Test07_AuthService_ResetPassword_ValidatesSecurityAnswer()
        {
            var req = new RegisterRequestDto
            {
                BusinessName = "Quetta Dry Fruit",
                OwnerName = "Jan Muhammad",
                Email = "jan@quettadryfruit.pk",
                Password = "OldPassword#100",
                SecurityQuestion = "What is your primary phone?",
                SecurityAnswer = "03331234567"
            };
            await _authService.RegisterAsync(req);

            // Incorrect Security Answer
            bool failedReset = await _authService.ResetPasswordAsync("jan@quettadryfruit.pk", "wrong-answer", "NewPassword#200");
            Assert.False(failedReset);

            // Correct Security Answer
            bool successReset = await _authService.ResetPasswordAsync("jan@quettadryfruit.pk", "03331234567", "NewPassword#200");
            Assert.True(successReset);

            // Verify login with new password
            var login = await _authService.LoginAsync("jan@quettadryfruit.pk", "NewPassword#200");
            Assert.True(login.Success);
        }

        [Fact]
        public async Task Test08_Repository_EnforcesMultiTenantIsolation()
        {
            string businessA = Guid.NewGuid().ToString();
            string businessB = Guid.NewGuid().ToString();

            var repo = new Repository<Product>(_context);

            var productA = new Product
            {
                BusinessId = businessA,
                Name = "LED Monitor 24 inch",
                Sku = "MON-24",
                Barcode = "89640001",
                SalePrice = 35000.00m,
                StockQuantity = 10.00m
            };

            var productB = new Product
            {
                BusinessId = businessB,
                Name = "Wireless Keyboard",
                Sku = "KB-WL",
                Barcode = "89640002",
                SalePrice = 4500.00m,
                StockQuantity = 25.00m
            };

            await repo.AddAsync(productA);
            await repo.AddAsync(productB);

            var productsForA = await repo.GetAllAsync(businessA);
            var productsForB = await repo.GetAllAsync(businessB);

            Assert.Single(productsForA);
            Assert.Equal("LED Monitor 24 inch", productsForA[0].Name);

            Assert.Single(productsForB);
            Assert.Equal("Wireless Keyboard", productsForB[0].Name);
        }

        [Fact]
        public async Task Test09_Repository_EnforcesSoftDelete_HidesDeletedEntitiesByDefault()
        {
            string businessId = Guid.NewGuid().ToString();
            var repo = new Repository<Customer>(_context);

            var customer = new Customer
            {
                BusinessId = businessId,
                Name = "Ahmed Enterprises",
                CurrentBalance = 5000.00m
            };
            await repo.AddAsync(customer);

            await repo.SoftDeleteAsync(customer.Id, businessId);

            var active = await repo.GetAllAsync(businessId);
            var all = await repo.GetAllAsync(businessId, includeDeleted: true);

            Assert.Empty(active);
            Assert.Single(all);
            Assert.True(all[0].IsDeleted);
        }

        [Fact]
        public async Task Test10_SyncQueue_CapturesOperationsInFifoOrder()
        {
            string businessId = Guid.NewGuid().ToString();

            await _syncQueueService.EnqueueAsync(businessId, "Customer", "cust-1", SyncOperationType.CREATE, new { Name = "Tariq Traders" });
            await _syncQueueService.EnqueueAsync(businessId, "Customer", "cust-1", SyncOperationType.UPDATE, new { Name = "Tariq Traders Pvt Ltd" });

            var queue = await _syncQueueService.GetPendingQueueAsync(businessId);
            Assert.Equal(2, queue.Count);
            Assert.Equal(SyncOperationType.CREATE, queue[0].OperationType);
            Assert.Equal(SyncOperationType.UPDATE, queue[1].OperationType);

            await _syncQueueService.MarkProcessedAsync(queue[0].Id);
            var remainingQueue = await _syncQueueService.GetPendingQueueAsync(businessId);
            Assert.Single(remainingQueue);
            Assert.Equal(SyncOperationType.UPDATE, remainingQueue[0].OperationType);
        }

        [Fact]
        public async Task Test11_SyncQueue_TracksRetryCountAndFailureErrors()
        {
            string businessId = Guid.NewGuid().ToString();
            await _syncQueueService.EnqueueAsync(businessId, "Sale", "sale-1", SyncOperationType.CREATE, new { InvoiceNumber = "INV-001" });

            var queue = await _syncQueueService.GetPendingQueueAsync(businessId);
            var item = queue.First();

            await _syncQueueService.RecordFailureAsync(item.Id, "Network timeout connecting to server.");

            var updatedQueue = await _syncQueueService.GetPendingQueueAsync(businessId);
            var updatedItem = updatedQueue.First();

            Assert.Equal(1, updatedItem.RetryCount);
            Assert.Equal("Network timeout connecting to server.", updatedItem.LastError);
            Assert.NotNull(updatedItem.LastAttemptAt);
        }

        [Fact]
        public async Task Test12_DashboardService_ReturnsZeroForEmptyDatabase_NoFakeData()
        {
            string businessId = Guid.NewGuid().ToString();

            var emptyMetrics = await _dashboardService.GetDashboardMetricsAsync(businessId);
            Assert.Equal(0.00m, emptyMetrics.TodaySales);
            Assert.Equal(0.00m, emptyMetrics.MonthlySales);
            Assert.Equal(0.00m, emptyMetrics.NetProfit);
            Assert.Equal(0.00m, emptyMetrics.TotalReceivables);
            Assert.Equal(0.00m, emptyMetrics.TotalPayables);
            Assert.Equal(0, emptyMetrics.TotalProductsCount);
            Assert.Equal(0, emptyMetrics.LowStockCount);
            Assert.Empty(emptyMetrics.RecentActivities);
        }

        [Fact]
        public async Task Test13_DashboardService_CalculatesAccurateFinancialMetrics()
        {
            string businessId = Guid.NewGuid().ToString();

            var prod = new Product
            {
                BusinessId = businessId,
                Name = "Core i7 Processor",
                CostPrice = 40000.00m,
                SalePrice = 55000.00m,
                StockQuantity = 2.00m,
                MinStockAlert = 5.00m
            };
            _context.Products.Add(prod);

            var sale = new Sale
            {
                BusinessId = businessId,
                InvoiceNumber = "INV-0001",
                SaleDate = DateTime.UtcNow,
                Subtotal = 55000.00m,
                GrandTotal = 55000.00m,
                PaidAmount = 40000.00m,
                DueBalance = 15000.00m
            };
            _context.Sales.Add(sale);

            var item = new SaleItem
            {
                SaleId = sale.Id,
                ProductId = prod.Id,
                ProductName = prod.Name,
                Quantity = 1.00m,
                UnitPrice = 55000.00m,
                UnitCost = 40000.00m,
                TotalPrice = 55000.00m
            };
            _context.SaleItems.Add(item);

            var expense = new Expense
            {
                BusinessId = businessId,
                Title = "Office Internet",
                Amount = 5000.00m,
                ExpenseDate = DateTime.UtcNow
            };
            _context.Expenses.Add(expense);

            var customer = new Customer
            {
                BusinessId = businessId,
                Name = "Faisal Associates",
                CurrentBalance = 15000.00m
            };
            _context.Customers.Add(customer);

            await _context.SaveChangesAsync();

            var metrics = await _dashboardService.GetDashboardMetricsAsync(businessId);

            Assert.Equal(55000.00m, metrics.TodaySales);
            Assert.Equal(55000.00m, metrics.MonthlySales);
            Assert.Equal(40000.00m, metrics.TotalCostOfGoodsSold);
            Assert.Equal(5000.00m, metrics.TotalExpenses);
            Assert.Equal(10000.00m, metrics.NetProfit);
            Assert.Equal(15000.00m, metrics.TotalReceivables);
            Assert.Equal(1, metrics.LowStockCount);
            Assert.Equal(1, metrics.TotalProductsCount);
        }

        [Fact]
        public async Task Test14_BackupService_CreatesDatabaseCopy_GeneratesSha256Checksum()
        {
            string tempDir = Path.Combine(Path.GetTempPath(), "ModernInventory_BackupTests_" + Guid.NewGuid());
            Directory.CreateDirectory(tempDir);
            string dbPath = Path.Combine(tempDir, "source.db");
            string backupDir = Path.Combine(tempDir, "Backups");

            var connStr = new SqliteConnectionStringBuilder
            {
                DataSource = dbPath,
                Pooling = false
            }.ToString();

            await using (var conn = new SqliteConnection(connStr))
            {
                await conn.OpenAsync();
                await using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "CREATE TABLE TestTable (Id INTEGER PRIMARY KEY, Val TEXT); INSERT INTO TestTable VALUES (1, 'OK');";
                    await cmd.ExecuteNonQueryAsync();
                }
                await conn.CloseAsync();
                SqliteConnection.ClearPool(conn);
            }

            string businessId = Guid.NewGuid().ToString();
            var backupRecord = await _backupService.CreateBackupAsync(businessId, backupDir, dbPath);

            Assert.NotNull(backupRecord);
            Assert.True(File.Exists(backupRecord.FilePath));
            Assert.NotEmpty(backupRecord.Checksum);
            Assert.True(backupRecord.FileSizeBytes > 0);

            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }
        }

        [Fact]
        public async Task Test15_BackupService_VerifiesSqliteIntegrity_RejectsCorruptFiles()
        {
            string tempDir = Path.Combine(Path.GetTempPath(), "ModernInventory_VerifyTests_" + Guid.NewGuid());
            Directory.CreateDirectory(tempDir);
            string validDbPath = Path.Combine(tempDir, "valid.db");
            string corruptDbPath = Path.Combine(tempDir, "corrupt.db");

            var connStr = new SqliteConnectionStringBuilder
            {
                DataSource = validDbPath,
                Pooling = false
            }.ToString();

            await using (var validConn = new SqliteConnection(connStr))
            {
                await validConn.OpenAsync();
                await using (var cmd = validConn.CreateCommand())
                {
                    cmd.CommandText = "CREATE TABLE TestTable (Id INTEGER PRIMARY KEY, Val TEXT); INSERT INTO TestTable VALUES (1, 'OK');";
                    await cmd.ExecuteNonQueryAsync();
                }
                await validConn.CloseAsync();
                SqliteConnection.ClearPool(validConn);
            }

            File.WriteAllText(corruptDbPath, "This is corrupt garbage text, not SQLite database!");

            bool isValid = await _backupService.VerifyBackupAsync(validDbPath);
            bool isCorruptValid = await _backupService.VerifyBackupAsync(corruptDbPath);

            Assert.True(isValid);
            Assert.False(isCorruptValid);

            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }
        }
    }
}
