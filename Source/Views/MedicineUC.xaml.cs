using System.Windows.Controls;
using System;
using System.Windows;
using PharmaBilling.Source.ViewModels;
using PharmaBilling.Source.Models;
using PharmaBilling.Source.Data;

namespace PharmaBilling.Source.Views
{
    public partial class MedicineUC : UserControl
    {
        private MedicineViewModel _viewModel;

        public MedicineUC()
        {
            InitializeComponent();
            _viewModel = new MedicineViewModel();
            this.DataContext = _viewModel;
            
            if (AppSession.IsCashier)
            {
                spAdminActions.Visibility = Visibility.Collapsed;
            }
        }

        private void AddMedicine_Click(object sender, RoutedEventArgs e)
        {
            AddMedicineWindow addWin = new AddMedicineWindow();
            addWin.Owner = Window.GetWindow(this);
            if (addWin.ShowDialog() == true)
            {
                string error = _viewModel.AddMedicine(addWin.NewMedicine);
                if (error != null)
                {
                    MessageBox.Show(error, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                else
                {
                    MessageBox.Show("Medicine added successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
        }

        private void EditMedicine_Click(object sender, RoutedEventArgs e)
        {
            if (AppSession.IsCashier)
            {
                MessageBox.Show("Access Denied: You do not have permission to modify medicine records. Please contact Admin.", "Restricted", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            
            var btn = (System.Windows.Controls.Button)sender;
            var med = (Medicine)btn.DataContext;

            AddMedicineWindow editWin = new AddMedicineWindow(med);
            editWin.Owner = Window.GetWindow(this);
            if (editWin.ShowDialog() == true)
            {
                _viewModel.UpdateMedicine(editWin.NewMedicine);
                MessageBox.Show("Medicine updated successfully!");
            }
        }

        private void DeleteMedicine_Click(object sender, RoutedEventArgs e)
        {
            if (AppSession.IsCashier)
            {
                MessageBox.Show("Access Denied: You do not have permission to delete medicine records. Please contact Admin.", "Restricted", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var btn = (System.Windows.Controls.Button)sender;
            var med = (Medicine)btn.DataContext;

            if (MessageBox.Show("Are you sure you want to delete " + med.Name + "?", "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
            {
                string result = _viewModel.DeleteMedicine(med.MedicineID);
                if (result != null)
                {
                    // This handles both errors and soft-delete notifications
                    MessageBox.Show(result, "Notice", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show("Medicine deleted successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
        }

        private void ImportExcel_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog openFile = new Microsoft.Win32.OpenFileDialog();
            openFile.Filter = "CSV Files (*.csv)|*.csv|All Files (*.*)|*.*";
            openFile.Title = "Select Medicine Data CSV File";

            if (openFile.ShowDialog() == true)
            {
                try
                {
                    string[] lines = System.IO.File.ReadAllLines(openFile.FileName);
                    int count = 0;
                    
                    // Skip header if it exists (assuming first line is header if it contains 'Name')
                    int startLine = (lines.Length > 0 && lines[0].ToLower().Contains("name")) ? 1 : 0;

                    for (int i = startLine; i < lines.Length; i++)
                    {
                        if (string.IsNullOrWhiteSpace(lines[i])) continue;

                        string[] parts = lines[i].Split(',');
                        if (parts.Length >= 2) // Basic requirement: Name, Type
                        {
                            double pp = 0;
                            double sp = 0;
                            int ms = 10;

                            if (parts.Length > 4) double.TryParse(parts[4], out pp);
                            if (parts.Length > 5) double.TryParse(parts[5], out sp);
                            if (parts.Length > 6) int.TryParse(parts[6], out ms);

                            Medicine m = new Medicine
                            {
                                Name = parts[0].Trim(),
                                Type = parts.Length > 1 ? parts[1].Trim() : "Tablet",
                                Category = parts.Length > 2 ? parts[2].Trim() : "",
                                Manufacturer = parts.Length > 3 ? parts[3].Trim() : "",
                                Unit = "Piece",
                                PurchasePrice = pp,
                                SalePrice = sp,
                                MinStock = ms
                            };
                            _viewModel.AddMedicine(m);
                            count++;
                        }
                    }
                    MessageBox.Show(count + " medicines imported successfully!", "Import Complete");
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error importing file: " + ex.Message, "Import Error");
                }
            }
        }

        private void Search_Click(object sender, RoutedEventArgs e)
        {
            FilterMedicines();
        }

        private void txtSearch_KeyUp(object sender, System.Windows.Input.KeyEventArgs e)
        {
            FilterMedicines();
        }

        private void FilterMedicines()
        {
            string query = txtSearch.Text.ToLower();
            System.ComponentModel.ICollectionView view = System.Windows.Data.CollectionViewSource.GetDefaultView(_viewModel.Medicines);
            if (view != null)
            {
                view.Filter = item =>
                {
                    if (string.IsNullOrEmpty(query)) return true;
                    var m = item as Medicine;
                    return m != null && (
                        (m.Name != null && m.Name.ToLower().Contains(query)) ||
                        (m.Type != null && m.Type.ToLower().Contains(query)) ||
                        (m.Category != null && m.Category.ToLower().Contains(query))
                    );
                };
            }
        }
    }
}


