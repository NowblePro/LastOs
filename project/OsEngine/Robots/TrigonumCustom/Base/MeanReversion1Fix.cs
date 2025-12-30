using OsEngine.Common;
using OsEngine.Entity;
using OsEngine.Indicators;
using OsEngine.Indicators.TrigonumCustom;
using OsEngine.OsTrader.Panels.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OsEngine.Robots.TrigonumCustom.Base
{
    [Bot("MeanReversion1Fix")]
    public class MeanReversion1Fix : BotPanelSimple
    {
        private Aindicator _sma;
        private AtrDev _atrDev;

        private AtrDecoration _atr;
        private StrategyParameterDecimal _atrMultDev;
        private StrategyParameterInt _smaLength;


        public MeanReversion1Fix(string name, StartProgram startProgram) : base(name, startProgram)
        {
            _multiplePosition = true;
            _tab.TPSLMode = TPSLMode.Partial;
            _sma = IndicatorsFactory.CreateIndicatorByName("Sma", name + "Sma", false);
            _sma = (Aindicator)_tab.CreateCandleIndicator(_sma, "Prime");

            _smaLength = CreateParameter("Sma Length", 14, 14, 500, 50, "Robot");

            _atr = new AtrDecoration(this);
            _atr.CancelTPSL = false;

            _atrDev = (AtrDev)IndicatorsFactory.CreateIndicatorByName("AtrDev", name + "AtrDev", false);
            _atrDev = (AtrDev)_tab.CreateCandleIndicator(_atrDev, "AtrDev");
            _atrDev.Sma = _sma;
            _atrDev.Atr = _atr;

            _atrMultDev = CreateParameter("Atr Mult Dev", 1m, 1m, 5m, 0.5m, "Robot");

            new VolatileStopDecoration(this, VolatileStopHandler);

            ParametersChangedByUser();
        }

        private void VolatileStopHandler()
        {
            
        }

        protected override bool CheckClosePosition(List<Candle> candles, Position position)
        {
            return false;
        }

        protected override bool CheckOpenLongPosition(List<Candle> candles)
        {
            return false;
        }

        protected override bool CheckOpenShortPosition(List<Candle> candles)
        {
            return false;
        }

        protected override List<Func<List<Candle>, bool>> GetCheckers()
        {
            return new List<Func<List<Candle>, bool>>();
        }

        private void SetAtrDevParameters()
        {
            if (_atrDev == null || _atrMultDev == null) return;
            _atrDev.AtrMultDev = _atrMultDev.ValueDecimal;
        }

        private void SetSmaParameters()
        {
            if (_smaLength == null || _sma == null) return;
            if (_sma?.Parameters[0] is IndicatorParameterInt parameter)
            {
                parameter.ValueInt = _smaLength.ValueInt;
            }
        }

        protected override void ParametersChangedByUser()
        {
            SetAtrDevParameters();
            SetSmaParameters();
        }
    }
}
