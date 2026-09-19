using System;
using System.IO;
using System.Windows;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ModernInventory.Core.Application.Repositories;
using ModernInventory.Core.Application.Services;
using ModernInventory.Core.Infrastructure.Database;
using ModernInventory.Core.Infrastructure.Security;
using ModernInventory.Desktop.Infrastructure;
using ModernInventory.Desktop.Presentation.ViewModels;

namespace ModernInventory.Desktop
{
    public partial class App : System.Windows.Application
    {
        public static IServiceProvider ServiceProvider { get; private set; } = null!;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Global exception handling to local log file
            DispatcherUnhandledException += (s, args) =>
            {
                LocalPathProvider.LogError("DispatcherUnhandledException", args.Exception);
                args.Handled = true;
                MessageBox.Show($"An unexpected error occurred: {args.Exception.Message}\n\nCheck local logs in {LocalPathProvider.LogsDirectory}", "Application Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            };

            AppDomain.CurrentDomain.UnhandledException += (s, args) =>
            {
                if (args.ExceptionObject is Exception ex)
                {
                    LocalPathProvider.LogError("AppDomainUnhandledException", ex);
                }
            };

            // Ensure robust local directories exist
            LocalPathProvider.EnsureDirectories();

            var services = new ServiceCollection();

            // SQLite Local Database Path with pooling disabled for deterministic lock management
            services.AddDbContext<AppDbContext>(options =>
                options.UseSqlite($"Data Source={LocalPathProvider.DatabasePath};Pooling=false;"));

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
            services.AddScoped<IAccountingService, AccountingService>();

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
            services.AddTransient<AccountingViewModel>();

            ServiceProvider = services.BuildServiceProvider();

            // Ensure SQLite database and tables are created
            using (var scope = ServiceProvider.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                db.Database.EnsureCreated();
            }

            var mainVm = ServiceProvider.GetRequiredService<MainViewModel>();
            var mainWindow = new MainWindow
            {
                DataContext = mainVm
            };
            mainWindow.Show();

            // Check for persistent session on startup
            _ = mainVm.TryRestoreSessionAsync();
        }
    }
}
