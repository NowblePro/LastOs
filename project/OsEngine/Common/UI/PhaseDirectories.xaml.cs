using Newtonsoft.Json;
using OsEngine.Entity;
using System;
using System.Collections.Generic;
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

namespace OsEngine.Common.UI
{
    /// <summary>
    /// Логика взаимодействия для PhaseDirectories.xaml
    /// </summary>
    public partial class PhaseDirectories : Window
    {
        private static PortfolioLongShortSettings Settings { get; set; } = new PortfolioLongShortSettings();

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
    }

    class PortfolioLongShortSettings
    {
        public string LongPath { get; set; }
        public string ShortPath { get; set; }
    }
}
