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
    public class VolatileStopDecoration
    {
        private BotPanel _bot;
        private BotTabSimple _tab;

        /// <summary>
        /// Период за который анализируются доходности свечей, чтобы отменять лимитки в случае резкого увеличении доходности в сторону, противоположную позиции
        /// </summary>
        private StrategyParameterInt _periodLossFilter;
        private StrategyParameterInt _lossCandlesCount;
        private StrategyParameterInt _quantile;
        private List<decimal> _profits = new List<decimal>();
        private List<decimal> _profitsLast = new List<decimal>();
        private StrategyParameterBool _volatileStopEnable;
        private Action _handler;

        /// <summary>
        /// Текущее значение квантиля, куда входят последние <see cref="_lossCandlesCount"/> свечей по доходности
        /// </summary>
        private int _currentQuantile = 0;

        public VolatileStopDecoration(BotPanel bot, Action handler, string paramEnableName = "", string tabControlName = "")
        {
            _handler = handler;
            _bot = bot;
            _tab = bot.TabsSimple[0];
            string nameEnable = paramEnableName;
            string tabName = tabControlName;
            if (string.IsNullOrEmpty(nameEnable))
            {
                nameEnable = "Volatile Stop Enable";
            }
            if (string.IsNullOrEmpty(tabControlName))
            {
                tabName = "Volatile Stop";
            }
            _volatileStopEnable = bot.CreateParameter(nameEnable, false, tabName);
            _periodLossFilter = bot.CreateParameter("Period", 100, 100, 500, 100, tabName);
            _lossCandlesCount = bot.CreateParameter("Candles Count", 3, 3, 5, 1, tabName);
            _quantile = bot.CreateParameter("Quantile", 90, 80, 95, 5, tabName);
            //bot.TabsSimple[0].PositionOpeningSuccesEvent += _tab_PositionOpeningSuccesEvent;
            _tab.CandleFinishedEvent += _tab_CandleFinishedEvent;
        }

        private void _tab_CandleFinishedEvent(List<Candle> obj)
        {
            CandleProfitFilter(obj);
        }

        /// <summary>
        /// Рассчитать доходности последних <see cref="_periodLossFilter"/> свечей для отмены лимиток, в случае резкого движения цены в сторону, противоположную позиции
        /// </summary>
        private void CandleProfitFilter(List<Candle> candles)
        {
            if (candles == null || candles.Count == 0) return;
            if (!_volatileStopEnable.ValueBool) return;
            if (candles.Count < _profits.Count)
            {
                _profits.Clear();
            }

            int take = candles.Count - _profits.Count;
            int skip = candles.Count - take;
            foreach (Candle candle in candles.Skip(skip).Take(take))
            {
                _profits.Add(GetCandleProfit(candle));
            }

            take = _periodLossFilter.ValueInt;
            skip = _profits.Count - take;

            _profitsLast = _profits.Skip(skip).Take(take).ToList();

            take = _lossCandlesCount.ValueInt;
            skip = _profitsLast.Count - take;

            decimal minProfit = _profitsLast.Skip(skip).Take(take).Min();
            _currentQuantile = (int)((float)_profitsLast.Where(v => v <= minProfit).Count() / ((float)_periodLossFilter.ValueInt) * 100);
            if (_currentQuantile >= _quantile.ValueInt)
            {
                _bot.SendNewLogMessage($"Сработал стоп по волатильности", Logging.LogMessageType.Trade);
                _handler.Invoke();
            }
        }

        private decimal GetCandleProfit(Candle candle)
        {
            return (candle.High - candle.Low) / candle.Low;
        }
    }
}
