using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using PharmaBilling.Source.Data;

namespace PharmaBilling.Source.Views
{
    public partial class BackupUC : UserControl
    {
        private string _dbPath;
        private string _backupDir;

        public BackupUC()
        {
            InitializeComponent();
            _dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "PharmaDB.sqlite");
            _backupDir = BackupService.GetLocalBackupFolder(_dbPath);
            Loaded += BackupUC_Loaded;
        }

        private void BackupUC_Loaded(object sender, RoutedEventArgs e)
        {
            RefreshUI();
        }

        private void RefreshUI()
        {
            var status = BackupService.GetStatus(_dbPath);
            txtLastBackup.Text = status.LatestBackupDate == "None" ? "No backup yet" : status.LatestBackupDate;
            txtTotalBackups.Text = status.TotalBackups + " snapshots";
            txtBackupPath.Text = _backupDir;

            LoadBackupList();
        }

        private void LoadBackupList()
        {
            var items = new List<BackupListItem>();

            if (Directory.Exists(_backupDir))
            {
                var files = Directory.GetFiles(_backupDir, "Medixa_Backup_*.sqlite")
                    .OrderByDescending(f => f)
                    .Take(30);

                foreach (var file in files)
                {
                    var info = new FileInfo(file);
                    string datePart = Path.GetFileNameWithoutExtension(file).Replace("Medixa_Backup_", "");
                    items.Add(new BackupListItem
                    {
                        Date = datePart,
                        FileName = info.Name,
                        Size = FormatSize(info.Length),
                        Location = "Local"
                    });
                }
            }

            // Check secondary drives too
            string[] drives = { "D:\\", "E:\\", "F:\\" };
            foreach (var drive in drives)
            {
                string secDir = Path.Combine(drive, "MedixaBackups");
                if (Directory.Exists(secDir))
                {
                    var secFiles = Directory.GetFiles(secDir, "Medixa_Backup_*.sqlite")
                        .OrderByDescending(f => f).Take(10);
                    foreach (var file in secFiles)
                    {
                        var info = new FileInfo(file);
                        string datePart = Path.GetFileNameWithoutExtension(file).Replace("Medixa_Backup_", "");
                        // Don't add duplicates
                        if (!items.Any(x => x.Date == datePart && x.Location == "Local"))
                        {
                            items.Add(new BackupListItem
                            {
                                Date = datePart,
                                FileName = info.Name,
                                Size = FormatSize(info.Length),
                                Location = drive.TrimEnd('\\')
                            });
                        }
                    }
                    break;
                }
            }

            dgBackups.ItemsSource = items.OrderByDescending(x => x.Date).ToList();
        }

        private void BtnBackupNow_Click(object sender, RoutedEventArgs e)
        {
            btnBackupNow.IsEnabled = false;
            btnBackupNow.Content = "⏳  Backing up...";

            System.Threading.Tasks.Task.Run(() =>
            {
                BackupService.RunBackupNow(_dbPath);
            }).ContinueWith(t =>
            {
                Dispatcher.Invoke(() =>
                {
                    btnBackupNow.IsEnabled = true;
                    btnBackupNow.Content = "✅  Done!";
                    RefreshUI();
                    System.Threading.Tasks.Task.Delay(2000).ContinueWith(_ =>
                        Dispatcher.Invoke(() => btnBackupNow.Content = "🔄  Backup Now"));
                });
            });
        }

        private void BtnOpenFolder_Click(object sender, RoutedEventArgs e)
        {
            if (!Directory.Exists(_backupDir))
                Directory.CreateDirectory(_backupDir);

            Process.Start("explorer.exe", _backupDir);
        }

        private string FormatSize(long bytes)
        {
            if (bytes > 1024 * 1024)
                return string.Format("{0:0.0} MB", bytes / (1024.0 * 1024));
            if (bytes > 1024)
                return string.Format("{0:0.0} KB", bytes / 1024.0);
            return bytes + " B";
        }
    }

    public class BackupListItem
    {
        public string Date { get; set; }
        public string FileName { get; set; }
        public string Size { get; set; }
        public string Location { get; set; }
    }
}
