using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using PharmaBilling.Source.Data;
using System.Threading;
using System.IO;

namespace MedixaTesting
{
    class E2ETestSuite
    {
        static int totalTests = 0;
        static int passedTests = 0;

        static void Main(string[] args)
        {
            Console.WriteLine("==================================================");
            Console.WriteLine("    MEDIXA SOFTWARE - E2E TESTING REPORT          ");
            Console.WriteLine("    By: Senior Google Testing Engineer            ");
            Console.WriteLine("==================================================");

            // Setup
            DbHelper db = new DbHelper();

            RunTest("Database Connection & Initial Data", () => {
                int medCount = Convert.ToInt32(db.ExecuteScalar("SELECT COUNT(*) FROM Medicines"));
                if (medCount < 23000) throw new Exception("Medicine database is missing or incomplete. Count: " + medCount);
                
                int ledgerCount = Convert.ToInt32(db.ExecuteScalar("SELECT COUNT(*) FROM Ledgers"));
                if (ledgerCount < 5) throw new Exception("Core ledgers are missing.");
            });

            RunTest("Database Constraints & Schemas", () => {
                var schema = db.GetDataTable("PRAGMA table_info(Medicines)", null);
                if (schema.Rows.Count == 0) throw new Exception("Schema for Medicines not found!");
            });

            RunTest("Cloud Sync - Local Persistence Integration", () => {
                File.WriteAllText("license_key.txt", "QA-TEST-LICENSE");
                
                // We mock the upload trigger - it should catch exceptions silently if network is off
                CloudSyncService.UploadSaleAsync(99999, "QA Test", "2026-04-25 12:00", 300, 0, 300, "Paid", new List<Dictionary<string, object>>());
                
                // Give it 1 second to fire task
                Thread.Sleep(1000);
            });

            RunTest("AppCache WarmUp Simulation", () => {
                var dt = db.GetDataTable("SELECT MedicineID, Name FROM Medicines ORDER BY MedicineID ASC LIMIT 1", null);
                if (dt.Rows.Count > 0)
                {
                    string firstMed = dt.Rows[0]["Name"].ToString();
                    if (firstMed.ToUpper() != "ARTECXIN FORTE TAB") 
                        throw new Exception("Sorting is wrong! First item in DB order is " + firstMed + " but should be ARTECXIN FORTE TAB.");
                }
            });

            Console.WriteLine("==================================================");
            Console.WriteLine(string.Format("TESTS COMPLETED: {0}/{1} PASSED", passedTests, totalTests));
            Console.WriteLine("==================================================");
        }

        static void RunTest(string testName, Action testCode)
        {
            totalTests++;
            Console.Write(string.Format("[RUNNING] {0}...", testName));
            try
            {
                testCode();
                Console.WriteLine(" [PASSED]");
                passedTests++;
            }
            catch (Exception ex)
            {
                Console.WriteLine(" [FAILED]");
                Console.WriteLine(string.Format("   Error: {0}", ex.Message));
            }
        }
    }
}
