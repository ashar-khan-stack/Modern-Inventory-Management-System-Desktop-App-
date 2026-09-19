import React, { useState } from 'react';
import {
  X,
  Play,
  CheckCircle2,
  XCircle,
  Clock,
  ShieldCheck,
  Database,
  Lock,
  HardDrive,
  RefreshCw,
  FlaskConical,
} from 'lucide-react';
import { Phase1TestRunner, TestResult } from '../../tests/phase1TestSuite';

interface Phase1TestRunnerModalProps {
  isOpen: boolean;
  onClose: () => void;
  darkMode: boolean;
}

export const Phase1TestRunnerModal: React.FC<Phase1TestRunnerModalProps> = ({
  isOpen,
  onClose,
  darkMode,
}) => {
  const [isRunning, setIsRunning] = useState(false);
  const [results, setResults] = useState<TestResult[]>([]);
  const [summary, setSummary] = useState<{ passed: number; failed: number; totalDuration: number } | null>(null);

  if (!isOpen) return null;

  const handleRunTests = async () => {
    setIsRunning(true);
    setResults([]);
    setSummary(null);

    const startTime = performance.now();
    const outcome = await Phase1TestRunner.runAllTests();
    const totalDuration = Math.round(performance.now() - startTime);

    setResults(outcome.results);
    setSummary({
      passed: outcome.passedCount,
      failed: outcome.failedCount,
      totalDuration,
    });
    setIsRunning(false);
  };

  const getSuiteIcon = (suite: string) => {
    switch (suite) {
      case 'Database':
        return <Database className="w-4 h-4 text-blue-500" />;
      case 'Authentication':
        return <Lock className="w-4 h-4 text-emerald-500" />;
      case 'Sync Foundation':
        return <RefreshCw className="w-4 h-4 text-amber-500" />;
      case 'Backup & Safety':
        return <HardDrive className="w-4 h-4 text-violet-500" />;
      default:
        return <FlaskConical className="w-4 h-4 text-neutral-400" />;
    }
  };

  return (
    <div className="fixed inset-0 bg-black/60 backdrop-blur-xs flex items-center justify-center p-4 z-50">
      <div
        className={`w-full max-w-3xl rounded-xl border shadow-2xl overflow-hidden flex flex-col max-h-[85vh] ${
          darkMode ? 'bg-neutral-900 border-neutral-800 text-neutral-100' : 'bg-white border-neutral-200 text-neutral-900'
        }`}
      >
        {/* Modal Header */}
        <div className="p-4 border-b border-neutral-200 dark:border-neutral-800 flex items-center justify-between">
          <div className="flex items-center gap-2.5">
            <div className="p-2 rounded-lg bg-emerald-500/10 text-emerald-600 dark:text-emerald-400">
              <FlaskConical className="w-5 h-5" />
            </div>
            <div>
              <h2 className="text-sm font-bold">Phase 1 Automated Test Verification Suite</h2>
              <p className="text-[11px] text-neutral-500">
                Database, PBKDF2 Auth, 30-Day Session, Sync Queue, Tenant Isolation & Backup
              </p>
            </div>
          </div>

          <div className="flex items-center gap-3">
            <button
              id="btn-execute-all-phase1-tests"
              onClick={handleRunTests}
              disabled={isRunning}
              className="flex items-center gap-1.5 px-3 py-1.5 bg-emerald-600 hover:bg-emerald-500 disabled:opacity-50 text-white text-xs font-semibold rounded-lg shadow-xs transition-colors"
            >
              {isRunning ? (
                <>
                  <RefreshCw className="w-3.5 h-3.5 animate-spin" />
                  <span>Running Assertions...</span>
                </>
              ) : (
                <>
                  <Play className="w-3.5 h-3.5" />
                  <span>Run All Tests</span>
                </>
              )}
            </button>

            <button
              onClick={onClose}
              className="p-1 rounded-md text-neutral-400 hover:text-neutral-600 dark:hover:text-neutral-200"
            >
              <X className="w-5 h-5" />
            </button>
          </div>
        </div>

        {/* Summary Bar */}
        {summary && (
          <div
            className={`px-4 py-3 border-b flex items-center justify-between text-xs font-medium ${
              summary.failed === 0
                ? 'bg-emerald-50 dark:bg-emerald-950/30 text-emerald-800 dark:text-emerald-300 border-emerald-200 dark:border-emerald-800'
                : 'bg-red-50 dark:bg-red-950/30 text-red-800 dark:text-red-300 border-red-200 dark:border-red-800'
            }`}
          >
            <div className="flex items-center gap-4">
              <span className="flex items-center gap-1.5 font-bold">
                {summary.failed === 0 ? (
                  <CheckCircle2 className="w-4 h-4 text-emerald-600" />
                ) : (
                  <XCircle className="w-4 h-4 text-red-600" />
                )}
                {summary.failed === 0 ? 'ALL PHASE 1 TESTS PASSED' : 'TESTS FAILED'}
              </span>
              <span>•</span>
              <span>Passed: {summary.passed}</span>
              <span>•</span>
              <span>Failed: {summary.failed}</span>
            </div>
            <div className="flex items-center gap-1 text-[11px] text-neutral-500">
              <Clock className="w-3 h-3" />
              <span>{summary.totalDuration} ms</span>
            </div>
          </div>
        )}

        {/* Test Results List */}
        <div className="flex-1 overflow-y-auto p-4 space-y-2">
          {results.length === 0 && !isRunning && (
            <div className="py-12 text-center text-neutral-400 text-xs">
              <FlaskConical className="w-8 h-8 text-neutral-400 mx-auto mb-2" />
              <p className="font-semibold text-neutral-600 dark:text-neutral-300">
                Test runner ready
              </p>
              <p className="text-[11px] mt-1 text-neutral-500">
                Click "Run All Tests" above to execute verification on database models, security, and sync.
              </p>
            </div>
          )}

          {results.map((res, idx) => (
            <div
              key={idx}
              className={`p-3 rounded-lg border flex items-start justify-between gap-3 text-xs transition-colors ${
                res.passed
                  ? 'bg-neutral-50/50 dark:bg-neutral-800/30 border-neutral-200 dark:border-neutral-800'
                  : 'bg-red-500/10 border-red-500/30'
              }`}
            >
              <div className="flex items-start gap-2.5">
                <div className="mt-0.5">{getSuiteIcon(res.suite)}</div>
                <div>
                  <div className="flex items-center gap-2">
                    <span className="font-semibold text-neutral-900 dark:text-neutral-100">
                      {res.testName}
                    </span>
                    <span className="text-[10px] px-1.5 py-0.2 rounded bg-neutral-200 dark:bg-neutral-800 text-neutral-600 dark:text-neutral-400">
                      {res.suite}
                    </span>
                  </div>
                  <p
                    className={`text-[11px] mt-0.5 ${
                      res.passed ? 'text-neutral-500 dark:text-neutral-400' : 'text-red-500 font-medium'
                    }`}
                  >
                    {res.message}
                  </p>
                </div>
              </div>

              <div className="flex items-center gap-2 shrink-0">
                <span className="font-mono text-[10px] text-neutral-400">
                  {res.durationMs}ms
                </span>
                {res.passed ? (
                  <CheckCircle2 className="w-4 h-4 text-emerald-500" />
                ) : (
                  <XCircle className="w-4 h-4 text-red-500" />
                )}
              </div>
            </div>
          ))}
        </div>

        {/* Footer */}
        <div className="p-3 border-t border-neutral-200 dark:border-neutral-800 text-[11px] text-neutral-500 flex items-center justify-between">
          <div className="flex items-center gap-1">
            <ShieldCheck className="w-3.5 h-3.5 text-emerald-600" />
            <span>Deterministic PBKDF2 • Multi-Tenant Isolation • PKR Decimal Model</span>
          </div>
          <button
            onClick={onClose}
            className="px-3 py-1 rounded bg-neutral-200 dark:bg-neutral-800 hover:bg-neutral-300 dark:hover:bg-neutral-700 text-neutral-700 dark:text-neutral-300 font-medium"
          >
            Close
          </button>
        </div>
      </div>
    </div>
  );
};
