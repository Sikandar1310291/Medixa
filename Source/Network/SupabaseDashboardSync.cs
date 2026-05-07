using System;
using System.IO;
using System.Net;
using System.Text;
using System.Web.Script.Serialization;
using PharmaBilling.Source.Data;

namespace PharmaBilling.Source.Network
{
    /// <summary>
    /// Syncs the Cloudflare tunnel URL to Supabase whenever the tunnel (re)starts.
    /// This allows the pharmacy owner — from anywhere in the world — to visit a
    /// single fixed lookup URL, enter their License Key, and instantly find their
    /// current live dashboard link.
    ///
    /// Supabase table required (run once in Supabase SQL editor):
    /// ─────────────────────────────────────────────────────────────
    ///   CREATE TABLE IF NOT EXISTS pharmacy_dashboard (
    ///     license_key   TEXT PRIMARY KEY,
    ///     pharmacy_name TEXT,
    ///     tunnel_url    TEXT,
    ///     owner_pin     TEXT DEFAULT '1234',
    ///     updated_at    TIMESTAMPTZ DEFAULT now()
    ///   );
    ///   ALTER TABLE pharmacy_dashboard ENABLE ROW LEVEL SECURITY;
    ///   CREATE POLICY "read_own" ON pharmacy_dashboard
    ///     FOR SELECT USING (true);   -- anon can read (license_key is the secret)
    ///   CREATE POLICY "write_own" ON pharmacy_dashboard
    ///     FOR ALL USING (true);      -- anon key upsert allowed
    /// ─────────────────────────────────────────────────────────────
    /// </summary>
    public static class SupabaseDashboardSync
    {
        // Reuse the same Supabase project as LicenseManager
        private const string SupabaseUrl = "https://idnfkbgswrbhmqzsnxnk.supabase.co";
        private const string SupabaseKey = "sb_publishable_-Uwrrbhxubrc3dDYDw6gMw_1xRb2oYd";

        /// <summary>
        /// Called (fire-and-forget) whenever CloudflareManager fires TunnelReady.
        /// Upserts { license_key, pharmacy_name, tunnel_url, updated_at } to Supabase.
        /// </summary>
        public static void SaveTunnelUrl(string tunnelUrl)
        {
            try
            {
                string licenseKey = LicenseManager.CurrentLicenseKey;
                if (string.IsNullOrWhiteSpace(licenseKey)) return;
                if (string.IsNullOrWhiteSpace(tunnelUrl))  return;

                string pharmName  = SafePharmName();
                string nowIso     = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");

                var jss     = new JavaScriptSerializer();
                string body = jss.Serialize(new
                {
                    license_key   = licenseKey,
                    pharmacy_name = pharmName,
                    tunnel_url    = tunnelUrl,
                    updated_at    = nowIso
                });

                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

                var req = (HttpWebRequest)WebRequest.Create(
                    SupabaseUrl + "/rest/v1/pharmacy_dashboard");
                req.Method      = "POST";
                req.ContentType = "application/json";
                req.Timeout     = 15000;
                req.Headers.Add("apikey",         SupabaseKey);
                req.Headers.Add("Authorization",  "Bearer " + SupabaseKey);
                req.Headers.Add("Prefer",         "resolution=merge-duplicates");

                byte[] data = Encoding.UTF8.GetBytes(body);
                req.ContentLength = data.Length;
                using (var s = req.GetRequestStream())
                    s.Write(data, 0, data.Length);

                using (var resp = (HttpWebResponse)req.GetResponse())
                {
                    Log("SUCCESS: URL synced to Supabase: " + tunnelUrl);
                }
            }
            catch (Exception ex)
            {
                Log("ERROR: Sync failed: " + ex.Message);
            }
        }

        private static void Log(string msg)
        {
            try
            {
                string path = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "MedixaPharmacy", "dashboard_sync.log");
                string line = string.Format("[{0}] {1}\r\n", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), msg);
                File.AppendAllText(path, line);
            }
            catch { }
        }

        private static string SafePharmName()
        {
            try { return ApiClient.Instance.PharmacyName ?? "Medixa Pharmacy"; }
            catch { return "Medixa Pharmacy"; }
        }
    }
}
