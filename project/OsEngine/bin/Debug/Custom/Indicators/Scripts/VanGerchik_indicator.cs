using OsEngine.Entity;
using OsEngine.Indicators;
using System.Collections.Generic;
using System.Drawing;

 class VanGerchik_indicator : Aindicator
{
    private IndicatorParameterInt _lenghtUp;
    private IndicatorParameterInt _lenghtDown;

    private IndicatorParameterDecimal  _deviationUp;
    private IndicatorParameterDecimal _deviationDown;

    private IndicatorDataSeries _seriesUpPC;
    private IndicatorDataSeries _seriesDownPC;

    private IndicatorDataSeries _seriesUpDev;
    private IndicatorDataSeries _seriesDownDev;

    public override void OnStateChange(IndicatorState state)
    {
        if (state == IndicatorState.Configure)
        {
            _lenghtUp = CreateParameterInt("Period up line", 21);
            _lenghtDown = CreateParameterInt("Period down line", 21);

            _deviationUp = CreateParameterDecimal("Deviation up line %", 1m);
            _deviationDown = CreateParameterDecimal("Deviation down line %", 1m);

            _seriesUpPC = CreateSeries("Up line PC", Color.Aqua, IndicatorChartPaintType.Line, true);
            _seriesDownPC = CreateSeries("Down line PC", Color.Yellow, IndicatorChartPaintType.Line, true);

            _seriesUpDev = CreateSeries("Deviation Up line", Color.Aquamarine, IndicatorChartPaintType.Point, true);
            _seriesDownDev = CreateSeries("Deviation Down line", Color.LightGoldenrodYellow, IndicatorChartPaintType.Point, true);
        }
    }

    public override void OnProcess(List<Candle> candles, int index)
    {
        if (index <= _lenghtUp.ValueInt || index <= _lenghtDown.ValueInt)
        {
            return;
        }

        decimal upLine = 0;

        if (index - _lenghtUp.ValueInt > 0)
        {
            for (int i = index; i > -1 && i > index - _lenghtUp.ValueInt; i--)
            {
                if (upLine < candles[i].High)
                {
                    upLine = candles[i].High;
                }
            }
        }

        decimal downLine = 0;

        if (index - _lenghtDown.ValueInt > 0)
        {
            downLine = decimal.MaxValue;

            for (int i = index; i > -1 && i > index - _lenghtDown.ValueInt; i--)
            {
                if (downLine > candles[i].Low)
                {
                    downLine = candles[i].Low;
                }
            }
        }

        _seriesUpPC.Values[index] = upLine;
        _seriesDownPC.Values[index] = downLine;

        _seriesUpDev.Values[index] = upLine - (upLine * _deviationUp.ValueDecimal / 100);
        _seriesDownDev.Values[index] = downLine + (downLine * _deviationDown.ValueDecimal / 100);

    }
}

