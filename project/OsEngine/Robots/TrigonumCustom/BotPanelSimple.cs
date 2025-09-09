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

        protected abstract void CandleFinishedEvent(List<Candle> candles);
        protected abstract void ParametersChangedByUser();

        public override void ShowIndividualSettingsDialog() { }

        protected enum BotRegime { Off, OnlyLong, OnlyShort, On }
    }
}
