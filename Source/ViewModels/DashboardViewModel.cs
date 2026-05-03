using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Threading.Tasks;
using System.Windows;
using PharmaBilling.Source.Data;

namespace PharmaBilling.Source.ViewModels
{
    public class DashboardViewModel : BaseViewModel
    {
        private DbHelper _db;

        private string _dailySales = "Rs. 0";
        public string DailySales { get { return _dailySales; } set { _dailySales = value; OnPropertyChanged("DailySales"); } }

        private string _totalMedicines = "0";
        public string TotalMedicines { get { return _totalMedicines; } set { _totalMedicines = value; OnPropertyChanged("TotalMedicines"); } }

        private string _lowStock = "0";
        public string LowStock { get { return _lowStock; } set { _lowStock = value; OnPropertyChanged("LowStock"); } }

        private string _expiredItems = "0";
        public string ExpiredItems { get { return _expiredItems; } set { _expiredItems = value; OnPropertyChanged("ExpiredItems"); } }

        private ObservableCollection<RecentSale> _recentSales;
        public ObservableCollection<RecentSale> RecentSales
        {
            get { return _recentSales; }
            set { _recentSales = value; OnPropertyChanged("RecentSales"); }
        }

        public DashboardViewModel()
        {
            _db = new DbHelper();
            RecentSales = new ObservableCollection<RecentSale>();
            LoadDashboardData();
        }

        public void LoadDashboardData()
        {
            Task.Run(() =>
            {
                try
                {
                    string today = DateTime.Now.ToString("yyyy-MM-dd");

                    // Run all scalar queries in background
                    object salesObj = _db.ExecuteScalar(string.Format("SELECT COALESCE(SUM(TotalAmount),0) FROM Sales WHERE date(SaleDate) = '{0}'", today));
                    double sales = salesObj != null && salesObj != DBNull.Value ? Convert.ToDouble(salesObj) : 0;

                    object medObj = _db.ExecuteScalar("SELECT COUNT(*) FROM Medicines");
                    string totalMed = medObj != null ? medObj.ToString() : "0";

                    // Low stock: respect each medicine's own MinStock threshold.
                    // Also counts medicines with NO stock entries (TotalStock = 0).
                    object lowObj = _db.ExecuteScalar(@"
                        SELECT COUNT(*) FROM Medicines m
                        WHERE m.Status != 'Inactive'
                        AND COALESCE(
                            (SELECT SUM(s.Quantity) FROM Stocks s WHERE s.MedicineID = m.MedicineID),
                            0
                        ) <= m.MinStock");
                    string lowStock = lowObj != null ? lowObj.ToString() : "0";

                    // Expired: only count stock batches that STILL HAVE quantity (Quantity > 0).
                    // Ignores depleted batches. Counts distinct medicines to avoid duplicates.
                    object expObj = _db.ExecuteScalar(@"
                        SELECT COUNT(DISTINCT MedicineID) FROM Stocks
                        WHERE Quantity > 0
                        AND ExpiryDate IS NOT NULL
                        AND ExpiryDate != ''
                        AND length(ExpiryDate) >= 7
                        AND date(ExpiryDate) < date('now')");
                    string expired = expObj != null && expObj != DBNull.Value ? expObj.ToString() : "0";

                    string recentSql = @"SELECT s.SaleID as InvoiceNo, 
                        COALESCE(c.Name, 'Walk-in') as Name, 
                        s.TotalAmount as Amount, s.Status
                        FROM Sales s LEFT JOIN Customers c ON s.CustomerID = c.CustomerID 
                        ORDER BY s.SaleID DESC LIMIT 10";
                    DataTable dt = _db.GetDataTable(recentSql);
                    var tempSales = new List<RecentSale>();
                    foreach (DataRow row in dt.Rows)
                    {
                        tempSales.Add(new RecentSale
                        {
                            InvoiceNo = "DOC-" + row["InvoiceNo"].ToString(),
                            Name = row["Name"].ToString(),
                            Amount = "Rs. " + Convert.ToDouble(row["Amount"]).ToString("N2"),
                            Status = row["Status"].ToString()
                        });
                    }

                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        DailySales = string.Format("Rs. {0:N0}", sales);
                        TotalMedicines = totalMed;
                        LowStock = lowStock;
                        ExpiredItems = expired;
                        RecentSales = new ObservableCollection<RecentSale>(tempSales);
                    });
                }
                catch
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        DailySales = "Rs. 0";
                        TotalMedicines = "0";
                        LowStock = "0";
                        ExpiredItems = "0";
                    });
                }
            });
        }
    }

    public class RecentSale
    {
        public string InvoiceNo { get; set; }
        public string Name { get; set; }
        public string Amount { get; set; }
        public string Status { get; set; }
        public string Items { get; set; }
    }
}
