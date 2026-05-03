using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using PharmaBilling.Source.Models;

namespace PharmaBilling.Source.Views
{
    public partial class SearchMedicineWindow : Window
    {
        private List<Medicine> _allMedicines;
        public Medicine SelectedMedicine { get; private set; }

        public SearchMedicineWindow(List<Medicine> medicines)
        {
            InitializeComponent();
            _allMedicines = medicines;
            dgMedicines.ItemsSource = _allMedicines;
            txtSearch.Focus();
        }

        private void txtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            FilterMedicines();
        }

        private void FilterMedicines()
        {
            string query = txtSearch.Text.Trim().ToLower();
            if (string.IsNullOrEmpty(query))
            {
                dgMedicines.ItemsSource = _allMedicines;
            }
            else
            {
                var filtered = _allMedicines.Where(m => 
                    (m.Name != null && m.Name.ToLower().Contains(query)) || 
                    (m.MedicineID.ToString().Contains(query)) ||
                    (m.Category != null && m.Category.ToLower().Contains(query)) ||
                    (m.GenericFormula != null && m.GenericFormula.ToLower().Contains(query)) ||
                    (m.Barcode != null && m.Barcode.Contains(query))
                ).ToList();
                dgMedicines.ItemsSource = filtered;
            }
        }

        private void btnOK_Click(object sender, RoutedEventArgs e)
        {
            SelectAndClose();
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void dgMedicines_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            SelectAndClose();
        }

        private void SelectAndClose()
        {
            Medicine med = dgMedicines.SelectedItem as Medicine;
            if (med != null)
            {
                SelectedMedicine = med;
                DialogResult = true;
                Close();
            }
        }

        private void btnFind_Click(object sender, RoutedEventArgs e)
        {
            FilterMedicines();
        }

        private void txtSearch_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Down)
            {
                if (dgMedicines.Items.Count > 0)
                {
                    e.Handled = true;
                    dgMedicines.Focus();
                    dgMedicines.SelectedIndex = 0;
                    dgMedicines.UpdateLayout(); // Ensure containers are generated
                    
                    // Ensure the row gets keyboard focus
                    var row = (DataGridRow)dgMedicines.ItemContainerGenerator.ContainerFromIndex(0);
                    if (row != null)
                    {
                        row.Focus();
                    }
                }
            }
            else if (e.Key == Key.Enter)
            {
                if (dgMedicines.Items.Count > 0)
                {
                    dgMedicines.SelectedIndex = 0;
                    SelectAndClose();
                }
                e.Handled = true;
            }
        }

        private void dgMedicines_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                SelectAndClose();
                e.Handled = true;
            }
            else if (e.Key == Key.Up && dgMedicines.SelectedIndex <= 0)
            {
                e.Handled = true;
                txtSearch.Focus();
                // Move cursor to end of text
                txtSearch.CaretIndex = txtSearch.Text.Length;
            }
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

