using OsEngine.Entity;
using OsEngine.Indicators;
using OsEngine.OsTrader.Panels;
using OsEngine.OsTrader.Panels.Attributes;
using OsEngine.OsTrader.Panels.Tab;
using System.Collections.Generic;
using System;

namespace OsEngine.Robots.TrigonumCustom.Channel
{

    [Bot("ZZChannelCustom")]
    public class ZZChannelCustom : BotPanel
    {
        private BotTabSimple _tab;
        private StrategyParameterString _regime;
        private StrategyParameterBool _reverseLogic;
        private StrategyParameterDecimal _volumeOnPosition;
        private StrategyParameterString _volumeRegime;
        private StrategyParameterDecimal _slippage;

        private StrategyParameterTimeOfDay _timeStart;
        private StrategyParameterTimeOfDay _timeEnd;

        private StrategyParameterBool _saveJson;

        private Aindicator _smaFilter;
        private StrategyParameterInt _smaFilterLength;
        private StrategyParameterBool _smaPositionFilterIsOn;
        private StrategyParameterBool _smaSlopeFilterIsOn;

        // RSI
        Aindicator _rsi;
        public StrategyParameterInt _lengthRsi;
        public StrategyParameterDecimal _oversoldRsi;
        public StrategyParameterDecimal _overboughtRsi;
        public StrategyParameterBool _drawRsiChannel;
        public StrategyParameterBool _rsiFilterIsOn;
        // RSI

        private Aindicator _zz;
        private StrategyParameterInt _depth;
        private StrategyParameterInt _deviation;
        private StrategyParameterInt _backstep;

        private Aindicator _volumeFilter;
        private StrategyParameterInt _volumeFilterLength;

        public ZZChannelCustom(string name, StartProgram startProgram) : base(name, startProgram)
        {
            TabCreate(BotTabType.Simple);
            _tab = TabsSimple[0];

            _regime = CreateParameter("Regime", "Off", new[] { "Off", "On", "OnlyLong", "OnlyShort", "OnlyClosePosition" }, "Base");
            _reverseLogic = CreateParameter("Reverse logic", true, "Base");
            _volumeRegime = CreateParameter("Volume type", "Number of contracts", new[] { "Number of contracts", "Contract currency", "% of the total portfolio" }, "Base");
            _volumeOnPosition = CreateParameter("Volume", 10, 1.0m, 50, 4, "Base");
            _slippage = CreateParameter("Slippage %", 0m, 0, 20, 1, "Base");

            _saveJson = CreateParameter("Save Json Data", false, "Base");

            _timeStart = CreateParameterTimeOfDay("Start Trade Time", 0, 0, 0, 0, "Base");
            _timeEnd = CreateParameterTimeOfDay("End Trade Time", 24, 0, 0, 0, "Base");

            _depth = CreateParameter("Length ZZ", 50, 50, 200, 20, "Robot parameters");

            _smaFilterLength = CreateParameter("Sma Length", 100, 10, 500, 1, "Filters");

            _smaPositionFilterIsOn = CreateParameter("Is SMA Filter On", true, "Filters");
            _smaSlopeFilterIsOn = CreateParameter("Is Sma Slope Filter On", false, "Filters");

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
            _smaFilter.ParametersDigit[0].Value = _smaFilterLength.ValueInt;
            _smaFilter.Save();

            _zz = IndicatorsFactory.CreateIndicatorByName(nameClass: "ZigZagChannel_indicator", name: name + "ZigZagChannel", canDelete: false);
            _zz = (Aindicator)_tab.CreateCandleIndicator(_zz, nameArea: "Prime");
            _zz.ParametersDigit[0].Value = _depth.ValueInt;
            _zz.Save();

            StopOrActivateIndicators();
            ParametrsChangeByUser += ZZCh_ParametrsChangeByUser;
            _tab.CandleFinishedEvent += _tab_CandleFinishedEvent;
            ZZCh_ParametrsChangeByUser();
        }

        private void ZZCh_ParametrsChangeByUser()
        {
            StopOrActivateIndicators();

            _tab.setSaveData(_saveJson.ValueBool);

            if (_zz.ParametersDigit[0].Value != _depth.ValueInt)
            {
                _zz.ParametersDigit[0].Value = _depth.ValueInt;
                _zz.Reload();
                _zz.Save();
            }

            if (_smaFilter.ParametersDigit[0].Value != _smaFilterLength.ValueInt)
            {
                _smaFilter.ParametersDigit[0].Value = _smaFilterLength.ValueInt;
                _smaFilter.Reload();
                _smaFilter.Save();
            }

            if (_smaFilter.DataSeries != null && _smaFilter.DataSeries.Count > 0)
            {
                if (!_smaPositionFilterIsOn.ValueBool && !_smaSlopeFilterIsOn.ValueBool)
                {
                    _smaFilter.DataSeries[0].IsPaint = false;
                }
                else if (_smaPositionFilterIsOn.ValueBool || _smaSlopeFilterIsOn.ValueBool)
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

            if (_smaPositionFilterIsOn.ValueBool == false
               && _smaSlopeFilterIsOn.ValueBool == false
               && _smaFilter.IsOn == true)
            {
                _smaFilter.IsOn = false;
                _smaFilter.Reload();
            }
            else if ((_smaPositionFilterIsOn.ValueBool == true
                || _smaSlopeFilterIsOn.ValueBool == true)
                && _smaFilter.IsOn == false)
            {
                _smaFilter.IsOn = true;
                _smaFilter.Reload();
            }
        }

        public override string GetNameStrategyType()
        {
            return "ZZChannelCustom";
        }

        public override void ShowIndividualSettingsDialog()
        {

        }

        // Logic
        private void _tab_CandleFinishedEvent(List<Candle> candles)
        {
            if (_regime.ValueString == "Off")
            {
                return;
            }

            if (_timeStart.Value > _tab.TimeServerCurrent ||
                _timeEnd.Value < _tab.TimeServerCurrent)
            {
                CancelStopsAndProfits();
                return;
            }

            if (_tab.CandlesAll == null)
            {
                return;
            }
            if (_depth.ValueInt >= candles.Count)
            {
                return;
            }

            if (_smaFilterLength.ValueInt >= candles.Count)
            {
                return;
            }

            List<Position> positions = _tab.PositionsOpenAll;

            decimal bb_up = _zz.DataSeries[4].Last;
            decimal bb_down = _zz.DataSeries[5].Last;

            decimal lastMaFilter = _smaFilter.DataSeries[0].Last;

            if (bb_down <= 0) return;
            if (bb_up <= 0) return;

            decimal slippage = 0;

            _tab.BuyAtStopCancel();
            _tab.SellAtStopCancel();

            if (positions.Count == 0)
            {// enter logic

                if (bb_up <= bb_down)
                {
                    return;
                }

                if (!BuySignalIsFiltered(candles))
                {
                    if (_reverseLogic.ValueBool)
                    {
                        if (lastMaFilter < bb_down)
                        {
                            return;
                        }

                        slippage = _slippage.ValueDecimal * bb_down / 100;
                        _tab.BuyAtStop(GetVolume(), bb_down + slippage, bb_down, StopActivateType.LowerOrEqual, 1);
                    }
                    else
                    {
                        if (lastMaFilter > bb_up)
                        {
                            return;
                        }

                        slippage = _slippage.ValueDecimal * bb_up / 100;
                        _tab.BuyAtStop(GetVolume(), bb_up + slippage, bb_up, StopActivateType.HigherOrEqual, 1);
                    }
                }

                if (!SellSignalIsFiltered(candles))
                {
                    if (_reverseLogic.ValueBool)
                    {
                        if (lastMaFilter > bb_up)
                        {
                            return;
                        }

                        slippage = _slippage.ValueDecimal * bb_up / 100;
                        _tab.SellAtStop(GetVolume(), bb_up - slippage, bb_up, StopActivateType.HigherOrEqual, 1);
                    }
                    else
                    {
                        if (lastMaFilter < bb_down)
                        {
                            return;
                        }

                        slippage = _slippage.ValueDecimal * bb_down / 100;
                        _tab.SellAtStop(GetVolume(), bb_down - slippage, bb_down, StopActivateType.LowerOrEqual, 1);
                    }
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

                    if (positions[i].Direction == Side.Buy)
                    {// logic to close long position

                        if (_reverseLogic.ValueBool)
                        {
                            slippage = _slippage.ValueDecimal * bb_up / 100;
                            _tab.CloseAtProfit(positions[i], bb_up, bb_up - slippage);
                        }
                        else
                        {
                            slippage = _slippage.ValueDecimal * bb_down / 100;
                            _tab.CloseAtStop(positions[i], bb_down, bb_down - slippage);
                        }
                    }
                    else if (positions[i].Direction == Side.Sell)
                    {//logic to close short position

                        if (_reverseLogic.ValueBool)
                        {
                            slippage = _slippage.ValueDecimal * bb_down / 100;
                            _tab.CloseAtProfit(positions[i], bb_down, bb_down + slippage);
                        }
                        else
                        {
                            slippage = _slippage.ValueDecimal * bb_up / 100;
                            _tab.CloseAtStop(positions[i], bb_up, bb_up + slippage);
                        }
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

            if (_smaPositionFilterIsOn.ValueBool)
            {
                if (lastSma > lastPrice)
                {
                    return true;
                }
                // if the price is lower than the last Sma - return true to the top
            }

            if (_smaSlopeFilterIsOn.ValueBool)
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
            if (_regime.ValueString == "Off" ||
                _regime.ValueString == "OnlyLong" ||
                _regime.ValueString == "OnlyClosePosition")
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

            if (_smaPositionFilterIsOn.ValueBool)
            {
                if (lastSma < lastPrice)
                {
                    return true;
                }
                // if the price is higher than the last Sma - return true to the top
            }

            if (_smaSlopeFilterIsOn.ValueBool)
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
