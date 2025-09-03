using OsEngine.Entity;
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

namespace OsEngine.OsOptimizer.Gui
{
    /// <summary>
    /// Логика взаимодействия для PhazePresetLoader.xaml
    /// </summary>
    public partial class PhazePresetLoader : Window
    {
        private OptimizerMaster _master;

        public PhazePresetLoader()
        {
            InitializeComponent();
        }

        public void Init(OptimizerMaster master)
        {
            _master = master;
            UpdatePhazePresetsCombobox();
        }

        private void UpdatePhazePresetsCombobox()
        {
            ComboBoxPhazePresets.Items.Clear();
            foreach (Phazes phazes in _master.PhazePresets)
            {
                ComboBoxPhazePresets.Items.Add(phazes.Name);
            }

            if (ComboBoxPhazePresets.Items.Count > 0 && ComboBoxPhazePresets.SelectedItem == null)
            {
                ComboBoxPhazePresets.SelectedItem = ComboBoxPhazePresets.Items[0];
            }
        }

        private void ButtonPhazePresetLoad_Click(object sender, RoutedEventArgs e)
        {
            string name = ComboBoxPhazePresets.SelectedItem.ToString();
            Phazes phazes = _master.PhazePresets.Where(x => x.Name == name).FirstOrDefault();
            if (phazes != null)
            {
                _master.Phazes = phazes.GetClone();
            }
            DialogResult = true;
        }
    }
}
