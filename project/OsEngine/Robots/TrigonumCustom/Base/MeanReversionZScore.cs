using OsEngine.Entity;
using OsEngine.Indicators;
using OsEngine.Indicators.TrigonumCustom;
using OsEngine.OsTrader.Panels.Attributes;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OsEngine.Robots.TrigonumCustom.Base
{
    [Bot("MeanReversionZScore")]
    public class MeanReversionZScore : BotPanelSimple
    {
        private Aindicator _sma;
        private ZScoreLow _zScoreLow;
        private ZScoreHigh _zScoreHigh;

        private StrategyParameterInt _periodSma;

        public MeanReversionZScore(string name, StartProgram startProgram) : base(name, startProgram)
        {
            _sma = IndicatorsFactory.CreateIndicatorByName("Sma", name + "Sma", false);
            _sma = (Aindicator)_tab.CreateCandleIndicator(_sma, "Prime");
            _periodSma = CreateParameter("Sma Period", 50, 50, 500, 50, "Robot");

            _zScoreLow = (ZScoreLow)IndicatorsFactory.CreateIndicatorByName(nameClass: "ZScoreLow", name: name + "ZScoreLow", canDelete: false);
            _zScoreLow = (ZScoreLow)_tab.CreateCandleIndicator(_zScoreLow, nameArea: "ZScoreLow");
            _zScoreLow.DataSeries[0].Color = Color.Blue;
            _zScoreLow.Save();
            _zScoreLow.SMA = _sma;

            _zScoreHigh = (ZScoreHigh)IndicatorsFactory.CreateIndicatorByName(nameClass: "ZScoreHigh", name: name + "ZScoreHigh", canDelete: false);
            _zScoreHigh = (ZScoreHigh)_tab.CreateCandleIndicator(_zScoreHigh, nameArea: "ZScoreHigh");
            _zScoreHigh.DataSeries[0].Color = Color.Red;
            _zScoreHigh.Save();
            _zScoreHigh.SMA = _sma;

            UpdateParameters();
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
            return new List<Func<List<Candle>, bool>>()
            {
                candles => true
            };
        }

        protected override void ParametersChangedByUser()
        {
            UpdateParameters();
        }

        private void UpdateParameters()
        {
            SetSmaPeriod();
        }

        private void SetSmaPeriod()
        {
            if (_sma?.Parameters[0] is IndicatorParameterInt parameter)
            {
                parameter.ValueInt = _periodSma.ValueInt;
            }
        }
    }
}
