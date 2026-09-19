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
    public partial class ProductsViewModel : ViewModelBase
    {
        private readonly IInventoryService _inventoryService;
        private readonly string _businessId;
        private readonly MainViewModel _mainVm;

        [ObservableProperty]
        private ObservableCollection<ProductListItemDto> _products = new();

        [ObservableProperty]
        private ObservableCollection<CategoryListItemDto> _categories = new();

        [ObservableProperty]
        private ProductListItemDto? _selectedProduct;

        [ObservableProperty]
        private string _searchText = string.Empty;

        [ObservableProperty]
        private string? _selectedCategoryId;

        [ObservableProperty]
        private bool _showLowStockOnly = false;

        [ObservableProperty]
        private bool _isBusy = false;

        [ObservableProperty]
        private string _statusMessage = string.Empty;

        [ObservableProperty]
        private string _errorMessage = string.Empty;

        [ObservableProperty]
        private InventoryValuationSummaryDto _valuationSummary = new();

        // Product Editor (Inline/Modal Drawer) Properties
        [ObservableProperty]
        private bool _isEditorOpen = false;

        [ObservableProperty]
        private bool _isEditMode = false;

        [ObservableProperty]
        private string _editorTitle = "Add New Product";

        [ObservableProperty]
        private string _formId = string.Empty;

        [ObservableProperty]
        private string _formName = string.Empty;

        [ObservableProperty]
        private string _formSku = string.Empty;

        [ObservableProperty]
        private string _formBarcode = string.Empty;

        [ObservableProperty]
        private string _formCategoryId = string.Empty;

        [ObservableProperty]
        private string _formUnit = "Pcs";

        [ObservableProperty]
        private decimal _formCostPrice = 0.00m;

        [ObservableProperty]
        private decimal _formSalePrice = 0.00m;

        [ObservableProperty]
        private decimal _formTaxRate = 18.00m;

        [ObservableProperty]
        private decimal _formMinStockAlert = 5.00m;

        [ObservableProperty]
        private decimal _formInitialStock = 0.00m;

        [ObservableProperty]
        private string _editorErrorMessage = string.Empty;

        public List<string> SupportedUnits { get; } = new()
        {
            "Pcs", "Box", "Kg", "Liter", "Meter", "Dozen", "Pack", "Gram", "Carton"
        };

        public ProductsViewModel(IInventoryService inventoryService, string businessId, MainViewModel mainVm)
        {
            _inventoryService = inventoryService;
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
                var cats = await _inventoryService.GetCategoriesAsync(_businessId);
                Categories = new ObservableCollection<CategoryListItemDto>(cats);

                var list = await _inventoryService.GetProductsAsync(
                    _businessId,
                    categoryId: SelectedCategoryId,
                    searchTerm: SearchText,
                    lowStockOnly: ShowLowStockOnly);

                Products = new ObservableCollection<ProductListItemDto>(list);
                ValuationSummary = await _inventoryService.GetStockValuationAsync(_businessId);
                await _mainVm.UpdateSyncStatusAsync();
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Failed to load products: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        public async Task SearchAsync()
        {
            await RefreshAsync();
        }

        [RelayCommand]
        public async Task ClearSearchAsync()
        {
            SearchText = string.Empty;
            SelectedCategoryId = null;
            ShowLowStockOnly = false;
            await RefreshAsync();
        }

        [RelayCommand]
        public void OpenAddProduct()
        {
            IsEditMode = false;
            EditorTitle = "Add New Product";
            FormId = string.Empty;
            FormName = string.Empty;
            FormSku = string.Empty;
            FormBarcode = string.Empty;
            FormCategoryId = Categories.FirstOrDefault()?.Id ?? string.Empty;
            FormUnit = "Pcs";
            FormCostPrice = 0.00m;
            FormSalePrice = 0.00m;
            FormTaxRate = 18.00m;
            FormMinStockAlert = 5.00m;
            FormInitialStock = 0.00m;
            EditorErrorMessage = string.Empty;
            IsEditorOpen = true;
        }

        [RelayCommand]
        public void OpenEditProduct(ProductListItemDto? product)
        {
            var p = product ?? SelectedProduct;
            if (p == null) return;

            IsEditMode = true;
            EditorTitle = $"Edit Product: {p.Name}";
            FormId = p.Id;
            FormName = p.Name;
            FormSku = p.Sku;
            FormBarcode = p.Barcode;
            FormCategoryId = p.CategoryId;
            FormUnit = p.Unit;
            FormCostPrice = p.CostPrice;
            FormSalePrice = p.SalePrice;
            FormTaxRate = p.TaxRate;
            FormMinStockAlert = p.MinStockAlert;
            FormInitialStock = 0.00m;
            EditorErrorMessage = string.Empty;
            IsEditorOpen = true;
        }

        [RelayCommand]
        public void CloseEditor()
        {
            IsEditorOpen = false;
            EditorErrorMessage = string.Empty;
        }

        [RelayCommand]
        public async Task SaveProductAsync()
        {
            EditorErrorMessage = string.Empty;

            if (string.IsNullOrWhiteSpace(FormName))
            {
                EditorErrorMessage = "Product name is required.";
                return;
            }

            if (FormCostPrice < 0 || FormSalePrice < 0)
            {
                EditorErrorMessage = "Prices cannot be negative.";
                return;
            }

            if (FormTaxRate < 0 || FormTaxRate > 100)
            {
                EditorErrorMessage = "Tax rate must be between 0% and 100%.";
                return;
            }

            if (FormMinStockAlert < 0)
            {
                EditorErrorMessage = "Minimum stock alert cannot be negative.";
                return;
            }

            IsBusy = true;
            try
            {
                if (IsEditMode)
                {
                    var updateDto = new UpdateProductDto
                    {
                        Id = FormId,
                        Name = FormName,
                        Sku = FormSku,
                        Barcode = FormBarcode,
                        CategoryId = FormCategoryId,
                        Unit = FormUnit,
                        CostPrice = FormCostPrice,
                        SalePrice = FormSalePrice,
                        TaxRate = FormTaxRate,
                        MinStockAlert = FormMinStockAlert
                    };

                    await _inventoryService.UpdateProductAsync(_businessId, updateDto);
                    StatusMessage = $"Product '{FormName}' updated successfully.";
                }
                else
                {
                    var createDto = new CreateProductDto
                    {
                        Name = FormName,
                        Sku = FormSku,
                        Barcode = FormBarcode,
                        CategoryId = FormCategoryId,
                        Unit = FormUnit,
                        CostPrice = FormCostPrice,
                        SalePrice = FormSalePrice,
                        TaxRate = FormTaxRate,
                        MinStockAlert = FormMinStockAlert,
                        InitialStock = FormInitialStock
                    };

                    await _inventoryService.AddProductAsync(_businessId, createDto);
                    StatusMessage = $"Product '{FormName}' added successfully.";
                }

                IsEditorOpen = false;
                await RefreshAsync();
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
        public async Task DeleteProductAsync(ProductListItemDto? product)
        {
            var p = product ?? SelectedProduct;
            if (p == null) return;

            IsBusy = true;
            ErrorMessage = string.Empty;
            try
            {
                var ok = await _inventoryService.DeleteProductAsync(p.Id, _businessId);
                if (ok)
                {
                    StatusMessage = $"Product '{p.Name}' deleted.";
                    await RefreshAsync();
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Delete error: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        public void NavigateToStockAdjustment(ProductListItemDto? product)
        {
            var p = product ?? SelectedProduct;
            _mainVm.NavigateToStockAdjustments(p?.Id);
        }

        [RelayCommand]
        public void NavigateToStockHistory(ProductListItemDto? product)
        {
            var p = product ?? SelectedProduct;
            _mainVm.NavigateToStockHistory(p?.Id);
        }
    }

    public partial class CategoriesViewModel : ViewModelBase
    {
        private readonly IInventoryService _inventoryService;
        private readonly string _businessId;
        private readonly MainViewModel _mainVm;

        [ObservableProperty]
        private ObservableCollection<CategoryListItemDto> _categories = new();

        [ObservableProperty]
        private CategoryListItemDto? _selectedCategory;

        [ObservableProperty]
        private string _searchText = string.Empty;

        [ObservableProperty]
        private bool _isBusy = false;

        [ObservableProperty]
        private string _statusMessage = string.Empty;

        [ObservableProperty]
        private string _errorMessage = string.Empty;

        // Editor properties
        [ObservableProperty]
        private bool _isEditorOpen = false;

        [ObservableProperty]
        private bool _isEditMode = false;

        [ObservableProperty]
        private string _editorTitle = "Add Category";

        [ObservableProperty]
        private string _formId = string.Empty;

        [ObservableProperty]
        private string _formName = string.Empty;

        [ObservableProperty]
        private string _formDescription = string.Empty;

        [ObservableProperty]
        private string _editorErrorMessage = string.Empty;

        public CategoriesViewModel(IInventoryService inventoryService, string businessId, MainViewModel mainVm)
        {
            _inventoryService = inventoryService;
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
                var list = await _inventoryService.GetCategoriesAsync(_businessId, SearchText);
                Categories = new ObservableCollection<CategoryListItemDto>(list);
                await _mainVm.UpdateSyncStatusAsync();
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Failed to load categories: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        public async Task ClearSearchAsync()
        {
            SearchText = string.Empty;
            await RefreshAsync();
        }

        [RelayCommand]
        public void OpenAddCategory()
        {
            IsEditMode = false;
            EditorTitle = "Add Category";
            FormId = string.Empty;
            FormName = string.Empty;
            FormDescription = string.Empty;
            EditorErrorMessage = string.Empty;
            IsEditorOpen = true;
        }

        [RelayCommand]
        public void OpenEditCategory(CategoryListItemDto? category)
        {
            var c = category ?? SelectedCategory;
            if (c == null) return;

            IsEditMode = true;
            EditorTitle = $"Edit Category: {c.Name}";
            FormId = c.Id;
            FormName = c.Name;
            FormDescription = c.Description;
            EditorErrorMessage = string.Empty;
            IsEditorOpen = true;
        }

        [RelayCommand]
        public void CloseEditor()
        {
            IsEditorOpen = false;
            EditorErrorMessage = string.Empty;
        }

        [RelayCommand]
        public async Task SaveCategoryAsync()
        {
            EditorErrorMessage = string.Empty;

            if (string.IsNullOrWhiteSpace(FormName))
            {
                EditorErrorMessage = "Category name is required.";
                return;
            }

            IsBusy = true;
            try
            {
                if (IsEditMode)
                {
                    await _inventoryService.UpdateCategoryAsync(_businessId, new UpdateCategoryDto
                    {
                        Id = FormId,
                        Name = FormName,
                        Description = FormDescription
                    });
                    StatusMessage = $"Category '{FormName}' updated successfully.";
                }
                else
                {
                    await _inventoryService.AddCategoryAsync(_businessId, new CreateCategoryDto
                    {
                        Name = FormName,
                        Description = FormDescription
                    });
                    StatusMessage = $"Category '{FormName}' added successfully.";
                }

                IsEditorOpen = false;
                await RefreshAsync();
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
        public async Task DeleteCategoryAsync(CategoryListItemDto? category)
        {
            var c = category ?? SelectedCategory;
            if (c == null) return;

            IsBusy = true;
            ErrorMessage = string.Empty;
            try
            {
                var ok = await _inventoryService.DeleteCategoryAsync(c.Id, _businessId);
                if (ok)
                {
                    StatusMessage = $"Category '{c.Name}' deleted.";
                    await RefreshAsync();
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Delete error: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }
    }

    public partial class StockAdjustmentViewModel : ViewModelBase
    {
        private readonly IInventoryService _inventoryService;
        private readonly string _businessId;
        private readonly MainViewModel _mainVm;

        [ObservableProperty]
        private ObservableCollection<ProductListItemDto> _products = new();

        [ObservableProperty]
        private ProductListItemDto? _selectedProduct;

        [ObservableProperty]
        private StockAdjustmentType _selectedAdjustmentType = StockAdjustmentType.ADDITION;

        [ObservableProperty]
        private decimal _adjustmentQuantity = 1.00m;

        [ObservableProperty]
        private decimal _costPerUnit = 0.00m;

        [ObservableProperty]
        private string _reason = string.Empty;

        [ObservableProperty]
        private DateTime _adjustmentDate = DateTime.Today;

        [ObservableProperty]
        private string _barcodeInput = string.Empty;

        [ObservableProperty]
        private decimal _currentStockPreview = 0.00m;

        [ObservableProperty]
        private decimal _newStockPreview = 1.00m;

        [ObservableProperty]
        private bool _isBusy = false;

        [ObservableProperty]
        private string _statusMessage = string.Empty;

        [ObservableProperty]
        private string _errorMessage = string.Empty;

        public List<StockAdjustmentType> AdjustmentTypes { get; } = new()
        {
            StockAdjustmentType.ADDITION,
            StockAdjustmentType.DAMAGE,
            StockAdjustmentType.THEFT,
            StockAdjustmentType.EXPIRED,
            StockAdjustmentType.AUDIT_CORRECTION
        };

        public StockAdjustmentViewModel(
            IInventoryService inventoryService,
            string businessId,
            MainViewModel mainVm,
            string? preselectedProductId = null)
        {
            _inventoryService = inventoryService;
            _businessId = businessId;
            _mainVm = mainVm;

            _ = InitializeAsync(preselectedProductId);
        }

        public async Task InitializeAsync(string? preselectedProductId)
        {
            if (string.IsNullOrEmpty(_businessId)) return;

            IsBusy = true;
            try
            {
                var list = await _inventoryService.GetProductsAsync(_businessId);
                Products = new ObservableCollection<ProductListItemDto>(list);

                if (!string.IsNullOrEmpty(preselectedProductId))
                {
                    SelectedProduct = Products.FirstOrDefault(p => p.Id == preselectedProductId);
                }
                else
                {
                    SelectedProduct = Products.FirstOrDefault();
                }

                UpdatePreview();
            }
            finally
            {
                IsBusy = false;
            }
        }

        partial void OnSelectedProductChanged(ProductListItemDto? value)
        {
            if (value != null)
            {
                CostPerUnit = value.CostPrice;
            }
            UpdatePreview();
        }

        partial void OnSelectedAdjustmentTypeChanged(StockAdjustmentType value)
        {
            UpdatePreview();
        }

        partial void OnAdjustmentQuantityChanged(decimal value)
        {
            UpdatePreview();
        }

        private void UpdatePreview()
        {
            if (SelectedProduct == null)
            {
                CurrentStockPreview = 0.00m;
                NewStockPreview = 0.00m;
                return;
            }

            CurrentStockPreview = SelectedProduct.StockQuantity;
            if (SelectedAdjustmentType == StockAdjustmentType.ADDITION)
            {
                NewStockPreview = CurrentStockPreview + AdjustmentQuantity;
            }
            else
            {
                NewStockPreview = CurrentStockPreview - AdjustmentQuantity;
            }
        }

        [RelayCommand]
        public async Task BarcodeLookupAsync()
        {
            if (string.IsNullOrWhiteSpace(BarcodeInput)) return;

            var match = await _inventoryService.LookupByBarcodeAsync(_businessId, BarcodeInput.Trim());
            if (match != null)
            {
                SelectedProduct = Products.FirstOrDefault(p => p.Id == match.Id);
                StatusMessage = $"Matched product: {match.Name}";
                BarcodeInput = string.Empty;
            }
            else
            {
                ErrorMessage = $"No product found for barcode '{BarcodeInput}'.";
            }
        }

        [RelayCommand]
        public async Task ConfirmAdjustmentAsync()
        {
            ErrorMessage = string.Empty;
            StatusMessage = string.Empty;

            if (SelectedProduct == null)
            {
                ErrorMessage = "Please select a product.";
                return;
            }

            if (AdjustmentQuantity <= 0)
            {
                ErrorMessage = "Adjustment quantity must be greater than zero.";
                return;
            }

            if (SelectedAdjustmentType != StockAdjustmentType.ADDITION && (CurrentStockPreview - AdjustmentQuantity < 0))
            {
                ErrorMessage = $"Cannot reduce stock by {AdjustmentQuantity}. Current stock is {CurrentStockPreview}. Negative inventory is not permitted.";
                return;
            }

            IsBusy = true;
            try
            {
                var req = new StockAdjustmentRequestDto
                {
                    ProductId = SelectedProduct.Id,
                    Type = SelectedAdjustmentType,
                    Quantity = AdjustmentQuantity,
                    CostPerUnit = CostPerUnit,
                    Reason = string.IsNullOrWhiteSpace(Reason) ? $"{SelectedAdjustmentType} adjustment" : Reason.Trim(),
                    Date = AdjustmentDate.ToUniversalTime()
                };

                await _inventoryService.AdjustStockAsync(_businessId, req);
                StatusMessage = $"Stock for '{SelectedProduct.Name}' adjusted successfully! New stock: {NewStockPreview} {SelectedProduct.Unit}";

                // Refresh product data
                await InitializeAsync(SelectedProduct.Id);
                await _mainVm.UpdateSyncStatusAsync();

                // Reset form inputs
                AdjustmentQuantity = 1.00m;
                Reason = string.Empty;
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
        public void ViewHistory()
        {
            _mainVm.NavigateToStockHistory(SelectedProduct?.Id);
        }
    }

    public partial class StockHistoryViewModel : ViewModelBase
    {
        private readonly IInventoryService _inventoryService;
        private readonly string _businessId;
        private readonly MainViewModel _mainVm;

        [ObservableProperty]
        private ObservableCollection<StockMovementHistoryDto> _history = new();

        [ObservableProperty]
        private ObservableCollection<ProductListItemDto> _products = new();

        [ObservableProperty]
        private ProductListItemDto? _filterProduct;

        [ObservableProperty]
        private string _searchText = string.Empty;

        [ObservableProperty]
        private bool _isBusy = false;

        [ObservableProperty]
        private string _errorMessage = string.Empty;

        public StockHistoryViewModel(
            IInventoryService inventoryService,
            string businessId,
            MainViewModel mainVm,
            string? preselectedProductId = null)
        {
            _inventoryService = inventoryService;
            _businessId = businessId;
            _mainVm = mainVm;

            _ = InitializeAsync(preselectedProductId);
        }

        public async Task InitializeAsync(string? preselectedProductId)
        {
            if (string.IsNullOrEmpty(_businessId)) return;

            IsBusy = true;
            try
            {
                var prods = await _inventoryService.GetProductsAsync(_businessId);
                Products = new ObservableCollection<ProductListItemDto>(prods);

                if (!string.IsNullOrEmpty(preselectedProductId))
                {
                    FilterProduct = Products.FirstOrDefault(p => p.Id == preselectedProductId);
                }

                await RefreshAsync();
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        public async Task RefreshAsync()
        {
            if (string.IsNullOrEmpty(_businessId)) return;

            IsBusy = true;
            ErrorMessage = string.Empty;

            try
            {
                var list = await _inventoryService.GetStockHistoryAsync(_businessId, FilterProduct?.Id);

                if (!string.IsNullOrWhiteSpace(SearchText))
                {
                    var term = SearchText.Trim().ToLower();
                    list = list.Where(h =>
                        h.ProductName.ToLower().Contains(term) ||
                        h.Sku.ToLower().Contains(term) ||
                        h.Reason.ToLower().Contains(term) ||
                        h.TypeDisplay.ToLower().Contains(term)).ToList();
                }

                History = new ObservableCollection<StockMovementHistoryDto>(list);
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Failed to load stock history: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        public async Task ClearFilterAsync()
        {
            FilterProduct = null;
            SearchText = string.Empty;
            await RefreshAsync();
        }

        [RelayCommand]
        public void NavigateToStockAdjustment()
        {
            _mainVm.NavigateToStockAdjustments(FilterProduct?.Id);
        }
    }
}
