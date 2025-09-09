using System.Collections.Generic;
using System.Drawing;
using OsEngine.Entity;

namespace OsEngine.Indicators.TrigonumCustom
{
    [Indicator("SmaCustom")]
    public class SmaCustom : Aindicator
    {
        private IndicatorParameterInt _length;

        private IndicatorParameterString _candlePoint;

        private IndicatorDataSeries _series;

        private Queue<decimal> _valuesQueue;
        private decimal _currentSumm;

        public override void OnStateChange(IndicatorState state)
        {
            if (state == IndicatorState.Configure)
            {
                NeedToResetDataEvent += sma_Reset;

                _length = CreateParameterInt("Length", 14);
                _candlePoint = CreateParameterStringCollection("Candle Point", "Close", Entity.CandlePointsArray);
                _series = CreateSeries("Ma", Color.DodgerBlue, IndicatorChartPaintType.Line, true);
            }
        }

        public override void OnProcess(List<Candle> candles, int index)
        {
            if (_length.ValueInt > index + 1)
            {
                _series.Values[index] = 0;
                return;
            }

            if (_valuesQueue == null)
            {
                _valuesQueue = new Queue<decimal>();
                for (int i = 0; i <= index; ++i)
                {
                    _valuesQueue.Enqueue(candles[i].GetPoint(_candlePoint.ValueString));
                    _currentSumm += candles[i].GetPoint(_candlePoint.ValueString);
                }
            }
            else
            {
                _currentSumm -= _valuesQueue.Dequeue();
                _currentSumm += candles[index].GetPoint(_candlePoint.ValueString);
                _valuesQueue.Enqueue(candles[index].GetPoint(_candlePoint.ValueString));
            }

            _series.Values[index] = _currentSumm / _length.ValueInt;
        }

        public void sma_Reset(IIndicator indicator)
        {
            _valuesQueue = null;
            _currentSumm = 0.0m;
        }
    }
}
