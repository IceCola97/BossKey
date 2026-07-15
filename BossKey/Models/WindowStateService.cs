using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace BossKey.Models
{
    internal sealed class WindowStateService : IWindowStateService
    {
        private readonly ConcurrentDictionary<nint, Dictionary<string, object?>> _windowStates = [];

        public WindowStateService()
        {
            ModelFactory.WindowScanner.WindowDestroyed += WindowScanner_WindowDestroyed;
        }

        private void WindowScanner_WindowDestroyed(ScannedWindow window)
        {
            _windowStates.TryRemove(window.Handle, out _);
        }

        public IWindowState? GetState(nint hWnd)
        {
            if (!WindowsAPI.IsWindow(hWnd))
            {
                return null;
            }

            var stateCore = _windowStates.GetOrAdd(hWnd, _ => []);
            return new WindowState(hWnd, stateCore);
        }

        private sealed class WindowState(nint handle, Dictionary<string, object?> stateCore) : IWindowState
        {
            private readonly nint _handle = handle;
            private readonly Dictionary<string, object?> _stateCore = stateCore;

            public nint Handle => _handle;

            private bool TrySetCore<T>(string key, [MaybeNull] T value, bool throwError)
            {
                lock (_stateCore)
                {
                    if (_stateCore.TryGetValue(key, out var oldValue)
                        && oldValue is not null)
                    {
                        if (!typeof(T).IsAssignableFrom(oldValue.GetType()))
                        {
                            if (throwError)
                                throw new ArgumentException($"无法将值类型 {typeof(T)} 分配给已存在的键 '{key}' 的类型 {oldValue.GetType()}。");

                            return false;
                        }
                    }

                    _stateCore[key] = value;
                    return true;
                }
            }

            public void Set<T>(string key, [MaybeNull] T value)
            {
                TrySetCore(key, value, true);
            }

            public void Set(string key, object? value)
            {
                lock (_stateCore)
                {
                    _stateCore[key] = value;
                }
            }

            public bool TrySet<T>(string key, [MaybeNull] T value)
            {
                return TrySetCore(key, value, false);
            }

            private bool TryGetCore<T>(string key, [MaybeNull] out T value, bool throwError)
            {
                lock (_stateCore)
                {
                    if (_stateCore.TryGetValue(key, out var rawValue))
                    {
                        if (rawValue is T typedValue)
                        {
                            value = typedValue;
                            return true;
                        }

                        value = default;

                        if (rawValue is null)
                        {
                            if (typeof(T).IsValueType
                                && Nullable.GetUnderlyingType(typeof(T)) is null)
                            {
                                if (throwError)
                                    throw new ArgumentException($"键 '{key}' 的值为 null，无法转换为非空值类型 {typeof(T)}。");

                                return false;
                            }

                            return true;
                        }

                        if (throwError)
                            throw new ArgumentException($"键 '{key}' 的值类型不匹配。期望类型: {typeof(T)}, 实际类型: {rawValue.GetType()}");

                        return false;
                    }

                    if (throwError)
                        throw new KeyNotFoundException($"键 '{key}' 不存在。");

                    value = default;
                    return false;
                }
            }

            [return: MaybeNull]
            public T Get<T>(string key)
            {
                TryGetCore(key, out T? value, true);
                return value;
            }

            public object? Get(string key)
            {
                lock (_stateCore)
                {
                    _stateCore.TryGetValue(key, out var value);
                    return value;
                }
            }

            public bool TryGet<T>(string key, [MaybeNull] out T value)
            {
                return TryGetCore(key, out value, false);
            }

            public override bool Equals(object? obj)
            {
                return obj is WindowState state &&
                       _handle.Equals(state._handle);
            }

            public override int GetHashCode()
            {
                return HashCode.Combine(_handle);
            }
        }
    }
}
