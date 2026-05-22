using System;
using System.IO;
using System.Threading;
using NUnit.Framework;
using OsEngine.Entity;
using OsEngine.Tests.Helpers;

namespace OsEngine.Tests.Unit
{
    [TestFixture]
    [Category("Unit")]
    [Apartment(ApartmentState.STA)]
    public class BotPanelParameterLoadTests
    {
        [Test]
        public void CreateParameter_LoadsSavedValues_ByParameterName_NotByDeclarationOrder()
        {
            string testRoot = Path.Combine(Path.GetTempPath(), "OsEngineTests", Guid.NewGuid().ToString("N"));
            string engineDir = Path.Combine(testRoot, "Engine");
            Directory.CreateDirectory(engineDir);

            string originalCurrentDirectory = Environment.CurrentDirectory;

            try
            {
                File.WriteAllLines(
                    Path.Combine(engineDir, "TestBotParametrs.txt"),
                    new[]
                    {
                        "Second Param#9#2#1#20#1#",
                        "First Param#7#1#1#20#1#"
                    });

                Environment.CurrentDirectory = testRoot;

                TestBotPanel bot = new TestBotPanel("TestBot", StartProgram.IsOsTrader);

                StrategyParameterInt first = bot.CreateParameter("First Param", 1, 1, 20, 1);
                StrategyParameterInt second = bot.CreateParameter("Second Param", 2, 1, 20, 1);
                StrategyParameterInt third = bot.CreateParameter("Third Param", 3, 1, 20, 1);

                Assert.That(first.ValueInt, Is.EqualTo(7));
                Assert.That(second.ValueInt, Is.EqualTo(9));
                Assert.That(third.ValueInt, Is.EqualTo(3));

                bot.Delete();
            }
            finally
            {
                Environment.CurrentDirectory = originalCurrentDirectory;

                if (Directory.Exists(testRoot))
                {
                    Directory.Delete(testRoot, true);
                }
            }
        }
    }
}
