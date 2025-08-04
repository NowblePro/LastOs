using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Microsoft.Office.Interop.Excel;
using OsEngine.Entity;

namespace OsEngine.Indicators
{
    [Indicator("ZigZagChannelCustomExtrems")]
    public class ZigZagChannelCustomExtrems : Aindicator
    {
        private IndicatorParameterInt _depth;
        private IndicatorParameterDecimal _deviation;
        private IndicatorParameterInt _backstep;

        private IndicatorParameterInt _channels_length;

        private IndicatorParameterBool _hasHighCh;
        private IndicatorParameterBool _hasLowCh;

        private List<ZZPoint> _ZZHighs;
        private List<ZZPoint> _ZZLows;

        private IndicatorDataSeries _seriesZZHighPoints;
        private IndicatorDataSeries _seriesZZLowPoints;

        private IndicatorDataSeries _seriesZZHighChannel;
        private IndicatorDataSeries _seriesZZLowChannel;

        private IndicatorDataSeries _seriesZZLine;

        private int _lastHighPointIndex = 0;
        private int _lastLowPointIndex = 0;

        private int _trendDir = 0;

        public override void OnStateChange(IndicatorState state)
        {
            if (state == IndicatorState.Configure)
            {
                NeedToResetDataEvent += zz_Reset;

                _depth = CreateParameterInt("Depth", 14);
                _deviation = CreateParameterDecimal("Deviation", 0.5m);
                _backstep = CreateParameterInt("Back Step", 3);

                _channels_length = CreateParameterInt("Channels Length", 3);

                _hasHighCh = CreateParameterBool("Has High Channel", false); // Do not change externaly!
                _hasLowCh = CreateParameterBool("Has Low Channel", false); // Do not change externaly!

                _seriesZZHighPoints = CreateSeries("_seriesZZHighPoints", Color.GreenYellow, IndicatorChartPaintType.Point, true);
                _seriesZZHighPoints.CanReBuildHistoricalValues = true;
                _seriesZZLowPoints = CreateSeries("_seriesZZLowPoints", Color.Red, IndicatorChartPaintType.Point, true);
                _seriesZZLowPoints.CanReBuildHistoricalValues = true;

                _seriesZZHighChannel = CreateSeries("_seriesZZHighChannel", Color.GreenYellow, IndicatorChartPaintType.Line, true);
                _seriesZZHighChannel.CanReBuildHistoricalValues = true;
                _seriesZZLowChannel = CreateSeries("_seriesZZLowChannel", Color.Red, IndicatorChartPaintType.Line, true);
                _seriesZZLowChannel.CanReBuildHistoricalValues = true;

                _seriesZZLine = CreateSeries("_seriesZZLine", Color.Blue, IndicatorChartPaintType.Line, true);
                _seriesZZLine.CanReBuildHistoricalValues = true;
            }
        }

        public void zz_Reset(IIndicator indicator)
        {
            _seriesZZHighChannel.IsPaint = true;
            _seriesZZLowChannel.IsPaint = true;

            _hasHighCh._valueBool = false;
            _hasLowCh._valueBool = false;

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
                }
                _trendDir = 1;
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
                }
                _trendDir = -1;
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
            if (_trendDir == 1 && candles[_lastHighPointIndex].High > candles[index].High)
            {
                return false;
            }

            for (int i = candles.Count - _depth.ValueInt - 1; i < candles.Count - 1; ++i)
            {
                if (candles[index].High <= candles[i].High)
                {
                    return false;
                }
            }

            if (calcDeviation(candles[index].High, candles[_lastLowPointIndex].Low) < _deviation.ValueDecimal
                || (index - _lastLowPointIndex) < _backstep.ValueInt)
            {
                return false;
            }

            return true;
        }

        bool isLow(List<Candle> candles, int index)
        {
            if (_trendDir == -1 && candles[_lastLowPointIndex].Low < candles[index].Low)
            {
                return false;
            }

            for (int i = candles.Count - _depth.ValueInt - 1; i < candles.Count - 1; ++i)
            {
                if (candles[index].Low >= candles[i].Low)
                {
                    return false;
                }
            }

            if (calcDeviation(candles[_lastHighPointIndex].High, candles[index].Low) < _deviation.ValueDecimal
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

            // build zz line
            if (_ZZLows.Count > 0)
            {
                ZZPoint last_low = _ZZLows[_ZZLows.Count - 1];
                ZZPoint last_high = _ZZHighs[_ZZHighs.Count - 1];
                for (int i = last_low.index + 1; i <= last_high.index; ++i)
                {
                    decimal y = ((last_high.value - last_low.value) / (last_high.index - last_low.index))
                                                * (i - last_low.index) + last_low.value;

                    _seriesZZLine.Values[i] = y;
                }
            }
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

            // build zz line
            if (_ZZHighs.Count > 0)
            {
                ZZPoint last_low = _ZZLows[_ZZLows.Count - 1];
                ZZPoint last_high = _ZZHighs[_ZZHighs.Count - 1];
                for (int i = last_high.index + 1; i <= last_low.index; ++i)
                {
                    decimal y = ((last_low.value - last_high.value) / (last_low.index - last_high.index))
                                                * (i - last_high.index) + last_high.value;

                    _seriesZZLine.Values[i] = y;
                }
            }
        }

        private decimal calcDeviation(decimal high_value, decimal low_value)
        {
            return (high_value - low_value) / (high_value / 100);
        }

        private void findChannelPoint(int index, bool isHigh)
        {
            List<ZZPoint> extrems;
            IndicatorDataSeries series;
            if (isHigh)
            {
                extrems = _ZZHighs;
                series = _seriesZZHighChannel;

                _hasHighCh.ValueBool = true;
            }
            else
            {
                extrems = _ZZLows;
                series = _seriesZZLowChannel;

                _hasLowCh.ValueBool = true;
            }

            ZZPoint start = extrems[extrems.Count - _channels_length.ValueInt];
            ZZPoint end = extrems[extrems.Count - 1];

            decimal y = ((end.value - start.value) / (end.index - start.index))
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