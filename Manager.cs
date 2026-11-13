using System.Windows.Controls;
using System.Collections.Generic;
using ЧудоОбувь;

namespace ЧудоОбувь
{
    public static class Manager
    {
        public static Frame MainFrame { get; set; }

        public static Users CurrentUser { get; set; }
        public static Dictionary<Stock, int> Cart { get; set; } = new Dictionary<Stock, int>();

        public static bool IsGuest => CurrentUser == null;
        public static bool IsAdmin => CurrentUser != null && CurrentUser.RoleID == 1;
        public static bool IsManager => CurrentUser != null && CurrentUser.RoleID == 2;
        public static bool IsUser => CurrentUser != null && CurrentUser.RoleID == 3;
    }
}