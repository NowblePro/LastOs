using OsEngine.Entity;
using OsEngine.OsTrader.Panels;

namespace OsEngine.Tests.Helpers
{
    internal sealed class TestBotPanel : BotPanel
    {
        public TestBotPanel(string name, StartProgram startProgram)
            : base(name, startProgram)
        {
        }

        public override string GetNameStrategyType()
        {
            return "TestBotPanel";
        }

        public override void ShowIndividualSettingsDialog()
        {
        }
    }
}
