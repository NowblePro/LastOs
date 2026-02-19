using OsEngine.Entity;
using OsEngine.Logging;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
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

        public bool PaintSeries
        {
            get
            {
                return _seriesZ.IsPaint;
            }
            set
            {
                _seriesZ.IsPaint = value;
            }
        }

        public decimal LastDeviation => _deviation.LastOrDefault();

        public bool Ready => _deviation.Count > _window_sigma.ValueInt;

        public decimal LastValue => _seriesZ.Values.LastOrDefault();
        private int lastIndex = -1;

        public Dictionary<DateTime, decimal> _all_deviations = new Dictionary<DateTime, decimal>();
        public Dictionary<DateTime, decimal> AllDeviations => _all_deviations;

        public Dictionary<DateTime, decimal> Means = new Dictionary<DateTime, decimal>();
        private DateTime _firstCandleTime = DateTime.MinValue;

        public override void OnProcess(List<Candle> source, int index)
        {
            try
            {
                if (_firstCandleTime == DateTime.MinValue)
                {
                    _firstCandleTime = source[0].TimeStart;
                }
                else if (source[0].TimeStart != _firstCandleTime)
                {
                    SendNewLogMessage($"ZScoreLow: сдвиг истории обнаружен. Старое время: {_firstCandleTime:HH:mm:ss}, новое: {source[0].TimeStart:HH:mm:ss}", LogMessageType.System);
                    _deviation.Clear();
                    _all_deviations.Clear();
                    Means.Clear();
                    lastIndex = -1;
                    _firstCandleTime = source[0].TimeStart;
                }

                for (int i = lastIndex + 1; i < source.Count; i++)
                {
                    Candle candle = source[i];
                    decimal sma = _sma.DataSeries[0].Values[i];
                    if (sma == 0) continue;
                    decimal low = Math.Max(0, sma - candle.Low);
                    if (low > 0)
                    {
                        _deviation.Add(low);
                    }
                    if (_sma == null || _deviation.Count < _window_sigma.ValueInt || _window_sigma.ValueInt == 0)
                    {
                        continue;
                    }
                    int skip = _deviation.Count - _window_sigma.ValueInt;
                    decimal avg = _deviation.Skip(skip).Average();
                    Means[candle.TimeStart] = avg / sma;
                    decimal sumOfSquares = (decimal)_deviation.Skip(skip).Sum(x => Math.Pow((double)(x - avg), 2));
                    decimal variance = sumOfSquares / _window_sigma.ValueInt;
                    decimal standartDeviation = (decimal)Math.Sqrt((double)variance);
                    _all_deviations[candle.TimeStart] = standartDeviation / sma;
                    if (standartDeviation == 0) continue;
                    decimal result = (low - avg) / standartDeviation;
                    _seriesZ.Values[i] = result;
                    lastIndex = i;
                }
            }
            catch (Exception ex)
            {
                SendNewLogMessage($"ZScoreLow.OnProcess ошибка: {ex.Message}", LogMessageType.Error);
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
            _seriesZ.Clear();
            lastIndex = -1;
            _deviation.Clear();
            _all_deviations.Clear();
            Means.Clear();
            SendNewLogMessage("ZScoreLow: сброс состояния", LogMessageType.System);
        }
    }
}
