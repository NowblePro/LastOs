using OsEngine.Common;
using OsEngine.Entity;
using OsEngine.Indicators;
using OsEngine.Indicators.TrigonumCustom;
using OsEngine.OsTrader.Panels.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OsEngine.Robots.TrigonumCustom.Base
{
    [Bot("MeanReversion1Fix")]
    public class MeanReversion1Fix : BotPanelSimple
    {
        private Aindicator _sma;
        private Aindicator _ema;
        private AtrDev _atrDev;

        private AtrDecoration _atr;
        private StrategyParameterDecimal _atrMultDev;
        private StrategyParameterInt _smaLength;

        private StrategyParameterDecimal _zEnterBaseLong;
        private StrategyParameterDecimal _zEnterBaseShort;
        private StrategyParameterInt _emaLength;
        private StrategyParameterDecimal _spread;

        private StrategyParameterDecimal _atrMultSpread;

        private MeanReverseGrid _currentGrid = null;

        private int _gridSize = 7;

        // Ключ уровня, который мы собираемся заполнить при следующем успешном открытии позиции.
        // -1 означает "не задан".
        private int _nextGridKeyToFill = -1;

        public MeanReversion1Fix(string name, StartProgram startProgram) : base(name, startProgram)
        {
            _multiplePosition = true;
            _tab.TPSLMode = TPSLMode.Partial;
            _emaLength = CreateParameter("EMA period", 200, 100, 300, 1, "Robot");
            _sma = IndicatorsFactory.CreateIndicatorByName("Sma", name + "Sma", false);
            _sma = (Aindicator)_tab.CreateCandleIndicator(_sma, "Prime");

            _ema = (Aindicator)IndicatorsFactory.CreateIndicatorByName(nameClass: "Ema", name: name + "Ema", canDelete: false);
            _ema = (Aindicator)_tab.CreateCandleIndicator(_ema, nameArea: "Prime");
            _ema.Save();

            _smaLength = CreateParameter("Sma Length", 14, 14, 500, 50, "Robot");

            _zEnterBaseLong = CreateParameter("Z Enter Base Long", -2m, -3m, -1m, 0.1m, "Robot");
            _zEnterBaseShort = CreateParameter("Z Enter Base Short", 2m, 1m, 3m, 0.1m, "Robot");

            _spread = CreateParameter("Spread", 1m, 0.1m, 1m, 0.1m, "Robot");

            _atr = new AtrDecoration(this);
            _atr.CancelTPSL = false;

            _atrDev = (AtrDev)IndicatorsFactory.CreateIndicatorByName("AtrDev", name + "AtrDev", false);
            _atrDev = (AtrDev)_tab.CreateCandleIndicator(_atrDev, "AtrDev");
            _atrDev.Sma = _sma;
            _atrDev.Atr = _atr;

            _atrMultDev = CreateParameter("Atr Mult Dev", 1m, 1m, 5m, 0.5m, "Robot");
            _atrMultSpread = CreateParameter("Atr Mult Spread", 1m, 1m, 5m, 0.5m, "Robot");

            new StopLossDecoration(this);
            new TakeProfitDecoration(this);
            new VolatileStopDecoration(this, VolatileStopHandler);
            _tab.PositionOpeningSuccesEvent += _tab_PositionOpeningSuccesEvent;
            ParametersChangedByUser();
        }

        private void _tab_PositionOpeningSuccesEvent(Position obj)
        {
            if (_currentGrid == null)
            {
                decimal atr = _atr.CurrentAtr;
                _currentGrid = new MeanReverseGrid(obj.EntryPrice, _spread.ValueDecimal + atr * _atrMultSpread.ValueDecimal, _gridSize, obj.Direction, _tab.GetChartMaster().Candles.Count - 1);
                _nextGridKeyToFill = -1;
            }
            else
            {
                try
                {
                    // Игнорируем позиции противоположного направления
                    if (obj.Direction != _currentGrid.Direction)
                    {
                        return;
                    }

                    Dictionary<int, decimal> grid = _currentGrid.GetGrid();
                    Dictionary<int, Position> positions = _currentGrid.GetPositions();

                    // Если в CheckOpen... мы заранее определили ключ уровня для заполнения,
                    // пытаемся присвоить позицию именно ему.
                    if (_nextGridKeyToFill != -1 && grid.ContainsKey(_nextGridKeyToFill) && !positions.ContainsKey(_nextGridKeyToFill))
                    {
                        _currentGrid.SetPosition(_nextGridKeyToFill, obj);

                        // Удалим (или пометим) прочие уровни, которые удовлетворяли условию открытия
                        // и находятся "выше" заполненного уровня, чтобы в дальнейшем
                        // сравнения проходили только для уровней более экстремальных.
                        List<int> keysToDelete = new List<int>();
                        decimal selectedValue = grid[_nextGridKeyToFill];

                        if (_currentGrid.Direction == Side.Buy)
                        {
                            // Для лонга оставляем только уровни ниже выбранного (меньше по цене).
                            foreach (var pair in grid)
                            {
                                if (pair.Key == _nextGridKeyToFill) continue;
                                if (pair.Value >= selectedValue && !positions.ContainsKey(pair.Key))
                                {
                                    keysToDelete.Add(pair.Key);
                                }
                            }
                        }
                        else if (_currentGrid.Direction == Side.Sell)
                        {
                            // Для шорта оставляем только уровни выше выбранного (больше по цене).
                            foreach (var pair in grid)
                            {
                                if (pair.Key == _nextGridKeyToFill) continue;
                                if (pair.Value <= selectedValue && !positions.ContainsKey(pair.Key))
                                {
                                    keysToDelete.Add(pair.Key);
                                }
                            }
                        }

                        foreach (int key in keysToDelete)
                        {
                            _currentGrid.DeleteByKey(key);
                        }

                        // Сбросим ожидание
                        _nextGridKeyToFill = -1;
                        return;
                    }
                }
                catch (Exception ex)
                {
                    SendNewLogMessage(ex.Message, Logging.LogMessageType.Error);
                }
            }
        }

        protected override void CandleFinishedEvent(List<Candle> candles)
        {
            if (_currentGrid != null)
            {

            }

            base.CandleFinishedEvent(candles);
        }

        private void VolatileStopHandler()
        {

        }

        protected override bool CheckClosePosition(List<Candle> candles, Position position)
        {
            return false;
        }

        protected override bool CheckOpenLongPosition(List<Candle> candles)
        {
            if (_currentGrid != null && _currentGrid.Direction == Side.Sell) return false;

            if (_currentGrid == null)
            {
                decimal z = _atrDev.LastValue;
                decimal ema = _ema.DataSeries[0].Last;
                decimal price = candles.Last().Close;
                decimal atr = _atr.CurrentAtr;
                if (z < _zEnterBaseLong.ValueDecimal && price > ema)
                {
                    return true;
                }
            }
            else
            {
                try
                {
                    // Логика: ищем незаполненные уровни грида, среди них те, у которых цена свечи ниже значения уровня.
                    // Если таких уровней несколько — выбираем самый "нижний" (самый маленький price для Buy).
                    Dictionary<int, decimal> grid = _currentGrid.GetGrid();
                    Dictionary<int, Position> positions = _currentGrid.GetPositions();

                    decimal price = candles.Last().Close;

                    var emptyLevels = grid.Where(p => !positions.ContainsKey(p.Key)).ToList();
                    var candidates = emptyLevels.Where(p => price < p.Value).ToList();

                    if (!candidates.Any())
                    {
                        return false;
                    }

                    // Для Buy выбираем самый нижний уровень (минимальное значение цены)
                    var target = candidates.OrderBy(p => p.Value).First();
                    _nextGridKeyToFill = target.Key;

                    return true;
                }
                catch (Exception ex)
                {
                    SendNewLogMessage(ex.Message, Logging.LogMessageType.Error);
                    return false;
                }
            }

            return false;
        }

        protected override bool CheckOpenShortPosition(List<Candle> candles)
        {
            if (_currentGrid != null && _currentGrid.Direction == Side.Buy) return false;

            if (_currentGrid == null)
            {
                decimal z = _atrDev.LastValue;
                decimal ema = _ema.DataSeries[0].Last;
                decimal price = candles.Last().Close;
                decimal atr = _atr.CurrentAtr;
                if (z > _zEnterBaseShort.ValueDecimal && price < ema)
                {
                    return true;
                }
            }
            else
            {
                try
                {
                    // Зеркальная логика для шорта:
                    // ищем незаполненные уровни грида, среди них те, у которых цена свечи выше значения уровня.
                    // Если таких уровней несколько — выбираем самый "верхний" (максимальное значение цены для Sell).
                    Dictionary<int, decimal> grid = _currentGrid.GetGrid();
                    Dictionary<int, Position> positions = _currentGrid.GetPositions();

                    decimal price = candles.Last().Close;

                    var emptyLevels = grid.Where(p => !positions.ContainsKey(p.Key)).ToList();
                    var candidates = emptyLevels.Where(p => price > p.Value).ToList();

                    if (!candidates.Any())
                    {
                        return false;
                    }

                    // Для Sell выбираем самый верхний уровень (максимальное значение цены)
                    var target = candidates.OrderByDescending(p => p.Value).First();
                    _nextGridKeyToFill = target.Key;

                    return true;
                }
                catch (Exception ex)
                {
                    SendNewLogMessage(ex.Message, Logging.LogMessageType.Error);
                    return false;
                }
            }

            return false;
        }

        protected override List<Func<List<Candle>, bool>> GetCheckers()
        {
            return new List<Func<List<Candle>, bool>>();
        }

        private void SetAtrDevParameters()
        {
            if (_atrDev == null || _atrMultDev == null) return;
            _atrDev.AtrMultDev = _atrMultDev.ValueDecimal;
        }

        private void SetSmaParameters()
        {
            if (_smaLength == null || _sma == null) return;
            if (_sma?.Parameters[0] is IndicatorParameterInt parameter)
            {
                parameter.ValueInt = _smaLength.ValueInt;
            }
        }

        private void SetEmaParameters()
        {
            if (_emaLength == null || _ema == null) return;
            if (_ema?.Parameters[0] is IndicatorParameterInt parameter)
            {
                parameter.ValueInt = _emaLength.ValueInt;
            }
        }

        protected override void ParametersChangedByUser()
        {
            SetAtrDevParameters();
            SetSmaParameters();
            SetEmaParameters();
        }
    }
}