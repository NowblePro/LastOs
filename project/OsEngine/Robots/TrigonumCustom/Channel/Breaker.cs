using OsEngine.Charts.CandleChart;
using OsEngine.Entity;

using OsEngine.Indicators;
using OsEngine.Indicators.TrigonumCustom;
using OsEngine.OsTrader.Panels;
using OsEngine.OsTrader.Panels.Attributes;
using OsEngine.OsTrader.Panels.Tab;
using OsEngine.Robots.TrigonumCustom.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms.DataVisualization.Charting;

namespace OsEngine.Robots.TrigonumCustom.Channel
{
    [Bot("Breaker")]
    public class Breaker : BotPanelSimple
    {
        #region Parameters
        private StrategyParameterDecimal _maxHigh;
        private StrategyParameterDecimal _minHigh;
        private StrategyParameterDecimal _margin;
        private StrategyParameterInt _rr;
        private StrategyParameterBool _useBody;
        private StrategyParameterInt _period;
        #endregion

        private OrderBlockZigZag _ob;
        private StrategyParameterInt _lengthZZ;
        private ChartCandleMaster _chartMaster;

        public Breaker(string name, StartProgram startProgram) : base(name, startProgram)
        {
            #region Breaker parameters
            _maxHigh = CreateParameter("Max High", 1.0m, 0.6m, 1.5m, 0.05m, "Breaker");
            _minHigh = CreateParameter("Min High", 0.2m, 0.1m, 0.4m, 0.05m, "Breaker");
            _margin = CreateParameter("Margin", 0.2m, 0.1m, 0.4m, 0.05m, "Breaker");
            _rr = CreateParameter("RR", 2, 1, 3, 1, "Breaker");
            _useBody = CreateParameter("Use Body", false, "Breaker");
            _period = CreateParameter("Period", 14, 5, 100, 1, "Breaker");
            #endregion

            _lengthZZ = CreateParameter("Length ZZ", 50, 50, 200, 20, "Breaker");

            _ob = (OrderBlockZigZag)IndicatorsFactory.CreateIndicatorByName(nameClass: "OrderBlockZigZag", name: name + "OrderBlockZigZag", canDelete: false);
            _ob = (OrderBlockZigZag)_tab.CreateCandleIndicator(_ob, nameArea: "Prime");

            _chartMaster = _tab.GetChartMaster();
            
            _lengthZZ.ValueChange += _lengthZZ_ValueChange;
            _ob.Save();
        }

        private void _lengthZZ_ValueChange()
        {
            _ob.Period.ValueInt = _lengthZZ.ValueInt;
        }

        List<Series> highUpSeries = new List<Series>();
        List<Series> highDownSeries = new List<Series>();

        private void DrawOrderBlocks(List<Candle> candles)
        {
            Chart chart = _chartMaster.ChartCandle?.GetChart();
            if (chart == null) return;

            if (chart.InvokeRequired)
            {
                chart.Invoke((Action<List<Candle>>)DrawOrderBlocks, candles);
                return;
            }

            ChartArea area = chart?.ChartAreas?.Where(a => a.Name == "Prime").SingleOrDefault();
            int indexHigh = 0;
            if (area != null)
            {
                foreach (Series series in highUpSeries)
                {
                    series.Points.Clear();
                    chart.Series.Remove(series);
                }

                highUpSeries.Clear();

                foreach (Series series in highDownSeries)
                {
                    series.Points.Clear();
                    chart.Series.Remove(series);
                }

                highDownSeries.Clear();

                foreach (var ob in _ob.HighOrderBlocks)
                {
                    int skip = candles.Count - ob.Length;
                    int period = candles.Count - skip;
                    Series seriesHighUp = GetHighUpSeries($"highUp{indexHigh}");
                    Series seriesHighDown = new Series($"highDown{indexHigh}");
                    var values = Enumerable.Repeat(double.NaN, skip).Concat(Enumerable.Repeat((double)ob.Top, period)).ToArray();
                    foreach (double d in values)
                    {
                        seriesHighUp.Points.AddY(d);
                    }

                    seriesHighUp.ChartArea = area.Name;
                    chart.Series.Add(seriesHighUp);
                    highUpSeries.Add(seriesHighUp);
                    indexHigh++;
                }

                area.AxisY.Minimum = area.AxisY2.Minimum;
                area.AxisY.Maximum = area.AxisY2.Maximum;
            }
        }

        private Series GetHighUpSeries(string name)
        {
            Series result = new Series(name);
            result.ChartType = SeriesChartType.Line;
            result.Color = System.Drawing.Color.Red;
            result.BorderWidth = 2;
            result.BorderDashStyle = ChartDashStyle.Solid;
            return result;
        }

        protected override void ParametersChangedByUser()
        {
            if (_ob.ParametersDigit[0].Value != _lengthZZ.ValueInt)
            {
                _ob.ParametersDigit[0].Value = _lengthZZ.ValueInt;
                _ob.Reload();
                _ob.Save();
            }
        }

        protected override void CandleFinishedEvent(List<Candle> candles)
        {
            if (candles.Count < _lengthZZ.ValueInt) return;
            DrawOrderBlocks(candles);

        }
        public override void ShowIndividualSettingsDialog() { }
    }
}
