using Microsoft.Office.Interop.Excel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Documents;
using Tinkoff.InvestApi.V1;

namespace OsEngine.Common
{
    internal class DivergenceDetector
    {
        /// <summary>
        /// Получить словарь максимумов из последовательности, где key - индекс значения найденного максимума, а value - само значение.
        /// </summary>
        /// <param name="array"></param>
        /// <param name="count">Максимальное количество максимумов в результате</param>
        /// <param name="order">Количество соседних точек с одной стороны, которые сравниваются с потенциальным максимумом</param>
        /// <param name="minDistance">Минимальное расстояние следующего максимума от найденного</param>
        /// <returns></returns>
        public static Dictionary<int, decimal> GetMaximums(decimal[] array, int count, int order = 1, int minDistance = 1, int maxDistance = 0)
        {
            if (maxDistance < 1)
            {
                maxDistance = array.Length;
            }
            if (order < 1) throw new ArgumentOutOfRangeException("order должен быть больше 0");
            if (minDistance < 1) throw new ArgumentOutOfRangeException("minDistance должен быть больше 0");
            Dictionary<int, decimal> result = new Dictionary<int, decimal>();
            int lastPeakIndex = array.Length - order * 2 - 1;
            for (int i = array.Length - order - 1; i >= order; i--)
            {
                if (result.Count == count) break;

                if (IsMaximum(i))
                {
                    decimal value = array[i];
                    if (result.Count < count)
                    {
                        result.Add(i, value);
                        i -= minDistance - 1;
                    }
                    else
                    {
                        break;
                    }
                    lastPeakIndex = i;
                }

                if (lastPeakIndex - i > maxDistance)
                {
                    break;
                }
            }

            bool IsMaximum(int index)
            {
                decimal[] left = new decimal[order];
                decimal[] right = new decimal[order];
                for (int i = 0; i < order; i++)
                {
                    left[i] = array[index - i - 1];
                    right[i] = array[index + i + 1];
                }
                decimal leftMax = left.Max();
                decimal rightMax = right.Max();
                return leftMax < array[index] && rightMax < array[index];
            }

            return result;
        }

        public static Dictionary<int, decimal> GetMinimums(decimal[] array, int count, int order = 1, int minDistance = 1, int maxDistance = 0)
        {
            if (maxDistance < 1)
            {
                maxDistance = array.Length;
            }
            if (order < 1) throw new ArgumentOutOfRangeException("order должен быть больше 0");
            if (minDistance < 1) throw new ArgumentOutOfRangeException("minDistance должен быть больше 0");
            Dictionary<int, decimal> result = new Dictionary<int, decimal>();
            int lastPeakIndex = array.Length - order * 2 - 1;
            for (int i = array.Length - order - 1; i >= order; i--)
            {
                if (result.Count == count) break;

                if (IsMinimum(i))
                {
                    decimal value = array[i];
                    if (result.Count < count)
                    {
                        result.Add(i, value);
                        i -= minDistance - 1;
                    }
                    else
                    {
                        break;
                    }
                    lastPeakIndex = i;
                }

                if (lastPeakIndex - i > maxDistance)
                {
                    break;
                }
            }

            bool IsMinimum(int index)
            {
                decimal[] left = new decimal[order];
                decimal[] right = new decimal[order];
                for (int i = 0; i < order; i++)
                {
                    left[i] = array[index - i - 1];
                    right[i] = array[index + i + 1];
                }
                decimal leftMax = left.Min();
                decimal rightMax = right.Min();
                return leftMax > array[index] && rightMax > array[index];
            }

            return result;
        }

        public static bool IsBearDivergence(decimal[] price, decimal[] indicator, int minDistance, int maxDistance, int syncTolerance, int order, out Dictionary<int, decimal> priceExtremums, out Dictionary<int, decimal> indicatorExtremums)
        {
            Dictionary<int, decimal> priceMax = GetMaximums(price, 2, order, minDistance, maxDistance);
            Dictionary<int, decimal> indicatorMax = GetMaximums(indicator, 2, order, minDistance, maxDistance);
            priceExtremums = priceMax;
            indicatorExtremums = indicatorMax;
            if (priceMax.Count < 2 || indicatorMax.Count < 2)
            {
                return false;
            }
            int maxPriceIndex = priceMax.Max(pair => pair.Key);
            if (maxPriceIndex != price.Length - 1 - order)
            {
                // Сигнал дивергенции выдавать только если пик цены только что произошёл
                return false;
            }

            List<int> sortedPriceIndexes = priceMax.Keys.ToList();
            sortedPriceIndexes.Sort();
            List<int> sortedIndicatorIndexes = indicatorMax.Keys.ToList();
            sortedIndicatorIndexes.Sort();
            for (int i = 0; i < sortedPriceIndexes.Count; i++)
            {
                int priceIndex = sortedPriceIndexes[i];
                int indicatorIndex = sortedIndicatorIndexes[i];
                if (Math.Abs(priceIndex - indicatorIndex) > syncTolerance)
                {
                    // Если экстремумы индикатора и цены не совпадают по времени больше чем на syncTolerance
                    return false;
                }
            }

            decimal price1 = priceMax[sortedPriceIndexes[0]];
            decimal price2 = priceMax[sortedPriceIndexes[1]];
            decimal ind1 = indicatorMax[sortedIndicatorIndexes[0]];
            decimal ind2 = indicatorMax[sortedIndicatorIndexes[1]];

            if (price1 > price2 || ind1 < ind2)
            {
                return false;
            }

            int priceDeltaIndexes = sortedPriceIndexes[1] - sortedPriceIndexes[0];
            decimal derivative = (price2 - price1) / priceDeltaIndexes;
            for (int i = sortedPriceIndexes[0] + 1; i < sortedPriceIndexes[1] - 1; i++)
            {
                decimal lineValue = price1 + derivative * (i - sortedPriceIndexes[0]);
                if (price[i] > lineValue)
                {
                    // Если цена пересекает линию между пиками
                    return false;
                }
            }

            return true;
        }

        public static bool IsBullDivergence(decimal[] price, decimal[] indicator, int minDistance, int maxDistance, int syncTolerance, int order, out Dictionary<int, decimal> priceExtremums, out Dictionary<int, decimal> indicatorExtremums)
        {
            Dictionary<int, decimal> priceMin = GetMinimums(price, 2, order, minDistance, maxDistance);
            Dictionary<int, decimal> indicatorMin = GetMinimums(indicator, 2, order, minDistance, maxDistance);
            priceExtremums = priceMin;
            indicatorExtremums = indicatorMin;
            if (priceMin.Count < 2 || indicatorMin.Count < 2)
            {
                return false;
            }
            int maxPriceIndex = priceMin.Max(pair => pair.Key);
            if (maxPriceIndex != price.Length - 1 - order)
            {
                // Сигнал дивергенции выдавать только если пик цены только что произошёл
                return false;
            }

            List<int> sortedPriceIndexes = priceMin.Keys.ToList();
            sortedPriceIndexes.Sort();
            List<int> sortedIndicatorIndexes = indicatorMin.Keys.ToList();
            sortedIndicatorIndexes.Sort();
            for (int i = 0; i < sortedPriceIndexes.Count; i++)
            {
                int priceIndex = sortedPriceIndexes[i];
                int indicatorIndex = sortedIndicatorIndexes[i];
                if (Math.Abs(priceIndex - indicatorIndex) > syncTolerance)
                {
                    // Если экстремумы индикатора и цены не совпадают по времени больше чем на syncTolerance
                    return false;
                }
            }

            decimal price1 = priceMin[sortedPriceIndexes[0]];
            decimal price2 = priceMin[sortedPriceIndexes[1]];
            decimal ind1 = indicatorMin[sortedIndicatorIndexes[0]];
            decimal ind2 = indicatorMin[sortedIndicatorIndexes[1]];

            if (price1 < price2 || ind1 > ind2)
            {
                return false;
            }

            int priceDeltaIndexes = sortedPriceIndexes[1] - sortedPriceIndexes[0];
            decimal derivative = (price2 - price1) / priceDeltaIndexes;
            for (int i = sortedPriceIndexes[0] + 1; i < sortedPriceIndexes[1] - 1; i++)
            {
                decimal lineValue = price1 + derivative * (i - sortedPriceIndexes[0]);
                if (price[i] < lineValue)
                {
                    // Если цена пересекает линию между впадинами
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Пересвип
        /// </summary>
        /// <returns></returns>
        public static bool LiquiditySweep(decimal[] data, int minDistance, int maxDistance, int order, ExtremumType type, out List<LiquiditySweep> sweeps)
        {
            sweeps = new List<LiquiditySweep>();
            Dictionary<int, decimal> extremums;
            switch (type)
            {
                case ExtremumType.Peek:
                    extremums = GetMaximums(data, data.Length, order, minDistance, maxDistance);
                    break;
                case ExtremumType.Trough:
                    extremums = GetMinimums(data, data.Length, order, minDistance, maxDistance);
                    break;
                default: throw new ArgumentException();
            }

            if (extremums.Count < 2)
            {
                return false;
            }

            List<int> sortedPriceIndexes = extremums.Keys.ToList();
            sortedPriceIndexes.Sort();

            // Отфильтровывание пересвипов, которые пересекаются свечами
            Func<decimal, decimal, bool> sweepFilter;
            switch (type)
            {
                case ExtremumType.Peek:
                    sweepFilter = (lineValue, data) => { return data > lineValue; };
                    break;
                case ExtremumType.Trough:
                    sweepFilter = (lineValue, data) => { return data < lineValue; };
                    break;
                default: throw new ArgumentException();
            }

            for (int i = 0; i < sortedPriceIndexes.Count - 1; i++)
            {
                decimal data1 = extremums[sortedPriceIndexes[i]];
                for (int j = i + 1; j < sortedPriceIndexes.Count; j++)
                {
                    decimal data2 = extremums[sortedPriceIndexes[j]];

                    if (data1 > data2)
                    {
                        continue;
                    }

                    int priceDeltaIndexes = sortedPriceIndexes[j] - sortedPriceIndexes[i];
                    decimal derivative = (data2 - data1) / priceDeltaIndexes;
                    bool result = true;
                    for (int n = sortedPriceIndexes[i] + 1; n < sortedPriceIndexes[j] - 1; n++)
                    {
                        decimal lineValue = data1 + derivative * (n - sortedPriceIndexes[i]);
                        if (sweepFilter(lineValue, data[n]))
                        {
                            // Если цена пересекает линию между пиками
                            result = false;
                            break;
                        }
                    }
                    if (result)
                    {
                        sweeps.Add(new LiquiditySweep() { Index1 = sortedPriceIndexes[i], Index2 = sortedPriceIndexes[j], Value1 = data1, Value2 = data2 });
                    }
                }
            }

            //decimal data1 = extremums[sortedPriceIndexes[0]];
            //decimal data2 = extremums[sortedPriceIndexes[1]];

            //if (data1 > data2)
            //{
            //    return false;
            //}

            //int priceDeltaIndexes = sortedPriceIndexes[1] - sortedPriceIndexes[0];
            //decimal derivative = (data2 - data1) / priceDeltaIndexes;
            //for (int i = sortedPriceIndexes[0] + 1; i < sortedPriceIndexes[1] - 1; i++)
            //{
            //    decimal lineValue = data1 + derivative * (i - sortedPriceIndexes[0]);
            //    if (data[i] > lineValue)
            //    {
            //        // Если цена пересекает линию между пиками
            //        return false;
            //    }
            //}

            return sweeps.Count > 0;
        }

        /// <summary>
        /// Недосвип
        /// </summary>
        /// <returns></returns>
        public static bool FailureSwing(decimal[] data, int minDistance, int maxDistance, int order, ExtremumType type, out List<LiquiditySweep> sweeps)
        {
            sweeps = new List<LiquiditySweep>();
            Dictionary<int, decimal> extremums;
            switch (type)
            {
                case ExtremumType.Peek:
                    extremums = GetMaximums(data, data.Length, order, minDistance, maxDistance);
                    break;
                case ExtremumType.Trough:
                    extremums = GetMinimums(data, data.Length, order, minDistance, maxDistance);
                    break;
                default: throw new ArgumentException();
            }

            if (extremums.Count < 2)
            {
                return false;
            }

            List<int> sortedPriceIndexes = extremums.Keys.ToList();
            sortedPriceIndexes.Sort();

            // Отфильтровывание пересвипов, которые пересекаются свечами
            Func<decimal, decimal, bool> sweepFilter;
            switch (type)
            {
                case ExtremumType.Peek:
                    sweepFilter = (lineValue, data) => { return data > lineValue; };
                    break;
                case ExtremumType.Trough:
                    sweepFilter = (lineValue, data) => { return data < lineValue; };
                    break;
                default: throw new ArgumentException();
            }

            for (int i = 0; i < sortedPriceIndexes.Count - 1; i++)
            {
                decimal data1 = extremums[sortedPriceIndexes[i]];
                for (int j = i + 1; j < sortedPriceIndexes.Count; j++)
                {
                    decimal data2 = extremums[sortedPriceIndexes[j]];

                    if (data1 < data2)
                    {
                        continue;
                    }

                    int priceDeltaIndexes = sortedPriceIndexes[j] - sortedPriceIndexes[i];
                    decimal derivative = (data2 - data1) / priceDeltaIndexes;
                    bool result = true;
                    for (int n = sortedPriceIndexes[i] + 1; n < sortedPriceIndexes[j] - 1; n++)
                    {
                        decimal lineValue = data1 + derivative * (n - sortedPriceIndexes[i]);
                        if (sweepFilter(lineValue, data[n]))
                        {
                            // Если цена пересекает линию между пиками
                            result = false;
                            break;
                        }
                    }
                    if (result)
                    {
                        sweeps.Add(new LiquiditySweep() { Index1 = sortedPriceIndexes[i], Index2 = sortedPriceIndexes[j], Value1 = data1, Value2 = data2 });
                    }
                }
            }

            //decimal data1 = extremums[sortedPriceIndexes[0]];
            //decimal data2 = extremums[sortedPriceIndexes[1]];

            //if (data1 < data2)
            //{
            //    return false;
            //}

            //int priceDeltaIndexes = sortedPriceIndexes[1] - sortedPriceIndexes[0];
            //decimal derivative = (data2 - data1) / priceDeltaIndexes;
            //for (int i = sortedPriceIndexes[0] + 1; i < sortedPriceIndexes[1] - 1; i++)
            //{
            //    decimal lineValue = data1 + derivative * (i - sortedPriceIndexes[0]);
            //    if (data[i] < lineValue)
            //    {
            //        // Если цена пересекает линию между впадинами
            //        return false;
            //    }
            //}

            return sweeps.Count > 0;
        }

        /// <summary>
        /// Определение дивергенций вторым способом - бычья по впадинам лоёв, медвежья по пикам хаёв
        /// </summary>
        /// <param name="price"></param>
        /// <param name="indicator"></param>
        /// <param name="minDistance"></param>
        /// <param name="maxDistance"></param>
        /// <param name="syncTolerance"></param>
        /// <param name="order"></param>
        /// <returns></returns>
        public static bool IsBullDivergence2(decimal[] price, decimal[] indicator, int minDistance, int maxDistance, int syncTolerance, int order, out List<LiquiditySweep> priceSweeps, out List<LiquiditySweep> indicatorSweeps)
        {
            List<LiquiditySweep> resultPrices = new List<LiquiditySweep>();
            List<LiquiditySweep> resultIndicators = new List<LiquiditySweep>();
            if ((LiquiditySweep(price, minDistance, maxDistance, order, ExtremumType.Trough, out priceSweeps) &&
                FailureSwing(indicator, minDistance, maxDistance, order, ExtremumType.Trough, out indicatorSweeps)) ||
                ((LiquiditySweep(indicator, minDistance, maxDistance, order, ExtremumType.Trough, out indicatorSweeps) &&
                FailureSwing(price, minDistance, maxDistance, order, ExtremumType.Trough, out priceSweeps))))
            {
                int maxPriceIndex = priceSweeps.Max(sweep => sweep.Index2);
                if (maxPriceIndex != price.Length - 1 - order)
                {
                    // Сигнал дивергенции выдавать только если пик только что произошёл
                    return false;
                }

                IEnumerable<LiquiditySweep> lastSweeps = priceSweeps.Where(s => s.Index2 == maxPriceIndex);

                foreach (LiquiditySweep sweep in lastSweeps)
                {
                    // Поиск дивергенций с точностью syncTolerance с последнего пересвипа цены
                    IEnumerable<LiquiditySweep> indicatorSync = indicatorSweeps.Where(s => (Math.Abs(s.Index1 - sweep.Index1) <= syncTolerance) && (Math.Abs(s.Index2 - sweep.Index2) <= syncTolerance));
                    LiquiditySweep i = indicatorSync.FirstOrDefault();
                    if (i != null)
                    {
                        resultPrices.Add(sweep);
                        resultIndicators.Add(i);
                    }
                }

                priceSweeps = resultPrices;
                indicatorSweeps = resultIndicators;
            }
            else
            {
                return false;
            }

            return priceSweeps.Any();
        }

        /// <summary>
        /// Определение дивергенций вторым способом - бычья по впадинам лоёв, медвежья по пикам хаёв
        /// </summary>
        /// <param name="price"></param>
        /// <param name="indicator"></param>
        /// <param name="minDistance"></param>
        /// <param name="maxDistance"></param>
        /// <param name="syncTolerance"></param>
        /// <param name="order"></param>
        /// <returns></returns>
        public static bool IsBearDivergence2(decimal[] price, decimal[] indicator, int minDistance, int maxDistance, int syncTolerance, int order, out List<LiquiditySweep> priceSweeps, out List<LiquiditySweep> indicatorSweeps)
        {
            List<LiquiditySweep> resultPrices = new List<LiquiditySweep>();
            List<LiquiditySweep> resultIndicators = new List<LiquiditySweep>();
            if ((LiquiditySweep(price, minDistance, maxDistance, order, ExtremumType.Peek, out priceSweeps) &&
                FailureSwing(indicator, minDistance, maxDistance, order, ExtremumType.Peek, out indicatorSweeps)) ||
                ((LiquiditySweep(indicator, minDistance, maxDistance, order, ExtremumType.Peek, out indicatorSweeps) &&
                FailureSwing(price, minDistance, maxDistance, order, ExtremumType.Peek, out priceSweeps))))
            {
                int maxPriceIndex = priceSweeps.Max(sweep => sweep.Index2);
                if (maxPriceIndex != price.Length - 1 - order)
                {
                    // Сигнал дивергенции выдавать только если пик только что произошёл
                    return false;
                }

                IEnumerable<LiquiditySweep> lastSweeps = priceSweeps.Where(s => s.Index2 == maxPriceIndex);

                foreach (LiquiditySweep sweep in lastSweeps)
                {
                    // Поиск дивергенций с точностью syncTolerance с последнего пересвипа цены
                    IEnumerable<LiquiditySweep> indicatorSync = indicatorSweeps.Where(s => (Math.Abs(s.Index1 - sweep.Index1) <= syncTolerance) && (Math.Abs(s.Index2 - sweep.Index2) <= syncTolerance));
                    LiquiditySweep i = indicatorSync.FirstOrDefault();
                    if (i != null)
                    {
                        resultPrices.Add(sweep);
                        resultIndicators.Add(i);
                    }
                }

                priceSweeps = resultPrices;
                indicatorSweeps = resultIndicators;
            }
            else
            {
                return false;
            }

            return priceSweeps.Any();
        }

        public enum ExtremumType { Peek, Trough }
    }

    public class LiquiditySweep
    {
        public int Index1 { get; set; }
        public int Index2 { get; set; }
        public decimal Value1 { get; set; }
        public decimal Value2 { get; set; }

    }
}
