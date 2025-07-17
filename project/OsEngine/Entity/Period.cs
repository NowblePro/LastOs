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
    }
}
