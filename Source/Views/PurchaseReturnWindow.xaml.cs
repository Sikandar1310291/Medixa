using System.Windows;
using PharmaBilling.Source.ViewModels;

namespace PharmaBilling.Source.Views
{
    public partial class PurchaseReturnWindow : Window
    {
        private PurchaseReturnViewModel _vm;

        public PurchaseReturnWindow()
        {
            InitializeComponent();
            _vm = new PurchaseReturnViewModel();
            this.DataContext = _vm;
        }

        private void SaveReturn_Click(object sender, RoutedEventArgs e)
        {
            if (_vm.SelectedPurchase == null)
            {
                MessageBox.Show("Please select a purchase invoice first.", "Required", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            double total = _vm.TotalAmount;
            if (total <= 0)
            {
                MessageBox.Show("Please enter return quantity (> 0) for at least one item.", "No Items", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (MessageBox.Show(
                string.Format("Process purchase return of Rs. {0:N2}?\nStock will be deducted and ledger updated.", total),
                "Confirm Return", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                bool ok = _vm.SaveReturn();
                if (ok)
                {
                    MessageBox.Show("Purchase return processed successfully!\nStock adjusted and ledger posted.", 
                        "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                    this.DialogResult = true;
                    this.Close();
                }
                else
                {
                    MessageBox.Show("An error occurred while processing the return. Please try again.", 
                        "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }

        private void Window_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Escape)
            {
                if (cboPurchase.IsDropDownOpen)
                {
                    cboPurchase.IsDropDownOpen = false;
                }
                else
                {
                    this.Close();
                }
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

