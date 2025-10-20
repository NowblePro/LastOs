using OsEngine.Entity;
using OsEngine.Indicators;
using OsEngine.OsTrader.Panels.Attributes;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OsEngine.Robots.TrigonumCustom.Base
{
    [Bot("MeanReversion2")]
    public class MeanReversion2 : BotPanelSimple
    {
        private Aindicator _sma;

        private StrategyParameterInt _periodSma;

        public MeanReversion2(string name, StartProgram startProgram) : base(name, startProgram)
        {
            _periodSma = CreateParameter("SMA period", 20, 20, 400, 1, "Robot parameters");

            _sma = IndicatorsFactory.CreateIndicatorByName(nameClass: "ZScore", name: name + "ZScore", canDelete: false);
            _sma = (Aindicator)_tab.CreateCandleIndicator(_sma, nameArea: "ZScore");
            _sma.ParametersDigit[0].Value = _periodSma.ValueInt;
            _sma.DataSeries[0].Color = Color.Red;
            _sma.Save();
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

        protected override void ParametersChangedByUser()
        {

        }
    }
}
