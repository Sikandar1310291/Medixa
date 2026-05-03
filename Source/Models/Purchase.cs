using System;

namespace PharmaBilling.Source.Models
{
    public class Purchase
    {
        public int PurchaseID { get; set; }
        public int SupplierID { get; set; }
        public string InvoiceNo { get; set; }
        public DateTime PurchaseDate { get; set; }
        public double TotalAmount { get; set; }
        public double Tax { get; set; }
        public string Status { get; set; }
    }

    public class PurchaseDetail
    {
        public int DetailID { get; set; }
        public int PurchaseID { get; set; }
        public int MedicineID { get; set; }
        public string BatchNo { get; set; }
        public DateTime ExpiryDate { get; set; }
        public double Quantity { get; set; }
        public double PurchasePrice { get; set; }
        public double TotalPrice { get; set; }
    }
}
