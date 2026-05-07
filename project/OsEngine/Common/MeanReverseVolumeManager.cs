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
        private decimal _nextBaseVolumeMultiplier = 1m;

        public decimal R
        {
            get { return _r; }
            set { _r = value; }
        }

        public Func<bool, decimal> GetVolumeFunc { get; set; }
        public Func<decimal, decimal> Rounding { get; set; }
        public decimal NextBaseVolumeMultiplier
        {
            get { return _nextBaseVolumeMultiplier; }
            set { _nextBaseVolumeMultiplier = value <= 0 ? 1m : value; }
        }

        public void Clear()
        {
            _currentVolume = 0;
            _nextBaseVolumeMultiplier = 1m;
        }

        public decimal GetNextVolume(bool getRounded = true)
        {
            if (_currentVolume == 0)
            {
                decimal baseVolume = GetVolumeFunc(getRounded);

                if (NextBaseVolumeMultiplier != 1m)
                {
                    baseVolume *= NextBaseVolumeMultiplier;

                    if (getRounded && Rounding != null)
                    {
                        baseVolume = Rounding(baseVolume);
                    }
                }

                _currentVolume = baseVolume;
                _nextBaseVolumeMultiplier = 1m;
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
