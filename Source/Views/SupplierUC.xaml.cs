using System.Windows.Controls;
using System.Windows;
using PharmaBilling.Source.Data;

namespace PharmaBilling.Source.Views
{
    public partial class SupplierUC : UserControl
    {
        private PharmaBilling.Source.ViewModels.SupplierViewModel _vm;

        public SupplierUC()
        {
            InitializeComponent();
            _vm = new PharmaBilling.Source.ViewModels.SupplierViewModel();
            this.DataContext = _vm;

            if (AppSession.IsCashier)
                spAdminActions.Visibility = Visibility.Collapsed;
        }

        private async void AddSupplier_Click(object sender, RoutedEventArgs e)
        {
            if (AppSession.IsCashier)
            {
                MessageBox.Show("Access Denied: You do not have permission to modify supplier records.", "Restricted", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var newSupplier = new PharmaBilling.Source.Models.Supplier();
            var addWin = new AddSupplierWindow(newSupplier);
            addWin.Owner = Window.GetWindow(this);
            addWin.ShowDialog();

            if (addWin.IsSaved)
            {
                SetBusy(true);
                string error = await _vm.SaveSupplierAsync(newSupplier);
                SetBusy(false);
                if (!string.IsNullOrEmpty(error))
                    MessageBox.Show(error, "Save Failed", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void EditSupplier_Click(object sender, RoutedEventArgs e)
        {
            if (AppSession.IsCashier)
            {
                MessageBox.Show("Access Denied: You do not have permission to modify supplier records.", "Restricted", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (_vm.SelectedSupplier == null)
            {
                MessageBox.Show("Please select a supplier to edit.", "Selection Required", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            // Clone so the DataGrid row doesn't mutate live while the user types
            var editSupplier = new PharmaBilling.Source.Models.Supplier
            {
                SupplierID = _vm.SelectedSupplier.SupplierID,
                Name    = _vm.SelectedSupplier.Name,
                Contact = _vm.SelectedSupplier.Contact,
                Email   = _vm.SelectedSupplier.Email,
                Address = _vm.SelectedSupplier.Address,
                LedgerID = _vm.SelectedSupplier.LedgerID
            };

            var editWin = new AddSupplierWindow(editSupplier);
            editWin.Owner = Window.GetWindow(this);
            editWin.Title = "Edit Supplier: " + editSupplier.Name;
            editWin.ShowDialog();

            if (editWin.IsSaved)
            {
                SetBusy(true);
                string error = await _vm.SaveSupplierAsync(editSupplier);
                SetBusy(false);
                if (!string.IsNullOrEmpty(error))
                    MessageBox.Show(error, "Save Failed", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void DeleteSupplier_Click(object sender, RoutedEventArgs e)
        {
            if (AppSession.IsCashier)
            {
                MessageBox.Show("Access Denied: You do not have permission to delete supplier records.", "Restricted", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (_vm.SelectedSupplier == null)
            {
                MessageBox.Show("Please select a supplier to delete.", "Selection Required", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var result = MessageBox.Show(
                string.Format("Are you sure you want to permanently delete '{0}'?\nThis action cannot be undone.", _vm.SelectedSupplier.Name),
                "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                int id = _vm.SelectedSupplier.SupplierID;
                SetBusy(true);
                string error = await _vm.DeleteSupplierAsync(id);
                SetBusy(false);
                if (!string.IsNullOrEmpty(error))
                    MessageBox.Show(error, "Delete Failed", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Search_TextChanged(object sender, TextChangedEventArgs e)
        {
            _vm.FilterSuppliers(txtSearch.Text);
        }

        private void SetBusy(bool busy)
        {
            spAdminActions.IsEnabled = !busy;
        }
    }
}
