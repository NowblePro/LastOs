using OsEngine.Charts.CandleChart;
using OsEngine.Entity;
using OsEngine.Logging;
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

        private int obCount = 2;

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

            if (area != null)
            {
                Axis xAxis = area.AxisX;
                Axis yAxis = area.AxisY;

                //ClearOrderBlocks(HighOrderBlocks, true, true);

                //int skipHigh = HighOrderBlocks.Count - obCount;

                //IEnumerable<OrderBlock> delete = HighOrderBlocks.Take(skipHigh);
                //foreach (OrderBlock ob in delete)
                //{
                //    DeleteOrderBlock(HighOrderBlocks, ob);
                //}
                foreach (var ob in HighOrderBlocks)
                {
                    int skip = _candles.Count - ob.Length - 1;
                    if (skip < 0) continue;
                    int period = _candles.Count - skip;
                    Series seriesHighUp = ob.SeriesUp;
                    seriesHighUp.Points.Clear();
                    var highUp = Enumerable.Repeat(double.NaN, skip).Concat(Enumerable.Repeat((double)ob.Top, period)).ToArray();
                    foreach (double d in highUp)
                    {
                        seriesHighUp.Points.AddY(d);
                    }

                    Series seriesHighDown = ob.SeriesDown;
                    seriesHighDown.Points.Clear();
                    var highDown = Enumerable.Repeat(double.NaN, skip).Concat(Enumerable.Repeat((double)ob.Bottom, period)).ToArray();
                    foreach (double d in highDown)
                    {
                        seriesHighDown.Points.AddY(d);
                    }
                }
                //int skipLow = LowOrderBlocks.Count - obCount;
                //delete = LowOrderBlocks.Take(skipLow);
                //foreach (OrderBlock ob in delete)
                //{
                //    DeleteOrderBlock(LowOrderBlocks, ob);
                //}
                foreach (var ob in LowOrderBlocks)
                {
                    int skip = _candles.Count - ob.Length - 1;
                    if (skip < 0) continue;
                    int period = _candles.Count - skip;
                    Series seriesLowUp = ob.SeriesUp;
                    seriesLowUp.Points.Clear();
                    var lowUp = Enumerable.Repeat(double.NaN, skip).Concat(Enumerable.Repeat((double)ob.Top, period)).ToArray();
                    foreach (double d in lowUp)
                    {
                        seriesLowUp.Points.AddY(d);
                    }

                    Series seriesLowDown = ob.SeriesDown;
                    seriesLowDown.Points.Clear();
                    var lowDown = Enumerable.Repeat(double.NaN, skip).Concat(Enumerable.Repeat((double)ob.Bottom, period)).ToArray();
                    foreach (double d in lowDown)
                    {
                        seriesLowDown.Points.AddY(d);
                    }
                }

                area.AxisY.Minimum = area.AxisY2.Minimum;
                area.AxisY.Maximum = area.AxisY2.Maximum;
            }
        }

        private string GetFreeSeriesName(Chart chart)
        {
            int index = 0;
            string name;
            do
            {
                name = $"series{index}";
                index++;
            }
            while (chart.Series.Any(s => s.Name == name));
            return name;
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
                ClearOrderBlocks(HighOrderBlocks, false, false);
                ClearOrderBlocks(LowOrderBlocks, false, false);
                return;
            }
            int skip = _zzSeries.Values.Count - _period.ValueInt;
            IEnumerable<decimal> highs = _zz.DataSeries[2].Values.Skip(skip).Where(v => v > 0);
            IEnumerable<decimal> lows = _zz.DataSeries[3].Values.Skip(skip).Where(v => v > 0);
            Enum.TryParse(_priceBasis.ValueString, out OrderBlockPriceBasis basis);
            //ClearOrderBlocks(HighOrderBlocks, false, false);
            List<OrderBlock> newHighOrderBlocks = new List<OrderBlock>();
            List<OrderBlock> newLowOrderBlocks = new List<OrderBlock>();
            foreach (decimal high in highs)
            {
                OrderBlock ob = new OrderBlock(high, _zz.DataSeries[2].Values, candles, OrderBlockType.Bullish, basis);
                newHighOrderBlocks.Add(ob);
            }

            //ClearOrderBlocks(LowOrderBlocks, false, false);
            foreach (decimal low in lows)
            {
                OrderBlock ob = new OrderBlock(low, _zz.DataSeries[3].Values, candles, OrderBlockType.Bearish, basis);
                newLowOrderBlocks.Add(ob);
            }

            UpdateOrderBlocks(HighOrderBlocks, newHighOrderBlocks);
            UpdateOrderBlocks(LowOrderBlocks, newLowOrderBlocks);
        }

        private void UpdateOrderBlocks(List<OrderBlock> collection, List<OrderBlock> newCollection)
        {
            Chart chart = _chartMaster.ChartCandle?.GetChart();
            if (chart == null) return;

            if (chart.InvokeRequired)
            {
                chart.Invoke((Action<List<OrderBlock>, List<OrderBlock>>)UpdateOrderBlocks, collection, newCollection);
                return;
            }
            ChartArea area = chart?.ChartAreas?.Where(a => a.Name == "Prime").SingleOrDefault();
            var newObs = newCollection.Where(ob => !collection.Any(old => old.Bottom == ob.Bottom && old.Top == ob.Top)).ToList();
            var updating = collection.Where(old => newCollection.Any(ob => old.Bottom == ob.Bottom && old.Top == ob.Top)).ToList();
            var deleting = collection.Except(updating).ToList();
            foreach (OrderBlock ob in deleting)
            {
                DeleteOrderBlock(collection, ob);
            }

            foreach (OrderBlock old in updating)
            {
                OrderBlock newOb = newCollection.Where(ob => ob.Top == old.Top && ob.Bottom == old.Bottom).FirstOrDefault();
                if (newOb != null)
                {
                    old.Length = newOb.Length;
                    old.BrokenIndex = newOb.BrokenIndex;
                }
            }

            foreach (OrderBlock ob in newObs)
            {
                if (ob.Type == OrderBlockType.Bullish)
                {
                    ob.SeriesUp = GetHighSeries(GetFreeSeriesName(chart));
                    ob.SeriesUp.ChartArea = area.Name;
                    chart.Series.Add(ob.SeriesUp);
                    ob.SeriesDown = GetHighSeries(GetFreeSeriesName(chart));
                    ob.SeriesDown.ChartArea = area.Name;
                    chart.Series.Add(ob.SeriesDown);
                }
                else if (ob.Type == OrderBlockType.Bearish)
                {
                    ob.SeriesUp = GetLowSeries(GetFreeSeriesName(chart));
                    ob.SeriesUp.ChartArea = area.Name;
                    chart.Series.Add(ob.SeriesUp);
                    ob.SeriesDown = GetLowSeries(GetFreeSeriesName(chart));
                    ob.SeriesDown.ChartArea = area.Name;
                    chart.Series.Add(ob.SeriesDown);
                }
                collection.Add(ob);
            }
        }

        private void ClearOrderBlocks(List<OrderBlock> obs, bool exceptInPosition, bool onlySeries)
        {
            Chart chart = _chartMaster.ChartCandle?.GetChart();
            if (chart == null) return;
            if (chart.InvokeRequired)
            {
                chart.Invoke((Action<List<OrderBlock>, bool, bool>)ClearOrderBlocks, obs, exceptInPosition, onlySeries);
                return;
            }
            IEnumerable<OrderBlock> deleting = obs.ToList();
            if (exceptInPosition)
            {
                deleting = deleting.Where(ob => !ob.IsInPosition).ToList();
            }
            foreach (OrderBlock ob in deleting)
            {
                DeleteOrderBlock(obs, ob);
                if (!onlySeries)
                {
                    obs.Remove(ob);
                }
            }
        }

        private void DeleteOrderBlock(List<OrderBlock> collection, OrderBlock ob)
        {
            Chart chart = _chartMaster.ChartCandle?.GetChart();
            if (chart == null) return;
            if (chart.InvokeRequired)
            {
                chart.Invoke((Action<List<OrderBlock>, OrderBlock>)DeleteOrderBlock, collection, ob);
                return;
            }
            ob.SeriesUp?.Points.Clear();
            chart?.Series.Remove(ob.SeriesUp);
            ob.SeriesUp = null;
            ob.SeriesDown?.Points.Clear();
            chart?.Series.Remove(ob.SeriesDown);
            ob.SeriesDown = null;
            collection.Remove(ob);
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
                    double xPixel2 = ob.IsBroken ? xAxis.ValueToPixelPosition(ob.BrokenIndex) : xAxis.ValueToPixelPosition(skip + period - 1);
                    double yPixel2 = yAxis.ValueToPixelPosition(highDown[skip]);

                    using (Brush brush = new SolidBrush(Color.FromArgb(5, Color.Red)))
                    {
                        g.FillRectangle(brush, new RectangleF((float)xPixel1, (float)yPixel1, (float)(xPixel2 - xPixel1), (float)(yPixel2 - yPixel1)));
                    }

                    if (ob.IsBroken)
                    {
                        double xPixel3 = xAxis.ValueToPixelPosition(skip + period - 1);
                        using (Brush brush = new SolidBrush(Color.FromArgb(5, Color.Green)))
                        {
                            g.FillRectangle(brush, new RectangleF((float)xPixel2, (float)yPixel1, (float)(xPixel3 - xPixel2), (float)(yPixel2 - yPixel1)));
                        }
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
                    double xPixel2 = ob.IsBroken ? xAxis.ValueToPixelPosition(ob.BrokenIndex) : xAxis.ValueToPixelPosition(skip + period - 1);
                    double yPixel2 = yAxis.ValueToPixelPosition(highDown[skip]);

                    using (Brush brush = new SolidBrush(Color.FromArgb(5, Color.Blue)))
                    {
                        g.FillRectangle(brush, new RectangleF((float)xPixel1, (float)yPixel1, (float)(xPixel2 - xPixel1), (float)(yPixel2 - yPixel1)));
                    }

                    if (ob.IsBroken)
                    {
                        double xPixel3 = xAxis.ValueToPixelPosition(skip + period - 1);
                        using (Brush brush = new SolidBrush(Color.FromArgb(5, Color.Green)))
                        {
                            g.FillRectangle(brush, new RectangleF((float)xPixel2, (float)yPixel1, (float)(xPixel3 - xPixel2), (float)(yPixel2 - yPixel1)));
                        }
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
        public OrderBlock(decimal value, List<decimal> zz, List<Candle> candles, OrderBlockType type, OrderBlockPriceBasis basis)
        {
            Type = type;
            int i = zz.LastIndexOf(value);
            Candle candle = candles[i];
            switch (basis)
            {
                case OrderBlockPriceBasis.Body:
                    Top = Math.Max(candle.Open, candle.Close);
                    Bottom = Math.Min(candle.Open, candle.Close); ;
                    break;
                case OrderBlockPriceBasis.Full:
                    Top = candle.High;
                    Bottom = candle.Low;
                    break;
            }
            Length = zz.Count - i;
            CheckBroken(candles);
        }

        public Series SeriesUp { get; set; }
        public Series SeriesDown { get; set; }

        public decimal Top { get; set; }
        public decimal Bottom { get; set; }
        public int Length { get; set; }
        public int BrokenIndex { get; set; } = -1;

        public OrderBlockType Type { get; private set; }

        public bool IsBroken => BrokenIndex > -1;

        public bool IsInPosition { get; set; } = false;

        public void CheckBroken(List<Candle> candles)
        {
            int skip = candles.Count - Length;
            List<Candle> periodCandles = candles.Skip(skip).ToList();
            switch (Type)
            {
                case OrderBlockType.Bullish:
                    BrokenIndex = periodCandles.FindIndex(c => c.Close > Top) + skip;
                    break;
                case OrderBlockType.Bearish:
                    BrokenIndex = periodCandles.FindIndex(c => c.Close < Bottom) + skip;
                    break;
            }
            if (BrokenIndex < candles.Count - Length)
            {
                BrokenIndex = -1;
            }
        }
    }

    public enum OrderBlockType { Bullish, Bearish }

    public enum OrderBlockPriceBasis { Body, Full }
}
