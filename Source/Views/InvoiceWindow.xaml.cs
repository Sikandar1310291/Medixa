using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Markup;
using System.Windows.Media;
using PharmaBilling.Source.Models;
using PharmaBilling.Source.ViewModels;
using PharmaBilling.Source.Data;

namespace PharmaBilling.Source.Views
{
    public partial class InvoiceWindow : Window
    {
        public InvoiceWindow(Customer customer, IEnumerable<SaleItemViewModel> items, SaleViewModel summary, int saleId)
        {
            try
            {
                InitializeComponent();

                // ── Pharmacy Header ──────────────────────────────────
                lblPharmacyName.Text    = ApiClient.Instance.PharmacyName;
                lblPharmacyTagline.Text = ApiClient.Instance.PharmacyTagline;

                lblPharmacyAddress.Text = ApiClient.Instance.PharmacyAddress;
                lblPharmacyContact.Text = "Tel: " + ApiClient.Instance.ContactInfo;
                lblPharmacyEmail.Text   = ApiClient.Instance.PharmacyEmail;

                if (string.IsNullOrWhiteSpace(ApiClient.Instance.PharmacyAddress) || ApiClient.Instance.PharmacyAddress == "N/A")
                    lblPharmacyAddress.Visibility = Visibility.Collapsed;
                if (string.IsNullOrWhiteSpace(ApiClient.Instance.ContactInfo) || ApiClient.Instance.ContactInfo == "N/A")
                    lblPharmacyContact.Visibility = Visibility.Collapsed;
                if (string.IsNullOrWhiteSpace(ApiClient.Instance.PharmacyEmail) || ApiClient.Instance.PharmacyEmail == "N/A")
                    lblPharmacyEmail.Visibility = Visibility.Collapsed;

                // ── Customer / Invoice Info ───────────────────────────
                lblCustomerName.Text = "Customer: " + customer.Name;
                lblContact.Text      = "Contact:  " + (string.IsNullOrWhiteSpace(customer.Contact) ? "N/A" : customer.Contact);
                lblInvoiceDate.Text  = DateTime.Now.ToString("dd-MMM-yy HH:mm");
                lblInvoiceID.Text    = "Invoice #" + saleId;

                // ── Items ─────────────────────────────────────────────
                lstItems.ItemsSource = items;

                // ── Totals: Read from DB using saleId (100% accurate) ─
                DbHelper db = new DbHelper();
                System.Data.DataTable saleRow = db.GetDataTable(
                    "SELECT TotalAmount, Discount, NetPaid FROM Sales WHERE SaleID = @ID",
                    new System.Data.SQLite.SQLiteParameter[] {
                        new System.Data.SQLite.SQLiteParameter("@ID", saleId)
                    });

                double grossTotal   = 0;
                double discPct      = 0;
                double grandTotal   = 0;

                if (saleRow != null && saleRow.Rows.Count > 0)
                {
                    grossTotal = Convert.ToDouble(saleRow.Rows[0]["TotalAmount"]);
                    discPct    = Convert.ToDouble(saleRow.Rows[0]["Discount"]);
                    grandTotal = Convert.ToDouble(saleRow.Rows[0]["NetPaid"]);
                }
                else
                {
                    // Fallback: sum items directly
                    foreach (var it in items)
                        grossTotal += it.TotalPrice;
                    discPct    = summary.DiscountPercent;
                    grandTotal = grossTotal - (grossTotal * discPct / 100.0);
                }

                lblGrossTotal.Text = string.Format("Rs. {0:N2}", grossTotal);
                lblDiscount.Text   = discPct.ToString("N0") + "%";
                lblGrandTotal.Text = string.Format("Rs. {0:N2}", grandTotal);

                // ── FBR ───────────────────────────────────────────────
                object fbrResult = db.ExecuteScalar(
                    "SELECT FBRInvoiceNo FROM Sales WHERE SaleID = @ID",
                    new System.Data.SQLite.SQLiteParameter[] {
                        new System.Data.SQLite.SQLiteParameter("@ID", saleId)
                    });
                lblFBR.Text = (fbrResult != null && fbrResult != DBNull.Value)
                    ? fbrResult.ToString()
                    : "NON-FBR System";
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error initializing invoice: " + ex.Message);
            }
        }

        private void btnPrint_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                PrintDialog pd = new PrintDialog();
                if (pd.ShowDialog() != true) return;

                // Hide buttons before printing
                btnPanel.Visibility = Visibility.Collapsed;

                try
                {
                    // Get the exact width the printer can print (for 58mm this is usually ~180-200)
                    double printableWidth = pd.PrintableAreaWidth;
                    
                    // We design the receipt for 180 WPF units baseline.
                    // Scale it to fit whatever printable width the driver reports.
                    double scale = printableWidth > 0 ? (printableWidth / 180.0) : 1.0;

                    // Remove fixed width constraints for printing
                    receiptPanel.Width = 180;
                    receiptPanel.HorizontalAlignment = HorizontalAlignment.Left;
                    receiptPanel.Margin = new Thickness(0);
                    
                    // Apply scale transform
                    receiptPanel.LayoutTransform = new ScaleTransform(scale, scale);

                    // Re-measure & arrange for the new scale
                    Size sz = new Size(printableWidth, double.PositiveInfinity);
                    receiptPanel.Measure(sz);
                    receiptPanel.Arrange(new Rect(new Point(0, 0), receiptPanel.DesiredSize));

                    // Print!
                    pd.PrintVisual(receiptPanel, "Pharmacy Receipt");
                    MessageBox.Show("Print job sent to printer!", "Success",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
                finally
                {
                    // Restore UI
                    receiptPanel.LayoutTransform = null;
                    receiptPanel.Width = 300; // Restore to window size
                    receiptPanel.HorizontalAlignment = HorizontalAlignment.Center;
                    receiptPanel.Margin = new Thickness(10, 15, 10, 10);
                    btnPanel.Visibility = Visibility.Visible;
                    
                    receiptPanel.InvalidateMeasure();
                    receiptPanel.InvalidateArrange();
                    UpdateLayout();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Printing failed: " + ex.Message, "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Window_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Escape)
                this.Close();
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
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

