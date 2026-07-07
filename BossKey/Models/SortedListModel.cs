using System;
using System.Collections;
using System.Collections.Generic;
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
            if (_list.TryAdd(key, value))
            {
                _realKeyMapper.Add(key, key);
                return _list.Keys.IndexOf(key);
            }

            return -1;
        }

        public void Clear()
        {
            _list.Clear();
        }

        public void AddAll(IEnumerable<KeyValuePair<TKey, TValue>> items)
        {
            foreach (var item in items)
            {
                if (_list.TryAdd(item.Key, item.Value))
                {
                    _realKeyMapper.Add(item.Key, item.Key);
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
                _list.RemoveAt(index);

            return index;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
