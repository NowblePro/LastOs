using OsEngine.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OsEngine.Common
{
    public static class ImbalanceDetector
    {
        public static bool GetImbalance(IEnumerable<Candle> candles, out decimal low, out decimal high)
        {
            if (candles == null || candles.Count() != 3) throw new ArgumentException();
            low = 0;
            high = 0;
            Candle first = candles.First();
            Candle middle = candles.ElementAt(1);
            Candle last = candles.Last();
            if (candles.All(c => c.IsUp))
            {
                low = first.High;
                high = last.Low;

                if (first.High > middle.Open &&
                    (high - low) > 0 &&
                    middle.Close > last.Low)
                {
                    return true;
                }
            }
            else if (candles.All(c => c.IsDown))
            {
                low = last.High;
                high = first.Low;

                if (first.Low < middle.Open &&
                    (high - low) > 0 &&
                    middle.Close < last.High)
                {
                    return true;
                }
            }
            return false;
        }
    }
}
