using OsEngine.Robots.TrigonumCustom.Periodic;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OsEngine.Entity
{
    public class PeriodSession : Period
    {
        public Color Color { get; set; }

        public bool CheckInSession(DateTime time)
        {
            bool result = false;
            bool inverse = End.Value.CompareTime(Start.Value) < 0;
            if (inverse)
            {
                result = !(time.CompareTime(Start.Value) <= 0 && time.CompareTime(End.Value) >= 0);
            }
            else
            {
                result = time.CompareTime(Start.Value) >= 0 && time.CompareTime(End.Value) <= 0;
            }

            return result;
        }
    }
}
