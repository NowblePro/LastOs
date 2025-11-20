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
    public class StopAtrDecoration
    {
        private BotTabSimple _tab;
        private BotPanel _bot;

        private StrategyParameterInt LengthAtr;
        private StrategyParameterDecimal MultiplierAtr;
        private StrategyParameterBool AtrFilterIsOn;
        private Aindicator _ATR;

        private decimal _lastAtr;
        private decimal _averageAtr;
        private decimal _lastCandleClose;
        private bool _needUpdateLastIndex;
        private bool _needUpdateIterator;
        private int _iterator = 1;

        public StopAtrDecoration(BotPanel bot)
        {
            _bot = bot;
            _tab = bot.TabsSimple[0];

            LengthAtr = bot.CreateParameter("Length ATR", 96, 7, 1000, 1, "ATR");
            MultiplierAtr = bot.CreateParameter("Multiplier Atr", 1, 1m, 10, 1, "ATR");
            AtrFilterIsOn = bot.CreateParameter("Is Atr Filter On", false, "ATR");
            _ATR = IndicatorsFactory.CreateIndicatorByName("ATR", (string.IsNullOrEmpty(bot.PublicName) ?  bot.NameStrategyUniq : bot.PublicName) + "Atr", false);
            _ATR = (Aindicator)_tab.CreateCandleIndicator(_ATR, "NewArea");
            bot.ParametrsChangeByUser += Bot_ParametrsChangeByUser;
            _tab.CandleFinishedEvent += _tab_CandleFinishedEvent;
        }

        public event EventHandler AtrStop;

        private void _tab_CandleFinishedEvent(List<Candle> candles)
        {
            List<Position> positions = _tab.PositionsOpenAll;
            decimal lastCandle = candles.Last().Close;
            if (positions.Count > 0 && AtrFilterIsOn.ValueBool)
            {
                if (_ATR.DataSeries[0].Last == 0 && _needUpdateIterator)
                {
                    _lastCandleClose = 0;
                    _averageAtr = 0;
                    _iterator = 1;
                    _needUpdateIterator = false;
                }

                if (candles.Count < LengthAtr.ValueInt)
                {
                    return;
                }

                _lastAtr = _ATR.DataSeries[0].Last;

                if (_ATR.DataSeries[0].Values.Count >= LengthAtr.ValueInt * _iterator)
                {
                    _lastCandleClose = lastCandle;
                    _averageAtr = _lastAtr;
                    _iterator++;
                    _needUpdateLastIndex = false;
                    _needUpdateIterator = true;
                }

                if (_needUpdateLastIndex || Math.Abs(lastCandle - _lastCandleClose) > _averageAtr * MultiplierAtr.ValueDecimal)
                {
                    if (_tab.PositionsOpenAll.Count > 0)
                    {
                        for (int i = 0; i < positions.Count; i++)
                        {
                            Position pos = positions[i];

                            pos.StopOrderIsActiv = false;
                            pos.ProfitOrderIsActiv = false;
                        }

                        _tab.BuyAtStopCancel();
                        _tab.SellAtStopCancel();
                    }
                    _needUpdateLastIndex = true;
                    return;
                }
                AtrStop?.Invoke(this, EventArgs.Empty);
            }
        }

        private void Bot_ParametrsChangeByUser()
        {
            ((IndicatorParameterInt)_ATR.Parameters[0]).ValueInt = LengthAtr.ValueInt;
            _ATR.Save();
            _ATR.Reload();
        }
    }
}
