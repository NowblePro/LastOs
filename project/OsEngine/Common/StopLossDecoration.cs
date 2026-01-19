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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="bot"></param>
        /// <param name="fixStop">Если false, тогда стандартная логика фиксированного стопа не будет работать, нужно присвоить <see cref="StopPriceFunc"/> значение.</param>
        /// <param name="paramEnableName"></param>
        /// <param name="tabControlName"></param>
        public StopLossDecoration(BotPanel bot, bool fixStop = true, string paramEnableName = "", string tabControlName = "")
        {
            _bot = bot;
            _tab = bot.TabsSimple[0];
            string nameEnable = paramEnableName;
            string tabName = tabControlName;
            if (string.IsNullOrEmpty(nameEnable))
            {
                nameEnable = "FixStopOn";
            }
            if (string.IsNullOrEmpty(tabControlName))
            {
                tabName = "Base";
            }
            _fixStopOn = bot.CreateParameter(nameEnable, false, tabName);
            if (fixStop)
            {
                _fixStop = bot.CreateParameter("FixStop", 5m, 1, 30, 1, tabName);
            }
            _slippageParam = _bot.Parameters.OfType<StrategyParameterDecimal>().Where(p => p.Name.ToLower() == "slippage").FirstOrDefault();
            _orderTypeParam = _bot.Parameters.OfType<StrategyParameterString>().Where(p => p.Name.ToLower() == "ordertype").FirstOrDefault();
            bot.TabsSimple[0].PositionOpeningSuccesEvent += _tab_PositionOpeningSuccesEvent;
        }

        public bool On
        {
            get => _fixStopOn.ValueBool;
            set
            {
                _fixStopOn.ValueBool = value;
            }
        }

        public Func<Position, decimal> StopPriceFunc { get; set; } = null;

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
                    if (StopPriceFunc != null)
                    {
                        stopPrice = StopPriceFunc.Invoke(position);
                    }
                    else
                    {
                        stopPrice = position.EntryPrice - position.EntryPrice * (_fixStop.ValueDecimal / 100);
                    }
                    stopOrderPrice = stopPrice - _tab.Security.PriceStep * _slippage;
                }
                else if (position.Direction == Side.Sell)
                {
                    if (StopPriceFunc != null)
                    {
                        stopPrice = StopPriceFunc.Invoke(position);
                    }
                    else
                    {
                        stopPrice = position.EntryPrice + position.EntryPrice * (_fixStop.ValueDecimal / 100);
                    }
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
