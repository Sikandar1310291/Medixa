using System.Windows;
using System.Windows.Controls;
using PharmaBilling.Source.Models;

namespace PharmaBilling.Source.Views
{
    public partial class AddCustomerWindow : Window
    {
        public Customer Customer { get; set; }
        public bool IsSaved { get; private set; }

        public AddCustomerWindow(Customer customer = null)
        {
            InitializeComponent();
            Customer = customer ?? new Customer();
            this.DataContext = Customer;

            this.Title = (customer != null && customer.CustomerID != 0)
                ? "Edit Customer: " + customer.Name
                : "Add New Customer";
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            // Force WPF to flush all in-progress bindings before reading values.
            // A TextBox that still has keyboard focus won't push its text back
            // to the bound property until it loses focus — we force that here.
            var beName = txtName.GetBindingExpression(TextBox.TextProperty);
            if (beName != null) beName.UpdateSource();
            
            var beContact = txtContact.GetBindingExpression(TextBox.TextProperty);
            if (beContact != null) beContact.UpdateSource();
            
            var beEmail = txtEmail.GetBindingExpression(TextBox.TextProperty);
            if (beEmail != null) beEmail.UpdateSource();
            
            var beAddress = txtAddress.GetBindingExpression(TextBox.TextProperty);
            if (beAddress != null) beAddress.UpdateSource();

            // Belt-and-suspenders: write directly into the model in case the
            // binding is one-time or DataContext was somehow replaced.
            Customer.Name    = txtName.Text.Trim();
            Customer.Contact = txtContact.Text.Trim();
            Customer.Email   = txtEmail.Text.Trim();
            Customer.Address = txtAddress.Text.Trim();

            if (string.IsNullOrEmpty(Customer.Name))
            {
                MessageBox.Show("Please enter a customer name.", "Validation Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                txtName.Focus();
                return;
            }

            IsSaved = true;
            this.Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            IsSaved = false;
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

