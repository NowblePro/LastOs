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
        public Dictionary<DateTime, decimal> _all_deviations = new Dictionary<DateTime, decimal>();
        public Dictionary<DateTime, decimal> AllDeviations => _all_deviations;

        public Dictionary<DateTime, decimal> Means = new Dictionary<DateTime, decimal>();
        private DateTime _firstCandleTime = DateTime.MinValue;

        public override void OnProcess(List<Candle> source, int index)
        {
            try
            {
                if (source == null ||
                    source.Count == 0 ||
                    index < 0 ||
                    index >= source.Count ||
                    _sma == null ||
                    _sma.DataSeries == null ||
                    _sma.DataSeries.Count == 0 ||
                    _sma.DataSeries[0] == null ||
                    _sma.DataSeries[0].Values == null ||
                    _sma.DataSeries[0].Values.Count <= index ||
                    _window_sigma == null ||
                    _window_sigma.ValueInt == 0)
                {
                    return;
                }

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
                    _firstCandleTime = source[0].TimeStart;
                }

                Candle candle = source[index];
                decimal sma = _sma.DataSeries[0].Values[index];
                if (sma == 0) return;
                decimal low = Math.Max(0, sma - candle.Low);
                if (candle.State == CandleState.Finished && low > 0)
                {
                    _deviation.Add(low);
                }
                if (_deviation.Count < _window_sigma.ValueInt)
                {
                    return;
                }
                int skip = _deviation.Count - _window_sigma.ValueInt;
                decimal avg = _deviation.Skip(skip).Average();
                Means[candle.TimeStart] = avg / sma;
                decimal sumOfSquares = (decimal)_deviation.Skip(skip).Sum(x => Math.Pow((double)(x - avg), 2));
                decimal variance = sumOfSquares / _window_sigma.ValueInt;
                decimal standartDeviation = (decimal)Math.Sqrt((double)variance);
                _all_deviations[candle.TimeStart] = standartDeviation / sma;
                if (standartDeviation == 0) return;
                decimal result = (low - avg) / standartDeviation;
                _seriesZ.Values[index] = result;
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
            _deviation.Clear();
            _all_deviations.Clear();
            Means.Clear();
            SendNewLogMessage("ZScoreLow: сброс состояния", LogMessageType.System);
        }
    }
}
