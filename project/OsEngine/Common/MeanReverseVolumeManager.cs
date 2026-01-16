using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OsEngine.Common
{
    public class MeanReverseVolumeManager
    {
        private decimal _r = 0.01m;
        private decimal _currentVolume;

        public decimal R
        {
            get { return _r; }
            set { _r = value; }
        }

        public Func<bool, decimal> GetVolumeFunc { get; set; }
        public Func<decimal, decimal> Rounding { get; set; }

        public void Clear()
        {
            _currentVolume = 0;
        }

        public decimal GetNextVolume(bool getRounded = true)
        {
            if (_currentVolume == 0)
            {
                _currentVolume = GetVolumeFunc(getRounded);
                return _currentVolume;
            }
            else
            {
                _currentVolume = Rounding(_currentVolume * (1 + R / 100m));
                return _currentVolume;
            }
        }
    }
}
