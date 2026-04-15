using Microsoft.Win32;
using System;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace LeastCostMethodApp
{
    /// <summary>
    /// Главное окно приложения для решения транспортной задачи методом минимальных элементов.
    /// </summary>
    public partial class MainWindow : Window
    {
        private DataTable costTable;

        /// <summary>
        /// Конструктор главного окна.
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Создаёт матрицу стоимостей заданного размера.
        /// </summary>
        /// <param name="sender">Источник события</param>
        /// <param name="e">Аргументы события</param>
        private void BtnCreateCostMatrix_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                int n = int.Parse(TxtNumSuppliers.Text.Trim());
                int m = int.Parse(TxtNumConsumers.Text.Trim());

                if (n < 1 || m < 1)
                {
                    MessageBox.Show("Количество поставщиков и потребителей должно быть больше 0.",
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                costTable = new DataTable("CostMatrix");

                for (int j = 0; j < m; j++)
                    costTable.Columns.Add($"C{j + 1}", typeof(int));

                for (int i = 0; i < n; i++)
                {
                    var row = costTable.NewRow();
                    for (int j = 0; j < m; j++)
                        row[j] = 0;
                    costTable.Rows.Add(row);
                }

                DataGridCost.ItemsSource = costTable.DefaultView;

                // Установка заголовков столбцов
                for (int j = 0; j < DataGridCost.Columns.Count; j++)
                {
                    DataGridCost.Columns[j].Header = $"Потребитель C{j + 1}";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка создания матрицы: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Выполняет валидацию всех входных данных.
        /// </summary>
        /// <returns>True, если данные корректны, иначе False</returns>
        private bool ValidateInputs()
        {
            if (costTable == null)
            {
                MessageBox.Show("Сначала создайте матрицу стоимостей.", "Внимание");
                return false;
            }

            // Проверка заполненности матрицы стоимостей
            foreach (DataRow row in costTable.Rows)
            {
                foreach (var item in row.ItemArray)
                {
                    if (item == DBNull.Value || !int.TryParse(item.ToString(), out _))
                    {
                        MessageBox.Show("Все ячейки матрицы стоимостей должны быть заполнены целыми числами.",
                            "Ошибка валидации");
                        return false;
                    }
                }
            }

            if (string.IsNullOrWhiteSpace(TxtSupply.Text) || string.IsNullOrWhiteSpace(TxtDemand.Text))
            {
                MessageBox.Show("Введите запасы поставщиков и потребности потребителей.", "Ошибка");
                return false;
            }

            return true;
        }

        /// <summary>
        /// Получает входные данные из интерфейса (запасы, потребности, матрицу стоимостей).
        /// </summary>
        /// <returns>Кортеж с массивами запасов, потребностей и матрицей стоимостей</returns>
        private (int[] supply, int[] demand, int[,] cost) GetInputData()
        {
            int[] supply = TxtSupply.Text.Split(',')
                .Select(s => int.Parse(s.Trim())).ToArray();

            int[] demand = TxtDemand.Text.Split(',')
                .Select(s => int.Parse(s.Trim())).ToArray();

            int n = costTable.Rows.Count;
            int m = costTable.Columns.Count;

            int[,] cost = new int[n, m];
            for (int i = 0; i < n; i++)
                for (int j = 0; j < m; j++)
                    cost[i, j] = Convert.ToInt32(costTable.Rows[i][j]);

            return (supply, demand, cost);
        }

        /// <summary>
        /// Решает транспортную задачу методом минимальных элементов (Least Cost Method).
        /// </summary>
        /// <param name="supply">Массив запасов поставщиков</param>
        /// <param name="demand">Массив потребностей потребителей</param>
        /// <param name="cost">Матрица стоимостей</param>
        /// <returns>Матрица распределения (план перевозок)</returns>
        private int[,] LeastCostMethod(int[] supply, int[] demand, int[,] cost)
        {
            int n = supply.Length;
            int m = demand.Length;
            int[,] alloc = new int[n, m];

            int[] s = (int[])supply.Clone();
            int[] d = (int[])demand.Clone();

            while (s.Sum() > 0 && d.Sum() > 0)
            {
                int minCost = int.MaxValue;
                int row = -1, col = -1;

                for (int i = 0; i < n; i++)
                {
                    if (s[i] == 0) continue;
                    for (int j = 0; j < m; j++)
                    {
                        if (d[j] == 0) continue;
                        if (cost[i, j] < minCost)
                        {
                            minCost = cost[i, j];
                            row = i;
                            col = j;
                        }
                    }
                }

                if (row == -1) break;

                int quantity = Math.Min(s[row], d[col]);
                alloc[row, col] = quantity;

                s[row] -= quantity;
                d[col] -= quantity;
            }

            return alloc;
        }

        /// <summary>
        /// Рассчитывает общую стоимость перевозок по матрице распределения.
        /// </summary>
        /// <param name="alloc">Матрица распределения</param>
        /// <param name="cost">Матрица стоимостей</param>
        /// <returns>Общая стоимость</returns>
        private int CalculateTotalCost(int[,] alloc, int[,] cost)
        {
            int total = 0;
            for (int i = 0; i < alloc.GetLength(0); i++)
                for (int j = 0; j < alloc.GetLength(1); j++)
                    total += alloc[i, j] * cost[i, j];
            return total;
        }

        /// <summary>
        /// Формирует подробное пошаговое описание решения транспортной задачи.
        /// </summary>
        /// <param name="supply">Исходные запасы поставщиков</param>
        /// <param name="demand">Исходные потребности потребителей</param>
        /// <param name="cost">Матрица стоимостей</param>
        /// <param name="alloc">Матрица распределения</param>
        /// <param name="totalCost">Общая стоимость</param>
        /// <returns>Строка с подробным отчётом о решении</returns>
        private string GetDetailedSolution(int[] supply, int[] demand, int[,] cost, int[,] alloc, int totalCost)
        {
            var sb = new StringBuilder();
            sb.AppendLine("=== ПОДРОБНЫЙ ХОД РЕШЕНИЯ (Метод минимальных элементов) ===\n");

            int n = supply.Length;
            int m = demand.Length;
            int[] s = (int[])supply.Clone();
            int[] d = (int[])demand.Clone();

            int step = 1;

            while (s.Sum() > 0 && d.Sum() > 0)
            {
                int minCost = int.MaxValue;
                int row = -1, col = -1;

                for (int i = 0; i < n; i++)
                {
                    if (s[i] == 0) continue;
                    for (int j = 0; j < m; j++)
                    {
                        if (d[j] == 0) continue;
                        if (cost[i, j] < minCost)
                        {
                            minCost = cost[i, j];
                            row = i;
                            col = j;
                        }
                    }
                }

                if (row == -1) break;

                int quantity = Math.Min(s[row], d[col]);

                sb.AppendLine($"Шаг {step}:");
                sb.AppendLine($"   Минимальный элемент: Поставщик S{row + 1} → Потребитель C{col + 1}");
                sb.AppendLine($"   Стоимость: {minCost}");
                sb.AppendLine($"   Перевезено: {quantity} единиц");
                sb.AppendLine($"   Остаток поставщика S{row + 1}: {s[row]} → {s[row] - quantity}");
                sb.AppendLine($"   Остаток потребителя C{col + 1}: {d[col]} → {d[col] - quantity}");
                sb.AppendLine(new string('─', 70));

                s[row] -= quantity;
                d[col] -= quantity;
                step++;
            }

            sb.AppendLine("\n=== ИТОГОВАЯ МАТРИЦА РАСПРЕДЕЛЕНИЯ ===\n");

            for (int i = 0; i < alloc.GetLength(0); i++)
            {
                sb.Append($"Поставщик S{i + 1} | ");
                for (int j = 0; j < alloc.GetLength(1); j++)
                {
                    sb.Append($"{alloc[i, j],6}");
                }
                sb.AppendLine();
            }

            sb.AppendLine($"\nОбщая стоимость перевозок: {totalCost} у.е.");

            return sb.ToString();
        }

        /// <summary>
        /// Отображает результат решения в TextBlock.
        /// </summary>
        private void ShowResult(int[] supply, int[] demand, int[,] cost, int[,] alloc, int totalCost)
        {
            string resultText = GetDetailedSolution(supply, demand, cost, alloc, totalCost);
            TxtResult.Text = resultText;
        }

        /// <summary>
        /// Обработчик кнопки "Решить". Запускает решение транспортной задачи.
        /// </summary>
        private void BtnSolve_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateInputs()) return;

            try
            {
                var (supply, demand, cost) = GetInputData();

                if (supply.Length != cost.GetLength(0) || demand.Length != cost.GetLength(1))
                {
                    MessageBox.Show("Количество запасов и потребностей не соответствует размерам матрицы.");
                    return;
                }

                var allocation = LeastCostMethod(supply, demand, cost);
                int totalCost = CalculateTotalCost(allocation, cost);

                ShowResult(supply, demand, cost, allocation, totalCost);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при решении задачи:\n{ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Очищает все поля и матрицу.
        /// </summary>
        private void BtnClear_Click(object sender, RoutedEventArgs e)
        {
            costTable = null;
            DataGridCost.ItemsSource = null;
            DataGridCost.Columns.Clear();

            TxtSupply.Clear();
            TxtDemand.Clear();
            TxtResult.Text = "";

            MessageBox.Show("Все поля успешно очищены.", "Очистка",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        /// <summary>
        /// Сохраняет подробное решение в текстовый файл.
        /// </summary>
        private void BtnSaveResult_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TxtResult.Text))
            {
                MessageBox.Show("Сначала решите задачу.", "Предупреждение");
                return;
            }

            SaveFileDialog dlg = new SaveFileDialog
            {
                Filter = "Текстовый файл (*.txt)|*.txt|Все файлы (*.*)|*.*",
                FileName = $"Транспортная_задача_решение_{DateTime.Now:yyyy-MM-dd_HH-mm}.txt"
            };

            if (dlg.ShowDialog() == true)
            {
                try
                {
                    File.WriteAllText(dlg.FileName, TxtResult.Text, Encoding.UTF8);
                    MessageBox.Show("Решение успешно сохранено в файл!", "Сохранение",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка сохранения файла:\n{ex.Message}");
                }
            }
        }

        /// <summary>
        /// Загружает данные задачи из текстового файла.
        /// </summary>
        private void BtnLoadFromFile_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dlg = new OpenFileDialog
            {
                Filter = "Текстовый файл (*.txt)|*.txt"
            };

            if (dlg.ShowDialog() != true) return;

            try
            {
                string[] lines = File.ReadAllLines(dlg.FileName);

                if (lines.Length < 4)
                {
                    MessageBox.Show("Неверный формат файла. Файл должен содержать минимум 4 строки.", "Ошибка");
                    return;
                }

                var sizes = lines[0].Split(',').Select(int.Parse).ToArray();
                TxtNumSuppliers.Text = sizes[0].ToString();
                TxtNumConsumers.Text = sizes[1].ToString();

                BtnCreateCostMatrix_Click(null, null);

                TxtSupply.Text = lines[1];
                TxtDemand.Text = lines[2];

                for (int i = 0; i < costTable.Rows.Count && i + 3 < lines.Length; i++)
                {
                    var values = lines[i + 3].Split(',').Select(s => int.Parse(s.Trim())).ToArray();

                    for (int j = 0; j < costTable.Columns.Count && j < values.Length; j++)
                    {
                        costTable.Rows[i][j] = values[j];
                    }
                }

                MessageBox.Show("Данные успешно загружены из файла!", "Загрузка завершена",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке файла:\n{ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}