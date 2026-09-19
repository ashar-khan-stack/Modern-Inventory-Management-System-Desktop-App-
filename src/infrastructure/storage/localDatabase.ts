/**
 * Local Offline SQLite-Compatible Database Storage Engine
 * Provides persistent offline-first relational document store with multi-tenant isolation,
 * UUID primary keys, soft-deletes, and transaction logs.
 */

import {
  AccountHead,
  BankAccount,
  BankTransaction,
  BusinessProfile,
  Category,
  Customer,
  Employee,
  Expense,
  LocalBackupRecord,
  Product,
  Purchase,
  PurchaseItem,
  Sale,
  SaleItem,
  StockAdjustment,
  SyncOperationType,
  SyncQueueItem,
  SyncStatus,
  User,
  UserSession,
  Voucher,
  VoucherEntry,
} from '../../types';
import { SYSTEM_ACCOUNT_HEADS } from '../../domain/constants';
import { generateUUID } from '../../domain/security';

const DB_STORAGE_KEY_PREFIX = 'modern_inv_db_';
const DB_VERSION = 1;

export interface DatabaseState {
  version: number;
  businesses: BusinessProfile[];
  users: User[];
  sessions: UserSession[];
  categories: Category[];
  products: Product[];
  stockAdjustments: StockAdjustment[];
  customers: Customer[];
  employees: Employee[];
  sales: Sale[];
  saleItems: SaleItem[];
  purchases: Purchase[];
  purchaseItems: PurchaseItem[];
  accountHeads: AccountHead[];
  vouchers: Voucher[];
  voucherEntries: VoucherEntry[];
  expenses: Expense[];
  bankAccounts: BankAccount[];
  bankTransactions: BankTransaction[];
  syncQueue: SyncQueueItem[];
  backups: LocalBackupRecord[];
}

const initialDbState: DatabaseState = {
  version: DB_VERSION,
  businesses: [],
  users: [],
  sessions: [],
  categories: [],
  products: [],
  stockAdjustments: [],
  customers: [],
  employees: [],
  sales: [],
  saleItems: [],
  purchases: [],
  purchaseItems: [],
  accountHeads: [],
  vouchers: [],
  voucherEntries: [],
  expenses: [],
  bankAccounts: [],
  bankTransactions: [],
  syncQueue: [],
  backups: [],
};

class LocalDatabase {
  private state: DatabaseState;
  private isLoaded = false;
  private listeners: Set<() => void> = new Set();

  constructor() {
    this.state = { ...initialDbState };
    this.loadFromStorage();
  }

  public subscribe(listener: () => void): () => void {
    this.listeners.add(listener);
    return () => this.listeners.delete(listener);
  }

  private notify() {
    this.listeners.forEach(cb => {
      try { cb(); } catch (e) { console.error(e); }
    });
  }

  private loadFromStorage() {
    try {
      const stored = localStorage.getItem(`${DB_STORAGE_KEY_PREFIX}v${DB_VERSION}`);
      if (stored) {
        const parsed = JSON.parse(stored);
        this.state = {
          ...initialDbState,
          ...parsed,
          version: DB_VERSION,
        };
      } else {
        this.state = { ...initialDbState };
      }
      this.isLoaded = true;
    } catch (err) {
      console.error('Failed to load local database from storage:', err);
      this.state = { ...initialDbState };
      this.isLoaded = true;
    }
  }

  public saveToStorage() {
    try {
      localStorage.setItem(`${DB_STORAGE_KEY_PREFIX}v${DB_VERSION}`, JSON.stringify(this.state));
      this.notify();
    } catch (err) {
      console.error('Failed to save local database to storage:', err);
    }
  }

  public getState(): DatabaseState {
    return this.state;
  }

  // --- Seed Default Chart of Accounts for a new Business ---
  public seedDefaultAccountHeads(businessId: string) {
    const now = new Date().toISOString();
    const defaultHeads: AccountHead[] = SYSTEM_ACCOUNT_HEADS.map(def => ({
      id: generateUUID(),
      businessId,
      code: def.code,
      name: def.name,
      type: def.type,
      description: def.description,
      isSystem: true,
      createdAt: now,
      updatedAt: now,
      isDeleted: false,
      syncStatus: SyncStatus.PendingCreate,
    }));

    // Queue sync items
    defaultHeads.forEach(head => {
      this.state.accountHeads.push(head);
      this.enqueueSync(businessId, 'accountHeads', head.id, SyncOperationType.CREATE, head);
    });

    this.saveToStorage();
  }

  // --- Sync Queue Helper ---
  public enqueueSync(
    businessId: string,
    entityType: string,
    entityId: string,
    operationType: SyncOperationType,
    payload: any
  ) {
    const now = new Date().toISOString();
    const queueItem: SyncQueueItem = {
      id: generateUUID(),
      businessId,
      entityType,
      entityId,
      operationType,
      payloadJson: JSON.stringify(payload),
      retryCount: 0,
      createdAt: now,
    };
    this.state.syncQueue.push(queueItem);
  }

  // --- Generic Entity Helper Operations with Business Tenant Isolation ---

  public getBusiness(id: string): BusinessProfile | undefined {
    return this.state.businesses.find(b => b.id === id);
  }

  public insertBusiness(business: BusinessProfile) {
    this.state.businesses.push(business);
    this.saveToStorage();
  }

  public getUserByEmail(email: string): User | undefined {
    return this.state.users.find(u => u.email.toLowerCase() === email.toLowerCase());
  }

  public getUserById(id: string): User | undefined {
    return this.state.users.find(u => u.id === id);
  }

  public insertUser(user: User) {
    this.state.users.push(user);
    this.saveToStorage();
  }

  public updateUser(user: User) {
    const index = this.state.users.findIndex(u => u.id === user.id);
    if (index >= 0) {
      this.state.users[index] = { ...user, updatedAt: new Date().toISOString() };
      this.saveToStorage();
    }
  }

  // --- Session Management ---

  public getSession(sessionId: string): UserSession | undefined {
    return this.state.sessions.find(s => s.sessionId === sessionId);
  }

  public getActiveSession(token: string): UserSession | undefined {
    return this.state.sessions.find(s => s.token === token);
  }

  public insertSession(session: UserSession) {
    // Remove expired or existing sessions for user if needed
    this.state.sessions = this.state.sessions.filter(s => s.userId !== session.userId);
    this.state.sessions.push(session);
    this.saveToStorage();
  }

  public deleteSession(token: string) {
    this.state.sessions = this.state.sessions.filter(s => s.token !== token);
    this.saveToStorage();
  }

  // --- Entity Collections with Scoped BusinessId Queries ---

  // Categories
  public getCategories(businessId: string): Category[] {
    return this.state.categories.filter(c => c.businessId === businessId && !c.isDeleted);
  }

  public insertCategory(item: Category) {
    this.state.categories.push(item);
    this.enqueueSync(item.businessId, 'categories', item.id, SyncOperationType.CREATE, item);
    this.saveToStorage();
  }

  public updateCategory(item: Category) {
    const idx = this.state.categories.findIndex(c => c.id === item.id && c.businessId === item.businessId);
    if (idx >= 0) {
      this.state.categories[idx] = { ...item, updatedAt: new Date().toISOString(), syncStatus: SyncStatus.PendingUpdate };
      this.enqueueSync(item.businessId, 'categories', item.id, SyncOperationType.UPDATE, this.state.categories[idx]);
      this.saveToStorage();
    }
  }

  public deleteCategory(businessId: string, id: string) {
    const idx = this.state.categories.findIndex(c => c.id === id && c.businessId === businessId);
    if (idx >= 0) {
      this.state.categories[idx].isDeleted = true;
      this.state.categories[idx].updatedAt = new Date().toISOString();
      this.state.categories[idx].syncStatus = SyncStatus.PendingDelete;
      this.enqueueSync(businessId, 'categories', id, SyncOperationType.DELETE, { id });
      this.saveToStorage();
    }
  }

  // Products
  public getProducts(businessId: string): Product[] {
    return this.state.products.filter(p => p.businessId === businessId && !p.isDeleted);
  }

  public getProductById(businessId: string, id: string): Product | undefined {
    return this.state.products.find(p => p.id === id && p.businessId === businessId && !p.isDeleted);
  }

  public insertProduct(item: Product) {
    this.state.products.push(item);
    this.enqueueSync(item.businessId, 'products', item.id, SyncOperationType.CREATE, item);
    this.saveToStorage();
  }

  public updateProduct(item: Product) {
    const idx = this.state.products.findIndex(p => p.id === item.id && p.businessId === item.businessId);
    if (idx >= 0) {
      this.state.products[idx] = { ...item, updatedAt: new Date().toISOString(), syncStatus: SyncStatus.PendingUpdate };
      this.enqueueSync(item.businessId, 'products', item.id, SyncOperationType.UPDATE, this.state.products[idx]);
      this.saveToStorage();
    }
  }

  public deleteProduct(businessId: string, id: string) {
    const idx = this.state.products.findIndex(p => p.id === id && p.businessId === businessId);
    if (idx >= 0) {
      this.state.products[idx].isDeleted = true;
      this.state.products[idx].updatedAt = new Date().toISOString();
      this.state.products[idx].syncStatus = SyncStatus.PendingDelete;
      this.enqueueSync(businessId, 'products', id, SyncOperationType.DELETE, { id });
      this.saveToStorage();
    }
  }

  // Customers (Strict separation from Employee)
  public getCustomers(businessId: string): Customer[] {
    return this.state.customers.filter(c => c.businessId === businessId && !c.isDeleted);
  }

  public getCustomerById(businessId: string, id: string): Customer | undefined {
    return this.state.customers.find(c => c.id === id && c.businessId === businessId && !c.isDeleted);
  }

  public insertCustomer(item: Customer) {
    this.state.customers.push(item);
    this.enqueueSync(item.businessId, 'customers', item.id, SyncOperationType.CREATE, item);
    this.saveToStorage();
  }

  public updateCustomer(item: Customer) {
    const idx = this.state.customers.findIndex(c => c.id === item.id && c.businessId === item.businessId);
    if (idx >= 0) {
      this.state.customers[idx] = { ...item, updatedAt: new Date().toISOString(), syncStatus: SyncStatus.PendingUpdate };
      this.enqueueSync(item.businessId, 'customers', item.id, SyncOperationType.UPDATE, this.state.customers[idx]);
      this.saveToStorage();
    }
  }

  public deleteCustomer(businessId: string, id: string) {
    const idx = this.state.customers.findIndex(c => c.id === id && c.businessId === businessId);
    if (idx >= 0) {
      this.state.customers[idx].isDeleted = true;
      this.state.customers[idx].updatedAt = new Date().toISOString();
      this.state.customers[idx].syncStatus = SyncStatus.PendingDelete;
      this.enqueueSync(businessId, 'customers', id, SyncOperationType.DELETE, { id });
      this.saveToStorage();
    }
  }

  // Employees (Strict separation from Customer)
  public getEmployees(businessId: string): Employee[] {
    return this.state.employees.filter(e => e.businessId === businessId && !e.isDeleted);
  }

  public getEmployeeById(businessId: string, id: string): Employee | undefined {
    return this.state.employees.find(e => e.id === id && e.businessId === businessId && !e.isDeleted);
  }

  public insertEmployee(item: Employee) {
    this.state.employees.push(item);
    this.enqueueSync(item.businessId, 'employees', item.id, SyncOperationType.CREATE, item);
    this.saveToStorage();
  }

  public updateEmployee(item: Employee) {
    const idx = this.state.employees.findIndex(e => e.id === item.id && e.businessId === item.businessId);
    if (idx >= 0) {
      this.state.employees[idx] = { ...item, updatedAt: new Date().toISOString(), syncStatus: SyncStatus.PendingUpdate };
      this.enqueueSync(item.businessId, 'employees', item.id, SyncOperationType.UPDATE, this.state.employees[idx]);
      this.saveToStorage();
    }
  }

  public deleteEmployee(businessId: string, id: string) {
    const idx = this.state.employees.findIndex(e => e.id === id && e.businessId === businessId);
    if (idx >= 0) {
      this.state.employees[idx].isDeleted = true;
      this.state.employees[idx].updatedAt = new Date().toISOString();
      this.state.employees[idx].syncStatus = SyncStatus.PendingDelete;
      this.enqueueSync(businessId, 'employees', id, SyncOperationType.DELETE, { id });
      this.saveToStorage();
    }
  }

  // Account Heads
  public getAccountHeads(businessId: string): AccountHead[] {
    return this.state.accountHeads.filter(a => a.businessId === businessId && !a.isDeleted);
  }

  public insertAccountHead(item: AccountHead) {
    this.state.accountHeads.push(item);
    this.enqueueSync(item.businessId, 'accountHeads', item.id, SyncOperationType.CREATE, item);
    this.saveToStorage();
  }

  // Vouchers
  public getVouchers(businessId: string): Voucher[] {
    return this.state.vouchers.filter(v => v.businessId === businessId && !v.isDeleted);
  }

  public insertVoucher(voucher: Voucher, entries: VoucherEntry[]) {
    this.state.vouchers.push(voucher);
    this.state.voucherEntries.push(...entries);
    this.enqueueSync(voucher.businessId, 'vouchers', voucher.id, SyncOperationType.CREATE, { voucher, entries });
    this.saveToStorage();
  }

  // Sales
  public getSales(businessId: string): Sale[] {
    return this.state.sales.filter(s => s.businessId === businessId && !s.isDeleted);
  }

  public insertSale(sale: Sale, items: SaleItem[]) {
    this.state.sales.push(sale);
    this.state.saleItems.push(...items);
    this.enqueueSync(sale.businessId, 'sales', sale.id, SyncOperationType.CREATE, { sale, items });
    this.saveToStorage();
  }

  // Purchases
  public getPurchases(businessId: string): Purchase[] {
    return this.state.purchases.filter(p => p.businessId === businessId && !p.isDeleted);
  }

  public insertPurchase(purchase: Purchase, items: PurchaseItem[]) {
    this.state.purchases.push(purchase);
    this.state.purchaseItems.push(...items);
    this.enqueueSync(purchase.businessId, 'purchases', purchase.id, SyncOperationType.CREATE, { purchase, items });
    this.saveToStorage();
  }

  // Expenses
  public getExpenses(businessId: string): Expense[] {
    return this.state.expenses.filter(e => e.businessId === businessId && !e.isDeleted);
  }

  public insertExpense(item: Expense) {
    this.state.expenses.push(item);
    this.enqueueSync(item.businessId, 'expenses', item.id, SyncOperationType.CREATE, item);
    this.saveToStorage();
  }

  // Bank Accounts
  public getBankAccounts(businessId: string): BankAccount[] {
    return this.state.bankAccounts.filter(b => b.businessId === businessId && !b.isDeleted);
  }

  public insertBankAccount(item: BankAccount) {
    this.state.bankAccounts.push(item);
    this.enqueueSync(item.businessId, 'bankAccounts', item.id, SyncOperationType.CREATE, item);
    this.saveToStorage();
  }

  // Sync Queue
  public getSyncQueue(businessId: string): SyncQueueItem[] {
    return this.state.syncQueue.filter(q => q.businessId === businessId);
  }

  public removeSyncQueueItem(id: string) {
    this.state.syncQueue = this.state.syncQueue.filter(q => q.id !== id);
    this.saveToStorage();
  }

  public markSyncQueueFailed(id: string, errorMsg: string) {
    const item = this.state.syncQueue.find(q => q.id === id);
    if (item) {
      item.retryCount += 1;
      item.lastAttemptAt = new Date().toISOString();
      item.lastError = errorMsg;
      this.saveToStorage();
    }
  }

  // Local Backups
  public getBackups(businessId: string): LocalBackupRecord[] {
    return this.state.backups.filter(b => b.businessId === businessId);
  }

  public insertBackupRecord(record: LocalBackupRecord) {
    this.state.backups.push(record);
    this.saveToStorage();
  }

  // Restore State
  public restoreState(newState: DatabaseState) {
    this.state = {
      ...initialDbState,
      ...newState,
      version: DB_VERSION,
    };
    this.saveToStorage();
  }

  // Reset / Clear (for testing or hard reset)
  public resetDatabase() {
    this.state = { ...initialDbState };
    this.saveToStorage();
  }
}

// Global Singleton Instance
export const localDb = new LocalDatabase();
