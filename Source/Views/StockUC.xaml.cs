using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using PharmaBilling.Source.Data;

namespace PharmaBilling.Source.Views
{
    // ── Data model for each stock row shown in the DataGrid ─────────────────
    public class StockRow
    {
        public int    RowNo         { get; set; }
        public int    StockID       { get; set; }
        public int    MedicineID    { get; set; }
        public string MedicineName  { get; set; }
        public string Type          { get; set; }
        public string BatchNo       { get; set; }
        public string RackNo        { get; set; }
        public string ExpiryDate    { get; set; }
        public int    Quantity      { get; set; }
        public double PurchasePrice { get; set; }
        public double SalePrice     { get; set; }
        /// <summary>"Expired", "Soon" (within 30 days), or "OK"</summary>
        public string ExpiryStatus  { get; set; }
    }

    public partial class StockUC : UserControl
    {
        private readonly DbHelper _db = new DbHelper();

        // Full un-filtered list kept in memory for instant search
        private List<StockRow> _allRows  = new List<StockRow>();

        // The row currently being edited
        private StockRow _editingRow;

        public StockUC()
        {
            InitializeComponent();
        }

        // ── Lifecycle ───────────────────────────────────────────────────────
        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            LoadStock();
        }

        // ── Public refresh – called by DashboardWindow on navigation ────────
        public void Refresh() => LoadStock();

        // ── Core data loader ─────────────────────────────────────────────────
        private void LoadStock()
        {
            System.Threading.Tasks.Task.Run(() =>
            {
                var rows = FetchFromDb(string.Empty);
                Dispatcher.Invoke(() => ApplyToGrid(rows));
            });
        }

        private List<StockRow> FetchFromDb(string filter)
        {
            var result  = new List<StockRow>();
            var today   = DateTime.Today;
            var soon    = today.AddDays(30);

            string sql = @"
                SELECT s.StockID,
                       m.MedicineID,
                       m.Name          AS MedicineName,
                       m.Type,
                       s.BatchNo,
                       s.RackNo,
                       s.ExpiryDate,
                       s.Quantity,
                       m.PurchasePrice,
                       m.SalePrice
                FROM   Stocks    s
                JOIN   Medicines m ON m.MedicineID = s.MedicineID
                WHERE  m.Status  != 'Inactive'
                  AND  s.Quantity > 0
                ORDER  BY m.Name ASC, s.ExpiryDate ASC";

            DataTable dt = _db.GetDataTable(sql);
            int rowNo = 1;

            foreach (DataRow dr in dt.Rows)
            {
                string name = dr["MedicineName"].ToString();

                // Search filter (case-insensitive)
                if (!string.IsNullOrEmpty(filter) &&
                    name.IndexOf(filter, StringComparison.OrdinalIgnoreCase) < 0)
                    continue;

                // Parse expiry
                string expiryStr = dr["ExpiryDate"].ToString().Trim();
                string expiryStatus = "OK";
                if (!string.IsNullOrEmpty(expiryStr) &&
                    DateTime.TryParseExact(expiryStr, "yyyy-MM-dd",
                        CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime expDt))
                {
                    if (expDt.Date <  today) expiryStatus = "Expired";
                    else if (expDt.Date <= soon) expiryStatus = "Soon";
                }

                result.Add(new StockRow
                {
                    RowNo         = rowNo++,
                    StockID       = Convert.ToInt32(dr["StockID"]),
                    MedicineID    = Convert.ToInt32(dr["MedicineID"]),
                    MedicineName  = name,
                    Type          = dr["Type"].ToString(),
                    BatchNo       = dr["BatchNo"].ToString(),
                    RackNo        = dr["RackNo"].ToString(),
                    ExpiryDate    = expiryStr,
                    Quantity      = Convert.ToInt32(dr["Quantity"]),
                    PurchasePrice = Convert.ToDouble(dr["PurchasePrice"]),
                    SalePrice     = Convert.ToDouble(dr["SalePrice"]),
                    ExpiryStatus  = expiryStatus
                });
            }
            return result;
        }

        // ── Bind list to grid and update KPI strip ───────────────────────────
        private void ApplyToGrid(List<StockRow> rows)
        {
            _allRows = rows;
            dgStock.ItemsSource = null;
            dgStock.ItemsSource = _allRows;

            // KPIs
            var uniqueMeds  = new HashSet<int>();
            long totalUnits = 0;
            double stockVal = 0;

            foreach (var r in _allRows)
            {
                uniqueMeds.Add(r.MedicineID);
                totalUnits += r.Quantity;
                stockVal   += r.Quantity * r.PurchasePrice;
            }

            lblTotalBadge.Text  = $"{_allRows.Count} batches";
            lblUniqueMeds.Text  = uniqueMeds.Count.ToString("N0");
            lblTotalUnits.Text  = totalUnits.ToString("N0");
            lblStockValue.Text  = "Rs. " + stockVal.ToString("N0");
        }

        // ── Search ───────────────────────────────────────────────────────────
        private void TxtSearch_KeyUp(object sender, KeyEventArgs e)
        {
            string q = txtSearch.Text.Trim();
            btnClear.Visibility = string.IsNullOrEmpty(q)
                ? Visibility.Collapsed : Visibility.Visible;

            // Search runs on background thread → fast even for 24k records
            System.Threading.Tasks.Task.Run(() =>
            {
                var filtered = FetchFromDb(q);
                Dispatcher.Invoke(() => ApplyToGrid(filtered));
            });
        }

        private void BtnClear_Click(object sender, RoutedEventArgs e)
        {
            txtSearch.Clear();
            btnClear.Visibility = Visibility.Collapsed;
            LoadStock();
        }

        private void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            txtSearch.Clear();
            btnClear.Visibility = Visibility.Collapsed;
            LoadStock();
        }

        // ── Edit ─────────────────────────────────────────────────────────────
        private void EditStock_Click(object sender, RoutedEventArgs e)
        {
            var row = (sender as Button)?.DataContext as StockRow;
            if (row == null) return;

            _editingRow = row;

            // Populate edit overlay fields
            txtEditMedName.Text = row.MedicineName;
            txtEditBatch.Text   = row.BatchNo;
            txtEditRack.Text    = row.RackNo;
            txtEditExpiry.Text  = row.ExpiryDate;
            txtEditQty.Text     = row.Quantity.ToString();

            editOverlay.Visibility = Visibility.Visible;
            txtEditQty.Focus();
            txtEditQty.SelectAll();
        }

        private void CancelEdit_Click(object sender, RoutedEventArgs e)
        {
            editOverlay.Visibility = Visibility.Collapsed;
            _editingRow = null;
        }

        private void SaveEdit_Click(object sender, RoutedEventArgs e)
        {
            if (_editingRow == null) return;

            // Validate quantity
            if (!int.TryParse(txtEditQty.Text.Trim(), out int newQty) || newQty < 0)
            {
                MessageBox.Show("Please enter a valid quantity (0 or more).",
                                "Invalid Input", MessageBoxButton.OK, MessageBoxImage.Warning);
                txtEditQty.Focus();
                return;
            }

            // Validate expiry
            string newExpiry = txtEditExpiry.Text.Trim();
            if (!string.IsNullOrEmpty(newExpiry) &&
                !DateTime.TryParseExact(newExpiry, "yyyy-MM-dd",
                    CultureInfo.InvariantCulture, DateTimeStyles.None, out _))
            {
                MessageBox.Show("Expiry Date must be in YYYY-MM-DD format (e.g. 2025-12-31).",
                                "Invalid Date", MessageBoxButton.OK, MessageBoxImage.Warning);
                txtEditExpiry.Focus();
                return;
            }

            string newBatch = txtEditBatch.Text.Trim();
            string newRack  = txtEditRack.Text.Trim();

            try
            {
                string sql = @"UPDATE Stocks
                               SET    BatchNo    = @Batch,
                                      RackNo     = @Rack,
                                      ExpiryDate = @Expiry,
                                      Quantity   = @Qty
                               WHERE  StockID    = @StockID";

                var prms = new System.Data.SQLite.SQLiteParameter[]
                {
                    new System.Data.SQLite.SQLiteParameter("@Batch",   newBatch),
                    new System.Data.SQLite.SQLiteParameter("@Rack",    newRack),
                    new System.Data.SQLite.SQLiteParameter("@Expiry",  newExpiry),
                    new System.Data.SQLite.SQLiteParameter("@Qty",     newQty),
                    new System.Data.SQLite.SQLiteParameter("@StockID", _editingRow.StockID)
                };

                _db.ExecuteNonQuery(sql, prms);

                editOverlay.Visibility = Visibility.Collapsed;
                _editingRow = null;

                // Notify Dashboard KPIs
                AppEvents.OnStockChanged();

                // Reload grid
                LoadStock();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error saving changes:\n" + ex.Message,
                                "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ── Delete ───────────────────────────────────────────────────────────
        private void DeleteStock_Click(object sender, RoutedEventArgs e)
        {
            var row = (sender as Button)?.DataContext as StockRow;
            if (row == null) return;

            var confirm = MessageBox.Show(
                $"Delete batch '{row.BatchNo}' of  '{row.MedicineName}'?\n" +
                $"This will permanently remove {row.Quantity} units from stock.",
                "Confirm Delete",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (confirm != MessageBoxResult.Yes) return;

            try
            {
                string sql = "DELETE FROM Stocks WHERE StockID = @StockID";
                var prms = new System.Data.SQLite.SQLiteParameter[]
                {
                    new System.Data.SQLite.SQLiteParameter("@StockID", row.StockID)
                };

                _db.ExecuteNonQuery(sql, prms);

                // Notify Dashboard KPIs + Sales screen
                AppEvents.OnStockChanged();

                // Reload grid
                LoadStock();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error deleting batch:\n" + ex.Message,
                                "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
