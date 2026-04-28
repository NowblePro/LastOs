using System;
using System.Threading.Tasks;
using System.Windows.Forms.Integration;
using System.Windows.Forms;
using OsEngine.Entity;
using OsEngine.OsTrader.Panels;
using OsEngine.Language;
using System.Threading;
using System.Collections.Generic;
using OsEngine.Market;
using System.Windows.Input;
using OsEngine.Journal;
using OsEngine.Logging;
using Excel = Microsoft.Office.Interop.Excel;
using System.IO;
using System.Linq;
using System.Globalization;
using System.Text;
using OsEngine.Common.UI;
using OsEngine.Market.Servers.Tester;
using OsEngine.OsTrader.Panels.Tab;

namespace OsEngine.OsTrader.Gui
{
    public class BotTabsPainter
    {
        public BotTabsPainter(OsTraderMaster master, WindowsFormsHost host)
        {
            _master = master;
            _host = host;

            CreateTable(master._startProgram);
            RePaintTable(); 
            _master.BotCreateEvent += _master_NewBotCreateEvent;
            _master.BotDeleteEvent += _master_BotDeleteEvent;
            _master.UserClickOnPositionShowBotInTableEvent += _master_UserClickOnPositionShowBotInTableEvent;
            Thread painterThread = new Thread(UpdaterThreadArea);
            painterThread.Start();
            PhaseDirectories.Load();
        }

        private void _master_BotDeleteEvent(Panels.BotPanel obj)
        {
            RePaintTable();
        }

        private void _master_NewBotCreateEvent(Panels.BotPanel obj)
        {
            RePaintTable();
        }

        OsTraderMaster _master;

        WindowsFormsHost _host;

        DataGridView _grid;

        private void CreateTable(StartProgram startProgram)
        {
            DataGridView newGrid =
             DataGridFactory.GetDataGridView(DataGridViewSelectionMode.CellSelect,
             DataGridViewAutoSizeRowsMode.AllCells);

            newGrid.ScrollBars = ScrollBars.Vertical;
            newGrid.EditMode = DataGridViewEditMode.EditOnEnter;

            newGrid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            DataGridViewTextBoxCell cell0 = new DataGridViewTextBoxCell();
            cell0.Style = newGrid.DefaultCellStyle;

            DataGridViewColumn colum0 = new DataGridViewColumn();
            colum0.CellTemplate = cell0;
            colum0.HeaderText = OsLocalization.Trader.Label165; //"Num";
            colum0.ReadOnly = true;
            colum0.AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
            newGrid.Columns.Add(colum0);

            DataGridViewColumn colum01 = new DataGridViewColumn();
            colum01.CellTemplate = cell0;
            colum01.HeaderText = OsLocalization.Trader.Label175;//"Name";
            colum01.ReadOnly = false;
            colum01.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            newGrid.Columns.Add(colum01);

            DataGridViewColumn colum02 = new DataGridViewColumn();
            colum02.CellTemplate = cell0;
            colum02.HeaderText = OsLocalization.Trader.Label167;//"Type";
            colum02.ReadOnly = true;
            colum02.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            newGrid.Columns.Add(colum02);

            DataGridViewColumn colum04 = new DataGridViewColumn();
            colum04.CellTemplate = cell0;
            colum04.HeaderText = OsLocalization.Trader.Label176;//"First Security";
            colum04.ReadOnly = startProgram != StartProgram.IsTester;
            colum04.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            newGrid.Columns.Add(colum04);

            DataGridViewColumn colum05 = new DataGridViewColumn();
            colum05.CellTemplate = cell0;
            colum05.HeaderText = OsLocalization.Trader.Label186;//"Position";
            colum05.ReadOnly = true;
            colum05.Width = 120;
            newGrid.Columns.Add(colum05);

            DataGridViewCheckBoxColumn column06 = new DataGridViewCheckBoxColumn();
            column06.HeaderText = OsLocalization.Trader.Label184; // On/off
            column06.ReadOnly = false;
            column06.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleCenter;
            column06.AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
            newGrid.Columns.Add(column06);

            DataGridViewCheckBoxColumn column07 = new DataGridViewCheckBoxColumn();
            column07.HeaderText = OsLocalization.Trader.Label185; // Emulator on/off
            column07.ReadOnly = false;
            column07.AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
            column07.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleCenter;
            newGrid.Columns.Add(column07);

            if(startProgram != StartProgram.IsOsTrader)
            {
                column07.ReadOnly = true;
            }

            DataGridViewButtonColumn colum08 = new DataGridViewButtonColumn();
            //colum06.CellTemplate = cell0;
            //colum06.HeaderText = "Chart";
            colum08.ReadOnly = true;
            colum08.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            newGrid.Columns.Add(colum08);

            DataGridViewButtonColumn colum09 = new DataGridViewButtonColumn();
            //colum07.CellTemplate = cell0;
            //colum07.HeaderText = "Parameters";
            colum09.ReadOnly = true;
            colum09.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            newGrid.Columns.Add(colum09);

            DataGridViewButtonColumn colum11 = new DataGridViewButtonColumn();
            // colum09.CellTemplate = cell0;
            //colum09.HeaderText = "Action";
            colum11.ReadOnly = true;
            colum11.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            newGrid.Columns.Add(colum11);

            DataGridViewButtonColumn colum12 = new DataGridViewButtonColumn();
            colum12.ReadOnly = true;
            colum12.AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
            newGrid.Columns.Add(colum12);

            _grid = newGrid;
            _host.Child = _grid;

            _grid.Click += _grid_Click;
            _grid.CellClick += _grid_CellClick;
            _grid.CellBeginEdit += _grid_CellBeginEdit;
            _grid.CellEndEdit += _grid_CellEndEdit;
            _grid.EditingControlShowing += _grid_EditingControlShowing;
            _grid.MouseLeave += _grid_MouseLeave;
            _grid.CellMouseClick += _grid_CellMouseClick;
            _grid.DataError += _grid_DataError;
        }

        private void _grid_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            try
            {
                if (e.RowIndex < 0 || e.ColumnIndex != 3)
                {
                    return;
                }

                if (_grid.Rows.Count <= e.RowIndex ||
                    _grid.Rows[e.RowIndex].Cells.Count <= e.ColumnIndex ||
                    !(_grid.Rows[e.RowIndex].Cells[e.ColumnIndex] is DataGridViewComboBoxCell))
                {
                    return;
                }

                _grid.CurrentCell = _grid.Rows[e.RowIndex].Cells[e.ColumnIndex];
                _grid.BeginEdit(true);

                if (_grid.EditingControl is ComboBox comboBox)
                {
                    comboBox.DroppedDown = true;
                }
            }
            catch (Exception ex)
            {
                _master.SendNewLogMessage(ex.ToString(), Logging.LogMessageType.Error);
            }
        }

        private void _grid_CellEndEdit(object sender, DataGridViewCellEventArgs e)
        {
            try
            {
                if (e.ColumnIndex == 3)
                {
                    ApplyTesterBotSecurityChange(e.RowIndex);
                    return;
                }

                if (e.ColumnIndex != 1)
                {
                    return;
                }

                if (_master.PanelsArray == null ||
                    _master.PanelsArray.Count == 0)
                {
                    return;
                }

                int rowIndex = e.RowIndex;

                if (rowIndex >= _grid.Rows.Count)
                {
                    return;
                }

                if (rowIndex >= _master.PanelsArray.Count)
                {
                    return;
                }

                string newName = null;

                if (_grid.Rows[rowIndex].Cells[1].Value != null)
                {
                    newName = _grid.Rows[rowIndex].Cells[1].Value.ToString();
                    newName = newName.Replace("@", "");
                }
                else
                {
                    newName = _master.PanelsArray[rowIndex].NameStrategyUniq;
                    _grid.Rows[rowIndex].Cells[1].Value = newName;
                }

                _master.PanelsArray[rowIndex].PublicName = newName;
                _master.Save();
            }
            catch (Exception ex)
            {
                _master.SendNewLogMessage(ex.ToString(), Logging.LogMessageType.Error);
            }
        }

        private void _grid_DataError(object sender, DataGridViewDataErrorEventArgs e)
        {
            if (e != null)
            {
                e.Cancel = false;
                e.ThrowException = false;
            }
        }

        private void _grid_EditingControlShowing(object sender, DataGridViewEditingControlShowingEventArgs e)
        {
            try
            {
                if (_grid.CurrentCell == null ||
                    _grid.CurrentCell.ColumnIndex != 3)
                {
                    return;
                }

                if (e.Control is ComboBox comboBox)
                {
                    comboBox.DroppedDown = true;
                }
            }
            catch (Exception ex)
            {
                _master.SendNewLogMessage(ex.ToString(), Logging.LogMessageType.Error);
            }
        }

        private void _grid_MouseLeave(object sender, EventArgs e)
        {
            try
            {
                _grid.ClearSelection();
            }
            catch (Exception ex) 
            {
                _master.SendNewLogMessage(ex.ToString(),Logging.LogMessageType.Error);
            }
        }

		private int _prevActiveRow;

        private void _grid_Click(object sender, EventArgs e)
        {
            try
            {
                System.Windows.Forms.MouseEventArgs mouse = (System.Windows.Forms.MouseEventArgs)e;

                if (mouse.Button == MouseButtons.Right)
                {
                    _mouseXPos = mouse.X;
                    _mouseYPos = mouse.Y;
                    return;
                }

                if (_grid.SelectedCells.Count == 0)
                {
                    return;
                }

                int coluIndex = _grid.SelectedCells[0].ColumnIndex;

                int rowIndex = _grid.SelectedCells[0].RowIndex;

                if(coluIndex < 3)
                {
                    return;
                }

                /*
    colum0.HeaderText = "Num";
    colum01.HeaderText = "Name";
    colum02.HeaderText = "Type";
    colum03.HeaderText = "First Security";
    colum04.HeaderText = "Position";
    colum05.HeaderText = "On/off";
    colum06.HeaderText = "Emulator on/off";
    colum07.HeaderText = "Chart";
    colum08.HeaderText = "Parameters";
    colum9.HeaderText = "Journal";
    colum10.HeaderText = "Action";
    */

                int botsCount = 0;

                if (_master.PanelsArray != null)
                {
                    botsCount = _master.PanelsArray.Count;
                }

                BotPanel bot = null;

                if (rowIndex < botsCount)
                {
                    bot = _master.PanelsArray[rowIndex];
                }

                if (coluIndex == 7 &&
                    rowIndex < botsCount)
                { // вызываем чарт робота
                    bot.ShowChartDialog();
                }
                else if (coluIndex == 8 &&
       rowIndex < botsCount)
                { // вызываем параметры
                    bot.ShowParametrDialog();
                }
                else if (coluIndex == 9 &&
        rowIndex < botsCount)
                { // вызываем окно удаление робота
                    _master.DeleteByNum(rowIndex);
                }
                else if (coluIndex == 10 &&
        rowIndex < botsCount)
                {
                    _master.DuplicateBot(rowIndex);
                }

                if (coluIndex == 8 &&
         rowIndex == botsCount + 1)
                { // вызываем общий журнал
                    _master.ShowCommunityJournal(2, 0, 0);
                }
                else if (coluIndex == 9 &&
        rowIndex == botsCount + 1)
                { // вызываем добавление нового бота
                    _master.CreateNewBot();
                }
                else if (coluIndex == 7 && rowIndex == botsCount + 1)
                {
                    LoadBotVolumesFromExcel();
                }
                else if (coluIndex == 7 && rowIndex == botsCount + 2)
                {
                    SetLongShortButtons();
                }
                else if (coluIndex == 8 && rowIndex == botsCount + 2)
                {
                    ChooseLongPhase();
                }
                else if (coluIndex == 9 && rowIndex == botsCount + 2)
                {
                    ChooseShortPhase();
                }

                if (_grid.Rows.Count <= _prevActiveRow)
                {
                    _prevActiveRow = rowIndex;
                    return;
                }

                _grid.Rows[_prevActiveRow].DefaultCellStyle.ForeColor = System.Drawing.Color.FromArgb(154, 156, 158);
                _grid.Rows[rowIndex].DefaultCellStyle.ForeColor = System.Drawing.Color.FromArgb(255, 255, 255);
                _prevActiveRow = rowIndex;
            }
            catch(Exception error)
            {
                _master.SendNewLogMessage(error.ToString(),Logging.LogMessageType.Error);
            }
        }

        #region Pop-up menu

        private int _mouseXPos;

        private int _mouseYPos;

        BotPanel _lastSelectedBot;

        private void LoadBotVolumesFromExcel()
        {
            using (OpenFileDialog dialog = new OpenFileDialog())
            {
                dialog.Filter = "Excel Files|*.xls;*.xlsx;*.xlsm|All files (*.*)|*.*";
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    if (string.IsNullOrEmpty(dialog.FileName) || !File.Exists(dialog.FileName)) return;
                    List<string> portfolios = PhaseDirectories.GetPortfolioNames(dialog.FileName);

                    VolumeSelection selection = new VolumeSelection();
                    selection.SetPortfolios(portfolios);
                    bool selected = selection.ShowDialog() ?? false;

                    if (!selected)
                    {
                        return;
                    }
                    string portfolioName = selection.SelectedPortfolio;

                    LoadVolumeFile(dialog.FileName, portfolioName);
                }
            }
        }

        private void LoadVolumeFile(string path, string portfolioName)
        {
            Excel.Application excelApp = new Excel.Application(); // Создание экземпляра Excel-приложения
            Excel.Workbooks workbooks = excelApp.Workbooks;
            Excel.Workbook workbook = workbooks.Open(path); // Открытие файла
            List<BotPanel> zeroVolumes = new List<BotPanel>();
            try
            {
                bool volumeSheetFound = false;
                foreach (Excel.Worksheet sheet in workbook.Sheets)
                {
                    Excel.Range usedRange = sheet.UsedRange; // Получение диапазона используемых ячеек

                    int rowsCount = usedRange.Rows.Count;

                    List<string> portfolios = new List<string>();
                    for (int i = 1; i <= rowsCount + 1; i++)
                    {
                        object cell = ((Excel.Range)usedRange.Cells[i, 1]).Value2;
                        string portfolio = $"{cell}";
                        if (!string.IsNullOrEmpty(portfolio))
                        {
                            portfolios.Add(portfolio);
                        }
                    }

                    int volumeRowIndex = -1;
                    for (int i = 1; i <= rowsCount + 1; i++)
                    {
                        object cell = ((Excel.Range)usedRange.Cells[i, 1]).Value2;
                        if (cell != null && $"{cell}" == portfolioName)
                        {
                            volumeRowIndex = i;
                            break;
                        }
                    }
                    if (volumeRowIndex < 0) throw new Exception("Не найдена строка с объёмами");

                    int namesRowIndex = 1;
                    int namesColumnStart = 2;
                    Dictionary<string, string> errors = new Dictionary<string, string>();
                    List<string> nameList = new List<string>();
                    for (int i = namesColumnStart; i < usedRange.Columns.Count + 1; i++)
                    {
                        object botName = ((Excel.Range)usedRange.Cells[namesRowIndex, i]).Value2;
                        try
                        {
                            object volume = ((Excel.Range)usedRange.Cells[volumeRowIndex, i]).Value2;
                            string botNameStr = $"{botName}";
                            nameList.Add(botNameStr);
                            BotPanel bot = GetBot(botNameStr);
                            if (bot == null) continue;
                            if (decimal.TryParse($"{volume}".Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out decimal decimalVolume))
                            {
                                bot.SetVolume(decimalVolume, VolumeType.Percent);
                                volumeSheetFound = true;
                            }
                            else
                            {
                                throw new Exception("Не удалось спарсить значение объёма");
                            }
                        }
                        catch (Exception ex)
                        {
                            errors.Add($"{botName}", $"{botName}: {ex.Message}");
                        }
                    }

                    zeroVolumes = GetBots(b => !nameList.Contains(string.IsNullOrEmpty(b.PublicName) ? b.NameStrategyUniq : b.PublicName));
                    foreach (BotPanel bot in zeroVolumes)
                    {
                        try
                        {
                            bot.SetVolume(0, VolumeType.Percent);
                        }
                        catch (Exception ex)
                        {
                            if (errors.ContainsKey($"{bot.FileName}"))
                            {
                                errors[$"{bot.FileName}"] += $"\r\n{bot.FileName}: {ex.Message}";
                            }
                            else
                            {
                                errors.Add($"{bot.FileName}", $"{bot.FileName}: {ex.Message}");
                            }
                        }
                    }

                    StringBuilder errorBuilder = new StringBuilder();
                    foreach (var pair in errors)
                    {
                        errorBuilder.AppendLine($"{pair.Key}: {pair.Value}");
                    }

                    if (errorBuilder.Length > 0)
                    {
                        throw new Exception(errorBuilder.ToString());
                    }

                    List<BotPanel> GetBots(Func<BotPanel, bool> predicate)
                    {
                        if (predicate == null) return null;
                        return _master.PanelsArray.FindAll(p => predicate(p));
                    }

                    BotPanel GetBot(string name)
                    {
                        for (int i = 0; i < _grid.Rows.Count; i++)
                        {
                            string botName = _grid.Rows[i].Cells[1].Value?.ToString() ?? "";
                            if (name == botName)
                            {
                                return _master.PanelsArray[i];
                            }
                        }
                        return null;
                    }

                    if (volumeSheetFound) break;
                }
                if (zeroVolumes.Count > 0)
                {
                    StringBuilder sb = new StringBuilder();
                    foreach (var volume in zeroVolumes)
                    {
                        string botName = string.IsNullOrEmpty(volume.PublicName) ? volume.NameStrategyUniq : volume.PublicName;
                        sb.AppendLine(botName);
                    }
                    MessageBox.Show($"Роботы с нулевым объёмом:\r\n{sb}");
                }
            }
            finally
            {
                workbook.Close(false); // Закрываем книгу без сохранения изменений
                excelApp.Quit();       // Завершаем приложение Excel
            }
        }

        private void _grid_CellMouseClick(object sender, DataGridViewCellMouseEventArgs e)
        {
            try
            {
                if (e.Button != MouseButtons.Right)
                {
                    return;
                }

                int rowIndex = e.RowIndex;
                int columnIndex = e.ColumnIndex;

                if(rowIndex >= _master.PanelsArray.Count
                    || rowIndex < 0)
                {
                    return;
                }

                _lastSelectedBot = _master.PanelsArray[rowIndex];

                List<MenuItem> items = new List<MenuItem>();

                items.Add(new MenuItem(_lastSelectedBot.GetNameStrategyType() + "  " + _lastSelectedBot.NameStrategyUniq));
                items[0].Enabled = false;

                items.Add(new MenuItem(OsLocalization.Trader.Label172));
                items[1].Click += BotTabsPainter_Chart_Click;

                items.Add(new MenuItem(OsLocalization.Trader.Label45));
                items[2].Click += BotTabsPainter_Parameters_Click;

                items.Add(new MenuItem(OsLocalization.Trader.Label40));
                items[3].Click += BotTabsPainter_Journal_Click;

                if(_lastSelectedBot.OnOffEventsInTabs == true)
                {
                    items.Add(new MenuItem(OsLocalization.Trader.Label412));
                }
                else //if (selectedBot.OnOffEventsInTabs == false)
                {
                    items.Add(new MenuItem(OsLocalization.Trader.Label413));
                }
                items[4].Click += BotTabsPainter_OnOffEvents_Click;

                if (_lastSelectedBot.OnOffEmulatorsInTabs == true)
                {
                    items.Add(new MenuItem(OsLocalization.Trader.Label414));
                }
                else //if (selectedBot.OnOffEventsInTabs == false)
                {
                    items.Add(new MenuItem(OsLocalization.Trader.Label415));
                }
                if(_master._startProgram == StartProgram.IsTester)
                {
                    items[5].Enabled = false;
                }
                items[5].Click += BotTabsPainter_OnOffEmulator_Click;

                items.Add(new MenuItem(OsLocalization.Trader.Label416));
                items[6].Click += BotTabsPainter_MoveUp_Click;

                items.Add(new MenuItem(OsLocalization.Trader.Label417));
                items[7].Click += BotTabsPainter_MoveDown_Click;

                items.Add(new MenuItem("Копия"));
                items[8].Click += BotTabsPainter_Duplicate_Click;

                items.Add(new MenuItem(OsLocalization.Trader.Label39));
                items[9].Click += BotTabsPainter_Delete_Click;

                ContextMenu menu = new ContextMenu(items.ToArray());

                _grid.ContextMenu = menu;
                _grid.ContextMenu.Show(_grid, new System.Drawing.Point(_mouseXPos, _mouseYPos));
            }
            catch (Exception ex)
            {
                _master.SendNewLogMessage(ex.ToString(), Logging.LogMessageType.Error);
            }
        }

        private void SetLongShortButtons()
        {
            PhaseDirectories.Instance.Show();
        }

        private void ChooseLongPhase()
        {
            if (!string.IsNullOrEmpty(PhaseDirectories.Settings.LongPath))
            {
                if (File.Exists(PhaseDirectories.Settings.LongPath))
                {
                    LoadVolumeFile(PhaseDirectories.Settings.LongPath, PhaseDirectories.Settings.LongPortfolio);
                }
                else
                {
                    MessageBox.Show($"Файл не найден {PhaseDirectories.Settings.LongPath}");
                }
            }
            else
            {
                MessageBox.Show($"Укажите файл");
            }
        }

        private void ChooseShortPhase()
        {
            if (!string.IsNullOrEmpty(PhaseDirectories.Settings.ShortPath))
            {
                if (File.Exists(PhaseDirectories.Settings.ShortPath))
                {
                    LoadVolumeFile(PhaseDirectories.Settings.ShortPath, PhaseDirectories.Settings.ShortPortfolio);
                }
                else
                {
                    MessageBox.Show($"Файл не найден {PhaseDirectories.Settings.ShortPath}");
                }
            }
            else
            {
                MessageBox.Show($"Укажите файл");
            }
        }

        private void BotTabsPainter_Chart_Click(object sender, EventArgs e)
        {
            try
            {
                _lastSelectedBot.ShowChartDialog();
            }
            catch(Exception ex)
            {
                _master.SendNewLogMessage(ex.ToString(),Logging.LogMessageType.Error);
            }
        }

        private void BotTabsPainter_Parameters_Click(object sender, EventArgs e)
        {
            try
            {
                _lastSelectedBot.ShowParametrDialog();
            }
            catch (Exception ex)
            {
                _master.SendNewLogMessage(ex.ToString(), Logging.LogMessageType.Error);
            }
        }

        private void BotTabsPainter_Journal_Click(object sender, EventArgs e)
        {
            try
            {
                string journalName = 
                    "Journal2Ui_" + _lastSelectedBot.NameStrategyUniq + _master._startProgram.ToString();

                for(int i = 0;i < _journalUi.Count;i++)
                {
                    if (_journalUi[i].JournalName == journalName)
                    {
                        _journalUi[i].Activate();
                        return;
                    }
                }

                List<BotPanelJournal> panelsJournal = new List<BotPanelJournal>();

                List<Journal.Journal> journals = _lastSelectedBot.GetJournals();


                BotPanelJournal botPanel = new BotPanelJournal();
                botPanel.BotName = _lastSelectedBot.NameStrategyUniq;
                botPanel.BotClass = _lastSelectedBot.GetNameStrategyType();

                botPanel._Tabs = new List<BotTabJournal>();

                for (int i2 = 0; journals != null && i2 < journals.Count; i2++)
                {
                    BotTabJournal botTabJournal = new BotTabJournal();
                    botTabJournal.TabNum = i2;
                    botTabJournal.Journal = journals[i2];
                    botPanel._Tabs.Add(botTabJournal);
                }

                panelsJournal.Add(botPanel);

                _journalUi.Add(new JournalUi2(panelsJournal, _lastSelectedBot.StartProgram));
                _journalUi[_journalUi.Count-1].Closed += _journalUi_Closed;
                _journalUi[_journalUi.Count - 1].LogMessageEvent += _journalUi_LogMessageEvent;
                _journalUi[_journalUi.Count - 1].Show();
            }
            catch (Exception error)
            {
                _master.SendNewLogMessage(error.ToString(), LogMessageType.Error);
            }
        }

        private List<JournalUi2> _journalUi = new List<JournalUi2>();

        private void _journalUi_LogMessageEvent(string message, LogMessageType type)
        {
            if (_master == null)
            {
                return;
            }
            _master.SendNewLogMessage(message, type);
        }

        private void _journalUi_Closed(object sender, EventArgs e)
        {
            try
            {
                JournalUi2 myJournal = (JournalUi2)sender;

                for (int i = 0; i < _journalUi.Count; i++)
                {
                    if (_journalUi[i].JournalName == myJournal.JournalName)
                    {
                        _journalUi[i].Closed -= _journalUi_Closed;
                        _journalUi[i].LogMessageEvent -= _journalUi_LogMessageEvent;
                        _journalUi[i].IsErase = true;
                        _journalUi.RemoveAt(i);
                        break;
                    }
                }
            }
            catch (Exception error)
            {
                _master.SendNewLogMessage(error.ToString(), LogMessageType.Error);
            }
        }

        private void BotTabsPainter_OnOffEvents_Click(object sender, EventArgs e)
        {
            try
            {
                if(_lastSelectedBot.OnOffEventsInTabs == true)
                {
                    _lastSelectedBot.OnOffEventsInTabs = false;
                }
                else
                {
                    _lastSelectedBot.OnOffEventsInTabs = true;
                }
            }
            catch (Exception ex)
            {
                _master.SendNewLogMessage(ex.ToString(), Logging.LogMessageType.Error);
            }
        }

        private void BotTabsPainter_OnOffEmulator_Click(object sender, EventArgs e)
        {
            try
            {
                if (_lastSelectedBot.OnOffEmulatorsInTabs == true)
                {
                    _lastSelectedBot.OnOffEmulatorsInTabs = false;
                }
                else
                {
                    _lastSelectedBot.OnOffEmulatorsInTabs = true;
                }
            }
            catch (Exception ex)
            {
                _master.SendNewLogMessage(ex.ToString(), Logging.LogMessageType.Error);
            }
        }

        private void BotTabsPainter_MoveUp_Click(object sender, EventArgs e)
        {
            try
            {
                int rowIndex = -1;

                for (int i = 1; i < _master.PanelsArray.Count; i++)
                {
                    if (_master.PanelsArray[i].NameStrategyUniq == _lastSelectedBot.NameStrategyUniq)
                    {
                        BotPanel panel = _master.PanelsArray[i];
                        _master.PanelsArray[i] = _master.PanelsArray[i - 1];
                        _master.PanelsArray[i - 1] = panel;
                        _master.Save();
                        RePaintTable();
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                _master.SendNewLogMessage(ex.ToString(), Logging.LogMessageType.Error);
            }
        }

        private void BotTabsPainter_MoveDown_Click(object sender, EventArgs e)
        {
            try
            {
                int rowIndex = -1;

                for (int i = 0; i < _master.PanelsArray.Count-1; i++)
                {
                    if (_master.PanelsArray[i].NameStrategyUniq == _lastSelectedBot.NameStrategyUniq)
                    {
                        BotPanel panel = _master.PanelsArray[i];
                        _master.PanelsArray[i] = _master.PanelsArray[i + 1];
                        _master.PanelsArray[i + 1] = panel;
                        _master.Save();
                        RePaintTable();
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                _master.SendNewLogMessage(ex.ToString(), Logging.LogMessageType.Error);
            }

        }

        private void BotTabsPainter_Delete_Click(object sender, EventArgs e)
        {
            try
            {
                int rowIndex = -1;

                for(int i = 0;i < _master.PanelsArray.Count;i++)
                {
                    if (_master.PanelsArray[i].NameStrategyUniq == _lastSelectedBot.NameStrategyUniq)
                    {
                        rowIndex = i;
                        break;
                    }
                }

                if(rowIndex == -1)
                {
                    return;
                }

                _master.DeleteByNum(rowIndex);
            }
            catch (Exception ex)
            {
                _master.SendNewLogMessage(ex.ToString(), Logging.LogMessageType.Error);
            }
        }

        private void BotTabsPainter_Duplicate_Click(object sender, EventArgs e)
        {
            try
            {
                if (_master.PanelsArray == null)
                {
                    return;
                }

                int rowIndex = -1;

                for (int i = 0; i < _master.PanelsArray.Count; i++)
                {
                    if (_master.PanelsArray[i].NameStrategyUniq == _lastSelectedBot.NameStrategyUniq)
                    {
                        rowIndex = i;
                        break;
                    }
                }

                if (rowIndex == -1)
                {
                    return;
                }

                _master.DuplicateBot(rowIndex);
            }
            catch (Exception ex)
            {
                _master.SendNewLogMessage(ex.ToString(), Logging.LogMessageType.Error);
            }
        }

        #endregion

        #region работа с чек-боксами включений и отключений

        int _lastChangeRow;

        int _lastChangeColumn;

        private void _grid_CellBeginEdit(object sender, DataGridViewCellCancelEventArgs e)
        {
            try
            {
                if (e.ColumnIndex != 5 &&
                    e.ColumnIndex != 6)
                {
                    return;
                }

                if (_lastTimeClick.AddMilliseconds(500) > DateTime.Now)
                {
                    return;
                }
                _lastTimeClick = DateTime.Now;

                _lastChangeRow = e.RowIndex;
                _lastChangeColumn = e.ColumnIndex;

                Task.Run(ChangeOnOffAwait);
            }
            catch (Exception ex)
            {
                _master.SendNewLogMessage(ex.ToString(), Logging.LogMessageType.Error);
            }
        }

        DateTime _lastTimeClick = DateTime.MinValue;

        private async void ChangeOnOffAwait()
        {
            try
            {
                await Task.Delay(200);
                ChangeFocus();
                await Task.Delay(200);
                ChangeOnOff();
            }
            catch(Exception error)
            {
                System.Windows.MessageBox.Show(error.ToString());
            }
        }

        private void ChangeFocus()
        {
            if (_grid.InvokeRequired)
            {
                _grid.Invoke(new Action(ChangeFocus));
                return;
            }

            _grid.Rows[_lastChangeRow].Cells[0].Selected = true;
        }

        private void ChangeOnOff()
        {
            if (_grid.InvokeRequired)
            {
                _grid.Invoke(new Action(ChangeOnOff));
                return;
            }

            int coluIndex = _lastChangeColumn;
            int rowIndex = _lastChangeRow;

            int botsCount = 0;

            if (_master.PanelsArray != null)
            {
                botsCount = _master.PanelsArray.Count;
            }

            if (coluIndex == 5 &&
                rowIndex < botsCount &&
                _grid.Rows[rowIndex].Cells[5].Value != null)
            {
                string textInCell = _grid.Rows[rowIndex].Cells[5].Value.ToString();
                bool isOn = Convert.ToBoolean(textInCell);

                OnOffBot(rowIndex, isOn);
            }
            if (coluIndex == 5 &&
                rowIndex == botsCount &&
                _grid.Rows[rowIndex].Cells[5].Value != null)
            {
                string textInCell = _grid.Rows[rowIndex].Cells[5].Value.ToString();
                bool isOn = Convert.ToBoolean(textInCell);

                OnOffAll(isOn);
            }

            if (coluIndex == 6 &&
                rowIndex < botsCount &&
                _grid.Rows[rowIndex].Cells[6].Value != null)
            {
                string textInCell = _grid.Rows[rowIndex].Cells[6].Value.ToString();

                bool isOn = Convert.ToBoolean(textInCell);

                OnOffEmulatorBot(rowIndex, isOn);
            }
            if (coluIndex == 6 &&
                rowIndex == botsCount &&
                _grid.Rows[rowIndex].Cells[6].Value != null)
            {
                string textInCell = _grid.Rows[rowIndex].Cells[6].Value.ToString();
                bool isOn = Convert.ToBoolean(textInCell);

                OnOffEmulatorAll(isOn);
            }
        }

        private void OnOffBot(int botNum, bool value)
        {
            BotPanel bot = _master.PanelsArray[botNum];
            bot.OnOffEventsInTabs = value;
        }

        private void OnOffAll(bool value)
        {
            if(_master.PanelsArray == null)
            {
                return;
            }
            for(int i = 0;i < _master.PanelsArray.Count;i++)
            {
                BotPanel bot = _master.PanelsArray[i];
                bot.OnOffEventsInTabs = value;
            }
        }

        private void OnOffEmulatorBot(int botNum, bool value)
        {
            BotPanel bot = _master.PanelsArray[botNum];
            bot.OnOffEmulatorsInTabs = value;
        }

        private void OnOffEmulatorAll(bool value)
        {
            if (_master.PanelsArray == null)
            {
                return;
            }
            for (int i = 0; i < _master.PanelsArray.Count; i++)
            {
                BotPanel bot = _master.PanelsArray[i];
                bot.OnOffEmulatorsInTabs = value;
            }
        }

        #endregion

        private void RePaintTable()
        {
            try
            {
                int lastShowRowIndex = _grid.FirstDisplayedScrollingRowIndex;

                _grid.Rows.Clear();

                for (int i = 0; _master.PanelsArray != null && i < _master.PanelsArray.Count; i++)
                {
                    _grid.Rows.Add(GetRow(_master.PanelsArray[i], i + 1));
                }

                _grid.Rows.Add(GetNullRow());

                _grid.Rows.Add(GetAddRow());
                _grid.Rows.Add(GetPhaseRow());
                if (lastShowRowIndex > 0 &&
                    lastShowRowIndex < _grid.Rows.Count)
                {
                    _grid.FirstDisplayedScrollingRowIndex = lastShowRowIndex;
                    _grid.Rows[lastShowRowIndex].Selected = true;

                    if (_grid.Rows[lastShowRowIndex].Cells != null
                        && _grid.Rows[lastShowRowIndex].Cells[0] != null)
                    {
                        _grid.Rows[lastShowRowIndex].Cells[0].Selected = true;
                    }
                }
            }
            catch (Exception error)
            {
                _master.SendNewLogMessage(error.ToString(), Logging.LogMessageType.Error);
            }
        }

        private DataGridViewRow GetRow(BotPanel bot, int num)
        {
            /*
colum0.HeaderText = "Num";
colum01.HeaderText = "Name";
colum02.HeaderText = "Type";
colum03.HeaderText = "First Security";
colum04.HeaderText = "Position";

colum05.HeaderText = "On/off";
colum06.HeaderText = "Emulator on/off";

colum07.HeaderText = "Chart";
colum08.HeaderText = "Parameters";
colum9.HeaderText = "Journal";
colum10.HeaderText = "Action";
*/
            DataGridViewRow row = new DataGridViewRow();

            row.Cells.Add(new DataGridViewTextBoxCell());
            row.Cells[0].Value = num.ToString();

            row.Cells.Add(new DataGridViewTextBoxCell());

            if(string.IsNullOrEmpty(bot.PublicName) == false)
            {
                row.Cells[1].Value = bot.PublicName;
            }
            else
            {
                row.Cells[1].Value = bot.NameStrategyUniq;
            }
           
            row.Cells.Add(new DataGridViewTextBoxCell());
            row.Cells[2].Value = bot.GetType().Name;

            row.Cells.Add(CreatePrimarySecurityCell(bot));

            row.Cells.Add(new DataGridViewTextBoxCell());
            row.Cells[4].Value = bot.PositionsCount.ToString() + "/" + bot.AllPositionsCount.ToString();

            row.Cells.Add(new DataGridViewCheckBoxCell());
            row.Cells[5].Value = bot.OnOffEventsInTabs;

            row.Cells.Add(new DataGridViewCheckBoxCell());
            row.Cells[6].Value = bot.OnOffEmulatorsInTabs;

            row.Cells.Add(new DataGridViewButtonCell());
            row.Cells[7].Value =  OsLocalization.Trader.Label172;//"Chart";

            row.Cells.Add(new DataGridViewButtonCell());
            row.Cells[8].Value = OsLocalization.Trader.Label45;//"Parameters";

            row.Cells.Add(new DataGridViewButtonCell());
            row.Cells[9].Value = OsLocalization.Trader.Label39;//"Delete";

            row.Cells.Add(new DataGridViewButtonCell());
            row.Cells[10].Value = "Копия";

            if (num % 2 == 0)
            {
                for (int i = 0; i < row.Cells.Count; i++)
                {
                    row.Cells[i].Style.BackColor = System.Drawing.Color.FromArgb(9, 11, 13);
                }
            }

            return row;
        }

        private DataGridViewCell CreatePrimarySecurityCell(BotPanel bot)
        {
            string currentSecurityName = GetPrimarySecurityName(bot);

            if (!CanEditPrimarySecurity(bot))
            {
                DataGridViewTextBoxCell textCell = new DataGridViewTextBoxCell();
                textCell.Value = currentSecurityName;
                return textCell;
            }

            List<string> testerSecurities = GetTesterSecurityNames();

            if (testerSecurities.Count == 0)
            {
                DataGridViewTextBoxCell textCell = new DataGridViewTextBoxCell();
                textCell.Value = currentSecurityName;
                return textCell;
            }

            DataGridViewComboBoxCell comboCell = new DataGridViewComboBoxCell();
            comboCell.DisplayStyle = DataGridViewComboBoxDisplayStyle.ComboBox;
            comboCell.DisplayStyleForCurrentCellOnly = false;
            comboCell.FlatStyle = FlatStyle.Popup;
            comboCell.MaxDropDownItems = 20;

            for (int i = 0; i < testerSecurities.Count; i++)
            {
                comboCell.Items.Add(testerSecurities[i]);
            }

            if (!string.IsNullOrWhiteSpace(currentSecurityName) &&
                comboCell.Items.Contains(currentSecurityName) == false)
            {
                comboCell.Items.Add(currentSecurityName);
            }

            if (!string.IsNullOrWhiteSpace(currentSecurityName))
            {
                comboCell.Value = currentSecurityName;
            }

            return comboCell;
        }

        private string GetPrimarySecurityName(BotPanel bot)
        {
            BotTabSimple tab = GetPrimarySimpleTab(bot);

            if (tab == null)
            {
                return null;
            }

            if (tab.Security != null)
            {
                return tab.Security.Name;
            }

            if (tab.Connector != null &&
                string.IsNullOrWhiteSpace(tab.Connector.SecurityName) == false)
            {
                return tab.Connector.SecurityName;
            }

            return null;
        }

        private BotTabSimple GetPrimarySimpleTab(BotPanel bot)
        {
            if (bot == null ||
                bot.TabsSimple == null ||
                bot.TabsSimple.Count == 0)
            {
                return null;
            }

            return bot.TabsSimple[0];
        }

        private bool CanEditPrimarySecurity(BotPanel bot)
        {
            if (_master == null ||
                _master._startProgram != StartProgram.IsTester)
            {
                return false;
            }

            BotTabSimple tab = GetPrimarySimpleTab(bot);

            return tab != null && tab.Connector != null;
        }

        private List<string> GetTesterSecurityNames()
        {
            TesterServer testerServer = GetTesterServer();

            if (testerServer == null ||
                testerServer.Securities == null)
            {
                return new List<string>();
            }

            return testerServer.Securities
                .Where(security => security != null && string.IsNullOrWhiteSpace(security.Name) == false)
                .Select(security => security.Name)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(name => name)
                .ToList();
        }

        private TesterServer GetTesterServer()
        {
            if (ServerMaster.GetServers() == null)
            {
                return null;
            }

            return ServerMaster.GetServers()
                .FirstOrDefault(server => server != null && server.ServerType == ServerType.Tester) as TesterServer;
        }

        private void ApplyTesterBotSecurityChange(int rowIndex)
        {
            if (_master == null ||
                _master.PanelsArray == null ||
                rowIndex < 0 ||
                rowIndex >= _master.PanelsArray.Count)
            {
                return;
            }

            BotPanel bot = _master.PanelsArray[rowIndex];

            if (!CanEditPrimarySecurity(bot))
            {
                return;
            }

            if (rowIndex >= _grid.Rows.Count ||
                _grid.Rows[rowIndex].Cells.Count <= 3 ||
                _grid.Rows[rowIndex].Cells[3].Value == null)
            {
                return;
            }

            string selectedSecurityName = _grid.Rows[rowIndex].Cells[3].Value.ToString();

            if (string.IsNullOrWhiteSpace(selectedSecurityName))
            {
                return;
            }

            BotTabSimple tab = GetPrimarySimpleTab(bot);

            if (tab == null ||
                tab.Connector == null ||
                string.Equals(tab.Connector.SecurityName, selectedSecurityName, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            TesterServer testerServer = GetTesterServer();
            Security selectedSecurity = testerServer?.Securities?
                .FirstOrDefault(security => security != null &&
                                            string.Equals(security.Name, selectedSecurityName, StringComparison.OrdinalIgnoreCase));

            if (selectedSecurity == null)
            {
                return;
            }

            tab.Connector.SetSecurity(selectedSecurity.Name, selectedSecurity.NameClass);
        }

        private DataGridViewRow GetNullRow()
        {
            /*
colum0.HeaderText = "Num";
colum01.HeaderText = "Name";
colum02.HeaderText = "Type";
colum03.HeaderText = "First Security";
colum04.HeaderText = "Position";

colum05.HeaderText = "On/off";
colum06.HeaderText = "Emulator on/off";

colum07.HeaderText = "Chart";
colum08.HeaderText = "Parameters";
colum9.HeaderText = "Journal";
*/

            DataGridViewRow row = new DataGridViewRow();

            row.Cells.Add(new DataGridViewTextBoxCell());
            row.Cells.Add(new DataGridViewTextBoxCell());
            row.Cells.Add(new DataGridViewTextBoxCell());
            row.Cells.Add(new DataGridViewTextBoxCell());
            row.Cells.Add(new DataGridViewTextBoxCell());

            row.Cells.Add(new DataGridViewCheckBoxCell());
            row.Cells.Add(new DataGridViewCheckBoxCell());

            row.Cells.Add(new DataGridViewButtonCell());
            row.Cells.Add(new DataGridViewButtonCell());
            row.Cells.Add(new DataGridViewButtonCell());
            row.Cells.Add(new DataGridViewButtonCell());

            return row;
        }

        private DataGridViewRow GetAddRow()
        {
            DataGridViewRow row = new DataGridViewRow();

            row.Cells.Add(new DataGridViewTextBoxCell());
            row.Cells.Add(new DataGridViewTextBoxCell());
            row.Cells.Add(new DataGridViewTextBoxCell());
            row.Cells.Add(new DataGridViewTextBoxCell());
            row.Cells.Add(new DataGridViewTextBoxCell());

            row.Cells.Add(new DataGridViewTextBoxCell());
            row.Cells[5].Value = "";

            row.Cells.Add(new DataGridViewTextBoxCell());
            row.Cells[6].Value = "";

            row.Cells.Add(new DataGridViewButtonCell());
            row.Cells[7].Value = "Загрузить объёмы";
            row.Cells.Add(new DataGridViewButtonCell());
            row.Cells[8].Value = OsLocalization.Trader.Label40; //"Journal";
            row.Cells.Add(new DataGridViewButtonCell());
            row.Cells[9].Value = OsLocalization.Trader.Label38; //"Add New...";
            row.Cells.Add(new DataGridViewButtonCell());
            row.Cells[10].Value = "";

            return row;
        }

        private DataGridViewRow GetPhaseRow()
        {
            DataGridViewRow row = new DataGridViewRow();

            row.Cells.Add(new DataGridViewTextBoxCell());
            row.Cells.Add(new DataGridViewTextBoxCell());
            row.Cells.Add(new DataGridViewTextBoxCell());
            row.Cells.Add(new DataGridViewTextBoxCell());
            row.Cells.Add(new DataGridViewTextBoxCell());

            row.Cells.Add(new DataGridViewTextBoxCell());
            row.Cells[5].Value = "";

            row.Cells.Add(new DataGridViewTextBoxCell());
            row.Cells[6].Value = "";

            row.Cells.Add(new DataGridViewButtonCell());
            row.Cells[7].Value = "Выбрать лонг/шорт";
            row.Cells.Add(new DataGridViewButtonCell());
            row.Cells[8].Value = "Лонг";
            row.Cells.Add(new DataGridViewButtonCell());
            row.Cells[9].Value = "Шорт";
            row.Cells.Add(new DataGridViewButtonCell());
            row.Cells[10].Value = "";

            return row;
        }

        private void UpdaterThreadArea()
        {
            while(true)
            {
                Thread.Sleep(2000);

                if (_lastTimeClick.AddSeconds(2) > DateTime.Now)
                {
                    continue;
                }

                if (MainWindow.ProccesIsWorked == false)
                {
                    return;
                }

                UpdateTable();
            }
        }

        private void UpdateTable()
        {
            if(_grid.InvokeRequired)
            {
                _grid.Invoke(new Action(UpdateTable));
                return;
            }

            if (_master.PanelsArray == null)return;
            try
            {
                for (int i = 0; i < _master.PanelsArray.Count; i++)
                {
                    if (_lastTimeClick.AddSeconds(2) > DateTime.Now)
                    {
                        return;
                    }

                    DataGridViewRow row = _grid.Rows[i];

                    BotPanel bot = _master.PanelsArray[i];

                    RefreshPrimarySecurityCell(row, bot);

                    if (bot.TabsSimple.Count != 0 &&
                        bot.TabsSimple[0].Security != null)
                    {
                        if(row.Cells[3].Value == null 
                            ||
                            (row.Cells[3].Value != null 
                            && row.Cells[3].Value.ToString() != bot.TabsSimple[0].Security.Name))
                        {
                            row.Cells[3].Value = bot.TabsSimple[0].Security.Name;
                        }
                    }

                    if (row.Cells[4].Value == null || (row.Cells[4].Value != null && row.Cells[4].Value.ToString() != bot.PositionsCount.ToString() + "/" + bot.AllPositionsCount.ToString()))
                    {
                        row.Cells[4].Value = bot.PositionsCount.ToString() + "/" + bot.AllPositionsCount.ToString();
                    }

                    if (row.Cells[5].Value == null ||
                       (row.Cells[5].Value != null
                       && row.Cells[5].Value.ToString() != bot.OnOffEventsInTabs.ToString()))
                    {
                        row.Cells[5].Value = bot.OnOffEventsInTabs;
                    }

                    if (row.Cells[6].Value == null ||
                       (row.Cells[6].Value != null
                        && row.Cells[6].Value.ToString() != bot.OnOffEmulatorsInTabs.ToString()))
                    {
                        row.Cells[6].Value = bot.OnOffEmulatorsInTabs;
                    }

                }
            }
            catch (Exception error)
            {
                _master.SendNewLogMessage(error.ToString(), Logging.LogMessageType.Error);
            }
        }

        private void RefreshPrimarySecurityCell(DataGridViewRow row, BotPanel bot)
        {
            if (row == null ||
                row.Cells == null ||
                row.Cells.Count <= 3)
            {
                return;
            }

            DataGridViewCell currentCell = row.Cells[3];
            bool shouldBeCombo = CanEditPrimarySecurity(bot) && GetTesterSecurityNames().Count > 0;
            bool isCombo = currentCell is DataGridViewComboBoxCell;

            if (shouldBeCombo == isCombo)
            {
                return;
            }

            DataGridViewCell newCell = CreatePrimarySecurityCell(bot);
            newCell.Style.BackColor = currentCell.Style.BackColor;
            newCell.Style.ForeColor = currentCell.Style.ForeColor;
            newCell.Style.SelectionBackColor = currentCell.Style.SelectionBackColor;
            newCell.Style.SelectionForeColor = currentCell.Style.SelectionForeColor;
            row.Cells[3] = newCell;
        }

        #region подсветка робота по клику по позиции

        private void _master_UserClickOnPositionShowBotInTableEvent(string botTabName)
        {
            if(_rowToPaintInOpenPoses != -1)
            {
                return;
            }

            int botNum = 0;

            bool findTheBot = false;

            for(int i = 0;i < _master.PanelsArray.Count;i++)
            {
                for(int i2 = 0;i2 < _master.PanelsArray[i].TabsSimple.Count; i2++)
                {
                    if (_master.PanelsArray[i].TabsSimple[i2].TabName == botTabName)
                    {
                        botNum = i;
                        findTheBot = true;
                        break;
                    }
                }

                if(findTheBot)
                {
                    break;
                }
            }

            if(findTheBot)
            {
                _rowToPaintInOpenPoses = botNum;
               Task.Run(PaintPos);
            }
        }

        int _rowToPaintInOpenPoses = -1;

        System.Drawing.Color _lastBackColor;

        private async void PaintPos()
        {
            await Task.Delay(200);
            ColoredRow(System.Drawing.Color.LightSlateGray);
            await Task.Delay(600);
            ColoredRow(_lastBackColor);
            _rowToPaintInOpenPoses = -1;
        }

        private void ColoredRow(System.Drawing.Color color)
        {
            if (_grid.InvokeRequired)
            {
                _grid.Invoke(new Action<System.Drawing.Color>(ColoredRow), color);
                return;
            }
            try
            {
                _lastBackColor = _grid.Rows[_rowToPaintInOpenPoses].Cells[0].Style.BackColor;

                for(int i =0;i < 7;i++)
                {
                    _grid.Rows[_rowToPaintInOpenPoses].Cells[i].Style.BackColor = color;
                }
            }
            catch
            {
                return;
            }
        }

        #endregion

    }
}
