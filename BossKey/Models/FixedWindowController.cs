using BossKey.Utils;
using System;

namespace BossKey.Models
{
    internal sealed class FixedWindowController : IFixedWindowController
    {
        private readonly nint _hWnd;
        private byte? _opacity;
        private Hotkey? _autoHideHotkey;
        private float? _volume;
        private bool _topMost;
        private nint _processId;

        public FixedWindowController(nint hWnd)
        {
            if (hWnd == 0)
            {
                throw new ArgumentException("窗口句柄无效。", nameof(hWnd));
            }

            _hWnd = hWnd;
            _opacity = null;
            _autoHideHotkey = null;
            _volume = null;
            _processId = 0;
            _topMost = false;

            InitWindowData();
        }

        private void InitWindowData()
        {
            // 动态获取当前窗口的置顶状态
            nint exStyle = WindowsAPI.GetWindowLong(_hWnd, WindowsAPI.WindowLongIndex.ExStyle);
            _topMost = ((uint)exStyle & (uint)WindowsAPI.WindowExStyle.TopMost) != 0;

            // 动态获取当前窗口的透明度
            if (((uint)exStyle & (uint)WindowsAPI.WindowExStyle.Layered) != 0)
            {
                if (WindowsAPI.GetLayeredWindowAttributes(_hWnd, out _, out byte alpha, out _)
                    && alpha != 255)
                {
                    _opacity = alpha;
                }
            }

            // 动态获取当前窗口进程的音量
            float? volume = WindowControllerCore.GetWindowProcessVolume(_hWnd, ref _processId);

            if (volume.HasValue && volume.Value < 1)
                _volume = volume.Value;
        }

        private void CheckWindowAlive()
        {
            if (!WindowsAPI.IsWindow(_hWnd))
            {
                throw new InvalidOperationException("窗口已经被销毁。");
            }
        }

        private T CheckWindowAlive<T>(T value)
        {
            CheckWindowAlive();
            return value;
        }

        private void AwakeWindow()
        {
            // 如果窗口被最小化，先还原
            WindowsAPI.ShowWindow(_hWnd, WindowsAPI.ShowWindowCmd.Restore);
            // 将窗口提升到 Z 序顶部并激活
            WindowsAPI.BringWindowToTop(_hWnd);
            // 设为系统前台窗口
            WindowsAPI.SetForegroundWindow(_hWnd);
        }

        private void ReapplyProperties()
        {
            WindowControllerCore.SetWindowOpacity(_hWnd, _opacity ?? 255);
            WindowControllerCore.SetWindowTopMost(_hWnd, _topMost);
        }

        public nint Current => _hWnd;

        public byte? Opacity
        {
            get => CheckWindowAlive(_opacity);
            set
            {
                CheckWindowAlive();

                if (_opacity == value)
                {
                    return;
                }

                WindowControllerCore.SetWindowOpacity(_hWnd, value ?? 255);
                _opacity = value;
            }
        }

        public Hotkey? AutoHideHotkey
        {
            get => CheckWindowAlive(_autoHideHotkey);
            set
            {
                CheckWindowAlive();

                if (_autoHideHotkey == value)
                {
                    return;
                }

                bool oldHasValue = _autoHideHotkey.HasValue;
                var hotkeyManager = ModelFactory.HotkeyManager;

                // 如果之前已经注册了热键，那么需要先注销之前的热键
                if (_autoHideHotkey != null)
                {
                    hotkeyManager.UnregisterHotkey(_autoHideHotkey.Value);
                }

                // 如果新的热键有值，那么需要注册新的热键
                if (value.HasValue)
                {
                    if (!hotkeyManager.RegisterHotkey(value.Value, (in _) =>
                    {
                        if (!WindowsAPI.IsWindow(_hWnd))
                        {
                            // 如果窗口已经不存在，那么需要注销热键
                            ModelFactory.HotkeyManager.UnregisterHotkey(value.Value);
                            ModelFactory.WindowControllerManager.Unregister(this);
                            return;
                        }

                        try
                        {
                            if (WindowControllerCore.ToggleWindowVisible(_hWnd))
                            {
                                // 重新应用属性防止失效
                                ReapplyProperties();
                                // 唤醒窗口防止被其他窗口遮挡
                                AwakeWindow();
                            }
                        }
                        catch { }
                    }))
                    {
                        throw new InvalidOperationException("注册热键失败，可能是热键冲突。");
                    }
                }

                _autoHideHotkey = value;

                var windowControllerManager = ModelFactory.WindowControllerManager;

                // 如果从没有热键变为有热键，那么需要增加引用计数器防止被释放
                // 如果从有热键变为没有热键，那么需要减少引用计数器允许被释放
                if (oldHasValue != value.HasValue)
                {
                    if (value.HasValue)
                    {
                        windowControllerManager.Register(this);
                    }
                    else
                    {
                        windowControllerManager.Unregister(this);
                    }
                }
            }
        }

        public float? Volume
        {
            get => CheckWindowAlive(_volume);
            set
            {
                CheckWindowAlive();

                if (_volume == value)
                {
                    return;
                }

                if (value.HasValue)
                {
                    WindowControllerCore.SetWindowProcessVolume(_hWnd, value.Value, ref _processId);
                }

                _volume = value;
            }
        }

        public bool TopMost
        {
            get => CheckWindowAlive(_topMost);
            set
            {
                CheckWindowAlive();

                if (_topMost == value)
                {
                    return;
                }

                WindowControllerCore.SetWindowTopMost(_hWnd, value);
                _topMost = value;
            }
        }
    }
}
