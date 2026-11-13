using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using iTextSharp.text;
using iTextSharp.text.pdf;
using ЧудоОбувь;

namespace ЧудоОбувь
{
    public class TopProductItem
    {
        public int Position { get; set; }
        public string ProductName { get; set; }
        public string ManufacturerName { get; set; }
        public int SoldQuantity { get; set; }
    }

    /// <summary>
    /// Логика взаимодействия для AnalyticsPage.xaml
    /// </summary>
    public partial class AnalyticsPage : Page
    {
        private bool _isInitialized = false;

        public AnalyticsPage()
        {
            InitializeComponent();

            // По умолчанию текущий календарный месяц (с 1-го числа по последнее)
            DateTime today = DateTime.Today;
            StartDatePicker.SelectedDate = new DateTime(today.Year, today.Month, 1);
            EndDatePicker.SelectedDate = new DateTime(today.Year, today.Month, DateTime.DaysInMonth(today.Year, today.Month));

            StartDatePicker.SelectedDateChanged += Period_SelectedDateChanged;
            EndDatePicker.SelectedDateChanged += Period_SelectedDateChanged;

            _isInitialized = true;
            UpdateAnalytics();
        }

        private void Period_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isInitialized)
            {
                UpdateAnalytics();
            }
        }

        private void ShowError(string message)
        {
            ErrorTB.Text = message;
            ErrorTB.Visibility = Visibility.Visible;
            BtnExportPdf.IsEnabled = false;

            OrdersCountTB.Text = "0";
            TotalSumTB.Text = "0.00 руб.";
            TopProductsGrid.ItemsSource = null;
            EmptyMessageTB.Visibility = Visibility.Visible;
        }

        private void ClearError()
        {
            ErrorTB.Text = string.Empty;
            ErrorTB.Visibility = Visibility.Collapsed;
            BtnExportPdf.IsEnabled = true;
        }

        private void UpdateAnalytics()
        {
            if (!StartDatePicker.SelectedDate.HasValue || !EndDatePicker.SelectedDate.HasValue)
            {
                ShowError("Выберите дату начала и дату окончания периода.");
                return;
            }

            DateTime startDate = StartDatePicker.SelectedDate.Value.Date;
            DateTime endDate = EndDatePicker.SelectedDate.Value.Date;

            // Валидация: дата начала не может быть позже даты окончания
            if (startDate > endDate)
            {
                ShowError("Дата начала не может быть позже даты окончания!");
                return;
            }

            ClearError();

            try
            {
                DateTime endDateTime = endDate.AddDays(1).AddTicks(-1);

                var context = ChudoShoesEntities.GetContext();
                var orders = context.Orders
                    .Include("OrderItems.Stock.Products.Manufacturers")
                    .Where(o => o.OrderDate >= startDate && o.OrderDate <= endDateTime)
                    .ToList();

                int totalOrders = orders.Count;
                decimal totalSum = orders.Sum(o => (decimal?)o.Cost) ?? 0m;

                OrdersCountTB.Text = totalOrders.ToString();
                TotalSumTB.Text = totalSum.ToString("N2") + " руб.";

                var topItems = orders
                    .SelectMany(o => o.OrderItems)
                    .Where(oi => oi.Stock != null && oi.Stock.Products != null)
                    .GroupBy(oi => oi.Stock.Products)
                    .Select(g => new
                    {
                        Product = g.Key,
                        Quantity = g.Sum(oi => oi.Quantity)
                    })
                    .OrderByDescending(x => x.Quantity)
                    .Take(5)
                    .ToList()
                    .Select((item, index) => new TopProductItem
                    {
                        Position = index + 1,
                        ProductName = item.Product.ProductName,
                        ManufacturerName = item.Product.Manufacturers != null ? item.Product.Manufacturers.ManufacturerName : "-",
                        SoldQuantity = item.Quantity
                    })
                    .ToList();

                TopProductsGrid.ItemsSource = topItems;
                EmptyMessageTB.Visibility = topItems.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при получении данных аналитики: " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnExportPdf_Click(object sender, RoutedEventArgs e)
        {
            if (!StartDatePicker.SelectedDate.HasValue || !EndDatePicker.SelectedDate.HasValue)
            {
                MessageBox.Show("Выберите корректный период дат перед экспортом.", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            DateTime startDate = StartDatePicker.SelectedDate.Value.Date;
            DateTime endDate = EndDatePicker.SelectedDate.Value.Date;

            if (startDate > endDate)
            {
                MessageBox.Show("Дата начала не может быть позже даты окончания!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            SaveFileDialog sfd = new SaveFileDialog
            {
                Title = "Сохранение отчёта по продажам в PDF",
                Filter = "PDF файлы (*.pdf)|*.pdf",
                FileName = $"Отчет_по_продажам_{startDate:yyyy_MM_dd}_{endDate:yyyy_MM_dd}.pdf"
            };

            if (sfd.ShowDialog() != true)
                return;

            try
            {
                using (FileStream fs = new FileStream(sfd.FileName, FileMode.Create))
                {
                    Document doc = new Document(PageSize.A4, 36, 36, 36, 36);
                    PdfWriter.GetInstance(doc, fs);
                    doc.Open();

                    string fontPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Fonts), "arial.ttf");
                    BaseFont bf;
                    if (File.Exists(fontPath))
                    {
                        bf = BaseFont.CreateFont(fontPath, BaseFont.IDENTITY_H, BaseFont.NOT_EMBEDDED);
                    }
                    else
                    {
                        bf = BaseFont.CreateFont(BaseFont.HELVETICA, BaseFont.WINANSI, BaseFont.NOT_EMBEDDED);
                    }

                    Font titleFont = new Font(bf, 18, Font.BOLD, new BaseColor(40, 40, 40));
                    Font subtitleFont = new Font(bf, 12, Font.NORMAL, new BaseColor(100, 100, 100));
                    Font sectionFont = new Font(bf, 14, Font.BOLD, new BaseColor(50, 50, 50));
                    Font boldFont = new Font(bf, 11, Font.BOLD, BaseColor.BLACK);
                    Font regularFont = new Font(bf, 11, Font.NORMAL, BaseColor.BLACK);
                    Font tableHeaderFont = new Font(bf, 11, Font.BOLD, BaseColor.WHITE);

                    // Заголовок
                    Paragraph pTitle = new Paragraph("Отчёт по продажам «Чудо Обувь»", titleFont)
                    {
                        Alignment = Element.ALIGN_CENTER,
                        SpacingAfter = 5
                    };
                    doc.Add(pTitle);

                    Paragraph pPeriod = new Paragraph($"Период: с {startDate:dd.MM.yyyy} по {endDate:dd.MM.yyyy}", subtitleFont)
                    {
                        Alignment = Element.ALIGN_CENTER,
                        SpacingAfter = 20
                    };
                    doc.Add(pPeriod);

                    // Сводные показатели
                    doc.Add(new Paragraph("Сводные показатели:", sectionFont) { SpacingAfter = 10 });

                    PdfPTable summaryTable = new PdfPTable(2) { WidthPercentage = 100 };
                    summaryTable.SetWidths(new float[] { 3f, 2f });

                    AddSummaryRow(summaryTable, "Общее количество заказов:", $"{OrdersCountTB.Text} шт.", boldFont, regularFont);
                    AddSummaryRow(summaryTable, "Общая сумма продаж:", TotalSumTB.Text, boldFont, regularFont);
                    summaryTable.SpacingAfter = 20;
                    doc.Add(summaryTable);

                    // Таблица топ-5
                    doc.Add(new Paragraph("Топ-5 моделей по количеству проданных пар:", sectionFont) { SpacingAfter = 10 });

                    PdfPTable table = new PdfPTable(4) { WidthPercentage = 100 };
                    table.SetWidths(new float[] { 1f, 5f, 3f, 2f });

                    BaseColor headerBg = new BaseColor(112, 178, 175); // #FF70B2AF

                    AddCell(table, "№", tableHeaderFont, headerBg, Element.ALIGN_CENTER);
                    AddCell(table, "Наименование модели", tableHeaderFont, headerBg, Element.ALIGN_LEFT);
                    AddCell(table, "Производитель", tableHeaderFont, headerBg, Element.ALIGN_LEFT);
                    AddCell(table, "Количество пар", tableHeaderFont, headerBg, Element.ALIGN_CENTER);

                    var items = TopProductsGrid.ItemsSource as List<TopProductItem>;
                    if (items == null || items.Count == 0)
                    {
                        PdfPCell emptyCell = new PdfPCell(new Phrase("Нет данных о продажах за выбранный период", regularFont))
                        {
                            Colspan = 4,
                            HorizontalAlignment = Element.ALIGN_CENTER,
                            Padding = 10
                        };
                        table.AddCell(emptyCell);
                    }
                    else
                    {
                        foreach (var item in items)
                        {
                            AddCell(table, item.Position.ToString(), regularFont, BaseColor.WHITE, Element.ALIGN_CENTER);
                            AddCell(table, item.ProductName, regularFont, BaseColor.WHITE, Element.ALIGN_LEFT);
                            AddCell(table, item.ManufacturerName, regularFont, BaseColor.WHITE, Element.ALIGN_LEFT);
                            AddCell(table, item.SoldQuantity.ToString(), boldFont, BaseColor.WHITE, Element.ALIGN_CENTER);
                        }
                    }

                    table.SpacingAfter = 20;
                    doc.Add(table);

                    // Дата формирования
                    Paragraph pFooter = new Paragraph($"Дата формирования отчёта: {DateTime.Now:dd.MM.yyyy HH:mm}", subtitleFont)
                    {
                        Alignment = Element.ALIGN_RIGHT
                    };
                    doc.Add(pFooter);

                    doc.Close();
                }

                MessageBox.Show("Отчёт успешно экспортирован в PDF!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при экспорте в PDF: " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void AddSummaryRow(PdfPTable table, string label, string value, Font labelFont, Font valueFont)
        {
            PdfPCell cell1 = new PdfPCell(new Phrase(label, labelFont))
            {
                Border = Rectangle.NO_BORDER,
                Padding = 5
            };
            PdfPCell cell2 = new PdfPCell(new Phrase(value, valueFont))
            {
                Border = Rectangle.NO_BORDER,
                Padding = 5
            };
            table.AddCell(cell1);
            table.AddCell(cell2);
        }

        private void AddCell(PdfPTable table, string text, Font font, BaseColor bg, int alignment)
        {
            PdfPCell cell = new PdfPCell(new Phrase(text ?? "", font))
            {
                BackgroundColor = bg,
                HorizontalAlignment = alignment,
                VerticalAlignment = Element.ALIGN_MIDDLE,
                Padding = 6
            };
            table.AddCell(cell);
        }
    }
}
