using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ЧудоОбувь;

namespace ЧудоОбувь
{
    public partial class OrdersPage : Page
    {
        public OrdersPage()
        {
            InitializeComponent();
            UpdateOrders();
        }
        //Менеджер должен иметь возможность (добавлять/удалять товарные позиции, изменять количество)
        //Администратор должен иметь возможность Администратор должен иметь возможность редактировать заказ.
        private void UpdateOrders()
        {
            try
            {
                var context = ChudoShoesEntities.GetContext();
                var ordersQuery = context.Orders.AsQueryable();

                if (Manager.IsUser)
                {
                    ordersQuery = ordersQuery.Where(o => o.UserID == Manager.CurrentUser.UserID);
                }

                var ordersList = ordersQuery.ToList().Select(o => new
                {
                    OrderID = o.OrderID,
                    OrderDate = o.OrderDate.Date,
                    ClientName = o.Users.UserSurname + " " + o.Users.UserName + " " + o.Users.UserLastName,
                    Cost = o.Cost,
                    OriginalOrder = o
                }).ToList();

                OrdersGrid.ItemsSource = ordersList;

                if (Manager.IsManager || Manager.IsAdmin)
                {
                    BtnDeleteOrder.Visibility = Visibility.Visible;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки заказов: " + ex.Message);
            }
        }
        private void OrdersGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (OrdersGrid.SelectedItem != null)
            {
                dynamic selected = OrdersGrid.SelectedItem;
                Orders order = selected.OriginalOrder;

                try
                {
                    var context = ChudoShoesEntities.GetContext();
                    var items = context.OrderItems.Where(oi => oi.OrderID == order.OrderID).ToList();
                    OrderItemsGrid.ItemsSource = items;
                    OrderDetailsPanel.Visibility = Visibility.Visible;

                    // Управление видимостью панели редактирования
                    if (Manager.IsAdmin)
                    {
                        OrderItemsGrid.Columns[3].Visibility = Visibility.Visible; // Индекс 3 - наша скрытая колонка с кнопками
                        EditOrderPanel.Visibility = Visibility.Visible;
                        OrderDateTB.Visibility = Visibility.Visible;
                        OrderDatePicker.Visibility = Visibility.Visible;
                        OrderDatePicker.SelectedDate = order.OrderDate;
                        // Подгружаем склад для добавления новых позиций
                        var stocks = context.Stock.Where(s => s.Available > 0).ToList().Select(s => new
                        {
                            StockID = s.StockID,
                            Display = $"{s.Products.ProductName} (Размер: {s.Sizes.SizeValue}, В наличии: {s.Available}, Цена: {s.Products.Price})"
                        }).ToList();

                        StockCB.ItemsSource = stocks;
                        StockCB.SelectedValuePath = "StockID";
                        StockCB.DisplayMemberPath = "Display";
                    }
                    else
                    {
                        OrderItemsGrid.Columns[3].Visibility = Visibility.Collapsed;
                        EditOrderPanel.Visibility = Visibility.Collapsed;
                        OrderDateTB.Visibility = Visibility.Collapsed;
                        OrderDatePicker.Visibility = Visibility.Collapsed;
                    }
                }
                catch
                {
                    OrderDetailsPanel.Visibility = Visibility.Collapsed;
                }
            }
            else
            {
                OrderDetailsPanel.Visibility = Visibility.Collapsed;
            }
        }

        // Вспомогательный метод для бесшовного обновления интерфейса после редактирования
        private void RefreshOrderData()
        {
            int index = OrdersGrid.SelectedIndex;
            UpdateOrders();
            if (index >= 0 && index < OrdersGrid.Items.Count)
            {
                OrdersGrid.SelectedIndex = index;
            }
        }

        // УВЕЛИЧИТЬ КОЛИЧЕСТВО ТОВАРА В ЗАКАЗЕ
        private void BtnPlusItem_Click(object sender, RoutedEventArgs e)
        {
            var item = (sender as Button).Tag as OrderItems;
            if (item == null) return;

            var context = ChudoShoesEntities.GetContext();
            var dbItem = context.OrderItems.FirstOrDefault(oi => oi.OrderID == item.OrderID && oi.StockID == item.StockID);
            var stock = context.Stock.Find(item.StockID);
            var order = context.Orders.Find(item.OrderID);
            if (dbItem != null && stock != null && order != null)
            {
                if (stock.Available > 0)
                {
                    dbItem.Quantity++;
                    stock.Available--;
                    order.Cost += stock.Products.Price;
                    context.SaveChanges();
                    RefreshOrderData();
                }
                else
                {
                    MessageBox.Show("Достигнуто максимальное доступное количество на складе.");
                }
            }
        }

        // УМЕНЬШИТЬ КОЛИЧЕСТВО ТОВАРА В ЗАКАЗЕ
        private void BtnMinusItem_Click(object sender, RoutedEventArgs e)
        {
            var item = (sender as Button).Tag as OrderItems;
            if (item == null) return;

            var context = ChudoShoesEntities.GetContext();
            var dbItem = context.OrderItems.FirstOrDefault(oi => oi.OrderID == item.OrderID && oi.StockID == item.StockID);
            var stock = context.Stock.Find(item.StockID);
            var order = context.Orders.Find(item.OrderID);

            if (dbItem != null && stock != null && order != null)
            {
                if (dbItem.Quantity > 1)
                {
                    dbItem.Quantity--;
                    stock.Available++;
                    order.Cost -= stock.Products.Price;
                }
                else
                {
                    // Если была 1 штука, то удаляем позицию совсем
                    stock.Available += dbItem.Quantity;
                    order.Cost -= (stock.Products.Price * dbItem.Quantity);
                    context.OrderItems.Remove(dbItem);
                }
                context.SaveChanges();
                RefreshOrderData();
            }
        }

        // УДАЛИТЬ ТОВАРНУЮ ПОЗИЦИЮ ИЗ ЗАКАЗА
        private void BtnRemoveItem_Click(object sender, RoutedEventArgs e)
        {
            var item = (sender as Button).Tag as OrderItems;
            if (item == null) return;

            var context = ChudoShoesEntities.GetContext();
            var dbItem = context.OrderItems.FirstOrDefault(oi => oi.OrderID == item.OrderID && oi.StockID == item.StockID);
            var stock = context.Stock.Find(item.StockID);
            var order = context.Orders.Find(item.OrderID);

            if (dbItem != null && stock != null && order != null)
            {
                stock.Available += dbItem.Quantity;
                order.Cost -= (stock.Products.Price * dbItem.Quantity);
                context.OrderItems.Remove(dbItem);
                context.SaveChanges();
                RefreshOrderData();
            }
        }

        // ДОБАВИТЬ НОВЫЙ ТОВАР В ЗАКАЗ
        private void BtnAddItem_Click(object sender, RoutedEventArgs e)
        {
            if (StockCB.SelectedValue == null)
            {
                MessageBox.Show("Пожалуйста, выберите товар для добавления.");
                return;
            }

            int stockId = (int)StockCB.SelectedValue;
            dynamic selectedOrderRow = OrdersGrid.SelectedItem;

            if (selectedOrderRow == null) return;

            Orders currentOrder = selectedOrderRow.OriginalOrder;
            var context = ChudoShoesEntities.GetContext();

            var stock = context.Stock.Find(stockId);
            var order = context.Orders.Find(currentOrder.OrderID);

            if (stock != null && order != null && stock.Available > 0)
            {
                // Проверяем, есть ли уже этот товар в текущем заказе
                var existingItem = context.OrderItems.FirstOrDefault(oi => oi.OrderID == order.OrderID && oi.StockID == stockId);

                if (existingItem != null)
                {
                    existingItem.Quantity++;
                }
                else
                {
                    OrderItems newItem = new OrderItems
                    {
                        OrderID = order.OrderID,
                        StockID = stockId,
                        Quantity = 1
                    };
                    context.OrderItems.Add(newItem);
                }

                stock.Available--;
                order.Cost += stock.Products.Price;
                context.SaveChanges();

                RefreshOrderData();
            }
            else
            {
                MessageBox.Show("Этого товара нет в наличии.");
            }
        }
        // ПОЛНОЕ УДАЛЕНИЕ ЗАКАЗА
        private void BtnDeleteOrder_Click(object sender, RoutedEventArgs e)
        {
            if (OrdersGrid.SelectedItem == null) return;

            if (MessageBox.Show("Вы точно хотите удалить этот заказ целиком?", "Внимание", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                try
                {
                    dynamic selected = OrdersGrid.SelectedItem;
                    Orders order = selected.OriginalOrder;

                    var context = ChudoShoesEntities.GetContext();
                    var orderToDelete = context.Orders.Find(order.OrderID);

                    if (orderToDelete != null)
                    {
                        var items = context.OrderItems.Where(oi => oi.OrderID == orderToDelete.OrderID).ToList();

                        // Возвращаем все товары на склад
                        foreach (var item in items)
                        {
                            var stock = context.Stock.Find(item.StockID);
                            if (stock != null) stock.Available += item.Quantity;
                        }

                        context.OrderItems.RemoveRange(items);
                        context.Orders.Remove(orderToDelete);
                        context.SaveChanges();

                        UpdateOrders();
                        OrderDetailsPanel.Visibility = Visibility.Collapsed;
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Ошибка при удалении заказа: " + ex.Message);
                }
            }
        }
        private void OrderDatePicker_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            // Только администратор
            if (!Manager.IsAdmin) return;

            // Должен быть выбран заказ
            if (OrdersGrid.SelectedItem == null) return;

            // Если дату очистили — откатываем к сохранённому значению
            if (OrderDatePicker.SelectedDate == null)
            {
                try
                {
                    dynamic sel = OrdersGrid.SelectedItem;
                    Orders ord = sel.OriginalOrder;

                    OrderDatePicker.SelectedDate = ord.OrderDate.Date;
                }
                catch { }
                return;
            }

            try
            {
                dynamic selected = OrdersGrid.SelectedItem;
                Orders order = selected.OriginalOrder;

                // Берем только дату (время обнуляем)
                DateTime newDate = OrderDatePicker.SelectedDate.Value.Date;

                // Проверка: дата не может быть в будущем
                if (newDate > DateTime.Today)
                {
                    MessageBox.Show(
                        "Дата заказа не может быть в будущем. Выберите сегодняшнюю или более раннюю дату.",
                        "Недопустимая дата",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);

                    // Откат DatePicker
                    OrderDatePicker.SelectedDate = order.OrderDate.Date;
                    return;
                }

                // Если дата не изменилась — выходим
                if (order.OrderDate.Date == newDate) return;

                // Сохраняем в БД
                var context = ChudoShoesEntities.GetContext();
                var dbOrder = context.Orders.Find(order.OrderID);
                if (dbOrder == null) return;

                // Если в базе хранится время (например, 14:30), при смене даты 
                // лучше сохранить это время, но с новой датой. 
                // Если время вообще не нужно, можно сделать просто: dbOrder.OrderDate = newDate;
                TimeSpan existingTime = dbOrder.OrderDate.TimeOfDay;
                dbOrder.OrderDate = newDate + existingTime;

                context.SaveChanges();

                RefreshOrderData();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка сохранения даты заказа: " + ex.Message);
            }
        }
    }
}
