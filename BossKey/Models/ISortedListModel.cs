using System;
using System.Collections.Generic;
using System.Text;

namespace BossKey.Models
{
    internal interface ISortedListModel<TKey, TValue>
        : IEnumerable<KeyValuePair<TKey, TValue>>
        where TKey : IComparable<TKey>
    {
        /// <summary>
        /// 获取集合中元素的数量。
        /// </summary>
        int Count { get; }

        /// <summary>
        /// 清空集合中的所有元素。
        /// </summary>
        void Clear();

        /// <summary>
        /// 有序的添加所有指定的键值对到集合中。
        /// </summary>
        /// <param name="items">要添加的键值对集合。</param>
        void AddAll(IEnumerable<KeyValuePair<TKey, TValue>> items);

        /// <summary>
        /// 有序的添加一个键值对到集合中。
        /// </summary>
        /// <param name="key">要添加的键。</param>
        /// <param name="value">要添加的值。</param>
        /// <returns>返回添加后的新元素的索引。</returns>
        int Add(TKey key, TValue value);

        /// <summary>
        /// 有序的移除指定键的元素。
        /// </summary>
        /// <param name="key">要移除的键。</param>
        /// <returns>返回移除的元素的索引，如果未找到则返回 -1。</returns>
        int Remove(TKey key);
    }
}
