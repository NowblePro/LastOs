using OsEngine.Common;
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
using System.Windows.Forms.DataVisualization.Charting;

namespace OsEngine.Robots.TrigonumCustom.Base
{
    [Bot("MeanReversionSma2")]
    public class MeanReversionSma2 : BotPanelSimple
    {
        private ZScore _zScore;
        private Aindicator _ema;

        private StrategyParameterInt _periodSma;
        private StrategyParameterDecimal _zEnterBaseLong;
        private StrategyParameterDecimal _zEnterBaseShort;
        private StrategyParameterInt _periodEma;

        public MeanReversionSma2(string name, StartProgram startProgram) : base(name, startProgram)
        {
            _multiplePosition = true;
            _periodSma = CreateParameter("SMA period", 20, 20, 400, 1, "Robot parameters");
            _periodEma = CreateParameter("EMA period", 200, 14, 250, 1, "Robot parameters");

            _zScore = (ZScore)IndicatorsFactory.CreateIndicatorByName(nameClass: "ZScore", name: name + "ZScore", canDelete: false);
            _zScore = (ZScore)_tab.CreateCandleIndicator(_zScore, nameArea: "ZScore");
            _zScore.ParametersDigit[0].Value = _periodSma.ValueInt;
            _zScore.DataSeries[0].Color = Color.Red;
            _zScore.Save();

            _ema = (Aindicator)IndicatorsFactory.CreateIndicatorByName(nameClass: "Ema", name: name + "Ema", canDelete: false);
            _ema = (Aindicator)_tab.CreateCandleIndicator(_ema, nameArea: "Prime");
            _ema.Save();

            _zEnterBaseLong = CreateParameter("Z Enter Base Long", -2m, -3m, -1m, 0.1m, "Robot");
            _zEnterBaseShort = CreateParameter("Z Enter Base Short", 2m, 1m, 3m, 0.1m, "Robot");

            new TakeProfitDecoration(this);
            new StopLossDecoration(this);

            SetEmaPeriod();
        }

        private void SetEmaPeriod()
        {
            if (_ema?.Parameters[0] is IndicatorParameterInt parameter)
            {
                parameter.ValueInt = _periodEma.ValueInt;
            }
        }

        protected override bool CheckClosePosition(List<Candle> candles, Position position)
        {
            return false;
        }

        protected override bool CheckOpenLongPosition(List<Candle> candles)
        {
            decimal z = _zScore.DataSeries[0].Last;
            decimal ema = _ema.DataSeries[0].Last;
            decimal price = candles.Last().Close;
            if (z < _zEnterBaseLong.ValueDecimal && ema < price)
            {
                return true;
            }
            return false;
        }

        protected override bool CheckOpenShortPosition(List<Candle> candles)
        {
            decimal z = _zScore.DataSeries[0].Last;
            decimal ema = _ema.DataSeries[0].Last;
            decimal price = candles.Last().Close;
            if (z > _zEnterBaseShort.ValueDecimal && ema > price)
            {
                return true;
            }
            return false;
        }

        protected override List<Func<List<Candle>, bool>> GetCheckers()
        {
            return new List<Func<List<Candle>, bool>>() 
            {
                (candles) => { return candles.Count >= _periodSma.ValueInt; }
            };
        }

        protected override void OnChartPostPaint(object sender, ChartPaintEventArgs e)
        {

        }

        protected override void ParametersChangedByUser()
        {
            SetEmaPeriod();
        }
    }
}
