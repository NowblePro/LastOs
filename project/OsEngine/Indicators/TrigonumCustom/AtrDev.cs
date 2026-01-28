using OsEngine.Common;
using OsEngine.Entity;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OsEngine.Indicators.TrigonumCustom
{
    [Indicator("AtrDev")]
    public class AtrDev : Aindicator
    {
        private IndicatorDataSeries _series;

        public Aindicator Sma { get; set; }

        public AtrDecoration Atr { get; set; }

        public decimal AtrMultDev { get; set; }

        private decimal SmaLast => Sma.DataSeries[0].Values.Last();

        public decimal LastValue => _series.Values.Last();

        /// <summary>
        /// Индикатор не имеет отрицательных значений, берётся модуль
        /// </summary>
        public bool OnlyPositive { get; set; } = false;

        public override void OnProcess(List<Candle> source, int index)
        {
            Candle last = source.Last();
            decimal sma = SmaLast;
            decimal price = GetMaxDeviationPrice(new List<decimal>() { last.Close, last.Low, last.High }, sma);
            if (sma == 0) return;
            decimal atr = Atr.CurrentAtr / sma;
            decimal atrMult = AtrMultDev;
            int sign = Math.Sign(price - sma);
            decimal z = ((price - sma)/sma) *   (OnlyPositive ? sign : 1)
                            + (atr * atrMult) * (OnlyPositive ? 1 : sign);
            _series.Values[index] = z;
        }

        public override void OnStateChange(IndicatorState state)
        {
            if (state == IndicatorState.Configure)
            {
                PaintOn = true;
                TypeIndicator = IndicatorChartPaintType.Line;
                NeedToResetDataEvent += Reset;

                _series = CreateSeries("_series", Color.GreenYellow, IndicatorChartPaintType.Line, true);
                _series.CanReBuildHistoricalValues = false;
                _series.ChartPaintType = IndicatorChartPaintType.Line;
            }
        }

        private void Reset(IIndicator indicator)
        {

        }

        private decimal GetMaxDeviationPrice(IEnumerable<decimal> prices, decimal sma)
        {
            decimal currentPrice = 0;
            decimal currentModule = -1;

            foreach (decimal price in prices)
            {
                SetCurrentValues(price);
            }

            void SetCurrentValues(decimal price)
            {
                decimal module = Math.Abs(price - sma);
                if (module > currentModule)
                {
                    currentPrice = price;
                    currentModule = module;
                }
            }

            return currentPrice;
        }
    }
}
