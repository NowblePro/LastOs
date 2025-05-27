using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using OsEngine.Charts.CandleChart.Indicators;
using OsEngine.Entity;
using OsEngine.Indicators;
using OsEngine.Market.Servers.TraderNet.Entity;
using OsEngine.OsTrader.Panels;
using OsEngine.OsTrader.Panels.Attributes;
using OsEngine.OsTrader.Panels.Tab;
using OsEngine.Robots.Classes;

namespace OsEngine.Robots.TrigonumCustom.Base
{
    [Bot("Stoch")]
    public class Stoch : BotPanel
    {
        private BotTabSimple tab;

        #region String constants
        private const string NUMBER_OF_CONTRACTS = "Number Of Contracts";
        private const string CONTRACT_CURRENCY = "Contract currency";
        private const string PERCENT = "Percent";
        #endregion

        #region Parameters
        private StrategyParameterInt periodK;
        private StrategyParameterInt periodD;
        private StrategyParameterDecimal overbought;
        private StrategyParameterDecimal oversold;
        private StrategyParameterString regimeString;
        private StrategyParameterString volumeType;
        private StrategyParameterDecimal slippage;
        private StrategyParameterTimeOfDay startTradeTime;
        private StrategyParameterTimeOfDay endTradeTime;
        private StrategyParameterDecimal volumeOnPosition;
        #endregion

        #region Trailing stop
        private TrailingStop trailingStop;
        private StrategyParameterBool TrailingStopIsOn;
        private StrategyParameterString TrailingStopTypeOrder;
        private StrategyParameterDecimal ChangeStepStop;
        private StrategyParameterDecimal MinDist;
        private StrategyParameterDecimal QuantityStepsPrices;
        private StrategyParameterString PointOrPercent;
        #endregion

        #region Indicators
        private StochasticOscillator stochasticOscillator;

        private Aindicator ps;
        private StrategyParameterDecimal step;
        private StrategyParameterDecimal maxStep;
        #endregion

        #region Stochastic fields
        private decimal prevK;
        private decimal prevD;
        private decimal lastSar;
        private StochasticRegime regime = StochasticRegime.Off;
        #endregion

        public Stoch(string name, StartProgram startProgram) : base(name, startProgram)
        {
            TabCreate(BotTabType.Simple);
            tab = TabsSimple[0];

            #region Common parameters init
            volumeType = CreateParameter("Volume Type", NUMBER_OF_CONTRACTS, new string[] { NUMBER_OF_CONTRACTS, CONTRACT_CURRENCY, PERCENT }, "Base");
            slippage = CreateParameter("Slippage", 0.1m, 0.1m, 5, 0.1m, "Base");
            startTradeTime = CreateParameterTimeOfDay("Start trade time", 0, 0, 0, 0, "Base");
            endTradeTime = CreateParameterTimeOfDay("End trade time", 24, 0, 0, 0, "Base");
            volumeOnPosition = CreateParameter("Volume", 10, 1.0m, 50, 4, "Base");
            #endregion

            #region Stochastic parameters init
            regimeString = CreateParameter("Regime", StochasticRegime.Off.ToString(), Enum.GetNames(typeof(StochasticRegime)), "Stochastic");
            periodK = CreateParameter("PeriodK", 14, 2, 50, 1, "Stochastic");
            periodD = CreateParameter("PeriodD", 3, 2, 50, 1, "Stochastic");
            overbought = CreateParameter("Overbuy", 20m, 1, 40, 1, "Stochastic");
            oversold = CreateParameter("OverSell", 80m, 60, 99, 1, "Stochastic");
            #endregion

            #region Indicators init
            stochasticOscillator = tab.Indicators.Where(i => i is StochasticOscillator).FirstOrDefault() as StochasticOscillator;
            if (stochasticOscillator == null)
            {
                stochasticOscillator = new StochasticOscillator(false);
                stochasticOscillator = (StochasticOscillator)tab.CreateCandleIndicator(stochasticOscillator, nameArea: "Stochastic");
            }

            ps = IndicatorsFactory.CreateIndicatorByName(nameClass: "ParabolicSAR", name: name + "Parabolic", canDelete: false);
            ps = (Aindicator)tab.CreateCandleIndicator(ps, nameArea: "Prime");
            step = CreateParameter("Step", 0.02m, 0.001m, 3, 0.001m, "Robot parameters");
            maxStep = CreateParameter("MaxStep", 0.2m, 0.01m, 1, 0.01m, "Robot parameters");
            #endregion

            #region Trailing init
            TrailingStopIsOn = CreateParameter("Is Trailing stop On", false, "Trailing Stop");
            TrailingStopTypeOrder = CreateParameter("Type order", OrderPriceType.Market.ToString(), new[] { OrderPriceType.Market.ToString(), OrderPriceType.Limit.ToString() }, "Trailing Stop");
            PointOrPercent = CreateParameter("Choise Points or Percent", "Points", new[] { "Points", "Percent" }, "Trailing Stop");
            ChangeStepStop = CreateParameter("Stop level change step", 1, 1, 10000, 001m, "Trailing Stop");
            MinDist = CreateParameter("Minimum distance to price", 1, 1, 10000, 0.01m, "Trailing Stop");
            QuantityStepsPrices = CreateParameter("Quantity steps prices for limit order", 0m, 0, 10000, 1, "Trailing Stop");
            #endregion

            UpdateParameters();

            tab.CandleFinishedEvent += Tab_CandleFinishedEvent;
            tab.PositionOpeningSuccesEvent += Tab_PositionOpeningSuccesEvent;

            ParametrsChangeByUser += Stoch_ParametrsChangeByUser;
        }

        private void Stoch_ParametrsChangeByUser()
        {
            UpdateParameters();
        }

        private void UpdateParameters()
        {
            SetTrailingParameters();
            SetStochasticParameters();
            SetAIndicatorParameters();
        }

        private void SetStochasticParameters()
        {
            stochasticOscillator.P1 = periodK.ValueInt;
            stochasticOscillator.P2 = periodD.ValueInt;
            stochasticOscillator.P3 = periodD.ValueInt;

            if (Enum.TryParse(regimeString.ValueString, out StochasticRegime regime))
            {
                this.regime = regime;
            }
        }

        private void SetAIndicatorParameters()
        {
            if (ps.ParametersDigit[0].Value != step.ValueDecimal ||
                ps.ParametersDigit[1].Value != maxStep.ValueDecimal)
            {
                ps.ParametersDigit[0].Value = step.ValueDecimal;
                ps.ParametersDigit[1].Value = maxStep.ValueDecimal;
                ps.Save();
                ps.Reload();
            }
        }

        private void SetTrailingParameters()
        {
            if (TrailingStopIsOn.ValueBool)
            {
                trailingStop = new TrailingStop(tab, TrailingStopTypeOrder.ValueString, ChangeStepStop.ValueDecimal, MinDist.ValueDecimal, QuantityStepsPrices.ValueDecimal, PointOrPercent.ValueString);
            }
        }

        private void Tab_PositionOpeningSuccesEvent(Position obj)
        {
            tab.SellAtStopCancel();
            tab.BuyAtStopCancel();

            if (TrailingStopIsOn.ValueBool)
            {
                trailingStop?.SetTrailingStop(obj.EntryPrice);
            }
        }

        private void Tab_CandleFinishedEvent(List<Candle> candles)
        {
            if (startTradeTime.Value > tab.TimeServerCurrent ||
            endTradeTime.Value < tab.TimeServerCurrent)
            {
                CancelStopsAndProfits();
                return;
            }

            if (candles.Count < periodK.ValueInt) return;
            lastSar = ps.DataSeries[0].Last;

            if (lastSar == 0)
            {
                return;
            }
            List<Position> positions = tab.PositionsOpenAll;
            decimal lastPrice = candles.Last().Close;
            decimal K = stochasticOscillator.ValuesUp.Last();
            decimal D = stochasticOscillator.ValuesDown.Last();

            if (positions.Count == 0)
            {
                decimal slippage = this.slippage.ValueDecimal * lastSar / 100;
                if (!BuySignalIsFiltered(candles, K, D))
                {
                    tab.BuyAtStopCancel();
                    tab.BuyAtStop(GetVolume(), lastSar + slippage, lastSar, StopActivateType.HigherOrEqual, 1);
                }
                if (!SellSignalIsFiltered(candles, K, D))
                {
                    tab.SellAtStopCancel();
                    tab.SellAtStop(GetVolume(), lastSar - slippage, lastSar, StopActivateType.LowerOrEqual, 1);
                }
            }
            else
            {
                if (TrailingStopIsOn.ValueBool)
                {
                    trailingStop.SetTrailingStop(candles.Last().Close);
                    return;
                }

                for (int i = 0; i < positions.Count; i++)
                {
                    tab.SellAtStopCancel();
                    tab.BuyAtStopCancel();
                    Position pos = positions[i];

                    if (pos.CloseActiv == true && pos.CloseOrders != null && pos.CloseOrders.Count > 0)
                    {
                        return;
                    }

                    decimal priceLine = lastSar;
                    decimal priceOrder = lastSar;
                    decimal _slippage = slippage.ValueDecimal * priceOrder / 100;

                    if (pos.Direction == Side.Buy)
                    {
                        tab.CloseAtStop(pos, priceLine, priceOrder - _slippage);
                    }
                    else if (pos.Direction == Side.Sell)
                    {
                        tab.CloseAtStop(pos, priceLine, priceOrder + _slippage);
                    }
                }
            }
            prevK = K;
            prevD = D;
        }

        private bool BuySignalIsFiltered(List<Candle> candles, decimal k, decimal d)
        {
            if ((regime & StochasticRegime.OnlyLong) == 0)
            {
                return true;
            }

            decimal treshold = overbought.ValueDecimal;
            if (prevK > treshold || prevD > treshold) return true;
            if (prevD > prevK && d < k)
            {
                return false;
            }
            return true;
        }

        private void CancelStopsAndProfits()
        {
            List<Position> positions = tab.PositionsOpenAll;

            for (int i = 0; i < positions.Count; i++)
            {
                Position pos = positions[i];

                pos.StopOrderIsActiv = false;
                pos.ProfitOrderIsActiv = false;
            }

            tab.BuyAtStopCancel();
            tab.SellAtStopCancel();
        }

        private bool SellSignalIsFiltered(List<Candle> candles, decimal k, decimal d)
        {
            if ((regime & StochasticRegime.OnlyShort) == 0)
            {
                return true;
            }

            decimal treshold = oversold.ValueDecimal;
            if (prevK < treshold || prevD < treshold) return true;
            if (prevD < prevK && d > k)
            {
                return false;
            }
            return true;
        }

        private decimal GetVolume()
        {
            decimal volume = 0;

            if (volumeType.ValueString == CONTRACT_CURRENCY)
            {
                decimal contractPrice = TabsSimple[0].PriceBestAsk;
                volume = volumeOnPosition.ValueDecimal / contractPrice;

            }
            else if (volumeType.ValueString == NUMBER_OF_CONTRACTS)
            {
                volume = volumeOnPosition.ValueDecimal;
            }
            else if (volumeType.ValueString == PERCENT)
            {
                volume = tab.Portfolio.ValueCurrent * (volumeOnPosition.ValueDecimal / 100) / tab.PriceBestAsk / tab.Security.Lot;
            }

            if (StartProgram == StartProgram.IsTester)
            {
                volume = Math.Round(volume, 6);
            }
            else
            {
                volume = Math.Round(volume, tab.Security.DecimalsVolume);
            }
            return volume;
        }

        public override string GetNameStrategyType() => $"{nameof(Stoch)}";

        public override void ShowIndividualSettingsDialog() { }
    }

    enum StochasticRegime { Off, OnlyLong, OnlyShort, On }
}
