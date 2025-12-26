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
        public decimal ZScoreReference { get; internal set; }

        public override void OnProcess(List<Candle> source, int index)
        {
            Candle last = source.Last();
            if (LowZScore.Ready)
            {
                decimal avgLow = LowZScore.SMA.DataSeries[0].Last;
                if (avgLow == 0) return;
                decimal deviationLow = Math.Max(0, last.Low - avgLow);
                decimal deviationPctLow = avgLow == 0 ? 0 : deviationLow / avgLow * 100;
                decimal meanLow = LowZScore.Mean;
                decimal standartDeviationLow = LowZScore.LastStandartDeviation;

                decimal dev3PctLow = meanLow + ZScoreReference * standartDeviationLow;
                decimal level3Low = avgLow * (1 - dev3PctLow);
                _channelDataLow.Values[index] = level3Low;
            }
            
            if (HighZScore.Ready)
            {
                decimal avgHigh = HighZScore.SMA.DataSeries[0].Last;
                if (avgHigh == 0) return;
                decimal deviationHigh = Math.Max(0, last.High - avgHigh);
                decimal deviationPctHigh = avgHigh == 0 ? 0 : deviationHigh / avgHigh * 100;
                decimal meanHigh = HighZScore.Mean;
                decimal standartDeviationHigh = HighZScore.LastStandartDeviation;

                decimal dev3PctHigh = meanHigh + ZScoreReference * standartDeviationHigh;
                decimal level3High = avgHigh * (1 + dev3PctHigh);
                _channelDataHigh.Values[index] = level3High;
            }
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
        {
            _channelDataLow.Clear();
            _channelDataHigh.Clear();
        }
    }
}
