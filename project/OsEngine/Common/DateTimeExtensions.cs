using System;

namespace OsEngine.Robots.TrigonumCustom.Periodic
{
    public static class DateTimeExtensions
    {
        public static bool CompareOnlyTime(this DateTime time1, DateTime time2)
        {
            return time1.Hour == time2.Hour && time1.Minute == time2.Minute && time1.Second == time2.Second;
        }
    }
}
