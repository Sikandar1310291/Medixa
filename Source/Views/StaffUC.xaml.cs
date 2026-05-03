using System;
using System.Windows;
using System.Windows.Controls;
using PharmaBilling.Source.Models;
using PharmaBilling.Source.ViewModels;

namespace PharmaBilling.Source.Views
{
    public partial class StaffUC : UserControl
    {
        private StaffViewModel _viewModel;

        public StaffUC()
        {
            InitializeComponent();
            _viewModel = new StaffViewModel();
            this.DataContext = _viewModel;
        }

        private void btnRefresh_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.LoadStaff();
        }

        private void btnAddStaff_Click(object sender, RoutedEventArgs e)
        {
            var win = new AddStaffWindow();
            win.Owner = Window.GetWindow(this);
            if (win.ShowDialog() == true)
            {
                var staff = win.NewStaff;
                string err = _viewModel.AddStaff(staff);
                if (err != null) MessageBox.Show(err, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                else MessageBox.Show("Staff added successfully. Payroll Ledgers connected.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void EditStaff_Click(object sender, RoutedEventArgs e)
        {
            Button btn = sender as Button;
            if (btn != null)
            {
                Staff staff = btn.DataContext as Staff;
                if (staff != null)
                {
                    var win = new AddStaffWindow(staff);
                    win.Owner = Window.GetWindow(this);
                    if (win.ShowDialog() == true)
                    {
                        _viewModel.UpdateStaff(win.NewStaff);
                        MessageBox.Show("Staff details updated.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
            }
        }

        private void IssueSalary_Click(object sender, RoutedEventArgs e)
        {
            Button btn = sender as Button;
            if (btn != null)
            {
                Staff staff = btn.DataContext as Staff;
                if (staff != null)
                {
                    // A simple prompt or window to confirm Bonus/Deduction
                    MessageBoxResult res = MessageBox.Show(string.Format("Are you sure you want to issue the exact base salary of Rs. {0:N2} to {1} for this month?\n\n(This will automatically deduct Cash and post to Staff Ledger)", staff.BaseSalary, staff.Name), "Issue Salary", MessageBoxButton.YesNo, MessageBoxImage.Question);
                    if (res == MessageBoxResult.Yes)
                    {
                        _viewModel.ManagePayroll(staff, 0, 0);
                        MessageBox.Show(string.Format("Salary issued to {0}. Accounts updated.", staff.Name), "Payroll Processed", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
            }
        }
    }
}
