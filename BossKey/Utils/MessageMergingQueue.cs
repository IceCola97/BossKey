using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace BossKey.Utils
{
    internal sealed class MessageMergingQueue<T>(int debounceInterval = 0, bool dequeueAll = false)
    {
        private readonly ConcurrentQueue<T> _queue = [];
        private readonly string _siteKey = Guid.NewGuid().ToString();
        private readonly int _debounceInterval = debounceInterval;
        private readonly bool _dequeueAll = dequeueAll;

        public event MessageMergingDequeueEventHandler<T>? OnDequeue;

        private IEnumerable<T> MessageGenerator()
        {
            while (_queue.TryDequeue(out var item))
            {
                yield return item;
            }
        }

        private void DispatchDequeueEvent()
        {
            IEnumerable<T> resultSet;

            if (_queue.IsEmpty)
            {
                resultSet = (T[])[];
            }
            else
            {
                resultSet = MessageGenerator();

                if (_dequeueAll)
                    resultSet = (T[])[.. resultSet];
            }

            try
            {
                OnDequeue?.Invoke(resultSet);
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

        public static bool IsEmptyItems(IEnumerable<T> items)
        {
            return items switch
            {
                null => true,
                ICollection<T> collection => collection.Count == 0,
                _ => false,
            };
        }
    }

    internal delegate void MessageMergingDequeueEventHandler<T>(IEnumerable<T> items);
}
