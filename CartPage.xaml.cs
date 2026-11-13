using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using ЧудоОбувь;

namespace ЧудоОбувь
{
    public class CartItemUI
    {
        public Stock StockRef { get; set; }
        public string ProductName => StockRef.Products.ProductName;
        public decimal Size => StockRef.Sizes.SizeValue;
        public decimal Price => StockRef.Products.Price;
        public int Quantity { get; set; }
        public decimal Total => Price * Quantity;
    }

    public partial class CartPage : Page
    {
        public CartPage()
        {
            InitializeComponent();
            UpdateUI();
        }

        private void UpdateUI()
        {
            var list = Manager.Cart.Select(kvp => new CartItemUI { StockRef = kvp.Key, Quantity = kvp.Value }).ToList();
            CartGrid.ItemsSource = list;
            TotalCostTB.Text = "Итого: " + list.Sum(x => x.Total) + " руб.";
        }

        private void BtnMinus_Click(object sender, RoutedEventArgs e)
        {
            var item = (sender as Button).Tag as CartItemUI;
            if (item != null)
            {
                if (Manager.Cart[item.StockRef] > 1)
                {
                    Manager.Cart[item.StockRef]--;
                }
                else
                {
                    Manager.Cart.Remove(item.StockRef);
                }
                UpdateUI();
            }
        }

        private void BtnPlus_Click(object sender, RoutedEventArgs e)
        {
            var item = (sender as Button).Tag as CartItemUI;
            if (item != null)
            {
                if (Manager.Cart[item.StockRef] < item.StockRef.Available)
                {
                    Manager.Cart[item.StockRef]++;
                    UpdateUI();
                }
                else
                {
                    MessageBox.Show("Достигнуто максимальное доступное количество на складе.");
                }
            }
        }

        private void BtnRemove_Click(object sender, RoutedEventArgs e)
        {
            var item = (sender as Button).Tag as CartItemUI;
            if (item != null)
            {
                Manager.Cart.Remove(item.StockRef);
                UpdateUI();
            }
        }

        private void BtnCheckout_Click(object sender, RoutedEventArgs e)
        {
            if (Manager.Cart.Count == 0)
            {
                MessageBox.Show("Корзина пуста.");
                return;
            }

            try
            {
                var context = ChudoShoesEntities.GetContext();
                
                Orders newOrder = new Orders
                {
                    UserID = Manager.CurrentUser.UserID,
                    OrderDate = DateTime.Now,
                    Cost = Manager.Cart.Sum(x => x.Key.Products.Price * x.Value)
                };

                context.Orders.Add(newOrder);

                foreach (var kvp in Manager.Cart)
                {
                    OrderItems orderItem = new OrderItems
                    {
                        Orders = newOrder,
                        StockID = kvp.Key.StockID,
                        Quantity = kvp.Value
                    };
                    context.OrderItems.Add(orderItem);

                    // Уменьшаем количество на складе
                    var stockInDb = context.Stock.Find(kvp.Key.StockID);
                    if (stockInDb != null)
                    {
                        stockInDb.Available -= kvp.Value;
                    }
                }

                context.SaveChanges();
                
                Manager.Cart.Clear();
                MessageBox.Show("Заказ успешно оформлен!");
                Manager.MainFrame.Navigate(new ProductPage());
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при оформлении заказа: " + ex.Message);
            }
        }
    }
}
