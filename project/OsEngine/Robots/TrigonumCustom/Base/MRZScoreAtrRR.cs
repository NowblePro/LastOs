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
        private StrategyParameterDecimal _atrMultSpread;
        private StrategyParameterBool _debugLogging;

        // Grid
        private MeanReverseGrid _currentGrid = null;
        private int _nextGridKeyToFill = -1;

        // Значение AtrDev при первой входной позиции
        private decimal _lastAtrDevAtEntry = 0m;

        // Volatile stop
        private bool _volatileStopActive = false;
        private Side _volatileStopDirection = Side.Buy;
        private GridTypePosition _gridPositionType = GridTypePosition.None;

        // TP/SL и volume manager
        private TakeProfitDecoration _takeProfit;
        private StrategyParameterDecimal _atrTpMultiplier;
        private StopLossDecoration _stopLoss;
        private StrategyParameterDecimal _atrSlMultiplier;
        private StrategyParameterDecimal _stopLossLimitPercent;

        private StrategyParameterDecimal _r;
        private MeanReverseVolumeManager _volumeManager;

        public MRZScoreAtrRR(string name, StartProgram startProgram) : base(name, startProgram)
        {
            _multiplePosition = true;
            _tab.TPSLMode = TPSLMode.Partial;

            // Параметры
            _periodSma = CreateParameter("Sma Period", 50, 10, 500, 10, "Robot");
            _zEnterBase = CreateParameter("Z Enter Base", 0.2m, 0.01m, 5m, 0.01m, "Robot");
            _gridSize = CreateParameter("Grid Size", 7, 3, 20, 1, "Robot");
            _spread = CreateParameter("AtrDev Spread", 0.2m, 0.01m, 5m, 0.01m, "Robot");
            _atrMultDev = CreateParameter("Atr Mult Dev", 1m, 0.1m, 5m, 0.1m, "AtrDev");
            _atrMultSpread = CreateParameter("Atr Mult Spread", 1m, 0.1m, 5m, 0.1m, "Robot");
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
            // Попытка установить OnlyPositive (если свойство есть)
            try
            {
                _atrDev.OnlyPositive = true;
            }
            catch { }

            // TP/SL
            _takeProfit = new TakeProfitDecoration(this, false, "ATR TP Enable", "ATR");
            _atrTpMultiplier = CreateParameter("ATR TP Multiplier", 1m, 0.5m, 5m, 0.5m, "ATR");
            _takeProfit.ActivationPriceFunc = GetTakeProfit;

            _stopLossLimitPercent = CreateParameter("Stop Loss Limit Percent", 1m, 1m, 10m, 1m, "ATR");
            _stopLoss = new StopLossDecoration(this, false, "ATR SL Enable", "ATR");
            _atrSlMultiplier = CreateParameter("ATR SL Multiplier", 1m, 0.5m, 5m, 0.5m, "ATR");
            _stopLoss.StopPriceFunc = GetStopLoss;

            // Volume manager
            _r = CreateParameter("R, %", 1m, 1m, 15m, 1m, "Volume Manager");
            _volumeManager = new MeanReverseVolumeManager();
            _volumeManager.GetVolumeFunc = base.GetVolume;
            _volumeManager.Rounding = GetRounded;

            // События
            _tab.PositionOpeningSuccesEvent += _tab_PositionOpeningSuccesEvent;

            // Volatile stop
            new VolatileStopDecoration(this, VolatileStopHandler);

            ParametersChangedByUser();
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
            if (_currentGrid == null)
            {
                decimal atr = _atr.CurrentAtr;
                decimal centerPrice = obj.EntryPrice;
                decimal step = _spread.ValueDecimal + atr * _atrMultSpread.ValueDecimal;
                _currentGrid = new MeanReverseGrid(centerPrice, step, _gridSize.ValueInt, obj.Direction, _tab.GetChartMaster().Candles.Count - 1);
                _currentGrid.SetPosition(0, obj);

                // Запоминаем значение AtrDev при первой позиции
                _lastAtrDevAtEntry = _atrDev.LastValue;
                _nextGridKeyToFill = -1;
                _gridPositionType = obj.Direction == Side.Buy ? GridTypePosition.Low : GridTypePosition.High;
                _volumeManager.Clear();

                LogDebug($"Grid created dir={obj.Direction} center={centerPrice:F8} step={step:F8} atrDevAtEntry={_lastAtrDevAtEntry:F8}");
            }
            else
            {
                try
                {
                    if (obj.Direction != _currentGrid.Direction) return;

                    Dictionary<int, decimal> grid = _currentGrid.GetGrid();
                    Dictionary<int, Position> positions = _currentGrid.GetPositions();

                    if (_nextGridKeyToFill != -1 && grid.ContainsKey(_nextGridKeyToFill) && !positions.ContainsKey(_nextGridKeyToFill))
                    {
                        _currentGrid.SetPosition(_nextGridKeyToFill, obj);

                        LogDebug($"Position set to grid key {_nextGridKeyToFill} price={grid[_nextGridKeyToFill]:F8} dir={obj.Direction}");

                        // Удаляем "пропущенные" менее экстремальные уровни
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
                        else
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

                        if (keysToDelete.Count > 0)
                        {
                            LogDebug($"Deleting non-extreme keys after fill: {string.Join(",", keysToDelete)}");
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
            // Вызов базовой обработки
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
                        LogDebug("All positions closed - resetting grid");
                        _currentGrid = null;
                        _nextGridKeyToFill = -1;
                        _gridPositionType = GridTypePosition.None;
                        _volumeManager.Clear();
                    }
                    else
                    {
                        // Проверяем открытия: отменяем ордера открытия, если текущая цена не проходит SMA-фильтр
                        List<int> keysToDelete = new List<int>();
                        foreach (KeyValuePair<int, Position> pair in _currentGrid.GetPositions())
                        {
                            int key = pair.Key;
                            Position pos = pair.Value;
                            if (pos == null) continue;

                            decimal currentPrice = candles.Last().Close;
                            if (!CanEnterPositionBySma(currentPrice, pos.Direction))
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

                        if (keysToDelete.Count > 0)
                        {
                            LogDebug($"CandleFinished: cancelling opening orders and deleting keys: {string.Join(",", keysToDelete)}");
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
                                LogDebug("No open or opening positions left - clearing grid");
                                _currentGrid = null;
                                _nextGridKeyToFill = -1;
                                _gridPositionType = GridTypePosition.None;
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
            decimal price = position.EntryPrice;
            if (_atr == null) return price;
            switch (position.Direction)
            {
                case Side.Buy:
                    return price + _atr.CurrentAtr * _atrTpMultiplier.ValueDecimal;
                case Side.Sell:
                    return price - _atr.CurrentAtr * _atrTpMultiplier.ValueDecimal;
                default:
                    return price;
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
                    _volatileStopDirection = _currentGrid.Direction;
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
            if (_currentGrid != null && _currentGrid.Direction == Side.Sell) return false;

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
                        LogDebug($"Long first-entry check: price={price:F8} sma={smaValue:F8} zLow={z:F8} threshold={_zEnterBase.ValueDecimal:F8}");
                        LogDebug("Long first-entry condition satisfied -> returning true");
                        return true;
                    }
                }

                return false;
            }
            else
            {
                // Добавления по гриду: требуются условие по цене (price < level) и по AtrDev
                try
                {
                    Dictionary<int, decimal> grid = _currentGrid.GetGrid();
                    Dictionary<int, Position> positions = _currentGrid.GetPositions();

                    var emptyLevels = grid.Where(p => !positions.ContainsKey(p.Key)).ToList();
                    decimal currAtrDev = _atrDev.LastValue;

                    var priceCandidates = emptyLevels.Where(p => currentPrice < p.Value).ToList();
                    if (!priceCandidates.Any()) return false;

                    // формируем кандидатов по AtrDev (требуем currAtrDev >= _lastAtrDevAtEntry + spread * levelIndex)
                    var atrCandidates = new List<KeyValuePair<int, decimal>>();
                    foreach (var lvl in priceCandidates)
                    {
                        decimal required = _lastAtrDevAtEntry + _spread.ValueDecimal * lvl.Key;
                        if (currAtrDev >= required)
                        {
                            atrCandidates.Add(new KeyValuePair<int, decimal>(lvl.Key, required));
                        }
                    }

                    LogDebug($"Long grid: priceCandidates={priceCandidates.Count} atrCandidates={atrCandidates.Count} currAtrDev={currAtrDev:F8} lastAtrDevAtEntry={_lastAtrDevAtEntry:F8}");

                    if (!atrCandidates.Any())
                    {
                        // доп. правило: если price опустилась ниже предыдущего входа — разрешаем вход
                        var openPositions = _currentGrid.GetPositions().Where(p => p.Value != null && p.Value.State == PositionStateType.Open).Select(p => p.Value).ToList();
                        if (openPositions.Any())
                        {
                            decimal lastEntryPrice = openPositions.OrderBy(p => p.EntryPrice).Last().EntryPrice;
                            if (currentPrice < lastEntryPrice)
                            {
                                var target = priceCandidates.OrderBy(p => p.Value).First();
                                _nextGridKeyToFill = target.Key;
                                LogDebug($"Long fallback by price below last entry: _nextGridKeyToFill={_nextGridKeyToFill}");
                                return true;
                            }
                        }

                        return false;
                    }

                    decimal delta = currAtrDev - _lastAtrDevAtEntry;
                    if (delta >= _spread.ValueDecimal * 2m)
                    {
                        int maxIdx = atrCandidates.Max(a => a.Key);
                        _nextGridKeyToFill = maxIdx;
                        LogDebug($"Long large-delta pick maxIdx={maxIdx} delta={delta:F8}");
                        return true;
                    }
                    else
                    {
                        int idx = atrCandidates.Min(a => a.Key);
                        _nextGridKeyToFill = idx;
                        LogDebug($"Long normal pick idx={idx} delta={delta:F8}");
                        return true;
                    }
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

            if (_currentGrid != null && _currentGrid.Direction == Side.Buy) return false;

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
                        LogDebug($"Short first-entry check: price={price:F8} sma={smaValue:F8} zHigh={z:F8} threshold={_zEnterBase.ValueDecimal:F8}");
                        LogDebug("Short first-entry condition satisfied -> returning true");
                        return true;
                    }
                }

                return false;
            }
            else
            {
                // Добавления по гриду: цена > level и atrDev пороги
                try
                {
                    Dictionary<int, decimal> grid = _currentGrid.GetGrid();
                    Dictionary<int, Position> positions = _currentGrid.GetPositions();

                    var emptyLevels = grid.Where(p => !positions.ContainsKey(p.Key)).ToList();
                    decimal currAtrDev = _atrDev.LastValue;

                    var priceCandidates = emptyLevels.Where(p => currentPrice > p.Value).ToList();
                    if (!priceCandidates.Any()) return false;

                    var atrCandidates = new List<KeyValuePair<int, decimal>>();
                    foreach (var lvl in priceCandidates)
                    {
                        decimal required = _lastAtrDevAtEntry + _spread.ValueDecimal * lvl.Key;
                        if (currAtrDev >= required)
                        {
                            atrCandidates.Add(new KeyValuePair<int, decimal>(lvl.Key, required));
                        }
                    }

                    LogDebug($"Short grid: priceCandidates={priceCandidates.Count} atrCandidates={atrCandidates.Count} currAtrDev={currAtrDev:F8} lastAtrDevAtEntry={_lastAtrDevAtEntry:F8}");

                    if (!atrCandidates.Any())
                    {
                        // доп. правило: если price выше предыдущего входа => вход
                        var openPositions = _currentGrid.GetPositions().Where(p => p.Value != null && p.Value.State == PositionStateType.Open).Select(p => p.Value).ToList();
                        if (openPositions.Any())
                        {
                            decimal lastEntryPrice = openPositions.OrderByDescending(p => p.EntryPrice).Last().EntryPrice;
                            if (currentPrice > lastEntryPrice)
                            {
                                var target = priceCandidates.OrderByDescending(p => p.Value).First();
                                _nextGridKeyToFill = target.Key;
                                LogDebug($"Short fallback by price above last entry: _nextGridKeyToFill={_nextGridKeyToFill}");
                                return true;
                            }
                        }

                        return false;
                    }

                    decimal delta = currAtrDev - _lastAtrDevAtEntry;
                    if (delta >= _spread.ValueDecimal * 2m)
                    {
                        int maxIdx = atrCandidates.Max(a => a.Key);
                        _nextGridKeyToFill = maxIdx;
                        LogDebug($"Short large-delta pick maxIdx={maxIdx} delta={delta:F8}");
                        return true;
                    }
                    else
                    {
                        int idx = atrCandidates.Max(a => a.Key);
                        _nextGridKeyToFill = idx;
                        LogDebug($"Short normal pick idx={idx} delta={delta:F8}");
                        return true;
                    }
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
                candles => _periodSma.ValueInt < candles.Count
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