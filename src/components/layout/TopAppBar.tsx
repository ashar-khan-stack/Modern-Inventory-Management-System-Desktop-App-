import React, { useState } from 'react';
import {
  Search,
  Cloud,
  CloudOff,
  RefreshCw,
  Sun,
  Moon,
  LogOut,
  Building2,
  CheckCircle2,
  AlertCircle,
  FlaskConical,
} from 'lucide-react';
import { useCurrentUser } from '../../infrastructure/context/CurrentUserContext';
import { localDb } from '../../infrastructure/storage/localDatabase';

interface TopAppBarProps {
  darkMode: boolean;
  onToggleDarkMode: () => void;
  onOpenTestRunner: () => void;
  onNavigate: (module: string) => void;
}

export const TopAppBar: React.FC<TopAppBarProps> = ({
  darkMode,
  onToggleDarkMode,
  onOpenTestRunner,
  onNavigate,
}) => {
  const { session, business, logout } = useCurrentUser();
  const [isSyncing, setIsSyncing] = useState(false);
  const [showUserMenu, setShowUserMenu] = useState(false);

  const pendingQueueCount = session ? localDb.getSyncQueue(session.businessId).length : 0;

  const handleManualSync = () => {
    setIsSyncing(true);
    setTimeout(() => {
      setIsSyncing(false);
    }, 900);
  };

  return (
    <header
      id="desktop-top-app-bar"
      className={`h-14 px-4 flex items-center justify-between border-b transition-colors shrink-0 ${
        darkMode ? 'bg-neutral-900 border-neutral-800 text-neutral-100' : 'bg-white border-neutral-200 text-neutral-900'
      }`}
    >
      {/* Left: Business Name & Tenant Indicator */}
      <div className="flex items-center gap-3 min-w-[240px]">
        <div className="w-8 h-8 rounded-lg bg-emerald-600 flex items-center justify-center text-white font-bold shadow-sm">
          <Building2 className="w-4 h-4" />
        </div>
        <div className="flex flex-col">
          <span className="font-semibold text-sm leading-tight truncate max-w-[200px]">
            {business?.businessName || 'Signix Business'}
          </span>
          <span className="text-[11px] text-neutral-500 flex items-center gap-1">
            <span>NTN: {business?.ntnNumber || 'Unregistered'}</span>
            <span>•</span>
            <span className="text-emerald-600 font-medium">PKR</span>
          </span>
        </div>
      </div>

      {/* Center: Global Search */}
      <div className="flex-1 max-w-md mx-4">
        <div className="relative">
          <Search className="w-4 h-4 absolute left-3 top-1/2 -translate-y-1/2 text-neutral-400" />
          <input
            id="global-search-input"
            type="text"
            placeholder="Search products, invoices, customers, vouchers (Ctrl+K)..."
            className={`w-full pl-9 pr-4 py-1.5 rounded-md text-xs border outline-none transition-all ${
              darkMode
                ? 'bg-neutral-800 border-neutral-700 text-neutral-100 focus:border-emerald-500'
                : 'bg-neutral-100 border-neutral-300 text-neutral-900 focus:bg-white focus:border-emerald-600'
            }`}
          />
        </div>
      </div>

      {/* Right Controls */}
      <div className="flex items-center gap-2.5">
        {/* Automated Test Suite Trigger */}
        <button
          id="btn-open-phase1-tests"
          onClick={onOpenTestRunner}
          className="flex items-center gap-1.5 px-2.5 py-1.5 rounded text-xs font-medium bg-emerald-50 hover:bg-emerald-100 text-emerald-800 border border-emerald-300 transition-colors shadow-xs"
          title="Run automated tests for Phase 1 verification"
        >
          <FlaskConical className="w-3.5 h-3.5 text-emerald-700" />
          <span>Phase 1 Verification Tests</span>
        </button>

        {/* Sync Status Badge */}
        <button
          id="btn-sync-status-indicator"
          onClick={handleManualSync}
          className={`flex items-center gap-2 px-2.5 py-1.5 rounded text-xs border transition-colors ${
            pendingQueueCount > 0
              ? 'bg-amber-500/10 text-amber-600 border-amber-500/30 hover:bg-amber-500/20'
              : 'bg-emerald-500/10 text-emerald-600 border-emerald-500/30 hover:bg-emerald-500/20'
          }`}
          title={pendingQueueCount > 0 ? `${pendingQueueCount} pending local changes to sync` : 'All local changes up to date'}
        >
          {isSyncing ? (
            <RefreshCw className="w-3.5 h-3.5 animate-spin" />
          ) : pendingQueueCount > 0 ? (
            <CloudOff className="w-3.5 h-3.5" />
          ) : (
            <Cloud className="w-3.5 h-3.5" />
          )}
          <span className="font-medium">
            {isSyncing ? 'Syncing...' : pendingQueueCount > 0 ? `${pendingQueueCount} Pending` : 'Synced'}
          </span>
        </button>

        {/* Theme Toggle */}
        <button
          id="btn-toggle-theme"
          onClick={onToggleDarkMode}
          className={`p-2 rounded border transition-colors ${
            darkMode ? 'bg-neutral-800 border-neutral-700 text-amber-400 hover:bg-neutral-700' : 'bg-neutral-100 border-neutral-300 text-neutral-600 hover:bg-neutral-200'
          }`}
          title={darkMode ? 'Switch to Light Mode' : 'Switch to Dark Mode'}
        >
          {darkMode ? <Sun className="w-3.5 h-3.5" /> : <Moon className="w-3.5 h-3.5" />}
        </button>

        {/* User Menu */}
        <div className="relative">
          <button
            id="btn-user-profile-menu"
            onClick={() => setShowUserMenu(!showUserMenu)}
            className="flex items-center gap-2 pl-2 pr-1 py-1 rounded hover:bg-neutral-100 dark:hover:bg-neutral-800 transition-colors"
          >
            <div className="w-7 h-7 rounded-full bg-emerald-700 text-white flex items-center justify-center text-xs font-semibold">
              {session?.email.charAt(0).toUpperCase() || 'U'}
            </div>
            <div className="text-left hidden md:block">
              <p className="text-xs font-medium leading-none">{session?.email.split('@')[0]}</p>
              <p className="text-[10px] text-neutral-500 leading-tight">Admin (30-Day Session)</p>
            </div>
          </button>

          {showUserMenu && (
            <div
              className={`absolute right-0 mt-2 w-56 rounded-md shadow-lg border py-1.5 z-50 text-xs ${
                darkMode ? 'bg-neutral-900 border-neutral-700 text-neutral-200' : 'bg-white border-neutral-200 text-neutral-800'
              }`}
            >
              <div className="px-3 py-2 border-b border-neutral-200 dark:border-neutral-800">
                <p className="font-semibold text-xs">{business?.ownerName || 'Business Owner'}</p>
                <p className="text-neutral-500 text-[11px] truncate">{session?.email}</p>
                <div className="mt-1.5 flex items-center gap-1 text-[10px] text-emerald-600 font-medium">
                  <CheckCircle2 className="w-3 h-3" />
                  <span>Session valid for 30 days</span>
                </div>
              </div>

              <button
                id="btn-menu-settings"
                onClick={() => {
                  setShowUserMenu(false);
                  onNavigate('settings-backup');
                }}
                className="w-full text-left px-3 py-2 hover:bg-neutral-100 dark:hover:bg-neutral-800 flex items-center justify-between"
              >
                <span>Backup & Sync Settings</span>
                <span className="text-[10px] text-neutral-400">Ctrl+,</span>
              </button>

              <div className="border-t border-neutral-200 dark:border-neutral-800 my-1"></div>

              <button
                id="btn-logout"
                onClick={() => {
                  setShowUserMenu(false);
                  logout();
                }}
                className="w-full text-left px-3 py-2 text-red-600 hover:bg-red-50 dark:hover:bg-red-950/30 flex items-center gap-2"
              >
                <LogOut className="w-3.5 h-3.5" />
                <span>Log Out</span>
              </button>
            </div>
          )}
        </div>
      </div>
    </header>
  );
};
