import React from 'react';
import { Minus, Square, X, ShieldCheck } from 'lucide-react';

interface DesktopWindowFrameProps {
  businessName?: string;
}

export const DesktopWindowFrame: React.FC<DesktopWindowFrameProps> = ({ businessName }) => {
  return (
    <div
      id="desktop-window-titlebar"
      className="h-9 bg-neutral-900 text-neutral-300 flex items-center justify-between px-3 select-none text-xs font-medium border-b border-neutral-800 z-50 shrink-0"
    >
      <div className="flex items-center gap-2">
        <div className="w-4 h-4 rounded bg-emerald-600 flex items-center justify-center text-[10px] font-bold text-white shadow-sm">
          M
        </div>
        <span className="font-semibold text-neutral-200">Modern Inventory Management System</span>
        <span className="text-neutral-500">|</span>
        <span className="text-neutral-400">
          {businessName ? `${businessName} (Windows Desktop x64)` : 'Desktop Edition (C# .NET 8 / SQLite)'}
        </span>
      </div>

      <div className="flex items-center gap-4">
        <div className="flex items-center gap-1.5 text-emerald-400 bg-emerald-950/60 px-2 py-0.5 rounded border border-emerald-800/40 text-[11px]">
          <ShieldCheck className="w-3 h-3" />
          <span>Offline SQLite Active</span>
        </div>

        {/* Windows Control Buttons */}
        <div className="flex items-center">
          <button
            id="btn-window-minimize"
            className="w-8 h-6 flex items-center justify-center hover:bg-neutral-800 text-neutral-400 hover:text-neutral-200 rounded transition-colors"
            title="Minimize"
          >
            <Minus className="w-3.5 h-3.5" />
          </button>
          <button
            id="btn-window-maximize"
            className="w-8 h-6 flex items-center justify-center hover:bg-neutral-800 text-neutral-400 hover:text-neutral-200 rounded transition-colors"
            title="Maximize"
          >
            <Square className="w-3 h-3" />
          </button>
          <button
            id="btn-window-close"
            className="w-8 h-6 flex items-center justify-center hover:bg-red-600 text-neutral-400 hover:text-white rounded transition-colors"
            title="Close"
          >
            <X className="w-3.5 h-3.5" />
          </button>
        </div>
      </div>
    </div>
  );
};
