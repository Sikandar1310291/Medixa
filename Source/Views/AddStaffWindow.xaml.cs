using System;
using System.Windows;
using PharmaBilling.Source.Models;

namespace PharmaBilling.Source.Views
{
    public partial class AddStaffWindow : Window
    {
        public Staff NewStaff { get; private set; }

        public AddStaffWindow()
        {
            InitializeComponent();
        }

        public AddStaffWindow(Staff existingStaff) : this()
        {
            NewStaff = existingStaff;
            txtTitle.Text = "Edit Staff";
            txtName.Text = existingStaff.Name;
            txtDesignation.Text = existingStaff.Designation;
            txtContact.Text = existingStaff.Contact;
            txtBaseSalary.Text = existingStaff.BaseSalary.ToString();
            btnSave.Content = "Update";
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtName.Text))
            {
                MessageBox.Show("Staff Name is required.");
                return;
            }

            if (NewStaff == null) NewStaff = new Staff();

            try
            {
                NewStaff.Name = txtName.Text.Trim();
                NewStaff.Designation = txtDesignation.Text.Trim();
                NewStaff.Contact = txtContact.Text.Trim();
                NewStaff.BaseSalary = string.IsNullOrWhiteSpace(txtBaseSalary.Text) ? 0 : Convert.ToDouble(txtBaseSalary.Text);

                this.DialogResult = true;
                this.Close();
            }
            catch (Exception)
            {
                MessageBox.Show("Invalid salary format.");
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

