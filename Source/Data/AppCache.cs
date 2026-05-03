using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using System.Windows;
using PharmaBilling.Source.Models;

namespace PharmaBilling.Source.Data
{
    /// <summary>
    /// APPLICATION-WIDE IN-MEMORY CACHE
    /// 
    /// Loaded ONCE after login on a background thread.
    /// All ViewModels read from this instead of hitting SQLite.
    /// Zero DB calls on page navigation = instant page loads.
    /// 
    /// Cache is invalidated selectively after write operations
    /// (e.g., after a sale is saved, only medicines/stock is refreshed).
    /// </summary>
    public static class AppCache
    {
        // Singleton DB access
        private static readonly DbHelper _db = new DbHelper();
        public static DbHelper Db { get { return _db; } }

        // ── CACHED DATA ─────────────────────────────────────────────────────
        public static List<Medicine>  Medicines  { get; private set; }
        public static List<Customer>  Customers  { get; private set; }
        public static List<Supplier>  Suppliers  { get; private set; }

        public static bool IsLoaded { get; private set; }

        static AppCache()
        {
            Medicines = new List<Medicine>();
            Customers = new List<Customer>();
            Suppliers = new List<Supplier>();
            IsLoaded  = false;
        }

        // ── LOAD ALL ON LOGIN ────────────────────────────────────────────────
        public static void WarmUp()
        {
            Task.Run(() =>
            {
                try
                {
                    // Load medicines
                    var meds = new List<Medicine>();
                    DataTable mdt = _db.GetDataTable(
                        @"SELECT m.MedicineID, m.Name, m.Type, m.Category, m.Manufacturer, 
                                 m.Unit, m.PurchasePrice, m.SalePrice, m.MinStock, m.Status,
                                 COALESCE((SELECT SUM(Quantity) FROM Stocks s WHERE s.MedicineID = m.MedicineID), 0) as TotalStock
                          FROM Medicines m WHERE m.Status != 'Inactive' ORDER BY m.MedicineID ASC");
                    foreach (DataRow row in mdt.Rows)
                    {
                        meds.Add(new Medicine
                        {
                            MedicineID    = Convert.ToInt32(row["MedicineID"]),
                            Name          = row["Name"].ToString(),
                            Type          = row["Type"].ToString(),
                            Category      = row["Category"].ToString(),
                            Manufacturer  = row["Manufacturer"].ToString(),
                            Unit          = row["Unit"].ToString(),
                            PurchasePrice = Convert.ToDouble(row["PurchasePrice"]),
                            SalePrice     = Convert.ToDouble(row["SalePrice"]),
                            MinStock      = Convert.ToDouble(row["MinStock"]),
                            TotalStock    = Convert.ToDouble(row["TotalStock"]),
                            Status        = row["Status"].ToString()
                        });
                    }
                    Medicines = meds;

                    // Load customers
                    var custs = new List<Customer>();
                    DataTable cdt = _db.GetDataTable("SELECT * FROM Customers ORDER BY Name");
                    foreach (DataRow row in cdt.Rows)
                    {
                        custs.Add(new Customer
                        {
                            CustomerID = Convert.ToInt32(row["CustomerID"]),
                            Name       = row["Name"].ToString(),
                            Contact    = row["Contact"].ToString(),
                            Email      = row["Email"].ToString(),
                            Address    = row["Address"].ToString(),
                            Balance    = Convert.ToDouble(row["Balance"]),
                            LedgerID   = row["LedgerID"] != DBNull.Value ? Convert.ToInt32(row["LedgerID"]) : 0
                        });
                    }
                    Customers = custs;

                    // Load suppliers
                    var supps = new List<Supplier>();
                    DataTable sdt = _db.GetDataTable("SELECT * FROM Suppliers ORDER BY Name");
                    foreach (DataRow row in sdt.Rows)
                    {
                        supps.Add(new Supplier
                        {
                            SupplierID = Convert.ToInt32(row["SupplierID"]),
                            Name       = row["Name"].ToString(),
                            Contact    = row["Contact"] != DBNull.Value ? row["Contact"].ToString() : "",
                            Email      = row["Email"] != DBNull.Value ? row["Email"].ToString() : "",
                            Address    = row["Address"] != DBNull.Value ? row["Address"].ToString() : "",
                            LedgerID   = row["LedgerID"] != DBNull.Value ? Convert.ToInt32(row["LedgerID"]) : 0
                        });
                    }
                    Suppliers = supps;

                    IsLoaded = true;
                }
                catch { /* fail silently, ViewModels fall back to DB */ }
            });
        }

        // ── SELECTIVE INVALIDATION ────────────────────────────────────────────
        public static void RefreshMedicines()
        {
            Task.Run(() =>
            {
                try
                {
                    var meds = new List<Medicine>();
                    DataTable mdt = _db.GetDataTable(
                        @"SELECT m.MedicineID, m.Name, m.Type, m.Category, m.Manufacturer, 
                                 m.Unit, m.PurchasePrice, m.SalePrice, m.MinStock, m.Status,
                                 COALESCE((SELECT SUM(Quantity) FROM Stocks s WHERE s.MedicineID = m.MedicineID), 0) as TotalStock
                          FROM Medicines m WHERE m.Status != 'Inactive' ORDER BY m.MedicineID ASC");
                    foreach (DataRow row in mdt.Rows)
                    {
                        meds.Add(new Medicine
                        {
                            MedicineID    = Convert.ToInt32(row["MedicineID"]),
                            Name          = row["Name"].ToString(),
                            Type          = row["Type"].ToString(),
                            Category      = row["Category"].ToString(),
                            Manufacturer  = row["Manufacturer"].ToString(),
                            Unit          = row["Unit"].ToString(),
                            PurchasePrice = Convert.ToDouble(row["PurchasePrice"]),
                            SalePrice     = Convert.ToDouble(row["SalePrice"]),
                            MinStock      = Convert.ToDouble(row["MinStock"]),
                            TotalStock    = Convert.ToDouble(row["TotalStock"]),
                            Status        = row["Status"].ToString()
                        });
                    }
                    Medicines = meds;

                    // Notify on UI thread
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        if (CacheRefreshed != null) CacheRefreshed(null, "Medicines");
                    });
                }
                catch { }
            });
        }

        public static void RefreshCustomers()
        {
            Task.Run(() =>
            {
                try
                {
                    var custs = new List<Customer>();
                    DataTable cdt = _db.GetDataTable("SELECT * FROM Customers ORDER BY Name");
                    foreach (DataRow row in cdt.Rows)
                    {
                        custs.Add(new Customer
                        {
                            CustomerID = Convert.ToInt32(row["CustomerID"]),
                            Name       = row["Name"].ToString(),
                            Contact    = row["Contact"].ToString(),
                            Email      = row["Email"].ToString(),
                            Address    = row["Address"].ToString(),
                            Balance    = Convert.ToDouble(row["Balance"]),
                            LedgerID   = row["LedgerID"] != DBNull.Value ? Convert.ToInt32(row["LedgerID"]) : 0
                        });
                    }
                    Customers = custs;
                    Application.Current.Dispatcher.Invoke(() => { if (CacheRefreshed != null) CacheRefreshed(null, "Customers"); });
                }
                catch { }
            });
        }

        public static void RefreshSuppliers()
        {
            Task.Run(() =>
            {
                try
                {
                    var supps = new List<Supplier>();
                    DataTable sdt = _db.GetDataTable("SELECT * FROM Suppliers ORDER BY Name");
                    foreach (DataRow row in sdt.Rows)
                    {
                        supps.Add(new Supplier
                        {
                            SupplierID = Convert.ToInt32(row["SupplierID"]),
                            Name       = row["Name"].ToString(),
                            Contact    = row["Contact"] != DBNull.Value ? row["Contact"].ToString() : "",
                            Email      = row["Email"] != DBNull.Value ? row["Email"].ToString() : "",
                            Address    = row["Address"] != DBNull.Value ? row["Address"].ToString() : "",
                            LedgerID   = row["LedgerID"] != DBNull.Value ? Convert.ToInt32(row["LedgerID"]) : 0
                        });
                    }
                    Suppliers = supps;
                    Application.Current.Dispatcher.Invoke(() => { if (CacheRefreshed != null) CacheRefreshed(null, "Suppliers"); });
                }
                catch { }
            });
        }

        // Event fired when cache is refreshed — ViewModels subscribe to stay in sync
        public static event EventHandler<string> CacheRefreshed;
    }
}

