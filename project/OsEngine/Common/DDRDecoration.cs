using OsEngine.Entity;
using OsEngine.Indicators.TrigonumCustom;
using OsEngine.OsTrader.Panels;
using OsEngine.OsTrader.Panels.Tab;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OsEngine.Common
{
    public class DDRDecoration
    {
        private StrategyParameterDecimal _slippageParam;
        private StrategyParameterString _orderTypeParam;
        private StrategyParameterInt _ddrN;
        private StrategyParameterDecimal _ddrThresh;
        private StrategyParameterDecimal _ddrMult;
        private BotTabSimple _tab;
        private BotPanel _bot;
        private bool _activated = false;

        private DDR _ddr;

        public DDRDecoration(BotPanel bot, DDR ddr)
        {
            _ddr = ddr;
            _bot = bot;
            _tab = bot.TabsSimple[0];
            _ddrN = bot.CreateParameter("DDR N", 24, 20, 50, 5, "DDR");
            _ddrThresh = bot.CreateParameter("DDR Thresh", 2m, 1.5m, 4m, 0.5m, "DDR");
            _ddrMult = bot.CreateParameter("DDR Mult", 2m, 1.5m, 4m, 0.5m, "DDR");
            _slippageParam = _bot.Parameters.OfType<StrategyParameterDecimal>().Where(p => p.Name.ToLower() == "slippage").FirstOrDefault();
            _orderTypeParam = _bot.Parameters.OfType<StrategyParameterString>().Where(p => p.Name.ToLower() == "ordertype").FirstOrDefault();
            bot.ParametrsChangeByUser += Bot_ParametrsChangeByUser;
            _tab.CandleFinishedEvent += _tab_CandleFinishedEvent;
        }

        public event EventHandler DDREvent = null;

        public bool Activated => _activated;

        private void _tab_CandleFinishedEvent(List<Candle> candles)
        {
            if (_ddr.LastValue >= _ddrThresh.ValueDecimal)
            {
                if (!_activated)
                {
                    Activate(true);
                    DDREvent?.Invoke(this, EventArgs.Empty);
                }
            }
            else
            {
                Activate(false);
            }
        }

        public void Activate(bool value)
        {
            _activated = value;
        }

        public void ChangeStep(ref decimal step)
        {
            if (Activated)
            {
                step *= _ddrMult.ValueDecimal;
            }
        }

        public bool BlocksEntry()
        {
            return Activated;
        }

        private void Bot_ParametrsChangeByUser()
        {
            if (_ddr == null || _ddrN == null) return;
            _ddr.N = _ddrN.ValueInt;
        }
    }
}
