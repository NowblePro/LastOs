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
    /// Логика взаимодействия для PhazePresetSaver.xaml
    /// </summary>
    public partial class PhazePresetSaver : Window
    {
        private OptimizerMaster _master;
        private Phazes _phazes;

        public PhazePresetSaver()
        {
            InitializeComponent();
        }

        public void Init(OptimizerMaster master)
        {
            _master = master;
            _phazes = master.Phazes;

            UpdateName(_phazes.Name);
        }

        private void UpdateName(string name)
        {
            if (!TextBoxPhazePresetName.Dispatcher.CheckAccess())
            {
                TextBoxPhazePresetName.Dispatcher.Invoke((Action<string>)UpdateName, name);
                return;
            }

            TextBoxPhazePresetName.Text = name;
        }

        private void ButtonPhazePresetSave_Click(object sender, RoutedEventArgs e)
        {
            _master.Phazes.Name = TextBoxPhazePresetName.Text;
            if (string.IsNullOrEmpty(_master.Phazes.Name))
            {
                MessageBox.Show("Поле имя должно быть заполнено");
                TextBoxPhazePresetName.Focus();
                return;
            }

            Phazes phazes = _master.PhazePresets.Where(p => p.Name == _master.Phazes.Name).FirstOrDefault();
            if (phazes == null)
            {
                phazes = _master.Phazes.GetClone();
            }
            else
            {
                MessageBoxResult result = MessageBox.Show("Пресет с таким именем уже существует, перезаписать?", "", MessageBoxButton.YesNoCancel);
                if (result != MessageBoxResult.Yes)
                {
                    return;
                }

                _master.PhazePresets.Remove(phazes);
                phazes = _master.Phazes.GetClone();
            }

            _master.PhazePresets.Add(phazes);
            _master.OnPeriodsChanged();
            DialogResult = true;
        }
    }
}
