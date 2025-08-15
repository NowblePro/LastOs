using OsEngine.Charts.CandleChart.Indicators;
using OsEngine.Entity;
using OsEngine.Indicators;
using OsEngine.Market.Servers.Bitfinex.BitfitnexEntity;
using OsEngine.OsTrader.Panels;
using OsEngine.OsTrader.Panels.Attributes;
using OsEngine.OsTrader.Panels.Tab;
using OsEngine.Robots.Classes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;

namespace OsEngine.Robots.TrigonumCustom.Channel
{
    [Bot("FisherChannelBot")]
    public class FisherChannelBot : BotPanel
    {
        #region String constants
        private const string NUMBER_OF_CONTRACTS = "Number Of Contracts";
        private const string CONTRACT_CURRENCY = "Contract currency";
        private const string PERCENT = "Percent";
        private const string ON = "On";
        private const string OFF = "Off";
        private const string ONLY_SHORT = "OnlyShort";
        private const string ONLY_LONG = "OnlyLong";
        private const string ONLY_CLOSE_POSITION = "OnlyClosePosition";
        #endregion
        /// <summary> Период, за который считается индикатор Фишера </summary>
        private StrategyParameterInt fisherPeriod;
        /// <summary> Размер окна для сглаженного значения Фишера </summary>
        private StrategyParameterInt smaPeriod;
        /// <summary> Период, в течении которого анализируются графики Фишера и цены на предмет дивергенции </summary>
        private StrategyParameterInt period;
        /// <summary> Значение на графике Фишера, при котором засчитывается перекупленность </summary>
        private StrategyParameterDecimal topLine;
        /// <summary> Значение на графике Фишера, при котором засчитывается перепроданность </summary>
        private StrategyParameterDecimal bottomLine;
        private StrategyParameterString Regime;
        /// <summary>
        /// Величина, показывающая необходимую разницу экстремумов для срабатывания логики покупки/продажи
        /// </summary>
        private StrategyParameterDecimal fisherDelta;
        /// <summary> Аналогично <see cref="fisherDelta"/> для графика цены</summary>
        private StrategyParameterDecimal priceDelta;
        private StrategyParameterString volumeType;
        private StrategyParameterDecimal slippage;
        private StrategyParameterDecimal volumeOnPosition;

        private StrategyParameterBool _saveJson;

        #region Trailing stop
        private TrailingStop trailingStop;
        private StrategyParameterBool TrailingStopIsOn;
        private StrategyParameterString TrailingStopTypeOrder;
        private StrategyParameterDecimal ChangeStepStop;
        private StrategyParameterDecimal MinDist;
        private StrategyParameterDecimal QuantityStepsPrices;
        private StrategyParameterString PointOrPercent;
        #endregion

        #region ATR
        private StrategyParameterInt LengthAtr;
        private StrategyParameterDecimal MultiplierAtr;
        private StrategyParameterBool AtrFilterIsOn;

        Aindicator _ATR;

        private decimal _lastAtr;
        private decimal _averageAtr;
        private decimal _lastCandleClose;
        private bool _needUpdateLastIndex;
        private bool _needUpdateIterator;
        private int _iterator = 1;
        #endregion

        private BotTabSimple tab;
        private FisherTransformIndicator fisher;
        private Aindicator ps;

        private FisherData fisherData = new FisherData();

        private decimal lastSar;
        private decimal lastPrice;

        public FisherChannelBot(string name, StartProgram startProgram) : base(name, startProgram)
        {
            TabCreate(BotTabType.Simple);
            tab = TabsSimple[0];

            tab.GetChartMaster().ChartClickEvent += FisherChannelBot_ChartClickEvent;

            Regime = CreateParameter("Regime", "Off", new[] { OFF, ON, ONLY_LONG, ONLY_SHORT, ONLY_CLOSE_POSITION }, "Base");
            slippage = CreateParameter("Slippage", 0.1m, 0.1m, 5, 0.1m, "Base");

            _saveJson = CreateParameter("Save Json Data", false, "Base");

            topLine = CreateParameter("TopLine", 0.5m, 0.5m, 2m, 0.1m, "Base");
            bottomLine = CreateParameter("BottomLine", -0.5m, -2m, -0.5m, 0.1m, "Base");

            period = CreateParameter("Period", 20, 2, 50, 1, "Base");
            fisherDelta = CreateParameter("FisherDelta", 0.1m, 0.1m, 3, 0.05m, "Base");
            priceDelta = CreateParameter("PriceDelta", 50m, 1m, 200, 1m, "Base");

            fisherPeriod = CreateParameter("FisherPeriod", 10, 2, 50, 1, "Fisher");
            smaPeriod = CreateParameter("SMAPeriod", 3, 2, 50, 1, "Fisher");
            volumeType = CreateParameter("Volume Type", NUMBER_OF_CONTRACTS, new string[] { NUMBER_OF_CONTRACTS, CONTRACT_CURRENCY, PERCENT }, "Base");
            volumeOnPosition = CreateParameter("Volume", 10, 1.0m, 50, 4, "Base");

            #region Trailing init
            TrailingStopIsOn = CreateParameter("Is Trailing stop On", false, "Trailing Stop");
            TrailingStopTypeOrder = CreateParameter("Type order", OrderPriceType.Market.ToString(), new[] { OrderPriceType.Market.ToString(), OrderPriceType.Limit.ToString() }, "Trailing Stop");
            PointOrPercent = CreateParameter("Choise Points or Percent", "Points", new[] { "Points", "Percent" }, "Trailing Stop");
            ChangeStepStop = CreateParameter("Stop level change step", 100, 1, 10000, 001m, "Trailing Stop");
            MinDist = CreateParameter("Minimum distance to price", 500, 1, 10000, 0.01m, "Trailing Stop");
            QuantityStepsPrices = CreateParameter("Quantity steps prices for limit order", 1000m, 0, 10000, 1, "Trailing Stop");
            #endregion

            #region ATR
            LengthAtr = CreateParameter("Length ATR", 96, 7, 1000, 1, "ATR");
            MultiplierAtr = CreateParameter("Multiplier Atr", 1, 1m, 10, 1, "ATR");
            AtrFilterIsOn = CreateParameter("Is Atr Filter On", false, "ATR");
            _ATR = IndicatorsFactory.CreateIndicatorByName("ATR", name + "Atr", false);
            _ATR = (Aindicator)tab.CreateCandleIndicator(_ATR, "NewArea");
            #endregion

            ps = IndicatorsFactory.CreateIndicatorByName(nameClass: "ParabolicSAR", name: name + "Parabolic", canDelete: false);
            ps = (Aindicator)tab.CreateCandleIndicator(ps, nameArea: "Prime");

            if (tab.Indicators.OfType<FisherTransformIndicator>().SingleOrDefault() is FisherTransformIndicator loaded)
            {
                fisher = loaded;
            }
            else
            {
                fisher = new FisherTransformIndicator("Fisher Indicator", false);
            }
            fisher.PaintOn = true;

            tab.CreateCandleIndicator(fisher, "Fisher");

            UpdateParameters();

            tab.CandleFinishedEvent += Tab_CandleFinishedEvent;
            tab.PositionOpeningSuccesEvent += Tab_PositionOpeningSuccesEvent;
            ParametrsChangeByUser += Stoch_ParametrsChangeByUser;
        }

        private Series maximumsFisherSeries = null;
        private Series maximumsPriceSeries = null;

        private Series minimumsFisherSeries = null;
        private Series minimumsPriceSeries = null;

        private Series topFisherLine = null;
        private Series bottomFisherLine = null;

        private void FisherChannelBot_ChartClickEvent(object sender, MouseEventArgs e)
        {
            if (sender is Chart chart)
            {
                System.Drawing.Point points = e.Location;

                if (maximumsPriceSeries == null)
                {
                    maximumsPriceSeries = new Series();
                    maximumsPriceSeries.ChartType = SeriesChartType.Line;
                    maximumsPriceSeries.Color = System.Drawing.Color.Red;
                    maximumsPriceSeries.BorderWidth = 3;
                    maximumsPriceSeries.BorderDashStyle = ChartDashStyle.Solid;
                    chart.Series.Add(maximumsPriceSeries);
                }

                if (minimumsPriceSeries == null)
                {
                    minimumsPriceSeries = new Series();
                    minimumsPriceSeries.ChartType = SeriesChartType.Line;
                    minimumsPriceSeries.Color = System.Drawing.Color.Blue;
                    minimumsPriceSeries.BorderWidth = 3;
                    minimumsPriceSeries.BorderDashStyle = ChartDashStyle.Solid;
                    chart.Series.Add(minimumsPriceSeries);
                }

                if (maximumsFisherSeries == null)
                {
                    maximumsFisherSeries = new Series();
                    maximumsFisherSeries.ChartType = SeriesChartType.Line;
                    maximumsFisherSeries.Color = System.Drawing.Color.Red;
                    maximumsFisherSeries.BorderWidth = 3;
                    maximumsFisherSeries.BorderDashStyle = ChartDashStyle.Solid;
                    try
                    {
                        ChartArea area = chart.ChartAreas.Where(a => a.Name.Contains("Fisher")).FirstOrDefault();
                        maximumsFisherSeries.ChartArea = area.Name;
                    }
                    catch { }
                    chart.Series.Add(maximumsFisherSeries);
                }

                if (minimumsFisherSeries == null)
                {
                    minimumsFisherSeries = new Series();
                    minimumsFisherSeries.ChartType = SeriesChartType.Line;
                    minimumsFisherSeries.Color = System.Drawing.Color.Blue;
                    minimumsFisherSeries.BorderWidth = 3;
                    minimumsFisherSeries.BorderDashStyle = ChartDashStyle.Solid;
                    try
                    {
                        ChartArea area = chart.ChartAreas.Where(a => a.Name.Contains("Fisher")).FirstOrDefault();
                        minimumsFisherSeries.ChartArea = area.Name;
                    }
                    catch { }
                    chart.Series.Add(minimumsFisherSeries);
                }

                if (topFisherLine == null)
                {
                    topFisherLine = new Series();
                    topFisherLine.ChartType = SeriesChartType.Line;
                    topFisherLine.Color = System.Drawing.Color.Yellow;
                    topFisherLine.BorderWidth = 2;
                    topFisherLine.BorderDashStyle = ChartDashStyle.Dash;
                    try
                    {
                        ChartArea area = chart.ChartAreas.Where(a => a.Name.Contains("Fisher")).FirstOrDefault();
                        topFisherLine.ChartArea = area.Name;
                    }
                    catch { }
                    chart.Series.Add(topFisherLine);
                }

                if (bottomFisherLine == null)
                {
                    bottomFisherLine = new Series();
                    bottomFisherLine.ChartType = SeriesChartType.Line;
                    bottomFisherLine.Color = System.Drawing.Color.Yellow;
                    bottomFisherLine.BorderWidth = 2;
                    bottomFisherLine.BorderDashStyle = ChartDashStyle.Dash;
                    try
                    {
                        ChartArea area = chart.ChartAreas.Where(a => a.Name.Contains("Fisher")).FirstOrDefault();
                        bottomFisherLine.ChartArea = area.Name;
                    }
                    catch { }
                    chart.Series.Add(bottomFisherLine);
                }

                // Преобразуем координаты в значения графика
                try
                {
                    var result = chart.HitTest(points.X, points.Y);

                    // Проверяем, был ли клик по точке данных
                    if (result.ChartElementType == ChartElementType.DataPoint && result.Series.ChartType == SeriesChartType.Candlestick)
                    {
                        maximumsPriceSeries.Points.Clear();
                        minimumsPriceSeries.Points.Clear();
                        maximumsFisherSeries.Points.Clear();
                        minimumsFisherSeries.Points.Clear();
                        topFisherLine.Points.Clear();
                        bottomFisherLine.Points.Clear();
                        // Получаем индекс серии и точку данных
                        int pointIndex = result.PointIndex;
                        // Получаем значение точки данных
                        double value = result.Series.Points[pointIndex].YValues[0];
                        FisherData fish = new FisherData();
                        
                        int skip = pointIndex - period.ValueInt;
                        int take = period.ValueInt;
                        IEnumerable<decimal> fisherValues = fisher.ValuesToChart[0].Skip(skip).Take(take);
                        IEnumerable<decimal> fisherSma = fisher.ValuesToChart[1].Skip(skip).Take(take);
                        IEnumerable<Candle> candles = tab.GetChartMaster().Candles.Skip(skip).Take(take);
                        fish.UpdateExtremums(fisherValues, fisherSma, candles, topLine.ValueDecimal, bottomLine.ValueDecimal);

                        StringBuilder reportLong = new StringBuilder();
                        StringBuilder reportShort = new StringBuilder();
                        CheckEnterPosition(fish, out bool @long, out bool @short, reportLong, reportShort);

                        if (reportShort.Length > 0)
                        {
                            int x = points.X * 100 / chart.Width;
                            int y = points.Y * 100 / chart.Height;

                            EditTextAnnotation(chart,$"Short: {reportShort}\r\nLong: {reportLong}", x, y);
                        }

                        ApproximatingExtremums(fish.CurrentMaxPrices);
                        ApproximatingExtremums(fish.CurrentMaximums);
                        ApproximatingExtremums(fish.CurrentMinPrices);
                        ApproximatingExtremums(fish.CurrentMinimums);

                        for (int i = 0; i < take; i++)
                        {
                            double max = (double)fish.CurrentMaxPrices[i];

                            if (max > 0)
                            {
                                maximumsPriceSeries.Points.AddXY(i + skip, max);
                            }
                        }

                        for (int i = 0; i < take; i++)
                        {
                            double max = (double)fish.CurrentMaximums[i];

                            if (max > 0)
                            {
                                maximumsFisherSeries.Points.AddXY(i + skip, max);
                            }
                        }

                        for (int i = 0; i < take; i++)
                        {
                            double min = (double)fish.CurrentMinPrices[i];

                            if (min > 0)
                            {
                                minimumsPriceSeries.Points.AddXY(i + skip, min);
                            }
                        }

                        for (int i = 0; i < take; i++)
                        {
                            double min = (double)fish.CurrentMinimums[i];

                            if (min != 0)
                            {
                                minimumsFisherSeries.Points.AddXY(i + skip, min);
                            }
                        }

                        for (int i = 0; i < take; i++)
                        {
                            bottomFisherLine.Points.AddXY(i + skip, bottomLine.ValueDecimal);
                        }

                        for (int i = 0; i < take; i++)
                        {
                            topFisherLine.Points.AddXY(i + skip, topLine.ValueDecimal);
                        }
                       
                        chart.ChartAreas[0].AxisY.Minimum = chart.ChartAreas[0].AxisY2.Minimum;
                        chart.ChartAreas[0].AxisY.Maximum = chart.ChartAreas[0].AxisY2.Maximum;

                        chart.ChartAreas[1].AxisY.Minimum = chart.ChartAreas[1].AxisY2.Minimum;
                        chart.ChartAreas[1].AxisY.Maximum = chart.ChartAreas[1].AxisY2.Maximum;

                        chart.ChartAreas[2].AxisY.Minimum = chart.ChartAreas[2].AxisY2.Minimum;
                        chart.ChartAreas[2].AxisY.Maximum = chart.ChartAreas[2].AxisY2.Maximum;

                        void ApproximatingExtremums(List<decimal> values)
                        {
                            int lastIndex = values.FindLastIndex(item => item != 0);
                            if (lastIndex < 0) return;
                            int startIndex = values.FindIndex(item => item != 0);
                            if (startIndex < 0) return;
                            double length = lastIndex - startIndex;
                            double c = (double)(values[lastIndex] - values[startIndex]);
                            double k = c / length;
                            for (int i = 1; i < lastIndex - startIndex; i++)
                            {
                                values[startIndex + i] = values[startIndex] + (decimal)(k * i);
                            }

                            for (int i = 0; i < values.Count; i++)
                            {
                                if (i < startIndex || i > lastIndex)
                                {
                                    values[i] = 0;
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {

                }
            }
        }

        private TextAnnotation annotation = new TextAnnotation();

        private void EditTextAnnotation(Chart chart, string text, float x, float y)
        {
            annotation.Text = text;
            annotation.X = x;
            annotation.Y = y;
            annotation.AnchorX = x;
            annotation.AnchorY = y;
            annotation.ForeColor = System.Drawing.Color.Yellow; // Цвет текста
            annotation.Font = new System.Drawing.Font("Arial", 10); // Шрифт
            chart.Annotations.Remove(annotation);
            chart.Annotations.Add(annotation);
        }

        private void Stoch_ParametrsChangeByUser()
        {
            UpdateParameters();
        }

        private void SetTrailingParameters()
        {
            if (TrailingStopIsOn.ValueBool)
            {
                trailingStop = new TrailingStop(tab, TrailingStopTypeOrder.ValueString, ChangeStepStop.ValueDecimal, MinDist.ValueDecimal, QuantityStepsPrices.ValueDecimal, PointOrPercent.ValueString);
            }
        }

        private void SetFisherParameters()
        {
            fisher.FisherPeriod = fisherPeriod.ValueInt;
            fisher.SmaPeriod = smaPeriod.ValueInt;
        }

        private void SetATRParameters()
        {
            ((IndicatorParameterInt)_ATR.Parameters[0]).ValueInt = LengthAtr.ValueInt;
            _ATR.Save();
            _ATR.Reload();
        }

        private void UpdateParameters()
        {
            tab.setSaveData(_saveJson.ValueBool);
            SetFisherParameters();
            SetTrailingParameters();
            SetATRParameters();
        }

        private void Tab_PositionOpeningSuccesEvent(Position position)
        {
            tab.SellAtStopCancel();
            tab.BuyAtStopCancel();

            if (TrailingStopIsOn.ValueBool)
            {
                trailingStop?.SetTrailingStop(position.EntryPrice);
            }
        }

        private void Tab_CandleFinishedEvent(List<Candle> candles)
        {
            if (candles.Count < 2 || fisher.FisherPeriod > candles.Count || period.ValueInt > candles.Count) return;
            lastSar = ps.DataSeries[0].Last;
            lastPrice = candles[candles.Count - 1].Close;

            if (lastSar == 0)
            {
                return;
            }
            int skip = candles.Count - period.ValueInt;
            IEnumerable<Candle> lastCandles = candles.Skip(skip);
            IEnumerable<decimal> fisherValues = fisher.ValuesToChart[0].Skip(skip);
            IEnumerable<decimal> fisherSma = fisher.ValuesToChart[1].Skip(skip);
            List<Position> positions = tab.PositionsOpenAll;

            fisherData.UpdateExtremums(fisherValues, fisherSma, lastCandles, topLine.ValueDecimal, bottomLine.ValueDecimal);
            if (Regime.ValueString == OFF) return;

            if (positions.Count == 0 && Regime.ValueString != ONLY_CLOSE_POSITION)
            {
                CheckEnterPosition(fisherData, out bool @long, out bool @short);
                if (@long)
                {
                    decimal slippage = this.slippage.ValueDecimal * lastPrice / 100;
                    tab.BuyAtLimit(GetVolume(), lastPrice + slippage);
                    fisherData.CurrentMinimums.Clear();
                }
                else if (@short)
                {
                    decimal slippage = this.slippage.ValueDecimal * lastPrice / 100;
                    tab.SellAtLimit(GetVolume(), lastPrice - slippage);
                    fisherData.CurrentMaximums.Clear();
                }
            }
            else 
            {
                CheckStop(positions, candles);
            }
        }

        class FisherData
        {
            private List<decimal> currentFisher = new List<decimal>();
            private List<decimal> currentFisherSma = new List<decimal>();
            private List<decimal> currentMaximums = new List<decimal>();
            private List<decimal> currentMinimums = new List<decimal>();
            private List<decimal> currentMaxPrices = new List<decimal>();
            private List<decimal> currentMinPrices = new List<decimal>();
            private List<Candle> currentLastCandles = new List<Candle>();

            public List<decimal> CurrentMaximums => currentMaximums;
            public List<decimal> CurrentMinimums => currentMinimums;
            public List<decimal> CurrentMaxPrices => currentMaxPrices;
            public List<decimal> CurrentMinPrices => currentMinPrices;

            public void Clear()
            {
                currentFisher.Clear();
                currentFisherSma.Clear();

                currentMaximums.Clear();
                currentMinimums.Clear();
                currentMaxPrices.Clear();
                currentMinPrices.Clear();
                currentLastCandles.Clear();
            }

            public void UpdateFisherValues(IEnumerable<decimal> fisherValues)
            {
                if (currentFisher.Any())
                {
                    currentFisher.Clear();
                }
                currentFisher.AddRange(fisherValues);
            }

            public void UpdateFisherSma(IEnumerable<decimal> fisherSma)
            {
                if (currentFisherSma.Any())
                {
                    currentFisherSma.Clear();
                }
                currentFisherSma.AddRange(fisherSma);
            }

            public void UpdateLastCandles(IEnumerable<Candle> candles)
            {
                if (currentLastCandles.Any())
                {
                    currentLastCandles.Clear();
                }
                currentLastCandles.AddRange(candles);
            }

            /// <summary>
            /// Обновить максимумы и минимумы за последние <see cref="period"/> свечей
            /// </summary>
            /// <param name="fisherValues"></param>
            /// <param name="fisherSma"></param>
            /// <param name="candles"></param>
            public void UpdateExtremums(IEnumerable<decimal> fisherValues, IEnumerable<decimal> fisherSma, IEnumerable<Candle> candles, decimal topLine, decimal bottomLine)
            {
                Clear();
                UpdateFisherValues(fisherValues);
                UpdateFisherSma(fisherSma);
                UpdateLastCandles(candles);

                decimal prevFish = 0;
                decimal prevSma = 0;
                decimal top = topLine;
                decimal bot = bottomLine;

                for (int i = 0; i < currentFisher.Count; i++)
                {
                    decimal curFish = currentFisher[i];
                    decimal curSma = currentFisherSma[i];

                    if (curFish > top && curSma > top && prevFish > top && prevSma > top)
                    {
                        // Поиск максимумов
                        if (prevFish > prevSma && curFish < curSma)
                        {
                            currentMaximums.Add(curSma);
                        }
                        else
                        {
                            currentMaximums.Add(0);
                        }
                    }
                    else
                    {
                        currentMaximums.Add(0);
                    }

                    if (curFish < bot && curSma < bot && prevFish < bot && prevSma < bot)
                    {
                        // Поиск минимумов
                        if (prevSma > prevFish && curSma < curFish)
                        {
                            currentMinimums.Add(curSma);
                        }
                        else
                        {
                            currentMinimums.Add(0);
                        }
                    }
                    else
                    {
                        currentMinimums.Add(0);
                    }

                    prevFish = curFish;
                    prevSma = curSma;
                }

                // После поиска экстремумов, проверить цену во время этих экстремумов
                for (int i = 0; i < currentMaximums.Count; i++)
                {
                    if (currentMaximums[i] != 0)
                    {
                        currentMaxPrices.Add(currentLastCandles[i].High);
                    }
                    else
                    {
                        currentMaxPrices.Add(0);
                    }
                }

                for (int i = 0; i < currentMinimums.Count; i++)
                {
                    if (currentMinimums[i] != 0)
                    {
                        currentMinPrices.Add(currentLastCandles[i].Low);
                    }
                    else
                    {
                        currentMinPrices.Add(0);
                    }
                }
            }
        }

        private bool CheckEnterLong(FisherData fisherData, StringBuilder report = null)
        {
            bool debug = report != null;
            bool result = true;
            //Оставить 2 последних максимума
            int lastExtremumsCount = 2;
            List<decimal> currentMinimums = fisherData.CurrentMinimums;
            List<decimal> currentMinPrices = fisherData.CurrentMinPrices;
            int minCount = currentMinimums.Where(i => i != 0).Count();

            if (Regime.ValueString == ONLY_SHORT)
            {
                if (debug)
                {
                    report.AppendLine("Regime == ONLY SHORT;");
                }
                result = false;
            }

            if (minCount < 2)
            {
                if (debug)
                {
                    report.AppendLine("Количество минимумов в периоде меньше двух;");
                }
                result = false;
                return result;
            }

            if (currentMinimums.Last() == 0)
            {
                if (debug)
                {
                    report.AppendLine("Минимум не на последней свече;");
                }
                result = false;
            }

            currentMinimums = currentMinimums.Where(i => i != 0).Skip(minCount - lastExtremumsCount).ToList();
            currentMinPrices = currentMinPrices.Where(i => i != 0).Skip(minCount - lastExtremumsCount).ToList();
            decimal firstMax = currentMinimums.First();
            decimal secondMax = currentMinimums.Last();
            decimal firstPrice = currentMinPrices.First();
            decimal secondPrice = currentMinPrices.Last();

            if (secondMax - firstMax < fisherDelta.ValueDecimal)
            {
                if (debug)
                {
                    report.AppendLine($"secondMax - firstMax < fisherDelta.ValueDecimal, {secondMax} - {firstMax} = {secondMax - firstMax} < {fisherDelta.ValueDecimal};");
                }
                result = false;
            }

            if (firstPrice - secondPrice < priceDelta.ValueDecimal)
            {
                if (debug)
                {
                    report.AppendLine($"firstPrice - secondPrice < priceDelta.ValueDecimal, {firstPrice} - {secondPrice} = {firstPrice - secondPrice} < {priceDelta.ValueDecimal};");
                }
                result = false;
            }

            return result;
        }

        private bool CheckEnterShort(FisherData fisherData, StringBuilder report = null)
        {
            bool debug = report != null;
            bool result = true;
            //Оставить 2 последних максимума
            int lastExtremumsCount = 2;
            List<decimal> currentMaximums = fisherData.CurrentMaximums;
            List<decimal> currentMaxPrices = fisherData.CurrentMaxPrices;
            int maxCount = currentMaximums.Where(i => i != 0).Count();

            if (Regime.ValueString == ONLY_LONG)
            {
                if (debug)
                {
                    report.AppendLine("Regime == ONLY LONG;");
                }
                result = false;
            }

            if (maxCount < 2)
            {
                if (debug)
                {
                    report.AppendLine("Количество максимумов в периоде меньше двух;");
                }
                result = false;
                return result;
            }

            if (currentMaximums.Last() == 0)
            {
                if (debug)
                {
                    report.AppendLine("Максимум не на последней свече;");
                }
                result = false;
            }

            currentMaximums = currentMaximums.Where(i => i != 0).Skip(maxCount - lastExtremumsCount).ToList();
            currentMaxPrices = currentMaxPrices.Where(i => i != 0).Skip(maxCount - lastExtremumsCount).ToList();
            decimal firstMax = currentMaximums.First();
            decimal secondMax = currentMaximums.Last();
            decimal firstPrice = currentMaxPrices.First();
            decimal secondPrice = currentMaxPrices.Last();

            if (result)
            {
                if (firstMax - secondMax < fisherDelta.ValueDecimal)
                {
                    if (debug)
                    {
                        report.AppendLine($"firstMax - secondMax < fisherDelta.ValueDecimal, {firstMax} - {secondMax} = {firstMax - secondMax} < {fisherDelta.ValueDecimal};");
                    }
                    result = false;
                }

                if (secondPrice - firstPrice < priceDelta.ValueDecimal)
                {
                    if (debug)
                    {
                        report.AppendLine($"secondPrice - firstPrice < priceDelta.ValueDecimal, {secondPrice} - {firstPrice} = {secondPrice - firstPrice} < {priceDelta.ValueDecimal};");
                    }
                    result = false;
                }
            }
            
            return result;
        }

        private void CheckEnterPosition(FisherData fisherData, out bool @long, out bool @short, StringBuilder reportLong = null, StringBuilder reportShort = null)
        {
            @long = CheckEnterLong(fisherData, reportLong);
            @short = CheckEnterShort(fisherData, reportShort);
        }

        private void CheckStop(List<Position> positions, List<Candle> candles)
        {
            if (TrailingStopIsOn.ValueBool)
            {
                trailingStop.SetTrailingStop(candles.Last().Close);
                return;
            }

            if (AtrFilterIsOn.ValueBool)
            {
                if (AtrLogic(candles, candles.Last().Close))
                {
                    return;
                }
            }

            List<decimal> currentMaximums = fisherData.CurrentMaximums;
            List<decimal> currentMinimums = fisherData.CurrentMinimums;

            for (int i = 0; i < positions.Count; i++)
            {
                tab.SellAtStopCancel();
                tab.BuyAtStopCancel();
                Position pos = positions[i];

                if (pos.CloseActiv == true && pos.CloseOrders != null && pos.CloseOrders.Count > 0)
                {
                    continue;
                }

                decimal priceOrder = lastSar;
                decimal _slippage = slippage.ValueDecimal * priceOrder / 100;

                if (pos.Direction == Side.Buy && currentMaximums.LastOrDefault() != 0)
                {
                    tab.CloseAtStop(pos, lastPrice, lastPrice - _slippage);
                }
                else if (pos.Direction == Side.Sell && currentMinimums.LastOrDefault() != 0)
                {
                    tab.CloseAtStop(pos, lastPrice, lastPrice + _slippage);
                }
            }
        }

        private decimal GetVolume()
        {
            decimal volume = 0;
            decimal contractPrice = tab.PriceBestAsk == 0 ? 1 : TabsSimple[0].PriceBestAsk;
            if (volumeType.ValueString == CONTRACT_CURRENCY)
            {
                volume = volumeOnPosition.ValueDecimal / contractPrice;
            }
            else if (volumeType.ValueString == NUMBER_OF_CONTRACTS)
            {
                volume = volumeOnPosition.ValueDecimal;
            }
            else if (volumeType.ValueString == PERCENT)
            {
                decimal lot = tab.Security.Lot == 0 ? 1 : tab.Security.Lot;
                volume = tab.Portfolio.ValueCurrent * (volumeOnPosition.ValueDecimal / 100) / contractPrice / lot;
            }

            if (StartProgram == StartProgram.IsTester)
            {
                volume = Math.Round(volume, 6);
            }
            else
            {
                volume = GetRoundedVolume(tab, volume);
            }
            return volume;
        }

        private bool AtrLogic(List<Candle> candles, decimal lastCandle)
        {
            if (_ATR.DataSeries[0].Last == 0 && _needUpdateIterator)
            {
                _lastCandleClose = 0;
                _averageAtr = 0;
                _iterator = 1;
                _needUpdateIterator = false;
            }

            if (candles.Count < LengthAtr.ValueInt)
            {
                return true;
            }

            _lastAtr = _ATR.DataSeries[0].Last;

            if (_ATR.DataSeries[0].Values.Count >= LengthAtr.ValueInt * _iterator)
            {
                _lastCandleClose = lastCandle;
                _averageAtr = _lastAtr;
                _iterator++;
                _needUpdateLastIndex = false;
                _needUpdateIterator = true;
            }

            if (_needUpdateLastIndex || Math.Abs(lastCandle - _lastCandleClose) > _averageAtr * MultiplierAtr.ValueDecimal)
            {
                if (tab.PositionsOpenAll.Count > 0)
                {
                    CancelStopsAndProfits();
                }
                _needUpdateLastIndex = true;
                return true;
            }

            return false;
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

        public override string GetNameStrategyType() => $"{nameof(FisherChannelBot)}";

        public override void ShowIndividualSettingsDialog() { }
    }
}
