using System.Windows;
using System.Windows.Controls;
using PharmaBilling.Source.Models;
using PharmaBilling.Source.ViewModels;
using PharmaBilling.Source.Data;

namespace PharmaBilling.Source.Views
{
    public partial class UsersUC : UserControl
    {
        private UserViewModel _vm;

        public UsersUC()
        {
            InitializeComponent();
            _vm = new UserViewModel();
            this.DataContext = _vm;
        }

        private void AddUser_Click(object sender, RoutedEventArgs e)
        {
            if (AppSession.IsCashier)
            {
                MessageBox.Show("Access Denied: Only Admins can create new Users.", "Restricted", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            AddUserWindow win = new AddUserWindow(new User() { Role = "Cashier" }); // Default role
            win.Owner = Window.GetWindow(this);
            win.ShowDialog();

            if (win.IsSaved)
            {
                string error = _vm.SaveUser(win.NewUser);
                if (error != null)
                {
                    MessageBox.Show(error, "Error Saving User", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                else
                {
                    MessageBox.Show("User added successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
        }

        private void EditUser_Click(object sender, RoutedEventArgs e)
        {
            if (AppSession.IsCashier)
            {
                MessageBox.Show("Access Denied: Only Admins can modify Users.", "Restricted", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            Button btn = sender as Button;
            User usr = btn != null ? btn.DataContext as User : null;
            if (usr != null)
            {
                // Must clone otherwise the Datagrid row will update in real-time when typing in the popup, before Saving.
                var cloneProfile = new User { UserID = usr.UserID, Username = usr.Username, Password = usr.Password, Role = usr.Role };
                
                AddUserWindow win = new AddUserWindow(cloneProfile);
                win.Owner = Window.GetWindow(this);
                win.ShowDialog();

                if (win.IsSaved)
                {
                    string error = _vm.SaveUser(win.NewUser);
                    if (error != null)
                    {
                        MessageBox.Show(error, "Error Updating User", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                    else
                    {
                        MessageBox.Show("User credentials and role updated successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
            }
        }

        private void DeleteUser_Click(object sender, RoutedEventArgs e)
        {
            if (AppSession.IsCashier)
            {
                MessageBox.Show("Access Denied: Only Admins can delete Users.", "Restricted", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            Button btn = sender as Button;
            User usr = btn != null ? btn.DataContext as User : null;
            if (usr != null)
            {
                if (MessageBox.Show(string.Format("Are you sure you want to permanently delete user '{0}'?\nThey will instantly lose POS Network Terminal access.", usr.Username), "Confirm Deletion", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                {
                    string error = _vm.DeleteUser(usr.UserID);
                    if (error != null)
                    {
                        MessageBox.Show(error, "Delete Failed", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }
    }
}

