using OsEngine.Entity;
using OsEngine.OsTrader.Panels;
using OsEngine.OsTrader.Panels.Tab;
using OsEngine.Robots.Classes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OsEngine.Common
{
    public class TrailingStopDecoration
    {
        private TrailingStop _trailingStop;
        private StrategyParameterBool _trailingStopIsOn;
        private StrategyParameterString _trailingStopTypeOrder;
        private StrategyParameterDecimal _changeStepStop;
        private StrategyParameterDecimal _minDist;
        private StrategyParameterDecimal _quantityStepsPrices;
        private StrategyParameterString _pointOrPercent;

        private BotTabSimple _tab;

        public TrailingStopDecoration(BotPanel bot)
        {
            _trailingStopIsOn = bot.CreateParameter("Is Trailing stop On", false, "Trailing Stop");
            _trailingStopTypeOrder = bot.CreateParameter("Type order", OrderPriceType.Market.ToString(), new[] { OrderPriceType.Market.ToString(), OrderPriceType.Limit.ToString() }, "Trailing Stop");
            _pointOrPercent = bot.CreateParameter("Choise Points or Percent", "Points", new[] { "Points", "Percent" }, "Trailing Stop");
            _changeStepStop = bot.CreateParameter("Stop level change step", 1, 1, 10000, 001m, "Trailing Stop");
            _minDist = bot.CreateParameter("Minimum distance to price", 1, 1, 10000, 0.01m, "Trailing Stop");
            _quantityStepsPrices = bot.CreateParameter("Quantity steps prices for limit order", 0m, 0, 10000, 1, "Trailing Stop");

            _tab = bot.TabsSimple[0];
            bot.ParametrsChangeByUser += Bot_ParametrsChangeByUser;
            Bot_ParametrsChangeByUser();
            _tab.PositionOpeningSuccesEvent += Tab_PositionOpeningSuccesEvent;
            _tab.CandleFinishedEvent += _tab_CandleFinishedEvent;
        }

        private void _tab_CandleFinishedEvent(List<Candle> candles)
        {
            if (_trailingStopIsOn.ValueBool)
            {
                _trailingStop.SetTrailingStop(candles.Last().Close);
            }
        }

        private void Tab_PositionOpeningSuccesEvent(Position position)
        {
            _tab.SellAtStopCancel();
            _tab.BuyAtStopCancel();

            if (_trailingStopIsOn.ValueBool)
            {
                _trailingStop?.SetTrailingStop(position.EntryPrice);
            }
        }

        private void Bot_ParametrsChangeByUser()
        {
            SetTrailingParameters();
        }

        private void SetTrailingParameters()
        {
            if (_trailingStopIsOn.ValueBool)
            {
                _trailingStop = new TrailingStop(_tab, _trailingStopTypeOrder.ValueString, _changeStepStop.ValueDecimal, _minDist.ValueDecimal, _quantityStepsPrices.ValueDecimal, _pointOrPercent.ValueString);
            }
        }
    }
}
