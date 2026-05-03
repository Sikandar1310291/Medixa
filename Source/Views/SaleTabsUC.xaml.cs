using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace PharmaBilling.Source.Views
{
    public partial class SaleTabsUC : UserControl
    {
        private int _tabCounter = 1;

        public SaleTabsUC()
        {
            InitializeComponent();
            AddNewTab(); // Open the first tab by default
        }

        private void AddNewTab_Click(object sender, RoutedEventArgs e)
        {
            AddNewTab();
        }

        private void AddNewTab()
        {
            var tabItem = new TabItem();
            
            // Create a custom header for the tab
            var headerPanel = new StackPanel { Orientation = Orientation.Horizontal };
            
            var titleText = new TextBlock 
            { 
                Text = "Bill " + _tabCounter++, 
                Margin = new Thickness(0, 0, 10, 0),
                VerticalAlignment = VerticalAlignment.Center,
                FontWeight = FontWeights.Bold
            };
            
            var closeBtn = new Button 
            { 
                Content = "✕", 
                Foreground = Brushes.Red, 
                Background = Brushes.Transparent, 
                BorderThickness = new Thickness(0), 
                FontWeight = FontWeights.Bold,
                Padding = new Thickness(5, 0, 5, 0),
                Cursor = System.Windows.Input.Cursors.Hand,
                ToolTip = "Close Draft"
            };
            
            // Remove Tab Logic
            closeBtn.Click += (s, e) => 
            {
                MainTabControl.Items.Remove(tabItem);
                
                // Keep at least one tab open
                if (MainTabControl.Items.Count == 0)
                {
                    AddNewTab();
                }
            };
            
            headerPanel.Children.Add(titleText);
            headerPanel.Children.Add(closeBtn);
            
            tabItem.Header = headerPanel;
            
            // The content of the tab is a fresh new SaleUC! 
            // It gets its own distinct SaleViewModel so nothing clashes.
            tabItem.Content = new SaleUC();
            
            MainTabControl.Items.Add(tabItem);
            MainTabControl.SelectedItem = tabItem; // Switch to the new tab
        }
        /// <summary>
        /// Called every time the user switches to the Sales (POS) tab.
        /// Forces each open SaleUC bill to reload medicines/prices from the DB
        /// so that anything added via Purchases is immediately visible.
        /// </summary>
        public void RefreshAllTabs()
        {
            foreach (TabItem tab in MainTabControl.Items)
            {
                var saleUC = tab.Content as SaleUC;
                if (saleUC != null)
                {
                    var vm = saleUC.DataContext as ViewModels.SaleViewModel;
                    if (vm != null)
                        vm.LoadMedicines();
                }
            }
        }
    }
}
