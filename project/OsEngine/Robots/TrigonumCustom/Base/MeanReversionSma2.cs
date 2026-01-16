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

        /// <summary>
        /// Период за который анализируются доходности свечей, чтобы отменять лимитки в случае резкого увеличении доходности в сторону, противоположную позиции
        /// </summary>
        private StrategyParameterInt _periodLossFilter;
        private StrategyParameterInt _lossCandlesCount;
        private StrategyParameterInt _quantile;
        private List<decimal> _profits = new List<decimal>();
        private List<decimal> _profitsLast = new List<decimal>();
        /// <summary>
        /// Текущее значение квантиля, куда входят последние <see cref="_lossCandlesCount"/> свечей по доходности
        /// </summary>
        private int _currentQuantile = 0;

        public MeanReversionSma2(string name, StartProgram startProgram) : base(name, startProgram)
        {
            _multiplePosition = true;
            _tab.TPSLMode = TPSLMode.Partial;
            _periodSma = CreateParameter("SMA period", 20, 20, 400, 1, "Robot");
            _periodEma = CreateParameter("EMA period", 200, 100, 300, 1, "Robot");

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

            _periodLossFilter = CreateParameter("Period", 100, 100, 500, 100, "Volatile Stop");
            _lossCandlesCount = CreateParameter("Candles Count", 3, 3, 5, 1, "Volatile Stop");
            _quantile = CreateParameter("Quantile", 90, 80, 95, 5, "Volatile Stop");

            new TakeProfitDecoration(this);
            new StopLossDecoration(this);
            _tab.PositionOpeningSuccesEvent += _tab_PositionOpeningSuccesEvent;
            UpdateParameters();
        }

        protected override void CandleFinishedEvent(List<Candle> candles)
        {
            CandleProfitFilter(candles);
            base.CandleFinishedEvent(candles);
            if (_tab.PositionsOpenAll.Count == 0)
            {
                if (_currentGrid != null)
                {
                    foreach (KeyValuePair<int, Position> pair in _currentGrid.GetPositions())
                    {
                        ClosePosition(pair.Value);
                    }
                    _currentGrid = null;
                }
            }
            else
            {
                if (_currentGrid != null)
                {
                    List<int> keysToDelete = new List<int>();
                    foreach (KeyValuePair<int, Position> pair in _currentGrid.GetPositions())
                    {
                        Position pos = pair.Value;
                        if (!CanEnterPositionByEma(pos.EntryPrice, pos.Direction))
                        {
                            if (pos.State == PositionStateType.Opening)
                            {
                                keysToDelete.Add(pair.Key);
                                ClosePosition(pos);
                            }
                        }
                    }

                    foreach (int key in keysToDelete)
                    {
                        _currentGrid.DeleteByKey(key);
                    }
                }
            }
        }

        /// <summary>
        /// Рассчитать доходности последних <see cref="_periodLossFilter"/> свечей для отмены лимиток, в случае резкого движения цены в сторону, противоположную позиции
        /// </summary>
        private void CandleProfitFilter(List<Candle> candles)
        {
            if (candles == null || candles.Count == 0) return;

            if (candles.Count < _profits.Count)
            {
                _profits.Clear();
            }

            int take = candles.Count - _profits.Count;
            int skip = candles.Count - take;
            foreach (Candle candle in candles.Skip(skip).Take(take))
            {
                _profits.Add(GetCandleProfit(candle));
            }

            take = _periodLossFilter.ValueInt;
            skip = _profits.Count - take;

            _profitsLast = _profits.Skip(skip).Take(take).ToList();

            take = _lossCandlesCount.ValueInt;
            skip = _profitsLast.Count - take;

            decimal minProfit = _profitsLast.Skip(skip).Take(take).Min();
            _currentQuantile = (int)((float)_profitsLast.Where(v => v <= minProfit).Count() / ((float)_periodLossFilter.ValueInt) * 100);
            if (_currentQuantile >= _quantile.ValueInt)
            {
                if (_currentGrid != null)
                {
                    List<KeyValuePair<int,Position>> activePositions = _currentGrid.GetPositions().Where(p => p.Value.OpenActiv && p.Value.State != PositionStateType.Open).ToList();
                    foreach (KeyValuePair<int, Position> p in activePositions)
                    {
                        foreach (Order order in p.Value.OpenOrders)
                        {
                            if (order.State != OrderStateType.Cancel && order.State != OrderStateType.Done)
                            {
                                _tab.Connector.OrderCancel(order);
                                _currentGrid.DeleteByKey(p.Key);
                                SendNewLogMessage($"Лимитный ордер отменён из-за того, что доходности последних {_lossCandlesCount.ValueInt} свечей больше доходностей {_quantile.ValueInt}% доходностей {_periodLossFilter.ValueInt} последних свечей", Logging.LogMessageType.Trade);
                            }
                        }
                    }
                }
            }
        }

        private decimal GetCandleProfit(Candle candle)
        {
            return (candle.High - candle.Low) / candle.Low;
        }

        private void ClosePosition(Position position)
        {
            if (position.State == PositionStateType.Open)
            {
                _tab.CloseAtMarket(position, position.OpenVolume);
            }
            else
            {
                foreach (Order order in position.OpenOrders)
                {
                    if (order.State != OrderStateType.Cancel && order.State != OrderStateType.Done)
                    {
                        _tab.Connector.OrderCancel(order);
                    }
                }
            }
        }

        private bool CanEnterPositionByEma(decimal price, Side side)
        {
            decimal ema = _ema.DataSeries[0].Last;
            if (side == Side.Buy)
            {
                return price > ema;
            }
            else
            {
                return price < ema;
            }
        }

        private void _tab_PositionOpeningSuccesEvent(Position obj)
        {
            if (_currentGrid == null)
            {
                _currentGrid = new MeanReverseGrid(obj.EntryPrice, _spread.ValueDecimal * _zScore.CurrentSigma, 7, obj.Direction, _tab.GetChartMaster().Candles.Count - 1);
                Dictionary<int, decimal> grid = _currentGrid.GetGrid();
                List<int> keysToDelete = new List<int>();
                foreach (KeyValuePair<int, decimal> pair in grid)
                {
                    try
                    {
                        if (!CanEnterPositionByEma(pair.Value, _currentGrid.Direction))
                        {
                            keysToDelete.Add(pair.Key);
                            continue;
                        }
                        Position position = null;
                        if (_currentGrid.Direction == Side.Buy)
                        {
                            position = _tab.BuyAtLimit(GetVolume(), pair.Value);
                        }
                        else if (_currentGrid.Direction == Side.Sell)
                        {
                            position = _tab.SellAtLimit(GetVolume(), pair.Value);
                        }
                        else
                        {
                            throw new Exception("Неизвестное направление позиции");
                        }

                        _currentGrid.SetPosition(pair.Key, position);
                    }
                    catch (Exception ex)
                    {
                        SendNewLogMessage(ex.Message, Logging.LogMessageType.Error);
                    }
                }

                foreach (int key in keysToDelete)
                {
                    _currentGrid.DeleteByKey(key);
                }
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

            if (_currentGrid == null)
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

            if (_currentGrid == null)
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
                    Dictionary<int, Position> positions = _currentGrid.GetPositions();

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
                            if (positions.ContainsKey(pair.Key) && positions[pair.Key].State == PositionStateType.Open)
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
}
