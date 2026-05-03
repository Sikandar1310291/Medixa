using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using PharmaBilling.Source.Data;
using PharmaBilling.Source.Models;

namespace PharmaBilling.Source.ViewModels
{
    public class SaleViewModel : BaseViewModel
    {
        private DbHelper _db;
        public ObservableCollection<SaleItemViewModel> SaleItems { get; set; }
        private ObservableCollection<Customer> _customers;
        public ObservableCollection<Customer> Customers
        {
            get { return _customers; }
            set { _customers = value; OnPropertyChanged("Customers"); }
        }

        private ObservableCollection<Medicine> _allMedicines;
        public ObservableCollection<Medicine> AllMedicines
        {
            get { return _allMedicines; }
            set { _allMedicines = value; OnPropertyChanged("AllMedicines"); }
        }

        private string _docNo;
        public string DocNo
        {
            get { return _docNo; }
            set { _docNo = value; OnPropertyChanged("DocNo"); }
        }

        private DateTime _saleDate = DateTime.Now;
        public DateTime SaleDate
        {
            get { return _saleDate; }
            set { _saleDate = value; OnPropertyChanged("SaleDate"); }
        }

        private string _billNo;
        public string BillNo
        {
            get { return _billNo; }
            set { _billNo = value; OnPropertyChanged("BillNo"); }
        }

        private string _saleType = "CASH";
        public string SaleType
        {
            get { return _saleType; }
            set { _saleType = value; OnPropertyChanged("SaleType"); }
        }

        public ObservableCollection<string> SaleTypes { get; set; }

        private string _cashPartyName;
        public string CashPartyName
        {
            get { return _cashPartyName; }
            set { _cashPartyName = value; OnPropertyChanged("CashPartyName"); }
        }

        private string _remarks;
        public string Remarks
        {
            get { return _remarks; }
            set { _remarks = value; OnPropertyChanged("Remarks"); }
        }

        private double _grossTotal;
        public double GrossTotal
        {
            get { return _grossTotal; }
            set { _grossTotal = value; OnPropertyChanged("GrossTotal"); CalculateGrandTotal(); }
        }

        private double _discountPercent;
        public double DiscountPercent
        {
            get { return _discountPercent; }
            set { _discountPercent = value; OnPropertyChanged("DiscountPercent"); CalculateGrandTotal(); }
        }

        private double _taxAmount;
        public double TaxAmount
        {
            get { return _taxAmount; }
            set { _taxAmount = value; OnPropertyChanged("TaxAmount"); }
        }

        private double _taxPercent;
        public double TaxPercent
        {
            get { return _taxPercent; }
            set { _taxPercent = value; OnPropertyChanged("TaxPercent"); CalculateGrandTotal(); }
        }

        private double _cashReceived;
        public double CashReceived
        {
            get { return _cashReceived; }
            set { _cashReceived = value; OnPropertyChanged("CashReceived"); CalculateBalance(); }
        }

        private double _balance;
        public double Balance
        {
            get { return _balance; }
            set { _balance = value; OnPropertyChanged("Balance"); }
        }

        private double _grandTotal;
        public double GrandTotal
        {
            get { return _grandTotal; }
            set { _grandTotal = value; OnPropertyChanged("GrandTotal"); CalculateBalance(); }
        }

        public SaleViewModel()
        {
            _db = new DbHelper();
            SaleItems = new ObservableCollection<SaleItemViewModel>();
            SaleItems.CollectionChanged += SaleItems_CollectionChanged;
            Customers = new ObservableCollection<Customer>();
            AllMedicines = new ObservableCollection<Medicine>();
            SaleTypes = new ObservableCollection<string> { "CASH", "CREDIT" };
            
            LoadCustomers();
            LoadMedicines();
        }

        void SaleItems_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems != null)
                foreach (SaleItemViewModel item in e.NewItems)
                    item.PropertyChanged += Item_PropertyChanged;

            if (e.OldItems != null)
                foreach (SaleItemViewModel item in e.OldItems)
                    item.PropertyChanged -= Item_PropertyChanged;
            
            CalculateTotals();
        }

        void Item_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "Quantity" || e.PropertyName == "UnitPrice" || e.PropertyName == "TotalPrice")
            {
                CalculateTotals();
                if (SelectedSaleItem == sender)
                {
                    UpdateSelectedStock();
                }
            }
        }

        public void LoadCustomers()
        {
            System.Threading.Tasks.Task.Run(() =>
            {
                var tempList = AppCache.IsLoaded
                    ? new System.Collections.Generic.List<Customer>(AppCache.Customers)
                    : FetchCustomersFromDb();

                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    Customers = new ObservableCollection<Customer>(tempList);
                });
            });
        }

        private System.Collections.Generic.List<Customer> FetchCustomersFromDb()
        {
            var list = new System.Collections.Generic.List<Customer>();
            DataTable dt = _db.GetDataTable("SELECT * FROM Customers");
            foreach (DataRow row in dt.Rows)
            {
                list.Add(new Customer
                {
                    CustomerID = Convert.ToInt32(row["CustomerID"]),
                    Name       = row["Name"].ToString(),
                    Contact    = row["Contact"].ToString(),
                    Address    = row["Address"].ToString(),
                    Balance    = Convert.ToDouble(row["Balance"])
                });
            }
            return list;
        }

        public void LoadMedicines()
        {
            System.Threading.Tasks.Task.Run(() =>
            {
                // ALWAYS read from DB — never the AppCache.
                // AppCache.Medicines is loaded once at login and is stale
                // as soon as a purchase is made (new stock, updated prices).
                // Reading fresh data here is the only way to guarantee the
                // sale screen shows the correct Retail Price and Available Stock.
                var tempList = FetchMedicinesFromDb();

                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    AllMedicines = new ObservableCollection<Medicine>(tempList);
                });
            });
        }

        private System.Collections.Generic.List<Medicine> FetchMedicinesFromDb()
        {
            var list = new System.Collections.Generic.List<Medicine>();
            string sql = @"SELECT m.MedicineID, m.Name, m.Type, m.PurchasePrice, m.SalePrice,
                            COALESCE((SELECT SUM(Quantity) FROM Stocks s WHERE s.MedicineID = m.MedicineID),0) as TotalStock 
                           FROM Medicines m WHERE m.Status != 'Inactive'";
            DataTable dt = _db.GetDataTable(sql);
            foreach (DataRow row in dt.Rows)
            {
                list.Add(new Medicine
                {
                    MedicineID    = Convert.ToInt32(row["MedicineID"]),
                    Name          = row["Name"].ToString(),
                    Type          = row["Type"].ToString(),
                    PurchasePrice = Convert.ToDouble(row["PurchasePrice"]),
                    SalePrice     = Convert.ToDouble(row["SalePrice"]),
                    TotalStock    = Convert.ToDouble(row["TotalStock"])
                });
            }
            return list;
        }

        private SaleItemViewModel _selectedSaleItem;
        public SaleItemViewModel SelectedSaleItem
        {
            get { return _selectedSaleItem; }
            set 
            { 
                _selectedSaleItem = value; 
                OnPropertyChanged("SelectedSaleItem");
                UpdateSelectedStock();
            }
        }

        private double _selectedStock;
        public double SelectedStock
        {
            get { return _selectedStock; }
            set { _selectedStock = value; OnPropertyChanged("SelectedStock"); }
        }

        private double _balancedStock;
        public double BalancedStock
        {
            get { return _balancedStock; }
            set { _balancedStock = value; OnPropertyChanged("BalancedStock"); }
        }

        private void UpdateSelectedStock()
        {
            if (SelectedSaleItem != null)
            {
                var med = AllMedicines.FirstOrDefault(m => m.MedicineID == SelectedSaleItem.MedicineID);
                SelectedStock = med != null ? med.TotalStock : 0;
                BalancedStock = SelectedStock - SelectedSaleItem.Quantity;
            }
            else
            {
                SelectedStock = 0;
                BalancedStock = 0;
            }
        }

        public void AddToSale(Medicine med, int qty)
        {
            var existing = SaleItems.FirstOrDefault(i => i.MedicineID == med.MedicineID);
            if (existing != null)
            {
                existing.Quantity += qty;
                SelectedSaleItem = existing;
            }
            else
            {
                // Fetch the default or earliest batch/box for this medicine
                string RackNo = "Rack-1";
                string batchNo = "Default";
                try 
                {
                    var dt = _db.GetDataTable("SELECT BatchNo, RackNo FROM Stocks WHERE MedicineID = @id AND Quantity > 0 ORDER BY StockID ASC LIMIT 1",
                        new System.Data.SQLite.SQLiteParameter[] { new System.Data.SQLite.SQLiteParameter("@id", med.MedicineID) });
                    if (dt.Rows.Count > 0)
                    {
                        batchNo = dt.Rows[0]["BatchNo"].ToString();
                        RackNo = dt.Rows[0]["RackNo"].ToString();
                    }
                }
                catch { }

                var newItem = new SaleItemViewModel
                {
                    MedicineID = med.MedicineID,
                    MedicineName = med.Name,
                    BatchNo = batchNo,
                    RackNo = RackNo,
                    Quantity = qty,
                    // SalePrice in DB is now stored as per-unit price (fixed in PurchaseViewModel)
                    UnitPrice = med.SalePrice,
                    TP = med.BoxSize > 0 ? med.PurchasePrice / med.BoxSize : med.PurchasePrice,
                    // Retail (Box) = unit price × pack size, for reference display
                    Retail = med.BoxSize > 0 ? med.SalePrice * med.BoxSize : med.SalePrice
                };
                SaleItems.Add(newItem);
                SelectedSaleItem = newItem;
            }
            CalculateTotals();
        }

        public void RemoveFromSale(SaleItemViewModel item)
        {
            SaleItems.Remove(item);
            CalculateTotals();
        }

        public void CalculateTotals()
        {
            GrossTotal = SaleItems.Sum(i => i.TotalPrice);
        }

        private void CalculateGrandTotal()
        {
            double discountAmount = GrossTotal * (DiscountPercent / 100.0);
            TaxAmount = GrossTotal * (TaxPercent / 100.0);
            GrandTotal = GrossTotal - discountAmount + TaxAmount;
        }

        private void CalculateBalance()
        {
            Balance = GrandTotal - CashReceived;
        }

        public Customer GetOrCreateCustomer(string name, string contact = "")
        {
            if (string.IsNullOrEmpty(name)) return null;

            var existing = Customers.FirstOrDefault(c => c.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
            if (existing != null) return existing;

            // Create new customer
            string sql = "INSERT INTO Customers (Name, Contact) VALUES (@Name, @Contact); SELECT last_insert_rowid();";
            object result = _db.ExecuteScalar(sql, new System.Data.SQLite.SQLiteParameter[] {
                new System.Data.SQLite.SQLiteParameter("@Name", name),
                new System.Data.SQLite.SQLiteParameter("@Contact", contact)
            });

            int newId = Convert.ToInt32(result);
            var newCust = new Customer { CustomerID = newId, Name = name, Contact = contact };
            Customers.Add(newCust);
            return newCust;
        }

        public int SaveSale(string status = "Paid")
        {
            if (SaleItems.Count == 0) return -1;

            string customerName = string.IsNullOrEmpty(CashPartyName) ? "Walk-in Customer" : CashPartyName;
            Customer cust = GetOrCreateCustomer(customerName);
            if (cust == null) return -1;

            var commands = new System.Collections.Generic.List<Tuple<string, System.Data.SQLite.SQLiteParameter[]>>();
            string fbrInvoiceNo = "FBR-" + DateTime.Now.ToString("yyyyMMdd-HHmmss");

            // 1. Insert Sales header
            string saleSql = @"INSERT INTO Sales (CustomerID, SaleDate, TotalAmount, Discount, Tax, NetPaid, Status, FBRInvoiceNo)
                               VALUES (@CustId, @Date, @Total, @Disc, @Tax, @Net, @Status, @FBR);";
            commands.Add(new Tuple<string, System.Data.SQLite.SQLiteParameter[]>(saleSql, new System.Data.SQLite.SQLiteParameter[]
            {
                new System.Data.SQLite.SQLiteParameter("@CustId", cust.CustomerID),
                new System.Data.SQLite.SQLiteParameter("@Date", SaleDate.ToString("yyyy-MM-dd HH:mm:ss")),
                new System.Data.SQLite.SQLiteParameter("@Total", GrossTotal),
                new System.Data.SQLite.SQLiteParameter("@Disc", DiscountPercent),
                new System.Data.SQLite.SQLiteParameter("@Tax", TaxAmount),
                new System.Data.SQLite.SQLiteParameter("@Net", GrandTotal),
                new System.Data.SQLite.SQLiteParameter("@Status", status),
                new System.Data.SQLite.SQLiteParameter("@FBR", fbrInvoiceNo)
            }));

            // 2. Insert SaleDetails + deduct stock (FIFO)
            foreach (var item in SaleItems)
            {
                // Insert detail row
                string detailSql = @"INSERT INTO SaleDetails (SaleID, MedicineID, BatchNo, RackNo, Quantity, UnitPrice, TotalPrice)
                                     SELECT MAX(SaleID), @MedId, @Batch, @Box, @Qty, @Price, @Total FROM Sales;";
                commands.Add(new Tuple<string, System.Data.SQLite.SQLiteParameter[]>(detailSql, new System.Data.SQLite.SQLiteParameter[]
                {
                    new System.Data.SQLite.SQLiteParameter("@MedId", item.MedicineID),
                    new System.Data.SQLite.SQLiteParameter("@Batch", string.IsNullOrEmpty(item.BatchNo) ? "Default" : item.BatchNo),
                    new System.Data.SQLite.SQLiteParameter("@Box", string.IsNullOrEmpty(item.RackNo) ? "Rack-1" : item.RackNo),
                    new System.Data.SQLite.SQLiteParameter("@Qty", item.Quantity),
                    new System.Data.SQLite.SQLiteParameter("@Price", item.UnitPrice),
                    new System.Data.SQLite.SQLiteParameter("@Total", item.TotalPrice)
                }));

                // FIFO stock deduction: fetch batches beforehand
                double remaining = item.Quantity;
                string getBatchesSql = "SELECT StockID, Quantity FROM Stocks WHERE MedicineID = @MedId AND Quantity > 0 ORDER BY StockID ASC";
                DataTable batches = _db.GetDataTable(getBatchesSql, new System.Data.SQLite.SQLiteParameter[] { new System.Data.SQLite.SQLiteParameter("@MedId", item.MedicineID) });

                foreach (DataRow batch in batches.Rows)
                {
                    if (remaining <= 0) break;
                    double batchQty = Convert.ToDouble(batch["Quantity"]);
                    double deduct = Math.Min(batchQty, remaining);
                    remaining -= deduct;

                    string deductSql = "UPDATE Stocks SET Quantity = Quantity - @Deduct WHERE StockID = @StockId";
                    commands.Add(new Tuple<string, System.Data.SQLite.SQLiteParameter[]>(deductSql, new System.Data.SQLite.SQLiteParameter[]
                    {
                        new System.Data.SQLite.SQLiteParameter("@Deduct", deduct),
                        new System.Data.SQLite.SQLiteParameter("@StockId", Convert.ToInt32(batch["StockID"]))
                    }));
                }
            }

            // 3. Post to Ledger: Debit Cash Account, Credit Sales Income
            string desc = string.Format("Sale — {0}", customerName);

            string ledgerSql = @"
                INSERT INTO LedgerTransactions (LedgerID, Description, Debit, Credit, Balance)
                SELECT LedgerID, @Desc, @Amt, 0, Balance + @Amt FROM Ledgers WHERE Name='Cash Account';
                
                UPDATE Ledgers SET Balance = Balance + @Amt WHERE Name='Cash Account';
                
                INSERT INTO LedgerTransactions (LedgerID, Description, Debit, Credit, Balance)
                SELECT LedgerID, @Desc, 0, @Amt, Balance + @Amt FROM Ledgers WHERE Name='Sales Income';
                
                UPDATE Ledgers SET Balance = Balance + @Amt WHERE Name='Sales Income';
            ";

            commands.Add(new Tuple<string, System.Data.SQLite.SQLiteParameter[]>(ledgerSql, new System.Data.SQLite.SQLiteParameter[]
            {
                new System.Data.SQLite.SQLiteParameter("@Desc", desc),
                new System.Data.SQLite.SQLiteParameter("@Amt", GrandTotal)
            }));

            try
            {
                bool success = _db.ExecuteTransaction(commands);
                if (success)
                {
                    object saleIdObj = _db.ExecuteScalar("SELECT MAX(SaleID) FROM Sales");
                    int savedSaleId = saleIdObj != null && saleIdObj != DBNull.Value ? Convert.ToInt32(saleIdObj) : -1;

                    if (savedSaleId > 0)
                    {
                        // Build items list for cloud sync
                        var cloudItems = new System.Collections.Generic.List<System.Collections.Generic.Dictionary<string, object>>();
                        foreach (var item in SaleItems)
                        {
                            cloudItems.Add(new System.Collections.Generic.Dictionary<string, object>
                            {
                                { "MedicineID",  item.MedicineID  },
                                { "MedicineName",item.MedicineName},
                                { "BatchNo",     item.BatchNo     },
                                { "RackNo",      item.RackNo      },
                                { "Quantity",    item.Quantity    },
                                { "UnitPrice",   item.UnitPrice   },
                                { "TotalPrice",  item.TotalPrice  }
                            });
                        }
                        // Upload to cloud silently in background
                        PharmaBilling.Source.Data.CloudSyncService.UploadSaleAsync(
                            savedSaleId, customerName,
                            SaleDate.ToString("yyyy-MM-dd HH:mm:ss"),
                            GrossTotal, DiscountPercent, GrandTotal,
                            status, cloudItems);
                    }

                    return savedSaleId;
                }
                return -1;
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(ex.Message);
                return -1;
            }
        }


    }
}
