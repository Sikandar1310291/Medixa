using System;
using System.Collections.ObjectModel;
using System.Data;
using System.Data.SQLite;
using PharmaBilling.Source.Data;
using PharmaBilling.Source.Models;

namespace PharmaBilling.Source.ViewModels
{
    public class StockViewModel : BaseViewModel
    {
        private DbHelper _db;
        public ObservableCollection<Stock> Stocks { get; set; }

        public StockViewModel()
        {
            _db = new DbHelper();
            Stocks = new ObservableCollection<Stock>();
            LoadStocks();
        }

        public void LoadStocks()
        {
            Stocks.Clear();
            string sql = @"SELECT s.*, m.Name as MedicineName FROM Stocks s 
                           JOIN Medicines m ON s.MedicineID = m.MedicineID";
            DataTable dt = _db.GetDataTable(sql);
            foreach (DataRow row in dt.Rows)
            {
                Stocks.Add(new Stock
                {
                    StockID = Convert.ToInt32(row["StockID"]),
                    MedicineID = Convert.ToInt32(row["MedicineID"]),
                    MedicineName = row["MedicineName"].ToString(),
                    BatchNo = row["BatchNo"].ToString(),
                    ExpiryDate = row["ExpiryDate"].ToString(),
                    Quantity = Convert.ToDouble(row["Quantity"]),
                    SupplierID = row["SupplierID"] != DBNull.Value ? Convert.ToInt32(row["SupplierID"]) : 0,
                    DateAdded = row["DateAdded"].ToString()
                });
            }
        }

        public void UpdateStock(int medicineID, string batchNo, int changeQty)
        {
            // Logic to update stock after sale or purchase
            string checkSql = "SELECT StockID, Quantity FROM Stocks WHERE MedicineID = @mid AND BatchNo = @batch";
            SQLiteParameter[] p = { 
                new SQLiteParameter("@mid", medicineID),
                new SQLiteParameter("@batch", batchNo) 
            };
            
            DataTable dt = _db.GetDataTable(checkSql, p);
            if (dt.Rows.Count > 0)
            {
                double currentQty = Convert.ToDouble(dt.Rows[0]["Quantity"]);
                double newQty = currentQty + changeQty;
                _db.ExecuteNonQuery("UPDATE Stocks SET Quantity = @qty WHERE StockID = @sid", 
                    new SQLiteParameter[] { 
                        new SQLiteParameter("@qty", newQty),
                        new SQLiteParameter("@sid", dt.Rows[0]["StockID"]) 
                    });
            }
            LoadStocks();
        }
    }
}
