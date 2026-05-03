using System;

namespace PharmaBilling.Source.Models
{
    public class Medicine
    {
        public int MedicineID { get; set; }
        public string Name { get; set; }
        public string Type { get; set; }
        public string Category { get; set; }
        public string Manufacturer { get; set; }
        public string Unit { get; set; }
        public string Barcode { get; set; }
        public string GenericFormula { get; set; }
        public double WholesalePrice { get; set; }
        public double PurchasePrice { get; set; }
        public double SalePrice { get; set; }
        public double MinStock { get; set; }
        public double TotalStock { get; set; }
        public string Status { get; set; }
        
        // Temporary fields for AddMedicine
        public string BatchNo { get; set; }
        public string RackNo { get; set; }
        public double BoxSize { get; set; }
    }
}
