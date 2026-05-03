namespace PharmaBilling.Source.Models
{
    public class Customer
    {
        public int CustomerID { get; set; }
        public string Name { get; set; }
        public string Contact { get; set; }
        public string Email { get; set; }
        public string Address { get; set; }
        public double Balance { get; set; }
        public int LedgerID { get; set; }
    }
}
