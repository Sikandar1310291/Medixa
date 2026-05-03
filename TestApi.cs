using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Web.Script.Serialization;
using System.IO;

namespace TestApp
{
    class Program
    {
        private static readonly string SupabaseUrl  = "https://idnfkbgswrbhmqzsnxnk.supabase.co";
        private static readonly string SupabaseKey  = "sb_publishable_-Uwrrbhxubrc3dDYDw6gMw_1xRb2oYd";
        private static readonly JavaScriptSerializer Json = new JavaScriptSerializer { MaxJsonLength = int.MaxValue };

        static void Main(string[] args)
        {
            Console.WriteLine("Testing Cloud Sync API directly...");
            
            var payload = new Dictionary<string, object>
            {
                { "license_key",    "E2E-TEST-KEY" },
                { "local_sale_id",  9999 },
                { "sale_date",      DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") },
                { "customer_name",  "End-to-End Test User" },
                { "total_amount",   150.0 },
                { "discount",       0.0 },
                { "net_paid",       150.0 },
                { "status",         "Paid" },
                { "items_json",     "[{\"MedicineID\": 1, \"Quantity\": 5, \"TotalPrice\": 150.0}]" }
            };

            // 1. Upload Test
            Console.WriteLine("Uploading mock sale...");
            try {
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                string json = Json.Serialize(payload);
                byte[] data = Encoding.UTF8.GetBytes(json);

                string url = SupabaseUrl + "/rest/v1/cloud_sales";
                var req = (HttpWebRequest)WebRequest.Create(url);
                req.Method         = "POST";
                req.ContentType    = "application/json";
                req.ContentLength  = data.Length;
                req.Headers.Add("apikey",         SupabaseKey);
                req.Headers.Add("Authorization",  "Bearer " + SupabaseKey);
                req.Headers.Add("Prefer",         "resolution=merge-duplicates,return=minimal");

                using (var stream = req.GetRequestStream())
                    stream.Write(data, 0, data.Length);

                using (req.GetResponse()) { } 
                Console.WriteLine("Upload SUCCESS.");
            }
            catch (Exception ex) {
                Console.WriteLine("Upload FAILED: " + ex.Message);
            }

            // 2. Download Test
            Console.WriteLine("Downloading mock sale...");
            try {
                var req2 = (HttpWebRequest)WebRequest.Create(SupabaseUrl + "/rest/v1/cloud_sales?license_key=eq.E2E-TEST-KEY&select=*");
                req2.Method  = "GET";
                req2.Headers.Add("apikey",        SupabaseKey);
                req2.Headers.Add("Authorization", "Bearer " + SupabaseKey);

                string response = "";
                using (var resp = (HttpWebResponse)req2.GetResponse())
                using (var reader = new StreamReader(resp.GetResponseStream()))
                    response = reader.ReadToEnd();
                
                Console.WriteLine("Download SUCCESS. Data received: ");
                Console.WriteLine(response);
            }
            catch (Exception ex) {
                Console.WriteLine("Download FAILED: " + ex.Message);
            }
        }
    }
}
