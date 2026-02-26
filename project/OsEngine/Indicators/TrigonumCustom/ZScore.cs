using OsEngine.Entity;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Documents;
using static Tinkoff.InvestApi.V1.GetTechAnalysisRequest.Types;

namespace OsEngine.Indicators.TrigonumCustom
{
    [Indicator("ZScore")]
    public class ZScore : Aindicator
    {
        private Aindicator _sma;
        private IndicatorDataSeries _seriesZ;
        private IndicatorDataSeries _seriesSigma;
        /// <summary>
        /// Ширина окна для расчёта отклонения
        /// </summary>
        private IndicatorParameterInt _window_sigma;
        private int lastIndex = -1;
        private DateTime _firstCandleTime = DateTime.MinValue;
        public override void OnProcess(List<Candle> source, int index)
        {
            if (_window_sigma.ValueInt > index || _sma == null)
            {
                return;
            }

            if (_firstCandleTime == DateTime.MinValue)
            {
                _firstCandleTime = source[0].TimeStart;
            }
            else if (source[0].TimeStart != _firstCandleTime)
            {
                lastIndex = -1;
                _firstCandleTime = source[0].TimeStart;
            }
            
            for (int i = lastIndex + 1; i <= index && i < source.Count; i++)
            {
                if (i >= _window_sigma.ValueInt)
                {
                    Candle candle = source[i];
                    if (candle.State != CandleState.Finished)
                    {
                        break;
                    }
                    decimal price = source[i].Close;
                    decimal sma = _sma.DataSeries[0].Values[i];
                    int startIndex = i - _window_sigma.ValueInt + 1;


                    IEnumerable<decimal> closes = source.Skip(startIndex).Take(_window_sigma.ValueInt).Select(candle => candle.Close);
                    
                    decimal sigma = GetSigma(closes);
                    decimal value = (price - sma) / sigma;
                    _seriesZ.Values[i] = value;
                    _seriesSigma.Values[i] = sigma;
                    lastIndex = i;
                }
            }
        }

        public Aindicator SMA
        {
            get { return _sma; }
            set { _sma = value; }
        }

        public decimal CurrentZ => DataSeries[0].Last;

        public decimal CurrentSigma => DataSeries[1].Last;

        private decimal GetSigma(IEnumerable<decimal> source)
        {
            double mean = (double)source.Average();
            double sum = 0;
            foreach (decimal value in source)
            {
                sum += Math.Pow(((double)value - mean), 2);
            }

            decimal result = (decimal)Math.Sqrt(sum / (source.Count() - 1));
            if (result == 0)
            {
                result = 0.000001m;
            }
            return result;
        }

        public override void OnStateChange(IndicatorState state)
        {
            if (state == IndicatorState.Configure)
            {
                PaintOn = true;
                TypeIndicator = IndicatorChartPaintType.Line;
                NeedToResetDataEvent += Reset;
                _seriesZ = CreateSeries("_seriesZ", Color.GreenYellow, IndicatorChartPaintType.Line, true);
                _seriesZ.CanReBuildHistoricalValues = false;
                _seriesZ.ChartPaintType = IndicatorChartPaintType.Line;

                _seriesSigma = CreateSeries("_seriesSigma", Color.Blue, IndicatorChartPaintType.Line, false);
                _seriesSigma.CanReBuildHistoricalValues = false;
                _seriesSigma.ChartPaintType = IndicatorChartPaintType.Line;

                _window_sigma = CreateParameterInt("Window Sigma", 30);
            }
        }

        private void Reset(IIndicator indicator)
        {
            lastIndex = -1;
            _seriesZ.Clear();
            _seriesSigma.Clear();
        }
    }
}
