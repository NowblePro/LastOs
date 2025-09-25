using Newtonsoft.Json;
using OsEngine.Common.UI;
using OsEngine.Entity;
using OsEngine.Indicators;
using OsEngine.Indicators.TrigonumCustom;
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
        private SessionIndicator _si;

        public SessionBot(string name, StartProgram startProgram) : base(name, startProgram)
        {
            foreach (Period period in SessionEditor.Sessions.Where(s => s.IsDefined))
            {
                StrategyParameterInt p = CreateParameter(period.Name, (int)3, 1, 3, 1, "Sessions");
                _periods.Add(p);
            }

            _si = (SessionIndicator)IndicatorsFactory.CreateIndicatorByName(nameClass: "SessionIndicator", name: name + "SessionIndicator", canDelete: false);
            _si = (SessionIndicator)_tab.CreateCandleIndicator(_si, nameArea: "Prime");
            _si.ChartMaster = _tab.GetChartMaster();
        }

        public override void ShowIndividualSettingsDialog() 
        {
            SessionEditor editor = new SessionEditor();
            editor.ShowDialog();
        }

        protected override bool CheckClosePosition(List<Candle> candles, Position position)
        {
            Candle last = candles.Last();
            IEnumerable<Period> periods = SessionEditor.Sessions.Where(s => position.TimeOpen.CompareOnlyTime((DateTime)s.Start));
            DateTime currentTime = last.TimeStart + _tab.Connector.TimeFrameTimeSpan;
            return periods.Any(p => currentTime.CompareOnlyTime((DateTime)p.End));
        }

        protected override bool CheckOpenLongPosition(List<Candle> candles)
        {
            Candle last = candles.Last();
            DateTime currentTime = last.TimeStart + _tab.Connector.TimeFrameTimeSpan;
            IEnumerable<Period> periods = SessionEditor.Sessions.Where(s => currentTime.CompareOnlyTime((DateTime)s.Start));
            IEnumerable<string> names = periods.Select(s => s.Name);
            IEnumerable<StrategyParameterInt> strats = _periods.Where(p => names.Contains(p.Name));
            foreach (StrategyParameterInt p in strats)
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
            DateTime currentTime = last.TimeStart + _tab.Connector.TimeFrameTimeSpan;
            IEnumerable<Period> periods = SessionEditor.Sessions.Where(s => currentTime.CompareOnlyTime((DateTime)s.Start));
            IEnumerable<string> names = periods.Select(s => s.Name);
            IEnumerable<StrategyParameterInt> strats = _periods.Where(p => names.Contains(p.Name));
            foreach (StrategyParameterInt p in strats)
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
