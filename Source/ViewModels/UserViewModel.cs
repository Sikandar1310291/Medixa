using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Data.SQLite;
using System.Threading.Tasks;
using System.Windows;
using PharmaBilling.Source.Models;
using PharmaBilling.Source.Data;

namespace PharmaBilling.Source.ViewModels
{
    public class UserViewModel : BaseViewModel
    {
        private DbHelper _db;

        private ObservableCollection<User> _users;
        public ObservableCollection<User> Users
        {
            get { return _users; }
            set { _users = value; OnPropertyChanged("Users"); }
        }

        private User _selectedUser;
        public User SelectedUser
        {
            get { return _selectedUser; }
            set { _selectedUser = value; OnPropertyChanged("SelectedUser"); }
        }

        public UserViewModel()
        {
            _db = new DbHelper();
            Users = new ObservableCollection<User>();
            LoadUsers();
        }

        public void LoadUsers()
        {
            Task.Run(() =>
            {
                var temp = new List<User>();
                try
                {
                    DataTable dt = _db.GetDataTable("SELECT * FROM Users");
                    foreach (DataRow row in dt.Rows)
                    {
                        temp.Add(new User
                        {
                            UserID = Convert.ToInt32(row["UserID"]),
                            Username = row["Username"].ToString(),
                            Password = row["Password"].ToString(),
                            Role = row["Role"].ToString()
                        });
                    }
                }
                catch { }

                Application.Current.Dispatcher.Invoke(() =>
                {
                    Users = new ObservableCollection<User>(temp);
                });
            });
        }

        public string SaveUser(User usr)
        {
            try
            {
                if (usr.UserID == 0)
                {
                    var dtCount = _db.GetDataTable("SELECT COUNT(*) FROM Users WHERE Username=@u",
                        new SQLiteParameter[] { new SQLiteParameter("@u", usr.Username) });
                    if (Convert.ToInt32(dtCount.Rows[0][0]) > 0) return "Username already exists.";

                    string sql = "INSERT INTO Users (Username, Password, Role) VALUES (@u, @p, @r)";
                    _db.ExecuteNonQuery(sql, new SQLiteParameter[] {
                        new SQLiteParameter("@u", usr.Username),
                        new SQLiteParameter("@p", usr.Password),
                        new SQLiteParameter("@r", usr.Role)
                    });
                }
                else
                {
                    string sql = "UPDATE Users SET Username=@u, Password=@p, Role=@r WHERE UserID=@id";
                    _db.ExecuteNonQuery(sql, new SQLiteParameter[] {
                        new SQLiteParameter("@u", usr.Username),
                        new SQLiteParameter("@p", usr.Password),
                        new SQLiteParameter("@r", usr.Role),
                        new SQLiteParameter("@id", usr.UserID)
                    });
                }
                LoadUsers();
                return null;
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }

        public string DeleteUser(int id)
        {
            try
            {
                if (id == AppSession.CurrentUserID)
                    return "You cannot delete yourself.";
                _db.ExecuteNonQuery("DELETE FROM Users WHERE UserID=@id",
                    new SQLiteParameter[] { new SQLiteParameter("@id", id) });
                LoadUsers();
                return null;
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }
    }
}
