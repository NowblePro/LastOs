using OsEngine.Entity;
using OsEngine.Indicators;
using OsEngine.OsTrader.Panels;
using OsEngine.OsTrader.Panels.Attributes;
using OsEngine.OsTrader.Panels.Tab;
using System.Collections.Generic;
using System;
using OsEngine.Charts.CandleChart.Indicators;
using Microsoft.Office.Interop.Excel;

[Bot("BreakZZChannelRsi")]
public class BreakZZChannelRsi : BotPanel
{
    BotTabSimple _tab;
    StrategyParameterString Regime;
    public StrategyParameterDecimal VolumeOnPosition;
    public StrategyParameterString VolumeRegime;
    StrategyParameterDecimal Slippage;

    private StrategyParameterTimeOfDay TimeStart;
    private StrategyParameterTimeOfDay TimeEnd;

    public Aindicator _smaFilter;
    private StrategyParameterInt SmaLengthFilter;
    public StrategyParameterBool SmaPositionFilterIsOn;
    public StrategyParameterBool SmaSlopeFilterIsOn;

    Aindicator _zz;
    StrategyParameterInt _lengthZZ;

    Aindicator _rsi;
    public StrategyParameterInt _lengthRsi;
    public StrategyParameterInt _oversoldRsi;
    public StrategyParameterInt _overboughtRsi;
    public StrategyParameterBool RsiFilterIsOn;
    private StrategyParameterBool _saveJson;

    public BreakZZChannelRsi(string name, StartProgram startProgram) : base(name, startProgram)
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

        _lengthZZ = CreateParameter("Length ZZ", 50, 50, 200, 20, "Robot parameters");

        SmaLengthFilter = CreateParameter("Sma Length", 100, 10, 500, 1, "Filters");

        SmaPositionFilterIsOn = CreateParameter("Is SMA Filter On", true, "Filters");
        SmaSlopeFilterIsOn = CreateParameter("Is Sma Slope Filter On", false, "Filters");

        RsiFilterIsOn = CreateParameter("Is RSI Filter On", true, "Filters");

        _lengthRsi = CreateParameter("Rsi Length", 14, 10, 33, 1, "Filters");
        _oversoldRsi = CreateParameter("Rsi Oversold", 30, 0, 0, 0, "Filters");
        _overboughtRsi = CreateParameter("Rsi Overbought", 70, 0, 0, 0, "Filters");
        _rsi = IndicatorsFactory.CreateIndicatorByName("RSI", name + "RSI", false);
        _rsi = (Aindicator)_tab.CreateCandleIndicator(_rsi, "Prime");
        _rsi.DataSeries[0].Color = System.Drawing.Color.Coral;
        _rsi.ParametersDigit[0].Value = _lengthRsi.ValueInt;
        _rsi.Save();

        _smaFilter = IndicatorsFactory.CreateIndicatorByName(nameClass: "Sma", name: name + "Sma_Filter", canDelete: false);
        _smaFilter = (Aindicator)_tab.CreateCandleIndicator(_smaFilter, nameArea: "Prime");
        _smaFilter.DataSeries[0].Color = System.Drawing.Color.Azure;
        _smaFilter.ParametersDigit[0].Value = SmaLengthFilter.ValueInt;
        _smaFilter.Save();

        _zz = IndicatorsFactory.CreateIndicatorByName(nameClass: "ZigZagChannel_indicator", name: name + "ZigZagChannel", canDelete: false);
        _zz = (Aindicator)_tab.CreateCandleIndicator(_zz, nameArea: "Prime");
        _zz.ParametersDigit[0].Value = _lengthZZ.ValueInt;
        _zz.Save();

        StopOrActivateIndicators();
        ParametrsChangeByUser += LRegBot_ParametrsChangeByUser;
        _tab.CandleFinishedEvent += _tab_CandleFinishedEvent;
        LRegBot_ParametrsChangeByUser();
    }

    private void LRegBot_ParametrsChangeByUser()
    {
        _tab.setSaveData(_saveJson.ValueBool);
        StopOrActivateIndicators();

        if (_zz.ParametersDigit[0].Value != _lengthZZ.ValueInt)
        {
            _zz.ParametersDigit[0].Value = _lengthZZ.ValueInt;
            _zz.Reload();
            _zz.Save();
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

        if (_rsi.DataSeries != null && _rsi.DataSeries.Count > 0)
        {
            _rsi.ParametersDigit[0].Value = _lengthRsi.ValueInt;
            _rsi.Reload();
            _rsi.Save();
        }
    }

    private void StopOrActivateIndicators()
    {
        if (RsiFilterIsOn.ValueBool == false)
        {
            _rsi.IsOn = false;
            _rsi.Reload();
        }
        else
        {
            _rsi.IsOn = true;
            _rsi.Reload();
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
        return "BreakZZChannelRsi";
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

        if (_tab.CandlesAll == null)
        {
            return;
        }
        if (_lengthZZ.ValueInt >= candles.Count)
        {
            return;
        }

        if (SmaLengthFilter.ValueInt >= candles.Count)
        {
            return;
        }

        List<Position> positions = _tab.PositionsOpenAll;

        decimal bb_up = _zz.DataSeries[4].Last;
        decimal bb_down = _zz.DataSeries[5].Last;

        decimal lastMaFilter = _smaFilter.DataSeries[0].Last;
        if (bb_down <= 0) return;
        if (bb_up <= 0) return;

        decimal _slippage = 0;

        if (positions.Count == 0)
        {// enter logic

            _slippage = Slippage.ValueDecimal * bb_up / 100;

            if (!BuySignalIsFiltered(candles))
            {
                if (lastMaFilter > bb_up)
                {
                    return;
                }

                _tab.BuyAtStop(GetVolume(), bb_up + _slippage, bb_up, StopActivateType.HigherOrEqual, 1);
            }
            _slippage = Slippage.ValueDecimal * bb_down / 100;

            if (!SellSignalIsFiltered(candles))
            {
                if (lastMaFilter < bb_down)
                {
                    return;
                }

                _tab.SellAtStop(GetVolume(), bb_down - _slippage, bb_down, StopActivateType.LowerOrEqyal, 1);
            }
        }
        else
        {//exit logic
            for (int i = 0; i < positions.Count; i++)
            {
                _tab.BuyAtStopCancel();
                _tab.SellAtStopCancel();

                if (positions[i].State != PositionStateType.Open)
                {
                    continue;
                }
                decimal stop_level = 0;

                if (positions[i].Direction == Side.Buy)
                {// logic to close long position

                    stop_level = bb_down < lastMaFilter ? bb_down : lastMaFilter;
                    _slippage = Slippage.ValueDecimal * stop_level / 100;

                    _tab.CloseAtTrailingStop(positions[i], stop_level, stop_level - _slippage);
                }
                else if (positions[i].Direction == Side.Sell)
                {//logic to close short position

                    stop_level = bb_up > lastMaFilter && bb_up > 0 ? bb_up : lastMaFilter;
                    _slippage = Slippage.ValueDecimal * stop_level / 100;

                    _tab.CloseAtTrailingStop(positions[i], stop_level, stop_level + _slippage);
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
        decimal lastRsi = _rsi.DataSeries[0].Last;
        // filter for buy
        if (Regime.ValueString == "Off" ||
            Regime.ValueString == "OnlyShort" ||
            Regime.ValueString == "OnlyClosePosition")
        {
            return true;
            //if the robot's operating mode does not correspond to the direction of the position
        }

        if (RsiFilterIsOn.ValueBool)
        {
            if (lastRsi >= _oversoldRsi.ValueInt)
            {
                return true;
            }
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
        decimal lastRsi = _rsi.DataSeries[0].Last;
        // filter for sell
        if (Regime.ValueString == "Off" ||
            Regime.ValueString == "OnlyLong" ||
            Regime.ValueString == "OnlyClosePosition")
        {
            return true;
            //if the robot's operating mode does not correspond to the direction of the position
        }

        if (RsiFilterIsOn.ValueBool)
        {
            if (lastRsi <= _overboughtRsi.ValueInt)
            {
                return true;
            }
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
}
