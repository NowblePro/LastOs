using System;
using System.Collections.Generic;
using OsEngine.Entity;
using OsEngine.Indicators;
using OsEngine.OsTrader.Panels;
using OsEngine.OsTrader.Panels.Attributes;
using OsEngine.OsTrader.Panels.Tab;

namespace OsEngine.Robots.TrigonumCustom.Channel
{
    [Bot("BreakNadarayaWatsonReverseRsi")]
    internal class BreakNadarayaWatsonReverseRsi : BotPanel
    {
        private BotTabSimple _tab;

        // Basic Settings
        private StrategyParameterString _regime;
        private StrategyParameterBool _reverseLogic;
        private StrategyParameterString _volumeRegime;
        private StrategyParameterDecimal _volumeOnPosition;
        private StrategyParameterDecimal _slippage;
        private StrategyParameterTimeOfDay _startTradeTime;
        private StrategyParameterTimeOfDay _endTradeTime;

        private StrategyParameterBool _saveJson;

        // Indicator setting 
        private StrategyParameterInt _NWLength;
        private StrategyParameterDecimal _NWMultiplier;
        private StrategyParameterString _NWKernel;
        private StrategyParameterDecimal _NWKernelBandwidth;

        // Indicator
        private Aindicator _NW;

        // The last value of the indicator
        private decimal _lastEstimate;
        private decimal _lastUpLine;
        private decimal _lastDownLine;

        // RSI
        private Aindicator _rsi;
        private StrategyParameterInt _lengthRsi;
        private StrategyParameterDecimal _oversoldRsi;
        private StrategyParameterDecimal _overboughtRsi;
        private StrategyParameterBool _drawRsiChannel;
        private StrategyParameterBool _rsiFilterIsOn;
        // RSI

        public BreakNadarayaWatsonReverseRsi(string name, StartProgram startProgram) : base(name, startProgram)
        {
            TabCreate(BotTabType.Simple);
            _tab = TabsSimple[0];

            // Basic setting
            _regime = CreateParameter("Regime", "Off", new[] { "Off", "On", "OnlyLong", "OnlyShort", "OnlyClosePosition" }, "Base");
            _reverseLogic = CreateParameter("Reverse logic", true, "Base");
            _volumeRegime = CreateParameter("Volume type", "Number of contracts", new[] { "Number of contracts", "Contract currency", "% of the total portfolio" }, "Base");
            _volumeOnPosition = CreateParameter("Volume", 1, 1.0m, 50, 4, "Base");
            _slippage = CreateParameter("Slippage %", 0m, 0, 20, 1, "Base");

            _saveJson = CreateParameter("Save Json Data", false, "Base");

            _startTradeTime = CreateParameterTimeOfDay("Start Trade Time", 0, 0, 0, 0, "Base");
            _endTradeTime = CreateParameterTimeOfDay("End Trade Time", 24, 0, 0, 0, "Base");

            // Indicator setting
            _NWLength = CreateParameter("Nadaraya-Watson Length", 14, 7, 48, 7, "Indicator");
            _NWMultiplier = CreateParameter("Nadaraya-Watson Multiplier", 1.0m, 1, 5, 0.2m, "Indicator");
            _NWKernel = CreateParameter("Nadaraya-Watson Kernel", "Gaussian", new[] { "Gaussian","Epanechnikov","Uniform","Triangular" }, "Indicator");
            _NWKernelBandwidth = CreateParameter("Kernel Bandwidth", 1.0m, 0.5m, 20m, 0.5m, "Indicator");

            // RSI
            _rsiFilterIsOn = CreateParameter("Is RSI Filter On", true, "Filters");
            _lengthRsi = CreateParameter("Rsi Length", 14, 7, 33, 1, "Filters");
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

            // Create indicator NW
            _NW = IndicatorsFactory.CreateIndicatorByName("NadarayaWatson", name + "NadarayaWatson", false);
            _NW = (Aindicator)_tab.CreateCandleIndicator(_NW, "Prime");
            ((IndicatorParameterInt)_NW.Parameters[0]).ValueInt = _NWLength.ValueInt;
            ((IndicatorParameterDecimal)_NW.Parameters[1]).ValueDecimal = _NWMultiplier.ValueDecimal;
            ((IndicatorParameterString)_NW.Parameters[2]).ValueString = _NWKernel.ValueString;
            ((IndicatorParameterDecimal)_NW.Parameters[3]).ValueDecimal = _NWKernelBandwidth.ValueDecimal;

            _NW.Save();

            // Subscribe to the indicator update event
            ParametrsChangeByUser += NW_ParametersChangeByUser;
            // Subscribe to the candle finished event
            _tab.CandleFinishedEvent += _tab_CandleFinishedEvent;

            NW_ParametersChangeByUser();
        }

        private void NW_ParametersChangeByUser()
        {
            StopOrActivateIndicators();

            _tab.setSaveData(_saveJson.ValueBool);

            ((IndicatorParameterInt)_NW.Parameters[0]).ValueInt = _NWLength.ValueInt;
            ((IndicatorParameterDecimal)_NW.Parameters[1]).ValueDecimal = _NWMultiplier.ValueDecimal;
            ((IndicatorParameterString)_NW.Parameters[2]).ValueString = _NWKernel.ValueString;
            ((IndicatorParameterDecimal)_NW.Parameters[3]).ValueDecimal = _NWKernelBandwidth.ValueDecimal;
            _NW.Save();
            _NW.Reload();

            // RSI
            if (_rsi.ParametersDigit[0].Value != _lengthRsi.ValueInt
                    || _rsi.ParametersDigit[1].Value != _oversoldRsi.ValueDecimal
                    || _rsi.ParametersDigit[2].Value != _overboughtRsi.ValueDecimal)
            {
                _rsi.ParametersDigit[0].Value = _lengthRsi.ValueInt;
                _rsi.ParametersDigit[1].Value = _oversoldRsi.ValueDecimal;
                _rsi.ParametersDigit[2].Value = _overboughtRsi.ValueDecimal;

                _rsi.Save();
                _rsi.Reload();
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
        }

        // The name of the robot in OsEngine
        public override string GetNameStrategyType()
        {
            return "BreakNadarayaWatsonReverseRsi";
        }
        public override void ShowIndividualSettingsDialog()
        {

        }

        // Candle Finished Event
        private void _tab_CandleFinishedEvent(List<Candle> candles)
        {
            // If the robot is turned off, exit the event handler
            if (_regime.ValueString == "Off")
            {
                return;
            }

            // If there are not enough candles to build an indicator, we exit
            if (candles.Count < _NWLength.ValueInt)
            {
                return;
            }

            // If the time does not match, we leave
            if (_startTradeTime.Value > _tab.TimeServerCurrent ||
                _endTradeTime.Value < _tab.TimeServerCurrent)
            {
                return;
            }

            List<Position> openPositions = _tab.PositionsOpenAll;

            // If there are positions, then go to the position closing method
            if (openPositions != null && openPositions.Count != 0)
            {
                LogicClosePosition(candles);
            }

            // If the position closing mode, then exit the method
            if (_regime.ValueString == "OnlyClosePosition")
            {
                return;
            }
            // If there are no positions, then go to the position opening method
            if (openPositions == null || openPositions.Count == 0)
            {
                LogicOpenPosition(candles);
            }
        }

        // Opening logic
        private void LogicOpenPosition(List<Candle> candles)
        {
            // The last value of the indicator
            _lastEstimate = _NW.DataSeries[0].Last;
            _lastUpLine = _NW.DataSeries[1].Last;
            _lastDownLine = _NW.DataSeries[2].Last;

            List<Position> openPositions = _tab.PositionsOpenAll;

            if (openPositions == null || openPositions.Count == 0)
            {
                decimal lastPrice = candles[candles.Count - 1].Close;

                // Slippage
                decimal slippage = _slippage.ValueDecimal * _tab.Security.PriceStep;

                // Long
                if (_regime.ValueString != "OnlyShort") // If the mode is not only short, then we enter long
                {
                    if (_reverseLogic.ValueBool)
                    {
                        if (lastPrice < _lastDownLine)
                        {
                            if (!BuySignalIsFiltered(candles))
                                _tab.BuyAtLimit(GetVolume(), _tab.PriceBestAsk + slippage);
                        }
                    }
                    else
                    {
                        if (lastPrice > _lastUpLine)
                        {
                            if (!BuySignalIsFiltered(candles))
                                _tab.BuyAtLimit(GetVolume(), _tab.PriceBestAsk + slippage);
                        }
                    }
                }

                // Short
                if (_regime.ValueString != "OnlyLong") // If the mode is not only long, then we enter short
                {
                    if (_reverseLogic.ValueBool)
                    {
                        if (lastPrice > _lastUpLine)
                        {
                            if (!SellSignalIsFiltered(candles))
                                _tab.SellAtLimit(GetVolume(), _tab.PriceBestBid - slippage);
                        }
                    }
                    else
                    {
                        if (lastPrice < _lastDownLine)
                        {
                            if (!SellSignalIsFiltered(candles))
                                _tab.SellAtLimit(GetVolume(), _tab.PriceBestBid - slippage);
                        }
                    }
                }
            }
        }

        // Logic close position
        private void LogicClosePosition(List<Candle> candles)
        {
            List<Position> openPositions = _tab.PositionsOpenAll;

            // The last value of the indicator
            _lastEstimate = _NW.DataSeries[0].Last;
            _lastUpLine = _NW.DataSeries[1].Last;
            _lastDownLine = _NW.DataSeries[2].Last;

            decimal slippage = _slippage.ValueDecimal * _tab.Security.PriceStep;

            decimal lastPrice = candles[candles.Count - 1].Close;

            for (int i = 0; openPositions != null && i < openPositions.Count; i++)
            {
                Position pos = openPositions[i];

                if (pos.State != PositionStateType.Open)
                {
                    continue;
                }

                if (pos.Direction == Side.Buy) // If the direction of the position is purchase
                {
                    if (_reverseLogic.ValueBool)
                    {
                        if (lastPrice > _lastEstimate)
                        {
                            _tab.CloseAtLimit(pos, lastPrice + slippage, pos.OpenVolume);
                        }
                    }
                    else
                    {
                        if (lastPrice < _lastDownLine)
                        {
                            _tab.CloseAtLimit(pos, lastPrice - slippage, pos.OpenVolume);
                        }
                    }
                }
                else // If the direction of the position is sale
                {
                    if (_reverseLogic.ValueBool)
                    {
                        if (lastPrice < _lastEstimate)
                        {
                            _tab.CloseAtLimit(pos, lastPrice - slippage, pos.OpenVolume);
                        }
                    }
                    else
                    {
                        if (lastPrice > _lastUpLine)
                        {
                            _tab.CloseAtLimit(pos, lastPrice + slippage, pos.OpenVolume);
                        }
                    }
                }
            }
        }

        private bool BuySignalIsFiltered(List<Candle> candles)
        {
            decimal lastPrice = candles[candles.Count - 1].Close;
            // filter for buy
            if (_regime.ValueString == "Off" ||
                _regime.ValueString == "OnlyShort" ||
                _regime.ValueString == "OnlyClosePosition")
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

            return false;
        }

        private bool SellSignalIsFiltered(List<Candle> candles)
        {
            decimal lastPrice = candles[candles.Count - 1].Close;
            // filter for sell
            if (_regime.ValueString == "Off" ||
                _regime.ValueString == "OnlyLong" ||
                _regime.ValueString == "OnlyClosePosition")
            {
                return true;
                // if the robot's operating mode does not correspond to the direction of the position
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

            return false;
        }

        // Method for calculating the volume of entry into a position
        private decimal GetVolume()
        {
            decimal volume = 0;

            if (_volumeRegime.ValueString == "Contract currency")
            {
                decimal contractPrice = TabsSimple[0].PriceBestAsk;
                volume = _volumeOnPosition.ValueDecimal / contractPrice;

            }
            else if (_volumeRegime.ValueString == "Number of contracts")
            {
                volume = _volumeOnPosition.ValueDecimal;
            }
            else // if (VolumeRegime.ValueString == "% of the total portfolio")
            {
                volume = _tab.Portfolio.ValueCurrent * (_volumeOnPosition.ValueDecimal / 100) / _tab.PriceBestAsk / _tab.Security.Lot;
            }

            volume = GetRoundedVolume(_tab, volume);

            return volume;
        }
    }
}
