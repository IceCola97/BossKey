using System;
using System.Collections.Generic;
using System.Text;

namespace BossKey.Models
{
    internal sealed class WindowControllerManager : IWindowControllerManager
    {
        private readonly Lock _lock = new();
        private readonly Dictionary<nint, ControllerReference> _controllers = [];

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
                if (_controllers.TryGetValue(hWnd, out var controllerRef))
                {
                    return controllerRef.Controller;
                }

                var newController = new FixedWindowController(hWnd);
                Register(newController);
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
                if (_controllers.TryGetValue(hWnd, out var controllerRef))
                {
                    controllerRef.Increment();
                    return;
                }

                _controllers[hWnd] = new ControllerReference(controller);
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

                if (!ReferenceEquals(actual.Controller, controller))
                {
                    throw new InvalidOperationException("尝试注销的窗口控制器与已注册的窗口控制器不匹配。");
                }

                if (!actual.Decrement())
                    _controllers.Remove(hWnd);
            }
        }

        private sealed class ControllerReference(IFixedWindowController controller)
        {
            private int _referenceCount = 1;

            public IFixedWindowController Controller { get; } = controller
                    ?? throw new ArgumentNullException(nameof(controller));

            public int ReferenceCount => _referenceCount;

            public void Increment()
            {
                _referenceCount++;
            }

            public bool Decrement()
            {
                return --_referenceCount > 0;
            }
        }
    }
}
