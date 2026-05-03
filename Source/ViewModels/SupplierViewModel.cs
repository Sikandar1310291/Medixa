using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Threading.Tasks;
using System.Windows;
using PharmaBilling.Source.Data;
using PharmaBilling.Source.Models;

namespace PharmaBilling.Source.ViewModels
{
    public class SupplierViewModel : BaseViewModel
    {
        private DbHelper _db;

        // Full unfiltered list — search filter works against this
        private List<Supplier> _allSuppliers = new List<Supplier>();

        private ObservableCollection<Supplier> _suppliers;
        public ObservableCollection<Supplier> Suppliers
        {
            get { return _suppliers; }
            set { _suppliers = value; OnPropertyChanged("Suppliers"); }
        }

        private Supplier _selectedSupplier;
        public Supplier SelectedSupplier
        {
            get { return _selectedSupplier; }
            set { _selectedSupplier = value; OnPropertyChanged("SelectedSupplier"); }
        }

        private bool _isBusy;
        public bool IsBusy
        {
            get { return _isBusy; }
            set { _isBusy = value; OnPropertyChanged("IsBusy"); }
        }

        public SupplierViewModel()
        {
            _db = new DbHelper();
            Suppliers = new ObservableCollection<Supplier>();
            LoadSuppliers();
        }

        // ── LOAD ──────────────────────────────────────────────────────────────
        public void LoadSuppliers()
        {
            Task.Run(() =>
            {
                var temp = FetchFromDb();
                Application.Current.Dispatcher.Invoke(() =>
                {
                    _allSuppliers = temp;
                    Suppliers = new ObservableCollection<Supplier>(temp);
                });
            });
        }

        private List<Supplier> FetchFromDb()
        {
            var list = new List<Supplier>();
            DataTable dt = _db.GetDataTable("SELECT * FROM Suppliers ORDER BY Name");
            foreach (DataRow row in dt.Rows)
            {
                list.Add(new Supplier
                {
                    SupplierID = Convert.ToInt32(row["SupplierID"]),
                    Name    = row["Name"].ToString(),
                    Contact = row["Contact"] != DBNull.Value ? row["Contact"].ToString() : "",
                    Email   = row["Email"]   != DBNull.Value ? row["Email"].ToString()   : "",
                    Address = row["Address"] != DBNull.Value ? row["Address"].ToString() : "",
                    LedgerID = row["LedgerID"] != DBNull.Value ? Convert.ToInt32(row["LedgerID"]) : 0
                });
            }
            return list;
        }

        // ── SEARCH (instant, no DB call) ───────────────────────────────────────
        public void FilterSuppliers(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                Suppliers = new ObservableCollection<Supplier>(_allSuppliers);
                return;
            }
            query = query.ToLower();
            var filtered = _allSuppliers.FindAll(s =>
                (s.Name    != null && s.Name.ToLower().Contains(query)) ||
                (s.Contact != null && s.Contact.ToLower().Contains(query)) ||
                (s.Email   != null && s.Email.ToLower().Contains(query)) ||
                (s.Address != null && s.Address.ToLower().Contains(query))
            );
            Suppliers = new ObservableCollection<Supplier>(filtered);
        }

        // ── SAVE (async — never blocks UI) ────────────────────────────────────
        // Returns empty string on success, error message on failure.
        public Task<string> SaveSupplierAsync(Supplier supplier)
        {
            return Task.Run(() =>
            {
                try
                {
                    if (supplier.SupplierID == 0)
                    {
                        // INSERT
                        string sql = "INSERT INTO Suppliers (Name, Contact, Email, Address, LedgerID) " +
                                     "VALUES (@Name, @Contact, @Email, @Address, 0)";
                        int rows = _db.ExecuteNonQuery(sql, new System.Data.SQLite.SQLiteParameter[]
                        {
                            new System.Data.SQLite.SQLiteParameter("@Name",    supplier.Name    ?? ""),
                            new System.Data.SQLite.SQLiteParameter("@Contact", supplier.Contact ?? ""),
                            new System.Data.SQLite.SQLiteParameter("@Email",   supplier.Email   ?? ""),
                            new System.Data.SQLite.SQLiteParameter("@Address", supplier.Address ?? "")
                        });
                        if (rows == 0)
                            return "Database INSERT returned 0 rows. Supplier was not saved.";
                    }
                    else
                    {
                        // UPDATE
                        string sql = "UPDATE Suppliers SET Name=@Name, Contact=@Contact, " +
                                     "Email=@Email, Address=@Address WHERE SupplierID=@ID";
                        _db.ExecuteNonQuery(sql, new System.Data.SQLite.SQLiteParameter[]
                        {
                            new System.Data.SQLite.SQLiteParameter("@Name",    supplier.Name    ?? ""),
                            new System.Data.SQLite.SQLiteParameter("@Contact", supplier.Contact ?? ""),
                            new System.Data.SQLite.SQLiteParameter("@Email",   supplier.Email   ?? ""),
                            new System.Data.SQLite.SQLiteParameter("@Address", supplier.Address ?? ""),
                            new System.Data.SQLite.SQLiteParameter("@ID",      supplier.SupplierID)
                        });
                    }

                    // Reload from DB on THIS background thread — single DB hit, no race.
                    var freshList = FetchFromDb();
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        _allSuppliers = freshList;
                        Suppliers = new ObservableCollection<Supplier>(freshList);
                    });

                    // Update shared cache for Purchase dropdowns etc.
                    AppCache.RefreshSuppliers();

                    return ""; // empty = success
                }
                catch (Exception ex)
                {
                    return "Database error: " + ex.Message;
                }
            });
        }

        // ── DELETE (async — never blocks UI) ──────────────────────────────────
        public Task<string> DeleteSupplierAsync(int supplierId)
        {
            return Task.Run(() =>
            {
                try
                {
                    int count = Convert.ToInt32(_db.ExecuteScalar(
                        "SELECT COUNT(*) FROM Purchases WHERE SupplierID = @ID",
                        new System.Data.SQLite.SQLiteParameter[]
                        {
                            new System.Data.SQLite.SQLiteParameter("@ID", supplierId)
                        }));
                    if (count > 0)
                        return "Cannot delete: this supplier has " + count + " purchase invoice(s) linked to them.";

                    _db.ExecuteNonQuery("DELETE FROM Suppliers WHERE SupplierID = @ID",
                        new System.Data.SQLite.SQLiteParameter[]
                        {
                            new System.Data.SQLite.SQLiteParameter("@ID", supplierId)
                        });

                    var freshList = FetchFromDb();
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        _allSuppliers = freshList;
                        Suppliers = new ObservableCollection<Supplier>(freshList);
                        SelectedSupplier = null;
                    });

                    AppCache.RefreshSuppliers();

                    return ""; // success
                }
                catch (Exception ex)
                {
                    return "Database error: " + ex.Message;
                }
            });
        }
    }
}
