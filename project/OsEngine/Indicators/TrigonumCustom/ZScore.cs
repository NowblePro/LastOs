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
                    sigma = (decimal)Math.Max((double)sigma, 0.001);
                    decimal value = (price - sma) / sigma;
                    _seriesZ.Values.Add(value);
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
            return result;
        }

        public override void OnStateChange(IndicatorState state)
        {
            if (state == IndicatorState.Configure)
            {
                TypeIndicator = IndicatorChartPaintType.Line;
                NeedToResetDataEvent += Reset;
                _seriesZ = CreateSeries("_seriesZZHighPoints", Color.GreenYellow, IndicatorChartPaintType.Point, true);
                _seriesZ.CanReBuildHistoricalValues = false;

                _sma = IndicatorsFactory.CreateIndicatorByName("Sma", "Sma", false);
                _window_sigma = CreateParameterInt("Window Sigma", 30);
                ProcessIndicator("ZigZag", _sma);
                TypeIndicator = IndicatorChartPaintType.Line;
            }
        }

        private void Reset(IIndicator indicator)
        { }
    }
}
