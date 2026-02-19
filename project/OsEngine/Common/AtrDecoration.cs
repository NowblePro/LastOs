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
    public class AtrDecoration
    {
        private BotTabSimple _tab;
        private BotPanel _bot;

        private StrategyParameterInt LengthAtr;
        private StrategyParameterDecimal MultiplierAtr;
        private StrategyParameterString _atrRegime;
        private Aindicator _ATR;

        private decimal _lastAtr;
        private decimal _averageAtr;
        private decimal _lastCandleClose;
        private bool _needUpdateLastIndex;
        private bool _needUpdateIterator;
        private int _iterator = 1;

        public AtrDecoration(BotPanel bot, bool showStandartAtr = true)
        {
            _bot = bot;
            _tab = bot.TabsSimple[0];

            LengthAtr = bot.CreateParameter("Length ATR", 96, 7, 1000, 1, "ATR");
            MultiplierAtr = bot.CreateParameter("Multiplier Atr", 1, 1m, 10, 1, "ATR");
            
            _ATR = IndicatorsFactory.CreateIndicatorByName("ATR", (string.IsNullOrEmpty(bot.PublicName) ?  bot.NameStrategyUniq : bot.PublicName) + "Atr", false);
            _ATR = (Aindicator)_tab.CreateCandleIndicator(_ATR, "NewArea");
            _ATR.DataSeries[0].IsPaint = showStandartAtr;
            bot.ParametrsChangeByUser += Bot_ParametrsChangeByUser;
            _tab.CandleFinishedEvent += _tab_CandleFinishedEvent;
        }

        private event EventHandler<bool> _signalCalculated;
        public event EventHandler<bool> SignalCalculated 
        {
            add
            {
                _atrRegime = _bot.CreateParameter("Atr Regime", AtrRegime.Off.ToString(), Enum.GetNames(typeof(AtrRegime)), "ATR");
            }
            remove
            {

            }
        }
        public event EventHandler<AtrRegime> AtrFilterIsOnChanged;

        public AtrRegime AtrRegime => (AtrRegime)Enum.Parse(typeof(AtrRegime), _atrRegime.ValueString);

        public decimal CurrentAtr => _ATR.DataSeries[0].Last;

        public List<decimal> AtrValues => _ATR.DataSeries[0].Values;

        public bool CancelTPSL { get; set; } = true;

        private void _tab_CandleFinishedEvent(List<Candle> candles)
        {
            List<Position> positions = _tab.PositionsOpenAll;
            if (candles.Count == 0) return;
            decimal lastCandle = candles.Last().Close;
            if (_atrRegime != null && (AtrRegime)Enum.Parse(typeof(AtrRegime), _atrRegime.ValueString) != AtrRegime.Off)
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
                    _signalCalculated?.Invoke(this, true);
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
                            if (CancelTPSL)
                            {
                                pos.StopOrderIsActiv = false;
                                pos.ProfitOrderIsActiv = false;
                            }
                        }

                        _tab.BuyAtStopCancel();
                        _tab.SellAtStopCancel();
                    }
                    _needUpdateLastIndex = true;
                    _signalCalculated?.Invoke(this, true);
                    return;
                }
                _signalCalculated?.Invoke(this, false);
            }
        }

        private void Bot_ParametrsChangeByUser()
        {
            ((IndicatorParameterInt)_ATR.Parameters[0]).ValueInt = LengthAtr.ValueInt;
            _ATR.Save();
            _ATR.Reload();
            AtrFilterIsOnChanged?.Invoke(this, (AtrRegime)Enum.Parse(typeof(AtrRegime), _atrRegime.ValueString));
        }
    }

    public enum AtrRegime { Off, On, EntryOnly, ExitOnly }
}
