using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;
using System.Windows.Forms.Integration;
using OsEngine.Charts;
using OsEngine.Entity;
using OsEngine.Language;
using OsEngine.Logging;
using OsEngine.OsOptimizer.OptEntity;
using OsEngine.OsTrader.Panels;

namespace OsEngine.OsOptimizer
{
    public class OptimizerReportCharting
    {
        private DataGridViewTextBoxCell cell0 = new DataGridViewTextBoxCell();

        public OptimizerReportCharting(
            WindowsFormsHost hostStepsOfOptimization,
            WindowsFormsHost hostRobustness,
            WindowsFormsHost hostStepsOfOptimizationCSC,
            System.Windows.Controls.ComboBox boxTypeSort,
            System.Windows.Controls.Label labelRobustnessMetricValue,
            System.Windows.Controls.ComboBox boxTypeSortBotNum,
            System.Windows.Controls.ComboBox boxTypeSortCSC,
            System.Windows.Controls.ComboBox boxTypeSortBotNumCSC
            )
        {
            _sortBotsType = SortBotsType.TotalProfit;
            _currentCulture = OsLocalization.CurCulture;
            _hostStepsOfOptimization = hostStepsOfOptimization;
            _hostStepsOfOptimizationCSC = hostStepsOfOptimizationCSC;
            _hostRobustness = hostRobustness;
            _labelRobustnessMetricValue = labelRobustnessMetricValue;

            boxTypeSort.Items.Add(SortBotsType.PositionCount.ToString());
            boxTypeSort.Items.Add(SortBotsType.TotalProfit.ToString());
            boxTypeSort.Items.Add(SortBotsType.MaxDrowDawn.ToString());
            boxTypeSort.Items.Add(SortBotsType.AverageProfit.ToString());
            boxTypeSort.Items.Add(SortBotsType.AverageProfitPercent.ToString());
            boxTypeSort.Items.Add(SortBotsType.ProfitFactor.ToString());
            boxTypeSort.Items.Add(SortBotsType.PayOffRatio.ToString());
            boxTypeSort.Items.Add(SortBotsType.Recovery.ToString());
            boxTypeSort.Items.Add(SortBotsType.SharpRatio.ToString());
            boxTypeSort.Items.Add(SortBotsType.SmaDeviation.ToString());

            boxTypeSort.SelectedItem = SortBotsType.TotalProfit.ToString();
            boxTypeSort.SelectionChanged += _gridResults_SelectionChanged;

            _boxTypeSort = boxTypeSort;

            _boxTypeSortBotNum = boxTypeSortBotNum;
            _boxTypeSortBotNumCSC = boxTypeSortBotNumCSC;

            for (int i = 0; i < 99; i++)
            {
                _boxTypeSortBotNum.Items.Add(i.ToString());
                _boxTypeSortBotNumCSC.Items.Add(i.ToString());
            }

            _boxTypeSortBotNum.SelectedItem = "0";
            _boxTypeSortBotNum.SelectionChanged += _boxTypeSortBotNum_SelectionChanged;

            _boxTypeSortBotNumCSC.SelectedItem = "0";
            _boxTypeSortBotNumCSC.SelectionChanged += _boxTypeSortBotNumCSC_SelectionChanged; ;

            _boxTypeSortCSC = boxTypeSortCSC;

            string[] cscSortTypes = Enum.GetNames(typeof(CSCSortType));
            foreach (string sortType in cscSortTypes)
            {
                boxTypeSortCSC.Items.Add(sortType);
            }

            boxTypeSortCSC.SelectedItem = CSCSortType.CSC.ToString();
            boxTypeSortCSC.SelectionChanged += BoxTypeSortCSC_SelectionChanged;

            CreateStepsOfOptimization();
            CreateRobustnessChart();
        }

        private void BoxTypeSortCSC_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            try
            {
                if (Enum.TryParse(_boxTypeSortCSC.SelectedItem.ToString(), out CSCSortType sortType))
                {
                    _sortBotsTypeCSC = sortType;
                }

                if (_reports != null)
                {
                    ReLoadCSC(_reports);
                }
            }
            catch
            {

            }
        }

        private CultureInfo _currentCulture;

        private System.Windows.Controls.ComboBox _boxTypeSort;

        private System.Windows.Controls.ComboBox _boxTypeSortBotNum;

        private System.Windows.Controls.ComboBox _boxTypeSortCSC;
        private System.Windows.Controls.ComboBox _boxTypeSortBotNumCSC;

        System.Windows.Controls.Label _labelRobustnessMetricValue;

        public event EventHandler<decimal> CSCCalculated;

        private void _boxTypeSortBotNum_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            try
            {
                _sortBotPercent = Convert.ToInt32(_boxTypeSortBotNum.SelectedItem.ToString());

                if (_reports != null)
                {
                    ReLoad(_reports);
                }
            }
            catch
            {

            }
        }

        private void _boxTypeSortBotNumCSC_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            try
            {
                _sortBotPercentCSC = Convert.ToInt32(_boxTypeSortBotNumCSC.SelectedItem.ToString());

                if (_reports != null)
                {
                    ReLoadCSC(_reports);
                }
            }
            catch
            {

            }
        }

        void _gridResults_SelectionChanged(object sender, EventArgs e)
        {

            if (_boxTypeSort.Items.Count == 0)
            {
                return;
            }

            int columnSelect = _boxTypeSort.SelectedIndex;

            if (columnSelect == 0)
            {
                _sortBotsType = SortBotsType.PositionCount;
            }
            else if (columnSelect == 1)
            {
                _sortBotsType = SortBotsType.TotalProfit;
            }
            else if (columnSelect == 2)
            {
                _sortBotsType = SortBotsType.MaxDrowDawn;
            }
            else if (columnSelect == 3)
            {
                _sortBotsType = SortBotsType.AverageProfit;
            }
            else if (columnSelect == 4)
            {
                _sortBotsType = SortBotsType.AverageProfitPercent;
            }
            else if (columnSelect == 5)
            {
                _sortBotsType = SortBotsType.ProfitFactor;
            }
            else if (columnSelect == 6)
            {
                _sortBotsType = SortBotsType.PayOffRatio;
            }
            else if (columnSelect == 7)
            {
                _sortBotsType = SortBotsType.Recovery;
            }
            else if (columnSelect == 8)
            {
                _sortBotsType = SortBotsType.SharpRatio;
            }
            else if (columnSelect == 9)
            {
                _sortBotsType = SortBotsType.SmaDeviation;
            }
            else
            {
                return;
            }

            if (_reports != null)
            {
                ReLoad(_reports);
            }
        }

        public void ReLoad(List<OptimazerFazeReport> reports)
        {
            try
            {
                _reports = reports;

                if (_reports == null
                    || _reports.Count <= 1)
                {
                    return;
                }

                for (int i = 0; i < reports.Count; i++)
                {
                    OptimazerFazeReport.SortResults(reports[i].Reports, _sortBotsType);
                }

                GetBestBotNum(reports[0].Reports);

                UpdGridStepsOfOptimization(_gridStepsOfOptimization, _sortBotNumber);
                UpdateRobustnessChart();
                UpdateTotalProfitChart(_gridStepsOfOptimization, _chartTotalProfit, _comboBoxTotalProfitEquityType, _sortBotNumber);
                UpdateAverageProfitChart(_hostAverageProfitChart, _chartAverageProfit, _sortBotNumber);
                UpdateProfitFactorChart(_hostProfitFactor, _chartProfitFactor);
            }
            catch (Exception e)
            {
                SendLogMessage(e.ToString(), LogMessageType.Error);
            }
        }

        private void SortCSCResults(List<OptimazerFazeReport> reports)
        {
            for (int i = 0; i < reports.Count; i++)
            {
                OptimazerFazeReport.SortResults(reports[i].Reports, _sortBotsTypeCSC);
            }
        }

        private void ReLoadCSCTask(List<OptimazerFazeReport> reports, bool calculate = false, bool calculateFRS = false)
        {
            try
            {
                _reports = reports;

                if (_reports == null
                    || _reports.Count <= 1)
                {
                    return;
                }
                if (calculate || calculateFRS)
                {
                    CalculateCSCResults(reports, !calculate);
                }
                SortCSCResults(reports);
                GetBestBotNum(reports[0].Reports);
                UpdGridStepsOfOptimization(_gridStepsOfOptimizationCSC, _sortBotNumberCSC);
                UpdateCSCTable();
                UpdateAverageProfitChart(_hostAverageProfitChartCSC, _chartAverageProfitCSC, _sortBotNumberCSC);
                UpdateProfitFactorChart(_hostProfitFactorCSC, _chartProfitFactorCSC);
                UpdateTotalProfitChart(_gridStepsOfOptimizationCSC, _chartTotalProfitCSC, _comboBoxTotalProfitEquityTypeCSC, _sortBotNumberCSC);
                GetCSC();
            }
            catch (Exception e)
            {
                SendLogMessage(e.ToString(), LogMessageType.Error);
            }
            finally
            {
                if (calculate)
                {
                    _awaitUiBotsInfoLoading?.Dispose();
                }
            }
        }

        private AwaitObject _awaitUiBotsInfoLoading;
        public void ReLoadCSC(List<OptimazerFazeReport> reports, bool calculate = false, bool calculateFRS = false)
        {
            try
            {
                if (calculate)
                {
                    _awaitUiBotsInfoLoading = new AwaitObject("Рассчёт", 100, 0, true);
                    AwaitUi ui = new AwaitUi(_awaitUiBotsInfoLoading);

                    Task.Factory.StartNew(() =>
                    {
                        Thread.CurrentThread.IsBackground = true;
                        ReLoadCSCTask(reports, calculate);
                    });

                    ui.ShowDialog();
                }
                else if (calculateFRS)
                {
                    ReLoadCSCTask(reports, calculate, calculateFRS);
                }
                else
                {
                    ReLoadCSCTask(reports, calculate);
                }
                    
            }
            catch (Exception e)
            {
                SendLogMessage(e.ToString(), LogMessageType.Error);
            }
        }

        private void GetBestBotNum(List<OptimizerReport> reports)
        {
            decimal countBotsPercent = reports.Count / 100m;

            decimal result = countBotsPercent * _sortBotPercent;
            decimal resultCSC = countBotsPercent * _sortBotPercentCSC;

            _sortBotNumber = Convert.ToInt32(result);
            _sortBotNumberCSC = Convert.ToInt32(resultCSC);
            if (_sortBotNumber > reports.Count)
            {
                _sortBotNumber = reports.Count - 1;
            }

            if (_sortBotNumberCSC > reports.Count)
            {
                _sortBotNumberCSC = reports.Count - 1;
            }
        }

        private SortBotsType _sortBotsType;
        private CSCSortType _sortBotsTypeCSC = CSCSortType.CSC;

        private int _sortBotPercent = 0;
        private int _sortBotPercentCSC = 0;

        private int _sortBotNumber = 0;
        private int _sortBotNumberCSC = 0;

        private List<OptimazerFazeReport> _reports;

        // fazes in table

        private WindowsFormsHost _hostStepsOfOptimization;
        private WindowsFormsHost _hostStepsOfOptimizationCSC;
        private DataGridView _gridStepsOfOptimization;
        private DataGridView _gridStepsOfOptimizationCSC;

        private DataGridView GetStepsOfOptimizationDGV()
        {
            DataGridView gridStepsOfOptimization = DataGridFactory.GetDataGridView(DataGridViewSelectionMode.ColumnHeaderSelect,
                DataGridViewAutoSizeRowsMode.None, true);

            cell0.Style = gridStepsOfOptimization.DefaultCellStyle;

            gridStepsOfOptimization.ScrollBars = ScrollBars.Vertical;

            gridStepsOfOptimization.Columns.Add(GetColumn("Period", 80));
            gridStepsOfOptimization.Columns.Add(GetColumn("Start", 80, false));
            gridStepsOfOptimization.Columns.Add(GetColumn("End", readOnly: false));
            gridStepsOfOptimization.Columns.Add(GetColumn("Best bot number InSample", readOnly: false));
            gridStepsOfOptimization.Columns.Add(GetColumn("Best bot in period", readOnly: false));
            gridStepsOfOptimization.Columns.Add(GetColumn("Parameters", readOnly: false));
            gridStepsOfOptimization.Columns.Add(GetColumn("Bot results in OutOfSample", readOnly: false));
            gridStepsOfOptimization.Columns.Add(GetColumn("Profit", readOnly: false));
            gridStepsOfOptimization.Columns.Add(GetColumn("Average profit %", readOnly: false));
            gridStepsOfOptimization.Columns.Add(GetColumn("Position count", readOnly: false));
            gridStepsOfOptimization.Columns.Add(GetColumn("Sharp ratio", readOnly: false));
            gridStepsOfOptimization.Columns.Add(GetColumn("SMA(20) Deviation", readOnly: false));

            DataGridViewButtonColumn column11 = new DataGridViewButtonColumn();
            column11.CellTemplate = new DataGridViewButtonCell();
            column11.ReadOnly = true;
            column11.AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
            gridStepsOfOptimization.Columns.Add(column11);

            gridStepsOfOptimization.Rows.Add(null, null);
            return gridStepsOfOptimization;
        }

        private void CreateStepsOfOptimization()
        {
            _gridStepsOfOptimization = GetStepsOfOptimizationDGV();
            _gridStepsOfOptimizationCSC = GetStepsOfOptimizationDGV();

            _hostStepsOfOptimization.Child = _gridStepsOfOptimization;
            _hostStepsOfOptimizationCSC.Child = _gridStepsOfOptimizationCSC;
        }

        private WindowsFormsHost _hostFRS;
        private DataGridView _gridFRS;
        private decimal _cscWeight = 0.25m;
        private decimal _psrWeight = 0.25m;
        private decimal _ddsWeight = 0.25m;
        private decimal _srcWeight = 0.25m;
        private decimal _ratioWeight = 0.25m;
        private decimal _totalReturnWeight = 0.25m;
        private decimal _totalDrowDownWeight = 0.25m;
        public decimal CscWeight => _cscWeight;
        public decimal PsrWeight => _psrWeight;
        public decimal DdsWeight => _ddsWeight;
        public decimal SrcWeight => _srcWeight;
        public decimal RatioWeight => _ratioWeight;
        public decimal TotalReturnWeight => _totalReturnWeight;
        public decimal TotalDrowDownWeight => _totalDrowDownWeight;

        internal void ActivateCSCChart(WindowsFormsHost hostFRS)
        {
            _hostFRS = hostFRS;
            _gridFRS = DataGridFactory.GetDataGridView(DataGridViewSelectionMode.ColumnHeaderSelect,
                DataGridViewAutoSizeRowsMode.None, true);

            try
            {
                _gridFRS.ScrollBars = ScrollBars.Vertical;

                _gridFRS.Columns.Add(GetColumn(""));
                _gridFRS.Columns.Add(GetColumn("FRS", 80, readOnly: false));
                _gridFRS.Columns.Add(GetColumn("CSC", 80, readOnly: false));
                _gridFRS.Columns.Add(GetColumn("PSR", 80, readOnly: false));
                _gridFRS.Columns.Add(GetColumn("DDS", 80, readOnly: false));
                _gridFRS.Columns.Add(GetColumn("SRC", 80, readOnly: false));
                _gridFRS.Columns.Add(GetColumn("Ratio", 80, readOnly: false));
                _gridFRS.Columns.Add(GetColumn("TotalReturn", 80, readOnly: false));
                _gridFRS.Columns.Add(GetColumn("TotalDrowDown", 110, readOnly: false));

                _gridFRS.Rows.Add(null, null);
                _gridFRS.CellValueChanged += _gridFRS_CellValueChanged;
                _hostFRS.Child = _gridFRS;
            }
            catch (Exception ex)
            {

            }
        }

        private void _gridFRS_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            if (_gridFRS == null || _gridFRS.Rows.Count < 3) return;
            decimal cscWeight = GetWeightFromTable(_gridFRS, 2, 2);
            decimal psrWeight = GetWeightFromTable(_gridFRS, 3, 2);
            decimal ddsWeight = GetWeightFromTable(_gridFRS, 4, 2);
            decimal srcWeight = GetWeightFromTable(_gridFRS, 5, 2);
            decimal ratioWeight = GetWeightFromTable(_gridFRS, 6, 2);
            decimal totalReturnWeight = GetWeightFromTable(_gridFRS, 7, 2);
            decimal totalDrawDownWeight = GetWeightFromTable(_gridFRS, 8, 2);

            if (_cscWeight != cscWeight ||
                _psrWeight != psrWeight ||
                _ddsWeight != ddsWeight ||
                _srcWeight != srcWeight ||
                _ratioWeight != ratioWeight ||
                _totalReturnWeight != totalReturnWeight ||
                _totalDrowDownWeight != totalDrawDownWeight)
            {
                _cscWeight = cscWeight;
                _psrWeight = psrWeight;
                _ddsWeight = ddsWeight;
                _srcWeight = srcWeight;
                _ratioWeight = ratioWeight;
                _totalReturnWeight = totalReturnWeight;
                _totalDrowDownWeight = totalDrawDownWeight;
            }
        }

        public void UpdateWeights(decimal csc, decimal psr, decimal dds, decimal src, decimal ratio, decimal totalReturn, decimal totalDrawDown)
        {
            _cscWeight = csc;
            _psrWeight = psr;
            _ddsWeight = dds;
            _srcWeight = src;
            _ratioWeight = ratio;
            _totalReturnWeight = totalReturn;
            _totalDrowDownWeight = totalDrawDown;
            Updateweights();
        }

        public event EventHandler WeightsChanged;
        internal void Updateweights()
        {
            decimal sum = _cscWeight + _psrWeight + _ddsWeight + _srcWeight + _ratioWeight + _totalReturnWeight + _totalDrowDownWeight;
            try
            {
                _cscWeight /= sum;
                _psrWeight /= sum;
                _ddsWeight /= sum;
                _srcWeight /= sum;
                _ratioWeight /= sum;
                _totalReturnWeight /= sum;
                _totalDrowDownWeight /= sum;
            }
            catch
            {
                _cscWeight = 1m;
                _psrWeight = 1m;
                _ddsWeight = 1m;
                _srcWeight = 1m;
                _ratioWeight = 1m;
                _totalReturnWeight = 1m;
                _totalDrowDownWeight = 1m;
            }

            WeightsChanged.Invoke(this, EventArgs.Empty);
            ReLoadCSC(_reports, calculateFRS: true);
        }

        private decimal GetWeightFromTable(DataGridView gridView, int column, int row)
        {
            decimal result = 0;
            try
            {
                decimal.TryParse(gridView.Rows[row].Cells[column].Value?.ToString().Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out result);
            }
            catch { }
            return result;
        }

        private void UpdateCSCTable()
        {
            if (_gridFRS.InvokeRequired)
            {
                _gridFRS.Invoke(new Action(UpdateCSCTable));
                return;
            }

            _gridFRS.Rows.Clear();

            if (_reports == null)
            {
                return;
            }

            try
            {
                OptimazerFazeReport curReport = _reports[0];
                OptimizerReport bot = reportsForCSCTable[curReport.Reports[_sortBotNumberCSC].GetParamsToDataTable()];
                int botCount = curReport.Reports.Count;
                DataGridViewRow row0 = new DataGridViewRow();
                row0.Height = 30;

                AddCell(row0, "Robot");
                AddCell(row0, bot.FRS);
                AddCell(row0, bot.CSC);
                AddCell(row0, bot.PSR);
                AddCell(row0, bot.DDS);
                AddCell(row0, bot.SRC);
                AddCell(row0, bot.Ratio);
                AddCell(row0, bot.TotalReturn);
                AddCell(row0, bot.TotalDrawDown);

                _gridFRS.Rows.Add(row0);

                DataGridViewRow row1 = new DataGridViewRow();
                row1.Height = 30;

                AddCell(row1, "Strategy");
                AddCell(row1, _strategyCSC);
                AddCell(row1, _strategyPSR);
                AddCell(row1, _strategyDDS);
                AddCell(row1, _strategySRC);
                AddCell(row1, _strategyFRS);
                AddCell(row1, "");
                AddCell(row1, "");
                AddCell(row1, "");

                _gridFRS.Rows.Add(row1);

                DataGridViewRow row2 = new DataGridViewRow();
                row2.Height = 30;
                row2.ReadOnly = false;

                AddCell(row2, "FRS weights");
                AddCell(row2, "-");
                AddCell(row2, _cscWeight, false);
                AddCell(row2, _psrWeight, false);
                AddCell(row2, _ddsWeight, false);
                AddCell(row2, _srcWeight, false);
                AddCell(row2, _ratioWeight, false);
                AddCell(row2, _totalReturnWeight, false);
                AddCell(row2, _totalDrowDownWeight, false);

                _gridFRS.Rows.Add(row2);

                DataGridViewRow row3 = new DataGridViewRow();
                row3.Height = 30;
                row3.ReadOnly = false;

                AddCell(row3, "Rank");
                AddCell(row3, $"{bot.FRSRank}/{botCount}");
                AddCell(row3, $"{bot.CSCRank}/{botCount}");
                AddCell(row3, $"{bot.PSRRank}/{botCount}");
                AddCell(row3, $"{bot.DDSRank}/{botCount}");
                AddCell(row3, $"{bot.SRCRank}/{botCount}");
                AddCell(row3, $"{bot.RatioRank}/{botCount}");
                AddCell(row3, $"{bot.TotalReturnRank}/{botCount}");
                AddCell(row3, $"{bot.TotalDrawDownRank}/{botCount}");
                _gridFRS.Rows.Add(row3);
            }
            catch { }
        }

        private void AddCell(DataGridViewRow row, decimal value, bool readOnly = true)
        {
            DataGridViewTextBoxCell cell = new DataGridViewTextBoxCell();
            cell.Value = Math.Round(value, 3);
            row.Cells.Add(cell);
            cell.ReadOnly = readOnly;
        }

        private void AddCell(DataGridViewRow row, object value, bool readOnly = true)
        {
            DataGridViewTextBoxCell cell = new DataGridViewTextBoxCell();
            cell.Value = value;
            row.Cells.Add(cell);
            cell.ReadOnly = readOnly;
        }

        private DataGridViewColumn GetColumn(string name, int width = 0, bool readOnly = true)
        {
            DataGridViewColumn column = new DataGridViewColumn();
            column.CellTemplate = cell0;
            column.HeaderText = name;
            column.ReadOnly = readOnly;
            if (width > 0)
            {
                column.Width = width;
            }
            else
            {
                column.AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
            }
            return column;
        }

        private void UpdGridStepsOfOptimization(DataGridView gridStepsOfOptimization, int sortBotNumber)
        {
            if (gridStepsOfOptimization.InvokeRequired)
            {
                gridStepsOfOptimization.Invoke(new Action<DataGridView, int>(UpdGridStepsOfOptimization), gridStepsOfOptimization, sortBotNumber);
                return;
            }

            gridStepsOfOptimization.CellMouseClick -= _gridResults_CellMouseClick;
            gridStepsOfOptimization.Rows.Clear();

            if (_reports == null)
            {
                return;
            }

            try
            {
                if (_reports.Count <= 1)
                {
                    return;
                }

                if (_reports.Count == 2 &&
                    _reports[1].Reports.Count == 0)
                {
                    return;
                }

                OptimizerReport inSampleReport = null;

                for (int i = 0; i < _reports.Count; i++)
                {
                    OptimazerFazeReport curReport = _reports[i];

                    if (curReport == null ||
                        curReport.Reports == null ||
                        curReport.Reports.Count == 0)
                    {
                        continue;
                    }

                    if (curReport.Faze.TypeFaze == OptimizerFazeType.InSample)
                    {
                        inSampleReport = curReport.Reports[sortBotNumber];
                    }

                    OptimizerReport reportToPaint;

                    if (curReport.Faze.TypeFaze == OptimizerFazeType.InSample)
                    {
                        reportToPaint = curReport.Reports[sortBotNumber];
                    }
                    else // if(curReport.Faze.TypeFaze == OptimizerFazeType.OutOfSample)
                    {
                        string botName = inSampleReport.BotName.Replace(" InSample", "").Replace("OpT", "");
                        reportToPaint = curReport.Reports.Find(rep => rep.BotName.StartsWith(botName));
                    }

                    if (reportToPaint == null)
                    {
                        continue;
                    }

                    DataGridViewRow row = new DataGridViewRow();
                    row.Height = 30;
                    row.Cells.Add(new DataGridViewTextBoxCell());
                    row.Cells[0].Value = curReport.Faze.TypeFaze.ToString();


                    DataGridViewTextBoxCell cell2 = new DataGridViewTextBoxCell();
                    cell2.Value = curReport.Faze.TimeStart.ToString(OsLocalization.ShortDateFormatString);
                    row.Cells.Add(cell2);

                    DataGridViewTextBoxCell cell3 = new DataGridViewTextBoxCell();
                    cell3.Value = curReport.Faze.TimeEnd.ToString(OsLocalization.ShortDateFormatString);
                    row.Cells.Add(cell3);

                    DataGridViewTextBoxCell cell4 = new DataGridViewTextBoxCell();

                    if (curReport.Faze.TypeFaze == OptimizerFazeType.InSample)
                    {
                        cell4.Value = inSampleReport.BotName.Replace(" InSample", "").Replace("OpT", "");
                    }
                    row.Cells.Add(cell4);

                    DataGridViewTextBoxCell cell5 = new DataGridViewTextBoxCell();
                    cell5.Value = curReport.Reports[0].BotName.Replace(" InSample", "").Replace(" OutOfSample", "").Replace("OpT", "");
                    row.Cells.Add(cell5);

                    DataGridViewTextBoxCell cell6 = new DataGridViewTextBoxCell();
                    cell6.Value = reportToPaint.GetParamsToDataTable();
                    row.Cells.Add(cell6);

                    DataGridViewTextBoxCell cell7 = new DataGridViewTextBoxCell();

                    if (curReport.Faze.TypeFaze == OptimizerFazeType.OutOfSample)
                    {
                        string botName = inSampleReport.BotName.Replace(" InSample", "");
                        // reportToPaint = curReport.Reports.Find(rep => rep.BotName.StartsWith(botName));

                        for (int i2 = 0; i2 < curReport.Reports.Count; i2++)
                        {
                            string curName = curReport.Reports[i2].BotName.Replace(" InSample", "").Replace(" OutOfSample", "");

                            if (curName == botName)
                            {
                                cell7.Value = (i2 + 1).ToString();
                                break;
                            }
                        }
                    }
                    row.Cells.Add(cell7);

                    DataGridViewTextBoxCell cell8 = new DataGridViewTextBoxCell();
                    cell8.Value = Math.Round(reportToPaint.TotalProfit, 4).ToStringWithNoEndZero();
                    row.Cells.Add(cell8);

                    DataGridViewTextBoxCell cell9 = new DataGridViewTextBoxCell();
                    cell9.Value = Math.Round(reportToPaint.AverageProfitPercentOneContract, 4).ToStringWithNoEndZero();
                    row.Cells.Add(cell9);

                    DataGridViewTextBoxCell cell10 = new DataGridViewTextBoxCell();
                    cell10.Value = reportToPaint.PositionsCount.ToString();
                    row.Cells.Add(cell10);

                    DataGridViewTextBoxCell cell11 = new DataGridViewTextBoxCell();
                    cell11.Value = reportToPaint.SharpRatio.ToString();
                    row.Cells.Add(cell11);

                    DataGridViewTextBoxCell cell12 = new DataGridViewTextBoxCell();
                    cell12.Value = reportToPaint.SmaDeviation.ToString();
                    row.Cells.Add(cell12);

                    DataGridViewButtonCell cell13 = new DataGridViewButtonCell();
                    cell13.Value = OsLocalization.Optimizer.Message44;
                    row.Cells.Add(cell13);

                    gridStepsOfOptimization.Rows.Add(row);
                }

                gridStepsOfOptimization.CellMouseClick += _gridResults_CellMouseClick;

            }
            catch (Exception ex)
            {
                SendLogMessage(ex.ToString(), LogMessageType.Error);
            }
        }

        private decimal _strategyCSC;

        private decimal _strategyPSR;

        private decimal _strategyDDS;

        private decimal _strategySRC;

        private decimal _strategyFRS;

        /// <summary>
        /// Список репортов с заполненными полями для третьей вкладки, так как показатели считаются для одного робота со всех периодов, они будут одинаковые
        /// </summary>
        private Dictionary<string, OptimizerReport> reportsForCSCTable = new Dictionary<string, OptimizerReport>();

        private void CalculateCSCResults(List<OptimazerFazeReport> reports, bool calculateFRSOnly = false)
        {
            if (calculateFRSOnly)
            {
                foreach (var r in reports.SelectMany(r => r.Reports))
                {
                    r.FRS =     r.CSC * _cscWeight +
                                r.PSR * _psrWeight +
                                r.DDS * _ddsWeight +
                                r.SRC * _srcWeight +
                                r.Ratio * _ratioWeight +
                                r.TotalReturn * _totalReturnWeight +
                                r.TotalDrawDown * _totalDrowDownWeight;
                }

                List<IGrouping<decimal, OptimizerReport>> frsRankGroup = reportsForCSCTable.Values.GroupBy(r => r.FRS).ToList();
                frsRankGroup.Sort(new Comparison<IGrouping<decimal, OptimizerReport>>((r1, r2) => r2.First().FRS.CompareTo(r1.First().FRS)));
                for (int i = 0; i < frsRankGroup.Count; i++)
                {
                    foreach (OptimizerReport report in frsRankGroup[i])
                    {
                        report.FRSRank = i + 1;
                    }
                }
                return;
            }

            var insamples = reports.Where(r => r.Faze.TypeFaze == OptimizerFazeType.InSample);

            // Формируется словарь, в котором для каждого in sample хранится свой out of sample (последний in sample без out of sample отбрасывается)
            Dictionary<OptimazerFazeReport, OptimazerFazeReport> pairs = new Dictionary<OptimazerFazeReport, OptimazerFazeReport>();
            foreach (OptimazerFazeReport @is in insamples)
            {
                OptimazerFazeReport oos = reports.Where(r => r.Faze.TypeFaze == OptimizerFazeType.OutOfSample && (r.Faze.TimeStart - @is.Faze.TimeEnd) <= TimeSpan.FromDays(2) && (r.Faze.TimeStart - @is.Faze.TimeEnd) > TimeSpan.FromDays(0)).SingleOrDefault();
                if (oos != null)
                {
                    pairs.Add(@is, oos);
                }
            }

            // Для каждого бота, который отличается по строковому ключу, генерирующегося из его параметров, составляется словарь из переиодов insample и соответствующим им out of sample
            Dictionary<string, Dictionary<OptimizerReport, OptimizerReport>> allReports = new Dictionary<string, Dictionary<OptimizerReport, OptimizerReport>>();
            Parallel.ForEach(pairs, pair =>
            {
                var inSample = pair.Key.Reports.Select(r => new { Insample = r, Key = r.GetParamsToDataTable() });
                var outSample = pair.Value.Reports.Select(r => new { OutOfsample = r, Key = r.GetParamsToDataTable() });
                Parallel.ForEach(inSample, i => 
                {
                    var oos = outSample.Where(o => i.Key == o.Key).SingleOrDefault();
                    if (oos!= null)
                    {
                        KeyValuePair<string, KeyValuePair<OptimizerReport, OptimizerReport>> pair = new KeyValuePair<string, KeyValuePair<OptimizerReport, OptimizerReport>>(i.Key, new KeyValuePair<OptimizerReport, OptimizerReport>(i.Insample, oos.OutOfsample));
                        lock (allReports)
                        {
                            if (!allReports.ContainsKey(i.Key))
                            {
                                allReports.Add(i.Key, new Dictionary<OptimizerReport, OptimizerReport>() { { i.Insample, oos.OutOfsample } });
                            }
                            else
                            {
                                allReports[i.Key].Add(i.Insample, oos.OutOfsample);
                            }
                        }
                    }
                });
            });

            Parallel.Invoke(() =>
            {
                Parallel.ForEach(allReports, (group) =>
                {
                    IEnumerable<OptimizerReport> allInSampleReports = group.Value.Keys.Select(r => r);
                    IEnumerable<OptimizerReport> allOutSampleReports = group.Value.Values.Select(r => r);

                    CalculateBots( group.Value,
                                out decimal csc,
                                out decimal psr,
                                out decimal dds,
                                out decimal src,
                                out decimal frs,
                                out decimal ratio,
                                out decimal totalReturn,
                                out decimal totalDrawDown);

                    foreach (var r in allInSampleReports.Union(allOutSampleReports))
                    {
                        r.CSC = csc;
                        r.PSR = psr;
                        r.DDS = dds;
                        r.SRC = src;
                        r.FRS = frs;
                        r.Ratio = ratio;
                        r.TotalReturn = totalReturn;
                        r.TotalDrawDown = totalDrawDown;
                    }
                });

                var results = allReports.Values.Select(p => p.Keys.FirstOrDefault());

                Parallel.Invoke(() => 
                {
                    List<IGrouping<decimal, OptimizerReport>> cscRankGroup = results.GroupBy(r => r.CSC).ToList();
                    cscRankGroup.Sort(new Comparison<IGrouping<decimal, OptimizerReport>>((r1, r2) => r2.First().CSC.CompareTo(r1.First().CSC)));
                    for (int i = 0; i < cscRankGroup.Count; i++)
                    {
                        foreach (OptimizerReport report in cscRankGroup[i])
                        {
                            report.CSCRank = i + 1;
                        }
                    }
                }, ()=> 
                {
                    List<IGrouping<decimal, OptimizerReport>> psrRankGroup = results.GroupBy(r => r.PSR).ToList();
                    psrRankGroup.Sort(new Comparison<IGrouping<decimal, OptimizerReport>>((r1, r2) => r2.First().PSR.CompareTo(r1.First().PSR)));
                    for (int i = 0; i < psrRankGroup.Count; i++)
                    {
                        foreach (OptimizerReport report in psrRankGroup[i])
                        {
                            report.PSRRank = i + 1;
                        }
                    }
                }, () =>
                {
                    List<IGrouping<decimal, OptimizerReport>> ddsRankGroup = results.GroupBy(r => r.DDS).ToList();
                    ddsRankGroup.Sort(new Comparison<IGrouping<decimal, OptimizerReport>>((r1, r2) => r2.First().DDS.CompareTo(r1.First().DDS)));
                    for (int i = 0; i < ddsRankGroup.Count; i++)
                    {
                        foreach (OptimizerReport report in ddsRankGroup[i])
                        {
                            report.DDSRank = i + 1;
                        }
                    }
                }, () =>
                {
                    List<IGrouping<decimal, OptimizerReport>> srcRankGroup = results.GroupBy(r => r.SRC).ToList();
                    srcRankGroup.Sort(new Comparison<IGrouping<decimal, OptimizerReport>>((r1, r2) => r2.First().SRC.CompareTo(r1.First().SRC)));
                    for (int i = 0; i < srcRankGroup.Count; i++)
                    {
                        foreach (OptimizerReport report in srcRankGroup[i])
                        {
                            report.SRCRank = i + 1;
                        }
                    }
                }, () =>
                {
                    List<IGrouping<decimal, OptimizerReport>> frsRankGroup = results.GroupBy(r => r.FRS).ToList();
                    frsRankGroup.Sort(new Comparison<IGrouping<decimal, OptimizerReport>>((r1, r2) => r2.First().FRS.CompareTo(r1.First().FRS)));
                    for (int i = 0; i < frsRankGroup.Count; i++)
                    {
                        foreach (OptimizerReport report in frsRankGroup[i])
                        {
                            report.FRSRank = i + 1;
                        }
                    }
                }, () =>
                {
                    List<IGrouping<decimal, OptimizerReport>> ratioRankGroup = results.GroupBy(r => r.Ratio).ToList();
                    ratioRankGroup.Sort(new Comparison<IGrouping<decimal, OptimizerReport>>((r1, r2) => r2.First().Ratio.CompareTo(r1.First().Ratio)));
                    for (int i = 0; i < ratioRankGroup.Count; i++)
                    {
                        foreach (OptimizerReport report in ratioRankGroup[i])
                        {
                            report.RatioRank = i + 1;
                        }
                    }
                }, () =>
                {
                    List<IGrouping<decimal, OptimizerReport>> totalReturnRankGroup = results.GroupBy(r => r.TotalReturn).ToList();
                    totalReturnRankGroup.Sort(new Comparison<IGrouping<decimal, OptimizerReport>>((r1, r2) => r2.First().TotalReturn.CompareTo(r1.First().TotalReturn)));
                    for (int i = 0; i < totalReturnRankGroup.Count; i++)
                    {
                        foreach (OptimizerReport report in totalReturnRankGroup[i])
                        {
                            report.TotalReturnRank = i + 1;
                        }
                    }
                }, () =>
                {
                    List<IGrouping<decimal, OptimizerReport>> totalDrawDownRankGroup = results.GroupBy(r => r.TotalDrawDown).ToList();
                    totalDrawDownRankGroup.Sort(new Comparison<IGrouping<decimal, OptimizerReport>>((r1, r2) => r2.First().TotalDrawDown.CompareTo(r1.First().TotalDrawDown)));
                    for (int i = 0; i < totalDrawDownRankGroup.Count; i++)
                    {
                        foreach (OptimizerReport report in totalDrawDownRankGroup[i])
                        {
                            report.TotalDrawDownRank = i + 1;
                        }
                    }
                });

                reportsForCSCTable.Clear();
                foreach (OptimizerReport report in results)
                {
                    reportsForCSCTable.Add(report.GetParamsToDataTable(), report);
                }

            }, () =>
            {
                var stratDictionary = allReports.Values.Aggregate((x1, x2) => { return x1.Concat(x2).ToDictionary(x => x.Key, x => x.Value); });
                CalculateBots(stratDictionary, out decimal csc, out decimal psr, out decimal dds, out decimal src, out decimal frs, out _, out _, out _);
                _strategyCSC = csc;
                _strategyPSR = psr;
                _strategyDDS = dds;
                _strategySRC = src;
                _strategyFRS = frs;
            });

            void CalculateBots(Dictionary<OptimizerReport, OptimizerReport> dicReports,
                            out decimal csc,
                            out decimal psr,
                            out decimal dds,
                            out decimal src,
                            out decimal frs,
                            out decimal ratio,
                            out decimal totalReturn,
                            out decimal totalDrawDown)
            {
                IEnumerable<OptimizerReport> allInSampleReports = dicReports.Keys.Select(r => r);
                IEnumerable<OptimizerReport> allOutSampleReports = dicReports.Values.Select(r => r);
                decimal avgProfitIS = allInSampleReports.Sum(r => r.AverageProfit) / allInSampleReports.Count();
                decimal avgProfitOOS = allOutSampleReports.Sum(r => r.AverageProfit) / allOutSampleReports.Count();
                csc = 0;
                psr = 0;
                dds = 0;
                src = 0;
                if (avgProfitIS > 0 && avgProfitOOS > 0)
                {
                    csc = (1 - (Math.Abs(avgProfitIS - avgProfitOOS) / Math.Max(avgProfitIS, avgProfitOOS))) * 100;
                }

                int psrCounter = 0;
                foreach (var pair in dicReports)
                {
                    psrCounter += Math.Sign(pair.Key.TotalProfit) == Math.Sign(pair.Value.TotalProfit) ? 1 : 0;
                }

                psr = ((decimal)psrCounter / dicReports.Count) * 100;

                decimal avgDDIS = allInSampleReports.Sum(r => r.MaxDrowDawn) / allInSampleReports.Count();
                decimal avgDDOOS = allOutSampleReports.Sum(r => r.MaxDrowDawn) / allOutSampleReports.Count();
                if (avgDDIS != 0)
                {
                    dds = (1 - ((avgDDOOS / avgDDIS) - 1)) * 100;
                    dds = Math.Max(0, dds);
                }

                double[] sharpIS = allInSampleReports.Select(r => (double)r.SharpRatio).ToArray();
                double[] sharpOOS = allOutSampleReports.Select(r => (double)r.SharpRatio).ToArray();

                // Ещё можно использовать CorrelationBuilder.Correlation(sharpIS, sharpOOS), даёт похожие результаты, но иногда возвращает NaN
                using (Chart chart = new Chart())
                using (Series ser1 = new Series("1"))
                using (Series ser2 = new Series("2"))
                {
                    for (int i = 0; i < sharpOOS.Length; i++)
                    {
                        ser1.Points.AddXY(i, sharpIS[i]);
                        ser2.Points.AddXY(i, sharpOOS[i]);
                    }

                    chart.Series.Add(ser1);
                    chart.Series.Add(ser2);
                    src = (decimal)((chart.DataManipulator.Statistics.Correlation("1", "2") + 1) / 2) * 100;
                }

                totalDrawDown = allOutSampleReports.Sum(r => Math.Abs(r.MaxDrowDawn));
                totalReturn = allOutSampleReports.Sum(r => r.TotalProfit);
                ratio = totalDrawDown == 0 ? 0 : totalReturn / totalDrawDown;

                frs = _cscWeight * csc +
                                _psrWeight * psr +
                                _ddsWeight * dds +
                                _srcWeight * src +
                                _ratioWeight * ratio +
                                _totalReturnWeight * totalReturn +
                                _totalDrowDownWeight * totalDrawDown;
            }
        }

        // Bot Charting
        void _gridResults_CellMouseClick(object sender, DataGridViewCellMouseEventArgs e)
        {
            if (e.RowIndex < 0)
            {
                return;
            }

            if (e.ColumnIndex == 12)
            {
                ShowBotFullChartDialog(e);
            }
        }

        private void ShowBotFullChartDialog(DataGridViewCellMouseEventArgs e)
        {
            if (_reports.Count < e.RowIndex + 1)
            {
                return;
            }

            OptimazerFazeReport fazeReport = new OptimazerFazeReport(_reports[e.RowIndex]);

            if (fazeReport.Reports.Count < _sortBotNumber + 1)
            {
                return;
            }

            OptimizerReport report = fazeReport.Reports[_sortBotNumber];

            if (ChartButtonClickEvent != null)
            {
                ChartButtonClickEvent(fazeReport, report);
            }
        }

        //public bool CaptureData
        //{
        //    get { return _captureData; }
        //    set { _captureData = value; }
        //}
        //private bool _captureData = false;

        // Robustness

        private WindowsFormsHost _hostRobustness;

        private Chart _chartRobustness;

        private void CreateRobustnessChart()
        {
            _chartRobustness = new Chart();

            ChartArea area = new ChartArea("Prime");

            _chartRobustness.ChartAreas.Clear();
            _chartRobustness.ChartAreas.Add(area);
            _chartRobustness.BackColor = Color.FromArgb(21, 26, 30);
            _chartRobustness.ChartAreas[0].AxisX.TitleForeColor = Color.FromArgb(149, 159, 176);

            for (int i = 0; _chartRobustness.ChartAreas != null && i < _chartRobustness.ChartAreas.Count; i++)
            {
                _chartRobustness.ChartAreas[i].BackColor = Color.FromArgb(21, 26, 30);
                _chartRobustness.ChartAreas[i].BorderColor = Color.FromArgb(17, 18, 23);
                _chartRobustness.ChartAreas[i].CursorY.LineColor = Color.FromArgb(149, 159, 176);
                _chartRobustness.ChartAreas[i].CursorX.LineColor = Color.FromArgb(149, 159, 176);

                foreach (var axe in _chartRobustness.ChartAreas[i].Axes)
                {
                    axe.LabelStyle.ForeColor = Color.FromArgb(149, 159, 176);
                }
            }

            Series series = new Series();
            series.ChartType = SeriesChartType.Column;
            _chartRobustness.Series.Clear();
            _chartRobustness.Series.Add(series);

            _hostRobustness.Child = _chartRobustness;

            _chartRobustness.SuppressExceptions = true;
        }

        private void GetCSC()
        {
            if (_gridStepsOfOptimizationCSC.InvokeRequired)
            {
                _gridStepsOfOptimizationCSC.Invoke(new Action(GetCSC));
                return;
            }
            try
            {
                CSCCalculated?.Invoke(this, _reports[0].Reports[_sortBotNumber].CSC);
            }
            catch
            { }
        }

        private void UpdateRobustnessChart()
        {
            if (_gridStepsOfOptimization.InvokeRequired)
            {
                _gridStepsOfOptimization.Invoke(new Action(UpdateRobustnessChart));
                return;
            }

            try
            {
                _labelRobustnessMetricValue.Content = "";

                int countBestTwenty = 0;
                int count20_40 = 0;
                int count40_60 = 0;
                int count60_80 = 0;
                int countWorst20 = 0;

                if (_reports == null)
                {
                    return;
                }

                if (_reports.Count <= 1)
                {
                    return;
                }

                if (_reports.Count == 2 &&
                    _reports[1].Reports.Count == 0)
                {
                    return;
                }

                int num = 0;

                OptimizerReport inSampleReport = null;

                decimal max = decimal.MinValue;

                for (int i = 0; i < _reports.Count; i++)
                {
                    OptimazerFazeReport curReport = _reports[i];

                    if (curReport == null ||
                        curReport.Reports == null ||
                        curReport.Reports.Count == 0)
                    {
                        continue;
                    }

                    if (curReport.Faze.TypeFaze == OptimizerFazeType.InSample)
                    {
                        inSampleReport = curReport.Reports[_sortBotNumber];
                    }

                    if (curReport.Faze.TypeFaze == OptimizerFazeType.OutOfSample)
                    {
                        string botName = inSampleReport.BotName.Replace(" InSample", "");
                        // reportToPaint = curReport.Reports.Find(rep => rep.BotName.StartsWith(botName));

                        for (int i2 = 0; i2 < curReport.Reports.Count; i2++)
                        {
                            if (curReport.Reports[i2].BotName.StartsWith(botName))
                            {
                                decimal botNum = Convert.ToDecimal(i2 + 1) / curReport.Reports.Count * 100m;

                                if (botNum <= 20)
                                {
                                    countBestTwenty += 1;

                                    if (countBestTwenty > max)
                                    {
                                        max = countBestTwenty;
                                    }
                                }
                                else if (botNum > 20 && botNum <= 40)
                                {
                                    count20_40 += 1;
                                    if (count20_40 > max)
                                    {
                                        max = count20_40;
                                    }
                                }
                                else if (botNum > 40 && botNum <= 60)
                                {
                                    count40_60 += 1;

                                    if (count40_60 > max)
                                    {
                                        max = count40_60;
                                    }
                                }
                                else if (botNum > 60 && botNum <= 80)
                                {
                                    count60_80 += 1;

                                    if (count60_80 > max)
                                    {
                                        max = count60_80;
                                    }
                                }
                                else if (botNum > 80)
                                {
                                    countWorst20 += 1;

                                    if (countWorst20 > max)
                                    {
                                        max = countWorst20;
                                    }
                                }

                                break;
                            }
                        }
                    }
                }


                decimal allCount = 0;

                allCount += countBestTwenty;
                allCount += countWorst20;
                allCount += count20_40;
                allCount += count40_60;
                allCount += count60_80;

                if (allCount != 0)
                {
                    decimal oneBestP = 100 / allCount;
                    decimal robustness = 0;

                    robustness += countBestTwenty * oneBestP;
                    robustness += count20_40 * oneBestP * 0.75m;
                    robustness += count40_60 * oneBestP * 0.5m;
                    robustness += count60_80 * oneBestP * 0.25m;

                    _labelRobustnessMetricValue.Content = Math.Round(robustness, 2).ToString() + " %";
                }

                _chartRobustness.Series[0].Points.ClearFast();

                DataPoint point1 = new DataPoint(1, countBestTwenty);
                point1.AxisLabel = "Best 20%";
                point1.Color = Color.DarkGreen;

                DataPoint point2 = new DataPoint(2, count20_40);
                point2.AxisLabel = "20 - 40 %";
                point2.Color = Color.DarkGreen;

                DataPoint point3 = new DataPoint(3, count40_60);
                point3.AxisLabel = "40 - 60 %";
                point3.Color = Color.FromArgb(149, 159, 176);

                DataPoint point4 = new DataPoint(4, count60_80);
                point4.AxisLabel = "60 - 80 %";
                point4.Color = Color.DarkRed;

                DataPoint point5 = new DataPoint(5, countWorst20);
                point5.AxisLabel = "Worst 20 %";
                point5.Color = Color.DarkRed;

                _chartRobustness.Series[0].Points.Add(point1);
                _chartRobustness.Series[0].Points.Add(point2);
                _chartRobustness.Series[0].Points.Add(point3);
                _chartRobustness.Series[0].Points.Add(point4);
                _chartRobustness.Series[0].Points.Add(point5);

                if (max != decimal.MinValue)
                {
                    _chartRobustness.ChartAreas[0].AxisY.Maximum = Convert.ToDouble(max);
                    _chartRobustness.ChartAreas[0].AxisY.Minimum = 0;
                }
            }
            catch (Exception ex)
            {
                SendLogMessage(ex.ToString(), LogMessageType.Error);
            }
        }

        // total profit

        System.Windows.Controls.ComboBox _comboBoxTotalProfitEquityType;

        private Chart _chartTotalProfit;

        private WindowsFormsHost _hostTotalProfit;

        public void ActivateTotalProfitChart(WindowsFormsHost hostTotalProfit, System.Windows.Controls.ComboBox comboBoxProfitType)
        {
            _hostTotalProfit = hostTotalProfit;
            _comboBoxTotalProfitEquityType = comboBoxProfitType;

            _comboBoxTotalProfitEquityType.Items.Add("Absolute");
            _comboBoxTotalProfitEquityType.Items.Add("Persent");
            _comboBoxTotalProfitEquityType.SelectedItem = "Absolute";

            _chartTotalProfit = GetTotalProfitChart();
            _hostTotalProfit.Child = _chartTotalProfit;
            UpdateTotalProfitChart(_gridStepsOfOptimization, _chartTotalProfit, _comboBoxTotalProfitEquityType, _sortBotNumber);

            _comboBoxTotalProfitEquityType.SelectionChanged += _comboBoxprofitType_SelectionChanged;
        }

        System.Windows.Controls.ComboBox _comboBoxTotalProfitEquityTypeCSC;

        private Chart _chartTotalProfitCSC;

        private WindowsFormsHost _hostTotalProfitCSC;

        public void ActivateTotalProfitChartCSC(WindowsFormsHost hostTotalProfit, System.Windows.Controls.ComboBox comboBoxProfitType)
        {
            _hostTotalProfitCSC = hostTotalProfit;
            _comboBoxTotalProfitEquityTypeCSC = comboBoxProfitType;

            _comboBoxTotalProfitEquityTypeCSC.Items.Add("Absolute");
            _comboBoxTotalProfitEquityTypeCSC.Items.Add("Persent");
            _comboBoxTotalProfitEquityTypeCSC.SelectedItem = "Absolute";

            _chartTotalProfitCSC = GetTotalProfitChart();
            _hostTotalProfitCSC.Child = _chartTotalProfitCSC;
            UpdateTotalProfitChart(_gridStepsOfOptimizationCSC, _chartTotalProfitCSC, _comboBoxTotalProfitEquityTypeCSC, _sortBotNumberCSC);

            _comboBoxTotalProfitEquityTypeCSC.SelectionChanged += _comboBoxTotalProfitEquityTypeCSC_SelectionChanged; ;
        }


        private void _comboBoxprofitType_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            UpdateTotalProfitChart(_gridStepsOfOptimization, _chartTotalProfit, _comboBoxTotalProfitEquityType, _sortBotNumber);
        }

        private void _comboBoxTotalProfitEquityTypeCSC_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            UpdateTotalProfitChart(_gridStepsOfOptimizationCSC, _chartTotalProfitCSC, _comboBoxTotalProfitEquityTypeCSC, _sortBotNumberCSC);
        }

        public Chart GetTotalProfitChart()
        {
            Chart result = new Chart();

            ChartArea area = new ChartArea("Prime");

            result.ChartAreas.Clear();
            result.ChartAreas.Add(area);
            result.BackColor = Color.FromArgb(21, 26, 30);
            result.ChartAreas[0].AxisX.TitleForeColor = Color.FromArgb(149, 159, 176);

            for (int i = 0; result.ChartAreas != null && i < result.ChartAreas.Count; i++)
            {
                result.ChartAreas[i].BackColor = Color.FromArgb(21, 26, 30);
                result.ChartAreas[i].BorderColor = Color.FromArgb(17, 18, 23);
                result.ChartAreas[i].CursorY.LineColor = Color.FromArgb(149, 159, 176);
                result.ChartAreas[i].CursorX.LineColor = Color.FromArgb(149, 159, 176);

                foreach (var axe in result.ChartAreas[i].Axes)
                {
                    axe.LabelStyle.ForeColor = Color.FromArgb(149, 159, 176);
                }
            }

            Series series = new Series();
            series.ChartType = SeriesChartType.Candlestick;
            result.Series.Clear();
            result.Series.Add(series);

            result.SuppressExceptions = true;
            return result;
        }

        private void UpdateTotalProfitChart(DataGridView gridStepsOfOptimization, Chart chartTotalProfit, System.Windows.Controls.ComboBox comboBoxTotalProfitEquityType, int sortBotNumber)
        {
            if (chartTotalProfit == null)
            {
                return;
            }

            if (gridStepsOfOptimization.InvokeRequired)
            {
                gridStepsOfOptimization.Invoke(new Action<DataGridView, Chart, System.Windows.Controls.ComboBox, int>(UpdateTotalProfitChart), gridStepsOfOptimization, chartTotalProfit, comboBoxTotalProfitEquityType, sortBotNumber);
                return;
            }

            if (_reports == null)
            {
                return;
            }

            if (_reports.Count <= 1)
            {
                return;
            }

            try
            {
                string profitType = "";
                comboBoxTotalProfitEquityType.Dispatcher.Invoke(() => { profitType = comboBoxTotalProfitEquityType.SelectedItem.ToString(); });

                List<decimal> profitsSumm = new List<decimal>();

                List<decimal> profit = new List<decimal>();

                OptimizerReport inSampleReport = null;

                List<OptimazerFazeReport> outOfSampleReports = new List<OptimazerFazeReport>();

                for (int i = 0; i < _reports.Count; i++)
                {
                    OptimazerFazeReport curReport = _reports[i];

                    if (curReport == null ||
                        curReport.Reports == null ||
                        curReport.Reports.Count == 0)
                    {
                        continue;
                    }

                    if (curReport.Faze.TypeFaze == OptimizerFazeType.InSample)
                    {
                        inSampleReport = curReport.Reports[sortBotNumber];
                    }

                    if (curReport.Faze.TypeFaze == OptimizerFazeType.OutOfSample)
                    {


                        string botName = inSampleReport.BotName.Replace(" InSample", "");
                        // reportToPaint = curReport.Reports.Find(rep => rep.BotName.StartsWith(botName));

                        for (int i2 = 0; i2 < curReport.Reports.Count; i2++)
                        {
                            if (curReport.Reports[i2].BotName.StartsWith(botName))
                            {
                                outOfSampleReports.Add(curReport);
                                if (profitType == "Absolute")
                                {
                                    profit.Add(curReport.Reports[i2].TotalProfit);
                                    if (profitsSumm.Count == 0)
                                    {
                                        profitsSumm.Add(curReport.Reports[i2].TotalProfit);
                                    }
                                    else
                                    {
                                        profitsSumm.Add(profitsSumm[profitsSumm.Count - 1] + curReport.Reports[i2].TotalProfit);
                                    }
                                }
                                else if (profitType == "Persent")
                                {
                                    profit.Add(curReport.Reports[i2].TotalProfitPersent);
                                    if (profitsSumm.Count == 0)
                                    {
                                        profitsSumm.Add(curReport.Reports[i2].TotalProfitPersent);
                                    }
                                    else
                                    {
                                        profitsSumm.Add(profitsSumm[profitsSumm.Count - 1] + curReport.Reports[i2].TotalProfitPersent);
                                    }
                                }

                                break;
                            }
                        }
                    }
                }

                Series series = chartTotalProfit.Series[0];

                series.Points.ClearFast();

                if (profitsSumm.Count == 0)
                {
                    return;
                }

                decimal max = decimal.MinValue;
                decimal min = decimal.MaxValue;

                for (int i = 0; i < profitsSumm.Count; i++)
                {
                    decimal open = 0;
                    decimal close = 0;
                    decimal low = 0;
                    decimal high = 0;

                    if (i > 0)
                    {
                        open = profitsSumm[i - 1];
                    }
                    close = profitsSumm[i];

                    if (close > max)
                    {
                        max = close;
                    }
                    if (close < min)
                    {
                        min = close;
                    }

                    if (close > open)
                    {
                        low = open;
                        high = close;
                    }
                    else
                    {
                        high = open;
                        low = close;
                    }

                    series.Points.AddXY(i + 1, low, high, open, close);

                    if (close > open)
                    {
                        series.Points[series.Points.Count - 1].Color = Color.DarkGreen;
                        series.Points[series.Points.Count - 1].BorderColor = Color.DarkGreen;
                        series.Points[series.Points.Count - 1].BackSecondaryColor = Color.DarkGreen;
                    }
                    else
                    {
                        series.Points[series.Points.Count - 1].Color = Color.DarkRed;
                        series.Points[series.Points.Count - 1].BorderColor = Color.DarkRed;
                        series.Points[series.Points.Count - 1].BackSecondaryColor = Color.DarkRed;
                    }

                    string toolTip = "";

                    toolTip = "OOS " + (i + 1) + "\n" +
                         "start: " + outOfSampleReports[i].Faze.TimeStart.ToString(OsLocalization.ShortDateFormatString) + "\n" +
                         "end: " + outOfSampleReports[i].Faze.TimeEnd.ToString(OsLocalization.ShortDateFormatString) + "\n" +
                         "profit: " + profit[i].ToStringWithNoEndZero();

                    series.Points[series.Points.Count - 1].ToolTip = toolTip;

                    if (i + 1 == profitsSumm.Count)
                    { // last point
                        series.Points[series.Points.Count - 1].Label = Math.Round(profitsSumm[i], 4).ToStringWithNoEndZero();
                        series.Points[series.Points.Count - 1].LabelForeColor = Color.AntiqueWhite;
                    }

                }

                if (max != decimal.MinValue &&
                    min != decimal.MaxValue)
                {
                    max = Math.Round(max + max * 0.2m, 4);
                    min = Math.Round(min, 4);

                    if (max > min)
                    {
                        chartTotalProfit.ChartAreas[0].AxisY.Maximum = Convert.ToDouble(max);
                        chartTotalProfit.ChartAreas[0].AxisY.Minimum = Convert.ToDouble(min);
                    }
                }
            }
            catch (Exception ex)
            {
                SendLogMessage(ex.ToString(), LogMessageType.Error);
            }
        }

        // average profit

        WindowsFormsHost _hostAverageProfitChart;

        private Chart _chartAverageProfit;

        public void ActivateAverageProfitChart(WindowsFormsHost hostAverageProfit)
        {
            _hostAverageProfitChart = hostAverageProfit;
            _chartAverageProfit = GetAverageProfitChart();
            _hostAverageProfitChart.Child = _chartAverageProfit;
            _chartAverageProfit.SuppressExceptions = true;
            ReLoad(_reports);
        }

        private WindowsFormsHost _hostAverageProfitChartCSC;
        private Chart _chartAverageProfitCSC;

        public void ActivateAverageProfitChartCSC(WindowsFormsHost hostAverageProfitCSC)
        {
            _hostAverageProfitChartCSC = hostAverageProfitCSC;
            _chartAverageProfitCSC = GetAverageProfitChart();
            _hostAverageProfitChartCSC.Child = _chartAverageProfitCSC;
            _chartAverageProfitCSC.SuppressExceptions = true;
            ReLoadCSC(_reports, true);
        }

        private Chart GetAverageProfitChart()
        {
            Chart chartAverageProfit = new Chart();

            ChartArea area = new ChartArea("Prime");

            chartAverageProfit.ChartAreas.Clear();
            chartAverageProfit.ChartAreas.Add(area);
            chartAverageProfit.BackColor = Color.FromArgb(21, 26, 30);
            chartAverageProfit.ChartAreas[0].AxisX.TitleForeColor = Color.FromArgb(149, 159, 176);

            for (int i = 0; chartAverageProfit.ChartAreas != null && i < chartAverageProfit.ChartAreas.Count; i++)
            {
                chartAverageProfit.ChartAreas[i].BackColor = Color.FromArgb(21, 26, 30);
                chartAverageProfit.ChartAreas[i].BorderColor = Color.FromArgb(17, 18, 23);
                chartAverageProfit.ChartAreas[i].CursorY.LineColor = Color.FromArgb(149, 159, 176);
                chartAverageProfit.ChartAreas[i].CursorX.LineColor = Color.FromArgb(149, 159, 176);

                foreach (var axe in chartAverageProfit.ChartAreas[i].Axes)
                {
                    axe.LabelStyle.ForeColor = Color.FromArgb(149, 159, 176);
                }
            }

            Series series = new Series();
            series.ChartType = SeriesChartType.Column;
            chartAverageProfit.Series.Clear();
            chartAverageProfit.Series.Add(series);

            Series series2 = new Series();
            series2.ChartType = SeriesChartType.Line;
            chartAverageProfit.Series.Add(series2);

            Series series3 = new Series();
            series3.ChartType = SeriesChartType.Point;
            chartAverageProfit.Series.Add(series3);
            return chartAverageProfit;
        }

        private void UpdateAverageProfitChart(WindowsFormsHost hostAverageProfitChart, Chart chartAverageProfit, int sortBotNumber)
        {
            if (_reports == null ||
                _reports.Count == 0)
            {
                return;
            }
            if (hostAverageProfitChart == null)
            {
                return;
            }

            if (chartAverageProfit.InvokeRequired)
            {
                chartAverageProfit.Invoke(new Action<WindowsFormsHost, Chart, int>(UpdateAverageProfitChart), hostAverageProfitChart, chartAverageProfit, sortBotNumber);
                return;
            }

            try
            {
                List<decimal> values = new List<decimal>();
                decimal maxValue = 0;

                decimal averageProfitPercent = 0;

                List<OptimazerFazeReport> outOfSampleReports = new List<OptimazerFazeReport>();

                for (int i = 0; i < _reports.Count; i += 2)
                {
                    // берём из ИнСампле таблицу роботов
                    List<OptimizerReport> bots = _reports[i].Reports;

                    OptimizerReport bestBot = _reports[i].Reports[sortBotNumber];

                    // находим этого робота в аутОфСемпл

                    if (i + 1 == _reports.Count)
                    {
                        break;
                    }

                    OptimizerReport bestBotInOutOfSample
                        = _reports[i + 1].Reports.Find(b => b.BotName.Replace(" OutOfSample", "") == bestBot.BotName.Replace(" InSample", ""));

                    if (bestBotInOutOfSample == null)
                    {
                        continue;
                    }

                    outOfSampleReports.Add(_reports[i + 1]);

                    decimal value = bestBotInOutOfSample.AverageProfitPercentOneContract;

                    if (maxValue < value)
                    {
                        maxValue = value;
                    }

                    if (values.Count == 0)
                    {
                        values.Add(value);
                    }
                    else
                    {
                        values.Add(value);
                    }

                    averageProfitPercent += bestBotInOutOfSample.AverageProfitPercentOneContract;
                }
                if (values.Count != 0)
                {
                    averageProfitPercent = averageProfitPercent / values.Count;
                }

                // прорисовка

                Series seriesOosValues = chartAverageProfit.Series[0];
                Series seriesAverageLine = chartAverageProfit.Series[1];
                Series seriesAveragePoint = chartAverageProfit.Series[2];

                seriesOosValues.Points.ClearFast();
                seriesAverageLine.Points.ClearFast();
                seriesAveragePoint.Points.ClearFast();

                if (values.Count == 0)
                {
                    return;
                }

                decimal max = decimal.MinValue;
                decimal min = decimal.MaxValue;

                for (int i = 0; i < values.Count; i++)
                {
                    seriesOosValues.Points.AddXY(i + 1, values[i]);

                    if (values[i] > max)
                    {
                        max = values[i];
                    }

                    if (values[i] < min)
                    {
                        min = values[i];
                    }

                    if (values[i] > 0)
                    {
                        seriesOosValues.Points[seriesOosValues.Points.Count - 1].Color = Color.DarkGreen;
                        seriesOosValues.Points[seriesOosValues.Points.Count - 1].BorderColor = Color.DarkGreen;
                        seriesOosValues.Points[seriesOosValues.Points.Count - 1].BackSecondaryColor = Color.DarkGreen;
                    }
                    else
                    {
                        seriesOosValues.Points[seriesOosValues.Points.Count - 1].Color = Color.DarkRed;
                        seriesOosValues.Points[seriesOosValues.Points.Count - 1].BorderColor = Color.DarkRed;
                        seriesOosValues.Points[seriesOosValues.Points.Count - 1].BackSecondaryColor = Color.DarkRed;
                    }

                    string toolTip = "";

                    toolTip = "OOS " + (i + 1) + "\n" +
                        "start: " + outOfSampleReports[i].Faze.TimeStart.ToString(OsLocalization.ShortDateFormatString) + "\n" +
                         "end: " + outOfSampleReports[i].Faze.TimeEnd.ToString(OsLocalization.ShortDateFormatString) + "\n" +
                         "P/L % " + Math.Round(values[i], 4).ToStringWithNoEndZero();

                    seriesOosValues.Points[seriesOosValues.Points.Count - 1].ToolTip = toolTip;
                }

                if (averageProfitPercent != 0)
                {
                    seriesAverageLine.Points.AddXY(1, averageProfitPercent);

                    seriesAverageLine.Points.AddXY(values.Count, averageProfitPercent);

                    for (int i = 0; i < seriesAverageLine.Points.Count; i++)
                    {
                        seriesAverageLine.Points[i].Color = Color.AntiqueWhite;
                    }

                    string label = "Average: " + Math.Round(averageProfitPercent, 4).ToStringWithNoEndZero();
                    seriesAveragePoint.Points.AddXY(values.Count - 1, maxValue + maxValue * 0.05m);
                    seriesAveragePoint.Points[0].Color = Color.AntiqueWhite;

                    seriesAveragePoint.Points[0].Label = label;
                    seriesAveragePoint.Points[0].LabelForeColor = Color.AntiqueWhite;
                }

                if (max != decimal.MinValue &&
                    min != decimal.MaxValue)
                {
                    max = Math.Round(max + max * 0.2m, 4);
                    min = Math.Round(min, 4);

                    if (max > min)
                    {
                        chartAverageProfit.ChartAreas[0].AxisY.Maximum = Convert.ToDouble(max);
                        chartAverageProfit.ChartAreas[0].AxisY.Minimum = Convert.ToDouble(min);
                    }
                }
            }
            catch (Exception ex)
            {
                SendLogMessage(ex.ToString(), LogMessageType.Error);
            }
        }

        // profit factor

        WindowsFormsHost _hostProfitFactor;

        private Chart _chartProfitFactor;

        public void ActivateProfitFactorChart(WindowsFormsHost hostProfitFactor)
        {
            _hostProfitFactor = hostProfitFactor;
            _chartProfitFactor = GetProfitFactorChart();
            _hostProfitFactor.Child = _chartProfitFactor;
            _chartProfitFactor.SuppressExceptions = true;

            ReLoad(_reports);
        }


        private WindowsFormsHost _hostProfitFactorCSC;

        private Chart _chartProfitFactorCSC;

        public void ActivateProfitFactorChartCSC(WindowsFormsHost hostProfitFactor)
        {
            _hostProfitFactorCSC = hostProfitFactor;
            _chartProfitFactorCSC = GetProfitFactorChart();
            _hostProfitFactorCSC.Child = _chartProfitFactorCSC;
            _chartProfitFactorCSC.SuppressExceptions = true;
            ReLoadCSC(_reports, true);
        }

        private Chart GetProfitFactorChart()
        {
            Chart result = new Chart();

            ChartArea area = new ChartArea("Prime");

            result.ChartAreas.Clear();
            result.ChartAreas.Add(area);
            result.BackColor = Color.FromArgb(21, 26, 30);
            result.ChartAreas[0].AxisX.TitleForeColor = Color.FromArgb(149, 159, 176);

            for (int i = 0; result.ChartAreas != null && i < result.ChartAreas.Count; i++)
            {
                result.ChartAreas[i].BackColor = Color.FromArgb(21, 26, 30);
                result.ChartAreas[i].BorderColor = Color.FromArgb(17, 18, 23);
                result.ChartAreas[i].CursorY.LineColor = Color.FromArgb(149, 159, 176);
                result.ChartAreas[i].CursorX.LineColor = Color.FromArgb(149, 159, 176);

                foreach (var axe in result.ChartAreas[i].Axes)
                {
                    axe.LabelStyle.ForeColor = Color.FromArgb(149, 159, 176);
                }
            }

            Series series = new Series();
            series.ChartType = SeriesChartType.Column;
            result.Series.Clear();
            result.Series.Add(series);

            Series series2 = new Series();
            series2.ChartType = SeriesChartType.Line;
            result.Series.Add(series2);

            Series series3 = new Series();
            series3.ChartType = SeriesChartType.Point;
            result.Series.Add(series3);
            return result;
        }

        private void UpdateProfitFactorChart(WindowsFormsHost hostProfitFactor, Chart chartProfitFactor)
        {
            if (_reports == null ||
                           _reports.Count == 0)
            {
                return;
            }
            if (hostProfitFactor == null)
            {
                return;
            }

            if (chartProfitFactor.InvokeRequired)
            {
                chartProfitFactor.Invoke(new Action<WindowsFormsHost, Chart>(UpdateProfitFactorChart), hostProfitFactor, chartProfitFactor);
                return;
            }

            try
            {
                List<decimal> values = new List<decimal>();

                decimal maxValue = 0;

                decimal averageProfitFactor = 0;

                List<OptimazerFazeReport> outOfSampleReports = new List<OptimazerFazeReport>();

                for (int i = 0; i < _reports.Count; i += 2)
                {
                    // берём из ИнСампле таблицу роботов
                    List<OptimizerReport> bots = _reports[i].Reports;

                    OptimizerReport bestBot = _reports[i].Reports[0];

                    // находим этого робота в аутОфСемпл

                    if (i + 1 == _reports.Count)
                    {
                        break;
                    }

                    OptimizerReport bestBotInOutOfSample
                        = _reports[i + 1].Reports.Find(b => b.BotName.Replace(" OutOfSample", "") == bestBot.BotName.Replace(" InSample", ""));

                    if (bestBotInOutOfSample == null)
                    {
                        continue;
                    }

                    outOfSampleReports.Add(_reports[i + 1]);

                    decimal value = bestBotInOutOfSample.ProfitFactor;

                    if (maxValue < value)
                    {
                        maxValue = value;
                    }

                    if (values.Count == 0)
                    {
                        values.Add(value);
                    }
                    else
                    {
                        values.Add(value);
                    }

                    averageProfitFactor += bestBotInOutOfSample.ProfitFactor;
                }
                if (values.Count != 0)
                {
                    averageProfitFactor = averageProfitFactor / values.Count;
                }

                // прорисовка

                Series seriesOosValues = chartProfitFactor.Series[0];
                Series seriesAverageLine = chartProfitFactor.Series[1];
                Series seriesAveragePoint = chartProfitFactor.Series[2];

                seriesOosValues.Points.ClearFast();
                seriesAverageLine.Points.ClearFast();
                seriesAveragePoint.Points.ClearFast();

                if (values.Count == 0)
                {
                    return;
                }

                decimal max = decimal.MinValue;
                decimal min = decimal.MaxValue;

                for (int i = 0; i < values.Count; i++)
                {
                    seriesOosValues.Points.AddXY(i + 1, values[i]);

                    if (values[i] > max)
                    {
                        max = values[i];
                    }

                    if (values[i] < min)
                    {
                        min = values[i];
                    }

                    if (values[i] > 0)
                    {
                        seriesOosValues.Points[seriesOosValues.Points.Count - 1].Color = Color.DarkGreen;
                        seriesOosValues.Points[seriesOosValues.Points.Count - 1].BorderColor = Color.DarkGreen;
                        seriesOosValues.Points[seriesOosValues.Points.Count - 1].BackSecondaryColor = Color.DarkGreen;
                    }
                    else
                    {
                        seriesOosValues.Points[seriesOosValues.Points.Count - 1].Color = Color.DarkRed;
                        seriesOosValues.Points[seriesOosValues.Points.Count - 1].BorderColor = Color.DarkRed;
                        seriesOosValues.Points[seriesOosValues.Points.Count - 1].BackSecondaryColor = Color.DarkRed;
                    }

                    string toolTip = "";

                    toolTip = "OOS " + (i + 1) + "\n" +
                        "start: " + outOfSampleReports[i].Faze.TimeStart.ToString(OsLocalization.ShortDateFormatString) + "\n" +
                         "end: " + outOfSampleReports[i].Faze.TimeEnd.ToString(OsLocalization.ShortDateFormatString) + "\n" +
                         "Profit Factor: " + Math.Round(values[i], 4).ToStringWithNoEndZero();

                    seriesOosValues.Points[seriesOosValues.Points.Count - 1].ToolTip = toolTip;
                }

                if (averageProfitFactor != 0)
                {
                    seriesAverageLine.Points.AddXY(1, averageProfitFactor);

                    seriesAverageLine.Points.AddXY(values.Count, averageProfitFactor);

                    for (int i = 0; i < seriesAverageLine.Points.Count; i++)
                    {
                        seriesAverageLine.Points[i].Color = Color.AntiqueWhite;
                    }

                    string label = "Average: " + Math.Round(averageProfitFactor, 4).ToStringWithNoEndZero();
                    seriesAveragePoint.Points.AddXY(values.Count - 1, maxValue + maxValue * 0.05m);
                    seriesAveragePoint.Points[0].Color = Color.AntiqueWhite;

                    seriesAveragePoint.Points[0].Label = label;
                    seriesAveragePoint.Points[0].LabelForeColor = Color.AntiqueWhite;
                }

                if (max != decimal.MinValue &&
                    min != decimal.MaxValue)
                {
                    max = Math.Round(max + max * 0.2m, 4);
                    min = Math.Round(min, 4);

                    if (max > min)
                    {
                        chartProfitFactor.ChartAreas[0].AxisY.Maximum = Convert.ToDouble(max);
                        chartProfitFactor.ChartAreas[0].AxisY.Minimum = Convert.ToDouble(min);
                    }
                }
            }
            catch (Exception ex)
            {
                SendLogMessage(ex.ToString(), LogMessageType.Error);
            }
        }

        // logging/логирование

        /// <summary>
        /// send up a new message
        /// выслать наверх новое сообщение
        /// </summary>
        /// <param name="message">Message text/текст сообщения</param>
        /// <param name="type">message type/тип сообщения</param>
        private void SendLogMessage(string message, LogMessageType type)
        {
            if (LogMessageEvent != null)
            {
                LogMessageEvent(message, type);
            }
        }


        /// <summary>
        /// event: new message for log
        /// событие: новое сообщение для лога
        /// </summary>
        public event Action<string, LogMessageType> LogMessageEvent;

        public event Action<OptimazerFazeReport, OptimizerReport> ChartButtonClickEvent;

    }
}
