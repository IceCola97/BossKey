using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace BossKey.Models
{
    internal sealed class SortedListModel<TKey, TValue>
        : ISortedListModel<TKey, TValue>
        where TKey : IComparable<TKey>
    {
        private readonly SortedList<TKey, TValue> _list = [];
        private readonly Dictionary<TKey, TKey> _realKeyMapper = [];

        public int Count => _list.Count;

        public int Add(TKey key, TValue value)
        {
            if (_realKeyMapper.TryAdd(key, key))
            {
                _list.Add(key, value);
                return _list.Keys.IndexOf(key);
            }

            return -1;
        }

        public void Clear()
        {
            _list.Clear();
            _realKeyMapper.Clear();
        }

        public void AddAll(IEnumerable<KeyValuePair<TKey, TValue>> items)
        {
            foreach (var item in items)
            {
                if (_realKeyMapper.TryAdd(item.Key, item.Key))
                {
                    _list.Add(item.Key, item.Value);
                }
            }
        }

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            foreach (var item in _list)
            {
                yield return item;
            }
        }

        public int Remove(TKey key)
        {
            if (!_realKeyMapper.TryGetValue(key, out var realKey))
                return -1;

            int index = _list.Keys.IndexOf(realKey);

            if (index >= 0)
            {
                _list.RemoveAt(index);
                _realKeyMapper.Remove(key);
            }

            return index;
        }

        public bool TryGetValue(TKey key, [NotNullWhen(true)] out TValue? value)
        {
            if (_realKeyMapper.TryGetValue(key, out var realKey))
            {
                return _list.TryGetValue(realKey, out value!);
            }

            value = default;
            return false;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
