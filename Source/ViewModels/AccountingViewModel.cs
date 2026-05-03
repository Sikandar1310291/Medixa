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
    public class AccountingViewModel : BaseViewModel
    {
        private DbHelper _db;

        private ObservableCollection<Ledger> _ledgers;
        public ObservableCollection<Ledger> Ledgers
        {
            get { return _ledgers; }
            set { _ledgers = value; OnPropertyChanged("Ledgers"); }
        }

        private ObservableCollection<LedgerTransaction> _transactions;
        public ObservableCollection<LedgerTransaction> Transactions
        {
            get { return _transactions; }
            set { _transactions = value; OnPropertyChanged("Transactions"); }
        }

        private Ledger _selectedLedger;
        public Ledger SelectedLedger
        {
            get { return _selectedLedger; }
            set { _selectedLedger = value; OnPropertyChanged("SelectedLedger"); }
        }

        private string _todaySales = "Rs. 0";
        public string TodaySales { get { return _todaySales; } set { _todaySales = value; OnPropertyChanged("TodaySales"); } }
        private string _todayPurchases = "Rs. 0";
        public string TodayPurchases { get { return _todayPurchases; } set { _todayPurchases = value; OnPropertyChanged("TodayPurchases"); } }
        private string _weekSales = "Rs. 0";
        public string WeekSales { get { return _weekSales; } set { _weekSales = value; OnPropertyChanged("WeekSales"); } }
        private string _monthSales = "Rs. 0";
        public string MonthSales { get { return _monthSales; } set { _monthSales = value; OnPropertyChanged("MonthSales"); } }
        private string _yearSales = "Rs. 0";
        public string YearSales { get { return _yearSales; } set { _yearSales = value; OnPropertyChanged("YearSales"); } }

        private string _weekPurchases = "Rs. 0";
        public string WeekPurchases { get { return _weekPurchases; } set { _weekPurchases = value; OnPropertyChanged("WeekPurchases"); } }
        private string _monthPurchases = "Rs. 0";
        public string MonthPurchases { get { return _monthPurchases; } set { _monthPurchases = value; OnPropertyChanged("MonthPurchases"); } }
        private string _yearPurchases = "Rs. 0";
        public string YearPurchases { get { return _yearPurchases; } set { _yearPurchases = value; OnPropertyChanged("YearPurchases"); } }
        public AccountingViewModel()
        {
            _db = new DbHelper();
            Ledgers = new ObservableCollection<Ledger>();
            Transactions = new ObservableCollection<LedgerTransaction>();
            LoadLedgers();
            LoadStatistics();
        }

        public void LoadLedgers()
        {
            Task.Run(() =>
            {
                var temp = new List<Ledger>();
                DataTable dt = _db.GetDataTable("SELECT * FROM Ledgers");
                foreach (DataRow row in dt.Rows)
                {
                    temp.Add(new Ledger
                    {
                        LedgerID = Convert.ToInt32(row["LedgerID"]),
                        Name = row["Name"].ToString(),
                        Type = row["Type"].ToString(),
                        Balance = Convert.ToDouble(row["Balance"])
                    });
                }
                Application.Current.Dispatcher.Invoke(() =>
                {
                    Ledgers = new ObservableCollection<Ledger>(temp);
                });
            });
        }

        public void LoadStatistics()
        {
            Task.Run(() =>
            {
                try
                {
                    // ── Sales Revenue (what customer paid) ─────────────────────
                    double todaySales   = GetMetric("SELECT COALESCE(SUM(TotalAmount),0) FROM Sales WHERE date(SaleDate) = date('now')");
                    double weekSales    = GetMetric("SELECT COALESCE(SUM(TotalAmount),0) FROM Sales WHERE date(SaleDate) >= date('now','-7 days')");
                    double monthSales   = GetMetric("SELECT COALESCE(SUM(TotalAmount),0) FROM Sales WHERE date(SaleDate) >= date('now','-30 days')");
                    double yearSales    = GetMetric("SELECT COALESCE(SUM(TotalAmount),0) FROM Sales WHERE date(SaleDate) >= date('now','-365 days')");

                    // ── Total Purchase Spend (cash outflow) ────────────────────
                    double todayPurchases  = GetMetric("SELECT COALESCE(SUM(TotalAmount),0) FROM Purchases WHERE date(PurchaseDate) = date('now')");
                    double weekPurchases   = GetMetric("SELECT COALESCE(SUM(TotalAmount),0) FROM Purchases WHERE date(PurchaseDate) >= date('now','-7 days')");
                    double monthPurchases  = GetMetric("SELECT COALESCE(SUM(TotalAmount),0) FROM Purchases WHERE date(PurchaseDate) >= date('now','-30 days')");
                    double yearPurchases   = GetMetric("SELECT COALESCE(SUM(TotalAmount),0) FROM Purchases WHERE date(PurchaseDate) >= date('now','-365 days')");

                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        TodaySales     = "Rs. " + todaySales.ToString("N2");
                        TodayPurchases = "Rs. " + todayPurchases.ToString("N2");

                        WeekSales      = "Rs. " + weekSales.ToString("N2");
                        WeekPurchases  = "Rs. " + weekPurchases.ToString("N2");

                        MonthSales     = "Rs. " + monthSales.ToString("N2");
                        MonthPurchases = "Rs. " + monthPurchases.ToString("N2");

                        YearSales      = "Rs. " + yearSales.ToString("N2");
                        YearPurchases  = "Rs. " + yearPurchases.ToString("N2");
                    });
                }
                catch { }
            });
        }


        private double GetMetric(string sql)
        {
            try
            {
                object res = _db.ExecuteScalar(sql);
                if (res != null && res != DBNull.Value) return Convert.ToDouble(res);
            }
            catch { }
            return 0;
        }

        public void AddTransaction(int ledgerID, string desc, double debit, double credit)
        {
            double currentBalance = Convert.ToDouble(_db.ExecuteScalar("SELECT Balance FROM Ledgers WHERE LedgerID = @id",
                new SQLiteParameter[] { new SQLiteParameter("@id", ledgerID) }));
            double newBalance = currentBalance + (debit - credit);

            string sqlTrans = @"INSERT INTO LedgerTransactions (LedgerID, Description, Debit, Credit, Balance) 
                                VALUES (@lid, @desc, @deb, @cre, @bal)";
            _db.ExecuteNonQuery(sqlTrans, new SQLiteParameter[] {
                new SQLiteParameter("@lid", ledgerID),
                new SQLiteParameter("@desc", desc),
                new SQLiteParameter("@deb", debit),
                new SQLiteParameter("@cre", credit),
                new SQLiteParameter("@bal", newBalance)
            });

            _db.ExecuteNonQuery("UPDATE Ledgers SET Balance = @bal WHERE LedgerID = @id",
                new SQLiteParameter[] {
                    new SQLiteParameter("@bal", newBalance),
                    new SQLiteParameter("@id", ledgerID)
                });

            LoadLedgers();
        }
    }
}
