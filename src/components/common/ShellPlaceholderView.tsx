import React from 'react';
import {
  Boxes,
  Users,
  Briefcase,
  ShoppingCart,
  Receipt,
  Truck,
  Building,
  FileSpreadsheet,
  BookOpen,
  BadgeDollarSign,
  Landmark,
  CreditCard,
  BarChart3,
  TrendingUp,
  Settings,
  Plus,
  Search,
  Filter,
} from 'lucide-react';
import { DEFAULT_CURRENCY_SYMBOL } from '../../domain/constants';

interface ShellPlaceholderViewProps {
  module: string;
  darkMode: boolean;
}

const moduleConfigs: Record<string, { title: string; category: string; icon: React.ElementType; phase: number; description: string }> = {
  'inventory-products': {
    title: 'Products & Inventory Catalog',
    category: 'INVENTORY',
    icon: Boxes,
    phase: 2,
    description: 'Product catalog with SKU, barcode, unit, cost price, sale price, GST rate, and stock alerts.',
  },
  'inventory-categories': {
    title: 'Product Categories',
    category: 'INVENTORY',
    icon: Boxes,
    phase: 2,
    description: 'Categorization and hierarchy for inventory items.',
  },
  'sales-list': {
    title: 'Point of Sale (POS) & Sales History',
    category: 'SALES & BILLING',
    icon: ShoppingCart,
    phase: 2,
    description: 'Point-of-sale checkout terminal, payment methods (Cash/Bank/Credit), and sale receipts.',
  },
  'sales-invoices': {
    title: 'Signix World Tax Invoices',
    category: 'SALES & BILLING',
    icon: Receipt,
    phase: 2,
    description: 'Pakistani FBR tax invoice generation with NTN/STRN, itemized GST, customer ledgers, and printing.',
  },
  'purchases-list': {
    title: 'Supplier Purchases',
    category: 'PURCHASES',
    icon: Truck,
    phase: 2,
    description: 'Supplier purchase bills, stock receiving, cost tracking, and payable ledgers.',
  },
  'customers-list': {
    title: 'Customers Management (Separate Entity)',
    category: 'PARTIES',
    icon: Users,
    phase: 2,
    description: 'Customer profiles, CNIC, NTN, credit limits, opening balance, and transaction history.',
  },
  'employees-list': {
    title: 'Employees Management (Separate Entity)',
    category: 'PARTIES',
    icon: Briefcase,
    phase: 2,
    description: 'Employee profiles, designations, monthly salaries, advances, and attendance (strictly isolated from customers).',
  },
  'accounting-heads': {
    title: 'Account Head (Chart of Accounts)',
    category: 'ACCOUNTING',
    icon: Building,
    phase: 3,
    description: 'Replaced legacy Acceptance Report. Standard 5-category chart of accounts: Assets, Liabilities, Equity, Revenue, Expenses.',
  },
  'accounting-vouchers': {
    title: 'Financial Vouchers (CRV, CPV, BRV, BPV, JV)',
    category: 'ACCOUNTING',
    icon: FileSpreadsheet,
    phase: 3,
    description: 'Double-entry voucher posting with automatic balance verification.',
  },
  'accounting-ledger': {
    title: 'General Ledger',
    category: 'ACCOUNTING',
    icon: BookOpen,
    phase: 3,
    description: 'Account-wise debit and credit statement with running balances.',
  },
  'accounting-cashbook': {
    title: 'Daily Cash Book',
    category: 'ACCOUNTING',
    icon: BadgeDollarSign,
    phase: 3,
    description: 'Daily cash in/out register and physical cash in hand reconciliation.',
  },
  'accounting-banking': {
    title: 'Bank Details & Summary',
    category: 'ACCOUNTING',
    icon: Landmark,
    phase: 3,
    description: 'Bank accounts, cheque book management, deposit and withdrawal tracking.',
  },
  'reports-client-position': {
    title: 'Client Position Report',
    category: 'REPORTS',
    icon: CreditCard,
    phase: 3,
    description: 'Customer receivables aging, total invoiced debits, total received credits, and net balances.',
  },
  'reports-monthly-position': {
    title: 'Monthly Position Report',
    category: 'REPORTS',
    icon: BarChart3,
    phase: 3,
    description: 'Comprehensive monthly business assets, payables, receivables, and liquidity balance sheet.',
  },
  'reports-monthly-pnl': {
    title: 'Monthly Profit & Loss Statement',
    category: 'REPORTS',
    icon: TrendingUp,
    phase: 3,
    description: 'Revenue − Cost of Goods Sold = Gross Profit − Operating Expenses = Net Profit.',
  },
  'settings-profile': {
    title: 'Business Profile & Tax Settings',
    category: 'SETTINGS',
    icon: Settings,
    phase: 1,
    description: 'Business name, NTN, STRN, address, owner contact, and currency (PKR).',
  },
};

export const ShellPlaceholderView: React.FC<ShellPlaceholderViewProps> = ({ module, darkMode }) => {
  const config = moduleConfigs[module] || {
    title: 'Business Module',
    category: 'SYSTEM',
    icon: Boxes,
    phase: 2,
    description: 'Module ready for next development phase.',
  };
  const Icon = config.icon;

  return (
    <div className="p-6 space-y-6 max-w-7xl mx-auto">
      {/* Breadcrumbs & Title */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4 pb-4 border-b border-neutral-200 dark:border-neutral-800">
        <div>
          <div className="text-[10px] font-bold text-emerald-600 dark:text-emerald-400 uppercase tracking-wider mb-1">
            {config.category}
          </div>
          <div className="flex items-center gap-3">
            <div className="p-2 rounded-lg bg-emerald-500/10 text-emerald-600 dark:text-emerald-400">
              <Icon className="w-5 h-5" />
            </div>
            <div>
              <h1 className="text-xl font-bold text-neutral-900 dark:text-neutral-100">
                {config.title}
              </h1>
              <p className="text-xs text-neutral-500 dark:text-neutral-400">
                {config.description}
              </p>
            </div>
          </div>
        </div>

        <div className="flex items-center gap-2">
          <span className="text-xs px-2.5 py-1 rounded-full bg-neutral-200 dark:bg-neutral-800 text-neutral-700 dark:text-neutral-300 font-medium">
            Scheduled for Phase {config.phase}
          </span>
        </div>
      </div>

      {/* High-density Desktop Table Header & Actions Bar */}
      <div
        className={`p-4 rounded-xl border flex flex-col sm:flex-row items-center justify-between gap-3 ${
          darkMode ? 'bg-neutral-900 border-neutral-800' : 'bg-white border-neutral-200'
        }`}
      >
        <div className="flex items-center gap-2 w-full sm:w-auto">
          <div className="relative flex-1 sm:w-64">
            <Search className="w-4 h-4 absolute left-3 top-1/2 -translate-y-1/2 text-neutral-400" />
            <input
              type="text"
              placeholder={`Search ${config.title}...`}
              className="w-full pl-9 pr-3 py-1.5 bg-neutral-100 dark:bg-neutral-800 border border-neutral-300 dark:border-neutral-700 rounded-lg text-xs outline-none focus:border-emerald-500"
            />
          </div>
          <button className="flex items-center gap-1 px-3 py-1.5 rounded-lg border border-neutral-300 dark:border-neutral-700 text-xs font-medium hover:bg-neutral-100 dark:hover:bg-neutral-800">
            <Filter className="w-3.5 h-3.5" />
            <span>Filter</span>
          </button>
        </div>

        <button className="w-full sm:w-auto flex items-center justify-center gap-1.5 px-3 py-1.5 bg-emerald-600 hover:bg-emerald-500 text-white text-xs font-semibold rounded-lg shadow-xs transition-colors">
          <Plus className="w-4 h-4" />
          <span>Add New Record</span>
        </button>
      </div>

      {/* Clean Zero-Data Table Placeholder */}
      <div
        className={`rounded-xl border overflow-hidden p-12 text-center ${
          darkMode ? 'bg-neutral-900 border-neutral-800' : 'bg-white border-neutral-200'
        }`}
      >
        <div className="w-12 h-12 rounded-full bg-neutral-100 dark:bg-neutral-800 flex items-center justify-center text-neutral-400 mx-auto mb-3">
          <Icon className="w-6 h-6" />
        </div>
        <h3 className="text-sm font-bold text-neutral-900 dark:text-neutral-100 mb-1">
          No records found in local SQLite database
        </h3>
        <p className="text-xs text-neutral-500 max-w-md mx-auto mb-4">
          All schema entities and repositories for {config.title} are fully defined in the Phase 1 architecture.
        </p>
        <div className="inline-flex items-center gap-2 text-[11px] font-mono bg-neutral-100 dark:bg-neutral-800 px-3 py-1 rounded text-neutral-600 dark:text-neutral-400">
          <span>Tenant Isolation: BusinessId Verified</span>
          <span>•</span>
          <span>Currency: {DEFAULT_CURRENCY_SYMBOL} (PKR)</span>
        </div>
      </div>
    </div>
  );
};
