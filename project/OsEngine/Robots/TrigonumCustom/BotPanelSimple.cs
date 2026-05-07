using OsEngine.Charts.CandleChart;
using OsEngine.Charts.CandleChart.Indicators;
using OsEngine.Common;
using OsEngine.Entity;
using OsEngine.Logging;
using OsEngine.OsTrader.Panels;
using OsEngine.OsTrader.Panels.Tab;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms.DataVisualization.Charting;

namespace OsEngine.Robots.TrigonumCustom
{
    public abstract class BotPanelSimple : BotPanel
    {
        private class DeferredParityEntry
        {
            public Side Side;
            public decimal Volume;
            public DateTime SignalCandleTime;
        }

        private class PlannedEntryAnchor
        {
            public decimal Price;
            public DateTime SignalCandleTime;
        }

        protected BotTabSimple _tab;
        protected StrategyParameterString _regimeString;
        protected StrategyParameterString _volumeType;
        protected StrategyParameterDecimal _slippage;
        protected StrategyParameterTimeOfDay _startTradeTime;
        protected StrategyParameterTimeOfDay _endTradeTime;
        protected StrategyParameterDecimal _volumeOnPosition;
        protected StrategyParameterBool _saveJson;
        protected BotRegime _regime = BotRegime.Off;
        protected StrategyParameterString _orderType;
        private ChartCandleMaster _chartMaster;
        private LogDecoration _debugLogger;
        private bool _debugSessionHeaderLogged;
        /// <summary>
        /// Если true - разрешено заходить в несколько позиций, false - только одна позиция
        /// </summary>
        protected bool _multiplePosition = false;
        private readonly List<DeferredParityEntry> _deferredParityEntries = new List<DeferredParityEntry>();
        private readonly Dictionary<Position, PlannedEntryAnchor> _plannedEntryAnchors = new Dictionary<Position, PlannedEntryAnchor>();

        public BotPanelSimple(string name, StartProgram startProgram) : base(name, startProgram)
        {
            TabCreate(BotTabType.Simple);
            _tab = TabsSimple[0];

            _regimeString = CreateParameter("Regime", BotRegime.Off.ToString(), Enum.GetNames(typeof(BotRegime)), "Base");
            _volumeType = CreateParameter("Volume Type", NUMBER_OF_CONTRACTS, new string[] { NUMBER_OF_CONTRACTS, CONTRACT_CURRENCY, PERCENT }, "Base");
            _slippage = CreateParameter("Slippage", 0.1m, 0.1m, 5, 0.1m, "Base");
            _startTradeTime = CreateParameterTimeOfDay("Start trade time", 0, 0, 0, 0, "Base");
            _endTradeTime = CreateParameterTimeOfDay("End trade time", 24, 0, 0, 0, "Base");
            _volumeOnPosition = CreateParameter("Volume", 10, 1.0m, 50, 4, "Base");
            _saveJson = CreateParameter("Save Json Data", false, "Base");
            _orderType = CreateParameter("OrderType", OrderType.Limit.ToString(), Enum.GetNames(typeof(OrderType)), "Base");
            GetDebugLogger();
            _tab.CandleFinishedEvent += _tab_CandleFinishedEvent;
            _tab.CandleUpdateEvent += _tab_CandleUpdateEvent;
            ParametrsChangeByUser += BotPanelSimple_ParametrsChangeByUser;
            BotPanelSimple_ParametrsChangeByUser();
            ChartMaster = _tab.GetChartMaster();
        }

        #region Chart Painting
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

        /// <summary>
        /// На случай, если нужно что то отрисовать на графике
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected virtual void OnChartPostPaint(object sender, ChartPaintEventArgs e)
        {

        }

        private void Chart_PostPaint(object sender, ChartPaintEventArgs e)
        {
            OnChartPostPaint(sender, e);
        }
        #endregion

        private void BotPanelSimple_ParametrsChangeByUser()
        {
            _tab.setSaveData(_saveJson.ValueBool);
            SetCommonParameters();
            ParametersChangedByUser();
        }

        private void _tab_CandleFinishedEvent(List<Candle> candles)
        {
            CandleFinishedEvent(candles);
        }

        private void _tab_CandleUpdateEvent(List<Candle> candles)
        {
            CandleUpdateEvent(candles);
        }

        private void SetCommonParameters()
        {
            if (Enum.TryParse(_regimeString.ValueString, out BotRegime regime))
            {
                this._regime = regime;
            }
        }

        protected virtual decimal GetVolume(bool getRounded = true)
        {
            decimal volume = 0;

            if (_volumeType.ValueString == CONTRACT_CURRENCY)
            {
                decimal contractPrice = TabsSimple[0].PriceBestAsk;
                volume = _volumeOnPosition.ValueDecimal / contractPrice;

            }
            else if (_volumeType.ValueString == NUMBER_OF_CONTRACTS)
            {
                volume = _volumeOnPosition.ValueDecimal;
            }
            else if (_volumeType.ValueString == PERCENT)
            {
                volume = _tab.Portfolio.ValueCurrent * (_volumeOnPosition.ValueDecimal / 100) / _tab.PriceBestAsk / _tab.Security.Lot;
            }

            if (getRounded)
            {
                volume = GetRoundedVolume(_tab, volume);
            }

            return volume;
        }

        protected bool IsDebugLoggingEnabled => GetDebugLogger().IsOn;

        protected void LogDebug(string message)
        {
            GetDebugLogger().LogDebug(message);
        }

        protected StrategyParameterBool GetOrCreateDebugLoggingParameter()
        {
            return Parameters?
                .FirstOrDefault(p => p.Name == "Debug Logging") as StrategyParameterBool
                ?? CreateParameter("Debug Logging", false, "Debug");
        }

        protected virtual string GetDebugStrategySnapshot(List<Candle> candles)
        {
            return string.Empty;
        }

        protected virtual string GetDebugOpenIntentSnapshot(List<Candle> candles, Side side)
        {
            return string.Empty;
        }

        protected virtual string GetDebugCloseIntentSnapshot(List<Candle> candles, Position position)
        {
            return string.Empty;
        }

        /// <summary>
        /// Проверки перед основной логикой обработки события обновления свечи, все они должны возвращать true, чтобы пройти фильтр
        /// </summary>
        /// <returns></returns>
        protected abstract List<Func<List<Candle>, bool>> GetCheckers();

        protected virtual bool UseTesterParityMode => false;

        protected bool UseTesterParityModeInLive =>
            UseTesterParityMode && StartProgram == StartProgram.IsOsTrader;

        protected virtual void CandleUpdateEvent(List<Candle> candles)
        {
            if (!UseTesterParityModeInLive)
            {
                return;
            }

            ProcessDeferredParityEntries(candles);
        }

        protected virtual void OnBeforeBaseEntryOrder(
            List<Candle> candles,
            Side side,
            OrderType orderType,
            decimal plannedPrice,
            decimal volume)
        {
        }

        protected void RememberPlannedEntry(Position position, decimal price, DateTime signalCandleTime)
        {
            if (position == null)
            {
                return;
            }

            _plannedEntryAnchors[position] = new PlannedEntryAnchor
            {
                Price = price,
                SignalCandleTime = signalCandleTime
            };
        }

        protected void ForgetPlannedEntry(Position position)
        {
            if (position == null)
            {
                return;
            }

            _plannedEntryAnchors.Remove(position);
        }

        protected decimal GetPlannedEntryPrice(Position position, decimal fallbackPrice)
        {
            if (position != null &&
                _plannedEntryAnchors.TryGetValue(position, out PlannedEntryAnchor anchor))
            {
                return anchor.Price;
            }

            return fallbackPrice;
        }

        protected DateTime? GetPlannedEntrySignalTime(Position position)
        {
            if (position != null &&
                _plannedEntryAnchors.TryGetValue(position, out PlannedEntryAnchor anchor))
            {
                return anchor.SignalCandleTime;
            }

            return null;
        }

        protected DateTime GetLatestFinishedCandleTime()
        {
            List<Candle> candles = _tab?.CandlesFinishedOnly;

            if (candles == null || candles.Count == 0)
            {
                return DateTime.MinValue;
            }

            return candles[candles.Count - 1].TimeStart;
        }

        protected Position OpenPlannedLimit(Side side, decimal volume, decimal price, DateTime signalCandleTime)
        {
            Position position = null;

            if (side == Side.Buy)
            {
                position = _tab.BuyAtLimit(volume, price);
            }
            else if (side == Side.Sell)
            {
                position = _tab.SellAtLimit(volume, price);
            }

            RememberPlannedEntry(position, price, signalCandleTime);

            return position;
        }

        protected Position OpenPlannedLimit(Side side, decimal volume, decimal price, DateTime signalCandleTime, string signalType)
        {
            Position position = null;

            if (side == Side.Buy)
            {
                position = _tab.BuyAtLimit(volume, price, signalType);
            }
            else if (side == Side.Sell)
            {
                position = _tab.SellAtLimit(volume, price, signalType);
            }

            RememberPlannedEntry(position, price, signalCandleTime);

            return position;
        }

        protected Position OpenPlannedMarket(Side side, decimal volume, decimal plannedPrice, DateTime signalCandleTime)
        {
            Position position = null;

            if (side == Side.Buy)
            {
                position = _tab.BuyAtMarket(volume);
            }
            else if (side == Side.Sell)
            {
                position = _tab.SellAtMarket(volume);
            }

            RememberPlannedEntry(position, plannedPrice, signalCandleTime);

            return position;
        }

        protected Position OpenPlannedMarket(Side side, decimal volume, decimal plannedPrice, DateTime signalCandleTime, string signalType)
        {
            Position position = null;

            if (side == Side.Buy)
            {
                position = _tab.BuyAtMarket(volume, signalType);
            }
            else if (side == Side.Sell)
            {
                position = _tab.SellAtMarket(volume, signalType);
            }

            RememberPlannedEntry(position, plannedPrice, signalCandleTime);

            return position;
        }

        protected Position OpenPlannedFake(
            Side side,
            decimal volume,
            decimal plannedPrice,
            decimal fillPrice,
            DateTime fillTime,
            DateTime signalCandleTime)
        {
            Position position = null;

            if (side == Side.Buy)
            {
                position = _tab.BuyAtFake(volume, fillPrice, fillTime);
            }
            else if (side == Side.Sell)
            {
                position = _tab.SellAtFake(volume, fillPrice, fillTime);
            }

            RememberPlannedEntry(position, plannedPrice, signalCandleTime);

            return position;
        }

        protected Position OpenPlannedFake(
            Side side,
            decimal volume,
            decimal plannedPrice,
            decimal fillPrice,
            DateTime fillTime,
            DateTime signalCandleTime,
            string signalType)
        {
            Position position = null;

            if (side == Side.Buy)
            {
                position = _tab.BuyAtFake(volume, fillPrice, fillTime, signalType);
            }
            else if (side == Side.Sell)
            {
                position = _tab.SellAtFake(volume, fillPrice, fillTime, signalType);
            }

            RememberPlannedEntry(position, plannedPrice, signalCandleTime);

            return position;
        }

        private void QueueDeferredParityEntry(Side side, decimal volume, DateTime signalCandleTime)
        {
            if (_deferredParityEntries.Any(entry =>
                    entry.SignalCandleTime == signalCandleTime &&
                    entry.Side == side))
            {
                return;
            }

            _deferredParityEntries.Add(new DeferredParityEntry
            {
                Side = side,
                Volume = volume,
                SignalCandleTime = signalCandleTime
            });
        }

        private void ProcessDeferredParityEntries(List<Candle> candles)
        {
            if (candles == null ||
                candles.Count == 0 ||
                _deferredParityEntries.Count == 0)
            {
                return;
            }

            Candle currentCandle = candles[candles.Count - 1];

            if (currentCandle == null)
            {
                return;
            }

            List<DeferredParityEntry> readyEntries = _deferredParityEntries
                .Where(entry => currentCandle.TimeStart > entry.SignalCandleTime)
                .ToList();

            for (int i = 0; i < readyEntries.Count; i++)
            {
                DeferredParityEntry entry = readyEntries[i];
                Position position = null;

                if (entry.Side == Side.Buy)
                {
                    position = _tab.BuyAtMarket(entry.Volume);
                }
                else if (entry.Side == Side.Sell)
                {
                    position = _tab.SellAtMarket(entry.Volume);
                }

                decimal plannedEntryPrice = currentCandle.Open != 0
                    ? currentCandle.Open
                    : currentCandle.Close;

                RememberPlannedEntry(position, plannedEntryPrice, entry.SignalCandleTime);
                _deferredParityEntries.Remove(entry);
            }
        }

        protected virtual void CandleFinishedEvent(List<Candle> candles)
        {
            if (candles == null || candles.Count == 0)
            {
                return;
            }

            Candle last = candles.Last();
            if (last.State != CandleState.Finished)
            {
                return;
            }

            LogDebugSessionHeader(last);

            if (GetCheckers().Any(p => !p(candles)))
            {
                LogDebug($"Skip candle: generic checker blocked, {GetCandleSnapshot(last)}, {GetPositionsSnapshot()}");
                return;
            }

            decimal lastPrice = last.Close;
            List<Position> positions = _tab.PositionsOpenAll;
            decimal slippage = _slippage.ValueDecimal * lastPrice / 100;
            OrderType orderType = OrderType.Limit;

            if (_regime == BotRegime.Off)
            {
                LogDebug($"Skip candle: regime off, {GetCandleSnapshot(last)}");
                return;
            }

            if (Enum.TryParse(_orderType.ValueString, true, out OrderType ot))
            {
                orderType = ot;
            }

            LogDebug(
                $"Candle step: {GetCandleSnapshot(last)}, orderType={orderType}, slippage={slippage:F8}, " +
                $"{GetPositionsSnapshot()}{AppendDebugFragment(GetDebugStrategySnapshot(candles))}");

            if (positions.Count > 0)
            {
                foreach (Position pos in positions)
                {
                    if (CheckClosePosition(candles, pos))
                    {
                        LogDebug(
                            $"Close signal: position={pos.Number}, side={pos.Direction}, entry={pos.EntryPrice:F8}, lastClose={lastPrice:F8}, " +
                            $"volume={pos.OpenVolume:F8}{AppendDebugFragment(GetDebugCloseIntentSnapshot(candles, pos))}");

                        if (pos.Direction == Side.Buy)
                        {
                            _tab.CloseAtStop(pos, lastPrice, lastPrice - slippage);
                        }
                        else if (pos.Direction == Side.Sell)
                        {
                            _tab.CloseAtStop(pos, lastPrice, lastPrice + slippage);
                        }
                    }
                }
            }
            if (positions.Count == 0 || _multiplePosition)
            {
                if (_regime != BotRegime.OnlyShort && CheckOpenLongPosition(candles))
                {
                    decimal volume = GetVolume();
                    DateTime signalCandleTime = last.TimeStart;

                    if (orderType == OrderType.Market || orderType == OrderType.MarketNextOpen)
                    {
                        decimal plannedPrice = last.Close;
                        OnBeforeBaseEntryOrder(candles, Side.Buy, orderType, plannedPrice, volume);
                        LogDebug(
                            $"Open signal: side=Buy, orderType={orderType}, plannedPrice={plannedPrice:F8}, volume={volume:F8}, " +
                            $"signalTime={FormatDateTime(signalCandleTime)}{AppendDebugFragment(GetDebugOpenIntentSnapshot(candles, Side.Buy))}");

                        if (UseTesterParityModeInLive)
                        {
                            QueueDeferredParityEntry(Side.Buy, volume, signalCandleTime);
                            LogDebug("Open signal queued for tester parity mode in live.");
                        }
                        else
                        {
                            OpenPlannedMarket(Side.Buy, volume, plannedPrice, signalCandleTime);
                        }
                    }
                    else if (orderType == OrderType.Limit)
                    {
                        decimal plannedPrice = lastPrice + slippage;
                        OnBeforeBaseEntryOrder(candles, Side.Buy, orderType, plannedPrice, volume);
                        LogDebug(
                            $"Open signal: side=Buy, orderType={orderType}, plannedPrice={plannedPrice:F8}, volume={volume:F8}, " +
                            $"signalTime={FormatDateTime(signalCandleTime)}{AppendDebugFragment(GetDebugOpenIntentSnapshot(candles, Side.Buy))}");
                        OpenPlannedLimit(Side.Buy, volume, plannedPrice, signalCandleTime);
                    }
                }
                else if (_regime != BotRegime.OnlyLong && CheckOpenShortPosition(candles))
                {
                    decimal volume = GetVolume();
                    DateTime signalCandleTime = last.TimeStart;

                    if (orderType == OrderType.Market || orderType == OrderType.MarketNextOpen)
                    {
                        decimal plannedPrice = last.Close;
                        OnBeforeBaseEntryOrder(candles, Side.Sell, orderType, plannedPrice, volume);
                        LogDebug(
                            $"Open signal: side=Sell, orderType={orderType}, plannedPrice={plannedPrice:F8}, volume={volume:F8}, " +
                            $"signalTime={FormatDateTime(signalCandleTime)}{AppendDebugFragment(GetDebugOpenIntentSnapshot(candles, Side.Sell))}");

                        if (UseTesterParityModeInLive)
                        {
                            QueueDeferredParityEntry(Side.Sell, volume, signalCandleTime);
                            LogDebug("Open signal queued for tester parity mode in live.");
                        }
                        else
                        {
                            OpenPlannedMarket(Side.Sell, volume, plannedPrice, signalCandleTime);
                        }
                    }
                    else if (orderType == OrderType.Limit)
                    {
                        decimal plannedPrice = lastPrice - slippage;
                        OnBeforeBaseEntryOrder(candles, Side.Sell, orderType, plannedPrice, volume);
                        LogDebug(
                            $"Open signal: side=Sell, orderType={orderType}, plannedPrice={plannedPrice:F8}, volume={volume:F8}, " +
                            $"signalTime={FormatDateTime(signalCandleTime)}{AppendDebugFragment(GetDebugOpenIntentSnapshot(candles, Side.Sell))}");
                        OpenPlannedLimit(Side.Sell, volume, plannedPrice, signalCandleTime);
                    }
                }
                else
                {
                    LogDebug($"No entry signal on candle {FormatDateTime(last.TimeStart)}.");
                }
            }
            else
            {
                LogDebug("Skip new entry: active position exists and multiple positions are disabled.");
            }
        }

        /// <summary>
        /// Проверить, соответствуют ли условия открытию позиции в лонг
        /// </summary>
        /// <param name="candles"></param>
        /// <returns></returns>
        protected abstract bool CheckOpenLongPosition(List<Candle> candles);

        /// <summary>
        /// Проверить, соответствуют ли условия открытию позиции в шорт
        /// </summary>
        /// <param name="candles"></param>
        /// <returns></returns>
        protected abstract bool CheckOpenShortPosition(List<Candle> candles);

        /// <summary>
        /// Проверить, соответствуют ли условия закрытия позиции
        /// </summary>
        /// <param name="candles"></param>
        /// <returns></returns>
        protected abstract bool CheckClosePosition(List<Candle> candles, Position position);

        protected abstract void ParametersChangedByUser();

        public override void ShowIndividualSettingsDialog() { }

        private LogDecoration GetDebugLogger()
        {
            if (_debugLogger == null)
            {
                _debugLogger = new LogDecoration(this);
                _tab.PositionOpeningSuccesEvent += _tab_PositionOpeningSuccesEvent_BaseDebug;
                _tab.PositionOpeningFailEvent += _tab_PositionOpeningFailEvent_BaseDebug;
                _tab.PositionClosingSuccesEvent += _tab_PositionClosingSuccesEvent_BaseDebug;
            }

            return _debugLogger;
        }

        private void _tab_PositionOpeningSuccesEvent_BaseDebug(Position position)
        {
            if (!IsDebugLoggingEnabled || position == null)
            {
                return;
            }

            decimal plannedPrice = GetPlannedEntryPrice(position, position.EntryPrice);
            DateTime? signalTime = GetPlannedEntrySignalTime(position);
            decimal entryDelta = position.EntryPrice - plannedPrice;
            string lag = signalTime.HasValue && position.TimeOpen != DateTime.MinValue
                ? (position.TimeOpen - signalTime.Value).ToString()
                : "n/a";

            LogDebug(
                $"Position opened: number={position.Number}, side={position.Direction}, plannedPrice={plannedPrice:F8}, " +
                $"actualEntry={position.EntryPrice:F8}, delta={entryDelta:F8}, signalTime={FormatDateTime(signalTime)}, " +
                $"fillTime={FormatDateTime(position.TimeOpen)}, lag={lag}, volume={position.OpenVolume:F8}, state={position.State}, signalOpen={position.SignalTypeOpen}");
        }

        private void _tab_PositionOpeningFailEvent_BaseDebug(Position position)
        {
            if (!IsDebugLoggingEnabled || position == null)
            {
                return;
            }

            LogDebug(
                $"Position opening failed: number={position.Number}, side={position.Direction}, plannedPrice={GetPlannedEntryPrice(position, position.EntryPrice):F8}, " +
                $"signalTime={FormatDateTime(GetPlannedEntrySignalTime(position))}, state={position.State}, signalOpen={position.SignalTypeOpen}");

            ForgetPlannedEntry(position);
        }

        private void _tab_PositionClosingSuccesEvent_BaseDebug(Position position)
        {
            if (!IsDebugLoggingEnabled || position == null)
            {
                return;
            }

            LogDebug(
                $"Position closed: number={position.Number}, side={position.Direction}, entry={position.EntryPrice:F8}, close={position.ClosePrice:F8}, " +
                $"volume={position.OpenVolume:F8}, pnl={position.ProfitPortfolioPunkt:F8}, timeOpen={FormatDateTime(position.TimeOpen)}, " +
                $"timeClose={FormatDateTime(position.TimeClose)}, signalClose={position.SignalTypeClose}");

            ForgetPlannedEntry(position);
        }

        private void LogDebugSessionHeader(Candle last)
        {
            if (!IsDebugLoggingEnabled || _debugSessionHeaderLogged)
            {
                return;
            }

            string securityName = _tab?.Security?.Name ?? _tab?.Connector?.SecurityName ?? "n/a";
            string securityClass = _tab?.Security?.NameClass ?? _tab?.Connector?.SecurityClass ?? "n/a";
            string timeFrame = _tab?.TimeFrameBuilder?.TimeFrame.ToString() ?? "n/a";
            string marketDataType = _tab?.Connector?.CandleMarketDataType.ToString() ?? "n/a";
            string candleCreateType = _tab?.Connector?.CandleCreateMethodType.ToString() ?? "n/a";

            LogDebug(
                $"Debug session context: program={StartProgram}, bot={NameStrategyUniq}, security={securityName}, class={securityClass}, " +
                $"timeframe={timeFrame}, candleType={marketDataType}/{candleCreateType}, regime={_regime}, orderType={_orderType?.ValueString}, " +
                $"volumeType={_volumeType?.ValueString}, volumeSetting={_volumeOnPosition?.ValueDecimal:F8}, time={FormatDateTime(last?.TimeStart)}");

            _debugSessionHeaderLogged = true;
        }

        private string GetCandleSnapshot(Candle candle)
        {
            if (candle == null)
            {
                return "candle=n/a";
            }

            return
                $"candle={FormatDateTime(candle.TimeStart)}, open={candle.Open:F8}, high={candle.High:F8}, low={candle.Low:F8}, close={candle.Close:F8}, volume={candle.Volume:F8}";
        }

        private string GetPositionsSnapshot()
        {
            int positionsCount = _tab?.PositionsOpenAll?.Count ?? 0;
            decimal portfolio = _tab?.Portfolio?.ValueCurrent ?? 0;
            decimal bestBid = _tab?.PriceBestBid ?? 0;
            decimal bestAsk = _tab?.PriceBestAsk ?? 0;
            return $"positions={positionsCount}, portfolio={portfolio:F8}, bestBid={bestBid:F8}, bestAsk={bestAsk:F8}";
        }

        protected string FormatDateTime(DateTime? time)
        {
            if (!time.HasValue || time.Value == DateTime.MinValue)
            {
                return "n/a";
            }

            return time.Value.ToString("yyyy-MM-dd HH:mm:ss.fff");
        }

        private string AppendDebugFragment(string fragment)
        {
            if (string.IsNullOrWhiteSpace(fragment))
            {
                return string.Empty;
            }

            return ", " + fragment.Trim();
        }

        protected enum BotRegime { Off, OnlyLong, OnlyShort, On }
    }
}
