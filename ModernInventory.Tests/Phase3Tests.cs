using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using ModernInventory.Core.Application.DTOs;
using ModernInventory.Core.Application.Repositories;
using ModernInventory.Core.Application.Services;
using ModernInventory.Core.Domain.Entities;
using ModernInventory.Core.Domain.Enums;
using ModernInventory.Core.Infrastructure.Database;
using Xunit;

namespace ModernInventory.Tests
{
    public class Phase3Tests : IDisposable
    {
        private readonly SqliteConnection _connection;
        private readonly AppDbContext _context;
        private readonly ICustomerRepository _customerRepo;
        private readonly IEmployeeRepository _employeeRepo;
        private readonly ISaleRepository _saleRepo;
        private readonly IProductRepository _productRepo;
        private readonly ICategoryRepository _categoryRepo;
        private readonly IStockAdjustmentRepository _stockRepo;
        private readonly ICustomerService _customerService;
        private readonly IEmployeeService _employeeService;
        private readonly ISaleService _saleService;
        private readonly IInventoryService _inventoryService;

        private const string TestBizA = "biz-corp-alpha";
        private const string TestBizB = "biz-corp-beta";

        public Phase3Tests()
        {
            _connection = new SqliteConnection("Data Source=:memory:");
            _connection.Open();

            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite(_connection)
                .Options;

            _context = new AppDbContext(options);
            _context.Database.EnsureCreated();

            _customerRepo = new CustomerRepository(_context);
            _employeeRepo = new EmployeeRepository(_context);
            _saleRepo = new SaleRepository(_context);
            _productRepo = new ProductRepository(_context);
            _categoryRepo = new CategoryRepository(_context);
            _stockRepo = new StockAdjustmentRepository(_context);

            _customerService = new CustomerService(_context, _customerRepo);
            _employeeService = new EmployeeService(_context, _employeeRepo);
            _saleService = new SaleService(_context, _saleRepo, _productRepo, _customerRepo);
            _inventoryService = new InventoryService(_context, _productRepo, _categoryRepo, _stockRepo);
        }

        public void Dispose()
        {
            _context.Dispose();
            _connection.Dispose();
        }

        [Fact]
        public async Task Test01_Customer_CreateAndSyncQueue()
        {
            var customer = await _customerService.AddCustomerAsync(TestBizA, new CreateCustomerDto
            {
                Name = "Ahmed Khan",
                CompanyName = "Khan Traders",
                Phone = "03001234567",
                Email = "ahmed@example.com",
                Cnic = "42101-1234567-1",
                Address = "Karachi Saddar",
                CreditLimit = 50000m,
                OpeningBalance = 5000m
            });

            Assert.NotNull(customer);
            Assert.Equal("Ahmed Khan", customer.Name);
            Assert.Equal(5000m, customer.CurrentBalance);
            Assert.Equal(50000m, customer.CreditLimit);

            // Verify SyncQueue
            var sync = await _context.SyncQueue
                .FirstOrDefaultAsync(q => q.EntityId == customer.Id && q.EntityType == nameof(Customer));
            Assert.NotNull(sync);
            Assert.Equal(SyncOperationType.CREATE, sync.OperationType);
        }

        [Fact]
        public async Task Test02_Customer_DuplicatePhoneThrows()
        {
            await _customerService.AddCustomerAsync(TestBizA, new CreateCustomerDto
            {
                Name = "Customer One",
                Phone = "03129998877"
            });

            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                await _customerService.AddCustomerAsync(TestBizA, new CreateCustomerDto
                {
                    Name = "Customer Duplicate",
                    Phone = "03129998877"
                });
            });
        }

        [Fact]
        public async Task Test03_Customer_SearchAndTenantIsolation()
        {
            await _customerService.AddCustomerAsync(TestBizA, new CreateCustomerDto
            {
                Name = "Ali Raza",
                Phone = "03211112233"
            });

            await _customerService.AddCustomerAsync(TestBizB, new CreateCustomerDto
            {
                Name = "Tariq Mehmood",
                Phone = "03334445566"
            });

            var bizACustomers = await _customerService.GetCustomersAsync(TestBizA);
            Assert.Single(bizACustomers);
            Assert.Equal("Ali Raza", bizACustomers[0].Name);

            var bizBCustomers = await _customerService.GetCustomersAsync(TestBizB);
            Assert.Single(bizBCustomers);
            Assert.Equal("Tariq Mehmood", bizBCustomers[0].Name);
        }

        [Fact]
        public async Task Test04_Employee_CreateAndActiveFilter()
        {
            var emp1 = await _employeeService.AddEmployeeAsync(TestBizA, new CreateEmployeeDto
            {
                Name = "Usman Tariq",
                Designation = "Cashier",
                Cnic = "35201-9876543-1",
                Phone = "03009988776",
                MonthlySalary = 35000m,
                AdvanceBalance = 5000m,
                IsActive = true
            });

            var emp2 = await _employeeService.AddEmployeeAsync(TestBizA, new CreateEmployeeDto
            {
                Name = "Hamza Ali",
                Designation = "Security Guard",
                Cnic = "35201-1122334-1",
                Phone = "03451122334",
                MonthlySalary = 25000m,
                IsActive = false
            });

            var all = await _employeeService.GetEmployeesAsync(TestBizA);
            Assert.Equal(2, all.Count);

            var activeOnly = await _employeeService.GetEmployeesAsync(TestBizA, activeOnly: true);
            Assert.Single(activeOnly);
            Assert.Equal("Usman Tariq", activeOnly[0].Name);
        }

        [Fact]
        public async Task Test05_Employee_DuplicateCnicThrows()
        {
            await _employeeService.AddEmployeeAsync(TestBizA, new CreateEmployeeDto
            {
                Name = "Emp 1",
                Cnic = "42201-9988776-5"
            });

            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                await _employeeService.AddEmployeeAsync(TestBizA, new CreateEmployeeDto
                {
                    Name = "Emp 2",
                    Cnic = "42201-9988776-5"
                });
            });
        }

        [Fact]
        public async Task Test06_Sale_CashSale_StockDeductionAndAuditAdjustment()
        {
            // Add product with 50 pcs
            var product = await _inventoryService.AddProductAsync(TestBizA, new CreateProductDto
            {
                Name = "Basmati Rice 5kg",
                Sku = "RICE-5KG",
                Barcode = "8961234567890",
                CostPrice = 800m,
                SalePrice = 1000m,
                InitialStock = 50m,
                TaxRate = 18m
            });

            // Process Sale for 5 pcs
            var saleDto = new CreateSaleDto
            {
                CustomerName = "Walk-in Cash Customer",
                PaymentMethod = PaymentMethod.CASH,
                PaidAmount = 5900m, // 5 * 1000 = 5000 + 18% tax (900) = 5900
                Items = new List<CreateSaleItemDto>
                {
                    new CreateSaleItemDto
                    {
                        ProductId = product.Id,
                        ProductName = product.Name,
                        Quantity = 5m,
                        UnitPrice = 1000m,
                        Discount = 0m,
                        TaxRate = 18m
                    }
                }
            };

            var sale = await _saleService.ProcessSaleAsync(TestBizA, saleDto);

            Assert.NotNull(sale);
            Assert.StartsWith("INV-", sale.InvoiceNumber);
            Assert.Equal(5000m, sale.Subtotal);
            Assert.Equal(900m, sale.TaxAmount);
            Assert.Equal(5900m, sale.GrandTotal);
            Assert.Equal(5900m, sale.PaidAmount);
            Assert.Equal(0m, sale.DueBalance);

            // Verify Stock decreased from 50 to 45
            var updatedProduct = await _productRepo.GetByIdAsync(product.Id, TestBizA);
            Assert.NotNull(updatedProduct);
            Assert.Equal(45m, updatedProduct.StockQuantity);

            // Verify Stock Adjustment recorded
            var adjustments = await _stockRepo.GetHistoryForProductAsync(product.Id, TestBizA);
            var saleAdj = adjustments.FirstOrDefault(a => a.Reason.Contains(sale.InvoiceNumber));
            Assert.NotNull(saleAdj);
            Assert.Equal(-5m, saleAdj.QuantityChanged);
            Assert.Equal(50m, saleAdj.PreviousQuantity);
            Assert.Equal(45m, saleAdj.NewQuantity);

            // Verify SyncQueue for Sale, Product, and StockAdjustment
            var syncSale = await _context.SyncQueue.FirstOrDefaultAsync(q => q.EntityId == sale.Id);
            Assert.NotNull(syncSale);
        }

        [Fact]
        public async Task Test07_Sale_CreditSale_CustomerReceivableBalanceUpdated()
        {
            var customer = await _customerService.AddCustomerAsync(TestBizA, new CreateCustomerDto
            {
                Name = "Bismillah Super Store",
                Phone = "03112233445",
                CreditLimit = 100000m,
                OpeningBalance = 0m
            });

            var product = await _inventoryService.AddProductAsync(TestBizA, new CreateProductDto
            {
                Name = "Cooking Oil 1L",
                Sku = "OIL-1L",
                CostPrice = 400m,
                SalePrice = 500m,
                InitialStock = 20m,
                TaxRate = 0m
            });

            var saleDto = new CreateSaleDto
            {
                CustomerId = customer.Id,
                CustomerName = customer.Name,
                PaymentMethod = PaymentMethod.CREDIT,
                PaidAmount = 0m,
                Items = new List<CreateSaleItemDto>
                {
                    new CreateSaleItemDto
                    {
                        ProductId = product.Id,
                        ProductName = product.Name,
                        Quantity = 10m,
                        UnitPrice = 500m,
                        Discount = 0m,
                        TaxRate = 0m
                    }
                }
            };

            var sale = await _saleService.ProcessSaleAsync(TestBizA, saleDto);

            Assert.Equal(5000m, sale.GrandTotal);
            Assert.Equal(0m, sale.PaidAmount);
            Assert.Equal(5000m, sale.DueBalance);

            // Verify Customer balance updated from 0 to 5000
            var updatedCustomer = await _customerRepo.GetByIdAsync(customer.Id, TestBizA);
            Assert.NotNull(updatedCustomer);
            Assert.Equal(5000m, updatedCustomer.CurrentBalance);

            // Customer transaction history has 1 entry
            var history = await _customerService.GetCustomerHistoryAsync(customer.Id, TestBizA);
            Assert.Single(history);
            Assert.Equal(sale.InvoiceNumber, history[0].ReferenceNumber);
            Assert.Equal(5000m, history[0].DueBalance);
        }

        [Fact]
        public async Task Test08_Sale_InsufficientStock_AtomicRollback()
        {
            var product = await _inventoryService.AddProductAsync(TestBizA, new CreateProductDto
            {
                Name = "Limited Luxury Item",
                Sku = "LUX-001",
                CostPrice = 5000m,
                SalePrice = 8000m,
                InitialStock = 2m,
                TaxRate = 0m
            });

            var saleDto = new CreateSaleDto
            {
                PaymentMethod = PaymentMethod.CASH,
                PaidAmount = 24000m,
                Items = new List<CreateSaleItemDto>
                {
                    new CreateSaleItemDto
                    {
                        ProductId = product.Id,
                        ProductName = product.Name,
                        Quantity = 5m, // Requested 5, but only 2 available
                        UnitPrice = 8000m
                    }
                }
            };

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                await _saleService.ProcessSaleAsync(TestBizA, saleDto);
            });

            Assert.Contains("Insufficient stock", ex.Message);

            // Verify product stock was NOT changed (remains 2m)
            var p = await _productRepo.GetByIdAsync(product.Id, TestBizA);
            Assert.NotNull(p);
            Assert.Equal(2m, p.StockQuantity);

            // Verify NO sales were created
            var sales = await _saleRepo.GetSalesHistoryAsync(TestBizA);
            Assert.Empty(sales);
        }

        [Fact]
        public async Task Test09_Sale_InvoiceNumberSequencePerMonth()
        {
            var product = await _inventoryService.AddProductAsync(TestBizA, new CreateProductDto
            {
                Name = "Item X",
                Sku = "ITEM-X",
                CostPrice = 10m,
                SalePrice = 20m,
                InitialStock = 100m,
                TaxRate = 0m
            });

            var sale1 = await _saleService.ProcessSaleAsync(TestBizA, new CreateSaleDto
            {
                Items = new List<CreateSaleItemDto>
                {
                    new CreateSaleItemDto { ProductId = product.Id, ProductName = product.Name, Quantity = 1m, UnitPrice = 20m }
                }
            });

            var sale2 = await _saleService.ProcessSaleAsync(TestBizA, new CreateSaleDto
            {
                Items = new List<CreateSaleItemDto>
                {
                    new CreateSaleItemDto { ProductId = product.Id, ProductName = product.Name, Quantity = 1m, UnitPrice = 20m }
                }
            });

            Assert.StartsWith("INV-", sale1.InvoiceNumber);
            Assert.StartsWith("INV-", sale2.InvoiceNumber);
            Assert.True(string.Compare(sale2.InvoiceNumber, sale1.InvoiceNumber) > 0);
        }

        [Fact]
        public async Task Test10_SalesSummaryMetrics()
        {
            var product = await _inventoryService.AddProductAsync(TestBizA, new CreateProductDto
            {
                Name = "Item Y",
                Sku = "ITEM-Y",
                CostPrice = 100m,
                SalePrice = 150m,
                InitialStock = 100m,
                TaxRate = 0m
            });

            await _saleService.ProcessSaleAsync(TestBizA, new CreateSaleDto
            {
                PaymentMethod = PaymentMethod.CASH,
                PaidAmount = 150m,
                Items = new List<CreateSaleItemDto>
                {
                    new CreateSaleItemDto { ProductId = product.Id, ProductName = product.Name, Quantity = 1m, UnitPrice = 150m, TaxRate = 0m }
                }
            });

            await _saleService.ProcessSaleAsync(TestBizA, new CreateSaleDto
            {
                PaymentMethod = PaymentMethod.CREDIT,
                PaidAmount = 0m,
                Items = new List<CreateSaleItemDto>
                {
                    new CreateSaleItemDto { ProductId = product.Id, ProductName = product.Name, Quantity = 2m, UnitPrice = 150m, TaxRate = 0m }
                }
            });

            var summary = await _saleService.GetSalesSummaryAsync(TestBizA);
            Assert.Equal(2, summary.TotalInvoicesCount);
            Assert.Equal(450m, summary.TotalRevenue);
            Assert.Equal(150m, summary.TotalPaid);
            Assert.Equal(300m, summary.TotalDueReceivables);
            Assert.Equal(150m, summary.CashSalesAmount);
            Assert.Equal(300m, summary.CreditSalesAmount);
        }

        [Fact]
        public async Task Test11_Sale_CreditLimitExceeded_ThrowsAndRollsBack()
        {
            var customer = await _customerService.AddCustomerAsync(TestBizA, new CreateCustomerDto
            {
                Name = "Strict Credit Customer",
                Phone = "03445566778",
                CreditLimit = 1000m, // Only 1,000 credit allowed
                OpeningBalance = 200m // Starts with 200 due
            });

            var product = await _inventoryService.AddProductAsync(TestBizA, new CreateProductDto
            {
                Name = "Bulk Sugar 50kg",
                Sku = "SUG-50",
                CostPrice = 4000m,
                SalePrice = 5000m,
                InitialStock = 10m,
                TaxRate = 0m
            });

            var saleDto = new CreateSaleDto
            {
                CustomerId = customer.Id,
                CustomerName = customer.Name,
                PaymentMethod = PaymentMethod.CREDIT,
                PaidAmount = 0m,
                Items = new List<CreateSaleItemDto>
                {
                    new CreateSaleItemDto
                    {
                        ProductId = product.Id,
                        ProductName = product.Name,
                        Quantity = 1m,
                        UnitPrice = 5000m, // 5000 + 200 = 5200 > 1000 credit limit!
                        Discount = 0m,
                        TaxRate = 0m
                    }
                }
            };

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                await _saleService.ProcessSaleAsync(TestBizA, saleDto);
            });

            Assert.Contains("Credit limit exceeded", ex.Message);

            // Verify customer balance was NOT modified (remains 200m)
            var c = await _customerRepo.GetByIdAsync(customer.Id, TestBizA);
            Assert.NotNull(c);
            Assert.Equal(200m, c.CurrentBalance);

            // Verify product stock was NOT changed (remains 10m)
            var p = await _productRepo.GetByIdAsync(product.Id, TestBizA);
            Assert.NotNull(p);
            Assert.Equal(10m, p.StockQuantity);
        }
    }
}
