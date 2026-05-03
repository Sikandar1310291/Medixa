using System;
using System.Windows;
using PharmaBilling.Source.ViewModels;
using PharmaBilling.Source.Models;

namespace PharmaBilling.Source.Views
{
    public partial class CustomerWindow : Window
    {
        private CustomerViewModel _vm;
        public CustomerWindow()
        {
            InitializeComponent();
            _vm = new CustomerViewModel();
            this.DataContext = _vm;
        }

        private void AddCustomer_Click(object sender, RoutedEventArgs e)
        {
            AddCustomerWindow win = new AddCustomerWindow();
            win.Owner = this;
            win.ShowDialog();
            
            if (win.IsSaved)
            {
                string error;
                if (!_vm.SaveCustomer(win.Customer, out error))
                {
                    MessageBox.Show(error, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void EditCustomer_Click(object sender, RoutedEventArgs e)
        {
            if (_vm.SelectedCustomer == null)
            {
                MessageBox.Show("Please select a customer to edit.", "Selection Required", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            AddCustomerWindow win = new AddCustomerWindow(_vm.SelectedCustomer);
            win.Owner = this;
            win.ShowDialog();

            if (win.IsSaved)
            {
                string error;
                if (!_vm.SaveCustomer(win.Customer, out error))
                {
                    MessageBox.Show(error, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void DeleteCustomer_Click(object sender, RoutedEventArgs e)
        {
            if (_vm.SelectedCustomer == null)
            {
                MessageBox.Show("Please select a customer to delete.", "Selection Required", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (MessageBox.Show(string.Format("Are you sure you want to permanently delete \"{0}\"? This action cannot be undone.", _vm.SelectedCustomer.Name), "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
            {
                string error;
                if (!_vm.DeleteCustomer(_vm.SelectedCustomer.CustomerID, out error))
                {
                    MessageBox.Show(error, "Delete Failed", MessageBoxButton.OK, MessageBoxImage.Error);
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

