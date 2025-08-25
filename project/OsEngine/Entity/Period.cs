using Newtonsoft.Json;
using OsEngine.OsOptimizer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OsEngine.Entity
{
    public class Period
    {
        public string Name { get; set; } = "";

        public DateTime? Start { get; set; } = null;

        public DateTime? End { get; set; } = null;

        [JsonIgnore]
        public OptimazerFazeReport Report { get; set; } = null;

        [JsonIgnore]
        public bool IsDefined => Start != null && End != null && Start < End;

        public override bool Equals(object obj)
        {
            return obj is Period period && period.Start == Start && period.End == End;
        }

        public override int GetHashCode()
        {
            return Start?.GetHashCode() ?? 0 + End?.GetHashCode() ?? 0;
        }
    }

    public class Phazes
    {
        public Period InSamplePeriod { get; set; } = new Period();
        public List<Period> OutOfSamplePeriods = new List<Period>();
    }
}
