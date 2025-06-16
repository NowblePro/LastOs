using OsEngine.Entity;
using OsEngine.Indicators;
using OsEngine.OsTrader.Panels;
using OsEngine.OsTrader.Panels.Tab;
using System.Collections.Generic;
using System.Drawing;
using System;
using OsEngine.OsTrader.Panels.Attributes;
using OsEngine.Robots.Classes;

namespace OsEngine.Robots.TrigonumCustom.Trailing
{
    [Bot("TrailingImpulseHmaRsi")]
    public class TrailingImpulseHmaRsi : BotPanel
    {
        BotTabSimple _tab;

        public StrategyParameterString Regime;
        public StrategyParameterDecimal VolumeOnPosition;
        public StrategyParameterString VolumeRegime;
        public StrategyParameterDecimal Slippage;
        public StrategyParameterString _orderType;

        private StrategyParameterTimeOfDay TimeStart;
        private StrategyParameterTimeOfDay TimeEnd;

        private StrategyParameterBool _saveJson;

        public Aindicator _Sma;
        public StrategyParameterInt _periodSma;

        public Aindicator _hma;
        public StrategyParameterInt _periodHma;

        public Aindicator _hma2;
        public StrategyParameterInt _periodHma2;

        public Aindicator _atr;
        public StrategyParameterInt _periodAtr;
        public StrategyParameterDecimal _multiplerAtr;

        public Aindicator _smaFilter;
        private StrategyParameterInt SmaLengthFilter;
        public StrategyParameterBool SmaPositionFilterIsOn;
        public StrategyParameterBool SmaSlopeFilterIsOn;

        // создаем переменные для Трейлинг стопа
        //---------------------------------
        private TrailingStop _trailingStop;
        private StrategyParameterBool TrailingStopIsOn;
        private StrategyParameterString TrailingStopTypeOrder;
        private StrategyParameterDecimal ChangeStepStop;
        private StrategyParameterDecimal MinDist;
        private StrategyParameterDecimal QuantityStepsPrices;
        private StrategyParameterString PointOrPercent;
        //---------------------------------

        // RSI
        Aindicator _rsi;
        public StrategyParameterInt _lengthRsi;
        public StrategyParameterDecimal _oversoldRsi;
        public StrategyParameterDecimal _overboughtRsi;
        public StrategyParameterBool _drawRsiChannel;
        public StrategyParameterBool _rsiFilterIsOn;
        // RSI

        private decimal _lastPrice;
        private decimal _lastSma;
        private decimal _prewSma;
        private decimal _prew2Sma;
        private decimal _lastHma;
        private decimal _prewHma;
        private decimal _prew2Hma;
        private decimal _lastFHma;
        private decimal _lastHma2;
        private decimal _prewHma2;
        private decimal _prew2Hma2;
        private decimal _lastFHma2;
        private decimal _lastAtr;

        public TrailingImpulseHmaRsi(string name, StartProgram startProgram) : base(name, startProgram)
        {
            TabCreate(BotTabType.Simple);
            _tab = TabsSimple[0];

            Regime = CreateParameter("Regime", "Off", new[] { "Off", "On", "OnlyLong", "OnlyShort", "OnlyClosePosition" }, "Base");
            VolumeRegime = CreateParameter("Volume type", "Number of contracts", new[] { "Number of contracts", "Contract currency", "% of the total portfolio" }, "Base");
            VolumeOnPosition = CreateParameter("Volume", 10, 1.0m, 50, 4, "Base");
            Slippage = CreateParameter("Slippage %", 0m, 0, 20, 1, "Base");
            _orderType = CreateParameter("Order type", "Stop", new[] { "Stop", "Market", "Market with checks" }, "Base");

            _saveJson = CreateParameter("Save Json Data", false, "Base");

            TimeStart = CreateParameterTimeOfDay("Start Trade Time", 0, 0, 0, 0, "Base");
            TimeEnd = CreateParameterTimeOfDay("End Trade Time", 24, 0, 0, 0, "Base");

            _periodSma = CreateParameter("SMA period", 500, 100, 1000, 100, "Robot parameters");
            _periodHma = CreateParameter("HMA period", 500, 100, 1000, 100, "Robot parameters");
            _periodHma2 = CreateParameter("HMA2 period", 150, 50, 500, 100, "Robot parameters");
            _periodAtr = CreateParameter("Atr period", 14, 5, 50, 5, "Robot parameters");
            _multiplerAtr = CreateParameter("Atr multipler", 1m, 0.1m, 5.0m, 0.5m, "Robot parameters");

            SmaLengthFilter = CreateParameter("Sma Length Filter", 100, 10, 500, 1, "Filters");

            SmaPositionFilterIsOn = CreateParameter("Is SMA Filter On", false, "Filters");
            SmaSlopeFilterIsOn = CreateParameter("Is Sma Slope Filter On", false, "Filters");

            // создаем параметры настроек и создаем объект класса TrailingStop
            //---------------------------------
            TrailingStopIsOn = CreateParameter("Is Trailing stop On", false, "Trailing Stop");
            TrailingStopTypeOrder = CreateParameter("Type order", OrderPriceType.Market.ToString(), new[] { OrderPriceType.Market.ToString(), OrderPriceType.Limit.ToString() }, "Trailing Stop");
            PointOrPercent = CreateParameter("Choice Points or Percent", "Points", new[] { "Points", "Percent" }, "Trailing Stop");
            ChangeStepStop = CreateParameter("Stop level change step", 1, 1, 100, 001m, "Trailing Stop");
            MinDist = CreateParameter("Minimum distance to price", 1, 1, 100, 0.01m, "Trailing Stop");
            QuantityStepsPrices = CreateParameter("Quantity steps prices for limit order", 0m, 0, 10000, 1, "Trailing Stop");
            _trailingStop = new TrailingStop(_tab, TrailingStopTypeOrder.ValueString, ChangeStepStop.ValueDecimal, MinDist.ValueDecimal, QuantityStepsPrices.ValueDecimal, PointOrPercent.ValueString);
            //---------------------------------

            // RSI
            _rsiFilterIsOn = CreateParameter("Is RSI Filter On", true, "Filters");
            _lengthRsi = CreateParameter("Rsi Length", 14, 10, 33, 1, "Filters");
            _oversoldRsi = CreateParameter("Rsi Oversold", 30m, 25, 45, 5, "Filters");
            _overboughtRsi = CreateParameter("Rsi Overbought", 70m, 55, 75, 5, "Filters");
            _drawRsiChannel = CreateParameter("Draw Ovb/Ovs Channel", false, "Filters");

            _rsi = IndicatorsFactory.CreateIndicatorByName(nameClass: "RSI", name: name + "RSI", canDelete: false);
            _rsi = (Aindicator)_tab.CreateCandleIndicator(_rsi, nameArea: "RsiArea");
            _rsi.DataSeries[0].Color = System.Drawing.Color.Coral;
            _rsi.ParametersDigit[0].Value = _lengthRsi.ValueInt;
            _rsi.ParametersDigit[1].Value = _oversoldRsi.ValueDecimal;
            _rsi.ParametersDigit[2].Value = _overboughtRsi.ValueDecimal;
            _rsi.DataSeries[1].IsPaint = _drawRsiChannel.ValueBool;
            _rsi.DataSeries[2].IsPaint = _drawRsiChannel.ValueBool;
            _rsi.Save();
            // RSI

            _smaFilter = IndicatorsFactory.CreateIndicatorByName(nameClass: "Sma", name: name + "Sma_Filter", canDelete: false);
            _smaFilter = (Aindicator)_tab.CreateCandleIndicator(_smaFilter, nameArea: "Prime");
            _smaFilter.DataSeries[0].Color = Color.Azure;
            _smaFilter.ParametersDigit[0].Value = SmaLengthFilter.ValueInt;
            _smaFilter.Save();

            _Sma = IndicatorsFactory.CreateIndicatorByName(nameClass: "Sma", name: name + "Sma", canDelete: false);
            _Sma = (Aindicator)_tab.CreateCandleIndicator(_Sma, nameArea: "Prime");
            _Sma.ParametersDigit[0].Value = _periodSma.ValueInt;
            _Sma.DataSeries[0].Color = Color.Green;
            _Sma.Save();

            _hma = IndicatorsFactory.CreateIndicatorByName("HMA_indicator", name: name + "HMA", canDelete: false);
            _hma = (Aindicator)_tab.CreateCandleIndicator(_hma, nameArea: "Prime");
            _hma.ParametersDigit[0].Value = _periodHma.ValueInt;
            _hma.DataSeries[0].Color = Color.Red;
            _hma.Save();

            _hma2 = IndicatorsFactory.CreateIndicatorByName("HMA_indicator", name: name + "HMA2", canDelete: false);
            _hma2 = (Aindicator)_tab.CreateCandleIndicator(_hma2, nameArea: "Prime");
            _hma2.ParametersDigit[0].Value = _periodHma2.ValueInt;
            _hma2.DataSeries[0].Color = Color.Blue;
            _hma2.Save();

            _atr = IndicatorsFactory.CreateIndicatorByName(nameClass: "ATR", name: name + "ATR", canDelete: false);
            _atr = (Aindicator)_tab.CreateCandleIndicator(_atr, nameArea: "New1");
            _atr.ParametersDigit[0].Value = _periodAtr.ValueInt;
            _atr.Save();

            StopOrActivateIndicators();
            _tab.CandleFinishedEvent += _tab_CandleFinishedEvent;
            ParametrsChangeByUser += LRegBot_ParametrsChangeByUser;
            LRegBot_ParametrsChangeByUser();

            _tab.PositionOpeningSuccesEvent += _tab_PositionOpeningSuccesEvent;
        }

        private void LRegBot_ParametrsChangeByUser()
        {
            StopOrActivateIndicators();

            _tab.setSaveData(_saveJson.ValueBool);

            if (_Sma.ParametersDigit[0].Value != _periodSma.ValueInt)
            {
                _Sma.ParametersDigit[0].Value = _periodSma.ValueInt;
                _Sma.Reload();
                _Sma.Save();
            }

            if (_hma.ParametersDigit[0].Value != _periodHma.ValueInt)
            {
                _hma.ParametersDigit[0].Value = _periodHma.ValueInt;
                _hma.Reload();
                _hma.Save();
            }

            if (_hma2.ParametersDigit[0].Value != _periodHma2.ValueInt)
            {
                _hma2.ParametersDigit[0].Value = _periodHma2.ValueInt;
                _hma2.Reload();
                _hma2.Save();
            }

            if (_atr.ParametersDigit[0].Value != _periodAtr.ValueInt)
            {
                _atr.ParametersDigit[0].Value = _periodAtr.ValueInt;
                _atr.Reload();
                _atr.Save();
            }

            if (_smaFilter.DataSeries.Count == 0)
            {
                return;
            }
            if (_smaFilter.ParametersDigit[0].Value != SmaLengthFilter.ValueInt)
            {
                _smaFilter.ParametersDigit[0].Value = SmaLengthFilter.ValueInt;
                _smaFilter.Reload();
                _smaFilter.Save();
            }

            if (_smaFilter.DataSeries != null && _smaFilter.DataSeries.Count > 0)
            {
                if (!SmaPositionFilterIsOn.ValueBool && !SmaSlopeFilterIsOn.ValueBool)
                {
                    _smaFilter.DataSeries[0].IsPaint = false;
                }
                else if (SmaPositionFilterIsOn.ValueBool || SmaSlopeFilterIsOn.ValueBool)
                {
                    _smaFilter.DataSeries[0].IsPaint = true;
                }
            }

            // если мы меняли параметры настроек, то пересоздаем объект класса TrailingStop
            //---------------
            if (TrailingStopIsOn.ValueBool)
            {
                _trailingStop = null;
                _trailingStop = new TrailingStop(_tab, TrailingStopTypeOrder.ValueString, ChangeStepStop.ValueDecimal, MinDist.ValueDecimal, QuantityStepsPrices.ValueDecimal, PointOrPercent.ValueString);
            }
            else
            {
                _trailingStop = null;
            }
            //-------------------

            // RSI
            if (_rsi.ParametersDigit[0].Value != _lengthRsi.ValueInt
                    || _rsi.ParametersDigit[1].Value != _oversoldRsi.ValueDecimal
                    || _rsi.ParametersDigit[2].Value != _overboughtRsi.ValueDecimal)
            {
                _rsi.ParametersDigit[0].Value = _lengthRsi.ValueInt;
                _rsi.ParametersDigit[1].Value = _oversoldRsi.ValueDecimal;
                _rsi.ParametersDigit[2].Value = _overboughtRsi.ValueDecimal;

                _rsi.Reload();
                _rsi.Save();
            }

            if (_rsi.DataSeries != null && _rsi.DataSeries.Count > 0)
            {
                if (!_rsiFilterIsOn.ValueBool)
                {
                    _rsi.DataSeries[0].IsPaint = false;
                    _rsi.DataSeries[1].IsPaint = false;
                    _rsi.DataSeries[2].IsPaint = false;
                }
                else
                {
                    _rsi.DataSeries[0].IsPaint = true;
                    _rsi.DataSeries[1].IsPaint = _drawRsiChannel.ValueBool;
                    _rsi.DataSeries[2].IsPaint = _drawRsiChannel.ValueBool;
                }
            }
            // RSI
        }

        private void StopOrActivateIndicators()
        {
            // RSI
            if (_rsiFilterIsOn.ValueBool == false)
            {
                _rsi.IsOn = false;
                _rsi.Reload();
            }
            else
            {
                _rsi.IsOn = true;
                _rsi.Reload();
            }
            // RSI

            if (SmaPositionFilterIsOn.ValueBool == false
                      && SmaSlopeFilterIsOn.ValueBool == false
                      && _smaFilter.IsOn == true)
            {
                _smaFilter.IsOn = false;
                _smaFilter.Reload();
            }
            else if ((SmaPositionFilterIsOn.ValueBool == true
                || SmaSlopeFilterIsOn.ValueBool == true)
                && _smaFilter.IsOn == false)
            {
                _smaFilter.IsOn = true;
                _smaFilter.Reload();
            }
        }

        private void _tab_PositionOpeningSuccesEvent(Position obj)
        {
            // этот код для того, чтобы стоп открывался в тот же момент, когда окрывается ордер
            // если включен режим трейлинг стопа, то обращаемся к методу SetTrailingStop и передаем в него цену закрытия последней свечи
            //-----------------------------------------
            if (TrailingStopIsOn.ValueBool)
            {
                _trailingStop.SetTrailingStop(obj.EntryPrice);
                return;
            }
            //--------------------------------------
        }

        public override string GetNameStrategyType()
        {
            return "TrailingImpulseHmaRsi";
        }

        public override void ShowIndividualSettingsDialog()
        {

        }

        // Logic
        private void _tab_CandleFinishedEvent(List<Candle> candles)
        {
            if (Regime.ValueString == "Off") { return; }

            if (TimeStart.Value > _tab.TimeServerCurrent ||
                TimeEnd.Value < _tab.TimeServerCurrent)
            {
                CancelStopsAndProfits();
                return;
            }

            if (_tab.CandlesAll == null) { return; }
            if (_periodSma.ValueInt + 10 > candles.Count || _periodAtr.ValueInt > candles.Count) { return; }
            if (_periodHma.ValueInt + 30 > candles.Count || _periodHma2.ValueInt + 10 > candles.Count) { return; }

            if (SmaLengthFilter.ValueInt + 10 >= candles.Count)
            {
                return;
            }

            List<Position> positions = _tab.PositionsOpenAll;

            _lastPrice = candles[candles.Count - 1].Close;
            _lastSma = _Sma.DataSeries[0].Last;
            _prewSma = _Sma.DataSeries[0].Values[candles.Count - 2];
            _prew2Sma = _Sma.DataSeries[0].Values[candles.Count - 3];
            _lastHma = _hma.DataSeries[0].Last;
            _prewHma = _hma.DataSeries[0].Values[candles.Count - 2];
            _prew2Hma = _hma.DataSeries[0].Values[candles.Count - 3];
            _lastFHma = _hma.DataSeries[1].Last;
            _lastHma2 = _hma2.DataSeries[0].Last;
            _prewHma2 = _hma2.DataSeries[0].Values[candles.Count - 2];
            _prew2Hma2 = _hma2.DataSeries[0].Values[candles.Count - 3];
            _lastFHma2 = _hma2.DataSeries[1].Last;
            _lastAtr = _atr.DataSeries[0].Last;
            decimal _slippage = 0;

            if (positions.Count == 0 && Regime.ValueString != "OnlyClosePosition")
            {// enter logic
                if (_orderType.ValueString == "Stop")
                {
                    if (!BuySignalIsFiltered(candles))
                    {
                        _slippage = Slippage.ValueDecimal * (_lastHma + _lastAtr * _multiplerAtr.ValueDecimal) / 100;
                        _tab.BuyAtStop(GetVolume(), (_lastHma + _lastAtr * _multiplerAtr.ValueDecimal) + _slippage, _lastHma + _lastAtr * _multiplerAtr.ValueDecimal, StopActivateType.HigherOrEqual, 1);
                    }
                    if (!SellSignalIsFiltered(candles))
                    {
                        _slippage = Slippage.ValueDecimal * (_lastHma - _lastAtr * _multiplerAtr.ValueDecimal) / 100;
                        _tab.SellAtStop(GetVolume(), (_lastHma - _lastAtr * _multiplerAtr.ValueDecimal) - _slippage, _lastHma - _lastAtr * _multiplerAtr.ValueDecimal, StopActivateType.LowerOrEqyal, 1);
                    }

                    if (BuySignalIsFiltered(candles) || checkMaLongConstraints())
                    {
                        _tab.BuyAtStopCancel();
                    }
                    if (SellSignalIsFiltered(candles) || checkMaShortConstraints())
                    {
                        _tab.SellAtStopCancel();
                    }
                }
                else if (_orderType.ValueString == "Market")
                {
                    if (!BuySignalIsFiltered(candles))
                    {
                        _tab.BuyAtMarket(GetVolume());
                    }
                    if (!SellSignalIsFiltered(candles))
                    {
                        _tab.SellAtMarket(GetVolume());
                    }
                }
                else
                {
                    if (!BuySignalIsFiltered(candles) && !checkMaLongConstraints())
                    {
                        _tab.BuyAtMarket(GetVolume());
                    }
                    if (!SellSignalIsFiltered(candles) && !checkMaShortConstraints())
                    {
                        _tab.SellAtMarket(GetVolume());
                    }
                }

            }
            else
            {//exit logic
                for (int i = 0; i < positions.Count; i++)
                {
                    if (positions[i].State == PositionStateType.ClosingFail)
                    {
                        _tab.CloseAtMarket(positions[i], positions[i].OpenVolume);
                        continue;
                    }
                    if (positions[i].State != PositionStateType.Open)
                    {
                        continue;
                    }

                    decimal stop_level = 0;

                    if (positions[i].Direction == Side.Buy)
                    {// logic to close long position

                        stop_level = _lastHma < _lastHma2 ? _lastHma - _lastAtr * _multiplerAtr.ValueDecimal : _lastHma2 > _lastSma ? _lastHma2 - _lastAtr * _multiplerAtr.ValueDecimal : _lastSma;
                        _slippage = Slippage.ValueDecimal * stop_level / 100;
                        _tab.CloseAtTrailingStop(positions[i], stop_level, stop_level - _slippage);
                    }
                    else if (positions[i].Direction == Side.Sell)
                    {//logic to close short position

                        stop_level = _lastHma > _lastHma2 ? _lastHma + _lastAtr * _multiplerAtr.ValueDecimal : _lastHma2 < _lastSma ? _lastHma2 + _lastAtr * _multiplerAtr.ValueDecimal : _lastSma;
                        _slippage = Slippage.ValueDecimal * stop_level / 100;
                        _tab.CloseAtTrailingStop(positions[i], stop_level, stop_level + _slippage);
                    }
                }
            }
        }

        private bool checkMaLongConstraints()
        {
            // the younger HMA grows more slowly than the older HMA
            if (_lastPrice < _lastSma && Math.Abs(_lastHma - _prewHma) < Math.Abs(_lastHma2 - _prewHma2))
            {
                return true;
            }
            // the younger HMA grows more slowly than the older HMA
            if (_lastPrice < _lastSma && Math.Abs(_prewHma - _prew2Hma) < Math.Abs(_prewHma2 - _prew2Hma2))
            {
                return true;
            }
            // SMA decreases and picks up speed
            if (_prewSma > _lastSma && Math.Abs(_prewSma - _lastSma) > Math.Abs(_prew2Sma - _prewSma))
            {
                return true;
            }
            // closing below the fast HMA, which 'grows' worse than the slow HMA
            if (_lastPrice < _lastHma && Math.Abs(_lastHma - _prewHma) < Math.Abs(_lastHma2 - _prewHma2))
            {
                return true;
            }
            // Junior HMA is below SMA and slowing down
            if (_lastHma < _lastSma && Math.Abs(_lastHma - _prewHma) < Math.Abs(_lastHma2 - _prewHma2))
            {
                return true;
            }
            if (_lastHma < _prewHma)
            {
                return true;
            }
            if (_lastFHma < _lastHma)
            {
                return true;
            }
            if (_lastHma2 < _lastSma)
            {
                return true;
            }
            if (_lastHma2 < _prewHma2)
            {
                return true;
            }
            if (_lastFHma2 < _lastHma2)
            {
                return true;
            }
            // The junior HMA is lower than the senior HMA and the junior HMA is slower than the SMA
            if (_lastHma < _lastHma2 && Math.Abs(_lastHma - _prewHma) < Math.Abs(_lastSma - _prewSma))
            {
                return true;
            }
            // The junior HMA is lower than the senior HMA and the senior HMA is slower than the SMA
            if (_lastHma < _lastHma2 && Math.Abs(_lastHma2 - _prewHma2) < Math.Abs(_lastSma - _prewSma))
            {
                return true;
            }
            // The junior HMA is lower than the senior HMA and the junior HMA is slower than the senior HMA
            if (_lastHma < _lastHma2 && Math.Abs(_lastHma - _prewHma) < Math.Abs(_lastHma2 - _prewHma2))
            {
                return true;
            }

            return false;
        }

        private bool checkMaShortConstraints()
        {
            // the younger HMA decreases more slowly than the older HMA
            if (_lastPrice > _lastSma && Math.Abs(_lastHma - _prewHma) < Math.Abs(_lastHma2 - _prewHma2))
            {
                return true;
            }
            // the younger HMA decreases more slowly than the older HMA
            if (_lastPrice > _lastSma && Math.Abs(_prewHma - _prew2Hma) < Math.Abs(_prewHma2 - _prew2Hma2))
            {
                return true;
            }
            // SMA is growing and picking up speed
            if (_prewSma < _lastSma && Math.Abs(_lastSma - _prewSma) > Math.Abs(_prewSma - _prew2Sma))
            {
                return true;
            }
            // closing above the fast HMA, which 'grows' worse than the slow HMA
            if (_lastPrice > _lastHma && Math.Abs(_lastHma - _prewHma) < Math.Abs(_lastHma2 - _prewHma2))
            {
                return true;
            }
            // Junior HMA is above SMA and slowing down
            if (_lastHma > _lastSma && Math.Abs(_lastHma - _prewHma) < Math.Abs(_lastHma2 - _prewHma2))
            {
                return true;
            }
            if (_lastHma > _prewHma)
            {
                return true;
            }
            if (_lastFHma > _lastHma)
            {
                return true;
            }
            if (_lastHma2 > _lastSma)
            {
                return true;
            }
            if (_lastHma2 > _prewHma2)
            {
                return true;
            }
            if (_lastFHma2 > _lastHma2)
            {
                return true;
            }
            // The junior HMA is higher than the senior HMA and the junior HMA is slower than the SMA
            if (_lastHma > _lastHma2 && Math.Abs(_lastHma - _prewHma) < Math.Abs(_lastSma - _prewSma))
            {
                return true;
            }
            // The junior HMA is higher than the senior HMA and the senior HMA is slower than the SMA
            if (_lastHma > _lastHma2 && Math.Abs(_lastHma2 - _prewHma2) < Math.Abs(_lastSma - _prewSma))
            {
                return true;
            }
            // The younger HMA is higher than the older HMA and the younger HMA is slower than the older HMA
            if (_lastHma > _lastHma2 && Math.Abs(_lastHma - _prewHma) < Math.Abs(_lastHma2 - _prewHma2))
            {
                return true;
            }

            return false;
        }

        private void CancelStopsAndProfits()
        {
            List<Position> positions = _tab.PositionsOpenAll;

            for (int i = 0; i < positions.Count; i++)
            {
                Position pos = positions[i];

                pos.StopOrderIsActiv = false;
                pos.ProfitOrderIsActiv = false;
            }

            _tab.BuyAtStopCancel();
            _tab.SellAtStopCancel();
        }

        private bool BuySignalIsFiltered(List<Candle> candles)
        {
            decimal lastPrice = candles[candles.Count - 1].Close;
            decimal lastSma = _smaFilter.DataSeries[0].Last;
            // filter for buy
            if (Regime.ValueString == "Off" ||
                Regime.ValueString == "OnlyShort" ||
                Regime.ValueString == "OnlyClosePosition")
            {
                return true;
                //if the robot's operating mode does not correspond to the direction of the position
            }

            // RSI
            if (_rsiFilterIsOn.ValueBool)
            {
                decimal lastRsi = _rsi.DataSeries[0].Last;
                if (lastRsi >= _oversoldRsi.ValueDecimal)
                {
                    return true;
                }
            }
            // RSI

            if (SmaPositionFilterIsOn.ValueBool)
            {
                if (_smaFilter.DataSeries[0].Last > lastPrice)
                {
                    return true;
                }
                // if the price is lower than the last Sma - return true to the top
            }
            if (SmaSlopeFilterIsOn.ValueBool)
            {
                decimal prevSma = _smaFilter.DataSeries[0].Values[_smaFilter.DataSeries[0].Values.Count - 2];

                if (lastSma < prevSma)
                {
                    return true;
                }
                // if the last Sma is lower than the previous Sma - return true to the top
            }

            return false;
        }

        private bool SellSignalIsFiltered(List<Candle> candles)
        {
            decimal lastPrice = candles[candles.Count - 1].Close;
            decimal lastSma = _smaFilter.DataSeries[0].Last;
            // filter for sell
            if (Regime.ValueString == "Off" ||
                Regime.ValueString == "OnlyLong" ||
                Regime.ValueString == "OnlyClosePosition")
            {
                return true;
                //if the robot's operating mode does not correspond to the direction of the position
            }

            // RSI
            if (_rsiFilterIsOn.ValueBool)
            {
                decimal lastRsi = _rsi.DataSeries[0].Last;
                if (lastRsi <= _overboughtRsi.ValueDecimal)
                {
                    return true;
                }
            }
            // RSI

            if (SmaPositionFilterIsOn.ValueBool)
            {
                if (lastSma < lastPrice)
                {
                    return true;
                }
                // if the price is higher than the last Sma - return true to the top
            }
            if (SmaSlopeFilterIsOn.ValueBool)
            {
                decimal prevSma = _smaFilter.DataSeries[0].Values[_smaFilter.DataSeries[0].Values.Count - 2];

                if (lastSma > prevSma)
                {
                    return true;
                }
                // if the last Sma is higher than the previous Sma - return true to the top
            }

            return false;
        }

        private decimal GetVolume()
        {
            decimal volume = 0;

            if (VolumeRegime.ValueString == "Contract currency")
            {
                decimal contractPrice = TabsSimple[0].PriceBestAsk;
                volume = VolumeOnPosition.ValueDecimal / contractPrice;

            }
            else if (VolumeRegime.ValueString == "Number of contracts")
            {
                volume = VolumeOnPosition.ValueDecimal;
            }
            else //if (VolumeRegime.ValueString == "% of the total portfolio")
            {
                volume = _tab.Portfolio.ValueCurrent * (VolumeOnPosition.ValueDecimal / 100) / _tab.PriceBestAsk / _tab.Security.Lot;
            }

            volume = GetRoundedVolume(_tab, volume);

            return volume;
        }
    }
}
