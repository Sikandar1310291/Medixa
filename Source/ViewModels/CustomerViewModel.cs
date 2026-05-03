using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Data.SQLite;
using System.Threading.Tasks;
using System.Windows;
using PharmaBilling.Source.Data;
using PharmaBilling.Source.Models;

namespace PharmaBilling.Source.ViewModels
{
    public class CustomerViewModel : BaseViewModel
    {
        private DbHelper _db;

        private ObservableCollection<Customer> _customers;
        public ObservableCollection<Customer> Customers
        {
            get { return _customers; }
            set { _customers = value; OnPropertyChanged("Customers"); }
        }

        private Customer _selectedCustomer;
        public Customer SelectedCustomer
        {
            get { return _selectedCustomer; }
            set { _selectedCustomer = value; OnPropertyChanged("SelectedCustomer"); }
        }

        public CustomerViewModel()
        {
            _db = new DbHelper();
            Customers = new ObservableCollection<Customer>();
            LoadCustomers();
        }

        public void LoadCustomers()
        {
            Task.Run(() =>
            {
                // Always fetch fresh from DB so the grid reflects the latest
                // writes without relying on a possibly stale in-memory cache.
                var temp = FetchFromDb();

                Application.Current.Dispatcher.Invoke(() =>
                {
                    Customers = new ObservableCollection<Customer>(temp);
                });
            });
        }

        private List<Customer> FetchFromDb()
        {
            var temp = new List<Customer>();
            string sql = "SELECT * FROM Customers ORDER BY Name";
            DataTable dt = _db.GetDataTable(sql);
            foreach (DataRow row in dt.Rows)
            {
                temp.Add(new Customer
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
            return temp;
        }

        public bool SaveCustomer(Customer customer, out string errorMessage)
        {
            errorMessage = "";
            try
            {
                if (customer.CustomerID == 0)
                {
                    string sql = "INSERT INTO Customers (Name, Contact, Email, Address, Balance) VALUES (@name, @contact, @email, @address, @balance)";
                    int rows = _db.ExecuteNonQuery(sql, new SQLiteParameter[] {
                        new SQLiteParameter("@name",    customer.Name    ?? ""),
                        new SQLiteParameter("@contact", customer.Contact ?? ""),
                        new SQLiteParameter("@email",   customer.Email   ?? ""),
                        new SQLiteParameter("@address", customer.Address ?? ""),
                        new SQLiteParameter("@balance", customer.Balance)
                    });
                    if (rows == 0)
                    {
                        errorMessage = "Database INSERT returned 0 rows affected. Customer was not saved.";
                        return false;
                    }
                }
                else
                {
                    string sql = "UPDATE Customers SET Name=@name, Contact=@contact, Email=@email, Address=@address, Balance=@balance WHERE CustomerID=@id";
                    _db.ExecuteNonQuery(sql, new SQLiteParameter[] {
                        new SQLiteParameter("@name",    customer.Name    ?? ""),
                        new SQLiteParameter("@contact", customer.Contact ?? ""),
                        new SQLiteParameter("@email",   customer.Email   ?? ""),
                        new SQLiteParameter("@address", customer.Address ?? ""),
                        new SQLiteParameter("@balance", customer.Balance),
                        new SQLiteParameter("@id",      customer.CustomerID)
                    });
                }

                // Invalidate cache so sale-screen customer dropdowns
                // and other ViewModels pick up the new/edited record.
                AppCache.RefreshCustomers();

                LoadCustomers();
                return true;
            }
            catch (Exception ex)
            {
                errorMessage = "Database error: " + ex.Message;
                return false;
            }
        }

        public bool DeleteCustomer(int customerId, out string errorMessage)
        {
            errorMessage = "";
            try
            {
                string checkSql = "SELECT COUNT(*) FROM Sales WHERE CustomerID = @id";
                int count = Convert.ToInt32(_db.ExecuteScalar(checkSql, new SQLiteParameter[] { new SQLiteParameter("@id", customerId) }));
                if (count > 0)
                {
                    errorMessage = "Cannot delete this customer because they have purchase/sale history in the system.";
                    return false;
                }
                _db.ExecuteNonQuery("DELETE FROM Customers WHERE CustomerID = @id", new SQLiteParameter[] { new SQLiteParameter("@id", customerId) });
                LoadCustomers();
                return true;
            }
            catch (Exception ex)
            {
                errorMessage = "Error deleting customer: " + ex.Message;
                return false;
            }
        }
    }
}
