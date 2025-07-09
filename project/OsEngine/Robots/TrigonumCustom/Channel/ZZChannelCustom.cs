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

        private Aindicator _zz;
        private StrategyParameterInt _depth;
        private StrategyParameterDecimal _deviation;
        private StrategyParameterInt _backstep;

        //private Aindicator _volumeFilter;
        //private StrategyParameterInt _volumeFilterLength;

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

            _zz = IndicatorsFactory.CreateIndicatorByName(nameClass: "ZigZagChannelCustom", name: name + "ZigZagChannel", canDelete: false);
            _zz = (Aindicator)_tab.CreateCandleIndicator(_zz, nameArea: "Prime");
            _zz.ParametersDigit[0].Value = _depth.ValueInt;
            _zz.ParametersDigit[1].Value = _deviation.ValueDecimal;
            _zz.ParametersDigit[2].Value = _backstep.ValueInt;
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

            if (_zz.ParametersDigit[0].Value != _depth.ValueInt
                || _zz.ParametersDigit[1].Value != _deviation.ValueDecimal
                || _zz.ParametersDigit[2].Value != _backstep.ValueInt)
            {
                _zz.ParametersDigit[0].Value = _depth.ValueInt;
                _zz.ParametersDigit[1].Value = _deviation.ValueDecimal;
                _zz.ParametersDigit[2].Value = _backstep.ValueInt;
                _zz.Reload();
                _zz.Save();
            }
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
            if (_depth.ValueInt >= candles.Count)
            {
                return;
            }

            List<Position> positions = _tab.PositionsOpenAll;

            if (positions.Count == 0)
            {// enter logic
                decimal high = _zz.DataSeries[0].Last;
                decimal low = _zz.DataSeries[1].Last;
            }
            else
            {//exit logic
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
            return false;
        }

        private bool SellSignalIsFiltered(List<Candle> candles)
        {
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
