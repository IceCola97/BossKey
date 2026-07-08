using System;
using System.Collections.Generic;
using System.Text;

namespace BossKey.Models
{
    internal interface IWindowScanner
    {
        /// <summary>
        /// 当有新窗口被创建时触发的事件
        /// </summary>
        public event WindowCreatedEventHandler? WindowCreated;

        /// <summary>
        /// 当有窗口被销毁时触发的事件
        /// </summary>
        public event WindowDestroyedEventHandler? WindowDestroyed;

        /// <summary>
        /// 当有窗口被显示时触发的事件
        /// </summary>
        public event WindowShownEventHandler? WindowShown;

        /// <summary>
        /// 当有窗口被隐藏时触发的事件
        /// </summary>
        public event WindowHiddenEventHandler? WindowHidden;

        /// <summary>
        /// 获取当前系统中所有窗口的数量
        /// </summary>
        int WindowCount { get; }

        /// <summary>
        /// 获取当前系统中所有窗口的集合
        /// </summary>
        IEnumerable<ScannedWindow> Windows { get; }

        /// <summary>
        /// 获取或设置窗口扫描的过滤条件
        /// </summary>
        string? Filter { get; set; }
    }

    internal delegate void WindowCreatedEventHandler(ScannedWindow window);
    internal delegate void WindowDestroyedEventHandler(ScannedWindow window);
    internal delegate void WindowShownEventHandler(ScannedWindow window);
    internal delegate void WindowHiddenEventHandler(ScannedWindow window);
}
