using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using OsEngine.Entity;
using OsEngine.Indicators;

namespace CustomIndicators.Scripts
{
    public class Mobility : Aindicator
    {
        private IndicatorParameterInt _length;
        private IndicatorParameterInt _smoothLength;
        private IndicatorDataSeries _series;
        private IndicatorDataSeries _seriesSmooth;

        public override void OnStateChange(IndicatorState state)
        {
            if (state == IndicatorState.Configure)
            {
                _length = CreateParameterInt("Period", 60);
                _smoothLength = CreateParameterInt("Smooth Mobility Length", 24);
                _series = CreateSeries("Mobility", Color.Aqua, IndicatorChartPaintType.Line, true);
                _seriesSmooth = CreateSeries("MobilitySmooth", Color.Blue, IndicatorChartPaintType.Line, true);
            }
            
            if (state == IndicatorState.Dispose)
            {
                _mobilityList.Clear();
            }
        }

        public override void OnProcess(List<Candle> candles, int index)
        {
            _series.Values[index] = GetValue(candles, index);
            _seriesSmooth.Values[index] = GetValueSmooth(index);
        }

        private decimal _mobility;
        private int _calcIndex;
        private readonly List<decimal> _mobilityList = new List<decimal>();
        private decimal GetValue(List<Candle> candles, int index)
        {
            if (index < _length.ValueInt)
            {
                return 0;
            }

            if (index >= _length.ValueInt && candles[index].TimeStart.Minute == _length.ValueInt - 1)
            {
                _calcIndex = index;
                
                int startIndex = index - _length.ValueInt + 1;

                decimal pSum = 0;

                decimal pricePrev = candles[startIndex].Close;

                for (int i = startIndex + 1; i <= index; i++)
                {
                    decimal price;

                    if (candles[i].High < pricePrev)
                    {
                        price = candles[i].High;
                    }
                    else if (candles[i].Low > pricePrev)
                    {
                        price = candles[i].Low;
                    }
                    else
                    {
                        price = pricePrev;
                    }
                
                    pSum += (price - pricePrev) * (price - pricePrev);
                    pricePrev = price;
                }

                _mobility = (decimal)Math.Sqrt((double)pSum * 1440 / _length.ValueInt);
                
                _mobilityList.Add(_mobility);
                
                if(_mobilityList.Count > _smoothLength.ValueInt) _mobilityList.RemoveAt(0);

                //decimal mobilityVol = _mobility * (decimal)Math.Sqrt(365) * 100 / candles[index].Close;

                return _mobility;
            }

            return _mobility;
        }

        private decimal _mobilitySmooth;
        private decimal GetValueSmooth(int index)
        {
            if (_mobilityList.Count < _smoothLength.ValueInt)
            {
                return 0;
            }

            if (index != _calcIndex) return _mobilitySmooth;
            
            _mobilitySmooth = (decimal)Math.Sqrt((double)_mobilityList.Sum(x => x * x) / _smoothLength.ValueInt);
                
            return _mobilitySmooth;
        }
    }
}