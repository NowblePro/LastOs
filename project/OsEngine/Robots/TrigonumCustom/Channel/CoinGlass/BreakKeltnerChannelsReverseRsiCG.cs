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

// BreakKeltnerChannelsReverseRsiCG

namespace OsEngine.Robots.TrigonumCustom.Channel.CoinGlass
{
    [Bot("BreakKeltnerChannelsReverseRsiCG")]
    public class BreakKeltnerChannelsReverseRsiCG : BotPanel
    {
        private BotTabSimple _tab;

        private StrategyParameterString Regime;
        private StrategyParameterBool ReverseLogic;
        private StrategyParameterDecimal VolumeOnPosition;
        private StrategyParameterString VolumeRegime;
        private StrategyParameterDecimal Slippage;

        private StrategyParameterTimeOfDay TimeStart;
        private StrategyParameterTimeOfDay TimeEnd;

        private StrategyParameterBool _saveJson;

        private Aindicator _keltnerChannels;
        private StrategyParameterInt KeltnerPeriod;
        private StrategyParameterDecimal AtrMultiplier;

        private Aindicator _sma;
        private StrategyParameterInt SmaPeriod;

        private Aindicator _smaFilter;
        private StrategyParameterInt SmaLengthFilter;
        private StrategyParameterBool SmaPositionFilterIsOn;
        private StrategyParameterBool SmaSlopeFilterIsOn;

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

        public BreakKeltnerChannelsReverseRsiCG(string name, StartProgram startProgram) : base(name, startProgram)
        {
            TabCreate(BotTabType.Simple);
            _tab = TabsSimple[0];

            Regime = CreateParameter("Regime", "Off", new[] { "Off", "On", "OnlyLong", "OnlyShort", "OnlyClosePosition" }, "Base");
            ReverseLogic = CreateParameter("Reverse logic", true, "Base");
            VolumeRegime = CreateParameter("Volume type", "Number of contracts", new[] { "Number of contracts", "Contract currency", "% of the total portfolio" }, "Base");
            VolumeOnPosition = CreateParameter("Volume", 10, 1.0m, 50, 4, "Base");
            Slippage = CreateParameter("Slippage %", 0m, 0, 20, 1, "Base");

            _saveJson = CreateParameter("Save Json Data", false, "Base");

            TimeStart = CreateParameterTimeOfDay("Start Trade Time", 0, 0, 0, 0, "Base");
            TimeEnd = CreateParameterTimeOfDay("End Trade Time", 24, 0, 0, 0, "Base");

            KeltnerPeriod = CreateParameter("Keltner Period", 14, 3, 50, 1, "Robot parameters");
            AtrMultiplier = CreateParameter("ATR  Multiplier", 1, 1, 10, 0.2m, "Robot parameters");
            SmaPeriod = CreateParameter("SMA Period", 100, 100, 400, 10, "Robot parameters");

            SmaLengthFilter = CreateParameter("Sma Length", 100, 10, 500, 1, "Filters");
            SmaPositionFilterIsOn = CreateParameter("Is SMA Filter On", false, "Filters");
            SmaSlopeFilterIsOn = CreateParameter("Is Sma Slope Filter On", false, "Filters");

            _smaFilter = IndicatorsFactory.CreateIndicatorByName(nameClass: "Sma", name: name + "Sma_Filter", canDelete: false);
            _smaFilter = (Aindicator)_tab.CreateCandleIndicator(_smaFilter, nameArea: "Prime");
            _smaFilter.DataSeries[0].Color = System.Drawing.Color.Azure;
            _smaFilter.ParametersDigit[0].Value = SmaLengthFilter.ValueInt;
            _smaFilter.Save();

            //------------------------------------------------------- CG LSR
            ApiKey = CreateParameter("API Key", "", "Base");
            MinIntervalTimeFrame = CreateParameter("Min History Interval API", "1d", new[] { "1m", "5m", "15m", "30m", "1h", "4h", "6h", "8h", "12h", "1d" }, "Base");
            ExchageForLongShort = CreateParameter("Exchange for Long Short", "Binance", new[] { "Binance", "Bybit" }, "Base");
            LongShortRatioBuy = CreateParameter("Long Short Ratio Buy", 0.96078m, 0.94m, 0.98m, 0.0001m, "Base");
            LongShortRatioSell = CreateParameter("Long Short Ratio Sell", 1.04m, 0.94m, 0.98m, 0.0001m, "Base");
            LongShortRatioFilterIsOn = CreateParameter("Is Long Short Ratio Filter On", false, "Filters");
            PrintLSR = CreateParameter("Print LSR Data on Chart", false, "Filters");
            //------------------------------------------------------- CG LSR

            // RSI
            _rsiFilterIsOn = CreateParameter("Is RSI Filter On", true, "Filters");
            _lengthRsi = CreateParameter("Rsi Length", 14, 10, 33, 1, "Filters");
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

            _sma = IndicatorsFactory.CreateIndicatorByName("Sma", name + "Sma", false);
            _sma = (Aindicator)_tab.CreateCandleIndicator(_sma, "Prime");
            _sma.ParametersDigit[0].Value = SmaPeriod.ValueInt;
            _sma.Save();

            _keltnerChannels = IndicatorsFactory.CreateIndicatorByName("KeltnerChannels_indicator", name + "KeltnerChannels", false);
            _keltnerChannels = (Aindicator)_tab.CreateCandleIndicator(_keltnerChannels, "Prime");
            _keltnerChannels.ParametersDigit[0].Value = KeltnerPeriod.ValueInt;
            _keltnerChannels.ParametersDigit[3].Value = AtrMultiplier.ValueDecimal;
            _keltnerChannels.Save();

            _sma.ToString();

            StopOrActivateIndicators();
            ParametrsChangeByUser += KeltnerChannelsBot_ParametrsChangeByUser;
            _tab.CandleFinishedEvent += _tab_CandleFinishedEvent;
            KeltnerChannelsBot_ParametrsChangeByUser();
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

        private void KeltnerChannelsBot_ParametrsChangeByUser()
        {
            StopOrActivateIndicators();

            _tab.setSaveData(_saveJson.ValueBool);

            _keltnerChannels.ParametersDigit[0].Value = KeltnerPeriod.ValueInt;
            _keltnerChannels.ParametersDigit[3].Value = AtrMultiplier.ValueDecimal;
            _keltnerChannels.Save();
            _keltnerChannels.Reload();

            _sma.ParametersDigit[0].Value = SmaPeriod.ValueInt;
            _sma.Save();
            _sma.Reload();

            if (_smaFilter.DataSeries.Count == 0)
            {
                return;
            }

            if (_smaFilter.ParametersDigit[0].Value != SmaLengthFilter.ValueInt)
            {
                _smaFilter.ParametersDigit[0].Value = SmaLengthFilter.ValueInt;
                _smaFilter.Save();
                _smaFilter.Reload();
            }

            if (_smaFilter.DataSeries != null && _smaFilter.DataSeries.Count > 0)
            {
                if (!SmaPositionFilterIsOn.ValueBool && !SmaSlopeFilterIsOn.ValueBool)
                {
                    _smaFilter.DataSeries[0].IsPaint = false;
                }
                else if (SmaPositionFilterIsOn.ValueBool || SmaSlopeFilterIsOn.ValueBool)
                {
                    _smaFilter.DataSeries[0].IsPaint = true;
                }
            }

            // RSI
            if (_rsi.ParametersDigit[0].Value != _lengthRsi.ValueInt
                    || _rsi.ParametersDigit[1].Value != _oversoldRsi.ValueDecimal
                    || _rsi.ParametersDigit[2].Value != _overboughtRsi.ValueDecimal)
            {
                _rsi.ParametersDigit[0].Value = _lengthRsi.ValueInt;
                _rsi.ParametersDigit[1].Value = _oversoldRsi.ValueDecimal;
                _rsi.ParametersDigit[2].Value = _overboughtRsi.ValueDecimal;

                _rsi.Reload();
                _rsi.Save();
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

            if (SmaPositionFilterIsOn.ValueBool == false
               && SmaSlopeFilterIsOn.ValueBool == false
               && _smaFilter.IsOn == true)
            {
                _smaFilter.IsOn = false;
                _smaFilter.Reload();
            }
            else if ((SmaPositionFilterIsOn.ValueBool == true
                || SmaSlopeFilterIsOn.ValueBool == true)
                && _smaFilter.IsOn == false)
            {
                _smaFilter.IsOn = true;
                _smaFilter.Reload();
            }
        }

        public override string GetNameStrategyType()
        {
            return "BreakKeltnerChannelsReverseRsiCG";
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

        // Logic
        private void _tab_CandleFinishedEvent(List<Candle> candles)
        {
            if (Regime.ValueString == "Off")
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

            if (TimeStart.Value > _tab.TimeServerCurrent ||
                TimeEnd.Value < _tab.TimeServerCurrent)
            {
                CancelStopsAndProfits();
                return;
            }

            if (SmaLengthFilter.ValueInt >= candles.Count)
            {
                return;
            }

            if (_keltnerChannels.DataSeries[0].Values == null || candles.Count < _keltnerChannels.ParametersDigit[0].Value ||
                candles.Count < SmaPeriod.ValueInt)
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

            if (openPositions != null && openPositions.Count != 0)
            {
                for (int i = 0; i < openPositions.Count; i++)
                {
                    LogicClosePosition(candles, openPositions[i]);
                }
            }

            if (openPositions == null || openPositions.Count == 0)
            {
                LogicOpenPosition(candles, openPositions);
            }
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

        private void LogicOpenPosition(List<Candle> candles, List<Position> position)
        {
            decimal _lastPrice = candles[candles.Count - 1].Close;
            decimal _keltnerUpLast = _keltnerChannels.DataSeries[1].Last;
            decimal _keltnerDownLast = _keltnerChannels.DataSeries[2].Last;
            decimal _smaLast = _sma.DataSeries[0].Last;

            decimal _slippage = Slippage.ValueDecimal * _lastPrice / 100;

            if (ReverseLogic.ValueBool)
            {
                if (_lastPrice < _keltnerDownLast && _keltnerDownLast < _smaLast)
                {
                    if (!BuySignalIsFiltered(candles))
                        _tab.BuyAtLimit(GetVolume(), _lastPrice + _slippage);
                }
            }
            else
            {
                if (_lastPrice > _keltnerUpLast && _keltnerUpLast > _smaLast)
                {
                    if (!BuySignalIsFiltered(candles))
                        _tab.BuyAtLimit(GetVolume(), _lastPrice + _slippage);
                }
            }

            if (ReverseLogic.ValueBool)
            {
                if (_lastPrice > _keltnerUpLast && _keltnerUpLast > _smaLast)
                {
                    if (!SellSignalIsFiltered(candles))
                        _tab.SellAtLimit(GetVolume(), _lastPrice - _slippage);
                }
            }
            else
            {
                if (_lastPrice < _keltnerDownLast && _keltnerDownLast < _smaLast)
                {
                    if (!SellSignalIsFiltered(candles))
                        _tab.SellAtLimit(GetVolume(), _lastPrice - _slippage);
                }
            }
        }

        private void LogicClosePosition(List<Candle> candles, Position position)
        {
            decimal _keltnerMiddleLine = _keltnerChannels.DataSeries[3].Last;
            decimal _smaLast = _sma.DataSeries[0].Last;

            if (position.State == PositionStateType.Closing ||
                position.CloseActiv == true ||
                (position.CloseOrders != null && position.CloseOrders.Count > 0))
            {
                return;
            }

            if (position.Direction == Side.Buy)
            {
                decimal activationPrice = _keltnerMiddleLine > _smaLast ? _keltnerMiddleLine : _smaLast; // when logic is reversed it can be dangerous
                decimal _slippage = Slippage.ValueDecimal * activationPrice / 100;

                if (ReverseLogic.ValueBool)
                {
                    _tab.CloseAtProfit(position, activationPrice, activationPrice + _slippage);
                }
                else
                {
                    _tab.CloseAtStop(position, activationPrice, activationPrice - _slippage);
                }
            }

            if (position.Direction == Side.Sell)
            {
                decimal activationPrice = _keltnerMiddleLine < _smaLast ? _keltnerMiddleLine : _smaLast; // same as when Side.Buy
                decimal _slippage = Slippage.ValueDecimal * activationPrice / 100;

                if (ReverseLogic.ValueBool)
                {
                    _tab.CloseAtProfit(position, activationPrice, activationPrice - _slippage);
                }
                else
                {
                    _tab.CloseAtStop(position, activationPrice, activationPrice + _slippage);
                }
            }
        }

        private bool BuySignalIsFiltered(List<Candle> candles)
        {
            decimal lastPrice = candles[candles.Count - 1].Close;
            decimal lastSma = _smaFilter.DataSeries[0].Last;

            DateTime dt = candles[candles.Count - 1].TimeStart; // CG LSR

            // filter for buy
            if (Regime.ValueString == "Off" ||
                Regime.ValueString == "OnlyShort" ||
                Regime.ValueString == "OnlyClosePosition")
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

            if (SmaPositionFilterIsOn.ValueBool)
            {
                if (_smaFilter.DataSeries[0].Last > lastPrice)
                {
                    return true;
                }
                // if the price is lower than the last Sma - return true to the top
            }

            if (SmaSlopeFilterIsOn.ValueBool)
            {
                decimal prevSma = _smaFilter.DataSeries[0].Values[_smaFilter.DataSeries[0].Values.Count - 2];

                if (lastSma < prevSma)
                {
                    return true;
                }
                // if the last Sma is lower than the previous Sma - return true to the top
            }

            return false;
        }

        private bool SellSignalIsFiltered(List<Candle> candles)
        {
            decimal lastPrice = candles[candles.Count - 1].Close;
            decimal lastSma = _smaFilter.DataSeries[0].Last;

            DateTime dt = candles[candles.Count - 1].TimeStart; // CG LSR

            // filter for sell
            if (Regime.ValueString == "Off" ||
                Regime.ValueString == "OnlyLong" ||
                Regime.ValueString == "OnlyClosePosition")
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

            if (SmaPositionFilterIsOn.ValueBool)
            {
                if (lastSma < lastPrice)
                {
                    return true;
                }
                // if the price is higher than the last Sma - return true to the top
            }
            if (SmaSlopeFilterIsOn.ValueBool)
            {
                decimal prevSma = _smaFilter.DataSeries[0].Values[_smaFilter.DataSeries[0].Values.Count - 2];

                if (lastSma > prevSma)
                {
                    return true;
                }
                // if the last Sma is higher than the previous Sma - return true to the top
            }

            return false;
        }

        private decimal GetVolume()
        {
            decimal volume = 0;

            if (VolumeRegime.ValueString == "Contract currency")
            {
                decimal contractPrice = TabsSimple[0].PriceBestAsk;
                volume = VolumeOnPosition.ValueDecimal / contractPrice;

            }
            else if (VolumeRegime.ValueString == "Number of contracts")
            {
                volume = VolumeOnPosition.ValueDecimal;
            }
            else // if (VolumeRegime.ValueString == "% of the total portfolio")
            {
                volume = _tab.Portfolio.ValueCurrent * (VolumeOnPosition.ValueDecimal / 100) / _tab.PriceBestAsk / _tab.Security.Lot;
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
