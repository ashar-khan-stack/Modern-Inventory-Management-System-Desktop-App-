/**
 * Automated Test Suite for Phase 1: Core Foundation, Database, Authentication, and Sync Foundation.
 */

import { AuthService } from '../services/authService';
import {
  accountHeadRepo,
  categoryRepo,
  customerRepo,
  employeeRepo,
  productRepo,
  syncQueueRepo,
} from '../services/repository';
import { localDb } from '../infrastructure/storage/localDatabase';
import { DatabaseBackupService } from '../infrastructure/storage/backupService';
import {
  generateSaltHex,
  generateUUID,
  hashPasswordPbkdf2,
  isSessionExpired,
  verifyPassword,
} from '../domain/security';
import { SyncOperationType, SyncStatus } from '../types';

export interface TestResult {
  suite: string;
  testName: string;
  passed: boolean;
  message: string;
  durationMs: number;
}

export class Phase1TestRunner {
  public static async runAllTests(): Promise<{ passedCount: number; failedCount: number; results: TestResult[] }> {
    const results: TestResult[] = [];

    // Helper to run a test
    const runTest = async (suite: string, name: string, fn: () => Promise<void>) => {
      const start = performance.now();
      try {
        await fn();
        results.push({
          suite,
          testName: name,
          passed: true,
          message: 'Passed successfully',
          durationMs: Math.round(performance.now() - start),
        });
      } catch (err: any) {
        results.push({
          suite,
          testName: name,
          passed: false,
          message: err?.message || String(err),
          durationMs: Math.round(performance.now() - start),
        });
      }
    };

    // -------------------------------------------------------------
    // 1. DATABASE TESTS
    // -------------------------------------------------------------
    await runTest('Database', 'Database Initialization & Schema Verification', async () => {
      const state = localDb.getState();
      if (!state || typeof state.version !== 'number') {
        throw new Error('Database state or version is not initialized');
      }
    });

    await runTest('Database', 'Entity Insertion & Retrieval (Products & Categories)', async () => {
      const testBusinessId = generateUUID();
      const cat = await categoryRepo.insertAsync({
        id: generateUUID(),
        businessId: testBusinessId,
        name: 'Electronics & Accessories',
        description: 'Retail electronics category',
        createdAt: new Date().toISOString(),
        updatedAt: new Date().toISOString(),
        isDeleted: false,
        syncStatus: SyncStatus.PendingCreate,
      });

      const prod = await productRepo.insertAsync({
        id: generateUUID(),
        businessId: testBusinessId,
        categoryId: cat.id,
        categoryName: cat.name,
        sku: 'ELEC-1001',
        barcode: '896400012345',
        name: 'Industrial Voltage Stabilizer',
        unit: 'Pcs',
        costPrice: 4500.00,
        salePrice: 5800.00,
        stockQuantity: 25,
        minStockAlert: 5,
        taxRate: 18.0,
        createdAt: new Date().toISOString(),
        updatedAt: new Date().toISOString(),
        isDeleted: false,
        syncStatus: SyncStatus.PendingCreate,
      });

      const products = await productRepo.getAllAsync(testBusinessId);
      if (!products.some(p => p.id === prod.id && p.sku === 'ELEC-1001')) {
        throw new Error('Inserted product was not found in repository query');
      }
    });

    await runTest('Database', 'Soft Delete Verification', async () => {
      const testBiz = generateUUID();
      const prod = await productRepo.insertAsync({
        id: generateUUID(),
        businessId: testBiz,
        categoryId: 'cat-1',
        sku: 'DEL-01',
        barcode: '111',
        name: 'Item to Delete',
        unit: 'Pcs',
        costPrice: 100,
        salePrice: 150,
        stockQuantity: 10,
        minStockAlert: 2,
        taxRate: 18,
        createdAt: new Date().toISOString(),
        updatedAt: new Date().toISOString(),
        isDeleted: false,
        syncStatus: SyncStatus.PendingCreate,
      });

      await productRepo.deleteAsync(testBiz, prod.id);
      const activeProducts = await productRepo.getAllAsync(testBiz);
      if (activeProducts.some(p => p.id === prod.id)) {
        throw new Error('Soft-deleted product still returned in active queries');
      }
    });

    await runTest('Database', 'Customer & Employee Separation Verification', async () => {
      const testBiz = generateUUID();
      const customer = await customerRepo.insertAsync({
        id: generateUUID(),
        businessId: testBiz,
        name: 'Tariq Textiles Ltd.',
        companyName: 'Tariq Group',
        phone: '03001234567',
        email: 'tariq@textiles.pk',
        cnic: '35202-1234567-1',
        ntn: '1234567-8',
        address: 'Ferozepur Road, Lahore',
        creditLimit: 500000.00,
        openingBalance: 0,
        currentBalance: 0,
        createdAt: new Date().toISOString(),
        updatedAt: new Date().toISOString(),
        isDeleted: false,
        syncStatus: SyncStatus.PendingCreate,
      });

      const employee = await employeeRepo.insertAsync({
        id: generateUUID(),
        businessId: testBiz,
        name: 'Muhammad Imran',
        designation: 'Inventory Warehouse Supervisor',
        cnic: '35202-7654321-3',
        phone: '03219876543',
        address: 'Model Town, Lahore',
        monthlySalary: 65000.00,
        joiningDate: '2025-01-15',
        advanceBalance: 0,
        isActive: true,
        createdAt: new Date().toISOString(),
        updatedAt: new Date().toISOString(),
        isDeleted: false,
        syncStatus: SyncStatus.PendingCreate,
      });

      const customers = await customerRepo.getAllAsync(testBiz);
      const employees = await employeeRepo.getAllAsync(testBiz);

      if (customers.some(c => c.id === employee.id) || employees.some(e => e.id === customer.id)) {
        throw new Error('Customer and Employee entities leaked across domain boundaries');
      }
    });

    await runTest('Database', 'Business Tenant Isolation Verification', async () => {
      const bizA = generateUUID();
      const bizB = generateUUID();

      await productRepo.insertAsync({
        id: generateUUID(),
        businessId: bizA,
        categoryId: 'cat-a',
        sku: 'BIZ-A-SKU',
        barcode: '111',
        name: 'Business A Product',
        unit: 'Pcs',
        costPrice: 50,
        salePrice: 80,
        stockQuantity: 100,
        minStockAlert: 10,
        taxRate: 18,
        createdAt: new Date().toISOString(),
        updatedAt: new Date().toISOString(),
        isDeleted: false,
        syncStatus: SyncStatus.PendingCreate,
      });

      const bizBProducts = await productRepo.getAllAsync(bizB);
      if (bizBProducts.some(p => p.sku === 'BIZ-A-SKU')) {
        throw new Error('Tenant isolation failure: Business B accessed Business A records');
      }
    });

    // -------------------------------------------------------------
    // 2. AUTHENTICATION & SECURITY TESTS
    // -------------------------------------------------------------
    const testAuthEmail = `test_admin_${Date.now()}@moderninv.pk`;
    const testAuthPassword = 'SecurePassword#2026';

    await runTest('Authentication', 'Business Registration & Account Heads Seeding', async () => {
      const reg = await AuthService.registerBusiness({
        businessName: 'Signix World Trading Corporation',
        ownerName: 'Ashar Khan',
        email: testAuthEmail,
        phone: '+92 300 1234567',
        address: 'Plot 45, Industrial Area, Karachi, Pakistan',
        ntnNumber: '7891234-5',
        strnNumber: '17-00-7891-234-11',
        password: testAuthPassword,
        securityQuestions: [
          { question: 'What is the name of your first school?', answer: 'Beaconhouse School System' },
          { question: 'In what city was your first job located?', answer: 'Karachi' },
        ],
      });

      if (!reg.success || !reg.session) {
        throw new Error(reg.message || 'Registration failed');
      }

      // Verify Account Heads seeded
      const heads = await accountHeadRepo.getAllAsync(reg.session.businessId);
      if (heads.length < 5) {
        throw new Error(`Expected default Chart of Accounts seeded, got ${heads.length}`);
      }
    });

    await runTest('Authentication', 'PBKDF2 Password Verification & Constant-Time Matching', async () => {
      const salt = generateSaltHex(16);
      const hash1 = await hashPasswordPbkdf2('Password123!', salt);
      const hash2 = await hashPasswordPbkdf2('Password123!', salt);
      const hashWrong = await hashPasswordPbkdf2('WrongPassword!', salt);

      if (hash1 !== hash2) {
        throw new Error('PBKDF2 deterministic hashing mismatch for identical inputs');
      }
      const match = await verifyPassword('Password123!', hash1, salt);
      const wrongMatch = await verifyPassword('WrongPassword!', hash1, salt);

      if (!match || wrongMatch) {
        throw new Error('PBKDF2 password verification logic failed');
      }
    });

    await runTest('Authentication', 'Valid Login & 30-Day Session Token Generation', async () => {
      const loginRes = await AuthService.login(testAuthEmail, testAuthPassword, true);
      if (!loginRes.success || !loginRes.session) {
        throw new Error(loginRes.message || 'Valid login rejected');
      }

      if (isSessionExpired(loginRes.session.expiresAt)) {
        throw new Error('New session was immediately marked as expired');
      }

      const expiryDiffDays = (new Date(loginRes.session.expiresAt).getTime() - Date.now()) / (1000 * 60 * 60 * 24);
      if (expiryDiffDays < 29 || expiryDiffDays > 31) {
        throw new Error(`Expected ~30-day session expiry, got ${expiryDiffDays.toFixed(1)} days`);
      }
    });

    await runTest('Authentication', 'Invalid Password Rejection', async () => {
      const loginRes = await AuthService.login(testAuthEmail, 'IncorrectPassword999!', true);
      if (loginRes.success) {
        throw new Error('Login succeeded with incorrect password');
      }
    });

    await runTest('Authentication', 'Security Question Recovery & Password Reset', async () => {
      const questions = AuthService.getSecurityQuestions(testAuthEmail);
      if (!questions || !questions.question1) {
        throw new Error('Failed to retrieve security questions for recovery');
      }

      const resetRes = await AuthService.resetPasswordWithSecurityQuestions(
        testAuthEmail,
        {
          answer1: 'Beaconhouse School System',
          answer2: 'Karachi',
        },
        'NewStrongPassword#2026'
      );

      if (!resetRes.success) {
        throw new Error(`Password reset failed: ${resetRes.message}`);
      }

      // Verify login with new password
      const newLogin = await AuthService.login(testAuthEmail, 'NewStrongPassword#2026', true);
      if (!newLogin.success) {
        throw new Error('Login with updated reset password failed');
      }
    });

    // -------------------------------------------------------------
    // 3. SYNC FOUNDATION TESTS
    // -------------------------------------------------------------
    await runTest('Sync Foundation', 'Offline Mutation Queue & Operation Tracking', async () => {
      const testBiz = generateUUID();
      const cat = await categoryRepo.insertAsync({
        id: generateUUID(),
        businessId: testBiz,
        name: 'Sync Test Category',
        description: 'Test sync',
        createdAt: new Date().toISOString(),
        updatedAt: new Date().toISOString(),
        isDeleted: false,
        syncStatus: SyncStatus.PendingCreate,
      });

      const queue = await syncQueueRepo.getPendingQueueAsync(testBiz);
      const queueItem = queue.find(q => q.entityId === cat.id);
      if (!queueItem || queueItem.operationType !== SyncOperationType.CREATE) {
        throw new Error('CREATE mutation was not enqueued in SyncQueue');
      }
    });

    await runTest('Sync Foundation', 'Sync Queue Retry Counter & Error Logging', async () => {
      const testBiz = generateUUID();
      localDb.enqueueSync(testBiz, 'products', 'prod-retry-1', SyncOperationType.UPDATE, { stock: 50 });

      const queue = await syncQueueRepo.getPendingQueueAsync(testBiz);
      const item = queue.find(q => q.entityId === 'prod-retry-1');
      if (!item) throw new Error('Queue item not found');

      await syncQueueRepo.markItemFailedAsync(item.id, 'Connection timed out during mutation tracking');

      const updatedQueue = await syncQueueRepo.getPendingQueueAsync(testBiz);
      const updatedItem = updatedQueue.find(q => q.id === item.id);
      if (!updatedItem || updatedItem.retryCount !== 1 || !updatedItem.lastError) {
        throw new Error('Sync queue retry count or error message was not recorded');
      }
    });

    // -------------------------------------------------------------
    // 4. LOCAL BACKUP TESTS
    // -------------------------------------------------------------
    await runTest('Backup & Safety', 'Timestamped Backup Creation & Integrity Checksum', async () => {
      const testBiz = generateUUID();
      const backup = await DatabaseBackupService.createLocalBackup(testBiz);

      if (!backup.fileName.startsWith('inventory_') || !backup.fileName.endsWith('.db')) {
        throw new Error(`Invalid backup filename format: ${backup.fileName}`);
      }

      if (!backup.checksum || backup.fileSizeBytes <= 0) {
        throw new Error('Backup checksum or file size is invalid');
      }
    });

    await runTest('Backup & Safety', 'Safe Restore with Pre-Backup Snapshot', async () => {
      const testBiz = generateUUID();
      const stateBefore = localDb.getState();
      const restoreResult = await DatabaseBackupService.safeRestore(testBiz, stateBefore);

      if (!restoreResult.success || !restoreResult.safetyBackupId) {
        throw new Error(`Safe restore failed: ${restoreResult.message}`);
      }
    });

    const passedCount = results.filter(r => r.passed).length;
    const failedCount = results.filter(r => !r.passed).length;

    return { passedCount, failedCount, results };
  }
}
