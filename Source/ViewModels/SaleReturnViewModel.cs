using System;
using System.Collections.ObjectModel;
using System.Data;
using System.Data.SQLite;
using System.Linq;
using PharmaBilling.Source.Data;
using PharmaBilling.Source.Models;

namespace PharmaBilling.Source.ViewModels
{
    // ── Row item for a sale return ──────────────────────────────────────────
    public class SaleReturnItemViewModel : BaseViewModel
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

        private double _soldQty;
        public double SoldQty
        {
            get { return _soldQty; }
            set { _soldQty = value; OnPropertyChanged("SoldQty"); }
        }

        private double _returnQty;
        public double ReturnQty
        {
            get { return _returnQty; }
            set
            {
                _returnQty = value < 0 ? 0 : (value > _soldQty ? _soldQty : value);
                OnPropertyChanged("ReturnQty");
                OnPropertyChanged("TotalPrice");
            }
        }

        private double _unitPrice;
        public double UnitPrice
        {
            get { return _unitPrice; }
            set { _unitPrice = value; OnPropertyChanged("UnitPrice"); OnPropertyChanged("TotalPrice"); }
        }

        public double TotalPrice { get { return ReturnQty * UnitPrice; } }
    }

    // ── Past sale entry for the selection combo ──────────────────────────────
    public class SaleListItem
    {
        public int SaleID { get; set; }
        public int CustomerID { get; set; }
        public string CustomerName { get; set; }
        public string Display { get; set; }  
    }

    // ────────────────────────────────────────────────────────────────────────
    // SALE RETURN VIEW-MODEL
    // ────────────────────────────────────────────────────────────────────────
    public class SaleReturnViewModel : BaseViewModel
    {
        private readonly DbHelper _db;

        public ObservableCollection<SaleListItem> AllSales { get; set; }
        public ObservableCollection<SaleReturnItemViewModel> ReturnItems { get; set; }

        private SaleListItem _selectedSale;
        public SaleListItem SelectedSale
        {
            get { return _selectedSale; }
            set
            {
                _selectedSale = value;
                OnPropertyChanged("SelectedSale");
                if (value != null) LoadSaleDetails(value.SaleID);
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

        public SaleReturnViewModel()
        {
            _db = new DbHelper();
            AllSales = new ObservableCollection<SaleListItem>();
            ReturnItems = new ObservableCollection<SaleReturnItemViewModel>();
            ReturnItems.CollectionChanged += (s, e) =>
            {
                if (e.NewItems != null)
                    foreach (SaleReturnItemViewModel i in e.NewItems)
                        i.PropertyChanged += (ss, ee) => { if (ee.PropertyName == "ReturnQty" || ee.PropertyName == "UnitPrice") CalculateTotal(); };
                CalculateTotal();
            };
            LoadAllSales();
        }

        public void LoadAllSales()
        {
            AllSales.Clear();
            string sql = @"SELECT s.SaleID, s.SaleDate, s.NetPaid, 
                                  c.Name as CustomerName, IFNULL(c.CustomerID,0) as CustomerID
                           FROM Sales s
                           LEFT JOIN Customers c ON s.CustomerID = c.CustomerID
                           ORDER BY s.SaleID DESC";
            DataTable dt = _db.GetDataTable(sql);
            foreach (DataRow row in dt.Rows)
            {
                AllSales.Add(new SaleListItem
                {
                    SaleID = Convert.ToInt32(row["SaleID"]),
                    CustomerID = Convert.ToInt32(row["CustomerID"]),
                    CustomerName = row["CustomerName"] != DBNull.Value ? row["CustomerName"].ToString() : "Walk-in",
                    Display = string.Format("Sale #{0} | {1} | {2} | Rs.{3:N0}",
                        row["SaleID"],
                        row["CustomerName"] != DBNull.Value ? row["CustomerName"].ToString() : "Walk-in",
                        row["SaleDate"].ToString().Substring(0, 10),
                        Convert.ToDouble(row["NetPaid"]))
                });
            }
        }

        private void LoadSaleDetails(int saleId)
        {
            ReturnItems.Clear();
            string sql = @"SELECT sd.MedicineID, m.Name, sd.BatchNo, sd.Quantity, sd.UnitPrice
                           FROM SaleDetails sd
                           JOIN Medicines m ON sd.MedicineID = m.MedicineID
                           WHERE sd.SaleID = @sid";
            DataTable dt = _db.GetDataTable(sql,
                new SQLiteParameter[] { new SQLiteParameter("@sid", saleId) });
            foreach (DataRow row in dt.Rows)
            {
                var item = new SaleReturnItemViewModel
                {
                    MedicineID = Convert.ToInt32(row["MedicineID"]),
                    MedicineName = row["Name"].ToString(),
                    BatchNo = row["BatchNo"].ToString(),
                    SoldQty = Convert.ToDouble(row["Quantity"]),
                    ReturnQty = 0,
                    UnitPrice = Convert.ToDouble(row["UnitPrice"])
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
            if (SelectedSale == null) return false;
            var itemsToReturn = ReturnItems.Where(i => i.ReturnQty > 0).ToList();
            if (itemsToReturn.Count == 0) return false;

            using (var conn = _db.GetConnection())
            {
                conn.Open();
                using (var dbTx = conn.BeginTransaction())
                {
                    try
                    {
                        // 1. Insert header
                        string hdr = @"INSERT INTO SaleReturns (SaleID, CustomerID, ReturnDate, TotalAmount, Reason, Status)
                                       VALUES (@sid, @cid, @date, @total, @reason, 'Returned');
                                       SELECT last_insert_rowid();";
                        int returnId;
                        using (var cmd = new SQLiteCommand(hdr, conn, dbTx))
                        {
                            cmd.Parameters.AddWithValue("@sid", SelectedSale.SaleID);
                            cmd.Parameters.AddWithValue("@cid", SelectedSale.CustomerID);
                            cmd.Parameters.AddWithValue("@date", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                            cmd.Parameters.AddWithValue("@total", TotalAmount);
                            cmd.Parameters.AddWithValue("@reason", Reason ?? "");
                            returnId = Convert.ToInt32(cmd.ExecuteScalar());
                        }

                        foreach (var item in itemsToReturn)
                        {
                            // 2. Insert return detail
                            string det = @"INSERT INTO SaleReturnDetails (ReturnID, MedicineID, BatchNo, Quantity, UnitPrice, TotalPrice)
                                           VALUES (@rid, @mid, @batch, @qty, @price, @total)";
                            using (var cmd = new SQLiteCommand(det, conn, dbTx))
                            {
                                cmd.Parameters.AddWithValue("@rid", returnId);
                                cmd.Parameters.AddWithValue("@mid", item.MedicineID);
                                cmd.Parameters.AddWithValue("@batch", item.BatchNo);
                                cmd.Parameters.AddWithValue("@qty", item.ReturnQty);
                                cmd.Parameters.AddWithValue("@price", item.UnitPrice);
                                cmd.Parameters.AddWithValue("@total", item.TotalPrice);
                                cmd.ExecuteNonQuery();
                            }

                            // 2.1 Deduct from SaleDetails
                            string updDet = "UPDATE SaleDetails SET Quantity = Quantity - @qty, TotalPrice = TotalPrice - @total WHERE SaleID = @sid AND MedicineID = @mid AND BatchNo = @batch";
                            using (var cmd = new SQLiteCommand(updDet, conn, dbTx))
                            {
                                cmd.Parameters.AddWithValue("@qty", item.ReturnQty);
                                cmd.Parameters.AddWithValue("@total", item.TotalPrice);
                                cmd.Parameters.AddWithValue("@sid", SelectedSale.SaleID);
                                cmd.Parameters.AddWithValue("@mid", item.MedicineID);
                                cmd.Parameters.AddWithValue("@batch", item.BatchNo);
                                cmd.ExecuteNonQuery();
                            }

                            // 3. Add stock back (FIFO – credit oldest batch or create a new batch entry)
                            string checkSql = "SELECT StockID FROM Stocks WHERE MedicineID = @mid AND BatchNo = @batch LIMIT 1";
                            object stockIdObj;
                            using (var cmd = new SQLiteCommand(checkSql, conn, dbTx))
                            {
                                cmd.Parameters.AddWithValue("@mid", item.MedicineID);
                                cmd.Parameters.AddWithValue("@batch", item.BatchNo);
                                stockIdObj = cmd.ExecuteScalar();
                            }

                            if (stockIdObj != null && stockIdObj != DBNull.Value)
                            {
                                string upd = "UPDATE Stocks SET Quantity = Quantity + @qty WHERE StockID = @sid";
                                using (var cmd = new SQLiteCommand(upd, conn, dbTx))
                                {
                                    cmd.Parameters.AddWithValue("@qty", item.ReturnQty);
                                    cmd.Parameters.AddWithValue("@sid", Convert.ToInt32(stockIdObj));
                                    cmd.ExecuteNonQuery();
                                }
                            }
                            else
                            {
                                string ins = @"INSERT INTO Stocks (MedicineID, BatchNo, RackNo, Quantity, DateAdded)
                                               VALUES (@mid, @batch, 'Rack-1', @qty, @date)";
                                using (var cmd = new SQLiteCommand(ins, conn, dbTx))
                                {
                                    cmd.Parameters.AddWithValue("@mid", item.MedicineID);
                                    cmd.Parameters.AddWithValue("@batch", item.BatchNo);
                                    cmd.Parameters.AddWithValue("@qty", item.ReturnQty);
                                    cmd.Parameters.AddWithValue("@date", DateTime.Now.ToString("yyyy-MM-dd"));
                                    cmd.ExecuteNonQuery();
                                }
                            }
                        }

                        // 3.1 Deduct from original Sale record
                        string deductSaleSql = "UPDATE Sales SET TotalAmount = TotalAmount - @total, NetPaid = NetPaid - @total WHERE SaleID = @sid";
                        using (var cmd = new SQLiteCommand(deductSaleSql, conn, dbTx))
                        {
                            cmd.Parameters.AddWithValue("@total", TotalAmount);
                            cmd.Parameters.AddWithValue("@sid", SelectedSale.SaleID);
                            cmd.ExecuteNonQuery();
                        }


                        // 4. Ledger: Debit Sale Returns, Credit Cash (refund)
                        string desc = string.Format("Sale Return #{0} — {1}", returnId, SelectedSale.CustomerName);
                        double cashBal = 0, retBal = 0;
                        using (var cmd = new SQLiteCommand("SELECT Balance FROM Ledgers WHERE Name='Cash Account'", conn, dbTx))
                        { object r = cmd.ExecuteScalar(); if (r != null && r != DBNull.Value) cashBal = Convert.ToDouble(r); }
                        using (var cmd = new SQLiteCommand("SELECT Balance FROM Ledgers WHERE Name='Sale Returns'", conn, dbTx))
                        { object r = cmd.ExecuteScalar(); if (r != null && r != DBNull.Value) retBal = Convert.ToDouble(r); }

                        using (var cmd = new SQLiteCommand(@"INSERT INTO LedgerTransactions (LedgerID, Description, Debit, Credit, Balance)
                            SELECT LedgerID, @d, 0, @a, @b FROM Ledgers WHERE Name='Cash Account'", conn, dbTx))
                        { cmd.Parameters.AddWithValue("@d", desc); cmd.Parameters.AddWithValue("@a", TotalAmount); cmd.Parameters.AddWithValue("@b", cashBal - TotalAmount); cmd.ExecuteNonQuery(); }
                        using (var cmd = new SQLiteCommand("UPDATE Ledgers SET Balance=Balance-@a WHERE Name='Cash Account'", conn, dbTx))
                        { cmd.Parameters.AddWithValue("@a", TotalAmount); cmd.ExecuteNonQuery(); }

                        using (var cmd = new SQLiteCommand(@"INSERT INTO LedgerTransactions (LedgerID, Description, Debit, Credit, Balance)
                            SELECT LedgerID, @d, @a, 0, @b FROM Ledgers WHERE Name='Sale Returns'", conn, dbTx))
                        { cmd.Parameters.AddWithValue("@d", desc); cmd.Parameters.AddWithValue("@a", TotalAmount); cmd.Parameters.AddWithValue("@b", retBal + TotalAmount); cmd.ExecuteNonQuery(); }
                        using (var cmd = new SQLiteCommand("UPDATE Ledgers SET Balance=Balance+@a WHERE Name='Sale Returns'", conn, dbTx))
                        { cmd.Parameters.AddWithValue("@a", TotalAmount); cmd.ExecuteNonQuery(); }

                        dbTx.Commit();
                        return true;
                    }
                    catch
                    {
                        dbTx.Rollback();
                        return false;
                    }
                }
            }
        }
    }

    // ── Display entry in the sale returns list ───────────────────────────────
    public class SaleReturnEntry
    {
        public int ReturnID { get; set; }
        public string CustomerName { get; set; }
        public string ReturnDate { get; set; }
        public string TotalAmount { get; set; }
        public string Reason { get; set; }
        public string Status { get; set; }
    }
}
