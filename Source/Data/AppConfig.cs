using System;
using System.IO;
using System.Web.Script.Serialization;

namespace PharmaBilling.Source.Data
{
    public class NetworkSettings
    {
        public bool IsServer { get; set; }
        public string ServerIP { get; set; }
        public string SharedFolderName { get; set; }

        public NetworkSettings()
        {
            IsServer = true;
            ServerIP = "127.0.0.1";
            SharedFolderName = "Database";
        }
    }

    public static class AppConfig
    {
        // Store config in C:\ProgramData\Medixa\ — same safe location as the database.
        // This is OUTSIDE OneDrive so it is never corrupted by cloud sync resets.
        // It persists across app reinstalls (unlike storing next to the .exe).
        private static readonly string SafeDataDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Medixa");

        private static string ConfigPath
        {
            get { return Path.Combine(SafeDataDir, "network_config.json"); }
        }

        private static NetworkSettings _currentSettings;

        public static NetworkSettings Current
        {
            get
            {
                if (_currentSettings == null)
                    _currentSettings = Load();
                return _currentSettings;
            }
        }

        /// <summary>
        /// Reload config from disk (call after Save to pick up new settings immediately).
        /// </summary>
        public static void Reload()
        {
            _currentSettings = null;
        }

        public static NetworkSettings Load()
        {
            try
            {
                // Ensure the directory always exists
                if (!Directory.Exists(SafeDataDir))
                    Directory.CreateDirectory(SafeDataDir);

                if (File.Exists(ConfigPath))
                {
                    string json = File.ReadAllText(ConfigPath);
                    JavaScriptSerializer serializer = new JavaScriptSerializer();
                    var settings = serializer.Deserialize<NetworkSettings>(json);
                    if (settings != null) return settings;
                }
            }
            catch { }

            // Also try the legacy location (bin\Debug or install dir)
            // so we can migrate existing user settings.
            try
            {
                string legacyPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "network_config.json");
                if (File.Exists(legacyPath))
                {
                    string json = File.ReadAllText(legacyPath);
                    JavaScriptSerializer serializer = new JavaScriptSerializer();
                    var legacy = serializer.Deserialize<NetworkSettings>(json);
                    if (legacy != null)
                    {
                        // Migrate to safe location and delete old file
                        Save(legacy);
                        try { File.Delete(legacyPath); } catch { }
                        return legacy;
                    }
                }
            }
            catch { }

            // Absolute default: Server mode
            return new NetworkSettings();
        }

        public static void Save(NetworkSettings settings)
        {
            try
            {
                if (!Directory.Exists(SafeDataDir))
                    Directory.CreateDirectory(SafeDataDir);

                JavaScriptSerializer serializer = new JavaScriptSerializer();
                string json = serializer.Serialize(settings);
                File.WriteAllText(ConfigPath, json);
                _currentSettings = settings;
            }
            catch (Exception ex)
            {
                throw new Exception("Could not save network configuration: " + ex.Message);
            }
        }
    }
}
