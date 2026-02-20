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
    public class LogDecoration
    {
        private StrategyParameterBool _debugLogging;

        private BotTabSimple _tab;
        private BotPanel _bot;

        public LogDecoration(BotPanel bot)
        {
            _bot = bot;
            _tab = bot.TabsSimple[0];
            _debugLogging = bot.CreateParameter("Debug Logging", false, "Robot");
            bot.ParametrsChangeByUser += Bot_ParametrsChangeByUser;
        }

        public void LogDebug(string message)
        {
            if (_debugLogging != null && _debugLogging.ValueBool)
            {
                _bot.SendNewLogMessage(message, Logging.LogMessageType.System);
            }
        }

        private void Bot_ParametrsChangeByUser()
        {

        }
    }
}
