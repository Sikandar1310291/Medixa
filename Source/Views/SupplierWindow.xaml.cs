using System.Threading.Tasks;
using System.Windows;

namespace PharmaBilling.Source.Views
{
    public partial class SupplierWindow : Window
    {
        private PharmaBilling.Source.ViewModels.SupplierViewModel _vm;

        public SupplierWindow()
        {
            InitializeComponent();
            _vm = new PharmaBilling.Source.ViewModels.SupplierViewModel();
            this.DataContext = _vm;
        }

        private async void AddSupplier_Click(object sender, RoutedEventArgs e)
        {
            var newSupplier = new PharmaBilling.Source.Models.Supplier();
            AddSupplierWindow addWin = new AddSupplierWindow(newSupplier);
            addWin.Owner = this;
            addWin.ShowDialog();

            if (addWin.IsSaved)
            {
                string error = await _vm.SaveSupplierAsync(newSupplier);
                if (!string.IsNullOrEmpty(error))
                    MessageBox.Show(error, "Save Failed", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void EditSupplier_Click(object sender, RoutedEventArgs e)
        {
            if (_vm.SelectedSupplier == null)
            {
                MessageBox.Show("Please select a supplier to edit.", "Selection Required", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var editSupplier = new PharmaBilling.Source.Models.Supplier
            {
                SupplierID = _vm.SelectedSupplier.SupplierID,
                Name = _vm.SelectedSupplier.Name,
                Contact = _vm.SelectedSupplier.Contact,
                Email = _vm.SelectedSupplier.Email,
                Address = _vm.SelectedSupplier.Address,
                LedgerID = _vm.SelectedSupplier.LedgerID
            };

            AddSupplierWindow editWin = new AddSupplierWindow(editSupplier);
            editWin.Owner = this;
            editWin.Title = "Edit Supplier Details";
            editWin.ShowDialog();

            if (editWin.IsSaved)
            {
                string error = await _vm.SaveSupplierAsync(editSupplier);
                if (!string.IsNullOrEmpty(error))
                    MessageBox.Show(error, "Save Failed", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void DeleteSupplier_Click(object sender, RoutedEventArgs e)
        {
            if (_vm.SelectedSupplier == null)
            {
                MessageBox.Show("Please select a supplier to delete.", "Selection Required", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (MessageBox.Show(
                string.Format("Are you sure you want to permanently delete '{0}'?\nThis action cannot be undone.", _vm.SelectedSupplier.Name),
                "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
            {
                string error = await _vm.DeleteSupplierAsync(_vm.SelectedSupplier.SupplierID);
                if (!string.IsNullOrEmpty(error))
                    MessageBox.Show(error, "Delete Failed", MessageBoxButton.OK, MessageBoxImage.Error);
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

