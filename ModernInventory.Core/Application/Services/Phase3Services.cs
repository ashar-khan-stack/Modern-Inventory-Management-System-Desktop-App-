using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ModernInventory.Core.Application.DTOs;
using ModernInventory.Core.Application.Repositories;
using ModernInventory.Core.Domain.Entities;
using ModernInventory.Core.Domain.Enums;
using ModernInventory.Core.Infrastructure.Database;

namespace ModernInventory.Core.Application.Services
{
    #region Customer Service

    public interface ICustomerService
    {
        Task<List<CustomerListItemDto>> GetCustomersAsync(string businessId, string? searchTerm = null);
        Task<Customer?> GetCustomerByIdAsync(string id, string businessId);
        Task<Customer> AddCustomerAsync(string businessId, CreateCustomerDto dto);
        Task<Customer> UpdateCustomerAsync(string businessId, UpdateCustomerDto dto);
        Task<bool> DeleteCustomerAsync(string id, string businessId);
        Task<List<CustomerTransactionHistoryItemDto>> GetCustomerHistoryAsync(string customerId, string businessId);
    }

    public class CustomerService : ICustomerService
    {
        private readonly AppDbContext _context;
        private readonly ICustomerRepository _customerRepo;

        public CustomerService(AppDbContext context, ICustomerRepository customerRepo)
        {
            _context = context;
            _customerRepo = customerRepo;
        }

        public async Task<List<CustomerListItemDto>> GetCustomersAsync(string businessId, string? searchTerm = null)
        {
            var customers = await _customerRepo.SearchAsync(businessId, searchTerm ?? string.Empty);

            var result = new List<CustomerListItemDto>();
            foreach (var c in customers)
            {
                var invoiceCount = await _customerRepo.GetInvoicesCountAsync(c.Id, businessId);
                var totalPurchases = await _customerRepo.GetTotalPurchasesAmountAsync(c.Id, businessId);

                result.Add(new CustomerListItemDto
                {
                    Id = c.Id,
                    Name = c.Name,
                    CompanyName = c.CompanyName,
                    Phone = c.Phone,
                    Email = c.Email,
                    Cnic = c.Cnic,
                    Ntn = c.Ntn,
                    Address = c.Address,
                    CreditLimit = c.CreditLimit,
                    OpeningBalance = c.OpeningBalance,
                    CurrentBalance = c.CurrentBalance,
                    Notes = c.Notes,
                    InvoicesCount = invoiceCount,
                    TotalPurchasedAmount = totalPurchases,
                    CreatedAt = c.CreatedAt,
                    UpdatedAt = c.UpdatedAt
                });
            }

            return result;
        }

        public async Task<Customer?> GetCustomerByIdAsync(string id, string businessId)
        {
            return await _customerRepo.GetByIdAsync(id, businessId);
        }

        public async Task<Customer> AddCustomerAsync(string businessId, CreateCustomerDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Name))
            {
                throw new ArgumentException("Customer name is required.", nameof(dto.Name));
            }

            var cleanName = dto.Name.Trim();
            var cleanPhone = dto.Phone?.Trim() ?? string.Empty;

            if (!string.IsNullOrEmpty(cleanPhone))
            {
                var isPhoneUnique = await _customerRepo.IsPhoneUniqueAsync(cleanPhone, businessId);
                if (!isPhoneUnique)
                {
                    throw new InvalidOperationException($"A customer with phone '{cleanPhone}' already exists.");
                }
            }

            var customer = new Customer
            {
                BusinessId = businessId,
                Name = cleanName,
                CompanyName = dto.CompanyName?.Trim() ?? string.Empty,
                Phone = cleanPhone,
                Email = dto.Email?.Trim() ?? string.Empty,
                Cnic = dto.Cnic?.Trim() ?? string.Empty,
                Ntn = dto.Ntn?.Trim() ?? string.Empty,
                Address = dto.Address?.Trim() ?? string.Empty,
                CreditLimit = Math.Max(0, dto.CreditLimit),
                OpeningBalance = dto.OpeningBalance,
                CurrentBalance = dto.OpeningBalance, // Starting balance equals opening balance
                Notes = dto.Notes?.Trim() ?? string.Empty,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                IsDeleted = false,
                SyncStatus = SyncStatus.PendingCreate
            };

            await _customerRepo.AddAsync(customer);
            return customer;
        }

        public async Task<Customer> UpdateCustomerAsync(string businessId, UpdateCustomerDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Name))
            {
                throw new ArgumentException("Customer name is required.", nameof(dto.Name));
            }

            var customer = await _customerRepo.GetByIdAsync(dto.Id, businessId);
            if (customer == null)
            {
                throw new KeyNotFoundException($"Customer with ID '{dto.Id}' was not found.");
            }

            var cleanPhone = dto.Phone?.Trim() ?? string.Empty;
            if (!string.IsNullOrEmpty(cleanPhone))
            {
                var isPhoneUnique = await _customerRepo.IsPhoneUniqueAsync(cleanPhone, businessId, excludeId: dto.Id);
                if (!isPhoneUnique)
                {
                    throw new InvalidOperationException($"Another customer with phone '{cleanPhone}' already exists.");
                }
            }

            customer.Name = dto.Name.Trim();
            customer.CompanyName = dto.CompanyName?.Trim() ?? string.Empty;
            customer.Phone = cleanPhone;
            customer.Email = dto.Email?.Trim() ?? string.Empty;
            customer.Cnic = dto.Cnic?.Trim() ?? string.Empty;
            customer.Ntn = dto.Ntn?.Trim() ?? string.Empty;
            customer.Address = dto.Address?.Trim() ?? string.Empty;
            customer.CreditLimit = Math.Max(0, dto.CreditLimit);
            customer.Notes = dto.Notes?.Trim() ?? string.Empty;
            customer.UpdatedAt = DateTime.UtcNow;

            await _customerRepo.UpdateAsync(customer);
            return customer;
        }

        public async Task<bool> DeleteCustomerAsync(string id, string businessId)
        {
            var customer = await _customerRepo.GetByIdAsync(id, businessId);
            if (customer == null) return false;

            if (customer.CurrentBalance != 0)
            {
                throw new InvalidOperationException($"Cannot delete customer with active outstanding balance of PKR {customer.CurrentBalance:N2}. Clear balance first.");
            }

            await _customerRepo.SoftDeleteAsync(id, businessId);
            return true;
        }

        public async Task<List<CustomerTransactionHistoryItemDto>> GetCustomerHistoryAsync(string customerId, string businessId)
        {
            var sales = await _context.Sales
                .Where(s => s.BusinessId == businessId && s.CustomerId == customerId && !s.IsDeleted)
                .OrderByDescending(s => s.SaleDate)
                .ToListAsync();

            var history = sales.Select(s => new CustomerTransactionHistoryItemDto
            {
                Id = s.Id,
                Date = s.SaleDate,
                TransactionType = "Sale Invoice",
                ReferenceNumber = s.InvoiceNumber,
                Description = $"Sale Invoice #{s.InvoiceNumber} - {s.PaymentMethod}",
                Amount = s.GrandTotal,
                Paid = s.PaidAmount,
                DueBalance = s.DueBalance,
                PaymentMethod = s.PaymentMethod.ToString()
            }).ToList();

            return history;
        }
    }

    #endregion

    #region Employee Service

    public interface IEmployeeService
    {
        Task<List<EmployeeListItemDto>> GetEmployeesAsync(string businessId, string? searchTerm = null, bool activeOnly = false);
        Task<Employee?> GetEmployeeByIdAsync(string id, string businessId);
        Task<Employee> AddEmployeeAsync(string businessId, CreateEmployeeDto dto);
        Task<Employee> UpdateEmployeeAsync(string businessId, UpdateEmployeeDto dto);
        Task<bool> DeleteEmployeeAsync(string id, string businessId);
    }

    public class EmployeeService : IEmployeeService
    {
        private readonly AppDbContext _context;
        private readonly IEmployeeRepository _employeeRepo;

        public EmployeeService(AppDbContext context, IEmployeeRepository employeeRepo)
        {
            _context = context;
            _employeeRepo = employeeRepo;
        }

        public async Task<List<EmployeeListItemDto>> GetEmployeesAsync(string businessId, string? searchTerm = null, bool activeOnly = false)
        {
            var employees = await _employeeRepo.SearchAsync(businessId, searchTerm ?? string.Empty);
            if (activeOnly)
            {
                employees = employees.Where(e => e.IsActive).ToList();
            }

            return employees.Select(e => new EmployeeListItemDto
            {
                Id = e.Id,
                Name = e.Name,
                Designation = e.Designation,
                Cnic = e.Cnic,
                Phone = e.Phone,
                Email = e.Email,
                Address = e.Address,
                MonthlySalary = e.MonthlySalary,
                JoiningDate = e.JoiningDate,
                AdvanceBalance = e.AdvanceBalance,
                IsActive = e.IsActive,
                CreatedAt = e.CreatedAt,
                UpdatedAt = e.UpdatedAt
            }).ToList();
        }

        public async Task<Employee?> GetEmployeeByIdAsync(string id, string businessId)
        {
            return await _employeeRepo.GetByIdAsync(id, businessId);
        }

        public async Task<Employee> AddEmployeeAsync(string businessId, CreateEmployeeDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Name))
            {
                throw new ArgumentException("Employee name is required.", nameof(dto.Name));
            }

            var cleanCnic = dto.Cnic?.Trim() ?? string.Empty;
            if (!string.IsNullOrEmpty(cleanCnic))
            {
                var isCnicUnique = await _employeeRepo.IsCnicUniqueAsync(cleanCnic, businessId);
                if (!isCnicUnique)
                {
                    throw new InvalidOperationException($"An employee with CNIC '{cleanCnic}' already exists.");
                }
            }

            var employee = new Employee
            {
                BusinessId = businessId,
                Name = dto.Name.Trim(),
                Designation = dto.Designation?.Trim() ?? string.Empty,
                Cnic = cleanCnic,
                Phone = dto.Phone?.Trim() ?? string.Empty,
                Email = dto.Email?.Trim() ?? string.Empty,
                Address = dto.Address?.Trim() ?? string.Empty,
                MonthlySalary = Math.Max(0, dto.MonthlySalary),
                JoiningDate = dto.JoiningDate,
                AdvanceBalance = dto.AdvanceBalance,
                IsActive = dto.IsActive,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                IsDeleted = false,
                SyncStatus = SyncStatus.PendingCreate
            };

            await _employeeRepo.AddAsync(employee);
            return employee;
        }

        public async Task<Employee> UpdateEmployeeAsync(string businessId, UpdateEmployeeDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Name))
            {
                throw new ArgumentException("Employee name is required.", nameof(dto.Name));
            }

            var employee = await _employeeRepo.GetByIdAsync(dto.Id, businessId);
            if (employee == null)
            {
                throw new KeyNotFoundException($"Employee with ID '{dto.Id}' was not found.");
            }

            var cleanCnic = dto.Cnic?.Trim() ?? string.Empty;
            if (!string.IsNullOrEmpty(cleanCnic))
            {
                var isCnicUnique = await _employeeRepo.IsCnicUniqueAsync(cleanCnic, businessId, excludeId: dto.Id);
                if (!isCnicUnique)
                {
                    throw new InvalidOperationException($"Another employee with CNIC '{cleanCnic}' already exists.");
                }
            }

            employee.Name = dto.Name.Trim();
            employee.Designation = dto.Designation?.Trim() ?? string.Empty;
            employee.Cnic = cleanCnic;
            employee.Phone = dto.Phone?.Trim() ?? string.Empty;
            employee.Email = dto.Email?.Trim() ?? string.Empty;
            employee.Address = dto.Address?.Trim() ?? string.Empty;
            employee.MonthlySalary = Math.Max(0, dto.MonthlySalary);
            employee.JoiningDate = dto.JoiningDate;
            employee.AdvanceBalance = dto.AdvanceBalance;
            employee.IsActive = dto.IsActive;
            employee.UpdatedAt = DateTime.UtcNow;

            await _employeeRepo.UpdateAsync(employee);
            return employee;
        }

        public async Task<bool> DeleteEmployeeAsync(string id, string businessId)
        {
            var employee = await _employeeRepo.GetByIdAsync(id, businessId);
            if (employee == null) return false;

            if (employee.AdvanceBalance > 0)
            {
                throw new InvalidOperationException($"Cannot delete employee with pending advance balance of PKR {employee.AdvanceBalance:N2}. Clear advance first.");
            }

            await _employeeRepo.SoftDeleteAsync(id, businessId);
            return true;
        }
    }

    #endregion

    #region Sale & POS Service

    public interface ISaleService
    {
        Task<SaleDetailDto> ProcessSaleAsync(string businessId, CreateSaleDto dto);
        Task<List<SaleListItemDto>> GetSalesHistoryAsync(string businessId, DateTime? fromDate = null, DateTime? toDate = null, string? customerId = null, string? searchTerm = null);
        Task<SaleDetailDto?> GetSaleByIdAsync(string saleId, string businessId);
        Task<SalesSummaryDto> GetSalesSummaryAsync(string businessId);
    }

    public class SaleService : ISaleService
    {
        private readonly AppDbContext _context;
        private readonly ISaleRepository _saleRepo;
        private readonly IProductRepository _productRepo;
        private readonly ICustomerRepository _customerRepo;

        public SaleService(
            AppDbContext context,
            ISaleRepository saleRepo,
            IProductRepository productRepo,
            ICustomerRepository customerRepo)
        {
            _context = context;
            _saleRepo = saleRepo;
            _productRepo = productRepo;
            _customerRepo = customerRepo;
        }

        public async Task<SaleDetailDto> ProcessSaleAsync(string businessId, CreateSaleDto dto)
        {
            if (dto.Items == null || dto.Items.Count == 0)
            {
                throw new ArgumentException("Cannot process a sale with no items.", nameof(dto.Items));
            }

            foreach (var item in dto.Items)
            {
                if (item.Quantity <= 0)
                {
                    throw new ArgumentException($"Invalid quantity for product '{item.ProductName}'. Quantity must be greater than zero.");
                }
            }

            // Begin atomic transaction
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // 1. Validate Stock and Fetch all Products
                var productIds = dto.Items.Select(i => i.ProductId).Distinct().ToList();
                var products = await _context.Products
                    .Where(p => p.BusinessId == businessId && productIds.Contains(p.Id) && !p.IsDeleted)
                    .ToDictionaryAsync(p => p.Id);

                foreach (var item in dto.Items)
                {
                    if (!products.TryGetValue(item.ProductId, out var product))
                    {
                        throw new KeyNotFoundException($"Product '{item.ProductName}' (ID: {item.ProductId}) does not exist.");
                    }

                    if (product.StockQuantity < item.Quantity)
                    {
                        throw new InvalidOperationException(
                            $"Insufficient stock for product '{product.Name}'. Available: {product.StockQuantity:G29}, Requested: {item.Quantity:G29}.");
                    }
                }

                // 2. Generate Next Sequential Invoice Number
                var invoiceNumber = await _saleRepo.GenerateNextInvoiceNumberAsync(businessId);
                var saleId = Guid.NewGuid().ToString();

                // 3. Compute Item Lines, Taxes, Subtotal, Grand Total
                decimal subtotal = 0.00m;
                decimal totalItemDiscount = 0.00m;
                decimal totalTax = 0.00m;

                var saleItems = new List<SaleItem>();
                var itemDetails = new List<SaleItemDetailDto>();

                foreach (var item in dto.Items)
                {
                    var product = products[item.ProductId];
                    var unitPrice = item.UnitPrice > 0 ? item.UnitPrice : product.SalePrice;
                    var unitCost = product.CostPrice; // Snapshot cost at time of sale for exact COGS
                    var lineSubtotal = item.Quantity * unitPrice;
                    var lineDiscount = Math.Max(0, item.Discount);
                    var taxableAmount = Math.Max(0, lineSubtotal - lineDiscount);
                    var taxRate = item.TaxRate >= 0 ? item.TaxRate : product.TaxRate;
                    var lineTax = Math.Round(taxableAmount * (taxRate / 100m), 2, MidpointRounding.AwayFromZero);
                    var lineTotal = taxableAmount + lineTax;

                    subtotal += lineSubtotal;
                    totalItemDiscount += lineDiscount;
                    totalTax += lineTax;

                    var saleItem = new SaleItem
                    {
                        Id = Guid.NewGuid().ToString(),
                        SaleId = saleId,
                        ProductId = product.Id,
                        ProductName = product.Name,
                        Unit = product.Unit,
                        Quantity = item.Quantity,
                        UnitPrice = unitPrice,
                        UnitCost = unitCost,
                        Discount = lineDiscount,
                        TaxRate = taxRate,
                        TaxAmount = lineTax,
                        TotalPrice = lineTotal
                    };
                    saleItems.Add(saleItem);

                    itemDetails.Add(new SaleItemDetailDto
                    {
                        Id = saleItem.Id,
                        ProductId = saleItem.ProductId,
                        ProductName = saleItem.ProductName,
                        Unit = saleItem.Unit,
                        Quantity = saleItem.Quantity,
                        UnitPrice = saleItem.UnitPrice,
                        UnitCost = saleItem.UnitCost,
                        Discount = saleItem.Discount,
                        TaxRate = saleItem.TaxRate,
                        TaxAmount = saleItem.TaxAmount,
                        TotalPrice = saleItem.TotalPrice
                    });
                }

                var totalDiscount = totalItemDiscount + Math.Max(0, dto.OrderDiscount);
                var grandTotal = Math.Max(0, subtotal - totalDiscount + totalTax);

                decimal paidAmount = 0.00m;
                if (dto.PaymentMethod == PaymentMethod.CREDIT)
                {
                    paidAmount = 0.00m;
                }
                else
                {
                    paidAmount = Math.Max(0, dto.PaidAmount);
                    if (paidAmount > grandTotal)
                    {
                        paidAmount = grandTotal; // Cap at grand total; change is returned immediately to customer in cash
                    }
                }

                var dueBalance = Math.Max(0, grandTotal - paidAmount);

                // 4. Validate Customer Credit Limit before modifying stock or customer
                Customer? customer = null;
                if (!string.IsNullOrWhiteSpace(dto.CustomerId))
                {
                    customer = await _context.Customers
                        .FirstOrDefaultAsync(c => c.Id == dto.CustomerId && c.BusinessId == businessId && !c.IsDeleted);

                    if (customer != null && dueBalance > 0)
                    {
                        if (customer.CreditLimit > 0 && (customer.CurrentBalance + dueBalance) > customer.CreditLimit)
                        {
                            throw new InvalidOperationException(
                                $"Credit limit exceeded for customer '{customer.Name}'. Current balance: PKR {customer.CurrentBalance:N2}, Due from sale: PKR {dueBalance:N2}, Resulting balance: PKR {(customer.CurrentBalance + dueBalance):N2}, Credit limit: PKR {customer.CreditLimit:N2}.");
                        }
                    }
                }

                // 5. Deduct Product Stock & Record Audit Movement
                foreach (var item in dto.Items)
                {
                    var product = products[item.ProductId];
                    var unitCost = product.CostPrice;
                    var prevStock = product.StockQuantity;
                    product.StockQuantity -= item.Quantity;
                    product.UpdatedAt = DateTime.UtcNow;
                    product.SyncStatus = SyncStatus.PendingUpdate;

                    var stockAdjustment = new StockAdjustment
                    {
                        Id = Guid.NewGuid().ToString(),
                        BusinessId = businessId,
                        ProductId = product.Id,
                        Type = StockAdjustmentType.AUDIT_CORRECTION,
                        QuantityChanged = -item.Quantity,
                        CostPerUnit = unitCost,
                        PreviousQuantity = prevStock,
                        NewQuantity = product.StockQuantity,
                        Reason = $"Sale Invoice #{invoiceNumber}",
                        AdjustmentDate = dto.SaleDate,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow,
                        IsDeleted = false,
                        SyncStatus = SyncStatus.PendingCreate
                    };

                    await _context.StockAdjustments.AddAsync(stockAdjustment);

                    // Sync Queue items for Product and StockAdjustment
                    await _context.SyncQueue.AddAsync(new SyncQueueItem
                    {
                        BusinessId = businessId,
                        EntityType = nameof(Product),
                        EntityId = product.Id,
                        OperationType = SyncOperationType.UPDATE,
                        PayloadJson = JsonSerializer.Serialize(product),
                        CreatedAt = DateTime.UtcNow
                    });

                    await _context.SyncQueue.AddAsync(new SyncQueueItem
                    {
                        BusinessId = businessId,
                        EntityType = nameof(StockAdjustment),
                        EntityId = stockAdjustment.Id,
                        OperationType = SyncOperationType.CREATE,
                        PayloadJson = JsonSerializer.Serialize(stockAdjustment),
                        CreatedAt = DateTime.UtcNow
                    });
                }

                // 6. Update Customer Balance
                if (customer != null && dueBalance > 0)
                {
                    customer.CurrentBalance += dueBalance;
                    customer.UpdatedAt = DateTime.UtcNow;
                    customer.SyncStatus = SyncStatus.PendingUpdate;

                    await _context.SyncQueue.AddAsync(new SyncQueueItem
                    {
                        BusinessId = businessId,
                        EntityType = nameof(Customer),
                        EntityId = customer.Id,
                        OperationType = SyncOperationType.UPDATE,
                        PayloadJson = JsonSerializer.Serialize(customer),
                        CreatedAt = DateTime.UtcNow
                    });
                }

                // 6. Create Sale Record
                var sale = new Sale
                {
                    Id = saleId,
                    BusinessId = businessId,
                    InvoiceNumber = invoiceNumber,
                    CustomerId = customer?.Id ?? string.Empty,
                    SaleDate = dto.SaleDate,
                    Subtotal = subtotal,
                    DiscountAmount = totalDiscount,
                    TaxAmount = totalTax,
                    GrandTotal = grandTotal,
                    PaidAmount = paidAmount,
                    DueBalance = dueBalance,
                    PaymentMethod = dto.PaymentMethod,
                    Notes = dto.Notes?.Trim() ?? string.Empty,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    IsDeleted = false,
                    SyncStatus = SyncStatus.PendingCreate,
                    Items = saleItems
                };

                await _context.Sales.AddAsync(sale);
                await _context.SaleItems.AddRangeAsync(saleItems);

                // 7. Enqueue Sync for Sale
                await _context.SyncQueue.AddAsync(new SyncQueueItem
                {
                    BusinessId = businessId,
                    EntityType = nameof(Sale),
                    EntityId = sale.Id,
                    OperationType = SyncOperationType.CREATE,
                    PayloadJson = JsonSerializer.Serialize(new { Sale = sale, Items = saleItems }),
                    CreatedAt = DateTime.UtcNow
                });

                // Commit everything atomically
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return new SaleDetailDto
                {
                    Id = sale.Id,
                    InvoiceNumber = sale.InvoiceNumber,
                    CustomerId = customer?.Id ?? string.Empty,
                    CustomerName = customer?.Name ?? (!string.IsNullOrWhiteSpace(dto.CustomerName) ? dto.CustomerName : "Walk-in Customer"),
                    CustomerPhone = customer?.Phone ?? string.Empty,
                    CustomerAddress = customer?.Address ?? string.Empty,
                    SaleDate = sale.SaleDate,
                    Subtotal = sale.Subtotal,
                    DiscountAmount = sale.DiscountAmount,
                    TaxAmount = sale.TaxAmount,
                    GrandTotal = sale.GrandTotal,
                    PaidAmount = sale.PaidAmount,
                    DueBalance = sale.DueBalance,
                    PaymentMethod = sale.PaymentMethod,
                    Notes = sale.Notes,
                    Items = itemDetails
                };
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<List<SaleListItemDto>> GetSalesHistoryAsync(
            string businessId,
            DateTime? fromDate = null,
            DateTime? toDate = null,
            string? customerId = null,
            string? searchTerm = null)
        {
            var sales = await _saleRepo.GetSalesHistoryAsync(businessId, fromDate, toDate, customerId, searchTerm);

            var customerIds = sales.Select(s => s.CustomerId).Where(id => !string.IsNullOrEmpty(id)).Distinct().ToList();
            var customerNames = await _context.Customers
                .Where(c => c.BusinessId == businessId && customerIds.Contains(c.Id))
                .ToDictionaryAsync(c => c.Id, c => c.Name);

            return sales.Select(s => new SaleListItemDto
            {
                Id = s.Id,
                InvoiceNumber = s.InvoiceNumber,
                CustomerId = s.CustomerId,
                CustomerName = (!string.IsNullOrEmpty(s.CustomerId) && customerNames.TryGetValue(s.CustomerId, out var name)) ? name : "Walk-in Customer",
                SaleDate = s.SaleDate,
                Subtotal = s.Subtotal,
                DiscountAmount = s.DiscountAmount,
                TaxAmount = s.TaxAmount,
                GrandTotal = s.GrandTotal,
                PaidAmount = s.PaidAmount,
                DueBalance = s.DueBalance,
                PaymentMethod = s.PaymentMethod,
                Notes = s.Notes,
                ItemCount = s.Items.Count
            }).ToList();
        }

        public async Task<SaleDetailDto?> GetSaleByIdAsync(string saleId, string businessId)
        {
            var sale = await _saleRepo.GetSaleWithItemsAsync(saleId, businessId);
            if (sale == null) return null;

            Customer? customer = null;
            if (!string.IsNullOrEmpty(sale.CustomerId))
            {
                customer = await _customerRepo.GetByIdAsync(sale.CustomerId, businessId);
            }

            return new SaleDetailDto
            {
                Id = sale.Id,
                InvoiceNumber = sale.InvoiceNumber,
                CustomerId = sale.CustomerId,
                CustomerName = customer?.Name ?? "Walk-in Customer",
                CustomerPhone = customer?.Phone ?? string.Empty,
                CustomerAddress = customer?.Address ?? string.Empty,
                SaleDate = sale.SaleDate,
                Subtotal = sale.Subtotal,
                DiscountAmount = sale.DiscountAmount,
                TaxAmount = sale.TaxAmount,
                GrandTotal = sale.GrandTotal,
                PaidAmount = sale.PaidAmount,
                DueBalance = sale.DueBalance,
                PaymentMethod = sale.PaymentMethod,
                Notes = sale.Notes,
                Items = sale.Items.Select(i => new SaleItemDetailDto
                {
                    Id = i.Id,
                    ProductId = i.ProductId,
                    ProductName = i.ProductName,
                    Unit = i.Unit,
                    Quantity = i.Quantity,
                    UnitPrice = i.UnitPrice,
                    UnitCost = i.UnitCost,
                    Discount = i.Discount,
                    TaxRate = i.TaxRate,
                    TaxAmount = i.TaxAmount,
                    TotalPrice = i.TotalPrice
                }).ToList()
            };
        }

        public async Task<SalesSummaryDto> GetSalesSummaryAsync(string businessId)
        {
            var today = DateTime.UtcNow.Date;
            var salesQuery = _context.Sales.Where(s => s.BusinessId == businessId && !s.IsDeleted);

            var allSales = await salesQuery
                .Select(s => new { s.GrandTotal, s.PaidAmount, s.DueBalance, s.PaymentMethod, s.SaleDate })
                .ToListAsync();

            var totalInvoices = allSales.Count;
            var todayInvoices = allSales.Count(s => s.SaleDate >= today);
            var totalRevenue = allSales.Sum(s => s.GrandTotal);
            var todayRevenue = allSales.Where(s => s.SaleDate >= today).Sum(s => s.GrandTotal);
            var totalPaid = allSales.Sum(s => s.PaidAmount);
            var totalDue = allSales.Sum(s => s.DueBalance);

            var cashSales = allSales.Where(s => s.PaymentMethod == PaymentMethod.CASH).Sum(s => s.GrandTotal);
            var creditSales = allSales.Where(s => s.PaymentMethod == PaymentMethod.CREDIT).Sum(s => s.GrandTotal);
            var bankSales = allSales.Where(s => s.PaymentMethod == PaymentMethod.BANK).Sum(s => s.GrandTotal);

            return new SalesSummaryDto
            {
                TotalInvoicesCount = totalInvoices,
                TodayInvoicesCount = todayInvoices,
                TotalRevenue = totalRevenue,
                TodayRevenue = todayRevenue,
                TotalPaid = totalPaid,
                TotalDueReceivables = totalDue,
                CashSalesAmount = cashSales,
                CreditSalesAmount = creditSales,
                BankSalesAmount = bankSales
            };
        }
    }

    #endregion
}
