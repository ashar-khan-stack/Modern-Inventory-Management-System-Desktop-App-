import React, { useState, useEffect } from 'react';
import {
  TrendingUp,
  Boxes,
  Truck,
  CreditCard,
  Building,
  BadgeDollarSign,
  AlertTriangle,
  ArrowUpRight,
  ArrowDownRight,
  PlusCircle,
  Receipt,
  ShoppingCart,
  Database,
  Calendar,
} from 'lucide-react';
import { useCurrentUser } from '../../infrastructure/context/CurrentUserContext';
import { localDb } from '../../infrastructure/storage/localDatabase';
import { DEFAULT_CURRENCY_SYMBOL } from '../../domain/constants';

interface DashboardShellViewProps {
  darkMode: boolean;
  onNavigate: (module: string) => void;
}

export const DashboardShellView: React.FC<DashboardShellViewProps> = ({ darkMode, onNavigate }) => {
  const { business } = useCurrentUser();
  const [metrics, setMetrics] = useState({
    totalSales: 0,
    totalPurchases: 0,
    stockValuation: 0,
    totalReceivables: 0,
    totalPayables: 0,
    totalExpenses: 0,
    netProfit: 0,
    lowStockCount: 0,
    productCount: 0,
    customerCount: 0,
    employeeCount: 0,
  });

  const loadRealMetrics = () => {
    if (!business) return;
    const bId = business.id;
    const products = localDb.getProducts(bId);
    const sales = localDb.getSales(bId);
    const purchases = localDb.getPurchases(bId);
    const expenses = localDb.getExpenses(bId);
    const customers = localDb.getCustomers(bId);
    const employees = localDb.getEmployees(bId);

    const totalSales = sales.reduce((acc, s) => acc + (s.grandTotal || 0), 0);
    const totalPurchases = purchases.reduce((acc, p) => acc + (p.grandTotal || 0), 0);
    const totalExpenses = expenses.reduce((acc, e) => acc + (e.amount || 0), 0);

    const stockValuation = products.reduce((acc, p) => acc + ((p.stockQuantity || 0) * (p.costPrice || 0)), 0);
    const lowStockCount = products.filter(p => p.stockQuantity <= p.minStockAlert).length;

    const totalReceivables = customers.reduce((acc, c) => acc + Math.max(0, c.currentBalance || 0), 0);
    const totalPayables = purchases.reduce((acc, p) => acc + (p.dueBalance || 0), 0);

    // Business Net P&L Formula: Gross Sales - Purchases - Expenses
    const netProfit = totalSales - totalPurchases - totalExpenses;

    setMetrics({
      totalSales,
      totalPurchases,
      stockValuation,
      totalReceivables,
      totalPayables,
      totalExpenses,
      netProfit,
      lowStockCount,
      productCount: products.length,
      customerCount: customers.length,
      employeeCount: employees.length,
    });
  };

  useEffect(() => {
    loadRealMetrics();
    const unsub = localDb.subscribe(loadRealMetrics);
    return () => unsub();
  }, [business]);

  const cards = [
    {
      title: 'Total Sales (PKR)',
      value: `${DEFAULT_CURRENCY_SYMBOL} ${metrics.totalSales.toLocaleString('en-PK', { minimumFractionDigits: 2 })}`,
      subtitle: 'Net invoiced revenue',
      icon: TrendingUp,
      color: 'text-emerald-500',
      bgColor: 'bg-emerald-500/10',
      borderColor: 'border-emerald-500/20',
      actionModule: 'sales-list',
    },
    {
      title: 'Total Purchases',
      value: `${DEFAULT_CURRENCY_SYMBOL} ${metrics.totalPurchases.toLocaleString('en-PK', { minimumFractionDigits: 2 })}`,
      subtitle: 'Supplier goods received',
      icon: Truck,
      color: 'text-blue-500',
      bgColor: 'bg-blue-500/10',
      borderColor: 'border-blue-500/20',
      actionModule: 'purchases-list',
    },
    {
      title: 'Stock Valuation (Cost)',
      value: `${DEFAULT_CURRENCY_SYMBOL} ${metrics.stockValuation.toLocaleString('en-PK', { minimumFractionDigits: 2 })}`,
      subtitle: `${metrics.productCount} active catalog items`,
      icon: Boxes,
      color: 'text-violet-500',
      bgColor: 'bg-violet-500/10',
      borderColor: 'border-violet-500/20',
      actionModule: 'inventory-products',
    },
    {
      title: 'Client Receivables',
      value: `${DEFAULT_CURRENCY_SYMBOL} ${metrics.totalReceivables.toLocaleString('en-PK', { minimumFractionDigits: 2 })}`,
      subtitle: `${metrics.customerCount} registered clients`,
      icon: CreditCard,
      color: 'text-amber-500',
      bgColor: 'bg-amber-500/10',
      borderColor: 'border-amber-500/20',
      actionModule: 'reports-client-position',
    },
    {
      title: 'Supplier Payables',
      value: `${DEFAULT_CURRENCY_SYMBOL} ${metrics.totalPayables.toLocaleString('en-PK', { minimumFractionDigits: 2 })}`,
      subtitle: 'Outstanding purchase bills',
      icon: Building,
      color: 'text-rose-500',
      bgColor: 'bg-rose-500/10',
      borderColor: 'border-rose-500/20',
      actionModule: 'purchases-list',
    },
    {
      title: 'Operating Expenses',
      value: `${DEFAULT_CURRENCY_SYMBOL} ${metrics.totalExpenses.toLocaleString('en-PK', { minimumFractionDigits: 2 })}`,
      subtitle: 'Utilities, rent, stationary',
      icon: BadgeDollarSign,
      color: 'text-orange-500',
      bgColor: 'bg-orange-500/10',
      borderColor: 'border-orange-500/20',
      actionModule: 'accounting-vouchers',
    },
  ];

  return (
    <div className="p-6 space-y-6 max-w-7xl mx-auto">
      {/* Top Banner */}
      <div className="flex flex-col md:flex-row md:items-center justify-between gap-4 pb-4 border-b border-neutral-200 dark:border-neutral-800">
        <div>
          <div className="flex items-center gap-2">
            <h1 className="text-xl font-bold text-neutral-900 dark:text-neutral-100">
              {business?.businessName}
            </h1>
            <span className="text-xs px-2 py-0.5 rounded bg-emerald-100 text-emerald-800 dark:bg-emerald-950 dark:text-emerald-300 font-medium">
              Offline-First SQLite Active
            </span>
          </div>
          <p className="text-xs text-neutral-500 dark:text-neutral-400 mt-1">
            Real-time business financial position • Currency: Pakistani Rupee (PKR)
          </p>
        </div>

        <div className="flex items-center gap-2">
          <button
            id="btn-quick-new-sale"
            onClick={() => onNavigate('sales-list')}
            className="flex items-center gap-1.5 px-3 py-2 bg-emerald-600 hover:bg-emerald-500 text-white text-xs font-semibold rounded-lg shadow-xs transition-colors"
          >
            <ShoppingCart className="w-4 h-4" />
            <span>Point of Sale (POS)</span>
          </button>

          <button
            id="btn-quick-new-invoice"
            onClick={() => onNavigate('sales-invoices')}
            className="flex items-center gap-1.5 px-3 py-2 bg-neutral-800 hover:bg-neutral-700 text-neutral-100 text-xs font-semibold rounded-lg shadow-xs transition-colors"
          >
            <Receipt className="w-4 h-4" />
            <span>Signix Tax Invoice</span>
          </button>
        </div>
      </div>

      {/* Metrics Cards Grid */}
      <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-4">
        {cards.map((card, idx) => {
          const Icon = card.icon;
          return (
            <div
              key={idx}
              onClick={() => onNavigate(card.actionModule)}
              className={`p-4 rounded-xl border transition-all cursor-pointer hover:shadow-md ${card.bgColor} ${card.borderColor} ${
                darkMode ? 'bg-neutral-900/40 hover:bg-neutral-900/80' : 'bg-white hover:bg-neutral-50'
              }`}
            >
              <div className="flex items-center justify-between mb-3">
                <span className="text-xs font-medium text-neutral-600 dark:text-neutral-400">
                  {card.title}
                </span>
                <div className={`p-2 rounded-lg ${card.bgColor} ${card.color}`}>
                  <Icon className="w-4 h-4" />
                </div>
              </div>
              <div className="text-lg font-bold text-neutral-900 dark:text-neutral-100 mb-1">
                {card.value}
              </div>
              <div className="text-[11px] text-neutral-500 flex items-center justify-between">
                <span>{card.subtitle}</span>
                <ArrowUpRight className="w-3.5 h-3.5 text-neutral-400" />
              </div>
            </div>
          );
        })}
      </div>

      {/* Financial Net P&L Summary Bar */}
      <div
        className={`p-5 rounded-xl border flex flex-col md:flex-row items-start md:items-center justify-between gap-4 ${
          darkMode ? 'bg-neutral-900 border-neutral-800' : 'bg-white border-neutral-200'
        }`}
      >
        <div className="flex items-center gap-3.5">
          <div
            className={`w-10 h-10 rounded-xl flex items-center justify-center font-bold text-lg ${
              metrics.netProfit >= 0
                ? 'bg-emerald-500/20 text-emerald-600 dark:text-emerald-400'
                : 'bg-red-500/20 text-red-600 dark:text-red-400'
            }`}
          >
            {metrics.netProfit >= 0 ? <ArrowUpRight className="w-6 h-6" /> : <ArrowDownRight className="w-6 h-6" />}
          </div>
          <div>
            <div className="text-xs font-semibold text-neutral-500 dark:text-neutral-400">
              Current Net Profit / (Loss)
            </div>
            <div
              className={`text-xl font-bold tracking-tight ${
                metrics.netProfit >= 0 ? 'text-emerald-600 dark:text-emerald-400' : 'text-red-600 dark:text-red-400'
              }`}
            >
              {DEFAULT_CURRENCY_SYMBOL} {metrics.netProfit.toLocaleString('en-PK', { minimumFractionDigits: 2 })}
            </div>
          </div>
        </div>

        <div className="flex items-center gap-4 text-xs text-neutral-500 dark:text-neutral-400">
          <div className="text-right">
            <div>Calculation Formula</div>
            <div className="font-mono text-[11px] text-neutral-400">Sales - Purchases - Expenses</div>
          </div>
          <button
            id="btn-view-monthly-pnl"
            onClick={() => onNavigate('reports-monthly-pnl')}
            className="px-3 py-1.5 rounded-lg border border-neutral-300 dark:border-neutral-700 hover:bg-neutral-100 dark:hover:bg-neutral-800 font-medium text-neutral-800 dark:text-neutral-200 transition-colors"
          >
            View Monthly P&L
          </button>
        </div>
      </div>

      {/* Real Low Stock / Empty Alert Section */}
      <div
        className={`p-5 rounded-xl border ${
          darkMode ? 'bg-neutral-900 border-neutral-800' : 'bg-white border-neutral-200'
        }`}
      >
        <div className="flex items-center justify-between pb-3 border-b border-neutral-200 dark:border-neutral-800 mb-4">
          <div className="flex items-center gap-2">
            <AlertTriangle className="w-4 h-4 text-amber-500" />
            <h2 className="text-xs font-bold uppercase tracking-wider text-neutral-900 dark:text-neutral-100">
              Low Stock Alerts & Reorder Status ({metrics.lowStockCount})
            </h2>
          </div>
          <button
            id="btn-manage-stock"
            onClick={() => onNavigate('inventory-products')}
            className="text-xs text-emerald-600 hover:text-emerald-500 font-medium"
          >
            Manage Inventory Catalog →
          </button>
        </div>

        {metrics.lowStockCount === 0 ? (
          <div className="text-center py-8 text-neutral-400 text-xs">
            <p className="font-medium text-neutral-500 dark:text-neutral-400">
              {metrics.productCount === 0
                ? 'No inventory items recorded yet in local SQLite database.'
                : 'All inventory stock levels are currently above reorder thresholds.'}
            </p>
            <p className="text-[11px] text-neutral-400 mt-1">
              Add products in Phase 2 to track real-time stock deductions and alerts.
            </p>
          </div>
        ) : (
          <div className="text-xs text-neutral-400">
            {metrics.lowStockCount} items require reordering.
          </div>
        )}
      </div>
    </div>
  );
};
