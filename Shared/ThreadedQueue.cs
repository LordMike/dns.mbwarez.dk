using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Shared
{
    public class ThreadedQueue<T>
    {
        private ConcurrentQueue<T> _queue;
        private SemaphoreSlim _queueLock;

        public int MaxConcurrent { get; private set; }

        private bool _isCompleted;

        private TaskCompletionSource<object> _completionSource;

        public int CurrentlyRunning { get { return MaxConcurrent - _queueLock.CurrentCount; } }
        public int CurrentlyInQueue { get { return _queue.Count; } }
        public bool IsWorking { get { return CurrentlyRunning > 0 || CurrentlyInQueue > 0; } }

        public Task CompletedTask { get { return _completionSource.Task; } }

        public Action<T> Processor { get; set; }

        public event Action<Exception, T> OnException;

        protected virtual void OnOnException(Exception arg1, T arg2)
        {
            Action<Exception, T> handler = OnException;
            if (handler != null)
                handler(arg1, arg2);
        }

        public ThreadedQueue(int maxConcurrent)
        {
            MaxConcurrent = maxConcurrent;

            _queue = new ConcurrentQueue<T>();
            _queueLock = new SemaphoreSlim(MaxConcurrent, MaxConcurrent);

            _completionSource = new TaskCompletionSource<object>();

            Processor = obj => { };
        }

        public void Add(T item)
        {
            Add(item, true);
        }

        private void Add(T item, bool startWork)
        {
            if (_isCompleted)
                throw new InvalidOperationException("Queue is closed for input");

            _queue.Enqueue(item);

            if (startWork)
                StartWork();
        }

        public void AddRange(IEnumerable<T> items)
        {
            foreach (T item in items)
                Add(item, false);

            for (int i = 0; i < MaxConcurrent; i++)
                StartWork();
        }

        public void MarkCompleted()
        {
            _isCompleted = true;

            StartWork();
        }

        private void StartWork()
        {
            bool gotLock = _queueLock.Wait(0);

            if (gotLock)
            {
                Task.Factory.StartNew(() =>
                {
                    T item;
                    bool gotItem = _queue.TryDequeue(out item);

                    try
                    {
                        if (!gotItem)
                            return;

                        // Process
                        Processor(item);
                    }
                    catch (Exception ex)
                    {
                        OnOnException(ex, item);
                    }
                    finally
                    {
                        _queueLock.Release();

                        if (!IsWorking && _isCompleted)
                        {
                            // We're done
                            _completionSource.TrySetResult(null);
                        }
                    }

                    StartWork();
                });
            }
        }
    }
}