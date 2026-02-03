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
    [Indicator("DDR")]
    public class DDR : Aindicator
    {
        private IndicatorDataSeries _rt;
        private IndicatorDataSeries _r;

        public override void OnProcess(List<Candle> source, int index)
        {
            if (index < 1) return;
            Candle prev = source[index - 1];
            Candle last = source[index];
            decimal Rt = (decimal)Math.Log((double)(last.Close / prev.Close));
            _rt.Values[index] = Rt;
        }

        /// <summary>
        /// Размер окна на котором считается r
        /// </summary>
        public int N { get; set; } = 5;

        public override void OnStateChange(IndicatorState state)
        {
            if (state == IndicatorState.Configure)
            {
                PaintOn = true;
                TypeIndicator = IndicatorChartPaintType.Line;
                NeedToResetDataEvent += Reset;

                _rt = CreateSeries("rt", Color.GreenYellow, IndicatorChartPaintType.Line, true);
                _rt.CanReBuildHistoricalValues = false;
                _rt.ChartPaintType = IndicatorChartPaintType.Line;

                _r = CreateSeries("r", Color.GreenYellow, IndicatorChartPaintType.Line, false);
                _r.CanReBuildHistoricalValues = false;
                _r.ChartPaintType = IndicatorChartPaintType.Line;
            }
        }

        private void Reset(IIndicator indicator)
        {

        }
    }
}
