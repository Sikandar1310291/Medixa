using System.Windows;
using System.Windows.Controls;
using PharmaBilling.Source.Data;

namespace PharmaBilling.Source.Views
{
    public partial class SettingsUC : UserControl
    {
        public SettingsUC()
        {
            InitializeComponent();
            LoadCurrentSettings();
        }

        private void LoadCurrentSettings()
        {
            var config = ApiClient.Instance;
            txtPharmacyName.Text = config.PharmacyName;
            txtTagline.Text = config.PharmacyTagline;
            txtContact.Text = config.ContactInfo;
            txtAddress.Text = config.PharmacyAddress;
            txtEmail.Text = config.PharmacyEmail;
        }

        private void SaveSettings_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtPharmacyName.Text))
            {
                MessageBox.Show("Pharmacy Name cannot be empty.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            ApiClient.Instance.SaveConfig(txtPharmacyName.Text, txtTagline.Text, txtContact.Text, txtAddress.Text, txtEmail.Text);
            
            MessageBox.Show("Pharmacy Profile updated successfully!", "Settings Saved", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}
