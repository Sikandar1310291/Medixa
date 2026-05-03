using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Data;
using System.Data.SQLite;

namespace PharmaBilling.Source.Data
{
    /// <summary>
    /// MEDIXA CLOUD SYNC SERVICE
    /// 
    /// Uploads every Sale and Purchase to Supabase cloud (keyed by License Key).
    /// On new machine + same license, RestoreFromCloud() pulls all records back.
    /// 
    /// All cloud ops run on background threads — they NEVER block the UI.
    /// </summary>
    public static class CloudSyncService
    {
        private static readonly string SupabaseUrl  = "https://idnfkbgswrbhmqzsnxnk.supabase.co";
        private static readonly string SupabaseKey  = "sb_publishable_-Uwrrbhxubrc3dDYDw6gMw_1xRb2oYd";
        private static readonly JavaScriptSerializer Json = new JavaScriptSerializer { MaxJsonLength = int.MaxValue };

        // ── PUBLIC ENTRY POINTS ───────────────────────────────────────────────

        /// <summary>Upload a completed sale to the cloud (fire-and-forget).</summary>
        public static void UploadSaleAsync(int saleId, string customerName,
                                            string saleDate, double total,
                                            double discount, double netPaid,
                                            string status,
                                            List<Dictionary<string, object>> items)
        {
            string key = LicenseManager.CurrentLicenseKey;
            if (string.IsNullOrEmpty(key)) return;

            Task.Run(() =>
            {
                try
                {
                    var payload = new Dictionary<string, object>
                    {
                        { "license_key",    key          },
                        { "local_sale_id",  saleId       },
                        { "sale_date",      saleDate     },
                        { "customer_name",  customerName },
                        { "total_amount",   total        },
                        { "discount",       discount     },
                        { "net_paid",       netPaid      },
                        { "status",         status       },
                        { "items_json",     Json.Serialize(items) }
                    };
                    Upsert("cloud_sales", payload);
                }
                catch { /* silent — cloud sync must never crash the app */ }
            });
        }

        /// <summary>Upload a completed purchase to the cloud (fire-and-forget).</summary>
        public static void UploadPurchaseAsync(int purchaseId, string supplierName,
                                                string invoiceNo, string purchaseDate,
                                                double total, string status,
                                                List<Dictionary<string, object>> items)
        {
            string key = LicenseManager.CurrentLicenseKey;
            if (string.IsNullOrEmpty(key)) return;

            Task.Run(() =>
            {
                try
                {
                    var payload = new Dictionary<string, object>
                    {
                        { "license_key",       key          },
                        { "local_purchase_id", purchaseId   },
                        { "purchase_date",     purchaseDate },
                        { "supplier_name",     supplierName },
                        { "invoice_no",        invoiceNo    },
                        { "total_amount",      total        },
                        { "status",            status       },
                        { "items_json",        Json.Serialize(items) }
                    };
                    Upsert("cloud_purchases", payload);
                }
                catch { /* silent */ }
            });
        }

        /// <summary>
        /// Called after license activation on a new machine.
        /// Downloads all cloud Sales + Purchases and inserts them into local SQLite.
        /// Returns (salesRestored, purchasesRestored) count.
        /// </summary>
        public static Tuple<int, int> RestoreFromCloud(string licenseKey)
        {
            int salesCount    = 0;
            int purchasesCount = 0;

            try
            {
                var db = new DbHelper();

                // ── RESTORE SALES ─────────────────────────────────────────────
                string salesJson = Get(string.Format("/rest/v1/cloud_sales?license_key=eq.{0}&order=local_sale_id.asc", Uri.EscapeDataString(licenseKey)));
                var salesArray = Json.Deserialize<object[]>(salesJson);

                if (salesArray != null)
                {
                    foreach (var s in salesArray)
                    {
                        var sale = s as Dictionary<string, object>;
                        if (sale == null) continue;

                        int    localSaleId   = Convert.ToInt32(sale["local_sale_id"]);
                        string saleDate      = sale.ContainsKey("sale_date") && sale["sale_date"] != null ? sale["sale_date"].ToString() : DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                        string customerName  = sale.ContainsKey("customer_name") && sale["customer_name"] != null ? sale["customer_name"].ToString() : "Walk-in Customer";
                        double totalAmount   = Convert.ToDouble(sale["total_amount"]);
                        double discount      = Convert.ToDouble(sale["discount"]);
                        double netPaid       = Convert.ToDouble(sale["net_paid"]);
                        string status        = sale.ContainsKey("status") && sale["status"] != null ? sale["status"].ToString() : "Paid";
                        string itemsJson     = sale.ContainsKey("items_json") && sale["items_json"] != null ? sale["items_json"].ToString() : "[]";

                        // Skip if this sale already exists locally
                        object existing = db.ExecuteScalar("SELECT COUNT(*) FROM Sales WHERE SaleID = @ID",
                            new SQLiteParameter[] { new SQLiteParameter("@ID", localSaleId) });
                        if (Convert.ToInt32(existing) > 0) continue;

                        // Get or create customer
                        object custIdObj = db.ExecuteScalar(
                            "SELECT CustomerID FROM Customers WHERE Name = @N LIMIT 1",
                            new SQLiteParameter[] { new SQLiteParameter("@N", customerName) });

                        int custId;
                        if (custIdObj == null || custIdObj == DBNull.Value)
                        {
                            db.ExecuteNonQuery(
                                "INSERT INTO Customers (Name, Contact, Email, Address, Balance) VALUES (@N,'','','',0)",
                                new SQLiteParameter[] { new SQLiteParameter("@N", customerName) });
                            custId = Convert.ToInt32(db.ExecuteScalar("SELECT last_insert_rowid()"));
                        }
                        else
                        {
                            custId = Convert.ToInt32(custIdObj);
                        }

                        // Insert the Sale with its original SaleID preserved
                        db.ExecuteNonQuery(
                            @"INSERT OR IGNORE INTO Sales (SaleID, CustomerID, SaleDate, TotalAmount, Discount, NetPaid, Status)
                              VALUES (@ID, @CustId, @Date, @Total, @Disc, @Net, @Status)",
                            new SQLiteParameter[]
                            {
                                new SQLiteParameter("@ID",     localSaleId),
                                new SQLiteParameter("@CustId", custId),
                                new SQLiteParameter("@Date",   saleDate),
                                new SQLiteParameter("@Total",  totalAmount),
                                new SQLiteParameter("@Disc",   discount),
                                new SQLiteParameter("@Net",    netPaid),
                                new SQLiteParameter("@Status", status)
                            });

                        // Insert SaleDetails items
                        var items = Json.Deserialize<object[]>(itemsJson);
                        if (items != null)
                        {
                            foreach (var i in items)
                            {
                                var item = i as Dictionary<string, object>;
                                if (item == null) continue;
                                db.ExecuteNonQuery(
                                    @"INSERT OR IGNORE INTO SaleDetails (SaleID, MedicineID, BatchNo, RackNo, Quantity, UnitPrice, TotalPrice)
                                      VALUES (@SID, @MID, @Batch, @Box, @Qty, @Price, @Total)",
                                    new SQLiteParameter[]
                                    {
                                        new SQLiteParameter("@SID",   localSaleId),
                                        new SQLiteParameter("@MID",   item.ContainsKey("MedicineID") && item["MedicineID"] != null ? (object)Convert.ToInt32(item["MedicineID"]) : DBNull.Value),
                                        new SQLiteParameter("@Batch", item.ContainsKey("BatchNo") && item["BatchNo"] != null ? item["BatchNo"].ToString()  : "Default"),
                                        new SQLiteParameter("@Box",   item.ContainsKey("RackNo") && item["RackNo"] != null ? item["RackNo"].ToString()   : "Rack-1"),
                                        new SQLiteParameter("@Qty",   item.ContainsKey("Quantity") && item["Quantity"] != null ? (object)Convert.ToDouble(item["Quantity"]) : 0),
                                        new SQLiteParameter("@Price", item.ContainsKey("UnitPrice") && item["UnitPrice"] != null ? (object)Convert.ToDouble(item["UnitPrice"]) : 0),
                                        new SQLiteParameter("@Total", item.ContainsKey("TotalPrice") && item["TotalPrice"] != null ? (object)Convert.ToDouble(item["TotalPrice"]) : 0)
                                    });
                            }
                        }

                        salesCount++;
                    }
                }

                // ── RESTORE PURCHASES ─────────────────────────────────────────
                string purchJson = Get(string.Format("/rest/v1/cloud_purchases?license_key=eq.{0}&order=local_purchase_id.asc", Uri.EscapeDataString(licenseKey)));
                var purchArray = Json.Deserialize<object[]>(purchJson);

                if (purchArray != null)
                {
                    foreach (var p in purchArray)
                    {
                        var purch = p as Dictionary<string, object>;
                        if (purch == null) continue;

                        int    localPurchId  = Convert.ToInt32(purch["local_purchase_id"]);
                        string purchDate     = purch.ContainsKey("purchase_date") && purch["purchase_date"] != null ? purch["purchase_date"].ToString() : DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                        string supplierName  = purch.ContainsKey("supplier_name") && purch["supplier_name"] != null ? purch["supplier_name"].ToString() : "Unknown";
                        string invoiceNo     = purch.ContainsKey("invoice_no") && purch["invoice_no"] != null ? purch["invoice_no"].ToString() : "";
                        double totalAmount   = Convert.ToDouble(purch["total_amount"]);
                        string status        = purch.ContainsKey("status") && purch["status"] != null ? purch["status"].ToString() : "Received";
                        string itemsJson     = purch.ContainsKey("items_json") && purch["items_json"] != null ? purch["items_json"].ToString() : "[]";

                        // Skip if exists locally
                        object existing = db.ExecuteScalar("SELECT COUNT(*) FROM Purchases WHERE PurchaseID = @ID",
                            new SQLiteParameter[] { new SQLiteParameter("@ID", localPurchId) });
                        if (Convert.ToInt32(existing) > 0) continue;

                        // Get or create supplier
                        object suppIdObj = db.ExecuteScalar(
                            "SELECT SupplierID FROM Suppliers WHERE Name = @N LIMIT 1",
                            new SQLiteParameter[] { new SQLiteParameter("@N", supplierName) });

                        int suppId;
                        if (suppIdObj == null || suppIdObj == DBNull.Value)
                        {
                            db.ExecuteNonQuery(
                                "INSERT INTO Suppliers (Name, Contact, Email, Address) VALUES (@N,'','','')",
                                new SQLiteParameter[] { new SQLiteParameter("@N", supplierName) });
                            suppId = Convert.ToInt32(db.ExecuteScalar("SELECT last_insert_rowid()"));
                        }
                        else
                        {
                            suppId = Convert.ToInt32(suppIdObj);
                        }

                        // Insert Purchase with original ID preserved
                        db.ExecuteNonQuery(
                            @"INSERT OR IGNORE INTO Purchases (PurchaseID, SupplierID, InvoiceNo, PurchaseDate, TotalAmount, Status, PurchaseType)
                              VALUES (@ID, @SuppId, @Inv, @Date, @Total, @Status, 'Normal')",
                            new SQLiteParameter[]
                            {
                                new SQLiteParameter("@ID",     localPurchId),
                                new SQLiteParameter("@SuppId", suppId),
                                new SQLiteParameter("@Inv",    invoiceNo),
                                new SQLiteParameter("@Date",   purchDate),
                                new SQLiteParameter("@Total",  totalAmount),
                                new SQLiteParameter("@Status", status)
                            });

                        // Insert PurchaseDetails
                        var items = Json.Deserialize<object[]>(itemsJson);
                        if (items != null)
                        {
                            foreach (var i in items)
                            {
                                var item = i as Dictionary<string, object>;
                                if (item == null) continue;
                                db.ExecuteNonQuery(
                                    @"INSERT OR IGNORE INTO PurchaseDetails (PurchaseID, MedicineID, BatchNo, ExpiryDate, Quantity, PurchasePrice, TotalPrice)
                                      VALUES (@PID, @MID, @Batch, @Exp, @Qty, @Price, @Total)",
                                    new SQLiteParameter[]
                                    {
                                        new SQLiteParameter("@PID",   localPurchId),
                                        new SQLiteParameter("@MID",   item.ContainsKey("MedicineID") && item["MedicineID"] != null ? (object)Convert.ToInt32(item["MedicineID"]) : DBNull.Value),
                                        new SQLiteParameter("@Batch", item.ContainsKey("BatchNo") && item["BatchNo"] != null ? item["BatchNo"].ToString() : "Default"),
                                        new SQLiteParameter("@Exp",   item.ContainsKey("ExpiryDate") && item["ExpiryDate"] != null ? item["ExpiryDate"].ToString() : ""),
                                        new SQLiteParameter("@Qty",   item.ContainsKey("Quantity") && item["Quantity"] != null ? (object)Convert.ToDouble(item["Quantity"]) : 0),
                                        new SQLiteParameter("@Price", item.ContainsKey("PurchasePrice") && item["PurchasePrice"] != null ? (object)Convert.ToDouble(item["PurchasePrice"]) : 0),
                                        new SQLiteParameter("@Total", item.ContainsKey("TotalPrice") && item["TotalPrice"] != null ? (object)Convert.ToDouble(item["TotalPrice"]) : 0)
                                    });
                            }
                        }

                        purchasesCount++;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("CloudSync RestoreFromCloud error: " + ex.Message);
            }

            return new Tuple<int, int>(salesCount, purchasesCount);
        }

        // ── HTTP HELPERS ──────────────────────────────────────────────────────

        private static void Upsert(string table, Dictionary<string, object> payload)
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            string json = Json.Serialize(payload);
            byte[] data = Encoding.UTF8.GetBytes(json);

            string url = SupabaseUrl + "/rest/v1/" + table;
            var req = (HttpWebRequest)WebRequest.Create(url);
            req.Method         = "POST";
            req.ContentType    = "application/json";
            req.ContentLength  = data.Length;
            req.Timeout        = 10000;
            req.Headers.Add("apikey",         SupabaseKey);
            req.Headers.Add("Authorization",  "Bearer " + SupabaseKey);
            req.Headers.Add("Prefer",         "resolution=merge-duplicates,return=minimal");

            using (var stream = req.GetRequestStream())
                stream.Write(data, 0, data.Length);

            using (req.GetResponse()) { }   // consume — no body needed
        }

        private static string Get(string endpoint)
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            var req = (HttpWebRequest)WebRequest.Create(SupabaseUrl + endpoint);
            req.Method  = "GET";
            req.Timeout = 15000;
            req.Headers.Add("apikey",        SupabaseKey);
            req.Headers.Add("Authorization", "Bearer " + SupabaseKey);

            using (var resp = (HttpWebResponse)req.GetResponse())
            using (var reader = new StreamReader(resp.GetResponseStream()))
                return reader.ReadToEnd();
        }
    }
}
