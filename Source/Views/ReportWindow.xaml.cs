using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using PharmaBilling.Source.ViewModels;
using PharmaBilling.Source.Data;

namespace PharmaBilling.Source.Views
{
    public partial class ReportWindow : Window
    {
        private ReportViewModel _vm;
        private string _currentReportType = "Stock";

        // Cached full table — search never re-hits the DB
        private DataTable _fullTable;

        // 300ms debounce timer
        private DispatcherTimer _searchDebounce;

        public ReportWindow(string reportType = "Stock")
        {
            InitializeComponent();
            _vm = new ReportViewModel();
            _currentReportType = reportType;

            _searchDebounce = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(300) };
            _searchDebounce.Tick += SearchDebounce_Tick;

            this.Loaded += (s, e) =>
            {
                LoadBranding();
                if      (_currentReportType == "LowStock") LowStockReport_Click(this, null);
                else if (_currentReportType == "Expired")  ExpiredReport_Click(this, null);
                else if (_currentReportType == "Sales")    SalesReport_Click(this, null);
                else if (_currentReportType == "Purchase") PurchaseReport_Click(this, null);
                else                                       StockReport_Click(this, null);
            };
        }

        // ── Search ──────────────────────────────────────────────────────────
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
                DataView view = _fullTable.DefaultView;
                if (string.IsNullOrEmpty(query))
                {
                    view.RowFilter = "";
                }
                else
                {
                    var conditions = new List<string>();
                    foreach (DataColumn col in _fullTable.Columns)
                    {
                        if (col.DataType == typeof(string))
                        {
                            string safeQuery = query.Replace("'", "''");
                            conditions.Add(string.Format("[{0}] LIKE '%{1}%'", col.ColumnName, safeQuery));
                        }
                    }
                    view.RowFilter = conditions.Count > 0 ? string.Join(" OR ", conditions) : "";
                }

                int count = view.Count;
                Dispatcher.Invoke(() =>
                {
                    reportGrid.ItemsSource = view;
                    lblSearchHint.Text = string.IsNullOrEmpty(query)
                        ? "Type to search…"
                        : string.Format("{0} result(s) for \"{1}\"", count, query);
                    UpdateSummaryFromView(view);
                });
            });
        }

        // ── Branding ────────────────────────────────────────────────────────
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

        // ── Report Buttons ──────────────────────────────────────────────────
        private void SalesReport_Click(object sender, RoutedEventArgs e)
        {
            _currentReportType = "Sales";
            salesFilters.Visibility = Visibility.Visible;
            AllTimeFilter_Click(null, null);
        }

        private void PurchaseReport_Click(object sender, RoutedEventArgs e)
        {
            _currentReportType = "Purchase";
            salesFilters.Visibility = Visibility.Visible;
            AllTimeFilter_Click(null, null);
        }

        private void TodayFilter_Click(object sender, RoutedEventArgs e)
        {
            DataTable dt = (_currentReportType == "Purchase") ? _vm.GetPurchaseReport("Today") : _vm.GetSalesReport("Today");
            reportTitle.Text = string.Format("{0} Report - {1}", _currentReportType, DateTime.Now.ToString("dd-MMM-yyyy"));
            SetGridAndSummary(dt);

            if (dt.Rows.Count == 0 || (dt.Rows.Count == 1 && dt.Rows[0][0].ToString() == "ERROR"))
            {
                MessageBox.Show("No records found for today. Showing Last 30 Days instead.", "Empty Report", MessageBoxButton.OK, MessageBoxImage.Information);
                Last30DaysFilter_Click(null, null);
            }
        }

        private void Last7DaysFilter_Click(object sender, RoutedEventArgs e)
        {
            reportTitle.Text = _currentReportType + " Report - Last 7 Days";
            DataTable dt = (_currentReportType == "Purchase") ? _vm.GetPurchaseReport("Last7Days") : _vm.GetSalesReport("Last7Days");
            SetGridAndSummary(dt);
        }

        private void Last30DaysFilter_Click(object sender, RoutedEventArgs e)
        {
            reportTitle.Text = _currentReportType + " Report - Last 30 Days";
            DataTable dt = (_currentReportType == "Purchase") ? _vm.GetPurchaseReport("Last30Days") : _vm.GetSalesReport("Last30Days");
            SetGridAndSummary(dt);
        }

        private void MonthFilter_Click(object sender, RoutedEventArgs e)
        {
            reportTitle.Text = string.Format("{0} Report - {1}", _currentReportType, DateTime.Now.ToString("MMMM yyyy"));
            DataTable dt = (_currentReportType == "Purchase") ? _vm.GetPurchaseReport("Month") : _vm.GetSalesReport("Month");
            SetGridAndSummary(dt);
        }

        private void AllTimeFilter_Click(object sender, RoutedEventArgs e)
        {
            reportTitle.Text = "All Time " + _currentReportType + " Report";
            DataTable dt = (_currentReportType == "Purchase") ? _vm.GetPurchaseReport("AllTime") : _vm.GetSalesReport("AllTime");
            SetGridAndSummary(dt);
        }

        private void SetGridAndSummary(DataTable dt)
        {
            _fullTable = dt;
            txtSearch.Text = "";
            lblSearchHint.Text = "Type to search…";
            reportGrid.ItemsSource = dt.DefaultView;
            UpdateSummary(dt);
        }

        private void UpdateSummary(DataTable dt)
        {
            decimal totalAmount = 0, totalTax = 0, totalNet = 0;
            int count = 0;

            if (dt != null && dt.Rows.Count > 0)
            {
                foreach (DataRow row in dt.Rows)
                {
                    if (row[0].ToString() == "ERROR") continue;
                    decimal am = 0, tx = 0, pd = 0, sa = 0;
                    if (dt.Columns.Contains("Amount")        && decimal.TryParse(row["Amount"].ToString(),        out am)) totalAmount += am;
                    if (dt.Columns.Contains("Tax")           && decimal.TryParse(row["Tax"].ToString(),           out tx)) totalTax    += tx;
                    if (dt.Columns.Contains("Paid")          && decimal.TryParse(row["Paid"].ToString(),          out pd)) totalNet    += pd;
                    else if (dt.Columns.Contains("Stock_Available") && decimal.TryParse(row["Stock_Available"].ToString(), out sa)) totalAmount += sa;
                    count++;
                }
            }

            if (_currentReportType == "Sales" || _currentReportType == "Purchase")
                txtSummary.Text = string.Format("Total Amount: {0:N2} | Total Tax: {1:N2} | Net Paid: {2:N2} | Count: {3}", totalAmount, totalTax, totalNet, count);
            else
                txtSummary.Text = string.Format("Total Units/Records: {0:N0} | Count: {1}", totalAmount, count);
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
                    if (view.Table.Columns.Contains("Amount")        && decimal.TryParse(row["Amount"].ToString(),        out am)) totalAmount += am;
                    if (view.Table.Columns.Contains("Tax")           && decimal.TryParse(row["Tax"].ToString(),           out tx)) totalTax    += tx;
                    if (view.Table.Columns.Contains("Paid")          && decimal.TryParse(row["Paid"].ToString(),          out pd)) totalNet    += pd;
                    else if (view.Table.Columns.Contains("Stock_Available") && decimal.TryParse(row["Stock_Available"].ToString(), out sa)) totalAmount += sa;
                    count++;
                }
            }

            if (_currentReportType == "Sales" || _currentReportType == "Purchase")
                txtSummary.Text = string.Format("Total Amount: {0:N2} | Total Tax: {1:N2} | Net Paid: {2:N2} | Count: {3}", totalAmount, totalTax, totalNet, count);
            else
                txtSummary.Text = string.Format("Total Units/Records: {0:N0} | Count: {1}", totalAmount, count);
        }

        private void StockReport_Click(object sender, RoutedEventArgs e)
        {
            _currentReportType = "Stock";
            salesFilters.Visibility = Visibility.Collapsed;
            reportTitle.Text = "Current Stock Report";
            SetGridAndSummary(_vm.GetStockReport());
        }

        private void ExpiredReport_Click(object sender, RoutedEventArgs e)
        {
            _currentReportType = "Expired";
            salesFilters.Visibility = Visibility.Collapsed;
            reportTitle.Text = "Expired Medicine Report";
            SetGridAndSummary(_vm.GetExpiredMedicineReport());
        }

        private void LowStockReport_Click(object sender, RoutedEventArgs e)
        {
            _currentReportType = "LowStock";
            salesFilters.Visibility = Visibility.Collapsed;
            reportTitle.Text = "Low Stock Alert Report";
            SetGridAndSummary(_vm.GetLowStockReport());
        }

        // ── Export ──────────────────────────────────────────────────────────
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
        protected override void OnKeyDown(System.Windows.Input.KeyEventArgs e)
        {
            base.OnKeyDown(e);
            if (!e.Handled && e.Key == System.Windows.Input.Key.Escape)
            {
                this.Close();
                e.Handled = true;
            }
        }
    }
}

