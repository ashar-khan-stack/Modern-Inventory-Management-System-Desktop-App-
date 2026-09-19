/**
 * Default Chart of Accounts and Business Constants
 * Compatible with Pakistani tax & accounting standard
 */
import { AccountType } from '../types';

export const DEFAULT_CURRENCY = 'PKR';
export const DEFAULT_CURRENCY_SYMBOL = 'Rs.';
export const DEFAULT_TAX_RATE = 18.0; // Standard Pakistani GST

export interface DefaultAccountHeadDefinition {
  code: string;
  name: string;
  type: AccountType;
  description: string;
}

export const SYSTEM_ACCOUNT_HEADS: DefaultAccountHeadDefinition[] = [
  // Assets
  { code: '1010', name: 'Cash in Hand', type: AccountType.ASSET, description: 'Physical cash available in business counter' },
  { code: '1020', name: 'Bank Accounts', type: AccountType.ASSET, description: 'Commercial bank checking and savings' },
  { code: '1030', name: 'Accounts Receivable (Customers)', type: AccountType.ASSET, description: 'Outstanding client credit balances' },
  { code: '1040', name: 'Inventory Stock', type: AccountType.ASSET, description: 'Merchandise inventory at cost' },
  { code: '1050', name: 'Advance to Employees', type: AccountType.ASSET, description: 'Salary advances given to staff' },

  // Liabilities
  { code: '2010', name: 'Accounts Payable (Suppliers)', type: AccountType.LIABILITY, description: 'Outstanding vendor purchase bills' },
  { code: '2020', name: 'Sales Tax / GST Payable', type: AccountType.LIABILITY, description: 'Output GST collected for FBR payment' },
  { code: '2030', name: 'Accrued Salaries', type: AccountType.LIABILITY, description: 'Employee salaries earned but unpaid' },

  // Equity
  { code: '3010', name: 'Owner Capital / Equity', type: AccountType.EQUITY, description: 'Initial and invested business capital' },
  { code: '3020', name: 'Retained Earnings', type: AccountType.EQUITY, description: 'Accumulated business profits and reserves' },

  // Revenue
  { code: '4010', name: 'Sales Revenue', type: AccountType.REVENUE, description: 'Revenue from merchandise sales' },
  { code: '4020', name: 'Sales Discounts Allowed', type: AccountType.REVENUE, description: 'Discounts granted on invoices' },
  { code: '4030', name: 'Other Income', type: AccountType.REVENUE, description: 'Miscellaneous business income' },

  // Expenses
  { code: '5010', name: 'Cost of Goods Sold (COGS)', type: AccountType.EXPENSE, description: 'Direct purchase cost of goods sold' },
  { code: '5020', name: 'Salaries & Wages', type: AccountType.EXPENSE, description: 'Monthly employee compensation' },
  { code: '5030', name: 'Shop / Office Rent', type: AccountType.EXPENSE, description: 'Premises lease and rental' },
  { code: '5040', name: 'Utilities (Electricity/Gas/Water)', type: AccountType.EXPENSE, description: 'Commercial utility bills' },
  { code: '5050', name: 'Printing & Stationary', type: AccountType.EXPENSE, description: 'Tax invoices, vouchers, and office supplies' },
  { code: '5060', name: 'Transportation & Carriage', type: AccountType.EXPENSE, description: 'Freight and delivery expenses' },
  { code: '5070', name: 'Damaged & Lost Stock', type: AccountType.EXPENSE, description: 'Inventory write-offs due to damage or theft' },
  { code: '5080', name: 'General Maintenance & Repairs', type: AccountType.EXPENSE, description: 'Equipment and premise repairs' },
];

export const STANDARD_SECURITY_QUESTIONS = [
  'What is the name of your first school?',
  'What was your childhood nickname?',
  'In what city was your first job located?',
  'What is your mother’s maiden name?',
  'What was the model of your first vehicle?',
  'What is your favorite book or author?',
];
