using BossKey.Components;
using BossKey.Utils;
using System;
using System.Collections.Generic;
using System.Reflection.Metadata;
using System.Text;

namespace BossKey.Models
{
    internal sealed class HotkeyManager : IHotkeyManager
    {
        private readonly Lock _lock = new();
        private readonly Dictionary<Hotkey, HotkeyRegistration> _hotkeys = [];
        private volatile nint _windowHandle = 0;
        private volatile int _nextId = 0x200000;

        public void BindWindow(HotkeyFormBase window)
        {
            ArgumentNullException.ThrowIfNull(window);

            lock (_lock)
            {
                nint handle = _windowHandle = window.Handle;
                window.BindHotkeyManager(this);

                if (_hotkeys.Count == 0)
                {
                    return;
                }

                foreach (var (hotkey, registration) in _hotkeys.ToArray())
                {
                    UnregisterHotkeyInternal(handle, registration.Id);

                    if (!RegisterHotkeyInternal(hotkey, handle, out int id))
                    {
                        throw new SystemException("更换热键绑定窗口失败");
                    }

                    _hotkeys[hotkey] = new HotkeyRegistration(id, registration.Owner, hotkey, registration.Callback);
                }
            }
        }

        public void DispatchHotkey(int id, Hotkey hotkey)
        {
            HotkeyCallback? callback;

            lock (_lock)
            {
                if (!_hotkeys.TryGetValue(hotkey, out var registration)
                    && registration.Id != id)
                {
                    return;
                }

                callback = registration.Callback;
            }

            callback?.Invoke(hotkey);
        }

        private bool RegisterHotkeyInternal(Hotkey hotkey, nint handle, out int id)
        {
            id = Interlocked.Increment(ref _nextId);
            uint modifiers = (uint)WindowsAPI.ModifierKeyToSystem(hotkey.ModifierKey) | (uint)WindowsAPI.HotKeyModifiers.NoRepeat;
            uint vk = (uint)WindowsAPI.VirtualKeyToSystem(hotkey.BaseKey);

            if (!WindowsAPI.RegisterHotKey(handle, id, modifiers, vk))
            {
                // 注册失败（可能被其他程序占用），回滚 ID
                Interlocked.Decrement(ref _nextId);
                return false;
            }

            WindowsAPI.AssertLastError();
            return true;
        }

        public bool RegisterHotkey(IHotkeyOwner owner, Hotkey hotkey, HotkeyCallback callback)
        {
            nint handle = _windowHandle;

            if (handle == 0)
            {
                throw new ArgumentException("热键管理器还没有绑定窗口，无法注册热键。", nameof(hotkey));
            }

            ArgumentNullException.ThrowIfNull(callback);

            hotkey = hotkey.NormalizeLeft();

            if (hotkey.ModifierKey == ModifierKey.None
                || hotkey.BaseKey == Keys.None)
            {
                throw new ArgumentException("注册的热键必须包含至少一个修饰键和一个基础键。", nameof(hotkey));
            }

            lock (_lock)
            {
                handle = _windowHandle;

                if (handle == 0)
                {
                    throw new ArgumentException("热键管理器还没有绑定窗口，无法注册热键。", nameof(hotkey));
                }

                if (_hotkeys.ContainsKey(hotkey))
                {
                    return false;
                }

                if (!RegisterHotkeyInternal(hotkey, handle, out int id))
                {
                    return false;
                }

                _hotkeys[hotkey] = new HotkeyRegistration(id, owner, hotkey, callback);
                return true;
            }
        }

        private static void UnregisterHotkeyInternal(nint handle, int id)
        {
            WindowsAPI.UnregisterHotKey(handle, id);
            WindowsAPI.AssertLastError();
        }

        public void UnregisterHotkey(Hotkey hotkey)
        {
            nint handle = _windowHandle;

            if (handle == 0)
            {
                throw new ArgumentException("热键管理器还没有绑定窗口，无法注册热键。", nameof(hotkey));
            }

            lock (_lock)
            {
                handle = _windowHandle;

                if (handle == 0)
                {
                    throw new ArgumentException("热键管理器还没有绑定窗口，无法注册热键。", nameof(hotkey));
                }

                if (!_hotkeys.TryGetValue(hotkey, out var registration))
                {
                    return;
                }

                UnregisterHotkeyInternal(handle, registration.Id);

                _hotkeys.Remove(hotkey);
            }
        }

        public IHotkeyOwner? GetHotkeyOwner(Hotkey hotkey)
        {
            nint handle = _windowHandle;

            if (handle == 0)
            {
                throw new ArgumentException("热键管理器还没有绑定窗口，无法注册热键。", nameof(hotkey));
            }

            lock (_lock)
            {
                handle = _windowHandle;

                if (handle == 0)
                {
                    throw new ArgumentException("热键管理器还没有绑定窗口，无法注册热键。", nameof(hotkey));
                }

                if (!_hotkeys.TryGetValue(hotkey, out var registration))
                {
                    return null;
                }

                return registration.Owner;
            }
        }

        private readonly struct HotkeyRegistration
        {
            public int Id { get; }

            public IHotkeyOwner Owner { get; }

            public Hotkey Hotkey { get; }

            public HotkeyCallback Callback { get; }

            public HotkeyRegistration(
                int id,
                IHotkeyOwner owner,
                Hotkey hotkey,
                HotkeyCallback callback
            )
            {
                Id = id;
                Owner = owner;
                Hotkey = hotkey;
                Callback = callback;
            }

            public void Deconstruct(out int id, out Hotkey hotkey, out HotkeyCallback callback)
            {
                id = Id;
                hotkey = Hotkey;
                callback = Callback;
            }
        }
    }
}
