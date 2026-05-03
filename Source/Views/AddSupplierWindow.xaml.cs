using System.Windows;
using System.Windows.Controls;
using PharmaBilling.Source.Models;

namespace PharmaBilling.Source.Views
{
    public partial class AddSupplierWindow : Window
    {
        private Supplier _supplier;
        public bool IsSaved { get; private set; }

        public AddSupplierWindow(Supplier supplier)
        {
            InitializeComponent();
            _supplier = supplier;
            this.DataContext = supplier;
            IsSaved = false;
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            // Force WPF to commit any in-progress bindings before we read values.
            // Without this, TextBox that still has focus won't have pushed its
            // text back to the bound property yet.
            var beName = txtName.GetBindingExpression(TextBox.TextProperty);
            if (beName != null) beName.UpdateSource();
            
            var beContact = txtContact.GetBindingExpression(TextBox.TextProperty);
            if (beContact != null) beContact.UpdateSource();
            
            var beEmail = txtEmail.GetBindingExpression(TextBox.TextProperty);
            if (beEmail != null) beEmail.UpdateSource();
            
            var beAddress = txtAddress.GetBindingExpression(TextBox.TextProperty);
            if (beAddress != null) beAddress.UpdateSource();

            // Also write directly into the model as a belt-and-suspenders fallback
            // in case the binding is one-time or the DataContext was replaced.
            _supplier.Name    = txtName.Text.Trim();
            _supplier.Contact = txtContact.Text.Trim();
            _supplier.Email   = txtEmail.Text.Trim();
            _supplier.Address = txtAddress.Text.Trim();

            if (string.IsNullOrEmpty(_supplier.Name))
            {
                MessageBox.Show("Supplier Name is required.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                txtName.Focus();
                return;
            }

            IsSaved = true;
            this.Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
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

