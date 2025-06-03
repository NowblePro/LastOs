using System;
using System.Collections.Generic;
using OsEngine.Entity;
using OsEngine.Indicators;
using OsEngine.OsTrader.Panels.Tab;
using OsEngine.OsTrader.Panels;
using OsEngine.OsTrader.Panels.Attributes;

namespace OsEngine.Robots.TrigonumCustom.Base
{

    [Bot("BreakHighLowByAtrExtTimeRsi")]
    public class BreakHighLowByAtrExtTimeRsi : BotPanel
    {
        private BotTabSimple _tab;

        public StrategyParameterString Regime;
        public StrategyParameterDecimal VolumeOnPosition;
        public StrategyParameterString VolumeRegime;
        public StrategyParameterDecimal Slippage;

        private StrategyParameterTimeOfDay TimeStart;
        private StrategyParameterTimeOfDay TimeEnd;

        public Aindicator _damping;
        public Aindicator avgHigh2;
        public Aindicator avgLow3;
        public Aindicator avgLow2;
        public Aindicator avgHigh3;
        public Aindicator avgHighShifted;
        public Aindicator avgLowShifted;

        private StrategyParameterInt DampingPeriod;
        public StrategyParameterInt ExitCandleCount;

        public Aindicator _smaFilter;
        private StrategyParameterInt SmaLengthFilter;
        public StrategyParameterBool SmaPositionFilterIsOn;
        public StrategyParameterBool SmaSlopeFilterIsOn;

        // RSI
        Aindicator _rsi;
        public StrategyParameterInt _lengthRsi;
        public StrategyParameterDecimal _oversoldRsi;
        public StrategyParameterDecimal _overboughtRsi;
        public StrategyParameterBool _drawRsiChannel;
        public StrategyParameterBool _rsiFilterIsOn;
        // RSI

        public BreakHighLowByAtrExtTimeRsi(string name, StartProgram startProgram)
            : base(name, startProgram)
        {
            TabCreate(BotTabType.Simple);
            _tab = TabsSimple[0];
            _tab.CandleFinishedEvent += _tab_CandleFinishedEvent;

            Regime = CreateParameter("Regime", "Off", new[] { "Off", "On", "OnlyLong", "OnlyShort", "OnlyClosePosition" }, "Base");
            VolumeRegime = CreateParameter("Volume type", "Number of contracts", new[] { "Number of contracts", "Contract currency", "% of the total portfolio" }, "Base");
            VolumeOnPosition = CreateParameter("Volume", 10, 1.0m, 50, 4, "Base");
            Slippage = CreateParameter("Slippage %", 0m, 0m, 20, 1, "Base");

            TimeStart = CreateParameterTimeOfDay("Start Trade Time", 0, 0, 0, 0, "Base");
            TimeEnd = CreateParameterTimeOfDay("End Trade Time", 24, 0, 0, 0, "Base");

            DampingPeriod = CreateParameter("Param Period", 14, 2, 300, 12, "Robot parameters");
            ExitCandleCount = CreateParameter("Exit Candle Count", 4, 2, 100, 2, "Robot parameters");

            SmaLengthFilter = CreateParameter("Sma Length Filter", 100, 10, 500, 1, "Filters");
            SmaPositionFilterIsOn = CreateParameter("Is SMA Filter On", false, "Filters");
            SmaSlopeFilterIsOn = CreateParameter("Is Sma Slope Filter On", false, "Filters");

            _smaFilter = IndicatorsFactory.CreateIndicatorByName(nameClass: "Sma", name: name + "Sma_Filter", canDelete: false);
            _smaFilter = (Aindicator)_tab.CreateCandleIndicator(_smaFilter, nameArea: "Prime");
            _smaFilter.DataSeries[0].Color = System.Drawing.Color.Azure;
            _smaFilter.ParametersDigit[0].Value = SmaLengthFilter.ValueInt;
            _smaFilter.Save();

            // RSI
            _rsiFilterIsOn = CreateParameter("Is RSI Filter On", true, "Filters");
            _lengthRsi = CreateParameter("Rsi Length", 14, 10, 33, 1, "Filters");
            _oversoldRsi = CreateParameter("Rsi Oversold", 30m, 0, 0, 0, "Filters");
            _overboughtRsi = CreateParameter("Rsi Overbought", 70m, 0, 0, 0, "Filters");
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

            _damping = IndicatorsFactory.CreateIndicatorByName("Damping_Indicator", name + "Damping", false);
            _damping = (Aindicator)_tab.CreateCandleIndicator(_damping, "dampingArea");
            _damping.ParametersDigit[0].Value = DampingPeriod.ValueInt;
            _damping.Save();

            avgHigh2 = IndicatorsFactory.CreateIndicatorByName("Sma", name + "Sma1", false);
            avgHigh2 = (Aindicator)_tab.CreateCandleIndicator(avgHigh2, "Prime");
            ((IndicatorParameterString)avgHigh2.Parameters[1]).ValueString = "High";
            avgHigh2.ParametersDigit[0].Value = 2;
            avgHigh2.DataSeries[0].IsPaint = false;

            avgLow3 = IndicatorsFactory.CreateIndicatorByName("Sma", name + "Sma2", false);
            avgLow3 = (Aindicator)_tab.CreateCandleIndicator(avgLow3, "Prime");
            ((IndicatorParameterString)avgLow3.Parameters[1]).ValueString = "Low";
            avgLow3.ParametersDigit[0].Value = 3;
            avgLow3.DataSeries[0].IsPaint = false;

            avgLow2 = IndicatorsFactory.CreateIndicatorByName("Sma", name + "Sma3", false);
            avgLow2 = (Aindicator)_tab.CreateCandleIndicator(avgLow2, "Prime");
            ((IndicatorParameterString)avgLow2.Parameters[1]).ValueString = "Low";
            avgLow2.ParametersDigit[0].Value = 2;
            avgLow2.DataSeries[0].IsPaint = false;

            avgHigh3 = IndicatorsFactory.CreateIndicatorByName("Sma", name + "Sma4", false);
            avgHigh3 = (Aindicator)_tab.CreateCandleIndicator(avgHigh3, "Prime");
            ((IndicatorParameterString)avgHigh3.Parameters[1]).ValueString = "High";
            avgHigh3.ParametersDigit[0].Value = 3;
            avgHigh3.DataSeries[0].IsPaint = false;

            avgHighShifted = IndicatorsFactory.CreateIndicatorByName("Sma", name + "Sma5", false);
            avgHighShifted = (Aindicator)_tab.CreateCandleIndicator(avgHighShifted, "Prime");
            ((IndicatorParameterString)avgHighShifted.Parameters[1]).ValueString = "High";
            avgHighShifted.ParametersDigit[0].Value = 4;
            avgHighShifted.DataSeries[0].IsPaint = false;

            avgLowShifted = IndicatorsFactory.CreateIndicatorByName("Sma", name + "Sma6", false);
            avgLowShifted = (Aindicator)_tab.CreateCandleIndicator(avgLowShifted, "Prime");
            ((IndicatorParameterString)avgLowShifted.Parameters[1]).ValueString = "Low";
            avgLowShifted.ParametersDigit[0].Value = 4;
            avgLowShifted.DataSeries[0].IsPaint = false;

            StopOrActivateIndicators();
            ParametrsChangeByUser += DampIndex_Param_ParametrsChangeByUser;
            DampIndex_Param_ParametrsChangeByUser();
        }

        private void DampIndex_Param_ParametrsChangeByUser()
        {
            StopOrActivateIndicators();

            if (_damping.ParametersDigit[0].Value != DampingPeriod.ValueInt)
            {
                _damping.ParametersDigit[0].Value = DampingPeriod.ValueInt;
                _damping.Save();
                _damping.Reload();
            }

            if (_smaFilter.DataSeries.Count == 0)
            {
                return;
            }

            if (_smaFilter.ParametersDigit[0].Value != SmaLengthFilter.ValueInt)
            {
                _smaFilter.ParametersDigit[0].Value = SmaLengthFilter.ValueInt;
                _smaFilter.Save();
                _smaFilter.Reload();
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
            return "BreakHighLowByAtrExtTimeRsi";
        }

        public override void ShowIndividualSettingsDialog()
        {

        }

        // Logic
        private void _tab_CandleFinishedEvent(List<Candle> candles)
        {
            if (TimeStart.Value > _tab.TimeServerCurrent ||
                TimeEnd.Value < _tab.TimeServerCurrent)
            {
                CancelStopsAndProfits();
                return;
            }

            if (candles.Count < ExitCandleCount.ValueInt + 1)
            {
                return;
            }

            if (SmaLengthFilter.ValueInt >= candles.Count)
            {
                return;
            }

            decimal damping = _damping.DataSeries[0].Last;

            decimal avgHigh2 = this.avgHigh2.DataSeries[0].Last;
            decimal avgLow3 = this.avgLow3.DataSeries[0].Last;
            decimal avgLow2 = this.avgLow2.DataSeries[0].Last;
            decimal avgHigh3 = this.avgHigh3.DataSeries[0].Last;
            decimal avgHighShifted = this.avgHighShifted.DataSeries[0].Values[this.avgHighShifted.DataSeries[0].Values.Count - 1 - 10];
            decimal avgLowShifted = this.avgLowShifted.DataSeries[0].Values[this.avgLowShifted.DataSeries[0].Values.Count - 1 - 10];
            decimal _lastPrice = candles[candles.Count - 1].Close;

            List<Position> positions = _tab.PositionsOpenAll;

            decimal _slippage = Slippage.ValueDecimal * _lastPrice / 100;

            if (positions.Count == 0)
            {
                if (damping < 1 && (_lastPrice > avgHigh2) && avgLow3 > avgHighShifted)
                {
                    if (BuySignalIsFiltered(candles) == true)
                    {
                        return;
                    }

                    _tab.BuyAtLimit(GetVolume(), _lastPrice + _slippage);
                }
                else if (damping < 1 && (_lastPrice < avgLow2) && avgHigh3 < avgLowShifted)
                {
                    if (SellSignalIsFiltered(candles) == true)
                    {
                        return;
                    }

                    _tab.SellAtLimit(GetVolume(), _lastPrice - _slippage);
                }
            }
            else
            {
                Position pos = positions[0];

                if (pos.Direction == Side.Buy)
                {
                    decimal low = Lowest(candles, ExitCandleCount.ValueInt);
                    _slippage = Slippage.ValueDecimal * low / 100;
                    _tab.CloseAtStop(pos, low, low - _slippage);
                }
                else if (pos.Direction == Side.Sell)
                {
                    decimal high = Highest(candles, ExitCandleCount.ValueInt);
                    _slippage = Slippage.ValueDecimal * high / 100;
                    _tab.CloseAtStop(pos, high, high + _slippage);
                }
            }
        }

        private bool BuySignalIsFiltered(List<Candle> candles)
        {
            // filter for buy
            decimal lastSma = _smaFilter.DataSeries[0].Last;
            decimal _lastPrice = candles[candles.Count - 1].Close;
            //if the mode is off then return true
            if (Regime.ValueString == "Off" ||
                Regime.ValueString == "OnlyShort" ||
                Regime.ValueString == "OnlyClosePosition")
            {
                return true;
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
                // if the price is lower than the last Sma - return to the top true

                if (_lastPrice < lastSma)
                {
                    return true;
                }

            }
            if (SmaSlopeFilterIsOn.ValueBool)
            {
                // if the last Sma is lower than the previous Sma - return true to the top           
                decimal previousSma = _smaFilter.DataSeries[0].Values[_smaFilter.DataSeries[0].Values.Count - 2]; ///

                if (lastSma < previousSma)
                {
                    return true;
                }
            }
            return false;
        }

        private bool SellSignalIsFiltered(List<Candle> candles)
        {
            // filter for sell
            decimal _lastPrice = candles[candles.Count - 1].Close;
            decimal lastSma = _smaFilter.DataSeries[0].Last;
            //if the mode is off then return true
            if (Regime.ValueString == "Off" ||
                Regime.ValueString == "OnlyLong" ||
                Regime.ValueString == "OnlyClosePosition")
            {
                return true;
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
                // if the price is higher than the last Sma - return true to the top

                if (_lastPrice > lastSma)
                {
                    return true;
                }
            }

            if (SmaSlopeFilterIsOn.ValueBool)
            {
                // f the last Sma is higher than the previous Sma - return true to the top
                decimal previousSma = _smaFilter.DataSeries[0].Values[_smaFilter.DataSeries[0].Values.Count - 2];

                if (lastSma > previousSma)
                {
                    return true;
                }
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

        private decimal Highest(List<Candle> candles, int ExitCandleCount)
        {
            decimal high = 0;

            for (int i = candles.Count - 1; i > candles.Count - 1 - ExitCandleCount; i--)
            {
                if (candles[i].High > high)
                {
                    high = candles[i].High;
                }
            }

            return high;
        }

        private decimal Lowest(List<Candle> candles, int ExitCandleCount)
        {
            decimal low = decimal.MaxValue;

            for (int i = candles.Count - 1; i > candles.Count - 1 - ExitCandleCount; i--)
            {
                if (candles[i].Low < low)
                {
                    low = candles[i].Low;
                }
            }

            return low;
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
