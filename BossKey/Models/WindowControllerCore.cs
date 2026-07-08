using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace BossKey.Models
{
    internal static class WindowControllerCore
    {
        /// <summary>
        /// 将指定窗口的透明度设置为指定值
        /// </summary>
        /// <param name="hWnd">窗口句柄</param>
        /// <param name="opacity">透明度值，范围为 0-255</param>
        public static void SetWindowOpacity(nint hWnd, byte opacity)
        {
            // 获取当前的扩展样式
            nint exStyle = WindowsAPI.GetWindowLong(hWnd, WindowsAPI.WindowLongIndex.ExStyle);

            // 添加 WS_EX_LAYERED 样式
            nint newExStyle = (nint)((uint)exStyle | (uint)WindowsAPI.WindowExStyle.Layered);
            WindowsAPI.SetWindowLong(hWnd, WindowsAPI.WindowLongIndex.ExStyle, newExStyle);
            WindowsAPI.AssertLastError();

            // 设置透明度（bAlpha: 0=全透明, 255=不透明）
            WindowsAPI.SetLayeredWindowAttributes(hWnd, 0, opacity, WindowsAPI.LayeredWindowAttribute.Alpha);
            WindowsAPI.AssertLastError();
        }

        /// <summary>
        /// 将指定窗口切换为可见或不可见
        /// </summary>
        /// <param name="hWnd"></param>
        /// <param name="visible"></param>
        public static bool ToggleWindowVisible(nint hWnd)
        {
            bool visible = WindowsAPI.IsWindowVisible(hWnd);
            WindowsAPI.ShowWindow(
                hWnd,
                visible
                    ? WindowsAPI.ShowWindowCmd.Hide
                    : WindowsAPI.ShowWindowCmd.Show
            );
            WindowsAPI.AssertLastError();

            return !visible;
        }

        /// <summary>
        /// 将指定窗口设置为可见或不可见
        /// </summary>
        /// <param name="hWnd"></param>
        /// <param name="visible"></param>
        public static void SetWindowVisible(nint hWnd, bool visible)
        {
            WindowsAPI.ShowWindow(
                hWnd,
                visible ? WindowsAPI.ShowWindowCmd.Show : WindowsAPI.ShowWindowCmd.Hide);
            WindowsAPI.AssertLastError();
        }

        /// <summary>
        /// 将指定窗口设置为顶层窗口或非顶层窗口
        /// </summary>
        /// <param name="hWnd">窗口句柄</param>
        /// <param name="topMost">是否设置为顶层窗口</param>
        public static void SetWindowTopMost(nint hWnd, bool topMost)
        {
            nint insertAfter = topMost ? WindowsAPI.HWND_TOPMOST : WindowsAPI.HWND_NOTOPMOST;

            WindowsAPI.SetWindowPos(
                hWnd,
                insertAfter,
                0, 0, 0, 0,
                WindowsAPI.SetWindowPosFlags.NoMove
                | WindowsAPI.SetWindowPosFlags.NoSize
                | WindowsAPI.SetWindowPosFlags.NoActivate);
            WindowsAPI.AssertLastError();
        }

        /// <summary>
        /// 将指定窗口所在进程的音量设置为指定值<br/>
        /// 如果给出了<paramref name="processId"/>，则使用该进程的音量设置；<br/>
        /// 否则，使用窗口句柄获取进程 ID 并设置音量，并通过<paramref name="processId"/>返回该进程 ID。
        /// </summary>
        /// <param name="hWnd"></param>
        /// <param name="volume">音量值，范围 0.0-1.0</param>
        /// <param name="processId"></param>
        public static void SetWindowProcessVolume(nint hWnd, float volume, ref nint processId)
        {
            // 获取目标进程 ID
            uint targetPid;
            if (processId == 0)
            {
                WindowsAPI.GetWindowThreadProcessId(hWnd, out targetPid);
                WindowsAPI.AssertLastError();
                processId = (nint)targetPid;
            }
            else
            {
                targetPid = (uint)processId;
            }

            // 钳制音量到 0.0-1.0
            float clampedVolume = Math.Clamp(volume, 0f, 1f);

            try
            {
                var cw = new StrategyBasedComWrappers();

                // 创建 IMMDeviceEnumerator COM 对象
                // CLSID_MMDeviceEnumerator = {BCDE0395-E52F-467C-8E3D-C4579291692E}
                var clsid = new Guid("BCDE0395-E52F-467C-8E3D-C4579291692E");
                var type = Type.GetTypeFromCLSID(clsid)!;
                var obj = Activator.CreateInstance(type)!;
                var enumerator = (WindowsAPI.IMMDeviceEnumerator)obj;

                // 获取默认音频渲染设备 (eRender=0, eConsole=0)
                if (enumerator.GetDefaultAudioEndpoint(0, 0, out nint pDevice) != 0)
                    return;

                var device = (WindowsAPI.IMMDevice)cw.GetOrCreateObjectForComInstance(pDevice, CreateObjectFlags.None);

                // 激活 IAudioSessionManager2
                Guid iidAsm2 = typeof(WindowsAPI.IAudioSessionManager2).GUID;
                if (device.Activate(ref iidAsm2, 1, 0, out nint pAsm2) != 0)
                    return;

                var sessionManager = (WindowsAPI.IAudioSessionManager2)cw.GetOrCreateObjectForComInstance(pAsm2, CreateObjectFlags.None);

                // 获取会话枚举器
                if (sessionManager.GetSessionEnumerator(out nint pEnum) != 0)
                    return;
                var sessionEnum = (WindowsAPI.IAudioSessionEnumerator)cw.GetOrCreateObjectForComInstance(pEnum, CreateObjectFlags.None);

                // 遍历音频会话，查找匹配进程 ID 的会话
                sessionEnum.GetCount(out int count);

                for (int i = 0; i < count; i++)
                {
                    if (sessionEnum.GetSession(i, out nint pSession) != 0)
                        continue;

                    var session = (WindowsAPI.IAudioSessionControl)cw.GetOrCreateObjectForComInstance(pSession, CreateObjectFlags.None);

                    // 通过 IAudioSessionControl2 获取进程 ID
                    var session2 = (WindowsAPI.IAudioSessionControl2)session;
                    session2.GetProcessId(out uint sessionPid);

                    if (sessionPid == targetPid)
                    {
                        // 获取 ISimpleAudioVolume 并设置音量
                        var simpleVolume = (WindowsAPI.ISimpleAudioVolume)session;
                        simpleVolume.SetMasterVolume(clampedVolume, ref Unsafe.NullRef<Guid>());
                        break;
                    }
                }
            }
            catch
            {
                throw;
            }
        }

        /// <summary>
        /// 获取指定窗口所在进程的当前音量
        /// </summary>
        /// <param name="hWnd">窗口句柄</param>
        /// <param name="processId">进程 ID（0 表示从窗口获取），返回时设置为实际使用的进程 ID</param>
        /// <returns>音量值 0.0-1.0，无法获取时返回 null</returns>
        public static float? GetWindowProcessVolume(nint hWnd, ref nint processId)
        {
            // 获取目标进程 ID
            uint targetPid;
            if (processId == 0)
            {
                WindowsAPI.GetWindowThreadProcessId(hWnd, out targetPid);
                processId = (nint)targetPid;
            }
            else
            {
                targetPid = (uint)processId;
            }

            try
            {
                var cw = new StrategyBasedComWrappers();

                var clsid = new Guid("BCDE0395-E52F-467C-8E3D-C4579291692E");
                var type = Type.GetTypeFromCLSID(clsid)!;
                var obj = Activator.CreateInstance(type)!;
                var enumerator = (WindowsAPI.IMMDeviceEnumerator)obj;

                if (enumerator.GetDefaultAudioEndpoint(0, 0, out nint pDevice) != 0)
                    return null;

                var device = (WindowsAPI.IMMDevice)cw.GetOrCreateObjectForComInstance(pDevice, CreateObjectFlags.None);

                Guid iidAsm2 = typeof(WindowsAPI.IAudioSessionManager2).GUID;
                if (device.Activate(ref iidAsm2, 1, 0, out nint pAsm2) != 0)
                    return null;

                var sessionManager = (WindowsAPI.IAudioSessionManager2)cw.GetOrCreateObjectForComInstance(pAsm2, CreateObjectFlags.None);

                if (sessionManager.GetSessionEnumerator(out nint pEnum) != 0)
                    return null;
                var sessionEnum = (WindowsAPI.IAudioSessionEnumerator)cw.GetOrCreateObjectForComInstance(pEnum, CreateObjectFlags.None);

                sessionEnum.GetCount(out int count);

                for (int i = 0; i < count; i++)
                {
                    if (sessionEnum.GetSession(i, out nint pSession) != 0)
                        continue;

                    var session = (WindowsAPI.IAudioSessionControl)cw.GetOrCreateObjectForComInstance(pSession, CreateObjectFlags.None);

                    var session2 = (WindowsAPI.IAudioSessionControl2)session;
                    session2.GetProcessId(out uint sessionPid);

                    if (sessionPid == targetPid)
                    {
                        var simpleVolume = (WindowsAPI.ISimpleAudioVolume)session;
                        simpleVolume.GetMasterVolume(out float vol);
                        return vol;
                    }
                }

                return null;
            }
            catch
            {
                return null;
            }
        }
    }
}
