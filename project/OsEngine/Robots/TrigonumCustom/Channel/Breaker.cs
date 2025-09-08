using OsEngine.Entity;
using OsEngine.OsTrader.Panels;
using OsEngine.OsTrader.Panels.Attributes;
using OsEngine.OsTrader.Panels.Tab;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OsEngine.Robots.TrigonumCustom.Channel
{
    [Bot("Breaker")]
    public class Breaker : BotPanel
    {
        private BotTabSimple _tab;

        #region Parameters
        private StrategyParameterDecimal _maxHigh;
        private StrategyParameterDecimal _minHigh;
        private StrategyParameterDecimal _margin;
        private StrategyParameterInt _rr;
        private StrategyParameterBool _useBody;

        private StrategyParameterString _regimeString;
        private StrategyParameterString _volumeType;
        private StrategyParameterDecimal _slippage;
        private StrategyParameterTimeOfDay _startTradeTime;
        private StrategyParameterTimeOfDay _endTradeTime;
        private StrategyParameterDecimal _volumeOnPosition;
        private StrategyParameterBool _saveJson;
        #endregion

        public Breaker(string name, StartProgram startProgram) : base(name, startProgram)
        {
            TabCreate(BotTabType.Simple);
            _tab = TabsSimple[0];

            #region Common parameters init
            _volumeType = CreateParameter("Volume Type", NUMBER_OF_CONTRACTS, new string[] { NUMBER_OF_CONTRACTS, CONTRACT_CURRENCY, PERCENT }, "Base");
            _slippage = CreateParameter("Slippage", 0.1m, 0.1m, 5, 0.1m, "Base");
            _startTradeTime = CreateParameterTimeOfDay("Start trade time", 0, 0, 0, 0, "Base");
            _endTradeTime = CreateParameterTimeOfDay("End trade time", 24, 0, 0, 0, "Base");
            _volumeOnPosition = CreateParameter("Volume", 10, 1.0m, 50, 4, "Base");
            _saveJson = CreateParameter("Save Json Data", false, "Base");
            #endregion

            #region Breaker parameters
            _maxHigh = new StrategyParameterDecimal("Max High", 1m, 0.6m, 1.5m, 0.05m, "Breaker");
            _minHigh = new StrategyParameterDecimal("Min High", 0.2m, 0.1m, 0.4m, 0.05m, "Breaker");
            _margin = new StrategyParameterDecimal("Margin", 0.2m, 0.1m, 0.4m, 0.05m, "Breaker");
            _rr = new StrategyParameterInt("RR", 2, 1, 3, 1, "Breaker");
            _useBody = new StrategyParameterBool("Use Body", false);
            #endregion
        }

        public override void ShowIndividualSettingsDialog()
        { }
    }
}
