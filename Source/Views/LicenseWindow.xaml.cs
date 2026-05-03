using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using PharmaBilling.Source.Data;
using PharmaBilling;

namespace PharmaBilling.Source.Views
{
    public partial class LicenseWindow : Window
    {
        public LicenseWindow()
        {
            InitializeComponent();

            // Pre-fill last used key if exists
            string saved = LicenseManager.CurrentLicenseKey;
            if (!string.IsNullOrEmpty(saved))
                txtLicenseKey.Text = saved;

            // Auto-verify in background if we already have a saved key
            if (!string.IsNullOrEmpty(saved))
                AutoVerifyOnStartup(saved);
        }

        // ── Silent startup check ──────────────────────────────────────────────
        private async void AutoVerifyOnStartup(string key)
        {
            SetButtonState(verifying: true);
            lblStatus.Text = "Checking license...";
            lblStatus.Foreground = new SolidColorBrush(Colors.Gray);

            var result = await Task.Run(() => LicenseManager.VerifyLicense(key));

            if (result.IsValid)
            {
                lblStatus.Text = "✅ " + result.Message;
                lblStatus.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#27AE60"));
                await Task.Delay(800); // short pause so user can see "Verified"
                OpenMainApp();
            }
            else
            {
                // Key expired or invalid — let user see error and re-enter
                SetButtonState(verifying: false);
                ShowError("❌ " + result.Message);
            }
        }

        // ── Manual verify button ──────────────────────────────────────────────
        private async void btnActivate_Click(object sender, RoutedEventArgs e)
        {
            string key = txtLicenseKey.Text != null ? txtLicenseKey.Text.Trim() : "";

            if (string.IsNullOrEmpty(key))
            {
                ShowError("❌ Please enter your License Key.");
                return;
            }

            SetButtonState(verifying: true);

            var result = await Task.Run(() => LicenseManager.VerifyLicense(key));

            SetButtonState(verifying: false);

            if (result.IsValid)
            {
                lblStatus.Text  = "✅ License Verified! Welcome, " + result.ClientName;
                lblStatus.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#27AE60"));

                await Task.Delay(600);
                OpenMainApp();
            }
            else
            {
                ShowError("❌ " + result.Message);
            }
        }

        // ── Helpers ───────────────────────────────────────────────────────────
        private void OpenMainApp()
        {
            var main = new MainWindow();
            main.Show();
            this.Close();
        }

        private void ShowError(string msg)
        {
            lblStatus.Text = msg;
            lblStatus.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E74C3C"));
        }

        private void SetButtonState(bool verifying)
        {
            btnActivate.IsEnabled = !verifying;
            btnActivate.Content   = verifying ? "VERIFYING..." : "VERIFY SERVER LICENSE";
        }
    }
}
