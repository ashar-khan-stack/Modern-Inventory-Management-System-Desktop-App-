/**
 * Local Safety Database Backup & Restore Service
 * Implements timestamped snapshots (e.g. inventory_2026-09-18_13-30-00.db / JSON)
 * and safe restore protocols with automatic safety snapshot before restore.
 */

import { DatabaseState, localDb } from './localDatabase';
import { LocalBackupRecord } from '../../types';
import { generateUUID } from '../../domain/security';

export class DatabaseBackupService {
  /**
   * Generates a timestamped backup filename
   * Example: inventory_2026-09-18_13-30-00.db
   */
  public static generateBackupFileName(): string {
    const now = new Date();
    const pad = (n: number) => n.toString().padStart(2, '0');
    const yyyy = now.getFullYear();
    const mm = pad(now.getMonth() + 1);
    const dd = pad(now.getDate());
    const hh = pad(now.getHours());
    const min = pad(now.getMinutes());
    const ss = pad(now.getSeconds());
    return `inventory_${yyyy}-${mm}-${dd}_${hh}-${min}-${ss}.db`;
  }

  /**
   * Simple checksum calculation for data integrity verification
   */
  public static calculateChecksum(content: string): string {
    let hash = 0;
    for (let i = 0; i < content.length; i++) {
      const char = content.charCodeAt(i);
      hash = ((hash << 5) - hash) + char;
      hash |= 0; // Convert to 32bit integer
    }
    return Math.abs(hash).toString(16).padStart(8, '0');
  }

  /**
   * Create a local backup snapshot for the active business
   */
  public static async createLocalBackup(businessId: string): Promise<LocalBackupRecord> {
    const state = localDb.getState();
    const stateJson = JSON.stringify(state, null, 2);
    const fileName = this.generateBackupFileName();
    const checksum = this.calculateChecksum(stateJson);

    const entityCounts: Record<string, number> = {
      products: state.products.filter(p => p.businessId === businessId && !p.isDeleted).length,
      categories: state.categories.filter(c => c.businessId === businessId && !c.isDeleted).length,
      customers: state.customers.filter(c => c.businessId === businessId && !c.isDeleted).length,
      employees: state.employees.filter(e => e.businessId === businessId && !e.isDeleted).length,
      sales: state.sales.filter(s => s.businessId === businessId && !s.isDeleted).length,
      purchases: state.purchases.filter(p => p.businessId === businessId && !p.isDeleted).length,
      vouchers: state.vouchers.filter(v => v.businessId === businessId && !v.isDeleted).length,
      expenses: state.expenses.filter(e => e.businessId === businessId && !e.isDeleted).length,
    };

    const record: LocalBackupRecord = {
      id: generateUUID(),
      fileName,
      businessId,
      createdAt: new Date().toISOString(),
      fileSizeBytes: new Blob([stateJson]).size,
      entityCounts,
      checksum,
    };

    localDb.insertBackupRecord(record);

    // Also store the snapshot dump in localStorage keyed by record ID
    try {
      localStorage.setItem(`modern_inv_backup_dump_${record.id}`, stateJson);
    } catch (e) {
      console.warn('Could not store full snapshot in local storage dump, header saved.', e);
    }

    return record;
  }

  /**
   * Export the complete database file for download/disk storage
   */
  public static downloadDatabaseDump(businessId: string): void {
    const state = localDb.getState();
    const stateJson = JSON.stringify(state, null, 2);
    const fileName = this.generateBackupFileName().replace('.db', '.json');
    const blob = new Blob([stateJson], { type: 'application/json' });
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = fileName;
    document.body.appendChild(a);
    a.click();
    document.body.removeChild(a);
    URL.revokeObjectURL(url);
  }

  /**
   * Safe restore workflow:
   * 1. Creates an automatic emergency safety snapshot of current state
   * 2. Validates new state integrity
   * 3. Applies restore
   */
  public static async safeRestore(businessId: string, backupState: DatabaseState): Promise<{ success: boolean; safetyBackupId: string; message: string }> {
    // 1. Create emergency safety backup
    const safetySnapshot = await this.createLocalBackup(businessId);

    // 2. Validate structure
    if (!backupState || typeof backupState.version !== 'number' || !Array.isArray(backupState.products)) {
      return {
        success: false,
        safetyBackupId: safetySnapshot.id,
        message: 'Invalid backup file structure. Restoration aborted safely.',
      };
    }

    // 3. Apply state
    localDb.restoreState(backupState);

    return {
      success: true,
      safetyBackupId: safetySnapshot.id,
      message: `Database restored successfully. Safety pre-backup saved as ${safetySnapshot.fileName}.`,
    };
  }
}
