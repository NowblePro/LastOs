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
        private StrategyParameterBool _reverse;
        private List<Candle> _candles;
        private decimal _emaPeriod = 0;

        public CanEnterByEmaDecoration(BotPanel bot)
        {
            _bot = bot;
            _tab = bot.TabsSimple[0];

            _enabled = bot.CreateParameter("Ema Filter Enabled", true, "Ema Filter");
            _reverse = bot.CreateParameter("Ema Filter Reverse", false, "Ema Filter");

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

        public bool CanBuy
        {
            get
            {
                if (!_enabled.ValueBool)
                {
                    return true;
                }

                if (!TryGetCurrentContext(out decimal close, out decimal ema))
                {
                    return false;
                }

                return _reverse.ValueBool
                    ? close < ema
                    : close > ema;
            }
        }

        public bool CanSell
        {
            get
            {
                if (!_enabled.ValueBool)
                {
                    return true;
                }

                if (!TryGetCurrentContext(out decimal close, out decimal ema))
                {
                    return false;
                }

                return _reverse.ValueBool
                    ? close > ema
                    : close < ema;
            }
        }

        public decimal CurrentEma
        {
            get
            {
                if (!TryGetCurrentEma(out decimal ema))
                {
                    return 0;
                }

                return ema;
            }
        }

        public bool IsPriceAllowed(Side side, decimal price)
        {
            if (!_enabled.ValueBool)
            {
                return true;
            }

            if (!TryGetCurrentEma(out decimal ema))
            {
                return false;
            }

            if (side == Side.Buy)
            {
                return _reverse.ValueBool
                    ? price < ema
                    : price > ema;
            }

            if (side == Side.Sell)
            {
                return _reverse.ValueBool
                    ? price > ema
                    : price < ema;
            }

            return true;
        }

        private void Bot_ParametrsChangeByUser()
        {
            if (Ema?.Parameters[0] is IndicatorParameterInt parameter)
            {
                _emaPeriod = parameter.ValueInt;
            }
        }

        private bool TryGetCurrentContext(out decimal close, out decimal ema)
        {
            close = 0;
            ema = 0;

            if (_candles == null || _candles.Count == 0)
            {
                return false;
            }

            if (_candles.Count < _emaPeriod)
            {
                return false;
            }

            if (!TryGetCurrentEma(out ema))
            {
                return false;
            }

            close = _candles[_candles.Count - 1].Close;
            return true;
        }

        private bool TryGetCurrentEma(out decimal ema)
        {
            ema = 0;

            if (Ema == null ||
                Ema.DataSeries == null ||
                Ema.DataSeries.Count == 0 ||
                Ema.DataSeries[0] == null ||
                Ema.DataSeries[0].Values == null ||
                Ema.DataSeries[0].Values.Count == 0)
            {
                return false;
            }

            ema = Ema.DataSeries[0].Last;
            return ema > 0;
        }
    }
}
