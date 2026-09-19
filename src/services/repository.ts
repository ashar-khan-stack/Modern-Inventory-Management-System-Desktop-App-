/**
 * Repository Layer with Business Tenant Isolation
 * Encapsulates asynchronous CRUD queries and soft-deletes.
 */

import {
  AccountHead,
  BankAccount,
  Category,
  Customer,
  Employee,
  Expense,
  Product,
  Purchase,
  PurchaseItem,
  Sale,
  SaleItem,
  SyncQueueItem,
  SyncStatus,
  Voucher,
  VoucherEntry,
} from '../types';
import { localDb } from '../infrastructure/storage/localDatabase';
import { generateUUID } from '../domain/security';

export interface IRepository<T> {
  getAllAsync(businessId: string): Promise<T[]>;
  getByIdAsync(businessId: string, id: string): Promise<T | null>;
  insertAsync(item: T): Promise<T>;
  updateAsync(item: T): Promise<T>;
  deleteAsync(businessId: string, id: string): Promise<boolean>;
}

// 1. Category Repository
export class CategoryRepository implements IRepository<Category> {
  async getAllAsync(businessId: string): Promise<Category[]> {
    return localDb.getCategories(businessId);
  }
  async getByIdAsync(businessId: string, id: string): Promise<Category | null> {
    return localDb.getCategories(businessId).find(c => c.id === id) || null;
  }
  async insertAsync(item: Category): Promise<Category> {
    const newItem: Category = {
      ...item,
      id: item.id || generateUUID(),
      createdAt: item.createdAt || new Date().toISOString(),
      updatedAt: new Date().toISOString(),
      isDeleted: false,
      syncStatus: SyncStatus.PendingCreate,
    };
    localDb.insertCategory(newItem);
    return newItem;
  }
  async updateAsync(item: Category): Promise<Category> {
    localDb.updateCategory(item);
    return item;
  }
  async deleteAsync(businessId: string, id: string): Promise<boolean> {
    localDb.deleteCategory(businessId, id);
    return true;
  }
}

// 2. Product Repository
export class ProductRepository implements IRepository<Product> {
  async getAllAsync(businessId: string): Promise<Product[]> {
    return localDb.getProducts(businessId);
  }
  async getByIdAsync(businessId: string, id: string): Promise<Product | null> {
    return localDb.getProductById(businessId, id) || null;
  }
  async insertAsync(item: Product): Promise<Product> {
    const newItem: Product = {
      ...item,
      id: item.id || generateUUID(),
      createdAt: item.createdAt || new Date().toISOString(),
      updatedAt: new Date().toISOString(),
      isDeleted: false,
      syncStatus: SyncStatus.PendingCreate,
    };
    localDb.insertProduct(newItem);
    return newItem;
  }
  async updateAsync(item: Product): Promise<Product> {
    localDb.updateProduct(item);
    return item;
  }
  async deleteAsync(businessId: string, id: string): Promise<boolean> {
    localDb.deleteProduct(businessId, id);
    return true;
  }
}

// 3. Customer Repository (Strict separation from Employee)
export class CustomerRepository implements IRepository<Customer> {
  async getAllAsync(businessId: string): Promise<Customer[]> {
    return localDb.getCustomers(businessId);
  }
  async getByIdAsync(businessId: string, id: string): Promise<Customer | null> {
    return localDb.getCustomerById(businessId, id) || null;
  }
  async insertAsync(item: Customer): Promise<Customer> {
    const newItem: Customer = {
      ...item,
      id: item.id || generateUUID(),
      createdAt: item.createdAt || new Date().toISOString(),
      updatedAt: new Date().toISOString(),
      isDeleted: false,
      syncStatus: SyncStatus.PendingCreate,
    };
    localDb.insertCustomer(newItem);
    return newItem;
  }
  async updateAsync(item: Customer): Promise<Customer> {
    localDb.updateCustomer(item);
    return item;
  }
  async deleteAsync(businessId: string, id: string): Promise<boolean> {
    localDb.deleteCustomer(businessId, id);
    return true;
  }
}

// 4. Employee Repository (Strict separation from Customer)
export class EmployeeRepository implements IRepository<Employee> {
  async getAllAsync(businessId: string): Promise<Employee[]> {
    return localDb.getEmployees(businessId);
  }
  async getByIdAsync(businessId: string, id: string): Promise<Employee | null> {
    return localDb.getEmployeeById(businessId, id) || null;
  }
  async insertAsync(item: Employee): Promise<Employee> {
    const newItem: Employee = {
      ...item,
      id: item.id || generateUUID(),
      createdAt: item.createdAt || new Date().toISOString(),
      updatedAt: new Date().toISOString(),
      isDeleted: false,
      syncStatus: SyncStatus.PendingCreate,
    };
    localDb.insertEmployee(newItem);
    return newItem;
  }
  async updateAsync(item: Employee): Promise<Employee> {
    localDb.updateEmployee(item);
    return item;
  }
  async deleteAsync(businessId: string, id: string): Promise<boolean> {
    localDb.deleteEmployee(businessId, id);
    return true;
  }
}

// 5. Account Head Repository
export class AccountHeadRepository implements IRepository<AccountHead> {
  async getAllAsync(businessId: string): Promise<AccountHead[]> {
    return localDb.getAccountHeads(businessId);
  }
  async getByIdAsync(businessId: string, id: string): Promise<AccountHead | null> {
    return localDb.getAccountHeads(businessId).find(a => a.id === id) || null;
  }
  async insertAsync(item: AccountHead): Promise<AccountHead> {
    const newItem: AccountHead = {
      ...item,
      id: item.id || generateUUID(),
      createdAt: item.createdAt || new Date().toISOString(),
      updatedAt: new Date().toISOString(),
      isDeleted: false,
      syncStatus: SyncStatus.PendingCreate,
    };
    localDb.insertAccountHead(newItem);
    return newItem;
  }
  async updateAsync(item: AccountHead): Promise<AccountHead> {
    return item;
  }
  async deleteAsync(_businessId: string, _id: string): Promise<boolean> {
    return false;
  }
}

// 6. Voucher Repository
export class VoucherRepository {
  async getAllAsync(businessId: string): Promise<Voucher[]> {
    return localDb.getVouchers(businessId);
  }
  async insertWithEntriesAsync(voucher: Voucher, entries: VoucherEntry[]): Promise<Voucher> {
    const voucherId = voucher.id || generateUUID();
    const newVoucher: Voucher = {
      ...voucher,
      id: voucherId,
      createdAt: voucher.createdAt || new Date().toISOString(),
      updatedAt: new Date().toISOString(),
      isDeleted: false,
      syncStatus: SyncStatus.PendingCreate,
    };
    const newEntries = entries.map(e => ({
      ...e,
      id: e.id || generateUUID(),
      voucherId,
    }));
    localDb.insertVoucher(newVoucher, newEntries);
    return newVoucher;
  }
}

// 7. Sale Repository
export class SaleRepository {
  async getAllAsync(businessId: string): Promise<Sale[]> {
    return localDb.getSales(businessId);
  }
  async insertSaleAsync(sale: Sale, items: SaleItem[]): Promise<Sale> {
    const saleId = sale.id || generateUUID();
    const newSale: Sale = {
      ...sale,
      id: saleId,
      createdAt: sale.createdAt || new Date().toISOString(),
      updatedAt: new Date().toISOString(),
      isDeleted: false,
      syncStatus: SyncStatus.PendingCreate,
    };
    const newItems = items.map(item => ({
      ...item,
      id: item.id || generateUUID(),
      saleId,
    }));
    localDb.insertSale(newSale, newItems);
    return newSale;
  }
}

// 8. Purchase Repository
export class PurchaseRepository {
  async getAllAsync(businessId: string): Promise<Purchase[]> {
    return localDb.getPurchases(businessId);
  }
  async insertPurchaseAsync(purchase: Purchase, items: PurchaseItem[]): Promise<Purchase> {
    const purchaseId = purchase.id || generateUUID();
    const newPurchase: Purchase = {
      ...purchase,
      id: purchaseId,
      createdAt: purchase.createdAt || new Date().toISOString(),
      updatedAt: new Date().toISOString(),
      isDeleted: false,
      syncStatus: SyncStatus.PendingCreate,
    };
    const newItems = items.map(item => ({
      ...item,
      id: item.id || generateUUID(),
      purchaseId,
    }));
    localDb.insertPurchase(newPurchase, newItems);
    return newPurchase;
  }
}

// 9. Expense Repository
export class ExpenseRepository implements IRepository<Expense> {
  async getAllAsync(businessId: string): Promise<Expense[]> {
    return localDb.getExpenses(businessId);
  }
  async getByIdAsync(businessId: string, id: string): Promise<Expense | null> {
    return localDb.getExpenses(businessId).find(e => e.id === id) || null;
  }
  async insertAsync(item: Expense): Promise<Expense> {
    const newItem: Expense = {
      ...item,
      id: item.id || generateUUID(),
      createdAt: item.createdAt || new Date().toISOString(),
      updatedAt: new Date().toISOString(),
      isDeleted: false,
      syncStatus: SyncStatus.PendingCreate,
    };
    localDb.insertExpense(newItem);
    return newItem;
  }
  async updateAsync(item: Expense): Promise<Expense> {
    return item;
  }
  async deleteAsync(_businessId: string, _id: string): Promise<boolean> {
    return false;
  }
}

// 10. Bank Repository
export class BankRepository implements IRepository<BankAccount> {
  async getAllAsync(businessId: string): Promise<BankAccount[]> {
    return localDb.getBankAccounts(businessId);
  }
  async getByIdAsync(businessId: string, id: string): Promise<BankAccount | null> {
    return localDb.getBankAccounts(businessId).find(b => b.id === id) || null;
  }
  async insertAsync(item: BankAccount): Promise<BankAccount> {
    const newItem: BankAccount = {
      ...item,
      id: item.id || generateUUID(),
      createdAt: item.createdAt || new Date().toISOString(),
      updatedAt: new Date().toISOString(),
      isDeleted: false,
      syncStatus: SyncStatus.PendingCreate,
    };
    localDb.insertBankAccount(newItem);
    return newItem;
  }
  async updateAsync(item: BankAccount): Promise<BankAccount> {
    return item;
  }
  async deleteAsync(_businessId: string, _id: string): Promise<boolean> {
    return false;
  }
}

// 11. Sync Queue Repository
export class SyncQueueRepository {
  async getPendingQueueAsync(businessId: string): Promise<SyncQueueItem[]> {
    return localDb.getSyncQueue(businessId);
  }
  async markItemFailedAsync(id: string, errorMessage: string): Promise<void> {
    localDb.markSyncQueueFailed(id, errorMessage);
  }
  async removeCompletedItemAsync(id: string): Promise<void> {
    localDb.removeSyncQueueItem(id);
  }
}

export const categoryRepo = new CategoryRepository();
export const productRepo = new ProductRepository();
export const customerRepo = new CustomerRepository();
export const employeeRepo = new EmployeeRepository();
export const accountHeadRepo = new AccountHeadRepository();
export const voucherRepo = new VoucherRepository();
export const saleRepo = new SaleRepository();
export const purchaseRepo = new PurchaseRepository();
export const expenseRepo = new ExpenseRepository();
export const bankRepo = new BankRepository();
export const syncQueueRepo = new SyncQueueRepository();
