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
        private int lastIndex = -1;
        public ZScoreLow LowZScore { get; set; }

        public ZScoreHigh HighZScore { get; set; }
        public decimal ZScoreReference { get; internal set; }

        public override void OnProcess(List<Candle> source, int index)
        {
            for (int i = lastIndex + 1; i < source.Count; i++)
            {
                Candle last = source[i];
                if (LowZScore.Ready)
                {
                    decimal avgLow = LowZScore.SMA.DataSeries[0].Values[i];
                    if (avgLow == 0) continue;
                    decimal deviationLow = Math.Max(0, last.Low - avgLow);
                    decimal deviationPctLow = avgLow == 0 ? 0 : deviationLow / avgLow * 100;
                    decimal meanLow;
                    if (LowZScore.Means.ContainsKey(i))
                    {
                        meanLow = LowZScore.Means[i];
                    }
                    else
                    {
                        var lower = LowZScore.Means.Keys.Where(k => k < i);
                        if (lower.Any())
                        {
                            int maxIndexMean = lower.Max();
                            meanLow = LowZScore.Means[maxIndexMean];
                        }
                        else
                        {
                            meanLow = 0;
                        }
                    }
                    //decimal standartDeviationLow = LowZScore.LastStandartDeviation;
                    decimal standartDeviationLow;
                    if (LowZScore.AllDeviations.ContainsKey(i))
                    {
                        standartDeviationLow = LowZScore.AllDeviations[i];
                    }
                    else
                    {
                        var lower = LowZScore.AllDeviations.Keys.Where(k => k < i);
                        if (lower.Any())
                        {
                            int stdIndex = lower.Max();
                            standartDeviationLow = LowZScore.AllDeviations[stdIndex];
                        }
                        else
                        {
                            standartDeviationLow = 0;
                        }
                            
                    }
                    decimal dev3PctLow = meanLow + ZScoreReference * standartDeviationLow;
                    decimal level3Low = avgLow * (1 - dev3PctLow);
                    _channelDataLow.Values[i] = level3Low;
                }

                if (HighZScore.Ready)
                {
                    decimal avgHigh = HighZScore.SMA.DataSeries[0].Values[i];
                    if (avgHigh == 0) continue;
                    decimal deviationHigh = Math.Max(0, last.High - avgHigh);
                    decimal deviationPctHigh = avgHigh == 0 ? 0 : deviationHigh / avgHigh * 100;
                    decimal meanHigh;
                    if (HighZScore.Means.ContainsKey(i))
                    {
                        meanHigh = HighZScore.Means[i];
                    }
                    else
                    {
                        var lower = HighZScore.Means.Keys.Where(k => k < i);
                        if (lower.Any())
                        {
                            int maxIndexMean = lower.Max();
                            meanHigh = HighZScore.Means[maxIndexMean];
                        }
                        else
                        {
                            meanHigh = 0;
                        }
                            
                    }
                    //decimal standartDeviationHigh = HighZScore.LastStandartDeviation;
                    decimal standartDeviationHigh;
                    if (HighZScore.AllDeviations.ContainsKey(i))
                    {
                        standartDeviationHigh = HighZScore.AllDeviations[i];
                    }
                    else
                    {
                        var lower = HighZScore.AllDeviations.Keys.Where(k => k < i);
                        if (lower.Any())
                        {
                            int stdIndex = lower.Max();
                            standartDeviationHigh = HighZScore.AllDeviations[stdIndex];
                        }
                        else
                        {
                            standartDeviationHigh = 0;
                        }
                    }

                    decimal dev3PctHigh = meanHigh + ZScoreReference * standartDeviationHigh;
                    decimal level3High = avgHigh * (1 + dev3PctHigh);
                    _channelDataHigh.Values[i] = level3High;
                }
                lastIndex = i;
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
            lastIndex = -1;
        }
    }
}
