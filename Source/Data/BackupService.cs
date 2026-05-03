using System;
using System.IO;
using System.Data.SQLite;
using System.Linq;
using System.Threading.Tasks;
using System.Net;
using System.IO.Compression;

namespace PharmaBilling.Source.Data
{
    /// <summary>
    /// Google-grade automatic backup service.
    /// - Takes one snapshot per day silently on startup.
    /// - Keeps 365 days (1 full year) of daily backups.
    /// - Saves to local Backups folder AND secondary drive (D:\ / E:\) if present.
    /// - Cleans up backups older than 365 days automatically.
    /// - Never blocks the UI (runs fully async in background).
    /// </summary>
    public static class BackupService
    {
        private const int RetentionDays = 365;          // 1 full year of backups
        private static readonly string[] SecondaryDrives = { "D:\\", "E:\\", "F:\\", "G:\\" };

        private static System.Timers.Timer _heartbeatTimer;
        private static string _currentDbPath;

        /// <summary>
        /// Call this once on app startup. Non-blocking, fully background.
        /// Starts the CDP (Continuous Data Protection) Heartbeat.
        /// </summary>
        public static void StartAsync(string sourceDbPath)
        {
            _currentDbPath = sourceDbPath;
            
            // 1. Run immediate backup on startup
            Task.Run(() =>
            {
                try { RunBackupNow(sourceDbPath); }
                catch { /* Silent */ }
            });

            // 2. Initialize the Heartbeat (Every 60 minutes)
            if (_heartbeatTimer == null)
            {
                _heartbeatTimer = new System.Timers.Timer(3600000); // 1 Hour
                _heartbeatTimer.Elapsed += (s, e) => 
                {
                    try { RunBackupNow(_currentDbPath); } catch { }
                };
                _heartbeatTimer.AutoReset = true;
                _heartbeatTimer.Enabled = true;
            }
        }

        /// <summary>
        /// Runs a full backup immediately. Can be called from UI for manual backup.
        /// </summary>
        public static void RunBackupNow(string sourceDbPath)
        {
            if (!File.Exists(sourceDbPath)) return;

            string today = DateTime.Now.ToString("yyyy-MM-dd");
            string backupFileName = string.Format("Medixa_Backup_{0}.sqlite", today);

            // --- PRIMARY BACKUP: Local Backups folder ---
            string localBackupDir = Path.Combine(Path.GetDirectoryName(sourceDbPath), "Backups");
            Directory.CreateDirectory(localBackupDir);
            string localBackupPath = Path.Combine(localBackupDir, backupFileName);

            // Always overwrite today's backup on manual trigger
            CreateBackup(sourceDbPath, localBackupPath);
            CleanOldBackups(localBackupDir);

            // --- SECONDARY BACKUP: External / Secondary Drive ---
            foreach (var drive in SecondaryDrives)
            {
                if (Directory.Exists(drive))
                {
                    try
                    {
                        string secondaryDir = Path.Combine(drive, "MedixaBackups");
                        Directory.CreateDirectory(secondaryDir);
                        string secondaryPath = Path.Combine(secondaryDir, backupFileName);
                        CreateBackup(sourceDbPath, secondaryPath);
                        CleanOldBackups(secondaryDir);
                    }
                    catch { }
                    break;
                }
            }

            // --- LAYER 3: Cloud Folder Backup (OneDrive / Google Drive / Dropbox) ---
            // If Windows gets formatted, data is still safe in the cloud!
            BackupToCloudFolders(sourceDbPath, backupFileName);

            // --- LAYER 4: SaaS Supabase Upload (Master Backup) ---
            BackupToSupabaseCloud(sourceDbPath, backupFileName);
        }

        /// <summary>
        /// Detects OneDrive, Google Drive, Dropbox folders and backs up there.
        /// This ensures data survives even if Windows is fully reinstalled.
        /// </summary>
        private static void BackupToCloudFolders(string sourceDbPath, string backupFileName)
        {
            string userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

            // Possible cloud sync folder locations
            var cloudCandidates = new[]
            {
                // OneDrive (Personal & Work)
                Path.Combine(userProfile, "OneDrive", "MedixaBackups"),
                Path.Combine(userProfile, "OneDrive - Personal", "MedixaBackups"),
                Environment.GetEnvironmentVariable("OneDrive") != null
                    ? Path.Combine(Environment.GetEnvironmentVariable("OneDrive"), "MedixaBackups") : null,

                // Google Drive (Desktop app default paths)
                Path.Combine(userProfile, "Google Drive", "MedixaBackups"),
                Path.Combine(userProfile, "My Drive", "MedixaBackups"),
                @"C:\Users\" + Environment.UserName + @"\Google Drive\MedixaBackups",

                // Dropbox
                Path.Combine(userProfile, "Dropbox", "MedixaBackups"),
            };

            foreach (var cloudDir in cloudCandidates)
            {
                if (cloudDir == null) continue;

                // Only use it if its parent folder exists (meaning the cloud app is installed)
                string parentDir = Directory.GetParent(cloudDir) != null 
                    ? Directory.GetParent(cloudDir).FullName : null;
                if (parentDir == null || !Directory.Exists(parentDir)) continue;

                try
                {
                    Directory.CreateDirectory(cloudDir);
                    string cloudPath = Path.Combine(cloudDir, backupFileName);
                    CreateBackup(sourceDbPath, cloudPath);
                    CleanOldBackups(cloudDir);
                    break; // Only need one cloud location
                }
                catch { }
            }
        }

        /// <summary>
        /// Compresses the database and uploads it to the main Supabase layer.
        /// </summary>
        private static void BackupToSupabaseCloud(string sourceDbPath, string backupFileName)
        {
            string licenseKey = LicenseManager.CurrentLicenseKey;
            if (string.IsNullOrEmpty(licenseKey)) return;

            string supabaseUrl = "https://idnfkbgswrbhmqzsnxnk.supabase.co";
            string supabaseKey = "sb_publishable_-Uwrrbhxubrc3dDYDw6gMw_1xRb2oYd";
            string zipName = backupFileName + ".zip";

            try
            {
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                byte[] zippedBytes;

                using (var ms = new MemoryStream())
                {
                    using (var archive = new ZipArchive(ms, ZipArchiveMode.Create, true))
                    {
                        var zipEntry = archive.CreateEntry(backupFileName);
                        using (var entryStream = zipEntry.Open())
                        using (var fileStream = new FileStream(sourceDbPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                        {
                            fileStream.CopyTo(entryStream);
                        }
                    }
                    zippedBytes = ms.ToArray();
                }

                // POST /storage/v1/object/firm-backups/{licenseKey}/{zipName}
                string url = string.Format("{0}/storage/v1/object/firm-backups/{1}/{2}", supabaseUrl, licenseKey, zipName);
                var request = (HttpWebRequest)WebRequest.Create(url);
                request.Method = "POST";
                request.Headers.Add("apikey", supabaseKey);
                request.Headers.Add("Authorization", "Bearer " + supabaseKey);
                request.ContentType = "application/zip";
                
                using (var reqStream = request.GetRequestStream())
                {
                    reqStream.Write(zippedBytes, 0, zippedBytes.Length);
                }

                using (var response = (HttpWebResponse)request.GetResponse())
                {
                    // Uploaded successfully
                }
            }
            catch (Exception)
            {
                // Silent catch for background service
            }
        }

        /// <summary>
        /// Uses SQLite Online Backup API for a crash-safe, consistent snapshot.
        /// This is the SAME method used by Google Firebase and production systems.
        /// It works even while the database is in use.
        /// </summary>
        private static void CreateBackup(string sourceDbPath, string destinationPath)
        {
            using (var sourceConn = new SQLiteConnection("Data Source=" + sourceDbPath + ";Version=3;"))
            using (var destConn = new SQLiteConnection("Data Source=" + destinationPath + ";Version=3;"))
            {
                sourceConn.Open();
                destConn.Open();
                sourceConn.BackupDatabase(destConn, "main", "main", -1, null, 0);
            }
        }

        /// <summary>
        /// Removes backups older than 365 days to maintain 1 year retention window.
        /// </summary>
        private static void CleanOldBackups(string backupDir)
        {
            var cutoffDate = DateTime.Now.AddDays(-RetentionDays);
            var oldFiles = Directory.GetFiles(backupDir, "Medixa_Backup_*.sqlite")
                .Where(f =>
                {
                    string datePart = Path.GetFileNameWithoutExtension(f).Replace("Medixa_Backup_", "");
                    DateTime parsed;
                    return DateTime.TryParse(datePart, out parsed) && parsed < cutoffDate;
                });

            foreach (var file in oldFiles)
            {
                try { File.Delete(file); } catch { }
            }
        }

        /// <summary>
        /// Returns the path where local backups are stored (for UI display).
        /// </summary>
        public static string GetLocalBackupFolder(string sourceDbPath)
        {
            return Path.Combine(Path.GetDirectoryName(sourceDbPath), "Backups");
        }

        /// <summary>
        /// Returns today's backup count and total backup count.
        /// </summary>
        public static BackupStatus GetStatus(string sourceDbPath)
        {
            string backupDir = Path.Combine(Path.GetDirectoryName(sourceDbPath), "Backups");
            if (!Directory.Exists(backupDir))
                return new BackupStatus { TotalBackups = 0, LatestBackupDate = "None" };

            var files = Directory.GetFiles(backupDir, "Medixa_Backup_*.sqlite")
                .OrderByDescending(f => f).ToList();

            return new BackupStatus
            {
                TotalBackups = files.Count,
                LatestBackupDate = files.Count > 0
                    ? Path.GetFileNameWithoutExtension(files[0]).Replace("Medixa_Backup_", "")
                    : "None"
            };
        }
    }

    public class BackupStatus
    {
        public int TotalBackups { get; set; }
        public string LatestBackupDate { get; set; }
    }
}
