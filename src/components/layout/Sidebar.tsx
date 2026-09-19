import React from 'react';
import {
  LayoutDashboard,
  Boxes,
  ShoppingCart,
  Receipt,
  Truck,
  Users,
  Briefcase,
  BookOpen,
  FileSpreadsheet,
  FileText,
  BadgeDollarSign,
  TrendingUp,
  Settings,
  Database,
  Building,
  CreditCard,
  Layers,
  BarChart3,
  Landmark,
} from 'lucide-react';

interface SidebarProps {
  currentModule: string;
  onSelectModule: (module: string) => void;
  darkMode: boolean;
}

interface NavSection {
  title: string;
  items: {
    id: string;
    label: string;
    icon: React.ElementType;
    badge?: string;
  }[];
}

export const Sidebar: React.FC<SidebarProps> = ({ currentModule, onSelectModule, darkMode }) => {
  const sections: NavSection[] = [
    {
      title: 'OVERVIEW',
      items: [
        { id: 'dashboard', label: 'Dashboard', icon: LayoutDashboard },
      ],
    },
    {
      title: 'INVENTORY',
      items: [
        { id: 'inventory-products', label: 'Products & Stock', icon: Boxes },
        { id: 'inventory-categories', label: 'Categories', icon: Layers },
      ],
    },
    {
      title: 'SALES & BILLING',
      items: [
        { id: 'sales-list', label: 'Sales & POS', icon: ShoppingCart },
        { id: 'sales-invoices', label: 'Signix Tax Invoices', icon: Receipt },
      ],
    },
    {
      title: 'PURCHASES',
      items: [
        { id: 'purchases-list', label: 'Supplier Purchases', icon: Truck },
      ],
    },
    {
      title: 'PARTIES (SEPARATE)',
      items: [
        { id: 'customers-list', label: 'Customers', icon: Users },
        { id: 'employees-list', label: 'Employees', icon: Briefcase },
      ],
    },
    {
      title: 'ACCOUNTING',
      items: [
        { id: 'accounting-heads', label: 'Account Head', icon: Building },
        { id: 'accounting-vouchers', label: 'Vouchers (CRV/CPV/JV)', icon: FileSpreadsheet },
        { id: 'accounting-ledger', label: 'General Ledger', icon: BookOpen },
        { id: 'accounting-cashbook', label: 'Cash Book', icon: BadgeDollarSign },
        { id: 'accounting-banking', label: 'Bank Details & Summary', icon: Landmark },
      ],
    },
    {
      title: 'FINANCIAL REPORTS',
      items: [
        { id: 'reports-client-position', label: 'Client Position', icon: CreditCard },
        { id: 'reports-monthly-position', label: 'Monthly Position', icon: BarChart3 },
        { id: 'reports-monthly-pnl', label: 'Monthly Profit & Loss', icon: TrendingUp },
      ],
    },
    {
      title: 'SETTINGS & BACKUP',
      items: [
        { id: 'settings-backup', label: 'Backup & Sync', icon: Database },
        { id: 'settings-profile', label: 'Business Profile', icon: Settings },
      ],
    },
  ];

  return (
    <aside
      id="desktop-sidebar"
      className={`w-64 border-r flex flex-col justify-between shrink-0 overflow-y-auto select-none transition-colors ${
        darkMode ? 'bg-neutral-900 border-neutral-800 text-neutral-300' : 'bg-neutral-50 border-neutral-200 text-neutral-700'
      }`}
    >
      <div className="py-3">
        {sections.map((section, idx) => (
          <div key={idx} className="mb-4">
            <p className="px-4 mb-1.5 text-[10px] font-bold tracking-wider text-neutral-400 dark:text-neutral-500 uppercase">
              {section.title}
            </p>
            <div className="space-y-0.5 px-2">
              {section.items.map(item => {
                const Icon = item.icon;
                const isActive = currentModule === item.id;
                return (
                  <button
                    key={item.id}
                    id={`nav-item-${item.id}`}
                    onClick={() => onSelectModule(item.id)}
                    className={`w-full flex items-center gap-2.5 px-3 py-2 rounded-md text-xs font-medium transition-colors text-left ${
                      isActive
                        ? 'bg-emerald-600 text-white font-semibold shadow-xs'
                        : darkMode
                        ? 'hover:bg-neutral-800 text-neutral-300 hover:text-white'
                        : 'hover:bg-neutral-200/70 text-neutral-700 hover:text-neutral-900'
                    }`}
                  >
                    <Icon className={`w-4 h-4 ${isActive ? 'text-white' : 'text-neutral-400 dark:text-neutral-500'}`} />
                    <span className="truncate">{item.label}</span>
                    {item.badge && (
                      <span className="ml-auto text-[10px] px-1.5 py-0.5 rounded-full bg-emerald-100 text-emerald-800">
                        {item.badge}
                      </span>
                    )}
                  </button>
                );
              })}
            </div>
          </div>
        ))}
      </div>

      <div className="p-3 border-t border-neutral-200 dark:border-neutral-800 text-[11px] text-neutral-400">
        <div className="flex items-center justify-between">
          <span>v1.0.0 (Phase 1)</span>
          <span className="font-mono text-[10px] bg-neutral-200 dark:bg-neutral-800 px-1.5 py-0.5 rounded">
            PKR Standard
          </span>
        </div>
      </div>
    </aside>
  );
};
