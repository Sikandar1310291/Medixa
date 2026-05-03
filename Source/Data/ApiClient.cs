using System;
using System.IO;
using System.Net;
using System.Text;
using System.Web.Script.Serialization;

namespace PharmaBilling.Source.Data
{
    /// <summary>
    /// Reads config.json and communicates with PharmaServer REST API.
    /// Used by all ViewModels instead of DbHelper in LAN/client mode.
    /// In server mode, DbHelper is still used directly for performance.
    /// </summary>
    public class ApiClient
    {
        private static ApiClient _instance;
        public static ApiClient Instance
        {
            get
            {
                if (_instance == null) _instance = new ApiClient();
                return _instance;
            }
        }

        public string Mode { get; private set; }
        public string ServerIP { get; private set; }
        public int ServerPort { get; private set; }
        public string DbPath { get; private set; }
        public string PharmacyName { get; private set; }
        public string PharmacyTagline { get; private set; }
        public string ContactInfo { get; private set; }
        public string PharmacyAddress { get; private set; }
        public string PharmacyEmail { get; private set; }
        private string _baseUrl;
        private JavaScriptSerializer _json;

        public ApiClient()
        {
            _json = new JavaScriptSerializer();
            _json.MaxJsonLength = int.MaxValue;

            // Load config.json
            string configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.json");
            if (!File.Exists(configPath)) configPath = "config.json";

            if (File.Exists(configPath))
            {
                string raw = File.ReadAllText(configPath);
                dynamic cfg = _json.DeserializeObject(raw);
                var map = cfg as System.Collections.Generic.Dictionary<string, object>;
                Mode = map != null && map.ContainsKey("mode") ? map["mode"].ToString() : "server";
                ServerIP = map != null && map.ContainsKey("serverIP") ? map["serverIP"].ToString() : "localhost";
                ServerPort = map != null && map.ContainsKey("serverPort") ? Convert.ToInt32(map["serverPort"]) : 5000;
                DbPath = map != null && map.ContainsKey("dbPath") ? map["dbPath"].ToString() : "PharmaDB.sqlite";
                PharmacyName = map != null && map.ContainsKey("pharmacyName") ? map["pharmacyName"].ToString() : "PHARMASOFT";
                PharmacyTagline = map != null && map.ContainsKey("pharmacyTagline") ? map["pharmacyTagline"].ToString() : "Providing Quality Healthcare";
                ContactInfo = map != null && map.ContainsKey("contactInfo") ? map["contactInfo"].ToString() : "N/A";
                PharmacyAddress = map != null && map.ContainsKey("pharmacyAddress") ? map["pharmacyAddress"].ToString() : "N/A";
                PharmacyEmail = map != null && map.ContainsKey("pharmacyEmail") ? map["pharmacyEmail"].ToString() : "N/A";
            }
            else
            {
                Mode = "server";
                ServerIP = "localhost";
                ServerPort = 5000;
                DbPath = "PharmaDB.sqlite";
                PharmacyName = "PHARMASOFT";
                PharmacyTagline = "Providing Quality Healthcare";
                ContactInfo = "N/A";
                PharmacyAddress = "N/A";
                PharmacyEmail = "N/A";
            }

            _baseUrl = string.Format("http://{0}:{1}", ServerIP, ServerPort);
        }

        public void SaveConfig(string name, string tagline, string contact, string address, string email)
        {
            PharmacyName = name;
            PharmacyTagline = tagline;
            ContactInfo = contact;
            PharmacyAddress = address;
            PharmacyEmail = email;

            var map = new System.Collections.Generic.Dictionary<string, object>
            {
                { "mode", Mode },
                { "serverIP", ServerIP },
                { "serverPort", ServerPort },
                { "dbPath", DbPath },
                { "pharmacyName", PharmacyName },
                { "pharmacyTagline", PharmacyTagline },
                { "contactInfo", ContactInfo },
                { "pharmacyAddress", PharmacyAddress },
                { "pharmacyEmail", PharmacyEmail }
            };

            string json = _json.Serialize(map);
            string configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.json");
            System.IO.File.WriteAllText(configPath, json);
        }

        public bool IsClientMode { get { return Mode == "client"; } }

        /// <summary>Check if PharmaServer is alive</summary>
        public bool IsServerAlive()
        {
            try
            {
                Get("/api/health");
                return true;
            }
            catch { return false; }
        }

        // ── HTTP METHODS ──────────────────────────────────────────────────────
        public string Get(string endpoint)
        {
            var req = (HttpWebRequest)WebRequest.Create(_baseUrl + endpoint);
            req.Method = "GET";
            req.Timeout = 10000;
            using (var resp = (HttpWebResponse)req.GetResponse())
            using (var reader = new StreamReader(resp.GetResponseStream()))
                return reader.ReadToEnd();
        }

        public string Post(string endpoint, object body)
        {
            string json = _json.Serialize(body);
            var req = (HttpWebRequest)WebRequest.Create(_baseUrl + endpoint);
            req.Method = "POST";
            req.ContentType = "application/json";
            req.Timeout = 15000;
            byte[] data = Encoding.UTF8.GetBytes(json);
            req.ContentLength = data.Length;
            using (var stream = req.GetRequestStream())
                stream.Write(data, 0, data.Length);
            using (var resp = (HttpWebResponse)req.GetResponse())
            using (var reader = new StreamReader(resp.GetResponseStream()))
                return reader.ReadToEnd();
        }

        public string Put(string endpoint, object body)
        {
            string json = _json.Serialize(body);
            var req = (HttpWebRequest)WebRequest.Create(_baseUrl + endpoint);
            req.Method = "PUT";
            req.ContentType = "application/json";
            req.Timeout = 15000;
            byte[] data = Encoding.UTF8.GetBytes(json);
            req.ContentLength = data.Length;
            using (var stream = req.GetRequestStream())
                stream.Write(data, 0, data.Length);
            using (var resp = (HttpWebResponse)req.GetResponse())
            using (var reader = new StreamReader(resp.GetResponseStream()))
                return reader.ReadToEnd();
        }

        public string Delete(string endpoint)
        {
            var req = (HttpWebRequest)WebRequest.Create(_baseUrl + endpoint);
            req.Method = "DELETE";
            req.Timeout = 10000;
            using (var resp = (HttpWebResponse)req.GetResponse())
            using (var reader = new StreamReader(resp.GetResponseStream()))
                return reader.ReadToEnd();
        }

        // ── TYPED HELPERS ─────────────────────────────────────────────────────
        public T GetObj<T>(string endpoint)
        {
            return _json.Deserialize<T>(Get(endpoint));
        }

        public T PostObj<T>(string endpoint, object body)
        {
            return _json.Deserialize<T>(Post(endpoint, body));
        }

        // ── SERIALIZER ACCESS ─────────────────────────────────────────────────
        public string Serialize(object obj) { return _json.Serialize(obj); }
        public T Deserialize<T>(string json) { return _json.Deserialize<T>(json); }
    }
}
