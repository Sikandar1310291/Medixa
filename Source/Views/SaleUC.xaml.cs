using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using PharmaBilling.Source.ViewModels;
using PharmaBilling.Source.Models;
using System.Linq;

namespace PharmaBilling.Source.Views
{
    public partial class SaleUC : UserControl
    {
        private SaleViewModel _viewModel;

        public SaleUC()
        {
            InitializeComponent();
            _viewModel = new SaleViewModel();
            this.DataContext = _viewModel;

            // Generate a Doc #
            _viewModel.DocNo = "DOC-" + DateTime.Now.Ticks.ToString().Substring(10);

            // Keyboard shortcuts (Use PreviewKeyDown to intercept before TextBoxes consume them)
            this.PreviewKeyDown += SaleUC_PreviewKeyDown;
            
            // Auto focus search on load
            this.Loaded += (s, e) => { 
                this.Focusable = true;
                Keyboard.Focus(this); 
            };
        }

        private DateTime _lastScanTime = DateTime.Now;
        private string _barcodeBuffer = "";

        private void SaleUC_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            // POS Shortcuts using Ctrl to avoid F-Key issues on laptops
            if (Keyboard.Modifiers == ModifierKeys.Control)
            {
                if (e.Key == Key.F)
                {
                    e.Handled = true; 
                    OpenSearchDialog(); 
                    return;
                }
                else if (e.Key == Key.D)
                {
                    e.Handled = true; 
                    txtDiscount.Focus(); 
                    txtDiscount.SelectAll(); 
                    return;
                }
                else if (e.Key == Key.P)
                {
                    e.Handled = true; 
                    txtCashReceived.Focus(); 
                    txtCashReceived.SelectAll(); 
                    return;
                }
                else if (e.Key == Key.Enter)
                {
                    e.Handled = true; 
                    btnPrintSave_Click(null, null); 
                    return;
                }
            }
            else if (Keyboard.Modifiers == (ModifierKeys.Control | ModifierKeys.Shift))
            {
                if (e.Key == Key.C)
                {
                    e.Handled = true; 
                    txtCashPartyName.Focus(); 
                    txtCashPartyName.SelectAll(); 
                    return;
                }
            }

            switch (e.Key)
            {
                case Key.Escape:
                    e.Handled = true;
                    // Reset POS
                    _viewModel = new SaleViewModel();
                    _viewModel.DocNo = "DOC-" + DateTime.Now.Ticks.ToString().Substring(10);
                    this.DataContext = _viewModel;
                    return;
                case Key.Delete:
                    if (_viewModel.SelectedSaleItem != null && dgSaleItems.IsFocused)
                    {
                        _viewModel.RemoveFromSale(_viewModel.SelectedSaleItem);
                        e.Handled = true;
                        return;
                    }
                    break;
                case Key.Add:
                case Key.OemPlus:
                    if (_viewModel.SelectedSaleItem != null && dgSaleItems.IsFocused)
                    {
                        _viewModel.SelectedSaleItem.Quantity++;
                        _viewModel.CalculateTotals();
                        e.Handled = true;
                        return;
                    }
                    break;
                case Key.Subtract:
                case Key.OemMinus:
                    if (_viewModel.SelectedSaleItem != null && dgSaleItems.IsFocused)
                    {
                        if (_viewModel.SelectedSaleItem.Quantity > 1)
                        {
                            _viewModel.SelectedSaleItem.Quantity--;
                            _viewModel.CalculateTotals();
                        }
                        e.Handled = true;
                        return;
                    }
                    break;
            }

            // Capture Barcode Stream
            if (e.Key != Key.Enter && e.Key != Key.System && e.Key != Key.Tab && e.Key != Key.LeftAlt && e.Key != Key.RightAlt && 
               (e.Key >= Key.A && e.Key <= Key.Z || e.Key >= Key.D0 && e.Key <= Key.D9 || e.Key >= Key.NumPad0 && e.Key <= Key.NumPad9))
            {
                // Measure speed
                TimeSpan diff = DateTime.Now - _lastScanTime;
                if (diff.TotalMilliseconds > 50)
                    _barcodeBuffer = ""; // Reset if typed too slow (human)
                
                _barcodeBuffer += GetCharFromKey(e.Key);
                _lastScanTime = DateTime.Now;
            }
            else if (e.Key == Key.Enter)
            {
                if (_barcodeBuffer.Length >= 3)
                {
                    // Search DB for barcode
                    var matchedMed = _viewModel.AllMedicines.FirstOrDefault(m => m.Barcode == _barcodeBuffer);
                    if (matchedMed != null)
                        _viewModel.AddToSale(matchedMed, 1);
                    else
                        OpenSearchDialog(); // Fallback if barcode failed to find
                    
                    _barcodeBuffer = ""; // Clear buffer
                    e.Handled = true; // Prevent Enter from bubbling if it was a scan
                }
            }
        }

        private string GetCharFromKey(Key key)
        {
            if (key >= Key.D0 && key <= Key.D9) return (key - Key.D0).ToString();
            if (key >= Key.NumPad0 && key <= Key.NumPad9) return (key - Key.NumPad0).ToString();
            return key.ToString();
        }

        private void btnSearchItem_Click(object sender, RoutedEventArgs e)
        {
            OpenSearchDialog();
        }

        private void OpenSearchDialog()
        {
            var searchWin = new SearchMedicineWindow(_viewModel.AllMedicines.ToList());
            if (searchWin.ShowDialog() == true && searchWin.SelectedMedicine != null)
                _viewModel.AddToSale(searchWin.SelectedMedicine, 1);
        }

        private void RemoveItem_Click(object sender, RoutedEventArgs e)
        {
            Button btn = sender as Button;
            if (btn != null)
            {
                SaleItemViewModel item = btn.DataContext as SaleItemViewModel;
                if (item != null)
                    _viewModel.RemoveFromSale(item);
            }
        }

        private void btnPrintSave_Click(object sender, RoutedEventArgs e)
        {
            int result = _viewModel.SaveSale();
            if (result != -1)
            {
                Customer cust = new Customer()
                {
                    Name = string.IsNullOrEmpty(_viewModel.CashPartyName) ? "Walk-in Customer" : _viewModel.CashPartyName,
                    Contact = "N/A"
                };
                InvoiceWindow inv = new InvoiceWindow(cust, _viewModel.SaleItems, _viewModel, result);
                inv.ShowDialog();
            }
            else
            {
                MessageBox.Show("Failed to save sale. Ensure items are added and database is accessible.",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void btnSaveDraft_Click(object sender, RoutedEventArgs e)
        {
            int result = _viewModel.SaveSale("Draft");
            if (result != -1)
            {
                MessageBox.Show("Sale saved as Draft.", "Saved", MessageBoxButton.OK, MessageBoxImage.Information);
                // Reset POS
                _viewModel = new SaleViewModel();
                _viewModel.DocNo = "DOC-" + DateTime.Now.Ticks.ToString().Substring(10);
                this.DataContext = _viewModel;
            }
            else
            {
                MessageBox.Show("Failed to save draft. Ensure items are added and database is accessible.",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>Opens the Sale Return window and refreshes medicines stock after.</summary>
        private void btnSaleReturn_Click(object sender, RoutedEventArgs e)
        {
            var win = new SaleReturnWindow();
            win.Owner = Window.GetWindow(this);
            bool? result = win.ShowDialog();
            if (result == true)
            {
                // Reload medicines so stock counts are fresh
                _viewModel.LoadMedicines();
                MessageBox.Show(
                    "Sale return processed. Stock has been restored to inventory.",
                    "Return Complete", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
    }
}
