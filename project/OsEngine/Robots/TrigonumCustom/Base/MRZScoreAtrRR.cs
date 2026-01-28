using OsEngine.Common;
using OsEngine.Entity;
using OsEngine.Indicators;
using OsEngine.Indicators.TrigonumCustom;
using OsEngine.OsTrader.Panels.Attributes;
using OsEngine.OsTrader.Panels.Tab;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;
using System.Text;

namespace OsEngine.Robots.TrigonumCustom.Base
{
    [Bot("MRZScoreAtrRR")]
    public class MRZScoreAtrRR : BotPanelSimple
    {
        // Индикаторы
        private Aindicator _sma;
        private ZScoreLow _zScoreLow;
        private ZScoreHigh _zScoreHigh;
        private ZScoreChannel _channel;
        private AtrDev _atrDev;
        private AtrDecoration _atr; // для ATR TP/SL

        // Параметры
        private StrategyParameterInt _periodSma;
        private StrategyParameterDecimal _zEnterBase;
        private StrategyParameterInt _gridSize;
        private StrategyParameterDecimal _spread; // шаг по AtrDev для определения уровней
        private StrategyParameterDecimal _atrMultDev;
        private StrategyParameterBool _debugLogging;

        // Grid
        private MeanReverseGrid _currentGrid = null;

        // Volatile stop
        private bool _volatileStopActive = false;
        private Side _volatileStopDirection = Side.Buy;
        private Side _gridDirection = Side.None;

        // TP/SL и volume manager
        private TakeProfitDecoration _takeProfit;
        private StopLossDecoration _stopLoss;
        private StrategyParameterDecimal _atrSlMultiplier;
        private StrategyParameterDecimal _stopLossLimitPercent;

        private StrategyParameterDecimal _r;
        private StrategyParameterDecimal _rr;
        private MeanReverseVolumeManager _volumeManager;

        // FairPrice Stop
        private FairPriceDecoration _fairPrice;

        public MRZScoreAtrRR(string name, StartProgram startProgram) : base(name, startProgram)
        {
            _multiplePosition = true;
            _tab.TPSLMode = TPSLMode.Partial;

            // Параметры
            _periodSma = CreateParameter("Sma Period", 50, 10, 500, 10, "Robot");
            _zEnterBase = CreateParameter("Z Enter Base", 3m, 1m, 5m, 0.5m, "Robot");
            _gridSize = CreateParameter("Grid Size", 7, 3, 20, 1, "Robot");
            _spread = CreateParameter("Spread", 0.2m, 0.01m, 5m, 0.01m, "Robot");
            _atrMultDev = CreateParameter("Atr Mult Setka", 1m, 0.1m, 5m, 0.1m, "ATR");
            _debugLogging = CreateParameter("Debug Logging", false, "Debug");

            // SMA
            _sma = (Aindicator)IndicatorsFactory.CreateIndicatorByName(nameClass: "Sma", name: name + "Sma", canDelete: false);
            _sma = (Aindicator)_tab.CreateCandleIndicator(_sma, nameArea: "Prime");
            _sma.Save();

            // ZScoreLow / ZScoreHigh / Channel
            _zScoreLow = (ZScoreLow)IndicatorsFactory.CreateIndicatorByName(nameClass: "ZScoreLow", name: name + "ZScoreLow", canDelete: false);
            _zScoreLow = (ZScoreLow)_tab.CreateCandleIndicator(_zScoreLow, nameArea: "ZScoreLow");
            _zScoreLow.Save();
            _zScoreLow.SMA = _sma;

            _zScoreHigh = (ZScoreHigh)IndicatorsFactory.CreateIndicatorByName(nameClass: "ZScoreHigh", name: name + "ZScoreHigh", canDelete: false);
            _zScoreHigh = (ZScoreHigh)_tab.CreateCandleIndicator(_zScoreHigh, nameArea: "ZScoreHigh");
            _zScoreHigh.Save();
            _zScoreHigh.SMA = _sma;

            _channel = (ZScoreChannel)IndicatorsFactory.CreateIndicatorByName(nameClass: "ZScoreChannel", name: name + "ZScoreChannel", canDelete: false);
            _channel = (ZScoreChannel)_tab.CreateCandleIndicator(_channel, nameArea: "Prime");
            _channel.ZScoreReference = _zEnterBase.ValueDecimal;
            _channel.LowZScore = _zScoreLow;
            _channel.HighZScore = _zScoreHigh;
            _channel.Save();

            // ATR decoration
            _atr = new AtrDecoration(this);
            _atr.CancelTPSL = false;

            // AtrDev
            _atrDev = (AtrDev)IndicatorsFactory.CreateIndicatorByName("AtrDev", name + "AtrDev", false);
            _atrDev = (AtrDev)_tab.CreateCandleIndicator(_atrDev, "AtrDev");
            _atrDev.Sma = _sma;
            _atrDev.Atr = _atr;
            _atrDev.OnlyPositive = true;

            // TP/SL
            _takeProfit = new TakeProfitDecoration(this, false, "ATR TP Enable", "ATR");
            _takeProfit.ActivationPriceFunc = GetTakeProfit;
            _rr = CreateParameter("Take RR", 3m, 0.5m, 10m, 0.5m, "ATR");

            _stopLossLimitPercent = CreateParameter("Stop Loss Limit Percent", 1m, 1m, 10m, 1m, "ATR");
            _stopLoss = new StopLossDecoration(this, false, "ATR SL Enable", "ATR");
            _atrSlMultiplier = CreateParameter("ATR SL Multiplier", 1m, 0.5m, 5m, 0.5m, "ATR");
            _stopLoss.StopPriceFunc = GetStopLoss;

            // Volume manager
            _r = CreateParameter("R, %", 1m, 1m, 15m, 1m, "Volume Manager");
            _volumeManager = new MeanReverseVolumeManager();
            _volumeManager.GetVolumeFunc = base.GetVolume;
            _volumeManager.Rounding = GetRounded;

            // FairPrice Decoration
            _fairPrice = new FairPriceDecoration(this, "FairPrice");
            _fairPrice.SetSma(_sma);

            // События
            _tab.PositionOpeningSuccesEvent += _tab_PositionOpeningSuccesEvent;
            _tab.PositionClosingSuccesEvent += _tab_PositionClosingSuccesEvent;

            // Volatile stop
            new VolatileStopDecoration(this, VolatileStopHandler);

            ParametersChangedByUser();
        }

        private void _tab_PositionClosingSuccesEvent(Position obj)
        {

        }

        private decimal GetRounded(decimal volume)
        {
            return GetRoundedVolume(_tab, volume);
        }

        private void LogDebug(string message)
        {
            if (_debugLogging != null && _debugLogging.ValueBool)
            {
                SendNewLogMessage(message, Logging.LogMessageType.System);
            }
        }

        private void _tab_PositionOpeningSuccesEvent(Position obj)
        {
            decimal atrDev = _atrDev.LastValue;
            if (_currentGrid == null)
            {
                decimal step = _spread.ValueDecimal;
                _currentGrid = new MeanReverseGrid(atrDev, step, _gridSize.ValueInt, Side.Sell, _tab.GetChartMaster().Candles.Count - 1);
                _currentGrid.SetPosition(0, obj);
                _volumeManager.Clear();

                if (_debugLogging != null && _debugLogging.ValueBool)
                {
                    StringBuilder sb = new StringBuilder();
                    foreach (var level in _currentGrid.GetGrid())
                    {
                        sb.Append($"[{level.Key};{level.Value}] ");
                    }
                    LogDebug($"Grid created dir={obj.Direction} center={atrDev:F8} step={step:F8} levels={sb}");
                }
            }
            else
            {
                try
                {
                    Dictionary<int, decimal> grid = _currentGrid.GetGrid();
                    Dictionary<int, Position> positions = _currentGrid.GetPositions();

                    // Уровни которые меньше текущего atrDev и не занятые позициями
                    var goodLevels = grid.Where(l => l.Value <= atrDev).Where(l => !positions.Keys.Contains(l.Key));

                    if (!goodLevels.Any())
                    {
                        return;
                    }

                    var maxValue = goodLevels.Max(l => l.Value);
                    var maxLevel = goodLevels.Where(l => l.Value == maxValue).FirstOrDefault();
                    _currentGrid.SetPosition(maxLevel.Key, obj);
                    var otherLevels = goodLevels.Except(new List<KeyValuePair<int, decimal>>() { maxLevel }).ToList();
                    foreach (var level in otherLevels)
                    {
                        _currentGrid.DeleteByKey(level.Key);
                    }
                }
                catch (Exception ex)
                {
                    SendNewLogMessage(ex.Message, Logging.LogMessageType.Error);
                }
            }
        }

        protected override void CandleFinishedEvent(List<Candle> candles)
        {
            if (candles.Count < 10 && _currentGrid != null && StartProgram == StartProgram.IsTester)
            {
                _currentGrid = null;
            }

            // Вызов базовой обработки
            base.CandleFinishedEvent(candles);

            if (_currentGrid != null)
            {
                try
                {
                    Candle last = candles.Last();
                    Dictionary<int, Position> positions = _currentGrid.GetPositions();
                    Dictionary<int, decimal> grid = _currentGrid.GetGrid();
                    var emptyLevels = grid.Where(l => !positions.ContainsKey(l.Key));
                    decimal sma = _sma.DataSeries[0].Last;
                    bool canEnterPositionBySma = true;
                    if (_gridDirection == Side.Buy)
                    {
                        decimal price = last.High;
                        canEnterPositionBySma = CanEnterPositionBySma(price, _gridDirection);
                    }
                    else if (_gridDirection == Side.Sell)
                    {
                        decimal price = last.Low;
                        canEnterPositionBySma = CanEnterPositionBySma(price, _gridDirection);
                    }
                    if ((_tab.PositionsOpenAll.Count == 0 && !emptyLevels.Any()) || (!canEnterPositionBySma && !positions.Where(p => p.Value.State == PositionStateType.Open || p.Value.State == PositionStateType.Opening).Any()))
                    {
                        foreach (KeyValuePair<int, Position> pair in _currentGrid.GetPositions())
                        {
                            Position pos = pair.Value;
                            if (pos != null)
                            {
                                ClosePosition(pos);
                            }
                        }
                        LogDebug("All positions closed - resetting grid");
                        _currentGrid = null;
                        _volumeManager.Clear();
                    }
                }
                catch (Exception ex)
                {
                    SendNewLogMessage(ex.Message, Logging.LogMessageType.Error);
                }
            }
        }

        private decimal GetTakeProfit(Position position)
        {
            // Получаем стоп-лосс (в цене)
            decimal stopLossPrice = GetStopLoss(position);
            decimal entry = position.EntryPrice;

            // Вычисляем расстояние стопа в пунктах
            decimal stopDistance = Math.Abs(entry - stopLossPrice);

            // Умножаем на RR
            decimal takeDistance = stopDistance * _rr.ValueDecimal;

            // Применяем в зависимости от направления
            switch (position.Direction)
            {
                case Side.Buy:
                    return entry + takeDistance;
                case Side.Sell:
                    return entry - takeDistance;
                default:
                    return entry;
            }
        }

        private decimal GetStopLoss(Position position)
        {
            decimal limit = _stopLossLimitPercent.ValueDecimal / 100m * position.EntryPrice;
            decimal stopLoss = Math.Min(limit, _atr.CurrentAtr * _atrSlMultiplier.ValueDecimal);
            switch (position.Direction)
            {
                case Side.Buy:
                    return position.EntryPrice - stopLoss;
                case Side.Sell:
                    return position.EntryPrice + stopLoss;
                default:
                    return position.EntryPrice - stopLoss;
            }
        }

        private void VolatileStopHandler()
        {
            try
            {
                if (_currentGrid != null)
                {
                    Dictionary<int, Position> positions = _currentGrid.GetPositions();

                    // Отменяем открытия
                    foreach (var pair in positions)
                    {
                        Position pos = pair.Value;
                        if (pos == null) continue;

                        if (pos.State == PositionStateType.Opening)
                        {
                            foreach (Order order in pos.OpenOrders)
                            {
                                if (order.State != OrderStateType.Cancel && order.State != OrderStateType.Done)
                                {
                                    try
                                    {
                                        SendNewLogMessage($"Стоп по волатильности отменил ордер от {order.TimeCreate}", Logging.LogMessageType.Trade);
                                        _tab.Connector.OrderCancel(order);
                                    }
                                    catch (Exception ex)
                                    {
                                        SendNewLogMessage(ex.Message, Logging.LogMessageType.Error);
                                    }
                                }
                            }
                        }
                    }

                    _volatileStopActive = true;
                    _volatileStopDirection = _gridDirection;
                    _volumeManager.Clear();

                    LogDebug($"VolatileStopHandler fired for direction {_volatileStopDirection}");
                }
            }
            catch (Exception ex)
            {
                SendNewLogMessage(ex.Message, Logging.LogMessageType.Error);
            }
        }

        protected override bool CheckClosePosition(List<Candle> candles, Position position)
        {
            return false;
        }

        // Логика первого входа и добавлений — объединяет ZScore (только для первой позиции) и AtrDev-grid
        protected override bool CheckOpenLongPosition(List<Candle> candles)
        {
            Candle last = candles.Last();

            // Снятие блокировки volatile stop по Buy
            if (_volatileStopActive && _volatileStopDirection == Side.Buy)
            {
                decimal sma = _sma.DataSeries[0].Last;
                if (sma <= last.High)
                {
                    _volatileStopActive = false;
                    LogDebug("Volatile stop cleared for Buy");
                }
                else
                {
                    return false;
                }
            }

            // Если грид противоположного направления — не входим
            if (_currentGrid != null && _gridDirection == Side.Sell) return false;

            decimal smaValue = _sma.DataSeries[0].Last;
            decimal currentPrice = last.Close;

            // Первая позиция: ZScore используется только при отсутствии грида
            if (_currentGrid == null)
            {
                // Для лонга логика: price (максимально удалённый от SMA) ниже SMA и соответствующий ZScoreLow exceed
                decimal diffHigh = Math.Abs(last.High - smaValue);
                decimal diffLow = Math.Abs(last.Low - smaValue);
                decimal diffClose = Math.Abs(last.Close - smaValue);

                decimal price;
                if (diffHigh >= diffLow && diffHigh >= diffClose) price = last.High;
                else if (diffLow >= diffHigh && diffLow >= diffClose) price = last.Low;
                else price = last.Close;

                if (price < smaValue)
                {
                    decimal z = _zScoreLow.LastValue;
                    if (z >= _zEnterBase.ValueDecimal)
                    {
                        _volumeManager.Clear();
                        _gridDirection = Side.Buy;
                        LogDebug($"Long first-entry check: price={price:F8} sma={smaValue:F8} zLow={z:F8} threshold={_zEnterBase.ValueDecimal:F8}");
                        LogDebug("Long first-entry condition satisfied -> returning true");
                        return true;
                    }
                }

                return false;
            }
            else
            {
                if (_gridDirection == Side.Sell) return false;
                try
                {
                    decimal atrDev = _atrDev.LastValue;
                    Dictionary<int, decimal> grid = _currentGrid.GetGrid();
                    Dictionary<int, Position> positions = _currentGrid.GetPositions();

                    var emptyLevels = grid.Where(p => !positions.ContainsKey(p.Key)).ToList();
                    decimal currAtrDev = _atrDev.LastValue;

                    var atrCandidates = emptyLevels.Where(p => atrDev >= p.Value).ToList();
                    if (!atrCandidates.Any()) return false;
                    if (positions.Where(p => p.Value.EntryPrice < currentPrice).Any()) return false;
                    return true;
                }
                catch (Exception ex)
                {
                    SendNewLogMessage(ex.Message, Logging.LogMessageType.Error);
                    return false;
                }
            }
        }

        protected override bool CheckOpenShortPosition(List<Candle> candles)
        {
            Candle last = candles.Last();

            // Снятие блокировки volatile stop по Sell
            if (_volatileStopActive && _volatileStopDirection == Side.Sell)
            {
                decimal sma = _sma.DataSeries[0].Last;
                if (sma >= last.Low)
                {
                    _volatileStopActive = false;
                    LogDebug("Volatile stop cleared for Sell");
                }
                else
                {
                    return false;
                }
            }

            if (_currentGrid != null && _gridDirection == Side.Buy) return false;

            decimal smaValue = _sma.DataSeries[0].Last;
            decimal currentPrice = last.Close;

            if (_currentGrid == null)
            {
                // Первая позиция для шорта: выбираем price наиболее удалённый от SMA; если он выше SMA и соответствующий ZScoreHigh >= threshold
                decimal diffHigh = Math.Abs(last.High - smaValue);
                decimal diffLow = Math.Abs(last.Low - smaValue);
                decimal diffClose = Math.Abs(last.Close - smaValue);

                decimal price;
                if (diffHigh >= diffLow && diffHigh >= diffClose) price = last.High;
                else if (diffLow >= diffHigh && diffLow >= diffClose) price = last.Low;
                else price = last.Close;

                if (price > smaValue)
                {
                    decimal z = _zScoreHigh.LastValue;
                    if (z >= _zEnterBase.ValueDecimal)
                    {
                        _volumeManager.Clear();
                        _gridDirection = Side.Sell;
                        LogDebug($"Short first-entry check: price={price:F8} sma={smaValue:F8} zHigh={z:F8} threshold={_zEnterBase.ValueDecimal:F8}");
                        LogDebug("Short first-entry condition satisfied -> returning true");
                        return true;
                    }
                }

                return false;
            }
            else
            {
                if (_gridDirection == Side.Buy) return false;
                try
                {
                    decimal atrDev = _atrDev.LastValue;
                    Dictionary<int, decimal> grid = _currentGrid.GetGrid();
                    Dictionary<int, Position> positions = _currentGrid.GetPositions();

                    var emptyLevels = grid.Where(p => !positions.ContainsKey(p.Key)).ToList();

                    var atrCandidates = emptyLevels.Where(p => atrDev >= p.Value).ToList();
                    if (!atrCandidates.Any()) return false;
                    if (positions.Where(p => p.Value.EntryPrice > currentPrice).Any()) return false;
                    return true;
                }
                catch (Exception ex)
                {
                    SendNewLogMessage(ex.Message, Logging.LogMessageType.Error);
                    return false;
                }
            }
        }

        protected override List<Func<List<Candle>, bool>> GetCheckers()
        {
            return new List<Func<List<Candle>, bool>>()
            {
                candles => _periodSma.ValueInt < candles.Count,
                candles => _atr.CurrentAtr != 0
            };
        }

        protected override decimal GetVolume(bool getRounded = true)
        {
            return _volumeManager.GetNextVolume(getRounded);
        }

        protected override void ParametersChangedByUser()
        {
            SetSmaParameters();
            SetAtrDevParameters();
            SetChannelParameters();
            SetVolumeManager();
            SetZScoreChannelReference();
        }

        private void SetZScoreChannelReference()
        {
            if (_channel == null || _zEnterBase == null) return;
            _channel.ZScoreReference = _zEnterBase.ValueDecimal;
        }

        private void SetAtrDevParameters()
        {
            if (_atrDev == null || _atrMultDev == null) return;
            _atrDev.AtrMultDev = _atrMultDev.ValueDecimal;
        }

        private void SetSmaParameters()
        {
            if (_sma == null || _periodSma == null) return;
            if (_sma?.Parameters[0] is IndicatorParameterInt parameter)
            {
                parameter.ValueInt = _periodSma.ValueInt;
            }
        }

        private void SetChannelParameters()
        {
            // канал получает ссылки на ZScoreLow / ZScoreHigh; их SMA уже настроена
        }

        private void SetVolumeManager()
        {
            if (_volumeManager == null || _r == null) return;
            _volumeManager.R = _r.ValueDecimal;
        }

        private void ClosePosition(Position position)
        {
            if (position == null) return;

            if (position.State == PositionStateType.Open)
            {
                try
                {
                    _tab.CloseAtMarket(position, position.OpenVolume);
                }
                catch (Exception ex)
                {
                    SendNewLogMessage(ex.Message, Logging.LogMessageType.Error);
                }
            }
            else
            {
                foreach (Order order in position.OpenOrders)
                {
                    if (order.State != OrderStateType.Cancel && order.State != OrderStateType.Done)
                    {
                        try
                        {
                            _tab.Connector.OrderCancel(order);
                        }
                        catch (Exception ex)
                        {
                            SendNewLogMessage(ex.Message, Logging.LogMessageType.Error);
                        }
                    }
                }
            }
        }

        private bool CanEnterPositionBySma(decimal price, Side side)
        {
            if (_sma == null || _sma.DataSeries == null || _sma.DataSeries[0].Values == null || _sma.DataSeries[0].Values.Count == 0) return true;
            decimal sma = _sma.DataSeries[0].Last;
            if (side == Side.Buy)
            {
                return price < sma; // для лонга цена должна быть ниже SMA
            }
            else
            {
                return price > sma; // для шорта — выше SMA
            }
        }
    }
}
enum GridTypePosition { None, Low, High }