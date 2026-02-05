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
        private IndicatorDataSeries _series;

        public override void OnProcess(List<Candle> source, int index)
        {
            if (index < 1) return;
            if (N < 2) return;
            Candle prev = source[index - 1];
            Candle last = source[index];
            decimal Rt = (decimal)Math.Log((double)(last.Close / prev.Close));
            _rt.Values[index] = Rt;

            if (index < N - 1) return;
            decimal r = 0;
            for (int i = index - (N - 1); i < index; i++)
            {
                r += _rt.Values[i];
            }
            r /= N;

            decimal s = 0;
            for (int i = index - (N - 1); i < index; i++)
            {
                s += (decimal)Math.Pow((double)(_rt.Values[i] - r), 2);
            }

            s /= N - 1;
            s = (decimal)Math.Sqrt((double)s);

            decimal drift = r * N;
            decimal diffusion = s * (decimal)Math.Sqrt(N);
            decimal ddr = Math.Abs(drift) / diffusion;
            _series.Values[index] = ddr;
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

                _rt = CreateSeries("rt", Color.GreenYellow, IndicatorChartPaintType.Line, false);
                _rt.CanReBuildHistoricalValues = false;
                _rt.ChartPaintType = IndicatorChartPaintType.Line;

                _series = CreateSeries("series", Color.GreenYellow, IndicatorChartPaintType.Line, true);
                _series.CanReBuildHistoricalValues = false;
                _series.ChartPaintType = IndicatorChartPaintType.Line;
            }
        }

        private void Reset(IIndicator indicator)
        {

        }
    }
}
