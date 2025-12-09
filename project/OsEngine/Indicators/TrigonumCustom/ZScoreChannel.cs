using OsEngine.Entity;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OsEngine.Indicators.TrigonumCustom
{
    [Indicator("ZScoreChannel")]
    public class ZScoreChannel : Aindicator
    {
        private IndicatorDataSeries _channelDataLow;
        private IndicatorDataSeries _channelDataHigh;

        public ZScoreLow LowZScore { get; set; }

        public ZScoreHigh HighZScore { get; set; }

        public override void OnProcess(List<Candle> source, int index)
        {
            Candle last = source.Last();
            decimal avg = LowZScore.SMA.DataSeries[0].Last;
            decimal deviationLow = Math.Max(0, last.Low - avg);
            decimal deviationPctLow = avg == 0 ? 0 : deviationLow / avg * 100;
            decimal meanLow = LowZScore.Mean;
            decimal standartDeviationLow = LowZScore.LastStandartDeviation;

            decimal dev3PctLow = meanLow + 3 * standartDeviationLow;
            decimal level3Low = avg * (1 - dev3PctLow);
            _channelDataLow.Values[index] = level3Low;

            decimal deviationHigh = Math.Max(0, avg - last.High);
            decimal deviationPctHigh = avg == 0 ? 0 : deviationHigh / avg * 100;
            decimal meanHigh = HighZScore.Mean;
            decimal standartDeviationHigh = HighZScore.LastStandartDeviation;

            decimal dev3PctHigh = meanHigh - 3 * standartDeviationHigh;
            decimal level3High = avg * (1 - dev3PctHigh);
            _channelDataHigh.Values[index] = level3High;
        }

        public override void OnStateChange(IndicatorState state)
        {
            if (state == IndicatorState.Configure)
            {
                PaintOn = true;
                TypeIndicator = IndicatorChartPaintType.Line;
                NeedToResetDataEvent += Reset;

                _channelDataLow = CreateSeries("Channel data low", Color.Yellow, IndicatorChartPaintType.Line, true);
                _channelDataLow.CanReBuildHistoricalValues = false;
                _channelDataLow.ChartPaintType = IndicatorChartPaintType.Line;

                _channelDataHigh = CreateSeries("Channel data high", Color.Green, IndicatorChartPaintType.Line, true);
                _channelDataHigh.CanReBuildHistoricalValues = false;
                _channelDataHigh.ChartPaintType = IndicatorChartPaintType.Line;
            }
        }

        private void Reset(IIndicator indicator)
        { }
    }
}
