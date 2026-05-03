using System;
using System.Collections.Generic;
using System.Data.SQLite;
using PharmaBilling.Source.Data;

namespace TestApp
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Starting Cloud Sync E2E Test...");
            
            // Set dummy license
            System.IO.File.WriteAllText("license_key.txt", "TEST-LICENSE-123");

            // 1. Upload a Dummy Sale
            var saleItems = new List<Dictionary<string, object>>
            {
                new Dictionary<string, object>
                {
                    { "MedicineID", 1001 },
                    { "BatchNo", "TEST-BATCH-A" },
                    { "RackNo", "R-1" },
                    { "Quantity", 5 },
                    { "UnitPrice", 150.0 },
                    { "TotalPrice", 750.0 }
                }
            };
            
            Console.WriteLine("Uploading Sale...");
            CloudSyncService.UploadSaleAsync(9999, "Test Customer", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), 750, 0, 750, "Paid", saleItems);

            // 2. Upload a Dummy Purchase
            var purchaseItems = new List<Dictionary<string, object>>
            {
                new Dictionary<string, object>
                {
                    { "MedicineID", 2002 },
                    { "BatchNo", "TEST-BATCH-B" },
                    { "ExpiryDate", "2027-12-31" },
                    { "Quantity", 100 },
                    { "PurchasePrice", 50.0 },
                    { "TotalPrice", 5000.0 }
                }
            };

            Console.WriteLine("Uploading Purchase...");
            CloudSyncService.UploadPurchaseAsync(8888, "Test Supplier", "INV-8888", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), 5000, "Received", purchaseItems);

            Console.WriteLine("Waiting 5 seconds for async uploads to finish...");
            System.Threading.Thread.Sleep(5000);

            // 3. Clear local DB Sales and Purchases for testing
            Console.WriteLine("Clearing local Sales and Purchases to simulate new PC...");
            var db = new DbHelper();
            db.ExecuteNonQuery("DELETE FROM SaleDetails WHERE SaleID = 9999");
            db.ExecuteNonQuery("DELETE FROM Sales WHERE SaleID = 9999");
            db.ExecuteNonQuery("DELETE FROM PurchaseDetails WHERE PurchaseID = 8888");
            db.ExecuteNonQuery("DELETE FROM Purchases WHERE PurchaseID = 8888");
            
            Console.WriteLine("Verifying deletion...");
            int sCount = Convert.ToInt32(db.ExecuteScalar("SELECT COUNT(*) FROM Sales WHERE SaleID = 9999"));
            Console.WriteLine("Local Sale 9999 count: " + sCount);

            // 4. Restore from Cloud
            Console.WriteLine("Restoring from Cloud...");
            var counts = CloudSyncService.RestoreFromCloud("TEST-LICENSE-123");
            Console.WriteLine(string.Format("Restored {0} sales and {1} purchases.", counts.Item1, counts.Item2));

            // 5. Verify local DB
            sCount = Convert.ToInt32(db.ExecuteScalar("SELECT COUNT(*) FROM Sales WHERE SaleID = 9999"));
            int pCount = Convert.ToInt32(db.ExecuteScalar("SELECT COUNT(*) FROM Purchases WHERE PurchaseID = 8888"));
            
            Console.WriteLine("Final Validation:");
            Console.WriteLine("Sale 9999 restored: " + (sCount > 0 ? "YES" : "NO"));
            Console.WriteLine("Purchase 8888 restored: " + (pCount > 0 ? "YES" : "NO"));

            if (sCount > 0 && pCount > 0)
                Console.WriteLine("TEST PASSED!");
            else
                Console.WriteLine("TEST FAILED!");
        }
    }
}
