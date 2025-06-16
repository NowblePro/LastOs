using System;
using System.Collections.Generic;
using System.Drawing;
using OsEngine.Charts.CandleChart.Indicators;
using OsEngine.Entity;
using OsEngine.Indicators;
using OsEngine.OsTrader.Panels.Tab;
using OsEngine.OsTrader.Panels;
using OsEngine.OsTrader.Panels.Attributes;

namespace OsEngine.Robots.TrigonumCustom.Base
{

    [Bot("BreakHighLowByAdxRsi")]
    public class BreakHighLowByAdxRsi : BotPanel
    {
        private BotTabSimple _tab;

        public StrategyParameterString Regime;
        public StrategyParameterDecimal VolumeOnPosition;
        public StrategyParameterString VolumeRegime;
        public StrategyParameterDecimal Slippage;

        private StrategyParameterTimeOfDay TimeStart;
        private StrategyParameterTimeOfDay TimeEnd;

        private StrategyParameterBool _saveJson;

        public StrategyParameterInt AdxHigh;
        public StrategyParameterInt Lookback;
        public StrategyParameterInt TrailBars;

        private Adx _adx;
        public StrategyParameterInt AdxPeriod;

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

        public BreakHighLowByAdxRsi(string name, StartProgram startProgram)
            : base(name, startProgram)
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

            AdxPeriod = CreateParameter("Adx period", 20, 10, 100, 10, "Robot parameters");
            AdxHigh = CreateParameter("AdxHigh", 20, 10, 100, 10, "Robot parameters");
            Lookback = CreateParameter("Lookback", 20, 10, 100, 10, "Robot parameters");
            TrailBars = CreateParameter("TrailBars", 5, 5, 20, 1, "Robot parameters");

            SmaLengthFilter = CreateParameter("Sma Length", 100, 10, 500, 1, "Filters");

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

            _adx = new Adx(name + "ADX", false) { ColorBase = Color.DodgerBlue, PaintOn = true };
            _adx = (Adx)_tab.CreateCandleIndicator(_adx, "AdxArea");
            _adx.Length = AdxPeriod.ValueInt;
            _adx.Save();

            StopOrActivateIndicators();
            _tab.CandleFinishedEvent += _tab_CandleFinishedEvent;
            ParametrsChangeByUser += Breakout_Param_ParametrsChangeByUser;
            Breakout_Param_ParametrsChangeByUser();
        }

        private void Breakout_Param_ParametrsChangeByUser()
        {
            StopOrActivateIndicators();

            _tab.setSaveData(_saveJson.ValueBool);

            if (_adx.Length != AdxPeriod.ValueInt)
            {
                _adx.Length = AdxPeriod.ValueInt;
                _adx.Save();
                _adx.Reload();
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
            return "BreakHighLowByAdxRsi";
        }

        public override void ShowIndividualSettingsDialog()
        {

        }

        // Logic
        private void _tab_CandleFinishedEvent(List<Candle> candles)
        {
            if (SmaLengthFilter.ValueInt >= candles.Count)
            {
                return;
            }

            if (TimeStart.Value > _tab.TimeServerCurrent ||
                TimeEnd.Value < _tab.TimeServerCurrent)
            {
                CancelStopsAndProfits();
                return;
            }

            if (candles.Count < 20)
            {
                return;
            }

            List<Position> positions = _tab.PositionsOpenAll;

            if (positions == null || positions.Count == 0)
            {
                TryOpenPosition(candles);
            }
            else
            {
                TryClosePosition(positions[0], candles);
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
                // if the last Sma is higher than the previous Sma - return true to the top
                decimal previousSma = _smaFilter.DataSeries[0].Values[_smaFilter.DataSeries[0].Values.Count - 2];

                if (lastSma > previousSma)
                {
                    return true;
                }
            }

            return false;
        }

        private void TryOpenPosition(List<Candle> candles)
        {
            decimal lastAdx = ((Adx)_adx).Values[candles.Count - 1];

            if (lastAdx == 0 || ((Adx)_adx).Values.Count + 1 < Lookback.ValueInt)
            {
                return;
            }

            decimal adxMax = 0;

            for (int i = ((Adx)_adx).Values.Count - 1; i > ((Adx)_adx).Values.Count - 1 - Lookback.ValueInt && i > 0; i--)
            {
                decimal value = ((Adx)_adx).Values[i];

                if (value > adxMax)
                {
                    adxMax = value;
                }
            }

            if (adxMax > AdxHigh.ValueInt)
            {
                return;
            }

            // buy
            decimal lineBuy = GetPriceToOpenPos(Side.Buy, candles, candles.Count - 1);
            decimal _lastPrice = candles[candles.Count - 1].Close;
            decimal _slippage = Slippage.ValueDecimal * _lastPrice / 100;
            if (lineBuy + _tab.Security.PriceStep * 5 < candles[candles.Count - 1].Close)
            {
                if (!BuySignalIsFiltered(candles))
                    _tab.BuyAtLimit(GetVolume(), _lastPrice + _slippage);
                return;
            }

            decimal priceOrder = lineBuy;
            decimal priceRedLine = lineBuy;
            _slippage = Slippage.ValueDecimal * priceOrder / 100;
            if (!BuySignalIsFiltered(candles))
                _tab.BuyAtStop(GetVolume(), priceOrder + _slippage, priceRedLine, StopActivateType.HigherOrEqual);

            // sell
            decimal lineSell = GetPriceToOpenPos(Side.Sell, candles, candles.Count - 1);

            if (lineSell - _tab.Security.PriceStep * 5 > candles[candles.Count - 1].Close)
            {
                _slippage = Slippage.ValueDecimal * _lastPrice / 100;
                if (!SellSignalIsFiltered(candles))
                    _tab.SellAtLimit(GetVolume(), _lastPrice - _slippage);
                return;
            }

            priceOrder = lineSell;
            priceRedLine = lineSell;
            _slippage = Slippage.ValueDecimal * priceOrder / 100;
            if (!SellSignalIsFiltered(candles))
                _tab.SellAtStop(GetVolume(), priceOrder - _slippage, priceRedLine, StopActivateType.LowerOrEqual);
        }

        private void TryClosePosition(Position position, List<Candle> candles)
        {
            decimal _slippage = 0;
            // exit in the stop
            if (position.Direction == Side.Buy)
            {
                decimal price = GetPriceStop(Side.Buy, candles, candles.Count - 1);
                if (price == 0)
                {
                    return;
                }

                decimal priceOrder = price;
                decimal priceRedLine = price;

                if (priceRedLine - _tab.Security.PriceStep * 10 > _tab.PriceBestAsk)
                {
                    _slippage = Slippage.ValueDecimal * _tab.PriceBestAsk / 100;
                    _tab.CloseAtLimit(position, _tab.PriceBestAsk - _slippage, position.OpenVolume);
                    return;
                }

                if (position.StopOrderRedLine == 0 || position.StopOrderRedLine < priceRedLine)
                {
                    _slippage = Slippage.ValueDecimal * priceOrder / 100;
                    _tab.CloseAtStop(position, priceRedLine, priceOrder - _slippage);
                }
                else if (position.StopOrderIsActiv == false)
                {
                    if (position.StopOrderRedLine - _tab.Security.PriceStep * 10 > _tab.PriceBestAsk)
                    {
                        _slippage = Slippage.ValueDecimal * _tab.PriceBestAsk / 100;
                        _tab.CloseAtLimit(position, _tab.PriceBestAsk - _slippage, position.OpenVolume);
                        return;
                    }
                    position.StopOrderIsActiv = true;
                }
            }

            if (position.Direction == Side.Sell)
            {
                decimal price = GetPriceStop(Side.Sell, candles, candles.Count - 1);
                if (price == 0)
                {
                    return;
                }

                decimal priceOrder = price;
                decimal priceRedLine = price;

                if (priceRedLine + _tab.Security.PriceStep * 10 < _tab.PriceBestAsk)
                {
                    _slippage = Slippage.ValueDecimal * _tab.PriceBestBid / 100;
                    _tab.CloseAtLimit(position, _tab.PriceBestBid + _slippage, position.OpenVolume);
                    return;
                }

                if (position.StopOrderRedLine == 0 || position.StopOrderRedLine > priceRedLine)
                {
                    _slippage = Slippage.ValueDecimal * priceOrder / 100;
                    _tab.CloseAtStop(position, priceRedLine, priceOrder + _slippage);
                }
                else if (position.StopOrderIsActiv == false)
                {
                    if (position.StopOrderRedLine + _tab.Security.PriceStep * 10 < _tab.PriceBestAsk)
                    {
                        _slippage = Slippage.ValueDecimal * _tab.PriceBestBid / 100;
                        _tab.CloseAtLimit(position, _tab.PriceBestBid + _slippage, position.OpenVolume);
                        return;
                    }
                    position.StopOrderIsActiv = true;
                }
            }
        }

        private decimal GetPriceToOpenPos(Side side, List<Candle> candles, int index)
        {
            if (side == Side.Buy)
            {
                decimal price = 0;

                for (int i = index; i > 0 && i > index - Lookback.ValueInt; i--)
                {
                    if (candles[i].High > price)
                    {
                        price = candles[i].High;
                    }
                }
                return price;
            }
            if (side == Side.Sell)
            {
                decimal price = decimal.MaxValue;
                for (int i = index; i > 0 && i > index - Lookback.ValueInt; i--)
                {
                    if (candles[i].Low < price)
                    {
                        price = candles[i].Low;
                    }
                }
                return price;
            }

            return 0;
        }

        private decimal GetPriceStop(Side side, List<Candle> candles, int index)
        {
            if (candles == null || index < TrailBars.ValueInt)
            {
                return 0;
            }

            if (side == Side.Buy)
            {
                decimal price = decimal.MaxValue;

                for (int i = index; i > index - TrailBars.ValueInt; i--)
                {
                    if (candles[i].Low < price)
                    {
                        price = candles[i].Low;
                    }
                }

                return price;
            }

            if (side == Side.Sell)
            {
                decimal price = 0;

                for (int i = index; i > index - TrailBars.ValueInt; i--)
                {
                    if (candles[i].High > price)
                    {
                        price = candles[i].High;
                    }
                }

                return price;
            }
            return 0;
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
