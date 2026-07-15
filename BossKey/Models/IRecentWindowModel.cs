using System;
using System.Collections.Generic;
using System.Text;

namespace BossKey.Models
{
    internal interface IRecentWindowModel
    {
        /// <summary>
        /// 当指定窗口被作为当前窗口激活时执行，将此窗口句柄置于最近窗口列表的首位
        /// </summary>
        /// <param name="hWnd"></param>
        void DispatchActivated(nint hWnd);

        /// <summary>
        /// 直接获取最近窗口列表
        /// </summary>
        /// <returns></returns>
        IEnumerable<RecentWindowItem> GetRecentWindows();

        /// <summary>
        /// 获取指定窗口的特定项状态
        /// </summary>
        /// <param name="hWnd"></param>
        /// <param name="action"></param>
        /// <returns></returns>
        bool GetRecentWindowState(nint hWnd, RecentClickAction action);

        /// <summary>
        /// 对指定窗口执行特定项的切换操作
        /// </summary>
        /// <param name="hWnd"></param>
        /// <param name="action"></param>
        /// <returns></returns>
        bool ToggleRecentWindowState(nint hWnd, RecentClickAction action, bool canUseUI);
    }

    internal enum RecentClickAction
    {
        ToggleVisible,
        ToggleHotkey,
        ToggleOpacity,
        ToggleTopmost,
    }
}
