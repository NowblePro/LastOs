using OsEngine.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OsEngine.Common
{
    class MeanReverseGrid
    {
        private Side _side;
        private Dictionary<int, decimal> _grid = new Dictionary<int, decimal>();
        private Dictionary<int, Position> _positions = new Dictionary<int, Position>();
        private int _index;

        public MeanReverseGrid(decimal price, decimal delta, int levelsCount, Side side, int index)
        {
            if (levelsCount < 2) throw new Exception("Уровней должно быть хотя бы 2");
            _side = side;
            _index = index;
            if (side == Side.Buy)
            {
                for (int i = 1; i < levelsCount + 1; i++)
                {
                    _grid.Add(i, price - delta * i);
                }
            }
            else
            {
                for (int i = 1; i < levelsCount + 1; i++)
                {
                    _grid.Add(i, price + delta * i);
                }
            }
        }

        public Side Direction => _side;

        public int Index => _index;

        public Dictionary<int, decimal> GetGrid()
        {
            return _grid;
        }

        internal void SetPosition(int key, Position position)
        {
            _positions.Add(key, position);
        }

        public Dictionary<int, Position> GetPositions()
        {
            return _positions;
        }

        internal void DeleteByKey(int key)
        {
            _grid.Remove(key);
            // Позиция к этому моменту должна быть закрыта
            _positions.Remove(key);
        }
    }
}
