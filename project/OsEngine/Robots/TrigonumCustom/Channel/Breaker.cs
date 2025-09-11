using OsEngine.Entity;

using OsEngine.Indicators;
using OsEngine.Indicators.TrigonumCustom;
using OsEngine.OsTrader.Panels;
using OsEngine.OsTrader.Panels.Attributes;
using OsEngine.OsTrader.Panels.Tab;
using OsEngine.Robots.TrigonumCustom.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OsEngine.Robots.TrigonumCustom.Channel
{
    [Bot("Breaker")]
    public class Breaker : BotPanelSimple
    {
        #region Parameters
        private StrategyParameterDecimal _maxHigh;
        private StrategyParameterDecimal _minHigh;
        private StrategyParameterDecimal _margin;
        private StrategyParameterInt _rr;
        private StrategyParameterBool _useBody;
        private StrategyParameterInt _period;
        #endregion

        OrderBlockZigZag _ob;
        StrategyParameterInt _lengthZZ;

        public Breaker(string name, StartProgram startProgram) : base(name, startProgram)
        {
            #region Breaker parameters
            _maxHigh = CreateParameter("Max High", 1.0m, 0.6m, 1.5m, 0.05m, "Breaker");
            _minHigh = CreateParameter("Min High", 0.2m, 0.1m, 0.4m, 0.05m, "Breaker");
            _margin = CreateParameter("Margin", 0.2m, 0.1m, 0.4m, 0.05m, "Breaker");
            _rr = CreateParameter("RR", 2, 1, 3, 1, "Breaker");
            _useBody = CreateParameter("Use Body", false, "Breaker");
            _period = CreateParameter("Period", 14, 5, 100, 1, "Breaker");
            #endregion

            _lengthZZ = CreateParameter("Length ZZ", 50, 50, 200, 20, "Breaker");

            _ob = (OrderBlockZigZag)IndicatorsFactory.CreateIndicatorByName(nameClass: "OrderBlockZigZag", name: name + "OrderBlockZigZag", canDelete: false);
            _ob = (OrderBlockZigZag)_tab.CreateCandleIndicator(_ob, nameArea: "Prime");
            //_zz.ParametersDigit[0].Value = _lengthZZ.ValueInt;
            //IndicatorParameterInt period = _ob.CreateParameterInt("Period", 30);
            //ParameterDigit param = new ParameterDigit(period);
            //_ob.ParametersDigit.Add(param);
            _lengthZZ.ValueChange += _lengthZZ_ValueChange;
            _ob.Save();
        }

        private void _lengthZZ_ValueChange()
        {
            _ob.Period.ValueInt = _lengthZZ.ValueInt;
        }

        protected override void ParametersChangedByUser()
        {
            if (_ob.ParametersDigit[0].Value != _lengthZZ.ValueInt)
            {
                _ob.ParametersDigit[0].Value = _lengthZZ.ValueInt;
                _ob.Reload();
                _ob.Save();
            }
        }

        protected override void CandleFinishedEvent(List<Candle> candles)
        {
            if (candles.Count < _lengthZZ.ValueInt) return;

        }
        public override void ShowIndividualSettingsDialog() { }
    }
}
