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
using System.Windows.Documents;

namespace OsEngine.Robots.TrigonumCustom.Base
{
    [Bot("MeanReversionZScore")]
    public class MeanReversionZScore : BotPanelSimple
    {
        /// <summary>
        /// Базовый уровень Z Score (который отрисовывается и который является последним уровнем)
        /// </summary>
        private int zScoreRef = 3;

        private Aindicator _sma;
        private ZScoreLow _zScoreLow;
        private ZScoreHigh _zScoreHigh;
        private ZScoreChannel _channel;

        private StrategyParameterInt _periodSma;

        private ZScoreGrid _highGrid = new ;
        private ZScoreGrid _lowGrid = ;

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

            _channel = (ZScoreChannel)IndicatorsFactory.CreateIndicatorByName(nameClass: "ZScoreChannel", name: name + "ZScoreChannel", canDelete: false);
            _channel = (ZScoreChannel)_tab.CreateCandleIndicator(_channel, nameArea: "Prime");
            _channel.LevelReference = zScoreRef;
            _channel.LowZScore = _zScoreLow;
            _channel.HighZScore = _zScoreHigh;
            _channel.DataSeries[0].Color = Color.Yellow;
            _channel.Save();

            UpdateParameters();
        }

        private decimal SMA => _sma.DataSeries[0].Values.Last();

        protected override bool CheckClosePosition(List<Candle> candles, Position position)
        {
            return false;
        }

        protected override bool CheckOpenLongPosition(List<Candle> candles)
        {
            if (!_zScoreHigh.Ready) return false;
            Candle last = candles.Last();
            if (SMA > last.Close)
            {

            }
            return false;
        }

        protected override bool CheckOpenShortPosition(List<Candle> candles)
        {
            if (!_zScoreLow.Ready) return false;
            Candle last = candles.Last();
            if (SMA < last.Close)
            {

            }
            return false;
        }

        protected override List<Func<List<Candle>, bool>> GetCheckers()
        {
            return new List<Func<List<Candle>, bool>>()
            {
                candles => _periodSma.ValueInt < candles.Count,
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

    class ZScoreGrid
    {
        private decimal _spread = 0.5m;
        private List<ZScoreLevel> _levels = new List<ZScoreLevel>();

        public ZScoreGrid(decimal spread, int zScoreReference)
        {
            if (spread == 0) throw new ArgumentException("Шаг z score не может быть равен 0");
            _spread = spread;
            int levelCount = (int)(zScoreReference / _spread);
            if (levelCount == 0) throw new ArgumentException("Количество уровней z score равно 0");
            for (int i = 0; i < levelCount; i++)
            {
                _levels.Add(new ZScoreLevel(spread * (i + 1), i));
            }
        }

        public bool CheckDeal(decimal currentZScore)
        {
            bool result = false;
            IEnumerable<ZScoreLevel> levels = _levels.Where(l => !l.IsDealed && l.CheckDeal(currentZScore));
            return result;
        }

        public void Deal(decimal currentZScore, Position position)
        {
            IEnumerable<ZScoreLevel> levels = _levels.Where(l => !l.IsDealed && l.CheckDeal(currentZScore));
            int maxIndex = levels.Max(l => l.Index);
            ZScoreLevel maxLevel = levels.Where(l => l.Index == maxIndex).Single();
            maxLevel.Deal(position);
            foreach (ZScoreLevel level in levels)
            {
                level.Deal();
            }
        }

        class ZScoreLevel
        {
            private decimal _level = 0;
            private bool _deal = false;
            private int _index;
            private Position _position = null;

            public ZScoreLevel(decimal level, int index)
            {
                _level = level;
                _index = index;
            }

            public bool IsDealed => _deal;

            public int Index => _index;

            public Position Position => _position;

            public bool CheckDeal(decimal currentZScore)
            {
                return currentZScore >= _level;
            }

            public void Deal(Position position = null)
            {
                if (_deal) throw new Exception("Повторная сделка");
                _position = position;
                _deal = true;
            }
        }
    }
}
