using System;
using System.Collections.Generic;
using OsEngine.Entity;
using OsEngine.Indicators;
using OsEngine.OsTrader.Panels.Tab;
using OsEngine.OsTrader.Panels;
using OsEngine.OsTrader.Panels.Attributes;
using OsEngine.Robots.Classes;

namespace OsEngine.Robots.TrigonumCustom.Trailing
{
    [Bot("TrailingParabolicSarClassicTradeRsi")]
    public class TrailingParabolicSarClassicTradeRsi : BotPanel
    {
        private BotTabSimple _tab;

        public StrategyParameterString Regime;
        public StrategyParameterDecimal VolumeOnPosition;
        public StrategyParameterString VolumeRegime;
        public StrategyParameterDecimal Slippage;

        private StrategyParameterTimeOfDay TimeStart;
        private StrategyParameterTimeOfDay TimeEnd;

        private StrategyParameterBool _saveJson;

        public Aindicator _PS;
        private StrategyParameterDecimal _Step;
        private StrategyParameterDecimal _MaxStep;

        public Aindicator _smaFilter;
        private StrategyParameterInt SmaLengthFilter;
        public StrategyParameterBool SmaPositionFilterIsOn;
        public StrategyParameterBool SmaSlopeFilterIsOn;

        private decimal _lastPrice;
        private decimal _lastSar;

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

        public StrategyParameterBool _fixTpIsOn;
        public StrategyParameterDecimal _fixTpPercent;

        public TrailingParabolicSarClassicTradeRsi(string name, StartProgram startProgram) : base(name, startProgram)
        {
            TabCreate(BotTabType.Simple);
            _tab = TabsSimple[0];

            Regime = CreateParameter("Regime", "Off", new[] { "Off", "On", "OnlyLong", "OnlyShort", "OnlyClosePosition" }, "Base");
            VolumeRegime = CreateParameter("Volume type", "Number of contracts", new[] { "Number of contracts", "Contract currency", "% of the total portfolio" }, "Base");
            VolumeOnPosition = CreateParameter("Volume", 10, 1.0m, 50, 4, "Base");

            Slippage = CreateParameter("Slippage %", 0m, 0, 20, 1, "Base");

            _saveJson = CreateParameter("Save Json Data", false, "Base");

            TimeStart = CreateParameterTimeOfDay("Start Trade Time", 0, 0, 0, 0, "Base");
            TimeEnd = CreateParameterTimeOfDay("End Trade Time", 24, 0, 0, 0, "Base");

            _Step = CreateParameter("Step", 0.02m, 0.001m, 3, 0.001m, "Robot parameters");
            _MaxStep = CreateParameter("MaxStep", 0.2m, 0.01m, 1, 0.01m, "Robot parameters");

            SmaLengthFilter = CreateParameter("Sma Length Filter", 100, 10, 500, 1, "Filters");

            SmaPositionFilterIsOn = CreateParameter("Is SMA Filter On", false, "Filters");
            SmaSlopeFilterIsOn = CreateParameter("Is Sma Slope Filter On", false, "Filters");

            // создаем параметры настроек и создаем объект класса TrailingStop
            //---------------------------------
            TrailingStopIsOn = CreateParameter("Is Trailing stop On", false, "Trailing Stop");
            TrailingStopTypeOrder = CreateParameter("Type order", OrderPriceType.Market.ToString(), new[] { OrderPriceType.Market.ToString(), OrderPriceType.Limit.ToString() }, "Trailing Stop");
            PointOrPercent = CreateParameter("Choise Points or Percent", "Points", new[] { "Points", "Percent" }, "Trailing Stop");
            ChangeStepStop = CreateParameter("Stop level change step", 1, 1, 10000, 001m, "Trailing Stop");
            MinDist = CreateParameter("Minimum distance to price", 1, 1, 10000, 0.01m, "Trailing Stop");
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
            _smaFilter.DataSeries[0].Color = System.Drawing.Color.Azure;
            _smaFilter.ParametersDigit[0].Value = SmaLengthFilter.ValueInt;
            _smaFilter.Save();

            _PS = IndicatorsFactory.CreateIndicatorByName(nameClass: "ParabolicSAR", name: name + "Parabolic", canDelete: false);
            _PS = (Aindicator)_tab.CreateCandleIndicator(_PS, nameArea: "Prime");
            _PS.ParametersDigit[0].Value = _Step.ValueDecimal;
            _PS.ParametersDigit[1].Value = _MaxStep.ValueDecimal;
            _PS.Save();

            _fixTpIsOn = CreateParameter("Fix Take Profit Is On", false, "Fix Take Profit");
            _fixTpPercent = CreateParameter("Fix Take Profit Percent", 1.0m, 0.1m, 2.0m, 0.2m, "Fix Take Profit");

            StopOrActivateIndicators();

            ParametrsChangeByUser += ParabolicSarClassicTrade_ParametrsChangeByUser;

            _tab.CandleFinishedEvent += _tab_CandleFinishedEvent;

            _tab.PositionOpeningSuccesEvent += _tab_PositionOpeningSuccesEvent;

            ParabolicSarClassicTrade_ParametrsChangeByUser();
        }

        private void _tab_PositionOpeningSuccesEvent(Position obj)
        {
            _tab.SellAtStopCancel();
            _tab.BuyAtStopCancel();

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

        private void ParabolicSarClassicTrade_ParametrsChangeByUser()
        {
            StopOrActivateIndicators();

            _tab.setSaveData(_saveJson.ValueBool);

            if (_PS.ParametersDigit[0].Value != _Step.ValueDecimal ||
                _PS.ParametersDigit[1].Value != _MaxStep.ValueDecimal)
            {
                _PS.ParametersDigit[0].Value = _Step.ValueDecimal;
                _PS.ParametersDigit[1].Value = _MaxStep.ValueDecimal;
                _PS.Save();
                _PS.Reload();
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

        public override string GetNameStrategyType()
        {
            return "TrailingParabolicSarClassicTradeRsi";
        }

        public override void ShowIndividualSettingsDialog()
        {

        }

        //Logic
        private void _tab_CandleFinishedEvent(List<Candle> candles)
        {
            if (TimeStart.Value > _tab.TimeServerCurrent ||
            TimeEnd.Value < _tab.TimeServerCurrent)
            {
                CancelStopsAndProfits();
                return;
            }

            if (SmaLengthFilter.ValueInt + 10 >= candles.Count)
            {
                return;
            }

            if (candles.Count < 20)
            {
                return;
            }

            _lastPrice = candles[candles.Count - 1].Close;
            _lastSar = _PS.DataSeries[0].Last;

            if (_lastSar == 0)
            {
                return;
            }

            List<Position> positions = _tab.PositionsOpenAll;

            if (positions.Count == 0)
            {
                if (BuySignalIsFiltered(candles) == false)
                {
                    if (_lastPrice < _lastSar)
                    {
                        decimal _slippage = Slippage.ValueDecimal * _lastSar / 100;
                        _tab.BuyAtStopCancel();
                        _tab.BuyAtStop(GetVolume(), _lastSar + _slippage, _lastSar, StopActivateType.HigherOrEqual, 1);
                    }
                }
                if (SellSignalIsFiltered(candles) == false)
                {
                    if (_lastPrice > _lastSar)
                    {
                        decimal _slippage = Slippage.ValueDecimal * _lastSar / 100;
                        _tab.SellAtStopCancel();
                        _tab.SellAtStop(GetVolume(), _lastSar - _slippage, _lastSar, StopActivateType.LowerOrEqual, 1);
                    }
                }
            }
            else
            {
                // если включен режим трейлинг стопа, то обращаемся к методу SetTrailingStop и передаем в него цену закрытия последней свечи
                //-----------------------------------------
                if (TrailingStopIsOn.ValueBool)
                {
                    _trailingStop.SetTrailingStop(candles[candles.Count - 1].Close);
                    return;
                }
                //--------------------------------------
                for (int i = 0; i < positions.Count; i++)
                {
                    _tab.SellAtStopCancel();
                    _tab.BuyAtStopCancel();
                    Position pos = positions[0];

                    if (_fixTpIsOn.ValueBool)
                    {
                        decimal lastPrice = candles[candles.Count - 1].Close;
                        if (pos.Direction == Side.Buy)
                        {
                            _tab.CloseAtProfitMarket(pos, lastPrice + (lastPrice * _fixTpPercent.ValueDecimal / 100));
                        }
                        else if (pos.Direction == Side.Sell)
                        {
                            _tab.CloseAtProfitMarket(pos, lastPrice - (lastPrice * _fixTpPercent.ValueDecimal / 100));
                        }
                    }

                    if (pos.CloseActiv == true && pos.CloseOrders != null && pos.CloseOrders.Count > 0)
                    {
                        return;
                    }

                    decimal priceLine = _lastSar;
                    decimal priceOrder = _lastSar;
                    decimal _slippage = Slippage.ValueDecimal * priceOrder / 100;

                    if (pos.Direction == Side.Buy)
                    {
                        _tab.CloseAtStop(pos, priceLine, priceOrder - _slippage);
                    }
                    else if (pos.Direction == Side.Sell)
                    {
                        _tab.CloseAtStop(pos, priceLine, priceOrder + _slippage);
                    }
                }
            }
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
                //if the robot's operating mode does not correspond to the direction of the positio
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
