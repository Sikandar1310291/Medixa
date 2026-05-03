using System;

namespace PharmaBilling.Source.Models
{
    public class Staff
    {
        public int StaffID { get; set; }
        public string Name { get; set; }
        public string Designation { get; set; }
        public string Contact { get; set; }
        public double BaseSalary { get; set; }
        public int LedgerID { get; set; }
    }
}
