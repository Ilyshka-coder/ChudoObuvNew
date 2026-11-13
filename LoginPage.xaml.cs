using System.Data.Entity;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using ЧудоОбувь;

namespace ЧудоОбувь
{
    public partial class LoginPage : Page
    {
        public LoginPage()
        {
            InitializeComponent();
        }

        private void LoginBtn_Click(object sender, RoutedEventArgs e)
        {
            string login = LoginTB.Text.Trim();

            if (string.IsNullOrEmpty(login))
            {
                ErrorTB.Text = "Введите логин";
                return;
            }

            try
            {
                var user = ChudoShoesEntities.GetContext().Users.Include("Roles")
                    .FirstOrDefault(u => u.UserLogin == login);

                if (user == null)
                {
                    ErrorTB.Text = "Пользователь не найден";
                    return;
                }

                Manager.CurrentUser = user;
                if (Window.GetWindow(this) is MainWindow mainWindow)
                {
                    mainWindow.UpdateUserInfo();
                }
                Manager.MainFrame.Navigate(new ProductPage());
            }
            catch (System.Exception ex)
            {
                ErrorTB.Text = "Ошибка БД: " + ex.Message;
            }
        }

        private void GuestBtn_Click(object sender, RoutedEventArgs e)
        {
            Manager.CurrentUser = null; // гость
            if (Window.GetWindow(this) is MainWindow mainWindow)
            {
                mainWindow.UpdateUserInfo();
            }
            Manager.MainFrame.Navigate(new ProductPage());
        }
    }
}