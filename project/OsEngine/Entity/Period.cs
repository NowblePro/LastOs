using OsEngine.OsOptimizer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OsEngine.Entity
{
    internal class Period
    {
        public string Name { get; set; } = "";
        public DateTime? Start { get; set; } = null;
        public DateTime? End { get; set; } = null;
        public OptimazerFazeReport Report { get; set; } = null;
        public string RobotKey { get; set; } = string.Empty;

        public override bool Equals(object obj)
        {
            return obj is Period period && period.Start == Start && period.End == End && period.RobotKey == RobotKey;
        }

        public override int GetHashCode()
        {
            return Start?.GetHashCode() ?? 0 + End?.GetHashCode() ?? 0 + RobotKey.GetHashCode();
        }
    }
}
