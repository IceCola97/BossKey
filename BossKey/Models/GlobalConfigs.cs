using BossKey.Utils;
using System;
using System.Collections.Generic;
using System.Text;

namespace BossKey.Models
{
    internal sealed class GlobalConfigs
    {
        /// <summary>
        /// 获取配置文件的路径，默认值为 AppData\Roaming\BossKey\config.json。
        /// </summary>
        public static readonly string ConfigFilePath;

        private static readonly Lazy<GlobalConfigs> _instance = new(() => new GlobalConfigs());

        static GlobalConfigs()
        {
            ConfigFilePath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "BossKey", "config.json");

            Reload();
        }

        public static void Reload()
        {
            ConfigLoader.Load(ConfigFilePath, Instance);
        }

        public static void Save()
        {
            ConfigLoader.Save(ConfigFilePath, Instance);
        }

        public static GlobalConfigs Instance => _instance.Value;

        /// <summary>
        /// 指示当前应用是否处于开发模式，开发模式下会启用一些额外的调试功能，例如日志输出、调试信息显示等。
        /// </summary>
        [NotConfig]
        public bool DevelopMode { get; set; } = false;

        /// <summary>
        /// 指示主窗口关闭时的行为，可能的值包括：<br/>
        /// - "Close": 关闭主窗口并退出应用。<br/>
        /// - "Tray": 关闭主窗口但保持应用在系统托盘中运行。
        /// </summary>
        public string? CloseAction { get; set; } = null;

        /// <summary>
        /// 指示托盘菜单的最近使用窗口列表的最大数量，默认值为 10。
        /// </summary>
        public int MaxRecentWindows { get; set; } = 10;
    }
}
