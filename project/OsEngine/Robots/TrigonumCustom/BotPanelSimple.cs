using OsEngine.Entity;
using OsEngine.OsTrader.Panels;
using OsEngine.OsTrader.Panels.Tab;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

            _tab.CandleFinishedEvent += _tab_CandleFinishedEvent;
            ParametrsChangeByUser += BotPanelSimple_ParametrsChangeByUser;
        }

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
            if (GetCheckers().Any(p => !p(candles))) return;
            decimal lastPrice = candles.Last().Close;
            List<Position> positions = _tab.PositionsOpenAll;
            decimal slippage = _slippage.ValueDecimal * lastPrice / 100;
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
            else
            {
                if (CheckOpenLongPosition(candles))
                {
                    _tab.BuyAtLimit(GetVolume(), lastPrice + slippage);
                }
                else if (CheckOpenShortPosition(candles))
                {
                    _tab.SellAtLimit(GetVolume(), lastPrice - slippage);
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
