using BossKey.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace BossKey.Models
{
    internal sealed class WindowController : IWindowController
    {
        private readonly Lock _lock = new();
        private volatile IFixedWindowController? _fixedWindowController = null;

        public nint Current => _fixedWindowController?.Current ?? 0;

        private static void CheckFixedWindowController([NotNull] IFixedWindowController? controller)
        {
            if (controller == null)
            {
                throw new InvalidOperationException("没有打开的窗口，无法进行操作。请先调用 Open 方法打开一个窗口。");
            }
        }

        private IFixedWindowController CheckWindowAlive()
        {
            var fixedWindowController = _fixedWindowController;
            CheckFixedWindowController(fixedWindowController);

            if (!WindowsAPI.IsWindow(fixedWindowController.Current))
            {
                CloseInternal(fixedWindowController);
                throw new InvalidOperationException("当前窗口已经销毁。");
            }

            return fixedWindowController;
        }

        public byte? Opacity
        {
            get => CheckWindowAlive().Opacity;
            set => CheckWindowAlive().Opacity = value;
        }

        public Hotkey? AutoHideHotkey
        {
            get => CheckWindowAlive().AutoHideHotkey;
            set => CheckWindowAlive().AutoHideHotkey = value;
        }

        public float? Volume
        {
            get => CheckWindowAlive().Volume;
            set => CheckWindowAlive().Volume = value;
        }

        public bool TopMost
        {
            get => CheckWindowAlive().TopMost;
            set => CheckWindowAlive().TopMost = value;
        }

        public void ReapplyProperties()
        {
            CheckWindowAlive().ReapplyProperties();
        }

        private nint CloseInternal(IFixedWindowController current)
        {
            CheckFixedWindowController(current);

            lock (_lock)
            {
                if (!ReferenceEquals(current, _fixedWindowController))
                {
                    throw new InvalidOperationException("出现并发关闭操作");
                }

                nint hWnd = _fixedWindowController.Current;
                ModelFactory.WindowControllerManager.Unregister(_fixedWindowController);
                _fixedWindowController = null;
                return hWnd;
            }
        }

        public nint Close()
        {
            var fixedWindowController = _fixedWindowController;
            CheckFixedWindowController(fixedWindowController);
            return CloseInternal(fixedWindowController);
        }

        public void Open(nint hWnd)
        {
            if (_fixedWindowController != null)
            {
                throw new InvalidOperationException("已经有一个窗口被打开，请先关闭当前窗口再打开新的窗口。");
            }

            lock (_lock)
            {
                if (_fixedWindowController != null)
                {
                    throw new InvalidOperationException("已经有一个窗口被打开，请先关闭当前窗口再打开新的窗口。");
                }

                _fixedWindowController = ModelFactory.WindowControllerManager.Obtain(hWnd);
            }
        }
    }
}
