namespace PharmaBilling.Source.Data
{
    public static class AppSession
    {
        public static int CurrentUserID { get; set; }
        public static string CurrentUsername { get; set; }
        public static string CurrentRole { get; set; }
        
        static AppSession()
        {
            CurrentUserID = 0;
            CurrentUsername = string.Empty;
            CurrentRole = "Admin";
        }

        public static bool IsAdmin { get { return CurrentRole == "Admin"; } }
        public static bool IsCashier { get { return CurrentRole == "Cashier"; } }
    }
}
