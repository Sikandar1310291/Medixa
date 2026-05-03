using System.Windows.Controls;
using System.Windows;
using PharmaBilling.Source.ViewModels;

namespace PharmaBilling.Source.Views
{
    public partial class AccountingUC : UserControl
    {
        private AccountingViewModel _vm;
        public AccountingUC()
        {
            InitializeComponent();
            _vm = new AccountingViewModel();
            this.DataContext = _vm;
        }

        private void Refresh_Click(object sender, RoutedEventArgs e)
        {
            _vm.LoadLedgers();
            _vm.LoadStatistics();
        }
    }
}


