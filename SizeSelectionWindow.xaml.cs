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
using System.Windows.Shapes;

namespace ЧудоОбувь
{
    /// <summary>
    /// Логика взаимодействия для SizeSelectionWindow.xaml
    /// </summary>
    public partial class SizeSelectionWindow : Window
    {
        // Сюда сохраняем выбранный склад (размер)
        public Stock SelectedStock { get; private set; }
        public SizeSelectionWindow(string productName, List<Stock> availableStocks)
        {
            InitializeComponent();
            ProductNameTextBlock.Text = $"Товар: {productName}";

            // Формируем список для вывода, чтобы в ListBox было красиво
            var displayItems = availableStocks.Select(s => new
            {
                StockObj = s, // сохраняем оригинальный объект
                Display = $"Размер: {s.Sizes.SizeValue}  (В наличии: {s.Available} шт.)"
            }).ToList();

            SizesListBox.ItemsSource = displayItems;

            // Автоматически выделяем первый размер в списке
            if (displayItems.Count > 0)
                SizesListBox.SelectedIndex = 0;
        }             

        // Обработка кнопки "Выбрать"
        private void BtnSelect_Click(object sender, RoutedEventArgs e)
        {
            ConfirmSelection();
        }

        // Обработка двойного клика по размеру (чтобы не нажимать кнопку)
        private void SizesListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            ConfirmSelection();
        }

        // Вспомогательный метод подтверждения
        private void ConfirmSelection()
        {
            if (SizesListBox.SelectedItem == null)
            {
                MessageBox.Show("Пожалуйста, выберите размер из списка.", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Достаем наш сохраненный объект Stock из анонимного типа
            dynamic selectedItem = SizesListBox.SelectedItem;
            SelectedStock = selectedItem.StockObj;

            // Закрываем окно с результатом true
            this.DialogResult = true;
        }
    }

}
