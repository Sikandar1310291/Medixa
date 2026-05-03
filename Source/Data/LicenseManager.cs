using System;
using System.IO;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Web.Script.Serialization;

namespace PharmaBilling.Source.Data
{
    /// <summary>
    /// Manages Medixa license verification and PC activation tracking.
    ///
    /// Every time a license is verified successfully, this class calls
    /// RegisterActivation() which upserts a record to the Supabase
    /// license_activations table. This lets the developer see at a glance:
    ///   - Which customers have active installations
    ///   - How many PCs each license key is running on
    ///   - When each PC last connected (last_seen timestamp)
    /// </summary>
    public class LicenseManager
    {
        // ── Supabase Config ──────────────────────────────────────────────────────
        private static readonly string SupabaseUrl = "https://idnfkbgswrbhmqzsnxnk.supabase.co";
        private static readonly string SupabaseKey = "sb_publishable_-Uwrrbhxubrc3dDYDw6gMw_1xRb2oYd";
        private const string AppVersion = "1.6";

        // ── Local Cache Paths ────────────────────────────────────────────────────
        private static readonly string AppDataFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "MedixaPharmacy",
            Environment.MachineName);

        private static readonly string LicenseFilePath = Path.Combine(AppDataFolder, "license_key.txt");
        private static readonly string LastCheckPath   = Path.Combine(AppDataFolder, "license_check.txt");

        // ── Public Properties ─────────────────────────────────────────────────
        public static string CurrentLicenseKey
        {
            get
            {
                if (File.Exists(LicenseFilePath))
                    return File.ReadAllText(LicenseFilePath).Trim();
                return string.Empty;
            }
        }

        public static void SaveLicenseKey(string key)
        {
            if (!Directory.Exists(AppDataFolder))
                Directory.CreateDirectory(AppDataFolder);
            File.WriteAllText(LicenseFilePath, key.Trim());
        }

        // ── Validation Result ─────────────────────────────────────────────────
        public class LicenseValidationResult
        {
            public bool IsValid { get; set; }
            public string Message { get; set; }
            public string ClientName { get; set; }
            public DateTime? ExpiryDate { get; set; }
        }

        // ── License Verification ──────────────────────────────────────────────
        public static LicenseValidationResult VerifyLicense(string keyStr)
        {
            string key = keyStr != null ? keyStr.Trim() : "";
            if (string.IsNullOrEmpty(key))
                return new LicenseValidationResult { IsValid = false, Message = "Please enter a License Key." };

            try
            {
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

                string url = SupabaseUrl + "/rest/v1/licenses?select=*&license_key=eq." + Uri.EscapeDataString(key);
                var request = (HttpWebRequest)WebRequest.Create(url);
                request.Method = "GET";
                request.Headers.Add("apikey", SupabaseKey);
                request.Headers.Add("Authorization", "Bearer " + SupabaseKey);
                request.Timeout = 10000;

                using (var response = (HttpWebResponse)request.GetResponse())
                using (var reader = new StreamReader(response.GetResponseStream()))
                {
                    string jsonResponse = reader.ReadToEnd();
                    var jss = new JavaScriptSerializer();
                    var array = jss.Deserialize<dynamic>(jsonResponse) as object[];

                    if (array != null && array.Length > 0)
                    {
                        var record = array[0] as System.Collections.Generic.Dictionary<string, object>;
                        string status     = record.ContainsKey("status")      ? (record["status"]      != null ? record["status"].ToString()      : "") : "";
                        string clientName = record.ContainsKey("client_name") ? (record["client_name"] != null ? record["client_name"].ToString() : "") : "";

                        DateTime expiryDate = DateTime.MinValue;
                        bool hasExpiry = record.ContainsKey("expiry_date") &&
                                         record["expiry_date"] != null &&
                                         DateTime.TryParse(record["expiry_date"].ToString(), out expiryDate);

                        if (status.ToLower() != "active")
                            return new LicenseValidationResult { IsValid = false, Message = "Your license is disabled or suspended by Admin." };

                        if (hasExpiry && expiryDate.Date < DateTime.Now.Date)
                            return new LicenseValidationResult { IsValid = false, Message = "Your subscription expired on " + expiryDate.ToString("dd-MMM-yyyy") + ". Please renew." };

                        // ── SUCCESS: save key + register this PC's activation ──
                        SaveLicenseKey(key);
                        File.WriteAllText(LastCheckPath, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));

                        // Fire-and-forget registration and cloud restore
                        System.Threading.Tasks.Task.Run(() => {
                            RegisterActivation(key);
                            var counts = CloudSyncService.RestoreFromCloud(key);
                            if (counts.Item1 > 0 || counts.Item2 > 0)
                            {
                                // Show a notification? We are in a background thread, so we'd have to invoke UI.
                                // It's better to just log it or refresh cache.
                                AppCache.RefreshMedicines();
                            }
                        });

                        return new LicenseValidationResult
                        {
                            IsValid = true,
                            Message = "Verified",
                            ClientName = clientName,
                            ExpiryDate = hasExpiry ? (DateTime?)expiryDate : null
                        };
                    }
                    else
                    {
                        return new LicenseValidationResult { IsValid = false, Message = "Invalid License Key. Not found in database." };
                    }
                }
            }
            catch (WebException)
            {
                return HandleOfflineMode(key);
            }
            catch (Exception ex)
            {
                return new LicenseValidationResult { IsValid = false, Message = "Error checking license: " + ex.Message };
            }
        }

        // ── PC Activation Tracking ────────────────────────────────────────────
        /// <summary>
        /// Registers (or updates) a heartbeat in the Supabase license_activations table.
        /// Called silently in the background after each successful license verification.
        ///
        /// This lets you see from the Supabase dashboard:
        ///   SELECT * FROM license_summary;  -- Activations per license key
        ///   SELECT * FROM license_activations WHERE license_key = 'your-key';
        /// </summary>
        public static void RegisterActivation(string licenseKey)
        {
            try
            {
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

                string machineId   = GetStableMachineId();
                string machineName = Environment.MachineName;
                string ipAddress   = GetLocalIP();
                string mode        = AppConfig.Current.IsServer ? "server" : "client";
                string osInfo      = Environment.OSVersion.ToString();
                string nowIso      = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");

                // Build the upsert payload for Supabase REST API
                var payload = new System.Collections.Generic.Dictionary<string, object>
                {
                    { "license_key",   licenseKey  },
                    { "machine_id",    machineId   },
                    { "machine_name",  machineName },
                    { "ip_address",    ipAddress   },
                    { "app_version",   AppVersion  },
                    { "mode",          mode        },
                    { "os_info",       osInfo      },
                    { "first_seen",    nowIso      },  // ignored on update (see SQL DEFAULT)
                    { "last_seen",     nowIso      },
                    { "is_active",     true        }
                };

                string json = new JavaScriptSerializer().Serialize(payload);
                byte[] data = Encoding.UTF8.GetBytes(json);

                // POST with Prefer: resolution=merge-duplicates → UPSERT
                // ON CONFLICT (license_key, machine_id) → updates last_seen
                string url = SupabaseUrl + "/rest/v1/license_activations";
                var request = (HttpWebRequest)WebRequest.Create(url);
                request.Method = "POST";
                request.ContentType = "application/json";
                request.ContentLength = data.Length;
                request.Timeout = 8000;
                request.Headers.Add("apikey", SupabaseKey);
                request.Headers.Add("Authorization", "Bearer " + SupabaseKey);
                request.Headers.Add("Prefer", "resolution=merge-duplicates,return=minimal");

                using (var stream = request.GetRequestStream())
                    stream.Write(data, 0, data.Length);

                using (var response = (HttpWebResponse)request.GetResponse())
                {
                    // 200/201 = success. We don't need the response body.
                }
            }
            catch
            {
                // Silently swallow — activation tracking must NEVER break the app.
            }
        }

        // ── Machine ID Generation ─────────────────────────────────────────────
        /// <summary>
        /// Generates a stable, unique 32-char ID for this PC.
        /// Based on hostname + primary MAC address, MD5-hashed.
        /// This ID stays the same across reboots, reinstalls, and updates.
        /// </summary>
        public static string GetStableMachineId()
        {
            try
            {
                string mac = GetPrimaryMacAddress();
                string raw = Environment.MachineName + "|" + mac;
                using (var md5 = MD5.Create())
                {
                    byte[] hash = md5.ComputeHash(Encoding.UTF8.GetBytes(raw));
                    return BitConverter.ToString(hash).Replace("-", "").ToLower();
                }
            }
            catch
            {
                // Fallback: username + machine name
                return Environment.MachineName.ToLower().PadRight(32, '0').Substring(0, 32);
            }
        }

        private static string GetPrimaryMacAddress()
        {
            foreach (var nic in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (nic.OperationalStatus == OperationalStatus.Up &&
                    nic.NetworkInterfaceType != NetworkInterfaceType.Loopback &&
                    nic.NetworkInterfaceType != NetworkInterfaceType.Tunnel)
                {
                    string mac = nic.GetPhysicalAddress().ToString();
                    if (!string.IsNullOrEmpty(mac) && mac != "000000000000")
                        return mac;
                }
            }
            return "NOMAC";
        }

        private static string GetLocalIP()
        {
            try
            {
                foreach (var nic in NetworkInterface.GetAllNetworkInterfaces())
                {
                    if (nic.OperationalStatus != OperationalStatus.Up) continue;
                    if (nic.NetworkInterfaceType == NetworkInterfaceType.Loopback) continue;
                    foreach (var addr in nic.GetIPProperties().UnicastAddresses)
                    {
                        if (addr.Address.AddressFamily == AddressFamily.InterNetwork)
                        {
                            string ip = addr.Address.ToString();
                            if (ip.StartsWith("192.168.") || ip.StartsWith("10."))
                                return ip;
                        }
                    }
                }
                return "unknown";
            }
            catch { return "unknown"; }
        }

        // ── Offline Fallback ──────────────────────────────────────────────
        // Grace period: 30 days from last successful online verification.
        // Every time TryBackgroundHeartbeat() succeeds, the 30-day window
        // resets — so a client who is offline for 10 days and then gets
        // internet has their window automatically refreshed.
        private static LicenseValidationResult HandleOfflineMode(string key)
        {
            if (File.Exists(LastCheckPath))
            {
                string lastCheckStr = File.ReadAllText(LastCheckPath);
                DateTime lastCheck;
                if (DateTime.TryParse(lastCheckStr, out lastCheck))
                {
                    int daysOffline = (int)(DateTime.Now - lastCheck).TotalDays;
                    if (daysOffline <= 30)
                        return new LicenseValidationResult
                        {
                            IsValid = true,
                            Message = "Offline Mode (last verified " + daysOffline + " day(s) ago)"
                        };
                    else
                        return new LicenseValidationResult
                        {
                            IsValid = false,
                            Message = "License verification required. Please connect to internet. Last sync was " + daysOffline + " days ago."
                        };
                }
            }
            return new LicenseValidationResult
            {
                IsValid = false,
                Message = "Internet connection required for first-time license activation."
            };
        }

        // ── Background Heartbeat ───────────────────────────────────────────────
        /// <summary>
        /// Called silently by a background timer every few minutes while the
        /// app is running.
        ///
        /// When internet comes back (even after 10+ days offline):
        ///   1. Pings Supabase to confirm license is still Active.
        ///   2. Updates last_check.txt with today's date → resets the 30-day
        ///      offline window, so the client can be offline for another 30 days.
        ///   3. Sends a full activation heartbeat (machine_id, IP, last_seen)
        ///      so the developer's dashboard shows the client is alive.
        ///
        /// If no internet: silently returns false, app continues normally.
        /// </summary>
        public static bool TryBackgroundHeartbeat()
        {
            string key = CurrentLicenseKey;
            if (string.IsNullOrEmpty(key)) return false;

            try
            {
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

                // Lightweight check: does the license still exist and is Active?
                string url = SupabaseUrl + "/rest/v1/licenses?select=status&license_key=eq." + Uri.EscapeDataString(key);
                var request = (HttpWebRequest)WebRequest.Create(url);
                request.Method = "GET";
                request.Headers.Add("apikey", SupabaseKey);
                request.Headers.Add("Authorization", "Bearer " + SupabaseKey);
                request.Timeout = 6000; // 6s — fail fast if no internet

                using (var response = (HttpWebResponse)request.GetResponse())
                using (var reader = new StreamReader(response.GetResponseStream()))
                {
                    reader.ReadToEnd(); // consume response

                    // Internet is back! Reset the 30-day offline window.
                    if (!Directory.Exists(AppDataFolder))
                        Directory.CreateDirectory(AppDataFolder);
                    File.WriteAllText(LastCheckPath, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));

                    // Push full activation heartbeat in the background
                    System.Threading.Tasks.Task.Run(() => RegisterActivation(key));

                    return true;
                }
            }
            catch
            {
                // No internet or timeout — silently ignore, app keeps running
                return false;
            }
        }
    }
}
