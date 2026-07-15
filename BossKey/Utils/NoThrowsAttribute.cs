using System;
using System.Collections.Generic;
using System.Text;

namespace BossKey.Utils
{
    /// <summary>
    /// 表示一个方法不会抛出异常的特性。<br/>
    /// 或其已经捕获了所有途中可能抛出的异常。
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
    internal sealed class NoThrowsAttribute : Attribute
    {
    }
}
