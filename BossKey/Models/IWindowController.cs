using System;
using System.Collections.Generic;
using System.Text;

namespace BossKey.Models
{
    internal interface IWindowController : IFixedWindowController
    {
        /// <summary>
        /// 打开指定句柄的窗口<br/>
        /// 在打开新的窗口前必须关闭旧的窗口
        /// </summary>
        /// <param name="hWnd"></param>
        void Open(nint hWnd);

        /// <summary>
        /// 关闭当前窗口并返回旧的窗口句柄
        /// </summary>
        nint Close();
    }
}
