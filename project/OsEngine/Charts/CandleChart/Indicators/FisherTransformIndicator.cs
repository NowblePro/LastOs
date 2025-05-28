using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OsEngine.Entity;
using OsEngine.Indicators;

namespace OsEngine.Charts.CandleChart.Indicators
{
    internal class FisherTransformIndicator : IIndicator
    {
        /// <summary> Результат </summary>
        private List<decimal> values = new List<decimal>();
        /// <summary> Промежуточные значения </summary>
        private List<decimal> v1 = new List<decimal>();
        private List<Candle> candles;
        /// <summary> SMA для значений индикатора Фишера </summary>
        private MovingAverage fma = new MovingAverage(false);
        private int period;
        private int smaPeriod;

        public FisherTransformIndicator(string uniqName, bool canDelete)
        {
            Name = uniqName;
            CanDelete = canDelete;
            period = 10;
            smaPeriod = 3;
            fma.Length = smaPeriod;
            PaintOn = true;
        }

        /// <summary>
        /// Candles quantity for Fisher Transform counting
        /// Количество свечей, для которых считается Фишер
        /// </summary>
        public int FisherPeriod
        {
            get => period;
            set => period = value;
        }

        /// <summary>
        /// SMA size of Fisher values smoothing
        /// Размер скользящего окна
        /// </summary>
        public int SmaPeriod
        {
            get => smaPeriod;
            set => smaPeriod = value;
        }

        public IndicatorChartPaintType TypeIndicator { get => IndicatorChartPaintType.Line; set { } }

        public List<Color> Colors => new List<Color>() { Color.Red, Color.Blue };

        public List<List<decimal>> ValuesToChart => new List<List<decimal>>() { values, fma.Values ?? new List<decimal>() };

        public bool CanDelete { get; set; }
        public string NameSeries { get; set; }
        public string NameArea { get; set; }
        public string Name { get; set; }
        public bool PaintOn { get; set; }

        public event Action<IIndicator> NeedToReloadEvent;

        public void Clear()
        {
            values.Clear();
            v1.Clear();
        }

        public void Delete()
        {
            if (File.Exists(@"Engine\" + Name + @".txt"))
            {
                File.Delete(@"Engine\" + Name + @".txt");
            }
        }

        public void Load()
        {
            if (!File.Exists(@"Engine\" + Name + @".txt"))
            {
                return;
            }
            try
            {

            }
            catch (Exception)
            {

            }
        }

        public void Process(List<Candle> candles)
        {
            if (candles.Count < period) return;
            int count = candles.Count - values.Count;
            if (count < 1) return;
            this.candles = candles;
            // Новые свечи, в первый расчёт == period, потом обычно 1 штука
            IEnumerable<Candle> newCandles = candles.Skip(values.Count);
            // Последние 10 свечей (period свечей)
            IEnumerable<Candle> lastCandles = candles.Skip(candles.Count - period);

            decimal min = lastCandles.Min(c => c.Low);
            decimal max = lastCandles.Max(c => c.High);

            foreach (Candle candle in newCandles)
            {
                decimal price = candle.Center;
                decimal prevV1 = v1.Any() ? v1.Last() : 0;
                decimal prevFish = values.Any() ? values.Last() : 0;
                decimal currV1 = ((price - min) / (max - min)) - 0.5m + 0.5m * prevV1;
                if (currV1 > 0.999m) currV1 = 0.999m;
                if (currV1 < -0.999m) currV1 = -0.999m;
                v1.Add(currV1);
                decimal currFish = 0.25m * (decimal)Math.Log((double)((1m + currV1) / (1m - currV1))) + 0.5m * prevFish;
                values.Add(currFish);
            }
            fma.Process(values);
        }

        public void Save()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(Name))
                {
                    return;
                }
            }
            catch (Exception)
            {

            }
        }

        public void ShowDialog()
        {

        }
    }
}
