using System;

namespace OsEngine.Robots.TrigonumCustom.Periodic
{
    public static class DateTimeExtensions
    {
        public static int CompareTime(this DateTime time1, DateTime time2)
        {
            DateTime temp1 = new DateTime(1988, 9, 21, time1.Hour, time1.Minute, time1.Second);
            DateTime temp2 = new DateTime(1988, 9, 21, time2.Hour, time2.Minute, time2.Second);
            return temp1.CompareTo(temp2);
        }
    }
}
