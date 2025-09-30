using Newtonsoft.Json;
using OsEngine.OsOptimizer;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
        public bool IsDefined => Start != null && End != null;

        public override bool Equals(object obj)
        {
            return obj is Period period && period.Start == Start && period.End == End;
        }

        public override int GetHashCode()
        {
            return Start?.GetHashCode() ?? 0 + End?.GetHashCode() ?? 0;
        }

        public Period GetClone()
        {
            Period period = new Period();
            period.Name = Name;
            period.Start = Start;
            period.End = End;
            return period;
        }
    }

    public class Phazes
    {
        public string Name { get; set; }
        public Period InSamplePeriod { get; set; } = new Period();
        public List<Period> OutOfSamplePeriods = new List<Period>();

        public Phazes GetClone()
        {
            Phazes result = new Phazes();
            result.Name = Name;
            result.InSamplePeriod = InSamplePeriod?.GetClone();
            foreach (Period period in OutOfSamplePeriods)
            {
                result.OutOfSamplePeriods.Add(period.GetClone());
            }
            return result;
        }
    }

    public class PhazePresets : ObservableCollection<Phazes>
    {
        
    }
}
