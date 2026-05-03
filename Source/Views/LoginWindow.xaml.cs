using System;
using System.Windows;
using System.Data.SQLite;
using PharmaBilling.Source.Data;
using PharmaBilling.Source.Network;

namespace PharmaBilling.Source.Views
{
    public partial class LoginWindow : Window
    {
        public LoginWindow()
        {
            InitializeComponent();
        }

        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string user = txtUsername.Text.Trim();
                string pass = txtPassword.Password;

                if (string.IsNullOrEmpty(user) || string.IsNullOrEmpty(pass))
                {
                    MessageBox.Show("Please enter username and password.", "Missing Fields",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // ── PRE-FLIGHT CHECK (CLIENT MODE ONLY) ─────────────────────────
                // Verify the Medixa LAN Server is reachable via HTTP before attempting
                // database access. In SERVER mode this is skipped (local access, always OK).
                var config = AppConfig.Current;
                if (!config.IsServer)
                {
                    var client = new MedixaLanClient(config.ServerIP);
                    string pingError = client.Ping();
                    if (pingError != null)
                    {
                        MessageBox.Show(
                            "Cannot connect to the Server PC.\n\n" +
                            "Medixa tried to reach: http://" + config.ServerIP + ":" + MedixaLanServer.Port + "\n\n" +
                            "Please check:\n" +
                            "  1. Server PC IP is correct (currently: " + config.ServerIP + ")\n" +
                            "  2. Medixa is OPEN on the Server PC in SERVER mode\n" +
                            "  3. Server PC ran 'Network Setup \u2192 SERVER \u2192 Save Setup'\n" +
                            "  4. Both PCs are on the same Wi-Fi/LAN\n" +
                            "  5. Port " + MedixaLanServer.Port + " is allowed through Server's Firewall\n\n" +
                            "To update the Server IP: click 'Network Setup' below.\n\n" +
                            "[Error: " + pingError + "]",
                            "Server Unreachable", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }
                }

                // ── AUTHENTICATE ─────────────────────────────────────────────────
                DbHelper db = new DbHelper();
                string sql = "SELECT UserID, Username, Role FROM Users WHERE Username = @u AND Password = @p";
                SQLiteParameter[] p = {
                    new SQLiteParameter("@u", user),
                    new SQLiteParameter("@p", pass)
                };

                var dt = db.GetDataTable(sql, p);

                if (dt.Rows.Count > 0)
                {
                    var row = dt.Rows[0];
                    AppSession.CurrentUserID = Convert.ToInt32(row["UserID"]);
                    AppSession.CurrentUsername = row["Username"].ToString();
                    AppSession.CurrentRole = row["Role"].ToString();

                    DashboardWindow dashboard = new DashboardWindow();
                    dashboard.Show();
                    this.Close();
                }
                else
                {
                    MessageBox.Show("Incorrect username or password.", "Login Failed",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Login Error: " + ex.Message, "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExitButton_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void NetworkSetupButton_Click(object sender, RoutedEventArgs e)
        {
            var configWindow = new NetworkConfigWindow();
            configWindow.ShowDialog();
            AppConfig.Reload();
        }
    }
}
