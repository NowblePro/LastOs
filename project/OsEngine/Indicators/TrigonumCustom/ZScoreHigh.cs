using OsEngine.Entity;
using OsEngine.Logging;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OsEngine.Indicators.TrigonumCustom
{
    [Indicator("ZScoreHigh")]
    public class ZScoreHigh : Aindicator
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

        public decimal LastDeviation => _deviation.LastOrDefault();

        //public decimal LastStandartDeviation { get; private set; }
        public Dictionary<int, decimal> _all_deviations = new Dictionary<int, decimal>();
        public Dictionary<int, decimal> AllDeviations => _all_deviations;
        //public decimal Mean { get; private set; }
        public Dictionary<int, decimal> Means = new Dictionary<int, decimal>();

        public bool Ready => _deviation.Count > _window_sigma.ValueInt;

        public decimal LastValue => _seriesZ.Values.LastOrDefault();
        private int lastIndex = -1;

        public override void OnProcess(List<Candle> source, int index)
        {
            try
            {
                for (int i = lastIndex + 1; i < source.Count; i++)
                {
                    Candle candle = source[i];
                    decimal sma = _sma.DataSeries[0].Values[i];
                    if (sma == 0) continue;
                    decimal high = Math.Max(0, candle.High - sma);
                    if (high > 0)
                    {
                        _deviation.Add(high);
                    }
                    if (_sma == null || _deviation.Count < _window_sigma.ValueInt || (_window_sigma.ValueInt) == 0)
                    {
                        continue;
                    }
                    int skip = _deviation.Count - _window_sigma.ValueInt;
                    decimal avg = _deviation.Skip(skip).Average();
                    Means.Add(i, avg / sma);
                    decimal sumOfSquares = (decimal)_deviation.Skip(skip).Sum(x => Math.Pow((double)(x - avg), 2));
                    decimal variance = sumOfSquares / _window_sigma.ValueInt;
                    decimal standartDeviation = (decimal)Math.Sqrt((double)variance);
                    _all_deviations.Add(i, standartDeviation / sma);
                    if (standartDeviation == 0) continue;
                    decimal result = (high - avg) / standartDeviation;
                    _seriesZ.Values[i] = result;
                    lastIndex = i;
                }
            }
            catch (Exception ex)
            {

            }

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
        {
            _deviation.Clear();
            _seriesZ.Clear();
            lastIndex = -1;
            _all_deviations.Clear();
            Means.Clear();
        }
    }
}
