using System;
using System.Collections.Generic;
using System.Drawing;
using OsEngine.Entity;

namespace OsEngine.Indicators
{
    [Indicator("RSI")]
    public class RSI : Aindicator
    {
        private IndicatorParameterInt _length;
        private IndicatorParameterDecimal _oversold;
        private IndicatorParameterDecimal _overbought;

        private IndicatorDataSeries _series;

        private IndicatorDataSeries _seriesOversold;
        private IndicatorDataSeries _seriesOverbought;

        public override void OnStateChange(IndicatorState state)
        {
            _length = CreateParameterInt("Length", 14);
            _oversold = CreateParameterDecimal("Oversold", 30m);
            _overbought = CreateParameterDecimal("Overbought", 70m);

            _series = CreateSeries("Ma", Color.DodgerBlue, IndicatorChartPaintType.Line, true);

            _seriesOversold = CreateSeries("Oversold", Color.White, IndicatorChartPaintType.Line, true);
            _seriesOverbought = CreateSeries("Overbought", Color.White, IndicatorChartPaintType.Line, true);
        }

        public override void OnProcess(List<Candle> candles, int index)
        {
            if (index - _length.ValueInt - 1 <= 0)
            {
                return;
            }

            int startIndex = 1;

            if (index > 150)
            {
                startIndex = index - 150;
            }

            List<decimal> priceChangeHigh = new List<decimal>();
            List<decimal> priceChangeLow = new List<decimal>();

            List<decimal> priceChangeHighAverage = new List<decimal>();
            List<decimal> priceChangeLowAverage = new List<decimal>();

            for (int i = startIndex, valueInd = 0; i <= index && i < candles.Count; i++, valueInd++)
            {
                if (candles[i].Close - candles[i - 1].Close > 0)
                {
                    priceChangeHigh.Add(candles[i].Close - candles[i - 1].Close);
                    priceChangeLow.Add(0);
                }
                else
                {
                    priceChangeLow.Add(candles[i - 1].Close - candles[i].Close);
                    priceChangeHigh.Add(0);
                }

                MovingAverageHard(priceChangeHigh, priceChangeHighAverage, _length.ValueInt, valueInd);
                MovingAverageHard(priceChangeLow, priceChangeLowAverage, _length.ValueInt, valueInd);
            }

            decimal averageHigh = priceChangeHighAverage[priceChangeHighAverage.Count - 1];
            decimal averageLow = priceChangeLowAverage[priceChangeHighAverage.Count - 1];

            decimal rsi;

            if (averageHigh != 0 &&
                averageLow != 0)
            {
                rsi = 100 * (1 - averageLow / (averageLow + averageHigh));
            }
            else
            {
                rsi = 100;
            }

            // TODO exception out of range here
            _series.Values[index] = Math.Round(rsi, 2);
            //_series.Values.Add( Math.Round(rsi, 2) );

            _seriesOversold.Values[index] = _oversold.ValueDecimal;
            _seriesOverbought.Values[index] = _overbought.ValueDecimal;
        }

        private void MovingAverageHard(List<decimal> valuesSeries, List<decimal> moving, int length, int index)
        {
            if (index == length)
            {
                decimal lastMoving = 0;

                for (int i = index; i < valuesSeries.Count && i > index - 1 - length; i--)
                {
                    lastMoving += valuesSeries[i];
                }
                lastMoving = lastMoving / length;

                while (moving.Count <= index)
                {
                    moving.Add(0);
                }

                moving[index] = lastMoving;
            }
            else if (index > length)
            {
                decimal a = 2.0m / (length * 2);
                decimal lastValueMoving = moving[index - 1];
                decimal lastValueSeries = valuesSeries[index];
                decimal nowValueMoving;
                nowValueMoving = lastValueMoving + a * (lastValueSeries - lastValueMoving);

                while (moving.Count <= index)
                {
                    moving.Add(0);
                }

                moving[index] = nowValueMoving;
            }
        }
    }
}