using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Microsoft.Office.Interop.Excel;
using OsEngine.Entity;

namespace OsEngine.Indicators
{
    [Indicator("ZigZagChannelCustom")]
    public class ZigZagChannelCustom : Aindicator
    {
        private IndicatorParameterInt _depth;
        private IndicatorParameterDecimal _deviation;
        private IndicatorParameterInt _backstep;

        private IndicatorParameterInt _channels_length;

        private List<ZZPoint> _ZZHighs;
        private List<ZZPoint> _ZZLows;

        private IndicatorDataSeries _seriesZZHighPoints;
        private IndicatorDataSeries _seriesZZLowPoints;

        private IndicatorDataSeries _seriesZZHighChannel;
        private IndicatorDataSeries _seriesZZLowChannel;

        private int _lastHighPointIndex = 0;
        private int _lastLowPointIndex = 0;

        private int _trendDir = 0;

        public override void OnStateChange(IndicatorState state)
        {
            if (state == IndicatorState.Configure)
            {
                NeedToReloadEvent += zz_Reload;

                _depth = CreateParameterInt("Depth", 14);
                _deviation = CreateParameterDecimal("Deviation", 0.5m);
                _backstep = CreateParameterInt("Back Step", 3);

                _channels_length = CreateParameterInt("Channels Length", 3);

                _seriesZZHighPoints = CreateSeries("_seriesZZHighPoints", Color.GreenYellow, IndicatorChartPaintType.Point, true);
                _seriesZZHighPoints.CanReBuildHistoricalValues = true;
                _seriesZZLowPoints = CreateSeries("_seriesZZLowPoints", Color.Red, IndicatorChartPaintType.Point, true);
                _seriesZZLowPoints.CanReBuildHistoricalValues = true;

                _seriesZZHighChannel = CreateSeries("_seriesZZHighChannel", Color.GreenYellow, IndicatorChartPaintType.Line, true);
                _seriesZZHighChannel.CanReBuildHistoricalValues = true;
                _seriesZZLowChannel = CreateSeries("_seriesZZLowChannel", Color.Red, IndicatorChartPaintType.Line, true);
                _seriesZZLowChannel.CanReBuildHistoricalValues = true;
            }
        }

        public void zz_Reload(IIndicator indicator)
        {
            _lastHighPointIndex = 0;
            _lastLowPointIndex = 0;
            _trendDir = 0;

            _ZZHighs = null;
            _ZZLows = null;
        }

        public override void OnProcess(List<Candle> candles, int index)
        {
            if (index < _depth.ValueInt)
            {
                return;
            }

            if (_ZZHighs == null)
            {
                _ZZHighs = new List<ZZPoint>();
            }
            if (_ZZLows == null)
            {
                _ZZLows = new List<ZZPoint>();
            }

            if (isHigh(candles, index))
            {
                if (_trendDir == 1)
                {
                    updateHigh(index, candles[index].High);
                }
                else
                {
                    if (_trendDir != 0)
                    {
                        fixLow(_lastLowPointIndex, candles[_lastLowPointIndex].Low);
                    }
                    setHigh(index, candles[index].High);

                    _trendDir = 1;
                }
            }

            if (isLow(candles, index))
            {
                if (_trendDir == -1)
                {
                    updateLow(index, candles[index].Low);
                }
                else
                {
                    if (_trendDir != 0)
                    {
                        fixHigh(_lastHighPointIndex, candles[_lastHighPointIndex].High);
                    }
                    setLow(index, candles[index].Low);

                    _trendDir = -1;
                }
            }

            if (_ZZHighs.Count >= _channels_length.ValueInt)
            {
                findChannelPoint(index, true);
            }

            if (_ZZLows.Count >= _channels_length.ValueInt)
            {
                findChannelPoint(index, false);
            }
        }

        bool isHigh(List<Candle> candles, int index)
        {
            for (int i = candles.Count - _depth.ValueInt - 1; i < candles.Count - 1; ++i)
            {
                if (candles[index].High <= candles[i].High)
                {
                    return false;
                }
            }

            if ((candles[index].High - candles[_lastLowPointIndex].Low) < _deviation.ValueDecimal
                || (index - _lastLowPointIndex) < _backstep.ValueInt)
            {
                return false;
            }

            return true;
        }

        bool isLow(List<Candle> candles, int index)
        {
            for (int i = candles.Count - _depth.ValueInt - 1; i < candles.Count - 1; ++i)
            {
                if (candles[index].Low >= candles[i].Low)
                {
                    return false;
                }
            }

            if ((candles[_lastHighPointIndex].High - candles[index].Low) < _deviation.ValueDecimal
                || (index - _lastHighPointIndex) < _backstep.ValueInt)
            {
                return false;
            }

            return true;
        }

        private void updateHigh(int index, decimal value)
        {
            _seriesZZHighPoints.Values[_lastHighPointIndex] = 0;
            setHigh(index, value);
        }

        private void setHigh(int index, decimal value)
        {
            _seriesZZHighPoints.Values[index] = value;
            _lastHighPointIndex = index;
        }

        private void fixHigh(int index, decimal value)
        {
            _ZZHighs.Add(new ZZPoint(index, value));
        }

        private void updateLow(int index, decimal value)
        {
            _seriesZZLowPoints.Values[_lastLowPointIndex] = 0;
            setLow(index, value);
        }

        private void setLow(int index, decimal value)
        {
            _seriesZZLowPoints.Values[index] = value;
            _lastLowPointIndex = index;
        }

        private void fixLow(int index, decimal value)
        {
            _ZZLows.Add(new ZZPoint(index, value));
        }

        private void findChannelPoint(int index, bool isHigh)
        {
            List<ZZPoint> extrems;
            IndicatorDataSeries series;
            if (isHigh)
            {
                extrems = _ZZHighs;
                series = _seriesZZHighChannel;
            }
            else
            {
                extrems = _ZZLows;
                series = _seriesZZLowChannel;
            }

            ZZPoint start = extrems[extrems.Count - _channels_length.ValueInt];

            ZZPoint mean = new ZZPoint(0, 0);
            mean.index = extrems[extrems.Count - 2].index; // TODO
            for (int i = 0; i < _channels_length.ValueInt; ++i)
            {
                mean.value += extrems[extrems.Count - i - 1].value;
            }
            mean.value /= _channels_length.ValueInt;

            decimal y = ((mean.value - start.value) / (mean.index - start.index))
                * (index - start.index) + start.value;

            series.Values[index] = y;

            return;
        }

        private class ZZPoint
        {
            public ZZPoint(int index, decimal value)
            {
                this.index = index;
                this.value = value;
            }

            public int index;
            public decimal value;
        };
    }
}