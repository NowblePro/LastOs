using OsEngine.Charts.CandleChart;
using OsEngine.Entity;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms.DataVisualization.Charting;

namespace OsEngine.Indicators.TrigonumCustom
{
    [Indicator("OrderBlockZigZag")]
    public class OrderBlockZigZag : Aindicator
    {
        private Aindicator _zz;
        private IndicatorDataSeries _zzSeries;
        private List<Candle> _candles;
        /// <summary>
        /// Период, в течении которого регистрируются ордер блоки
        /// </summary>
        private IndicatorParameterInt _period;
        private IndicatorParameterString _priceBasis;

        public IndicatorParameterInt Period => _period;

        public List<OrderBlock> HighOrderBlocks { get; private set; } = new List<OrderBlock>();
        public List<OrderBlock> LowOrderBlocks { get; private set; } = new List<OrderBlock>();

        private List<Series> highUpSeries = new List<Series>();
        private List<Series> highDownSeries = new List<Series>();
        private List<Series> lowUpSeries = new List<Series>();
        private List<Series> lowDownSeries = new List<Series>();


        private ChartCandleMaster _chartMaster;
        public ChartCandleMaster ChartMaster
        {
            get => _chartMaster;
            set
            {
                _chartMaster = value;
                if (_chartMaster.ChartCandle == null)
                {
                    _chartMaster.ChartCandleCreated += _chartMaster_ChartCandleCreated;
                }
                else
                {
                    BindChart();
                }
            }
        }

        public override void OnProcess(List<Candle> candles, int index)
        {
            //int index = _series.Values.Count - 1;
            if (index == 0) return;
            int delta = 1;
            int i = index - delta;
            while (i >= 0 && _zzSeries.Values[i] != _zz.DataSeries[1].Values[i])
            {
                _zzSeries.Values[i] = _zz.DataSeries[1].Values[i];
                delta++;
                i = index - delta;
            }
            UpdateOrderBlocks(candles);
            DrawOrderBlocks(candles);
        }

        private void DrawOrderBlocks(List<Candle> candles)
        {
            this._candles = candles;

            Chart chart = _chartMaster.ChartCandle?.GetChart();
            if (chart == null) return;

            if (chart.InvokeRequired)
            {
                chart.Invoke((Action<List<Candle>>)DrawOrderBlocks, candles);
                return;
            }

            if (_candles == null || _candles.Count == 0) return;

            ChartArea area = chart?.ChartAreas?.Where(a => a.Name == "Prime").SingleOrDefault();
            int indexHigh = 0;
            int indexLow = 0;
            if (area != null)
            {
                Axis xAxis = area.AxisX;
                Axis yAxis = area.AxisY;

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

                foreach (var ob in HighOrderBlocks)
                {
                    int skip = _candles.Count - ob.Length - 1;
                    if (skip < 0) continue;
                    int period = _candles.Count - skip;
                    Series seriesHighUp = GetHighSeries($"highUp{indexHigh}");
                    var highUp = Enumerable.Repeat(double.NaN, skip).Concat(Enumerable.Repeat((double)ob.Top, period)).ToArray();
                    foreach (double d in highUp)
                    {
                        seriesHighUp.Points.AddY(d);
                    }

                    seriesHighUp.ChartArea = area.Name;
                    chart.Series.Add(seriesHighUp);
                    highUpSeries.Add(seriesHighUp);


                    Series seriesHighDown = GetHighSeries($"highDown{indexHigh}");
                    var highDown = Enumerable.Repeat(double.NaN, skip).Concat(Enumerable.Repeat((double)ob.Bottom, period)).ToArray();
                    foreach (double d in highDown)
                    {
                        seriesHighDown.Points.AddY(d);
                    }

                    seriesHighDown.ChartArea = area.Name;
                    chart.Series.Add(seriesHighDown);
                    highDownSeries.Add(seriesHighDown);

                    indexHigh++;
                }

                foreach (Series series in lowUpSeries)
                {
                    series.Points.Clear();
                    chart.Series.Remove(series);
                }

                lowUpSeries.Clear();

                foreach (Series series in lowDownSeries)
                {
                    series.Points.Clear();
                    chart.Series.Remove(series);
                }

                lowDownSeries.Clear();

                foreach (var ob in LowOrderBlocks)
                {
                    int skip = _candles.Count - ob.Length - 1;
                    if (skip < 0) continue;
                    int period = _candles.Count - skip;
                    Series seriesLowUp = GetLowSeries($"lowUp{indexLow}");
                    var lowUp = Enumerable.Repeat(double.NaN, skip).Concat(Enumerable.Repeat((double)ob.Top, period)).ToArray();
                    foreach (double d in lowUp)
                    {
                        seriesLowUp.Points.AddY(d);
                    }

                    seriesLowUp.ChartArea = area.Name;
                    chart.Series.Add(seriesLowUp);
                    lowUpSeries.Add(seriesLowUp);


                    Series seriesLowDown = GetLowSeries($"lowDown{indexLow}");
                    var lowDown = Enumerable.Repeat(double.NaN, skip).Concat(Enumerable.Repeat((double)ob.Bottom, period)).ToArray();
                    foreach (double d in lowDown)
                    {
                        seriesLowDown.Points.AddY(d);
                    }

                    seriesLowDown.ChartArea = area.Name;
                    chart.Series.Add(seriesLowDown);
                    lowDownSeries.Add(seriesLowDown);

                    indexLow++;
                }

                area.AxisY.Minimum = area.AxisY2.Minimum;
                area.AxisY.Maximum = area.AxisY2.Maximum;
            }
        }

        private Series GetHighSeries(string name)
        {
            Series result = new Series(name);
            result.ChartType = SeriesChartType.Line;
            result.Color = System.Drawing.Color.Red;
            result.BorderWidth = 1;
            result.BorderDashStyle = ChartDashStyle.Solid;
            return result;
        }

        private Series GetLowSeries(string name)
        {
            Series result = new Series(name);
            result.ChartType = SeriesChartType.Line;
            result.Color = System.Drawing.Color.Blue;
            result.BorderWidth = 1;
            result.BorderDashStyle = ChartDashStyle.Solid;
            return result;
        }

        private void _chartMaster_ChartCandleCreated(object sender, EventArgs e)
        {
            BindChart();
        }

        private void UpdateOrderBlocks(List<Candle> candles)
        {
            if (_zzSeries.Values.Count < _period.ValueInt)
            {
                HighOrderBlocks.Clear();
                LowOrderBlocks.Clear();
                return;
            }
            //high
            int skip = _zzSeries.Values.Count - _period.ValueInt;
            IEnumerable<decimal> highs = _zz.DataSeries[2].Values.Skip(skip).Where(v => v > 0);
            //lows
            IEnumerable<decimal> lows = _zz.DataSeries[3].Values.Skip(skip).Where(v => v > 0);

            HighOrderBlocks.Clear();
            foreach (decimal high in highs)
            {
                int i = _zz.DataSeries[2].Values.LastIndexOf(high);
                Candle candle = candles[i];
                decimal top = high;
                Enum.TryParse(_priceBasis.ValueString, out OrderBlockPriceBasis basis);
                decimal bottom = top - top * 0.1m;
                switch (basis)
                {
                    case OrderBlockPriceBasis.Body:
                        top = Math.Max(candle.Open, candle.Close);
                        bottom = Math.Min(candle.Open, candle.Close); ;
                        break;
                    case OrderBlockPriceBasis.Full:
                        top = candle.High;
                        bottom = candle.Low;
                        break;
                }
                HighOrderBlocks.Add(new OrderBlock() { Top = top, Bottom = bottom, Length = _zz.DataSeries[2].Values.Count - i });
            }

            LowOrderBlocks.Clear();
            foreach (decimal low in lows)
            {
                int i = _zz.DataSeries[3].Values.LastIndexOf(low);
                Candle candle = candles[i];
                Enum.TryParse(_priceBasis.ValueString, out OrderBlockPriceBasis basis);
                decimal bottom = low;
                decimal top = bottom + bottom * 0.1m;
                switch (basis)
                {
                    case OrderBlockPriceBasis.Body:
                        top = Math.Max(candle.Open, candle.Close);
                        bottom = Math.Min(candle.Open, candle.Close); ;
                        break;
                    case OrderBlockPriceBasis.Full:
                        top = candle.High;
                        bottom = candle.Low;
                        break;
                }
                LowOrderBlocks.Add(new OrderBlock() { Top = top, Bottom = bottom, Length = _zz.DataSeries[3].Values.Count - i });
            }
        }

        private void Chart_PostPaint(object sender, ChartPaintEventArgs e)
        {
            Graphics g = e.ChartGraphics.Graphics;

            if (_candles == null || _candles.Count == 0) return;
            Chart chart = e.Chart;
            ChartArea area = chart?.ChartAreas?.Where(a => a.Name == "Prime").SingleOrDefault();

            if (area != null)
            {
                Axis xAxis = area.AxisX;
                Axis yAxis = area.AxisY;

                foreach (var ob in HighOrderBlocks)
                {
                    int skip = _candles.Count - ob.Length;
                    if (skip < 0) continue;
                    int period = _candles.Count - skip;
                    var highUp = Enumerable.Repeat(double.NaN, skip).Concat(Enumerable.Repeat((double)ob.Top, period)).ToArray();
                    var highDown = Enumerable.Repeat(double.NaN, skip).Concat(Enumerable.Repeat((double)ob.Bottom, period)).ToArray();

                    double xPixel1 = xAxis.ValueToPixelPosition(skip);
                    double yPixel1 = yAxis.ValueToPixelPosition(highUp[skip]);
                    double xPixel2 = xAxis.ValueToPixelPosition(skip + period - 1);
                    double yPixel2 = yAxis.ValueToPixelPosition(highDown[skip]);

                    using (Brush brush = new SolidBrush(Color.FromArgb(5, Color.Red)))
                    {
                        g.FillRectangle(brush, new RectangleF((float)xPixel1, (float)yPixel1, (float)(xPixel2 - xPixel1), (float)(yPixel2 - yPixel1)));
                    }
                }

                foreach (var ob in LowOrderBlocks)
                {
                    int skip = _candles.Count - ob.Length;
                    if (skip < 0) continue;
                    int period = _candles.Count - skip;
                    var highUp = Enumerable.Repeat(double.NaN, skip).Concat(Enumerable.Repeat((double)ob.Top, period)).ToArray();
                    var highDown = Enumerable.Repeat(double.NaN, skip).Concat(Enumerable.Repeat((double)ob.Bottom, period)).ToArray();

                    double xPixel1 = xAxis.ValueToPixelPosition(skip);
                    double yPixel1 = yAxis.ValueToPixelPosition(highUp[skip]);
                    double xPixel2 = xAxis.ValueToPixelPosition(skip + period - 1);
                    double yPixel2 = yAxis.ValueToPixelPosition(highDown[skip]);

                    using (Brush brush = new SolidBrush(Color.FromArgb(5, Color.Blue)))
                    {
                        g.FillRectangle(brush, new RectangleF((float)xPixel1, (float)yPixel1, (float)(xPixel2 - xPixel1), (float)(yPixel2 - yPixel1)));
                    }
                }

                area.AxisY.Minimum = area.AxisY2.Minimum;
                area.AxisY.Maximum = area.AxisY2.Maximum;
            }
        }

        private void ChartCandle_ChartCreated(object sender, Chart e)
        {
            e.PostPaint += Chart_PostPaint;
        }

        private void ChartCandle_ChartDeleting(object sender, Chart e)
        {
            e.PostPaint += Chart_PostPaint;
        }

        public override void OnStateChange(IndicatorState state)
        {
            if (state == IndicatorState.Configure)
            {
                _zzSeries = CreateSeries("ZZ", Color.DarkGreen, IndicatorChartPaintType.Line, false);
                _zz = IndicatorsFactory.CreateIndicatorByName("ZigZag", Name + "ZigZag", false);
                _period = CreateParameterInt("Period", 30);
                _priceBasis = CreateParameterString("PriceBasis", OrderBlockPriceBasis.Full.ToString());
                _priceBasis.ValuesString.AddRange(Enum.GetNames(typeof(OrderBlockPriceBasis)).Except(_priceBasis.ValuesString));

                ProcessIndicator("ZigZag", _zz);
                TypeIndicator = IndicatorChartPaintType.Line;
            }
        }

        private void BindChart()
        {
            Chart chart = _chartMaster.ChartCandle.GetChart();
            _chartMaster.ChartCandle.ChartCreated += ChartCandle_ChartCreated;
            _chartMaster.ChartCandle.ChartDeleting += ChartCandle_ChartDeleting;
            if (chart != null)
            {
                chart.PostPaint += Chart_PostPaint;
            }
        }
    }

    public class OrderBlock
    {
        public decimal Top { get; set; }
        public decimal Bottom { get; set; }
        public int Length { get; set; }
    }

    enum OrderBlockPriceBasis { Body, Full }
}
