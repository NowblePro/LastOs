using OsEngine.Entity;
using OsEngine.OsTrader.Panels.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OsEngine.Robots.TrigonumCustom.Base
{
    [Bot("DivergenceBot")]
    public class DivergenceBot : BotPanelSimple
    {
        public DivergenceBot(string name, StartProgram startProgram) : base(name, startProgram)
        {
        }

        protected override bool CheckClosePosition(List<Candle> candles, Position position)
        {

        }

        protected override bool CheckOpenLongPosition(List<Candle> candles)
        {

        }

        protected override bool CheckOpenShortPosition(List<Candle> candles)
        {

        }

        protected override List<Func<List<Candle>, bool>> GetCheckers()
        {

        }

        protected override void ParametersChangedByUser()
        {

        }
    }
}
