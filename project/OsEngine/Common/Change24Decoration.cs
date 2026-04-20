using Microsoft.Office.Interop.Excel;
using OsEngine.Charts.CandleChart.Indicators;
using OsEngine.Entity;
using OsEngine.Indicators;
using OsEngine.Market.Servers.Bitfinex.BitfitnexEntity;
using OsEngine.OsTrader.Panels;
using OsEngine.OsTrader.Panels.Tab;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace OsEngine.Common
{
    /// <summary>
    /// Декоратор, показывающий изменение цены в процентах, относительно цены 24 часа назад.
    /// </summary>
    public class Change24Decoration
    {
        private BotTabSimple _tab;
        private BotPanel _bot;
        private int _period = 96;

        private StrategyParameterDecimal _thresh;
        private StrategyParameterBool _enabled;

        public Change24Decoration(BotPanel bot)
        {
            _bot = bot;
            _tab = bot.TabsSimple[0];

            _thresh = bot.CreateParameter("Thresh", 3m, 1m, 5m, 0.5m, "Change24");
            _enabled = bot.CreateParameter("Change24 Enabled", true, "Change24");

            bot.ParametrsChangeByUser += Bot_ParametrsChangeByUser;
            _tab.CandleFinishedEvent += _tab_CandleFinishedEvent;
        }

        /// <summary>
        /// Изменение цены в процентах
        /// </summary>
        public decimal Change { get; private set; } = 0;

        private void _tab_CandleFinishedEvent(List<Candle> candles)
        {
            int period = GetPeriod();
            int lastIndex = candles.Count - 1;
            int prevIndex = lastIndex - period;

            if (prevIndex < 0) return;

            decimal prevPrice = candles[prevIndex].Close;

            if (prevPrice <= 0) return;

            decimal curPrice = candles[lastIndex].Close;
            Change = (curPrice - prevPrice) / prevPrice * 100;
        }

        public bool CanBuy => !_enabled.ValueBool || Change > 0 || (-Change < _thresh.ValueDecimal);

        public bool CanSell => !_enabled.ValueBool || Change < 0 || (Change < _thresh.ValueDecimal);

        private void Bot_ParametrsChangeByUser()
        {

        }

        private int GetPeriod()
        {
            TimeSpan timeFrame = _tab?.TimeFrame ?? TimeSpan.Zero;

            if (timeFrame <= TimeSpan.Zero || timeFrame.TotalMinutes <= 0)
            {
                return _period;
            }

            return Math.Max(1, (int)Math.Ceiling(TimeSpan.FromHours(24).TotalMinutes / timeFrame.TotalMinutes));
        }
    }
}
