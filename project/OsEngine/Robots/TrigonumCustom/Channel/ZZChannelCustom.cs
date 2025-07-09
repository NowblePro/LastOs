using OsEngine.Entity;
using OsEngine.Indicators;
using OsEngine.OsTrader.Panels;
using OsEngine.OsTrader.Panels.Attributes;
using OsEngine.OsTrader.Panels.Tab;
using OsEngine.Robots.Classes;
using System.Collections.Generic;

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

        private Aindicator _zz;
        private StrategyParameterInt _depth;
        private StrategyParameterDecimal _deviation;
        private StrategyParameterInt _backstep;

        private Aindicator _volumeFilter;
        private StrategyParameterInt _volumeFilterLength;

        private Aindicator _smaFilter;
        private StrategyParameterInt _smaFilterLength;

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

            _depth = CreateParameter("Depth", 14, 5, 30, 1, "Robot parameters");
            _deviation = CreateParameter("Deviation", 0.5m, 0.1m, 10m, 0.5m, "Robot parameters");
            _backstep = CreateParameter("Back Step", 3, 1, 15, 1, "Robot parameters");

            _volumeFilterLength = CreateParameter("Volume Filter Length", 10, 1, 15, 1, "Robot parameters");

            _smaFilterLength = CreateParameter("Sma Filter Length", 100, 50, 500, 10, "Robot parameters");

            _zz = IndicatorsFactory.CreateIndicatorByName(nameClass: "ZigZagChannelCustom", name: name + "ZigZagChannel", canDelete: false);
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

            if (_zz.ParametersDigit[0].Value != _depth.ValueInt
                || _zz.ParametersDigit[1].Value != _deviation.ValueDecimal
                || _zz.ParametersDigit[2].Value != _backstep.ValueInt
                || _smaFilter.ParametersDigit[0].Value != _smaFilterLength.ValueInt)
            {
                _zz.ParametersDigit[0].Value = _depth.ValueInt;
                _zz.ParametersDigit[1].Value = _deviation.ValueDecimal;
                _zz.ParametersDigit[2].Value = _backstep.ValueInt;
                _smaFilter.ParametersDigit[0].Value = _smaFilterLength.ValueInt;
                _zz.Reload();
                _zz.Save();
                _smaFilter.Reload();
                _smaFilter.Save();
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
        }

        private void StopOrActivateIndicators()
        {
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
            if (_depth.ValueInt >= candles.Count
                || _volumeFilterLength.ValueInt >= candles.Count
                || _smaFilterLength.ValueInt >= candles.Count)
            {
                return;
            }

            List<Position> positions = _tab.PositionsOpenAll;

            decimal last_price = candles[candles.Count - 1].Close;

            decimal zz_up = _zz.DataSeries[2].Last;
            decimal zz_down = _zz.DataSeries[3].Last;

            decimal sma = _smaFilter.DataSeries[0].Last;

            if (positions.Count == 0)
            {// enter logic
                if (zz_up <= zz_down)
                {
                    return;
                }

                if (!BuySignalIsFiltered(candles))
                {
                    if (last_price > zz_up && last_price > sma)
                    {
                        _tab.BuyAtLimit(GetVolume(), last_price + _slippage.ValueDecimal);
                    }
                }

                if (!SellSignalIsFiltered(candles))
                {
                    if (last_price < zz_down && last_price < sma)
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

                if (pos.Direction == Side.Buy)
                {
                    if (last_price < zz_up)
                    {
                        _tab.CloseAtStop(pos, last_price, last_price - _slippage.ValueDecimal);
                    }
                }

                if (pos.Direction == Side.Sell)
                {
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
            decimal last_volume = _volumeFilter.DataSeries[0].Last;
            for (int i = 1; i < _volumeFilterLength.ValueInt; ++i)
            {
                if (_volumeFilter.DataSeries[0].Values[_volumeFilter.DataSeries[0].Values.Count - i - 1] > last_volume)
                {
                    return true;
                }
            }

            return false;
        }

        private bool SellSignalIsFiltered(List<Candle> candles)
        {
            decimal last_volume = _volumeFilter.DataSeries[0].Last;
            for (int i = 1; i < _volumeFilterLength.ValueInt; ++i)
            {
                if (_volumeFilter.DataSeries[0].Values[_volumeFilter.DataSeries[0].Values.Count - i - 1] > last_volume)
                {
                    return true;
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
