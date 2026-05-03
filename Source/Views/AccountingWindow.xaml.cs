using System.Windows;
using PharmaBilling.Source.ViewModels;

namespace PharmaBilling.Source.Views
{
    public partial class AccountingWindow : Window
    {
        private AccountingViewModel _vm;
        public AccountingWindow()
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

