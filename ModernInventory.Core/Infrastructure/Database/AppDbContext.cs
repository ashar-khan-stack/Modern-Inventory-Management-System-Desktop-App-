using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using ModernInventory.Core.Domain.Entities;

namespace ModernInventory.Core.Infrastructure.Database
{
    public class AppDbContext : DbContext
    {
        public DbSet<BusinessProfile> Businesses => Set<BusinessProfile>();
        public DbSet<User> Users => Set<User>();
        public DbSet<UserSession> UserSessions => Set<UserSession>();
        public DbSet<Category> Categories => Set<Category>();
        public DbSet<Product> Products => Set<Product>();
        public DbSet<StockAdjustment> StockAdjustments => Set<StockAdjustment>();
        public DbSet<Customer> Customers => Set<Customer>();
        public DbSet<Supplier> Suppliers => Set<Supplier>();
        public DbSet<Employee> Employees => Set<Employee>();
        public DbSet<Sale> Sales => Set<Sale>();
        public DbSet<SaleItem> SaleItems => Set<SaleItem>();
        public DbSet<Purchase> Purchases => Set<Purchase>();
        public DbSet<PurchaseItem> PurchaseItems => Set<PurchaseItem>();
        public DbSet<AccountHead> AccountHeads => Set<AccountHead>();
        public DbSet<Voucher> Vouchers => Set<Voucher>();
        public DbSet<VoucherEntry> VoucherEntries => Set<VoucherEntry>();
        public DbSet<Expense> Expenses => Set<Expense>();
        public DbSet<BankAccount> BankAccounts => Set<BankAccount>();
        public DbSet<BankTransaction> BankTransactions => Set<BankTransaction>();
        public DbSet<SyncQueueItem> SyncQueue => Set<SyncQueueItem>();
        public DbSet<LocalBackupRecord> LocalBackups => Set<LocalBackupRecord>();

        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Indexes for Multi-Tenant Scoping & Soft Delete performance
            modelBuilder.Entity<User>().HasIndex(u => new { u.Email }).IsUnique();
            modelBuilder.Entity<User>().HasIndex(u => u.BusinessId);
            modelBuilder.Entity<UserSession>().HasIndex(s => s.Token).IsUnique();
            modelBuilder.Entity<UserSession>().HasIndex(s => new { s.BusinessId, s.ExpiresAt });

            modelBuilder.Entity<Category>().HasIndex(c => new { c.BusinessId, c.IsDeleted });
            modelBuilder.Entity<Product>().HasIndex(p => new { p.BusinessId, p.IsDeleted });
            modelBuilder.Entity<Product>().HasIndex(p => new { p.BusinessId, p.Barcode });
            modelBuilder.Entity<Product>().HasIndex(p => new { p.BusinessId, p.Sku });

            modelBuilder.Entity<StockAdjustment>().HasIndex(s => new { s.BusinessId, s.ProductId });
            modelBuilder.Entity<Customer>().HasIndex(c => new { c.BusinessId, c.IsDeleted });
            modelBuilder.Entity<Supplier>().HasIndex(s => new { s.BusinessId, s.IsDeleted });
            modelBuilder.Entity<Employee>().HasIndex(e => new { e.BusinessId, e.IsDeleted });

            modelBuilder.Entity<Sale>().HasIndex(s => new { s.BusinessId, s.IsDeleted });
            modelBuilder.Entity<Sale>().HasIndex(s => new { s.BusinessId, s.InvoiceNumber });
            modelBuilder.Entity<SaleItem>().HasIndex(i => i.SaleId);

            modelBuilder.Entity<Purchase>().HasIndex(p => new { p.BusinessId, p.IsDeleted });
            modelBuilder.Entity<PurchaseItem>().HasIndex(i => i.PurchaseId);

            modelBuilder.Entity<AccountHead>().HasIndex(a => new { a.BusinessId, a.IsDeleted });
            modelBuilder.Entity<AccountHead>().HasIndex(a => new { a.BusinessId, a.Code });

            modelBuilder.Entity<Voucher>().HasIndex(v => new { v.BusinessId, v.IsDeleted });
            modelBuilder.Entity<Voucher>().HasIndex(v => new { v.BusinessId, v.VoucherNumber });
            modelBuilder.Entity<VoucherEntry>().HasIndex(e => e.VoucherId);

            modelBuilder.Entity<Expense>().HasIndex(e => new { e.BusinessId, e.IsDeleted });
            modelBuilder.Entity<BankAccount>().HasIndex(b => new { b.BusinessId, b.IsDeleted });
            modelBuilder.Entity<BankTransaction>().HasIndex(t => new { t.BusinessId, t.BankAccountId });

            modelBuilder.Entity<SyncQueueItem>().HasIndex(q => new { q.BusinessId, q.CreatedAt });
            modelBuilder.Entity<LocalBackupRecord>().HasIndex(b => new { b.BusinessId, b.CreatedAt });

            // Decimal Precision Config for Pakistani Rupee (PKR - 18, 2)
            foreach (var property in modelBuilder.Model.GetEntityTypes()
                .SelectMany(t => t.GetProperties())
                .Where(p => p.ClrType == typeof(decimal) || p.ClrType == typeof(decimal?)))
            {
                property.SetPrecision(18);
                property.SetScale(2);
            }
        }
    }
}
