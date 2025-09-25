using Com.Lmax.Api.Internal;
using OsEngine.Charts.CandleChart;
using OsEngine.Common.UI;
using OsEngine.Entity;
using OsEngine.Robots.TrigonumCustom.Periodic;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms.DataVisualization.Charting;

namespace OsEngine.Indicators.TrigonumCustom
{
    [Indicator("SessionIndicator")]
    public class SessionIndicator : Aindicator
    {
        private IndicatorDataSeries _series;
        private List<Candle> _candles;
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

        private void _chartMaster_ChartCandleCreated(object sender, EventArgs e)
        {
            BindChart();
        }

        private void ChartCandle_ChartCreated(object sender, Chart e)
        {
            e.PostPaint += Chart_PostPaint;
        }

        private void ChartCandle_ChartDeleting(object sender, Chart e)
        {
            e.PostPaint -= Chart_PostPaint;
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

        private List<Candle> GetVisibleCandles(Chart chart, List<Candle> candles)
        {
            List<Candle> result = new List<Candle>();
            ChartArea area = chart?.ChartAreas?.Where(a => a.Name == "Prime").SingleOrDefault();
            Series series = chart.Series.Where(s => s.ChartType == SeriesChartType.Candlestick).FirstOrDefault();
            if (area != null && series != null)
            {
                Axis axisX = area.AxisX;
                double minX = axisX.ScaleView.ViewMinimum;
                double maxX = axisX.ScaleView.ViewMaximum;
                IEnumerable<DataPoint> points = series.Points.Where(p => p.XValue >= minX && p.XValue <= maxX);
                result = candles.GetRange((int)points.First().XValue, points.Count());
            }
            return result;
        }

        List<SessionPaint> _paints = new List<SessionPaint>();

        private void Chart_PostPaint(object sender, ChartPaintEventArgs e)
        {
            try
            {
                Graphics g = e.ChartGraphics.Graphics;

                if (_candles == null || _candles.Count == 0) return;

                Chart chart = e.Chart;
                ChartArea area = chart?.ChartAreas?.Where(a => a.Name == "Prime").SingleOrDefault();
                Series series = chart.Series.Where(s => s.ChartType == SeriesChartType.Candlestick).FirstOrDefault();
                double xMax = (area.InnerPlotPosition.Width / 100 * chart.ClientRectangle.Width) - (area.Position.X / 100 * chart.ClientRectangle.Width);
                double yMax = (area.InnerPlotPosition.Height / 100 * chart.ClientRectangle.Height) - (area.Position.Y / 100 * chart.ClientRectangle.Height);

                if (area != null && series != null)
                {
                    Axis xAxis = area.AxisX;
                    Axis yAxis = area.AxisY;

                    double minX = xAxis.ScaleView.ViewMinimum;
                    double maxX = xAxis.ScaleView.ViewMaximum;
                    IEnumerable<DataPoint> points = series.Points.Where(p => p.XValue >= minX && p.XValue <= maxX);
                    List<Candle> visible = _candles.GetRange((int)points.First().XValue, points.Count());

                    if (double.IsNaN(yAxis.Maximum))
                    {
                        area.AxisY.Minimum = area.AxisY2.Minimum;
                        area.AxisY.Maximum = area.AxisY2.Maximum;
                        return;
                    }

                    _paints.Clear();

                    int offsetX = _candles.IndexOf(visible.First());

                    foreach (Candle candle in visible)
                    {
                        // hack добавить + TimeFrameSpan
                        IEnumerable<Period> start = SessionEditor.Sessions.Where(s => s.IsDefined).Where(s => candle.TimeStart.CompareOnlyTime((DateTime)s.Start));
                        IEnumerable<Period> end = SessionEditor.Sessions.Where(s => s.IsDefined).Where(s => candle.TimeStart.CompareOnlyTime((DateTime)s.End));

                        foreach (Period p in end)
                        {
                            SessionPaint s = _paints.FindLast(sp => sp.Period == p);
                            if (s == null)
                            {
                                s = new SessionPaint() { Period = p };
                                _paints.Add(s);
                            }
                            if (s.CandleStart == null)
                            {
                                s.CandleStart = visible.First();
                            }
                            s.CandleStop = candle;

                            SetSessionsCoordinates(s);
                        }
                        
                        foreach (Period p in start)
                        {
                            SessionPaint s = new SessionPaint() { Period = p, CandleStart = candle };
                            _paints.Add(s);
                        }
                    }

                    void SetSessionsCoordinates(SessionPaint sp)
                    {
                        int indexX1 = visible.IndexOf(sp.CandleStart);
                        int indexX2 = visible.IndexOf(sp.CandleStop);
                        
                        float x1 = indexX1 + offsetX;
                        float x2 = indexX2 + offsetX;

                        float y1 = (float)visible.GetRange(indexX1, indexX2 - indexX1).Max(c => c.High);
                        y1 = (float)yAxis.ValueToPixelPosition(y1);

                        float y2 = (float)visible.GetRange(indexX1, indexX2 - indexX1).Min(c => c.Low);
                        y2 = (float)yAxis.ValueToPixelPosition(y2);

                        sp.X1 = (int)x1;
                        sp.X2 = (int)x2;
                        sp.Y1 = (int)y1;
                        sp.Y2 = (int)y2;
                    }

                    foreach (SessionPaint sp in _paints)
                    {
                        using (Brush fillBrush = new SolidBrush(Color.FromArgb(30, Color.Red)))
                        using (Brush brush = new SolidBrush(Color.Red))
                        using (Pen pen = new Pen(brush))
                        {
                            g.FillRectangle(fillBrush, new RectangleF(sp.X1, sp.Y1, sp.X2 - sp.X1, sp.Y2 - sp.Y1));
                            //g.DrawLine(pen, (float)xPixel1, (float)yPixel1, (float)xPixel3, (float)yPixel1);
                            //g.DrawLine(pen, (float)xPixel1, (float)yPixel2, (float)xPixel3, (float)yPixel2);
                        }
                    }

                    area.AxisY.Minimum = area.AxisY2.Minimum;
                    area.AxisY.Maximum = area.AxisY2.Maximum;
                }
            }
            catch { }
        }

        public override void OnProcess(List<Candle> candles, int index)
        {
            _candles = candles;
        }

        public override void OnStateChange(IndicatorState state)
        {
            if (state == IndicatorState.Configure)
            {
                _series = CreateSeries("Session", Color.DarkGreen, IndicatorChartPaintType.Line, false);
                //    _zz = IndicatorsFactory.CreateIndicatorByName("ZigZag", Name + "ZigZag", false);
                //    _period = CreateParameterInt("Period", 30);
                //    _priceBasis = CreateParameterString("PriceBasis", PriceBasis.Full.ToString());
                //    _priceBasis.ValuesString.AddRange(Enum.GetNames(typeof(PriceBasis)).Except(_priceBasis.ValuesString));

                //    ProcessIndicator("ZigZag", _zz);
                //    TypeIndicator = IndicatorChartPaintType.Line;
            }
        }
    }

    public class SessionPaint
    {
        public Period Period { get; set; }

        public Candle CandleStart { get; set; }
        public Candle CandleStop { get; set; }

        public int X1 { get; set; }

        public int Y1 { get; set; }
        public int X2 { get; set; }

        public int Y2 { get; set; }
    }
}
