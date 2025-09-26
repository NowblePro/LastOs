using System;

namespace OsEngine.Robots.TrigonumCustom.Periodic
{
    public static class DateTimeExtensions
    {
        public static bool CompareOnlyTime(this DateTime time1, DateTime time2)
        {
            return time1.Hour == time2.Hour && time1.Minute == time2.Minute && time1.Second == time2.Second;
        }

        public static bool CompareOnlyTime(this DateTime time1, DateTime time2, TimeSpan tolerance)
        {
            DateTime temp1 = new DateTime(1988, 9, 21, time1.Hour, time1.Minute, time1.Second);
            DateTime temp2 = new DateTime(1988, 9, 21, time2.Hour, time2.Minute, time2.Second);
            int half = (int)tolerance.TotalMilliseconds / 2;
            DateTime t1 = temp2 - TimeSpan.FromMilliseconds(half);
            DateTime t2 = temp2 + TimeSpan.FromMilliseconds(half);
            return t1 < temp1 && t2 > temp1;
        }
    }
}
