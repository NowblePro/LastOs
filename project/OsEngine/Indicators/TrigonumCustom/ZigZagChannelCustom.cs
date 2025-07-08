using System;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;
using OsEngine.Entity;

namespace OsEngine.Indicators
{
    [Indicator("ZigZagChannelCustom")]
    public class ZigZagChannelCustom : Aindicator
    {
        private IndicatorParameterInt _depth;
        private IndicatorParameterInt _deviation;
        private IndicatorParameterInt _backstep;

        private IndicatorDataSeries _seriesZigZagHighs;
        private IndicatorDataSeries _seriesZigZagLows;

        //private IndicatorDataSeries _seriesZigZag;
        //private IndicatorDataSeries _seriesToLine;

        public override void OnStateChange(IndicatorState state)
        {
            if (state == IndicatorState.Configure)
            {
                _depth = CreateParameterInt("Depth", 14);
                _deviation = CreateParameterInt("Deviation", 5);
                _backstep = CreateParameterInt("Back Step", 3);

                _seriesZigZagHighs = CreateSeries("_seriesZigZagHighs", Color.GreenYellow, IndicatorChartPaintType.Point, false);
                _seriesZigZagHighs.CanReBuildHistoricalValues = true;
                _seriesZigZagLows = CreateSeries("_seriesZigZagLows", Color.Red, IndicatorChartPaintType.Point, false);
                _seriesZigZagLows.CanReBuildHistoricalValues = true;

                //_seriesZigZag = CreateSeries("ZigZag", Color.CornflowerBlue, IndicatorChartPaintType.Point, false);
                //_seriesZigZag.CanReBuildHistoricalValues = true;
                //_seriesToLine = CreateSeries("ZigZagLine", Color.CornflowerBlue, IndicatorChartPaintType.Point, true);
                //_seriesToLine.CanReBuildHistoricalValues = true;
            }
        }

        public override void OnProcess(List<Candle> candles, int index)
        {
            if (index < _depth.ValueInt)
            {
                return;
            }

            for (int i = candles.Count - _depth.ValueInt - 1; i < _depth.ValueInt; ++i) { }
        }

        private void ReBuildLine(List<decimal> zigZag, List<decimal> line)
        {
            decimal curPoint = 0;
            int lastPointIndex = 0;

            for (int i = 0; i < zigZag.Count; i++)
            {
                if (zigZag[i] == 0)
                {
                    continue;
                }

                if (curPoint == 0)
                {
                    curPoint = zigZag[i];
                    lastPointIndex = i;
                    continue;
                }

                decimal mult = Math.Abs(curPoint - zigZag[i]) / (i - lastPointIndex);

                if (zigZag[i] < curPoint)
                {
                    mult = mult * -1;
                }

                decimal curValue = curPoint;

                for (int i2 = lastPointIndex; i2 < i; i2++)
                {
                    line[i2] = curValue;
                    curValue += mult;
                }

                curPoint = zigZag[i];
                lastPointIndex = i;
            }
        }
    }
}