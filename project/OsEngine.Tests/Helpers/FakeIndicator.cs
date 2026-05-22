using System.Collections.Generic;
using System.Drawing;
using OsEngine.Entity;
using OsEngine.Indicators;

namespace OsEngine.Tests.Helpers
{
    internal sealed class FakeIndicator : Aindicator
    {
        public FakeIndicator(params decimal[] values)
        {
            DataSeries = new List<IndicatorDataSeries>
            {
                new IndicatorDataSeries(Color.White, "TestSeries", IndicatorChartPaintType.Line, false)
            };

            if (values != null)
            {
                DataSeries[0].Values.AddRange(values);
            }
        }

        public override void OnStateChange(IndicatorState state)
        {
        }

        public override void OnProcess(List<Candle> source, int index)
        {
        }
    }
}
