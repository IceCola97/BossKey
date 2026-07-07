using System;
using System.Collections.Generic;
using System.Text;

namespace BossKey.Models
{
    internal interface IWindowControllerManager
    {
        /// <summary>
        /// 将窗口控制器注册到管理器中，并绑定对应的窗口句柄。<br/>
        /// 如果窗口控制器已经注册过，则将引用计数加1。<br/>
        /// </summary>
        /// <param name="controller"></param>
        void Register(IFixedWindowController controller);

        /// <summary>
        /// 将窗口控制器的引用计数减1，如果引用计数为0，则从管理器中移除该窗口控制器。<br/>
        /// </summary>
        /// <param name="controller"></param>
        void Unregister(IFixedWindowController controller);

        /// <summary>
        /// 判断窗口控制器是否已经注册到管理器中。
        /// </summary>
        /// <param name="controller"></param>
        /// <returns></returns>
        bool IsRegistered(IFixedWindowController controller);

        /// <summary>
        /// 判断窗口句柄是否已经注册到管理器中。
        /// </summary>
        /// <param name="hWnd"></param>
        /// <returns></returns>
        bool IsRegistered(nint hWnd);

        /// <summary>
        /// 根据窗口句柄获取对应的窗口控制器，如果没有注册，则创建新的<see cref="IFixedWindowController"/>实例并注册。
        /// </summary>
        /// <param name="hWnd"></param>
        /// <returns></returns>
        IFixedWindowController Obtain(nint hWnd);
    }
}
