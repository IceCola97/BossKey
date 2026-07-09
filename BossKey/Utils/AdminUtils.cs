using BossKey.Models;
using System;
using System.Security.Principal;

namespace BossKey.Utils
{
    /// <summary>
    /// 提供管理员权限相关的检查工具方法
    /// </summary>
    internal static class AdminUtils
    {
        private static readonly Lazy<bool> _isCurrentProcessAdmin = new(() => IsCurrentProcessAdminCore());

        private static bool IsCurrentProcessAdminCore()
        {
            using var identity = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }

        #region Public Methods

        /// <summary>
        /// 检查当前进程是否以管理员身份运行
        /// </summary>
        /// <returns>如果当前进程以管理员身份运行，返回 <see langword="true"/>；否则返回 <see langword="false"/></returns>
        public static bool IsCurrentProcessAdmin() => _isCurrentProcessAdmin.Value;

        /// <summary>
        /// 检查指定窗口能否被当前进程访问
        /// </summary>
        /// <param name="hWnd">窗口句柄</param>
        /// <returns>如果该窗口的进程可以被当前进程访问，返回 <see langword="true"/>；否则返回 <see langword="false"/></returns>
        public static bool CanAccess(nint hWnd)
        {
            WindowsAPI.GetWindowThreadProcessId(hWnd, out uint pid);

            if (WindowsAPI.IsLastAccessDenied())
                return false;

            WindowsAPI.AssertLastError();

            nint hProcess = WindowsAPI.OpenProcess(WindowsAPI.PROCESS_QUERY_INFORMATION, false, pid);

            if (WindowsAPI.IsLastAccessDenied())
                return false;

            WindowsAPI.AssertLastError();

            try
            {
                if (!WindowsAPI.OpenProcessToken(hProcess, WindowsAPI.TOKEN_QUERY, out nint hToken))
                {
                    if (WindowsAPI.IsLastAccessDenied())
                        return false;

                    WindowsAPI.AssertLastError();
                }

                try
                {

                    if (WindowsAPI.GetTokenInformation(
                        hToken, WindowsAPI.TOKEN_ELEVATION,
                        out int isElevated, sizeof(int),
                        out uint returnLength
                    ))
                    {
                        return isElevated == 0 || _isCurrentProcessAdmin.Value;
                    }

                    if (WindowsAPI.IsLastAccessDenied())
                        return false;

                    WindowsAPI.AssertLastError();
                }
                finally
                {
                    WindowsAPI.CloseHandle(hToken);
                }
            }
            finally
            {
                WindowsAPI.CloseHandle(hProcess);
            }

            throw new SystemException("无法确定指定窗口的访问权限。");
        }

        #endregion
    }
}
