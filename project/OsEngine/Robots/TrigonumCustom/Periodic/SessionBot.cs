using Newtonsoft.Json;
using OsEngine.Common;
using OsEngine.Common.UI;
using OsEngine.Entity;
using OsEngine.Indicators;
using OsEngine.Indicators.TrigonumCustom;
using OsEngine.OsTrader.Panels;
using OsEngine.OsTrader.Panels.Attributes;
using OsEngine.Robots.Classes;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Documents;

namespace OsEngine.Robots.TrigonumCustom.Periodic
{
    [Bot("SessionBot")]
    public class SessionBot : BotPanelSimple
    {
        private List<StrategyParameterInt> _periods = new List<StrategyParameterInt>();
        private SessionIndicator _si;
        /// <summary>
        /// Связанные позиции с сессиями
        /// </summary>
        private Dictionary<Position, PeriodSession> _activeSessions = new Dictionary<Position, PeriodSession>();
        private PeriodSession _sessionOpeningPosition = null;

        /// <summary>
        /// Выход по концу сессии
        /// </summary>
        private StrategyParameterInt _sessionExit;

        public SessionBot(string name, StartProgram startProgram) : base(name, startProgram)
        {
            _multiplePosition = true;
            foreach (Period period in SessionEditor.Sessions.Where(s => s.IsDefined))
            {
                StrategyParameterInt p = CreateParameter(period.Name, (int)3, 1, 3, 1, "Sessions");
                _periods.Add(p);
            }

            var journal = _tab.GetJournal();
            journal.PositionStateChangeEvent += Journal_PositionStateChangeEvent;

            _sessionExit = CreateParameter("SessionExit", 1, 0, 1, 1, "Sessions");
            new TakeProfitDecoration(this);
            new StopLossDecoration(this);
            new TrailingStopDecoration(this);

            _si = (SessionIndicator)IndicatorsFactory.CreateIndicatorByName(nameClass: "SessionIndicator", name: name + "SessionIndicator", canDelete: false);
            _si = (SessionIndicator)_tab.CreateCandleIndicator(_si, nameArea: "Prime");
            _si.ChartMaster = _tab.GetChartMaster();
        }

        private void Journal_PositionStateChangeEvent(Position obj)
        {
            if (_sessionOpeningPosition != null)
            {
                _activeSessions.Add(obj, _sessionOpeningPosition);
                _sessionOpeningPosition = null;
            }
            List<Position> delete = _activeSessions.Keys.Where(p => p.State == PositionStateType.Done).ToList();
            foreach (Position p in delete)
            {
                _activeSessions.Remove(p);
            }
        }

        public override void ShowIndividualSettingsDialog() 
        {
            
        }

        private bool IsStopBySessionEnd(List<Candle> candles, Position position)
        {
            Candle last = candles.Last();
            if (_activeSessions.TryGetValue(position, out PeriodSession session))
            {
                return !session.CheckInSession(last.TimeStart + _tab.Connector.TimeFrameTimeSpan);
            }
            else
            {
                session = SessionEditor.Sessions.Where(s => s.IsDefined && s.Start.Value.CompareTime(position.TimeOpen) == 0).FirstOrDefault();
                if (session != null)
                {
                    return !session.CheckInSession(last.TimeStart + _tab.Connector.TimeFrameTimeSpan);
                }
                return true;
            }
        }

        protected override bool CheckClosePosition(List<Candle> candles, Position position)
        {
            bool result = false;
            if (_sessionExit.ValueInt == 1)
            {
                result |= IsStopBySessionEnd(candles, position);
            }
            return result;
        }

        private bool IsStartSession(PeriodSession session, List<Candle> candles)
        {
            Candle candle = candles.Last();
            DateTime currentTime = candles.Last().TimeStart + _tab.Connector.TimeFrameTimeSpan;
            DateTime prevTime = candles.Last().TimeStart;
            if (session.CheckInSession(currentTime) && !session.CheckInSession(prevTime))
            {
                return true;
            }
            return false;

        }

        protected override bool CheckOpenLongPosition(List<Candle> candles)
        {
            IEnumerable<PeriodSession> periods = SessionEditor.Sessions.Where(s => s.IsDefined && IsStartSession(s, candles));
            IEnumerable<string> names = periods.Select(s => s.Name);
            IEnumerable<StrategyParameterInt> strats = _periods.Where(p => names.Contains(p.Name));
            foreach (StrategyParameterInt p in strats)
            {
                if (p.ValueInt == 1)
                {
                    _sessionOpeningPosition = periods.Where(s => s.Name == p.Name).FirstOrDefault();
                    return true;
                }
            }
            return false;
        }

        protected override bool CheckOpenShortPosition(List<Candle> candles)
        {
            IEnumerable<PeriodSession> periods = SessionEditor.Sessions.Where(s => s.IsDefined && IsStartSession(s, candles));
            IEnumerable<string> names = periods.Select(s => s.Name);
            IEnumerable<StrategyParameterInt> strats = _periods.Where(p => names.Contains(p.Name));
            foreach (StrategyParameterInt p in strats)
            {
                if (p.ValueInt == 2)
                {
                    _sessionOpeningPosition = periods.Where(s => s.Name == p.Name).FirstOrDefault();
                    return true;
                }
            }
            return false;
        }

        protected override List<Func<List<Candle>, bool>> GetCheckers()
        {
            return new List<Func<List<Candle>, bool>>()
            {
                (candles) => candles.Count > 1
            };
        }

        protected override void ParametersChangedByUser()
        {

        }
    }
}
