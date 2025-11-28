using OsEngine.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OsEngine.Common
{
    public static class CandleMerger
    {
        public static List<Candle> Merge(List<Candle> candles, int mergeCount)
        {
            if (mergeCount <= 1 || candles == null || candles.Count == 0) return candles;
            int skip = 0;
            List<Candle> result = new List<Candle>();
            IEnumerable<Candle> sub;
            do
            {
                sub = candles.Skip(skip).Take(mergeCount);
                if (sub.Count() > 0)
                {
                    result.Add(Concate(sub));
                }
                else
                {
                    break;
                }
                skip += mergeCount;
            } while (true);
            return result;
        }

        private static Candle Concate(IEnumerable<Candle> candles)
        {
            Candle result = new Candle(candles.First());
            foreach (Candle candle in candles.Skip(1))
            {
                result.Merge(candle);
            }
            return result;
        }

        private static void Merge(this Candle candle, Candle other)
        {
            if (other.Trades != null)
            {
                candle.Trades.AddRange(other.Trades);
            }

            candle.Volume += other.Volume;
            if (other.High > candle.High)
            {
                candle.High = other.High;
            }

            if (other.Low < candle.Low)
            {
                candle.Low = other.Low;
            }

            candle.Close = other.Close;
        }
    }
}
