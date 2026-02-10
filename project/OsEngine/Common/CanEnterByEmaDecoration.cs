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
    public class CanEnterByEmaDecoration
    {
        private BotTabSimple _tab;
        private BotPanel _bot;

        private StrategyParameterBool _enabled;
        private List<Candle> _candles;
        private decimal _emaPeriod = 0;

        public CanEnterByEmaDecoration(BotPanel bot)
        {
            _bot = bot;
            _tab = bot.TabsSimple[0];

            _enabled = bot.CreateParameter("Ema Filter Enabled", true, "Ema Filter");

            bot.ParametrsChangeByUser += Bot_ParametrsChangeByUser;
            _tab.CandleFinishedEvent += _tab_CandleFinishedEvent;
        }

        public Aindicator _ema;
        public Aindicator Ema { get => _ema;
        
            set
            {
                _ema = value;
                if (_ema?.Parameters[0] is IndicatorParameterInt parameter)
                {
                    _emaPeriod = parameter.ValueInt;
                }
            }
        }

        private void _tab_CandleFinishedEvent(List<Candle> candles)
        {
            _candles = candles;
        }

        public bool CanBuy => _candles == null || _candles.Count < _emaPeriod || !_enabled.ValueBool || _candles.Last().Close > Ema.DataSeries[0].Last;
        public bool CanSell => _candles == null || _candles.Count < _emaPeriod || !_enabled.ValueBool || _candles.Last().Close < Ema.DataSeries[0].Last;

        private void Bot_ParametrsChangeByUser()
        {
            if (Ema?.Parameters[0] is IndicatorParameterInt parameter)
            {
                _emaPeriod = parameter.ValueInt;
            }
        }
    }
}
