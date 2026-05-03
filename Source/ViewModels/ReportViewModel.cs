using System;
using System.Collections.ObjectModel;
using System.Data;
using System.Data.SQLite;
using PharmaBilling.Source.Data;
using PharmaBilling.Source.Models;

namespace PharmaBilling.Source.ViewModels
{
    public class ReportViewModel : BaseViewModel
    {
        private DbHelper _db;

        public ReportViewModel()
        {
            _db = new DbHelper();
        }

        public DataTable GetSalesReport(string period)
        {
            DataTable dt = new DataTable();
            dt.Columns.Add("Invoice_No");
            dt.Columns.Add("Customer_Name");
            dt.Columns.Add("Date");
            dt.Columns.Add("Amount");
            dt.Columns.Add("Discount");
            dt.Columns.Add("Tax");
            dt.Columns.Add("Paid");
            dt.Columns.Add("Status");
            dt.Columns.Add("Items");

            try
            {
                string baseSql = @"SELECT s.SaleID as [Invoice_No], c.Name as [Customer_Name], 
                                  s.SaleDate as [Date], s.TotalAmount as [Amount], 
                                  s.Discount, s.Tax, s.NetPaid as [Paid], s.Status,
                                  (SELECT GROUP_CONCAT(m.Name, ', ') FROM SaleDetails sd JOIN Medicines m ON sd.MedicineID = m.MedicineID WHERE sd.SaleID = s.SaleID) as [Items]
                                  FROM Sales s 
                                  LEFT JOIN Customers c ON s.CustomerID = c.CustomerID ";
                
                string whereClause = "";
                if (period == "Today")
                {
                    whereClause = "WHERE date(s.SaleDate) = date('now', 'localtime') ";
                }
                else if (period == "Month")
                {
                    whereClause = "WHERE date(s.SaleDate) >= date('now', 'start of month') ";
                }
                else if (period == "Last7Days")
                {
                    whereClause = "WHERE date(s.SaleDate) >= date('now', '-7 days') ";
                }
                else if (period == "Last30Days")
                {
                    whereClause = "WHERE date(s.SaleDate) >= date('now', '-30 days') ";
                }

                DataTable result = _db.GetDataTable(baseSql + whereClause + " ORDER BY s.SaleID DESC");
                if (result.Rows.Count > 0) return result;
                return dt;
            }
            catch (Exception ex)
            {
                dt.Rows.Add("ERROR", "Check Database", DateTime.Now, 0, 0, 0, 0, ex.Message);
                return dt;
            }
        }

        public DataTable GetPurchaseReport(string period)
        {
            DataTable dt = new DataTable();
            dt.Columns.Add("PurchaseID");
            dt.Columns.Add("Supplier");
            dt.Columns.Add("InvoiceNo");
            dt.Columns.Add("Date");
            dt.Columns.Add("Amount");
            dt.Columns.Add("Tax");
            dt.Columns.Add("Status");
            dt.Columns.Add("Items");

            try
            {
                string baseSql = @"SELECT p.PurchaseID, s.Name as [Supplier], p.InvoiceNo, 
                                  p.PurchaseDate as [Date], p.TotalAmount as [Amount], p.Tax, p.Status,
                                  (SELECT GROUP_CONCAT(m.Name, ', ') FROM PurchaseDetails pd JOIN Medicines m ON pd.MedicineID = m.MedicineID WHERE pd.PurchaseID = p.PurchaseID) as [Items]
                                  FROM Purchases p 
                                  LEFT JOIN Suppliers s ON p.SupplierID = s.SupplierID ";
                
                string whereClause = "";
                if (period == "Today")
                    whereClause = "WHERE date(p.PurchaseDate) = date('now', 'localtime') ";
                else if (period == "Month")
                    whereClause = "WHERE date(p.PurchaseDate) >= date('now', 'start of month') ";
                else if (period == "Last7Days")
                    whereClause = "WHERE date(p.PurchaseDate) >= date('now', '-7 days') ";
                else if (period == "Last30Days")
                    whereClause = "WHERE date(p.PurchaseDate) >= date('now', '-30 days') ";

                DataTable result = _db.GetDataTable(baseSql + whereClause + " ORDER BY p.PurchaseID DESC");
                if (result.Rows.Count > 0) return result;
                return dt;
            }
            catch (Exception ex)
            {
                dt.Rows.Add(0, "ERROR", ex.Message, DateTime.Now, 0, 0, "Fail");
                return dt;
            }
        }

        public DataTable GetStockReport()
        {
            DataTable dt = new DataTable();
            dt.Columns.Add("Medicine_Name");
            dt.Columns.Add("Type");
            dt.Columns.Add("Category");
            dt.Columns.Add("Manufacturer");
            dt.Columns.Add("Stock_Available");
            dt.Columns.Add("Min_Limit");

            try
            {
                string sql = @"SELECT m.Name as [Medicine_Name], m.Type, m.Category, m.Manufacturer,
                               COALESCE(SUM(s.Quantity), 0) as [Stock_Available], m.MinStock as [Min_Limit]
                               FROM Medicines m
                               LEFT JOIN Stocks s ON m.MedicineID = s.MedicineID
                               GROUP BY m.MedicineID
                               ORDER BY m.Name ASC";
                DataTable result = _db.GetDataTable(sql);
                if (result.Rows.Count > 0) return result;
                return dt;
            }
            catch (Exception ex)
            {
                dt.Rows.Add("ERROR", "DB Error", "", "", 0, ex.Message);
                return dt;
            }
        }

        public DataTable GetExpiredMedicineReport()
        {
            DataTable dt = new DataTable();
            dt.Columns.Add("Medicine_Name");
            dt.Columns.Add("BatchNo");
            dt.Columns.Add("Expiry");
            dt.Columns.Add("Qty");

            try
            {
                string sql = @"SELECT m.Name as [Medicine_Name], s.BatchNo, s.ExpiryDate as [Expiry], s.Quantity as [Qty]
                               FROM Stocks s
                               JOIN Medicines m ON s.MedicineID = m.MedicineID
                               WHERE date(s.ExpiryDate) <= date('now', '+1 month') AND s.Quantity > 0
                               ORDER BY s.ExpiryDate ASC";
                DataTable result = _db.GetDataTable(sql);
                if (result.Rows.Count > 0) return result;
                return dt;
            }
            catch (Exception ex)
            {
                dt.Rows.Add("ERROR", "DB Error", ex.Message, 0);
                return dt;
            }
        }

        public DataTable GetLowStockReport()
        {
            DataTable dt = new DataTable();
            dt.Columns.Add("Medicine_Name");
            dt.Columns.Add("Available");
            dt.Columns.Add("Limit");

            try
            {
                string sql = @"SELECT m.Name as [Medicine_Name], COALESCE(SUM(s.Quantity), 0) as [Available], m.MinStock as [Limit]
                               FROM Medicines m
                               LEFT JOIN Stocks s ON m.MedicineID = s.MedicineID
                               GROUP BY m.MedicineID
                               HAVING [Available] <= m.MinStock
                               ORDER BY [Available] ASC";
                DataTable result = _db.GetDataTable(sql);
                if (result.Rows.Count > 0) return result;
                return dt;
            }
            catch (Exception ex)
            {
                dt.Rows.Add("ERROR", 0, ex.Message);
                return dt;
            }
        }
    }
}
