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
    #region Suppliers ViewModel

    public partial class SuppliersViewModel : ViewModelBase
    {
        private readonly ISupplierService _supplierService;
        private readonly string _businessId;
        private readonly MainViewModel _mainVm;

        [ObservableProperty]
        private ObservableCollection<SupplierListItemDto> _suppliers = new();

        [ObservableProperty]
        private SupplierListItemDto? _selectedSupplier;

        [ObservableProperty]
        private string _searchText = string.Empty;

        [ObservableProperty]
        private bool _isBusy = false;

        [ObservableProperty]
        private string _statusMessage = string.Empty;

        [ObservableProperty]
        private string _errorMessage = string.Empty;

        [ObservableProperty]
        private decimal _totalPayables = 0.00m;

        [ObservableProperty]
        private int _totalSuppliersCount = 0;

        // Editor Drawer Properties
        [ObservableProperty]
        private bool _isEditorOpen = false;

        [ObservableProperty]
        private bool _isEditMode = false;

        [ObservableProperty]
        private string _editorTitle = "Add Supplier";

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
        private decimal _formOpeningBalance = 0.00m;

        [ObservableProperty]
        private string _formNotes = string.Empty;

        [ObservableProperty]
        private string _editorErrorMessage = string.Empty;

        public SuppliersViewModel(ISupplierService supplierService, string businessId, MainViewModel mainVm)
        {
            _supplierService = supplierService;
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
                var list = await _supplierService.GetSuppliersAsync(_businessId, SearchText);
                Suppliers = new ObservableCollection<SupplierListItemDto>(list);
                TotalSuppliersCount = list.Count;
                TotalPayables = list.Sum(s => s.CurrentPayableBalance);
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Failed to load suppliers: {ex.Message}";
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
        public void OpenAddSupplier()
        {
            IsEditMode = false;
            EditorTitle = "Add New Supplier";
            FormId = string.Empty;
            FormName = string.Empty;
            FormCompanyName = string.Empty;
            FormPhone = string.Empty;
            FormEmail = string.Empty;
            FormCnic = string.Empty;
            FormNtn = string.Empty;
            FormAddress = string.Empty;
            FormOpeningBalance = 0.00m;
            FormNotes = string.Empty;
            EditorErrorMessage = string.Empty;
            IsEditorOpen = true;
        }

        [RelayCommand]
        public void OpenEditSupplier(SupplierListItemDto? item)
        {
            var target = item ?? SelectedSupplier;
            if (target == null) return;

            IsEditMode = true;
            EditorTitle = $"Edit Supplier - {target.Name}";
            FormId = target.Id;
            FormName = target.Name;
            FormCompanyName = target.CompanyName;
            FormPhone = target.Phone;
            FormEmail = target.Email;
            FormAddress = target.Address;
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
        public async Task SaveSupplierAsync()
        {
            EditorErrorMessage = string.Empty;

            if (string.IsNullOrWhiteSpace(FormName))
            {
                EditorErrorMessage = "Supplier name is required.";
                return;
            }

            IsBusy = true;
            try
            {
                if (IsEditMode)
                {
                    await _supplierService.UpdateSupplierAsync(_businessId, new UpdateSupplierDto
                    {
                        Id = FormId,
                        Name = FormName,
                        CompanyName = FormCompanyName,
                        Phone = FormPhone,
                        Email = FormEmail,
                        Address = FormAddress,
                        Notes = FormNotes
                    });
                    StatusMessage = $"Supplier '{FormName}' updated successfully.";
                }
                else
                {
                    await _supplierService.AddSupplierAsync(_businessId, new CreateSupplierDto
                    {
                        Name = FormName,
                        CompanyName = FormCompanyName,
                        Phone = FormPhone,
                        Email = FormEmail,
                        Address = FormAddress,
                        OpeningBalance = FormOpeningBalance,
                        Notes = FormNotes
                    });
                    StatusMessage = $"Supplier '{FormName}' registered successfully.";
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
        public async Task DeleteSupplierAsync(SupplierListItemDto? item)
        {
            var target = item ?? SelectedSupplier;
            if (target == null) return;

            ErrorMessage = string.Empty;
            IsBusy = true;

            try
            {
                var success = await _supplierService.DeleteSupplierAsync(target.Id, _businessId);
                if (success)
                {
                    StatusMessage = $"Supplier '{target.Name}' deleted.";
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

    #region Purchases ViewModel

    public partial class PurchasesViewModel : ViewModelBase
    {
        private readonly IPurchaseService _purchaseService;
        private readonly ISupplierService _supplierService;
        private readonly IInventoryService _inventoryService;
        private readonly string _businessId;
        private readonly MainViewModel _mainVm;

        [ObservableProperty]
        private ObservableCollection<PurchaseListItemDto> _purchases = new();

        [ObservableProperty]
        private PurchaseListItemDto? _selectedPurchase;

        [ObservableProperty]
        private string _searchText = string.Empty;

        [ObservableProperty]
        private bool _isBusy = false;

        [ObservableProperty]
        private string _statusMessage = string.Empty;

        [ObservableProperty]
        private string _errorMessage = string.Empty;

        // Purchase Creation
        [ObservableProperty]
        private bool _isCreatorOpen = false;

        [ObservableProperty]
        private ObservableCollection<SupplierListItemDto> _suppliers = new();

        [ObservableProperty]
        private SupplierListItemDto? _selectedSupplierForPurchase;

        [ObservableProperty]
        private ObservableCollection<ProductListItemDto> _products = new();

        [ObservableProperty]
        private ObservableCollection<CreatePurchaseItemDto> _cartItems = new();

        [ObservableProperty]
        private decimal _discount = 0.00m;

        [ObservableProperty]
        private decimal _tax = 0.00m;

        [ObservableProperty]
        private decimal _paidAmount = 0.00m;

        [ObservableProperty]
        private PaymentMethod _paymentMethod = PaymentMethod.CASH;

        [ObservableProperty]
        private string _notes = string.Empty;

        // Computed totals
        public decimal Subtotal => CartItems.Sum(i => i.Quantity * i.UnitCost);
        public decimal GrandTotal => Subtotal - Discount + Tax;
        public decimal DueBalance => GrandTotal - PaidAmount;

        public PurchasesViewModel(
            IPurchaseService purchaseService,
            ISupplierService supplierService,
            IInventoryService inventoryService,
            string businessId,
            MainViewModel mainVm)
        {
            _purchaseService = purchaseService;
            _supplierService = supplierService;
            _inventoryService = inventoryService;
            _businessId = businessId;
            _mainVm = mainVm;

            _ = InitializeAsync();
        }

        private async Task InitializeAsync()
        {
            await RefreshAsync();
            var suppliers = await _supplierService.GetSuppliersAsync(_businessId);
            Suppliers = new ObservableCollection<SupplierListItemDto>(suppliers);
            var products = await _inventoryService.GetProductsAsync(_businessId);
            Products = new ObservableCollection<ProductListItemDto>(products);
        }

        [RelayCommand]
        public async Task RefreshAsync()
        {
            if (string.IsNullOrEmpty(_businessId)) return;

            IsBusy = true;
            try
            {
                var list = await _purchaseService.GetPurchasesHistoryAsync(_businessId, searchTerm: SearchText);
                Purchases = new ObservableCollection<PurchaseListItemDto>(list);
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        public void OpenCreatePurchase()
        {
            CartItems.Clear();
            Discount = 0m;
            Tax = 0m;
            PaidAmount = 0m;
            Notes = string.Empty;
            SelectedSupplierForPurchase = null;
            IsCreatorOpen = true;
        }

        [RelayCommand]
        public void CloseCreator()
        {
            IsCreatorOpen = false;
        }

        [RelayCommand]
        public void AddToCart(ProductListItemDto? product)
        {
            if (product == null) return;
            var existing = CartItems.FirstOrDefault(i => i.ProductId == product.Id);
            if (existing != null)
            {
                existing.Quantity += 1;
            }
            else
            {
                CartItems.Add(new CreatePurchaseItemDto
                {
                    ProductId = product.Id,
                    ProductName = product.Name,
                    Quantity = 1,
                    UnitCost = product.CostPrice
                });
            }
            OnPropertyChanged(nameof(Subtotal));
            OnPropertyChanged(nameof(GrandTotal));
            OnPropertyChanged(nameof(DueBalance));
        }

        [RelayCommand]
        public async Task SavePurchaseAsync()
        {
            if (SelectedSupplierForPurchase == null)
            {
                ErrorMessage = "Please select a supplier.";
                return;
            }
            if (CartItems.Count == 0)
            {
                ErrorMessage = "Cart is empty.";
                return;
            }

            IsBusy = true;
            try
            {
                var dto = new CreatePurchaseDto
                {
                    SupplierId = SelectedSupplierForPurchase.Id,
                    Discount = Discount,
                    Tax = Tax,
                    PaidAmount = PaidAmount,
                    PaymentMethod = PaymentMethod,
                    Notes = Notes,
                    Items = CartItems.ToList()
                };

                await _purchaseService.CreatePurchaseAsync(_businessId, dto);
                IsCreatorOpen = false;
                await RefreshAsync();
                await _mainVm.UpdateSyncStatusAsync();
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
}
