using OsEngine.Common;
using OsEngine.Entity;
using OsEngine.Indicators;
using OsEngine.Indicators.TrigonumCustom;
using OsEngine.OsTrader.Panels.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;

namespace OsEngine.Robots.TrigonumCustom.Base
{
    [Bot("MeanReversion1Fix")]
    public class MeanReversion1Fix : BotPanelSimple
    {
        private Aindicator _sma;
        private Aindicator _ema;
        private AtrDev _atrDev;

        private AtrDecoration _atr;
        private StrategyParameterDecimal _atrMultDev;
        private StrategyParameterInt _smaLength;

        private StrategyParameterDecimal _zEnterBaseLong;
        private StrategyParameterDecimal _zEnterBaseShort;
        private StrategyParameterInt _emaLength;
        private StrategyParameterDecimal _spread;
        private StrategyParameterBool _emaReverseLogic;
        private StrategyParameterDecimal _atrMultSpread;
        private StrategyParameterDecimal _r;

        private MeanReverseGrid _currentGrid = null;
        private int _gridSize = 7;

        // Ключ уровня, который мы собираемся заполнить при следующем успешном открытии позиции.
        // -1 означает "не задан".
        private int _nextGridKeyToFill = -1;

        // Волатильный стоп
        private bool _volatileStopActive = false;
        private Side _volatileStopDirection = Side.Buy;

        private MeanReverseVolumeManager _volumeManager;

        private TakeProfitDecoration _takeProfit;
        private StrategyParameterDecimal _atrTpMultiplier;

        private StopLossDecoration _stopLoss;
        private StrategyParameterDecimal _atrSlMultiplier;
        private StrategyParameterDecimal _stopLossLimitPercent;
        private LogDecoration _logDecoration;

        public MeanReversion1Fix(string name, StartProgram startProgram) : base(name, startProgram)
        {
            _multiplePosition = true;
            _tab.TPSLMode = TPSLMode.Partial;

            // Индикаторы
            _emaLength = CreateParameter("EMA period", 200, 100, 300, 1, "Robot");
            _sma = IndicatorsFactory.CreateIndicatorByName("Sma", name + "Sma", false);
            _sma = (Aindicator)_tab.CreateCandleIndicator(_sma, "Prime");

            _ema = (Aindicator)IndicatorsFactory.CreateIndicatorByName(nameClass: "Ema", name: name + "Ema", canDelete: false);
            _ema = (Aindicator)_tab.CreateCandleIndicator(_ema, nameArea: "Prime");
            _ema.Save();

            _smaLength = CreateParameter("Sma Length", 14, 14, 500, 50, "Robot");

            _zEnterBaseLong = CreateParameter("Z Enter Base Long", -2m, -3m, -1m, 0.1m, "Robot");
            _zEnterBaseShort = CreateParameter("Z Enter Base Short", 2m, 1m, 3m, 0.1m, "Robot");

            _spread = CreateParameter("Spread", 1m, 0.1m, 1m, 0.1m, "Robot");

            _atr = new AtrDecoration(this);
            _atr.CancelTPSL = false;

            _atrDev = (AtrDev)IndicatorsFactory.CreateIndicatorByName("AtrDev", name + "AtrDev", false);
            _atrDev = (AtrDev)_tab.CreateCandleIndicator(_atrDev, "AtrDev");
            _atrDev.Sma = _sma;
            _atrDev.Atr = _atr;

            _atrMultDev = CreateParameter("Atr Mult Dev", 1m, 1m, 5m, 0.5m, "Robot");
            _atrMultSpread = CreateParameter("Atr Mult Spread", 1m, 1m, 5m, 0.5m, "Robot");

            _emaReverseLogic = CreateParameter("Ema Reverse Logic", false, "Robot");

            // Управление объёмом
            _r = CreateParameter("R, %", 1m, 1m, 15m, 1m, "Volume Manager");
            _volumeManager = new MeanReverseVolumeManager();
            _volumeManager.GetVolumeFunc = base.GetVolume;
            _volumeManager.Rounding = GetRounded;

            // Декорации
            _takeProfit = new TakeProfitDecoration(this, false, "ATR TP Enable", "ATR");
            _atrTpMultiplier = CreateParameter("ATR TP Multiplier", 1m, 1m, 5m, 0.5m, "ATR");
            _takeProfit.ActivationPriceFunc = GetTakeProfit;

            _stopLossLimitPercent = CreateParameter("Stop Loss Limit Percent", 1m, 1m, 5m, 0.5m, "ATR");
            _stopLoss = new StopLossDecoration(this, false, "ATR SL Enable", "ATR");
            _atrSlMultiplier = CreateParameter("ATR SL Multiplier", 1m, 1m, 5m, 0.5m, "ATR");
            _stopLoss.StopPriceFunc = GetStopLoss;

            new VolatileStopDecoration(this, VolatileStopHandler);

            _logDecoration = new LogDecoration(this);

            // События
            _tab.PositionOpeningSuccesEvent += _tab_PositionOpeningSuccesEvent;

            ParametersChangedByUser();
        }

        private void _tab_PositionOpeningSuccesEvent(Position obj)
        {
            if (_currentGrid == null)
            {
                decimal atr = _atr.CurrentAtr;
                decimal centerPrice = obj.EntryPrice;
                decimal step = _spread.ValueDecimal + atr * _atrMultSpread.ValueDecimal;

                _currentGrid = new MeanReverseGrid(centerPrice, step, _gridSize, obj.Direction, _tab.GetChartMaster().Candles.Count - 1);
                _currentGrid.SetPosition(0, obj);
                _nextGridKeyToFill = -1;
            }
            else
            {
                try
                {
                    // Игнорируем позиции противоположного направления
                    if (obj.Direction != _currentGrid.Direction)
                    {
                        return;
                    }

                    Dictionary<int, decimal> grid = _currentGrid.GetGrid();
                    Dictionary<int, Position> positions = _currentGrid.GetPositions();

                    if (_nextGridKeyToFill != -1 && grid.ContainsKey(_nextGridKeyToFill) && !positions.ContainsKey(_nextGridKeyToFill))
                    {
                        _currentGrid.SetPosition(_nextGridKeyToFill, obj);

                        // Удаляем уровни, которые "пропущены" (уже неактуальны)
                        List<int> keysToDelete = new List<int>();
                        decimal selectedValue = grid[_nextGridKeyToFill];

                        if (_currentGrid.Direction == Side.Buy)
                        {
                            foreach (var pair in grid)
                            {
                                if (pair.Key == _nextGridKeyToFill) continue;
                                if (pair.Value >= selectedValue && !positions.ContainsKey(pair.Key))
                                {
                                    keysToDelete.Add(pair.Key);
                                }
                            }
                        }
                        else if (_currentGrid.Direction == Side.Sell)
                        {
                            foreach (var pair in grid)
                            {
                                if (pair.Key == _nextGridKeyToFill) continue;
                                if (pair.Value <= selectedValue && !positions.ContainsKey(pair.Key))
                                {
                                    keysToDelete.Add(pair.Key);
                                }
                            }
                        }

                        foreach (int key in keysToDelete)
                        {
                            _currentGrid.DeleteByKey(key);
                        }

                        _nextGridKeyToFill = -1;
                        return;
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
            if (_logDecoration.IsOn)
            {
                Candle last = candles.Last();
                decimal currentPrice = last.Close;
                decimal z = _atrDev.LastValue;
                decimal ema = _ema.DataSeries[0].Last;
                decimal sma = _sma.DataSeries[0].Last;
                decimal price = last.Close;
                bool signalLong = z < _zEnterBaseLong.ValueDecimal && (_emaReverseLogic.ValueBool ? (currentPrice < ema) : (currentPrice > ema));
                bool signalShort = z > _zEnterBaseShort.ValueDecimal && (_emaReverseLogic.ValueBool ? (currentPrice > ema) : (currentPrice < ema));
                _logDecoration.LogDebug(
                        $"Time: {last.TimeStart:dd.MM.yyyy HH:mm:ss} | " +
                        $"Open: {last.Open:F3} | " +
                        $"Close: {last.Close:F3} | " +
                        $"Low: {last.Low:F3} | " +
                        $"High: {last.High:F3} | " +
                        $"EMA: {ema:F3} | " +
                        $"SMA: {sma:F3} | " +
                        $"AtrDev: {z:F3} | " +
                        $"zEnterBaseLong: {_zEnterBaseLong.ValueDecimal:F3} | " +
                        $"zEnterBaseShort: {_zEnterBaseShort.ValueDecimal:F3} | " +
                        $"LongSignal: {signalLong} | " +
                        $"ShortSignal: {signalShort} |" +
                        $"{_currentGrid}");
            }
            ApplySmaFilter(candles);

            base.CandleFinishedEvent(candles);

            if (_currentGrid != null)
            {
                try
                {
                    if (_tab.PositionsOpenAll.Count == 0)
                    {
                        foreach (KeyValuePair<int, Position> pair in _currentGrid.GetPositions())
                        {
                            Position pos = pair.Value;
                            if (pos != null)
                            {
                                ClosePosition(pos);
                            }
                        }
                        _currentGrid = null;
                        _nextGridKeyToFill = -1;
                        _volumeManager.Clear();
                    }
                    else
                    {
                        List<int> keysToDelete = new List<int>();
                        foreach (KeyValuePair<int, Position> pair in _currentGrid.GetPositions())
                        {
                            int key = pair.Key;
                            Position pos = pair.Value;
                            if (pos == null) continue;

                            if (!CanEnterPositionByEma(pos.EntryPrice, pos.Direction))
                            {
                                if (pos.State == PositionStateType.Opening)
                                {
                                    foreach (Order order in pos.OpenOrders)
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
                                    keysToDelete.Add(key);
                                }
                            }
                        }

                        foreach (int key in keysToDelete)
                        {
                            try
                            {
                                _currentGrid.DeleteByKey(key);
                            }
                            catch (Exception ex)
                            {
                                SendNewLogMessage(ex.Message, Logging.LogMessageType.Error);
                            }
                        }

                        if (_currentGrid.GetGrid().Count == 0 || !_currentGrid.GetPositions().Any())
                        {
                            bool hasOpenOrOpening = _currentGrid.GetPositions().Any(p => p.Value != null && (p.Value.State == PositionStateType.Open || p.Value.State == PositionStateType.Opening));
                            if (!hasOpenOrOpening)
                            {
                                _currentGrid = null;
                                _nextGridKeyToFill = -1;
                                _volumeManager.Clear();
                            }
                        }
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
            decimal result = 0;
            decimal price = position.EntryPrice;
            switch (position.Direction)
            {
                case Side.Buy:
                    result = price + _atr.CurrentAtr * _atrTpMultiplier.ValueDecimal;
                    break;
                case Side.Sell:
                    result = price - _atr.CurrentAtr * _atrTpMultiplier.ValueDecimal;
                    break;
                default:
                    result = price + _atr.CurrentAtr * _atrTpMultiplier.ValueDecimal;
                    break;
            }
            return result;
        }

        private decimal GetStopLoss(Position position)
        {
            decimal limit = _stopLossLimitPercent.ValueDecimal / 100m * position.EntryPrice;
            decimal result = 0;
            decimal price = position.EntryPrice;
            decimal stopLoss = Math.Min(limit, _atr.CurrentAtr * _atrSlMultiplier.ValueDecimal);
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

        private void VolatileStopHandler()
        {
            try
            {
                if (_currentGrid != null)
                {
                    Dictionary<int, Position> positions = _currentGrid.GetPositions();

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
                    _volatileStopDirection = _currentGrid.Direction;
                    _volumeManager.Clear();
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

        protected override bool CheckOpenLongPosition(List<Candle> candles)
        {
            if (_volatileStopActive && _volatileStopDirection == Side.Buy)
            {
                Candle last = candles.Last();
                decimal sma = _sma.DataSeries[0].Last;
                if (sma <= last.High)
                {
                    if (_logDecoration.IsOn)
                    {
                        _logDecoration.LogDebug("Стоп по волатильности на лонг прекратил действовать (sma <= last.High)");
                    }
                    _volatileStopActive = false;
                }
                else
                {
                    if (_logDecoration.IsOn)
                    {
                        _logDecoration.LogDebug("Стоп по волатильности отменил проверку на лонг");
                    }
                    return false;
                }
            }

            if (_currentGrid != null && _currentGrid.Direction == Side.Sell) return false;

            Candle lastCandle = candles.Last();
            decimal currentPrice = lastCandle.Close;

            // --- Проверка фильтра по предыдущей цене входа ---
            if (_currentGrid != null && _currentGrid.GetPositions().Count > 0)
            {
                // Получаем последнюю (наиболее экстремальную) позицию по цене
                var positions = _currentGrid.GetPositions()
                    .Where(p => p.Value != null && p.Value.State == PositionStateType.Open)
                    .Select(p => p.Value)
                    .OrderBy(p => p.EntryPrice) // от меньшей к большей
                    .ToList();

                if (positions.Count > 0)
                {
                    decimal lastEntryPrice = positions.Last().EntryPrice; // самая высокая цена среди открытых — последняя точка входа

                    // Фильтр: следующий вход должен быть ДЕШЕВЛЕ последнего
                    if (currentPrice >= lastEntryPrice)
                    {
                        return false;
                    }
                }
            }

            if (_currentGrid == null)
            {
                decimal z = _atrDev.LastValue;
                decimal ema = _ema.DataSeries[0].Last;
                bool signal = z < _zEnterBaseLong.ValueDecimal && (_emaReverseLogic.ValueBool ? (currentPrice < ema) : (currentPrice > ema));
                if (signal)
                {
                    _volumeManager.Clear();
                    _logDecoration.LogDebug(
                    $"LONG check | " +
                    $"Time: {lastCandle.TimeStart:dd.MM.yyyy HH:mm:ss} | " +
                    $"Close: {currentPrice:F3} | " +
                    $"EMA: {ema:F3} | " +
                    $"Z: {z:F3} | " +
                    $"zEnterBaseLong: {_zEnterBaseLong.ValueDecimal:F3} | " +
                    $"Price < EMA: {currentPrice < ema} | " +
                    $"Z < zEnterBaseLong: {z < _zEnterBaseLong.ValueDecimal} | " +
                    $"emaReverseLogic: {_emaReverseLogic.ValueBool} | " +
                    $"Signal: {signal}");
                    return true;
                }
            }
            else
            {
                try
                {
                    Dictionary<int, decimal> grid = _currentGrid.GetGrid();
                    Dictionary<int, Position> positions = _currentGrid.GetPositions();

                    var emptyLevels = grid.Where(p => !positions.ContainsKey(p.Key)).ToList();
                    var candidates = emptyLevels.Where(p => currentPrice < p.Value).ToList();

                    if (!candidates.Any())
                    {
                        return false;
                    }

                    var target = candidates.OrderBy(p => p.Value).First();
                    _nextGridKeyToFill = target.Key;
                    _logDecoration.LogDebug(
                    $"LONG fills grid | " +
                    $"Time: {lastCandle.TimeStart:dd.MM.yyyy HH:mm:ss} | " +
                    $"Close: {currentPrice:F3} | " +
                    $"emptyLevels.Count: {emptyLevels.Count:F3} | " +
                    $"zEnterBaseLong: {_zEnterBaseLong.ValueDecimal:F3} | " +
                    $"nextGridKeyToFill: {_nextGridKeyToFill} | " +
                    $"emaReverseLogic: {_emaReverseLogic.ValueBool} | ");
                    return true;
                }
                catch (Exception ex)
                {
                    SendNewLogMessage(ex.Message, Logging.LogMessageType.Error);
                    return false;
                }
            }

            return false;
        }

        protected override bool CheckOpenShortPosition(List<Candle> candles)
        {
            if (_volatileStopActive && _volatileStopDirection == Side.Sell)
            {
                Candle last = candles.Last();
                decimal sma = _sma.DataSeries[0].Last;
                if (sma >= last.Low)
                {
                    if (_logDecoration.IsOn)
                    {
                        _logDecoration.LogDebug("Стоп по волатильности на шорт прекратил действовать (sma >= last.Low)");
                    }
                    _volatileStopActive = false;
                }
                else
                {
                    if (_logDecoration.IsOn)
                    {
                        _logDecoration.LogDebug("Стоп по волатильности отменил проверку на шорт");
                    }
                    return false;
                }
            }

            if (_currentGrid != null && _currentGrid.Direction == Side.Buy) return false;

            Candle lastCandle = candles.Last();
            decimal currentPrice = lastCandle.Close;

            // --- Проверка фильтра по предыдущей цене входа ---
            if (_currentGrid != null && _currentGrid.GetPositions().Count > 0)
            {
                var positions = _currentGrid.GetPositions()
                    .Where(p => p.Value != null && p.Value.State == PositionStateType.Open)
                    .Select(p => p.Value)
                    .OrderByDescending(p => p.EntryPrice) // от большей к меньшей
                    .ToList();

                if (positions.Count > 0)
                {
                    decimal lastEntryPrice = positions.Last().EntryPrice; // самая низкая цена — последняя точка входа

                    // Фильтр: следующий вход должен быть ДОРОЖЕ последнего
                    if (currentPrice <= lastEntryPrice)
                    {
                        return false;
                    }
                }
            }

            if (_currentGrid == null)
            {
                decimal z = _atrDev.LastValue;
                decimal ema = _ema.DataSeries[0].Last;
                bool signal = z > _zEnterBaseShort.ValueDecimal && (_emaReverseLogic.ValueBool ? (currentPrice > ema) : (currentPrice < ema));
                if (signal)
                {
                    _volumeManager.Clear();
                    _logDecoration.LogDebug(
                    $"SHORT check | " +
                    $"Time: {lastCandle.TimeStart:dd.MM.yyyy HH:mm:ss} | " +
                    $"Close: {currentPrice:F3} | " +
                    $"EMA: {ema:F3} | " +
                    $"Z: {z:F3} | " +
                    $"zEnterBaseShort: {_zEnterBaseShort.ValueDecimal:F3} | " +
                    $"Price < EMA: {currentPrice < ema} | " +
                    $"Z > zEnterBaseShort: {z > _zEnterBaseShort.ValueDecimal} | " +
                    $"emaReverseLogic: {_emaReverseLogic.ValueBool} | " +
                    $"Signal: {signal}");
                    return true;
                }
            }
            else
            {
                try
                {
                    Dictionary<int, decimal> grid = _currentGrid.GetGrid();
                    Dictionary<int, Position> positions = _currentGrid.GetPositions();

                    var emptyLevels = grid.Where(p => !positions.ContainsKey(p.Key)).ToList();
                    var candidates = emptyLevels.Where(p => currentPrice > p.Value).ToList();

                    if (!candidates.Any())
                    {
                        return false;
                    }

                    var target = candidates.OrderByDescending(p => p.Value).First();
                    _nextGridKeyToFill = target.Key;
                    _logDecoration.LogDebug(
                    $"SHORT fills grid | " +
                    $"Time: {lastCandle.TimeStart:dd.MM.yyyy HH:mm:ss} | " +
                    $"Close: {currentPrice:F3} | " +
                    $"emptyLevels.Count: {emptyLevels.Count:F3} | " +
                    $"zEnterBaseShort: {_zEnterBaseShort.ValueDecimal:F3} | " +
                    $"nextGridKeyToFill: {_nextGridKeyToFill} | " +
                    $"emaReverseLogic: {_emaReverseLogic.ValueBool} | ");
                    return true;
                }
                catch (Exception ex)
                {
                    SendNewLogMessage(ex.Message, Logging.LogMessageType.Error);
                    return false;
                }
            }

            return false;
        }

        private void ApplySmaFilter(List<Candle> candles)
        {
            if (_currentGrid == null) return;

            Candle last = candles.Last();
            decimal smaValue = _sma.DataSeries[0].Last;

            bool allowNewEntries = false;

            if (_currentGrid.Direction == Side.Buy && last.Close < smaValue)
            {
                allowNewEntries = true; // лонг: цена ниже SMA — можно добавлять
            }
            else if (_currentGrid.Direction == Side.Sell && last.Close > smaValue)
            {
                allowNewEntries = true; // шорт: цена выше SMA — можно добавлять
            }

            if (!allowNewEntries)
            {
                Dictionary<int, decimal> grid = _currentGrid.GetGrid();
                Dictionary<int, Position> positions = _currentGrid.GetPositions();

                List<int> emptyKeys = grid.Keys.Where(k => !positions.ContainsKey(k)).ToList();

                foreach (int key in emptyKeys)
                {
                    _currentGrid.DeleteByKey(key);
                }
            }
        }

        protected override List<Func<List<Candle>, bool>> GetCheckers()
        {
            return new List<Func<List<Candle>, bool>>();
        }

        protected override decimal GetVolume(bool getRounded = true)
        {
            return _volumeManager.GetNextVolume(getRounded);
        }

        protected override void ParametersChangedByUser()
        {
            SetAtrDevParameters();
            SetSmaParameters();
            SetEmaParameters();
            SetVolumeManager();
        }

        private void SetAtrDevParameters()
        {
            if (_atrDev == null || _atrMultDev == null) return;
            _atrDev.AtrMultDev = _atrMultDev.ValueDecimal;
        }

        private void SetSmaParameters()
        {
            if (_smaLength == null || _sma == null) return;
            if (_sma?.Parameters[0] is IndicatorParameterInt parameter)
            {
                parameter.ValueInt = _smaLength.ValueInt;
            }
        }

        private void SetEmaParameters()
        {
            if (_emaLength == null || _ema == null) return;
            if (_ema?.Parameters[0] is IndicatorParameterInt parameter)
            {
                parameter.ValueInt = _emaLength.ValueInt;
            }
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

        private bool CanEnterPositionByEma(decimal price, Side side)
        {
            if (_ema == null || _ema.DataSeries == null || _ema.DataSeries[0].Values == null || _ema.DataSeries[0].Values.Count == 0) return true;
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

        private decimal GetRounded(decimal volume)
        {
            return GetRoundedVolume(_tab, volume);
        }
    }
}