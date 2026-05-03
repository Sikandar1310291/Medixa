using System;

namespace PharmaBilling.Source.Models
{
    public class Stock
    {
        public int StockID { get; set; }
        public int MedicineID { get; set; }
        public string BatchNo { get; set; }
        public string RackNo { get; set; }
        public string ExpiryDate { get; set; }
        public double Quantity { get; set; }
        public int SupplierID { get; set; }
        public string DateAdded { get; set; }

        // Virtual properties for UI
        public string MedicineName { get; set; }
        public string ExpiryStatus
        {
            get
            {
                DateTime exp;
                if (DateTime.TryParse(ExpiryDate, out exp))
                {
                    if (exp < DateTime.Now) return "Expired";
                    if (exp < DateTime.Now.AddMonths(3)) return "Expiring Soon";
                    return "Good";
                }
                return "Unknown";
            }
        }
    }
}
