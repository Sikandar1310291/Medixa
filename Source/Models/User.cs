using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace PharmaBilling.Source.Models
{
    public class User : INotifyPropertyChanged
    {
        private int _userID;
        private string _username;
        private string _password; // Managed carefully
        private string _role;

        public int UserID
        {
            get { return _userID; }
            set { _userID = value; OnPropertyChanged(); }
        }
        public string Username
        {
            get { return _username; }
            set { _username = value; OnPropertyChanged(); }
        }
        public string Password
        {
            get { return _password; }
            set { _password = value; OnPropertyChanged(); }
        }
        public string Role
        {
            get { return _role; }
            set { _role = value; OnPropertyChanged(); }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            if (PropertyChanged != null) PropertyChanged(this, new PropertyChangedEventArgs(name));
        }
    }
}

