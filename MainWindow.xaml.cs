using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ЧудоОбувь
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            Manager.MainFrame = MainFrame;
            MainFrame.Navigated += MainFrame_Navigated;

            MainFrame.Navigate(new LoginPage());
            UpdateUserInfo();
        }

        private void MainFrame_Navigated(object sender, NavigationEventArgs e)
        {
            BtnBack.Visibility = MainFrame.CanGoBack ? Visibility.Visible : Visibility.Collapsed;
            BtnAnalytics.Visibility = MainFrame.CanGoBack ? Visibility.Visible : Visibility.Collapsed;
            BtnOrders.Visibility = MainFrame.CanGoBack ? Visibility.Visible : Visibility.Collapsed;
            BtnCart.Visibility = MainFrame.CanGoBack ? Visibility.Visible : Visibility.Collapsed;

            if (e.Content is LoginPage)
            {
                UserInfoTB.Visibility = Visibility.Collapsed;
                
                // Очищаем историю навигации при возврате на страницу логина
                while (MainFrame.CanGoBack)
                {
                    MainFrame.RemoveBackEntry();
                }
                BtnBack.Visibility = Visibility.Collapsed;
                BtnAnalytics.Visibility = Visibility.Collapsed;
                BtnCart.Visibility = Visibility.Collapsed;
                BtnOrders.Visibility = Visibility.Collapsed;
            }
            else
            {
                UserInfoTB.Visibility = Visibility.Visible;
            }
        }

        public void UpdateUserInfo()
        {
            if (Manager.CurrentUser == null)
            {
                UserInfoTB.Text = "Гость";
            }
            else
            {
                string role = Manager.CurrentUser.Roles?.RoleName?.Trim();
                if (string.IsNullOrEmpty(role))
                {
                    if (Manager.CurrentUser.RoleID == 1) role = "Администратор";
                    else if (Manager.CurrentUser.RoleID == 2) role = "Менеджер";
                    else if (Manager.CurrentUser.RoleID == 3) role = "Авторизованный пользователь";
                }

                UserInfoTB.Text = $"{Manager.CurrentUser.UserSurname} {Manager.CurrentUser.UserName} ({role})";
            }

            if (Manager.IsGuest)
            {
                BtnCart.Visibility = Visibility.Collapsed;
                BtnOrders.Visibility = Visibility.Collapsed;
                BtnAnalytics.Visibility = Visibility.Collapsed;
            }
            else
            {
                BtnCart.Visibility = Visibility.Visible;
                BtnOrders.Visibility = (Manager.IsManager || Manager.IsAdmin) ? Visibility.Visible : Visibility.Collapsed;
                BtnAnalytics.Visibility = Manager.IsAdmin ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        private void BtnAnalytics_Click(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(new AnalyticsPage());
        }

        private void BtnCart_Click(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(new CartPage());
        }

        private void BtnOrders_Click(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(new OrdersPage());
        }

        private void BtnBack_Click(object sender, RoutedEventArgs e)
        {
            if (MainFrame.CanGoBack)
            {
                MainFrame.GoBack();
            }
        }
    }
}
