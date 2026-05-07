using OsEngine.Entity;
using OsEngine.Indicators;
using OsEngine.OsTrader.Panels;
using OsEngine.OsTrader.Panels.Tab;
using System;
using System.Collections.Generic;
using System.Linq;

namespace OsEngine.Common
{
    /// <summary>
    /// Декорация, устанавливающая динамический Take Profit на основе SMA.
    /// TP = SMA ± (|EntryPrice - SMA| * TakePercent), следует за SMA.
    /// При достижении — закрывает всю позицию по рынку.
    /// </summary>
    public class TakeSmaDecoration
    {
        private BotPanel _bot;
        private BotTabSimple _tab;

        private StrategyParameterBool _enabled;
        private StrategyParameterDecimal _takePercent; // 0.2 = 1/5
        private StrategyParameterString _orderTypeParam;
        private StrategyParameterDecimal _slippageParam;
        private StrategyParameterBool _followSma;

        private Aindicator _sma;
        private decimal _takeOffset;
        private int _positionCount = 0;

        public TakeSmaDecoration(BotPanel bot, string paramTabName = "TakeSma")
        {
            _bot = bot;
            _tab = bot.TabsSimple[0];

            // Параметры
            _enabled = bot.CreateParameter("TakeSma Enabled", true, paramTabName);
            _takePercent = bot.CreateParameter("Take Sma Percent", 20m, 10m, 30m, 5m, paramTabName);
            _followSma = bot.CreateParameter("Follow Sma", true, paramTabName);

            // Slippage и тип ордера
            _slippageParam = _bot.Parameters
                .OfType<StrategyParameterDecimal>()
                .FirstOrDefault(p => p.Name.ToLower() == "slippage");

            _orderTypeParam = _bot.Parameters
                .OfType<StrategyParameterString>()
                .FirstOrDefault(p => p.Name.ToLower() == "ordertype");

            // Подписка на завершение свечи
            _tab.CandleFinishedEvent += OnCandleFinished;
        }

        public bool FollowSma
        {
            get => _followSma.ValueBool;
            set
            {
                _followSma.ValueBool = value;
            }
        }

        public void SetSma(Aindicator sma)
        {
            _sma = sma;
        }

        private void OnCandleFinished(List<Candle> candles)
        {
            if (!_enabled.ValueBool || _sma == null || candles == null || candles.Count == 0 || _sma.DataSeries[0].Values == null)
                return;

            int smaIndex = candles.Count - 1;
            if (smaIndex >= _sma.DataSeries[0].Values.Count || smaIndex < 0)
                return;

            List<Position> openPositions = _tab.PositionsOpenAll;

            decimal _slippage = 0;
            if (_slippageParam != null)
            {
                _slippage = _slippageParam.ValueDecimal;
            }

            OrderType orderType = OrderType.Limit;
            if (_orderTypeParam != null && Enum.TryParse(_orderTypeParam.ValueString, false, out OrderType ot))
            {
                orderType = ot;
            }

            decimal sma = _sma.DataSeries[0].Values[smaIndex];
            int positionCount = openPositions.Count();
            if (positionCount == 0)
            {
                _positionCount = positionCount;
                return; 
            }
            
            if (positionCount != _positionCount)
            {
                decimal entryPrice = openPositions.Sum(p => p.EntryPrice) / openPositions.Count();
                decimal deviation = Math.Abs(entryPrice - sma) / sma;
                _takeOffset = deviation * _takePercent.ValueDecimal / 100m;
                _positionCount = positionCount;
                if (!FollowSma)
                {
                    ChangeTake();
                }
            }
            
            if (FollowSma)
            {
                ChangeTake();
            }

            void ChangeTake()
            {
                Side side = openPositions.First().Direction;
                foreach (var position in openPositions)
                {
                    if (position.State != PositionStateType.Open)
                        continue;

                    decimal stopPrice = 0;
                    decimal stopOrderPrice = 0;
                    if (side == Side.Buy)
                    {
                        // Лонг: тейк ниже SMA
                        stopPrice = sma - _takeOffset * sma;
                        stopOrderPrice = stopPrice - _tab.Security.PriceStep * _slippage;
                    }
                    else
                    {
                        // Шорт: тейк выше SMA
                        stopPrice = sma + _takeOffset * sma;
                        stopOrderPrice = stopPrice + _tab.Security.PriceStep * _slippage;
                    }

                    if (orderType == OrderType.Limit)
                    {
                        _tab.CloseAtProfit(position, stopPrice, stopOrderPrice);
                    }
                    else if (orderType == OrderType.Market || orderType == OrderType.MarketNextOpen)
                    {
                        _tab.CloseAtProfitMarket(position, stopPrice);
                    }
                }
            }
        }
    }
}
