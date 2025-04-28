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

namespace OsEngine.Robots.TrigonumCustom.Channel.CoinGlass
{
    [Bot("BreakLinearRegressionChannelReverseCG")]
    public class BreakLinearRegressionChannelReverseCG : BotPanel
    {
        private BotTabSimple _tab;

        private StrategyParameterString Regime;
        private StrategyParameterBool ReverseLogic;
        private StrategyParameterDecimal VolumeOnPosition;
        private StrategyParameterString VolumeRegime;
        private StrategyParameterInt VolumeDecimals;
        private StrategyParameterDecimal Slippage;

        private StrategyParameterTimeOfDay TimeStart;
        private StrategyParameterTimeOfDay TimeEnd;

        private Aindicator _LinearRegression;
        private StrategyParameterDecimal UpDeviation;
        private StrategyParameterInt PeriodLR;

        private Aindicator _smaFilter;
        private StrategyParameterInt SmaLengthFilter;

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

        public BreakLinearRegressionChannelReverseCG(string name, StartProgram startProgram)
            : base(name, startProgram)
        {
            TabCreate(BotTabType.Simple);
            _tab = TabsSimple[0];

            Regime = CreateParameter("Regime", "Off", new[] { "Off", "On", "OnlyLong", "OnlyShort", "OnlyClosePosition" }, "Base");
            ReverseLogic = CreateParameter("Reverse logic", false, "Base");
            VolumeRegime = CreateParameter("Volume type", "Number of contracts", new[] { "Number of contracts", "Contract currency", "% of the total portfolio" }, "Base");
            VolumeDecimals = CreateParameter("Number of Digits after the decimal point in the volume", 2, 1, 50, 4, "Base");
            VolumeOnPosition = CreateParameter("Volume", 10, 1.0m, 50, 4, "Base");

            Slippage = CreateParameter("Slippage %", 0m, 0, 20, 1, "Base");

            TimeStart = CreateParameterTimeOfDay("Start Trade Time", 0, 0, 0, 0, "Base");
            TimeEnd = CreateParameterTimeOfDay("End Trade Time", 24, 0, 0, 0, "Base");

            PeriodLR = CreateParameter("Period Linear Regression", 50, 50, 300, 1, "Robot parameters");
            UpDeviation = CreateParameter("Deviation LR", 1, 0.1m, 3, 0.1m, "Robot parameters");

            //------------------------------------------------------- CG LSR
            ApiKey = CreateParameter("API Key", "", "Base");
            MinIntervalTimeFrame = CreateParameter("Min History Interval API", "1d", new[] { "1m", "5m", "15m", "30m", "1h", "4h", "6h", "8h", "12h", "1d" }, "Base");
            ExchageForLongShort = CreateParameter("Exchange for Long Short", "Binance", new[] { "Binance", "Bybit" }, "Base");
            LongShortRatioBuy = CreateParameter("Long Short Ratio Buy", 0.96078m, 0.94m, 0.98m, 0.0001m, "Base");
            LongShortRatioSell = CreateParameter("Long Short Ratio Sell", 1.04m, 0.94m, 0.98m, 0.0001m, "Base");
            LongShortRatioFilterIsOn = CreateParameter("Is Long Short Ratio Filter On", false, "Filters");
            PrintLSR = CreateParameter("Print LSR Data on Chart", false, "Filters");
            //------------------------------------------------------- CG LSR

            SmaLengthFilter = CreateParameter("Sma Length Filter", 100, 10, 500, 1, "Filters");

            _smaFilter = IndicatorsFactory.CreateIndicatorByName(nameClass: "Sma", name: name + "Sma_Filter", canDelete: false);
            _smaFilter = (Aindicator)_tab.CreateCandleIndicator(_smaFilter, nameArea: "Prime");
            _smaFilter.DataSeries[0].Color = System.Drawing.Color.Azure;
            _smaFilter.ParametersDigit[0].Value = SmaLengthFilter.ValueInt;
            _smaFilter.IsOn = true;
            _smaFilter.Save();

            _LinearRegression = IndicatorsFactory.CreateIndicatorByName("LinearRegressionChannelFast_Indicator", name + "LinearRegressionChannel", false);
            _LinearRegression = (Aindicator)_tab.CreateCandleIndicator(_LinearRegression, "Prime");
            _LinearRegression.ParametersDigit[0].Value = PeriodLR.ValueInt;
            _LinearRegression.ParametersDigit[1].Value = UpDeviation.ValueDecimal;
            _LinearRegression.ParametersDigit[2].Value = UpDeviation.ValueDecimal;
            _LinearRegression.Save();

            _tab.CandleFinishedEvent += _tab_CandleFinishedEvent;
            ParametrsChangeByUser += LinearRegressionTraderParam_ParametrsChangeByUser;
            LinearRegressionTraderParam_ParametrsChangeByUser();
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

        private void LinearRegressionTraderParam_ParametrsChangeByUser()
        {
            if (_LinearRegression.ParametersDigit[0].Value != PeriodLR.ValueInt ||
            _LinearRegression.ParametersDigit[1].Value != UpDeviation.ValueDecimal ||
            _LinearRegression.ParametersDigit[2].Value != UpDeviation.ValueDecimal)
            {
                _LinearRegression.ParametersDigit[0].Value = PeriodLR.ValueInt;
                _LinearRegression.ParametersDigit[1].Value = UpDeviation.ValueDecimal;
                _LinearRegression.ParametersDigit[2].Value = UpDeviation.ValueDecimal;
                _LinearRegression.Save();
                _LinearRegression.Reload();
            }

            if (_smaFilter.DataSeries.Count == 0)
            {
                return;
            }

            if (_smaFilter.ParametersDigit[0].Value != SmaLengthFilter.ValueInt)
            {
                _smaFilter.ParametersDigit[0].Value = SmaLengthFilter.ValueInt;
                _smaFilter.Reload();
                _smaFilter.Save();
            }
        }

        public override string GetNameStrategyType()
        {
            return "BreakLinearRegressionChannelReverseCG";
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

        // логика

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

            // использование
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

            if (candles.Count < 20)
            {
                return;
            }

            List<Position> positions = _tab.PositionsOpenAll;

            //------------------------------------------------------- CG LSR
            //если активирован PrintLSR - на последней свече пишем значение показателя и ставим цветную метку
            if (PrintLSR.ValueBool)
            {
                DrawLabelOnChart(candles, candles[candles.Count - 1].IsUp ? Side.Buy : Side.Sell);
            }
            //------------------------------------------------------- CG LSR

            if (positions.Count == 0)
            {
                TryOpenPosition(candles);
            }
            else
            {
                TryClosePosition(positions[0]);
            }
        }

        private bool BuySignalIsFiltered(List<Candle> candles)
        {
            decimal lastPrice = candles[candles.Count - 1].Close;
            decimal lastSma = _smaFilter.DataSeries[0].Last;

            DateTime dt = candles[candles.Count - 1].TimeStart; // CG LSR

            // фильтр для покупок
            if (Regime.ValueString == "Off" ||
                Regime.ValueString == "OnlyShort" ||
                Regime.ValueString == "OnlyClosePosition")
            {
                return true;
                //если режим работы робота не соответсвует направлению позиции
            }

            if (_smaFilter.DataSeries[0].Last > lastPrice)
            {
                return true;
            }
            // если цена ниже последней сма - возвращаем на верх true

            decimal prevSma = _smaFilter.DataSeries[0].Values[_smaFilter.DataSeries[0].Values.Count - 2];

            if (lastSma < prevSma)
            {
                return true;
            }
            // если последняя сма ниже предыдущей сма - возвращаем на верх true

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
            decimal lastSma = _smaFilter.DataSeries[0].Last;

            DateTime dt = candles[candles.Count - 1].TimeStart; // CG LSR

            // фильтр для продаж
            if (Regime.ValueString == "Off" ||
                Regime.ValueString == "OnlyLong" ||
                Regime.ValueString == "OnlyClosePosition")
            {
                return true;
                //если режим работы робота не соответсвует направлению позиции
            }

            if (lastSma < lastPrice)
            {
                return true;
            }
            // если цена выше последней сма - возвращаем на верх true

            decimal prevSma = _smaFilter.DataSeries[0].Values[_smaFilter.DataSeries[0].Values.Count - 2];

            if (lastSma > prevSma)
            {
                return true;
            }
            // если последняя сма выше предыдущей сма - возвращаем на верх true

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

        private void TryOpenPosition(List<Candle> candles)
        {
            decimal upChannel = _LinearRegression.DataSeries[0].Last;
            decimal downChannel = _LinearRegression.DataSeries[2].Last;

            if (upChannel == 0 ||
                downChannel == 0)
            {
                return;
            }

            bool signalUpLine = candles[candles.Count - 1].Close > upChannel;
            bool signalDownLine = candles[candles.Count - 1].Close < downChannel;

            if (signalUpLine) // При пересечении верхней линии канала
            {
                if (ReverseLogic.ValueBool)
                {
                    if (!SellSignalIsFiltered(candles))//если метод возвращает false можно входить в сделку
                        _tab.SellAtLimit(GetVolume(), upChannel - GetSlippage(upChannel));
                }
                else
                {
                    if (!BuySignalIsFiltered(candles))//если метод возвращает false можно входить в сделку
                        _tab.BuyAtLimit(GetVolume(), upChannel + GetSlippage(upChannel));
                }
            }
            else if (signalDownLine) // При пересечении нижней линии канала
            {
                if (ReverseLogic.ValueBool)
                {
                    if (!BuySignalIsFiltered(candles))//если метод возвращает false можно входить в сделку
                        _tab.BuyAtLimit(GetVolume(), upChannel + GetSlippage(upChannel));
                }
                else
                {
                    if (!SellSignalIsFiltered(candles))//если метод возвращает false можно входить в сделку
                        _tab.SellAtLimit(GetVolume(), upChannel - GetSlippage(upChannel));
                }
            }
        }

        private void TryClosePosition(Position position)
        {
            decimal upChannel = _LinearRegression.DataSeries[0].Last;
            decimal downChannel = _LinearRegression.DataSeries[2].Last;

            if (upChannel == 0 ||
                downChannel == 0)
            {
                return;
            }

            if (position.Direction == Side.Buy)
            {
                if (ReverseLogic.ValueBool)
                {
                    _tab.CloseAtProfit(position, upChannel, upChannel - GetSlippage(upChannel));
                }
                else
                {
                    _tab.CloseAtStop(position, downChannel, downChannel - GetSlippage(downChannel));
                }
            }
            else if (position.Direction == Side.Sell)
            {
                if (ReverseLogic.ValueBool)
                {
                    _tab.CloseAtProfit(position, downChannel, downChannel + GetSlippage(downChannel));
                }
                else
                {
                    _tab.CloseAtStop(position, upChannel, upChannel + GetSlippage(upChannel));
                }
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
            else //if (VolumeRegime.ValueString == "% of the total portfolio")
            {
                volume = _tab.Portfolio.ValueCurrent * (VolumeOnPosition.ValueDecimal / 100) / _tab.PriceBestAsk / _tab.Security.Lot;
            }

            // If the robot is running in the tester
            if (StartProgram == StartProgram.IsTester)
            {
                volume = Math.Round(volume, 6);
            }
            else
            {
                volume = Math.Round(volume, _tab.Security.DecimalsVolume);
            }

            return volume;

        }

        private decimal GetSlippage(decimal price)
        {
            return price * Slippage.ValueDecimal / 100;
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
