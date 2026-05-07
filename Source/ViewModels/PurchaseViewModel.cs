using System;
using System.Collections.ObjectModel;
using System.Data;
using System.Linq;
using PharmaBilling.Source.Data;
using PharmaBilling.Source.Models;

namespace PharmaBilling.Source.ViewModels
{
    public class PurchaseViewModel : BaseViewModel
    {
        private DbHelper _db;

        // For the List View
        public ObservableCollection<PurchaseEntry> PastPurchases { get; set; }
        public ObservableCollection<PurchaseReturnEntry> PastPurchaseReturns { get; set; }

        // For the Add View
        public ObservableCollection<PurchaseItemViewModel> PurchaseItems { get; set; }
        public ObservableCollection<Supplier> Suppliers { get; set; }
        public ObservableCollection<Medicine> AllMedicines { get; set; }
        
        private ObservableCollection<Medicine> _filteredMedicines;
        public ObservableCollection<Medicine> FilteredMedicines 
        { 
            get { return _filteredMedicines; } 
            set { _filteredMedicines = value; OnPropertyChanged("FilteredMedicines"); } 
        }

        private string _invoiceNo;
        public string InvoiceNo
        {
            get { return _invoiceNo; }
            set { _invoiceNo = value; OnPropertyChanged("InvoiceNo"); }
        }

        private DateTime _purchaseDate = DateTime.Now;
        public DateTime PurchaseDate
        {
            get { return _purchaseDate; }
            set { _purchaseDate = value; OnPropertyChanged("PurchaseDate"); }
        }

        private Supplier _selectedSupplier;
        public Supplier SelectedSupplier
        {
            get { return _selectedSupplier; }
            set { _selectedSupplier = value; OnPropertyChanged("SelectedSupplier"); }
        }

        private double _grossTotal;
        public double GrossTotal
        {
            get { return _grossTotal; }
            set { _grossTotal = value; OnPropertyChanged("GrossTotal"); }
        }

        public PurchaseViewModel()
        {
            _db = new DbHelper();
            PastPurchases = new ObservableCollection<PurchaseEntry>();
            PastPurchaseReturns = new ObservableCollection<PurchaseReturnEntry>();
            PurchaseItems = new ObservableCollection<PurchaseItemViewModel>();
            PurchaseItems.CollectionChanged += PurchaseItems_CollectionChanged;
            Suppliers = new ObservableCollection<Supplier>();
            AllMedicines = new ObservableCollection<Medicine>();
            FilteredMedicines = new ObservableCollection<Medicine>();
            
            LoadPastPurchases();
            LoadSuppliers();
            LoadMedicines();

            InvoiceNo = "INV-" + DateTime.Now.Ticks.ToString().Substring(10);
        }

        public void LoadPastPurchases() { LoadPastPurchasesByType(null); }

        public void LoadPastPurchasesByType(string purchaseType)
        {
            PastPurchases.Clear();
            string filter = string.IsNullOrEmpty(purchaseType)
                ? ""
                : " AND IFNULL(p.PurchaseType,'Normal') = '" + purchaseType + "'";
            string sql = @"SELECT p.PurchaseID, s.Name as SupplierName, p.InvoiceNo, p.PurchaseDate,
                                  p.TotalAmount, p.Status, IFNULL(p.PurchaseType,'Normal') as PurchaseType,
                                  (SELECT GROUP_CONCAT(m.Name, ', ') FROM PurchaseDetails pd
                                   JOIN Medicines m ON pd.MedicineID = m.MedicineID
                                   WHERE pd.PurchaseID = p.PurchaseID) as Items
                           FROM Purchases p
                           LEFT JOIN Suppliers s ON p.SupplierID = s.SupplierID
                           WHERE 1=1" + filter + @"
                           ORDER BY p.PurchaseID DESC";
            DataTable dt = _db.GetDataTable(sql);
            foreach (DataRow row in dt.Rows)
            {
                PastPurchases.Add(new PurchaseEntry
                {
                    PurchaseID   = Convert.ToInt32(row["PurchaseID"]),
                    SupplierName = row["SupplierName"] != DBNull.Value ? row["SupplierName"].ToString() : "N/A",
                    InvoiceNo    = row["InvoiceNo"].ToString(),
                    PurchaseDate = row["PurchaseDate"].ToString(),
                    TotalAmount  = string.Format("Rs. {0:N2}", Convert.ToDouble(row["TotalAmount"])),
                    Status       = row["Status"].ToString(),
                    PurchaseType = row["PurchaseType"].ToString(),
                    Items        = row.Table.Columns.Contains("Items") && row["Items"] != DBNull.Value ? row["Items"].ToString() : ""
                });
            }
        }

        public void LoadSuppliers()
        {
            Suppliers.Clear();
            DataTable dt = _db.GetDataTable("SELECT * FROM Suppliers");
            foreach (DataRow row in dt.Rows)
            {
                Suppliers.Add(new Supplier
                {
                    SupplierID = Convert.ToInt32(row["SupplierID"]),
                    Name = row["Name"].ToString()
                });
            }
        }

        public void LoadMedicines()
        {
            var tempList = new System.Collections.Generic.List<Medicine>();
            DataTable dt = _db.GetDataTable("SELECT * FROM Medicines");
            foreach (DataRow row in dt.Rows)
            {
                var m = new Medicine
                {
                    MedicineID = Convert.ToInt32(row["MedicineID"]),
                    Name = row["Name"].ToString(),
                    PurchasePrice = Convert.ToDouble(row["PurchasePrice"]),
                    SalePrice = Convert.ToDouble(row["SalePrice"]),
                    BoxSize = row.Table.Columns.Contains("BoxSize") && row["BoxSize"] != DBNull.Value ? Convert.ToDouble(row["BoxSize"]) : 1
                };
                tempList.Add(m);
            }
            AllMedicines = new ObservableCollection<Medicine>(tempList);
            FilteredMedicines = new ObservableCollection<Medicine>(tempList.Take(50));
        }

        public async System.Threading.Tasks.Task FilterMedicinesAsync(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    FilteredMedicines = new ObservableCollection<Medicine>(AllMedicines.Take(50));
                });
                return;
            }

            var results = await System.Threading.Tasks.Task.Run(() =>
            {
                // Take Top 50 to prevent combobox UI freeze with 16k items
                return AllMedicines.Where(m => m.Name.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0).Take(50).ToList();
            });

            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                FilteredMedicines = new ObservableCollection<Medicine>(results);
            });
        }

        public Medicine CreateQuickMedicine(string name)
        {
            string sql = @"INSERT INTO Medicines (Name, Type, Category, Manufacturer, Unit, PurchasePrice, SalePrice, MinStock, Status) 
                           VALUES (@Name, 'Tablet', 'General', 'Unknown', 'Piece', 0, 0, 10, 'Active');
                           SELECT last_insert_rowid();";
            int newId = Convert.ToInt32(_db.ExecuteScalar(sql, new System.Data.SQLite.SQLiteParameter[] { new System.Data.SQLite.SQLiteParameter("@Name", name) }));
            Medicine m = new Medicine { MedicineID = newId, Name = name, PurchasePrice = 0, SalePrice = 0 };
            AllMedicines.Add(m);
            return m;
        }

        public Supplier CreateQuickSupplier(string name)
        {
            string sql = @"INSERT INTO Suppliers (Name, Contact, Email, Address, LedgerID) VALUES (@Name, '', '', '', 0); SELECT last_insert_rowid();";
            int newId = Convert.ToInt32(_db.ExecuteScalar(sql, new System.Data.SQLite.SQLiteParameter[] { new System.Data.SQLite.SQLiteParameter("@Name", name) }));
            Supplier s = new Supplier { SupplierID = newId, Name = name };
            Suppliers.Add(s);
            return s;
        }

        void PurchaseItems_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems != null)
                foreach (PurchaseItemViewModel item in e.NewItems)
                    item.PropertyChanged += Item_PropertyChanged;

            if (e.OldItems != null)
                foreach (PurchaseItemViewModel item in e.OldItems)
                    item.PropertyChanged -= Item_PropertyChanged;
            
            CalculateTotals();
        }

        void Item_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "Quantity" || e.PropertyName == "TP" || 
                e.PropertyName == "TotalPrice" || e.PropertyName == "Packs" || e.PropertyName == "PackSize")
            {
                CalculateTotals();
            }
        }

        public void AddToPurchase(Medicine med, int qty)
        {
            double initPackSize = med.BoxSize > 0 ? med.BoxSize : 1;
            
            var newItem = new PurchaseItemViewModel();
            newItem.BeginBatchUpdate();

            newItem.MedicineID   = med.MedicineID;
            newItem.MedicineName = med.Name;
            newItem.BatchNo      = "BATCH-" + DateTime.Now.ToString("MMdd");
            newItem.RackNo       = "A-1";
            newItem.ExpiryDate   = DateTime.Now.AddYears(1).ToString("yyyy-MM-dd");
            newItem.PackSize     = initPackSize;
            
            // DB stores PurchasePrice and SalePrice per-unit. 
            // In the UI, TP and Retail are always per-PACK.
            newItem.TP           = med.PurchasePrice * initPackSize;
            newItem.Retail       = med.SalePrice * initPackSize;
            
            newItem.Packs        = 1;
            newItem.LooseQty     = 0;
            
            newItem.EndBatchUpdate();
            
            PurchaseItems.Add(newItem);
        }

        public void RemoveFromPurchase(PurchaseItemViewModel item)
        {
            PurchaseItems.Remove(item);
        }

        public void CalculateTotals()
        {
            GrossTotal = PurchaseItems.Sum(i => i.TotalPrice);
        }

        public bool SavePurchase() { return SavePurchaseWithType("Normal"); }

        public bool SavePurchaseWithType(string purchaseType)
        {
            if (PurchaseItems.Count == 0) return false;

            var commands = new System.Collections.Generic.List<Tuple<string, System.Data.SQLite.SQLiteParameter[]>>();

            // 1. Insert Purchase
            string purchSql = @"INSERT INTO Purchases (SupplierID, InvoiceNo, PurchaseDate, TotalAmount, Status, PurchaseType) 
                                VALUES (@SuppId, @Inv, @Date, @Total, 'Received', @PType);";
            var purchParams = new System.Data.SQLite.SQLiteParameter[]
            {
                new System.Data.SQLite.SQLiteParameter("@SuppId", SelectedSupplier != null ? SelectedSupplier.SupplierID : (object)DBNull.Value),
                new System.Data.SQLite.SQLiteParameter("@Inv", InvoiceNo),
                new System.Data.SQLite.SQLiteParameter("@Date", PurchaseDate.ToString("yyyy-MM-dd HH:mm:ss")),
                new System.Data.SQLite.SQLiteParameter("@Total", GrossTotal),
                new System.Data.SQLite.SQLiteParameter("@PType", purchaseType ?? "Normal")
            };
            commands.Add(new Tuple<string, System.Data.SQLite.SQLiteParameter[]>(purchSql, purchParams));

            // 2. Insert Details & 3. Update Stocks
            foreach (var item in PurchaseItems)
            {
                // Derive per-unit values for DB storage.
                // DB always stores prices per-unit so Sale screen can work at tablet/unit level.
                double perUnitTP     = item.PackSize > 0 ? item.TP     / item.PackSize : item.TP;
                double perUnitRetail = item.PackSize > 0 ? item.Retail / item.PackSize : item.Retail;

                // Also store PackSize so that editing this purchase later always
                // reconstructs the EXACT same Packs/TP values — regardless of whether
                // the medicine's BoxSize is changed afterwards.
                string detSql = @"INSERT INTO PurchaseDetails
                                    (PurchaseID, MedicineID, BatchNo, ExpiryDate, Quantity, PurchasePrice, TotalPrice, PackSize)
                                  SELECT p.PurchaseID, @MID, @Batch, @Exp, @Qty, @Price, @Total, @PackSize
                                  FROM Purchases p WHERE p.InvoiceNo = @Inv ORDER BY p.PurchaseID DESC LIMIT 1;";

                var detParams = new System.Data.SQLite.SQLiteParameter[]
                {
                    new System.Data.SQLite.SQLiteParameter("@Inv",      InvoiceNo),
                    new System.Data.SQLite.SQLiteParameter("@MID",      item.MedicineID),
                    new System.Data.SQLite.SQLiteParameter("@Batch",    item.BatchNo),
                    new System.Data.SQLite.SQLiteParameter("@Exp",      item.ExpiryDate),
                    new System.Data.SQLite.SQLiteParameter("@Qty",      item.Quantity),
                    new System.Data.SQLite.SQLiteParameter("@Price",    perUnitTP),
                    new System.Data.SQLite.SQLiteParameter("@Total",    item.TotalPrice),
                    new System.Data.SQLite.SQLiteParameter("@PackSize", item.PackSize)
                };
                commands.Add(new Tuple<string, System.Data.SQLite.SQLiteParameter[]>(detSql, detParams));

                string stockSql = @"INSERT INTO Stocks (MedicineID, BatchNo, RackNo, ExpiryDate, Quantity, DateAdded) 
                                    VALUES (@MID, @Batch, @Box, @Exp, @Qty, @Date)";
                var stockParams = new System.Data.SQLite.SQLiteParameter[]
                {
                    new System.Data.SQLite.SQLiteParameter("@MID", item.MedicineID),
                    new System.Data.SQLite.SQLiteParameter("@Batch", item.BatchNo),
                    new System.Data.SQLite.SQLiteParameter("@Box", item.RackNo),
                    new System.Data.SQLite.SQLiteParameter("@Exp", item.ExpiryDate),
                    new System.Data.SQLite.SQLiteParameter("@Qty", item.Quantity),
                    new System.Data.SQLite.SQLiteParameter("@Date", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"))
                };
                commands.Add(new Tuple<string, System.Data.SQLite.SQLiteParameter[]>(stockSql, stockParams));

                string medUpdateSql = @"UPDATE Medicines SET SalePrice = @Retail, PurchasePrice = @TP, BoxSize = @BoxSize WHERE MedicineID = @MID";
                var medParams = new System.Data.SQLite.SQLiteParameter[]
                {
                    new System.Data.SQLite.SQLiteParameter("@Retail", perUnitRetail),
                    new System.Data.SQLite.SQLiteParameter("@TP", perUnitTP),
                    new System.Data.SQLite.SQLiteParameter("@BoxSize", item.PackSize),
                    new System.Data.SQLite.SQLiteParameter("@MID", item.MedicineID)
                };
                commands.Add(new Tuple<string, System.Data.SQLite.SQLiteParameter[]>(medUpdateSql, medParams));
            }

            try
            {
                bool success = _db.ExecuteTransaction(commands);
                if (success)
                {
                    // Refresh the global medicine cache so every screen
                    // (Dashboard low-stock, Medicine list, next Sale tab)
                    // sees the updated stock quantities and prices.
                    AppCache.RefreshMedicines();

                    object purchIdObj = _db.ExecuteScalar("SELECT MAX(PurchaseID) FROM Purchases");
                    int savedPurchId = purchIdObj != null && purchIdObj != DBNull.Value ? Convert.ToInt32(purchIdObj) : -1;

                    if (savedPurchId > 0)
                    {
                        string suppName = SelectedSupplier != null ? SelectedSupplier.Name : "Unknown";
                        var cloudItems = new System.Collections.Generic.List<System.Collections.Generic.Dictionary<string, object>>();
                        foreach (var item in PurchaseItems)
                        {
                            cloudItems.Add(new System.Collections.Generic.Dictionary<string, object>
                            {
                                { "MedicineID",    item.MedicineID },
                                { "MedicineName",  item.MedicineName },
                                { "BatchNo",       item.BatchNo },
                                { "ExpiryDate",    item.ExpiryDate },
                                { "Quantity",      item.Quantity },
                                { "PurchasePrice", item.TP },
                                { "TotalPrice",    item.TotalPrice }
                            });
                        }

                        PharmaBilling.Source.Data.CloudSyncService.UploadPurchaseAsync(
                            savedPurchId, suppName, InvoiceNo ?? "",
                            PurchaseDate.ToString("yyyy-MM-dd HH:mm:ss"),
                            GrossTotal, "Received", cloudItems);
                    }
                }
                return success;
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(ex.ToString());
                return false;
            }
        }
    }

    public class PurchaseEntry
    {
        public int PurchaseID { get; set; }
        public string SupplierName { get; set; }
        public string InvoiceNo { get; set; }
        public string PurchaseDate { get; set; }
        public string TotalAmount { get; set; }
        public string Status { get; set; }
        public string PurchaseType { get; set; }
        public string Items { get; set; }
    }
}
