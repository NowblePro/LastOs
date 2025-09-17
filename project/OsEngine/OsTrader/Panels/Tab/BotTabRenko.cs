using System;
using OsEngine.Entity;
using OsEngine.Logging;
using OsEngine.Market.Connectors;
using OsEngine.Logging;
using System.Collections.Generic;

namespace OsEngine.OsTrader.Panels.Tab
{
    public class BotTabRenko : IIBotTab
    {
        public BotTabRenko(string name, StartProgram startProgram)
        {
            TabName = name;
            StartProgram = startProgram;

            try
            {
                _connector = new ConnectorCandles(TabName, startProgram, true);
                _connector.NewCandlesChangeEvent += LogicToEndCandle;
                _connector.LastCandlesChangeEvent += LogicToUpdateLastCandle;
                _connector.TickChangeEvent += LogicToNewTick;
                _connector.LogMessageEvent += SetNewLogMessage;
            }
            catch (Exception error)
            {
                SetNewLogMessage(error.ToString(), LogMessageType.Error);
            }
        }

        public StartProgram StartProgram;

        public BotTabType TabType { get { return BotTabType.Renko; } }

        public string TabName { get; set; }
        public int TabNum { get; set; }
        public bool EventsIsOn { get; set; }
        public bool EmulatorIsOn { get; set; }
        public DateTime LastTimeCandleUpdate { get; set; }

        public event Action TabDeletedEvent;
        public event Action<string, LogMessageType> LogMessageEvent;
        public event Action<List<Candle>> BarFinishedEvent;
        public event Action<List<Candle>> BarUpdateEvent;

        private void LogicToEndCandle(List<Candle> candles)
        {

        }

        private void LogicToUpdateLastCandle(List<Candle> candles)
        {

        }

        private void LogicToNewTick(List<Trade> trades)
        {

        }


        public void Clear()
        {
        }

        public void Delete()
        {
        }

        public void StopPaint()
        {
        }

        public ConnectorCandles Connector
        {
            get { return _connector; }
        }
        private ConnectorCandles _connector;

        public void SetNewLogMessage(string message, LogMessageType messageType)
        {
            if (LogMessageEvent != null)
            {
                LogMessageEvent(message, messageType);
            }
            else if (messageType == LogMessageType.Error)
            {
                System.Windows.MessageBox.Show(message);
            }
        }
    }
}
