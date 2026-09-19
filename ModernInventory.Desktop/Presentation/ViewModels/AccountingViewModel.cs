using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ModernInventory.Core.Application.DTOs;
using ModernInventory.Core.Application.Services;
using ModernInventory.Core.Domain.Enums;
using ModernInventory.Core.Infrastructure.Database;

namespace ModernInventory.Desktop.Presentation.ViewModels
{
    public partial class AccountingViewModel : ObservableObject
    {
        private readonly IAccountingService _accountingService;
        private readonly AppDbContext _context;
        private string _businessId = string.Empty;

        [ObservableProperty]
        private ObservableCollection<AccountHeadDto> _accountHeads = new();

        [ObservableProperty]
        private ObservableCollection<VoucherDto> _vouchers = new();

        [ObservableProperty]
        private ObservableCollection<ExpenseDto> _expenses = new();

        [ObservableProperty]
        private ObservableCollection<LedgerItemDto> _generalLedger = new();

        [ObservableProperty]
        private ObservableCollection<ClientPositionDto> _clientPositions = new();

        [ObservableProperty]
        private ObservableCollection<MonthlyPnLDto> _monthlyPnLList = new();

        [ObservableProperty]
        private string _selectedTab = "Ledger"; // Ledger, Vouchers, Expenses, ChartOfAccounts, PnL

        // New Expense Form Properties
        [ObservableProperty]
        private string _newExpenseTitle = string.Empty;

        [ObservableProperty]
        private string _newExpenseCategory = "General";

        [ObservableProperty]
        private decimal _newExpenseAmount = 0.00m;

        [ObservableProperty]
        private DateTime _newExpenseDate = DateTime.Today;

        [ObservableProperty]
        private PaymentMethod _newExpensePaymentMethod = PaymentMethod.CASH;

        [ObservableProperty]
        private string _newExpenseNotes = string.Empty;

        public AccountingViewModel(IAccountingService accountingService, AppDbContext context)
        {
            _accountingService = accountingService;
            _context = context;
        }

        public async Task InitializeAsync(string businessId)
        {
            _businessId = businessId;
            await LoadDataAsync();
        }

        [RelayCommand]
        public async Task LoadDataAsync()
        {
            if (string.IsNullOrEmpty(_businessId)) return;

            try
            {
                var heads = await _accountingService.GetAccountHeadsAsync(_businessId);
                AccountHeads = new ObservableCollection<AccountHeadDto>(heads);

                var vouchers = await _accountingService.GetVouchersAsync(_businessId);
                Vouchers = new ObservableCollection<VoucherDto>(vouchers);

                var expenses = await _accountingService.GetExpensesAsync(_businessId);
                Expenses = new ObservableCollection<ExpenseDto>(expenses);

                var ledger = await _accountingService.GetGeneralLedgerAsync(_businessId);
                GeneralLedger = new ObservableCollection<LedgerItemDto>(ledger);

                var positions = await _accountingService.GetClientPositionAsync(_businessId);
                ClientPositions = new ObservableCollection<ClientPositionDto>(positions);

                var pnl = await _accountingService.GetMonthlyPnLAsync(_businessId);
                MonthlyPnLList = new ObservableCollection<MonthlyPnLDto>(pnl);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading accounting data: {ex.Message}", "Accounting", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        [RelayCommand]
        public async Task SaveExpenseAsync()
        {
            if (string.IsNullOrWhiteSpace(NewExpenseTitle) || NewExpenseAmount <= 0)
            {
                MessageBox.Show("Please enter a valid expense title and amount.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var dto = new CreateExpenseDto
                {
                    Title = NewExpenseTitle,
                    Category = NewExpenseCategory,
                    Amount = NewExpenseAmount,
                    ExpenseDate = NewExpenseDate,
                    PaymentMethod = NewExpensePaymentMethod,
                    Notes = NewExpenseNotes
                };

                await _accountingService.AddExpenseAsync(_businessId, dto);

                NewExpenseTitle = string.Empty;
                NewExpenseAmount = 0.00m;
                NewExpenseNotes = string.Empty;

                await LoadDataAsync();
                MessageBox.Show("Expense recorded successfully and posted to accounting ledger.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving expense: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
