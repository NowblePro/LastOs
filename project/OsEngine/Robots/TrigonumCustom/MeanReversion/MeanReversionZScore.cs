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
        private decimal _spreadPrev = 0;
        private decimal _zScoreRefPrev = 0;

        private ZScoreGrid _highGrid;
        private ZScoreGrid _lowGrid;

        private AtrDecoration _atrStop;
        private TakeProfitDecoration _takeProfit;
        private StrategyParameterDecimal _atrTpMultiplier;
        private StopLossDecoration _stopLoss;
        private StrategyParameterDecimal _atrSlMultiplier;
        private StrategyParameterDecimal _stopLossLimitPercent;

        private GridTypePosition _volatileStopType = GridTypePosition.None;

        private StrategyParameterDecimal _r;
        private MeanReverseVolumeManager _volumeManager;

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

            _takeProfit = new TakeProfitDecoration(this, false, "ATR TP Enable", "ATR");
            _atrTpMultiplier = CreateParameter("ATR TP Multiplier", 1m, 1m, 5m, 0.5m, "ATR");
            _takeProfit.ActivationPriceFunc = GetTakeProfit;

            _stopLossLimitPercent = CreateParameter("Stop Loss Limit Percent", 1m, 1m, 5m, 0.5m, "ATR");
            _stopLoss = new StopLossDecoration(this, false, "ATR SL Enable", "ATR");
            _atrSlMultiplier = CreateParameter("ATR SL Multiplier", 1m, 1m, 5m, 0.5m, "ATR");
            _stopLoss.StopPriceFunc = GetStopLoss;

            _r = CreateParameter("R, %", 1m, 1m, 15m, 1m, "Volume Manager");
            _volumeManager = new MeanReverseVolumeManager();
            _volumeManager.GetVolumeFunc = base.GetVolume;
            _volumeManager.Rounding = GetRounded;

            _tab.PositionStartOpeningSuccessEvent += _tab_PositionStartOpeningSuccessEvent;
            _tab.PositionOpeningFailEvent += _tab_PositionOpeningFailEvent;

            _atrStop = new AtrDecoration(this);
            _atrStop.CancelTPSL = false;

            VolatileStopDecoration vs = new VolatileStopDecoration(this, VolatileStopHandler);
            UpdateParameters();
        }

        private decimal GetRounded(decimal volume)
        {
            return GetRoundedVolume(_tab, volume);
        }

        private void _tab_PositionOpeningFailEvent(Position position)
        {
            //_lowGrid.ClearPosition(position);
            //_highGrid.ClearPosition(position);
        }

        private void _tab_PositionStartOpeningSuccessEvent(Position obj)
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

        private void VolatileStopHandler()
        {
            if (_gridPositionType == GridTypePosition.Low)
            {
                if (_lowGrid.HasPositions)
                {
                    _lowGrid.CancelAll();
                    IEnumerable<Position> opening = _tab.PositionsAll.Where(p => p.State == PositionStateType.Opening);
                    foreach (Position p in opening)
                    {
                        CancelPosition(p);
                    }
                }
                _volatileStopType = GridTypePosition.Low;
            }
            else if (_gridPositionType == GridTypePosition.High)
            {
                if (_highGrid.HasPositions)
                {
                    _highGrid.CancelAll();
                    IEnumerable<Position> opening = _tab.PositionsAll.Where(p => p.State == PositionStateType.Opening);
                    foreach (Position p in opening)
                    {
                        CancelPosition(p);
                    }
                }
                _volatileStopType = GridTypePosition.High;
            }

            void CancelPosition(Position position)
            {
                foreach (Order order in position.OpenOrders)
                {
                    SendNewLogMessage($"Стоп по волатильности отменил ордер от {order.TimeCreate}", Logging.LogMessageType.Trade);
                    _tab.Connector.OrderCancel(order);
                }
            }
        }

        private decimal GetTakeProfit(Position position)
        {
            decimal result = 0;
            decimal price = position.EntryPrice;
            switch (position.Direction)
            {
                case Side.Buy:
                    result = price + _atrStop.CurrentAtr * _atrTpMultiplier.ValueDecimal;
                    break;
                case Side.Sell:
                    result = price - _atrStop.CurrentAtr * _atrTpMultiplier.ValueDecimal;
                    break;
                default:
                    result = price + _atrStop.CurrentAtr * _atrTpMultiplier.ValueDecimal;
                    break;
            }
            return result;
        }

        private decimal GetStopLoss(Position position)
        {
            decimal limit = _stopLossLimitPercent.ValueDecimal / 100m * position.EntryPrice;
            decimal result = 0;
            decimal price = position.EntryPrice;
            decimal stopLoss = Math.Min(limit, _atrStop.CurrentAtr * _atrSlMultiplier.ValueDecimal);
            switch (position.Direction)
            {
                case Side.Buy:
                    result = price - stopLoss;
                    break;
                case Side.Sell:
                    result = price + stopLoss;
                    break;
                default:
                    result = price - stopLoss;
                    break;
            }
            return result;
        }

        enum GridTypePosition { None, Low, High }

        private GridTypePosition _gridPositionType = GridTypePosition.None;

        private decimal SMA => _sma.DataSeries[0].Values.Last();

        protected override bool CheckClosePosition(List<Candle> candles, Position position)
        {
            return false;
        }

        protected override bool CheckOpenLongPosition(List<Candle> candles)
        {
            if (!_zScoreHigh.Ready)
            {
                _highGrid.Reset();
            }

            Candle last = candles.Last();

            if (_gridPositionType == GridTypePosition.Low && SMA <= last.High)
            {
                if (!_lowGrid.HasPositions)
                {
                    _lowGrid.Clear();
                    _gridPositionType = GridTypePosition.None;
                    _volumeManager.Clear();
                }
            }

            if (_volatileStopType == GridTypePosition.Low)
            {
                if (SMA <= last.High)
                {
                    _volatileStopType = GridTypePosition.None;
                }
                else
                {
                    return false;
                }
            }

            if (!_zScoreHigh.Ready || _gridPositionType == GridTypePosition.High) return false;
            
            if (SMA > last.Close)
            {
                if (_lowGrid.AllPositions.Any(p => p.EntryPrice < last.Close))
                {
                    return false;
                }

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
                _lowGrid.Reset();
            }

            Candle last = candles.Last();

            if (_gridPositionType == GridTypePosition.High && SMA >= last.High)
            {
                if (!_highGrid.HasPositions)
                {
                    _highGrid.Clear();
                    _gridPositionType = GridTypePosition.None;
                    _volumeManager.Clear();
                }
            }

            if (_volatileStopType == GridTypePosition.High)
            {
                if (SMA >= last.Low)
                {
                    _volatileStopType = GridTypePosition.None;
                }
                else
                {
                    return false;
                }
            }

            if (!_zScoreLow.Ready || _gridPositionType == GridTypePosition.Low) return false;

            if (SMA < last.Close)
            {
                if (_highGrid.AllPositions.Any(p => p.EntryPrice > last.Close))
                {
                    return false;
                }

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

        protected override decimal GetVolume(bool getRounded = true)
        {
            return _volumeManager.GetNextVolume(getRounded);
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
            SetVolumeManager();
        }

        private void SetVolumeManager()
        {
            if (_volumeManager == null || _r == null) return;
            _volumeManager.R = _r.ValueDecimal;
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

            if (_spread.ValueDecimal == _spreadPrev && _zScoreRef.ValueDecimal == _zScoreRefPrev)
            {
                return;
            }

            _spreadPrev = _spread.ValueDecimal;
            _zScoreRefPrev = _zScoreRef.ValueDecimal;
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
        private BotTabSimple _tab;
        public ZScoreGrid(decimal spread, decimal zScoreReference, BotTabSimple tab)
        {
            if (spread == 0) throw new ArgumentException("Шаг z score не может быть равен 0");
            _spread = spread;
            int levelCount = (int)(zScoreReference / _spread);
            if (levelCount == 0) levelCount = 1;
            for (int i = 0; i < levelCount; i++)
            {
                _levels.Add(new ZScoreLevel(spread * (i + 1), i, tab));
            }
            _tab = tab;
        }

        public bool HasPositions => _levels.Any(l => l.IsActivePosition);

        public IEnumerable<Position> AllPositions => _levels.Where(l => l.Position != null).Select(l => l.Position);

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

        public void Reset()
        {
            foreach (ZScoreLevel level in _levels)
            {
                level.Reset();
            }
        }

        public void CancelAll()
        {
            foreach(ZScoreLevel level in _levels)
            {
                level?.Cancel();
            }
        }


        internal void ClearPosition(Position position)
        {
            ZScoreLevel level = _levels.Where(l => l.Position == position).FirstOrDefault();
            if (level != null)
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
                _tab = tab;
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
                ClosePosition(Position, _tab);
                _position = null;
            }

            public void Reset()
            {
                _deal = false;
                _position = null;
            }

            public static void ClosePosition(Position position, BotTabSimple tab)
            {
                if (position.State == PositionStateType.Open)
                {
                    tab.CloseAtMarket(position, position.OpenVolume);
                }
                else
                {
                    foreach (Order order in position.OpenOrders)
                    {
                        if (order.State != OrderStateType.Cancel && order.State != OrderStateType.Done)
                        {
                            tab.Connector.OrderCancel(order);
                        }
                    }
                }
            }

            public void Cancel()
            {
                if (!_deal)
                {
                    Deal(null);
                }

                if (Position != null)
                {
                    foreach (Order order in Position.OpenOrders)
                    {
                        if (order.State != OrderStateType.Cancel && order.State != OrderStateType.Done)
                        {
                            _tab.Connector.OrderCancel(order);
                        }
                    }
                }
            }
        }
    }
}
