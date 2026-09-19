using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ModernInventory.Core.Application.DTOs;
using ModernInventory.Core.Application.Services;
using ModernInventory.Core.Domain.Entities;
using ModernInventory.Core.Domain.Enums;

namespace ModernInventory.Desktop.Presentation.ViewModels
{
    #region Customers ViewModel

    public partial class CustomersViewModel : ViewModelBase
    {
        private readonly ICustomerService _customerService;
        private readonly string _businessId;
        private readonly MainViewModel _mainVm;

        [ObservableProperty]
        private ObservableCollection<CustomerListItemDto> _customers = new();

        [ObservableProperty]
        private CustomerListItemDto? _selectedCustomer;

        [ObservableProperty]
        private string _searchText = string.Empty;

        [ObservableProperty]
        private bool _isBusy = false;

        [ObservableProperty]
        private string _statusMessage = string.Empty;

        [ObservableProperty]
        private string _errorMessage = string.Empty;

        [ObservableProperty]
        private decimal _totalReceivables = 0.00m;

        [ObservableProperty]
        private int _totalCustomersCount = 0;

        // Editor Drawer Properties
        [ObservableProperty]
        private bool _isEditorOpen = false;

        [ObservableProperty]
        private bool _isEditMode = false;

        [ObservableProperty]
        private string _editorTitle = "Add Customer";

        [ObservableProperty]
        private string _formId = string.Empty;

        [ObservableProperty]
        private string _formName = string.Empty;

        [ObservableProperty]
        private string _formCompanyName = string.Empty;

        [ObservableProperty]
        private string _formPhone = string.Empty;

        [ObservableProperty]
        private string _formEmail = string.Empty;

        [ObservableProperty]
        private string _formCnic = string.Empty;

        [ObservableProperty]
        private string _formNtn = string.Empty;

        [ObservableProperty]
        private string _formAddress = string.Empty;

        [ObservableProperty]
        private decimal _formCreditLimit = 0.00m;

        [ObservableProperty]
        private decimal _formOpeningBalance = 0.00m;

        [ObservableProperty]
        private string _formNotes = string.Empty;

        [ObservableProperty]
        private string _editorErrorMessage = string.Empty;

        // Customer History Modal
        [ObservableProperty]
        private bool _isHistoryModalOpen = false;

        [ObservableProperty]
        private ObservableCollection<CustomerTransactionHistoryItemDto> _customerHistory = new();

        public CustomersViewModel(ICustomerService customerService, string businessId, MainViewModel mainVm)
        {
            _customerService = customerService;
            _businessId = businessId;
            _mainVm = mainVm;

            _ = RefreshAsync();
        }

        [RelayCommand]
        public async Task RefreshAsync()
        {
            if (string.IsNullOrEmpty(_businessId)) return;

            IsBusy = true;
            ErrorMessage = string.Empty;

            try
            {
                var list = await _customerService.GetCustomersAsync(_businessId, SearchText);
                Customers = new ObservableCollection<CustomerListItemDto>(list);
                TotalCustomersCount = list.Count;
                TotalReceivables = list.Where(c => c.CurrentBalance > 0).Sum(c => c.CurrentBalance);
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Failed to load customers: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        public void ClearSearch()
        {
            SearchText = string.Empty;
            _ = RefreshAsync();
        }

        [RelayCommand]
        public void OpenAddCustomer()
        {
            IsEditMode = false;
            EditorTitle = "Add New Customer";
            FormId = string.Empty;
            FormName = string.Empty;
            FormCompanyName = string.Empty;
            FormPhone = string.Empty;
            FormEmail = string.Empty;
            FormCnic = string.Empty;
            FormNtn = string.Empty;
            FormAddress = string.Empty;
            FormCreditLimit = 0.00m;
            FormOpeningBalance = 0.00m;
            FormNotes = string.Empty;
            EditorErrorMessage = string.Empty;
            IsEditorOpen = true;
        }

        [RelayCommand]
        public void OpenEditCustomer(CustomerListItemDto? item)
        {
            var target = item ?? SelectedCustomer;
            if (target == null) return;

            IsEditMode = true;
            EditorTitle = $"Edit Customer - {target.Name}";
            FormId = target.Id;
            FormName = target.Name;
            FormCompanyName = target.CompanyName;
            FormPhone = target.Phone;
            FormEmail = target.Email;
            FormCnic = target.Cnic;
            FormNtn = target.Ntn;
            FormAddress = target.Address;
            FormCreditLimit = target.CreditLimit;
            FormOpeningBalance = target.OpeningBalance;
            FormNotes = target.Notes;
            EditorErrorMessage = string.Empty;
            IsEditorOpen = true;
        }

        [RelayCommand]
        public void CloseDrawer()
        {
            IsEditorOpen = false;
            EditorErrorMessage = string.Empty;
        }

        [RelayCommand]
        public async Task SaveCustomerAsync()
        {
            EditorErrorMessage = string.Empty;

            if (string.IsNullOrWhiteSpace(FormName))
            {
                EditorErrorMessage = "Customer name is required.";
                return;
            }

            IsBusy = true;
            try
            {
                if (IsEditMode)
                {
                    await _customerService.UpdateCustomerAsync(_businessId, new UpdateCustomerDto
                    {
                        Id = FormId,
                        Name = FormName,
                        CompanyName = FormCompanyName,
                        Phone = FormPhone,
                        Email = FormEmail,
                        Cnic = FormCnic,
                        Ntn = FormNtn,
                        Address = FormAddress,
                        CreditLimit = FormCreditLimit,
                        Notes = FormNotes
                    });
                    StatusMessage = $"Customer '{FormName}' updated successfully.";
                }
                else
                {
                    await _customerService.AddCustomerAsync(_businessId, new CreateCustomerDto
                    {
                        Name = FormName,
                        CompanyName = FormCompanyName,
                        Phone = FormPhone,
                        Email = FormEmail,
                        Cnic = FormCnic,
                        Ntn = FormNtn,
                        Address = FormAddress,
                        CreditLimit = FormCreditLimit,
                        OpeningBalance = FormOpeningBalance,
                        Notes = FormNotes
                    });
                    StatusMessage = $"Customer '{FormName}' registered successfully.";
                }

                IsEditorOpen = false;
                await RefreshAsync();
                await _mainVm.UpdateSyncStatusAsync();
            }
            catch (Exception ex)
            {
                EditorErrorMessage = ex.Message;
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        public async Task DeleteCustomerAsync(CustomerListItemDto? item)
        {
            var target = item ?? SelectedCustomer;
            if (target == null) return;

            ErrorMessage = string.Empty;
            IsBusy = true;

            try
            {
                var success = await _customerService.DeleteCustomerAsync(target.Id, _businessId);
                if (success)
                {
                    StatusMessage = $"Customer '{target.Name}' deleted.";
                    await RefreshAsync();
                    await _mainVm.UpdateSyncStatusAsync();
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        public async Task ViewCustomerHistoryAsync(CustomerListItemDto? item)
        {
            var target = item ?? SelectedCustomer;
            if (target == null) return;

            IsBusy = true;
            try
            {
                var history = await _customerService.GetCustomerHistoryAsync(target.Id, _businessId);
                CustomerHistory = new ObservableCollection<CustomerTransactionHistoryItemDto>(history);
                SelectedCustomer = target;
                IsHistoryModalOpen = true;
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Failed to load transaction history: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        public void CloseHistoryModal()
        {
            IsHistoryModalOpen = false;
        }
    }

    #endregion

    #region Employees ViewModel

    public partial class EmployeesViewModel : ViewModelBase
    {
        private readonly IEmployeeService _employeeService;
        private readonly string _businessId;
        private readonly MainViewModel _mainVm;

        [ObservableProperty]
        private ObservableCollection<EmployeeListItemDto> _employees = new();

        [ObservableProperty]
        private EmployeeListItemDto? _selectedEmployee;

        [ObservableProperty]
        private string _searchText = string.Empty;

        [ObservableProperty]
        private bool _showActiveOnly = false;

        [ObservableProperty]
        private bool _isBusy = false;

        [ObservableProperty]
        private string _statusMessage = string.Empty;

        [ObservableProperty]
        private string _errorMessage = string.Empty;

        [ObservableProperty]
        private int _totalEmployeesCount = 0;

        [ObservableProperty]
        private decimal _totalMonthlyPayroll = 0.00m;

        [ObservableProperty]
        private decimal _totalAdvanceBalances = 0.00m;

        // Drawer Properties
        [ObservableProperty]
        private bool _isEditorOpen = false;

        [ObservableProperty]
        private bool _isEditMode = false;

        [ObservableProperty]
        private string _editorTitle = "Add Employee";

        [ObservableProperty]
        private string _formId = string.Empty;

        [ObservableProperty]
        private string _formName = string.Empty;

        [ObservableProperty]
        private string _formDesignation = string.Empty;

        [ObservableProperty]
        private string _formCnic = string.Empty;

        [ObservableProperty]
        private string _formPhone = string.Empty;

        [ObservableProperty]
        private string _formEmail = string.Empty;

        [ObservableProperty]
        private string _formAddress = string.Empty;

        [ObservableProperty]
        private decimal _formMonthlySalary = 0.00m;

        [ObservableProperty]
        private DateTime _formJoiningDate = DateTime.Today;

        [ObservableProperty]
        private decimal _formAdvanceBalance = 0.00m;

        [ObservableProperty]
        private bool _formIsActive = true;

        [ObservableProperty]
        private string _editorErrorMessage = string.Empty;

        public EmployeesViewModel(IEmployeeService employeeService, string businessId, MainViewModel mainVm)
        {
            _employeeService = employeeService;
            _businessId = businessId;
            _mainVm = mainVm;

            _ = RefreshAsync();
        }

        [RelayCommand]
        public async Task RefreshAsync()
        {
            if (string.IsNullOrEmpty(_businessId)) return;

            IsBusy = true;
            ErrorMessage = string.Empty;

            try
            {
                var list = await _employeeService.GetEmployeesAsync(_businessId, SearchText, ShowActiveOnly);
                Employees = new ObservableCollection<EmployeeListItemDto>(list);
                TotalEmployeesCount = list.Count;
                TotalMonthlyPayroll = list.Where(e => e.IsActive).Sum(e => e.MonthlySalary);
                TotalAdvanceBalances = list.Sum(e => e.AdvanceBalance);
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Failed to load employees: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        public void ClearSearch()
        {
            SearchText = string.Empty;
            ShowActiveOnly = false;
            _ = RefreshAsync();
        }

        [RelayCommand]
        public void OpenAddEmployee()
        {
            IsEditMode = false;
            EditorTitle = "Add New Employee";
            FormId = string.Empty;
            FormName = string.Empty;
            FormDesignation = string.Empty;
            FormCnic = string.Empty;
            FormPhone = string.Empty;
            FormEmail = string.Empty;
            FormAddress = string.Empty;
            FormMonthlySalary = 0.00m;
            FormJoiningDate = DateTime.Today;
            FormAdvanceBalance = 0.00m;
            FormIsActive = true;
            EditorErrorMessage = string.Empty;
            IsEditorOpen = true;
        }

        [RelayCommand]
        public void OpenEditEmployee(EmployeeListItemDto? item)
        {
            var target = item ?? SelectedEmployee;
            if (target == null) return;

            IsEditMode = true;
            EditorTitle = $"Edit Employee - {target.Name}";
            FormId = target.Id;
            FormName = target.Name;
            FormDesignation = target.Designation;
            FormCnic = target.Cnic;
            FormPhone = target.Phone;
            FormEmail = target.Email;
            FormAddress = target.Address;
            FormMonthlySalary = target.MonthlySalary;
            FormJoiningDate = target.JoiningDate;
            FormAdvanceBalance = target.AdvanceBalance;
            FormIsActive = target.IsActive;
            EditorErrorMessage = string.Empty;
            IsEditorOpen = true;
        }

        [RelayCommand]
        public void CloseDrawer()
        {
            IsEditorOpen = false;
            EditorErrorMessage = string.Empty;
        }

        [RelayCommand]
        public async Task SaveEmployeeAsync()
        {
            EditorErrorMessage = string.Empty;

            if (string.IsNullOrWhiteSpace(FormName))
            {
                EditorErrorMessage = "Employee name is required.";
                return;
            }

            IsBusy = true;
            try
            {
                if (IsEditMode)
                {
                    await _employeeService.UpdateEmployeeAsync(_businessId, new UpdateEmployeeDto
                    {
                        Id = FormId,
                        Name = FormName,
                        Designation = FormDesignation,
                        Cnic = FormCnic,
                        Phone = FormPhone,
                        Email = FormEmail,
                        Address = FormAddress,
                        MonthlySalary = FormMonthlySalary,
                        JoiningDate = FormJoiningDate,
                        AdvanceBalance = FormAdvanceBalance,
                        IsActive = FormIsActive
                    });
                    StatusMessage = $"Employee '{FormName}' updated successfully.";
                }
                else
                {
                    await _employeeService.AddEmployeeAsync(_businessId, new CreateEmployeeDto
                    {
                        Name = FormName,
                        Designation = FormDesignation,
                        Cnic = FormCnic,
                        Phone = FormPhone,
                        Email = FormEmail,
                        Address = FormAddress,
                        MonthlySalary = FormMonthlySalary,
                        JoiningDate = FormJoiningDate,
                        AdvanceBalance = FormAdvanceBalance,
                        IsActive = FormIsActive
                    });
                    StatusMessage = $"Employee '{FormName}' added successfully.";
                }

                IsEditorOpen = false;
                await RefreshAsync();
                await _mainVm.UpdateSyncStatusAsync();
            }
            catch (Exception ex)
            {
                EditorErrorMessage = ex.Message;
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        public async Task DeleteEmployeeAsync(EmployeeListItemDto? item)
        {
            var target = item ?? SelectedEmployee;
            if (target == null) return;

            ErrorMessage = string.Empty;
            IsBusy = true;

            try
            {
                var success = await _employeeService.DeleteEmployeeAsync(target.Id, _businessId);
                if (success)
                {
                    StatusMessage = $"Employee '{target.Name}' deleted.";
                    await RefreshAsync();
                    await _mainVm.UpdateSyncStatusAsync();
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
            }
            finally
            {
                IsBusy = false;
            }
        }
    }

    #endregion

    #region Point of Sale (POS) ViewModel

    public partial class POSViewModel : ViewModelBase
    {
        private readonly ISaleService _saleService;
        private readonly IInventoryService _inventoryService;
        private readonly ICustomerService _customerService;
        private readonly string _businessId;
        private readonly MainViewModel _mainVm;

        [ObservableProperty]
        private string _scanInput = string.Empty;

        [ObservableProperty]
        private ObservableCollection<ProductListItemDto> _quickProducts = new();

        [ObservableProperty]
        private ObservableCollection<CartItemDto> _cartItems = new();

        [ObservableProperty]
        private ObservableCollection<CustomerListItemDto> _customers = new();

        [ObservableProperty]
        private CustomerListItemDto? _selectedCustomer;

        [ObservableProperty]
        private PaymentMethod _selectedPaymentMethod = PaymentMethod.CASH;

        [ObservableProperty]
        private decimal _orderDiscount = 0.00m;

        [ObservableProperty]
        private decimal _paidAmount = 0.00m;

        [ObservableProperty]
        private string _notes = string.Empty;

        [ObservableProperty]
        private bool _isBusy = false;

        [ObservableProperty]
        private string _statusMessage = string.Empty;

        [ObservableProperty]
        private string _errorMessage = string.Empty;

        // Computed Totals
        public decimal Subtotal => CartItems.Sum(i => i.LineSubtotal);
        public decimal TotalItemDiscount => CartItems.Sum(i => i.Discount);
        public decimal TotalDiscount => TotalItemDiscount + Math.Max(0, OrderDiscount);
        public decimal TotalTax => CartItems.Sum(i => i.TaxAmount);
        public decimal GrandTotal => Math.Max(0, Subtotal - TotalDiscount + TotalTax);
        public decimal ChangeOrDueAmount => SelectedPaymentMethod == PaymentMethod.CREDIT ? GrandTotal : (PaidAmount >= GrandTotal ? PaidAmount - GrandTotal : GrandTotal - PaidAmount);
        public string ChangeOrDueLabel => SelectedPaymentMethod == PaymentMethod.CREDIT ? "Customer Receivable (Due)" : (PaidAmount >= GrandTotal ? "Change Due to Customer" : "Remaining Balance (Due)");

        // Invoice Receipt Modal
        [ObservableProperty]
        private bool _isReceiptOpen = false;

        [ObservableProperty]
        private SaleDetailDto? _completedSale;

        public List<PaymentMethod> AvailablePaymentMethods { get; } = new()
        {
            PaymentMethod.CASH,
            PaymentMethod.BANK,
            PaymentMethod.CREDIT,
            PaymentMethod.SPLIT
        };

        public POSViewModel(
            ISaleService saleService,
            IInventoryService inventoryService,
            ICustomerService customerService,
            string businessId,
            MainViewModel mainVm)
        {
            _saleService = saleService;
            _inventoryService = inventoryService;
            _customerService = customerService;
            _businessId = businessId;
            _mainVm = mainVm;

            _ = InitializeAsync();
        }

        private async Task InitializeAsync()
        {
            if (string.IsNullOrEmpty(_businessId)) return;

            IsBusy = true;
            try
            {
                var products = await _inventoryService.GetProductsAsync(_businessId);
                QuickProducts = new ObservableCollection<ProductListItemDto>(products);

                var customersList = await _customerService.GetCustomersAsync(_businessId);
                Customers = new ObservableCollection<CustomerListItemDto>(customersList);
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        public async Task HandleBarcodeOrSearchAsync()
        {
            if (string.IsNullOrWhiteSpace(ScanInput)) return;

            var query = ScanInput.Trim();
            ScanInput = string.Empty;

            // 1. Try Barcode lookup first
            var product = await _inventoryService.LookupByBarcodeAsync(_businessId, query);
            if (product == null)
            {
                // 2. Try SKU or Name exact match
                product = QuickProducts.FirstOrDefault(p =>
                    p.Sku.Equals(query, StringComparison.OrdinalIgnoreCase) ||
                    p.Name.Equals(query, StringComparison.OrdinalIgnoreCase));
            }

            if (product == null)
            {
                // 3. Fallback to first search result
                var results = await _inventoryService.SearchProductsAsync(_businessId, query);
                product = results.FirstOrDefault();
            }

            if (product != null)
            {
                AddToCart(product);
            }
            else
            {
                ErrorMessage = $"No product found for barcode/query '{query}'.";
            }
        }

        [RelayCommand]
        public void AddToCart(ProductListItemDto? product)
        {
            if (product == null) return;
            ErrorMessage = string.Empty;

            if (product.StockQuantity <= 0)
            {
                ErrorMessage = $"Cannot add '{product.Name}'. Product is out of stock.";
                return;
            }

            var existing = CartItems.FirstOrDefault(i => i.ProductId == product.Id);
            if (existing != null)
            {
                if (existing.Quantity + 1 > product.StockQuantity)
                {
                    ErrorMessage = $"Cannot add more. Available stock for '{product.Name}' is {product.StockQuantity}.";
                    return;
                }
                existing.Quantity += 1;
            }
            else
            {
                CartItems.Add(new CartItemDto
                {
                    ProductId = product.Id,
                    ProductName = product.Name,
                    Sku = product.Sku,
                    Barcode = product.Barcode,
                    Unit = product.Unit,
                    AvailableStock = product.StockQuantity,
                    UnitCost = product.CostPrice,
                    UnitPrice = product.SalePrice,
                    Quantity = 1m,
                    Discount = 0m,
                    TaxRate = product.TaxRate
                });
            }

            NotifyTotalsChanged();
            AutoSetPaidAmount();
        }

        [RelayCommand]
        public void IncreaseQuantity(CartItemDto? item)
        {
            if (item == null) return;
            ErrorMessage = string.Empty;

            if (item.Quantity + 1 > item.AvailableStock)
            {
                ErrorMessage = $"Cannot add more. Available stock for '{item.ProductName}' is {item.AvailableStock}.";
                return;
            }

            item.Quantity += 1;
            NotifyTotalsChanged();
            AutoSetPaidAmount();
        }

        [RelayCommand]
        public void DecreaseQuantity(CartItemDto? item)
        {
            if (item == null) return;
            ErrorMessage = string.Empty;

            if (item.Quantity > 1)
            {
                item.Quantity -= 1;
            }
            else
            {
                CartItems.Remove(item);
            }

            NotifyTotalsChanged();
            AutoSetPaidAmount();
        }

        [RelayCommand]
        public void RemoveFromCart(CartItemDto? item)
        {
            if (item == null) return;
            CartItems.Remove(item);
            NotifyTotalsChanged();
            AutoSetPaidAmount();
        }

        [RelayCommand]
        public void ClearCart()
        {
            CartItems.Clear();
            OrderDiscount = 0m;
            PaidAmount = 0m;
            Notes = string.Empty;
            ErrorMessage = string.Empty;
            StatusMessage = string.Empty;
            NotifyTotalsChanged();
        }

        partial void OnOrderDiscountChanged(decimal value)
        {
            NotifyTotalsChanged();
            AutoSetPaidAmount();
        }

        partial void OnPaidAmountChanged(decimal value)
        {
            OnPropertyChanged(nameof(ChangeOrDueAmount));
            OnPropertyChanged(nameof(ChangeOrDueLabel));
        }

        partial void OnSelectedPaymentMethodChanged(PaymentMethod value)
        {
            AutoSetPaidAmount();
            OnPropertyChanged(nameof(ChangeOrDueAmount));
            OnPropertyChanged(nameof(ChangeOrDueLabel));
        }

        private void AutoSetPaidAmount()
        {
            if (SelectedPaymentMethod == PaymentMethod.CASH || SelectedPaymentMethod == PaymentMethod.BANK)
            {
                PaidAmount = GrandTotal;
            }
            else if (SelectedPaymentMethod == PaymentMethod.CREDIT)
            {
                PaidAmount = 0m;
            }
        }

        private void NotifyTotalsChanged()
        {
            OnPropertyChanged(nameof(Subtotal));
            OnPropertyChanged(nameof(TotalItemDiscount));
            OnPropertyChanged(nameof(TotalDiscount));
            OnPropertyChanged(nameof(TotalTax));
            OnPropertyChanged(nameof(GrandTotal));
            OnPropertyChanged(nameof(ChangeOrDueAmount));
            OnPropertyChanged(nameof(ChangeOrDueLabel));
        }

        [RelayCommand]
        public async Task CheckoutAsync()
        {
            ErrorMessage = string.Empty;

            if (CartItems.Count == 0)
            {
                ErrorMessage = "Cart is empty. Add products before checking out.";
                return;
            }

            if (SelectedPaymentMethod == PaymentMethod.CREDIT && SelectedCustomer == null)
            {
                ErrorMessage = "Please select a registered customer for Credit / Khata sale.";
                return;
            }

            IsBusy = true;
            try
            {
                var saleDto = new CreateSaleDto
                {
                    CustomerId = SelectedCustomer?.Id,
                    CustomerName = SelectedCustomer?.Name ?? "Walk-in Customer",
                    SaleDate = DateTime.UtcNow,
                    PaymentMethod = SelectedPaymentMethod,
                    OrderDiscount = OrderDiscount,
                    PaidAmount = SelectedPaymentMethod == PaymentMethod.CREDIT ? 0m : PaidAmount,
                    Notes = Notes,
                    Items = CartItems.Select(i => new CreateSaleItemDto
                    {
                        ProductId = i.ProductId,
                        ProductName = i.ProductName,
                        Unit = i.Unit,
                        Quantity = i.Quantity,
                        UnitPrice = i.UnitPrice,
                        Discount = i.Discount,
                        TaxRate = i.TaxRate
                    }).ToList()
                };

                var completedSale = await _saleService.ProcessSaleAsync(_businessId, saleDto);

                CompletedSale = completedSale;
                IsReceiptOpen = true;
                StatusMessage = $"Invoice #{completedSale.InvoiceNumber} processed successfully!";

                // Reset Cart and refresh products & customers
                ClearCart();
                await InitializeAsync();
                await _mainVm.UpdateSyncStatusAsync();
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Checkout failed: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        public void CloseReceipt()
        {
            IsReceiptOpen = false;
            CompletedSale = null;
        }
    }

    #endregion

    #region Sales History ViewModel

    public partial class SalesViewModel : ViewModelBase
    {
        private readonly ISaleService _saleService;
        private readonly string _businessId;
        private readonly MainViewModel _mainVm;

        [ObservableProperty]
        private ObservableCollection<SaleListItemDto> _sales = new();

        [ObservableProperty]
        private SaleListItemDto? _selectedSale;

        [ObservableProperty]
        private string _searchText = string.Empty;

        [ObservableProperty]
        private string _filterPeriod = "All"; // "All", "Today", "This Month"

        [ObservableProperty]
        private SalesSummaryDto _summary = new();

        [ObservableProperty]
        private bool _isBusy = false;

        [ObservableProperty]
        private string _statusMessage = string.Empty;

        [ObservableProperty]
        private string _errorMessage = string.Empty;

        // Invoice Details Modal
        [ObservableProperty]
        private bool _isDetailsOpen = false;

        [ObservableProperty]
        private SaleDetailDto? _invoiceDetails;

        public SalesViewModel(ISaleService saleService, string businessId, MainViewModel mainVm)
        {
            _saleService = saleService;
            _businessId = businessId;
            _mainVm = mainVm;

            _ = RefreshAsync();
        }

        [RelayCommand]
        public async Task RefreshAsync()
        {
            if (string.IsNullOrEmpty(_businessId)) return;

            IsBusy = true;
            ErrorMessage = string.Empty;

            try
            {
                DateTime? fromDate = null;
                if (FilterPeriod == "Today")
                {
                    fromDate = DateTime.UtcNow.Date;
                }
                else if (FilterPeriod == "This Month")
                {
                    var now = DateTime.UtcNow;
                    fromDate = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
                }

                var list = await _saleService.GetSalesHistoryAsync(_businessId, fromDate: fromDate, searchTerm: SearchText);
                Sales = new ObservableCollection<SaleListItemDto>(list);

                Summary = await _saleService.GetSalesSummaryAsync(_businessId);
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Failed to load sales history: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        public void ClearFilter()
        {
            SearchText = string.Empty;
            FilterPeriod = "All";
            _ = RefreshAsync();
        }

        [RelayCommand]
        public async Task ViewDetailsAsync(SaleListItemDto? item)
        {
            var target = item ?? SelectedSale;
            if (target == null) return;

            IsBusy = true;
            try
            {
                var details = await _saleService.GetSaleByIdAsync(target.Id, _businessId);
                if (details != null)
                {
                    InvoiceDetails = details;
                    IsDetailsOpen = true;
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Failed to load invoice details: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        public void CloseDetails()
        {
            IsDetailsOpen = false;
            InvoiceDetails = null;
        }
    }

    #endregion
}
