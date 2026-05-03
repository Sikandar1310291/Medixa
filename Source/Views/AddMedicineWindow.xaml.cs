using System;
using System.Windows;
using PharmaBilling.Source.Models;
using PharmaBilling.Source.ViewModels;

namespace PharmaBilling.Source.Views
{
    public partial class AddMedicineWindow : Window
    {
        public Medicine NewMedicine { get; private set; }

        public AddMedicineWindow()
        {
            InitializeComponent();
        }

        public AddMedicineWindow(Medicine med) : this()
        {
            NewMedicine = med;
            txtTitle.Text = "Edit Medicine";
            txtName.Text = med.Name;
            cmbType.Text = med.Type;
            txtCategory.Text = med.Category;
            txtManufacturer.Text = med.Manufacturer;
            txtPurchasePrice.Text = med.PurchasePrice.ToString();
            txtWholesalePrice.Text = med.WholesalePrice.ToString();
            txtSalePrice.Text = med.SalePrice.ToString();
            txtMinStock.Text = med.MinStock.ToString();
            txtInitialStock.Text = med.TotalStock.ToString();
            txtBarcode.Text = med.Barcode;
            txtFormula.Text = med.GenericFormula;
            txtBatchNo.Text = string.IsNullOrEmpty(med.BatchNo) ? "BATCH-001" : med.BatchNo;
            txtRackNo.Text = string.IsNullOrEmpty(med.RackNo) ? "Rack-1" : med.RackNo;
            txtBoxSize.Text = med.BoxSize > 0 ? med.BoxSize.ToString() : "10";
            btnSave.Content = "Update";
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(txtName.Text))
            {
                MessageBox.Show("Please enter medicine name.");
                return;
            }

            try
            {
                if (NewMedicine == null) NewMedicine = new Medicine();

                NewMedicine.Name = txtName.Text;
                NewMedicine.Type = cmbType.Text;
                NewMedicine.Category = txtCategory.Text;
                NewMedicine.Manufacturer = txtManufacturer.Text;
                NewMedicine.Unit = "Piece";
                NewMedicine.Barcode = txtBarcode.Text != null ? txtBarcode.Text.Trim() : "";
                NewMedicine.GenericFormula = txtFormula.Text != null ? txtFormula.Text.Trim() : "";
                NewMedicine.PurchasePrice = string.IsNullOrEmpty(txtPurchasePrice.Text) ? 0 : Convert.ToDouble(txtPurchasePrice.Text);
                NewMedicine.WholesalePrice = string.IsNullOrEmpty(txtWholesalePrice.Text) ? 0 : Convert.ToDouble(txtWholesalePrice.Text);
                NewMedicine.SalePrice = string.IsNullOrEmpty(txtSalePrice.Text) ? 0 : Convert.ToDouble(txtSalePrice.Text);
                NewMedicine.MinStock = string.IsNullOrEmpty(txtMinStock.Text) ? 10 : Convert.ToInt32(txtMinStock.Text);
                NewMedicine.TotalStock = string.IsNullOrEmpty(txtInitialStock.Text) ? 0 : Convert.ToInt32(txtInitialStock.Text);
                NewMedicine.BatchNo = txtBatchNo.Text.Trim();
                NewMedicine.RackNo = txtRackNo.Text.Trim();
                NewMedicine.BoxSize = string.IsNullOrEmpty(txtBoxSize.Text) ? 1 : Convert.ToInt32(txtBoxSize.Text);

                this.DialogResult = true;
                this.Close();
            }
            catch (Exception)
            {
                MessageBox.Show("Invalid input format. Please check prices and stock.");
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
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

