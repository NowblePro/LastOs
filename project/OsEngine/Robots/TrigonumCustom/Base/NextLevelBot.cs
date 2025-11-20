using OsEngine.Common;
using OsEngine.Entity;
using OsEngine.Indicators;
using OsEngine.OsTrader.Panels.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OsEngine.Robots.TrigonumCustom.Base
{
    [Bot("NextLevelBot")]
    public class NextLevelBot : BotPanelSimple
    {
        private StrategyParameterInt _x;
        private StrategyParameterDecimal _minSize;

        private Aindicator _sma;
        private StrategyParameterInt _smaPeriod;
        private StrategyParameterBool _smaFilter;
        private bool _atrStop = false;

        public NextLevelBot(string name, StartProgram startProgram) : base(name, startProgram)
        {
            _x = CreateParameter("X", 3, 3, 10, 1, "Robot");
            _minSize = CreateParameter("Min Size", 1m, 1m, 20m, 0.1m, "Robot");

            _smaFilter = CreateParameter("Sma Filter", false, "Robot");
            _smaPeriod = CreateParameter("SMA Period", 10, 100, 150, 10, "Robot");
            _sma = IndicatorsFactory.CreateIndicatorByName("Sma", name + "Sma", false);
            _sma = (Aindicator)_tab.CreateCandleIndicator(_sma, "Prime");
            _sma.ParametersDigit[0].Value = _smaPeriod.ValueInt;
            _sma.Save();
            new TakeProfitDecoration(this);
            new StopLossDecoration(this);
            new TrailingStopDecoration(this);
            StopAtrDecoration atrStop = new StopAtrDecoration(this);
            atrStop.AtrStop += AtrStop_AtrStop;
            ParametersChangedByUser();
        }

        private void AtrStop_AtrStop(object sender, EventArgs e)
        {
            _atrStop = true;
        }

        protected override bool CheckClosePosition(List<Candle> candles, Position position)
        {
            if (_atrStop)
            {
                _atrStop = false;
                return true;
            }
            return false;
        }

        protected override bool CheckOpenLongPosition(List<Candle> candles)
        {
            int x = _x.ValueInt;
            if (candles.Count < x) return false;
            int skip = candles.Count - x;
            decimal sma = _sma.DataSeries[0].Values.Last();
            if (candles.Last().Close < sma) return false;
            return candles.Skip(skip).All(c => c.Close > c.Open && GetCandleSize(c) >= _minSize.ValueDecimal);
        }

        protected override bool CheckOpenShortPosition(List<Candle> candles)
        {
            int x = _x.ValueInt;
            if (candles.Count < x) return false;
            int skip = candles.Count - x;
            decimal sma = _sma.DataSeries[0].Values.Last();
            if (candles.Last().Close > sma) return false;
            return candles.Skip(skip).All(c => c.Close < c.Open && GetCandleSize(c) >= _minSize.ValueDecimal);
        }

        private decimal GetCandleSize(Candle candle)
        {
            return (candle.High - candle.Low) / candle.Low * 100;
        }

        protected override List<Func<List<Candle>, bool>> GetCheckers()
        {
            return new List<Func<List<Candle>, bool>>() 
            {
                candles => candles.Count >= _x.ValueInt,
            };
        }

        protected override void ParametersChangedByUser()
        {
            if (_sma == null) return;
            _sma.ParametersDigit[0].Value = _smaPeriod.ValueInt;
            _sma.Save();
        }
    }
}
