using OsEngine.Common;
using OsEngine.Common.UI;
using OsEngine.Entity;
using OsEngine.Indicators;
using OsEngine.Indicators.TrigonumCustom;
using OsEngine.OsTrader.Panels.Attributes;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms.DataVisualization.Charting;

namespace OsEngine.Robots.TrigonumCustom.Base
{
    [Bot("MeanReversionSma2")]
    public class MeanReversionSma2 : BotPanelSimple
    {
        private ZScore _zScore;
        private Aindicator _ema;
        private Aindicator _sma;

        private StrategyParameterInt _periodSma;
        private StrategyParameterDecimal _zEnterBaseLong;
        private StrategyParameterDecimal _zEnterBaseShort;
        private StrategyParameterInt _periodEma;
        private StrategyParameterDecimal _spread;
        private MeanReverseGrid _currentGrid = null;

        public MeanReversionSma2(string name, StartProgram startProgram) : base(name, startProgram)
        {
            _multiplePosition = true;
            _periodSma = CreateParameter("SMA period", 20, 20, 400, 1, "Robot parameters");
            _periodEma = CreateParameter("EMA period", 200, 100, 300, 1, "Robot parameters");

            _sma = (Aindicator)IndicatorsFactory.CreateIndicatorByName(nameClass: "Sma", name: name + "Sma", canDelete: false);
            _sma = (Aindicator)_tab.CreateCandleIndicator(_sma, nameArea: "Prime");
            _sma.Save();

            _zScore = (ZScore)IndicatorsFactory.CreateIndicatorByName(nameClass: "ZScore", name: name + "ZScore", canDelete: false);
            _zScore = (ZScore)_tab.CreateCandleIndicator(_zScore, nameArea: "ZScore");
            //_zScore.ParametersDigit[0].Value = _periodSma.ValueInt;
            _zScore.DataSeries[0].Color = Color.Red;
            _zScore.Save();

            _ema = (Aindicator)IndicatorsFactory.CreateIndicatorByName(nameClass: "Ema", name: name + "Ema", canDelete: false);
            _ema = (Aindicator)_tab.CreateCandleIndicator(_ema, nameArea: "Prime");
            _ema.Save();

            _zScore.SMA = _sma;

            _zEnterBaseLong = CreateParameter("Z Enter Base Long", -2m, -3m, -1m, 0.1m, "Robot");
            _zEnterBaseShort = CreateParameter("Z Enter Base Short", 2m, 1m, 3m, 0.1m, "Robot");

            _spread = CreateParameter("Spread", 1m, 0.1m, 1m, 0.1m, "Robot");

            new TakeProfitDecoration(this);
            new StopLossDecoration(this);
            _tab.PositionOpeningSuccesEvent += _tab_PositionOpeningSuccesEvent;
            UpdateParameters();
        }

        protected override void CandleFinishedEvent(List<Candle> candles)
        {
            base.CandleFinishedEvent(candles);
            if (_tab.PositionsOpenAll.Count == 0)
            {
                _currentGrid = null;
            }
        }

        private void _tab_PositionOpeningSuccesEvent(Position obj)
        {
            if (_currentGrid == null)
            {
                _currentGrid = new MeanReverseGrid(obj.EntryPrice, _spread.ValueDecimal, _zScore.CurrentSigma, 7, obj.Direction, _tab.GetChartMaster().Candles.Count - 1);
            }
        }

        private void UpdateParameters()
        {
            SetEmaPeriod();
            SetSmaPeriod();
        }

        private void SetEmaPeriod()
        {
            if (_ema?.Parameters[0] is IndicatorParameterInt parameter)
            {
                parameter.ValueInt = _periodEma.ValueInt;
            }
        }

        private void SetSmaPeriod()
        {
            if (_sma?.Parameters[0] is IndicatorParameterInt parameter)
            {
                parameter.ValueInt = _periodSma.ValueInt;
            }
        }

        protected override bool CheckClosePosition(List<Candle> candles, Position position)
        {
            return false;
        }

        protected override bool CheckOpenLongPosition(List<Candle> candles)
        {
            if (_currentGrid != null && _currentGrid.Direction == Side.Sell) return false;

            if (_currentGrid != null)
            {
                decimal price = candles.Last().Low;
                decimal ema = _ema.DataSeries[0].Last;
                Dictionary<int, bool> dict = _currentGrid.GetDict();
                Dictionary<int, decimal> grid = _currentGrid.GetGrid();
                IEnumerable<KeyValuePair<int, decimal>> pairs = grid.Where(pair => pair.Value >= price && pair.Value > ema);
                bool result = false;
                foreach (KeyValuePair<int, decimal> pair in pairs)
                {
                    if (!dict[pair.Key])
                    {
                        dict[pair.Key] = true;
                        result |= true;
                    }
                }
                return result;
            }
            else
            {
                decimal z = _zScore.CurrentZ;
                decimal ema = _ema.DataSeries[0].Last;
                decimal price = candles.Last().Close;
                if (z < _zEnterBaseLong.ValueDecimal && price > ema)
                {
                    return true;
                }
            }

            return false;
        }

        protected override bool CheckOpenShortPosition(List<Candle> candles)
        {
            if (_currentGrid != null && _currentGrid.Direction == Side.Buy) return false;


            if (_currentGrid != null)
            {
                decimal price = candles.Last().High;
                decimal ema = _ema.DataSeries[0].Last;
                Dictionary<int, bool> dict = _currentGrid.GetDict();
                Dictionary<int, decimal> grid = _currentGrid.GetGrid();
                IEnumerable<KeyValuePair<int, decimal>> pairs = grid.Where(pair => pair.Value <= price && pair.Value < ema);
                bool result = false;
                foreach (KeyValuePair<int, decimal> pair in pairs)
                {
                    if (!dict[pair.Key])
                    {
                        dict[pair.Key] = true;
                        result |= true;
                    }
                }
                return result;
            }
            else
            {
                decimal z = _zScore.CurrentZ;
                decimal ema = _ema.DataSeries[0].Last;
                decimal price = candles.Last().Close;
                if (z > _zEnterBaseShort.ValueDecimal && price < ema)
                {
                    return true;
                }
            }

            return false;
        }

        protected override List<Func<List<Candle>, bool>> GetCheckers()
        {
            return new List<Func<List<Candle>, bool>>() 
            {
                (candles) => { return candles.Count >= _periodSma.ValueInt && candles.Count >= _periodEma.ValueInt; }
            };
        }

        protected override void OnChartPostPaint(object sender, ChartPaintEventArgs e)
        {
            if (_currentGrid == null) return;
            try
            {
                Graphics g = e.ChartGraphics.Graphics;

                Chart chart = e.Chart;
                ChartArea area = chart?.ChartAreas?.Where(a => a.Name == "Prime").SingleOrDefault();
                Series series = chart.Series.Where(s => s.ChartType == SeriesChartType.Candlestick).FirstOrDefault();

                if (area != null && series != null)
                {
                    Axis xAxis = area.AxisX;
                    Axis yAxis = area.AxisY;

                    int index = _currentGrid.Index;

                    double minX = xAxis.ScaleView.ViewMinimum;
                    double maxX = xAxis.ScaleView.ViewMaximum;

                    area.AxisY.Minimum = area.AxisY2.Minimum;
                    area.AxisY.Maximum = area.AxisY2.Maximum;

                    Dictionary<int, decimal> grid = _currentGrid.GetGrid();
                    Dictionary<int, bool> dict = _currentGrid.GetDict();

                    using (Brush brush = new SolidBrush(Color.Gray))
                    using (Pen pen = new Pen(brush))
                    using (Brush brushGreen = new SolidBrush(Color.Green))
                    using (Pen penGreen = new Pen(brushGreen))
                    {
                        foreach (KeyValuePair<int, decimal> pair in grid)
                        {
                            float x1 = (float)xAxis.ValueToPixelPosition(index - 3);
                            float x2 = (float)xAxis.ValueToPixelPosition(maxX);
                            float y = (float)yAxis.ValueToPixelPosition((float)pair.Value);
                            if (dict[pair.Key])
                            {

                                g.DrawLine(penGreen, x1, y, x2, y);
                            }
                            else
                            {
                                g.DrawLine(pen, x1, y, x2, y);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {

            }
        }

        protected override void ParametersChangedByUser()
        {
            UpdateParameters();
        }
    }

    class MeanReverseGrid
    {
        private Side _side;
        /// <summary>
        /// Словарь, key - индекс уровня в гриде, value - исполнен ли ордер
        /// </summary>
        private Dictionary<int, bool> _dict = new Dictionary<int, bool>();
        private Dictionary<int, decimal> _grid = new Dictionary<int, decimal>();
        private int _index;

        public MeanReverseGrid(decimal price, decimal spread, decimal sigma, int levelsCount, Side side, int index)
        {
            if (levelsCount < 2) throw new Exception("Уровней должно быть хотя бы 2");
            _side = side;
            _index = index;
            decimal delta = spread * sigma;
            if (side == Side.Buy)
            {
                for (int i = 1; i < levelsCount + 1; i++)
                {
                    _grid.Add(i, price - delta * i);
                    _dict.Add(i, false);
                }
            }
            else
            {
                for (int i = 1 ; i < levelsCount + 1; i++)
                {
                    _grid.Add(i, price + delta * i);
                    _dict.Add(i, false);
                }
            }
        }

        public Side Direction => _side;

        public int Index => _index;

        public Dictionary<int, decimal> GetGrid()
        {
            return _grid;
        }

        public Dictionary<int, bool> GetDict()
        {
            return _dict;
        }
    }
}
