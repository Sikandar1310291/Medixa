using System;
using System.Data.SQLite;
using System.Collections.Generic;

namespace SeedData
{
    class Program
    {
        static void Main(string[] args)
        {
            string dbPath = "PharmaDB.sqlite";
            using (var conn = new SQLiteConnection($"Data Source={dbPath};Version=3;"))
            {
                conn.Open();
                
                // 1. Check if Supplier exists
                long supplierId = 1;
                using (var cmd = new SQLiteCommand("SELECT count(*) FROM Suppliers", conn))
                {
                    if (Convert.ToInt32(cmd.ExecuteScalar()) == 0)
                    {
                        using (var ins = new SQLiteCommand("INSERT INTO Suppliers (Name, Phone, Address) VALUES ('Medixa Global Services', '0300-1234567', 'Main Distribution Center'); SELECT last_insert_rowid();", conn))
                        {
                            supplierId = (long)ins.ExecuteScalar();
                        }
                    }
                }

                // 2. Clear old empty purchase attempts if any
                using (var cmd = new SQLiteCommand("DELETE FROM Purchases", conn)) { cmd.ExecuteScalar(); }

                // 3. Seed Purchases (Deep History Jan - April 2026)
                Random rnd = new Random();
                string[] dates = { "2026-01-05", "2026-01-15", "2026-02-10", "2026-03-01", "2026-03-25", "2026-04-01", "2026-04-04" };

                foreach (var d in dates)
                {
                    int amount = rnd.Next(5000, 25000);
                    int tax = amount / 10;
                    string sql = $"INSERT INTO Purchases (SupplierID, InvoiceNo, PurchaseDate, TotalAmount, Tax, Status) VALUES ({supplierId}, 'INV-{rnd.Next(1000,9999)}', '{d} 10:00:00', {amount}, {tax}, 'Received')";
                    using (var cmd = new SQLiteCommand(sql, conn))
                    {
                        cmd.ExecuteNonQuery();
                    }
                }

                Console.WriteLine("DATABASE SEEDED SUCCESSFULLY WITH 7 PROFESSIONAL INVOICES!");
            }
        }
    }
}
