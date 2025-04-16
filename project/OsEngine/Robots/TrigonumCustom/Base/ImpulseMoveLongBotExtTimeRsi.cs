using System;
using System.Collections.Generic;
using OsEngine.Entity;
using OsEngine.Indicators;
using OsEngine.OsTrader.Panels.Tab;
using OsEngine.OsTrader.Panels;
using OsEngine.OsTrader.Panels.Attributes;


[Bot("ImpulseMoveLongBotExtTimeRsi")]
public class ImpulseMoveLongBotExtTimeRsi : BotPanel
{
    private BotTabSimple _tab;

    StrategyParameterString Regime;
    public StrategyParameterDecimal VolumeOnPosition;
    public StrategyParameterString VolumeRegime;
    public StrategyParameterInt VolumeDecimals;
    StrategyParameterDecimal Slippage;

    private StrategyParameterDecimal ParamMult;
    private StrategyParameterDecimal ParamMode;

    private StrategyParameterTimeOfDay TimeStart;
    private StrategyParameterTimeOfDay TimeEnd;

    public Aindicator _chandelier;
    private StrategyParameterDecimal ChandelierMult;

    public Aindicator _atr;
    private StrategyParameterInt AtrPeriod;

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

    public ImpulseMoveLongBotExtTimeRsi(string name, StartProgram startProgram)
            : base(name, startProgram)
    {

        TabCreate(BotTabType.Simple);
        _tab = TabsSimple[0];
        _tab.CandleFinishedEvent += _tab_CandleFinishedEvent;

        Regime = CreateParameter("Regime", "Off", new[] { "Off", "On", "OnlyClosePosition" }, "Base");
        VolumeRegime = CreateParameter("Volume type", "Number of contracts", new[] { "Number of contracts", "Contract currency", "% of the total portfolio" }, "Base");
        VolumeDecimals = CreateParameter("Decimals Volume", 2, 1, 50, 4, "Base");
        VolumeOnPosition = CreateParameter("Volume", 10, 1.0m, 50, 4, "Base");
        Slippage = CreateParameter("Slippage %", 0m, 0, 20, 1, "Base");

        TimeStart = CreateParameterTimeOfDay("Start Trade Time", 0, 0, 0, 0, "Base");
        TimeEnd = CreateParameterTimeOfDay("End Trade Time", 24, 0, 0, 0, "Base");

        ParamMult = CreateParameter("Multiple", 1.5m, 1, 3, 0.2m, "Robot parameters");
        ParamMode = CreateParameter("White/Johnson", 1m, 1, 2, 1, "Robot parameters");

        AtrPeriod = CreateParameter("Atr Period", 10, 2, 30, 12, "Robot parameters");
        ChandelierMult = CreateParameter("Chandelier Mult", 4m, 2, 300, 12, "Robot parameters");

        SmaLengthFilter = CreateParameter("Sma Length", 100, 10, 500, 1, "Filters");

        SmaPositionFilterIsOn = CreateParameter("Is SMA Filter On", false, "Filters");
        SmaSlopeFilterIsOn = CreateParameter("Is Sma Slope Filter On", false, "Filters");

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

        _chandelier = IndicatorsFactory.CreateIndicatorByName("Chandelier_Indicator", name + "Chandelier", false);
        _chandelier = (Aindicator)_tab.CreateCandleIndicator(_chandelier, "Prime");
        _chandelier.ParametersDigit[0].Value = AtrPeriod.ValueInt;
        _chandelier.ParametersDigit[1].Value = ChandelierMult.ValueDecimal;
        _chandelier.Save();

        _atr = IndicatorsFactory.CreateIndicatorByName("ATR", name + "atr", false);
        _atr = (Aindicator)_tab.CreateCandleIndicator(_atr, "AtrArea");
        _atr.ParametersDigit[0].Value = AtrPeriod.ValueInt;
        _atr.Save();

        StopOrActivateIndicators();
        ParametrsChangeByUser += Bearish_Param_ParametrsChangeByUser;
        Bearish_Param_ParametrsChangeByUser();
    }

    private void Bearish_Param_ParametrsChangeByUser()
    {
        StopOrActivateIndicators();

        // Old, from bearish:
        if (_chandelier.ParametersDigit[0].Value != AtrPeriod.ValueInt ||
        _chandelier.ParametersDigit[1].Value != ChandelierMult.ValueDecimal)
        {
            _chandelier.ParametersDigit[0].Value = AtrPeriod.ValueInt;
            _chandelier.ParametersDigit[1].Value = ChandelierMult.ValueDecimal;
            _chandelier.Save();
            _chandelier.Reload();
        }

        if (_atr.ParametersDigit[0].Value != AtrPeriod.ValueInt)
        {
            _atr.ParametersDigit[0].Value = AtrPeriod.ValueInt;
            _atr.Save();
            _atr.Reload();
        }

        if (_smaFilter.ParametersDigit[0].Value != SmaLengthFilter.ValueInt)
        {
            _smaFilter.ParametersDigit[0].Value = SmaLengthFilter.ValueInt;
            _smaFilter.Reload();
            _smaFilter.Save();
        }

        if (_smaFilter.DataSeries != null && _smaFilter.DataSeries.Count > 0)
        {
            if (!SmaPositionFilterIsOn.ValueBool)
            {
                _smaFilter.DataSeries[0].IsPaint = false;
            }
            else
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
        return "ImpulseMoveLongBotExtTimeRsi";
    }

    public override void ShowIndividualSettingsDialog()
    {

    }

    private void _tab_CandleFinishedEvent(List<Candle> candles)
    {
        if (SmaLengthFilter.ValueInt >= candles.Count)
        {
            return;
        }

        // использование
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

        if (positions.Count == 0)
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
        // фильтр для покупок

        decimal lastSma = _smaFilter.DataSeries[0].Last;
        decimal _lastPrice = candles[candles.Count - 1].Close;
        //если режим выкл то возвращаем тру
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
            // если цена ниже последней сма - возвращаем на верх true

            if (_lastPrice < lastSma)
            {
                return true;
            }

        }
        if (SmaSlopeFilterIsOn.ValueBool)
        {
            // если последняя сма ниже предыдущей сма - возвращаем на верх true            
            decimal previousSma = _smaFilter.DataSeries[0].Values[_smaFilter.DataSeries[0].Values.Count - 2]; ///

            if (lastSma < previousSma)
            {
                return true;
            }
        }

        return false;
    }

    private void TryOpenPosition(List<Candle> candles)
    {
        if (_atr == null || _atr.DataSeries == null ||
            _atr.DataSeries.Count == 0)
        {
            return;
        }

        decimal lastAtr = _atr.DataSeries[0].Last;

        if (lastAtr == 0)
        {
            return;
        }

        if (candles.Count < 23)
        {
            return;
        }

        //New:


        decimal priceEtalon = candles[candles.Count - 21].High + (lastAtr * 4);


        decimal _lastPrice = candles[candles.Count - 1].Close;
        decimal _slippage = Slippage.ValueDecimal * _lastPrice / 100;

        if (priceEtalon + _tab.Security.PriceStep * 5 < candles[candles.Count - 1].Close)
        {

            if (BuySignalIsFiltered(candles) == true)
            {
                return;
            }


            _tab.BuyAtLimit(GetVolume(), _lastPrice + _slippage);
            return;
        }

        decimal priceOrder = priceEtalon;
        decimal priceRedLine = priceEtalon;

        if (BuySignalIsFiltered(candles) == true)
        {
            return;
        }

        _slippage = Slippage.ValueDecimal * priceOrder / 100;
        _tab.BuyAtStop(GetVolume(), priceOrder + _slippage, priceRedLine, StopActivateType.HigherOrEqual);
    }

    private void TryClosePosition(Position position, List<Candle> candles)
    {
        // первый выход по проколу
        decimal _lastPrice = candles[candles.Count - 1].Close;
        decimal _slippage = Slippage.ValueDecimal * _lastPrice / 100;

        if (Shoulderette(candles.Count - 1, candles))
        { // если произошёл прокол и мы заработали больше 20%
            if (position.EntryPrice * 1.2m <= candles[candles.Count - 1].Close)
            {
                _tab.CloseAtLimit(position, _lastPrice - _slippage, position.OpenVolume);
                //Sell(_settings.Position, _myCandles[_myCandles.Length - 1].ClosePrice);
                return;
            }
        }

        // второй выход по стопам
        if (position.Direction == Side.Buy)
        {
            decimal priceEtalon = _chandelier.DataSeries[0].Last;
            if (_tab.Security.Decimals == 0)
            {
                priceEtalon = Math.Truncate(_chandelier.DataSeries[0].Last);
            }
            decimal priceOrder = priceEtalon; // ЗДЕСЬ!!!!!!!!!!!!!! 
            decimal priceRedLine = priceEtalon;

            _slippage = Slippage.ValueDecimal * _tab.PriceBestAsk / 100;
            if (priceRedLine - _tab.Security.PriceStep * 10 > _tab.PriceBestAsk)
            {
                _tab.CloseAtLimit(position, _tab.PriceBestAsk - _slippage, position.OpenVolume);
                return;
            }
            _slippage = Slippage.ValueDecimal * priceOrder / 100;
            _tab.CloseAtStop(position, priceRedLine, priceOrder - _slippage);

        }
    }

    private bool Shoulderette(int index, List<Candle> candles)
    {
        decimal jumpMount;

        if (ParamMode.ValueDecimal != 2)
        {
            jumpMount = ParamMult.ValueDecimal * (candles[index].Close / 100);
        }
        else
        {
            jumpMount = ParamMult.ValueDecimal * _atr.DataSeries[0].Last;
        }

        decimal max = 0;

        for (int i = index; i > index - 20; i--)
        {
            if (candles[i].High > max)
            {
                max = candles[i].High;
            }
        }

        if ((candles[index].Close > (candles[index - 1].Close + jumpMount)) &&
            (candles[index].High < max))
        {
            return true;
        }
        else
        {
            return false;
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

    private decimal GetVolume()
    {
        decimal volume = VolumeOnPosition.ValueDecimal;

        if (VolumeRegime.ValueString == "Contract currency") // "Валюта контракта"
        {
            decimal contractPrice = TabsSimple[0].PriceBestAsk;
            volume = Math.Round(VolumeOnPosition.ValueDecimal / contractPrice, VolumeDecimals.ValueInt);
            return volume;
        }
        else if (VolumeRegime.ValueString == "Number of contracts")
        {
            return volume;
        }
        else //if (VolumeRegime.ValueString == "% of the total portfolio")
        {
            return Math.Round(_tab.Portfolio.ValueCurrent * (volume / 100) / _tab.PriceBestAsk / _tab.Security.Lot, VolumeDecimals.ValueInt);
        }
    }
}
