using System;
using System.IO;
using System.Windows;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ModernInventory.Core.Application.Repositories;
using ModernInventory.Core.Application.Services;
using ModernInventory.Core.Infrastructure.Database;
using ModernInventory.Core.Infrastructure.Security;
using ModernInventory.Desktop.Presentation.ViewModels;

namespace ModernInventory.Desktop
{
    public partial class App : System.Windows.Application
    {
        public static IServiceProvider ServiceProvider { get; private set; } = null!;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            var services = new ServiceCollection();

            // SQLite Local Database Path
            var appDataFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ModernInventoryDesktop");
            if (!Directory.Exists(appDataFolder))
            {
                Directory.CreateDirectory(appDataFolder);
            }
            var dbPath = Path.Combine(appDataFolder, "modern_inventory.db");

            services.AddDbContext<AppDbContext>(options =>
                options.UseSqlite($"Data Source={dbPath}"));

            // Core Security & Repositories & Services
            services.AddSingleton<IPasswordHasher, PasswordHasher>();
            services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
            services.AddScoped<IProductRepository, ProductRepository>();
            services.AddScoped<ICategoryRepository, CategoryRepository>();
            services.AddScoped<IStockAdjustmentRepository, StockAdjustmentRepository>();
            services.AddScoped<ICustomerRepository, CustomerRepository>();
            services.AddScoped<IEmployeeRepository, EmployeeRepository>();
            services.AddScoped<ISupplierRepository, SupplierRepository>();
            services.AddScoped<IPurchaseRepository, PurchaseRepository>();
            services.AddScoped<ISaleRepository, SaleRepository>();
            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<IDashboardService, DashboardService>();
            services.AddScoped<IInventoryService, InventoryService>();
            services.AddScoped<ICustomerService, CustomerService>();
            services.AddScoped<IEmployeeService, EmployeeService>();
            services.AddScoped<ISupplierService, SupplierService>();
            services.AddScoped<IPurchaseService, PurchaseService>();
            services.AddScoped<ISaleService, SaleService>();
            services.AddScoped<IBackupService, BackupService>();
            services.AddScoped<ISyncQueueService, SyncQueueService>();

            // ViewModels
            services.AddSingleton<MainViewModel>();
            services.AddTransient<LoginViewModel>();
            services.AddTransient<RegisterViewModel>();
            services.AddTransient<ForgotPasswordViewModel>();
            services.AddTransient<DashboardViewModel>();
            services.AddTransient<BackupSyncViewModel>();
            services.AddTransient<ProductsViewModel>();
            services.AddTransient<CategoriesViewModel>();
            services.AddTransient<StockAdjustmentViewModel>();
            services.AddTransient<StockHistoryViewModel>();
            services.AddTransient<CustomersViewModel>();
            services.AddTransient<EmployeesViewModel>();
            services.AddTransient<SuppliersViewModel>();
            services.AddTransient<PurchasesViewModel>();
            services.AddTransient<POSViewModel>();
            services.AddTransient<SalesViewModel>();

            ServiceProvider = services.BuildServiceProvider();

            // Ensure SQLite database and tables are created
            using (var scope = ServiceProvider.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                db.Database.EnsureCreated();
            }

            var mainWindow = new MainWindow
            {
                DataContext = ServiceProvider.GetRequiredService<MainViewModel>()
            };
            mainWindow.Show();
        }
    }
}
