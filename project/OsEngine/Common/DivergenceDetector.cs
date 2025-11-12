using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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

            return true;
        }
    }
}
