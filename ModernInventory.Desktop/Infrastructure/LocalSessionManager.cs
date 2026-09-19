using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace ModernInventory.Desktop.Infrastructure
{
    public class LocalSessionRecord
    {
        public string Token { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public DateTime SavedAtUtc { get; set; }
    }

    /// <summary>
    /// Provides persistent session management. Sessions never expire automatically
    /// across restarts until the user explicitly clicks Sign Out / Logout.
    /// </summary>
    public static class LocalSessionManager
    {
        private static string SessionFilePath => Path.Combine(LocalPathProvider.SettingsDirectory, "session.json");
        private static readonly object _fileLock = new();

        public static async Task SaveSessionAsync(string token, string email)
        {
            if (string.IsNullOrWhiteSpace(token)) return;

            try
            {
                LocalPathProvider.EnsureDirectories();

                var record = new LocalSessionRecord
                {
                    Token = token,
                    Email = email,
                    SavedAtUtc = DateTime.UtcNow
                };

                var json = JsonSerializer.Serialize(record, new JsonSerializerOptions { WriteIndented = true });

                lock (_fileLock)
                {
                    File.WriteAllText(SessionFilePath, json);
                }

                LocalPathProvider.LogInfo($"Persistent session saved locally for {email}.");
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                LocalPathProvider.LogError("SaveSession", ex);
            }
        }

        public static async Task<string?> GetSavedTokenAsync()
        {
            try
            {
                if (!File.Exists(SessionFilePath)) return null;

                string json;
                lock (_fileLock)
                {
                    json = File.ReadAllText(SessionFilePath);
                }

                if (string.IsNullOrWhiteSpace(json)) return null;

                var record = JsonSerializer.Deserialize<LocalSessionRecord>(json);
                await Task.CompletedTask;
                return record?.Token;
            }
            catch (Exception ex)
            {
                LocalPathProvider.LogError("GetSavedToken", ex);
                return null;
            }
        }

        public static async Task ClearSessionAsync()
        {
            try
            {
                lock (_fileLock)
                {
                    if (File.Exists(SessionFilePath))
                    {
                        File.Delete(SessionFilePath);
                    }
                }
                LocalPathProvider.LogInfo("Persistent session cleared on explicit logout.");
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                LocalPathProvider.LogError("ClearSession", ex);
            }
        }
    }
}
