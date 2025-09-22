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
            if (index == 0) return;
            int delta = 1;
            int i = index - delta;
            while (i >= 0 && _zzSeries.Values[i] != _zz.DataSeries[1].Values[i])
            {
                _zzSeries.Values[i] = _zz.DataSeries[1].Values[i];
                delta++;
                i = index - delta;
            }
            _candles = candles;
            UpdateOrderBlocks(candles);
        }

        private void _chartMaster_ChartCandleCreated(object sender, EventArgs e)
        {
            BindChart();
        }

        private void UpdateOrderBlocks(List<Candle> candles)
        {
            if (_zzSeries.Values.Count < _period.ValueInt)
            {
                ClearOrderBlocks(HighOrderBlocks, false);
                ClearOrderBlocks(LowOrderBlocks, false);
                return;
            }
            int skip = _zzSeries.Values.Count - _period.ValueInt;
            IEnumerable<decimal> highs = _zz.DataSeries[2].Values.Skip(skip).Where(v => v > 0);
            IEnumerable<decimal> lows = _zz.DataSeries[3].Values.Skip(skip).Where(v => v > 0);
            Enum.TryParse(_priceBasis.ValueString, out OrderBlockPriceBasis basis);
            List<OrderBlock> newHighOrderBlocks = new List<OrderBlock>();
            List<OrderBlock> newLowOrderBlocks = new List<OrderBlock>();
            foreach (decimal high in highs)
            {
                OrderBlock ob = new OrderBlock(high, _zz.DataSeries[2].Values, candles, OrderBlockType.Bullish, basis);
                newHighOrderBlocks.Add(ob);
            }

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

            if (chart != null && chart.InvokeRequired)
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

            collection.AddRange(newObs);
        }

        private void ClearOrderBlocks(List<OrderBlock> obs, bool onlySeries)
        {
            Chart chart = _chartMaster.ChartCandle?.GetChart();
            if (chart != null && chart.InvokeRequired)
            {
                chart.Invoke((Action<List<OrderBlock>, bool>)ClearOrderBlocks, obs, onlySeries);
                return;
            }
            IEnumerable<OrderBlock> deleting = obs.ToList();

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

            if (chart != null && chart.InvokeRequired)
            {
                chart.Invoke((Action<List<OrderBlock>, OrderBlock>)DeleteOrderBlock, collection, ob);
                return;
            }
            
            collection.Remove(ob);
        }

        private void Chart_PostPaint(object sender, ChartPaintEventArgs e)
        {
            try
            {
                Graphics g = e.ChartGraphics.Graphics;

                if (_candles == null || _candles.Count == 0) return;
                Chart chart = e.Chart;
                ChartArea area = chart?.ChartAreas?.Where(a => a.Name == "Prime").SingleOrDefault();

                double xMax = (area.InnerPlotPosition.Width / 100 * chart.ClientRectangle.Width) - (area.Position.X / 100 * chart.ClientRectangle.Width);
                double yMax = (area.InnerPlotPosition.Height / 100 * chart.ClientRectangle.Height) - (area.Position.Y / 100 * chart.ClientRectangle.Height);

                if (area != null)
                {
                    Axis xAxis = area.AxisX;
                    Axis yAxis = area.AxisY;
                    if (double.IsNaN(yAxis.Maximum))
                    {
                        area.AxisY.Minimum = area.AxisY2.Minimum;
                        area.AxisY.Maximum = area.AxisY2.Maximum;
                        return;
                    }
                    foreach (var ob in HighOrderBlocks.Where(ob => ob.Visible))
                    {
                        int skip = _candles.Count - ob.Length;
                        if (skip < 0) continue;
                        int period = _candles.Count - skip;
                        double highUp = (double)ob.Top;
                        double highDown = (double)ob.Bottom;

                        double xPixel1 = xAxis.ValueToPixelPosition(skip);
                        double yPixel1 = yAxis.ValueToPixelPosition(highUp);
                        double xPixel2 = ob.IsBroken ? xAxis.ValueToPixelPosition(ob.BrokenIndex) : xAxis.ValueToPixelPosition(skip + period - 1);
                        double yPixel2 = yAxis.ValueToPixelPosition(highDown);
                        double xPixel3 = xAxis.ValueToPixelPosition(skip + period - 1);
                        xPixel1 = Math.Min(xPixel1, xMax);
                        xPixel2 = Math.Min(xPixel2, xMax);
                        yPixel2 = Math.Min(yPixel2, yMax);
                        xPixel3 = Math.Min(xPixel3, xMax);

                        using (Brush fillBrush = new SolidBrush(Color.FromArgb(30, Color.Red)))
                        using (Brush brush = new SolidBrush(Color.Red))
                        using (Pen pen = new Pen(brush))
                        {
                            g.FillRectangle(fillBrush, new RectangleF((float)xPixel1, (float)yPixel1, (float)(xPixel2 - xPixel1), (float)(yPixel2 - yPixel1)));
                            g.DrawLine(pen, (float)xPixel1, (float)yPixel1, (float)xPixel3, (float)yPixel1);
                            g.DrawLine(pen, (float)xPixel1, (float)yPixel2, (float)xPixel3, (float)yPixel2);
                        }

                        if (ob.IsBroken)
                        {
                            using (Brush brush = new SolidBrush(Color.FromArgb(30, Color.Green)))
                            {
                                g.FillRectangle(brush, new RectangleF((float)xPixel2, (float)yPixel1, (float)(xPixel3 - xPixel2), (float)(yPixel2 - yPixel1)));
                            }
                        }
                    }

                    foreach (var ob in LowOrderBlocks.Where(ob => ob.Visible))
                    {
                        int skip = _candles.Count - ob.Length;
                        if (skip < 0) continue;
                        int period = _candles.Count - skip;
                        double highUp = (double)ob.Top;
                        double highDown = (double)ob.Bottom;

                        double xPixel1 = xAxis.ValueToPixelPosition(skip);
                        double yPixel1 = yAxis.ValueToPixelPosition(highUp);
                        double xPixel2 = ob.IsBroken ? xAxis.ValueToPixelPosition(ob.BrokenIndex) : xAxis.ValueToPixelPosition(skip + period - 1);
                        double yPixel2 = yAxis.ValueToPixelPosition(highDown);
                        double xPixel3 = xAxis.ValueToPixelPosition(skip + period - 1);
                        xPixel1 = Math.Min(xPixel1, xMax);
                        xPixel2 = Math.Min(xPixel2, xMax);
                        yPixel2 = Math.Min(yPixel2, yMax);
                        xPixel3 = Math.Min(xPixel3, xMax);

                        using (Brush fillBrush = new SolidBrush(Color.FromArgb(30, Color.Blue)))
                        using (Brush brush = new SolidBrush(Color.Blue))
                        using (Pen pen = new Pen(brush))
                        {
                            g.FillRectangle(fillBrush, new RectangleF((float)xPixel1, (float)yPixel1, (float)(xPixel2 - xPixel1), (float)(yPixel2 - yPixel1)));
                            g.DrawLine(pen, (float)xPixel1, (float)yPixel1, (float)xPixel3, (float)yPixel1);
                            g.DrawLine(pen, (float)xPixel1, (float)yPixel2, (float)xPixel3, (float)yPixel2);
                        }

                        if (ob.IsBroken)
                        {
                            using (Brush brush = new SolidBrush(Color.FromArgb(30, Color.Green)))
                            {
                                g.FillRectangle(brush, new RectangleF((float)xPixel2, (float)yPixel1, (float)(xPixel3 - xPixel2), (float)(yPixel2 - yPixel1)));
                            }
                        }
                    }

                    area.AxisY.Minimum = area.AxisY2.Minimum;
                    area.AxisY.Maximum = area.AxisY2.Maximum;
                }
            }
            catch { }
        }

        private void ChartCandle_ChartCreated(object sender, Chart e)
        {
            e.PostPaint += Chart_PostPaint;
        }

        private void ChartCandle_ChartDeleting(object sender, Chart e)
        {
            e.PostPaint -= Chart_PostPaint;
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

        public decimal Top { get; set; }
        public decimal Bottom { get; set; }
        public int Length { get; set; }
        public int BrokenIndex { get; set; } = -1;
        public bool Visible { get; set; } = true;

        public OrderBlockType Type { get; private set; }

        /// <summary>
        /// Ордер блок пробит, цена пересекла
        /// </summary>
        public bool IsBroken => BrokenIndex > -1;

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
