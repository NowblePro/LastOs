using OsEngine.Entity;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Tinkoff.InvestApi.V1.GetTechAnalysisRequest.Types;

namespace OsEngine.Indicators.TrigonumCustom
{
    [Indicator("ZScore")]
    public class ZScore : Aindicator
    {
        private Aindicator _sma;
        private IndicatorDataSeries _seriesZ;
        /// <summary>
        /// Ширина окна для расчёта отклонения
        /// </summary>
        private IndicatorParameterInt _window_sigma;
        public override void OnProcess(List<Candle> source, int index)
        {
            if (_window_sigma.ValueInt > index)
            {
                return;
            }

            int i = source.Count - 1;

            for (; i < source.Count; i++)
            {
                if (i < _window_sigma.ValueInt)
                {
                    _seriesZ.Values.Add(0);
                }
                else
                {
                    decimal price = source[i].Close;
                    decimal sma = _sma.DataSeries[0].Values[i];
                    int startIndex = i - _window_sigma.ValueInt + 1;
                    decimal sigma = GetSigma(source.Skip(startIndex).Take(_window_sigma.ValueInt).Select(candle => candle.Close));
                    decimal value = (price - sma) / sigma;
                    _seriesZ.Values[i] = value;
                }
            }
        }

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
                _seriesZ = CreateSeries("_seriesZZHighPoints", Color.GreenYellow, IndicatorChartPaintType.Point, true);
                _seriesZ.CanReBuildHistoricalValues = false;
                _seriesZ.ChartPaintType = IndicatorChartPaintType.Line;

                _sma = IndicatorsFactory.CreateIndicatorByName("Sma", "Sma", false);
                _sma.TypeIndicator = IndicatorChartPaintType.Line;
                _window_sigma = CreateParameterInt("Window Sigma", 30);
                ProcessIndicator("ZigZag", _sma);
            }
        }

        private void Reset(IIndicator indicator)
        { }
    }
}
