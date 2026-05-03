using System;
using System.Collections.ObjectModel;
using System.Data;
using System.Data.SQLite;
using System.Windows;
using PharmaBilling.Source.Data;
using PharmaBilling.Source.Models;

namespace PharmaBilling.Source.ViewModels
{
    public class MedicineViewModel : BaseViewModel
    {
        private DbHelper _db;
        private ObservableCollection<Medicine> _medicines;
        public ObservableCollection<Medicine> Medicines
        {
            get { return _medicines; }
            set { _medicines = value; OnPropertyChanged("Medicines"); }
        }

        public MedicineViewModel()
        {
            _db = new DbHelper();
            Medicines = new ObservableCollection<Medicine>();
            LoadMedicines();
        }

        public void LoadMedicines()
        {
            // Read from RAM cache — instant, no DB call
            System.Threading.Tasks.Task.Run(() =>
            {
                var tempList = AppCache.IsLoaded
                    ? new System.Collections.Generic.List<Medicine>(AppCache.Medicines)
                    : FetchFromDb();

                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    Medicines = new ObservableCollection<Medicine>(tempList);
                });
            });
        }

        private System.Collections.Generic.List<Medicine> FetchFromDb()
        {
            var list = new System.Collections.Generic.List<Medicine>();
            string sql = @"SELECT m.*, COALESCE((SELECT SUM(Quantity) FROM Stocks s WHERE s.MedicineID = m.MedicineID),0) as TotalStock 
                           FROM Medicines m 
                           ORDER BY m.MedicineID ASC";
            DataTable dt = _db.GetDataTable(sql);
            foreach (DataRow row in dt.Rows)
            {
                list.Add(new Medicine
                {
                    MedicineID    = Convert.ToInt32(row["MedicineID"]),
                    Name          = row["Name"].ToString(),
                    Type          = row["Type"].ToString(),
                    Category      = row["Category"].ToString(),
                    Manufacturer  = row["Manufacturer"].ToString(),
                    Unit          = row["Unit"].ToString(),
                    Barcode       = row.Table.Columns.Contains("Barcode") ? row["Barcode"].ToString() : "",
                    GenericFormula= row.Table.Columns.Contains("GenericFormula") ? row["GenericFormula"].ToString() : "",
                    WholesalePrice= row.Table.Columns.Contains("WholesalePrice") && row["WholesalePrice"] != DBNull.Value ? Convert.ToDouble(row["WholesalePrice"]) : 0,
                    PurchasePrice = Convert.ToDouble(row["PurchasePrice"]),
                    SalePrice     = Convert.ToDouble(row["SalePrice"]),
                    MinStock      = Convert.ToDouble(row["MinStock"]),
                    BoxSize       = row.Table.Columns.Contains("BoxSize") && row["BoxSize"] != DBNull.Value ? Convert.ToDouble(row["BoxSize"]) : 1,
                    TotalStock    = Convert.ToDouble(row["TotalStock"]),
                    Status        = row["Status"].ToString()
                });
            }
            return list;
        }

        public string AddMedicine(Medicine med)
        {
            try
            {
                // Prevent creating 97+ duplicates again
                string checkSql = "SELECT COUNT(*) FROM Medicines WHERE Name = @Name";
                int count = Convert.ToInt32(_db.ExecuteScalar(checkSql, new SQLiteParameter[] { new SQLiteParameter("@Name", med.Name) }));
                if (count > 0)
                {
                    return "A medicine with this name already exists.";
                }

                string sql = @"INSERT INTO Medicines (Name, Type, Category, Manufacturer, Unit, Barcode, GenericFormula, WholesalePrice, PurchasePrice, SalePrice, MinStock, BoxSize) 
                               VALUES (@Name, @Type, @Category, @Manufacturer, @Unit, @Barcode, @GenericFormula, @WholesalePrice, @PurchasePrice, @SalePrice, @MinStock, @BoxSize);
                               SELECT last_insert_rowid();";
                
                object result = _db.ExecuteScalar(sql, new SQLiteParameter[]
                {
                    new SQLiteParameter("@Name", med.Name),
                    new SQLiteParameter("@Type", med.Type),
                    new SQLiteParameter("@Category", med.Category),
                    new SQLiteParameter("@Manufacturer", med.Manufacturer),
                    new SQLiteParameter("@Unit", med.Unit),
                    new SQLiteParameter("@Barcode", med.Barcode ?? (object)DBNull.Value),
                    new SQLiteParameter("@GenericFormula", med.GenericFormula ?? (object)DBNull.Value),
                    new SQLiteParameter("@WholesalePrice", med.WholesalePrice),
                    new SQLiteParameter("@PurchasePrice", med.PurchasePrice),
                    new SQLiteParameter("@SalePrice", med.SalePrice),
                    new SQLiteParameter("@MinStock", med.MinStock),
                    new SQLiteParameter("@BoxSize", med.BoxSize > 0 ? med.BoxSize : 1)
                });

                int newId = Convert.ToInt32(result);

                if (med.TotalStock > 0)
                {
                    string stockSql = @"INSERT INTO Stocks (MedicineID, BatchNo, RackNo, Quantity, DateAdded) 
                                        VALUES (@MID, @Batch, @Box, @Qty, @Date)";
                    _db.ExecuteNonQuery(stockSql, new SQLiteParameter[] {
                        new SQLiteParameter("@MID", newId),
                        new SQLiteParameter("@Batch", string.IsNullOrEmpty(med.BatchNo) ? "Initial" : med.BatchNo),
                        new SQLiteParameter("@Box", string.IsNullOrEmpty(med.RackNo) ? "Rack-1" : med.RackNo),
                        new SQLiteParameter("@Qty", med.TotalStock),
                        new SQLiteParameter("@Date", DateTime.Now.ToString("yyyy-MM-dd"))
                    });
                }

                LoadMedicines();
                return null;
            }
            catch (Exception ex)
            {
                return "Database Error: " + ex.Message;
            }
        }

        public void UpdateMedicine(Medicine med)
        {
            string sql = @"UPDATE Medicines SET Name=@Name, Type=@Type, Category=@Category, 
                           Manufacturer=@Manufacturer, Unit=@Unit, Barcode=@Barcode, GenericFormula=@GenericFormula, 
                           WholesalePrice=@WholesalePrice, PurchasePrice=@PurchasePrice, 
                           SalePrice=@SalePrice, MinStock=@MinStock, BoxSize=@BoxSize WHERE MedicineID=@ID";
            
            _db.ExecuteNonQuery(sql, new SQLiteParameter[] {
                new SQLiteParameter("@Name", med.Name),
                new SQLiteParameter("@Type", med.Type),
                new SQLiteParameter("@Category", med.Category),
                new SQLiteParameter("@Manufacturer", med.Manufacturer),
                new SQLiteParameter("@Unit", med.Unit),
                new SQLiteParameter("@Barcode", med.Barcode ?? (object)DBNull.Value),
                new SQLiteParameter("@GenericFormula", med.GenericFormula ?? (object)DBNull.Value),
                new SQLiteParameter("@WholesalePrice", med.WholesalePrice),
                new SQLiteParameter("@PurchasePrice", med.PurchasePrice),
                new SQLiteParameter("@SalePrice", med.SalePrice),
                new SQLiteParameter("@MinStock", med.MinStock),
                new SQLiteParameter("@BoxSize", med.BoxSize > 0 ? med.BoxSize : 1),
                new SQLiteParameter("@ID", med.MedicineID)
            });

            // Adjust stock if changed during edit
            object stockObj = _db.ExecuteScalar("SELECT COALESCE(SUM(Quantity), 0) FROM Stocks WHERE MedicineID = " + med.MedicineID);
            double currentStock = stockObj != null && stockObj != DBNull.Value ? Convert.ToDouble(stockObj) : 0;
            double diff = med.TotalStock - currentStock;

            if (diff != 0)
            {
                string stockSql = @"INSERT INTO Stocks (MedicineID, BatchNo, RackNo, Quantity, DateAdded) 
                                    VALUES (@MID, @Batch, @Box, @Qty, @Date)";
                _db.ExecuteNonQuery(stockSql, new SQLiteParameter[] {
                    new SQLiteParameter("@MID", med.MedicineID),
                    new SQLiteParameter("@Batch", string.IsNullOrEmpty(med.BatchNo) ? "Adjustment" : med.BatchNo),
                    new SQLiteParameter("@Box", string.IsNullOrEmpty(med.RackNo) ? "Rack-1" : med.RackNo),
                    new SQLiteParameter("@Qty", diff),
                    new SQLiteParameter("@Date", DateTime.Now.ToString("yyyy-MM-dd"))
                });
            }

            LoadMedicines();
        }

        public string DeleteMedicine(int id)
        {
            try
            {
                // DEEP CHECK: Check if medicine is used in Sales or Purchases
                string checkSales = "SELECT COUNT(*) FROM SaleDetails WHERE MedicineID = @ID";
                int inSales = Convert.ToInt32(_db.ExecuteScalar(checkSales, new SQLiteParameter[] { new SQLiteParameter("@ID", id) }));
                
                string checkPurchases = "SELECT COUNT(*) FROM PurchaseDetails WHERE MedicineID = @ID";
                int inPurchases = Convert.ToInt32(_db.ExecuteScalar(checkPurchases, new SQLiteParameter[] { new SQLiteParameter("@ID", id) }));

                if (inSales > 0 || inPurchases > 0)
                {
                    // Soft Delete instead of hard crash
                    string softSql = "UPDATE Medicines SET Status = 'Inactive' WHERE MedicineID = @ID";
                    _db.ExecuteNonQuery(softSql, new SQLiteParameter[] { new SQLiteParameter("@ID", id) });
                    LoadMedicines();
                    return "Medicine is used in Sales/Purchase history. It has been marked as 'Inactive' instead of being deleted to preserve your records.";
                }

                // If no history, we can hard delete
                string sql = "DELETE FROM Medicines WHERE MedicineID = @ID";
                _db.ExecuteNonQuery(sql, new SQLiteParameter[] { new SQLiteParameter("@ID", id) });
                
                // Also clean up any orphan stock records if any
                _db.ExecuteNonQuery("DELETE FROM Stocks WHERE MedicineID = @ID", new SQLiteParameter[] { new SQLiteParameter("@ID", id) });
                
                LoadMedicines();
                return null;
            }
            catch (Exception ex)
            {
                return "Could not delete medicine: " + ex.Message;
            }
        }

        private void ExecuteMedicineQuery(string sql, Medicine med)
        {
            SQLiteParameter[] p = new SQLiteParameter[]
            {
                new SQLiteParameter("@Name", med.Name),
                new SQLiteParameter("@Type", med.Type),
                new SQLiteParameter("@Category", med.Category),
                new SQLiteParameter("@Manufacturer", med.Manufacturer),
                new SQLiteParameter("@Unit", med.Unit),
                new SQLiteParameter("@PurchasePrice", med.PurchasePrice),
                new SQLiteParameter("@SalePrice", med.SalePrice),
                new SQLiteParameter("@MinStock", med.MinStock)
            };

            _db.ExecuteNonQuery(sql, p);
            LoadMedicines();
        }
    }
}
