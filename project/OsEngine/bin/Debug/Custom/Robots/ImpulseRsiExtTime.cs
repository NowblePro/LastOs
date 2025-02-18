using OsEngine.Entity;
using OsEngine.Indicators;
using OsEngine.OsTrader.Panels;
using OsEngine.OsTrader.Panels.Attributes;
using OsEngine.OsTrader.Panels.Tab;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

[Bot("ImpulseRsiExtTime")]
internal class ImpulseRsiExtTime : BotPanel
{
    BotTabSimple _tab;

    StrategyParameterString _Regime;

    StrategyParameterDecimal _Slippage;

    StrategyParameterDecimal _VolumeOnPosition;
    StrategyParameterString _VolumeRegime;
    StrategyParameterInt _VolumeDecimals;

    StrategyParameterTimeOfDay _TimeStart;
    StrategyParameterTimeOfDay _TimeEnd;

    StrategyParameterString _PlaceOpenPosition;

    StrategyParameterDecimal _RSIBoostPercent;
    StrategyParameterInt _CandleForBoost;

    StrategyParameterInt _ExitCandleCount;

    Aindicator _Van_Gerch;
    StrategyParameterInt _PeriodVGUp;
    StrategyParameterInt _PeriodVGDown;
    StrategyParameterDecimal _DeviaitonVGUp;
    StrategyParameterDecimal _DeviaitonVGDown;
    Aindicator _Rsi;
    StrategyParameterInt _PeriodRsi;

    Aindicator _SmaFilter;
    StrategyParameterBool _SmaPositionFilterIsOn;
    StrategyParameterInt _SmaLengthFilter;
    StrategyParameterBool _SmaSlopeFilterIsOn;

    StrategyParameterLabel label1;
    StrategyParameterLabel label2;
    StrategyParameterLabel label3;
    StrategyParameterLabel label4;
    StrategyParameterLabel label5;
    StrategyParameterLabel label6;
    StrategyParameterLabel label7;


    public ImpulseRsiExtTime(string name, StartProgram startProgram) : base(name, startProgram)
    {
        TabCreate(BotTabType.Simple);
        _tab = TabsSimple[0];

        _Regime = CreateParameter("Regime", "Off", new[] { "Off", "On", "OnlyClosePosition" }, "Base");
        label5 = CreateParameterLabel("label5", "--------", "--------", 10, 5, Color.White, "Base");
        _Slippage = CreateParameter("Slippage %", 0m, 0, 20, 1, "Base");
        label6 = CreateParameterLabel("label6", "--------", "--------", 10, 5, Color.White, "Base");
        _VolumeRegime = CreateParameter("Volume type", "Contract currency", new[] { "Number of contracts", "Contract currency" }, "Base");
        _VolumeDecimals = CreateParameter("Number of Digits after the decimal point in the volume", 2, 1, 50, 4, "Base");
        _VolumeOnPosition = CreateParameter("Volume", 10, 1.0m, 50, 4, "Base");
        label7 = CreateParameterLabel("label7", "--------", "--------", 10, 5, Color.White, "Base");
        _TimeStart = CreateParameterTimeOfDay("Start Trade Time", 0, 0, 0, 0, "Base");
        _TimeEnd = CreateParameterTimeOfDay("End Trade Time", 24, 0, 0, 0, "Base");

        _PlaceOpenPosition = CreateParameter("Place of position opening", "Up_channel", new string[] { "Up_channel", "Down_channel" }, "Robot parameters");

        label1 = CreateParameterLabel("label1", "--------", "--------", 10, 5, Color.White, "Robot parameters");
        _RSIBoostPercent = CreateParameter("Rsi boost %", 5, 1.0m, 50, 5, "Robot parameters");
        _PeriodRsi = CreateParameter("Period Rsi indicator", 2, 1, 10, 1, "Robot parameters");

        label3 = CreateParameterLabel("label3", "--------", "--------", 10, 5, Color.White, "Robot parameters");
        _CandleForBoost = CreateParameter("Boost for the number of candles", 2, 1, 10, 1, "Robot parameters");

        label2 = CreateParameterLabel("label2", "--------", "--------", 10, 5, Color.White, "Robot parameters");
        _PeriodVGUp = CreateParameter("Period Up VanGerchik indicator", 50, 1, 10, 1, "Robot parameters");
        _PeriodVGDown = CreateParameter("Period Down VanGerchik indicator", 50, 1, 10, 1, "Robot parameters");
        _DeviaitonVGUp = CreateParameter("Deviation Up VanGerchik indicator %", 1, 1.0m, 50, 5, "Robot parameters");
        _DeviaitonVGDown = CreateParameter("Deviation Down VanGerchik indicator %", 1, 1.0m, 50, 5, "Robot parameters");
       
        label4 = CreateParameterLabel("label4", "--------", "--------", 10, 5, Color.White, "Robot parameters");
        _ExitCandleCount = CreateParameter("Exit Candle Count", 4, 2, 100, 2, "Robot parameters");

        _SmaLengthFilter = CreateParameter("Sma Length Filter", 100, 10, 500, 1, "Filter parameters");
        _SmaPositionFilterIsOn = CreateParameter("Is SMA Filter On", false, "Filter parameters");
        _SmaSlopeFilterIsOn = CreateParameter("Is Sma Slope Filter On", false, "Filter parameters");       

        _Van_Gerch = IndicatorsFactory.CreateIndicatorByName(nameClass: "VanGerchik_indicator", name: name + "VanGerchik_indicator", canDelete: false);
        _Van_Gerch = (Aindicator)_tab.CreateCandleIndicator(_Van_Gerch, nameArea: "Prime");
        _Van_Gerch.DataSeries[0].Color = System.Drawing.Color.Azure;
        _Van_Gerch.ParametersDigit[0].Value = _PeriodVGUp.ValueInt;
        _Van_Gerch.ParametersDigit[1].Value = _PeriodVGDown.ValueInt;
        _Van_Gerch.ParametersDigit[2].Value = _DeviaitonVGUp.ValueDecimal;
        _Van_Gerch.ParametersDigit[3].Value = _DeviaitonVGDown.ValueDecimal;
        _Van_Gerch.Save();

        _Rsi = IndicatorsFactory.CreateIndicatorByName(nameClass: "RSI", name: name + "Rsi", canDelete: false);
        _Rsi = (Aindicator)_tab.CreateCandleIndicator(_Rsi, nameArea: "Second");
        _Rsi.DataSeries[0].Color = System.Drawing.Color.Azure;
        _Rsi.ParametersDigit[0].Value = _PeriodRsi.ValueInt;
        _Rsi.Save();

        _SmaFilter = IndicatorsFactory.CreateIndicatorByName(nameClass: "Sma", name: name + "Sma_Filter", canDelete: false);
        _SmaFilter = (Aindicator)_tab.CreateCandleIndicator(_SmaFilter, nameArea: "Prime");
        _SmaFilter.DataSeries[0].Color = System.Drawing.Color.Azure;
        _SmaFilter.ParametersDigit[0].Value = _SmaLengthFilter.ValueInt;
        _SmaFilter.Save();

        ParametrsChangeByUser += ImpulseRsiExtTime_ParametrsChangeByUser;
        _tab.CandleFinishedEvent += _tab_CandleFinishedEvent;
        ImpulseRsiExtTime_ParametrsChangeByUser();
    }

    private void _tab_CandleFinishedEvent(List<Candle> candles)
    {
        if (_TimeStart.Value > _tab.TimeServerCurrent ||
          _TimeEnd.Value < _tab.TimeServerCurrent)
        {
            CancelStopsAndProfits();
            return;
        }
        if (candles.Count < _ExitCandleCount.ValueInt + 1)
        {
            return;
        }

        if (_SmaLengthFilter.ValueInt >= candles.Count)
        {
            return;
        }

        ClosePosition(candles);

        if (BuySignalIsFiltered(candles) == true)
        {
            return;
        }

        OpenPosotion(candles);
    }

    private void OpenPosotion(List<Candle> candles)
    {
        List<Position> positions = _tab.PositionsOpenAll;

        decimal lastPrice = candles[candles.Count - 1].Close;
        decimal lastVanGerchUp = _Van_Gerch.DataSeries[2].Last;
        decimal lastVanGerchDown = _Van_Gerch.DataSeries[3].Last;
        decimal slippage = _Slippage.ValueDecimal * lastPrice / 100;

        if (positions.Count == 0 && CheckBoost(candles))
        {
            if (_PlaceOpenPosition.ValueString.Contains("Up_channel") && lastPrice > lastVanGerchUp)
            {
                _tab.BuyAtStop(GetVolume(), lastPrice + slippage, lastPrice, StopActivateType.HigherOrEqual, 1);
            }
            if (_PlaceOpenPosition.ValueString.Contains("Down_channel") && lastPrice < lastVanGerchDown)
            {
                _tab.BuyAtStop(GetVolume(), lastPrice + slippage, lastPrice, StopActivateType.LowerOrEqyal, 1);
            }
        }
    }

    private void ClosePosition(List<Candle> candles)
    {
        List<Position> positions = _tab.PositionsOpenAll;

        if (positions == null || positions.Count == 0)
        {
            return;
        }

        decimal low = Lowest(candles, _ExitCandleCount.ValueInt);
        decimal lastPrice = candles[candles.Count - 1].Close;
        decimal slippage = _Slippage.ValueDecimal * lastPrice / 100;

        _tab.CloseAtTrailingStop(positions[0], low, low - slippage);
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
    public bool CheckBoost(List<Candle> candles)
    {
        decimal lastRsi = _Rsi.DataSeries[0].Last;
        decimal rsiForCandlesBoost = _Rsi.DataSeries[0].Values[_Rsi.DataSeries[0].Values.Count - 1 - _CandleForBoost.ValueInt];

        decimal prevRsiPlusPercent = rsiForCandlesBoost + (rsiForCandlesBoost * _RSIBoostPercent.ValueDecimal / 100);
        if (lastRsi > prevRsiPlusPercent)
            return true;

        return false;
    }


    private void ImpulseRsiExtTime_ParametrsChangeByUser()
    {

        if (_Van_Gerch.ParametersDigit[0].Value != _PeriodVGUp.ValueInt ||
            _Van_Gerch.ParametersDigit[1].Value != _PeriodVGDown.ValueInt ||
            _Van_Gerch.ParametersDigit[2].Value != _DeviaitonVGUp.ValueDecimal ||
            _Van_Gerch.ParametersDigit[3].Value != _DeviaitonVGDown.ValueDecimal)
        {
            _Van_Gerch.ParametersDigit[0].Value = _PeriodVGUp.ValueInt;
            _Van_Gerch.ParametersDigit[1].Value = _PeriodVGDown.ValueInt;
            _Van_Gerch.ParametersDigit[2].Value = _DeviaitonVGUp.ValueDecimal;
            _Van_Gerch.ParametersDigit[3].Value = _DeviaitonVGDown.ValueDecimal;
            _Van_Gerch.Save();
            _Van_Gerch.Reload();
        }
        if (_Rsi.ParametersDigit[0].Value != _PeriodRsi.ValueInt)
        {
            _Rsi.ParametersDigit[0].Value = _PeriodRsi.ValueInt;
            _Rsi.Reload();
            _Rsi.Save();
        }

        if (_SmaFilter.ParametersDigit[0].Value != _SmaLengthFilter.ValueInt)
        {
            _SmaFilter.ParametersDigit[0].Value = _SmaLengthFilter.ValueInt;
            _SmaFilter.Reload();
            _SmaFilter.Save();
        }


        if (_SmaFilter.DataSeries != null && _SmaFilter.DataSeries.Count > 0)
        {
            if (!_SmaPositionFilterIsOn.ValueBool)
            {
                _SmaFilter.DataSeries[0].IsPaint = false;
            }
            else
            {
                _SmaFilter.DataSeries[0].IsPaint = true;
            }
        }
    }
    #region GetVolume()
    private decimal GetVolume()
    {
        decimal volume = _VolumeOnPosition.ValueDecimal;


        if (_VolumeRegime.ValueString == "Contract currency") // "Валюта контракта"
        {
            decimal contractPrice = TabsSimple[0].PriceBestAsk;
            volume = Math.Round(_VolumeOnPosition.ValueDecimal / contractPrice, _VolumeDecimals.ValueInt);
            return volume;
        }
        else// "Кол-во контрактов
            return volume;
    }
    #endregion
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
        decimal lastSma = _SmaFilter.DataSeries[0].Last;
        // фильтр для покупок
        if (_Regime.ValueString == "Off" ||
            _Regime.ValueString == "OnlyClosePosition")
        {
            return true;
            //если режим работы робота не соответсвует направлению позициивозвращаем на верх true
        }    
        if (_SmaPositionFilterIsOn.ValueBool)
        {
            if (_SmaFilter.DataSeries[0].Last > lastPrice)
            {
                return true;
            }
            // если цена ниже последней сма - возвращаем на верх true
        }
        if (_SmaSlopeFilterIsOn.ValueBool)
        {
            // если последняя сма ниже предыдущей сма - возвращаем на верх true            
            decimal previousSma = _SmaFilter.DataSeries[0].Values[_SmaFilter.DataSeries[0].Values.Count - 2]; ///

            if (lastSma < previousSma)
            {
                return true;
            }
        }

        return false;
    }
    public override string GetNameStrategyType()
    {
        return "ImpulseRsiExtTime";
    }

    public override void ShowIndividualSettingsDialog()
    {

    }
}

