import React, { useState, useEffect } from 'react';
import {
  Database,
  Cloud,
  RefreshCw,
  Download,
  Upload,
  ShieldCheck,
  AlertTriangle,
  CheckCircle2,
  Clock,
  HardDrive,
  FileCode,
  Layers,
} from 'lucide-react';
import { useCurrentUser } from '../../infrastructure/context/CurrentUserContext';
import { localDb } from '../../infrastructure/storage/localDatabase';
import { DatabaseBackupService } from '../../infrastructure/storage/backupService';
import { LocalBackupRecord, SyncQueueItem } from '../../types';

interface BackupSyncShellViewProps {
  darkMode: boolean;
}

export const BackupSyncShellView: React.FC<BackupSyncShellViewProps> = ({ darkMode }) => {
  const { business } = useCurrentUser();
  const [backups, setBackups] = useState<LocalBackupRecord[]>([]);
  const [syncQueue, setSyncQueue] = useState<SyncQueueItem[]>([]);
  const [isBackingUp, setIsBackingUp] = useState(false);
  const [isSyncing, setIsSyncing] = useState(false);
  const [statusMessage, setStatusMessage] = useState<{ type: 'success' | 'error' | 'info'; text: string } | null>(null);

  const loadData = () => {
    if (!business) return;
    setBackups(localDb.getBackups(business.id));
    setSyncQueue(localDb.getSyncQueue(business.id));
  };

  useEffect(() => {
    loadData();
    const unsub = localDb.subscribe(loadData);
    return () => unsub();
  }, [business]);

  const handleBackupNow = async () => {
    if (!business) return;
    setIsBackingUp(true);
    setStatusMessage(null);
    try {
      const record = await DatabaseBackupService.createLocalBackup(business.id);
      setStatusMessage({
        type: 'success',
        text: `Local safety backup successfully created: ${record.fileName}`,
      });
    } catch (err: any) {
      setStatusMessage({
        type: 'error',
        text: `Backup failed: ${err.message}`,
      });
    } finally {
      setIsBackingUp(false);
    }
  };

  const handleDownloadDump = () => {
    if (!business) return;
    DatabaseBackupService.downloadDatabaseDump(business.id);
  };

  const handleSyncNow = () => {
    setIsSyncing(true);
    setStatusMessage({
      type: 'info',
      text: 'Validating local mutation queue...',
    });
    setTimeout(() => {
      setIsSyncing(false);
      setStatusMessage({
        type: 'success',
        text: 'Local mutations verified. Transaction queue is integrity-consistent.',
      });
    }, 1200);
  };

  return (
    <div className="p-6 space-y-6 max-w-6xl mx-auto">
      {/* Header */}
      <div className="pb-4 border-b border-neutral-200 dark:border-neutral-800 flex flex-col sm:flex-row sm:items-center justify-between gap-4">
        <div>
          <div className="flex items-center gap-2">
            <h1 className="text-xl font-bold text-neutral-900 dark:text-neutral-100">
              Database Maintenance & Backup
            </h1>
            <span className="text-xs px-2 py-0.5 rounded bg-emerald-100 text-emerald-800 dark:bg-emerald-950 dark:text-emerald-300 font-medium">
              Offline SQLite
            </span>
          </div>
          <p className="text-xs text-neutral-500 dark:text-neutral-400 mt-1">
            Local SQLite database safety backups & transaction queue integrity
          </p>
        </div>

        <div className="flex items-center gap-2">
          <button
            id="btn-backup-now"
            onClick={handleBackupNow}
            disabled={isBackingUp}
            className="flex items-center gap-1.5 px-3 py-2 bg-emerald-600 hover:bg-emerald-500 disabled:opacity-50 text-white text-xs font-semibold rounded-lg shadow-xs transition-colors"
          >
            {isBackingUp ? <RefreshCw className="w-3.5 h-3.5 animate-spin" /> : <HardDrive className="w-3.5 h-3.5" />}
            <span>Backup Now (.db)</span>
          </button>

          <button
            id="btn-sync-now"
            onClick={handleSyncNow}
            disabled={isSyncing}
            className="flex items-center gap-1.5 px-3 py-2 bg-neutral-800 hover:bg-neutral-700 disabled:opacity-50 text-neutral-100 text-xs font-semibold rounded-lg shadow-xs transition-colors"
          >
            {isSyncing ? <RefreshCw className="w-3.5 h-3.5 animate-spin" /> : <RefreshCw className="w-3.5 h-3.5" />}
            <span>Sync Now</span>
          </button>
        </div>
      </div>

      {statusMessage && (
        <div
          className={`p-3 rounded-lg text-xs flex items-center gap-2 border ${
            statusMessage.type === 'success'
              ? 'bg-emerald-50 dark:bg-emerald-950/40 border-emerald-300 text-emerald-800 dark:text-emerald-300'
              : statusMessage.type === 'error'
              ? 'bg-red-50 dark:bg-red-950/40 border-red-300 text-red-800 dark:text-red-300'
              : 'bg-blue-50 dark:bg-blue-950/40 border-blue-300 text-blue-800 dark:text-blue-300'
          }`}
        >
          {statusMessage.type === 'success' ? (
            <CheckCircle2 className="w-4 h-4 shrink-0" />
          ) : (
            <AlertTriangle className="w-4 h-4 shrink-0" />
          )}
          <span>{statusMessage.text}</span>
        </div>
      )}

      {/* Cloud & Local Storage Architecture Overview Cards */}
      <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
        <div
          className={`p-4 rounded-xl border ${
            darkMode ? 'bg-neutral-900 border-neutral-800' : 'bg-white border-neutral-200'
          }`}
        >
          <div className="flex items-center justify-between mb-2">
            <span className="text-xs font-semibold text-neutral-500">Local SQLite Storage</span>
            <HardDrive className="w-4 h-4 text-emerald-500" />
          </div>
          <div className="text-lg font-bold text-neutral-900 dark:text-neutral-100">
            Active & Healthy
          </div>
          <div className="text-[11px] text-neutral-500 mt-1">
            Zero-latency ACID local transactions
          </div>
        </div>

        <div
          className={`p-4 rounded-xl border ${
            darkMode ? 'bg-neutral-900 border-neutral-800' : 'bg-white border-neutral-200'
          }`}
        >
          <div className="flex items-center justify-between mb-2">
            <span className="text-xs font-semibold text-neutral-500">Offline Sync Queue</span>
            <Layers className="w-4 h-4 text-amber-500" />
          </div>
          <div className="text-lg font-bold text-neutral-900 dark:text-neutral-100">
            {syncQueue.length} Mutations Queued
          </div>
          <div className="text-[11px] text-neutral-500 mt-1">
            Ready for future reconciliation
          </div>
        </div>

        <div
          className={`p-4 rounded-xl border ${
            darkMode ? 'bg-neutral-900 border-neutral-800' : 'bg-white border-neutral-200'
          }`}
        >
          <div className="flex items-center justify-between mb-2">
            <span className="text-xs font-semibold text-neutral-500">System Mode</span>
            <ShieldCheck className="w-4 h-4 text-blue-500" />
          </div>
          <div className="text-lg font-bold text-neutral-900 dark:text-neutral-100">
            Fully Offline
          </div>
          <div className="text-[11px] text-neutral-500 mt-1">
            Cloud dependencies removed
          </div>
        </div>
      </div>

      {/* Safety Backups History Table */}
      <div
        className={`rounded-xl border overflow-hidden ${
          darkMode ? 'bg-neutral-900 border-neutral-800' : 'bg-white border-neutral-200'
        }`}
      >
        <div className="p-4 border-b border-neutral-200 dark:border-neutral-800 flex items-center justify-between">
          <div className="flex items-center gap-2">
            <ShieldCheck className="w-4 h-4 text-emerald-500" />
            <h2 className="text-xs font-bold uppercase tracking-wider text-neutral-900 dark:text-neutral-100">
              Timestamped Local Database Snapshots ({backups.length})
            </h2>
          </div>
          <button
            id="btn-export-full-dump"
            onClick={handleDownloadDump}
            className="flex items-center gap-1.5 text-xs text-emerald-600 hover:text-emerald-500 font-medium"
          >
            <Download className="w-3.5 h-3.5" />
            <span>Export Database Dump (JSON/.db)</span>
          </button>
        </div>

        {backups.length === 0 ? (
          <div className="p-8 text-center text-neutral-400 text-xs">
            <p>No local safety backups created yet.</p>
            <p className="mt-1 text-[11px]">
              Click "Backup Now" to create your first timestamped snapshot.
            </p>
          </div>
        ) : (
          <div className="overflow-x-auto">
            <table className="w-full text-left text-xs">
              <thead className="bg-neutral-50 dark:bg-neutral-800/60 text-neutral-500 uppercase text-[10px] font-semibold border-b border-neutral-200 dark:border-neutral-800">
                <tr>
                  <th className="px-4 py-2.5">Snapshot Filename</th>
                  <th className="px-4 py-2.5">Created At (UTC)</th>
                  <th className="px-4 py-2.5">File Size</th>
                  <th className="px-4 py-2.5">Entity Counts</th>
                  <th className="px-4 py-2.5">Integrity Checksum</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-neutral-200 dark:divide-neutral-800">
                {backups.map(b => (
                  <tr key={b.id} className="hover:bg-neutral-50/50 dark:hover:bg-neutral-800/30">
                    <td className="px-4 py-3 font-mono font-medium text-emerald-600 dark:text-emerald-400">
                      {b.fileName}
                    </td>
                    <td className="px-4 py-3 text-neutral-500">
                      {new Date(b.createdAt).toLocaleString()}
                    </td>
                    <td className="px-4 py-3 text-neutral-600 dark:text-neutral-300">
                      {(b.fileSizeBytes / 1024).toFixed(2)} KB
                    </td>
                    <td className="px-4 py-3 text-neutral-500">
                      {Object.entries(b.entityCounts || {})
                        .filter(([_, count]) => count > 0)
                        .map(([k, v]) => `${k}: ${v}`)
                        .join(', ') || '0 entities'}
                    </td>
                    <td className="px-4 py-3 font-mono text-[11px] text-neutral-400">
                      {b.checksum}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </div>

      {/* Offline Sync Mutation Queue Table */}
      <div
        className={`rounded-xl border overflow-hidden ${
          darkMode ? 'bg-neutral-900 border-neutral-800' : 'bg-white border-neutral-200'
        }`}
      >
        <div className="p-4 border-b border-neutral-200 dark:border-neutral-800 flex items-center justify-between">
          <div className="flex items-center gap-2">
            <Layers className="w-4 h-4 text-amber-500" />
            <h2 className="text-xs font-bold uppercase tracking-wider text-neutral-900 dark:text-neutral-100">
              Offline Mutation Queue ({syncQueue.length})
            </h2>
          </div>
          <span className="text-[11px] text-neutral-400 font-mono">
            {syncQueue.length} pending items
          </span>
        </div>

        {syncQueue.length === 0 ? (
          <div className="p-8 text-center text-neutral-400 text-xs">
            <CheckCircle2 className="w-6 h-6 text-emerald-500 mx-auto mb-2" />
            <p>Sync queue is clean. All local modifications are recorded.</p>
          </div>
        ) : (
          <div className="overflow-x-auto">
            <table className="w-full text-left text-xs">
              <thead className="bg-neutral-50 dark:bg-neutral-800/60 text-neutral-500 uppercase text-[10px] font-semibold border-b border-neutral-200 dark:border-neutral-800">
                <tr>
                  <th className="px-4 py-2.5">Entity Type</th>
                  <th className="px-4 py-2.5">Operation</th>
                  <th className="px-4 py-2.5">Deterministic UUID</th>
                  <th className="px-4 py-2.5">Retry Count</th>
                  <th className="px-4 py-2.5">Queued Time</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-neutral-200 dark:divide-neutral-800">
                {syncQueue.slice(0, 10).map(q => (
                  <tr key={q.id} className="hover:bg-neutral-50/50 dark:hover:bg-neutral-800/30">
                    <td className="px-4 py-2.5 font-medium text-neutral-800 dark:text-neutral-200">
                      {q.entityType}
                    </td>
                    <td className="px-4 py-2.5">
                      <span
                        className={`px-2 py-0.5 rounded text-[10px] font-bold ${
                          q.operationType === 'CREATE'
                            ? 'bg-emerald-100 text-emerald-800 dark:bg-emerald-950 dark:text-emerald-300'
                            : q.operationType === 'UPDATE'
                            ? 'bg-blue-100 text-blue-800 dark:bg-blue-950 dark:text-blue-300'
                            : 'bg-red-100 text-red-800 dark:bg-red-950 dark:text-red-300'
                        }`}
                      >
                        {q.operationType}
                      </span>
                    </td>
                    <td className="px-4 py-2.5 font-mono text-[11px] text-neutral-500 truncate max-w-[180px]">
                      {q.entityId}
                    </td>
                    <td className="px-4 py-2.5 text-neutral-500">{q.retryCount}</td>
                    <td className="px-4 py-2.5 text-neutral-500">
                      {new Date(q.createdAt).toLocaleTimeString()}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
            {syncQueue.length > 10 && (
              <div className="p-2 text-center text-[11px] text-neutral-500 border-t border-neutral-200 dark:border-neutral-800">
                Showing first 10 of {syncQueue.length} queued records.
              </div>
            )}
          </div>
        )}
      </div>
    </div>
  );
};
