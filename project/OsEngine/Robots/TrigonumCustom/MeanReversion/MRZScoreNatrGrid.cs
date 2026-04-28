using OsEngine.Common;
using OsEngine.Entity;
using OsEngine.Indicators;
using OsEngine.Indicators.TrigonumCustom;
using OsEngine.Logging;
using OsEngine.OsTrader.Panels.Attributes;
using OsEngine.OsTrader.Panels.Tab;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace OsEngine.Robots.TrigonumCustom.Base
{
    [Bot("MRZScoreNatrGrid")]
    public class MRZScoreNatrGrid : BotPanelSimple
    {
        private class GridLevelState
        {
            public int Index;
            public decimal DeviationPercent;
            public decimal Price;
            public decimal Volume;
            public Position Position;
            public bool Consumed;
            public bool WaitingForReplacement;
            public bool CancelRequested;
        }

        private Aindicator _sma;
        private Aindicator _ema;
        private Aindicator _natrAtr;
        private DDR _ddr;
        private ZScoreLow _zScoreLow;
        private ZScoreHigh _zScoreHigh;
        private ZScoreChannel _channel;
        private AtrDecoration _atrStop;

        private StrategyParameterInt _periodSma;
        private StrategyParameterInt _emaLength;
        private StrategyParameterDecimal _zEnterBase;
        private StrategyParameterDecimal _fixPercent;
        private StrategyParameterInt _gridSize;
        private StrategyParameterInt _natrLength;
        private StrategyParameterDecimal _natrMult;
        private StrategyParameterBool _debugLogging;
        private StrategyParameterBool _emaStopEnable;
        private StrategyParameterBool _zScoreChannelTpEnable;

        private TakeProfitDecoration _takeProfit;
        private StopLossDecoration _stopLoss;
        private FairPriceDecoration _fairPrice;
        private StrategyParameterDecimal _atrSlMultiplier;
        private StrategyParameterDecimal _stopLossLimitPercent;
        private StrategyParameterDecimal _rr;

        private StrategyParameterDecimal _r;
        private MeanReverseVolumeManager _volumeManager;
        private DDRDecoration _ddrDecoration;
        private Change24Decoration _change24;
        private CanEnterByEmaDecoration _canEnterByEma;
        private bool _volatileStopActive;
        private Side _volatileStopDirection = Side.None;

        private readonly List<GridLevelState> _gridLevels = new List<GridLevelState>();
        private readonly Dictionary<Position, GridLevelState> _levelBindingsByReference = new Dictionary<Position, GridLevelState>();
        private readonly Dictionary<int, GridLevelState> _levelBindingsByNumber = new Dictionary<int, GridLevelState>();
        private readonly Dictionary<int, GridLevelState> _levelBindingsByOrderUserNumber = new Dictionary<int, GridLevelState>();
        private readonly Dictionary<string, GridLevelState> _levelBindingsByOrderMarketNumber = new Dictionary<string, GridLevelState>(StringComparer.OrdinalIgnoreCase);
        private readonly List<GridLevelState> _levelsAwaitingOpeningSuccess = new List<GridLevelState>();
        private Side _gridSide = Side.None;
        private decimal _gridSma;
        private decimal _gridThresholdPercent;
        private decimal _gridNatrPercent;
        private DateTime _gridSignalTime = DateTime.MinValue;
        private int _lastProcessedCandlesCount;
        private DateTime _lastProcessedCandleTime = DateTime.MinValue;
        private GridLevelState _levelAwaitingImmediateBinding;
        private bool _debugSessionHeaderLogged;

        public MRZScoreNatrGrid(string name, StartProgram startProgram) : base(name, startProgram)
        {
            _multiplePosition = true;
            _tab.TPSLMode = TPSLMode.Partial;

            _emaLength = CreateParameter("EMA period", 200, 100, 300, 1, "Ema Filter");
            _periodSma = CreateParameter("Sma Period", 50, 10, 500, 10, "Robot");
            _zEnterBase = CreateParameter("Z Enter Base", 2m, 0.5m, 6m, 0.1m, "Robot");
            _fixPercent = CreateParameter("Fix Percent", 2m, 0.1m, 10m, 0.1m, "Robot");
            _gridSize = CreateParameter("Grid Size", 7, 1, 20, 1, "Robot");
            _natrLength = CreateParameter("NATR Length", 14, 5, 200, 1, "Robot");
            _natrMult = CreateParameter("NATR Multiplier", 2m, 0m, 10m, 0.1m, "Robot");
            _debugLogging = GetOrCreateDebugLoggingParameter();
            _emaStopEnable = CreateParameter("EMA Stop Enable", false, "Ema Filter");
            _zScoreChannelTpEnable = CreateParameter("ZScore Channel TP Enable", false, "ATR");

            _sma = IndicatorsFactory.CreateIndicatorByName("Sma", name + "Sma", false);
            _sma = (Aindicator)_tab.CreateCandleIndicator(_sma, "Prime");
            _sma.Save();

            _ema = IndicatorsFactory.CreateIndicatorByName("Ema", name + "Ema", false);
            _ema = (Aindicator)_tab.CreateCandleIndicator(_ema, "Prime");
            _ema.Save();

            _zScoreLow = (ZScoreLow)IndicatorsFactory.CreateIndicatorByName("ZScoreLow", name + "ZScoreLow", false);
            _zScoreLow.PaintSeries = false;
            _zScoreLow = (ZScoreLow)_tab.CreateCandleIndicator(_zScoreLow, "ZScoreLow");
            _zScoreLow.DataSeries[0].Color = Color.Blue;
            _zScoreLow.SMA = _sma;
            _zScoreLow.Save();

            _zScoreHigh = (ZScoreHigh)IndicatorsFactory.CreateIndicatorByName("ZScoreHigh", name + "ZScoreHigh", false);
            _zScoreHigh.PaintSeries = false;
            _zScoreHigh = (ZScoreHigh)_tab.CreateCandleIndicator(_zScoreHigh, "ZScoreHigh");
            _zScoreHigh.DataSeries[0].Color = Color.Red;
            _zScoreHigh.SMA = _sma;
            _zScoreHigh.Save();

            _channel = (ZScoreChannel)IndicatorsFactory.CreateIndicatorByName("ZScoreChannel", name + "ZScoreChannel", false);
            _channel = (ZScoreChannel)_tab.CreateCandleIndicator(_channel, "Prime");
            _channel.LowZScore = _zScoreLow;
            _channel.HighZScore = _zScoreHigh;
            _channel.ZScoreReference = _zEnterBase.ValueDecimal;
            _channel.Save();

            _ddr = (DDR)IndicatorsFactory.CreateIndicatorByName("DDR", name + "DDR", false);
            _ddr = (DDR)_tab.CreateCandleIndicator(_ddr, "DDR");
            _ddrDecoration = new DDRDecoration(this, _ddr);
            _ddrDecoration.DDREvent += _ddrDecoration_DDREvent;

            _natrAtr = IndicatorsFactory.CreateIndicatorByName("ATR", name + "NatrAtr", false);
            _natrAtr = (Aindicator)_tab.CreateCandleIndicator(_natrAtr, "NATR");
            _natrAtr.DataSeries[0].IsPaint = false;
            _natrAtr.Save();

            _atrStop = new AtrDecoration(this, true);
            _atrStop.CancelTPSL = false;

            _takeProfit = new TakeProfitDecoration(this, false, "ATR TP Enable", "ATR");
            _takeProfit.ActivationPriceFunc = GetTakeProfit;
            _rr = CreateParameter("Take RR", 3m, 0.5m, 10m, 0.5m, "ATR");

            _stopLossLimitPercent = CreateParameter("Stop Loss Limit Percent", 1m, 0.1m, 10m, 0.1m, "ATR");
            _stopLoss = new StopLossDecoration(this, false, "ATR SL Enable", "ATR");
            _atrSlMultiplier = CreateParameter("ATR SL Multiplier", 1m, 0.5m, 5m, 0.5m, "ATR");
            _stopLoss.StopPriceFunc = GetAtrStopLoss;

            _r = CreateParameter("R, %", 1m, 0m, 15m, 0.1m, "Volume Manager");
            _volumeManager = new MeanReverseVolumeManager();
            _volumeManager.GetVolumeFunc = base.GetVolume;
            _volumeManager.Rounding = GetRounded;

            _fairPrice = new FairPriceDecoration(this, "FairPrice");
            _fairPrice.SetSma(_sma);

            new VolatileStopDecoration(this, VolatileStopHandler);
            _change24 = new Change24Decoration(this);
            _canEnterByEma = new CanEnterByEmaDecoration(this);
            _canEnterByEma.Ema = _ema;

            _tab.PositionOpeningSuccesEvent += _tab_PositionOpeningSuccesEvent;
            _tab.PositionOpeningFailEvent += _tab_PositionOpeningFailEvent;
            _tab.PositionClosingSuccesEvent += _tab_PositionClosingSuccesEvent;
            _tab.PositionStartOpeningSuccessEvent += _tab_PositionStartOpeningSuccessEvent;

            ParametersChangedByUser();
        }

        private void _tab_PositionStartOpeningSuccessEvent(Position position)
        {
            if (_levelAwaitingImmediateBinding == null || position == null)
            {
                return;
            }

            SetLevelPosition(_levelAwaitingImmediateBinding, position);
        }

        private void _tab_PositionOpeningSuccesEvent(Position position)
        {
            GridLevelState level = FindLevel(position);

            if (level == null)
            {
                level = FindAwaitingOpeningSuccessLevel(position);
            }

            if (level == null)
            {
                SendNewLogMessage($"MRZScoreNatrGrid: opening position #{position?.Number} is not bound to any grid level", LogMessageType.Error);
                return;
            }

            SetLevelPosition(level, position);
            ForgetAwaitingOpeningSuccess(level);
            level.Consumed = true;
            level.WaitingForReplacement = false;
            level.CancelRequested = false;
            decimal plannedPrice = GetPlannedEntryPrice(position, level.Price);
            DateTime? signalTime = GetPlannedEntrySignalTime(position);
            decimal actualEntry = position.EntryPrice;
            decimal entryDelta = actualEntry - plannedPrice;
            string signalLag = signalTime.HasValue && position.TimeOpen != DateTime.MinValue
                ? (position.TimeOpen - signalTime.Value).ToString()
                : "n/a";

            LogDebug(
                $"Level filled: side={position.Direction}, index={level.Index}, deviation={level.DeviationPercent:F4}%, " +
                $"plannedPrice={plannedPrice:F8}, actualEntry={actualEntry:F8}, delta={entryDelta:F8}, " +
                $"signalTime={FormatDateTime(signalTime)}, fillTime={FormatDateTime(position.TimeOpen)}, lag={signalLag}, " +
                $"volume={level.Volume:F8}, position={position.Number}, state={position.State}, signalOpen={position.SignalTypeOpen}");
            UpdateDynamicStop(position);
            UpdateDynamicProfit(position);
        }

        private void _tab_PositionOpeningFailEvent(Position position)
        {
            GridLevelState level = FindLevel(position);

            if (level == null)
            {
                return;
            }

            SetLevelPosition(level, null);
            ForgetAwaitingOpeningSuccess(level);
            level.CancelRequested = false;
            if (level.WaitingForReplacement && IsLimitOrderMode())
            {
                ReopenPendingLevel(level, "DDR replacement after cancel/fail");
            }
            LogDebug(
                $"Level opening failed/cancelled: index={level.Index}, deviation={level.DeviationPercent:F4}%, " +
                $"plannedPrice={GetPlannedEntryPrice(position, level.Price):F8}, signalTime={FormatDateTime(GetPlannedEntrySignalTime(position))}, " +
                $"position={position?.Number}, state={position?.State}");
        }

        private void _tab_PositionClosingSuccesEvent(Position position)
        {
            GridLevelState level = FindLevel(position);

            if (level == null)
            {
                return;
            }

            SetLevelPosition(level, null);
            ForgetAwaitingOpeningSuccess(level);
            level.Consumed = true;
            level.WaitingForReplacement = false;
            level.CancelRequested = false;
            LogDebug(
                $"Level position closed: index={level.Index}, deviation={level.DeviationPercent:F4}%, " +
                $"position={position.Number}, entry={position.EntryPrice:F8}, close={position.ClosePrice:F8}, volume={position.OpenVolume:F8}, " +
                $"pnl={position.ProfitPortfolioPunkt:F8}, timeOpen={FormatDateTime(position.TimeOpen)}, timeClose={FormatDateTime(position.TimeClose)}, " +
                $"signalClose={position.SignalTypeClose}");
        }

        private void _ddrDecoration_DDREvent(object sender, EventArgs e)
        {
            if (_gridSide == Side.None || !HasConsumedLevels())
            {
                return;
            }

            List<GridLevelState> levelsToRebuild = _gridLevels
                .Where(level => !level.Consumed &&
                                !level.CancelRequested &&
                                (IsLimitOrderMode() ||
                                 (level.Position == null &&
                                  !_levelsAwaitingOpeningSuccess.Contains(level))))
                .OrderBy(level => level.DeviationPercent)
                .ToList();

            if (!levelsToRebuild.Any())
            {
                return;
            }

            decimal stepPercent = GetStepPercent();
            decimal maxDeviation = _gridLevels
                .Where(level => level.Consumed)
                .Select(level => level.DeviationPercent)
                .DefaultIfEmpty(_gridLevels.Max(level => level.DeviationPercent))
                .Max();

            int shiftIndex = 1;

            foreach (GridLevelState level in levelsToRebuild)
            {
                level.DeviationPercent = maxDeviation + stepPercent * shiftIndex++;
                level.Price = RoundPrice(_gridSide == Side.Buy
                    ? _gridSma * (1 - level.DeviationPercent / 100m)
                    : _gridSma * (1 + level.DeviationPercent / 100m));
                level.WaitingForReplacement = IsLimitOrderMode();

                if (IsLimitOrderMode() &&
                    level.Position != null &&
                    level.Position.State == PositionStateType.Opening)
                {
                    CancelLevelOrders(level, $"DDR repricing index={level.Index}");
                }
                else
                {
                    SetLevelPosition(level, null);

                    if (IsLimitOrderMode())
                    {
                        ReopenPendingLevel(level, "DDR replacement");
                    }
                }
            }

            LogDebug($"DDR activated: pending levels were repriced with step={stepPercent:F4}%");
        }

        protected override void CandleFinishedEvent(List<Candle> candles)
        {
            if (candles == null || candles.Count == 0)
            {
                return;
            }

            Candle last = candles[candles.Count - 1];

            if (last.State != CandleState.Finished)
            {
                return;
            }

            LogDebugSessionHeader(last);

            if (NeedResetStateOnTesterRestart(candles, last))
            {
                ResetRuntimeState("Tester restart detected");
            }

            _lastProcessedCandlesCount = candles.Count;
            _lastProcessedCandleTime = last.TimeStart;

            CleanupGridState();
            TryReopenRepricedLevels();
            UpdateDynamicExitOrders();

            if (_gridSide != Side.None && IsMarketOrderMode())
            {
                LogDebug($"Candle step: mode=MarketGrid, {GetCandleSnapshot(last)}, {GetGridStateSnapshot()}");
                HandleMarketGrid(last);
            }
            else if (_gridSide != Side.None && HasConsumedLevels())
            {
                LogDebug($"Candle step: mode=ActiveSeries, {GetCandleSnapshot(last)}, {GetGridStateSnapshot()}");
                HandleActiveSeries(last);
                return;
            }
            else if (_gridSide != Side.None)
            {
                LogDebug($"Candle step: mode=PendingGrid, {GetCandleSnapshot(last)}, {GetGridStateSnapshot()}");
                HandlePendingGrid(last);
            }

            if (_regime == BotRegime.Off)
            {
                LogDebug($"Skip build: regime off, {GetCandleSnapshot(last)}");
                return;
            }

            if (!IsTradingTime())
            {
                LogDebug($"Skip build: outside trading time, time={last.TimeStart}, start={_startTradeTime.Value}, end={_endTradeTime.Value}");
                return;
            }

            if (_gridSide != Side.None)
            {
                LogDebug($"Skip build: existing grid remains active, {GetGridStateSnapshot()}");
                return;
            }

            if (candles.Count <= Math.Max(_periodSma.ValueInt, _natrLength.ValueInt))
            {
                LogDebug(
                    $"Skip build: insufficient candles, candles={candles.Count}, required>{Math.Max(_periodSma.ValueInt, _natrLength.ValueInt)}, " +
                    $"{GetCandleSnapshot(last)}");
                return;
            }

            if (!_zScoreLow.Ready || !_zScoreHigh.Ready)
            {
                LogDebug(
                    $"Skip build: zscore indicators not ready, zLowReady={_zScoreLow.Ready}, zHighReady={_zScoreHigh.Ready}, " +
                    $"{GetCandleSnapshot(last)}");
                return;
            }

            if (GetCheckers().Any(checker => !checker(candles)))
            {
                LogDebug($"Skip build: generic checker blocked, {GetCandleSnapshot(last)}");
                return;
            }

            TryBuildPendingGrid(candles);
        }

        private void HandleActiveSeries(Candle last)
        {
            if (ShouldCancelRemainingPendingOrders(last))
            {
                CancelPendingOrders("Mean reversion touched SMA");
            }

            if (!HasActiveFilledPositions())
            {
                CancelPendingOrders("Series has no active filled positions");

                if (!HasPendingOpeningOrders())
                {
                    ClearGrid("Series completed");
                }
            }
        }

        private void HandlePendingGrid(Candle last)
        {
            CancelPendingLevelsInvalidByEma();

            string invalidReason = GetPendingGridInvalidationReason(last);

            if (invalidReason != null)
            {
                CancelPendingOrders(invalidReason);
            }

            if (!HasPendingOpeningOrders())
            {
                ClearGrid("Pending grid cleared before first fill");
            }
        }

        private void HandleMarketGrid(Candle last)
        {
            string invalidReason = GetPendingGridInvalidationReason(last);

            if (invalidReason != null)
            {
                DiscardRemainingMarketLevels(invalidReason);

                if (!HasConsumedLevels() && !HasPendingOpeningOrders())
                {
                    ClearGrid("Pending market grid cleared before first fill");
                }
                else if (!HasActiveFilledPositions() &&
                         !HasPendingOpeningOrders() &&
                         !HasRemainingInactiveLevels())
                {
                    ClearGrid("Series completed");
                }
                return;
            }

            if (HasConsumedLevels() && ShouldCancelRemainingPendingOrders(last))
            {
                DiscardRemainingMarketLevels("Mean reversion touched SMA");
            }

            TryActivateTriggeredMarketLevels(last);

            if (!HasActiveFilledPositions() &&
                !HasPendingOpeningOrders() &&
                !HasRemainingInactiveLevels())
            {
                ClearGrid("Series completed");
            }
        }

        private void TryBuildPendingGrid(List<Candle> candles)
        {
            int lastIndex = candles.Count - 1;
            decimal sma = GetIndicatorLastValue(_sma, lastIndex);

            if (sma <= 0)
            {
                LogDebug($"Skip grid build: SMA unavailable at candle={candles[lastIndex].TimeStart}");
                return;
            }

            Candle last = candles[lastIndex];
            Side side = last.Close < sma ? Side.Buy : last.Close > sma ? Side.Sell : Side.None;

            if (side == Side.None)
            {
                LogDebug($"Skip grid build: close equals SMA, {GetDecisionSnapshot(last, side, sma)}");
                return;
            }

            if ((_regime == BotRegime.OnlyLong && side != Side.Buy) ||
                (_regime == BotRegime.OnlyShort && side != Side.Sell))
            {
                LogDebug($"Skip grid build: regime/side mismatch, {GetDecisionSnapshot(last, side, sma)}");
                return;
            }

            if (!CanCreateGridByFilters(side, last, sma))
            {
                LogDebug($"Skip grid build: directional filters blocked, {GetDecisionSnapshot(last, side, sma)}");
                return;
            }

            decimal natrPercent = GetNatrPercent(lastIndex, last.Close);

            if (natrPercent < 0)
            {
                LogDebug($"Skip grid build: NATR unavailable, {GetDecisionSnapshot(last, side, sma)}");
                return;
            }

            decimal thresholdPercent = GetThresholdPercent(side, sma);

            if (thresholdPercent < 0)
            {
                LogDebug($"Skip grid build: threshold unavailable, {GetDecisionSnapshot(last, side, sma, natrPercent)}");
                return;
            }

            decimal currentDeviationPercent = GetDeviationPercent(side, sma, last.Close);
            decimal triggeredDeviationPercent = GetTriggeredDeviationPercent(side, sma, last);

            LogDebug(
                $"Grid build decision: {GetDecisionSnapshot(last, side, sma, natrPercent, thresholdPercent, currentDeviationPercent, triggeredDeviationPercent)}");

            List<GridLevelState> levels = CreateLevels(
                side,
                sma,
                last.Close,
                thresholdPercent,
                currentDeviationPercent,
                triggeredDeviationPercent,
                natrPercent,
                last.TimeStart);

            if (levels.Count == 0)
            {
                LogDebug(
                    $"Skip grid build: no valid levels survived generation, " +
                    $"{GetDecisionSnapshot(last, side, sma, natrPercent, thresholdPercent, currentDeviationPercent, triggeredDeviationPercent)}");
                return;
            }

            _gridLevels.Clear();
            _gridLevels.AddRange(levels);
            _gridSide = side;
            _gridSma = sma;
            _gridThresholdPercent = thresholdPercent;
            _gridNatrPercent = natrPercent;
            _gridSignalTime = last.TimeStart;

            string levelsLog = string.Join("; ", _gridLevels.Select(l => $"[{l.Index}] {l.DeviationPercent:F4}% => {l.Price:F8}"));
            LogDebug(
                $"Grid built: side={side}, sma={sma:F8}, threshold={thresholdPercent:F4}%, currentDeviation={currentDeviationPercent:F4}%, natr={natrPercent:F4}%, levels={levelsLog}");
        }

        private List<GridLevelState> CreateLevels(
            Side side,
            decimal sma,
            decimal currentPrice,
            decimal thresholdPercent,
            decimal currentDeviationPercent,
            decimal triggeredDeviationPercent,
            decimal natrPercent,
            DateTime signalTime)
        {
            List<GridLevelState> result = new List<GridLevelState>();
            _volumeManager.Clear();
            decimal stepPercent = GetStepPercent();
            bool limitOrderMode = IsLimitOrderMode();

            for (int rawIndex = 1; result.Count < _gridSize.ValueInt; rawIndex++)
            {
                decimal deviationPercent = stepPercent * rawIndex + natrPercent * _natrMult.ValueDecimal;

                if (deviationPercent >= 100m)
                {
                    LogDebug($"Level generation stopped: rawIndex={rawIndex}, deviation={deviationPercent:F4}% makes price non-positive");
                    break;
                }

                if (deviationPercent < thresholdPercent)
                {
                    continue;
                }

                decimal price = side == Side.Buy
                    ? sma * (1 - deviationPercent / 100m)
                    : sma * (1 + deviationPercent / 100m);

                price = RoundPrice(price);

                if ((side == Side.Buy && price >= sma) ||
                    (side == Side.Sell && price <= sma) ||
                    (limitOrderMode && side == Side.Buy && price >= currentPrice) ||
                    (limitOrderMode && side == Side.Sell && price <= currentPrice) ||
                    price <= 0)
                {
                    if (price <= 0)
                    {
                        LogDebug($"Level skipped: non-positive price. side={side}, rawIndex={rawIndex}, deviation={deviationPercent:F4}%, price={price:F8}");
                    }
                    continue;
                }

                if (!IsLevelAllowedByEma(side, price))
                {
                    LogDebug(
                        $"Level skipped by EMA price filter: side={side}, rawIndex={rawIndex}, deviation={deviationPercent:F4}%, price={price:F8}, ema={GetCurrentEma():F8}");
                    continue;
                }

                decimal volume = GetVolume();
                Position position = null;

                if (limitOrderMode)
                {
                    if (deviationPercent <= currentDeviationPercent)
                    {
                        continue;
                    }

                    GridLevelState level = new GridLevelState
                    {
                        Index = rawIndex,
                        DeviationPercent = deviationPercent,
                        Price = price,
                        Volume = volume,
                        Consumed = false
                    };

                    position = OpenLevelPosition(level, side, volume, price, signalTime, false);

                    if (position == null)
                    {
                        LogDebug($"Level skipped because position was not created: side={side}, rawIndex={rawIndex}, deviation={deviationPercent:F4}%, price={price:F8}");
                        continue;
                    }

                    result.Add(level);
                    continue;
                }
                else if (deviationPercent <= triggeredDeviationPercent)
                {
                    GridLevelState level = new GridLevelState
                    {
                        Index = rawIndex,
                        DeviationPercent = deviationPercent,
                        Price = price,
                        Volume = volume,
                        Consumed = false
                    };

                    position = OpenLevelPosition(level, side, volume, price, signalTime, true);

                    if (position == null)
                    {
                        LogDebug($"Market level activation postponed because position was not created: side={side}, rawIndex={rawIndex}, deviation={deviationPercent:F4}%, price={price:F8}");
                    }

                    result.Add(level);
                    continue;
                }

                result.Add(new GridLevelState
                {
                    Index = rawIndex,
                    DeviationPercent = deviationPercent,
                    Price = price,
                    Volume = volume,
                    Position = position,
                    Consumed = false
                });
            }

            if (result.Count == 0)
            {
                _volumeManager.Clear();
                LogDebug(
                    $"No valid levels: side={side}, threshold={thresholdPercent:F4}%, currentDeviation={currentDeviationPercent:F4}%, natr={natrPercent:F4}%, requestedGridSize={_gridSize.ValueInt}");
            }

            return result;
        }

        private string GetPendingGridInvalidationReason(Candle last)
        {
            if (_gridSide == Side.Buy)
            {
                if (last.Close >= _gridSma)
                {
                    return "Pending grid invalidated by SMA touch";
                }
            }
            else if (_gridSide == Side.Sell)
            {
                if (last.Close <= _gridSma)
                {
                    return "Pending grid invalidated by SMA touch";
                }
            }

            if (_gridSide != Side.None && !PassesDirectionalFilters(_gridSide, last))
            {
                return "Pending grid invalidated by entry filters";
            }

            return null;
        }

        private bool ShouldCancelRemainingPendingOrders(Candle last)
        {
            if (_gridSide == Side.Buy)
            {
                return last.High >= _gridSma;
            }

            if (_gridSide == Side.Sell)
            {
                return last.Low <= _gridSma;
            }

            return false;
        }

        private void CancelPendingOrders(string reason)
        {
            foreach (GridLevelState level in _gridLevels.Where(l =>
                !l.Consumed &&
                !l.CancelRequested &&
                l.Position != null &&
                l.Position.State == PositionStateType.Opening))
            {
                CancelLevelOrders(level, reason);
            }

            LogDebug($"Pending orders cancel requested: reason={reason}");
        }

        private void CleanupGridState()
        {
            foreach (GridLevelState level in _gridLevels)
            {
                if (level.Position == null)
                {
                    continue;
                }

                if (level.Position.State == PositionStateType.Done ||
                    level.Position.State == PositionStateType.OpeningFail)
                {
                    level.CancelRequested = false;
                    SetLevelPosition(level, null);
                }
                else if (level.Position.State == PositionStateType.Open)
                {
                    level.Consumed = true;
                    level.CancelRequested = false;
                }
            }
        }

        private void ClearGrid(string reason)
        {
            _gridLevels.Clear();
            _levelsAwaitingOpeningSuccess.Clear();
            _gridSide = Side.None;
            _gridSma = 0;
            _gridThresholdPercent = 0;
            _gridNatrPercent = 0;
            _gridSignalTime = DateTime.MinValue;
            _volumeManager.Clear();
            _levelAwaitingImmediateBinding = null;
            LogDebug($"Grid cleared: reason={reason}");
        }

        private bool NeedResetStateOnTesterRestart(List<Candle> candles, Candle last)
        {
            if (StartProgram != StartProgram.IsTester)
            {
                return false;
            }

            bool hasRuntimeState = _gridSide != Side.None ||
                _gridLevels.Count != 0 ||
                _volatileStopActive;

            if (!hasRuntimeState &&
                _lastProcessedCandlesCount == 0 &&
                _lastProcessedCandleTime == DateTime.MinValue)
            {
                return false;
            }

            if (candles.Count < _lastProcessedCandlesCount)
            {
                return true;
            }

            if (_lastProcessedCandleTime != DateTime.MinValue &&
                last.TimeStart < _lastProcessedCandleTime)
            {
                return true;
            }

            return candles.Count < 10 && hasRuntimeState;
        }

        private void ResetRuntimeState(string reason)
        {
            _gridLevels.Clear();
            _gridSide = Side.None;
            _gridSma = 0;
            _gridThresholdPercent = 0;
            _gridNatrPercent = 0;
            _gridSignalTime = DateTime.MinValue;
            _volatileStopActive = false;
            _volatileStopDirection = Side.None;
            _volumeManager.Clear();
            _lastProcessedCandlesCount = 0;
            _lastProcessedCandleTime = DateTime.MinValue;
            _levelAwaitingImmediateBinding = null;
            _levelBindingsByReference.Clear();
            _levelBindingsByNumber.Clear();
            _levelBindingsByOrderUserNumber.Clear();
            _levelBindingsByOrderMarketNumber.Clear();
            _levelsAwaitingOpeningSuccess.Clear();
            LogDebug($"Runtime state reset: reason={reason}");
        }

        private bool HasConsumedLevels()
        {
            return _gridLevels.Any(level => level.Consumed);
        }

        private bool HasRemainingInactiveLevels()
        {
            return _gridLevels.Any(level => !level.Consumed && level.Position == null);
        }

        private void CancelPendingLevelsInvalidByEma()
        {
            if (_gridSide == Side.None)
            {
                return;
            }

            foreach (GridLevelState level in _gridLevels
                .Where(level =>
                    !level.Consumed &&
                    !level.CancelRequested &&
                    level.Position != null &&
                    level.Position.State == PositionStateType.Opening)
                .ToList())
            {
                if (!IsLevelAllowedByEma(_gridSide, level.Price))
                {
                    CancelLevelOrders(level,
                        $"Pending level invalidated by EMA price filter. price={level.Price:F8}, ema={GetCurrentEma():F8}");
                }
            }
        }

        private bool HasPendingOpeningOrders()
        {
            return _gridLevels.Any(level => level.Position != null && !level.Consumed && level.Position.State == PositionStateType.Opening);
        }

        private bool HasPendingCancelRequests()
        {
            return _gridLevels.Any(level =>
                level.CancelRequested &&
                level.Position != null &&
                !level.Consumed &&
                level.Position.State == PositionStateType.Opening);
        }

        private bool HasActiveFilledPositions()
        {
            return _gridLevels.Any(level =>
                level.Consumed &&
                level.Position != null &&
                level.Position.State != PositionStateType.Done &&
                level.Position.State != PositionStateType.OpeningFail);
        }

        private GridLevelState FindLevel(Position position)
        {
            if (position == null)
            {
                return null;
            }

            GridLevelState levelFromGrid = _gridLevels.FirstOrDefault(level =>
                level.Position != null &&
                (ReferenceEquals(level.Position, position) || level.Position.Number == position.Number));

            if (levelFromGrid != null)
            {
                return levelFromGrid;
            }

            if (_levelBindingsByReference.TryGetValue(position, out GridLevelState boundByReference))
            {
                return boundByReference;
            }

            if (position.Number != 0 &&
                _levelBindingsByNumber.TryGetValue(position.Number, out GridLevelState boundByNumber))
            {
                return boundByNumber;
            }

            GridLevelState boundByOrder = FindLevelByOpenOrders(position);

            if (boundByOrder != null)
            {
                return boundByOrder;
            }

            GridLevelState fallbackLevel = FindFallbackLevel(position);

            if (fallbackLevel != null)
            {
                LogDebug(
                    $"Level binding restored by fallback: position={position.Number}, direction={position.Direction}, anchorPrice={GetPositionBindingAnchorPrice(position):F8}, levelIndex={fallbackLevel.Index}, levelPrice={fallbackLevel.Price:F8}");
                return fallbackLevel;
            }

            return null;
        }

        private GridLevelState FindLevelByOpenOrders(Position position)
        {
            if (position?.OpenOrders == null || position.OpenOrders.Count == 0)
            {
                return null;
            }

            for (int i = 0; i < position.OpenOrders.Count; i++)
            {
                Order order = position.OpenOrders[i];

                if (order == null)
                {
                    continue;
                }

                if (order.NumberUser != 0 &&
                    _levelBindingsByOrderUserNumber.TryGetValue(order.NumberUser, out GridLevelState boundByUserOrder))
                {
                    return boundByUserOrder;
                }

                if (!string.IsNullOrWhiteSpace(order.NumberMarket) &&
                    _levelBindingsByOrderMarketNumber.TryGetValue(order.NumberMarket, out GridLevelState boundByMarketOrder))
                {
                    return boundByMarketOrder;
                }
            }

            foreach (GridLevelState level in GetKnownLevels())
            {
                if (level?.Position?.OpenOrders == null || level.Position.OpenOrders.Count == 0)
                {
                    continue;
                }

                if (HasMatchingOpenOrder(level.Position.OpenOrders, position.OpenOrders))
                {
                    return level;
                }
            }

            return null;
        }

        private GridLevelState FindFallbackLevel(Position position)
        {
            if (position == null)
            {
                return null;
            }

            List<GridLevelState> candidates = GetKnownLevels()
                .Where(level => level != null &&
                                !level.Consumed &&
                                (level.Position == null ||
                                 level.Position.State == PositionStateType.Opening ||
                                 level.Position.Number == 0))
                .OrderBy(level => level.Index)
                .ToList();

            if (candidates.Count == 1)
            {
                return candidates[0];
            }

            decimal anchorPrice = GetPositionBindingAnchorPrice(position);

            if (anchorPrice <= 0)
            {
                return null;
            }

            decimal priceTolerance = GetBindingPriceTolerance(anchorPrice);

            List<GridLevelState> priceMatchedCandidates = candidates
                .Where(level => Math.Abs(level.Price - anchorPrice) <= priceTolerance)
                .OrderBy(level => Math.Abs(level.Price - anchorPrice))
                .ThenBy(level => level.Index)
                .ToList();

            if (priceMatchedCandidates.Count == 1)
            {
                return priceMatchedCandidates[0];
            }

            return null;
        }

        private GridLevelState FindAwaitingOpeningSuccessLevel(Position position)
        {
            if (position == null || _levelsAwaitingOpeningSuccess.Count == 0)
            {
                return null;
            }

            decimal anchorPrice = GetPositionBindingAnchorPrice(position);
            List<GridLevelState> candidates = _levelsAwaitingOpeningSuccess
                .Where(level => level != null && !level.Consumed)
                .ToList();

            if (candidates.Count == 0)
            {
                return null;
            }

            if (anchorPrice > 0)
            {
                decimal tolerance = GetBindingPriceTolerance(anchorPrice);
                GridLevelState priceMatchedLevel = candidates
                    .Where(level => Math.Abs(level.Price - anchorPrice) <= tolerance)
                    .OrderBy(level => Math.Abs(level.Price - anchorPrice))
                    .ThenBy(level => level.Index)
                    .FirstOrDefault();

                if (priceMatchedLevel != null)
                {
                    LogDebug(
                        $"Level binding restored by awaiting-success queue: position={position.Number}, anchorPrice={anchorPrice:F8}, levelIndex={priceMatchedLevel.Index}, levelPrice={priceMatchedLevel.Price:F8}");
                    return priceMatchedLevel;
                }
            }

            GridLevelState firstAwaitingLevel = candidates[0];
            LogDebug(
                $"Level binding restored by awaiting-success order: position={position.Number}, levelIndex={firstAwaitingLevel.Index}, levelPrice={firstAwaitingLevel.Price:F8}");
            return firstAwaitingLevel;
        }

        private List<GridLevelState> GetKnownLevels()
        {
            HashSet<GridLevelState> knownLevels = new HashSet<GridLevelState>();

            foreach (GridLevelState level in _gridLevels)
            {
                if (level != null)
                {
                    knownLevels.Add(level);
                }
            }

            foreach (GridLevelState level in _levelBindingsByReference.Values)
            {
                if (level != null)
                {
                    knownLevels.Add(level);
                }
            }

            foreach (GridLevelState level in _levelBindingsByNumber.Values)
            {
                if (level != null)
                {
                    knownLevels.Add(level);
                }
            }

            foreach (GridLevelState level in _levelBindingsByOrderUserNumber.Values)
            {
                if (level != null)
                {
                    knownLevels.Add(level);
                }
            }

            foreach (GridLevelState level in _levelBindingsByOrderMarketNumber.Values)
            {
                if (level != null)
                {
                    knownLevels.Add(level);
                }
            }

            return knownLevels.ToList();
        }

        private bool HasMatchingOpenOrder(List<Order> leftOrders, List<Order> rightOrders)
        {
            if (leftOrders == null || rightOrders == null || leftOrders.Count == 0 || rightOrders.Count == 0)
            {
                return false;
            }

            for (int i = 0; i < leftOrders.Count; i++)
            {
                Order leftOrder = leftOrders[i];

                if (leftOrder == null)
                {
                    continue;
                }

                for (int j = 0; j < rightOrders.Count; j++)
                {
                    Order rightOrder = rightOrders[j];

                    if (rightOrder == null)
                    {
                        continue;
                    }

                    if (leftOrder.NumberUser != 0 && leftOrder.NumberUser == rightOrder.NumberUser)
                    {
                        return true;
                    }

                    if (!string.IsNullOrWhiteSpace(leftOrder.NumberMarket) &&
                        !string.IsNullOrWhiteSpace(rightOrder.NumberMarket) &&
                        leftOrder.NumberMarket.Equals(rightOrder.NumberMarket, StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private decimal GetPositionBindingAnchorPrice(Position position)
        {
            if (position == null)
            {
                return 0;
            }

            if (position.EntryPrice > 0)
            {
                return position.EntryPrice;
            }

            if (position.OpenOrders == null || position.OpenOrders.Count == 0)
            {
                return 0;
            }

            decimal sum = 0;
            int count = 0;

            for (int i = 0; i < position.OpenOrders.Count; i++)
            {
                Order order = position.OpenOrders[i];

                if (order == null || order.Price <= 0)
                {
                    continue;
                }

                sum += order.Price;
                count++;
            }

            if (count == 0)
            {
                return 0;
            }

            return sum / count;
        }

        private decimal GetBindingPriceTolerance(decimal anchorPrice)
        {
            decimal priceStep = _tab?.Security?.PriceStep ?? 0;
            decimal percentTolerance = anchorPrice * 0.0005m;
            decimal stepTolerance = priceStep > 0 ? priceStep * 2 : 0;
            return Math.Max(percentTolerance, stepTolerance);
        }

        private decimal GetTakeProfit(Position position)
        {
            return GetRegularTakeProfit(position);
        }

        private decimal GetRegularTakeProfit(Position position)
        {
            decimal stopLossPrice = GetAtrStopLossForTakeProfit(position);
            decimal stopDistance = Math.Abs(position.EntryPrice - stopLossPrice);
            decimal takeDistance = stopDistance * _rr.ValueDecimal;

            if (position.Direction == Side.Buy)
            {
                return position.EntryPrice + takeDistance;
            }

            return position.EntryPrice - takeDistance;
        }

        private decimal GetZScoreChannelTakeProfit(Position position)
        {
            if (position == null || _channel == null)
            {
                return 0;
            }

            decimal takePrice = position.Direction == Side.Buy
                ? _channel.ChannelDataHighLast
                : _channel.ChannelDataLowLast;

            if (takePrice <= 0)
            {
                return 0;
            }

            return RoundPrice(takePrice);
        }

        private decimal GetAtrStopLoss(Position position)
        {
            decimal stopDistance = GetAtrStopLossDistance(position);

            if (position.Direction == Side.Buy)
            {
                return position.EntryPrice - stopDistance;
            }

            return position.EntryPrice + stopDistance;
        }

        private decimal GetAtrStopLossForTakeProfit(Position position)
        {
            decimal stopDistance = GetAtrStopLossDistance(position);

            if (stopDistance <= 0)
            {
                return GetAbsoluteStopLoss(position);
            }

            if (position.Direction == Side.Buy)
            {
                return position.EntryPrice - stopDistance;
            }

            return position.EntryPrice + stopDistance;
        }

        private decimal GetAtrStopLossDistance(Position position)
        {
            if (position == null || position.EntryPrice <= 0)
            {
                return 0;
            }

            decimal atrDistance = _atrStop.CurrentAtr * _atrSlMultiplier.ValueDecimal;

            if (atrDistance <= 0)
            {
                return 0;
            }

            return atrDistance;
        }

        private decimal GetAbsoluteStopLoss(Position position)
        {
            if (position == null || position.EntryPrice <= 0)
            {
                return 0;
            }

            decimal limitDistance = _stopLossLimitPercent.ValueDecimal / 100m * position.EntryPrice;

            if (position.Direction == Side.Buy)
            {
                return position.EntryPrice - limitDistance;
            }

            return position.EntryPrice + limitDistance;
        }

        private decimal GetAbsoluteSeriesStop(Side direction)
        {
            List<Position> openPositions = _tab?.PositionsOpenAll;

            if (openPositions == null || openPositions.Count == 0)
            {
                return 0;
            }

            List<decimal> stopPrices = openPositions
                .Where(position => position != null &&
                                   position.Direction == direction &&
                                   position.OpenVolume > 0 &&
                                   position.State != PositionStateType.Done &&
                                   position.State != PositionStateType.OpeningFail)
                .Select(GetAbsoluteStopLoss)
                .Where(price => price > 0)
                .ToList();

            if (stopPrices.Count == 0)
            {
                return 0;
            }

            return direction == Side.Buy
                ? stopPrices.Max()
                : stopPrices.Min();
        }

        private decimal GetNatrPercent(int index, decimal referencePrice)
        {
            if (referencePrice <= 0 ||
                _natrAtr == null ||
                _natrAtr.DataSeries == null ||
                _natrAtr.DataSeries.Count == 0 ||
                _natrAtr.DataSeries[0] == null ||
                _natrAtr.DataSeries[0].Values == null ||
                _natrAtr.DataSeries[0].Values.Count <= index)
            {
                return -1;
            }

            decimal atr = _natrAtr.DataSeries[0].Values[index];

            if (atr <= 0)
            {
                return -1;
            }

            return atr / referencePrice * 100m;
        }

        private decimal GetThresholdPercent(Side side, decimal sma)
        {
            if (sma <= 0)
            {
                return 0;
            }

            if (side == Side.Buy)
            {
                decimal lowChannel = _channel.ChannelDataLowLast;
                return lowChannel > 0 ? Math.Max(0, (sma - lowChannel) / sma * 100m) : -1;
            }

            decimal highChannel = _channel.ChannelDataHighLast;
            return highChannel > 0 ? Math.Max(0, (highChannel - sma) / sma * 100m) : -1;
        }

        private decimal GetDeviationPercent(Side side, decimal sma, decimal price)
        {
            if (sma <= 0)
            {
                return 0;
            }

            if (side == Side.Buy)
            {
                return Math.Max(0, (sma - price) / sma * 100m);
            }

            return Math.Max(0, (price - sma) / sma * 100m);
        }

        private decimal GetIndicatorLastValue(Aindicator indicator, int index)
        {
            if (indicator == null ||
                indicator.DataSeries == null ||
                indicator.DataSeries.Count == 0 ||
                indicator.DataSeries[0] == null ||
                indicator.DataSeries[0].Values == null ||
                indicator.DataSeries[0].Values.Count <= index)
            {
                return 0;
            }

            return indicator.DataSeries[0].Values[index];
        }

        private bool IsTradingTime()
        {
            return !(_startTradeTime.Value > _tab.TimeServerCurrent ||
                     _endTradeTime.Value < _tab.TimeServerCurrent);
        }

        private bool CanCreateGridByFilters(Side side, Candle last, decimal sma)
        {
            if (!PassesDirectionalFilters(side, last))
            {
                return false;
            }

            if (_volatileStopActive && _volatileStopDirection == side)
            {
                if (HasPendingCancelRequests())
                {
                    LogDebug(
                        $"Filter blocked by volatile stop pending cancels: side={side}, " +
                        $"{GetDecisionSnapshot(last, side, sma)}");
                    return false;
                }

                if (side == Side.Buy)
                {
                    if (last.Close >= sma)
                    {
                        _volatileStopActive = false;
                        LogDebug("Volatile stop cleared for Buy");
                    }
                    else
                    {
                        LogDebug(
                            $"Filter blocked by volatile stop latch: side=Buy, close={last.Close:F8}, sma={sma:F8}, " +
                            $"{GetGridStateSnapshot()}");
                        return false;
                    }
                }
                else if (side == Side.Sell)
                {
                    if (last.Close <= sma)
                    {
                        _volatileStopActive = false;
                        LogDebug("Volatile stop cleared for Sell");
                    }
                    else
                    {
                        LogDebug(
                            $"Filter blocked by volatile stop latch: side=Sell, close={last.Close:F8}, sma={sma:F8}, " +
                            $"{GetGridStateSnapshot()}");
                        return false;
                    }
                }
            }

            return true;
        }

        private bool PassesDirectionalFilters(Side side, Candle last)
        {
            if (_change24 != null)
            {
                if (side == Side.Buy && !_change24.CanBuy)
                {
                    LogDebug($"Change24 ({last.TimeStart}): buy blocked, change {_change24.Change:F2}%");
                    return false;
                }

                if (side == Side.Sell && !_change24.CanSell)
                {
                    LogDebug($"Change24 ({last.TimeStart}): sell blocked, change {_change24.Change:F2}%");
                    return false;
                }
            }

            if (_canEnterByEma != null)
            {
                if (side == Side.Buy && !_canEnterByEma.CanBuy)
                {
                    decimal ema = _ema?.DataSeries != null &&
                                  _ema.DataSeries.Count > 0 &&
                                  _ema.DataSeries[0] != null
                        ? _ema.DataSeries[0].Last
                        : 0;

                    LogDebug($"EMA filter blocked buy at {last.TimeStart}. close={last.Close:F8}, ema={ema:F8}");
                    return false;
                }

                if (side == Side.Sell && !_canEnterByEma.CanSell)
                {
                    decimal ema = _ema?.DataSeries != null &&
                                  _ema.DataSeries.Count > 0 &&
                                  _ema.DataSeries[0] != null
                        ? _ema.DataSeries[0].Last
                        : 0;

                    LogDebug($"EMA filter blocked sell at {last.TimeStart}. close={last.Close:F8}, ema={ema:F8}");
                    return false;
                }
            }

            return true;
        }

        private bool IsLevelAllowedByEma(Side side, decimal price)
        {
            return _canEnterByEma == null || _canEnterByEma.IsPriceAllowed(side, price);
        }

        private decimal GetCurrentEma()
        {
            if (_ema == null ||
                _ema.DataSeries == null ||
                _ema.DataSeries.Count == 0 ||
                _ema.DataSeries[0] == null ||
                _ema.DataSeries[0].Values == null ||
                _ema.DataSeries[0].Values.Count == 0)
            {
                return 0;
            }

            return _ema.DataSeries[0].Last;
        }

        private decimal GetTriggeredDeviationPercent(Side side, decimal sma, Candle candle)
        {
            if (candle == null)
            {
                return 0;
            }

            if (side == Side.Buy)
            {
                return GetDeviationPercent(side, sma, candle.Low);
            }

            if (side == Side.Sell)
            {
                return GetDeviationPercent(side, sma, candle.High);
            }

            return 0;
        }

        private OrderType GetSelectedOrderType()
        {
            if (_orderType != null &&
                Enum.TryParse(_orderType.ValueString, true, out OrderType selectedOrderType))
            {
                return selectedOrderType;
            }

            return OrderType.Limit;
        }

        private bool IsLimitOrderMode()
        {
            return GetSelectedOrderType() == OrderType.Limit;
        }

        private bool IsMarketOrderMode()
        {
            return GetSelectedOrderType() == OrderType.Market;
        }

        private decimal GetStepPercent()
        {
            decimal step = _fixPercent.ValueDecimal;
            _ddrDecoration?.ChangeStep(ref step);
            return step;
        }

        private decimal RoundPrice(decimal price)
        {
            if (_tab?.Security == null || _tab.Security.PriceStep <= 0)
            {
                return price;
            }

            decimal steps = Math.Round(price / _tab.Security.PriceStep, MidpointRounding.AwayFromZero);
            decimal rounded = steps * _tab.Security.PriceStep;
            return Math.Round(rounded, _tab.Security.Decimals);
        }

        private decimal GetRounded(decimal volume)
        {
            return GetRoundedVolume(_tab, volume);
        }

        private void LogDebug(string message)
        {
            if (_debugLogging.ValueBool)
            {
                SendNewLogMessage(message, LogMessageType.System);
            }
        }

        private void LogDebugSessionHeader(Candle last)
        {
            if (!_debugLogging.ValueBool || _debugSessionHeaderLogged)
            {
                return;
            }

            _debugSessionHeaderLogged = true;

            string securityName = _tab?.Security?.Name ?? _tab?.Connector?.SecurityName ?? "n/a";
            string securityClass = _tab?.Security?.NameClass ?? _tab?.Connector?.SecurityClass ?? "n/a";
            string timeFrame = _tab?.TimeFrame.ToString() ?? "n/a";

            LogDebug(
                $"Debug session context: program={StartProgram}, bot={NameStrategyUniq}, security={securityName}, class={securityClass}, timeframe={timeFrame}, " +
                $"regime={_regime}, orderType={GetSelectedOrderType()}, volumeType={_volumeType.ValueString}, volumeParam={_volumeOnPosition.ValueDecimal:F8}, " +
                $"gridSize={_gridSize.ValueInt}, smaPeriod={_periodSma.ValueInt}, emaPeriod={_emaLength.ValueInt}, zEnter={_zEnterBase.ValueDecimal:F4}, " +
                $"fixPercent={_fixPercent.ValueDecimal:F4}, natrLength={_natrLength.ValueInt}, natrMultiplier={_natrMult.ValueDecimal:F4}, " +
                $"r={_r.ValueDecimal:F4}, debugCandle={GetCandleSnapshot(last)}");
        }

        private string GetDecisionSnapshot(
            Candle last,
            Side side,
            decimal sma,
            decimal? natrPercent = null,
            decimal? thresholdPercent = null,
            decimal? currentDeviationPercent = null,
            decimal? triggeredDeviationPercent = null)
        {
            string natr = natrPercent.HasValue ? natrPercent.Value.ToString("F4") : "n/a";
            string threshold = thresholdPercent.HasValue ? thresholdPercent.Value.ToString("F4") : "n/a";
            string currentDeviation = currentDeviationPercent.HasValue ? currentDeviationPercent.Value.ToString("F4") : "n/a";
            string triggeredDeviation = triggeredDeviationPercent.HasValue ? triggeredDeviationPercent.Value.ToString("F4") : "n/a";

            return
                $"{GetCandleSnapshot(last)}, sideCandidate={side}, sma={sma:F8}, ema={GetCurrentEma():F8}, " +
                $"change24={_change24?.Change:F4}, canBuy={_change24?.CanBuy}, canSell={_change24?.CanSell}, " +
                $"emaCanBuy={_canEnterByEma?.CanBuy}, emaCanSell={_canEnterByEma?.CanSell}, " +
                $"natr={natr}, threshold={threshold}, currentDeviation={currentDeviation}, triggeredDeviation={triggeredDeviation}, " +
                $"{GetGridStateSnapshot()}";
        }

        private string GetCandleSnapshot(Candle candle)
        {
            if (candle == null)
            {
                return "candle=n/a";
            }

            return
                $"candle={candle.TimeStart:yyyy-MM-dd HH:mm:ss}, o={candle.Open:F8}, h={candle.High:F8}, l={candle.Low:F8}, c={candle.Close:F8}";
        }

        private string GetGridStateSnapshot()
        {
            int totalLevels = _gridLevels?.Count ?? 0;
            int consumedLevels = _gridLevels?.Count(level => level.Consumed) ?? 0;
            int levelsAwaitingFill = _levelsAwaitingOpeningSuccess?.Count ?? 0;
            int levelsWithPosition = _gridLevels?.Count(level => level.Position != null) ?? 0;
            int pendingOpenOrders = _gridLevels?.Count(level =>
                level.Position != null &&
                level.Position.OpenVolume == 0 &&
                level.Position.State != PositionStateType.Done &&
                level.Position.State != PositionStateType.OpeningFail) ?? 0;
            int activeFilledPositions = _gridLevels?.Count(level =>
                level.Position != null &&
                level.Position.OpenVolume > 0 &&
                level.Position.State != PositionStateType.Done &&
                level.Position.State != PositionStateType.OpeningFail) ?? 0;

            return
                $"gridSide={_gridSide}, gridSignal={FormatDateTime(_gridSignalTime)}, volatileStop={_volatileStopActive}/{_volatileStopDirection}, " +
                $"levelsTotal={totalLevels}, consumed={consumedLevels}, withPosition={levelsWithPosition}, awaitingFill={levelsAwaitingFill}, " +
                $"pendingOrders={pendingOpenOrders}, activeFilled={activeFilledPositions}";
        }

        private string FormatDateTime(DateTime? time)
        {
            if (!time.HasValue || time.Value == DateTime.MinValue)
            {
                return "n/a";
            }

            return time.Value.ToString("yyyy-MM-dd HH:mm:ss");
        }

        private void VolatileStopHandler()
        {
            if (_gridSide == Side.None)
            {
                return;
            }

            CancelPendingOrders("Volatile stop");
            _volatileStopActive = true;
            _volatileStopDirection = _gridSide;
            _volumeManager.Clear();
            LogDebug($"Volatile stop fired for {_volatileStopDirection}");
        }

        private void TryReopenRepricedLevels()
        {
            if (!IsLimitOrderMode())
            {
                return;
            }

            foreach (GridLevelState level in _gridLevels.Where(level => level.WaitingForReplacement && level.Position == null).ToList())
            {
                ReopenPendingLevel(level, "Deferred DDR replacement");
            }
        }

        private void ReopenPendingLevel(GridLevelState level, string reason)
        {
            if (level == null || level.Consumed || _gridSide == Side.None || !IsLimitOrderMode())
            {
                return;
            }

            level.CancelRequested = false;

            if (!IsLevelAllowedByEma(_gridSide, level.Price))
            {
                LogDebug(
                    $"Level reopen skipped by EMA price filter: reason={reason}, index={level.Index}, price={level.Price:F8}, ema={GetCurrentEma():F8}");
                return;
            }

            DateTime signalTime = GetLatestFinishedCandleTime();

            if (signalTime == DateTime.MinValue)
            {
                signalTime = _gridSignalTime;
            }

            Position position = OpenLevelPosition(level, _gridSide, level.Volume, level.Price, signalTime, false);
            level.WaitingForReplacement = position == null;

            LogDebug($"Level reopened: reason={reason}, index={level.Index}, deviation={level.DeviationPercent:F4}%, price={level.Price:F8}");
        }

        private void TryActivateTriggeredMarketLevels(Candle last)
        {
            if (!IsMarketOrderMode() || _gridSide == Side.None)
            {
                return;
            }

            decimal triggeredDeviationPercent = GetTriggeredDeviationPercent(_gridSide, _gridSma, last);

            foreach (GridLevelState level in _gridLevels
                .Where(level => !level.Consumed && level.Position == null && level.DeviationPercent <= triggeredDeviationPercent)
                .OrderBy(level => level.DeviationPercent)
                .ToList())
            {
                if (!IsLevelAllowedByEma(_gridSide, level.Price))
                {
                    LogDebug(
                        $"Market level skipped by EMA price filter: index={level.Index}, price={level.Price:F8}, ema={GetCurrentEma():F8}");
                    continue;
                }

                Position position = OpenLevelPosition(level, _gridSide, level.Volume, level.Price, last.TimeStart, true);

                if (position == null)
                {
                    LogDebug(
                        $"Market level activation failed: index={level.Index}, deviation={level.DeviationPercent:F4}%, price={level.Price:F8}");
                    continue;
                }

                LogDebug(
                    $"Market level activated: index={level.Index}, deviation={level.DeviationPercent:F4}%, price={level.Price:F8}, triggerDeviation={triggeredDeviationPercent:F4}%");
            }
        }

        private void DiscardRemainingMarketLevels(string reason)
        {
            List<GridLevelState> levelsToRemove = _gridLevels
                .Where(level => !level.Consumed && level.Position == null)
                .ToList();

            if (levelsToRemove.Count == 0)
            {
                return;
            }

            for (int i = 0; i < levelsToRemove.Count; i++)
            {
                _gridLevels.Remove(levelsToRemove[i]);
            }

            LogDebug($"Remaining market levels discarded: reason={reason}, count={levelsToRemove.Count}");
        }

        private void CancelLevelOrders(GridLevelState level, string reason)
        {
            if (level?.Position == null)
            {
                return;
            }

            if (level.CancelRequested)
            {
                return;
            }

            bool cancelRequested = false;

            foreach (Order order in level.Position.OpenOrders)
            {
                if (order.State == OrderStateType.Cancel || order.State == OrderStateType.Done)
                {
                    continue;
                }

                try
                {
                    _tab.Connector.OrderCancel(order);
                    cancelRequested = true;
                }
                catch (Exception ex)
                {
                    SendNewLogMessage(ex.Message, LogMessageType.Error);
                }
            }

            if (cancelRequested)
            {
                level.CancelRequested = true;
            }

            if (cancelRequested)
            {
                LogDebug($"Order cancel requested: reason={reason}, index={level.Index}");
            }
        }

        private Position OpenLevelPosition(
            GridLevelState level,
            Side side,
            decimal volume,
            decimal price,
            DateTime signalTime,
            bool useMarketOrder)
        {
            if (level == null)
            {
                return null;
            }

            Position position = null;
            GridLevelState previousAwaitingBinding = _levelAwaitingImmediateBinding;
            _levelAwaitingImmediateBinding = level;

            try
            {
                position = useMarketOrder
                    ? OpenPlannedMarket(side, volume, price, signalTime)
                    : OpenPlannedLimit(side, volume, price, signalTime);

                if (position != null &&
                    !ReferenceEquals(level.Position, position))
                {
                    RememberAwaitingOpeningSuccess(level);
                    SetLevelPosition(level, position);
                }

                return position;
            }
            finally
            {
                _levelAwaitingImmediateBinding = previousAwaitingBinding;
            }
        }

        private void UpdateDynamicExitOrders()
        {
            List<Position> positions = _tab?.PositionsOpenAll;

            if (positions == null || positions.Count == 0)
            {
                return;
            }

            for (int i = 0; i < positions.Count; i++)
            {
                Position position = positions[i];

                if (position == null ||
                    position.OpenVolume <= 0 ||
                    position.State == PositionStateType.Done ||
                    position.State == PositionStateType.OpeningFail)
                {
                    continue;
                }

                UpdateDynamicStop(position);
                UpdateDynamicProfit(position);
            }
        }

        private void UpdateDynamicStop(Position position)
        {
            if (position == null)
            {
                return;
            }

            decimal bestStopPrice = 0;

            if (_stopLoss != null && _stopLoss.On)
            {
                decimal atrStopPrice = GetAtrStopLoss(position);
                bestStopPrice = SelectMoreProtectiveStop(position.Direction, bestStopPrice, atrStopPrice);
            }

            decimal absoluteSeriesStop = GetAbsoluteSeriesStop(position.Direction);
            bestStopPrice = SelectMoreProtectiveStop(position.Direction, bestStopPrice, absoluteSeriesStop);

            if (_emaStopEnable != null && _emaStopEnable.ValueBool)
            {
                decimal ema = GetCurrentEma();
                bestStopPrice = SelectMoreProtectiveStop(position.Direction, bestStopPrice, ema);
            }

            if (bestStopPrice <= 0)
            {
                return;
            }

            bestStopPrice = RoundPrice(bestStopPrice);
            PlaceStopOrder(position, bestStopPrice);
        }

        private void UpdateDynamicProfit(Position position)
        {
            if (position == null ||
                _zScoreChannelTpEnable == null ||
                !_zScoreChannelTpEnable.ValueBool)
            {
                return;
            }

            decimal zScoreTakeProfit = GetZScoreChannelTakeProfit(position);

            if (!IsProfitableTarget(position, zScoreTakeProfit))
            {
                return;
            }

            decimal targetPrice = zScoreTakeProfit;

            if (position.ProfitOrderIsActiv && position.ProfitOrderPrice > 0)
            {
                targetPrice = position.Direction == Side.Buy
                    ? Math.Min(position.ProfitOrderPrice, zScoreTakeProfit)
                    : Math.Max(position.ProfitOrderPrice, zScoreTakeProfit);
            }

            if (!IsProfitableTarget(position, targetPrice))
            {
                return;
            }

            PlaceProfitOrder(position, targetPrice);
        }

        private void PlaceStopOrder(Position position, decimal stopPrice)
        {
            if (position == null || stopPrice <= 0)
            {
                return;
            }

            decimal priceStep = _tab?.Security?.PriceStep ?? 0;
            decimal orderPrice = position.Direction == Side.Buy
                ? stopPrice - priceStep * _slippage.ValueDecimal
                : stopPrice + priceStep * _slippage.ValueDecimal;

            if (GetSelectedOrderType() == OrderType.Market)
            {
                _tab.CloseAtStopMarket(position, stopPrice);
            }
            else
            {
                _tab.CloseAtStop(position, stopPrice, orderPrice);
            }

            bool changed = !position.StopOrderIsActiv || position.StopOrderPrice != stopPrice;

            if (changed)
            {
                LogDebug(
                    $"Protective stop placed: position={position.Number}, direction={position.Direction}, mode={GetSelectedOrderType()}, " +
                    $"stopPrice={stopPrice:F8}, orderPrice={orderPrice:F8}, entry={position.EntryPrice:F8}, plannedEntry={GetPlannedEntryPrice(position, position.EntryPrice):F8}");
            }
        }

        private void PlaceProfitOrder(Position position, decimal targetPrice)
        {
            if (position == null || targetPrice <= 0)
            {
                return;
            }

            decimal priceStep = _tab?.Security?.PriceStep ?? 0;
            decimal orderPrice = position.Direction == Side.Buy
                ? targetPrice - priceStep * _slippage.ValueDecimal
                : targetPrice + priceStep * _slippage.ValueDecimal;

            if (GetSelectedOrderType() == OrderType.Market)
            {
                _tab.CloseAtProfitMarket(position, targetPrice);
            }
            else
            {
                _tab.CloseAtProfit(position, targetPrice, orderPrice);
            }

            bool changed = !position.ProfitOrderIsActiv || position.ProfitOrderPrice != targetPrice;

            if (changed)
            {
                LogDebug(
                    $"Protective profit placed: position={position.Number}, direction={position.Direction}, mode={GetSelectedOrderType()}, " +
                    $"targetPrice={targetPrice:F8}, orderPrice={orderPrice:F8}, entry={position.EntryPrice:F8}, plannedEntry={GetPlannedEntryPrice(position, position.EntryPrice):F8}");
            }
        }

        private bool IsProtectiveStopPrice(Position position, decimal stopPrice)
        {
            if (position == null || stopPrice <= 0)
            {
                return false;
            }

            if (position.Direction == Side.Buy)
            {
                return stopPrice < position.EntryPrice;
            }

            return stopPrice > position.EntryPrice;
        }

        private bool IsProfitableTarget(Position position, decimal targetPrice)
        {
            if (position == null || targetPrice <= 0)
            {
                return false;
            }

            if (position.Direction == Side.Buy)
            {
                return targetPrice > position.EntryPrice;
            }

            return targetPrice < position.EntryPrice;
        }

        private decimal SelectMoreProtectiveStop(Side side, decimal currentStopPrice, decimal candidateStopPrice)
        {
            if (candidateStopPrice <= 0)
            {
                return currentStopPrice;
            }

            if (currentStopPrice <= 0)
            {
                return candidateStopPrice;
            }

            if (side == Side.Buy)
            {
                return Math.Max(currentStopPrice, candidateStopPrice);
            }

            return Math.Min(currentStopPrice, candidateStopPrice);
        }

        private void RememberAwaitingOpeningSuccess(GridLevelState level)
        {
            if (level == null)
            {
                return;
            }

            _levelsAwaitingOpeningSuccess.Remove(level);
            _levelsAwaitingOpeningSuccess.Add(level);
        }

        private void ForgetAwaitingOpeningSuccess(GridLevelState level)
        {
            if (level == null)
            {
                return;
            }

            _levelsAwaitingOpeningSuccess.Remove(level);
        }

        private void SetLevelPosition(GridLevelState level, Position position)
        {
            if (level == null)
            {
                return;
            }

            UnregisterLevelBinding(level.Position);
            level.Position = position;
            level.CancelRequested = false;
            RegisterLevelBinding(position, level);

            if (position == null)
            {
                ForgetAwaitingOpeningSuccess(level);
            }
        }

        private void RegisterLevelBinding(Position position, GridLevelState level)
        {
            if (position == null || level == null)
            {
                return;
            }

            _levelBindingsByReference[position] = level;

            if (position.Number != 0)
            {
                _levelBindingsByNumber[position.Number] = level;
            }

            RegisterLevelOrderBindings(position, level);
        }

        private void UnregisterLevelBinding(Position position)
        {
            if (position == null)
            {
                return;
            }

            _levelBindingsByReference.Remove(position);

            if (position.Number != 0)
            {
                _levelBindingsByNumber.Remove(position.Number);
            }

            UnregisterLevelOrderBindings(position);
        }

        private void RegisterLevelOrderBindings(Position position, GridLevelState level)
        {
            if (position?.OpenOrders == null || level == null)
            {
                return;
            }

            for (int i = 0; i < position.OpenOrders.Count; i++)
            {
                Order order = position.OpenOrders[i];

                if (order == null)
                {
                    continue;
                }

                if (order.NumberUser != 0)
                {
                    _levelBindingsByOrderUserNumber[order.NumberUser] = level;
                }

                if (!string.IsNullOrWhiteSpace(order.NumberMarket))
                {
                    _levelBindingsByOrderMarketNumber[order.NumberMarket] = level;
                }
            }
        }

        private void UnregisterLevelOrderBindings(Position position)
        {
            if (position?.OpenOrders == null)
            {
                return;
            }

            for (int i = 0; i < position.OpenOrders.Count; i++)
            {
                Order order = position.OpenOrders[i];

                if (order == null)
                {
                    continue;
                }

                if (order.NumberUser != 0)
                {
                    _levelBindingsByOrderUserNumber.Remove(order.NumberUser);
                }

                if (!string.IsNullOrWhiteSpace(order.NumberMarket))
                {
                    _levelBindingsByOrderMarketNumber.Remove(order.NumberMarket);
                }
            }
        }

        protected override List<Func<List<Candle>, bool>> GetCheckers()
        {
            return new List<Func<List<Candle>, bool>>
            {
                candles => candles.Count > Math.Max(_periodSma.ValueInt, _natrLength.ValueInt),
                candles => _zScoreLow.Ready && _zScoreHigh.Ready
            };
        }

        protected override decimal GetVolume(bool getRounded = true)
        {
            if (_volumeManager == null)
            {
                return base.GetVolume(getRounded);
            }

            return _volumeManager.GetNextVolume(getRounded);
        }

        protected override bool CheckOpenLongPosition(List<Candle> candles)
        {
            return false;
        }

        protected override bool CheckOpenShortPosition(List<Candle> candles)
        {
            return false;
        }

        protected override bool CheckClosePosition(List<Candle> candles, Position position)
        {
            if (candles == null || candles.Count == 0 || position == null)
            {
                return false;
            }

            Candle last = candles[candles.Count - 1];
            decimal ema = GetCurrentEma();

            if (_emaStopEnable != null && _emaStopEnable.ValueBool && ema > 0)
            {
                if (position.Direction == Side.Buy && last.Low <= ema)
                {
                    LogDebug($"EMA stop close: direction=Buy, low={last.Low:F8}, ema={ema:F8}, position={position.Number}");
                    return true;
                }

                if (position.Direction == Side.Sell && last.High >= ema)
                {
                    LogDebug($"EMA stop close: direction=Sell, high={last.High:F8}, ema={ema:F8}, position={position.Number}");
                    return true;
                }
            }

            if (_zScoreChannelTpEnable != null && _zScoreChannelTpEnable.ValueBool)
            {
                decimal zScoreTakeProfit = GetZScoreChannelTakeProfit(position);

                if (zScoreTakeProfit > 0)
                {
                    if (position.Direction == Side.Buy && last.High >= zScoreTakeProfit)
                    {
                        LogDebug($"ZScore TP close: direction=Buy, high={last.High:F8}, tp={zScoreTakeProfit:F8}, position={position.Number}");
                        return true;
                    }

                    if (position.Direction == Side.Sell && last.Low <= zScoreTakeProfit)
                    {
                        LogDebug($"ZScore TP close: direction=Sell, low={last.Low:F8}, tp={zScoreTakeProfit:F8}, position={position.Number}");
                        return true;
                    }
                }
            }

            return false;
        }

        protected override void ParametersChangedByUser()
        {
            _debugSessionHeaderLogged = false;
            SetOrderType();
            SetSmaParameters();
            SetEmaParameters();
            SetNatrParameters();
            SetZScoreChannelReference();
            SetVolumeManager();

            _sma?.Save();
            _sma?.Reload();
            _ema?.Save();
            _ema?.Reload();
            _natrAtr?.Save();
            _natrAtr?.Reload();
            _channel?.Save();
        }

        private void SetOrderType()
        {
            if (_orderType == null)
            {
                return;
            }

            if (!Enum.TryParse(_orderType.ValueString, true, out OrderType _))
            {
                _orderType.ValueString = OrderType.Limit.ToString();
            }
        }

        private void SetSmaParameters()
        {
            if (_sma?.Parameters[0] is IndicatorParameterInt smaLength &&
                _periodSma != null)
            {
                smaLength.ValueInt = _periodSma.ValueInt;
            }
        }

        private void SetEmaParameters()
        {
            if (_ema?.Parameters[0] is IndicatorParameterInt emaLength &&
                _emaLength != null)
            {
                emaLength.ValueInt = _emaLength.ValueInt;
            }
        }

        private void SetNatrParameters()
        {
            if (_natrAtr?.Parameters[0] is IndicatorParameterInt natrLength &&
                _natrLength != null)
            {
                natrLength.ValueInt = _natrLength.ValueInt;
            }
        }

        private void SetZScoreChannelReference()
        {
            if (_channel == null || _zEnterBase == null)
            {
                return;
            }

            _channel.ZScoreReference = _zEnterBase.ValueDecimal;
        }

        private void SetVolumeManager()
        {
            if (_volumeManager == null || _r == null)
            {
                return;
            }

            _volumeManager.R = _r.ValueDecimal;
        }
    }
}
