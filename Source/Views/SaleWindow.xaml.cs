using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using PharmaBilling.Source.ViewModels;
using PharmaBilling.Source.Models;
using PharmaBilling.Source.Data;
using System.Linq;

namespace PharmaBilling.Source.Views
{
    public partial class SaleWindow : Window
    {
        private SaleViewModel _viewModel;

        public SaleWindow()
        {
            InitializeComponent();
            _viewModel = new SaleViewModel();
            this.DataContext = _viewModel;
            
            // Generate a Doc #
            _viewModel.DocNo = "DOC-" + DateTime.Now.Ticks.ToString().Substring(10);
            
            // Shortcut for Search
            this.KeyDown += SaleWindow_KeyDown;
        }

        private void SaleWindow_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.F2 || e.Key == Key.Enter)
            {
                OpenSearchDialog();
            }
        }

        private void btnSearchItem_Click(object sender, RoutedEventArgs e)
        {
            OpenSearchDialog();
        }

        private void OpenSearchDialog()
        {
            var searchWin = new SearchMedicineWindow(_viewModel.AllMedicines.ToList());
            if (searchWin.ShowDialog() == true && searchWin.SelectedMedicine != null)
            {
                _viewModel.AddToSale(searchWin.SelectedMedicine, 1);
            }
        }

        private void RemoveItem_Click(object sender, RoutedEventArgs e)
        {
            Button btn = sender as Button;
            if (btn != null)
            {
                SaleItemViewModel item = btn.DataContext as SaleItemViewModel;
                if (item != null)
                {
                    _viewModel.RemoveFromSale(item);
                }
            }
        }

        private void btnPrintSave_Click(object sender, RoutedEventArgs e)
        {
            int result = _viewModel.SaveSale();
            if (result != -1)
            {
                // ── Notify Dashboard immediately ──────────────────────────
                AppEvents.OnSaleDataChanged();

                Customer cust = new Customer()
                {
                    Name = string.IsNullOrEmpty(_viewModel.CashPartyName) ? "Walk-in Customer" : _viewModel.CashPartyName,
                    Contact = "N/A"
                };
                InvoiceWindow inv = new InvoiceWindow(cust, _viewModel.SaleItems, _viewModel, result);
                inv.ShowDialog();
                this.Close();
            }
            else
            {
                MessageBox.Show("Failed to save sale. Ensure items are added and database is accessible.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnSaveDraft_Click(object sender, RoutedEventArgs e)
        {
            int result = _viewModel.SaveSale("Draft");
            if (result != -1)
            {
                AppEvents.OnSaleDataChanged();
                MessageBox.Show("Sale saved as Draft.", "Saved", MessageBoxButton.OK, MessageBoxImage.Information);
                this.Close();
            }
            else
            {
                MessageBox.Show("Failed to save draft. Ensure items are added and database is accessible.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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
