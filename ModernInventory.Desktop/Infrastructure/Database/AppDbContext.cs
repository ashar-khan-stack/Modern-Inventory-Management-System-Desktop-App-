using Microsoft.EntityFrameworkCore;
using ModernInventory.Desktop.Domain.Entities;

namespace ModernInventory.Desktop.Infrastructure.Database
{
    public class AppDbContext : DbContext
    {
        public DbSet<BusinessProfile> Businesses => Set<BusinessProfile>();
        public DbSet<User> Users => Set<User>();
        public DbSet<Category> Categories => Set<Category>();
        public DbSet<Product> Products => Set<Product>();
        public DbSet<Customer> Customers => Set<Customer>();
        public DbSet<Employee> Employees => Set<Employee>();
        public DbSet<Sale> Sales => Set<Sale>();
        public DbSet<SaleItem> SaleItems => Set<SaleItem>();
        public DbSet<AccountHead> AccountHeads => Set<AccountHead>();
        public DbSet<Voucher> Vouchers => Set<Voucher>();
        public DbSet<VoucherEntry> VoucherEntries => Set<VoucherEntry>();
        public DbSet<SyncQueueItem> SyncQueue => Set<SyncQueueItem>();

        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Indexes for fast tenant-scoped queries
            modelBuilder.Entity<Product>().HasIndex(p => new { p.BusinessId, p.IsDeleted });
            modelBuilder.Entity<Customer>().HasIndex(c => new { c.BusinessId, c.IsDeleted });
            modelBuilder.Entity<Employee>().HasIndex(e => new { e.BusinessId, e.IsDeleted });
            modelBuilder.Entity<Sale>().HasIndex(s => new { s.BusinessId, s.IsDeleted });
            modelBuilder.Entity<AccountHead>().HasIndex(a => new { a.BusinessId, a.IsDeleted });
            modelBuilder.Entity<SyncQueueItem>().HasIndex(q => new { q.BusinessId, q.CreatedAt });

            // Ensure Decimal Precision for Pakistani Rupee (PKR)
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
