using System;
using System.Windows;
using PharmaBilling.Source.Data;

namespace PharmaBilling.Source.Views
{
    public partial class DashboardWindow : Window
    {
        private ViewModels.DashboardViewModel _viewModel;

        public DashboardWindow()
        {
            InitializeComponent();
            
            // Branding update
            lblSidebarName.Text = ApiClient.Instance.PharmacyName;

            _viewModel = new ViewModels.DashboardViewModel();
            this.DataContext = _viewModel;
            
            // Set dynamic branding and authentication label
            txtWelcomeHeader.Text = string.Format("Welcome, {0}", AppSession.CurrentUsername);

            // APPLY ROLE BASED ACCESS CONTROL (RBAC) RESTRICTIONS
            EnforceSecurityPrivileges();

            try
            {
                // Set initial content to Dashboard Overview
                _dashboardUC = new DashboardUC() { DataContext = _viewModel };
                MainContentArea.Content = _dashboardUC;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading DashboardUC:\n" + ex.Message + "\nInner: " + (ex.InnerException != null ? ex.InnerException.Message : "None"), "Crash Intercepted");
            }

            // PRE-WARM: Creates other UI tabs in background for instant navigation
            Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Background,
               new Action(PreWarmAllPages));
        }

        // Creates all UserControls silently during idle time after dashboard loads.
        // WPF must create UI on UI thread, so we use Background priority (lowest)
        // which only runs when the UI thread has nothing else to do.
        private void PreWarmAllPages()
        {
            try
            {
                if (_medicineUC == null)    _medicineUC    = new MedicineUC();
                if (_stockUC == null)       _stockUC       = new StockUC();
                if (_saleTabsUC == null)    _saleTabsUC    = new SaleTabsUC();
                if (_purchaseUC == null)    _purchaseUC    = new PurchaseUC();
                if (_supplierUC == null)    _supplierUC    = new SupplierUC();
                if (_customerUC == null)    _customerUC    = new CustomerUC();
                if (_reportUC == null)      _reportUC      = new ReportUC();
                if (_analysisUC == null)    _analysisUC    = new AnalysisUC();
                if (_accountingUC == null)  _accountingUC  = new AccountingUC();
                if (_staffUC == null)       _staffUC       = new StaffUC();
                if (_usersUC == null)       _usersUC       = new UsersUC();
                if (_settingsUC == null)    _settingsUC    = new SettingsUC();
                if (_changePasswordUC == null) _changePasswordUC = new ChangePasswordUC();
                if (_backupUC == null)      _backupUC      = new BackupUC();
            }
            catch { /* silently ignore any pre-warm errors */ }
        }

        private void EnforceSecurityPrivileges()
        {
            if (AppSession.IsCashier)
            {
                // A Cashier / Terminal PC should NOT have access to these panels
                btnAnalysis.Visibility = Visibility.Collapsed;
                btnAccounting.Visibility = Visibility.Collapsed;
                btnHR.Visibility = Visibility.Collapsed;
                btnSettings.Visibility = Visibility.Collapsed;
                btnUsers.Visibility = Visibility.Collapsed;
                btnBackup.Visibility = Visibility.Collapsed;
                btnPurchases.Visibility = Visibility.Collapsed; // Purchasing handled by admins
                btnReports.Visibility = Visibility.Collapsed;
            }
        }

        // UI Cache to fix navigation lag
        private DashboardUC _dashboardUC;
        private MedicineUC _medicineUC;
        private StockUC    _stockUC;
        private SaleTabsUC _saleTabsUC;
        private SupplierUC _supplierUC;
        private CustomerUC _customerUC;
        private AccountingUC _accountingUC;
        private PurchaseUC _purchaseUC;
        private ReportUC _reportUC;
        private AnalysisUC _analysisUC;
        private StaffUC _staffUC;
        private UsersUC _usersUC;
        private SettingsUC _settingsUC;
        private ChangePasswordUC _changePasswordUC;
        private BackupUC _backupUC;

        private void SideMenu_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            try
            {
                if (sideMenu == null || sideMenu.SelectedItem == null) return;

                var item = sideMenu.SelectedItem as System.Windows.Controls.ListBoxItem;
                if (item == null || item.Content == null) return;

                string content = item.Content.ToString();
                NavigateTo(content);
            }
            catch (Exception) { }
        }

        private void Window_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            // Global Navigation Shortcuts (Alt + Key)
            if (System.Windows.Input.Keyboard.Modifiers == System.Windows.Input.ModifierKeys.Alt)
            {
                // In WPF, when Alt is pressed, the key is sent to SystemKey instead of Key
                switch (e.SystemKey)
                {
                    case System.Windows.Input.Key.D: NavigateTo("Dashboard"); e.Handled = true; break;
                    case System.Windows.Input.Key.M: NavigateTo("Medicines"); e.Handled = true; break;
                    case System.Windows.Input.Key.S: NavigateTo("Sales (POS)"); e.Handled = true; break;
                    case System.Windows.Input.Key.P: NavigateTo("Purchases"); e.Handled = true; break;
                    case System.Windows.Input.Key.R: NavigateTo("Reports"); e.Handled = true; break;
                    case System.Windows.Input.Key.A: NavigateTo("Accounting"); e.Handled = true; break;
                    case System.Windows.Input.Key.C: NavigateTo("Customers"); e.Handled = true; break;
                    case System.Windows.Input.Key.U: NavigateTo("Suppliers"); e.Handled = true; break;
                    case System.Windows.Input.Key.E: NavigateTo("HR & Staff"); e.Handled = true; break;
                    case System.Windows.Input.Key.K: NavigateTo("System Users"); e.Handled = true; break;
                }
            }
            else if (System.Windows.Input.Keyboard.Modifiers == System.Windows.Input.ModifierKeys.Control)
            {
                if (e.Key == System.Windows.Input.Key.L)
                {
                    LogoutButton_Click(null, null);
                    e.Handled = true;
                }
            }
        }

        public void NavigateTo(string content)
        {
            // Security check for cashier
            if (AppSession.IsCashier && (content == "Analysis" || content == "Accounting" || 
                content == "HR & Staff" || content == "Settings" || content == "System Users" || 
                content == "Backup & Restore" || content == "Purchases" || content == "Reports" || content == "Stock Report"))
            {
                return; // Access denied silently
            }

            // Optional: Update ListBox selection visually if triggered via keyboard
            foreach (System.Windows.Controls.ListBoxItem item in sideMenu.Items)
            {
                if (item.Content != null && item.Content.ToString() == content)
                {
                    sideMenu.SelectedItem = item;
                    break;
                }
            }

            if (content == "Dashboard")
            {
                if (_dashboardUC == null) _dashboardUC = new DashboardUC() { DataContext = _viewModel };
                else _dashboardUC.RefreshDashboard(); // Force immediate refresh
                MainContentArea.Content = _dashboardUC;
            }
            else if (content == "Medicines")
            {
                if (_medicineUC == null) _medicineUC = new MedicineUC();
                else { var mvm = _medicineUC.DataContext as ViewModels.MedicineViewModel; if (mvm != null) mvm.LoadMedicines(); }
                MainContentArea.Content = _medicineUC;
            }
            else if (content == "Available Stock")
            {
                if (_stockUC == null) _stockUC = new StockUC();
                else _stockUC.Refresh(); // always pull fresh data on navigation
                MainContentArea.Content = _stockUC;
            }
            else if (content == "Sales (POS)")
            {
                if (_saleTabsUC == null) _saleTabsUC = new SaleTabsUC();
                // Immediately refresh medicine list in every open sale tab
                // so prices/stock added via Purchases are visible without delay
                _saleTabsUC.RefreshAllTabs();
                MainContentArea.Content = _saleTabsUC;
            }
            else if (content == "Suppliers")
            {
                if (_supplierUC == null) _supplierUC = new SupplierUC();
                MainContentArea.Content = _supplierUC;
            }
            else if (content == "Customers")
            {
                if (_customerUC == null) _customerUC = new CustomerUC();
                MainContentArea.Content = _customerUC;
            }
            else if (content == "Accounting")
            {
                if (_accountingUC == null) _accountingUC = new AccountingUC();
                MainContentArea.Content = _accountingUC;
            }
            else if (content == "Purchases")
            {
                if (_purchaseUC == null) _purchaseUC = new PurchaseUC();
                MainContentArea.Content = _purchaseUC;
            }
            else if (content == "Stock Report" || content == "Reports")
            {
                if (_reportUC == null) _reportUC = new ReportUC();
                MainContentArea.Content = _reportUC;
            }
            else if (content == "Analysis")
            {
                if (_analysisUC == null) _analysisUC = new AnalysisUC();
                else _analysisUC.RefreshData(); // Force real-time refresh
                MainContentArea.Content = _analysisUC;
            }
            else if (content == "HR & Staff")
            {
                if (_staffUC == null) _staffUC = new StaffUC();
                MainContentArea.Content = _staffUC;
            }
            else if (content == "System Users")
            {
                if (_usersUC == null) _usersUC = new UsersUC();
                MainContentArea.Content = _usersUC;
            }
            else if (content == "Settings")
            {
                if (_settingsUC == null) _settingsUC = new SettingsUC();
                MainContentArea.Content = _settingsUC;
            }
            else if (content == "Change Password")
            {
                if (_changePasswordUC == null) _changePasswordUC = new ChangePasswordUC();
                MainContentArea.Content = _changePasswordUC;
            }
            else if (content == "Backup & Restore")
            {
                if (_backupUC == null) _backupUC = new BackupUC();
                MainContentArea.Content = _backupUC;
            }

            // Aggressively force keyboard focus to the newly loaded page
            var contentElement = MainContentArea.Content as UIElement;
            if (contentElement != null)
            {
                contentElement.Focusable = true;
                System.Windows.Input.Keyboard.Focus(contentElement);
            }
        }

        private void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            LoginWindow login = new LoginWindow();
            login.Show();
            this.Close();
        }



        private void TotalMedicines_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            MedicineWindow med = new MedicineWindow();
            med.ShowDialog();
        }

        private void LowStock_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            ReportWindow rep = new ReportWindow("LowStock");
            rep.ShowDialog();
        }

        private void ExpiredItems_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            ReportWindow rep = new ReportWindow("Expired");
            rep.ShowDialog();
        }
    }
}
