using OsEngine.Common;
using OsEngine.Entity;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OsEngine.Indicators.TrigonumCustom
{
    [Indicator("LinearRegression")]
    public class LinearRegression : Aindicator
    {
        private IndicatorDataSeries _series;
        private IndicatorParameterInt _length;
        /// <summary>
        /// Размер окна
        /// </summary>
        public int N
        {
            get => _length.ValueInt;
            set => _length.ValueInt = value;
        }

        public decimal LastValue => _series.Values.LastOrDefault();

        public override void OnProcess(List<Candle> source, int index)
        {
            if (index < N - 1) return;
            decimal sumX = 0;
            decimal sumY = 0;
            decimal sumXY = 0;
            decimal sumX2 = 0;

            for (int i = 0; i < N; i++)
            {
                decimal x = i + 1;
                decimal y = source[source.Count - N + i].Close;

                sumX += x;
                sumY += y;
                sumXY += x * y;
                sumX2 += x * x;
            }

            // Знаменатель
            decimal divisor = (N * sumX2 - sumX * sumX);
            if (divisor == 0)
            {
                _series.Values[index] = source.Last().Close;
                return;
            }

            // Коэффициент наклона (Slope)
            decimal m = (N * sumXY - sumX * sumY) / divisor;

            // Точка пересечения (Intercept)
            decimal b = (sumY - m * sumX) / N;

            // Итоговое значение на текущей (последней) свече
            _series.Values[index] = m * N + b;
        }

        public override void OnStateChange(IndicatorState state)
        {
            if (state == IndicatorState.Configure)
            {
                PaintOn = true;
                TypeIndicator = IndicatorChartPaintType.Line;
                NeedToResetDataEvent += Reset;
                _length = CreateParameterInt("Length", 50);
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
