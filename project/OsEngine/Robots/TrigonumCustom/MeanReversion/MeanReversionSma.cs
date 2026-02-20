using OsEngine.Entity;
using OsEngine.Indicators;
using OsEngine.Market.Servers.TraderNet.Entity;
using OsEngine.OsTrader.Panels;
using OsEngine.OsTrader.Panels.Attributes;
using OsEngine.OsTrader.Panels.Tab;
using System.Collections.Generic;
using System.Drawing;
using System.Security.Cryptography;
using System.Windows.Media.Converters;

namespace OsEngine.Robots.TrigonumCustom.Base
{

    [Bot("MeanReversionSma")]
    class MeanReversionSma : BotPanel
    {
        BotTabSimple _tab;

        StrategyParameterString _regime;
        private StrategyParameterDecimal _volumeOnPosition;
        private StrategyParameterString _volumeRegime;
        private StrategyParameterDecimal _slippage;

        private StrategyParameterTimeOfDay TimeStart;
        private StrategyParameterTimeOfDay TimeEnd;

        private StrategyParameterBool _saveJson;

        private Aindicator _sma;
        private Aindicator _atr;

        private StrategyParameterString _averagingAlgorithm;

        private StrategyParameterInt _periodSma;
        private StrategyParameterDecimal _rPercent;
        private StrategyParameterDecimal _atrMultiplier;
        private StrategyParameterInt _periodAtr;
        private StrategyParameterDecimal _k;

        private AvgMode _avgMode;

        public MeanReversionSma(string name, StartProgram startProgram) : base(name, startProgram)
        {
            TabCreate(BotTabType.Simple);
            _tab = TabsSimple[0];

            _tab.PositionSellAtStopActivateEvent += _tab_PositionSellAtStopActivateEvent;
            _tab.PositionBuyAtStopActivateEvent += _tab_PositionBuyAtStopActivateEvent;

            _regime = CreateParameter("Regime", "Off", new[] { "Off", "On", "OnlyLong", "OnlyShort", "OnlyClosePosition" }, "Base");
            _volumeRegime = CreateParameter("Volume type", "Number of contracts", new[] { "Number of contracts", "Contract currency", "% of the total portfolio" }, "Base");
            _volumeOnPosition = CreateParameter("Volume", 10, 1.0m, 50, 4, "Base");
            _slippage = CreateParameter("Slippage %", 0m, 0, 20, 1, "Base");

            TimeStart = CreateParameterTimeOfDay("Start Trade Time", 0, 0, 0, 0, "Base");
            TimeEnd = CreateParameterTimeOfDay("End Trade Time", 24, 0, 0, 0, "Base");

            _saveJson = CreateParameter("Save Json Data", false, "Base");

            _averagingAlgorithm = CreateParameter("Averaging algorithm", "DCA grid",
                new[] { "DCA grid", "Volatility grid", "Linear averaging" }, "Robot parameters");

            _periodSma = CreateParameter("SMA period", 20, 20, 400, 1, "Robot parameters");
            _rPercent = CreateParameter("Deviation percent (r%)", 0.5m, 0.1m, 3.0m, 0.1m, "Robot parameters");
            _atrMultiplier = CreateParameter("ATR multiplier", 1.0m, 0.4m, 3.0m, 0.2m, "Robot parameters");
            _periodAtr = CreateParameter("ATR period", 14, 20, 400, 1, "Robot parameters");
            _k = CreateParameter("Linear averaging percent (k%)", 1.2m, 1.1m, 1.6m, 0.1m, "Robot parameters");

            _sma = IndicatorsFactory.CreateIndicatorByName(nameClass: "SmaCustom", name: name + "SmaCustom", canDelete: false);
            _sma = (Aindicator)_tab.CreateCandleIndicator(_sma, nameArea: "Prime");
            _sma.ParametersDigit[0].Value = _periodSma.ValueInt;
            _sma.DataSeries[0].Color = Color.Red;
            _sma.Save();

            _atr = IndicatorsFactory.CreateIndicatorByName(nameClass: "ATR", name: name + "ATR", canDelete: false);
            _atr = (Aindicator)_tab.CreateCandleIndicator(_atr, nameArea: "ATR Area");
            ((IndicatorParameterInt)_atr.Parameters[0]).ValueInt = _periodAtr.ValueInt;
            _atr.DataSeries[0].Color = Color.Blue;
            _atr.Save();

            StopOrActivateIndicators();
            _tab.CandleFinishedEvent += _tab_CandleFinishedEvent;
            ParametrsChangeByUser += MRS_ParametrsChangeByUser;
            MRS_ParametrsChangeByUser();
        }

        private void MRS_ParametrsChangeByUser()
        {
            StopOrActivateIndicators();

            _tab.setSaveData(_saveJson.ValueBool);

            if (_sma.ParametersDigit[0].Value != _periodSma.ValueInt)
            {
                _sma.ParametersDigit[0].Value = _periodSma.ValueInt;
                _sma.Reload();
                _sma.Save();
            }

            if (_averagingAlgorithm.ValueString == "DCA grid")
            {
                _avgMode = AvgMode.DCA;
            }
            else if (_averagingAlgorithm.ValueString == "Volatility grid")
            {
                _avgMode = AvgMode.Volatility;
            }
            else
            {
                _avgMode = AvgMode.Linear;
            }
        }

        private void StopOrActivateIndicators()
        {
        }

        public override string GetNameStrategyType()
        {
            return "MeanReversionSma";
        }

        public override void ShowIndividualSettingsDialog()
        {

        }

        // Logic
        private void _tab_CandleFinishedEvent(List<Candle> candles)
        {
            if (_regime.ValueString == "Off") { return; }

            if (TimeStart.Value > _tab.TimeServerCurrent ||
                TimeEnd.Value < _tab.TimeServerCurrent)
            {
                CancelStopsAndProfits();
                return;
            }

            if (candles.Count < _periodSma.ValueInt) { return; }

            decimal lastMa = _sma.DataSeries[0].Last;

            List<Position> positions = _tab.PositionsOpenAll;

            if (positions.Count == 0)
            {
                // enter logic
                
                if (_regime.ValueString == "On" ||
                    _regime.ValueString == "OnlyLong")
                {
                    decimal buyPrice;
                    decimal buySlippage;

                    if (_avgMode == AvgMode.Volatility)
                    {
                        decimal lastAtr = _atr.DataSeries[0].Last;
                        buyPrice = lastMa - (positions.Count + 1) * _atrMultiplier.ValueDecimal * lastAtr;
                        buySlippage = buyPrice * (1 + _slippage.ValueDecimal / 100);
                    }
                    else
                    {
                        buyPrice = lastMa * (1 - _rPercent.ValueDecimal / 100);
                        buySlippage = buyPrice * (1 + _slippage.ValueDecimal / 100);
                    }
                    _tab.BuyAtStop(GetVolume(), buyPrice, buySlippage, StopActivateType.LowerOrEqual, 1);
                }

                if (_regime.ValueString == "On" ||
                    _regime.ValueString == "OnlyShort")
                {
                    decimal sellPrice;
                    decimal sellSlippage;

                    if (_avgMode == AvgMode.Volatility)
                    {
                        decimal lastAtr = _atr.DataSeries[0].Last;
                        sellPrice = lastMa + (positions.Count + 1) * _atrMultiplier.ValueDecimal * lastAtr;
                        sellSlippage = sellPrice * (1 - _slippage.ValueDecimal / 100);
                    }
                    else
                    {
                        sellPrice = lastMa * (1 + _rPercent.ValueDecimal / 100);
                        sellSlippage = sellPrice * (1 - _slippage.ValueDecimal / 100);
                    }

                    _tab.SellAtStop(GetVolume(), sellPrice, sellSlippage, StopActivateType.HigherOrEqual, 1);
                }
            }
            else
            {
                //exit logic
                if (positions[0].Direction == Side.Buy)
                {
                    for (int i = 0; i < positions.Count; i++)
                    {
                        decimal closeSlippage = lastMa * (1 - _slippage.ValueDecimal / 100);

                        _tab.CloseAtProfit(positions[i], lastMa, closeSlippage);
                    }
                }
                else if (positions[0].Direction == Side.Sell)
                {
                    for (int i = 0; i < positions.Count; i++)
                    {
                        decimal closeSlippage = lastMa * (1 + _slippage.ValueDecimal / 100);

                        _tab.CloseAtProfit(positions[i], lastMa, closeSlippage);
                    }
                }

                switch (_avgMode)
                {
                    case AvgMode.DCA:
                        HandleDCA(positions);
                        break;
                    case AvgMode.Volatility:
                        HandleVolatility(positions);
                        break;
                    case AvgMode.Linear:
                        HandleLinear(positions);
                        break;
                    default:
                        break;
                }
            }
        }

        private void HandleDCA(List<Position> positions)
        {
            decimal lastMa = _sma.DataSeries[0].Last;

            if (positions[0].Direction == Side.Buy)
            {
                if (positions.Count < 20)
                {
                    decimal buyPrice = lastMa * (1 - (_rPercent.ValueDecimal * positions.Count + 1) / 100);
                    decimal buySlippage = buyPrice * (1 + _slippage.ValueDecimal / 100);

                    _tab.BuyAtStop(GetVolume(), buyPrice, buySlippage, StopActivateType.LowerOrEqual, 1);
                }
            }
            else if (positions[0].Direction == Side.Sell)
            {
                if (positions.Count < 20)
                {
                    decimal sellPrice = lastMa * (1 + (_rPercent.ValueDecimal * positions.Count + 1) / 100);
                    decimal sellSlippage = sellPrice * (1 - _slippage.ValueDecimal / 100);

                    _tab.SellAtStop(GetVolume(), sellPrice, sellSlippage, StopActivateType.HigherOrEqual, 1);
                }
            }
        }

        private void HandleVolatility(List<Position> positions)
        {
            decimal lastMa = _sma.DataSeries[0].Last;
            decimal lastAtr = _atr.DataSeries[0].Last;

            if (positions[0].Direction == Side.Buy)
            {
                if (positions.Count < 20)
                {
                    decimal buyPrice = lastMa - (positions.Count + 1) * _atrMultiplier.ValueDecimal * lastAtr;
                    decimal buySlippage = buyPrice * (1 + _slippage.ValueDecimal / 100);

                    _tab.BuyAtStop(GetVolume(), buyPrice, buySlippage, StopActivateType.LowerOrEqual, 1);
                }
            }
            else if (positions[0].Direction == Side.Sell)
            {
                if (positions.Count < 20)
                {
                    decimal sellPrice = lastMa + (positions.Count + 1) * _atrMultiplier.ValueDecimal * lastAtr;
                    decimal sellSlippage = sellPrice * (1 - _slippage.ValueDecimal / 100);

                    _tab.SellAtStop(GetVolume(), sellPrice, sellSlippage, StopActivateType.HigherOrEqual, 1);
                }
            }
        }

        private void HandleLinear(List<Position> positions)
        {
            decimal lastMa = _sma.DataSeries[0].Last;

            decimal volume = positions[0].OpenVolume * (1 + positions.Count * _k.ValueDecimal / 100);
            volume = GetRoundedVolume(_tab, volume);

            if (positions[0].Direction == Side.Buy)
            {
                if (positions.Count < 20)
                {
                    decimal buyPrice = lastMa * (1 - (_rPercent.ValueDecimal * positions.Count + 1) / 100);
                    decimal buySlippage = buyPrice * (1 + _slippage.ValueDecimal / 100);

                    _tab.BuyAtStop(volume, buyPrice, buySlippage, StopActivateType.LowerOrEqual, 1);
                }
            }
            else if (positions[0].Direction == Side.Sell)
            {
                if (positions.Count < 20)
                {
                    decimal sellPrice = lastMa * (1 + (_rPercent.ValueDecimal * positions.Count + 1) / 100);
                    decimal sellSlippage = sellPrice * (1 - _slippage.ValueDecimal / 100);

                    _tab.SellAtStop(volume, sellPrice, sellSlippage, StopActivateType.HigherOrEqual, 1);
                }
            }
        }

        private void _tab_PositionBuyAtStopActivateEvent(Position pos)
        {
            List<Position> positions = _tab.PositionsOpenAll;

            if (positions.Count == 1)
            {
                CancelStopsAndProfits();
            }

            decimal lastMa = _sma.DataSeries[0].Last;
            decimal closeSlippage = lastMa * (1 - _slippage.ValueDecimal / 100);

            _tab.CloseAtProfit(pos, lastMa, closeSlippage);

            switch (_avgMode)
            {
                case AvgMode.DCA:
                    HandleDCA(positions);
                    break;
                case AvgMode.Volatility:
                    HandleVolatility(positions);
                    break;
                case AvgMode.Linear:
                    HandleLinear(positions);
                    break;
                default:
                    break;
            }
        }

        private void _tab_PositionSellAtStopActivateEvent(Position pos)
        {
            List<Position> positions = _tab.PositionsOpenAll;

            if (positions.Count == 1)
            {
                CancelStopsAndProfits();
            }

            decimal lastMa = _sma.DataSeries[0].Last;
            decimal closeSlippage = lastMa * (1 + _slippage.ValueDecimal / 100);

            _tab.CloseAtProfit(pos, lastMa, closeSlippage);

            switch (_avgMode)
            {
                case AvgMode.DCA:
                    HandleDCA(positions);
                    break;
                case AvgMode.Volatility:
                    HandleVolatility(positions);
                    break;
                case AvgMode.Linear:
                    HandleLinear(positions);
                    break;
                default:
                    break;
            }
        }

        private bool BuySignalIsFiltered(List<Candle> candles)
        {
            return true;
        }

        private bool SellSignalIsFiltered(List<Candle> candles)
        {
            return true;
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

        private decimal GetVolume(bool getRounded = true)
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
            else //if (VolumeRegime.ValueString == "% of the total portfolio")
            {
                volume = _tab.Portfolio.ValueCurrent * (_volumeOnPosition.ValueDecimal / 100) / _tab.PriceBestAsk / _tab.Security.Lot;
            }

            if (getRounded)
            {
                volume = GetRoundedVolume(_tab, volume);
            }

            return volume;
        }

        private enum AvgMode
        {
            None = 0,
            DCA = 1,
            Volatility = 2,
            Linear = 3
        }
    }

}
