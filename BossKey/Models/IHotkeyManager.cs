using BossKey.Components;
using BossKey.Utils;
using System;
using System.Collections.Generic;
using System.Text;

namespace BossKey.Models
{
    public interface IHotkeyManager
    {
        /// <summary>
        /// 绑定热键管理器到窗口
        /// </summary>
        /// <param name="window"></param>
        void BindWindow(HotkeyFormBase window);

        /// <summary>
        /// 获取热键的拥有者，如果热键没有被注册，则返回 null
        /// </summary>
        /// <param name="hotkey"></param>
        /// <returns></returns>
        IHotkeyOwner? GetHotkeyOwner(Hotkey hotkey);

        /// <summary>
        /// 注册热键回调并返回结果
        /// </summary>
        /// <param name="hotkey"></param>
        /// <param name="callback"></param>
        /// <returns></returns>
        bool RegisterHotkey(IHotkeyOwner owner, Hotkey hotkey, HotkeyCallback callback);

        /// <summary>
        /// 注销热键回调
        /// </summary>
        /// <param name="hotkey"></param>
        void UnregisterHotkey(Hotkey hotkey);

        /// <summary>
        /// 触发热键回调
        /// </summary>
        /// <param name="hotkey"></param>
        void DispatchHotkey(int systemId, Hotkey hotkey);
    }

    public interface IHotkeyOwner
    {
        void ReleaseHotkey();
    }

    public delegate void HotkeyCallback(in Hotkey hotkey);
}
