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

        public override void OnProcess(List<Candle> source, int index)
        {
            decimal price = source.Last().Close;
            decimal sma = SmaLast;
            if (sma == 0) return;
            decimal atr = Atr.CurrentAtr / price;
            decimal atrMult = AtrMultDev;
            decimal z = ((Math.Abs(price - sma))/sma) + atr * atrMult;
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
    }
}
