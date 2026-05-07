using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.IO;
using System.Threading;
using PharmaBilling.Source.Network;

namespace PharmaBilling.Source.Data
{
    /// <summary>
    /// The single database access layer for the entire application.
    ///
    /// SERVER mode: opens SQLite directly on local disk (C:\ProgramData\Medixa\).
    /// CLIENT mode: delegates ALL operations to MedixaLanClient (HTTP) which
    ///              calls the server's built-in HTTP API on port 5000.
    ///
    /// WHY HTTP instead of SQLite-over-SMB:
    ///   SQLite's own documentation explicitly states that accessing a database
    ///   over a network filesystem (SMB/NFS) is unreliable because byte-range
    ///   locking semantics differ from local disk.  We eliminate this entirely.
    /// </summary>
    public class DbHelper
    {
        private string _dbPath;
        private string _connectionString;
        private bool _isClientMode;
        private MedixaLanClient _lanClient;

        private static readonly string SafeDataDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Medixa");

        private static bool _initialized = false;
        private static readonly object _initLock = new object();

        public DbHelper()
        {
            // Constructor is now lightweight. Configuration is checked lazily.
        }

        private void EnsureConfigured()
        {
            var config = AppConfig.Current;
            bool currentIsClient = !config.IsServer;

            // If configuration changed or first run
            if (_lanClient == null && currentIsClient)
            {
                _lanClient = new MedixaLanClient(config.ServerIP);
                _isClientMode = true;
            }
            else if (_connectionString == null && !currentIsClient)
            {
                _isClientMode = false;
                ConfigureLocalServer();
            }
            else
            {
                // Sync mode flag in case it was toggled in settings
                _isClientMode = currentIsClient;
                if (_isClientMode && _lanClient != null && _lanClient.ServerIP != config.ServerIP)
                {
                    _lanClient = new MedixaLanClient(config.ServerIP);
                }
            }
        }

        private void ConfigureLocalServer()
        {
            if (!Directory.Exists(SafeDataDir))
                Directory.CreateDirectory(SafeDataDir);

            _dbPath = Path.Combine(SafeDataDir, "PharmaDB.sqlite");

            if (!File.Exists(_dbPath))
            {
                string old = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "PharmaDB.sqlite");
                if (File.Exists(old)) try { File.Copy(old, _dbPath); } catch { }
            }

            _connectionString = string.Format(
                "Data Source={0};Version=3;BusyTimeout=10000;Pooling=True;",
                _dbPath);

            if (!_initialized)
            {
                lock (_initLock)
                {
                    if (!_initialized)
                    {
                        InitializeDatabase();
                        _initialized = true;
                    }
                }
            }
        }

        // ── PUBLIC API ──────────────────────────────────────────────────────────

        public DataTable GetDataTable(string sql, SQLiteParameter[] parameters = null)
        {
            EnsureConfigured();
            if (_isClientMode)
                return _lanClient.GetDataTable(sql, parameters);

            return LocalGetDataTable(sql, parameters);
        }

        public int ExecuteNonQuery(string sql, SQLiteParameter[] parameters = null)
        {
            EnsureConfigured();
            if (_isClientMode)
                return _lanClient.ExecuteNonQuery(sql, parameters);

            return LocalExecuteNonQuery(sql, parameters);
        }

        public object ExecuteScalar(string sql, SQLiteParameter[] parameters = null)
        {
            EnsureConfigured();
            if (_isClientMode)
                return _lanClient.ExecuteScalar(sql, parameters);

            return LocalExecuteScalar(sql, parameters);
        }

        public bool ExecuteTransaction(List<Tuple<string, SQLiteParameter[]>> commands)
        {
            EnsureConfigured();
            if (_isClientMode)
                return _lanClient.ExecuteTransaction(commands);

            return LocalExecuteTransaction(commands);
        }

        public SQLiteConnection GetConnection()
        {
            if (_isClientMode)
                throw new InvalidOperationException("GetConnection() is not available in CLIENT mode. Use GetDataTable/ExecuteNonQuery.");
            return new SQLiteConnection(_connectionString);
        }

        // ── LOCAL SQLITE IMPLEMENTATIONS ────────────────────────────────────────

        private DataTable LocalGetDataTable(string sql, SQLiteParameter[] parameters, int retries = 3)
        {
            Exception lastEx = null;
            for (int attempt = 0; attempt < retries; attempt++)
            {
                try
                {
                    using (var conn = new SQLiteConnection(_connectionString))
                    {
                        conn.Open();
                        using (var cmd = new SQLiteCommand(sql, conn))
                        {
                            if (parameters != null) cmd.Parameters.AddRange(parameters);
                            using (var adapter = new SQLiteDataAdapter(cmd))
                            {
                                var dt = new DataTable();
                                adapter.Fill(dt);
                                return dt;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    lastEx = ex;
                    string msg = ex.Message.ToLower();
                    if ((msg.Contains("locked") || msg.Contains("busy")) && attempt < retries - 1)
                        Thread.Sleep(500 * (attempt + 1));
                    else
                        throw;
                }
            }
            throw lastEx;
        }

        private int LocalExecuteNonQuery(string sql, SQLiteParameter[] parameters)
        {
            using (var conn = new SQLiteConnection(_connectionString))
            {
                conn.Open();
                using (var cmd = new SQLiteCommand(sql, conn))
                {
                    if (parameters != null) cmd.Parameters.AddRange(parameters);
                    return cmd.ExecuteNonQuery();
                }
            }
        }

        private object LocalExecuteScalar(string sql, SQLiteParameter[] parameters)
        {
            using (var conn = new SQLiteConnection(_connectionString))
            {
                conn.Open();
                using (var cmd = new SQLiteCommand(sql, conn))
                {
                    if (parameters != null) cmd.Parameters.AddRange(parameters);
                    return cmd.ExecuteScalar();
                }
            }
        }

        private bool LocalExecuteTransaction(List<Tuple<string, SQLiteParameter[]>> commands)
        {
            using (var conn = new SQLiteConnection(_connectionString))
            {
                conn.Open();
                using (var tx = conn.BeginTransaction())
                {
                    try
                    {
                        foreach (var c in commands)
                        {
                            using (var cmd = new SQLiteCommand(c.Item1, conn, tx))
                            {
                                if (c.Item2 != null) cmd.Parameters.AddRange(c.Item2);
                                cmd.ExecuteNonQuery();
                            }
                        }
                        tx.Commit();
                        return true;
                    }
                    catch { tx.Rollback(); throw; }
                }
            }
        }

        private void InitializeDatabase()
        {
            try
            {
                if (!File.Exists(_dbPath))
                    SQLiteConnection.CreateFile(_dbPath);

                using (var conn = new SQLiteConnection(_connectionString))
                {
                    conn.Open();

                    // Lock database into DELETE journal mode — never WAL.
                    // WAL creates .wal/.shm temp files that break SMB access.
                    try { new SQLiteCommand("PRAGMA journal_mode=DELETE;", conn).ExecuteNonQuery(); } catch { }

                    // Schema migrations (silently ignored if already applied)
                    try { new SQLiteCommand("ALTER TABLE Stocks RENAME COLUMN BoxNo TO RackNo;", conn).ExecuteNonQuery(); } catch { }
                    try { new SQLiteCommand("ALTER TABLE SaleDetails RENAME COLUMN BoxNo TO RackNo;", conn).ExecuteNonQuery(); } catch { }
                    try { new SQLiteCommand("ALTER TABLE Stocks ADD COLUMN RackNo TEXT DEFAULT 'Rack-1';", conn).ExecuteNonQuery(); } catch { }
                    try { new SQLiteCommand("ALTER TABLE SaleDetails ADD COLUMN RackNo TEXT;", conn).ExecuteNonQuery(); } catch { }

                    string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                    string schemaPath = Path.Combine(baseDir, "Source", "Data", "schema.sql");
                    if (!File.Exists(schemaPath)) schemaPath = Path.Combine(baseDir, "schema.sql");

                    if (File.Exists(schemaPath))
                    {
                        string schema = File.ReadAllText(schemaPath);
                        using (var cmd = new SQLiteCommand(schema, conn))
                            cmd.ExecuteNonQuery();
                    }

                    try { new SQLiteCommand("ALTER TABLE Medicines ADD COLUMN BoxSize INTEGER DEFAULT 1", conn).ExecuteNonQuery(); } catch { }
                    try { new SQLiteCommand("ALTER TABLE Purchases ADD COLUMN PurchaseType TEXT DEFAULT 'Normal'", conn).ExecuteNonQuery(); } catch { }
                    // PackSize stores the exact pack/box size used at purchase time.
                    // This prevents inside/outside total mismatch when BoxSize is later edited in Medicines table.
                    try { new SQLiteCommand("ALTER TABLE PurchaseDetails ADD COLUMN PackSize REAL DEFAULT 1", conn).ExecuteNonQuery(); } catch { }


                    try
                    {
                        new SQLiteCommand(@"CREATE TABLE IF NOT EXISTS PurchaseReturns (
                            ReturnID INTEGER PRIMARY KEY AUTOINCREMENT,
                            PurchaseID INTEGER, SupplierID INTEGER,
                            ReturnDate TEXT DEFAULT (datetime('now')),
                            TotalAmount REAL DEFAULT 0, Reason TEXT, Status TEXT DEFAULT 'Returned')", conn).ExecuteNonQuery();
                    } catch { }
                    try
                    {
                        new SQLiteCommand(@"CREATE TABLE IF NOT EXISTS PurchaseReturnDetails (
                            DetailID INTEGER PRIMARY KEY AUTOINCREMENT,
                            ReturnID INTEGER, MedicineID INTEGER, BatchNo TEXT,
                            Quantity INTEGER, PurchasePrice REAL, TotalPrice REAL)", conn).ExecuteNonQuery();
                    } catch { }
                    try
                    {
                        new SQLiteCommand(@"CREATE TABLE IF NOT EXISTS SaleReturns (
                            ReturnID INTEGER PRIMARY KEY AUTOINCREMENT,
                            SaleID INTEGER, CustomerID INTEGER,
                            ReturnDate TEXT DEFAULT (datetime('now')),
                            TotalAmount REAL DEFAULT 0, Reason TEXT, Status TEXT DEFAULT 'Returned')", conn).ExecuteNonQuery();
                    } catch { }
                    try
                    {
                        new SQLiteCommand(@"CREATE TABLE IF NOT EXISTS SaleReturnDetails (
                            DetailID INTEGER PRIMARY KEY AUTOINCREMENT,
                            ReturnID INTEGER, MedicineID INTEGER, BatchNo TEXT,
                            Quantity INTEGER, UnitPrice REAL, TotalPrice REAL)", conn).ExecuteNonQuery();
                    } catch { }

                    try { new SQLiteCommand("INSERT OR IGNORE INTO Ledgers (Name, Type, Balance) VALUES ('Purchase Returns', 'Income', 0)", conn).ExecuteNonQuery(); } catch { }
                    try { new SQLiteCommand("INSERT OR IGNORE INTO Ledgers (Name, Type, Balance) VALUES ('Sale Returns', 'Expense', 0)", conn).ExecuteNonQuery(); } catch { }

                    // --- Auto-Merge 24k Medicines from Master DB if available ---
                    try
                    {
                        string masterDbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "PharmaDB.sqlite");
                        if (File.Exists(masterDbPath) && !masterDbPath.Equals(_dbPath, StringComparison.OrdinalIgnoreCase))
                        {
                            using (var cmd = conn.CreateCommand())
                            {
                                cmd.CommandText = "ATTACH DATABASE '" + masterDbPath.Replace("'", "''") + "' AS masterDB;";
                                cmd.ExecuteNonQuery();

                                // Step 1: Insert missing medicines
                                cmd.CommandText = @"
                                    INSERT INTO main.Medicines (Name, Type, Category, Manufacturer, Unit, Barcode, GenericFormula, WholesalePrice, PurchasePrice, SalePrice, MinStock, Status)
                                    SELECT Name, Type, Category, Manufacturer, Unit, Barcode, GenericFormula, WholesalePrice, PurchasePrice, SalePrice, MinStock, Status 
                                    FROM masterDB.Medicines
                                    WHERE Name NOT IN (SELECT Name FROM main.Medicines);";
                                cmd.ExecuteNonQuery();

                                // Step 2: Update prices for existing medicines that have 0.0 prices (e.g. client upgrade)
                                cmd.CommandText = @"
                                    UPDATE main.Medicines
                                    SET 
                                        PurchasePrice = (SELECT s.PurchasePrice FROM masterDB.Medicines s WHERE s.Name = main.Medicines.Name),
                                        SalePrice     = (SELECT s.SalePrice     FROM masterDB.Medicines s WHERE s.Name = main.Medicines.Name)
                                    WHERE (main.Medicines.PurchasePrice = 0 OR main.Medicines.SalePrice = 0)
                                      AND EXISTS (SELECT 1 FROM masterDB.Medicines s WHERE s.Name = main.Medicines.Name AND (s.PurchasePrice > 0 OR s.SalePrice > 0));";
                                cmd.ExecuteNonQuery();

                                cmd.CommandText = "DETACH DATABASE masterDB;";
                                cmd.ExecuteNonQuery();
                            }
                        }
                    } catch { }
                }
            }
            catch (Exception ex)
            {
                // Log the exact path to help debugging
                string detailedError = string.Format("Database initialization failed.\nPath: {0}\nError: {1}", _dbPath, ex.Message);
                throw new Exception(detailedError, ex);
            }
        }
    }
}
