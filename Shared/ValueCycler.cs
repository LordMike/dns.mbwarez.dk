using System.Collections.Generic;
using System.Linq;

namespace Shared
{
    public class ValueCycler<T>
    {
        private readonly T[] _values;
        private int _idx;

        private object _syncObj = new object();

        public int Count { get { return _values.Length; } }

        public ValueCycler(IEnumerable<T> values)
        {
            _values = values.ToArray();
            _idx = 0;
        }

        public T GetNext()
        {
            lock (_syncObj)
            {
                T item = _values[_idx++];

                if (_idx == _values.Length)
                    _idx = 0;

                return item;
            }
        }
    }
}