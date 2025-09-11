using OsEngine.Entity;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OsEngine.Indicators.TrigonumCustom
{
    [Indicator("OrderBlockZigZag")]
    public class OrderBlockZigZag : Aindicator
    {
        private Aindicator _zz;
        private IndicatorDataSeries _zzSeries;
        private IndicatorDataSeries _obSeries;
        /// <summary>
        /// Период, в течении которого регистрируются ордер блоки
        /// </summary>
        private IndicatorParameterInt _period;
        public IndicatorParameterInt Period => _period;

        List<IndicatorDataSeries> _dynamicSeries = new List<IndicatorDataSeries>();

        public override void OnProcess(List<Candle> source, int index)
        {
            //int index = _series.Values.Count - 1;
            if (index == 0) return;
            int delta = 1;
            int i = index - delta;
            while (i >= 0 && _zzSeries.Values[i] != _zz.DataSeries[1].Values[i])
            {
                _zzSeries.Values[i] = _zz.DataSeries[1].Values[i];
                delta++;
                i = index - delta;
            }
            GetExtremum();
        }

        private void GetExtremum()
        {
            if (_zzSeries.Values.Count < _period.ValueInt) return;
            //high
            int skip = _zzSeries.Values.Count - _period.ValueInt;
            IEnumerable<decimal> highs = _zz.DataSeries[2].Values.Skip(skip).Where(v => v > 0);
            //lows
            IEnumerable<decimal> lows = _zz.DataSeries[3].Values.Skip(skip).Where(v => v > 0);
            
            foreach (IndicatorDataSeries series in _dynamicSeries)
            {
                DeleteSeries(series);
            }
            _dynamicSeries.Clear();

            int index = 1;

            decimal[] skips = new decimal[skip];

            foreach (decimal v in highs)
            {
                decimal[] val = new decimal[_period.ValueInt];
                for (int i = 0; i< val.Length; i++)
                {
                    val[i] = v;
                }
                IndicatorDataSeries ser = CreateSeries($"high{index}", Color.FromArgb(255 - index, 0, 0), IndicatorChartPaintType.Line, true);
                _dynamicSeries.Add(ser);
                ser.Values.AddRange(skips);
                ser.Values.AddRange(val);
                index++;
            }
        }

        public override void OnStateChange(IndicatorState state)
        {
            if (state == IndicatorState.Configure)
            {
                //_lengthFastLine = CreateParameterInt("Fast line length", 5);
                //_lengthSlowLine = CreateParameterInt("Slow line length", 34);
                //_candlePoint = CreateParameterStringCollection("Candle point", "Typical", Entity.CandlePointsArray);

                //_series = CreateSeries("AC2", Color.DarkGreen, IndicatorChartPaintType.Column, true);
                _zzSeries = CreateSeries("ZZ", Color.DarkGreen, IndicatorChartPaintType.Line, true);
                _obSeries = CreateSeries("OB", Color.Red, IndicatorChartPaintType.Line, true);
                _zz = IndicatorsFactory.CreateIndicatorByName("ZigZag", Name + "ZigZag", false);
                _period = CreateParameterInt("Period", 30);
                //((IndicatorParameterInt)_ao.Parameters[0]).Bind(_lengthFastLine);
                //((IndicatorParameterInt)_ao.Parameters[1]).Bind(_lengthSlowLine);
                //((IndicatorParameterString)_ao.Parameters[2]).Bind(_candlePoint);
                ProcessIndicator("ZigZag", _zz);
                TypeIndicator = IndicatorChartPaintType.Line;
            }
        }
    }
}
