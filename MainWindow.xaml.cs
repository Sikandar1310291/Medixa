using System;
using System.IO;
using System.Windows;
using PharmaBilling.Source.Views;
using PharmaBilling.Source.Data;
using System.Threading.Tasks;

namespace PharmaBilling
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            LoadApp();
        }

        private async void LoadApp()
        {
            // Fire backup silently in background (non-blocking)
            // This uses SQLite Online Backup API - safe even while DB is in use
            string dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "PharmaDB.sqlite");
            BackupService.StartAsync(dbPath);

            // Simulate splash loading
            await Task.Delay(2000);

            LoginWindow login = new LoginWindow();
            login.Show();
            this.Close();
        }
    }
}

