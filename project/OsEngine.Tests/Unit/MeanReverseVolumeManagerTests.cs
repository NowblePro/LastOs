using NUnit.Framework;
using OsEngine.Common;

namespace OsEngine.Tests.Unit
{
    [TestFixture]
    [Category("Unit")]
    public class MeanReverseVolumeManagerTests
    {
        [Test]
        public void GetNextVolume_BuildsExpectedSeries_ForR50()
        {
            MeanReverseVolumeManager manager = new MeanReverseVolumeManager
            {
                R = 50,
                GetVolumeFunc = _ => 100m,
                Rounding = value => decimal.Round(value, 2)
            };

            decimal first = manager.GetNextVolume();
            decimal second = manager.GetNextVolume();
            decimal third = manager.GetNextVolume();
            decimal fourth = manager.GetNextVolume();

            Assert.That(first, Is.EqualTo(100m));
            Assert.That(second, Is.EqualTo(150m));
            Assert.That(third, Is.EqualTo(225m));
            Assert.That(fourth, Is.EqualTo(337.50m));
        }

        [Test]
        public void GetNextVolume_AppliesNextBaseVolumeMultiplier_OnlyToFirstLevel()
        {
            MeanReverseVolumeManager manager = new MeanReverseVolumeManager
            {
                R = 50,
                GetVolumeFunc = _ => 100m,
                Rounding = value => decimal.Round(value, 2),
                NextBaseVolumeMultiplier = 1.5m
            };

            decimal first = manager.GetNextVolume();
            decimal second = manager.GetNextVolume();

            Assert.That(first, Is.EqualTo(150m));
            Assert.That(second, Is.EqualTo(225m));
            Assert.That(manager.NextBaseVolumeMultiplier, Is.EqualTo(1m));
        }
    }
}
