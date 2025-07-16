using OsEngine.Entity;
using OsEngine.Indicators;
using OsEngine.OsTrader.Panels;
using OsEngine.OsTrader.Panels.Attributes;
using OsEngine.OsTrader.Panels.Tab;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Security.Cryptography;

namespace OsEngine.Robots.TrigonumCustom.Testing
{
    [Bot("FrequentOrders")]
    class FrequentOrders : BotPanel
    {
        BotTabSimple _tab;

        StrategyParameterString Regime;
        public StrategyParameterDecimal VolumeOnPosition;
        public StrategyParameterString VolumeRegime;

        private StrategyParameterTimeOfDay TimeStart;
        private StrategyParameterTimeOfDay TimeEnd;

        private StrategyParameterBool _saveJson;

        public Aindicator _sma;
        public StrategyParameterInt _periodSma;

        public FrequentOrders(string name, StartProgram startProgram) : base(name, startProgram)
        {
            TabCreate(BotTabType.Simple);
            _tab = TabsSimple[0];

            Regime = CreateParameter("Regime", "Off", new[] { "Off", "On", "OnlyLong", "OnlyShort", "OnlyClosePosition" }, "Base");
            VolumeRegime = CreateParameter("Volume type", "Number of contracts", new[] { "Number of contracts", "Contract currency", "% of the total portfolio" }, "Base");
            VolumeOnPosition = CreateParameter("Volume", 10, 1.0m, 50, 4, "Base");

            TimeStart = CreateParameterTimeOfDay("Start Trade Time", 0, 0, 0, 0, "Base");
            TimeEnd = CreateParameterTimeOfDay("End Trade Time", 24, 0, 0, 0, "Base");

            _saveJson = CreateParameter("Save Json Data", false, "Base");

            _periodSma = CreateParameter("fast SMA period", 250, 50, 500, 50, "Robot parameters");

            _sma = IndicatorsFactory.CreateIndicatorByName(nameClass: "Sma", name: name + "Sma", canDelete: false);
            _sma = (Aindicator)_tab.CreateCandleIndicator(_sma, nameArea: "Prime");
            _sma.ParametersDigit[0].Value = _periodSma.ValueInt;
            _sma.DataSeries[0].Color = Color.Red;
            _sma.Save();

            StopOrActivateIndicators();
            _tab.CandleFinishedEvent += _tab_CandleFinishedEvent;
            ParametrsChangeByUser += LRegBot_ParametrsChangeByUser;
            LRegBot_ParametrsChangeByUser();
        }

        private void LRegBot_ParametrsChangeByUser()
        {
            StopOrActivateIndicators();

            _tab.setSaveData(_saveJson.ValueBool);

            if (_sma.ParametersDigit[0].Value != _periodSma.ValueInt)
            {
                _sma.ParametersDigit[0].Value = _periodSma.ValueInt;
                _sma.Reload();
                _sma.Save();
            }
        }

        private void StopOrActivateIndicators()
        {
        }

        public override string GetNameStrategyType()
        {
            return "FrequentOrders";
        }

        public override void ShowIndividualSettingsDialog()
        {

        }

        // Logic
        private void _tab_CandleFinishedEvent(List<Candle> candles)
        {
            if (Regime.ValueString == "Off") { return; }

            if (TimeStart.Value > _tab.TimeServerCurrent ||
                TimeEnd.Value < _tab.TimeServerCurrent)
            {
                CancelStopsAndProfits();
                return;
            }

            if (candles.Count < _periodSma.ValueInt) { return; }

            List<Position> positions = _tab.PositionsOpenAll;

            if (positions.Count == 0)
            {
                // enter logic
                if (!BuySignalIsFiltered(candles))
                {
                    _tab.BuyAtMarket(GetVolume());
                }

                if (!SellSignalIsFiltered(candles))
                {
                    _tab.SellAtMarket(GetVolume());
                }
            }
            else
            {
                //exit logic
                for (int i = 0; i < positions.Count; i++)
                {
                    if (positions[i].State == PositionStateType.ClosingFail)
                    {
                        _tab.CloseAtMarket(positions[i], positions[i].OpenVolume);
                        continue;
                    }

                    if (positions[i].State != PositionStateType.Open) { continue; }

                    // logic to reverse long position
                    if (positions[i].Direction == Side.Buy)
                    {
                        _tab.CloseAtMarket(positions[i], positions[i].OpenVolume);

                        continue;
                    }

                    // logic to reverse short position
                    if (positions[i].Direction == Side.Sell)
                    {
                        _tab.CloseAtMarket(positions[i], positions[i].OpenVolume);

                        continue;
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
            decimal lastMa = _sma.DataSeries[0].Last;
            decimal lastPrice = candles[candles.Count - 1].Close;

            if (lastMa < lastPrice)
            {
                return true;
            }

            return false;
        }

        private bool SellSignalIsFiltered(List<Candle> candles)
        {
            decimal lastMa = _sma.DataSeries[0].Last;
            decimal lastPrice = candles[candles.Count - 1].Close;

            if (lastMa > lastPrice)
            {
                return true;
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
