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
        private StrategyParameterInt _rsiOverSold;
        private StrategyParameterInt _rsiOverBought;

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
            _period = CreateParameter("Period", 50, 10, 100, 1, "Robot");
            _rsiOverSold = CreateParameter("RSI OverSold", 30, 20, 40, 5, "Robot");
            _rsiOverBought = CreateParameter("RSI OverBought", 70, 60, 80, 5, "Robot");
            _minDistance = CreateParameter("Min Distance", 5, 5, 30, 1, "Robot");
            _maxDistance = CreateParameter("Max Distance", 40, 40, 200, 1, "Robot");
            _syncTolerance = CreateParameter("Sync Tolerance", 40, 40, 200, 1, "Robot");
            _extremaOrder = CreateParameter("Extrema Order", 5, 5, 30, 1, "Robot");
            _minDivergenceStrength = CreateParameter("Min Divergence Strength", 50, 50, 90, 5, "Robot");
            _rsi = IndicatorsFactory.CreateIndicatorByName("RSI", name + "RSI", false);
            _rsi = (Aindicator)_tab.CreateCandleIndicator(_rsi, "RSI");
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
                strength = GetDivergenceLongStrength(priceDic, rsiDic);
            }

            return strength >= _minDivergenceStrength.ValueInt;
        }

        private decimal GetDivergenceLongStrength(Dictionary<int, decimal> priceDic, Dictionary<int, decimal> rsiDic)
        {
            decimal result = 0;
            decimal price1 = priceDic[0];
            decimal price2 = priceDic[1];
            decimal ind1 = rsiDic[0];
            decimal ind2 = rsiDic[1];

            // Величина расхождения
            decimal pricePercent = (price1 - price2) / price1;
            decimal indicatorPercent = (ind2 - ind1) / ind2;

            decimal averagePercent = (pricePercent + indicatorPercent) / 2;
            decimal divergenceValue = averagePercent * 3;
            if (divergenceValue > 30)
            {
                divergenceValue = 30;
            }
            result += divergenceValue;

            // Экстремальность rsi
            decimal rsiExtrem = 0;
            rsiExtrem = _rsiOverSold.ValueInt - _rsi.DataSeries[0].Values.Last();
            if (rsiExtrem > 25)
            {
                rsiExtrem = 25;
            }
            if (rsiExtrem < 0)
            {
                rsiExtrem = 0;
            }
            result += rsiExtrem;
            // Длительность паттерна
            decimal lengthPoints = 0;
            int minIndex = priceDic.Keys.Union(rsiDic.Keys).Min();
            int maxIndex = priceDic.Keys.Union(rsiDic.Keys).Max();
            int length = maxIndex - minIndex;
            if (length >= 30)
            {
                lengthPoints = 20;
            }
            else if (length > 20 && length < 30)
            {
                lengthPoints = 15;
            }
            else if (length > 15 && length <= 20)
            {
                lengthPoints = 10;
            }
            else if (length <= 15 && length > 10)
            {
                lengthPoints = 5;
            }
            result += lengthPoints;
            // Угол/наклон ?

            return result;
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
            if (_rsi != null)
            {
                _rsi.ParametersDigit[0].Value = _rsiPeriod.ValueInt;
            }
        }
    }
}
