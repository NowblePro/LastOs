using OsEngine.Entity;
using OsEngine.OsTrader.Panels.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OsEngine.Robots.TrigonumCustom.Base
{
    [Bot("MRZScoreAtrRR")]
    public class MRZScoreAtrRR : BotPanelSimple
    {
        public MRZScoreAtrRR(string name, StartProgram startProgram) : base(name, startProgram)
        {
        }

        protected override bool CheckClosePosition(List<Candle> candles, Position position)
        {
            throw new NotImplementedException();
        }

        protected override bool CheckOpenLongPosition(List<Candle> candles)
        {
            throw new NotImplementedException();
        }

        protected override bool CheckOpenShortPosition(List<Candle> candles)
        {
            throw new NotImplementedException();
        }

        protected override List<Func<List<Candle>, bool>> GetCheckers()
        {
            throw new NotImplementedException();
        }

        protected override void ParametersChangedByUser()
        {
            throw new NotImplementedException();
        }
    }
}
