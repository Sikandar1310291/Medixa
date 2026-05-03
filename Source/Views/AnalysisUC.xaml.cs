using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using PharmaBilling.Source.ViewModels;

namespace PharmaBilling.Source.Views
{
    public partial class AnalysisUC : UserControl
    {
        private AnalysisViewModel _viewModel;

        public AnalysisUC()
        {
            InitializeComponent();
            _viewModel = new AnalysisViewModel();
            this.DataContext = _viewModel;
            UpdateActiveButton(btnToday);
        }

        private void Today_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.LoadTopMedicines("Today");
            UpdateActiveButton(btnToday);
        }

        private void Weekly_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.LoadTopMedicines("Weekly");
            UpdateActiveButton(btnWeekly);
        }

        private void Monthly_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.LoadTopMedicines("Monthly");
            UpdateActiveButton(btnMonthly);
        }

        private void UpdateActiveButton(Button activeBtn)
        {
            // Reset all
            btnToday.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#ECF0F1"));
            btnToday.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2C3E50"));
            
            btnWeekly.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#ECF0F1"));
            btnWeekly.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2C3E50"));
            
            btnMonthly.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#ECF0F1"));
            btnMonthly.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2C3E50"));

            // Set active
            activeBtn.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#3498DB"));
            activeBtn.Foreground = new SolidColorBrush(Colors.White);
        }

        public void RefreshData()
        {
            // Re-trigger the active tab's logic
            SolidColorBrush brush = btnToday.Background as SolidColorBrush;
            if (brush != null && brush.Color == (Color)ColorConverter.ConvertFromString("#3498DB"))
            {
                _viewModel.LoadTopMedicines("Today");
                return;
            }

            SolidColorBrush brushW = btnWeekly.Background as SolidColorBrush;
            if (brushW != null && brushW.Color == (Color)ColorConverter.ConvertFromString("#3498DB"))
            {
                _viewModel.LoadTopMedicines("Weekly");
                return;
            }

            _viewModel.LoadTopMedicines("Monthly");
        }
    }
}
