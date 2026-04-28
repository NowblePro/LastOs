using OsEngine.Entity;
using OsEngine.Indicators.TrigonumCustom;
using OsEngine.OsTrader.Panels;
using OsEngine.OsTrader.Panels.Tab;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
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
        private string robotNameUniq;
        private string robotPath;

        public LogDecoration(BotPanel bot)
        {
            _bot = bot;
            _tab = bot.TabsSimple[0];
            _debugLogging = bot.Parameters?
                .FirstOrDefault(p => p.Name == "Debug Logging") as StrategyParameterBool;

            if (_debugLogging == null)
            {
                _debugLogging = bot.CreateParameter("Debug Logging", false, "Debug");
            }

            bot.ParametrsChangeByUser += Bot_ParametrsChangeByUser;
            string path = Path.Combine(AppContext.BaseDirectory, "Engine", "Log", "Robots");
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
            robotNameUniq = _bot.NameStrategyUniq;
            robotPath = Path.Combine(AppContext.BaseDirectory, "Engine", "Log", "Robots", $"{_bot.NameStrategyUniq}.txt");
        }

        public bool IsOn => _debugLogging?.ValueBool ?? false;

        private string RobotPath
        {
            get
            {
                if (string.IsNullOrEmpty(_bot.NameStrategyUniq)) return "";
                if (_bot.NameStrategyUniq != robotNameUniq)
                {
                    robotPath = Path.Combine(AppContext.BaseDirectory, "Engine", "Log", "Robots", $"{_bot.NameStrategyUniq}.txt");
                    robotNameUniq = _bot.NameStrategyUniq;
                }
                return robotPath;
            }
        }

        public void LogDebug(string message)
        {
            if (_debugLogging == null || !_debugLogging.ValueBool) return;

            try
            {
                _bot.SendNewLogMessage(message, Logging.LogMessageType.System);

                string robotPath = RobotPath;
                if (_tab.StartProgram == StartProgram.IsOsOptimizer || string.IsNullOrEmpty(robotPath))
                {
                    return;
                }

                if (!message.EndsWith("\n") && !message.EndsWith("\r"))
                {
                    message += Environment.NewLine;
                }

                File.AppendAllText(robotPath, message);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Log Error: {ex.Message}");
            }
        }

        private void Bot_ParametrsChangeByUser()
        {

        }
    }
}
