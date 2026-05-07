using System.Windows.Controls;
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using System.Linq;
using PharmaBilling.Source.ViewModels;
using PharmaBilling.Source.Data;

namespace PharmaBilling.Source.Views
{
    public partial class ReportUC : UserControl
    {
        private ReportViewModel _vm;
        private string _currentReportType = "Stock";
        private DataTable _fullTable;
        private DispatcherTimer _searchDebounce;

        public ReportUC(string reportType = "Stock")
        {
            InitializeComponent();
            _vm = new ReportViewModel();
            _currentReportType = reportType;

            _searchDebounce = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(300) };
            _searchDebounce.Tick += SearchDebounce_Tick;

            this.Loaded += (s, e) =>
            {
                LoadBranding();
                // Trigger the correct report asynchronously - DO NOT block UI thread
                if      (_currentReportType == "LowStock") LowStockReport_Click(this, null);
                else if (_currentReportType == "Expired")  ExpiredReport_Click(this, null);
                else if (_currentReportType == "Sales")    SalesReport_Click(this, null);
                else if (_currentReportType == "Purchase") PurchaseReport_Click(this, null);
                else                                       StockReport_Click(this, null);
            };
        }

        // ── SEARCH (debounced) ────────────────────────────────────────────────
        private void txtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            _searchDebounce.Stop();
            _searchDebounce.Start();
        }

        private void SearchDebounce_Tick(object sender, EventArgs e)
        {
            _searchDebounce.Stop();
            ApplySearch();
        }

        private void ApplySearch()
        {
            if (_fullTable == null) return;
            string query = (txtSearch.Text != null ? txtSearch.Text.Trim() : "");

            Task.Run(() =>
            {
                string rowFilter = "";
                if (!string.IsNullOrEmpty(query))
                {
                    var conditions = new List<string>();
                    foreach (DataColumn col in _fullTable.Columns)
                    {
                        string safeQuery = query.Replace("'", "''");
                        conditions.Add(string.Format("CONVERT([{0}], 'System.String') LIKE '%{1}%'", col.ColumnName, safeQuery));
                    }
                    rowFilter = conditions.Count > 0 ? string.Join(" OR ", conditions) : "";
                }

                Dispatcher.Invoke(() =>
                {
                    DataView view = _fullTable.DefaultView;
                    view.RowFilter = rowFilter;
                    reportGrid.ItemsSource = view;
                    lblSearchHint.Text = string.IsNullOrEmpty(query)
                        ? "Type to search…"
                        : string.Format("{0} result(s) for \"{1}\"", view.Count, query);
                    UpdateSummaryFromView(view);
                });
            });
        }

        // ── BRANDING ─────────────────────────────────────────────────────────
        private void LoadBranding()
        {
            try
            {
                var config = ApiClient.Instance;
                brandName.Text    = config.PharmacyName;
                brandTagline.Text = config.PharmacyTagline;
            }
            catch { }
        }

        // ── ASYNC REPORT LOADER ───────────────────────────────────────────────
        // All report queries now run on a background thread.
        // The UI shows a loading state immediately, then updates when done.
        private void LoadReportAsync(string title, Func<DataTable> queryFunc, bool showFilters = false)
        {
            reportTitle.Text = "Loading…";
            salesFilters.Visibility = showFilters ? Visibility.Visible : Visibility.Collapsed;
            pdfBtnGeneric.Visibility = Visibility.Collapsed;
            reportGrid.ItemsSource = null;
            txtSummary.Text = "Fetching data…";

            Task.Run(() =>
            {
                DataTable dt = null;
                try { dt = queryFunc(); }
                catch (Exception ex)
                {
                    dt = new DataTable();
                    dt.Columns.Add("Error");
                    dt.Rows.Add(ex.Message);
                }

                // compute summary on background thread too
                var summaryText = ComputeSummary(dt);

                Dispatcher.Invoke(() =>
                {
                    _fullTable = dt;
                    txtSearch.Text = "";
                    lblSearchHint.Text = "Type to search…";
                    reportGrid.ItemsSource = (dt != null ? dt.DefaultView : null);
                    reportTitle.Text = title;
                    txtSummary.Text = summaryText;
                    pdfBtnGeneric.Visibility = Visibility.Visible;
                });
            });
        }

        // ── REPORT BUTTONS ────────────────────────────────────────────────────
        private void SalesReport_Click(object sender, RoutedEventArgs e)
        {
            _currentReportType = "Sales";
            LoadReportAsync("All Time Sales Report",
                () => _vm.GetSalesReport("AllTime"),
                showFilters: true);
        }

        private void PurchaseReport_Click(object sender, RoutedEventArgs e)
        {
            _currentReportType = "Purchase";
            LoadReportAsync("All Time Purchase Report",
                () => _vm.GetPurchaseReport("AllTime"),
                showFilters: true);
        }

        private void TodayFilter_Click(object sender, RoutedEventArgs e)
        {
            string title = string.Format("{0} Report - {1}", _currentReportType, DateTime.Now.ToString("dd-MMM-yyyy"));
            LoadReportAsync(title,
                () => _currentReportType == "Purchase" ? _vm.GetPurchaseReport("Today") : _vm.GetSalesReport("Today"),
                showFilters: true);
        }

        private void Last7DaysFilter_Click(object sender, RoutedEventArgs e)
        {
            LoadReportAsync(_currentReportType + " Report - Last 7 Days",
                () => _currentReportType == "Purchase" ? _vm.GetPurchaseReport("Last7Days") : _vm.GetSalesReport("Last7Days"),
                showFilters: true);
        }

        private void Last30DaysFilter_Click(object sender, RoutedEventArgs e)
        {
            LoadReportAsync(_currentReportType + " Report - Last 30 Days",
                () => _currentReportType == "Purchase" ? _vm.GetPurchaseReport("Last30Days") : _vm.GetSalesReport("Last30Days"),
                showFilters: true);
        }

        private void MonthFilter_Click(object sender, RoutedEventArgs e)
        {
            LoadReportAsync(string.Format("{0} Report - {1}", _currentReportType, DateTime.Now.ToString("MMMM yyyy")),
                () => _currentReportType == "Purchase" ? _vm.GetPurchaseReport("Month") : _vm.GetSalesReport("Month"),
                showFilters: true);
        }

        private void AllTimeFilter_Click(object sender, RoutedEventArgs e)
        {
            LoadReportAsync("All Time " + _currentReportType + " Report",
                () => _currentReportType == "Purchase" ? _vm.GetPurchaseReport("AllTime") : _vm.GetSalesReport("AllTime"),
                showFilters: true);
        }

        private void StockReport_Click(object sender, RoutedEventArgs e)
        {
            _currentReportType = "Stock";
            LoadReportAsync("Current Stock Report", () => _vm.GetStockReport(), showFilters: false);
        }

        private void ExpiredReport_Click(object sender, RoutedEventArgs e)
        {
            _currentReportType = "Expired";
            LoadReportAsync("Expired Medicine Report", () => _vm.GetExpiredMedicineReport(), showFilters: false);
        }

        private void LowStockReport_Click(object sender, RoutedEventArgs e)
        {
            _currentReportType = "LowStock";
            LoadReportAsync("Low Stock Alert Report", () => _vm.GetLowStockReport(), showFilters: false);
        }

        // ── SUMMARY HELPERS ────────────────────────────────────────────────────
        private string ComputeSummary(DataTable dt)
        {
            decimal totalAmount = 0, totalTax = 0, totalNet = 0;
            int count = 0;
            if (dt != null && dt.Rows.Count > 0)
            {
                foreach (DataRow row in dt.Rows)
                {
                    if (row[0].ToString() == "ERROR") continue;
                    decimal am = 0, tx = 0, pd = 0, sa = 0;
                    if (dt.Columns.Contains("Amount")         && decimal.TryParse(row["Amount"].ToString(), out am))        totalAmount += am;
                    if (dt.Columns.Contains("Tax")            && decimal.TryParse(row["Tax"].ToString(), out tx))           totalTax    += tx;
                    if (dt.Columns.Contains("Paid")           && decimal.TryParse(row["Paid"].ToString(), out pd))          totalNet    += pd;
                    else if (dt.Columns.Contains("Stock_Available") && decimal.TryParse(row["Stock_Available"].ToString(), out sa)) totalAmount += sa;
                    count++;
                }
            }
            if (_currentReportType == "Sales" || _currentReportType == "Purchase")
                return string.Format("Total Amount: {0:N2} | Total Tax: {1:N2} | Net Paid: {2:N2} | Count: {3}", totalAmount, totalTax, totalNet, count);
            return string.Format("Total Units/Records: {0:N0} | Count: {1}", totalAmount, count);
        }

        private void UpdateSummaryFromView(DataView view)
        {
            decimal totalAmount = 0, totalTax = 0, totalNet = 0;
            int count = 0;
            if (view != null)
            {
                foreach (DataRowView rv in view)
                {
                    DataRow row = rv.Row;
                    if (row[0].ToString() == "ERROR") continue;
                    decimal am = 0, tx = 0, pd = 0, sa = 0;
                    if (view.Table.Columns.Contains("Amount")        && decimal.TryParse(row["Amount"].ToString(), out am))        totalAmount += am;
                    if (view.Table.Columns.Contains("Tax")           && decimal.TryParse(row["Tax"].ToString(), out tx))           totalTax    += tx;
                    if (view.Table.Columns.Contains("Paid")          && decimal.TryParse(row["Paid"].ToString(), out pd))          totalNet    += pd;
                    else if (view.Table.Columns.Contains("Stock_Available") && decimal.TryParse(row["Stock_Available"].ToString(), out sa)) totalAmount += sa;
                    count++;
                }
            }
            if (_currentReportType == "Sales" || _currentReportType == "Purchase")
                txtSummary.Text = string.Format("Total Amount: {0:N2} | Total Tax: {1:N2} | Net Paid: {2:N2} | Count: {3}", totalAmount, totalTax, totalNet, count);
            else
                txtSummary.Text = string.Format("Total Units/Records: {0:N0} | Count: {1}", totalAmount, count);
        }

        private void ExportPDF_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                PrintDialog pd = new PrintDialog();
                if (pd.ShowDialog() == true)
                {
                    System.Windows.Documents.FlowDocument doc = new System.Windows.Documents.FlowDocument();
                    doc.PagePadding = new Thickness(30);
                    doc.ColumnWidth = pd.PrintableAreaWidth;
                    doc.FontFamily = new System.Windows.Media.FontFamily("Segoe UI");

                    // Title
                    System.Windows.Documents.Paragraph titlePara = new System.Windows.Documents.Paragraph(new System.Windows.Documents.Run("Medixa Pharmacy - " + reportTitle.Text));
                    titlePara.FontSize = 20;
                    titlePara.FontWeight = FontWeights.Bold;
                    titlePara.TextAlignment = TextAlignment.Center;
                    titlePara.Margin = new Thickness(0, 0, 0, 5);
                    doc.Blocks.Add(titlePara);

                    // Timestamp & Summary
                    System.Windows.Documents.Paragraph sumPara = new System.Windows.Documents.Paragraph(new System.Windows.Documents.Run("Generated: " + DateTime.Now.ToString("dd-MMM-yyyy HH:mm") + "\n" + txtSummary.Text));
                    sumPara.FontSize = 12;
                    sumPara.FontStyle = FontStyles.Italic;
                    sumPara.TextAlignment = TextAlignment.Center;
                    sumPara.Margin = new Thickness(0, 0, 0, 20);
                    doc.Blocks.Add(sumPara);

                    // Build Table
                    System.Data.DataView view = reportGrid.ItemsSource as System.Data.DataView;
                    if (view != null && view.Table != null && view.Table.Columns.Count > 0)
                    {
                        System.Windows.Documents.Table table = new System.Windows.Documents.Table();
                        table.CellSpacing = 0;
                        table.BorderBrush = System.Windows.Media.Brushes.Gray;
                        table.BorderThickness = new Thickness(1);

                        int colCount = view.Table.Columns.Count;
                        for (int i = 0; i < colCount; i++) table.Columns.Add(new System.Windows.Documents.TableColumn());

                        // Header
                        System.Windows.Documents.TableRowGroup headerGroup = new System.Windows.Documents.TableRowGroup();
                        System.Windows.Documents.TableRow headerRow = new System.Windows.Documents.TableRow();
                        headerRow.Background = System.Windows.Media.Brushes.LightGray;
                        headerRow.FontWeight = FontWeights.Bold;

                        for (int i = 0; i < colCount; i++)
                        {
                            System.Windows.Documents.TableCell cell = new System.Windows.Documents.TableCell(new System.Windows.Documents.Paragraph(new System.Windows.Documents.Run(view.Table.Columns[i].ColumnName)));
                            cell.Padding = new Thickness(4);
                            cell.BorderBrush = System.Windows.Media.Brushes.Gray;
                            cell.BorderThickness = new Thickness(0, 0, 1, 1);
                            headerRow.Cells.Add(cell);
                        }
                        headerGroup.Rows.Add(headerRow);
                        table.RowGroups.Add(headerGroup);

                        // Data
                        System.Windows.Documents.TableRowGroup dataGroup = new System.Windows.Documents.TableRowGroup();
                        foreach (System.Data.DataRowView rv in view)
                        {
                            System.Windows.Documents.TableRow row = new System.Windows.Documents.TableRow();
                            for (int i = 0; i < colCount; i++)
                            {
                                System.Windows.Documents.TableCell cell = new System.Windows.Documents.TableCell(new System.Windows.Documents.Paragraph(new System.Windows.Documents.Run(rv[i].ToString())));
                                cell.Padding = new Thickness(4);
                                cell.BorderBrush = System.Windows.Media.Brushes.Gray;
                                cell.BorderThickness = new Thickness(0, 0, 1, 1);
                                row.Cells.Add(cell);
                            }
                            dataGroup.Rows.Add(row);
                        }
                        table.RowGroups.Add(dataGroup);
                        doc.Blocks.Add(table);
                    }

                    System.Windows.Documents.IDocumentPaginatorSource idpSource = doc;
                    pd.PrintDocument(idpSource.DocumentPaginator, "Pharma Report Export");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("PDF Export Error: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void ReportGrid_AutoGeneratedColumns(object sender, EventArgs e)
        {
            var existing = reportGrid.Columns.FirstOrDefault(c => c.Header != null && c.Header.ToString() == "Actions");
            if (existing != null) reportGrid.Columns.Remove(existing);

            if (_currentReportType == "Stock" || _currentReportType == "Expired" || _currentReportType == "LowStock")
            {
                reportGrid.Columns.Add(new DataGridTemplateColumn
                {
                    Header = "Actions",
                    CellTemplate = (DataTemplate)this.Resources["MedicineActionTemplate"],
                    Width = 140
                });
            }
            else if (_currentReportType == "Sales")
            {
                reportGrid.Columns.Add(new DataGridTemplateColumn
                {
                    Header = "Actions",
                    CellTemplate = (DataTemplate)this.Resources["SaleActionTemplate"],
                    Width = 180
                });
            }
            else if (_currentReportType == "Purchase")
            {
                reportGrid.Columns.Add(new DataGridTemplateColumn
                {
                    Header = "Actions",
                    CellTemplate = (DataTemplate)this.Resources["PurchaseActionTemplate"],
                    Width = 180
                });
            }
        }

        private void ActionEdit_Click(object sender, RoutedEventArgs e)
        {
            if (AppSession.IsCashier)
            {
                MessageBox.Show("Access Denied: You do not have permission to modify medicine records.", "Restricted", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var btn = sender as Button;
            var row = btn.DataContext as DataRowView;
            if (row == null) return;

            string medName = row["Medicine_Name"].ToString();
            var med = AppCache.Medicines.Find(m => m.Name == medName);
            if (med == null)
            {
                MessageBox.Show("Could not locate medicine details in cache. Try searching by name in the Medicines tab.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            AddMedicineWindow editWin = new AddMedicineWindow(med);
            editWin.Owner = Window.GetWindow(this);
            if (editWin.ShowDialog() == true)
            {
                var vm = new MedicineViewModel();
                vm.UpdateMedicine(editWin.NewMedicine);
                MessageBox.Show("Medicine updated successfully! Refresh report to see changes.", "Success");
            }
        }

        private void ActionSale_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            var row = btn.DataContext as DataRowView;
            if (row == null) return;

            string medName = row["Medicine_Name"].ToString();
            Clipboard.SetText(medName);

            MessageBox.Show(string.Format("To sell this item, you will be redirected to the POS.\n\nThe medicine name '{0}' has been copied to your clipboard.\nPlease paste it in the search bar.", medName), "Shortcut");

            var win = Window.GetWindow(this) as DashboardWindow;
            if (win != null)
            {
                win.NavigateTo("Sales (POS)");
            }
        }

        private void ActionSaleReturn_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            var row = btn.DataContext as DataRowView;
            if (row == null) return;

            int saleId = Convert.ToInt32(row["Invoice_No"]);
            
            var win = new SaleReturnWindow();
            win.Owner = Window.GetWindow(this);
            var vm = win.DataContext as PharmaBilling.Source.ViewModels.SaleReturnViewModel;
            if (vm != null)
            {
                var s = vm.AllSales.FirstOrDefault(x => x.SaleID == saleId);
                if (s != null) vm.SelectedSale = s;
            }

            if (win.ShowDialog() == true)
            {
                MessageBox.Show("Refresh the report to see the updated totals.", "Refresh Required");
            }
        }

        private void ActionSaleDelete_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            var row = btn.DataContext as DataRowView;
            if (row == null) return;

            int saleId = Convert.ToInt32(row["Invoice_No"]);

            if (MessageBox.Show("Are you sure you want to completely DELETE this sale?\nStock will be restored.", "Delete Sale", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
            {
                try
                {
                    var commands = new System.Collections.Generic.List<Tuple<string, System.Data.SQLite.SQLiteParameter[]>>();
                    
                    string revStockSql = @"INSERT INTO Stocks (MedicineID, BatchNo, RackNo, ExpiryDate, Quantity, DateAdded) 
                                           SELECT MedicineID, BatchNo, 'REVERSAL', '', Quantity, datetime('now')
                                           FROM SaleDetails WHERE SaleID = @sid";
                    commands.Add(new Tuple<string, System.Data.SQLite.SQLiteParameter[]>(revStockSql, new System.Data.SQLite.SQLiteParameter[] { new System.Data.SQLite.SQLiteParameter("@sid", saleId) }));

                    string delDet = "DELETE FROM SaleDetails WHERE SaleID = @sid";
                    commands.Add(new Tuple<string, System.Data.SQLite.SQLiteParameter[]>(delDet, new System.Data.SQLite.SQLiteParameter[] { new System.Data.SQLite.SQLiteParameter("@sid", saleId) }));

                    string delSale = "DELETE FROM Sales WHERE SaleID = @sid";
                    commands.Add(new Tuple<string, System.Data.SQLite.SQLiteParameter[]>(delSale, new System.Data.SQLite.SQLiteParameter[] { new System.Data.SQLite.SQLiteParameter("@sid", saleId) }));

                    new PharmaBilling.Source.Data.DbHelper().ExecuteTransaction(commands);
                    PharmaBilling.Source.Data.CloudSyncService.DeleteSaleAsync(saleId);
                    PharmaBilling.Source.Data.AppEvents.OnSaleDataChanged();

                    MessageBox.Show("Sale deleted and stock restored successfully. Please refresh the report.", "Success");
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error deleting sale: " + ex.Message, "Error");
                }
            }
        }

        private void ActionPurchaseEdit_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            var row = btn.DataContext as DataRowView;
            if (row == null) return;

            int purchaseId = Convert.ToInt32(row["PurchaseID"]);
            string invoiceNo = row["InvoiceNo"].ToString();
            string supplierName = row["Supplier"].ToString();

            AddPurchaseWindow editWin = new AddPurchaseWindow(purchaseId, invoiceNo, supplierName);
            editWin.Owner = Window.GetWindow(this);
            editWin.ShowDialog();
        }

        private void ActionPurchaseDelete_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            var row = btn.DataContext as DataRowView;
            if (row == null) return;

            int purchaseId = Convert.ToInt32(row["PurchaseID"]);

            if (MessageBox.Show("Are you sure you want to completely DELETE this purchase?\nStock added from this purchase will be reversed.", "Delete Purchase", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
            {
                try
                {
                    var commands = new System.Collections.Generic.List<Tuple<string, System.Data.SQLite.SQLiteParameter[]>>();
                    
                    // Reverse the stock
                    string revStockSql = @"INSERT INTO Stocks (MedicineID, BatchNo, RackNo, ExpiryDate, Quantity, DateAdded) 
                                           SELECT MedicineID, BatchNo, 'REVERSAL', ExpiryDate, -Quantity, datetime('now')
                                           FROM PurchaseDetails WHERE PurchaseID = @pid";
                    commands.Add(new Tuple<string, System.Data.SQLite.SQLiteParameter[]>(revStockSql, new System.Data.SQLite.SQLiteParameter[] { new System.Data.SQLite.SQLiteParameter("@pid", purchaseId) }));

                    string delDet = "DELETE FROM PurchaseDetails WHERE PurchaseID = @pid";
                    commands.Add(new Tuple<string, System.Data.SQLite.SQLiteParameter[]>(delDet, new System.Data.SQLite.SQLiteParameter[] { new System.Data.SQLite.SQLiteParameter("@pid", purchaseId) }));

                    string delPurchase = "DELETE FROM Purchases WHERE PurchaseID = @pid";
                    commands.Add(new Tuple<string, System.Data.SQLite.SQLiteParameter[]>(delPurchase, new System.Data.SQLite.SQLiteParameter[] { new System.Data.SQLite.SQLiteParameter("@pid", purchaseId) }));

                    new PharmaBilling.Source.Data.DbHelper().ExecuteTransaction(commands);
                    PharmaBilling.Source.Data.AppEvents.OnPurchaseDataChanged();

                    MessageBox.Show("Purchase deleted and stock restored successfully. Please refresh the report.", "Success");
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error deleting purchase: " + ex.Message, "Error");
                }
            }
        }
    }
}

