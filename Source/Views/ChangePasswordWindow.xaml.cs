using System;
using System.Data.SQLite;
using System.Windows;
using PharmaBilling.Source.Data;

namespace PharmaBilling.Source.Views
{
    public partial class ChangePasswordWindow : Window
    {
        private DbHelper _db;

        public ChangePasswordWindow()
        {
            InitializeComponent();
            _db = new DbHelper();
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            string current = txtCurrentPass.Password;
            string newPass = txtNewPass.Password;
            string confirm = txtConfirmPass.Password;

            if (string.IsNullOrEmpty(current) || string.IsNullOrEmpty(newPass) || string.IsNullOrEmpty(confirm))
            {
                MessageBox.Show("Please fill all fields.");
                return;
            }

            if (newPass != confirm)
            {
                MessageBox.Show("New passwords do not match.");
                return;
            }

            // Verify current password (assuming admin for now)
            object result = _db.ExecuteScalar("SELECT Password FROM Users WHERE Username = 'admin'");
            if (result == null || result.ToString() != current)
            {
                MessageBox.Show("Incorrect current password.");
                return;
            }

            // Update password
            string sql = "UPDATE Users SET Password = @pass WHERE Username = 'admin'";
            SQLiteParameter[] p = { new SQLiteParameter("@pass", newPass) };
            _db.ExecuteNonQuery(sql, p);

            MessageBox.Show("Password changed successfully.");
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

