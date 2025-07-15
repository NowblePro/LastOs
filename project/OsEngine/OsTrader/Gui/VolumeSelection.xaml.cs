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

namespace OsEngine.OsTrader.Gui
{
    /// <summary>
    /// Логика взаимодействия для VolumeSelection.xaml
    /// </summary>
    public partial class VolumeSelection : Window
    {
        public VolumeSelection()
        {
            InitializeComponent();
        }

        public string SelectedPortfolio => ComboBoxPortfolio.SelectedItem.ToString();

        public void SetPortfolios(List<string> portfolios)
        {
            ComboBoxPortfolio.Items.Clear();
            foreach (string portfolio in portfolios) 
            {
                ComboBoxPortfolio.Items.Add(portfolio);
            }
            ComboBoxPortfolio.SelectedIndex = 0;
        }

        private void bOk_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }
    }
}
