using OsEngine.Entity;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OsEngine.Indicators.TrigonumCustom
{
    [Indicator("ZScoreLow")]
    public class ZScoreLow : Aindicator
    {
        private Aindicator _sma;
        private List<decimal> _deviation = new List<decimal>();
        private IndicatorDataSeries _seriesZ;
        /// <summary>
        /// Ширина окна для расчёта отклонения
        /// </summary>
        private IndicatorParameterInt _window_sigma;

        public Aindicator SMA
        {
            get { return _sma; }
            set { _sma = value; }
        }

        public override void OnProcess(List<Candle> source, int index)
        {
            int i = source.Count - 1;
            Candle candle = source[i];
            decimal sma = _sma.DataSeries[0].Values[i];
            if (sma == 0) return;
            decimal low = Math.Max(0, sma - candle.Low);
            if (low > 0)
            {
                _deviation.Add(low);
            }
            if (_sma == null || _deviation.Count < _window_sigma.ValueInt)
            {
                return;
            }
            int skip = _deviation.Count - _window_sigma.ValueInt;
            decimal avg = _deviation.Skip(skip).Average();
            decimal sumOfSquares = (decimal)_deviation.Skip(skip).Sum(x => Math.Pow((double)(x - avg), 2));
            decimal variance = sumOfSquares / _window_sigma.ValueInt - 1;
            decimal standartDeviation = (decimal)Math.Sqrt((double)variance);
            decimal result = (low - avg) / standartDeviation;
            _seriesZ.Values[i] = result;
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

                _window_sigma = CreateParameterInt("Window Sigma", 500);
            }
        }

        private void Reset(IIndicator indicator)
        { }
    }
}
