using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using static System.Net.WebRequestMethods;

namespace BossKey.Models
{
    [DebuggerDisplay("Handle = {Handle}, Title = {Title}, ProcessId = {ProcessId}")]
    internal sealed class ScannedWindow : IEquatable<ScannedWindow>, IComparable<ScannedWindow>
    {
        public nint Handle { get; internal set; }

        public string? Title { get; internal set; }

        public int ProcessId { get; internal set; }

        public bool Visible { get; internal set; }

        public int CompareTo(ScannedWindow? other)
        {
            if (other == null) return 1;
            if (Equals(other)) return 0;
            if (other.Title == null) return 1;
            if (Title == null) return -1;

            int result = string.Compare(Title, other.Title, StringComparison.CurrentCultureIgnoreCase);

            if (result == 0)
                result = Handle.CompareTo(other.Handle);

            return result;
        }

        public override bool Equals(object? obj)
        {
            return obj is ScannedWindow window &&
                   Handle.Equals(window.Handle);
        }

        public bool Equals(ScannedWindow? other)
        {
            return other != null && Handle.Equals(other.Handle);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Handle);
        }

        /// <summary>
        /// 从窗口句柄创建 ScannedWindow 实例，如果窗口标题为空或不符合过滤条件，则返回 null
        /// </summary>
        /// <param name="hWnd"></param>
        /// <param name="filter"></param>
        /// <returns></returns>
        public static ScannedWindow? FromHandle(nint hWnd, string? filter = null)
        {
            // 获取窗口标题
            string? title = GetWindowTitle(hWnd);

            // 检查是否符合过滤条件
            if (filter is not null)
            {
                if (title is null || !title.Contains(filter, StringComparison.OrdinalIgnoreCase))
                    return null;
            }

            // 获取进程 ID
            _ = WindowsAPI.GetWindowThreadProcessId(hWnd, out uint pid);

            if (pid == 0)
                return null;

            bool visible = WindowsAPI.IsWindowVisible(hWnd);

            return new ScannedWindow
            {
                Handle = hWnd,
                Title = title,
                ProcessId = (int)pid,
                Visible = visible,
            };
        }

        /// <summary>
        /// 获取指定窗口的标题文本
        /// </summary>
        private static string? GetWindowTitle(nint hWnd)
        {
            int length = WindowsAPI.GetWindowTextLength(hWnd);

            if (length == 0)
                return null;

            // 缓冲区大小：(字符数 + 1) × sizeof(char)
            byte[] buffer = new byte[(length + 1) * 2];
            _ = WindowsAPI.GetWindowText(hWnd, buffer, buffer.Length);
            return Encoding.Unicode.GetString(buffer).TrimEnd('\0');
        }

        public static bool operator ==(ScannedWindow? left, ScannedWindow? right)
        {
            return EqualityComparer<ScannedWindow>.Default.Equals(left, right);
        }

        public static bool operator !=(ScannedWindow? left, ScannedWindow? right)
        {
            return !(left == right);
        }
    }
}
