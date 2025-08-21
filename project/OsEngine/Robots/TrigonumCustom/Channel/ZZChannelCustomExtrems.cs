using OsEngine.Charts.CandleChart.Indicators;
using OsEngine.Entity;
using OsEngine.Indicators;
using OsEngine.OsTrader.Panels;
using OsEngine.OsTrader.Panels.Attributes;
using OsEngine.OsTrader.Panels.Tab;
using OsEngine.Robots.Classes;
using System.Collections.Generic;
using System.Drawing;

namespace OsEngine.Robots.TrigonumCustom.Channel
{

    [Bot("ZZChannelCustomExtrems")]
    public class ZZChannelCustomExtrems : BotPanel
    {
        private BotTabSimple _tab;
        private StrategyParameterString _regime;
        private StrategyParameterDecimal _volumeOnPosition;
        private StrategyParameterString _volumeRegime;
        private StrategyParameterDecimal _slippage;

        private StrategyParameterTimeOfDay _timeStart;
        private StrategyParameterTimeOfDay _timeEnd;

        private StrategyParameterBool _saveJson;

        private Aindicator _zz;
        private StrategyParameterInt _depth;
        private StrategyParameterDecimal _deviation;
        private StrategyParameterInt _backstep;

        private StrategyParameterInt _channelLength;

        private Aindicator _volumeFilter;
        private StrategyParameterInt _volumeFilterLength;
        private StrategyParameterBool _volumeFilterIsOn;

        private Aindicator _smaFilter;
        private StrategyParameterInt _smaFilterLength;
        private StrategyParameterBool _smaFilterIsOn;

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
        public StrategyParameterBool _rsiExitIsOn;
        // RSI

        public StrategyParameterBool _fixTpIsOn;
        public StrategyParameterDecimal _fixTpPercent;

        public ZZChannelCustomExtrems(string name, StartProgram startProgram) : base(name, startProgram)
        {
            TabCreate(BotTabType.Simple);
            _tab = TabsSimple[0];

            _regime = CreateParameter("Regime", "Off", new[] { "Off", "On", "OnlyLong", "OnlyShort", "OnlyClosePosition" }, "Base");
            _volumeRegime = CreateParameter("Volume type", "Number of contracts", new[] { "Number of contracts", "Contract currency", "% of the total portfolio" }, "Base");
            _volumeOnPosition = CreateParameter("Volume", 10, 1.0m, 50, 4, "Base");
            _slippage = CreateParameter("Slippage %", 0m, 0, 20, 1, "Base");

            _saveJson = CreateParameter("Save Json Data", false, "Base");

            _timeStart = CreateParameterTimeOfDay("Start Trade Time", 0, 0, 0, 0, "Base");
            _timeEnd = CreateParameterTimeOfDay("End Trade Time", 24, 0, 0, 0, "Base");

            _depth = CreateParameter("Depth", 14, 5, 30, 1, "Robot parameters");
            _deviation = CreateParameter("Deviation", 0.5m, 0.1m, 10m, 0.5m, "Robot parameters");
            _backstep = CreateParameter("Back Step", 3, 1, 15, 1, "Robot parameters");

            _channelLength = CreateParameter("Channel Length", 3, 3, 5, 1, "Channel parameters");

            _volumeFilterLength = CreateParameter("Volume Filter Length", 10, 1, 15, 1, "Filters");
            _volumeFilterIsOn = CreateParameter("Volume Filter Is On", false, "Filters");
            _smaFilterLength = CreateParameter("SMA Filter Length", 100, 50, 500, 10, "Filters");
            _smaFilterIsOn = CreateParameter("SMA Filter Is On", false, "Filters");

            _zz = IndicatorsFactory.CreateIndicatorByName(nameClass: "ZigZagChannelCustomExtrems", name: name + "ZigZagChannel", canDelete: false);
            _zz = (Aindicator)_tab.CreateCandleIndicator(_zz, nameArea: "Prime");
            _zz.ParametersDigit[0].Value = _depth.ValueInt;
            _zz.ParametersDigit[1].Value = _deviation.ValueDecimal;
            _zz.ParametersDigit[2].Value = _backstep.ValueInt;
            _zz.Save();

            _volumeFilter = IndicatorsFactory.CreateIndicatorByName(nameClass: "Volume", name: name + "Volume", canDelete: false);
            _volumeFilter = (Aindicator)_tab.CreateCandleIndicator(_volumeFilter, nameArea: "VolumeArea");
            _volumeFilter.Save();

            _smaFilter = IndicatorsFactory.CreateIndicatorByName(nameClass: "Sma", name: name + "Sma", canDelete: false);
            _smaFilter = (Aindicator)_tab.CreateCandleIndicator(_smaFilter, nameArea: "Prime");
            _smaFilter.ParametersDigit[0].Value = _smaFilterLength.ValueInt;
            _smaFilter.Save();

            // создаем параметры настроек и создаем объект класса TrailingStop
            //---------------------------------
            TrailingStopIsOn = CreateParameter("Is Trailing stop On", false, "Trailing Stop");
            TrailingStopTypeOrder = CreateParameter("Type order", OrderPriceType.Market.ToString(), new[] { OrderPriceType.Market.ToString(), OrderPriceType.Limit.ToString() }, "Trailing Stop");
            PointOrPercent = CreateParameter("Choice Points or Percent", "Points", new[] { "Points", "Percent" }, "Trailing Stop");
            ChangeStepStop = CreateParameter("Stop level change step", 1, 1, 100, 001m, "Trailing Stop");
            MinDist = CreateParameter("Minimum distance to price", 1, 1, 100, 0.01m, "Trailing Stop");
            QuantityStepsPrices = CreateParameter("Quantity steps prices for limit order", 0m, 0, 10000, 1, "Trailing Stop");
            _trailingStop = new TrailingStop(_tab, TrailingStopTypeOrder.ValueString, ChangeStepStop.ValueDecimal, MinDist.ValueDecimal, QuantityStepsPrices.ValueDecimal, PointOrPercent.ValueString);
            //---------------------------------

            // RSI
            _rsiExitIsOn = CreateParameter("RSI Exit Is On", false, "RSI Exit");
            _lengthRsi = CreateParameter("Rsi Length", 14, 10, 33, 1, "RSI Exit");
            _oversoldRsi = CreateParameter("Rsi Oversold", 30m, 25, 45, 5, "RSI Exit");
            _overboughtRsi = CreateParameter("Rsi Overbought", 70m, 55, 75, 5, "RSI Exit");
            _drawRsiChannel = CreateParameter("Draw Ovb/Ovs Channel", false, "RSI Exit");

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

            _fixTpIsOn = CreateParameter("Fix Take Profit Is On", false, "Fix Take Profit");
            _fixTpPercent = CreateParameter("Fix Take Profit Percent", 1.0m, 0.1m, 2.0m, 0.2m, "Fix Take Profit");

            StopOrActivateIndicators();
            ParametrsChangeByUser += ZZCh_ParametrsChangeByUser;
            _tab.CandleFinishedEvent += _tab_CandleFinishedEvent;
            ZZCh_ParametrsChangeByUser();

            _tab.PositionOpeningSuccesEvent += _tab_PositionOpeningSuccesEvent;
        }

        private void _tab_PositionOpeningSuccesEvent(Position obj)
        {
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

        private void ZZCh_ParametrsChangeByUser()
        {
            StopOrActivateIndicators();

            _tab.setSaveData(_saveJson.ValueBool);

            _zz.ParametersDigit[0].Value = _depth.ValueInt;
            _zz.ParametersDigit[1].Value = _deviation.ValueDecimal;
            _zz.ParametersDigit[2].Value = _backstep.ValueInt;
            _zz.ParametersDigit[3].Value = _channelLength.ValueInt;
            _zz.Save();
            _zz.Reload();

            _smaFilter.ParametersDigit[0].Value = _smaFilterLength.ValueInt;
            if (_smaFilterIsOn.ValueBool)
            {
                _smaFilter.DataSeries[0].IsPaint = true;
            }
            else
            {
                _smaFilter.DataSeries[0].IsPaint = false;
            }
            _smaFilter.Save();
            _smaFilter.Reload();

            if (_volumeFilterIsOn.ValueBool)
            {
                _volumeFilter.DataSeries[0].IsPaint = true;
            }
            else
            {
                _volumeFilter.DataSeries[0].IsPaint = false;
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
                if (!_rsiExitIsOn.ValueBool)
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
            if (_rsiExitIsOn.ValueBool == false)
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

            if (_smaFilter.IsOn == true)
            {
                _smaFilter.IsOn = true;
                _smaFilter.Reload();
            }
            else if (_smaFilter.IsOn == false)
            {
                _smaFilter.IsOn = false;
                _smaFilter.Reload();
            }
        }

        public override string GetNameStrategyType()
        {
            return "ZZChannelCustomExtrems";
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

            if (_volumeFilterIsOn.ValueBool && _volumeFilterLength.ValueInt >= candles.Count)
            {
                return;
            }
            if (_smaFilterIsOn.ValueBool && _smaFilterLength.ValueInt >= candles.Count)
            {
                return;
            }
            if (_rsiExitIsOn.ValueBool && _lengthRsi.ValueInt >= candles.Count)
            {
                return;
            }

            if (_depth.ValueInt >= candles.Count
                || ((IndicatorParameterBool)_zz.Parameters[4]).ValueBool == false
                || ((IndicatorParameterBool)_zz.Parameters[5]).ValueBool == false)
            {
                return;
            }

            List<Position> positions = _tab.PositionsOpenAll;

            decimal last_price = candles[candles.Count - 1].Close;

            decimal zz_up = _zz.DataSeries[2].Last;
            decimal zz_down = _zz.DataSeries[3].Last;

            if (positions.Count == 0)
            {// enter logic
                if (zz_up <= zz_down)
                {
                    return;
                }

                if (last_price > zz_up)
                {
                    if (!BuySignalIsFiltered(candles))
                    {
                        _tab.BuyAtLimit(GetVolume(), last_price + _slippage.ValueDecimal);
                    }
                }

                if (last_price < zz_down)
                {
                    if (!SellSignalIsFiltered(candles))
                    {
                        _tab.SellAtLimit(GetVolume(), last_price + _slippage.ValueDecimal);
                    }
                }
            }
            else
            {//exit logic
                // если включен режим трейлинг стопа, то обращаемся к методу SetTrailingStop и передаем в него цену закрытия последней свечи
                //-----------------------------------------
                if (TrailingStopIsOn.ValueBool)
                {
                    _trailingStop.SetTrailingStop(candles[candles.Count - 1].Close);
                    return;
                }
                //--------------------------------------

                Position pos = positions[0];

                if (_fixTpIsOn.ValueBool && !pos.ProfitOrderIsActiv)
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

                if (pos.Direction == Side.Buy)
                {
                    if (_rsiExitIsOn.ValueBool)
                    {
                        decimal lastRsi = _rsi.DataSeries[0].Last;
                        if (lastRsi >= _overboughtRsi.ValueDecimal)
                        {
                            _tab.CloseAtStop(pos, last_price, last_price - _slippage.ValueDecimal);
                            return;
                        }
                    }

                    if (last_price < zz_up)
                    {
                        _tab.CloseAtStop(pos, last_price, last_price - _slippage.ValueDecimal);
                    }
                }

                if (pos.Direction == Side.Sell)
                {
                    if (_rsiExitIsOn.ValueBool)
                    {
                        decimal lastRsi = _rsi.DataSeries[0].Last;
                        if (lastRsi <= _oversoldRsi.ValueDecimal)
                        {
                            _tab.CloseAtProfit(pos, last_price, last_price + _slippage.ValueDecimal);
                            return;
                        }
                    }

                    if (last_price > zz_down)
                    {
                        _tab.CloseAtProfit(pos, last_price, last_price + _slippage.ValueDecimal);
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
            if (_regime.ValueString == "Off" ||
                _regime.ValueString == "OnlyShort" ||
                _regime.ValueString == "OnlyClosePosition")
            {
                return true;
            }

            decimal lastPrice = candles[candles.Count - 1].Close;

            if (_smaFilterIsOn.ValueBool)
            {
                decimal lastSma = _smaFilter.DataSeries[0].Last;
                if (lastPrice < lastSma)
                {
                    return true;
                }
            }

            if (_volumeFilterIsOn.ValueBool)
            {
                decimal last_volume = _volumeFilter.DataSeries[0].Last;
                for (int i = 1; i < _volumeFilterLength.ValueInt; ++i)
                {
                    if (_volumeFilter.DataSeries[0].Values[_volumeFilter.DataSeries[0].Values.Count - i - 1] > last_volume)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private bool SellSignalIsFiltered(List<Candle> candles)
        {
            if (_regime.ValueString == "Off" ||
                _regime.ValueString == "OnlyLong" ||
                _regime.ValueString == "OnlyClosePosition")
            {
                return true;
            }

            decimal lastPrice = candles[candles.Count - 1].Close;

            if (_smaFilterIsOn.ValueBool)
            {
                decimal lastSma = _smaFilter.DataSeries[0].Last;
                if (lastPrice > lastSma)
                {
                    return true;
                }
            }

            if (_volumeFilterIsOn.ValueBool)
            {
                decimal last_volume = _volumeFilter.DataSeries[0].Last;
                for (int i = 1; i < _volumeFilterLength.ValueInt; ++i)
                {
                    if (_volumeFilter.DataSeries[0].Values[_volumeFilter.DataSeries[0].Values.Count - i - 1] > last_volume)
                    {
                        return true;
                    }
                }
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
