using OsEngine.Charts.CandleChart.Indicators;
using OsEngine.Common;
using OsEngine.Entity;
using OsEngine.Indicators;
using OsEngine.OsTrader.Panels.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OsEngine.Robots.TrigonumCustom.Base
{
    [Bot("DivergenceBot")]
    public class DivergenceBot : BotPanelSimple
    {
        private StrategyParameterInt _period;
        private StrategyParameterInt _rsiPeriod;
        /// <summary>
        /// Минимальное расстояние в индексах между пиками цены.
        /// </summary>
        private StrategyParameterInt _minDistance;
        /// <summary>
        /// Максимальное расстояние в индексах между пиками цены.
        /// </summary>
        private StrategyParameterInt _maxDistance;
        /// <summary>
        /// Допуск совпадения индексов цены и RSI.
        /// </summary>
        private StrategyParameterInt _syncTolerance;
        private StrategyParameterInt _extremaOrder;
        private StrategyParameterInt _minDivergenceStrength;

        private Aindicator _rsi;

        public DivergenceBot(string name, StartProgram startProgram) : base(name, startProgram)
        {
            _rsiPeriod = CreateParameter("RSI Period", 14, 7, 28, 1, "Robot");
            _minDistance = CreateParameter("Min Distance", 5, 5, 30, 1, "Robot");
            _maxDistance = CreateParameter("Max Distance", 40, 40, 200, 1, "Robot");
            _syncTolerance = CreateParameter("Sync Tolerance", 40, 40, 200, 1, "Robot");
            _extremaOrder = CreateParameter("Extrema Order", 5, 5, 30, 1, "Robot");
            _minDivergenceStrength = CreateParameter("Min Divergence Strength", 50, 50, 90, 5, "Robot");
            _rsi = IndicatorsFactory.CreateIndicatorByName("RSI", name + "RSI", false);
            new TakeProfitDecoration(this);
            new StopLossDecoration(this);
            ParametersChangedByUser();
        }

        protected override bool CheckClosePosition(List<Candle> candles, Position position)
        {
            return false;
        }

        protected override bool CheckOpenLongPosition(List<Candle> candles)
        {
            int skip = candles.Count - _period.ValueInt;
            decimal[] price = candles.Skip(skip).Select(c => c.Low).ToArray();
            decimal[] rsi = _rsi.DataSeries[0].Values.Skip(skip).ToArray();
            decimal strength = 0;
            if (DivergenceDetector.IsBullDivergence(price, rsi, _minDistance.ValueInt, _maxDistance.ValueInt, _syncTolerance.ValueInt, _extremaOrder.ValueInt, out Dictionary<int, decimal> priceDic, out Dictionary<int, decimal> rsiDic))
            {

            }

            return strength >= _minDivergenceStrength.ValueInt;
        }

        private decimal GetDivergenceStrength(Dictionary<int, decimal> priceDic, Dictionary<int, decimal> rsiDic)
        {
            List<int> sortedPriceIndexes = priceMin.Keys.ToList();
            sortedPriceIndexes.Sort();
            List<int> sortedIndicatorIndexes = indicatorMin.Keys.ToList();
            sortedIndicatorIndexes.Sort();
            for (int i = 0; i < sortedPriceIndexes.Count; i++)
            {
                int priceIndex = sortedPriceIndexes[i];
                int indicatorIndex = sortedIndicatorIndexes[i];
                if (Math.Abs(priceIndex - indicatorIndex) > syncTolerance)
                {
                    // Если экстремумы индикатора и цены не совпадают по времени больше чем на syncTolerance
                    return false;
                }
            }

            decimal price1 = priceMin[sortedPriceIndexes[0]];
            decimal price2 = priceMin[sortedPriceIndexes[1]];
            decimal ind1 = indicatorMin[sortedIndicatorIndexes[0]];
            decimal ind2 = indicatorMin[sortedIndicatorIndexes[1]];

            if (price1 < price2 || ind1 > ind2)
            {
                return false;
            }

            decimal pricePercent = (price1 - price2) / price1;
            decimal indicatorPercent = (ind2 - ind1) / ind2;

            decimal averagePercent = (pricePercent + indicatorPercent) / 2;
            decimal divergenceValue = averagePercent * 3;
            if (divergenceValue > 30)
            {
                divergenceValue = 30;
            }

            decimal indicatorPower = 0;
            if (indicatorPowerFunc != null)
            {
                indicatorPower = indicatorPowerFunc(ind2);
                if (indicatorPower > 25)
                {
                    indicatorPower = 25;
                }
            }
        }

        protected override bool CheckOpenShortPosition(List<Candle> candles)
        {
            int skip = candles.Count - _period.ValueInt;
            decimal[] price = candles.Skip(skip).Select(c => c.Low).ToArray();
            decimal[] rsi = _rsi.DataSeries[0].Values.Skip(skip).ToArray();
            return DivergenceDetector.IsBearDivergence(price, rsi, _minDistance.ValueInt, _maxDistance.ValueInt, _syncTolerance.ValueInt, _extremaOrder.ValueInt);
        }

        protected override List<Func<List<Candle>, bool>> GetCheckers()
        {
            return new List<Func<List<Candle>, bool>>() 
            {
                candles => { return candles.Count >= _period.ValueInt;  },
                candles => { return candles.Count >= _rsiPeriod.ValueInt; }
            };
        }

        protected override void ParametersChangedByUser()
        {
            _rsi.ParametersDigit[0].Value = _rsiPeriod.ValueInt;
        }
    }
}
