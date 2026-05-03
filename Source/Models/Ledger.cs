using System;

namespace PharmaBilling.Source.Models
{
    public class Ledger
    {
        public int LedgerID { get; set; }
        public string Name { get; set; }
        public string Type { get; set; } // Customer, Supplier, Cash, Bank, Expense, Income
        public double Balance { get; set; }
    }

    public class LedgerTransaction
    {
        public int TransactionID { get; set; }
        public int LedgerID { get; set; }
        public DateTime TransactionDate { get; set; }
        public string Description { get; set; }
        public double Debit { get; set; }
        public double Credit { get; set; }
        public double Balance { get; set; }
    }
}
