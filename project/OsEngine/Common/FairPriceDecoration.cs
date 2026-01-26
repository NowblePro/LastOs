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
    /// Декорация, которая при выполнении условия (N свечей подряд выше/ниже SMA) 
    /// перемещает стоп-лосс всех позиций направления на уровень SMA.
    /// </summary>
    public class FairPriceDecoration
    {
        private BotPanel _bot;
        private BotTabSimple _tab;

        private StrategyParameterBool _enabled;
        private StrategyParameterInt _candlesToWait;
        private Aindicator _sma;
        private StrategyParameterDecimal _slippageParam;
        private StrategyParameterString _orderTypeParam;

        // Счётчики свечей
        private int _buyConditionStreak = 0;
        private int _sellConditionStreak = 0;

        public FairPriceDecoration(BotPanel bot, string paramTabName = "FairPrice")
        {
            _bot = bot;
            _tab = bot.TabsSimple[0];

            // Параметры
            _enabled = bot.CreateParameter("FairPrice Enabled", true, paramTabName);
            _candlesToWait = bot.CreateParameter("FairPrice Candles To Wait", 3, 1, 10, 1, paramTabName);

            // Slippage и тип ордера — как в других декорациях
            _slippageParam = _bot.Parameters
                .OfType<StrategyParameterDecimal>()
                .FirstOrDefault(p => p.Name.ToLower() == "slippage");

            _orderTypeParam = _bot.Parameters
                .OfType<StrategyParameterString>()
                .FirstOrDefault(p => p.Name.ToLower() == "ordertype");

            // Подписываемся на завершение свечи
            _tab.CandleFinishedEvent += OnCandleFinished;
        }

        /// <summary>
        /// Устанавливает SMA индикатор, по которому будет происходить сравнение
        /// </summary>
        public void SetSma(Aindicator sma)
        {
            _sma = sma;
        }

        private void OnCandleFinished(List<Candle> candles)
        {
            if (!_enabled.ValueBool || _sma == null || candles == null || candles.Count == 0 || _sma.DataSeries[0].Values == null)
                return;

            Candle last = candles.Last();
            int smaIndex = candles.Count - 1;

            if (smaIndex < 0 || smaIndex >= _sma.DataSeries[0].Values.Count)
                return;

            decimal currentSma = _sma.DataSeries[0].Values[smaIndex];

            // Сброс счётчиков
            bool buyConditionMet = last.Low > currentSma;
            bool sellConditionMet = last.High < currentSma;

            if (buyConditionMet)
            {
                _buyConditionStreak++;
            }
            else
            {
                _buyConditionStreak = 0;
            }

            if (sellConditionMet)
            {
                _sellConditionStreak++;
            }
            else
            {
                _sellConditionStreak = 0;
            }

            // Проверяем условия для активации FairPrice
            if (_buyConditionStreak >= _candlesToWait.ValueInt)
            {
                MoveStopToSma(Side.Buy, currentSma);
            }
            else if (_sellConditionStreak >= _candlesToWait.ValueInt)
            {
                MoveStopToSma(Side.Sell, currentSma);
            }
        }

        /// <summary>
        /// Перемещает стоп-лоссы всех открытых позиций указанного направления на уровень SMA
        /// </summary>
        private void MoveStopToSma(Side side, decimal smaLevel)
        {
            List<Position> positions = _tab.PositionsOpenAll
                .Where(p => p.Direction == side && p.State == PositionStateType.Open)
                .ToList();

            if (positions.Count == 0)
                return;

            decimal slippageSteps = _slippageParam?.ValueDecimal ?? 0;
            decimal priceStep = _tab.Security.PriceStep;
            OrderType orderType = OrderType.Limit;

            if (_orderTypeParam != null &&
                Enum.TryParse(_orderTypeParam.ValueString, ignoreCase: true, out OrderType ot))
            {
                orderType = ot;
            }

            foreach (Position position in positions)
            {
                decimal newStopPrice = 0;
                decimal newOrderPrice = 0;

                if (side == Side.Buy)
                {
                    newStopPrice = smaLevel; // стоп — на уровне SMA
                    newOrderPrice = newStopPrice - priceStep * slippageSteps; // лимит ниже
                }
                else if (side == Side.Sell)
                {
                    newStopPrice = smaLevel;
                    newOrderPrice = newStopPrice + priceStep * slippageSteps; // лимит выше
                }

                // Закрываем по стопу, но с новым уровнем — используем те же методы
                if (orderType == OrderType.Limit)
                {
                    _tab.CloseAtStop(position, newStopPrice, newOrderPrice);
                }
                else if (orderType == OrderType.Market)
                {
                    _tab.CloseAtStopMarket(position, newStopPrice);
                }

                _bot.SendNewLogMessage($"FairPrice: stop moved to SMA={smaLevel:F8} for {side} position #{position.Number}", Logging.LogMessageType.Trade);
            }
        }
    }
}