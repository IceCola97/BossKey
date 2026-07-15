using BossKey.Resources;
using BossKey.Utils;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Text;

namespace BossKey.Models
{
    /// <summary>
    /// 托盘图标管理实现，封装 <see cref="NotifyIcon"/> 并提供 Windows 平台系统托盘图标及右键菜单管理。
    /// </summary>
    internal sealed class TrayModel : ITrayModel, IDisposable
    {
        private readonly NotifyIcon? _notifyIcon;
        private readonly RecentWindowModel _recentWindowModel;
        private bool _disposed;

        public event Action? OpenMainRequested;
        public event Action? ExitRequested;
        public event Action<nint>? RecentWindowStateChanged;

        public TrayModel(IWindowController controller)
        {
            var contextMenu = new ContextMenuStrip();

            _notifyIcon = new NotifyIcon
            {
                Icon = Resource.Icon,
                Text = Resource.AppName,
                ContextMenuStrip = contextMenu,
                Visible = false
            };

            _notifyIcon.DoubleClick += OnTrayDoubleClick;
            contextMenu.Opening += ContextMenu_Opening;

            _recentWindowModel = new RecentWindowModel(controller);

            ResetTrayMenu();
        }

        public void Show()
        {
            _notifyIcon?.Visible = true;
        }

        public void Hide()
        {
            _notifyIcon?.Visible = false;
        }

        public void SetIcon(Icon icon)
        {
            _notifyIcon?.Icon = icon;
        }

        public void SetToolTip(string? text)
        {
            _notifyIcon?.Text = text;
        }

        private void ResetTrayMenu()
        {
            var contextMenu = _notifyIcon?.ContextMenuStrip;

            if (contextMenu == null)
                return;

            contextMenu.Items.Clear();
            contextMenu.Items.Add(new ToolStripMenuItem("打开主界面(&O)", null, OnOpenMainClicked));
            contextMenu.Items.Add(new ToolStripSeparator());

            var recentWindows = _recentWindowModel.GetRecentWindows();
            bool hasItem = false;

            foreach (var recentWindowsItem in recentWindows)
            {
                var menuItem = new ToolStripMenuItem(recentWindowsItem.Title);
                CreateRecentSubMenu(menuItem, recentWindowsItem);
                contextMenu.Items.Add(menuItem);
                hasItem = true;
            }

            if (hasItem)
                contextMenu.Items.Add(new ToolStripSeparator());

            contextMenu.Items.Add(new ToolStripMenuItem("退出(&X)", null, OnExitClicked));
        }

        #region Event Handlers

        private void OnTrayDoubleClick(object? sender, EventArgs e)
        {
            OnOpenMainClicked(sender, e);
        }

        private void OnOpenMainClicked(object? sender, EventArgs e)
        {
            try
            {
                OpenMainRequested?.Invoke();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error while handling open main request: {ex}");
            }
        }

        private void OnExitClicked(object? sender, EventArgs e)
        {
            try
            {
                ExitRequested?.Invoke();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error while handling exit request: {ex}");
            }
        }

        private void ContextMenu_Opening(object? sender, CancelEventArgs e)
        {
            ResetTrayMenu();
        }

        #endregion

        #region Recent Menu

        private void CreateRecentSubMenu(ToolStripMenuItem menuItem, RecentWindowItem item)
        {
            menuItem.DropDownItems.Add(new ToolStripMenuItem("窗口可见(&V)", null,
                (s, e) => HandleItemClick(item, RecentClickAction.ToggleVisible), nameof(RecentClickAction.ToggleVisible)));
            menuItem.DropDownItems.Add(new ToolStripMenuItem("窗口隐藏快捷键(&H)", null,
                (s, e) => HandleItemClick(item, RecentClickAction.ToggleHotkey), nameof(RecentClickAction.ToggleHotkey)));
            menuItem.DropDownItems.Add(new ToolStripMenuItem("窗口透明度(&O)", null,
                (s, e) => HandleItemClick(item, RecentClickAction.ToggleOpacity), nameof(RecentClickAction.ToggleOpacity)));
            menuItem.DropDownItems.Add(new ToolStripMenuItem("窗口置顶(&T)", null,
                (s, e) => HandleItemClick(item, RecentClickAction.ToggleTopmost), nameof(RecentClickAction.ToggleTopmost)));

            menuItem.DropDownOpening += (s, e) =>
            {
                if (!WindowsAPI.IsWindow(item.Handle))
                {
                    menuItem.DropDownItems.Clear();
                    menuItem.DropDownItems.Add("此窗口已经被关闭").Enabled = false;
                    return;
                }

                foreach (var subItem in menuItem.DropDownItems.OfType<ToolStripMenuItem>())
                {
                    if (subItem.Name is string actionName
                        && Enum.TryParse<RecentClickAction>(actionName, out var action))
                    {
                        subItem.Checked = _recentWindowModel.GetRecentWindowState(item.Handle, action);
                    }
                }
            };
        }

        private void HandleItemClick(RecentWindowItem item, RecentClickAction action)
        {
            bool result;

            try
            {
                result = _recentWindowModel.ToggleRecentWindowState(item.Handle, action, true);
            }
            catch (OperationCanceledException)
            {
                // 用户取消了操作，直接返回，不显示错误提示
                return;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error while toggling recent window state: {ex}");
                result = false;
            }

            if (!result)
            {
                ReportUnableRestore();
            }
            else
            {
                try
                {
                    RecentWindowStateChanged?.Invoke(item.Handle);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error while handling recent window state changed: {ex}");
                }
            }
        }

        private static void ReportUnableRestore()
        {
            MessageBox.Show("无法恢复窗口的属性设置，请确保窗口仍然存在并且在主界面配置过属性。", "操作失败", MessageBoxButtons.OK, MessageBoxIcon.Warning, default, MessageBoxOptions.ServiceNotification);
        }

        #endregion Recent Menu

        #region IDisposable

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;

            if (_notifyIcon != null)
            {
                _notifyIcon.ContextMenuStrip?.Dispose();

                _notifyIcon.DoubleClick -= OnTrayDoubleClick;
                _notifyIcon.Visible = false;
                _notifyIcon.Dispose();
            }
        }

        #endregion
    }
}
