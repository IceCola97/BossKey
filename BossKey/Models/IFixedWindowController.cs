using BossKey.Utils;
using System;
using System.Collections.Generic;
using System.Text;

namespace BossKey.Models
{
    internal interface IFixedWindowController
    {
        /// <summary>
        /// 指示当前窗口句柄
        /// </summary>
        nint Current { get; }

        /// <summary>
        /// 当前窗口的透明度，范围为 0-255，null 表示不设置
        /// </summary>
        byte? Opacity { get; set; }

        /// <summary>
        /// 当前窗口的自动隐藏热键
        /// </summary>
        Hotkey? AutoHideHotkey { get; set; }

        /// <summary>
        /// 当前窗口所在进程的音量，范围为 0-1，null 表示不设置
        /// </summary>
        float? Volume { get; set; }

        /// <summary>
        /// 当前窗口是否置顶
        /// </summary>
        bool TopMost { get; set; }

        /// <summary>
        /// 当前窗口的透明色，顺序是0xBBGGRR，null 表示不设置
        /// </summary>
        int? TransparentColor { get; set; }

        /// <summary>
        /// 重新应用当前窗口的属性设置（透明度、音量、置顶状态等）到窗口上
        /// </summary>
        void ReapplyProperties();
    }
}
