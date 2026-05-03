using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Data.SQLite;
using System.Threading.Tasks;
using System.Windows;
using PharmaBilling.Source.Data;
using PharmaBilling.Source.Models;

namespace PharmaBilling.Source.ViewModels
{
    public class StaffViewModel : BaseViewModel
    {
        private DbHelper _db;

        private ObservableCollection<Staff> _staffList;
        public ObservableCollection<Staff> StaffList
        {
            get { return _staffList; }
            set { _staffList = value; OnPropertyChanged("StaffList"); }
        }

        public StaffViewModel()
        {
            _db = new DbHelper();
            StaffList = new ObservableCollection<Staff>();
            LoadStaff();
        }

        public void LoadStaff()
        {
            Task.Run(() =>
            {
                var temp = new List<Staff>();
                try
                {
                    DataTable dt = _db.GetDataTable("SELECT * FROM Staff");
                    foreach (DataRow row in dt.Rows)
                    {
                        temp.Add(new Staff
                        {
                            StaffID = Convert.ToInt32(row["StaffID"]),
                            Name = row["Name"].ToString(),
                            Designation = row["Designation"].ToString(),
                            Contact = row["Contact"].ToString(),
                            BaseSalary = Convert.ToDouble(row["BaseSalary"]),
                            LedgerID = Convert.ToInt32(row["LedgerID"])
                        });
                    }
                }
                catch { }

                Application.Current.Dispatcher.Invoke(() =>
                {
                    StaffList = new ObservableCollection<Staff>(temp);
                });
            });
        }

        public string AddStaff(Staff stf)
        {
            try
            {
                string ledgerName = "Staff: " + stf.Name + " (" + Guid.NewGuid().ToString().Substring(0, 4) + ")";
                string ls = "INSERT INTO Ledgers (Name, Type, Balance) VALUES (@Name, 'Expense', 0); SELECT last_insert_rowid();";
                int l_id = Convert.ToInt32(_db.ExecuteScalar(ls, new SQLiteParameter[] { new SQLiteParameter("@Name", ledgerName) }));

                string sql = @"INSERT INTO Staff (Name, Designation, Contact, BaseSalary, LedgerID) 
                               VALUES (@Name, @Desig, @Contact, @Salary, @LedgerID)";
                _db.ExecuteNonQuery(sql, new SQLiteParameter[] {
                    new SQLiteParameter("@Name", stf.Name),
                    new SQLiteParameter("@Desig", stf.Designation),
                    new SQLiteParameter("@Contact", stf.Contact),
                    new SQLiteParameter("@Salary", stf.BaseSalary),
                    new SQLiteParameter("@LedgerID", l_id)
                });
                LoadStaff();
                return null;
            }
            catch (Exception ex)
            {
                return "Failed to add staff: " + ex.Message;
            }
        }

        public void UpdateStaff(Staff stf)
        {
            string sql = @"UPDATE Staff SET Name=@Name, Designation=@Desig, Contact=@Contact, BaseSalary=@Salary 
                           WHERE StaffID=@ID";
            _db.ExecuteNonQuery(sql, new SQLiteParameter[] {
                new SQLiteParameter("@Name", stf.Name),
                new SQLiteParameter("@Desig", stf.Designation),
                new SQLiteParameter("@Contact", stf.Contact),
                new SQLiteParameter("@Salary", stf.BaseSalary),
                new SQLiteParameter("@ID", stf.StaffID)
            });
            LoadStaff();
        }

        public void ManagePayroll(Staff stf, double bonus, double deduction)
        {
            double payout = stf.BaseSalary + bonus - deduction;
            string desc = "Salary Payout - " + DateTime.Now.ToString("MMMM yyyy");

            string ls = @"INSERT INTO LedgerTransactions (LedgerID, Description, Debit, Credit, Balance)
                          VALUES (@LID, @Desc, @Amt, 0, (SELECT Balance + @Amt FROM Ledgers WHERE LedgerID = @LID))";
            _db.ExecuteNonQuery(ls, new SQLiteParameter[] {
                new SQLiteParameter("@LID", stf.LedgerID),
                new SQLiteParameter("@Desc", desc),
                new SQLiteParameter("@Amt", payout)
            });

            _db.ExecuteNonQuery("UPDATE Ledgers SET Balance = Balance + @Amt WHERE LedgerID = @LID",
                new SQLiteParameter[] { new SQLiteParameter("@Amt", payout), new SQLiteParameter("@LID", stf.LedgerID) });

            string prSql = @"INSERT INTO Payroll (StaffID, Month, AmountPaid, Bonuses, Deductions, DatePaid) 
                             VALUES (@SID, @Month, @Paid, @Bonus, @Ded, @Date)";
            _db.ExecuteNonQuery(prSql, new SQLiteParameter[] {
                new SQLiteParameter("@SID", stf.StaffID),
                new SQLiteParameter("@Month", DateTime.Now.ToString("MMMM yyyy")),
                new SQLiteParameter("@Paid", payout),
                new SQLiteParameter("@Bonus", bonus),
                new SQLiteParameter("@Ded", deduction),
                new SQLiteParameter("@Date", DateTime.Now.ToString("yyyy-MM-dd"))
            });
        }
    }
}
