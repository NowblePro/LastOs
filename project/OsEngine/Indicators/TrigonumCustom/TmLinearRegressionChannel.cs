using System;
using System.Collections.Generic;
using System.Drawing;
using OsEngine.Entity;

// LinearRegressionChannel custom indicator

namespace OsEngine.Indicators.TrigonumCustom
{
    [Indicator("TmLinearRegressionChannel")]
    public class TmLinearRegressionChannel : Aindicator
    {
        private IndicatorParameterInt _period;
        private IndicatorDataSeries _seriesRegressionLine;
        private IndicatorParameterString _candlePoint;

        private decimal _x_summ;
        private decimal _x_sqr_summ;

        public override void OnStateChange(IndicatorState state)
        {
            if (state == IndicatorState.Configure)
            {
                _period = CreateParameterInt("Period", 14);

                _candlePoint = CreateParameterStringCollection("Candle Point", "Close", Entity.CandlePointsArray);

                _seriesRegressionLine = CreateSeries("Regression Line ", Color.HotPink, IndicatorChartPaintType.Line, true);
            }
        }

        public override void OnProcess(List<Candle> candles, int index)
        {
            if (index - _period.ValueInt <= 0 || _period.ValueInt < 1)
            {
                return;
            }

            if (_period.ValueInt > 0)
            {
                _x_summ = _period.ValueInt * (_period.ValueInt - 1) / 2m;
                _x_sqr_summ = (_period.ValueInt - 1) * _period.ValueInt * (2 * (_period.ValueInt - 1) + 1) / 6m;
            }
            else
            {
                _x_summ = 0;
                _x_sqr_summ = 0;
            }

            _seriesRegressionLine.Values[index] = CalcLRVal(candles, index);
        }

        private decimal CalcLRVal(List<Candle> candles, int index)
        {
            if (index < _period.ValueInt - 1) return 0;

            decimal y_summ = 0m;
            decimal xy_summ = 0m;

            for (int i = 0; i < _period.ValueInt; i++)
            {
                int x = i;
                decimal y = candles[index - (_period.ValueInt - 1) + i].GetPoint(_candlePoint.ValueString);
                y_summ += y;
                xy_summ += x * y;
            }

            decimal koef_a = CalcKoefA(y_summ, xy_summ);
            decimal koef_b = CalcKoefB(y_summ, koef_a);

            return koef_a * (_period.ValueInt - 1) + koef_b;
        }

        private decimal CalcKoefA(decimal y_summ, decimal xy_summ)
        {
            decimal denominator = _period.ValueInt * _x_sqr_summ - _x_summ * _x_summ;
            return denominator == 0 ? 0 : (_period.ValueInt * xy_summ - _x_summ * y_summ) / denominator;
        }

        private decimal CalcKoefB(decimal y_summ, decimal koef_a)
        {
            return (y_summ - koef_a * _x_summ) / _period.ValueInt;
        }

    }
}
