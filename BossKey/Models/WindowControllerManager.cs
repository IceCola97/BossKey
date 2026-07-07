using System;
using System.Collections.Generic;
using System.Text;

namespace BossKey.Models
{
    internal sealed class WindowControllerManager : IWindowControllerManager
    {
        private readonly Lock _lock = new();
        private readonly Dictionary<nint, IFixedWindowController> _controllers = [];

        public bool IsRegistered(IFixedWindowController controller)
        {
            nint hWnd = controller.Current;
            return IsRegistered(hWnd);
        }

        public bool IsRegistered(nint hWnd)
        {
            if (hWnd == 0)
            {
                return false;
            }

            lock (_lock)
            {
                return _controllers.ContainsKey(hWnd);
            }
        }

        public IFixedWindowController Obtain(nint hWnd)
        {
            if (hWnd == 0)
            {
                throw new ArgumentException("窗口句柄无效。", nameof(hWnd));
            }

            lock (_lock)
            {
                if (_controllers.TryGetValue(hWnd, out var controller))
                {
                    return controller;
                }

                var newController = new FixedWindowController(hWnd);
                _controllers[hWnd] = newController;
                return newController;
            }

        }

        public void Register(IFixedWindowController controller)
        {
            ArgumentNullException.ThrowIfNull(controller);

            nint hWnd = controller.Current;

            if (hWnd == 0)
            {
                throw new ArgumentException("窗口句柄无效。", nameof(controller));
            }

            lock (_lock)
            {
                if (_controllers.ContainsKey(hWnd))
                {
                    throw new InvalidOperationException("相同句柄的窗口控制器已注册。");
                }

                _controllers[hWnd] = controller;
            }
        }

        public void Unregister(IFixedWindowController controller)
        {
            ArgumentNullException.ThrowIfNull(controller);

            nint hWnd = controller.Current;

            if (hWnd == 0)
            {
                throw new ArgumentException("窗口句柄无效。", nameof(controller));
            }

            lock (_lock)
            {
                if (!_controllers.TryGetValue(hWnd, out var actual))
                {
                    return;
                }

                if (!ReferenceEquals(actual, controller))
                {
                    throw new InvalidOperationException("尝试注销的窗口控制器与已注册的窗口控制器不匹配。");
                }

                _controllers.Remove(hWnd);
            }
        }
    }
}
