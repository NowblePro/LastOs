using Microsoft.Office.Core;
using OsEngine.Charts;
using OsEngine.Entity;
using OsEngine.Journal.Internal;
using OsEngine.Language;
using OsEngine.Logging;
using OsEngine.Market.Servers.Optimizer;
using OsEngine.Market.Servers.Tester;
using OsEngine.OsOptimizer.OptEntity;
using OsEngine.OsTrader.Panels;
using OsEngine.Robots;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;
using System.Windows.Forms.Integration;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TaskbarClock;

namespace OsEngine.OsOptimizer
{
    public class OptimizerReportCharting
    {
        private DataGridViewTextBoxCell cell0 = new DataGridViewTextBoxCell();
        private OptimizerMaster _master;
        public OptimizerReportCharting(
            WindowsFormsHost hostStepsOfOptimization,
            WindowsFormsHost hostRobustness,
            WindowsFormsHost hostStepsOfOptimizationCSC,
            WindowsFormsHost hostDynamicTable,
            System.Windows.Controls.ComboBox boxTypeSort,
            System.Windows.Controls.Label labelRobustnessMetricValue,
            System.Windows.Controls.ComboBox boxTypeSortBotNum,
            System.Windows.Controls.ComboBox boxTypeSortCSC,
            System.Windows.Controls.ComboBox boxTypeSortBotNumCSC,
            OptimizerMaster master
            )
        {
            _master = master;
            _sortBotsType = SortBotsType.TotalProfit;
            _currentCulture = OsLocalization.CurCulture;
            _hostStepsOfOptimization = hostStepsOfOptimization;
            _hostStepsOfOptimizationCSC = hostStepsOfOptimizationCSC;
            _hostDynamicTable = hostDynamicTable;
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
            _boxTypeSortBotNumCSC.SelectionChanged += _boxTypeSortBotNumCSC_SelectionChanged;

            _boxTypeSortCSC = boxTypeSortCSC;

            string[] cscSortTypes = Enum.GetNames(typeof(CSCSortType));
            foreach (string sortType in cscSortTypes)
            {
                boxTypeSortCSC.Items.Add(sortType);
            }

            boxTypeSortCSC.SelectedItem = CSCSortType.FRS.ToString();
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
                if (_master.CustomFazes) return;
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
                UpdateTotalProfitChart(_gridStepsOfOptimization, _chartTotalProfit, _comboBoxTotalProfitEquityType, _sortBotNumber, _reports);
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
            if (reports == null || reports.Count == 0) return;

            OptimazerFazeReport first = reports.First();
            OptimazerFazeReport.SortResults(first.Reports, _sortBotsTypeCSC);
            Parallel.For(1, reports.Count, i => 
            {
                OptimazerFazeReport faze = reports[i];

                IEnumerable<OptimizerReport> reportsFiltered = faze.Reports.Where(r => first.Reports.Any(f => f.GetParamsToDataTable() == r.GetParamsToDataTable()));
                var indexes = reportsFiltered.Select(r => new { Report = r, Index = first.Reports.IndexOf(first.Reports.Where(f => f.GetParamsToDataTable() == r.GetParamsToDataTable()).First()) });
                var newList = indexes.ToList();
                newList.Sort((rep1, rep2) => rep1.Index.CompareTo(rep2.Index));
                faze.Reports = newList.Select(r => r.Report).ToList();
            });
        }

        private void SortDynamicResults(List<OptimazerFazeReport> reports)
        {
            if (reports == null || reports.Count == 0) return;

            OptimazerFazeReport first = reports.First();
            OptimazerFazeReport.SortResults(first.Reports, _sortTypeDynamicTable);
            Parallel.For(1, reports.Count, i =>
            {
                OptimazerFazeReport faze = reports[i];
                var indexes = faze.Reports.Select(r => new { Report = r, Index = first.Reports.IndexOf(first.Reports.Where(f => f.GetParamsToDataTable() == r.GetParamsToDataTable()).First()) });
                var newList = indexes.ToList();
                newList.Sort((rep1, rep2) => rep1.Index.CompareTo(rep2.Index));
                faze.Reports = newList.Select(r => r.Report).ToList();
            });
        }

        private void ReLoadCSCTask(List<OptimazerFazeReport> reports, bool calculate = false, bool calculateFRS = false)
        {
            try
            {
                if (_master.CustomFazes) return;
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
                UpdateTotalProfitChart(_gridStepsOfOptimizationCSC, _chartTotalProfitCSC, _comboBoxTotalProfitEquityTypeCSC, _sortBotNumberCSC, _reports);
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
            if (_sortBotNumber >= reports.Count)
            {
                _sortBotNumber = reports.Count - 1;
            }

            if (_sortBotNumberCSC >= reports.Count)
            {
                _sortBotNumberCSC = reports.Count - 1;
            }
        }

        private SortBotsType _sortBotsType;
        private CSCSortType _sortBotsTypeCSC = CSCSortType.FRS;

        private int _sortBotPercent = 0;
        private int _sortBotPercentCSC = 0;

        private int _sortBotNumber = 0;
        private int _sortBotNumberCSC = 0;

        private List<OptimazerFazeReport> _reports;

        // fazes in table

        private WindowsFormsHost _hostStepsOfOptimization;
        private WindowsFormsHost _hostStepsOfOptimizationCSC;
        private WindowsFormsHost _hostDynamicTable;
        private DataGridView _gridStepsOfOptimization;
        private DataGridView _gridStepsOfOptimizationCSC;
        private DataGridView _gridDynamicTable;

        private DataGridView GetStepsOfOptimizationDGV()
        {
            DataGridView gridStepsOfOptimization = DataGridFactory.GetDataGridView(DataGridViewSelectionMode.ColumnHeaderSelect,
                DataGridViewAutoSizeRowsMode.None, true);
            gridStepsOfOptimization.CellMouseClick += _gridResults_CellMouseClick;
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

        private DataGridView GetDynamicDGV()
        {
            DataGridView gridDynamicStepsTable = DataGridFactory.GetDataGridView(DataGridViewSelectionMode.ColumnHeaderSelect,
                DataGridViewAutoSizeRowsMode.None, false);
            ContextMenu menu = new ContextMenu();


            gridDynamicStepsTable.ContextMenu = menu;
            cell0.Style = gridDynamicStepsTable.DefaultCellStyle;
            gridDynamicStepsTable.ScrollBars = ScrollBars.Vertical;

            gridDynamicStepsTable.CellMouseClick += GridDynamicStepsTable_CellMouseClick;

            gridDynamicStepsTable.Columns.Add(GetColumn("Period"));
            gridDynamicStepsTable.Columns.Add(GetColumn("Start", readOnly: false));
            gridDynamicStepsTable.Columns.Add(GetColumn("End", readOnly: false));
            gridDynamicStepsTable.Columns.Add(GetColumn("Period name", readOnly: false));
            gridDynamicStepsTable.Columns.Add(GetColumn("Parameters", readOnly: false));
            gridDynamicStepsTable.Columns.Add(GetColumn("Profit", readOnly: false));
            gridDynamicStepsTable.Columns.Add(GetColumn("Average profit %", readOnly: false));
            gridDynamicStepsTable.Columns.Add(GetColumn("Position count", readOnly: false));
            gridDynamicStepsTable.Columns.Add(GetColumn("Sharp ratio", readOnly: false));
            gridDynamicStepsTable.Columns.Add(GetColumn("Max DrawDown", readOnly: false));
            gridDynamicStepsTable.Columns.Add(GetColumn("RR Profit", readOnly: false));
            gridDynamicStepsTable.Columns.Add(GetColumn("Win rate", readOnly: false));
            gridDynamicStepsTable.Columns.Add(GetColumn("OOS Profit", readOnly: false));
            gridDynamicStepsTable.Columns.Add(GetColumn("Eval", readOnly: false));

            DataGridViewButtonColumn column10 = new DataGridViewButtonColumn();
            column10.CellTemplate = new DataGridViewButtonCell();
            column10.ReadOnly = true;
            column10.AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
            gridDynamicStepsTable.Columns.Add(column10);

            DataGridViewButtonColumn column11 = new DataGridViewButtonColumn();
            column11.CellTemplate = new DataGridViewButtonCell();
            column11.ReadOnly = true;
            column11.AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
            gridDynamicStepsTable.Columns.Add(column11);

            gridDynamicStepsTable.Rows.Add(null, null);
            return gridDynamicStepsTable;
        }

        private void GridDynamicStepsTable_CellMouseClick(object sender, DataGridViewCellMouseEventArgs e)
        {
            if (e.RowIndex < 0)
            {
                return;
            }

            if (_reports == null || _master.Phazes.OutOfSamplePeriods.Count + 1 < e.RowIndex + 1)
            {
                return;
            }

            if (sender is DataGridView dgv)
            {
                if (e.ColumnIndex == 14)
                {
                    OptimazerFazeReport fazeReport = new OptimazerFazeReport(_reports[e.RowIndex]);

                    string parameterString = $"{dgv.Rows[e.RowIndex].Cells[4].Value}";

                    OptimizerReport report = fazeReport.Reports.Where(r => r.GetParamsToDataTable() == parameterString).SingleOrDefault();

                    if (report == null)
                    {
                        return;
                    }

                    if (ChartButtonClickEvent != null)
                    {
                        ChartButtonClickEvent(fazeReport, report);
                    }
                }

                if (e.ColumnIndex == 15)
                {
                    OptimazerFazeReport fazeReport = new OptimazerFazeReport(_reports[e.RowIndex]);

                    string parameterString = $"{dgv.Rows[e.RowIndex].Cells[4].Value}";

                    OptimizerReport report = fazeReport.Reports.Where(r => r.GetParamsToDataTable() == parameterString).SingleOrDefault();

                    if (report == null)
                    {
                        return;
                    }

                    if (FullChartButtonClickEvent != null)
                    {
                        FullChartButtonClickEvent(fazeReport, report);
                    }
                }
            }
        }

        private void CreateStepsOfOptimization()
        {
            _gridStepsOfOptimization = GetStepsOfOptimizationDGV();
            _gridStepsOfOptimizationCSC = GetStepsOfOptimizationDGV();
            _gridDynamicTable = GetDynamicDGV();

            _hostStepsOfOptimization.Child = _gridStepsOfOptimization;
            _hostStepsOfOptimizationCSC.Child = _gridStepsOfOptimizationCSC;
            _hostDynamicTable.Child = _gridDynamicTable;
        }

        private WindowsFormsHost _hostFRS;
        private DataGridView _gridFRS;

        private decimal _pprWeight = 0.25m;
        private decimal _trWeight = 0.25m;
        private decimal _gprWeight = 0.25m;
        public decimal PPRWeight => _pprWeight;
        public decimal TRWeight => _trWeight;
        public decimal GPRWeight => _gprWeight;

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
                _gridFRS.Columns.Add(GetColumn("PPR", 80, readOnly: false));
                _gridFRS.Columns.Add(GetColumn("TR", 80, readOnly: false));
                _gridFRS.Columns.Add(GetColumn("GPR", 110, readOnly: false));
                _gridFRS.Columns.Add(GetColumn("Total Profit", 110, readOnly: false));
                _gridFRS.Columns.Add(GetColumn("Max DrawDown", 110, readOnly: false));
                _gridFRS.Columns.Add(GetColumn("TProfit/MaxDD", 110, readOnly: false));

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
            decimal pprWeight = GetWeightFromTable(_gridFRS, 2, 2);
            decimal trWeight = GetWeightFromTable(_gridFRS, 3, 2);
            decimal gprWeight = GetWeightFromTable(_gridFRS, 4, 2);

            if (_pprWeight != pprWeight ||
                _trWeight != trWeight ||
                _gprWeight != gprWeight)
            {
                _pprWeight = pprWeight;
                _trWeight = trWeight;
                _gprWeight = gprWeight;
            }
        }

        public void UpdateWeights(decimal ppr, decimal tr, decimal gpr)
        {
            _pprWeight = ppr;
            _trWeight = tr;
            _gprWeight = gpr;
            Updateweights();
        }

        public event EventHandler WeightsChanged;
        internal void Updateweights()
        {
            decimal sum = _pprWeight + _trWeight + _gprWeight;
            try
            {
                _pprWeight /= sum;
                _trWeight /= sum;
                _gprWeight /= sum;
            }
            catch
            {
                _pprWeight = 1m;
                _trWeight = 1m;
                _gprWeight = 1m;
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

                DataGridFactory.AddTextBoxCell(row0, "Robot");
                DataGridFactory.AddTextBoxCell(row0, bot.FRS);
                DataGridFactory.AddTextBoxCell(row0, bot.PPR);
                DataGridFactory.AddTextBoxCell(row0, bot.TR);
                DataGridFactory.AddTextBoxCell(row0, bot.GPR);
                DataGridFactory.AddTextBoxCell(row0, bot.TotalProfitAllPeriod);
                DataGridFactory.AddTextBoxCell(row0, bot.MaxDrawDownAllPeriod);
                DataGridFactory.AddTextBoxCell(row0, bot.ProfitToDrawDownAllPeriod);

                _gridFRS.Rows.Add(row0);

                DataGridViewRow row1 = new DataGridViewRow();
                row1.Height = 30;

                DataGridFactory.AddTextBoxCell(row1, "Strategy");
                DataGridFactory.AddTextBoxCell(row1, _strategyFRS);
                DataGridFactory.AddTextBoxCell(row1, _strategyPPR);
                DataGridFactory.AddTextBoxCell(row1, _strategyTR);
                DataGridFactory.AddTextBoxCell(row1, _strategyGPR);
                DataGridFactory.AddTextBoxCell(row1, "-");
                DataGridFactory.AddTextBoxCell(row1, "-");
                DataGridFactory.AddTextBoxCell(row1, "-");

                _gridFRS.Rows.Add(row1);

                DataGridViewRow row2 = new DataGridViewRow();
                row2.Height = 30;
                row2.ReadOnly = false;

                DataGridFactory.AddTextBoxCell(row2, "FRS weights");
                DataGridFactory.AddTextBoxCell(row2, "-");
                DataGridFactory.AddTextBoxCell(row2, _pprWeight, false);
                DataGridFactory.AddTextBoxCell(row2, _trWeight, false);
                DataGridFactory.AddTextBoxCell(row2, _gprWeight, false);
                DataGridFactory.AddTextBoxCell(row2, "-");
                DataGridFactory.AddTextBoxCell(row2, "-");
                DataGridFactory.AddTextBoxCell(row2, "-");

                _gridFRS.Rows.Add(row2);

                DataGridViewRow row3 = new DataGridViewRow();
                row3.Height = 30;
                row3.ReadOnly = false;

                DataGridFactory.AddTextBoxCell(row3, "Rank");
                DataGridFactory.AddTextBoxCell(row3, $"{bot.FRSRank}/{botCount}");
                DataGridFactory.AddTextBoxCell(row3, $"{bot.PPRRank}/{botCount}");
                DataGridFactory.AddTextBoxCell(row3, $"{bot.TRRank}/{botCount}");
                DataGridFactory.AddTextBoxCell(row3, $"{bot.GPRRank}/{botCount}");
                DataGridFactory.AddTextBoxCell(row3, $"{bot.TotalProfitAllPeriodRank}/{botCount}");
                DataGridFactory.AddTextBoxCell(row3, $"{bot.MaxDrawDownAllPeriodRank}/{botCount}");
                DataGridFactory.AddTextBoxCell(row3, $"{bot.ProfitToDrawDownAllPeriodRank}/{botCount}");
                _gridFRS.Rows.Add(row3);
            }
            catch { }
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
            }
            catch (Exception ex)
            {
                SendLogMessage(ex.ToString(), LogMessageType.Error);
            }
        }

        private void GetFazeFromPeriod(Period period)
        {
            period.Report = _reports.Where(r => r.Faze.TimeStart == period.Start && r.Faze.TimeEnd == period.End).Single();
        }

        private void GetFazeFromPeriod(List<Period> periods)
        {
            for (int i = 0; i < periods.Count; i++)
            {
                Period period = periods[i];
                GetFazeFromPeriod(period);
            }
        }

        private List<OptimazerFazeReport> FillDynamicTable(DataGridView table, bool groupPeriods)
        {
            if (table.InvokeRequired)
            {
                return table.Invoke(new Func<DataGridView, bool, List<OptimazerFazeReport>>(FillDynamicTable), table, groupPeriods) as List<OptimazerFazeReport>;
            }
            table.Rows.Clear();
            List<OptimazerFazeReport> result = new List<OptimazerFazeReport>();
            List<Period> periods = new List<Period>() { _master.Phazes.InSamplePeriod };
            periods.AddRange(_master.Phazes.OutOfSamplePeriods);

            string parameters = string.Empty;
            if (_reports.Count > 0)
            {
                parameters = _reports[0].Reports[_sortTypeDynamicTableNum].GetParamsToDataTable();
            }

            if (_master.Phazes.InSamplePeriod.IsDefined)
            {
                GetFazeFromPeriod(_master.Phazes.InSamplePeriod);
            }
            FillPeriod(_master.Phazes.InSamplePeriod, "In Sample");
            OptimazerFazeReport insampleForChart = new OptimazerFazeReport();
            insampleForChart.Faze = new OptimizerFaze(_master.Phazes.InSamplePeriod.Report.Faze);
            insampleForChart.Reports.Add(_master.Phazes.InSamplePeriod.Report.Reports[_sortTypeDynamicTableNum]);
            insampleForChart.Faze.SpecialPeriodName = _master.Phazes.InSamplePeriod.Name;
            result.Add(insampleForChart);
            // Заполнение Out Of Sample периодов

            if (groupPeriods)
            {
                for (int i = 0; i < _master.Phazes.OutOfSamplePeriods.Count; i++)
                {
                    Period period = _master.Phazes.OutOfSamplePeriods[i];
                    if (period.IsDefined)
                    {
                        GetFazeFromPeriod(period);
                    }
                }
                // Объединить периоды по названиям
                IEnumerable<IGrouping<string, Period>> groups = _master.Phazes.OutOfSamplePeriods.Where(p => !string.IsNullOrEmpty(p.Name)).GroupBy(p => p.Name);
                IEnumerable<Period> nonamePeriods = _master.Phazes.OutOfSamplePeriods.Where(p => string.IsNullOrEmpty(p.Name));
                List<Period> grouped = new List<Period>();
                foreach (IGrouping<string, Period> group in groups)
                {
                    Period period = new Period() { Name = group.Key, Start = group.Min(p => p.Start.Value), End = group.Max(p => p.End.Value )};
                    period.Report = new OptimazerFazeReport();
                    period.Report.Faze = new OptimizerFaze(group.First().Report.Faze);
                    period.Report.Faze.SpecialPeriodName = group.Key;
                    result.Add(period.Report);
                    period.Report.Reports.Add(new OptimizerReport());
                    period.Report.Reports[0].BotName = group.First().Report.Reports[_sortTypeDynamicTableNum].BotName;
                    period.Report.Reports[0].TotalProfit = group.Sum(p => p.Report.Reports[_sortTypeDynamicTableNum].TotalProfit);
                    period.Report.Reports[0].PositionsCount = group.Sum(p => p.Report.Reports[_sortTypeDynamicTableNum].PositionsCount);
                    period.Report.Reports[0].MaxDrowDawn = group.Min(p => p.Report.Reports[_sortTypeDynamicTableNum].MaxDrowDawn);
                    period.Report.Reports[0].AverageProfit = group.Sum(p => p.Report.Reports[_sortTypeDynamicTableNum].AverageProfit) / group.Count();
                    period.Report.Reports[0].RRProfit = group.Max(p => p.Report.Reports[_sortTypeDynamicTableNum].RRProfit);
                    period.Report.Reports[0].WinRate = group.Average(p => p.Report.Reports[_sortTypeDynamicTableNum].WinRate);
                    period.Report.Reports[0].Eval = group.Average(p => p.Report.Reports[_sortTypeDynamicTableNum].Eval);
                    FillPeriod(period, "Out Of Sample", true);
                }
            }
            else
            {
                for (int i = 0; i < _master.Phazes.OutOfSamplePeriods.Count; i++)
                {
                    Period period = _master.Phazes.OutOfSamplePeriods[i];
                    if (period.IsDefined)
                    {
                        GetFazeFromPeriod(period);
                    }
                    FillPeriod(period, "Out Of Sample");
                }
            }

            return result;

            string GetCellValue(Period period, int columnIndex, bool firstReport = false)
            {
                string cellVAlue = string.Empty;
                if (period.Report != null && period.Report.Reports.Count > 0)
                {
                    OptimazerFazeReport faze = period.Report;
                    OptimizerReport report = faze.Reports[firstReport ? 0 : _sortTypeDynamicTableNum];
                    if (faze != null)
                    {
                        if (columnIndex == 5)
                        {
                            // Profit
                            cellVAlue = $"{Math.Round(report.TotalProfit, 3)}";
                        }
                        if (columnIndex == 6)
                        {
                            // Average Profit
                            cellVAlue = $"{Math.Round(report.AverageProfit, 3)}";
                        }
                        if (columnIndex == 7)
                        {
                            // Positions count
                            cellVAlue = $"{report.PositionsCount}";
                        }
                        if (columnIndex == 8)
                        {
                            // Sharp Ratio
                            cellVAlue = $"{Math.Round(report.SharpRatio, 3)}";
                        }

                        if (columnIndex == 9)
                        {
                            // Max DrawDown
                            cellVAlue = $"{Math.Round(report.MaxDrowDawn, 3)}";
                        }

                        if (columnIndex == 10)
                        {
                            // rr
                            cellVAlue = $"{Math.Round(report.RRProfit, 3)}";
                        }

                        if (columnIndex == 11)
                        {
                            // win rate
                            cellVAlue = $"{Math.Round(report.WinRate, 3)}";
                        }

                        if (columnIndex == 12)
                        {
                            // суммарный тотал профит за весь out of sample
                            cellVAlue = $"{Math.Round(report.TotalOOSProfit, 3)}";
                        }

                        if (columnIndex == 13)
                        {
                            // eval
                            cellVAlue = $"{Math.Round(report.Eval, 3)}";
                        }

                        if (columnIndex == 14)
                        {
                            // График
                            cellVAlue = $"График";
                        }

                        if (columnIndex == 15)
                        {
                            // График
                            cellVAlue = $"Полный график";
                        }
                    }
                }
                return cellVAlue;
            }

            void FillPeriod(Period period, string periodName, bool firstReport = false)
            {
                lock (table)
                {
                    DataGridViewRow row = new DataGridViewRow() { Height = 30 };

                    DataGridFactory.AddTextBoxCell(row, periodName, true);

                    for (int i = 1; i < table.Columns.Count; i++)
                    {
                        if (i == 1)
                        {
                            if (period.Start == null)
                            {
                                DataGridViewButtonCell cell1 = new DataGridViewButtonCell();
                                cell1.Value = "Редактировать";
                                row.Cells.Add(cell1);
                            }
                            else
                            {
                                DataGridViewTextBoxCell cell = DataGridFactory.AddTextBoxCell(row, period.Start.Value.ToString("dd.MM.yyyy"));
                                cell.ToolTipText = period.Start.Value.ToString();
                            }
                        }
                        else if (i == 2)
                        {
                            if (period.End == null)
                            {
                                DataGridViewButtonCell cell1 = new DataGridViewButtonCell();
                                cell1.Value = "Редактировать";
                                row.Cells.Add(cell1);
                            }
                            else
                            {
                                DataGridViewTextBoxCell cell = DataGridFactory.AddTextBoxCell(row, period.End.Value.ToString("dd.MM.yyyy"));
                                cell.ToolTipText = period.End.Value.ToString();
                            }
                        }
                        else
                        {
                            if (period.IsDefined)
                            {
                                if (i == 3)
                                {
                                    // Название периода (опционально)
                                    DataGridFactory.AddTextBoxCell(row, period.Name, true);
                                }
                                else if (i == 4)
                                {
                                    // Параметры
                                    DataGridFactory.AddTextBoxCell(row, parameters);
                                }
                                else if (i == 14 || i == 15)
                                {
                                    // График
                                    DataGridViewButtonCell cellChart = new DataGridViewButtonCell();
                                    cellChart.Value = $"График";
                                    row.Cells.Add(cellChart);
                                    cellChart.ReadOnly = true;
                                }
                                else
                                {
                                    DataGridFactory.AddTextBoxCell(row, GetCellValue(period, i, firstReport), true);
                                }
                                string value = GetCellValue(period, i, firstReport);
                                if (!string.IsNullOrEmpty(value))
                                {
                                    DataGridViewCell cell = row.Cells.Cast<DataGridViewCell>().Last();
                                    cell.Value = value;
                                }
                            }
                            else
                            {
                                DataGridFactory.AddTextBoxCell(row, "-", true);
                            }
                        }
                    }
                    table.Rows.Add(row);
                }
            }
        }

        private bool isPhazeGroupingByPeriods;
        public bool IsPhazeGroupingByPeriods
        {
            get => isPhazeGroupingByPeriods;
            set
            {
                if (value == isPhazeGroupingByPeriods) return;
                isPhazeGroupingByPeriods = value;
                List<OptimazerFazeReport> reportsForChart = FillDynamicTable(_gridDynamicTable, value);
                if (value)
                {
                    UpdateTotalProfitChart(_gridDynamicTable, _chartTotalProfitDynamic, _comboBoxProfitTypeCustomPhazes, 0, reportsForChart, true);
                }
                else
                {
                    UpdateTotalProfitChart(_gridDynamicTable, _chartTotalProfitDynamic, _comboBoxProfitTypeCustomPhazes, _sortTypeDynamicTableNum, _reports, true);
                }  
            }
        }

        private void CalculateAndUpdateDynamicTable(DataGridView table)
        {
            SortDynamicResults(_reports);

            List<OptimazerFazeReport> reportsForChart = FillDynamicTable(_gridDynamicTable, IsPhazeGroupingByPeriods);
            if (IsPhazeGroupingByPeriods)
            {
                UpdateTotalProfitChart(_gridDynamicTable, _chartTotalProfitDynamic, _comboBoxProfitTypeCustomPhazes, 0, reportsForChart, true);
            }
            else
            {
                UpdateTotalProfitChart(_gridDynamicTable, _chartTotalProfitDynamic, _comboBoxProfitTypeCustomPhazes, _sortTypeDynamicTableNum, _reports, true);
            }

            _master.OnPeriodsChanged();
        }

        private decimal _strategyFRS;
        private decimal _strategyPPR;
        private decimal _strategyTR;
        private decimal _strategyGPR;


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
                    r.FRS =     r.PPR * _pprWeight +
                                r.TR * _trWeight +
                                r.GPR * _gprWeight;
                }

                List<IGrouping<decimal, OptimizerReport>> frsRankGroup = reports.SelectMany(r => r.Reports).GroupBy(r => r.FRS).ToList();
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
            var outsamples = reports.Where(r => r.Faze.TypeFaze == OptimizerFazeType.OutOfSample);
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
            if (pairs.Count == 0) return;
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
                                out decimal frs,
                                out decimal ppr,
                                out decimal tr,
                                out decimal gpr);

                    foreach (var r in allInSampleReports.Union(allOutSampleReports))
                    {
                        r.FRS = frs;
                        r.PPR = ppr;
                        r.TR = tr;
                        r.GPR = gpr;
                    }
                });

                var results = allReports.Values.Select(p => p.Keys.FirstOrDefault());

                Parallel.Invoke(() =>
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
                    List<IGrouping<decimal, OptimizerReport>> pprRankGroup = results.GroupBy(r => r.PPR).ToList();
                    pprRankGroup.Sort(new Comparison<IGrouping<decimal, OptimizerReport>>((r1, r2) => r2.First().PPR.CompareTo(r1.First().PPR)));
                    for (int i = 0; i < pprRankGroup.Count; i++)
                    {
                        foreach (OptimizerReport report in pprRankGroup[i])
                        {
                            report.PPRRank = i + 1;
                        }
                    }
                }, () =>
                {
                    List<IGrouping<decimal, OptimizerReport>> trRankGroup = results.GroupBy(r => r.TR).ToList();
                    trRankGroup.Sort(new Comparison<IGrouping<decimal, OptimizerReport>>((r1, r2) => r2.First().TR.CompareTo(r1.First().TR)));
                    for (int i = 0; i < trRankGroup.Count; i++)
                    {
                        foreach (OptimizerReport report in trRankGroup[i])
                        {
                            report.TRRank = i + 1;
                        }
                    }
                }, () =>
                {
                    List<IGrouping<decimal, OptimizerReport>> gprRankGroup = results.GroupBy(r => r.GPR).ToList();
                    gprRankGroup.Sort(new Comparison<IGrouping<decimal, OptimizerReport>>((r1, r2) => r2.First().GPR.CompareTo(r1.First().GPR)));
                    for (int i = 0; i < gprRankGroup.Count; i++)
                    {
                        foreach (OptimizerReport report in gprRankGroup[i])
                        {
                            report.GPRRank = i + 1;
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
                CalculateBots(stratDictionary, out decimal frs, out decimal ppr, out decimal tr, out decimal gpr);
                _strategyFRS = frs;
                _strategyPPR = ppr;
                _strategyTR = tr;
                _strategyGPR = gpr;
            });

            void CalculateBots(Dictionary<OptimizerReport, OptimizerReport> dicReports,
                            out decimal frs,
                            out decimal ppr,
                            out decimal tr,
                            out decimal gpr)
            {
                IEnumerable<OptimizerReport> allInSampleReports = dicReports.Keys.Select(r => r);
                IEnumerable<OptimizerReport> allOutSampleReports = dicReports.Values.Select(r => r);
                if (allInSampleReports.Count() == 0 || allOutSampleReports.Count() == 0)
                {
                    ppr = 0;
                    tr = 0;
                    gpr = 0;
                    frs = 0;
                    return;
                }
                decimal avgProfitIS = allInSampleReports.Sum(r => r.AverageProfit) / allInSampleReports.Count();
                decimal avgProfitOOS = allOutSampleReports.Sum(r => r.AverageProfit) / allOutSampleReports.Count();
                
                ppr = GetPPR(dicReports);
                tr = GetTR(allOutSampleReports);
                gpr = GetGPR(allInSampleReports, allOutSampleReports);

                frs =   _pprWeight * ppr +
                        _trWeight * tr +
                        _gprWeight * gpr;
            }

            decimal CalculateTotalProfit(OptimizerReport firstInSamples, IEnumerable<OptimizerReport> allOutOfSamples)
            {
                return firstInSamples.TotalProfit + allOutOfSamples.Sum(r => r.TotalProfit);
            }

            decimal GetMaxDrawDown(IEnumerable<OptimizerReport> allInSamples, IEnumerable<OptimizerReport> allOutOfSamples)
            {
                return Math.Max(allInSamples.Max(r => Math.Abs(r.MaxDrowDawn)), allOutOfSamples.Max(r => Math.Abs(r.MaxDrowDawn)));
            }

            decimal GetPPR(Dictionary<OptimizerReport, OptimizerReport> reportPairs)
            {
                int count = 0;
                if (reportPairs.Count == 0) return count;
                foreach (var pair in reportPairs)
                {
                    if (pair.Key.TotalProfit > 0 && pair.Value.TotalProfit > 0)
                    {
                        count++;
                    }
                }
                return ((decimal)count) / ((decimal)reportPairs.Count);
            }

            decimal GetMedian(IEnumerable<decimal> numbers)
            {
                List<decimal> list = numbers.ToList();
                list.Sort();
                if (list.Count % 2 == 1)
                {
                    int index = list.Count / 2;
                    return list[index];
                }
                else
                {
                    int index = list.Count / 2;
                    return (list[index] + list[index - 1]) / 2;
                }
            }

            decimal GetTR(IEnumerable<OptimizerReport> outOfSamples)
            {
                if (outOfSamples.Count() == 0) return 0;
                decimal median = GetMedian(outOfSamples.Select(r => r.TotalProfit));
                decimal average = outOfSamples.Select(r => r.TotalProfit).Sum() / outOfSamples.Count();
                
                return average == 0 ? 0 : 1 - Math.Abs((average - median) / average);
            }

            decimal GetGPR(IEnumerable<OptimizerReport> inSamples, IEnumerable<OptimizerReport> outOfSamples)
            {
                decimal positiveIsCount = inSamples.Where(r => r.TotalProfit > 0).Count();
                decimal positiveOsCount = outOfSamples.Where(r => r.TotalProfit > 0).Count();
                int totalCount = inSamples.Count() + outOfSamples.Count();
                if (totalCount == 0) return 0;
                return (positiveIsCount + positiveOsCount) / (totalCount);
            }
        }

        // Bot Charting
        void _gridResults_CellMouseClick(object sender, DataGridViewCellMouseEventArgs e)
        {
            if (e.RowIndex < 0)
            {
                return;
            }

            if (e.ColumnIndex == 12 && sender is DataGridView dgv)
            {
                ShowBotFullChartDialog(dgv, e);
            }
        }

        private void ShowBotFullChartDialog(DataGridView gridView, DataGridViewCellMouseEventArgs e)
        {
            if (_reports == null || _reports.Count < e.RowIndex + 1)
            {
                return;
            }

            OptimazerFazeReport fazeReport = new OptimazerFazeReport(_reports[e.RowIndex]);

            string parameterString = $"{gridView.Rows[e.RowIndex].Cells[5].Value}";

            OptimizerReport report = fazeReport.Reports.Where(r => r.GetParamsToDataTable() == parameterString).SingleOrDefault();

            if (report == null)
            {
                return;
            }

            if (FullChartButtonClickEvent != null)
            {
                FullChartButtonClickEvent(fazeReport, report);
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
            UpdateTotalProfitChart(_gridStepsOfOptimization, _chartTotalProfit, _comboBoxTotalProfitEquityType, _sortBotNumber, _reports);

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
            UpdateTotalProfitChart(_gridStepsOfOptimizationCSC, _chartTotalProfitCSC, _comboBoxTotalProfitEquityTypeCSC, _sortBotNumberCSC, _reports);

            _comboBoxTotalProfitEquityTypeCSC.SelectionChanged += _comboBoxTotalProfitEquityTypeCSC_SelectionChanged;
        }

        private Chart _chartTotalProfitDynamic;
        private System.Windows.Controls.ComboBox _comboBoxProfitTypeCustomPhazes;

        public void ActivateTotalProfitChartDynamic(WindowsFormsHost hostTotalProfit, System.Windows.Controls.ComboBox comboBoxProfitType)
        {
            _comboBoxProfitTypeCustomPhazes = comboBoxProfitType;
            _comboBoxProfitTypeCustomPhazes.Items.Add("Absolute");
            _comboBoxProfitTypeCustomPhazes.Items.Add("Persent");
            _comboBoxProfitTypeCustomPhazes.SelectedItem = "Absolute";
            _chartTotalProfitDynamic = GetTotalProfitChart();
            hostTotalProfit.Child = _chartTotalProfitDynamic;
            UpdateTotalProfitChart(_gridDynamicTable, _chartTotalProfitDynamic, _comboBoxProfitTypeCustomPhazes, _sortTypeDynamicTableNum, _reports, true);

            _comboBoxProfitTypeCustomPhazes.SelectionChanged += _comboBoxProfitTypeCustomPhazes_SelectionChanged;
        }

        private void _comboBoxProfitTypeCustomPhazes_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            UpdateTotalProfitChart(_gridDynamicTable, _chartTotalProfitDynamic, _comboBoxProfitTypeCustomPhazes, _sortTypeDynamicTableNum, _reports, true);
        }

        private void _comboBoxprofitType_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            UpdateTotalProfitChart(_gridStepsOfOptimization, _chartTotalProfit, _comboBoxTotalProfitEquityType, _sortBotNumber, _reports);
        }

        private void _comboBoxTotalProfitEquityTypeCSC_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            UpdateTotalProfitChart(_gridStepsOfOptimizationCSC, _chartTotalProfitCSC, _comboBoxTotalProfitEquityTypeCSC, _sortBotNumberCSC, _reports);
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

        private void UpdateTotalProfitChart(DataGridView gridStepsOfOptimization, Chart chartTotalProfit, System.Windows.Controls.ComboBox comboBoxTotalProfitEquityType, int sortBotNumber, List<OptimazerFazeReport> reports, bool includeInSample = false)
        {
            if (chartTotalProfit == null)
            {
                return;
            }

            if (gridStepsOfOptimization.InvokeRequired)
            {
                gridStepsOfOptimization.Invoke(new Action<DataGridView, Chart, System.Windows.Controls.ComboBox, int, List<OptimazerFazeReport>, bool>(UpdateTotalProfitChart), gridStepsOfOptimization, chartTotalProfit, comboBoxTotalProfitEquityType, sortBotNumber, reports, includeInSample);
                return;
            }

            if (reports == null)
            {
                return;
            }

            if (reports.Count <= 1)
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
                List<string> periodNames = new List<string>();
                int isCounter = 0;
                int oosCounter = 0;
                for (int i = 0; i < reports.Count; i++)
                {
                    OptimazerFazeReport curReport = reports[i];

                    if (curReport == null ||
                        curReport.Reports == null ||
                        curReport.Reports.Count == 0)
                    {
                        continue;
                    }

                    if (curReport.Faze.TypeFaze == OptimizerFazeType.InSample)
                    {
                        inSampleReport = curReport.Reports[sortBotNumber];
                        isCounter++;
                    }

                    if (curReport.Faze.TypeFaze == OptimizerFazeType.OutOfSample)
                    {
                        oosCounter++;
                    }

                    if (curReport.Faze.TypeFaze == OptimizerFazeType.OutOfSample || includeInSample)
                    {
                        if (string.IsNullOrEmpty(curReport.Faze.SpecialPeriodName))
                        {
                            periodNames.Add(curReport.Faze.TypeFaze == OptimizerFazeType.OutOfSample ? $"OOS {oosCounter}" : $"IS {isCounter}");
                        }
                        else
                        {
                            periodNames.Add(curReport.Faze.SpecialPeriodName);
                        }
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

                    max = Math.Max(close, max);
                    max = Math.Max(open, max);
                    min = Math.Min(close, min);
                    min = Math.Min(open, min);

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
                    try
                    {
                        toolTip = $"{periodNames[i]}" + "\n" +
                         "start: " + outOfSampleReports[i].Faze.TimeStart.ToString(OsLocalization.ShortDateFormatString) + "\n" +
                         "end: " + outOfSampleReports[i].Faze.TimeEnd.ToString(OsLocalization.ShortDateFormatString) + "\n" +
                         "profit: " + profit[i].ToStringWithNoEndZero();
                    }
                    catch { }
                    
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

        private void UpdateAverageProfitChart(Chart chartAverageProfit, List<OptimazerFazeReport> reports, List<string> periodNames = null)
        {
            if (_reports == null ||
                _reports.Count == 0)
            {
                return;
            }

            if (chartAverageProfit.InvokeRequired)
            {
                chartAverageProfit.Invoke(new Action<Chart, List<OptimazerFazeReport>, List<string>>(UpdateAverageProfitChart), chartAverageProfit, reports, periodNames);
                return;
            }

            try
            {
                List<decimal> values = new List<decimal>();
                decimal maxValue = 0;

                decimal averageProfitPercent = 0;

                for (int i = 0; i < reports.Count; i++)
                {
                    OptimizerReport bot = reports[i].Reports.First();

                    decimal value = bot.AverageProfitPercentOneContract;

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

                    averageProfitPercent += bot.AverageProfitPercentOneContract;
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
                    string periodName = (periodNames != null && i < periodNames.Count) ? periodNames[i] : string.Empty;
                    toolTip = "OOS " + (i + 1) + $"{(string.IsNullOrEmpty(periodName) ? "" : $"({periodName})")}" + "\n" +
                        "start: " + reports[i].Faze.TimeStart.ToString(OsLocalization.ShortDateFormatString) + "\n" +
                         "end: " + reports[i].Faze.TimeEnd.ToString(OsLocalization.ShortDateFormatString) + "\n" +
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

        System.Windows.Controls.ComboBox _comboBoxSortResultsDynamicTable;
        System.Windows.Controls.ComboBox _comboBoxSortResultsDynamicTableNum;
        private SortBotsType _sortTypeDynamicTable = SortBotsType.TotalProfit;
        private int _sortTypeDynamicTableNum = 0;

        internal void ActivateDynamicTableComboboxSort(System.Windows.Controls.ComboBox comboBoxSortResultsType2, System.Windows.Controls.ComboBox comboBoxSortResultsBotNumPercent2)
        {
            _comboBoxSortResultsDynamicTable = comboBoxSortResultsType2;

            _comboBoxSortResultsDynamicTable.Items.Add(SortBotsType.TotalProfit.ToString());
            _comboBoxSortResultsDynamicTable.Items.Add(SortBotsType.MaxDrowDawn.ToString());
            _comboBoxSortResultsDynamicTable.Items.Add(SortBotsType.AverageProfit.ToString());
            _comboBoxSortResultsDynamicTable.Items.Add(SortBotsType.RRProfit.ToString());
            _comboBoxSortResultsDynamicTable.Items.Add(SortBotsType.WinRate.ToString());
            _comboBoxSortResultsDynamicTable.Items.Add(SortBotsType.OOSProfit.ToString());
            _comboBoxSortResultsDynamicTable.Items.Add(SortBotsType.Eval.ToString());

            _comboBoxSortResultsDynamicTable.SelectedItem = SortBotsType.TotalProfit.ToString();
            _comboBoxSortResultsDynamicTable.SelectionChanged += _comboBoxSortResultsDynamicTable_SelectionChanged;

            _comboBoxSortResultsDynamicTableNum = comboBoxSortResultsBotNumPercent2;
            for (int i = 0; i < 99; i++)
            {
                _comboBoxSortResultsDynamicTableNum.Items.Add(i.ToString());
            }

            _comboBoxSortResultsDynamicTableNum.SelectedItem = "0";
            _comboBoxSortResultsDynamicTableNum.SelectionChanged += _comboBoxSortResultsDynamicTableNum_SelectionChanged;
        }

        private void _comboBoxSortResultsDynamicTable_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            try
            {
                if (Enum.TryParse(_comboBoxSortResultsDynamicTable.SelectedItem.ToString(), out SortBotsType sortType))
                {
                    _sortTypeDynamicTable = sortType;
                }

                if (_reports != null)
                {
                    CalculateAndUpdateDynamicTable(_gridDynamicTable);
                }
            }
            catch
            {

            }
        }

        private void _comboBoxSortResultsDynamicTableNum_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            try
            {
                _sortTypeDynamicTableNum = Convert.ToInt32(_comboBoxSortResultsDynamicTableNum.SelectedItem.ToString());
                _sortTypeDynamicTableNum = Math.Min(_reports.First().Reports.Count - 1, _sortTypeDynamicTableNum);
                if (_reports != null)
                {
                    CalculateAndUpdateDynamicTable(_gridDynamicTable);
                }
            }
            catch
            {

            }
        }

        internal void PaintDynamicTable(List<OptimazerFazeReport> reports)
        {
            _reports = reports;
            if (_reports == null) return;

            try
            {
                CalculateAndUpdateDynamicTable(_gridDynamicTable);
            }
            catch (Exception ex) 
            {
                SendLogMessage(ex.ToString(), LogMessageType.Error);
            }
        }

        /// <summary>
        /// event: new message for log
        /// событие: новое сообщение для лога
        /// </summary>
        public event Action<string, LogMessageType> LogMessageEvent;

        public event Action<OptimazerFazeReport, OptimizerReport> FullChartButtonClickEvent;
        public event Action<OptimazerFazeReport, OptimizerReport> ChartButtonClickEvent;

    }
}
