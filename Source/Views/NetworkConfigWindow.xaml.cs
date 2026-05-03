using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Windows;
using PharmaBilling.Source.Data;
using PharmaBilling.Source.Network;

namespace PharmaBilling.Source.Views
{
    public partial class NetworkConfigWindow : Window
    {
        public NetworkConfigWindow()
        {
            InitializeComponent();
            LoadExistingConfig();
            DetectAndShowThisIP();
        }

        private void DetectAndShowThisIP()
        {
            try
            {
                string ip = GetLocalLanIP();
                txtThisIP.Text = string.IsNullOrEmpty(ip) ? "Could not detect (check Wi-Fi/LAN)" : ip;
            }
            catch
            {
                txtThisIP.Text = "Could not detect IP";
            }
        }

        private string GetLocalLanIP()
        {
            foreach (var ni in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (ni.OperationalStatus != OperationalStatus.Up) continue;
                if (ni.NetworkInterfaceType == NetworkInterfaceType.Loopback) continue;

                foreach (var addr in ni.GetIPProperties().UnicastAddresses)
                {
                    if (addr.Address.AddressFamily == AddressFamily.InterNetwork)
                    {
                        string ip = addr.Address.ToString();
                        if (ip.StartsWith("192.168.") || ip.StartsWith("10."))
                            return ip;
                    }
                }
            }
            return Dns.GetHostAddresses(Dns.GetHostName())
                      .Where(a => a.AddressFamily == AddressFamily.InterNetwork && !IPAddress.IsLoopback(a))
                      .Select(a => a.ToString())
                      .FirstOrDefault();
        }

        private void LoadExistingConfig()
        {
            var config = AppConfig.Current;
            if (config.IsServer)
                rbServer.IsChecked = true;
            else
                rbClient.IsChecked = true;

            txtServerIP.Text = config.ServerIP;
            txtSharedFolder.Text = config.SharedFolderName;
        }

        private void OperatingMode_Changed(object sender, RoutedEventArgs e)
        {
            if (rbClient == null || rbServer == null || brdClientSettings == null || brdServerSettings == null)
                return;

            if (rbClient.IsChecked == true)
            {
                brdClientSettings.Visibility = Visibility.Visible;
                brdServerSettings.Visibility = Visibility.Collapsed;
            }
            else
            {
                brdClientSettings.Visibility = Visibility.Collapsed;
                brdServerSettings.Visibility = Visibility.Visible;
                DetectAndShowThisIP();
            }
        }

        /// <summary>
        /// Tests if Medixa is running on the server PC in SERVER mode via HTTP ping.
        /// This replaces the old SMB/File.Exists check which was unreliable.
        /// </summary>
        private void TestConnection_Click(object sender, RoutedEventArgs e)
        {
            string ip = txtServerIP.Text.Trim();

            if (string.IsNullOrEmpty(ip))
            {
                MessageBox.Show("Please enter the Server IP address first.", "IP Required",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Ping the MedixaLanServer HTTP endpoint (not SMB)
            var client = new MedixaLanClient(ip);
            string error = client.Ping();

            if (error == null)
            {
                MessageBox.Show(
                    "\u2705 Connection Successful!\n\n" +
                    "Server: " + ip + " (Port: " + MedixaLanServer.Port + ")\n" +
                    "Status: Medixa Server is running \u2713\n\n" +
                    "Click 'Save Setup' to confirm this configuration.",
                    "Connection OK", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show(
                    "\u274c Cannot connect to Medixa Server at " + ip + "\n\n" +
                    "Error: " + error + "\n\n" +
                    "Please ensure:\n" +
                    "  1. Medixa is OPEN on the Server PC (IP: " + ip + ")\n" +
                    "  2. Server PC is in SERVER mode (Network Setup \u2192 SERVER \u2192 Save)\n" +
                    "  3. Both PCs are on the same Wi-Fi/LAN\n" +
                    "  4. Windows Firewall allows Port " + MedixaLanServer.Port + " on the Server PC",
                    "Connection Failed", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            if (rbClient.IsChecked == true && string.IsNullOrWhiteSpace(txtServerIP.Text))
            {
                MessageBox.Show("Please specify the Server IP address.", "Missing IP",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var newConfig = new NetworkSettings
            {
                IsServer = rbServer.IsChecked == true,
                ServerIP = txtServerIP.Text.Trim(),
                SharedFolderName = string.IsNullOrWhiteSpace(txtSharedFolder.Text) ? "Database" : txtSharedFolder.Text.Trim()
            };

            AppConfig.Save(newConfig);

            if (newConfig.IsServer)
            {
                AutoConfigureServerShare();
            }

            string myIP = GetLocalLanIP() ?? "(check ipconfig)";
            MessageBox.Show(
                "Network Configuration Saved!\n\n" +
                (newConfig.IsServer
                    ? "This PC is now the SERVER.\n" +
                      "Clients connect via HTTP (Port: " + MedixaLanServer.Port + ")\n" +
                      "Tell clients to use this IP: " + myIP
                    : "This PC is set as CLIENT.\n" +
                      "It will connect to: " + newConfig.ServerIP),
                "Setup Complete", MessageBoxButton.OK, MessageBoxImage.Information);
            this.Close();
        }

        private void AutoConfigureServerShare()
        {
            try
            {
                MessageBox.Show(
                    "Medixa will now configure your network automatically.\n" +
                    "Please click 'Yes' if Windows asks for Administrator permission.",
                    "Network Setup", MessageBoxButton.OK, MessageBoxImage.Information);

                // Open Port 5000 for the HTTP API server + register URL ACL
                string args = "/c " +
                    "netsh advfirewall firewall delete rule name=\"Medixa API Port 5000\" 2>nul & " +
                    "netsh advfirewall firewall add rule name=\"Medixa API Port 5000\" dir=in action=allow protocol=TCP localport=5000 & " +
                    "netsh http delete urlacl url=http://+:5000/ 2>nul & " +
                    "netsh http add urlacl url=http://+:5000/ user=Everyone";

                var psi = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = args,
                    Verb = "runas",
                    UseShellExecute = true,
                    WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden
                };
                var proc = System.Diagnostics.Process.Start(psi);
                if (proc != null) proc.WaitForExit();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Could not auto-configure network. Run Medixa as Administrator to set it up.\nError: " + ex.Message,
                    "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
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

