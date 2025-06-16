using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading;
using OsEngine.Entity;
using OsEngine.Indicators;
using OsEngine.OsTrader.Panels.Attributes;
using OsEngine.OsTrader.Panels.Tab;
using OsEngine.OsTrader.Panels;
using OsEngine.Market.CustomConnectors.Coinglass.Entity;
using OsEngine.Market.CustomConnectors.Coinglass;
using OsEngine.Market.Servers.Optimizer;
using OsEngine.Charts.CandleChart.Elements;

// BreakNadarayaWatsonReverseRsiCG

namespace OsEngine.Robots.TrigonumCustom.Channel.CoinGlass
{
    [Bot("BreakNadarayaWatsonReverseRsiCG")]
    internal class BreakNadarayaWatsonReverseRsiCG : BotPanel
    {
        private BotTabSimple _tab;

        // Basic Settings
        private StrategyParameterString _regime;
        private StrategyParameterBool _reverseLogic;
        private StrategyParameterString _volumeRegime;
        private StrategyParameterDecimal _volumeOnPosition;
        private StrategyParameterDecimal _slippage;
        private StrategyParameterTimeOfDay _startTradeTime;
        private StrategyParameterTimeOfDay _endTradeTime;

        private StrategyParameterBool _saveJson;

        // Indicator setting 
        private StrategyParameterInt _NWLength;
        private StrategyParameterDecimal _NWMultiplier;
        private StrategyParameterString _NWKernel;
        private StrategyParameterDecimal _NWKernelBandwidth;

        // Indicator
        private Aindicator _NW;

        // The last value of the indicator
        private decimal _lastEstimate;
        private decimal _lastUpLine;
        private decimal _lastDownLine;

        // RSI
        private Aindicator _rsi;
        private StrategyParameterInt _lengthRsi;
        private StrategyParameterDecimal _oversoldRsi;
        private StrategyParameterDecimal _overboughtRsi;
        private StrategyParameterBool _drawRsiChannel;
        private StrategyParameterBool _rsiFilterIsOn;
        // RSI

        //------------------------------------------------------- CG LSR
        public StrategyParameterBool LongShortRatioFilterIsOn;
        public StrategyParameterString ApiKey;
        public StrategyParameterString MinIntervalTimeFrame;
        public StrategyParameterString ExchageForLongShort;
        private StrategyParameterDecimal LongShortRatioBuy;
        private StrategyParameterDecimal LongShortRatioSell;
        private StrategyParameterBool PrintLSR;
        private List<LongShortRatio> _longShortRatio = new List<LongShortRatio>();
        private CoinglassConnector _conn;
        private readonly RequestContent _requestContent = new RequestContent();
        //------------------------------------------------------- CG LSR

        public BreakNadarayaWatsonReverseRsiCG(string name, StartProgram startProgram) : base(name, startProgram)
        {
            TabCreate(BotTabType.Simple);
            _tab = TabsSimple[0];

            // Basic setting
            _regime = CreateParameter("Regime", "Off", new[] { "Off", "On", "OnlyLong", "OnlyShort", "OnlyClosePosition" }, "Base");
            _reverseLogic = CreateParameter("Reverse logic", true, "Base");
            _volumeRegime = CreateParameter("Volume type", "Number of contracts", new[] { "Number of contracts", "Contract currency", "% of the total portfolio" }, "Base");
            _volumeOnPosition = CreateParameter("Volume", 1, 1.0m, 50, 4, "Base");
            _slippage = CreateParameter("Slippage %", 0m, 0, 20, 1, "Base");
            _startTradeTime = CreateParameterTimeOfDay("Start Trade Time", 0, 0, 0, 0, "Base");
            _endTradeTime = CreateParameterTimeOfDay("End Trade Time", 24, 0, 0, 0, "Base");

            _saveJson = CreateParameter("Save Json Data", false, "Base");

            // Indicator setting
            _NWLength = CreateParameter("Nadaraya-Watson Length", 14, 7, 48, 7, "Indicator");
            _NWMultiplier = CreateParameter("Nadaraya-Watson Multiplier", 1.0m, 1, 5, 0.2m, "Indicator");
            _NWKernel = CreateParameter("Nadaraya-Watson Kernel", "Gaussian", new[] { "Gaussian", "Epanechnikov", "Uniform", "Triangular" }, "Indicator");
            _NWKernelBandwidth = CreateParameter("Kernel Bandwidth", 1.0m, 0.5m, 20m, 0.5m, "Indicator");

            // RSI
            _rsiFilterIsOn = CreateParameter("Is RSI Filter On", true, "Filters");
            _lengthRsi = CreateParameter("Rsi Length", 14, 7, 33, 1, "Filters");
            _oversoldRsi = CreateParameter("Rsi Oversold", 30m, 25, 45, 5, "Filters");
            _overboughtRsi = CreateParameter("Rsi Overbought", 70m, 55, 75, 5, "Filters");
            _drawRsiChannel = CreateParameter("Draw Ovb/Ovs Channel", false, "Filters");

            _rsi = IndicatorsFactory.CreateIndicatorByName(nameClass: "RSI", name: name + "RSI", canDelete: false);
            _rsi = (Aindicator)_tab.CreateCandleIndicator(_rsi, nameArea: "RsiArea");
            _rsi.DataSeries[0].Color = System.Drawing.Color.Coral;
            _rsi.ParametersDigit[0].Value = _lengthRsi.ValueInt;
            _rsi.ParametersDigit[1].Value = _oversoldRsi.ValueDecimal;
            _rsi.ParametersDigit[2].Value = _overboughtRsi.ValueDecimal;
            _rsi.DataSeries[1].IsPaint = _drawRsiChannel.ValueBool;
            _rsi.DataSeries[2].IsPaint = _drawRsiChannel.ValueBool;
            _rsi.Save();
            // RSI

            //------------------------------------------------------- CG LSR
            ApiKey = CreateParameter("API Key", "", "Base");
            MinIntervalTimeFrame = CreateParameter("Min History Interval API", "1d", new[] { "1m", "5m", "15m", "30m", "1h", "4h", "6h", "8h", "12h", "1d" }, "Base");
            ExchageForLongShort = CreateParameter("Exchange for Long Short", "Binance", new[] { "Binance", "Bybit" }, "Base");
            LongShortRatioBuy = CreateParameter("Long Short Ratio Buy", 0.96078m, 0.94m, 0.98m, 0.0001m, "Base");
            LongShortRatioSell = CreateParameter("Long Short Ratio Sell", 1.04m, 0.94m, 0.98m, 0.0001m, "Base");
            LongShortRatioFilterIsOn = CreateParameter("Is Long Short Ratio Filter On", false, "Filters");
            PrintLSR = CreateParameter("Print LSR Data on Chart", false, "Filters");
            //------------------------------------------------------- CG LSR

            // Create indicator NW
            _NW = IndicatorsFactory.CreateIndicatorByName("NadarayaWatson", name + "NadarayaWatson", false);
            _NW = (Aindicator)_tab.CreateCandleIndicator(_NW, "Prime");
            ((IndicatorParameterInt)_NW.Parameters[0]).ValueInt = _NWLength.ValueInt;
            ((IndicatorParameterDecimal)_NW.Parameters[1]).ValueDecimal = _NWMultiplier.ValueDecimal;
            ((IndicatorParameterString)_NW.Parameters[2]).ValueString = _NWKernel.ValueString;
            ((IndicatorParameterDecimal)_NW.Parameters[3]).ValueDecimal = _NWKernelBandwidth.ValueDecimal;

            _NW.Save();

            // Subscribe to the indicator update event
            ParametrsChangeByUser += NW_ParametersChangeByUser;
            // Subscribe to the candle finished event
            _tab.CandleFinishedEvent += _tab_CandleFinishedEvent;

            NW_ParametersChangeByUser();
        }

        //------------------------------------------------------- CG LSR
        private void CoinglassUpdateEvent(string botName, ResponseType type, List<LongShortRatio> lsrList)
        {
            if (botName != _tab.TabName) return; //если имя вкладки не совпало - выходим

            if (type == ResponseType.LongShortRatio)
            {
                _longShortRatio = lsrList; //обновляем значение показателя
            }
        }
        //------------------------------------------------------- CG LSR

        private void NW_ParametersChangeByUser()
        {
            StopOrActivateIndicators();

            _tab.setSaveData(_saveJson.ValueBool);

            ((IndicatorParameterInt)_NW.Parameters[0]).ValueInt = _NWLength.ValueInt;
            ((IndicatorParameterDecimal)_NW.Parameters[1]).ValueDecimal = _NWMultiplier.ValueDecimal;
            ((IndicatorParameterString)_NW.Parameters[2]).ValueString = _NWKernel.ValueString;
            ((IndicatorParameterDecimal)_NW.Parameters[3]).ValueDecimal = _NWKernelBandwidth.ValueDecimal;
            _NW.Save();
            _NW.Reload();

            // RSI
            if (_rsi.ParametersDigit[0].Value != _lengthRsi.ValueInt
                    || _rsi.ParametersDigit[1].Value != _oversoldRsi.ValueDecimal
                    || _rsi.ParametersDigit[2].Value != _overboughtRsi.ValueDecimal)
            {
                _rsi.ParametersDigit[0].Value = _lengthRsi.ValueInt;
                _rsi.ParametersDigit[1].Value = _oversoldRsi.ValueDecimal;
                _rsi.ParametersDigit[2].Value = _overboughtRsi.ValueDecimal;

                _rsi.Save();
                _rsi.Reload();
            }

            if (_rsi.DataSeries != null && _rsi.DataSeries.Count > 0)
            {
                if (!_rsiFilterIsOn.ValueBool)
                {
                    _rsi.DataSeries[0].IsPaint = false;
                    _rsi.DataSeries[1].IsPaint = false;
                    _rsi.DataSeries[2].IsPaint = false;
                }
                else
                {
                    _rsi.DataSeries[0].IsPaint = true;
                    _rsi.DataSeries[1].IsPaint = _drawRsiChannel.ValueBool;
                    _rsi.DataSeries[2].IsPaint = _drawRsiChannel.ValueBool;
                }
            }
            // RSI
        }

        private void StopOrActivateIndicators()
        {
            // RSI
            if (_rsiFilterIsOn.ValueBool == false)
            {
                _rsi.IsOn = false;
                _rsi.Reload();
            }
            else
            {
                _rsi.IsOn = true;
                _rsi.Reload();
            }
            // RSI
        }

        // The name of the robot in OsEngine
        public override string GetNameStrategyType()
        {
            return "BreakNadarayaWatsonReverseRsiCG";
        }
        public override void ShowIndividualSettingsDialog()
        {

        }

        //------------------------------------------------------- CG LSR
        private void Server_TestingEndEvent(int obj)
        {
            _conn.CoinglassUpdateEvent -= CoinglassUpdateEvent;
            _conn = null;
        }
        //------------------------------------------------------- CG LSR

        // Candle Finished Event
        private void _tab_CandleFinishedEvent(List<Candle> candles)
        {
            // If the robot is turned off, exit the event handler
            if (_regime.ValueString == "Off")
            {
                return;
            }

            //------------------------------------------------------- CG LSR
            if (_conn == null)
            {
                _conn = CoinglassConnector.GetServer(ApiKey.ValueString); //Получаем или создаем коннектор
                _conn.CoinglassUpdateEvent += CoinglassUpdateEvent; //Подписываемся на обновления из коннектора

                if (StartProgram == StartProgram.IsOsOptimizer)
                {
                    OptimizerServer server = (OptimizerServer)_tab.Connector.MyServer;
                    server.TestingEndEvent += Server_TestingEndEvent;
                }
            }

            DateTime dt = candles[candles.Count - 1].TimeStart; //запоминаем время последней свечи

            if (LongShortRatioFilterIsOn.ValueBool) //если фильтр по LSR включен
            {
                //заполняем поля класса запроса
                _requestContent.Exchange = ExchageForLongShort.ValueString; //биржа, на которой смотрим показатель
                _requestContent.Interval = MinIntervalTimeFrame.ValueString; //мин. доступный интервал для ключа API
                _requestContent.BotName = _tab.TabName; //имя вкладки бота
                _requestContent.ResponseType = ResponseType.LongShortRatio; //запрашиваемый показатель

                string symbol = _tab.Security.Name;
                if (symbol.EndsWith(".txt")) // Check for pair correct
                {
                    symbol = symbol.Substring(0, symbol.Length - 4);
                }
                _requestContent.Symbol = symbol; //название инструмента

                if (_requestContent.StartProgram != StartProgram.IsOsTrader) //вызывающая программа не OsTrader
                {
                    if (_longShortRatio == null || _longShortRatio.Count < 2) //если данные еще не получены
                    {
                        _conn.SendRequest(_requestContent); //отправляем запрос серверу
                        while (_longShortRatio == null || _longShortRatio.Count < 2) //ждем поступления данных
                        {
                            Thread.Sleep(500);
                        }
                    }
                }
                else
                {
                    _conn.SendRequest(_requestContent); //отправляем запрос серверу
                }
            }
            //------------------------------------------------------- CG LSR

            // If there are not enough candles to build an indicator, we exit
            if (candles.Count < _NWLength.ValueInt)
            {
                return;
            }

            // If the time does not match, we leave
            if (_startTradeTime.Value > _tab.TimeServerCurrent ||
                _endTradeTime.Value < _tab.TimeServerCurrent)
            {
                return;
            }

            List<Position> openPositions = _tab.PositionsOpenAll;

            //------------------------------------------------------- CG LSR
            //если активирован PrintLSR - на последней свече пишем значение показателя и ставим цветную метку
            if (PrintLSR.ValueBool)
            {
                DrawLabelOnChart(candles, candles[candles.Count - 1].IsUp ? Side.Buy : Side.Sell);
            }
            //------------------------------------------------------- CG LSR

            // If there are positions, then go to the position closing method
            if (openPositions != null && openPositions.Count != 0)
            {
                LogicClosePosition(candles);
            }

            // If the position closing mode, then exit the method
            if (_regime.ValueString == "OnlyClosePosition")
            {
                return;
            }
            // If there are no positions, then go to the position opening method
            if (openPositions == null || openPositions.Count == 0)
            {
                LogicOpenPosition(candles);
            }
        }

        // Opening logic
        private void LogicOpenPosition(List<Candle> candles)
        {
            // The last value of the indicator
            _lastEstimate = _NW.DataSeries[0].Last;
            _lastUpLine = _NW.DataSeries[1].Last;
            _lastDownLine = _NW.DataSeries[2].Last;

            List<Position> openPositions = _tab.PositionsOpenAll;

            if (openPositions == null || openPositions.Count == 0)
            {
                decimal lastPrice = candles[candles.Count - 1].Close;

                // Slippage
                decimal slippage = _slippage.ValueDecimal * _tab.Security.PriceStep;

                // Long
                if (_regime.ValueString != "OnlyShort") // If the mode is not only short, then we enter long
                {
                    if (_reverseLogic.ValueBool)
                    {
                        if (lastPrice < _lastDownLine)
                        {
                            if (!BuySignalIsFiltered(candles))
                                _tab.BuyAtLimit(GetVolume(), _tab.PriceBestAsk + slippage);
                        }
                    }
                    else
                    {
                        if (lastPrice > _lastUpLine)
                        {
                            if (!BuySignalIsFiltered(candles))
                                _tab.BuyAtLimit(GetVolume(), _tab.PriceBestAsk + slippage);
                        }
                    }
                }

                // Short
                if (_regime.ValueString != "OnlyLong") // If the mode is not only long, then we enter short
                {
                    if (_reverseLogic.ValueBool)
                    {
                        if (lastPrice > _lastUpLine)
                        {
                            if (!SellSignalIsFiltered(candles))
                                _tab.SellAtLimit(GetVolume(), _tab.PriceBestBid - slippage);
                        }
                    }
                    else
                    {
                        if (lastPrice < _lastDownLine)
                        {
                            if (!SellSignalIsFiltered(candles))
                                _tab.SellAtLimit(GetVolume(), _tab.PriceBestBid - slippage);
                        }
                    }
                }
            }
        }

        // Logic close position
        private void LogicClosePosition(List<Candle> candles)
        {
            List<Position> openPositions = _tab.PositionsOpenAll;

            // The last value of the indicator
            _lastEstimate = _NW.DataSeries[0].Last;
            _lastUpLine = _NW.DataSeries[1].Last;
            _lastDownLine = _NW.DataSeries[2].Last;

            decimal slippage = _slippage.ValueDecimal * _tab.Security.PriceStep;

            decimal lastPrice = candles[candles.Count - 1].Close;

            for (int i = 0; openPositions != null && i < openPositions.Count; i++)
            {
                Position pos = openPositions[i];

                if (pos.State != PositionStateType.Open)
                {
                    continue;
                }

                if (pos.Direction == Side.Buy) // If the direction of the position is purchase
                {
                    if (_reverseLogic.ValueBool)
                    {
                        if (lastPrice > _lastEstimate)
                        {
                            _tab.CloseAtLimit(pos, lastPrice + slippage, pos.OpenVolume);
                        }
                    }
                    else
                    {
                        if (lastPrice < _lastDownLine)
                        {
                            _tab.CloseAtLimit(pos, lastPrice - slippage, pos.OpenVolume);
                        }
                    }
                }
                else // If the direction of the position is sale
                {
                    if (_reverseLogic.ValueBool)
                    {
                        if (lastPrice < _lastEstimate)
                        {
                            _tab.CloseAtLimit(pos, lastPrice - slippage, pos.OpenVolume);
                        }
                    }
                    else
                    {
                        if (lastPrice > _lastUpLine)
                        {
                            _tab.CloseAtLimit(pos, lastPrice + slippage, pos.OpenVolume);
                        }
                    }
                }
            }
        }

        private bool BuySignalIsFiltered(List<Candle> candles)
        {
            decimal lastPrice = candles[candles.Count - 1].Close;

            DateTime dt = candles[candles.Count - 1].TimeStart; // CG LSR

            // filter for buy
            if (_regime.ValueString == "Off" ||
                _regime.ValueString == "OnlyShort" ||
                _regime.ValueString == "OnlyClosePosition")
            {
                return true;
                //if the robot's operating mode does not correspond to the direction of the position
            }

            // RSI
            if (_rsiFilterIsOn.ValueBool)
            {
                decimal lastRsi = _rsi.DataSeries[0].Last;
                if (lastRsi >= _oversoldRsi.ValueDecimal)
                {
                    return true;
                }
            }
            // RSI

            //------------------------------------------------------- CG LSR
            if (LongShortRatioFilterIsOn.ValueBool) //если включен фильтр LongShortRatioFilter
            {
                decimal lsr = GetLsr(dt, _longShortRatio); //получаем актуальное значение LongShortRatio

                if (lsr == 0) return true; //если значение показателя не обновлено - возвращаем true
                // если LongShortRatio больше порогового значения - возвращаем true
                if (lsr > LongShortRatioBuy.ValueDecimal) return true;
            }
            //------------------------------------------------------- CG LSR

            return false;
        }

        private bool SellSignalIsFiltered(List<Candle> candles)
        {
            decimal lastPrice = candles[candles.Count - 1].Close;

            DateTime dt = candles[candles.Count - 1].TimeStart; // CG LSR

            // filter for sell
            if (_regime.ValueString == "Off" ||
                _regime.ValueString == "OnlyLong" ||
                _regime.ValueString == "OnlyClosePosition")
            {
                return true;
                // if the robot's operating mode does not correspond to the direction of the position
            }

            // RSI
            if (_rsiFilterIsOn.ValueBool)
            {
                decimal lastRsi = _rsi.DataSeries[0].Last;
                if (lastRsi <= _overboughtRsi.ValueDecimal)
                {
                    return true;
                }
            }
            // RSI

            //------------------------------------------------------- CG LSR
            if (LongShortRatioFilterIsOn.ValueBool) //если включен фильтр LongShortRatioFilter
            {
                decimal lsr = GetLsr(dt, _longShortRatio); //получаем актуальный LongShortRatio

                if (lsr == 0) return true; //если значение показателя не обновлено - возвращаем true
                // если LongShortRatio меньше порогового значения - возвращаем true
                if (lsr < LongShortRatioSell.ValueDecimal) return true;
            }
            //------------------------------------------------------- CG LSR

            return false;
        }

        // Method for calculating the volume of entry into a position
        private decimal GetVolume()
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
            else // if (VolumeRegime.ValueString == "% of the total portfolio")
            {
                volume = _tab.Portfolio.ValueCurrent * (_volumeOnPosition.ValueDecimal / 100) / _tab.PriceBestAsk / _tab.Security.Lot;
            }

            volume = GetRoundedVolume(_tab, volume);

            return volume;
        }

        /// <summary>
        /// Получает актуальное значение показателя LongShortRatio
        /// </summary>
        /// <param name="dt">Время актуальной свечи</param>
        /// <param name="lsr">Список всех доступных объектов LongShortRatio</param>
        /// <returns>Значение LongShortRatio</returns>
        private decimal GetLsr(DateTime dt, List<LongShortRatio> lsr)
        {
            if (lsr == null || lsr.Count == 0) return 0;

            if (_requestContent.StartProgram == StartProgram.IsOsTrader)
            {
                return lsr[0].LSR;
            }

            for (int i = 1; i < lsr.Count; i++)
            {
                if (lsr[i].Time > dt && lsr[i - 1].Time <= dt)
                    return lsr[i - 1].LSR;
            }

            return 0;
        }

        private PointElement _point;

        /// <summary>
        /// Вывод на график значения показателя LSR
        /// </summary>
        /// <param name="candles">Список свечей</param>
        /// <param name="side">Сторона сделки</param>
        private void DrawLabelOnChart(List<Candle> candles, Side side)
        {
            if (StartProgram == StartProgram.IsOsOptimizer)
            {
                return;
            }

            DateTime dt = candles[candles.Count - 1].TimeStart;
            decimal lsr = GetLsr(dt, _longShortRatio);

            if (lsr == 0) { return; }

            PointElement point = new PointElement("LSR", "Prime");

            if (side == Side.Buy)
            {
                point.Y = candles[candles.Count - 1].Low;
                point.Color = Color.Green;
            }
            else
            {
                point.Y = candles[candles.Count - 1].High;
                point.Color = Color.Red;
            }

            point.TimePoint = candles[candles.Count - 1].TimeStart;
            point.Label = "LSR: " + lsr;
            point.Font = new Font("Arial", 8);
            point.LabelTextColor = Color.White;
            point.Size = 6;

            _point = point;

            _tab.SetChartElement(_point);
        }
    }
}
