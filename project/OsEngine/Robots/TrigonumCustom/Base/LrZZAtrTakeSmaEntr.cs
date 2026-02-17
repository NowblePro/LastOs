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
using OsEngine.Charts.CandleChart.Indicators;

namespace OsEngine.Robots.TrigonumCustom.Base
{
    [Bot("LrZZAtrTakeSmaEntr")]
    public class LrZZAtrTakeSmaEntr : BotPanelSimple
    {
        // Индикаторы
        private ZScoreLow _zScoreLow;
        private ZScoreHigh _zScoreHigh;
        private ZScoreChannel _channel;
        private AtrDecoration _atr; // для ATR TP/SL
        private DDR _ddr;
        private LinearRegression _lr;
        private Aindicator _sma;

        // Параметры
        private StrategyParameterInt _periodCentralLine;
        private StrategyParameterDecimal _zEnterBase;
        private StrategyParameterInt _gridSize;
        private StrategyParameterDecimal _spread; // шаг по AtrDev для определения уровней
        private StrategyParameterBool _debugLogging;
        private StrategyParameterString _centralLine;
        private StrategyParameterBool _enterFirstPosition;

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

        // DDR
        private DDRDecoration _ddrDecoration;

        // TakeSma
        private TakeSmaDecoration _takeSma;

        // Условия входа для лонга (для шорта зеркально):
        // 1. Нижний канал пересекает свечу между Low и High.
        // 2. Свеча лонговая (зелёная).
        // 3. Свеча импульсная (хвосты маленькие, тело большое).
        // 4. DDR меньше порога.
        // 5. На каждый левел 1 вход. (стандартно для всех)
        // 6. Последующие входы должны быть ниже по цене чем предыдущие.

        // Особенности:
        // 1. Усреднение по z-score каналу
        // 2. "Недоход до SMA" рассчитывается только во время входа (тэйк не следует за SMA)
        // 3. В роли центральной линии может выступать не только SMA но и линейная регрессия, переключается параметром Central Line

        public LrZZAtrTakeSmaEntr(string name, StartProgram startProgram) : base(name, startProgram)
        {
            _multiplePosition = true;
            _tab.TPSLMode = TPSLMode.Partial;

            // Параметры
            _periodCentralLine = CreateParameter("Central Line Period", 50, 10, 500, 10, "Robot");
            _zEnterBase = CreateParameter("Z Enter Base", 3m, 1m, 5m, 0.5m, "Robot");
            _gridSize = CreateParameter("Grid Size", 7, 3, 20, 1, "Robot");
            _spread = CreateParameter("Spread", 0.2m, 0.01m, 5m, 0.01m, "Robot");
            _debugLogging = CreateParameter("Debug Logging", false, "Debug");
            string[] centralLineTypes = Enum.GetNames(typeof(CentralLineType));
            _centralLine = CreateParameter("Central Line", CentralLineType.LR.ToString(), centralLineTypes, "Robot");
            _enterFirstPosition = CreateParameter("Enter First Position", true, "Robot");

            _lr = (LinearRegression)IndicatorsFactory.CreateIndicatorByName(nameClass: "LinearRegression", name: name + "LR", canDelete: false);
            _lr = (LinearRegression)_tab.CreateCandleIndicator(_lr, nameArea: "Prime");
            _lr.Save();

            _sma = IndicatorsFactory.CreateIndicatorByName("Sma", name + "Sma", false);
            _sma = (Aindicator)_tab.CreateCandleIndicator(_sma, "Prime");

            // ZScoreLow / ZScoreHigh / Channel
            _zScoreLow = (ZScoreLow)IndicatorsFactory.CreateIndicatorByName(nameClass: "ZScoreLow", name: name + "ZScoreLow", canDelete: false);
            _zScoreLow.PaintSeries = false;
            _zScoreLow = (ZScoreLow)_tab.CreateCandleIndicator(_zScoreLow, nameArea: "ZScoreLow");
            _zScoreLow.Save();
            
            _zScoreHigh = (ZScoreHigh)IndicatorsFactory.CreateIndicatorByName(nameClass: "ZScoreHigh", name: name + "ZScoreHigh", canDelete: false);
            _zScoreHigh.PaintSeries = false;
            _zScoreHigh = (ZScoreHigh)_tab.CreateCandleIndicator(_zScoreHigh, nameArea: "ZScoreHigh");
            _zScoreHigh.Save();
            
            _channel = (ZScoreChannel)IndicatorsFactory.CreateIndicatorByName(nameClass: "ZScoreChannel", name: name + "ZScoreChannel", canDelete: false);
            _channel = (ZScoreChannel)_tab.CreateCandleIndicator(_channel, nameArea: "Prime");
            _channel.ZScoreReference = _zEnterBase.ValueDecimal;
            _channel.LowZScore = _zScoreLow;
            _channel.HighZScore = _zScoreHigh;
            _channel.Save();

            _ddr = (DDR)IndicatorsFactory.CreateIndicatorByName(nameClass: "DDR", name: name + "DDR", canDelete: false);
            _ddr = (DDR)_tab.CreateCandleIndicator(_ddr, nameArea: "DDR");

            _ddrDecoration = new DDRDecoration(this, _ddr);

            // ATR decoration
            _atr = new AtrDecoration(this, true);
            _atr.CancelTPSL = false;

            // TP/SL
            _takeProfit = new TakeProfitDecoration(this, false, "ATR TP Enable", "ATR");
            _takeProfit.ActivationPriceFunc = GetTakeProfit;
            _rr = CreateParameter("Take RR", 3m, 0.5m, 10m, 0.5m, "ATR");

            _stopLossLimitPercent = CreateParameter("Stop Loss Limit Percent", 1m, 1m, 10m, 1m, "ATR");
            _stopLoss = new StopLossDecoration(this, false, "ATR SL Enable", "ATR");
            _atrSlMultiplier = CreateParameter("ATR SL Multiplier", 1m, 0.5m, 5m, 0.5m, "ATR");
            _stopLoss.StopPriceFunc = GetStopLoss;
            _stopLoss.StopPriceFuncIfDisabled = GetStopLossIfDisabled;

            // Volume manager
            _r = CreateParameter("R, %", 1m, 1m, 15m, 1m, "Volume Manager");
            _volumeManager = new MeanReverseVolumeManager();
            _volumeManager.GetVolumeFunc = base.GetVolume;
            _volumeManager.Rounding = GetRounded;

            // FairPrice Decoration
            _fairPrice = new FairPriceDecoration(this, "FairPrice");

            // TakeSma Decoration
            _takeSma = new TakeSmaDecoration(this, "TakeSma");

            // События
            _tab.PositionOpeningSuccesEvent += _tab_PositionOpeningSuccesEvent;
            _tab.PositionClosingSuccesEvent += _tab_PositionClosingSuccesEvent;

            // Volatile stop
            new VolatileStopDecoration(this, VolatileStopHandler);

            ParametersChangedByUser();
        }

        private Aindicator CentralLineIndicator
        {
            get
            {
                Aindicator result = null;
                CentralLineType centralType = (CentralLineType)Enum.Parse(typeof(CentralLineType), _centralLine.ValueString);
                switch (centralType)
                {
                    case CentralLineType.Sma:
                        result = _sma;
                        break;
                    case CentralLineType.LR:
                        result = _lr;
                        break;
                }
                return result;
            }
        }

        private decimal Spread
        {
            get
            {
                decimal result = _spread.ValueDecimal;
                //_ddrDecoration.ChangeStep(ref result);
                return result;
            }
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
            decimal gridValue = obj.Direction == Side.Buy ? _zScoreLow.LastValue : -_zScoreHigh.LastValue;
            if (_currentGrid == null)
            {
                decimal step = Spread;
                _currentGrid = new MeanReverseGrid(gridValue, step, _gridSize.ValueInt, Side.Sell, _tab.GetChartMaster().Candles.Count - 1);
                _currentGrid.SetPosition(0, obj);
                _volumeManager.Clear();

                if (_debugLogging != null && _debugLogging.ValueBool)
                {
                    StringBuilder sb = new StringBuilder();
                    foreach (var level in _currentGrid.GetGrid())
                    {
                        sb.Append($"[{level.Key};{level.Value:F3}] ");
                    }
                    LogDebug($"Grid created dir={obj.Direction} center={gridValue:F3} step={step:F3} levels={sb}");
                }
            }
            else
            {
                try
                {
                    Dictionary<int, decimal> grid = _currentGrid.GetGrid();
                    Dictionary<int, Position> positions = _currentGrid.GetPositions();

                    // Уровни которые меньше текущего atrDev и не занятые позициями
                    var goodLevels = grid.Where(l => l.Value <= gridValue).Where(l => !positions.Keys.Contains(l.Key));

                    if (!goodLevels.Any())
                    {
                        return;
                    }

                    var maxValue = goodLevels.Max(l => l.Value);
                    var maxLevel = goodLevels.Where(l => l.Value == maxValue).FirstOrDefault();
                    _currentGrid.SetPosition(maxLevel.Key, obj);
                    LogDebug($"Позиция присвоена уровню с индексом {maxLevel.Key}, AtrDev уровня = {maxValue:F6}");
                    var otherLevels = goodLevels.Except(new List<KeyValuePair<int, decimal>>() { maxLevel }).ToList();
                    foreach (var level in otherLevels)
                    {
                        LogDebug($"Пустой уровень с индексом {level.Key} и значением {level.Value} удалён");
                        _currentGrid.DeleteByKey(level.Key);
                    }
                    StringBuilder sb = new StringBuilder();
                    foreach (var position in positions)
                    {
                        sb.Append($"| [{position.Key}] {position.Value.EntryPrice:F3} | ");
                    }
                    if (_tab.PositionsAll.Any())
                    {
                        LogDebug($"В гриде на данный момент позиции с ценами входа {sb}");
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
                _ddrDecoration.Activate(false);
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
                    if (_tab.PositionsOpenAll.Count == 0 && (!canEnterPositionBySma && !positions.Where(p => p.Value.State == PositionStateType.Open || p.Value.State == PositionStateType.Opening).Any()))
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
                        _ddrDecoration.Activate(false);
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
            decimal stopLossPrice = _stopLoss.On ? GetStopLoss(position) : GetStopLossForTakeProfitIfDisabled(position);
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

        private decimal GetStopLossIfDisabled(Position position)
        {
            decimal stopPrice = 0;
            if (position.Direction == Side.Buy)
            {
                stopPrice = position.EntryPrice - position.EntryPrice * (_stopLossLimitPercent.ValueDecimal / 100);
            }
            else if (position.Direction == Side.Sell)
            {
                stopPrice = position.EntryPrice + position.EntryPrice * (_stopLossLimitPercent.ValueDecimal / 100);
            }
            return stopPrice;
        }

        private decimal GetStopLossForTakeProfitIfDisabled(Position position)
        {
            decimal stopPrice = 0;
            if (position.Direction == Side.Buy)
            {
                stopPrice = position.EntryPrice - (_atrSlMultiplier.ValueDecimal * _atr.CurrentAtr);
            }
            else if (position.Direction == Side.Sell)
            {
                stopPrice = position.EntryPrice + (_atrSlMultiplier.ValueDecimal * _atr.CurrentAtr);
            }
            return stopPrice;
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

            if (_ddrDecoration.Activated)
            {
                return false;
            }

            // Свеча должна быть "зелёная"
            if (last.Close < last.Open)
            {
                return false;
            }

            // Свеча должна быть импульсная (тело свечи должно быть минимум 60%)
            if ((Math.Abs(last.Open - last.Close) / (last.High - last.Low)) < 0.6m)
            {
                return false;
            }

            // Снятие блокировки volatile stop по Buy
            if (_volatileStopActive && _volatileStopDirection == Side.Buy)
            {
                decimal sma = CentralLineIndicator.DataSeries[0].Last;
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

            decimal smaValue = CentralLineIndicator.DataSeries[0].Last;
            decimal currentPrice = last.Close;

            // Первая позиция: ZScore используется только при отсутствии грида
            if (_currentGrid == null)
            {
                decimal z = _zScoreLow.LastValue;
                decimal channelValue = _channel.ChannelDataLowLast;
                if (z >= _zEnterBase.ValueDecimal && channelValue > last.Low && channelValue < last.High)
                {
                    _volumeManager.Clear();
                    _gridDirection = Side.Buy;

                    if (!_enterFirstPosition.ValueBool)
                    {
                        // Строим грид, а впозицию не входим
                        decimal atrDev = z;
                        decimal step = Spread;
                        _currentGrid = new MeanReverseGrid(atrDev + step, step, _gridSize.ValueInt, Side.Sell, _tab.GetChartMaster().Candles.Count - 1);
                        _volumeManager.Clear();
                        if (_debugLogging != null && _debugLogging.ValueBool)
                        {
                            StringBuilder sb = new StringBuilder();
                            foreach (var level in _currentGrid.GetGrid())
                            {
                                sb.Append($"[{level.Key};{level.Value:F3}] ");
                            }
                            LogDebug($"Grid created dir={_gridDirection} center={(atrDev + step):F3} step={step:F3} levels={sb}");
                        }
                        return false;
                    }

                    LogDebug($"Long first-entry check: price={last.Close:F3} sma={smaValue:F3} zLow={z:F3} threshold={_zEnterBase.ValueDecimal:F3}");
                    LogDebug("Long first-entry condition satisfied -> returning true");
                    return true;
                }

                return false;
            }
            else
            {
                if (_gridDirection == Side.Sell) return false;
                try
                {
                    decimal gridValue = _zScoreLow.LastValue;
                    Dictionary<int, decimal> grid = _currentGrid.GetGrid();
                    Dictionary<int, Position> positions = _currentGrid.GetPositions();

                    var emptyLevels = grid.Where(p => !positions.ContainsKey(p.Key)).ToList();

                    var gridCandidates = emptyLevels.Where(p => gridValue >= p.Value).ToList();
                    if (!gridCandidates.Any()) return false;
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

            if (_ddrDecoration.Activated)
            {
                return false;
            }

            // Свеча должна быть "красная"
            if (last.Close > last.Open)
            {
                return false;
            }

            // Свеча должна быть импульсная (тело свечи должно быть минимум 60%)
            if ((Math.Abs(last.Open - last.Close) / (last.High - last.Low)) < 0.6m)
            {
                return false;
            }

            // Снятие блокировки volatile stop по Sell
            if (_volatileStopActive && _volatileStopDirection == Side.Sell)
            {
                decimal sma = CentralLineIndicator.DataSeries[0].Last;
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

            decimal smaValue = CentralLineIndicator.DataSeries[0].Last;
            decimal currentPrice = last.Close;

            if (_currentGrid == null)
            {
                decimal z = -_zScoreHigh.LastValue;
                decimal channelValue = _channel.ChannelDataHighLast;
                if (z >= _zEnterBase.ValueDecimal && channelValue > last.Low && channelValue < last.High)
                {
                    _volumeManager.Clear();
                    _gridDirection = Side.Sell;

                    if (!_enterFirstPosition.ValueBool)
                    {
                        // Строим грид, а впозицию не входим
                        decimal atrDev = z;
                        decimal step = Spread;
                        _currentGrid = new MeanReverseGrid(atrDev + step, step, _gridSize.ValueInt, Side.Sell, _tab.GetChartMaster().Candles.Count - 1);
                        _volumeManager.Clear();
                        if (_debugLogging != null && _debugLogging.ValueBool)
                        {
                            StringBuilder sb = new StringBuilder();
                            foreach (var level in _currentGrid.GetGrid())
                            {
                                sb.Append($"[{level.Key};{level.Value:F3}] ");
                            }
                            LogDebug($"Grid created dir={_gridDirection} center={(atrDev + step):F3} step={step:F3} levels={sb}");
                        }
                        return false;
                    }

                    LogDebug($"Short first-entry check: price={last.Close:F3} sma={smaValue:F3} zHigh={z:F3} threshold={_zEnterBase.ValueDecimal:F3}");
                    LogDebug("Short first-entry condition satisfied -> returning true");
                    return true;
                }

                return false;
            }
            else
            {
                if (_gridDirection == Side.Buy) return false;
                try
                {
                    decimal gridValue = -_zScoreHigh.LastValue;
                    Dictionary<int, decimal> grid = _currentGrid.GetGrid();
                    Dictionary<int, Position> positions = _currentGrid.GetPositions();

                    var emptyLevels = grid.Where(p => !positions.ContainsKey(p.Key)).ToList();

                    var gridCandidates = emptyLevels.Where(p => gridValue >= p.Value).ToList();
                    if (!gridCandidates.Any()) return false;
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
                candles => _periodCentralLine.ValueInt < candles.Count,
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
            SetChannelParameters();
            SetVolumeManager();
            SetZScoreChannelReference();
            SetCentralLineIndicator();
        }

        private void SetZScoreChannelReference()
        {
            if (_channel == null || _zEnterBase == null) return;
            _channel.ZScoreReference = _zEnterBase.ValueDecimal;
        }

        private void SetSmaParameters()
        {
            if (_lr == null || _periodCentralLine == null || _sma == null) return;
            _lr.N = _periodCentralLine.ValueInt;
            if (_sma?.Parameters[0] is IndicatorParameterInt parameter)
            {
                parameter.ValueInt = _periodCentralLine.ValueInt;
            }
        }

        private void SetCentralLineIndicator()
        {
            if (_centralLine == null ||
                _zScoreLow == null ||
                _zScoreHigh == null ||
                _fairPrice == null ||
                _takeSma == null ||
                CentralLineIndicator == null)
            {
                return;
            }
            _zScoreLow.SMA = CentralLineIndicator;
            _zScoreHigh.SMA = CentralLineIndicator;
            _fairPrice.SetSma(CentralLineIndicator);
            _takeSma.SetSma(CentralLineIndicator);
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
            if (CentralLineIndicator == null || CentralLineIndicator.DataSeries == null || CentralLineIndicator.DataSeries[0].Values == null || CentralLineIndicator.DataSeries[0].Values.Count == 0) return true;
            decimal sma = CentralLineIndicator.DataSeries[0].Last;
            if (side == Side.Buy)
            {
                return price < sma; // для лонга цена должна быть ниже SMA
            }
            else
            {
                return price > sma; // для шорта — выше SMA
            }
        }

        enum CentralLineType
        {
            Sma, LR
        }
    }
}