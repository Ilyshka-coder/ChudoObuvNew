using System;
using System.Collections.Generic;
using System.Linq;
using System.Data.Entity;
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
using ЧудоОбувь;

namespace ЧудоОбувь
{
    /// <summary>
    /// Логика взаимодействия для ProductPage.xaml
    /// </summary>
    public partial class ProductPage : Page
    {
        public ProductPage()
        {
            InitializeComponent();
           
            FiltCB.SelectedIndex = 0;
            SortCB.SelectedIndex = 0;

            if (Manager.IsGuest)
            {
                SearchPanel.Visibility = Visibility.Collapsed;
                FilterPanel.Visibility = Visibility.Collapsed;
                SortPanel.Visibility = Visibility.Collapsed;
            }
            UpdateProducts();
        }
        private void UpdateProducts()
        {
            var currentProducts = ChudoShoesEntities.GetContext().Products.Include("Stock").ToList();

            

            // filt
            if (FiltCB.SelectedIndex == 1)
                currentProducts = currentProducts.Where(p => p.CategoryID == 1).ToList();
            if (FiltCB.SelectedIndex == 2)
                currentProducts = currentProducts.Where(p => p.CategoryID == 2).ToList();
            if (FiltCB.SelectedIndex == 3)
                currentProducts = currentProducts.Where(p => p.CategoryID == 3).ToList();

            // search
            string searchText = (SearchTB.Text ?? "").ToLower();
            currentProducts = currentProducts
                .Where(p => (p.ProductName ?? "").ToLower().Contains(searchText))
                .ToList();

            // sort
            if (SortCB.SelectedIndex == 1)
                currentProducts = currentProducts.OrderByDescending(p => p.Price).ToList();
            if (SortCB.SelectedIndex == 2)
                currentProducts = currentProducts.OrderBy(p => p.Price).ToList();

            ProductListView.ItemsSource = currentProducts;
        }
        private void Button_Click(object sender, RoutedEventArgs e)
        {

            Manager.MainFrame.Navigate(new ProductPage());
        }
        private void SearchTB_TextChanged(object sender, TextChangedEventArgs e)
        {
            UpdateProducts();
        }

        private void FiltCB_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateProducts();
        }

        private void SortCB_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateProducts();
        }

        private void AddToCart_Click(object sender, RoutedEventArgs e)
        {
            // 1. Проверка на гостя
            if (Manager.IsGuest)
            {
                MessageBox.Show("Для оформления заказа необходимо авторизоваться.");
                return;
            }

            // 2. Получаем кнопку и товар
            Button btn = sender as Button;
            if (btn == null) return;

            Products selectedProduct = btn.Tag as Products;

            // Если Tag не привязался к Product, покажем ошибку (для отладки)
            if (selectedProduct == null)
            {
                MessageBox.Show("Ошибка: к кнопке не привязан товар (Tag={Binding}).");
                return;
            }

            try
            {
                var context = ChudoShoesEntities.GetContext();

                // 3. Загружаем доступные размеры (Stock) для этого товара
                var availableStocks = context.Stock
                    .Include("Sizes") // Обязательно, чтобы подгрузились размеры (36, 37 и т.д.)
                    .Where(s => s.ProductID == selectedProduct.ProductID && s.Available > 0)
                    .ToList();

                if (availableStocks.Count == 0)
                {
                    MessageBox.Show("К сожалению, этого товара нет в наличии ни в одном размере.");
                    return;
                }

                // 4. Если доступен только один размер, добавляем сразу без лишних окон
                if (availableStocks.Count == 1)
                {
                    AddStockToCart(availableStocks[0]);
                    return;
                }

                // 5. Если размеров несколько, открываем окно выбора
                var sizeWindow = new SizeSelectionWindow(selectedProduct.ProductName, availableStocks);
                sizeWindow.Owner = Window.GetWindow(this);

                // Если пользователь нажал "Выбрать"
                if (sizeWindow.ShowDialog() == true)
                {
                    AddStockToCart(sizeWindow.SelectedStock);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Произошла ошибка при добавлении в корзину: " + ex.Message);
            }
        }

        // Вспомогательный метод для добавления в корзину
        private void AddStockToCart(Stock selectedStock)
        {
            if (Manager.Cart.ContainsKey(selectedStock))
            {
                if (Manager.Cart[selectedStock] < selectedStock.Available)
                {
                    Manager.Cart[selectedStock]++;
                    MessageBox.Show("Количество увеличено.");
                }
                else
                {
                    MessageBox.Show("Достигнуто максимальное доступное количество на складе.");
                }
            }
            else
            {
                Manager.Cart.Add(selectedStock, 1);
                MessageBox.Show($"Товар (Размер: {selectedStock.Sizes.SizeValue}) добавлен в корзину.");
            }
        }
        private void ListViewItem_Loaded(object sender, RoutedEventArgs e)
        {
            var container = sender as ListViewItem;
            if (container == null) return;

            Products product = container.DataContext as Products;
            if (product == null) return;

            // Ищем TextBlock внутри карточки
            var tb = FindChild<TextBlock>(container, "ProductQuantityTB");
            if (tb == null) return;

            // Суммируем количество по всем размерам (Stock)
            int totalPairs = product.Stock?.Sum(s => s.Available) ?? 0;

            string label = totalPairs > 5 ? "много" : "мало";
            tb.Text = $"{label}";
        }
        private static T FindChild<T>(DependencyObject parent, string name) where T : FrameworkElement
        {
            if (parent == null) return null;

            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);

                if (child is T typed && typed.Name == name)
                    return typed;

                var result = FindChild<T>(child, name);
                if (result != null) return result;
            }
            return null;
        }
    }
}
