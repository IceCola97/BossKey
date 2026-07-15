using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;

namespace BossKey.Models
{
    /// <summary>
    /// 托盘图标管理接口，提供 Windows 平台系统托盘图标及右键菜单的注册与管理。
    /// </summary>
    internal interface ITrayModel
    {
        /// <summary>
        /// 显示托盘图标。
        /// </summary>
        void Show();

        /// <summary>
        /// 隐藏托盘图标。
        /// </summary>
        void Hide();

        /// <summary>
        /// 设置托盘图标。
        /// </summary>
        /// <param name="icon">要显示的图标。</param>
        void SetIcon(Icon icon);

        /// <summary>
        /// 设置托盘提示文本。
        /// </summary>
        /// <param name="text">提示文本。</param>
        void SetToolTip(string? text);

        /// <summary>
        /// 当用户通过托盘菜单请求打开主界面时触发。
        /// </summary>
        event Action? OpenMainRequested;

        /// <summary>
        /// 当用户通过托盘菜单请求退出应用时触发。
        /// </summary>
        event Action? ExitRequested;

        /// <summary>
        /// 当最近使用的窗口状态发生变化时触发。
        /// </summary>
        event Action<nint>? RecentWindowStateChanged;
    }
}
