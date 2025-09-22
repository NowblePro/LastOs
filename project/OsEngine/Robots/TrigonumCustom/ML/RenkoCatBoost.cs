using OsEngine.Charts.CandleChart;
using OsEngine.Charts.CandleChart.Indicators;
using OsEngine.Entity;
using OsEngine.Indicators;
using OsEngine.OsTrader.Panels;
using OsEngine.OsTrader.Panels.Attributes;
using OsEngine.OsTrader.Panels.Tab;
using OsEngine.Robots.Classes;
using System;
using System.Collections.Generic;
using System.Drawing;

namespace OsEngine.Robots.TrigonumCustom.ML
{

    [Bot("RenkoCatBoost")]
    public class RenkoCatBoost : BotPanelSimple
    {
        private ChartCandleMaster _chartMaster;

        public RenkoCatBoost(string name, StartProgram startProgram) : base(name, startProgram)
        {
            _chartMaster = _tab.GetChartMaster();
        }

        private void _tab_PositionOpeningSuccesEvent(Position obj)
        {
        }

        protected override void ParametersChangedByUser()
        {
        }

        private void StopOrActivateIndicators()
        {
        }

        public override string GetNameStrategyType()
        {
            return "RenkoCatBoost";
        }

        public override void ShowIndividualSettingsDialog()
        {
        }

        // Logic

        protected override List<Func<List<Candle>, bool>> GetCheckers()
        {
            return new List<Func<List<Candle>, bool>>();
        }

        protected override bool CheckOpenLongPosition(List<Candle> candles)
        {
            return false;
        }

        protected override bool CheckOpenShortPosition(List<Candle> candles)
        {
            return false;
        }

        protected override bool CheckClosePosition(List<Candle> candles, Position position)
        {
            return false;
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

        protected override decimal GetVolume(bool getRounded = true)
        {
            decimal volume = 0;

            if (_volumeType.ValueString == CONTRACT_CURRENCY)
            {
                decimal contractPrice = TabsSimple[0].PriceBestAsk;
                volume = _volumeOnPosition.ValueDecimal / contractPrice;

            }
            else if (_volumeType.ValueString == NUMBER_OF_CONTRACTS)
            {
                volume = _volumeOnPosition.ValueDecimal;
            }
            else // if (_volumeType.ValueString == PERCENT)
            {
                volume = _tab.Portfolio.ValueCurrent * (_volumeOnPosition.ValueDecimal / 100) / _tab.PriceBestAsk / _tab.Security.Lot;
            }

            if (getRounded)
            {
                volume = GetRoundedVolume(_tab, volume);
            }

            return volume;
        }
    }
}
