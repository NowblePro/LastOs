using OsEngine.Common;
using OsEngine.Entity;
using OsEngine.Indicators;
using OsEngine.OsTrader.Panels.Attributes;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms.DataVisualization.Charting;

namespace OsEngine.Robots.TrigonumCustom.Base
{
    [Bot("DivergenceRsi")]
    public class DivergenceRsi : BotPanelSimple
    {
        private StrategyParameterInt _period;
        private StrategyParameterInt _rsiPeriod;
        //private StrategyParameterInt _rsiOverSold;
        //private StrategyParameterInt _rsiOverBought;

        /// <summary>
        /// Минимальное расстояние в индексах между пиками цены.
        /// </summary>
        private StrategyParameterInt _minDistance;
        /// <summary>
        /// Максимальное расстояние в индексах между пиками цены.
        /// </summary>
        private StrategyParameterInt _maxDistance;
        /// <summary>
        /// Допуск совпадения индексов цены и RSI.
        /// </summary>
        private StrategyParameterInt _syncTolerance;
        private StrategyParameterInt _extremaOrder;
        //private StrategyParameterInt _minDivergenceStrength;
        private List<LiquiditySweep> currentDivergencePriceBear = new List<LiquiditySweep>();
        private List<LiquiditySweep> currentDivergenceRsiBear = new List<LiquiditySweep>();
        private List<LiquiditySweep> currentDivergencePriceBull = new List<LiquiditySweep>();
        private List<LiquiditySweep> currentDivergenceRsiBull = new List<LiquiditySweep>();
        private List<Candle> candles4Hour = new List<Candle>();
        private int candlesCountMerge = 16;
        /// <summary>
        /// Длительность актуальности имбаланса в 4-х-часовых свечах
        /// </summary>
        private int imbalanceMemoryCount = 6;

        private StrategyParameterBool _TPSLSmartOn;
        private StrategyParameterDecimal _smartStopLossOffset;
        private StrategyParameterDecimal _smartTakeProfitMultiplier;

        private List<ImbalanceData> _imbalances = new List<ImbalanceData>();
        private StrategyParameterDecimal _imbalanceMin;
        private StrategyParameterInt _imbalanceLiveTimeHours;
        private StrategyParameterBool _imbalanceFilter;
        private StrategyParameterBool _imbalanceDirectionFilterOn;
        private Aindicator _rsi;
        private TakeProfitDecoration _tp;
        private StopLossDecoration _sl;
        private List<Candle> _candles;

        public DivergenceRsi(string name, StartProgram startProgram) : base(name, startProgram)
        {
            _rsiPeriod = CreateParameter("RSI Period", 14, 7, 28, 1, "Robot");
            _period = CreateParameter("Period", 50, 10, 100, 1, "Robot");
            //_rsiOverSold = CreateParameter("RSI OverSold", 30, 20, 40, 5, "Robot");
            //_rsiOverBought = CreateParameter("RSI OverBought", 70, 60, 80, 5, "Robot");
            _minDistance = CreateParameter("Min Distance", 5, 5, 30, 1, "Robot");
            _maxDistance = CreateParameter("Max Distance", 40, 40, 200, 1, "Robot");
            _syncTolerance = CreateParameter("Sync Tolerance", 3, 2, 8, 1, "Robot");
            _extremaOrder = CreateParameter("Extrema Order", 5, 5, 30, 1, "Robot");
            //_minDivergenceStrength = CreateParameter("Min Divergence Strength", 50, 50, 90, 5, "Robot");
            _imbalanceMin = CreateParameter("Imbalance Minimum", 100m, 1m, 1000m, 10, "Robot");
            _imbalanceLiveTimeHours = CreateParameter("Imbalance Live Time Hours", 24, 24, 120, 4, "Robot");
            _imbalanceFilter = CreateParameter("Imbalance Filter On", false, "Robot");
            _imbalanceDirectionFilterOn = CreateParameter("Imbalance Direction Filter On", false, "Robot");

            _TPSLSmartOn = CreateParameter("TPSL Smart On", false, "Smart TPSL");
            _smartStopLossOffset = CreateParameter("Stop Loss Offset", 0m, 0, 100m, 10m, "Smart TPSL");
            _smartTakeProfitMultiplier = CreateParameter("Take Profit Multiplier", 1m, 1m, 5m, 0.5m, "Smart TPSL");

            _rsi = IndicatorsFactory.CreateIndicatorByName("RSI", name + "RSI", false);
            _rsi = (Aindicator)_tab.CreateCandleIndicator(_rsi, "RSI");
            _tp = new TakeProfitDecoration(this);
            _sl = new StopLossDecoration(this);
            ParametersChangedByUser();
        }

        protected override void CandleFinishedEvent(List<Candle> candles)
        {
            _candles = candles;
            if (_imbalanceFilter.ValueBool)
            {
                if (candles.Count < candlesCountMerge)
                {
                    candles4Hour.Clear();
                }
                else
                {
                    if (candles4Hour.Count > 2)
                    {
                        int canleTime = (int)(candles4Hour[1].TimeStart - candles4Hour[0].TimeStart).Add(TimeSpan.FromSeconds(1)).TotalHours;
                        imbalanceMemoryCount = _imbalanceLiveTimeHours.ValueInt / canleTime;
                    }

                    int expect = candles.Count / candlesCountMerge;
                    int need = expect - candles4Hour.Count;
                    if (need > 0)
                    {
                        // Заполнение 4-х-часовых свечей
                        int start = candles4Hour.Count * candlesCountMerge;
                        List<Candle> newCandles = CandleMerger.Merge(candles.Skip(start).Take(candlesCountMerge).ToList(), candlesCountMerge);
                        candles4Hour.AddRange(newCandles);

                        if (candles4Hour.Count > 3)
                        {
                            _imbalances.Clear();
                            int startImb = candles4Hour.Count - imbalanceMemoryCount;
                            List<Candle> last4Hour = candles4Hour.Skip(startImb).Take(imbalanceMemoryCount).ToList();
                            for (int i = 0; i < last4Hour.Count - 2; i++)
                            {
                                ImbalanceType imbalanceType = ImbalanceDetector.GetImbalance(last4Hour.Skip(i).Take(3), out decimal low, out decimal high);
                                if (imbalanceType != ImbalanceType.None && (high - low) > _imbalanceMin.ValueDecimal)
                                {
                                    _imbalances.Add(new ImbalanceData() { High = high, Low = low, IndexStart = (startImb + i) * candlesCountMerge, Type = imbalanceType });
                                }
                            }
                        }
                    }
                }
            }
            
            base.CandleFinishedEvent(candles);
        }

        protected override bool CheckClosePosition(List<Candle> candles, Position position)
        {
            if (position.Direction == Side.Buy && IsBearDivergence(candles))
            {
                return true;
            }
            else if (position.Direction == Side.Sell && IsBullDivergence(candles))
            {
                return true;
            }
            return false;
        }

        private bool IsBullDivergence(List<Candle> candles/*, out decimal strength*/)
        {
            int skip = candles.Count - _period.ValueInt;
            decimal[] price = candles.Skip(skip).Select(c => c.Low).ToArray();
            decimal[] rsi = _rsi.DataSeries[0].Values.Skip(skip).ToArray();
            //strength = 0;
            bool result = false;
            
            if (DivergenceDetector.IsBullDivergence2(price, rsi, _minDistance.ValueInt, _maxDistance.ValueInt, _syncTolerance.ValueInt, _extremaOrder.ValueInt, out List<LiquiditySweep> priceDic, out List<LiquiditySweep> rsiDic))
            {
                if ((!_imbalanceFilter.ValueBool) || priceDic.Any(price => _imbalances.Any(i => (i.Type == ImbalanceType.Long || (!_imbalanceDirectionFilterOn.ValueBool)) && ((price.Value1 > i.Low && price.Value1 < i.High) || (price.Value2 > i.Low && price.Value2 < i.High)))))
                {
                    // Если какой то из экстремумов лежит в зоне актуального имбаланса
                    currentDivergencePriceBull.Clear();
                    currentDivergenceRsiBull.Clear();
                    foreach (LiquiditySweep sweep in priceDic)
                    {
                        currentDivergencePriceBull.Add(new LiquiditySweep() { Index1 = sweep.Index1 + skip, Index2 = sweep.Index2 + skip, Value1 = sweep.Value1, Value2 = sweep.Value2 });
                    }
                    foreach (LiquiditySweep sweep in rsiDic)
                    {
                        currentDivergenceRsiBull.Add(new LiquiditySweep() { Index1 = sweep.Index1 + skip, Index2 = sweep.Index2 + skip, Value1 = sweep.Value1, Value2 = sweep.Value2 });
                    }
                    result = true;
                }
            }
            return result;
        }

        protected override bool CheckOpenLongPosition(List<Candle> candles)
        {
            return IsBullDivergence(candles/*, out decimal strength*/);
        }

        /*private decimal GetDivergenceLongStrength(Dictionary<int, decimal> priceDic, Dictionary<int, decimal> rsiDic)
        {
            decimal result = 0;
            int minIndex = priceDic.Keys.Min();
            int maxIndex = priceDic.Keys.Max();
            decimal price1 = priceDic[minIndex];
            decimal price2 = priceDic[maxIndex];
            minIndex = rsiDic.Keys.Min();
            maxIndex = rsiDic.Keys.Max();
            decimal ind1 = rsiDic[minIndex];
            decimal ind2 = rsiDic[maxIndex];

            // Величина расхождения
            decimal pricePercent = (price1 - price2) / price1 * 100;
            decimal indicatorPercent = ind2 - ind1;

            decimal averagePercent = (pricePercent + indicatorPercent) / 2;
            decimal divergenceValue = averagePercent * 3;
            if (divergenceValue > 30)
            {
                divergenceValue = 30;
            }
            result += divergenceValue;

            // Экстремальность rsi
            decimal rsiExtrem = 0;
            decimal rsi = _rsi.DataSeries[0].Values.Last();
            rsiExtrem = 0;
            if (rsi < 30)
            {
                rsiExtrem = 25;
            }
            else if (rsi < 35)
            {
                rsiExtrem = 20;
            }
            else if (rsi < 40)
            {
                rsiExtrem = 15;
            }
            else if (rsi < 45)
            {
                rsiExtrem = 10;
            }
            result += rsiExtrem;
            // Длительность паттерна
            decimal lengthPoints = 0;
            minIndex = priceDic.Keys.Min();
            maxIndex = priceDic.Keys.Max();
            int length = maxIndex - minIndex;
            if (length >= 30)
            {
                lengthPoints = 20;
            }
            else if (length > 20 && length < 30)
            {
                lengthPoints = 15;
            }
            else if (length > 15 && length <= 20)
            {
                lengthPoints = 10;
            }
            else if (length <= 15 && length > 10)
            {
                lengthPoints = 5;
            }
            result += lengthPoints;
            // Угол/наклон
            decimal priceSlope = (price2 - price1) / price1 / length;
            decimal rsiSlope = (ind2 - ind1) / length;
            decimal angleStrength = Math.Abs(priceSlope) * 100 + Math.Abs(rsiSlope) / 10;
            result += Math.Min(angleStrength * 5, 25);
            return result;
        }
        */

        bool IsBearDivergence(List<Candle> candles/*, out decimal strength*/)
        {
            int skip = candles.Count - _period.ValueInt;
            decimal[] price = candles.Skip(skip).Select(c => c.High).ToArray();
            decimal[] rsi = _rsi.DataSeries[0].Values.Skip(skip).ToArray();
            //strength = 0;
            bool result = false;
            if (DivergenceDetector.IsBearDivergence2(price, rsi, _minDistance.ValueInt, _maxDistance.ValueInt, _syncTolerance.ValueInt, _extremaOrder.ValueInt, out List<LiquiditySweep> priceDic, out List<LiquiditySweep> rsiDic))
            {
                if ((!_imbalanceFilter.ValueBool) || priceDic.Any(price => _imbalances.Any(i => (i.Type == ImbalanceType.Short || !_imbalanceDirectionFilterOn.ValueBool) && ((price.Value1 > i.Low && price.Value1 < i.High) || (price.Value2 > i.Low && price.Value2 < i.High)))))
                {
                    // Если какой то из экстремумов лежит в зоне актуального имбаланса
                    currentDivergencePriceBear.Clear();
                    currentDivergenceRsiBear.Clear();
                    foreach (LiquiditySweep sweep in priceDic)
                    {
                        currentDivergencePriceBear.Add(new LiquiditySweep() { Index1 = sweep.Index1 + skip, Index2 = sweep.Index2 + skip, Value1 = sweep.Value1, Value2 = sweep.Value2 });
                    }
                    foreach (LiquiditySweep sweep in rsiDic)
                    {
                        currentDivergenceRsiBear.Add(new LiquiditySweep() { Index1 = sweep.Index1 + skip, Index2 = sweep.Index2 + skip, Value1 = sweep.Value1, Value2 = sweep.Value2 });
                    }
                    result = true;
                }
            }
            return result;
        }

        protected override bool CheckOpenShortPosition(List<Candle> candles)
        {
            return IsBearDivergence(candles/*, out decimal strength*/);
        }

        /*private decimal GetDivergenceShortStrength(Dictionary<int, decimal> priceDic, Dictionary<int, decimal> rsiDic)
        {
            decimal result = 0;
            int minIndex = priceDic.Keys.Min();
            int maxIndex = priceDic.Keys.Max();
            decimal price1 = priceDic[minIndex];
            decimal price2 = priceDic[maxIndex];
            minIndex = rsiDic.Keys.Min();
            maxIndex = rsiDic.Keys.Max();
            decimal ind1 = rsiDic[minIndex];
            decimal ind2 = rsiDic[maxIndex];

            // Величина расхождения
            decimal pricePercent = (price2 - price1) / price2 * 100;
            decimal indicatorPercent = ind1 - ind2;

            decimal averagePercent = (pricePercent + indicatorPercent) / 2;
            decimal divergenceValue = averagePercent * 3;
            if (divergenceValue > 30)
            {
                divergenceValue = 30;
            }
            result += divergenceValue;

            // Экстремальность rsi
            decimal rsiExtrem = 0;
            decimal rsi = _rsi.DataSeries[0].Values.Last();
            rsiExtrem = 0;
            if (rsi > 70)
            {
                rsiExtrem = 25;
            }
            else if (rsi > 65)
            {
                rsiExtrem = 20;
            }
            else if (rsi > 60)
            {
                rsiExtrem = 15;
            }
            else if (rsi > 55)
            {
                rsiExtrem = 10;
            }
            result += rsiExtrem;
            // Длительность паттерна
            decimal lengthPoints = 0;
            minIndex = priceDic.Keys.Min();
            maxIndex = priceDic.Keys.Max();
            int length = maxIndex - minIndex;
            if (length >= 30)
            {
                lengthPoints = 20;
            }
            else if (length > 20 && length < 30)
            {
                lengthPoints = 15;
            }
            else if (length > 15 && length <= 20)
            {
                lengthPoints = 10;
            }
            else if (length <= 15 && length > 10)
            {
                lengthPoints = 5;
            }
            result += lengthPoints;
            // Угол/наклон
            decimal priceSlope = (price2 - price1) / price1 / length;
            decimal rsiSlope = (ind2 - ind1) / length;
            decimal angleStrength = Math.Abs(priceSlope) * 100 + Math.Abs(rsiSlope) / 10;
            result += Math.Min(angleStrength * 5, 25);

            return result;
        }
        */

        protected override List<Func<List<Candle>, bool>> GetCheckers()
        {
            return new List<Func<List<Candle>, bool>>() 
            {
                candles => { return candles.Count >= _period.ValueInt;  },
                candles => { return candles.Count >= _rsiPeriod.ValueInt; }
            };
        }

        private DateTime _lastMessageShow = DateTime.Now;
        protected override void ParametersChangedByUser()
        {
            if (_rsi != null)
            {
                _rsi.ParametersDigit[0].Value = _rsiPeriod.ValueInt;
            }

            if (_TPSLSmartOn != null && _TPSLSmartOn.ValueBool && _tp != null && _sl != null && (!_tp.On || !_sl.On))
            {
                if (!_tp.On || !_sl.On)
                {
                    if ((DateTime.Now - _lastMessageShow).TotalMilliseconds > 1000)
                    {
                        //MessageBox.Show("Take Profit и Stop Loss включены принудительно, т. к. включён Smart TPSL", "Внимание.", MessageBoxButton.OK, MessageBoxImage.Information);
                        _lastMessageShow = DateTime.Now;
                    }
                }
                _tp.On = true;
                _sl.On = true;
            }

            if (_TPSLSmartOn != null && _TPSLSmartOn.ValueBool)
            {
                if (_tp != null)
                {
                    _tp.ActivationPriceFunc = GetTakeProfitPrice; 
                }
                if (_sl != null)
                {
                    _sl.StopPriceFunc = GetStopLossPrice;
                }
            }
            else
            {
                if (_tp != null)
                {
                    _tp.ActivationPriceFunc = null;
                }
                if (_sl != null)
                {
                    _sl.StopPriceFunc = null;
                }
            } 
        }

        private decimal GetStopLossPrice(Side side)
        {
            decimal result = 0;
            LiquiditySweep sweep = null;
            switch (side)
            {
                case Side.Buy:
                    sweep = currentDivergencePriceBull.LastOrDefault();
                    if (sweep != null)
                    {
                        result = _candles[sweep.Index2].Low - _smartStopLossOffset.ValueDecimal;
                    }
                    break;
                case Side.Sell:
                    sweep = currentDivergencePriceBear.LastOrDefault();
                    if (sweep != null)
                    {
                        result = _candles[sweep.Index2].High + _smartStopLossOffset.ValueDecimal;
                    }
                    break;
                default:
                    result = _candles.Last().Center;
                    break;
            }
            return result;
        }

        private decimal GetTakeProfitPrice(Side side)
        {
            decimal result = 0;
            LiquiditySweep sweep = null;
            Candle last = _candles.Last();
            switch (side)
            {
                case Side.Buy:
                    sweep = currentDivergencePriceBull.LastOrDefault();
                    if (sweep != null)
                    {
                        decimal sl = _candles[sweep.Index2].Low - _smartStopLossOffset.ValueDecimal;
                        decimal slDelta = Math.Abs(last.Close - sl);
                        result = last.Close + slDelta * _smartTakeProfitMultiplier.ValueDecimal;
                    }
                    break;
                case Side.Sell:
                    sweep = currentDivergencePriceBear.LastOrDefault();
                    if (sweep != null)
                    {
                        decimal sl = _candles[sweep.Index2].High + _smartStopLossOffset.ValueDecimal;
                        decimal slDelta = Math.Abs(sl - last.Close);
                        result = last.Close - slDelta * _smartTakeProfitMultiplier.ValueDecimal;
                    }
                    break;
                default:
                    result = _candles.Last().Center;
                    break;
            }
            return result;
        }

        protected override void OnChartPostPaint(object sender, ChartPaintEventArgs e)
        {
            try
            {
                if (sender is Chart chart)
                {
                    Graphics g = e.ChartGraphics.Graphics;
                    ChartArea areaPrime = chart?.ChartAreas?.Where(a => a.Name == "Prime").SingleOrDefault();
                    ChartArea areaRsi = chart?.ChartAreas?.Where(a => a.Name == "RSI").SingleOrDefault();
                    PaintDic(currentDivergencePriceBull, areaPrime, Color.Green, g);
                    PaintDic(currentDivergencePriceBear, areaPrime, Color.Green, g);
                    PaintDic(currentDivergenceRsiBull, areaRsi, Color.Green, g);
                    PaintDic(currentDivergenceRsiBear, areaRsi, Color.Green, g);
                    if (_imbalances.Count > 0)
                    {
                        foreach (ImbalanceData i in _imbalances)
                        {
                            PaintImbalance(i, areaPrime, Color.White, g);
                        }
                    }
                }
            }
            catch { }

            void PaintDic(List<LiquiditySweep> sweeps, ChartArea area, Color color, Graphics g)
            {
                if (sweeps.Count < 1 || area == null) return;
                foreach (LiquiditySweep sweep in sweeps)
                {
                    Axis xAxis = area.AxisX;
                    Axis yAxis = area.AxisY;
                    area.AxisY.Minimum = area.AxisY2.Minimum;
                    area.AxisY.Maximum = area.AxisY2.Maximum;
                    using (Brush brush = new SolidBrush(color))
                    using (Pen pen = new Pen(brush))
                    {
                        float x1 = (float)xAxis.ValueToPixelPosition(sweep.Index1);
                        float x2 = (float)xAxis.ValueToPixelPosition(sweep.Index2);
                        float y1 = (float)yAxis.ValueToPixelPosition((float)sweep.Value1);
                        float y2 = (float)yAxis.ValueToPixelPosition((float)sweep.Value2);
                        g.DrawLine(pen, x1, y1, x2, y2);
                    }
                }
            }

            void PaintImbalance(ImbalanceData imbalance, ChartArea area, Color color, Graphics g)
            {
                Axis xAxis = area.AxisX;
                Axis yAxis = area.AxisY;
                area.AxisY.Minimum = area.AxisY2.Minimum;
                area.AxisY.Maximum = area.AxisY2.Maximum;
                using (Brush brush = new SolidBrush(color))
                using (Brush fill = new SolidBrush(Color.FromArgb(5, imbalance.Type == ImbalanceType.Long ? Color.Blue : Color.Red)))
                using (Pen pen = new Pen(brush) { DashPattern = new float[] { 5, 3 } })
                {
                    float x1 = (float)xAxis.ValueToPixelPosition(imbalance.IndexStart);
                    float x2 = (float)xAxis.ValueToPixelPosition((float)area.AxisX.Maximum);
                    float y1 = (float)yAxis.ValueToPixelPosition((double)imbalance.Low);
                    float y2 = y1;

                    float x3 = x1;
                    float x4 = x2;
                    float y3 = (float)yAxis.ValueToPixelPosition((double)imbalance.High);
                    float y4 = y3;
                    g.FillRectangle(fill, new RectangleF(x1, y3, x2 - x1, Math.Abs(y4 - y2)));
                    g.DrawLine(pen, x1, y1, x2, y2);
                    g.DrawLine(pen, x3, y3, x4, y4);
                }
            }
        }
    }

    struct ImbalanceData
    {
        public decimal High { get; set; }
        public decimal Low { get; set; }
        public ImbalanceType Type { get; set; }

        public int IndexStart { get; set; }
    }
}
