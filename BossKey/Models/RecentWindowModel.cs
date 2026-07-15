using BossKey.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace BossKey.Models
{
    internal sealed class RecentWindowModel : IRecentWindowModel
    {
        private readonly Lock _lock = new();
        private readonly List<RecentWindowItem> _recentWindows = [];
        private readonly HashSet<nint> _recentWindowHandles = [];

        public RecentWindowModel(IWindowController controller)
        {
            controller.WindowOpened += DispatchActivated;
        }

        private static RecentWindowItem? CreateItem(nint hWnd)
        {
            if (hWnd == 0)
            {
                throw new ArgumentException("窗口句柄无效。", nameof(hWnd));
            }

            var scannedWindow = ScannedWindow.FromHandle(hWnd);

            if (scannedWindow is null)
            {
                return null;
            }

            return new RecentWindowItem(scannedWindow);
        }

        public void DispatchActivated(nint hWnd)
        {
            var item = CreateItem(hWnd)
                ?? throw new ArgumentException("无法创建 RecentWindowItem。", nameof(hWnd));

            lock (_lock)
            {
                if (_recentWindowHandles.Contains(hWnd))
                {
                    // 如果已经存在，则移动到列表的开头
                    _recentWindows.RemoveAll(x => x.Handle == hWnd);
                    _recentWindows.Insert(0, item);
                }
                else
                {
                    // 如果不存在，则添加到列表的开头
                    _recentWindows.Insert(0, item);
                    _recentWindowHandles.Add(hWnd);
                }
            }
        }

        public IEnumerable<RecentWindowItem> GetRecentWindows()
        {
            foreach (var item in _recentWindows)
                yield return item;
        }

        #region Window Action

        private static void SetWindowOpacity(nint hWnd, byte? opacity)
        {
            // 如果有窗口控制器，则使用它来设置透明度
            if (ModelFactory.WindowControllerManager.TryGet(hWnd, out var controller))
            {
                controller.Opacity = opacity;
            }
            else
            {
                // 如果没有窗口控制器，则直接使用 WindowControllerCore 来设置透明度
                WindowControllerCore.SetWindowOpacity(hWnd, opacity);
            }
        }

        private static void SetWindowTopMost(nint hWnd, bool topMost)
        {
            // 如果有窗口控制器，则使用它来设置置顶状态
            if (ModelFactory.WindowControllerManager.TryGet(hWnd, out var controller))
            {
                controller.TopMost = topMost;
            }
            else
            {
                // 如果没有窗口控制器，则直接使用 WindowControllerCore 来设置置顶状态
                WindowControllerCore.SetWindowTopMost(hWnd, topMost);
            }
        }

        public bool GetRecentWindowState(nint hWnd, RecentClickAction action)
        {
            return action switch
            {
                RecentClickAction.ToggleVisible => WindowControllerCore.GetWindowVisible(hWnd),
                RecentClickAction.ToggleHotkey => ModelFactory.WindowControllerManager.TryGet(hWnd, out var controller)
                                                && controller.AutoHideHotkey.HasValue,
                RecentClickAction.ToggleOpacity => WindowControllerCore.GetWindowOpacity(hWnd).HasValue,
                RecentClickAction.ToggleTopmost => WindowControllerCore.GetWindowTopMost(hWnd),
                _ => throw new ArgumentOutOfRangeException(nameof(action)),
            };
        }

        public bool ToggleRecentWindowState(nint hWnd, RecentClickAction action, bool canUseUI)
        {
            switch (action)
            {
                case RecentClickAction.ToggleVisible:
                    WindowControllerCore.ToggleWindowVisible(hWnd);
                    break;
                case RecentClickAction.ToggleHotkey:
                    {
                        bool needRelease = false;

                        if (!ModelFactory.WindowControllerManager.TryGet(hWnd, out var controller))
                        {
                            controller = ModelFactory.WindowControllerManager.Obtain(hWnd);
                            needRelease = true;
                        }

                        try
                        {
                            var oldValue = controller.AutoHideHotkey;

                            if (oldValue.HasValue)
                            {
                                controller.AutoHideHotkey = null;
                            }
                            else
                            {
                                var state = ModelFactory.WindowStateService.GetState(hWnd);

                                if (state is not null
                                    && state.TryGet<Hotkey>(nameof(controller.AutoHideHotkey), out var hotkey))
                                {
                                    var hotkeyManager = ModelFactory.HotkeyManager;
                                    var owner = hotkeyManager.GetHotkeyOwner(hotkey);

                                    if (owner is not null)
                                    {
                                        if (!canUseUI)
                                            throw new InvalidOperationException("窗口使用的上一次热键已经被其他窗口占用。");

                                        if (MessageBox.Show(
                                            "窗口使用的上一次热键已经被其他窗口占用，是否解除占用？",
                                            "热键冲突提示",
                                            MessageBoxButtons.YesNo,
                                            MessageBoxIcon.Warning,
                                            default,
                                            MessageBoxOptions.ServiceNotification
                                        ) == DialogResult.No)
                                            throw new OperationCanceledException();

                                        owner.ReleaseHotkey();
                                    }

                                    controller.AutoHideHotkey = hotkey;
                                }
                                else
                                {
                                    return false;
                                }
                            }
                        }
                        finally
                        {
                            if (needRelease)
                                ModelFactory.WindowControllerManager.Unregister(controller);
                        }
                    }
                    break;
                case RecentClickAction.ToggleOpacity:
                    if (WindowControllerCore.GetWindowOpacity(hWnd).HasValue)
                    {
                        // 如果当前有透明度，则恢复为不透明
                        SetWindowOpacity(hWnd, null);
                    }
                    else
                    {
                        // 如果当前没有透明度，则读取上次的 State 中的透明度值
                        var state = ModelFactory.WindowStateService.GetState(hWnd);

                        if (state is not null
                            && state.TryGet<byte>(nameof(IFixedWindowController.Opacity), out var opacity))
                        {
                            SetWindowOpacity(hWnd, opacity);
                        }
                        else
                        {
                            return false;
                        }
                    }
                    break;
                case RecentClickAction.ToggleTopmost:
                    SetWindowTopMost(hWnd, !WindowControllerCore.GetWindowTopMost(hWnd));
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(action));
            }

            return true;
        }

        #endregion Window Action
    }
}
