using System;
using System.Collections.ObjectModel;
using System.Data;
using System.Data.SQLite;
using System.Linq;
using PharmaBilling.Source.Data;
using PharmaBilling.Source.Models;

namespace PharmaBilling.Source.ViewModels
{
    // ── Item row in a purchase return ──────────────────────────────────────────
    public class ReturnItemViewModel : BaseViewModel
    {
        private int _medicineID;
        public int MedicineID
        {
            get { return _medicineID; }
            set { _medicineID = value; OnPropertyChanged("MedicineID"); }
        }

        private string _medicineName;
        public string MedicineName
        {
            get { return _medicineName; }
            set { _medicineName = value; OnPropertyChanged("MedicineName"); }
        }

        private string _batchNo;
        public string BatchNo
        {
            get { return _batchNo; }
            set { _batchNo = value; OnPropertyChanged("BatchNo"); }
        }

        private double _maxQty;
        public double MaxQty
        {
            get { return _maxQty; }
            set { _maxQty = value; OnPropertyChanged("MaxQty"); }
        }

        private double _returnQty;
        public double ReturnQty
        {
            get { return _returnQty; }
            set
            {
                _returnQty = value < 0 ? 0 : (value > _maxQty ? _maxQty : value);
                OnPropertyChanged("ReturnQty");
                OnPropertyChanged("TotalPrice");
            }
        }

        private double _purchasePrice;
        public double PurchasePrice
        {
            get { return _purchasePrice; }
            set { _purchasePrice = value; OnPropertyChanged("PurchasePrice"); OnPropertyChanged("TotalPrice"); }
        }

        public double TotalPrice { get { return ReturnQty * PurchasePrice; } }
    }

    // ── Past purchase shown in search/combo ──────────────────────────────────
    public class PurchaseListItem
    {
        public int PurchaseID { get; set; }
        public string Display { get; set; }   // "INV-001 | Supplier | 01-Apr-2026 | Rs.5,000"
        public int SupplierID { get; set; }
        public string SupplierName { get; set; }
    }

    // ────────────────────────────────────────────────────────────────────────
    // PURCHASE RETURN VIEW-MODEL
    // ────────────────────────────────────────────────────────────────────────
    public class PurchaseReturnViewModel : BaseViewModel
    {
        private readonly DbHelper _db;

        public ObservableCollection<PurchaseListItem> AllPurchases { get; set; }
        public ObservableCollection<ReturnItemViewModel> ReturnItems { get; set; }

        private PurchaseListItem _selectedPurchase;
        public PurchaseListItem SelectedPurchase
        {
            get { return _selectedPurchase; }
            set
            {
                _selectedPurchase = value;
                OnPropertyChanged("SelectedPurchase");
                if (value != null) LoadPurchaseDetails(value.PurchaseID);
            }
        }

        private string _reason;
        public string Reason
        {
            get { return _reason; }
            set { _reason = value; OnPropertyChanged("Reason"); }
        }

        private double _totalAmount;
        public double TotalAmount
        {
            get { return _totalAmount; }
            set { _totalAmount = value; OnPropertyChanged("TotalAmount"); }
        }

        public PurchaseReturnViewModel()
        {
            _db = new DbHelper();
            AllPurchases = new ObservableCollection<PurchaseListItem>();
            ReturnItems = new ObservableCollection<ReturnItemViewModel>();
            ReturnItems.CollectionChanged += (s, e) =>
            {
                if (e.NewItems != null)
                    foreach (ReturnItemViewModel i in e.NewItems)
                        i.PropertyChanged += (ss, ee) => { if (ee.PropertyName == "ReturnQty" || ee.PropertyName == "PurchasePrice") CalculateTotal(); };
                CalculateTotal();
            };
            LoadAllPurchases();
        }

        public void LoadAllPurchases()
        {
            AllPurchases.Clear();
            string sql = @"SELECT p.PurchaseID, p.InvoiceNo, p.PurchaseDate, p.TotalAmount,
                                  s.Name as SupplierName, IFNULL(s.SupplierID,0) as SupplierID
                           FROM Purchases p
                           LEFT JOIN Suppliers s ON p.SupplierID = s.SupplierID
                           ORDER BY p.PurchaseID DESC";
            DataTable dt = _db.GetDataTable(sql);
            foreach (DataRow row in dt.Rows)
            {
                AllPurchases.Add(new PurchaseListItem
                {
                    PurchaseID = Convert.ToInt32(row["PurchaseID"]),
                    SupplierID = Convert.ToInt32(row["SupplierID"]),
                    SupplierName = row["SupplierName"] != DBNull.Value ? row["SupplierName"].ToString() : "N/A",
                    Display = string.Format("#{0} | {1} | {2} | Rs.{3:N0}",
                        row["PurchaseID"],
                        row["SupplierName"] != DBNull.Value ? row["SupplierName"].ToString() : "N/A",
                        row["PurchaseDate"].ToString().Substring(0, 10),
                        Convert.ToDouble(row["TotalAmount"]))
                });
            }
        }

        private void LoadPurchaseDetails(int purchaseId)
        {
            ReturnItems.Clear();
            string sql = @"SELECT pd.MedicineID, m.Name, pd.BatchNo, pd.Quantity, pd.PurchasePrice
                           FROM PurchaseDetails pd
                           JOIN Medicines m ON pd.MedicineID = m.MedicineID
                           WHERE pd.PurchaseID = @pid";
            DataTable dt = _db.GetDataTable(sql,
                new SQLiteParameter[] { new SQLiteParameter("@pid", purchaseId) });
            foreach (DataRow row in dt.Rows)
            {
                var item = new ReturnItemViewModel
                {
                    MedicineID = Convert.ToInt32(row["MedicineID"]),
                    MedicineName = row["Name"].ToString(),
                    BatchNo = row["BatchNo"].ToString(),
                    MaxQty = Convert.ToDouble(row["Quantity"]),
                    ReturnQty = 0,
                    PurchasePrice = Convert.ToDouble(row["PurchasePrice"])
                };
                ReturnItems.Add(item);
            }
        }

        public void CalculateTotal()
        {
            TotalAmount = ReturnItems.Sum(i => i.TotalPrice);
        }

        public bool SaveReturn()
        {
            if (SelectedPurchase == null) return false;
            var itemsToReturn = ReturnItems.Where(i => i.ReturnQty > 0).ToList();
            if (itemsToReturn.Count == 0) return false;

            var commands = new System.Collections.Generic.List<Tuple<string, SQLiteParameter[]>>();
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

            // 1. Insert header
            string hdr = @"INSERT INTO PurchaseReturns (PurchaseID, SupplierID, ReturnDate, TotalAmount, Reason, Status)
                           VALUES (@pid, @sid, @date, @total, @reason, 'Returned');";
            commands.Add(new Tuple<string, SQLiteParameter[]>(hdr, new SQLiteParameter[]
            {
                new SQLiteParameter("@pid", SelectedPurchase.PurchaseID),
                new SQLiteParameter("@sid", SelectedPurchase.SupplierID),
                new SQLiteParameter("@date", timestamp),
                new SQLiteParameter("@total", TotalAmount),
                new SQLiteParameter("@reason", Reason ?? "")
            }));

            foreach (var item in itemsToReturn)
            {
                // 2. Insert detail
                string det = @"INSERT INTO PurchaseReturnDetails (ReturnID, MedicineID, BatchNo, Quantity, PurchasePrice, TotalPrice)
                               VALUES ((SELECT MAX(ReturnID) FROM PurchaseReturns), @mid, @batch, @qty, @price, @total)";
                commands.Add(new Tuple<string, SQLiteParameter[]>(det, new SQLiteParameter[]
                {
                    new SQLiteParameter("@mid", item.MedicineID),
                    new SQLiteParameter("@batch", item.BatchNo),
                    new SQLiteParameter("@qty", item.ReturnQty),
                    new SQLiteParameter("@price", item.PurchasePrice),
                    new SQLiteParameter("@total", item.TotalPrice)
                }));

                // 3. Deduct stock (return means stock goes down)
                string stockSql = @"UPDATE Stocks SET Quantity = Quantity - @qty
                                    WHERE StockID = (
                                        SELECT StockID FROM Stocks 
                                        WHERE MedicineID = @mid AND BatchNo = @batch AND Quantity >= @qty 
                                        ORDER BY StockID ASC LIMIT 1
                                    )";
                commands.Add(new Tuple<string, SQLiteParameter[]>(stockSql, new SQLiteParameter[]
                {
                    new SQLiteParameter("@qty", item.ReturnQty),
                    new SQLiteParameter("@mid", item.MedicineID),
                    new SQLiteParameter("@batch", item.BatchNo)
                }));
            }

            // 4. Ledger: Debit Purchase Returns, Credit Cash
            string desc = string.Format("Purchase Return — {0}", SelectedPurchase.SupplierName);

            string ledgerSql = @"
                INSERT INTO LedgerTransactions (LedgerID, Description, Debit, Credit, Balance)
                SELECT LedgerID, @d, 0, @a, Balance - @a FROM Ledgers WHERE Name='Cash Account';
                
                UPDATE Ledgers SET Balance = Balance - @a WHERE Name='Cash Account';
                
                INSERT INTO LedgerTransactions (LedgerID, Description, Debit, Credit, Balance)
                SELECT LedgerID, @d, @a, 0, Balance + @a FROM Ledgers WHERE Name='Purchase Returns';
                
                UPDATE Ledgers SET Balance = Balance + @a WHERE Name='Purchase Returns';
            ";

            commands.Add(new Tuple<string, SQLiteParameter[]>(ledgerSql, new SQLiteParameter[]
            {
                new SQLiteParameter("@d", desc),
                new SQLiteParameter("@a", TotalAmount)
            }));

            try
            {
                return _db.ExecuteTransaction(commands);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(ex.ToString());
                return false;
            }
        }
    }

    // ────────────────────────────────────────────────────────────────────────
    // PAST PURCHASE RETURNS LIST (for display in PurchaseUC tab)
    // ────────────────────────────────────────────────────────────────────────
    public class PurchaseReturnEntry
    {
        public int ReturnID { get; set; }
        public string SupplierName { get; set; }
        public string ReturnDate { get; set; }
        public string TotalAmount { get; set; }
        public string Reason { get; set; }
        public string Status { get; set; }
    }
}
