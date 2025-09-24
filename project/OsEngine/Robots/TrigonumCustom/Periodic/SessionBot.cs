using Newtonsoft.Json;
using OsEngine.Common.UI;
using OsEngine.Entity;
using OsEngine.OsTrader.Panels;
using OsEngine.OsTrader.Panels.Attributes;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OsEngine.Robots.TrigonumCustom.Periodic
{
    [Bot("SessionBot")]
    public class SessionBot : BotPanelSimple
    {
        private List<StrategyParameterInt> _periods = new List<StrategyParameterInt>();

        public SessionBot(string name, StartProgram startProgram) : base(name, startProgram)
        {
            foreach (Period period in SessionEditor.Sessions.Where(s => s.IsDefined))
            {
                StrategyParameterInt p = CreateParameter(period.Name, (int)3, 1, 3, 1, "Sessions");
                _periods.Add(p);
            }
        }

        public override void ShowIndividualSettingsDialog() 
        {
            SessionEditor editor = new SessionEditor();
            editor.ShowDialog();
        }

        protected override bool CheckClosePosition(List<Candle> candles, Position position)
        {
            return false;
        }

        protected override bool CheckOpenLongPosition(List<Candle> candles)
        {
            Candle last = candles.Last();
            foreach (StrategyParameterInt p in _periods)
            {
                if (p.ValueInt == 1)
                {
                    return true;
                }
            }
            return false;
        }

        protected override bool CheckOpenShortPosition(List<Candle> candles)
        {
            Candle last = candles.Last();
            foreach (StrategyParameterInt p in _periods)
            {
                if (p.ValueInt == 2)
                {
                    return true;
                }
            }
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
