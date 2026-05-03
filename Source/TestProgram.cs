using System;
using PharmaBilling.Source.Data;
using PharmaBilling.Source.Models;
using PharmaBilling.Source.ViewModels;

namespace PharmaBilling.Test
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("PharmaSoft Database Test Utility");
            Console.WriteLine("===============================");
            
            try
            {
                MedicineViewModel medicineVM = new MedicineViewModel();
                Console.WriteLine("Database initialized successfully.");
                
                // Add a test medicine
                Medicine testMed = new Medicine
                {
                    Name = "Paracetamol 500mg",
                    Type = "Tab",
                    Category = "General",
                    Manufacturer = "PharmaCorp",
                    Unit = "Box",
                    PurchasePrice = 50,
                    SalePrice = 65,
                    MinStock = 100
                };
                
                medicineVM.AddMedicine(testMed);
                Console.WriteLine("Test Medicine added.");
                
                medicineVM.LoadMedicines();
                Console.WriteLine("Total Medicines in DB: " + medicineVM.Medicines.Count);
                
                foreach (var med in medicineVM.Medicines)
                {
                    Console.WriteLine("- " + med.Name + " (" + med.Type + ")");
                }
                
                Console.WriteLine("Test completed successfully!");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error during test: " + ex.Message);
            }
        }
    }
}
