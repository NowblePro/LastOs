using OsEngine.Charts.CandleChart;
using OsEngine.Common;
using OsEngine.Entity;

using OsEngine.Indicators;
using OsEngine.Indicators.TrigonumCustom;
using OsEngine.OsTrader.Panels;
using OsEngine.OsTrader.Panels.Attributes;
using OsEngine.OsTrader.Panels.Tab;
using OsEngine.Robots.TrigonumCustom.Base;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms.DataVisualization.Charting;

namespace OsEngine.Robots.TrigonumCustom.Channel
{
    [Bot("Breaker")]
    public class Breaker : BotPanelSimple
    {
        #region Parameters
        private StrategyParameterDecimal _maxHigh;
        private StrategyParameterDecimal _minHigh;
        #endregion

        private OrderBlockZigZag _ob;
        private StrategyParameterInt _lengthZZ;

        /// <summary>
        /// Значение в процентах (от высоты брейкера), на сколько минимум должна пересечь цена ближнюю границу брейкера, чтобы засчиталось условие входа в сделку
        /// </summary>
        private StrategyParameterDecimal _touchTolerance;

        /// <summary>
        /// Параметр входа в сделку, проверять цена вошла в зону фитилём или по Close
        /// </summary>
        private StrategyParameterString _entryTouchBasis;

        /// <summary>
        /// Подтверждение отбоя на выбор: бар закрылся выше ближней границы, свечной паттерн отбоя, N подряд баров в "правильную сторону"
        /// </summary>
        private StrategyParameterString _entryConfirm;

        /// <summary>
        /// Количество баров подряд в правильную сторону в случае выбора варианта подтверждения отбоя <see cref="EntryConfirmType.NConfirmBars"/>
        /// </summary>
        private StrategyParameterInt _nConfirmBars;
        /// <summary>
        /// Стоп-лосс - смещение от дальней границы брейкера
        /// </summary>
        private StrategyParameterDecimal _stopLossOffset;

        /// <summary>
        /// Соотношение риска и прибыли
        /// </summary>
        private StrategyParameterDecimal _rrTarget;

        private OrderBlock _currentLong = null;
        private OrderBlock _currentShort = null;

        public Breaker(string name, StartProgram startProgram) : base(name, startProgram)
        {
            _maxHigh = CreateParameter("Max High", 1.0m, 0.6m, 1.5m, 0.05m, "Breaker");
            _minHigh = CreateParameter("Min High", 0.2m, 0.1m, 0.4m, 0.05m, "Breaker");
            _touchTolerance = CreateParameter("TouchTolerance", 1m, 1m, 100m, 1m, "Breaker");
            _lengthZZ = CreateParameter("Length ZZ", 50, 50, 200, 20, "Breaker");

            _ob = (OrderBlockZigZag)IndicatorsFactory.CreateIndicatorByName(nameClass: "OrderBlockZigZag", name: name + "OrderBlockZigZag", canDelete: false);
            _ob = (OrderBlockZigZag)_tab.CreateCandleIndicator(_ob, nameArea: "Prime");

            _entryTouchBasis = CreateParameter("EntryTouchBasis", PriceBasis.Body.ToString(), Enum.GetNames(typeof(PriceBasis)), "Breaker");
            _entryConfirm = CreateParameter("EntryConfirm", EntryConfirmType.None.ToString(), Enum.GetNames(typeof(EntryConfirmType)), "Breaker");
            _nConfirmBars = CreateParameter("NConfirmBars", 1, 1, 10, 1, "Breaker");
            _stopLossOffset = CreateParameter("StopLossOffset", 0m, 0m, 100m, 1m, "Breaker");
            _rrTarget = CreateParameter("RRTarget", 1m, 1m, 3m, 0.5m, "Breaker");
            _ob.ChartMaster = _tab.GetChartMaster();
            _lengthZZ.ValueChange += _lengthZZ_ValueChange;
            TabsSimple[0].PositionOpeningSuccesEvent += Breaker_PositionOpeningSuccesEvent;
            _ob.Save();
        }

        private void Breaker_PositionOpeningSuccesEvent(Position position)
        {
            // SL
            OrderType orderType = OrderType.Limit;
            decimal stopPrice = 0;
            decimal stopOrderPrice = 0;
            if (position.Direction == Side.Buy)
            {
                stopPrice = _currentLong.Bottom - _currentLong.Bottom * (_stopLossOffset.ValueDecimal / 100);
                stopOrderPrice = stopPrice - _tab.Security.PriceStep * _slippage.ValueDecimal;
            }
            else if (position.Direction == Side.Sell)
            {
                stopPrice = _currentShort.Top + _currentShort.Top * (_stopLossOffset.ValueDecimal / 100);
                stopOrderPrice = stopPrice + _tab.Security.PriceStep * _slippage.ValueDecimal;
            }

            if (orderType == OrderType.Limit)
            {
                _tab.CloseAtStop(position, stopPrice, stopOrderPrice);
            }
            else if (orderType == OrderType.Market)
            {
                _tab.CloseAtStopMarket(position, stopPrice);
            }

            // TP
            stopPrice = 0;
            stopOrderPrice = 0;

            if (position.Direction == Side.Buy)
            {
                stopPrice = position.EntryPrice + position.EntryPrice * (_rrTarget.ValueDecimal / 100);
                stopOrderPrice = stopPrice - _tab.Security.PriceStep * _slippage.ValueDecimal;
            }
            else if (position.Direction == Side.Sell)
            {
                stopPrice = position.EntryPrice - position.EntryPrice * (_rrTarget.ValueDecimal / 100);
                stopOrderPrice = stopPrice + _tab.Security.PriceStep * _slippage.ValueDecimal;
            }

            if (orderType == OrderType.Limit)
            {
                _tab.CloseAtProfit(position, stopPrice, stopOrderPrice);
            }
            else if (orderType == OrderType.Market)
            {
                _tab.CloseAtProfitMarket(position, stopPrice);
            }
        }

        private void _lengthZZ_ValueChange()
        {
            _ob.Period.ValueInt = _lengthZZ.ValueInt;
        }

        protected override void ParametersChangedByUser()
        {
            if (_ob != null && _ob.ParametersDigit[0].Value != _lengthZZ.ValueInt)
            {
                _ob.ParametersDigit[0].Value = _lengthZZ.ValueInt;
                _ob.Reload();
                _ob.Save();
            }
        }
        
        protected override List<Func<List<Candle>, bool>> GetCheckers()
        {
            return new List<Func<List<Candle>, bool>>()
            {
                (candles) => { return candles.Count >= _lengthZZ.ValueInt; }
            };
        }

        protected override bool CheckOpenLongPosition(List<Candle> candles)
        {
            _currentLong = GetLongOrderBlock(candles);
            return _currentLong != null;
        }

        protected override bool CheckOpenShortPosition(List<Candle> candles)
        {
            _currentShort = GetShortOrderBlock(candles);
            return _currentShort != null;
        }

        private bool CheckOrderBlock(OrderBlock ob)
        {
            if (!ob.Visible) return false;
            if (!ob.IsBroken) return false;
            decimal height = ob.Top - ob.Bottom;
            if (height / ob.Bottom < _minHigh.ValueDecimal / 100 || height / ob.Bottom > _maxHigh.ValueDecimal / 100)
            {
                return false;
            }
            return true;
        }

        private OrderBlock GetLongOrderBlock(List<Candle> candles)
        {
            return _ob.HighOrderBlocks.Where(ob => 
            {
                if (!CheckOrderBlock(ob)) return false;
                if (ob.Type != OrderBlockType.Bullish) return false;
                int index = candles.Count - ob.Length;
                Candle candle = candles[index]; // первая свеча ордер блока
                //Candle brokenCandle = candles[ob.BrokenIndex];
                int countAfterBreak = candles.Count - ob.BrokenIndex - 1;
                if (countAfterBreak < 1) return false;
                List<Candle> candlesAfterBreak = candles.GetRange(ob.BrokenIndex + 1, countAfterBreak);
                PriceBasis basis = PriceBasis.Full;
                Enum.TryParse(_entryTouchBasis.ValueString, out basis);
                decimal min;
                Candle bounceCandle;
                switch (basis)
                {
                    case PriceBasis.Full:
                        min = candlesAfterBreak.Min(c => c.Low);
                        bounceCandle = candlesAfterBreak.Where(c => c.Low == min).LastOrDefault();
                        break;
                    case PriceBasis.Body:
                        min = candlesAfterBreak.Min(c => c.Close);
                        bounceCandle = candlesAfterBreak.Where(c => c.Close == min).LastOrDefault();
                        break;
                    default:
                        min = candlesAfterBreak.Min(c => c.Low);
                        bounceCandle = candlesAfterBreak.Where(c => c.Low == min).LastOrDefault();
                        break;
                }

                decimal touch = 0;
                if (min < ob.Top)
                {
                    touch = (ob.Top - min) / (ob.Top - ob.Bottom) * 100;
                }

                if (touch < _touchTolerance.ValueDecimal)
                {
                    return false;
                }
                int bounceIndex = candlesAfterBreak.IndexOf(bounceCandle);
                List<Candle> candlesAfterBounce = candlesAfterBreak.GetRange(bounceIndex, candlesAfterBreak.Count - bounceIndex);
                if (!EntryConfirm(candlesAfterBounce, ob))
                {
                    return false;
                }

                return true;
            }).FirstOrDefault();
        }

        private OrderBlock GetShortOrderBlock(List<Candle> candles)
        {
            return _ob.LowOrderBlocks.Where(ob =>
            {
                if (!CheckOrderBlock(ob)) return false;
                if (ob.Type != OrderBlockType.Bearish) return false;
                int index = candles.Count - ob.Length;
                Candle candle = candles[index]; // первая свеча ордер блока
                //Candle brokenCandle = candles[ob.BrokenIndex];
                int countAfterBreak = candles.Count - ob.BrokenIndex - 1;
                if (countAfterBreak < 1) return false;
                List<Candle> candlesAfterBreak = candles.GetRange(ob.BrokenIndex + 1, countAfterBreak);
                PriceBasis basis = PriceBasis.Full;
                Enum.TryParse(_entryTouchBasis.ValueString, out basis);
                decimal max;
                Candle bounceCandle;
                switch (basis)
                {
                    case PriceBasis.Full:
                        max = candlesAfterBreak.Max(c => c.High);
                        bounceCandle = candlesAfterBreak.Where(c => c.High == max).LastOrDefault();
                        break;
                    case PriceBasis.Body:
                        max = candlesAfterBreak.Max(c => c.Close);
                        bounceCandle = candlesAfterBreak.Where(c => c.Close == max).LastOrDefault();
                        break;
                    default:
                        max = candlesAfterBreak.Max(c => c.High);
                        bounceCandle = candlesAfterBreak.Where(c => c.High == max).LastOrDefault();
                        break;
                }

                decimal touch = 0;
                if (max > ob.Bottom)
                {
                    touch = (max - ob.Bottom) / (ob.Top - ob.Bottom) * 100;
                }

                if (touch < _touchTolerance.ValueDecimal)
                {
                    return false;
                }
                int bounceIndex = candlesAfterBreak.IndexOf(bounceCandle);
                List<Candle> candlesAfterBounce = candlesAfterBreak.GetRange(bounceIndex, candlesAfterBreak.Count - bounceIndex);
                if (!EntryConfirm(candlesAfterBounce, ob))
                {
                    return false;
                }

                return true;
            }).FirstOrDefault();
        }

        private bool EntryConfirm(List<Candle> candlesAfterBounce, OrderBlock ob)
        {
            EntryConfirmType confirmType = EntryConfirmType.CloseBackAboveNear;
            Enum.TryParse(_entryConfirm.ValueString, out confirmType);
            switch (confirmType)
            {
                case EntryConfirmType.None: return true;
                case EntryConfirmType.CloseBackAboveNear:
                    if (ob.Type == OrderBlockType.Bullish)
                    {
                        return candlesAfterBounce.Last().Close > ob.Top;
                    }
                    else if (ob.Type == OrderBlockType.Bearish)
                    {
                        return candlesAfterBounce.Last().Close < ob.Bottom;
                    }
                    else return false;
                case EntryConfirmType.NConfirmBars:
                    int n = 0;
                    int enough = _nConfirmBars.ValueInt;
                    Func<Candle, bool> predicate = ob.Type == OrderBlockType.Bullish ? (Func<Candle, bool>)(c => { return c.IsUp; }) : (Func<Candle, bool>)(c => { return c.IsDown; });
                    for (int i = 0; i < candlesAfterBounce.Count; i++)
                    {
                        Candle candle = candlesAfterBounce[i];
                        if (predicate(candle))
                        {
                            n++;
                            if (n >= enough) return true;
                        }
                        else
                        {
                            n = 0;
                        }
                    }
                    return false;
                default:
                    return false;
            }
        }

        protected override bool CheckClosePosition(List<Candle> candles, Position position)
        {
            return false;
        }


        enum EntryConfirmType {None, CloseBackAboveNear, NConfirmBars }
    }
}
