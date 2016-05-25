using System;
using System.Collections.Concurrent;
using System.Linq;

namespace Shared
{
    public class ObjectPool<T> : IDisposable where T : new()
    {
        private bool _disposed;
        private bool _canDispose;
        private ConcurrentBag<T> _pool;

        public Action<T> OnReturn { get; set; }

        public ObjectPool()
        {
            _pool = new ConcurrentBag<T>();

            _canDispose = typeof(T).GetInterfaces().Contains(typeof(IDisposable));
        }

        public T Get()
        {
            if (_disposed)
                throw new ObjectDisposedException("Is disposed");

            T tmp;
            if (!_pool.TryTake(out tmp))
                tmp = new T();

            return tmp;
        }

        public void Return(T obj)
        {
            if (_disposed)
            {
                DisposeIfNeeded(obj);
                return;
            }

            Action<T> action = OnReturn;
            if (action != null)
                action(obj);

            _pool.Add(obj);
        }

        private void DisposeIfNeeded(T item)
        {
            if (!_canDispose)
                return;

            ((IDisposable)item).Dispose();
        }

        public void Dispose()
        {
            _disposed = true;

            T tmp;
            while (_pool.TryTake(out tmp))
                DisposeIfNeeded(tmp);
        }
    }
}