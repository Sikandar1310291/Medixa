using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Threading.Tasks;
using System.Windows;
using PharmaBilling.Source.Data;

namespace PharmaBilling.Source.ViewModels
{
    public class AnalysisViewModel : BaseViewModel
    {
        private DbHelper _db;

        private ObservableCollection<AnalysisItem> _topMedicines;
        public ObservableCollection<AnalysisItem> TopMedicines
        {
            get { return _topMedicines; }
            set { _topMedicines = value; OnPropertyChanged("TopMedicines"); }
        }

        private string _currentPeriod = "Today";
        public string CurrentPeriod
        {
            get { return _currentPeriod; }
            set { _currentPeriod = value; OnPropertyChanged("CurrentPeriod"); }
        }

        private bool _isLoading = false;
        public bool IsLoading
        {
            get { return _isLoading; }
            set { _isLoading = value; OnPropertyChanged("IsLoading"); }
        }

        public AnalysisViewModel()
        {
            _db = new DbHelper();
            TopMedicines = new ObservableCollection<AnalysisItem>();
            LoadTopMedicines("Today");
        }

        public void LoadTopMedicines(string period)
        {
            CurrentPeriod = period;
            IsLoading = true;

            Task.Run(() =>
            {
                string whereClause = "";
                if (period == "Today")
                    whereClause = "WHERE date(s.SaleDate) = date('now', 'localtime')";
                else if (period == "Weekly")
                    whereClause = "WHERE date(s.SaleDate) >= date('now', '-7 days')";
                else if (period == "Monthly")
                    whereClause = "WHERE date(s.SaleDate) >= date('now', 'start of month')";

                string sql = string.Format(@"
                    SELECT m.Name, SUM(sd.Quantity) as TotalSold, SUM(sd.TotalPrice) as TotalRevenue
                    FROM SaleDetails sd
                    JOIN Sales s ON sd.SaleID = s.SaleID
                    JOIN Medicines m ON sd.MedicineID = m.MedicineID
                    {0}
                    GROUP BY m.MedicineID
                    ORDER BY TotalSold DESC
                    LIMIT 10", whereClause);

                var temp = new List<AnalysisItem>();
                string[] colors = { "#E74C3C", "#3498DB", "#2ECC71", "#9B59B6", "#F39C12", "#1ABC9C", "#D35400", "#34495E", "#7F8C8D", "#27AE60" };

                try
                {
                    DataTable dt = _db.GetDataTable(sql);
                    int maxSold = dt.Rows.Count > 0 ? Convert.ToInt32(dt.Rows[0]["TotalSold"]) : 0;

                    int index = 0;
                    foreach (DataRow row in dt.Rows)
                    {
                        int sold = Convert.ToInt32(row["TotalSold"]);
                        double width = maxSold > 0 ? ((double)sold / maxSold) * 100 : 0;
                        temp.Add(new AnalysisItem
                        {
                            Rank = index + 1,
                            MedicineName = row["Name"].ToString(),
                            TotalSold = sold,
                            Revenue = "Rs. " + Convert.ToDouble(row["TotalRevenue"]).ToString("N0"),
                            BarWidth = width + "*",
                            RemainingWidth = (100 - width) + "*",
                            Color = colors[index % colors.Length]
                        });
                        index++;
                    }

                    if (dt.Rows.Count == 0)
                    {
                        temp.Add(new AnalysisItem
                        {
                            MedicineName = "No Sales Found for " + period,
                            TotalSold = 0,
                            BarWidth = "0*",
                            RemainingWidth = "100*",
                            Color = "#BDC3C7"
                        });
                    }
                }
                catch
                {
                    temp.Add(new AnalysisItem { MedicineName = "Error Loading Data", BarWidth = "0*", RemainingWidth = "100*", Color = "#E74C3C" });
                }

                Application.Current.Dispatcher.Invoke(() =>
                {
                    TopMedicines = new ObservableCollection<AnalysisItem>(temp);
                    IsLoading = false;
                });
            });
        }
    }

    public class AnalysisItem
    {
        public int Rank { get; set; }
        public string MedicineName { get; set; }
        public int TotalSold { get; set; }
        public string Revenue { get; set; }
        public string BarWidth { get; set; }
        public string RemainingWidth { get; set; }
        public string Color { get; set; }
    }
}
