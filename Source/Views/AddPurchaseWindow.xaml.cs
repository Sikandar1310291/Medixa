using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using PharmaBilling.Source.Data;
using PharmaBilling.Source.Models;
using PharmaBilling.Source.ViewModels;

namespace PharmaBilling.Source.Views
{
    public partial class AddPurchaseWindow : Window
    {
        private PurchaseViewModel _vm;
        private DispatcherTimer _searchDebounceTimer;

        public AddPurchaseWindow()
        {
            InitializeComponent();
            _vm = new PurchaseViewModel();
            this.DataContext = _vm;

            _searchDebounceTimer = new DispatcherTimer();
            _searchDebounceTimer.Interval = TimeSpan.FromMilliseconds(300);
            _searchDebounceTimer.Tick += SearchDebounceTimer_Tick;
        }

        private int _editPurchaseId = 0;

        public AddPurchaseWindow(int purchaseId, string invoiceNo, string supplierName) : this()
        {
            _editPurchaseId = purchaseId;
            _vm.InvoiceNo = invoiceNo;

            foreach (var s in _vm.Suppliers)
            {
                if (s.Name == supplierName)
                {
                    _vm.SelectedSupplier = s;
                    break;
                }
            }

            LoadPurchaseItems(purchaseId);
        }

        private void LoadPurchaseItems(int purchaseId)
        {
            var dt = AppCache.Db.GetDataTable(
                "SELECT pd.*, m.Name as MedicineName, m.SalePrice, m.BoxSize FROM PurchaseDetails pd " +
                "JOIN Medicines m ON pd.MedicineID = m.MedicineID WHERE pd.PurchaseID = " + purchaseId);

            foreach (System.Data.DataRow row in dt.Rows)
            {
                double qty     = Convert.ToDouble(row["Quantity"]);
                double boxSize = row["BoxSize"] != DBNull.Value ? Convert.ToDouble(row["BoxSize"]) : 1;
                if (boxSize < 1) boxSize = 1;

                double perUnitTP     = Convert.ToDouble(row["PurchasePrice"]);
                double perUnitRetail = row["SalePrice"] != DBNull.Value ? Convert.ToDouble(row["SalePrice"]) : 0;

                var item = new PurchaseItemViewModel
                {
                    MedicineID   = Convert.ToInt32(row["MedicineID"]),
                    MedicineName = row["MedicineName"].ToString(),
                    BatchNo      = row["BatchNo"].ToString(),
                    ExpiryDate   = row["ExpiryDate"].ToString(),
                    PackSize     = boxSize,
                    TP           = perUnitTP * boxSize,
                    Retail       = perUnitRetail * boxSize,
                };

                item.Packs    = Math.Floor(qty / boxSize);
                item.LooseQty = qty % boxSize;

                _vm.PurchaseItems.Add(item);
            }
        }

        private async void SearchDebounceTimer_Tick(object sender, EventArgs e)
        {
            _searchDebounceTimer.Stop();
            await PerformSearchAsync();
        }

        private void cboMedicines_KeyUp(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                if (cboMedicines.IsDropDownOpen)
                    cboMedicines.IsDropDownOpen = false;
                AddItem_Click(sender, new RoutedEventArgs());
                e.Handled = true;
                return;
            }

            if (e.Key == System.Windows.Input.Key.Up   || e.Key == System.Windows.Input.Key.Down ||
                e.Key == System.Windows.Input.Key.Escape ||
                e.Key == System.Windows.Input.Key.Left  || e.Key == System.Windows.Input.Key.Right ||
                e.Key == System.Windows.Input.Key.Tab)
            {
                return;
            }

            _searchDebounceTimer.Stop();
            _searchDebounceTimer.Start();
        }

        private async System.Threading.Tasks.Task PerformSearchAsync()
        {
            var cbo = cboMedicines;
            var tb  = cbo.Template.FindName("PART_EditableTextBox", cbo) as TextBox;

            if (tb != null)
            {
                string filterText = tb.Text;
                int    caretPos   = tb.CaretIndex;

                await _vm.FilterMedicinesAsync(filterText);

                if (!cbo.IsDropDownOpen)
                    cbo.IsDropDownOpen = true;

                if (tb.Text != filterText)
                {
                    tb.Text       = filterText;
                    tb.CaretIndex = caretPos;
                }
            }
        }

        private void AddItem_Click(object sender, RoutedEventArgs e)
        {
            Medicine selected = cboMedicines.SelectedItem as Medicine;

            if (selected == null && !string.IsNullOrWhiteSpace(cboMedicines.Text))
            {
                foreach (Medicine m in _vm.AllMedicines)
                {
                    if (m.Name.Equals(cboMedicines.Text, System.StringComparison.OrdinalIgnoreCase))
                    {
                        selected = m;
                        break;
                    }
                }

                if (selected == null)
                {
                    if (MessageBox.Show("Medicine '" + cboMedicines.Text + "' does not exist. Do you want to quickly add it?", "Add New Medicine", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                        selected = _vm.CreateQuickMedicine(cboMedicines.Text);
                }
            }

            if (selected != null)
            {
                _vm.AddToPurchase(selected, 1);
                cboMedicines.SelectedItem = null;
                cboMedicines.Text         = "";
            }
            else
            {
                MessageBox.Show("Please select a valid medicine from the dropdown to add.", "Selection Required", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void RemoveItem_Click(object sender, RoutedEventArgs e)
        {
            var btn  = (Button)sender;
            var item = (PurchaseItemViewModel)btn.DataContext;
            if (item != null)
                _vm.RemoveFromPurchase(item);
        }

        private void SavePurchase_Click(object sender, RoutedEventArgs e)
        {
            if (_vm.SelectedSupplier == null && !string.IsNullOrWhiteSpace(cboSupplier.Text))
                _vm.SelectedSupplier = _vm.CreateQuickSupplier(cboSupplier.Text);

            if (_vm.SelectedSupplier == null)
            {
                MessageBox.Show("Please select or type a supplier's name.", "Required Field", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (_vm.PurchaseItems.Count == 0)
            {
                MessageBox.Show("Please add at least one medicine to this purchase invoice.", "No Items", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (MessageBox.Show("Are you sure you want to save this purchase? Stock will be updated immediately.", "Confirm Save", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                if (_editPurchaseId > 0)
                {
                    // Reverse old stock and delete old purchase before saving the new one
                    var commands = new System.Collections.Generic.List<Tuple<string, System.Data.SQLite.SQLiteParameter[]>>();

                    string revStockSql = @"INSERT INTO Stocks (MedicineID, BatchNo, RackNo, ExpiryDate, Quantity, DateAdded) 
                                           SELECT MedicineID, BatchNo, 'REVERSAL', ExpiryDate, -Quantity, datetime('now')
                                           FROM PurchaseDetails WHERE PurchaseID = @pid";
                    commands.Add(new Tuple<string, System.Data.SQLite.SQLiteParameter[]>(revStockSql,
                        new System.Data.SQLite.SQLiteParameter[] { new System.Data.SQLite.SQLiteParameter("@pid", _editPurchaseId) }));

                    commands.Add(new Tuple<string, System.Data.SQLite.SQLiteParameter[]>(
                        "DELETE FROM PurchaseDetails WHERE PurchaseID = @pid",
                        new System.Data.SQLite.SQLiteParameter[] { new System.Data.SQLite.SQLiteParameter("@pid", _editPurchaseId) }));

                    commands.Add(new Tuple<string, System.Data.SQLite.SQLiteParameter[]>(
                        "DELETE FROM Purchases WHERE PurchaseID = @pid",
                        new System.Data.SQLite.SQLiteParameter[] { new System.Data.SQLite.SQLiteParameter("@pid", _editPurchaseId) }));

                    new DbHelper().ExecuteTransaction(commands);
                }

                bool success = _vm.SavePurchase();
                if (success)
                {
                    // ── Notify Dashboard immediately ──────────────────────────
                    AppEvents.OnPurchaseDataChanged();

                    MessageBox.Show(_editPurchaseId > 0 ? "Purchase updated successfully!" : "Purchase saved successfully!",
                        "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                    this.Close();
                }
                else
                {
                    MessageBox.Show("An error occurred while saving the purchase.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void Window_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Escape)
            {
                if (cboMedicines.IsDropDownOpen)
                    cboMedicines.IsDropDownOpen = false;
                else if (cboSupplier.IsDropDownOpen)
                    cboSupplier.IsDropDownOpen = false;
                else
                    this.Close();

                e.Handled = true;
            }
        }
    }
}
