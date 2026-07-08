using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace BossKey.Utils
{
    internal sealed class MessageMergingQueue<T>(int debounceInterval = 0)
    {
        private readonly ConcurrentQueue<T> _queue = [];
        private readonly string _siteKey = Guid.NewGuid().ToString();
        private readonly int _debounceInterval = debounceInterval;

        public event MessageMergingDequeueEventHandler<T>? OnDequeue;

        private void DispatchDequeueEvent()
        {
            T[] resultArray;

            if (_queue.IsEmpty)
            {
                resultArray = [];
            }
            else
            {
                var items = new List<T>();

                while (_queue.TryDequeue(out var item))
                {
                    items.Add(item);
                }

                resultArray = [.. items];
            }

            try
            {
                OnDequeue?.Invoke(resultArray);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Exception occurred while invoking OnDequeue event: {ex}");
            }
        }

        public void Enqueue(T item)
        {
            _queue.Enqueue(item);

            if (_debounceInterval > 0)
                LazyCall.Debounce(_siteKey, _debounceInterval, DispatchDequeueEvent);
        }

        public void TriggerDequeue()
        {
            if (_debounceInterval > 0)
                LazyCall.CancelDebounce(_siteKey);

            DispatchDequeueEvent();
        }
    }

    internal delegate void MessageMergingDequeueEventHandler<T>(T[] items);
}
