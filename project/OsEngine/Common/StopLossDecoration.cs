using OsEngine.Entity;
using OsEngine.OsTrader.Panels;
using OsEngine.OsTrader.Panels.Tab;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OsEngine.Common
{
    public class StopLossDecoration
    {
        private StrategyParameterBool _fixStopOn;
        private StrategyParameterDecimal _fixStop;
        private StrategyParameterDecimal _slippageParam;
        private StrategyParameterString _orderTypeParam;
        private BotTabSimple _tab;
        private BotPanel _bot;
        public StopLossDecoration(BotPanel bot)
        {
            _bot = bot;
            _tab = bot.TabsSimple[0];
            _fixStopOn = bot.CreateParameter("FixStopOn", false, "Sessions");
            _fixStop = bot.CreateParameter("FixStop", 5m, 1, 30, 1, "Sessions");
            _slippageParam = _bot.Parameters.OfType<StrategyParameterDecimal>().Where(p => p.Name.ToLower() == "slippage").FirstOrDefault();
            _orderTypeParam = _bot.Parameters.OfType<StrategyParameterString>().Where(p => p.Name.ToLower() == "ordertype").FirstOrDefault();
            bot.TabsSimple[0].PositionOpeningSuccesEvent += _tab_PositionOpeningSuccesEvent;
        }

        private void _tab_PositionOpeningSuccesEvent(Position position)
        {
            decimal _slippage = 0;
            OrderType orderType = OrderType.Limit;
            if (_slippageParam != null)
            {
                _slippage = _slippageParam.ValueDecimal;
            }

            if (_orderTypeParam != null && Enum.TryParse(_orderTypeParam.ValueString, false, out OrderType ot))
            {
                orderType = ot;
            }

            decimal stopPrice = 0;
            decimal stopOrderPrice = 0;

            if (_fixStopOn.ValueBool)
            {
                if (position.Direction == Side.Buy)
                {
                    stopPrice = position.EntryPrice - position.EntryPrice * (_fixStop.ValueDecimal / 100);
                    stopOrderPrice = stopPrice - _tab.Security.PriceStep * _slippage;
                }
                else if (position.Direction == Side.Sell)
                {
                    stopPrice = position.EntryPrice + position.EntryPrice * (_fixStop.ValueDecimal / 100);
                    stopOrderPrice = stopPrice + _tab.Security.PriceStep * _slippage;
                }

                if (orderType == OrderType.Limit)
                {
                    _tab.CloseAtStop(position, stopPrice, stopOrderPrice);
                }
                else if (orderType == OrderType.Market)
                {
                    _tab.CloseAtStopMarket(position, stopPrice);
                }
            }
        }
    }
}
