using System;
using System.Data.SQLite;

class MigrateDb
{
    static void Main()
    {
        string dbPath = @"c:\Users\ma516\OneDrive\Desktop\Pharma\PharmaDB.sqlite";
        using (var conn = new SQLiteConnection(string.Format("Data Source={0};Version=3;", dbPath)))
        {
            conn.Open();
            try { new SQLiteCommand("ALTER TABLE Stocks RENAME COLUMN BoxNo TO RackNo;", conn).ExecuteNonQuery(); Console.WriteLine("Stocks BoxNo renamed."); } catch { }
            try { new SQLiteCommand("ALTER TABLE SaleDetails RENAME COLUMN BoxNo TO RackNo;", conn).ExecuteNonQuery(); Console.WriteLine("SaleDetails BoxNo renamed.");} catch { }
            try { new SQLiteCommand("ALTER TABLE Stocks ADD COLUMN RackNo TEXT DEFAULT 'Rack-1';", conn).ExecuteNonQuery(); Console.WriteLine("Stocks RackNo added.");} catch { }
            try { new SQLiteCommand("ALTER TABLE SaleDetails ADD COLUMN RackNo TEXT;", conn).ExecuteNonQuery(); Console.WriteLine("SaleDetails RackNo added.");} catch { }
            Console.WriteLine("DB Migration complete.");
        }
    }
}
