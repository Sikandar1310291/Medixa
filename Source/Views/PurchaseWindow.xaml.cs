using System.Windows;

namespace PharmaBilling.Source.Views
{
    public partial class PurchaseWindow : Window
    {
        private PharmaBilling.Source.ViewModels.PurchaseViewModel _vm;

        public PurchaseWindow()
        {
            InitializeComponent();
            _vm = new PharmaBilling.Source.ViewModels.PurchaseViewModel();
            this.DataContext = _vm;
        }

        private void AddPurchase_Click(object sender, RoutedEventArgs e)
        {
            AddPurchaseWindow addWin = new AddPurchaseWindow();
            addWin.Owner = this;
            addWin.ShowDialog();
            _vm.LoadPastPurchases(); // Refresh list after closing
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

