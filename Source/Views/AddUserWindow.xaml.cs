using System.Windows;
using System.Windows.Controls;
using PharmaBilling.Source.Models;

namespace PharmaBilling.Source.Views
{
    public partial class AddUserWindow : Window
    {
        public User NewUser { get; private set; }
        public bool IsSaved { get; private set; }

        public AddUserWindow(User userToEdit = null)
        {
            IsSaved = false;
            InitializeComponent();

            if (userToEdit != null)
            {
                NewUser = userToEdit;
                txtUsername.Text = userToEdit.Username;
                txtPassword.Text = userToEdit.Password;
                
                if (userToEdit.Role == "Admin") cmbRole.SelectedIndex = 1;
                else cmbRole.SelectedIndex = 0;
            }
            else
            {
                NewUser = new User();
                cmbRole.SelectedIndex = 0; // Default to Cashier for Safety
            }
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtUsername.Text) || string.IsNullOrWhiteSpace(txtPassword.Text))
            {
                MessageBox.Show("Please enter a valid Username and Password.", "Missing Information", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            NewUser.Username = txtUsername.Text.Trim();
            NewUser.Password = txtPassword.Text.Trim();
            
            var selectedItem = cmbRole.SelectedItem as ComboBoxItem;
            NewUser.Role = selectedItem != null ? selectedItem.Content.ToString() : "Cashier";

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

