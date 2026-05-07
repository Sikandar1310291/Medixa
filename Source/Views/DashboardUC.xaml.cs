using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using PharmaBilling.Source.Data;

namespace PharmaBilling.Source.Views
{
    // ── Data models ──────────────────────────────────────────────────────
    public class BarItem
    {
        public string DayLabel    { get; set; }
        public string AmountLabel { get; set; }
        public double BarHeight   { get; set; }
        public Brush  Color       { get; set; }
    }

    public class LowStockItem
    {
        public string Name  { get; set; }
        public string Stock { get; set; }
    }

    public class RecentSaleItem
    {
        public string InvoiceNo { get; set; }
        public string Name      { get; set; }
        public string Amount    { get; set; }
        public string Status    { get; set; }
    }

    public partial class DashboardUC : UserControl
    {
        private readonly DbHelper _db = new DbHelper();
        private DispatcherTimer _autoRefresh;

        public DashboardUC()
        {
            InitializeComponent();
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            RefreshDashboard();

            // Auto-refresh every 30 seconds as a safety net
            _autoRefresh = new DispatcherTimer { Interval = TimeSpan.FromSeconds(30) };
            _autoRefresh.Tick += (s, a) => RefreshDashboard();
            _autoRefresh.Start();

            // ── Real-time event subscriptions ────────────────────────────────
            // Refresh immediately whenever a sale or purchase is saved/deleted anywhere in the app
            PharmaBilling.Source.Data.AppEvents.SaleDataChanged     += (s, a) => Dispatcher.Invoke(RefreshDashboard);
            PharmaBilling.Source.Data.AppEvents.PurchaseDataChanged  += (s, a) => Dispatcher.Invoke(RefreshDashboard);
        }

        private void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            RefreshDashboard();
        }

        // ── MAIN LOAD ────────────────────────────────────────────────────────
        public void RefreshDashboard()
        {
            Task.Run(() =>
            {
                try
                {
                    // KPIs
                    string today   = DateTime.Now.ToString("yyyy-MM-dd");
                    double sales   = GetScalarDouble(string.Format("SELECT COALESCE(SUM(TotalAmount),0) FROM Sales WHERE date(SaleDate)='{0}'", today));
                    int    invCnt  = GetScalarInt("SELECT COUNT(*) FROM Sales WHERE date(SaleDate)='" + today + "'");
                    int    totMeds = GetScalarInt("SELECT COUNT(*) FROM Medicines WHERE Status != 'Inactive'");
                    int    lowSt   = GetScalarInt(@"SELECT COUNT(*) FROM Medicines m WHERE m.Status != 'Inactive'
                        AND COALESCE((SELECT SUM(s.Quantity) FROM Stocks s WHERE s.MedicineID=m.MedicineID),0) <= m.MinStock");
                    int    expired = GetScalarInt(@"SELECT COUNT(DISTINCT MedicineID) FROM Stocks
                        WHERE Quantity>0 AND ExpiryDate IS NOT NULL AND ExpiryDate!=''
                        AND length(ExpiryDate)>=7 AND date(ExpiryDate)<date('now', '+6 months')");

                    // Donut
                    int inStk  = GetScalarInt(@"SELECT COUNT(*) FROM Medicines m WHERE m.Status != 'Inactive'
                        AND COALESCE((SELECT SUM(Quantity) FROM Stocks s WHERE s.MedicineID=m.MedicineID),0) > m.MinStock");
                    int lowStk = GetScalarInt(@"SELECT COUNT(*) FROM Medicines m WHERE m.Status != 'Inactive'
                        AND COALESCE((SELECT SUM(Quantity) FROM Stocks s WHERE s.MedicineID=m.MedicineID),0) > 0
                        AND COALESCE((SELECT SUM(Quantity) FROM Stocks s WHERE s.MedicineID=m.MedicineID),0) <= m.MinStock");
                    int outStk = GetScalarInt(@"SELECT COUNT(*) FROM Medicines m WHERE m.Status != 'Inactive'
                        AND COALESCE((SELECT SUM(Quantity) FROM Stocks s WHERE s.MedicineID=m.MedicineID),0) = 0");

                    // Bar chart
                    List<BarItem> bars = BuildBarChart();

                    // Tables
                    List<RecentSaleItem>  recent   = LoadRecentSales();
                    List<LowStockItem>    lowAlerts = LoadLowStockAlerts();

                    Dispatcher.Invoke((Action)(() =>
                    {
                        // KPIs
                        lblDailySales.Text     = string.Format("Rs. {0:N0}", sales);
                        lblSalesToday.Text      = invCnt + " invoices today";
                        lblTotalMedicines.Text  = totMeds.ToString("N0");
                        lblLowStock.Text        = lowSt.ToString();
                        lblExpiredItems.Text    = expired.ToString();

                        // Bar chart
                        barChart.ItemsSource = bars;

                        // Donut
                        DrawDonut(inStk, lowStk, outStk);

                        // Tables
                        gridRecentSales.ItemsSource = recent;

                        txtLastRefresh.Text = "Last updated: " + DateTime.Now.ToString("hh:mm:ss tt");
                    }));
                }
                catch { /* silent */ }
            });
        }

        // ── HELPERS ──────────────────────────────────────────────────────────
        private double GetScalarDouble(string sql)
        {
            object r = _db.ExecuteScalar(sql);
            return (r != null && r != DBNull.Value) ? Convert.ToDouble(r) : 0;
        }

        private int GetScalarInt(string sql)
        {
            object r = _db.ExecuteScalar(sql);
            return (r != null && r != DBNull.Value) ? Convert.ToInt32(r) : 0;
        }

        // ── BAR CHART ────────────────────────────────────────────────────────
        private List<BarItem> BuildBarChart()
        {
            DataTable dt = _db.GetDataTable(@"
                SELECT date(SaleDate) as Day, COALESCE(SUM(TotalAmount),0) as Total
                FROM Sales
                WHERE date(SaleDate) >= date('now','-6 days')
                GROUP BY date(SaleDate)
                ORDER BY Day ASC");

            var lookup = new Dictionary<string, double>();
            foreach (DataRow row in dt.Rows)
                lookup[row["Day"].ToString()] = Convert.ToDouble(row["Total"]);

            var raw = new List<double>();
            var days = new List<string>();
            double maxVal = 1;

            for (int i = 6; i >= 0; i--)
            {
                DateTime d   = DateTime.Today.AddDays(-i);
                string   key = d.ToString("yyyy-MM-dd");
                double   val = lookup.ContainsKey(key) ? lookup[key] : 0;
                raw.Add(val);
                days.Add(i == 0 ? "Today" : d.ToString("ddd"));
                if (val > maxVal) maxVal = val;
            }

            double scale = 140.0 / maxVal;
            var palette = new Color[]
            {
                Color.FromRgb(147,197,253), Color.FromRgb(96,165,250),
                Color.FromRgb(59,130,246),  Color.FromRgb(37,99,235),
                Color.FromRgb(29,78,216),   Color.FromRgb(30,64,175),
                Color.FromRgb(34,197,94)    // today = green
            };

            var items = new List<BarItem>();
            for (int i = 0; i < raw.Count; i++)
            {
                var brush = new SolidColorBrush(palette[i % palette.Length]);
                brush.Freeze(); // REQUIRED: allow UI thread to access brush created on background thread
                items.Add(new BarItem
                {
                    DayLabel    = days[i],
                    AmountLabel = raw[i] > 0 ? string.Format("{0:N0}", raw[i]) : "",
                    BarHeight   = Math.Max(4, raw[i] * scale),
                    Color       = brush
                });
            }
            return items;
        }

        // ── DONUT CHART ──────────────────────────────────────────────────────
        private void DrawDonut(int inStock, int lowStock, int outOfStock)
        {
            donutCanvas.Children.Clear();

            lblInStock.Text        = "In Stock: "     + inStock;
            lblLowStockLegend.Text = "Low Stock: "    + lowStock;
            lblOutOfStock.Text     = "Out of Stock: " + outOfStock;

            int total = inStock + lowStock + outOfStock;
            if (total == 0)
            {
                DrawArc(80, 80, 60, 24, -90, 359.9, Color.FromRgb(229,231,235));
                return;
            }

            double start = -90;
            double inPct  = (double)inStock   / total * 360;
            double lowPct = (double)lowStock   / total * 360;
            double outPct = (double)outOfStock / total * 360;

            if (inStock    > 0) { DrawArc(80,80,60,24, start, inPct,  Color.FromRgb(34,197,94));  start += inPct;  }
            if (lowStock   > 0) { DrawArc(80,80,60,24, start, lowPct, Color.FromRgb(245,158,11)); start += lowPct; }
            if (outOfStock > 0) { DrawArc(80,80,60,24, start, outPct, Color.FromRgb(239,68,68));  }

            // Center text
            AddCanvasLabel(total.ToString(), 18, FontWeights.Bold, Color.FromRgb(26,26,46), 80, 80-12);
            AddCanvasLabel("total",          10, FontWeights.Normal, Color.FromRgb(156,163,175), 80, 80+8);
        }

        private void AddCanvasLabel(string text, double size, FontWeight weight, Color color, double cx, double cy)
        {
            var tb = new TextBlock
            {
                Text       = text,
                FontSize   = size,
                FontWeight = weight,
                Foreground = new SolidColorBrush(color)
            };
            tb.Measure(new Size(160, 40));
            Canvas.SetLeft(tb, cx - tb.DesiredSize.Width / 2);
            Canvas.SetTop(tb,  cy - tb.DesiredSize.Height / 2);
            donutCanvas.Children.Add(tb);
        }

        private void DrawArc(double cx, double cy, double outerR, double innerR,
                              double startAngle, double sweepAngle, Color color)
        {
            if (sweepAngle >= 360) sweepAngle = 359.99;
            Func<double, double> rad = deg => deg * Math.PI / 180.0;

            double s = rad(startAngle);
            double e = rad(startAngle + sweepAngle);
            bool   large = sweepAngle > 180;

            var p1 = new Point(cx + outerR * Math.Cos(s), cy + outerR * Math.Sin(s));
            var p2 = new Point(cx + outerR * Math.Cos(e), cy + outerR * Math.Sin(e));
            var p3 = new Point(cx + innerR * Math.Cos(e), cy + innerR * Math.Sin(e));
            var p4 = new Point(cx + innerR * Math.Cos(s), cy + innerR * Math.Sin(s));

            var fig = new PathFigure { StartPoint = p1, IsClosed = true, IsFilled = true };
            fig.Segments.Add(new ArcSegment(p2, new Size(outerR, outerR), 0, large, SweepDirection.Clockwise, true));
            fig.Segments.Add(new LineSegment(p3, true));
            fig.Segments.Add(new ArcSegment(p4, new Size(innerR, innerR), 0, large, SweepDirection.Counterclockwise, true));

            var geo = new PathGeometry();
            geo.Figures.Add(fig);

            donutCanvas.Children.Add(new System.Windows.Shapes.Path
            {
                Data            = geo,
                Fill            = new SolidColorBrush(color),
                Stroke          = Brushes.White,
                StrokeThickness = 2
            });
        }

        // ── RECENT SALES ────────────────────────────────────────────────────
        private List<RecentSaleItem> LoadRecentSales()
        {
            var list = new List<RecentSaleItem>();
            DataTable dt = _db.GetDataTable(@"
                SELECT s.SaleID, COALESCE(c.Name,'Walk-in') as Name, s.TotalAmount, s.Status
                FROM Sales s LEFT JOIN Customers c ON s.CustomerID=c.CustomerID
                ORDER BY s.SaleID DESC LIMIT 12");

            foreach (DataRow row in dt.Rows)
                list.Add(new RecentSaleItem
                {
                    InvoiceNo = "DOC-" + row["SaleID"],
                    Name      = row["Name"].ToString(),
                    Amount    = "Rs. " + Convert.ToDouble(row["TotalAmount"]).ToString("N2"),
                    Status    = row["Status"].ToString()
                });
            return list;
        }

        // ── LOW STOCK ALERTS ────────────────────────────────────────────────
        private List<LowStockItem> LoadLowStockAlerts()
        {
            var list = new List<LowStockItem>();
            DataTable dt = _db.GetDataTable(@"
                SELECT m.Name,
                       COALESCE((SELECT SUM(Quantity) FROM Stocks s WHERE s.MedicineID=m.MedicineID),0) as Qty
                FROM Medicines m
                WHERE m.Status != 'Inactive'
                AND COALESCE((SELECT SUM(Quantity) FROM Stocks s WHERE s.MedicineID=m.MedicineID),0) <= m.MinStock
                ORDER BY Qty ASC LIMIT 20");

            foreach (DataRow row in dt.Rows)
                list.Add(new LowStockItem
                {
                    Name  = row["Name"].ToString(),
                    Stock = row["Qty"].ToString()
                });
            return list;
        }
        // ── KPI CLICK HANDLERS ───────────────────────────────────────────────
        private void TotalMedicines_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var win = Window.GetWindow(this) as DashboardWindow;
            if (win != null)
            {
                win.NavigateTo("Medicines");
            }
        }

        private void LowStock_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            ReportWindow rep = new ReportWindow("LowStock");
            rep.ShowDialog();
        }

        private void ExpiredItems_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            ReportWindow rep = new ReportWindow("Expired");
            rep.ShowDialog();
        }
    }
}
