using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ModernInventory.Core.Application.DTOs;
using ModernInventory.Core.Domain.Entities;
using ModernInventory.Core.Domain.Enums;
using ModernInventory.Core.Infrastructure.Database;

namespace ModernInventory.Core.Application.Services
{
    public interface IAccountingService
    {
        Task SeedDefaultChartOfAccountsAsync(string businessId);
        Task<List<AccountHeadDto>> GetAccountHeadsAsync(string businessId);
        Task<AccountHead> AddAccountHeadAsync(string businessId, CreateAccountHeadDto dto);
        
        Task<List<VoucherDto>> GetVouchersAsync(string businessId, VoucherType? type = null, DateTime? startDate = null, DateTime? endDate = null);
        Task<Voucher> CreateVoucherAsync(string businessId, CreateVoucherDto dto);
        
        Task<List<ExpenseDto>> GetExpensesAsync(string businessId, DateTime? startDate = null, DateTime? endDate = null);
        Task<Expense> AddExpenseAsync(string businessId, CreateExpenseDto dto);
        Task<bool> DeleteExpenseAsync(string id, string businessId);

        Task<List<BankAccountDto>> GetBankAccountsAsync(string businessId);
        Task<BankAccount> AddBankAccountAsync(string businessId, CreateBankAccountDto dto);

        Task<List<LedgerItemDto>> GetGeneralLedgerAsync(string businessId, string? accountHeadId = null, DateTime? startDate = null, DateTime? endDate = null);
        Task<List<LedgerItemDto>> GetCashBookAsync(string businessId, DateTime? startDate = null, DateTime? endDate = null);
        Task<List<LedgerItemDto>> GetBankSummaryAsync(string businessId, DateTime? startDate = null, DateTime? endDate = null);
        Task<List<ClientPositionDto>> GetClientPositionAsync(string businessId);
        Task<List<MonthlyPositionDto>> GetMonthlyPositionsAsync(string businessId);
        Task<List<MonthlyPnLDto>> GetMonthlyPnLAsync(string businessId);
    }

    public class AccountingService : IAccountingService
    {
        private readonly AppDbContext _context;

        public AccountingService(AppDbContext context)
        {
            _context = context;
        }

        public async Task SeedDefaultChartOfAccountsAsync(string businessId)
        {
            var exists = await _context.AccountHeads.AnyAsync(a => a.BusinessId == businessId && !a.IsDeleted);
            if (exists) return;

            var defaults = new List<AccountHead>
            {
                new() { BusinessId = businessId, Code = "1010", Name = "Cash in Hand", Type = AccountType.ASSET, IsSystem = true },
                new() { BusinessId = businessId, Code = "1020", Name = "Bank Account", Type = AccountType.ASSET, IsSystem = true },
                new() { BusinessId = businessId, Code = "1030", Name = "Accounts Receivable (Customers)", Type = AccountType.ASSET, IsSystem = true },
                new() { BusinessId = businessId, Code = "1040", Name = "Inventory Stock", Type = AccountType.ASSET, IsSystem = true },
                new() { BusinessId = businessId, Code = "2010", Name = "Accounts Payable (Suppliers)", Type = AccountType.LIABILITY, IsSystem = true },
                new() { BusinessId = businessId, Code = "3010", Name = "Owner's Capital", Type = AccountType.EQUITY, IsSystem = true },
                new() { BusinessId = businessId, Code = "4010", Name = "Sales Revenue", Type = AccountType.REVENUE, IsSystem = true },
                new() { BusinessId = businessId, Code = "5010", Name = "Cost of Goods Sold (COGS)", Type = AccountType.EXPENSE, IsSystem = true },
                new() { BusinessId = businessId, Code = "5020", Name = "General & Admin Expenses", Type = AccountType.EXPENSE, IsSystem = true }
            };

            _context.AccountHeads.AddRange(defaults);
            await _context.SaveChangesAsync();
        }

        public async Task<List<AccountHeadDto>> GetAccountHeadsAsync(string businessId)
        {
            await SeedDefaultChartOfAccountsAsync(businessId);
            var heads = await _context.AccountHeads
                .Where(a => a.BusinessId == businessId && !a.IsDeleted)
                .OrderBy(a => a.Code)
                .ToListAsync();

            return heads.Select(h => new AccountHeadDto
            {
                Id = h.Id,
                Code = h.Code,
                Name = h.Name,
                Type = h.Type,
                IsSystem = h.IsSystem,
                Description = h.Description,
                CreatedAt = h.CreatedAt
            }).ToList();
        }

        public async Task<AccountHead> AddAccountHeadAsync(string businessId, CreateAccountHeadDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Name) || string.IsNullOrWhiteSpace(dto.Code))
            {
                throw new ArgumentException("Account code and name are required.");
            }

            var head = new AccountHead
            {
                BusinessId = businessId,
                Code = dto.Code.Trim(),
                Name = dto.Name.Trim(),
                Type = dto.Type,
                Description = dto.Description?.Trim() ?? string.Empty,
                IsSystem = false
            };

            _context.AccountHeads.Add(head);
            await _context.SaveChangesAsync();
            return head;
        }

        public async Task<List<VoucherDto>> GetVouchersAsync(string businessId, VoucherType? type = null, DateTime? startDate = null, DateTime? endDate = null)
        {
            var query = _context.Vouchers
                .Include(v => v.Entries)
                .Where(v => v.BusinessId == businessId && !v.IsDeleted);

            if (type.HasValue) query = query.Where(v => v.VoucherType == type.Value);
            if (startDate.HasValue) query = query.Where(v => v.Date >= startDate.Value.Date);
            if (endDate.HasValue) query = query.Where(v => v.Date <= endDate.Value.Date.AddDays(1).AddSeconds(-1));

            var vouchers = await query.OrderByDescending(v => v.Date).ThenByDescending(v => v.CreatedAt).ToListAsync();
            var accountHeads = await _context.AccountHeads.Where(a => a.BusinessId == businessId).ToDictionaryAsync(a => a.Id, a => a.Name);

            return vouchers.Select(v => new VoucherDto
            {
                Id = v.Id,
                VoucherNumber = v.VoucherNumber,
                VoucherType = v.VoucherType,
                Date = v.Date,
                TotalAmount = v.TotalAmount,
                ReferenceType = v.ReferenceType,
                ReferenceId = v.ReferenceId,
                Narration = v.Narration,
                CreatedAt = v.CreatedAt,
                Entries = v.Entries.Select(e => new VoucherEntryDto
                {
                    Id = e.Id,
                    AccountHeadId = e.AccountHeadId,
                    AccountHeadName = accountHeads.TryGetValue(e.AccountHeadId, out var name) ? name : "Unknown Account",
                    DebitAmount = e.DebitAmount,
                    CreditAmount = e.CreditAmount,
                    PartyType = e.PartyType,
                    PartyId = e.PartyId,
                    Notes = e.Notes
                }).ToList()
            }).ToList();
        }

        public async Task<Voucher> CreateVoucherAsync(string businessId, CreateVoucherDto dto)
        {
            if (dto.Entries == null || !dto.Entries.Any())
            {
                throw new ArgumentException("Voucher must have at least one entry.");
            }

            var totalDebit = dto.Entries.Sum(e => e.DebitAmount);
            var totalCredit = dto.Entries.Sum(e => e.CreditAmount);

            if (Math.Abs(totalDebit - totalCredit) > 0.01m)
            {
                throw new InvalidOperationException($"Voucher is unbalanced. Total Debits ({totalDebit:N2}) must equal Total Credits ({totalCredit:N2}).");
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var count = await _context.Vouchers.CountAsync(v => v.BusinessId == businessId) + 1;
                var prefix = dto.VoucherType.ToString();
                var voucherNumber = $"{prefix}-{DateTime.UtcNow:yyyyMMdd}-{count:D4}";

                var voucher = new Voucher
                {
                    BusinessId = businessId,
                    VoucherNumber = voucherNumber,
                    VoucherType = dto.VoucherType,
                    Date = dto.Date,
                    TotalAmount = totalDebit,
                    ReferenceType = dto.ReferenceType ?? string.Empty,
                    ReferenceId = dto.ReferenceId,
                    Narration = dto.Narration ?? string.Empty,
                    Entries = dto.Entries.Select(e => new VoucherEntry
                    {
                        BusinessId = businessId,
                        AccountHeadId = e.AccountHeadId,
                        DebitAmount = e.DebitAmount,
                        CreditAmount = e.CreditAmount,
                        PartyType = e.PartyType,
                        PartyId = e.PartyId,
                        Notes = e.Notes ?? string.Empty
                    }).ToList()
                };

                _context.Vouchers.Add(voucher);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                return voucher;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<List<ExpenseDto>> GetExpensesAsync(string businessId, DateTime? startDate = null, DateTime? endDate = null)
        {
            var query = _context.Expenses.Where(e => e.BusinessId == businessId && !e.IsDeleted);
            if (startDate.HasValue) query = query.Where(e => e.ExpenseDate >= startDate.Value.Date);
            if (endDate.HasValue) query = query.Where(e => e.ExpenseDate <= endDate.Value.Date.AddDays(1).AddSeconds(-1));

            var list = await query.OrderByDescending(e => e.ExpenseDate).ToListAsync();
            return list.Select(e => new ExpenseDto
            {
                Id = e.Id,
                Category = e.Category,
                Title = e.Title,
                Amount = e.Amount,
                ExpenseDate = e.ExpenseDate,
                PaymentMethod = e.PaymentMethod,
                AccountHeadId = e.AccountHeadId,
                Notes = e.Notes,
                CreatedAt = e.CreatedAt
            }).ToList();
        }

        public async Task<Expense> AddExpenseAsync(string businessId, CreateExpenseDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Title) || dto.Amount <= 0)
            {
                throw new ArgumentException("Expense title and valid amount are required.");
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var expense = new Expense
                {
                    BusinessId = businessId,
                    Category = dto.Category?.Trim() ?? "General",
                    Title = dto.Title.Trim(),
                    Amount = dto.Amount,
                    ExpenseDate = dto.ExpenseDate,
                    PaymentMethod = dto.PaymentMethod,
                    AccountHeadId = dto.AccountHeadId,
                    Notes = dto.Notes?.Trim() ?? string.Empty
                };

                _context.Expenses.Add(expense);

                // Automatically post Journal Voucher / Expense entry
                var expenseAccount = await _context.AccountHeads.FirstOrDefaultAsync(a => a.BusinessId == businessId && a.Type == AccountType.EXPENSE);
                var cashAccount = await _context.AccountHeads.FirstOrDefaultAsync(a => a.BusinessId == businessId && a.Code == "1010");

                if (expenseAccount != null && cashAccount != null)
                {
                    var voucher = new Voucher
                    {
                        BusinessId = businessId,
                        VoucherNumber = $"EXP-{DateTime.UtcNow:yyyyMMddHHmmss}",
                        VoucherType = VoucherType.CPV,
                        Date = dto.ExpenseDate,
                        TotalAmount = dto.Amount,
                        ReferenceType = "Expense",
                        ReferenceId = expense.Id,
                        Narration = $"Expense: {expense.Title} ({expense.Category})",
                        Entries = new List<VoucherEntry>
                        {
                            new() { BusinessId = businessId, AccountHeadId = expenseAccount.Id, DebitAmount = dto.Amount, CreditAmount = 0 },
                            new() { BusinessId = businessId, AccountHeadId = cashAccount.Id, DebitAmount = 0, CreditAmount = dto.Amount }
                        }
                    };
                    _context.Vouchers.Add(voucher);
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                return expense;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<bool> DeleteExpenseAsync(string id, string businessId)
        {
            var exp = await _context.Expenses.FirstOrDefaultAsync(e => e.Id == id && e.BusinessId == businessId);
            if (exp == null) return false;
            exp.IsDeleted = true;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<List<BankAccountDto>> GetBankAccountsAsync(string businessId)
        {
            var banks = await _context.BankAccounts.Where(b => b.BusinessId == businessId && !b.IsDeleted).ToListAsync();
            return banks.Select(b => new BankAccountDto
            {
                Id = b.Id,
                BankName = b.BankName,
                AccountTitle = b.AccountTitle,
                AccountNumber = b.AccountNumber,
                BranchCode = b.BranchCode,
                Iban = b.Iban,
                OpeningBalance = b.OpeningBalance,
                CurrentBalance = b.CurrentBalance,
                IsActive = b.IsActive
            }).ToList();
        }

        public async Task<BankAccount> AddBankAccountAsync(string businessId, CreateBankAccountDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.BankName) || string.IsNullOrWhiteSpace(dto.AccountNumber))
            {
                throw new ArgumentException("Bank name and account number are required.");
            }

            var bank = new BankAccount
            {
                BusinessId = businessId,
                BankName = dto.BankName.Trim(),
                AccountTitle = dto.AccountTitle?.Trim() ?? string.Empty,
                AccountNumber = dto.AccountNumber.Trim(),
                BranchCode = dto.BranchCode?.Trim() ?? string.Empty,
                Iban = dto.Iban?.Trim() ?? string.Empty,
                OpeningBalance = dto.OpeningBalance,
                CurrentBalance = dto.OpeningBalance,
                IsActive = true
            };

            _context.BankAccounts.Add(bank);
            await _context.SaveChangesAsync();
            return bank;
        }

        public async Task<List<LedgerItemDto>> GetGeneralLedgerAsync(string businessId, string? accountHeadId = null, DateTime? startDate = null, DateTime? endDate = null)
        {
            var query = _context.VoucherEntries
                .Include(e => e.Voucher)
                .Where(e => e.BusinessId == e.Voucher.BusinessId && !e.Voucher.IsDeleted);

            if (!string.IsNullOrEmpty(accountHeadId))
            {
                query = query.Where(e => e.AccountHeadId == accountHeadId);
            }
            if (startDate.HasValue)
            {
                query = query.Where(e => e.Voucher.Date >= startDate.Value.Date);
            }
            if (endDate.HasValue)
            {
                query = query.Where(e => e.Voucher.Date <= endDate.Value.Date.AddDays(1).AddSeconds(-1));
            }

            var entries = await query.OrderBy(e => e.Voucher.Date).ThenBy(e => e.Voucher.CreatedAt).ToListAsync();
            var accountHeads = await _context.AccountHeads.Where(a => a.BusinessId == businessId).ToDictionaryAsync(a => a.Id, a => a.Name);

            decimal runningBal = 0.00m;
            var result = new List<LedgerItemDto>();

            foreach (var en in entries)
            {
                runningBal += (en.DebitAmount - en.CreditAmount);
                result.Add(new LedgerItemDto
                {
                    Id = en.Id,
                    Date = en.Voucher.Date,
                    VoucherNumber = en.Voucher.VoucherNumber,
                    AccountName = accountHeads.TryGetValue(en.AccountHeadId, out var name) ? name : "Account",
                    Description = !string.IsNullOrEmpty(en.Notes) ? en.Notes : en.Voucher.Narration,
                    Debit = en.DebitAmount,
                    Credit = en.CreditAmount,
                    RunningBalance = runningBal
                });
            }

            return result;
        }

        public async Task<List<LedgerItemDto>> GetCashBookAsync(string businessId, DateTime? startDate = null, DateTime? endDate = null)
        {
            var cashHead = await _context.AccountHeads.FirstOrDefaultAsync(a => a.BusinessId == businessId && a.Code == "1010");
            if (cashHead == null) return new List<LedgerItemDto>();
            return await GetGeneralLedgerAsync(businessId, cashHead.Id, startDate, endDate);
        }

        public async Task<List<LedgerItemDto>> GetBankSummaryAsync(string businessId, DateTime? startDate = null, DateTime? endDate = null)
        {
            var bankHead = await _context.AccountHeads.FirstOrDefaultAsync(a => a.BusinessId == businessId && a.Code == "1020");
            if (bankHead == null) return new List<LedgerItemDto>();
            return await GetGeneralLedgerAsync(businessId, bankHead.Id, startDate, endDate);
        }

        public async Task<List<ClientPositionDto>> GetClientPositionAsync(string businessId)
        {
            var customers = await _context.Customers.Where(c => c.BusinessId == businessId && !c.IsDeleted).ToListAsync();
            var result = new List<ClientPositionDto>();

            foreach (var c in customers)
            {
                var sales = await _context.Sales.Where(s => s.BusinessId == businessId && s.CustomerId == c.Id && !s.IsDeleted).ToListAsync();
                var totalSales = sales.Sum(s => s.GrandTotal);
                var totalPaid = sales.Sum(s => s.PaidAmount);

                result.Add(new ClientPositionDto
                {
                    CustomerId = c.Id,
                    CustomerName = c.Name,
                    CompanyName = c.CompanyName,
                    Phone = c.Phone,
                    OpeningBalance = c.OpeningBalance,
                    TotalSales = totalSales,
                    TotalPaid = totalPaid,
                    OutstandingBalance = c.CurrentBalance
                });
            }

            return result;
        }

        public async Task<List<MonthlyPositionDto>> GetMonthlyPositionsAsync(string businessId)
        {
            var sales = await _context.Sales.Where(s => s.BusinessId == businessId && !s.IsDeleted).ToListAsync();
            var purchases = await _context.Purchases.Where(p => p.BusinessId == businessId && !p.IsDeleted).ToListAsync();
            var expenses = await _context.Expenses.Where(e => e.BusinessId == businessId && !e.IsDeleted).ToListAsync();

            var months = Enumerable.Range(0, 6).Select(i => DateTime.UtcNow.AddMonths(-i)).OrderBy(d => d).ToList();
            var result = new List<MonthlyPositionDto>();

            foreach (var m in months)
            {
                var monthSales = sales.Where(s => s.SaleDate.Month == m.Month && s.SaleDate.Year == m.Year).Sum(s => s.GrandTotal);
                var monthPurchases = purchases.Where(p => p.PurchaseDate.Month == m.Month && p.PurchaseDate.Year == m.Year).Sum(p => p.GrandTotal);
                var monthExpenses = expenses.Where(e => e.ExpenseDate.Month == m.Month && e.ExpenseDate.Year == m.Year).Sum(e => e.Amount);

                result.Add(new MonthlyPositionDto
                {
                    MonthName = m.ToString("MMMM yyyy"),
                    SalesRevenue = monthSales,
                    PurchasesCost = monthPurchases,
                    ExpensesAmount = monthExpenses,
                    NetCashFlow = monthSales - monthPurchases - monthExpenses
                });
            }

            return result;
        }

        public async Task<List<MonthlyPnLDto>> GetMonthlyPnLAsync(string businessId)
        {
            var sales = await _context.Sales
                .Include(s => s.Items)
                .Where(s => s.BusinessId == businessId && !s.IsDeleted)
                .ToListAsync();

            var expenses = await _context.Expenses
                .Where(e => e.BusinessId == businessId && !e.IsDeleted)
                .ToListAsync();

            var months = Enumerable.Range(0, 6).Select(i => DateTime.UtcNow.AddMonths(-i)).OrderBy(d => d).ToList();
            var result = new List<MonthlyPnLDto>();

            foreach (var m in months)
            {
                var mSales = sales.Where(s => s.SaleDate.Month == m.Month && s.SaleDate.Year == m.Year).ToList();
                var rev = mSales.Sum(s => s.GrandTotal);
                var cogs = mSales.SelectMany(s => s.Items).Sum(item => item.Quantity * item.UnitCost);
                var grossProfit = rev - cogs;
                var opExp = expenses.Where(e => e.ExpenseDate.Month == m.Month && e.ExpenseDate.Year == m.Year).Sum(e => e.Amount);
                var netProfit = grossProfit - opExp;
                var margin = rev > 0 ? (netProfit / rev) * 100 : 0;

                result.Add(new MonthlyPnLDto
                {
                    MonthName = m.ToString("MMMM yyyy"),
                    GrossRevenue = rev,
                    CostOfGoodsSold = cogs,
                    GrossProfit = grossProfit,
                    OperatingExpenses = opExp,
                    NetProfitLoss = netProfit,
                    ProfitMarginPercent = Math.Round(margin, 2)
                });
            }

            return result;
        }
    }
}
