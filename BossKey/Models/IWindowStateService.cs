using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace BossKey.Models
{
    internal interface IWindowStateService
    {
        /// <summary>
        /// 获取指定窗口的状态对象。如果窗口不存在，将返回 null。
        /// </summary>
        /// <param name="hWnd"></param>
        /// <returns></returns>
        IWindowState? GetState(nint hWnd);
    }

    internal interface IWindowState
    {
        /// <summary>
        /// 获取当前窗口的句柄。
        /// </summary>
        nint Handle { get; }

        /// <summary>
        /// 按指定类型设置键值对。如果键已存在且类型不匹配，将抛出异常。
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <param name="value"></param>
        void Set<T>(string key, [MaybeNull] T value);

        /// <summary>
        /// 按指定键设置值，允许值为 null。不会检查类型匹配。
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        void Set(string key, object? value);

        /// <summary>
        /// 按指定类型尝试设置键值对。如果键已存在且类型不匹配，将返回 false，而不会抛出异常。
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        bool TrySet<T>(string key, [MaybeNull] T value);

        /// <summary>
        /// 按指定类型获取键对应的值。如果键不存在或类型不匹配，将抛出异常。
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <returns></returns>
        [return: MaybeNull]
        T Get<T>(string key);

        /// <summary>
        /// 按指定键获取值，允许返回 null。如果键不存在，将返回 null。
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        object? Get(string key);

        /// <summary>
        /// 按指定类型尝试获取键对应的值。如果键不存在或类型不匹配，将返回 false，而不会抛出异常。
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        bool TryGet<T>(string key, [MaybeNull] out T value);
    }
}
