using OsEngine.Entity;
using OsEngine.Logging;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Tinkoff.InvestApi.V1.GetTechAnalysisRequest.Types;

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
        int lastCount = -1;
        private DateTime _firstCandleTime = DateTime.MinValue;

        public decimal ChannelDataLowLast => _channelDataLow.Values.LastOrDefault();
        public decimal ChannelDataHighLast => _channelDataHigh.Values.LastOrDefault();

        public override void OnProcess(List<Candle> source, int index)
        {
            if (_firstCandleTime == DateTime.MinValue)
            {
                _firstCandleTime = source[0].TimeStart;
            }
            else if (source[0].TimeStart != _firstCandleTime)
            {
                lastIndex = -1;
                _firstCandleTime = source[0].TimeStart;
            }

            for (int i = lastIndex + 1; i < source.Count; i++)
            {
                Candle candle = source[i];
                DateTime time = candle.TimeStart;

                if (LowZScore.Ready)
                {
                    decimal avgLow = LowZScore.SMA.DataSeries[0].Values[i];
                    if (avgLow == 0) continue;
                    decimal deviationLow = Math.Max(0, candle.Low - avgLow);
                    decimal deviationPctLow = avgLow == 0 ? 0 : deviationLow / avgLow;

                    decimal meanLow = 0;
                    decimal stdLow = 0;

                    if (LowZScore.Means.TryGetValue(time, out decimal mLow))
                        meanLow = mLow;
                    else
                    {
                        var earlier = LowZScore.Means.Keys.Where(t => t < time).OrderByDescending(t => t).FirstOrDefault();
                        if (earlier != default)
                            meanLow = LowZScore.Means[earlier];
                    }

                    if (LowZScore.AllDeviations.TryGetValue(time, out decimal sLow))
                        stdLow = sLow;
                    else
                    {
                        var earlier = LowZScore.AllDeviations.Keys.Where(t => t < time).OrderByDescending(t => t).FirstOrDefault();
                        if (earlier != default)
                            stdLow = LowZScore.AllDeviations[earlier];
                    }

                    decimal dev3PctLow = meanLow + ZScoreReference * stdLow;
                    decimal level3Low = avgLow * (1 - dev3PctLow);
                    _channelDataLow.Values[i] = level3Low;
                }

                if (HighZScore.Ready)
                {
                    decimal avgHigh = HighZScore.SMA.DataSeries[0].Values[i];
                    if (avgHigh == 0) continue;
                    decimal deviationHigh = Math.Max(0, candle.High - avgHigh);
                    decimal deviationPctHigh = avgHigh == 0 ? 0 : deviationHigh / avgHigh;

                    decimal meanHigh = 0;
                    decimal stdHigh = 0;

                    if (HighZScore.Means.TryGetValue(time, out decimal mHigh))
                        meanHigh = mHigh;
                    else
                    {
                        var earlier = HighZScore.Means.Keys.Where(t => t < time).OrderByDescending(t => t).FirstOrDefault();
                        if (earlier != default)
                            meanHigh = HighZScore.Means[earlier];
                    }

                    if (HighZScore.AllDeviations.TryGetValue(time, out decimal sHigh))
                        stdHigh = sHigh;
                    else
                    {
                        var earlier = HighZScore.AllDeviations.Keys.Where(t => t < time).OrderByDescending(t => t).FirstOrDefault();
                        if (earlier != default)
                            stdHigh = HighZScore.AllDeviations[earlier];
                    }

                    decimal dev3PctHigh = meanHigh + ZScoreReference * stdHigh;
                    decimal level3High = avgHigh * (1 + dev3PctHigh);
                    _channelDataHigh.Values[i] = level3High;
                }

                lastIndex = i;
            }

            lastCount = source.Count;
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
            SendNewLogMessage("ZScoreChannel: сброс состояния", LogMessageType.System);
        }
    }
}
