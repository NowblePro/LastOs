using Newtonsoft.Json;
using OsEngine.Entity;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Excel = Microsoft.Office.Interop.Excel;

namespace OsEngine.Common.UI
{
    /// <summary>
    /// Логика взаимодействия для PhaseDirectories.xaml
    /// </summary>
    public partial class PhaseDirectories : Window
    {
        public static PortfolioLongShortSettings Settings { get; set; } = new PortfolioLongShortSettings();

        protected PhaseDirectories()
        {
            InitializeComponent();
        }

        public static string LongPortfolioPath
        {
            get => Settings.LongPath;
            set
            {
                Settings.LongPath = value;
                UpdateView();
                Save();
            }
        }

        public static string ShortPortfolioPath
        {
            get => Settings.ShortPath;
            set
            {
                Settings.ShortPath = value;
                UpdateView();
                Save();
            }
        }

        public static void Load()
        {
            if (File.Exists(@"Engine\PortfolioSettings.json"))
            {
                try
                {
                    using (StreamReader reader = new StreamReader(@"Engine\PortfolioSettings.json"))
                    {
                        string str = reader.ReadToEnd();
                        Settings = JsonConvert.DeserializeObject(str, typeof(PortfolioLongShortSettings)) as PortfolioLongShortSettings;
                    }
                }
                catch { }
            }
        }

        private static void Save()
        {
            using (StreamWriter writer = new StreamWriter(@"Engine\PortfolioSettings.json", false))
            {
                string str = JsonConvert.SerializeObject(Settings, Formatting.Indented);
                writer.WriteLine(str);
            }
        }

        private static void UpdateView()
        {
            if (_instance != null)
            {
                if (!_instance.CheckAccess())
                {
                    _instance.Dispatcher.Invoke(UpdateView);
                    return;
                }

                _instance.LabelLong.Content = LongPortfolioPath;
                _instance.LabelShort.Content = ShortPortfolioPath;

                _instance.LabelLong.ToolTip = LongPortfolioPath;
                _instance.LabelShort.ToolTip = ShortPortfolioPath;

                _instance.ButtonLongClear.Visibility = string.IsNullOrEmpty(LongPortfolioPath) ? Visibility.Hidden : Visibility.Visible;
                _instance.ButtonShortClear.Visibility = string.IsNullOrEmpty(ShortPortfolioPath) ? Visibility.Hidden : Visibility.Visible;
            }
        }

        private static PhaseDirectories _instance = null;
        public static PhaseDirectories Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new PhaseDirectories();
                    UpdateView();
                    _instance.Closed += _instance_Closed;
                }
                return _instance;
            }
        }

        private static void _instance_Closed(object sender, EventArgs e)
        {
            _instance.Closed -= _instance_Closed;
            _instance = null;
        }

        private void ButtonLong_Click(object sender, RoutedEventArgs e)
        {
            using (OpenFileDialog dialog = new OpenFileDialog())
            {
                dialog.Filter = "Excel Files|*.xls;*.xlsx;*.xlsm|All files (*.*)|*.*";
                if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    // 
                    LongPortfolioPath = dialog.FileName;
                    LabelLong.Content = LongPortfolioPath;
                }
            }
        }

        private void ButtonShort_Click(object sender, RoutedEventArgs e)
        {
            using (OpenFileDialog dialog = new OpenFileDialog())
            {
                dialog.Filter = "Excel Files|*.xls;*.xlsx;*.xlsm|All files (*.*)|*.*";
                if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    //
                    ShortPortfolioPath = dialog.FileName;
                    LabelShort.Content = ShortPortfolioPath;
                }
            }
        }

        private void ButtonLongClear_Click(object sender, RoutedEventArgs e)
        {
            LongPortfolioPath = "";
        }

        private void ButtonShortClear_Click(object sender, RoutedEventArgs e)
        {
            ShortPortfolioPath = "";
        }

        public static List<string> GetPortfolioNames(string path)
        {
            List<string> result = new List<string>();
            Excel.Application excelApp = new Excel.Application(); // Создание экземпляра Excel-приложения
            Excel.Workbooks workbooks = excelApp.Workbooks;
            Excel.Workbook workbook = workbooks.Open(path); // Открытие файла

            try
            {
                if (workbook.Sheets.Count < 1) throw new Exception();
                Excel.Worksheet sheet = workbook.Sheets[0];
                Excel.Range usedRange = sheet.UsedRange; // Получение диапазона используемых ячеек

                int rowsCount = usedRange.Rows.Count;

                for (int i = 1; i <= rowsCount + 1; i++)
                {
                    object cell = ((Excel.Range)usedRange.Cells[i, 1]).Value2;
                    string portfolio = $"{cell}";
                    if (!string.IsNullOrEmpty(portfolio))
                    {
                        result.Add(portfolio);
                    }
                }
            }
            catch(Exception ex)
            {
                System.Windows.Forms.MessageBox.Show(ex.Message);
            }
            finally
            {
                workbook.Close(false); // Закрываем книгу без сохранения изменений
                excelApp.Quit();       // Завершаем приложение Excel
            }

            return result;
        }
    }

    public class PortfolioLongShortSettings
    {
        public string LongPath { get; set; }
        public string ShortPath { get; set; }

        public string LongPortfolio { get; set; }

        public string ShortPortfolio { get; set; }
    }
}
