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
                    if (orderType == OrderType.Market)
                    {
                        _tab.BuyAtMarket(GetVolume());
                    }
                    else if (orderType == OrderType.Limit)
                    {
                        _tab.BuyAtLimit(GetVolume(), lastPrice + slippage);
                    }
                }
                else if (_regime != BotRegime.OnlyLong && CheckOpenShortPosition(candles))
                {
                    if (orderType == OrderType.Market)
                    {
                        _tab.SellAtMarket(GetVolume());
                    }
                    else if (orderType == OrderType.Limit)
                    {
                        _tab.SellAtLimit(GetVolume(), lastPrice - slippage);
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
