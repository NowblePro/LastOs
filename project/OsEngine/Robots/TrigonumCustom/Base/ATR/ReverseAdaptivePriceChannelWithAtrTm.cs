using System;
using System.Collections.Generic;
using OsEngine.Entity;
using OsEngine.Indicators;
using OsEngine.OsTrader.Panels.Tab;
using OsEngine.OsTrader.Panels;
using OsEngine.OsTrader.Panels.Attributes;

namespace OsEngine.Robots.TrigonumCustom.Base.ATR
{

    [Bot("ReverseAdaptivePriceChannelAtrTm")]
    public class ReverseAdaptivePriceChannelAtrTm : BotPanel
    {
        private BotTabSimple _tab;

        public StrategyParameterString Regime;
        public StrategyParameterDecimal VolumeOnPosition;
        public StrategyParameterString VolumeRegime;
        public StrategyParameterDecimal Slippage;

        private StrategyParameterTimeOfDay TimeStart;
        private StrategyParameterTimeOfDay TimeEnd;

        public Aindicator _APC;
        private StrategyParameterInt AdxPeriod;
        private StrategyParameterInt Ratio;

        public Aindicator _smaFilter;
        private StrategyParameterInt SmaLengthFilter;
        public StrategyParameterBool SmaPositionFilterIsOn;
        public StrategyParameterBool SmaSlopeFilterIsOn;

        private StrategyParameterInt LengthAtr;
        private StrategyParameterDecimal MultiplierAtr;
        private StrategyParameterString AtrRegime;

        Aindicator _ATR;

        private decimal _lastAtr;
        private decimal _averageAtr;
        private decimal _lastCandleClose;
        private bool _needUpdateLastIndex;
        private bool _needUpdateIterator;
        private int _iterator = 1;

        private bool _atrResult = false;

        public ReverseAdaptivePriceChannelAtrTm(string name, StartProgram startProgram)
            : base(name, startProgram)
        {
            TabCreate(BotTabType.Simple);
            _tab = TabsSimple[0];

            Regime = CreateParameter("Regime", "Off", new[] { "Off", "On", "OnlyLong", "OnlyShort", "OnlyClosePosition" }, "Base");
            VolumeRegime = CreateParameter("Volume type", "Number of contracts", new[] { "Number of contracts", "Contract currency", "% of the total portfolio" }, "Base");
            VolumeOnPosition = CreateParameter("Volume", 10, 1.0m, 50, 4, "Base");

            Slippage = CreateParameter("Slippage %", 0m, 0, 20, 1, "Base");

            TimeStart = CreateParameterTimeOfDay("Start Trade Time", 0, 0, 0, 0, "Base");
            TimeEnd = CreateParameterTimeOfDay("End Trade Time", 24, 0, 0, 0, "Base");

            AdxPeriod = CreateParameter("Ronco Period", 14, 2, 300, 12, "Robot parameters");
            Ratio = CreateParameter("Ratio", 100, 50, 300, 10, "Robot parameters");

            SmaLengthFilter = CreateParameter("Sma Length Filter", 100, 10, 500, 1, "Filters");

            SmaPositionFilterIsOn = CreateParameter("Is SMA Filter On", false, "Filters");
            SmaSlopeFilterIsOn = CreateParameter("Is Sma Slope Filter On", false, "Filters");

            LengthAtr = CreateParameter("Length ATR", 96, 7, 1000, 1, "Indicator");
            MultiplierAtr = CreateParameter("Multiplier Atr", 1, 1m, 10, 1, "Indicator");
            AtrRegime = CreateParameter("Atr Regime", "Off", new[] { "Off", "On", "Entry Only", "Exit Only" }, "Indicator");

            _ATR = IndicatorsFactory.CreateIndicatorByName("ATR", name + "Atr", false);
            _ATR = (Aindicator)_tab.CreateCandleIndicator(_ATR, "NewArea");
            ((IndicatorParameterInt)_ATR.Parameters[0]).ValueInt = LengthAtr.ValueInt;
            _ATR.Save();

            _smaFilter = IndicatorsFactory.CreateIndicatorByName(nameClass: "Sma", name: name + "Sma_Filter", canDelete: false);
            _smaFilter = (Aindicator)_tab.CreateCandleIndicator(_smaFilter, nameArea: "Prime");
            _smaFilter.DataSeries[0].Color = System.Drawing.Color.Azure;
            _smaFilter.ParametersDigit[0].Value = SmaLengthFilter.ValueInt;
            _smaFilter.Save();

            _APC = IndicatorsFactory.CreateIndicatorByName("AdaptivePriceChannel_Indicator", name + "APC", false);
            _APC = (Aindicator)_tab.CreateCandleIndicator(_APC, "Prime");
            _APC.ParametersDigit[0].Value = AdxPeriod.ValueInt;
            _APC.ParametersDigit[1].Value = Ratio.ValueInt;
            _APC.Save();

            StopOrActivateIndicators();
            ParametrsChangeByUser += RoncoParam_ParametrsChangeByUser;
            _tab.CandleFinishedEvent += _tab_CandleFinishedEvent;
            RoncoParam_ParametrsChangeByUser();
            _tab.PositionOpeningSuccesEvent += _tab_PositionOpeningSuccesEvent;
        }

        private void _tab_PositionOpeningSuccesEvent(Position obj)
        {
            _tab.SellAtStopCancel();
            _tab.BuyAtStopCancel();
        }

        private void RoncoParam_ParametrsChangeByUser()
        {
            StopOrActivateIndicators();

            if (_APC.ParametersDigit[0].Value != AdxPeriod.ValueInt ||
                    _APC.ParametersDigit[1].Value != Ratio.ValueInt)
            {
                _APC.ParametersDigit[0].Value = AdxPeriod.ValueInt;
                _APC.ParametersDigit[1].Value = Ratio.ValueInt;
                _APC.Save();
                _APC.Reload();
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
            ////////////////////////
            ((IndicatorParameterInt)_ATR.Parameters[0]).ValueInt = LengthAtr.ValueInt;
            _ATR.Save();
            _ATR.Reload();
            ////////////////////////
        }

        private void StopOrActivateIndicators()
        {
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
            return "ReverseAdaptivePriceChannelAtrTm";
        }

        public override void ShowIndividualSettingsDialog()
        {

        }

        // Logic
        private void _tab_CandleFinishedEvent(List<Candle> candles)
        {
            decimal lastCandle = candles[candles.Count - 1].Close;

            if (AtrRegime.ValueString != "Off")
            {
                _atrResult = AtrLogic(candles, lastCandle);
            }

            if (TimeStart.Value > _tab.TimeServerCurrent ||
                TimeEnd.Value < _tab.TimeServerCurrent)
            {
                CancelStopsAndProfits();
                return;
            }

            if (candles.Count < AdxPeriod.ValueInt + 10 ||
                candles.Count < 50)
            {
                return;
            }

            decimal upChannel = _APC.DataSeries[0].Last;
            decimal downChannel = _APC.DataSeries[1].Last;

            if (upChannel == 0 || downChannel == 0)
            {
                return;
            }

            List<Position> positions = _tab.PositionsOpenAll;

            if (positions.Count == 0)
            {
                ////////////////////////
                if (AtrRegime.ValueString == "On" || AtrRegime.ValueString == "Entry Only")
                {
                    if (_atrResult) return;
                }
                ////////////////////////

                if (BuySignalIsFiltered(candles) == false)
                {
                    decimal _slippage = Slippage.ValueDecimal * upChannel / 100;
                    _tab.BuyAtStopCancel();
                    _tab.BuyAtStop(GetVolume(), upChannel + _tab.Securiti.PriceStep + _slippage, upChannel + _tab.Securiti.PriceStep,
                        StopActivateType.HigherOrEqual);
                }
                if (SellSignalIsFiltered(candles) == false)
                {
                    decimal _slippage = Slippage.ValueDecimal * downChannel / 100;
                    _tab.SellAtStopCancel();
                    _tab.SellAtStop(GetVolume(), downChannel - _tab.Securiti.PriceStep - _slippage, downChannel - _tab.Securiti.PriceStep,
                        StopActivateType.LowerOrEqyal);
                }
            }
            else
            {
                ////////////////////////
                if (AtrRegime.ValueString == "On" || AtrRegime.ValueString == "Exit Only")
                {
                    if (_atrResult) return;
                }
                ////////////////////////

                _tab.SellAtStopCancel();
                _tab.BuyAtStopCancel();
                Position pos = positions[0];

                if (positions.Count > 1)
                {

                }

                if (pos.CloseActiv == true)
                {
                    return;
                }

                if (pos.Direction == Side.Buy)
                {
                    decimal priceLine = downChannel - _tab.Securiti.PriceStep;
                    decimal priceOrder = downChannel - _tab.Securiti.PriceStep;
                    decimal _slippage = Slippage.ValueDecimal * priceOrder / 100;

                    if (SellSignalIsFiltered(candles) == false)
                    {
                        _tab.SellAtStopCancel();
                        _tab.SellAtStop(GetVolume(), priceOrder - _slippage, priceLine, StopActivateType.LowerOrEqyal);
                    }

                    _tab.CloseAtStop(pos, priceLine, priceOrder - _slippage);
                }
                else if (pos.Direction == Side.Sell)
                {
                    decimal priceLine = upChannel + _tab.Securiti.PriceStep;
                    decimal priceOrder = upChannel + _tab.Securiti.PriceStep;
                    decimal _slippage = Slippage.ValueDecimal * priceOrder / 100;

                    if (BuySignalIsFiltered(candles) == false)
                    {
                        _tab.BuyAtStopCancel();
                        _tab.BuyAtStop(GetVolume(), priceOrder + _slippage, priceLine, StopActivateType.HigherOrEqual);
                    }
                    _tab.CloseAtStop(pos, priceLine, priceOrder + _slippage);
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
                volume = _tab.Portfolio.ValueCurrent * (VolumeOnPosition.ValueDecimal / 100) / _tab.PriceBestAsk / _tab.Securiti.Lot;
            }

            // If the robot is running in the tester
            if (StartProgram == StartProgram.IsTester)
            {
                volume = Math.Round(volume, 6);
            }
            else
            {
                volume = Math.Round(volume, _tab.Securiti.DecimalsVolume);
            }

            return volume;
        }

        private bool AtrLogic(List<Candle> candles, decimal lastCandle)
        {
            if (_ATR.DataSeries[0].Last == 0 && _needUpdateIterator)
            {
                _lastCandleClose = 0;
                _averageAtr = 0;
                _iterator = 1;
                _needUpdateIterator = false;
            }

            if (candles.Count < LengthAtr.ValueInt)
            {
                return true;
            }

            _lastAtr = _ATR.DataSeries[0].Last;

            if (_ATR.DataSeries[0].Values.Count >= LengthAtr.ValueInt * _iterator)
            {
                _lastCandleClose = lastCandle;
                _averageAtr = _lastAtr;
                _iterator++;
                _needUpdateLastIndex = false;
                _needUpdateIterator = true;
            }

            if (_needUpdateLastIndex || Math.Abs(lastCandle - _lastCandleClose) > _averageAtr * MultiplierAtr.ValueDecimal)
            {
                if (_tab.PositionsOpenAll.Count > 0)
                {
                    CancelStopsAndProfits();
                }
                _needUpdateLastIndex = true;
                return true;
            }

            return false;
        }
    }

}