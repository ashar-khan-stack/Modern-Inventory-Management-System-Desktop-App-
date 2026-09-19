using System;
using System.IO;

namespace ModernInventory.Desktop.Infrastructure
{
    /// <summary>
    /// Guarantees robust, writable local storage for SQLite database, backups, logs, and settings
    /// without relying on any cloud infrastructure or external services.
    /// Handles permissions for both portable and Program Files installations.
    /// </summary>
    public static class LocalPathProvider
    {
        private static readonly object _lock = new();
        private static string? _resolvedDataDirectory;

        public static string DataDirectory
        {
            get
            {
                if (_resolvedDataDirectory == null)
                {
                    lock (_lock)
                    {
                        if (_resolvedDataDirectory == null)
                        {
                            _resolvedDataDirectory = ResolveWritableDataDirectory();
                        }
                    }
                }
                return _resolvedDataDirectory;
            }
        }

        public static string DatabasePath => Path.Combine(DataDirectory, "modern_inventory.db");
        public static string BackupsDirectory => Path.Combine(DataDirectory, "Backups");
        public static string LogsDirectory => Path.Combine(DataDirectory, "Logs");
        public static string SettingsDirectory => Path.Combine(DataDirectory, "Settings");

        public static string StoragePathDescription => $"Local Storage: {DataDirectory}";

        public static void EnsureDirectories()
        {
            try
            {
                if (!Directory.Exists(DataDirectory))
                {
                    Directory.CreateDirectory(DataDirectory);
                }

                if (!Directory.Exists(BackupsDirectory))
                {
                    Directory.CreateDirectory(BackupsDirectory);
                }

                if (!Directory.Exists(LogsDirectory))
                {
                    Directory.CreateDirectory(LogsDirectory);
                }

                if (!Directory.Exists(SettingsDirectory))
                {
                    Directory.CreateDirectory(SettingsDirectory);
                }

                // Check for existing database in legacy LocalAppData path and migrate if needed
                var legacyAppDataDir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "ModernInventoryDesktop");
                var legacyDb = Path.Combine(legacyAppDataDir, "modern_inventory.db");

                if (File.Exists(legacyDb) && !File.Exists(DatabasePath))
                {
                    try
                    {
                        File.Copy(legacyDb, DatabasePath, overwrite: false);
                        LogInfo($"Preserved existing database from legacy path: {legacyDb}");
                    }
                    catch (Exception ex)
                    {
                        LogError("LegacyDatabaseCopy", ex);
                    }
                }
            }
            catch (Exception ex)
            {
                LogError("EnsureDirectories", ex);
            }
        }

        private static string ResolveWritableDataDirectory()
        {
            // 1. Try application base directory Data folder first (ideal for portable / standalone installs)
            try
            {
                var candidate = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data");
                Directory.CreateDirectory(candidate);
                var testFile = Path.Combine(candidate, ".write_test_" + Guid.NewGuid().ToString("N"));
                File.WriteAllText(testFile, "OK");
                File.Delete(testFile);
                return candidate;
            }
            catch
            {
                // Protected directory (e.g. Program Files) - fallback to LocalAppData
            }

            // 2. Guaranteed writable Windows LocalAppData directory
            var localAppDataCandidate = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "ModernInventoryDesktop",
                "Data");

            Directory.CreateDirectory(localAppDataCandidate);
            return localAppDataCandidate;
        }

        public static void LogInfo(string message)
        {
            try
            {
                lock (_lock)
                {
                    var logFile = Path.Combine(LogsDirectory, $"app_{DateTime.UtcNow:yyyy-MM-dd}.log");
                    var line = $"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss.fff}] [INFO] {message}{Environment.NewLine}";
                    File.AppendAllText(logFile, line);
                }
            }
            catch
            {
                // Never crash the UI on logging failure
            }
        }

        public static void LogError(string context, Exception ex)
        {
            try
            {
                lock (_lock)
                {
                    var logFile = Path.Combine(LogsDirectory, $"app_{DateTime.UtcNow:yyyy-MM-dd}.log");
                    var line = $"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss.fff}] [ERROR] [{context}] {ex.GetType().Name}: {ex.Message}{Environment.NewLine}{ex.StackTrace}{Environment.NewLine}";
                    File.AppendAllText(logFile, line);
                }
            }
            catch
            {
                // Never crash the UI on logging failure
            }
        }
    }
}
