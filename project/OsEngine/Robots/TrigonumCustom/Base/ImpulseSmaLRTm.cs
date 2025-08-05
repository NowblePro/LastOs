using OsEngine.Entity;
using OsEngine.Indicators;
using OsEngine.OsTrader.Panels;
using OsEngine.OsTrader.Panels.Attributes;
using OsEngine.OsTrader.Panels.Tab;
using System;
using System.Drawing;
using System.Collections.Generic;

namespace OsEngine.Robots.TrigonumCustom.Base
{

    [Bot("ImpulseSmaLRTm")]
    public class ImpulseSmaLRTm : BotPanel
    {
        BotTabSimple _tab;

        public StrategyParameterString Regime;
        public StrategyParameterDecimal VolumeOnPosition;
        public StrategyParameterString VolumeRegime;
        public StrategyParameterDecimal Slippage;

        private StrategyParameterTimeOfDay TimeStart;
        private StrategyParameterTimeOfDay TimeEnd;

        private StrategyParameterBool _saveJson;

        private Aindicator _LinearRegression;
        StrategyParameterDecimal _upChannel_dev;
        StrategyParameterDecimal _downChannel_dev;
        StrategyParameterInt _lenghtLR;

        public StrategyParameterBool UseRsi;

        Aindicator _Rsi;
        StrategyParameterInt _PeriodRsi;
        StrategyParameterDecimal UpRsiValue;
        StrategyParameterDecimal DownRsiValue;

        StrategyParameterString _regimeTrendFilter;

        public Aindicator _sma;
        StrategyParameterInt _periodSma;

        public Aindicator _smaFilter;
        private StrategyParameterInt SmaLengthFilter;
        public StrategyParameterBool SmaPositionFilterIsOn;
        public StrategyParameterBool SmaSlopeFilterIsOn;

        public StrategyParameterBool _fixTpIsOn;
        public StrategyParameterDecimal _fixTpPercent;

        public ImpulseSmaLRTm(string name, StartProgram startProgram) : base(name, startProgram)
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

            _regimeTrendFilter = CreateParameter("Regime trend filter", "candle", new[] { "candle", "CenterLRC" }, "Base");

            _periodSma = CreateParameter("SMA period", 100, 50, 400, 10, "Robot parameters");

            _lenghtLR = CreateParameter("Lenght LR", 100, 50, 200, 20, "Robot parameters");
            _upChannel_dev = CreateParameter("Up channel deviation LR", 2, 1, 100, 5m, "Robot parameters");
            _downChannel_dev = CreateParameter("Down channel deviation LR", 2, 1, 100, 5m, "Robot parameters");
            UseRsi = CreateParameter("Use Rsi", false, "Robot parameters");
            _PeriodRsi = CreateParameter("Period Rsi indicator", 14, 1, 20, 1, "Robot parameters");
            UpRsiValue = CreateParameter("Up Line Value", 60, 60.0m, 90, 0.5m, "Robot parameters");
            DownRsiValue = CreateParameter("Down Line Value", 30, 10.0m, 40, 0.5m, "Robot parameters");

            SmaLengthFilter = CreateParameter("Sma Length", 100, 10, 500, 1, "Filters");

            SmaPositionFilterIsOn = CreateParameter("Is SMA Filter On", false, "Filters");
            SmaSlopeFilterIsOn = CreateParameter("Is Sma Slope Filter On", false, "Filters");

            _smaFilter = IndicatorsFactory.CreateIndicatorByName(nameClass: "Sma", name: name + "Sma_Filter", canDelete: false);
            _smaFilter = (Aindicator)_tab.CreateCandleIndicator(_smaFilter, nameArea: "Prime");
            _smaFilter.DataSeries[0].Color = Color.Azure;
            _smaFilter.ParametersDigit[0].Value = SmaLengthFilter.ValueInt;
            _smaFilter.Save();

            _LinearRegression = IndicatorsFactory.CreateIndicatorByName("LinearRegressionChannelFast_Indicator", name + "LinearRegressionChannel", false);
            _LinearRegression = (Aindicator)_tab.CreateCandleIndicator(_LinearRegression, "Prime");
            _LinearRegression.ParametersDigit[0].Value = _lenghtLR.ValueInt;
            _LinearRegression.ParametersDigit[1].Value = _upChannel_dev.ValueDecimal;
            _LinearRegression.ParametersDigit[2].Value = _downChannel_dev.ValueDecimal;
            _LinearRegression.Save();

            _Rsi = IndicatorsFactory.CreateIndicatorByName(nameClass: "RSI", name: name + "Rsi", canDelete: false);
            _Rsi = (Aindicator)_tab.CreateCandleIndicator(_Rsi, nameArea: "RsiArea");
            _Rsi.DataSeries[0].Color = Color.Azure;
            _Rsi.ParametersDigit[0].Value = _PeriodRsi.ValueInt;
            _Rsi.Save();

            _sma = IndicatorsFactory.CreateIndicatorByName(nameClass: "Sma", name: name + "Sma", canDelete: false);
            _sma = (Aindicator)_tab.CreateCandleIndicator(_sma, nameArea: "Prime");
            _sma.ParametersDigit[0].Value = _periodSma.ValueInt;
            _sma.Save();

            _fixTpIsOn = CreateParameter("Fix Take Profit Is On", false, "Fix Take Profit");
            _fixTpPercent = CreateParameter("Fix Take Profit Percent", 1.0m, 0.1m, 2.0m, 0.2m, "Fix Take Profit");

            StopOrActivateIndicators();
            ParametrsChangeByUser += LRegBot_ParametrsChangeByUser;
            _tab.CandleFinishedEvent += _tab_CandleFinishedEvent;
            LRegBot_ParametrsChangeByUser();
        }

        private void LRegBot_ParametrsChangeByUser()
        {
            StopOrActivateIndicators();

            _tab.setSaveData(_saveJson.ValueBool);

            if (_LinearRegression.ParametersDigit[0].Value != _lenghtLR.ValueInt ||
            _LinearRegression.ParametersDigit[1].Value != _upChannel_dev.ValueDecimal ||
            _LinearRegression.ParametersDigit[2].Value != _downChannel_dev.ValueDecimal)
            {
                _LinearRegression.ParametersDigit[0].Value = _lenghtLR.ValueInt;
                _LinearRegression.ParametersDigit[1].Value = _upChannel_dev.ValueDecimal;
                _LinearRegression.ParametersDigit[2].Value = _downChannel_dev.ValueDecimal;
                _LinearRegression.Save();
                _LinearRegression.Reload();
            }

            if (_sma.ParametersDigit[0].Value != _periodSma.ValueInt)
            {
                _sma.ParametersDigit[0].Value = _periodSma.ValueInt;
                _sma.Reload();
                _sma.Save();
            }

            if (_Rsi.ParametersDigit[0].Value != _PeriodRsi.ValueInt)
            {
                _Rsi.ParametersDigit[0].Value = _PeriodRsi.ValueInt;
                _Rsi.Reload();
                _Rsi.Save();
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
        }

        private void StopOrActivateIndicators()
        {
            if (UseRsi.ValueBool
                != _Rsi.IsOn)
            {
                _Rsi.IsOn = UseRsi.ValueBool;
                _Rsi.Reload();
            }

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
            return "ImpulseSmaLRTm";
        }

        public override void ShowIndividualSettingsDialog()
        {

        }

        // Logic
        private void _tab_CandleFinishedEvent(List<Candle> candles)
        {
            if (Regime.ValueString == "Off")
            {
                return;
            }

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

            if (_Rsi.DataSeries[0].Values == null)
            {
                return;
            }

            if (_Rsi.DataSeries[0].Values.Count < _Rsi.ParametersDigit[0].Value + 5)
            {
                return;

            }

            if (_tab.CandlesAll == null)
            {
                return;
            }
            if (_lenghtLR.ValueInt + 10 > candles.Count || _periodSma.ValueInt + 10 > candles.Count)
            {
                return;
            }

            List<Position> positions = _tab.PositionsOpenAll;
            decimal lastPrice = candles[candles.Count - 1].Close;
            decimal lr_up = _LinearRegression.DataSeries[0].Last;
            decimal flag = lastPrice;
            decimal lr_down = _LinearRegression.DataSeries[2].Last;
            decimal _slippage = 0;

            if (_regimeTrendFilter.ValueString == "CenterLRC")
            {
                flag = _LinearRegression.DataSeries[1].Last;
            }
            decimal lastSma = _sma.DataSeries[0].Last;
            decimal lastRsi = _Rsi.DataSeries[0].Last;
            decimal prevtRsi = _Rsi.DataSeries[0].Values[_Rsi.DataSeries[0].Values.Count - 2];

            if (positions.Count == 0)
            {// enter logic
                if (flag > lastSma)
                {
                    if (lr_up < lastSma)
                    {
                        return;
                    }
                    _slippage = Slippage.ValueDecimal * lr_up / 100;

                    if (UseRsi.ValueBool)
                    {
                        if (lastRsi > UpRsiValue.ValueDecimal && prevtRsi < UpRsiValue.ValueDecimal)
                        {
                            if (!BuySignalIsFiltered(candles))
                                _tab.BuyAtStop(GetVolume(), lr_up + _slippage, lr_up, StopActivateType.HigherOrEqual, 1);
                        }
                    }
                    else
                    {
                        if (!BuySignalIsFiltered(candles))
                            _tab.BuyAtStop(GetVolume(), lr_up + _slippage, lr_up, StopActivateType.HigherOrEqual, 1);
                    }
                }
                if (flag < lastSma)
                {
                    if (lr_down > lastSma)
                    {
                        return;
                    }
                    _slippage = Slippage.ValueDecimal * lr_down / 100;

                    if (UseRsi.ValueBool)
                    {
                        if (lastRsi < DownRsiValue.ValueDecimal && prevtRsi > DownRsiValue.ValueDecimal)
                        {
                            if (!SellSignalIsFiltered(candles))
                                _tab.SellAtStop(GetVolume(), lr_down - _slippage, lr_down, StopActivateType.LowerOrEqual, 1);
                        }
                    }
                    else
                    {
                        if (!SellSignalIsFiltered(candles))
                            _tab.SellAtStop(GetVolume(), lr_down - _slippage, lr_down, StopActivateType.LowerOrEqual, 1);
                    }
                }
                if (flag < lastSma || BuySignalIsFiltered(candles))
                {
                    _tab.BuyAtStopCancel();
                }
                if (flag > lastSma || SellSignalIsFiltered(candles))
                {
                    _tab.SellAtStopCancel();
                }

            }
            else
            {//exit logic
                for (int i = 0; i < positions.Count; i++)
                {
                    if (positions[i].State != PositionStateType.Open)
                    {
                        continue;
                    }

                    if (_fixTpIsOn.ValueBool && !positions[i].ProfitOrderIsActiv)
                    {
                        if (positions[i].Direction == Side.Buy)
                        {
                            _tab.CloseAtProfitMarket(positions[i], lastPrice + (lastPrice * _fixTpPercent.ValueDecimal / 100));
                        }
                        else if (positions[i].Direction == Side.Sell)
                        {
                            _tab.CloseAtProfitMarket(positions[i], lastPrice - (lastPrice * _fixTpPercent.ValueDecimal / 100));
                        }
                    }

                    decimal stop_level = 0;

                    if (positions[i].Direction == Side.Buy)
                    {// logic to close long position

                        stop_level = lr_down > lastSma ? lr_down : lastSma;
                        _slippage = Slippage.ValueDecimal * stop_level / 100;

                        _tab.CloseAtStop(positions[i], stop_level, stop_level - _slippage);
                    }
                    else if (positions[i].Direction == Side.Sell)
                    {//logic to close short position

                        stop_level = lr_up < lastSma ? lr_up : lastSma;
                        _slippage = Slippage.ValueDecimal * stop_level / 100;

                        _tab.CloseAtStop(positions[i], stop_level, stop_level + _slippage);
                    }
                }
            }
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
