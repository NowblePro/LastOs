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
        public Dictionary<int, decimal> GetMaximums(decimal[] array, int count)
        {
            Dictionary<int, decimal> result = new Dictionary<int, decimal>();
            for (int i = 0; i < array.Length - 2; i++)
            {
                decimal a = array[i];
                decimal b = array[i + 1];
                decimal c = array[i + 2];
                if (a < b && c < b)
                {
                    if (result.Count < count)
                    {
                        result.Add(i, b);
                    }
                    else
                    {
                        KeyValuePair<int, decimal> min = result.Where(pair => pair.Value == result.Min(pair => pair.Value)).SingleOrDefault();
                        if (min.Value < b)
                        {
                            result.Remove(min.Key);
                            result.Add(i, b);
                        }
                    }
                }
            }
            return result;
        }

        public Dictionary<int, decimal> GetMinimums(decimal[] array, int count)
        {
            Dictionary<int, decimal> result = new Dictionary<int, decimal>();
            for (int i = 0; i < array.Length - 2; i++)
            {
                decimal a = array[i];
                decimal b = array[i + 1];
                decimal c = array[i + 2];
                if (a > b && c > b)
                {
                    if (result.Count < count)
                    {
                        result.Add(i, b);
                    }
                    else
                    {
                        KeyValuePair<int, decimal> max = result.Where(pair => pair.Value == result.Max(pair => pair.Value)).SingleOrDefault();
                        if (max.Value > b)
                        {
                            result.Remove(max.Key);
                            result.Add(i, b);
                        }
                    }
                }
            }
            return result;
        }
    }
}
