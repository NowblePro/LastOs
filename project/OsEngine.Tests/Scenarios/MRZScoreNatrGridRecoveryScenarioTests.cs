using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using OsEngine.Entity;
using OsEngine.Robots.TrigonumCustom.Base;
using OsEngine.Tests.Helpers;

namespace OsEngine.Tests.Scenarios
{
    [TestFixture]
    [Category("Scenario")]
    [Category("Natr")]
    public class MRZScoreNatrGridRecoveryScenarioTests
    {
        [Test]
        public void TryBuildPendingGrid_BuildsExpectedBuyLevels_ForMarketNextOpenCandleScenario()
        {
            MRZScoreNatrGrid robot = MRZScoreNatrGridTestFactory.CreateConfiguredGridRobot(seriesVolumeMultiplier: 1m);
            List<Candle> candles = new List<Candle>
            {
                CreateFinishedCandle(new DateTime(2026, 5, 19, 12, 0, 0), 95m, 96m, 94m, 95m)
            };

            MRZScoreNatrGridTestFactory.InvokePrivate(robot, "TryBuildPendingGrid", candles);

            IList gridLevels = (IList)MRZScoreNatrGridTestFactory.GetField(robot, "_gridLevels");
            Assert.That(gridLevels.Count, Is.EqualTo(3));
            AssertLevel(gridLevels[0], expectedIndex: 2, expectedPrice: 94.68421052631578947368421052m, expectedVolume: 10m, expectedDeviation: 5.315789m);
            AssertLevel(gridLevels[1], expectedIndex: 3, expectedPrice: 92.68421052631578947368421053m, expectedVolume: 15m, expectedDeviation: 7.315789m);
            AssertLevel(gridLevels[2], expectedIndex: 4, expectedPrice: 90.68421052631578947368421052m, expectedVolume: 22.5m, expectedDeviation: 9.315789m);

            Assert.That(MRZScoreNatrGridTestFactory.GetField(robot, "_gridSide").ToString(), Is.EqualTo("Buy"));
            Assert.That((decimal)MRZScoreNatrGridTestFactory.GetField(robot, "_gridSma"), Is.EqualTo(100m));
            Assert.That((decimal)MRZScoreNatrGridTestFactory.GetField(robot, "_gridThresholdPercent"), Is.EqualTo(2m));
            Assert.That((decimal)MRZScoreNatrGridTestFactory.GetField(robot, "_gridNatrPercent"), Is.EqualTo(1m / 95m * 100m).Within(0.000001m));
        }

        [Test]
        public void TryBuildPendingGrid_BuildsExpectedSellLevels_ForMarketNextOpenCandleScenario()
        {
            MRZScoreNatrGrid robot = MRZScoreNatrGridTestFactory.CreateConfiguredGridRobot(
                seriesVolumeMultiplier: 1m,
                lowChannel: 98m,
                highChannel: 102m);

            List<Candle> candles = new List<Candle>
            {
                CreateFinishedCandle(new DateTime(2026, 5, 19, 12, 15, 0), 105m, 106m, 104m, 105m)
            };

            MRZScoreNatrGridTestFactory.InvokePrivate(robot, "TryBuildPendingGrid", candles);

            IList gridLevels = (IList)MRZScoreNatrGridTestFactory.GetField(robot, "_gridLevels");
            Assert.That(gridLevels.Count, Is.EqualTo(3));
            AssertLevel(gridLevels[0], expectedIndex: 2, expectedPrice: 105.1904761904761904761904762m, expectedVolume: 10m, expectedDeviation: 5.190476m);
            AssertLevel(gridLevels[1], expectedIndex: 3, expectedPrice: 107.1904761904761904761904762m, expectedVolume: 15m, expectedDeviation: 7.190476m);
            AssertLevel(gridLevels[2], expectedIndex: 4, expectedPrice: 109.1904761904761904761904762m, expectedVolume: 22.5m, expectedDeviation: 9.190476m);

            Assert.That(MRZScoreNatrGridTestFactory.GetField(robot, "_gridSide").ToString(), Is.EqualTo("Sell"));
        }

        [Test]
        public void TryBuildPendingGrid_AppliesRecoveryMultiplier_ToFirstSeriesVolume()
        {
            MRZScoreNatrGrid robot = MRZScoreNatrGridTestFactory.CreateConfiguredGridRobot(seriesVolumeMultiplier: 1.5m);
            List<Candle> candles = new List<Candle>
            {
                CreateFinishedCandle(new DateTime(2026, 5, 19, 12, 0, 0), 95m, 96m, 94m, 95m)
            };

            MRZScoreNatrGridTestFactory.InvokePrivate(robot, "TryBuildPendingGrid", candles);

            IList gridLevels = (IList)MRZScoreNatrGridTestFactory.GetField(robot, "_gridLevels");
            Assert.That(gridLevels.Count, Is.EqualTo(3));
            AssertLevel(gridLevels[0], expectedIndex: 2, expectedPrice: 94.68421052631578947368421052m, expectedVolume: 15m, expectedDeviation: 5.315789m);
            AssertLevel(gridLevels[1], expectedIndex: 3, expectedPrice: 92.68421052631578947368421053m, expectedVolume: 22.5m, expectedDeviation: 7.315789m);
            AssertLevel(gridLevels[2], expectedIndex: 4, expectedPrice: 90.68421052631578947368421052m, expectedVolume: 33.75m, expectedDeviation: 9.315789m);
            Assert.That((bool)MRZScoreNatrGridTestFactory.GetField(robot, "_currentGridRecoveryBoostActive"), Is.True);
        }

        [Test]
        public void TryScheduleTriggeredMarketNextOpenLevels_SchedulesOnlyTouchedLevels()
        {
            MRZScoreNatrGrid robot = MRZScoreNatrGridTestFactory.CreateConfiguredGridRobot(seriesVolumeMultiplier: 1m);
            Type levelType = MRZScoreNatrGridTestFactory.GetGridLevelStateType();
            IList gridLevels = MRZScoreNatrGridTestFactory.CreateGridLevelsList(levelType);

            object level1 = MRZScoreNatrGridTestFactory.CreateGridLevel(levelType, 2, false);
            MRZScoreNatrGridTestFactory.SetField(level1, "Price", 94.5m);
            MRZScoreNatrGridTestFactory.SetField(level1, "DeviationPercent", 5m);

            object level2 = MRZScoreNatrGridTestFactory.CreateGridLevel(levelType, 3, false);
            MRZScoreNatrGridTestFactory.SetField(level2, "Price", 92m);
            MRZScoreNatrGridTestFactory.SetField(level2, "DeviationPercent", 7m);

            gridLevels.Add(level1);
            gridLevels.Add(level2);

            MRZScoreNatrGridTestFactory.SetField(robot, "_gridLevels", gridLevels);
            MRZScoreNatrGridTestFactory.SetField(robot, "_gridSide", Side.Buy);

            Candle triggerCandle = CreateFinishedCandle(new DateTime(2026, 5, 19, 13, 0, 0), 95m, 96m, 94m, 95m);

            MRZScoreNatrGridTestFactory.InvokePrivate(robot, "TryScheduleTriggeredMarketNextOpenLevels", triggerCandle);

            Assert.That((bool)MRZScoreNatrGridTestFactory.GetField(level1, "PendingNextOpenFill"), Is.True);
            Assert.That((DateTime)MRZScoreNatrGridTestFactory.GetField(level1, "PendingNextOpenSignalTime"), Is.EqualTo(triggerCandle.TimeStart));
            Assert.That((bool)MRZScoreNatrGridTestFactory.GetField(level2, "PendingNextOpenFill"), Is.False);
        }

        [Test]
        public void PositionStartOpeningSuccess_BindsImmediateLevel_AndRegistersBinding()
        {
            MRZScoreNatrGrid robot = MRZScoreNatrGridTestFactory.CreateConfiguredGridRobot(seriesVolumeMultiplier: 1m);
            Type levelType = MRZScoreNatrGridTestFactory.GetGridLevelStateType();
            object level = MRZScoreNatrGridTestFactory.CreateGridLevel(levelType, 2, false);
            MRZScoreNatrGridTestFactory.SetField(level, "Price", 94.5m);
            MRZScoreNatrGridTestFactory.SetField(robot, "_levelAwaitingImmediateBinding", level);

            Position position = CreatePosition(101, Side.Buy, 94.5m, null);

            MRZScoreNatrGridTestFactory.InvokePrivate(robot, "_tab_PositionStartOpeningSuccessEvent", position);

            Assert.That(MRZScoreNatrGridTestFactory.GetField(level, "Position"), Is.SameAs(position));

            IDictionary bindingsByNumber = (IDictionary)MRZScoreNatrGridTestFactory.GetField(robot, "_levelBindingsByNumber");
            Assert.That(bindingsByNumber.Contains(101), Is.True);
            Assert.That(bindingsByNumber[101], Is.SameAs(level));
        }

        [Test]
        public void PositionOpeningSuccess_BindsLevelBySignalType_AndConsumesRecovery()
        {
            MRZScoreNatrGrid robot = MRZScoreNatrGridTestFactory.CreateConfiguredGridRobot(seriesVolumeMultiplier: 1.5m);
            Type levelType = MRZScoreNatrGridTestFactory.GetGridLevelStateType();
            IList gridLevels = MRZScoreNatrGridTestFactory.CreateGridLevelsList(levelType);
            IList awaitingLevels = MRZScoreNatrGridTestFactory.CreateGridLevelsList(levelType);

            object level = MRZScoreNatrGridTestFactory.CreateGridLevel(levelType, 2, false);
            MRZScoreNatrGridTestFactory.SetField(level, "Price", 94.5m);
            MRZScoreNatrGridTestFactory.SetField(level, "Volume", 15m);
            MRZScoreNatrGridTestFactory.SetField(level, "DeviationPercent", 5m);
            gridLevels.Add(level);
            awaitingLevels.Add(level);

            MRZScoreNatrGridTestFactory.SetField(robot, "_gridLevels", gridLevels);
            MRZScoreNatrGridTestFactory.SetField(robot, "_levelsAwaitingOpeningSuccess", awaitingLevels);
            MRZScoreNatrGridTestFactory.SetField(robot, "_currentGridRecoveryBoostActive", true);
            MRZScoreNatrGridTestFactory.SetField(robot, "_recoverySeriesRemaining", 1);

            Position position = CreatePosition(202, Side.Buy, 94.5m, "MRNatrLevel:2");

            MRZScoreNatrGridTestFactory.InvokePrivate(robot, "_tab_PositionOpeningSuccesEvent", position);

            Assert.That(MRZScoreNatrGridTestFactory.GetField(level, "Position"), Is.SameAs(position));
            Assert.That((bool)MRZScoreNatrGridTestFactory.GetField(level, "Consumed"), Is.True);
            Assert.That((int)MRZScoreNatrGridTestFactory.GetField(robot, "_recoverySeriesRemaining"), Is.EqualTo(0));
            Assert.That((bool)MRZScoreNatrGridTestFactory.GetField(robot, "_currentGridRecoveryBoostActive"), Is.False);
            Assert.That(((IList)MRZScoreNatrGridTestFactory.GetField(robot, "_levelsAwaitingOpeningSuccess")).Count, Is.EqualTo(0));
        }

        [Test]
        public void PositionOpeningSuccess_BindsLevelByAwaitingQueuePrice_WhenSignalTypeIsMissing()
        {
            MRZScoreNatrGrid robot = MRZScoreNatrGridTestFactory.CreateConfiguredGridRobot(seriesVolumeMultiplier: 1m);
            Type levelType = MRZScoreNatrGridTestFactory.GetGridLevelStateType();
            IList gridLevels = MRZScoreNatrGridTestFactory.CreateGridLevelsList(levelType);
            IList awaitingLevels = MRZScoreNatrGridTestFactory.CreateGridLevelsList(levelType);

            object firstLevel = MRZScoreNatrGridTestFactory.CreateGridLevel(levelType, 2, false);
            MRZScoreNatrGridTestFactory.SetField(firstLevel, "Price", 92m);
            MRZScoreNatrGridTestFactory.SetField(firstLevel, "Volume", 10m);
            MRZScoreNatrGridTestFactory.SetField(firstLevel, "DeviationPercent", 7m);

            object matchedLevel = MRZScoreNatrGridTestFactory.CreateGridLevel(levelType, 3, false);
            MRZScoreNatrGridTestFactory.SetField(matchedLevel, "Price", 94.5m);
            MRZScoreNatrGridTestFactory.SetField(matchedLevel, "Volume", 10m);
            MRZScoreNatrGridTestFactory.SetField(matchedLevel, "DeviationPercent", 5m);

            gridLevels.Add(firstLevel);
            gridLevels.Add(matchedLevel);
            awaitingLevels.Add(firstLevel);
            awaitingLevels.Add(matchedLevel);

            MRZScoreNatrGridTestFactory.SetField(robot, "_gridLevels", gridLevels);
            MRZScoreNatrGridTestFactory.SetField(robot, "_levelsAwaitingOpeningSuccess", awaitingLevels);

            Position position = CreatePosition(303, Side.Buy, 94.501m, null);

            MRZScoreNatrGridTestFactory.InvokePrivate(robot, "_tab_PositionOpeningSuccesEvent", position);

            Assert.That(MRZScoreNatrGridTestFactory.GetField(matchedLevel, "Position"), Is.SameAs(position));
            Assert.That((bool)MRZScoreNatrGridTestFactory.GetField(matchedLevel, "Consumed"), Is.True);
            Assert.That(MRZScoreNatrGridTestFactory.GetField(firstLevel, "Position"), Is.Null);
            Assert.That((bool)MRZScoreNatrGridTestFactory.GetField(firstLevel, "Consumed"), Is.False);
        }

        [Test]
        public void TryBuildPendingGrid_SkipsBuild_WhenEmaFilterBlocksBuy()
        {
            MRZScoreNatrGrid robot = MRZScoreNatrGridTestFactory.CreateConfiguredGridRobot(
                seriesVolumeMultiplier: 1m,
                ema: 97m,
                emaEnabled: true,
                emaReverse: false);

            List<Candle> candles = new List<Candle>
            {
                CreateFinishedCandle(new DateTime(2026, 5, 19, 12, 30, 0), 95m, 96m, 94m, 95m)
            };

            MRZScoreNatrGridTestFactory.InvokePrivate(robot, "TryBuildPendingGrid", candles);

            IList gridLevels = (IList)MRZScoreNatrGridTestFactory.GetField(robot, "_gridLevels");
            Assert.That(gridLevels.Count, Is.EqualTo(0));
            Assert.That(MRZScoreNatrGridTestFactory.GetField(robot, "_gridSide").ToString(), Is.EqualTo("None"));
        }

        [Test]
        public void ClearGrid_ArmsRecoveryAndResetsState_WhenLosingSeriesCompleted()
        {
            MRZScoreNatrGrid robot = MRZScoreNatrGridTestFactory.CreateRecoveryConfiguredRobot(threshold: 2, seriesCount: 2, multiplier: 1.5m);
            Type levelType = MRZScoreNatrGridTestFactory.GetGridLevelStateType();
            IList gridLevels = MRZScoreNatrGridTestFactory.CreateGridLevelsList(levelType);

            object level1 = MRZScoreNatrGridTestFactory.CreateGridLevel(levelType, 1, true);
            object level2 = MRZScoreNatrGridTestFactory.CreateGridLevel(levelType, 2, true);
            gridLevels.Add(level1);
            gridLevels.Add(level2);

            MRZScoreNatrGridTestFactory.SetField(robot, "_gridLevels", gridLevels);
            MRZScoreNatrGridTestFactory.SetField(robot, "_gridSide", Side.Buy);
            MRZScoreNatrGridTestFactory.SetField(robot, "_gridSma", 100m);
            MRZScoreNatrGridTestFactory.SetField(robot, "_gridThresholdPercent", 2m);
            MRZScoreNatrGridTestFactory.SetField(robot, "_gridNatrPercent", 1m);
            MRZScoreNatrGridTestFactory.SetField(robot, "_gridSignalTime", new DateTime(2026, 5, 19, 12, 0, 0));
            MRZScoreNatrGridTestFactory.SetField(robot, "_currentGridRecoveryBoostActive", true);
            MRZScoreNatrGridTestFactory.SetField(robot, "_activeSeriesRealizedPnl", -123m);

            MRZScoreNatrGridTestFactory.InvokePrivate(robot, "ClearGrid", "Series completed");

            Assert.That((int)MRZScoreNatrGridTestFactory.GetField(robot, "_recoverySeriesRemaining"), Is.EqualTo(2));
            Assert.That(((IList)MRZScoreNatrGridTestFactory.GetField(robot, "_gridLevels")).Count, Is.EqualTo(0));
            Assert.That(MRZScoreNatrGridTestFactory.GetField(robot, "_gridSide").ToString(), Is.EqualTo("None"));
            Assert.That((decimal)MRZScoreNatrGridTestFactory.GetField(robot, "_activeSeriesRealizedPnl"), Is.EqualTo(0m));
            Assert.That((bool)MRZScoreNatrGridTestFactory.GetField(robot, "_currentGridRecoveryBoostActive"), Is.False);
        }

        [Test]
        public void TryBuildPendingGrid_SkipsBuild_WhenThresholdIsUnavailable()
        {
            MRZScoreNatrGrid robot = MRZScoreNatrGridTestFactory.CreateConfiguredGridRobot(
                seriesVolumeMultiplier: 1m,
                lowChannel: 0m,
                highChannel: 102m);

            List<Candle> candles = new List<Candle>
            {
                CreateFinishedCandle(new DateTime(2026, 5, 19, 12, 45, 0), 95m, 96m, 94m, 95m)
            };

            MRZScoreNatrGridTestFactory.InvokePrivate(robot, "TryBuildPendingGrid", candles);

            IList gridLevels = (IList)MRZScoreNatrGridTestFactory.GetField(robot, "_gridLevels");
            Assert.That(gridLevels.Count, Is.EqualTo(0));
            Assert.That(MRZScoreNatrGridTestFactory.GetField(robot, "_gridSide").ToString(), Is.EqualTo("None"));
        }

        [Test]
        public void GetConsumedSeriesDepth_ReturnsHighestConsumedLevelIndex()
        {
            MRZScoreNatrGrid robot = MRZScoreNatrGridTestFactory.CreateUninitializedRobot();
            Type levelType = MRZScoreNatrGridTestFactory.GetGridLevelStateType();
            IList gridLevels = MRZScoreNatrGridTestFactory.CreateGridLevelsList(levelType);

            gridLevels.Add(MRZScoreNatrGridTestFactory.CreateGridLevel(levelType, 2, false));
            gridLevels.Add(MRZScoreNatrGridTestFactory.CreateGridLevel(levelType, 4, true));
            gridLevels.Add(MRZScoreNatrGridTestFactory.CreateGridLevel(levelType, 6, true));

            MRZScoreNatrGridTestFactory.SetField(robot, "_gridLevels", gridLevels);

            int depth = (int)MRZScoreNatrGridTestFactory.InvokePrivate(robot, "GetConsumedSeriesDepth");

            Assert.That(depth, Is.EqualTo(6));
        }

        [Test]
        public void ArmRecoveryBoostAfterCompletedSeries_Arms_WhenLosingSeriesMeetsThreshold()
        {
            MRZScoreNatrGrid robot = MRZScoreNatrGridTestFactory.CreateRecoveryConfiguredRobot(threshold: 5, seriesCount: 2, multiplier: 1.5m);

            MRZScoreNatrGridTestFactory.InvokePrivate(robot, "ArmRecoveryBoostAfterCompletedSeries", true, -150m, 5);

            Assert.That((int)MRZScoreNatrGridTestFactory.GetField(robot, "_recoverySeriesRemaining"), Is.EqualTo(2));
        }

        [Test]
        public void ArmRecoveryBoostAfterCompletedSeries_DoesNotArm_WhenLosingSeriesDepthIsBelowThreshold()
        {
            MRZScoreNatrGrid robot = MRZScoreNatrGridTestFactory.CreateRecoveryConfiguredRobot(threshold: 5, seriesCount: 2, multiplier: 1.5m);

            MRZScoreNatrGridTestFactory.InvokePrivate(robot, "ArmRecoveryBoostAfterCompletedSeries", true, -150m, 4);

            Assert.That((int)MRZScoreNatrGridTestFactory.GetField(robot, "_recoverySeriesRemaining"), Is.EqualTo(0));
        }

        [Test]
        public void ArmRecoveryBoostAfterCompletedSeries_ArmsForAnyLosingSeries_WhenThresholdIsZero()
        {
            MRZScoreNatrGrid robot = MRZScoreNatrGridTestFactory.CreateRecoveryConfiguredRobot(threshold: 0, seriesCount: 3, multiplier: 1.35m);

            MRZScoreNatrGridTestFactory.InvokePrivate(robot, "ArmRecoveryBoostAfterCompletedSeries", true, -50m, 1);

            Assert.That((int)MRZScoreNatrGridTestFactory.GetField(robot, "_recoverySeriesRemaining"), Is.EqualTo(3));
        }

        private static Candle CreateFinishedCandle(DateTime time, decimal open, decimal high, decimal low, decimal close)
        {
            return new Candle
            {
                TimeStart = time,
                Open = open,
                High = high,
                Low = low,
                Close = close,
                State = CandleState.Finished
            };
        }

        private static Position CreatePosition(int number, Side side, decimal entryPrice, string signalTypeOpen)
        {
            Position position = new Position
            {
                Number = number,
                Direction = side,
                SignalTypeOpen = signalTypeOpen,
                State = PositionStateType.Open
            };

            position.AddNewOpenOrder(new Order
            {
                SecurityNameCode = "TESTUSDT.P",
                Price = entryPrice,
                Volume = 1m,
                State = OrderStateType.Done,
                TimeCallBack = new DateTime(2026, 5, 19, 13, 30, 0)
            });
            position.State = PositionStateType.Open;
            return position;
        }

        private static void AssertLevel(object level, int expectedIndex, decimal expectedPrice, decimal expectedVolume, decimal expectedDeviation)
        {
            Assert.That((int)MRZScoreNatrGridTestFactory.GetField(level, "Index"), Is.EqualTo(expectedIndex));
            Assert.That((decimal)MRZScoreNatrGridTestFactory.GetField(level, "Price"), Is.EqualTo(expectedPrice).Within(0.000001m));
            Assert.That((decimal)MRZScoreNatrGridTestFactory.GetField(level, "Volume"), Is.EqualTo(expectedVolume).Within(0.000001m));
            Assert.That((decimal)MRZScoreNatrGridTestFactory.GetField(level, "DeviationPercent"), Is.EqualTo(expectedDeviation).Within(0.000001m));
        }
    }
}
