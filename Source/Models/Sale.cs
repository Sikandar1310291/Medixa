using System;

namespace PharmaBilling.Source.Models
{
    public class Sale
    {
        public int SaleID { get; set; }
        public int CustomerID { get; set; }
        public DateTime SaleDate { get; set; }
        public double TotalAmount { get; set; }
        public double Discount { get; set; }
        public double Tax { get; set; }
        public double NetPaid { get; set; }
        public string Status { get; set; } // Paid, Partial, Credit
        public string FBRInvoiceNo { get; set; }
    }

    public class SaleDetail
    {
        public int DetailID { get; set; }
        public int SaleID { get; set; }
        public int MedicineID { get; set; }
        public string BatchNo { get; set; }
        public string RackNo { get; set; }
        public double Quantity { get; set; }
        public double UnitPrice { get; set; }
        public double TotalPrice { get; set; }
    }
}
