using System;
using System.Collections.Generic;
using System.Text;

namespace BossKey.Models
{
    internal static class GlobalConfigs
    {
        /// <summary>
        /// 指示当前应用是否处于开发模式，开发模式下会启用一些额外的调试功能，例如日志输出、调试信息显示等。
        /// </summary>
        public static bool DevelopMode { get; set; } = false;
    }
}
