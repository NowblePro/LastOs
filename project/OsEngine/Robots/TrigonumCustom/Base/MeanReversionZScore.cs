using OsEngine.Common;
using OsEngine.Entity;
using OsEngine.Indicators;
using OsEngine.Indicators.TrigonumCustom;
using OsEngine.OsTrader.Panels.Attributes;
using OsEngine.OsTrader.Panels.Tab;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Documents;

namespace OsEngine.Robots.TrigonumCustom.Base
{
    [Bot("MeanReversionZScore")]
    public class MeanReversionZScore : BotPanelSimple
    {
        private Aindicator _sma;
        private ZScoreLow _zScoreLow;
        private ZScoreHigh _zScoreHigh;
        private ZScoreChannel _channel;

        private StrategyParameterInt _periodSma;
        private StrategyParameterDecimal _spread;
        /// <summary>
        /// Базовый уровень Z Score (который отрисовывается и который является последним уровнем)
        /// </summary>
        private StrategyParameterDecimal _zScoreRef;

        private ZScoreGrid _highGrid;
        private ZScoreGrid _lowGrid;

        private AtrDecoration _atrStop;
        private TakeProfitDecoration _takeProfit;
        private StopLossDecoration _stopLoss;

        public MeanReversionZScore(string name, StartProgram startProgram) : base(name, startProgram)
        {
            _multiplePosition = true;
            _tab.TPSLMode = TPSLMode.Partial;
            _sma = IndicatorsFactory.CreateIndicatorByName("Sma", name + "Sma", false);
            _sma = (Aindicator)_tab.CreateCandleIndicator(_sma, "Prime");
            _periodSma = CreateParameter("Sma Period", 50, 50, 500, 50, "Robot");
            _spread = CreateParameter("Spread", 0.5m, 0.5m, 1.5m, 0.5m, "Robot");
            _zScoreRef = CreateParameter("Channel Size", 3m, 2m, 5m, 1m, "Robot");

            _zScoreLow = (ZScoreLow)IndicatorsFactory.CreateIndicatorByName(nameClass: "ZScoreLow", name: name + "ZScoreLow", canDelete: false);
            _zScoreLow = (ZScoreLow)_tab.CreateCandleIndicator(_zScoreLow, nameArea: "ZScoreLow");
            _zScoreLow.DataSeries[0].Color = Color.Blue;
            _zScoreLow.Save();
            _zScoreLow.SMA = _sma;

            _zScoreHigh = (ZScoreHigh)IndicatorsFactory.CreateIndicatorByName(nameClass: "ZScoreHigh", name: name + "ZScoreHigh", canDelete: false);
            _zScoreHigh = (ZScoreHigh)_tab.CreateCandleIndicator(_zScoreHigh, nameArea: "ZScoreHigh");
            _zScoreHigh.DataSeries[0].Color = Color.Red;
            _zScoreHigh.Save();
            _zScoreHigh.SMA = _sma;

            _channel = (ZScoreChannel)IndicatorsFactory.CreateIndicatorByName(nameClass: "ZScoreChannel", name: name + "ZScoreChannel", canDelete: false);
            _channel = (ZScoreChannel)_tab.CreateCandleIndicator(_channel, nameArea: "Prime");
            _channel.ZScoreReference = _zScoreRef.ValueDecimal;
            _channel.LowZScore = _zScoreLow;
            _channel.HighZScore = _zScoreHigh;
            _channel.DataSeries[0].Color = Color.Yellow;
            _channel.Save();

            _takeProfit = new TakeProfitDecoration(this, false, "ATR TP Enable");
            _takeProfit.ActivationPriceFunc = GetTakeProfit;

            _stopLoss = new StopLossDecoration(this, false, "ATR SL Enable");
            _stopLoss.StopPriceFunc = GetStopLoss;

            _tab.PositionOpeningSuccesEvent += _tab_PositionOpeningSuccesEvent;
            _tab.PositionClosingSuccesEvent += _tab_PositionClosingSuccesEvent;

            _atrStop = new AtrDecoration(this);

            UpdateParameters();
        }

        private decimal GetTakeProfit(Position position)
        {
            decimal result = 0;
            LiquiditySweep sweep = null;
            Candle last = _candles.Last();
            switch (position.Direction)
            {
                case Side.Buy:
                    sweep = currentDivergencePriceBull.LastOrDefault();
                    if (sweep != null)
                    {
                        decimal sl = _candles[sweep.Index2].Low - _smartStopLossOffset.ValueDecimal;
                        decimal slDelta = Math.Abs(last.Close - sl);
                        result = last.Close + slDelta * _smartTakeProfitMultiplier.ValueDecimal;
                    }
                    break;
                case Side.Sell:
                    sweep = currentDivergencePriceBear.LastOrDefault();
                    if (sweep != null)
                    {
                        decimal sl = _candles[sweep.Index2].High + _smartStopLossOffset.ValueDecimal;
                        decimal slDelta = Math.Abs(sl - last.Close);
                        result = last.Close - slDelta * _smartTakeProfitMultiplier.ValueDecimal;
                    }
                    break;
                default:
                    result = _candles.Last().Center;
                    break;
            }
            return result;
        }

        private decimal GetStopLoss(Position position)
        {
            decimal result = 0;
            LiquiditySweep sweep = null;
            switch (position.Direction)
            {
                case Side.Buy:
                    sweep = currentDivergencePriceBull.LastOrDefault();
                    if (sweep != null)
                    {
                        result = _candles[sweep.Index2].Low - _smartStopLossOffset.ValueDecimal;
                    }
                    break;
                case Side.Sell:
                    sweep = currentDivergencePriceBear.LastOrDefault();
                    if (sweep != null)
                    {
                        result = _candles[sweep.Index2].High + _smartStopLossOffset.ValueDecimal;
                    }
                    break;
                default:
                    result = _candles.Last().Center;
                    break;
            }
            return result;
        }

        private void _tab_PositionClosingSuccesEvent(Position obj)
        {
            if (_gridPositionType == GridTypePosition.Low)
            {
                if (!_lowGrid.HasPositions)
                {
                    _lowGrid.Clear();
                    _highGrid.Clear();
                    _gridPositionType = GridTypePosition.None;
                }
            }
            else if (_gridPositionType == GridTypePosition.High)
            {
                if (!_highGrid.HasPositions)
                {
                    _lowGrid.Clear();
                    _highGrid.Clear();
                    _gridPositionType = GridTypePosition.None;
                }
            }
        }

        enum GridTypePosition { None, Low, High }

        private GridTypePosition _gridPositionType = GridTypePosition.None;
        private void _tab_PositionOpeningSuccesEvent(Position obj)
        {
            if (_gridPositionType == GridTypePosition.Low)
            {
                _lowGrid.Deal(_zScoreLow.LastValue, obj);
            }
            else if (_gridPositionType == GridTypePosition.High)
            {
                _highGrid.Deal(_zScoreHigh.LastValue, obj);
            }
        }

        private decimal SMA => _sma.DataSeries[0].Values.Last();

        protected override bool CheckClosePosition(List<Candle> candles, Position position)
        {
            return false;
        }

        protected override bool CheckOpenLongPosition(List<Candle> candles)
        {
            if (!_zScoreHigh.Ready)
            {
                _highGrid.Clear();
            }

            if (!_zScoreHigh.Ready || _gridPositionType == GridTypePosition.High) return false;
            Candle last = candles.Last();
            if (SMA > last.Close)
            {
                if (_lowGrid.CheckDeal(_zScoreLow.LastValue))
                {
                    _gridPositionType = GridTypePosition.Low;
                    return true;
                }
            }
            return false;
        }

        protected override bool CheckOpenShortPosition(List<Candle> candles)
        {
            if (!_zScoreLow.Ready)
            {
                _lowGrid.Clear();
            }

            if (!_zScoreLow.Ready || _gridPositionType == GridTypePosition.Low) return false;
            Candle last = candles.Last();
            if (SMA < last.Close)
            {
                if (_highGrid.CheckDeal(_zScoreHigh.LastValue))
                {
                    _gridPositionType = GridTypePosition.High;
                    return true;
                }
            }
            return false;
        }

        protected override List<Func<List<Candle>, bool>> GetCheckers()
        {
            return new List<Func<List<Candle>, bool>>()
            {
                candles => _periodSma.ValueInt < candles.Count,
            };
        }

        protected override void ParametersChangedByUser()
        {
            UpdateParameters();
        }

        private void UpdateParameters()
        {
            SetZScoreChannelReference();
            SetSmaPeriod();
            SetGrids();
        }

        private void SetGrids()
        {
            if (_spread == null || _zScoreRef == null) return;
            if (_highGrid != null)
            {
                _highGrid.Clear();
            }

            if (_lowGrid != null)
            {
                _lowGrid.Clear();
            }

            _highGrid = new ZScoreGrid(_spread.ValueDecimal, _zScoreRef.ValueDecimal, _tab);
            _lowGrid = new ZScoreGrid(_spread.ValueDecimal, _zScoreRef.ValueDecimal, _tab);
        }

        private void SetZScoreChannelReference()
        {
            if (_channel == null || _zScoreRef == null) return;
            _channel.ZScoreReference = _zScoreRef.ValueDecimal;
        }

        private void SetSmaPeriod()
        {
            if (_sma?.Parameters[0] is IndicatorParameterInt parameter)
            {
                parameter.ValueInt = _periodSma.ValueInt;
            }
        }
    }

    class ZScoreGrid
    {
        private decimal _spread = 0.5m;
        private List<ZScoreLevel> _levels = new List<ZScoreLevel>();

        public ZScoreGrid(decimal spread, decimal zScoreReference, BotTabSimple tab)
        {
            if (spread == 0) throw new ArgumentException("Шаг z score не может быть равен 0");
            _spread = spread;
            int levelCount = (int)(zScoreReference / _spread);
            if (levelCount == 0) throw new ArgumentException("Количество уровней z score равно 0");
            for (int i = 0; i < levelCount; i++)
            {
                _levels.Add(new ZScoreLevel(spread * (i + 1), i, tab));
            }
        }

        public bool HasPositions => _levels.Any(l => l.IsActivePosition);

        public bool CheckDeal(decimal currentZScore)
        {
            IEnumerable<ZScoreLevel> levels = _levels.Where(l => !l.IsDealed && l.CheckDeal(currentZScore));
            return levels.Any();
        }

        public void Deal(decimal currentZScore, Position position)
        {
            IEnumerable<ZScoreLevel> levels = _levels.Where(l => !l.IsDealed && l.CheckDeal(currentZScore));
            int maxIndex = levels.Max(l => l.Index);
            ZScoreLevel maxLevel = levels.Where(l => l.Index == maxIndex).Single();
            maxLevel.Deal(position);
            foreach (ZScoreLevel level in levels)
            {
                if (level == maxLevel) continue;
                level.Deal();
            }
        }

        /// <summary>
        /// Закрыть все открытые позиции
        /// </summary>
        public void Clear()
        {
            foreach (ZScoreLevel level in _levels)
            {
                level.Clear();
            }
        }

        class ZScoreLevel
        {
            BotTabSimple _tab;
            private decimal _level = 0;
            private bool _deal = false;
            private int _index;
            private Position _position = null;

            public ZScoreLevel(decimal level, int index, BotTabSimple tab)
            {
                _level = level;
                _index = index;
            }

            public bool IsActivePosition
            {
                get
                {
                    if (Position != null && (Position.State == PositionStateType.Open || Position.State == PositionStateType.Opening))
                    {
                        return true;
                    }

                    return false;
                }
            }

            public bool IsDealed => _deal;

            public int Index => _index;

            public Position Position => _position;

            public bool CheckDeal(decimal currentZScore)
            {
                return currentZScore >= _level;
            }

            public void Deal(Position position = null)
            {
                if (_deal) throw new Exception("Повторная сделка");
                _position = position;
                _deal = true;
            }

            /// <summary>
            /// Закрыть позицию
            /// </summary>
            public void Clear()
            {
                _deal = false;
                if (Position == null) return;
                ClosePosition(Position);
                _position = null;
            }

            private void ClosePosition(Position position)
            {
                if (position.State == PositionStateType.Open)
                {
                    _tab?.CloseAtMarket(position, position.OpenVolume);
                }
                else
                {
                    foreach (Order order in position.OpenOrders)
                    {
                        if (order.State != OrderStateType.Cancel && order.State != OrderStateType.Done)
                        {
                            _tab?.Connector.OrderCancel(order);
                        }
                    }
                }
            }
        }
    }
}
