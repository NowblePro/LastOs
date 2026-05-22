using System;
using NUnit.Framework;
using OsEngine.Entity;
using OsEngine.Journal.Internal;
namespace OsEngine.Tests.Unit
{
    [TestFixture]
    [Category("Unit")]
    public class PositionStatisticGeneratorTests
    {
        [Test]
        public void GetStabilityScore_ReturnsZero_WhenProfitIsNegative()
        {
            Position[] deals =
            {
                CreateClosedPosition(1000m, -10m),
                CreateClosedPosition(1000m, -5m),
                CreateClosedPosition(1000m, -3m)
            };

            decimal score = PositionStatisticGenerator.GetStabilityScore(deals, 1);

            Assert.That(score, Is.EqualTo(0m));
        }

        [Test]
        public void GetStabilityScore_IsHigher_ForSmootherEquityCurve()
        {
            Position[] smoothDeals =
            {
                CreateClosedPosition(1000m, 30m),
                CreateClosedPosition(1000m, 30m),
                CreateClosedPosition(1000m, 30m),
                CreateClosedPosition(1000m, 30m)
            };

            Position[] jaggedDeals =
            {
                CreateClosedPosition(1000m, 90m),
                CreateClosedPosition(1000m, -60m),
                CreateClosedPosition(1000m, 80m),
                CreateClosedPosition(1000m, 10m)
            };

            decimal smoothScore = PositionStatisticGenerator.GetStabilityScore(smoothDeals, 1);
            decimal jaggedScore = PositionStatisticGenerator.GetStabilityScore(jaggedDeals, 1);

            Assert.That(smoothScore, Is.GreaterThan(jaggedScore));
        }

        private static Position CreateClosedPosition(decimal portfolioValueOnOpen, decimal profitPortfolioPunkt)
        {
            Position position = new Position
            {
                PortfolioValueOnOpenPosition = portfolioValueOnOpen,
                ProfitOperationPunkt = profitPortfolioPunkt,
                ProfitOperationPersent = profitPortfolioPunkt / portfolioValueOnOpen * 100m,
                Direction = Side.Buy,
                State = PositionStateType.Done,
                PriceStep = 1m,
                PriceStepCost = 1m,
                MultToJournal = 100
            };

            Order openOrder = new Order
            {
                SecurityNameCode = "TEST",
                ServerType = OsEngine.Market.ServerType.Tester,
                Volume = 1m,
                VolumeExecute = 1m,
                State = OrderStateType.Done
            };

            position.AddNewOpenOrder(openOrder);

            return position;
        }
    }
}
