using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ModernInventory.Core.Application.DTOs;
using ModernInventory.Core.Application.Services;
using ModernInventory.Core.Domain.Entities;
using ModernInventory.Desktop.Infrastructure;

namespace ModernInventory.Desktop.Presentation.ViewModels
{
    public class ViewModelBase : ObservableObject
    {
    }

    public partial class MainViewModel : ViewModelBase
    {
        private readonly IAuthService _authService;
        private readonly IDashboardService _dashboardService;
        private readonly IBackupService _backupService;
        private readonly ISyncQueueService _syncQueueService;
        private readonly IInventoryService _inventoryService;
        private readonly ICustomerService _customerService;
        private readonly IEmployeeService _employeeService;
        private readonly ISaleService _saleService;
        private readonly ISupplierService _supplierService;
        private readonly IPurchaseService _purchaseService;
        private readonly IAccountingService _accountingService;

        [ObservableProperty]
        private object? _currentView;

        [ObservableProperty]
        private bool _isAuthenticated = false;

        [ObservableProperty]
        private string _userEmail = string.Empty;

        [ObservableProperty]
        private string _userFullName = string.Empty;

        [ObservableProperty]
        private string _businessName = "Modern Inventory System";

        [ObservableProperty]
        private string _businessId = string.Empty;

        [ObservableProperty]
        private string _userToken = string.Empty;

        [ObservableProperty]
        private int _pendingSyncCount = 0;

        [ObservableProperty]
        private string _activeSection = "Dashboard";

        public MainViewModel(
            IAuthService authService,
            IDashboardService dashboardService,
            IBackupService backupService,
            ISyncQueueService syncQueueService,
            IInventoryService inventoryService,
            ICustomerService customerService,
            IEmployeeService employeeService,
            ISaleService saleService,
            ISupplierService supplierService,
            IPurchaseService purchaseService,
            IAccountingService accountingService)
        {
            _authService = authService;
            _dashboardService = dashboardService;
            _backupService = backupService;
            _syncQueueService = syncQueueService;
            _inventoryService = inventoryService;
            _customerService = customerService;
            _employeeService = employeeService;
            _saleService = saleService;
            _supplierService = supplierService;
            _purchaseService = purchaseService;
            _accountingService = accountingService;

            // Default to Login View if not authenticated
            NavigateToLogin();
        }

        public void SetAuthenticatedUser(AuthResultDto auth)
        {
            IsAuthenticated = true;
            UserEmail = auth.Email ?? string.Empty;
            UserFullName = auth.FullName ?? string.Empty;
            BusinessName = auth.BusinessName ?? "Modern Inventory System";
            BusinessId = auth.BusinessId ?? string.Empty;
            UserToken = auth.Token ?? string.Empty;

            _ = LocalSessionManager.SaveSessionAsync(UserToken, UserEmail);

            NavigateToDashboard();
            _ = UpdateSyncStatusAsync();
        }

        public async Task TryRestoreSessionAsync()
        {
            try
            {
                var savedToken = await LocalSessionManager.GetSavedTokenAsync();
                if (!string.IsNullOrWhiteSpace(savedToken))
                {
                    var result = await _authService.ValidateSessionAsync(savedToken);
                    if (result.Success)
                    {
                        SetAuthenticatedUser(result);
                        return;
                    }
                    else
                    {
                        await LocalSessionManager.ClearSessionAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                LocalPathProvider.LogError("TryRestoreSession", ex);
            }

            NavigateToLogin();
        }

        [RelayCommand]
        public void NavigateToLogin()
        {
            CurrentView = new LoginViewModel(this, _authService);
        }

        [RelayCommand]
        public void NavigateToRegister()
        {
            CurrentView = new RegisterViewModel(this, _authService);
        }

        [RelayCommand]
        public void NavigateToForgotPassword()
        {
            CurrentView = new ForgotPasswordViewModel(this, _authService);
        }

        [RelayCommand]
        public void NavigateToDashboard()
        {
            ActiveSection = "Dashboard";
            CurrentView = new DashboardViewModel(_dashboardService, BusinessId);
        }

        [RelayCommand]
        public void NavigateToProducts()
        {
            ActiveSection = "Products";
            CurrentView = new ProductsViewModel(_inventoryService, BusinessId, this);
        }

        [RelayCommand]
        public void NavigateToCategories()
        {
            ActiveSection = "Categories";
            CurrentView = new CategoriesViewModel(_inventoryService, BusinessId, this);
        }

        [RelayCommand]
        public void NavigateToStockAdjustments(string? productId = null)
        {
            ActiveSection = "StockAdjustments";
            CurrentView = new StockAdjustmentViewModel(_inventoryService, BusinessId, this, productId);
        }

        [RelayCommand]
        public void NavigateToStockHistory(string? productId = null)
        {
            ActiveSection = "StockHistory";
            CurrentView = new StockHistoryViewModel(_inventoryService, BusinessId, this, productId);
        }

        [RelayCommand]
        public void NavigateToBackupSync()
        {
            ActiveSection = "BackupSync";
            CurrentView = new BackupSyncViewModel(_backupService, _syncQueueService, BusinessId, this);
        }

        [RelayCommand]
        public void NavigateToCustomers()
        {
            ActiveSection = "Customers";
            CurrentView = new CustomersViewModel(_customerService, BusinessId, this);
        }

        [RelayCommand]
        public void NavigateToSuppliers()
        {
            ActiveSection = "Suppliers";
            CurrentView = new SuppliersViewModel(_supplierService, BusinessId, this);
        }

        [RelayCommand]
        public void NavigateToPurchases()
        {
            ActiveSection = "Purchases";
            CurrentView = new PurchasesViewModel(_purchaseService, _supplierService, _inventoryService, BusinessId, this);
        }

        [RelayCommand]
        public void NavigateToEmployees()
        {
            ActiveSection = "Employees";
            CurrentView = new EmployeesViewModel(_employeeService, BusinessId, this);
        }

        [RelayCommand]
        public void NavigateToPOS()
        {
            ActiveSection = "POS";
            CurrentView = new POSViewModel(_saleService, _inventoryService, _customerService, BusinessId, this);
        }

        [RelayCommand]
        public void NavigateToSales()
        {
            ActiveSection = "Sales";
            CurrentView = new SalesViewModel(_saleService, BusinessId, this);
        }

        [RelayCommand]
        public void NavigateToAccounting()
        {
            ActiveSection = "Accounting & Vouchers";
            var vm = App.ServiceProvider.GetRequiredService<AccountingViewModel>();
            _ = vm.InitializeAsync(BusinessId);
            CurrentView = vm;
        }

        [RelayCommand]
        public void NavigateToReports()
        {
            ActiveSection = "Reports & P&L";
            var vm = App.ServiceProvider.GetRequiredService<AccountingViewModel>();
            _ = vm.InitializeAsync(BusinessId);
            CurrentView = vm;
        }

        [RelayCommand]
        public async Task LogoutAsync()
        {
            if (!string.IsNullOrEmpty(UserToken))
            {
                await _authService.LogoutAsync(UserToken);
            }

            await LocalSessionManager.ClearSessionAsync();

            IsAuthenticated = false;
            UserEmail = string.Empty;
            UserFullName = string.Empty;
            BusinessName = "Modern Inventory System";
            BusinessId = string.Empty;
            UserToken = string.Empty;

            NavigateToLogin();
        }

        public async Task UpdateSyncStatusAsync()
        {
            if (!string.IsNullOrEmpty(BusinessId))
            {
                PendingSyncCount = await _syncQueueService.GetPendingCountAsync(BusinessId);
            }
        }
    }

    public partial class LoginViewModel : ViewModelBase
    {
        private readonly MainViewModel _mainVm;
        private readonly IAuthService _authService;

        [ObservableProperty]
        private string _email = string.Empty;

        [ObservableProperty]
        private string _password = string.Empty;

        [ObservableProperty]
        private bool _isPasswordVisible = false;

        [ObservableProperty]
        private bool _rememberMe = true;

        [ObservableProperty]
        private bool _isLoading = false;

        [ObservableProperty]
        private string _errorMessage = string.Empty;

        public LoginViewModel(MainViewModel mainVm, IAuthService authService)
        {
            _mainVm = mainVm;
            _authService = authService;
        }

        [RelayCommand]
        public void TogglePasswordVisibility()
        {
            IsPasswordVisible = !IsPasswordVisible;
        }

        [RelayCommand]
        public async Task LoginAsync()
        {
            ErrorMessage = string.Empty;

            var trimmedEmail = Email?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(trimmedEmail) || string.IsNullOrWhiteSpace(Password))
            {
                ErrorMessage = "Email and password are required.";
                return;
            }

            if (!Regex.IsMatch(trimmedEmail, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
            {
                ErrorMessage = "Please enter a valid email address.";
                return;
            }

            IsLoading = true;

            try
            {
                var result = await _authService.LoginAsync(trimmedEmail, Password, RememberMe);
                if (result.Success)
                {
                    await LocalSessionManager.SaveSessionAsync(result.Token!, result.Email ?? trimmedEmail);
                    _mainVm.SetAuthenticatedUser(result);
                }
                else
                {
                    ErrorMessage = result.ErrorMessage ?? "Invalid email or password.";
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Login error: {ex.Message}";
                LocalPathProvider.LogError("Login", ex);
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        public void GoToRegister() => _mainVm.NavigateToRegister();

        [RelayCommand]
        public void GoToForgotPassword() => _mainVm.NavigateToForgotPassword();
    }

    public partial class RegisterViewModel : ViewModelBase
    {
        private readonly MainViewModel _mainVm;
        private readonly IAuthService _authService;

        [ObservableProperty]
        private string _businessName = string.Empty;

        [ObservableProperty]
        private string _ownerName = string.Empty;

        [ObservableProperty]
        private string _email = string.Empty;

        [ObservableProperty]
        private string _phone = string.Empty;

        [ObservableProperty]
        private string _password = string.Empty;

        [ObservableProperty]
        private bool _isPasswordVisible = false;

        [ObservableProperty]
        private string _confirmPassword = string.Empty;

        [ObservableProperty]
        private bool _isConfirmPasswordVisible = false;

        [ObservableProperty]
        private string _securityQuestion = "What is your primary phone?";

        [ObservableProperty]
        private string _securityAnswer = string.Empty;

        [ObservableProperty]
        private bool _isLoading = false;

        [ObservableProperty]
        private string _errorMessage = string.Empty;

        public RegisterViewModel(MainViewModel mainVm, IAuthService authService)
        {
            _mainVm = mainVm;
            _authService = authService;
        }

        [RelayCommand]
        public void TogglePasswordVisibility()
        {
            IsPasswordVisible = !IsPasswordVisible;
        }

        [RelayCommand]
        public void ToggleConfirmPasswordVisibility()
        {
            IsConfirmPasswordVisible = !IsConfirmPasswordVisible;
        }

        [RelayCommand]
        public async Task RegisterAsync()
        {
            ErrorMessage = string.Empty;

            var trimmedBizName = BusinessName?.Trim() ?? string.Empty;
            var trimmedOwnerName = OwnerName?.Trim() ?? string.Empty;
            var trimmedEmail = Email?.Trim() ?? string.Empty;
            var trimmedPhone = Phone?.Trim() ?? string.Empty;
            var trimmedAnswer = SecurityAnswer?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(trimmedBizName) || trimmedBizName.Length < 2)
            {
                ErrorMessage = "Business Name is required (at least 2 characters).";
                return;
            }

            if (string.IsNullOrWhiteSpace(trimmedOwnerName) || trimmedOwnerName.Length < 2)
            {
                ErrorMessage = "Owner Full Name is required.";
                return;
            }

            if (string.IsNullOrWhiteSpace(trimmedEmail) || !Regex.IsMatch(trimmedEmail, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
            {
                ErrorMessage = "A valid business email address is required.";
                return;
            }

            if (string.IsNullOrWhiteSpace(trimmedPhone) || trimmedPhone.Length < 7)
            {
                ErrorMessage = "A valid contact phone number is required.";
                return;
            }

            if (string.IsNullOrWhiteSpace(Password) || Password.Length < 6)
            {
                ErrorMessage = "Password must be at least 6 characters long.";
                return;
            }

            if (Password != ConfirmPassword)
            {
                ErrorMessage = "Passwords do not match.";
                return;
            }

            if (string.IsNullOrWhiteSpace(trimmedAnswer) || trimmedAnswer.Length < 2)
            {
                ErrorMessage = "Security recovery answer is required.";
                return;
            }

            IsLoading = true;
            try
            {
                var req = new RegisterRequestDto
                {
                    BusinessName = trimmedBizName,
                    OwnerName = trimmedOwnerName,
                    Email = trimmedEmail,
                    Phone = trimmedPhone,
                    Password = Password,
                    SecurityQuestion = SecurityQuestion,
                    SecurityAnswer = trimmedAnswer,
                    Currency = "PKR"
                };

                var result = await _authService.RegisterAsync(req);
                if (result.Success)
                {
                    await LocalSessionManager.SaveSessionAsync(result.Token!, result.Email ?? trimmedEmail);
                    _mainVm.SetAuthenticatedUser(result);
                }
                else
                {
                    ErrorMessage = result.ErrorMessage ?? "Registration failed.";
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Registration error: {ex.Message}";
                LocalPathProvider.LogError("Register", ex);
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        public void GoToLogin() => _mainVm.NavigateToLogin();
    }

    public partial class ForgotPasswordViewModel : ViewModelBase
    {
        private readonly MainViewModel _mainVm;
        private readonly IAuthService _authService;

        [ObservableProperty]
        private string _email = string.Empty;

        [ObservableProperty]
        private string _securityAnswer = string.Empty;

        [ObservableProperty]
        private string _newPassword = string.Empty;

        [ObservableProperty]
        private string _statusMessage = string.Empty;

        [ObservableProperty]
        private bool _isSuccess = false;

        public ForgotPasswordViewModel(MainViewModel mainVm, IAuthService authService)
        {
            _mainVm = mainVm;
            _authService = authService;
        }

        [RelayCommand]
        public async Task ResetPasswordAsync()
        {
            StatusMessage = string.Empty;
            IsSuccess = false;

            var ok = await _authService.ResetPasswordAsync(Email, SecurityAnswer, NewPassword);
            if (ok)
            {
                IsSuccess = true;
                StatusMessage = "Password reset successfully! You can now log in.";
            }
            else
            {
                StatusMessage = "Invalid email or security answer.";
            }
        }

        [RelayCommand]
        public void GoToLogin() => _mainVm.NavigateToLogin();
    }

    public partial class DashboardViewModel : ViewModelBase
    {
        private readonly IDashboardService _dashboardService;
        private readonly string _businessId;

        [ObservableProperty]
        private DashboardSummaryDto _metrics = new();

        [ObservableProperty]
        private bool _isLoading = false;

        public DashboardViewModel(IDashboardService dashboardService, string businessId)
        {
            _dashboardService = dashboardService;
            _businessId = businessId;
            _ = LoadMetricsAsync();
        }

        [RelayCommand]
        public async Task LoadMetricsAsync()
        {
            if (string.IsNullOrEmpty(_businessId)) return;

            IsLoading = true;
            try
            {
                Metrics = await _dashboardService.GetDashboardMetricsAsync(_businessId);
            }
            finally
            {
                IsLoading = false;
            }
        }
    }

    public partial class BackupSyncViewModel : ViewModelBase
    {
        private readonly IBackupService _backupService;
        private readonly ISyncQueueService _syncQueueService;
        private readonly string _businessId;
        private readonly MainViewModel _mainVm;

        [ObservableProperty]
        private ObservableCollection<LocalBackupRecord> _backupHistory = new();

        [ObservableProperty]
        private int _pendingQueueCount = 0;

        [ObservableProperty]
        private string _statusMessage = string.Empty;

        [ObservableProperty]
        private bool _isBusy = false;

        [ObservableProperty]
        private LocalBackupRecord? _selectedBackup;

        public BackupSyncViewModel(
            IBackupService backupService,
            ISyncQueueService syncQueueService,
            string businessId,
            MainViewModel mainVm)
        {
            _backupService = backupService;
            _syncQueueService = syncQueueService;
            _businessId = businessId;
            _mainVm = mainVm;

            _ = RefreshAsync();
        }

        [RelayCommand]
        public async Task RefreshAsync()
        {
            if (string.IsNullOrEmpty(_businessId)) return;

            var list = await _backupService.GetBackupHistoryAsync(_businessId);
            BackupHistory = new ObservableCollection<LocalBackupRecord>(list);
            PendingQueueCount = await _syncQueueService.GetPendingCountAsync(_businessId);
            await _mainVm.UpdateSyncStatusAsync();
        }

        [RelayCommand]
        public async Task CreateBackupAsync()
        {
            IsBusy = true;
            StatusMessage = "Creating SQLite backup...";

            try
            {
                var appDataFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ModernInventoryDesktop");
                var dbPath = Path.Combine(appDataFolder, "modern_inventory.db");
                var backupDir = Path.Combine(appDataFolder, "Backups");

                var record = await _backupService.CreateBackupAsync(_businessId, backupDir, dbPath);
                StatusMessage = $"Backup created successfully: {record.FileName} ({record.FileSizeBytes / 1024} KB)";
                await RefreshAsync();
            }
            catch (Exception ex)
            {
                StatusMessage = $"Backup error: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        public async Task RestoreBackupAsync(LocalBackupRecord? record)
        {
            var target = record ?? SelectedBackup;
            if (target == null)
            {
                StatusMessage = "Please select a backup record to restore.";
                return;
            }

            IsBusy = true;
            StatusMessage = $"Validating and restoring backup: {target.FileName}...";

            try
            {
                var appDataFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ModernInventoryDesktop");
                var dbPath = Path.Combine(appDataFolder, "modern_inventory.db");

                var success = await _backupService.RestoreBackupAsync(_businessId, target.FilePath, dbPath);
                if (success)
                {
                    StatusMessage = $"Database successfully restored from {target.FileName}. A pre-restore safety snapshot was created.";
                    await RefreshAsync();
                }
                else
                {
                    StatusMessage = "Restore failed. Integrity verification check did not pass.";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Restore error: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }
    }
}
