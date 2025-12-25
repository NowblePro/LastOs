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

        public override void OnProcess(List<Candle> source, int index)
        {

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

                _window_sigma = CreateParameterInt("Window Sigma", 500);
            }
        }

        private void Reset(IIndicator indicator)
        {
            throw new NotImplementedException();
        }
    }
}
