/**
 * Modern Inventory Management System - Core Domain Types & Enums
 * Source of Truth: Android Modern Inventory Management System
 */

export enum SyncStatus {
  Synced = 'Synced',
  PendingCreate = 'PendingCreate',
  PendingUpdate = 'PendingUpdate',
  PendingDelete = 'PendingDelete',
  SyncFailed = 'SyncFailed',
}

export enum SyncOperationType {
  CREATE = 'CREATE',
  UPDATE = 'UPDATE',
  DELETE = 'DELETE',
}

export enum AccountType {
  ASSET = 'ASSET',
  LIABILITY = 'LIABILITY',
  EQUITY = 'EQUITY',
  REVENUE = 'REVENUE',
  EXPENSE = 'EXPENSE',
}

export enum VoucherType {
  CRV = 'CRV', // Cash Receipt Voucher
  CPV = 'CPV', // Cash Payment Voucher
  BRV = 'BRV', // Bank Receipt Voucher
  BPV = 'BPV', // Bank Payment Voucher
  JV = 'JV',   // Journal Voucher
}

export enum PaymentMethod {
  CASH = 'CASH',
  BANK = 'BANK',
  CREDIT = 'CREDIT',
  SPLIT = 'SPLIT',
}

export enum StockAdjustmentType {
  ADDITION = 'ADDITION',
  DAMAGE = 'DAMAGE',
  THEFT = 'THEFT',
  EXPIRED = 'EXPIRED',
  AUDIT_CORRECTION = 'AUDIT_CORRECTION',
}

export enum PartyType {
  CUSTOMER = 'CUSTOMER',
  EMPLOYEE = 'EMPLOYEE',
  SUPPLIER = 'SUPPLIER',
  NONE = 'NONE',
}

// Base Entity with Multi-Tenant and Sync metadata
export interface BaseEntity {
  id: string;
  businessId: string;
  createdAt: string; // ISO 8601 UTC string
  updatedAt: string; // ISO 8601 UTC string
  isDeleted: boolean;
  syncStatus: SyncStatus;
}

// 1. Business Profile & User
export interface BusinessProfile {
  id: string;
  businessName: string;
  ownerName: string;
  email: string;
  phone: string;
  address: string;
  ntnNumber: string; // National Tax Number
  strnNumber: string; // Sales Tax Registration Number
  currency: string; // PKR
  createdAt: string;
  updatedAt: string;
}

export interface SecurityQuestion {
  question: string;
  answerHash: string;
  salt: string;
}

export interface User {
  id: string;
  businessId: string;
  email: string;
  fullName: string;
  phone: string;
  passwordHash: string;
  salt: string;
  securityQuestions: SecurityQuestion[];
  createdAt: string;
  updatedAt: string;
}

export interface UserSession {
  sessionId: string;
  userId: string;
  businessId: string;
  email: string;
  businessName: string;
  token: string;
  createdAt: string;
  expiresAt: string; // 30-day requirement
  rememberMe: boolean;
}

// 2. Inventory Entities
export interface Category extends BaseEntity {
  name: string;
  description: string;
}

export interface Product extends BaseEntity {
  categoryId: string;
  categoryName?: string;
  sku: string;
  barcode: string;
  name: string;
  unit: string; // Pcs, Box, Kg, Liter, Meter, etc.
  costPrice: number; // decimal in C#
  salePrice: number; // decimal in C#
  stockQuantity: number;
  minStockAlert: number;
  taxRate: number; // e.g. 18% GST
}

export interface StockAdjustment extends BaseEntity {
  productId: string;
  productName?: string;
  adjustmentType: StockAdjustmentType;
  quantityDelta: number;
  costPerUnit: number;
  reason: string;
  adjustmentDate: string;
}

// 3. Customer Entity (Strict separation from Employee)
export interface Customer extends BaseEntity {
  name: string;
  companyName: string;
  phone: string;
  email: string;
  cnic: string; // Pakistani CNIC
  ntn: string;
  address: string;
  creditLimit: number;
  openingBalance: number;
  currentBalance: number;
}

// 4. Employee Entity (Strict separation from Customer)
export interface Employee extends BaseEntity {
  name: string;
  designation: string;
  cnic: string;
  phone: string;
  address: string;
  monthlySalary: number;
  joiningDate: string;
  advanceBalance: number;
  isActive: boolean;
}

// 5. Sales & Tax Invoices
export interface SaleItem {
  id: string;
  saleId: string;
  productId: string;
  productName: string;
  unit: string;
  quantity: number;
  unitPrice: number;
  unitCost: number;
  discount: number;
  taxRate: number;
  taxAmount: number;
  totalPrice: number;
}

export interface Sale extends BaseEntity {
  invoiceNumber: string; // Signix World Tax Invoice Format
  customerId: string;
  customerName?: string;
  saleDate: string;
  subtotal: number;
  discountAmount: number;
  taxAmount: number;
  grandTotal: number;
  paidAmount: number;
  dueBalance: number;
  paymentMethod: PaymentMethod;
  notes: string;
  items?: SaleItem[];
}

// 6. Purchases
export interface PurchaseItem {
  id: string;
  purchaseId: string;
  productId: string;
  productName: string;
  quantity: number;
  unitCost: number;
  totalCost: number;
}

export interface Purchase extends BaseEntity {
  billNumber: string;
  supplierName: string;
  supplierPhone: string;
  purchaseDate: string;
  subtotal: number;
  taxAmount: number;
  grandTotal: number;
  paidAmount: number;
  dueBalance: number;
  paymentMethod: PaymentMethod;
  items?: PurchaseItem[];
}

// 7. Accounting
export interface AccountHead extends BaseEntity {
  code: string;
  name: string;
  type: AccountType;
  isSystem: boolean;
  description: string;
}

export interface VoucherEntry {
  id: string;
  voucherId: string;
  accountHeadId: string;
  accountHeadName?: string;
  debitAmount: number;
  creditAmount: number;
  partyType: PartyType;
  partyId?: string;
  notes?: string;
}

export interface Voucher extends BaseEntity {
  voucherNumber: string;
  voucherType: VoucherType;
  date: string;
  totalAmount: number;
  referenceType: string;
  referenceId?: string;
  narration: string;
  entries?: VoucherEntry[];
}

export interface Expense extends BaseEntity {
  accountHeadId: string;
  accountHeadName?: string;
  title: string;
  category: string;
  amount: number;
  paymentMethod: PaymentMethod;
  expenseDate: string;
  paidTo: string;
  receiptRef: string;
  notes: string;
}

// 8. Banking
export interface BankAccount extends BaseEntity {
  bankName: string;
  accountTitle: string;
  accountNumber: string;
  iban: string;
  branchCode: string;
  currentBalance: number;
  chequeBookRef: string;
}

export interface BankTransaction extends BaseEntity {
  bankAccountId: string;
  transactionType: 'DEPOSIT' | 'WITHDRAWAL' | 'TRANSFER' | 'CHEQUE';
  amount: number;
  transactionDate: string;
  referenceNumber: string;
  description: string;
  status: 'CLEARED' | 'UNCLEARED' | 'BOUNCED';
}

// 9. Sync Queue Entity
export interface SyncQueueItem {
  id: string;
  businessId: string;
  entityType: string;
  entityId: string;
  operationType: SyncOperationType;
  payloadJson: string;
  retryCount: number;
  lastAttemptAt?: string;
  lastError?: string;
  createdAt: string;
}

// 10. Local Backup Record
export interface LocalBackupRecord {
  id: string;
  fileName: string;
  businessId: string;
  createdAt: string;
  fileSizeBytes: number;
  entityCounts: Record<string, number>;
  checksum: string;
}
