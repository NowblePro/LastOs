using OsEngine.Charts.CandleChart;
using OsEngine.Charts.CandleChart.Indicators;
using OsEngine.Common;
using OsEngine.Entity;
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
            Candle last = candles.Last();
            if (last.State != CandleState.Finished)
            {
                return;
            }
            if (GetCheckers().Any(p => !p(candles))) return;
            decimal lastPrice = last.Close;
            List<Position> positions = _tab.PositionsOpenAll;
            decimal slippage = _slippage.ValueDecimal * lastPrice / 100;
            OrderType orderType = OrderType.Limit;

            if (_regime == BotRegime.Off) return;

            if (Enum.TryParse(_orderType.ValueString, true, out OrderType ot))
            {
                orderType = ot;
            }
            if (positions.Count > 0)
            {
                foreach (Position pos in positions)
                {
                    if (CheckClosePosition(candles, pos))
                    {
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

                    if (orderType == OrderType.Market)
                    {
                        decimal plannedPrice = last.Close;
                        OnBeforeBaseEntryOrder(candles, Side.Buy, orderType, plannedPrice, volume);

                        if (UseTesterParityModeInLive)
                        {
                            QueueDeferredParityEntry(Side.Buy, volume, signalCandleTime);
                        }
                        else
                        {
                            _tab.BuyAtMarket(volume);
                        }
                    }
                    else if (orderType == OrderType.Limit)
                    {
                        decimal plannedPrice = lastPrice + slippage;
                        OnBeforeBaseEntryOrder(candles, Side.Buy, orderType, plannedPrice, volume);
                        OpenPlannedLimit(Side.Buy, volume, plannedPrice, signalCandleTime);
                    }
                }
                else if (_regime != BotRegime.OnlyLong && CheckOpenShortPosition(candles))
                {
                    decimal volume = GetVolume();
                    DateTime signalCandleTime = last.TimeStart;

                    if (orderType == OrderType.Market)
                    {
                        decimal plannedPrice = last.Close;
                        OnBeforeBaseEntryOrder(candles, Side.Sell, orderType, plannedPrice, volume);

                        if (UseTesterParityModeInLive)
                        {
                            QueueDeferredParityEntry(Side.Sell, volume, signalCandleTime);
                        }
                        else
                        {
                            _tab.SellAtMarket(volume);
                        }
                    }
                    else if (orderType == OrderType.Limit)
                    {
                        decimal plannedPrice = lastPrice - slippage;
                        OnBeforeBaseEntryOrder(candles, Side.Sell, orderType, plannedPrice, volume);
                        OpenPlannedLimit(Side.Sell, volume, plannedPrice, signalCandleTime);
                    }
                }
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

        protected enum BotRegime { Off, OnlyLong, OnlyShort, On }
    }
}
