using System.Windows;
using System.Windows.Controls;
using System.Collections.ObjectModel;
using System.Data;
using System;
using PharmaBilling.Source.ViewModels;
using PharmaBilling.Source.Data;

namespace PharmaBilling.Source.Views
{
    public partial class PurchaseUC : UserControl
    {
        private PurchaseViewModel _vm;
        private string _activeTab = "All";

        public PurchaseUC()
        {
            InitializeComponent();
            _vm = new PurchaseViewModel();
            this.DataContext = _vm;
            LoadPurchaseReturns();
        }

        // ── Tab buttons ──────────────────────────────────────────────────────

        private void TabAll_Click(object sender, RoutedEventArgs e)
        {
            _activeTab = "All";
            ShowPurchasesPanel();
            _vm.LoadPastPurchases();
            SetTabActive(btnTabAll);
        }

        private void TabNormal_Click(object sender, RoutedEventArgs e)
        {
            _activeTab = "Normal";
            ShowPurchasesPanel();
            _vm.LoadPastPurchasesByType("Normal");
            SetTabActive(btnTabNormal);
        }

        private void TabLoose_Click(object sender, RoutedEventArgs e)
        {
            _activeTab = "Loose";
            ShowPurchasesPanel();
            _vm.LoadPastPurchasesByType("Loose");
            SetTabActive(btnTabLoose);
        }

        private void TabOpening_Click(object sender, RoutedEventArgs e)
        {
            _activeTab = "Opening";
            ShowPurchasesPanel();
            _vm.LoadPastPurchasesByType("Opening");
            SetTabActive(btnTabOpening);
        }

        private void TabReturns_Click(object sender, RoutedEventArgs e)
        {
            _activeTab = "Returns";
            pnlPurchases.Visibility = Visibility.Collapsed;
            pnlReturns.Visibility = Visibility.Visible;
            LoadPurchaseReturns();
            SetTabActive(btnTabReturns);
        }

        private void ShowPurchasesPanel()
        {
            pnlPurchases.Visibility = Visibility.Visible;
            pnlReturns.Visibility = Visibility.Collapsed;
        }

        private void SetTabActive(Button active)
        {
            var tabs = new[] { btnTabAll, btnTabNormal, btnTabLoose, btnTabOpening, btnTabReturns };
            foreach (var btn in tabs)
                btn.Background = new System.Windows.Media.SolidColorBrush(
                    System.Windows.Media.Color.FromRgb(0xBD, 0xC3, 0xC7));
            active.Background = new System.Windows.Media.SolidColorBrush(
                System.Windows.Media.Color.FromRgb(0x2C, 0x3E, 0x50));
        }

        private void LoadPurchaseReturns()
        {
            try
            {
                var db = new DbHelper();
                string sql = @"SELECT pr.ReturnID, IFNULL(s.Name,'N/A') as SupplierName,
                                      pr.ReturnDate, pr.TotalAmount, pr.Reason, pr.Status
                               FROM PurchaseReturns pr
                               LEFT JOIN Suppliers s ON pr.SupplierID = s.SupplierID
                               ORDER BY pr.ReturnID DESC";
                DataTable dt = db.GetDataTable(sql);

                var list = new ObservableCollection<PurchaseReturnEntry>();
                foreach (DataRow row in dt.Rows)
                {
                    list.Add(new PurchaseReturnEntry
                    {
                        ReturnID = Convert.ToInt32(row["ReturnID"]),
                        SupplierName = row["SupplierName"].ToString(),
                        ReturnDate = row["ReturnDate"].ToString(),
                        TotalAmount = string.Format("Rs. {0:N2}", Convert.ToDouble(row["TotalAmount"])),
                        Reason = row["Reason"] != DBNull.Value ? row["Reason"].ToString() : "",
                        Status = row["Status"].ToString()
                    });
                }
                _vm.PastPurchaseReturns = list;
                // Force binding refresh
                dgReturns.ItemsSource = null;
                dgReturns.ItemsSource = _vm.PastPurchaseReturns;
            }
            catch { }
        }

        // ── Action buttons ───────────────────────────────────────────────────

        private void AddPurchase_Click(object sender, RoutedEventArgs e)
        {
            var win = new AddPurchaseWindow();
            win.Owner = Window.GetWindow(this);
            win.ShowDialog();
            RefreshCurrentTab();
        }

        private void LoosePurchase_Click(object sender, RoutedEventArgs e)
        {
            var win = new LoosePurchaseWindow();
            win.Owner = Window.GetWindow(this);
            win.ShowDialog();
            RefreshCurrentTab();
        }

        private void OpeningPurchase_Click(object sender, RoutedEventArgs e)
        {
            var win = new OpeningPurchaseWindow();
            win.Owner = Window.GetWindow(this);
            win.ShowDialog();
            RefreshCurrentTab();
        }

        private void PurchaseReturn_Click(object sender, RoutedEventArgs e)
        {
            var win = new PurchaseReturnWindow();
            win.Owner = Window.GetWindow(this);
            win.ShowDialog();
            // Switch to Returns tab after processing
            TabReturns_Click(null, null);
        }

        private void RefreshCurrentTab()
        {
            switch (_activeTab)
            {
                case "Normal":   _vm.LoadPastPurchasesByType("Normal");  break;
                case "Loose":    _vm.LoadPastPurchasesByType("Loose");   break;
                case "Opening":  _vm.LoadPastPurchasesByType("Opening"); break;
                case "Returns":  LoadPurchaseReturns();                  break;
                default:         _vm.LoadPastPurchases();                break;
            }
        }

        private void EditPurchase_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            var item = (btn != null ? btn.DataContext : null) as PharmaBilling.Source.ViewModels.PurchaseEntry;
            if (item == null) return;

            // Open a fresh AddPurchaseWindow pre-loaded with this purchase's invoice number
            // so the user can see and adjust the original entry. 
            // NOTE: Saving will create a new corrected entry (re-entry workflow).
            var win = new AddPurchaseWindow(item.PurchaseID, item.InvoiceNo, item.SupplierName);
            win.Owner = Window.GetWindow(this);
            win.ShowDialog();
            RefreshCurrentTab();
        }

        private void DeletePurchase_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            var item = (btn != null ? btn.DataContext : null) as PharmaBilling.Source.ViewModels.PurchaseEntry;
            if (item != null)
            {
                if (MessageBox.Show(string.Format("Are you sure you want to delete Invoice #{0}?\nThis action cannot be undone.", item.InvoiceNo), "Delete Purchase", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                {
                    try
                    {
                        var commands = new System.Collections.Generic.List<Tuple<string, System.Data.SQLite.SQLiteParameter[]>>();
                        
                        string revStockSql = @"INSERT INTO Stocks (MedicineID, BatchNo, RackNo, ExpiryDate, Quantity, DateAdded) 
                                               SELECT MedicineID, BatchNo, 'REVERSAL', ExpiryDate, -Quantity, datetime('now')
                                               FROM PurchaseDetails WHERE PurchaseID = @pid";
                        commands.Add(new Tuple<string, System.Data.SQLite.SQLiteParameter[]>(revStockSql, new System.Data.SQLite.SQLiteParameter[] { new System.Data.SQLite.SQLiteParameter("@pid", item.PurchaseID) }));
                        
                        string delDet = "DELETE FROM PurchaseDetails WHERE PurchaseID = @pid";
                        commands.Add(new Tuple<string, System.Data.SQLite.SQLiteParameter[]>(delDet, new System.Data.SQLite.SQLiteParameter[] { new System.Data.SQLite.SQLiteParameter("@pid", item.PurchaseID) }));
                        
                        string delPur = "DELETE FROM Purchases WHERE PurchaseID = @pid";
                        commands.Add(new Tuple<string, System.Data.SQLite.SQLiteParameter[]>(delPur, new System.Data.SQLite.SQLiteParameter[] { new System.Data.SQLite.SQLiteParameter("@pid", item.PurchaseID) }));
                        
                        var db = new DbHelper();
                        if (db.ExecuteTransaction(commands))
                        {
                            RefreshCurrentTab();
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Delete error: " + ex.Message);
                    }
                }
            }
        }

        private void DeleteReturn_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            var item = (btn != null ? btn.DataContext : null) as PharmaBilling.Source.ViewModels.PurchaseReturnEntry;
            if (item != null)
            {
                if (MessageBox.Show("Are you sure you want to delete this purchase return?\nThis action cannot be undone.", "Delete Return", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                {
                    try
                    {
                        var commands = new System.Collections.Generic.List<Tuple<string, System.Data.SQLite.SQLiteParameter[]>>();
                        
                        string delDet = "DELETE FROM PurchaseReturnDetails WHERE ReturnID = @rid";
                        commands.Add(new Tuple<string, System.Data.SQLite.SQLiteParameter[]>(delDet, new System.Data.SQLite.SQLiteParameter[] { new System.Data.SQLite.SQLiteParameter("@rid", item.ReturnID) }));
                        
                        string delRet = "DELETE FROM PurchaseReturns WHERE ReturnID = @rid";
                        commands.Add(new Tuple<string, System.Data.SQLite.SQLiteParameter[]>(delRet, new System.Data.SQLite.SQLiteParameter[] { new System.Data.SQLite.SQLiteParameter("@rid", item.ReturnID) }));
                        
                        var db = new DbHelper();
                        if (db.ExecuteTransaction(commands))
                        {
                            RefreshCurrentTab();
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Delete error: " + ex.Message);
                    }
                }
            }
        }
    }
}

