using BossKey.Utils;
using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace BossKey.Models
{
    internal partial class WindowsAPI
    {
        #region Enums

        /// <summary>ShowWindow 命令</summary>
        public enum ShowWindowCmd : int
        {
            Hide = 0,
            ShowNormal = 1,
            ShowMinimized = 2,
            ShowMaximized = 3,
            ShowNoActivate = 4,
            Show = 5,
            Minimize = 6,
            ShowMinNoActive = 7,
            ShowNA = 8,
            Restore = 9
        }

        /// <summary>GetWindowLong / SetWindowLong 索引</summary>
        public enum WindowLongIndex : int
        {
            ExStyle = -20,
            Style = -16
        }

        /// <summary>扩展窗口样式 (WS_EX_*)</summary>
        [Flags]
        public enum WindowExStyle : uint
        {
            TopMost = 0x00000008,
            Layered = 0x00080000,
            Transparent = 0x00000020,
            ToolWindow = 0x00000080,
            AppWindow = 0x00040000
        }

        /// <summary>SetLayeredWindowAttributes 标志</summary>
        [Flags]
        public enum LayeredWindowAttribute : uint
        {
            ColorKey = 0x00000001,
            Alpha = 0x00000002
        }

        /// <summary>SetWindowPos 标志</summary>
        [Flags]
        public enum SetWindowPosFlags : uint
        {
            NoSize = 0x0001,
            NoMove = 0x0002,
            NoZOrder = 0x0004,
            NoRedraw = 0x0008,
            NoActivate = 0x0010,
            ShowWindow = 0x0040,
            HideWindow = 0x0080
        }

        /// <summary>SetWinEventHook 标志</summary>
        [Flags]
        public enum WinEventFlags : uint
        {
            OutOfContext = 0x0000,
            SkipOwnThread = 0x0001,
            SkipOwnProcess = 0x0002,
            InContext = 0x0004
        }

        /// <summary>WinEvent 事件 ID（常用）</summary>
        public enum WinEventId : uint
        {
            Min = 0x00000001,
            Max = 0x7FFFFFFF,
            ObjectCreate = 0x8000,
            ObjectDestroy = 0x8001,
            ObjectShow = 0x8002,
            ObjectHide = 0x8003,
            ObjectReorder = 0x8004,
            ObjectFocus = 0x8005,
            ObjectSelection = 0x8006,
            ObjectSelectionAdd = 0x8007,
            ObjectSelectionRemove = 0x8008,
            ObjectSelectionWithin = 0x8009,
            ObjectStateChange = 0x800A,
            ObjectLocationChange = 0x800B,
            ObjectNameChange = 0x800C,
            ObjectDescriptionChange = 0x800D,
            ObjectValueChange = 0x800E,
            ObjectParentChange = 0x800F,
            ObjectHelpChange = 0x8010,
            ObjectDefActionChange = 0x8011,
            ObjectAcceleratorChange = 0x8012,
            SystemSound = 0x0001,
            SystemAlert = 0x0002,
            SystemForeground = 0x0003,
            SystemMenuStart = 0x0004,
            SystemMenuEnd = 0x0005,
            SystemMenuPopupStart = 0x0006,
            SystemMenuPopupEnd = 0x0007,
            SystemCaptureStart = 0x0008,
            SystemCaptureEnd = 0x0009,
            SystemMoveSizeStart = 0x000A,
            SystemMoveSizeEnd = 0x000B,
            SystemContextHelpStart = 0x000C,
            SystemContextHelpEnd = 0x000D,
            SystemDragStart = 0x000E,
            SystemDragEnd = 0x000F,
            SystemDialogStart = 0x0010,
            SystemDialogEnd = 0x0011,
            SystemScrollingStart = 0x0012,
            SystemScrollingEnd = 0x0013,
            SystemSwitchStart = 0x0014,
            SystemSwitchEnd = 0x0015,
            SystemMinimizeStart = 0x0016,
            SystemMinimizeEnd = 0x0017,
            SystemDesktopSwitch = 0x0020
        }

        /// <summary>WM_GETICON 的 wParam 值</summary>
        public enum IconSize : int
        {
            Small = 0,
            Big = 1,
            Small2 = 2,
        }

        /// <summary>GetClassLong 索引</summary>
        public enum ClassLongIndex : int
        {
            HIcon = -14,
            HIconSm = -34
        }

        /// <summary>常用窗口消息</summary>
        public enum WindowMessage : uint
        {
            GetIcon = 0x007F,
            HotKey = 0x0312
        }

        /// <summary>RegisterHotKey 修饰符</summary>
        [Flags]
        public enum HotKeyModifiers : uint
        {
            None = 0x0000,
            Alt = 0x0001,
            Control = 0x0002,
            Shift = 0x0004,
            Win = 0x0008,
            NoRepeat = 0x4000
        }

        /// <summary>GetAncestor 标志</summary>
        public enum GetAncestorFlags : uint
        {
            Parent = 1,
            Root = 2,
            RootOwner = 3
        }

        #endregion

        #region Special HWND Values

        public static readonly nint HWND_TOPMOST = -1;
        public static readonly nint HWND_NOTOPMOST = -2;
        public static readonly nint HWND_TOP = 0;
        public static readonly nint HWND_BOTTOM = 1;

        #endregion

        #region User32.dll P/Invoke

        [LibraryImport("user32.dll", SetLastError = true)]
        public static partial uint GetWindowThreadProcessId(nint hWnd, out uint lpdwProcessId);

        [LibraryImport("user32.dll", EntryPoint = "FindWindowW", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
        public static partial nint FindWindow(string? lpClassName, string? lpWindowName);

        [LibraryImport("user32.dll", EntryPoint = "FindWindowExW", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
        public static partial nint FindWindowEx(nint hWndParent, nint hWndChildAfter, string? lpszClass, string? lpszWindow);

        [LibraryImport("user32.dll", EntryPoint = "GetWindowTextW", SetLastError = true)]
        public static partial int GetWindowText(nint hWnd, byte[] lpString, int nMaxCount);

        [LibraryImport("user32.dll", EntryPoint = "GetWindowTextLengthW", SetLastError = true)]
        public static partial int GetWindowTextLength(nint hWnd);

        [LibraryImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static partial bool IsWindowVisible(nint hWnd);

        [LibraryImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static partial bool IsWindow(nint hWnd);

        [LibraryImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static partial bool GetWindowRect(nint hWnd, out RECT lpRect);

        [LibraryImport("user32.dll", EntryPoint = "SetWindowLongW", SetLastError = true)]
        public static partial nint SetWindowLong(nint hWnd, WindowLongIndex nIndex, nint dwNewLong);

        [LibraryImport("user32.dll", EntryPoint = "GetWindowLongW", SetLastError = true)]
        public static partial nint GetWindowLong(nint hWnd, WindowLongIndex nIndex);

        [LibraryImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static partial bool SetLayeredWindowAttributes(nint hWnd, uint crKey, byte bAlpha, LayeredWindowAttribute dwFlags);

        [LibraryImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static partial bool ShowWindow(nint hWnd, ShowWindowCmd nCmdShow);

        [LibraryImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static partial bool SetWindowPos(nint hWnd, nint hWndInsertAfter, int X, int Y, int cx, int cy, SetWindowPosFlags uFlags);

        [LibraryImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static partial bool RegisterHotKey(nint hWnd, int id, uint fsModifiers, uint vk);

        [LibraryImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static partial bool UnregisterHotKey(nint hWnd, int id);

        /// <summary>EnumWindows 回调委托</summary>
        public delegate bool EnumWindowsProc(nint hWnd, nint lParam);

        [LibraryImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static partial bool EnumWindows(EnumWindowsProc lpEnumFunc, nint lParam);

        /// <summary>WinEvent 回调委托</summary>
        public delegate void WinEventProc(nint hWinEventHook, uint eventType, nint hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime);

        [LibraryImport("user32.dll", SetLastError = true)]
        public static partial nint SetWinEventHook(WinEventId eventMin, WinEventId eventMax, nint hmodWinEventProc, WinEventProc lpfnWinEventProc, uint idProcess, uint idThread, WinEventFlags dwFlags);

        [LibraryImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static partial bool UnhookWinEvent(nint hWinEventHook);

        [LibraryImport("user32.dll", EntryPoint = "SendMessageW", SetLastError = true)]
        public static partial nint SendMessage(nint hWnd, WindowMessage Msg, nint wParam, nint lParam);

        [LibraryImport("user32.dll", EntryPoint = "GetClassLongPtrW", SetLastError = true)]
        public static partial nint GetClassLong(nint hWnd, ClassLongIndex nIndex);

        [LibraryImport("user32.dll", SetLastError = true)]
        public static partial nint GetAncestor(nint hWnd, GetAncestorFlags gaFlags);

        [LibraryImport("user32.dll", SetLastError = true)]
        public static partial nint GetDesktopWindow();

        [LibraryImport("kernel32.dll", SetLastError = true)]
        public static partial int GetCurrentProcessId();

        #endregion

        #region Structs

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        #endregion Structs

        #region COM Interfaces

        /// <summary>
        /// IMMDeviceEnumerator - {A95664D2-9614-4F35-A746-DE8DB63617E6}
        /// </summary>
        [GeneratedComInterface]
        [Guid("A95664D2-9614-4F35-A746-DE8DB63617E6")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        public partial interface IMMDeviceEnumerator
        {
            int EnumAudioEndpoints(int dataFlow, int dwStateMask, out nint ppDevices);

            int GetDefaultAudioEndpoint(int dataFlow, int role, out nint ppDevice);

            int GetDevice([MarshalAs(UnmanagedType.LPWStr)] string pwstrId, out nint ppDevice);

            int RegisterEndpointNotificationCallback(nint pClient);

            int UnregisterEndpointNotificationCallback(nint pClient);
        }

        /// <summary>
        /// IMMDevice - {D666063F-1587-4E43-81F1-B948E807363F}
        /// </summary>
        [GeneratedComInterface]
        [Guid("D666063F-1587-4E43-81F1-B948E807363F")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        public partial interface IMMDevice
        {
            int Activate(ref Guid iid, uint dwClsCtx, nint pActivationParams, out nint ppInterface);

            int OpenPropertyStore(uint stgmAccess, out nint ppProperties);

            int GetId([MarshalAs(UnmanagedType.LPWStr)] out string ppstrId);

            int GetState(out uint pdwState);
        }

        /// <summary>
        /// IAudioSessionManager2 - {77AA99A0-1BD6-484F-8BC7-2C654C9A9B6F}
        /// </summary>
        [GeneratedComInterface]
        [Guid("77AA99A0-1BD6-484F-8BC7-2C654C9A9B6F")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        public partial interface IAudioSessionManager2
        {
            int GetAudioSessionControl(nint AudioSessionGuid, uint StreamFlags, out nint SessionControl);

            int GetSimpleAudioVolume(nint AudioSessionGuid, uint StreamFlags, out nint AudioVolume);

            int GetSessionEnumerator(out nint SessionEnum);

            int RegisterSessionNotification(nint NewSession);

            int UnregisterSessionNotification(nint SessionNotification);

            int RegisterDuckNotification([MarshalAs(UnmanagedType.LPWStr)] string sessionID, nint duckNotification);

            int UnregisterDuckNotification(nint duckNotification);
        }

        /// <summary>
        /// IAudioSessionEnumerator - {E2F5BB11-0570-40CA-ACDD-3AA01277DEE8}
        /// </summary>
        [GeneratedComInterface]
        [Guid("E2F5BB11-0570-40CA-ACDD-3AA01277DEE8")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        public partial interface IAudioSessionEnumerator
        {
            int GetCount(out int SessionCount);

            int GetSession(int SessionCount, out nint Session);
        }

        /// <summary>
        /// IAudioSessionControl - {F4B1A599-7266-4319-A8CA-E70ACB11E8CD}
        /// </summary>
        [GeneratedComInterface]
        [Guid("F4B1A599-7266-4319-A8CA-E70ACB11E8CD")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        public partial interface IAudioSessionControl
        {
            int GetState(out int pRetVal);

            int GetDisplayName([MarshalAs(UnmanagedType.LPWStr)] out string pRetVal);

            int SetDisplayName([MarshalAs(UnmanagedType.LPWStr)] string Value, ref Guid EventContext);

            int GetIconPath([MarshalAs(UnmanagedType.LPWStr)] out string pRetVal);

            int GetGroupingParam(out Guid pRetVal);

            int SetGroupingParam(ref Guid Override, ref Guid EventContext);

            int RegisterAudioSessionNotification(nint NewNotifications);

            int UnregisterAudioSessionNotification(nint NewNotifications);
        }

        /// <summary>
        /// IAudioSessionControl2 - {BFB7FF88-7239-4FC9-8FA2-07C950BE9C6D}
        /// </summary>
        [GeneratedComInterface]
        [Guid("BFB7FF88-7239-4FC9-8FA2-07C950BE9C6D")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        public partial interface IAudioSessionControl2
        {
            // IAudioSessionControl methods
            int GetState(out int pRetVal);

            int GetDisplayName([MarshalAs(UnmanagedType.LPWStr)] out string pRetVal);

            int SetDisplayName([MarshalAs(UnmanagedType.LPWStr)] string Value, ref Guid EventContext);

            int GetIconPath([MarshalAs(UnmanagedType.LPWStr)] out string pRetVal);

            int GetGroupingParam(out Guid pRetVal);

            int SetGroupingParam(ref Guid Override, ref Guid EventContext);

            int RegisterAudioSessionNotification(nint NewNotifications);

            int UnregisterAudioSessionNotification(nint NewNotifications);

            // IAudioSessionControl2 methods
            int GetSessionIdentifier([MarshalAs(UnmanagedType.LPWStr)] out string pRetVal);

            int GetSessionInstanceIdentifier([MarshalAs(UnmanagedType.LPWStr)] out string pRetVal);

            int GetProcessId(out uint pRetVal);

            [return: MarshalAs(UnmanagedType.Bool)]
            bool IsSystemSoundsSession();

            int SetDuckingPreference([MarshalAs(UnmanagedType.Bool)] bool optOut);
        }

        /// <summary>
        /// ISimpleAudioVolume - {87CE5498-68D6-44E5-9215-6DA47EF883D8}
        /// </summary>
        [GeneratedComInterface]
        [Guid("87CE5498-68D6-44E5-9215-6DA47EF883D8")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        public partial interface ISimpleAudioVolume
        {
            int SetMasterVolume(float fLevel, ref Guid EventContext);

            int GetMasterVolume(out float pfLevel);

            int SetMute([MarshalAs(UnmanagedType.Bool)] bool bMute, ref Guid EventContext);

            int GetMute([MarshalAs(UnmanagedType.Bool)] out bool pbMute);
        }

        #endregion COM Interfaces

        #region Helpers

        /// <summary>
        /// 断言上一次P/Invoke调用成功，失败抛出Win32异常
        /// </summary>
        /// <param name="message"></param>
        /// <exception cref="System.ComponentModel.Win32Exception"></exception>
        public static void AssertLastError(string? message = null)
        {
            int errorCode = Marshal.GetLastPInvokeError();

            if (errorCode != 0)
            {
                if (message == null)
                    throw new Win32Exception(errorCode);
                else
                    throw new Win32Exception(errorCode, message);
            }
        }

        /// <summary>
        /// 检查上一次P/Invoke调用是否成功
        /// </summary>
        /// <returns></returns>
        /// <exception cref="System.ComponentModel.Win32Exception"></exception>
        public static bool IsLastErrorSuccess()
        {
            int errorCode = Marshal.GetLastPInvokeError();
            return errorCode == 0;
        }

        /// <summary>
        /// 将应用定义的修饰键码（区分左右）转化成系统定义（不区分左右）。
        /// </summary>
        /// <param name="modifierKey">应用侧 ModifierKey 标志组合</param>
        /// <returns>系统 MOD_ALT/MOD_CONTROL/MOD_SHIFT/MOD_WIN 组合</returns>
        public static int ModifierKeyToSystem(ModifierKey modifierKey)
        {
            int result = 0;

            if ((modifierKey & (ModifierKey.LControl | ModifierKey.RControl)) != 0)
                result |= (int)HotKeyModifiers.Control;
            if ((modifierKey & (ModifierKey.LShift | ModifierKey.RShift)) != 0)
                result |= (int)HotKeyModifiers.Shift;
            if ((modifierKey & (ModifierKey.LAlt | ModifierKey.RAlt)) != 0)
                result |= (int)HotKeyModifiers.Alt;
            if ((modifierKey & (ModifierKey.LWindows | ModifierKey.RWindows)) != 0)
                result |= (int)HotKeyModifiers.Win;

            return result;
        }

        /// <summary>
        /// 将系统定义的修饰键码（不区分左右）转化成应用定义（统一归为左修饰键）。
        /// </summary>
        /// <param name="modifierKey">系统 MOD_ALT/MOD_CONTROL/MOD_SHIFT/MOD_WIN 组合</param>
        /// <returns>应用侧 ModifierKey 标志组合</returns>
        public static ModifierKey ModifierKeyToApplication(int modifierKey)
        {
            ModifierKey result = ModifierKey.None;

            if ((modifierKey & (int)HotKeyModifiers.Alt) != 0)
                result |= ModifierKey.LAlt;
            if ((modifierKey & (int)HotKeyModifiers.Control) != 0)
                result |= ModifierKey.LControl;
            if ((modifierKey & (int)HotKeyModifiers.Shift) != 0)
                result |= ModifierKey.LShift;
            if ((modifierKey & (int)HotKeyModifiers.Win) != 0)
                result |= ModifierKey.LWindows;

            return result;
        }

        /// <summary>
        /// 将 WinForms Keys 虚拟键码转化成系统定义的虚拟键码。
        /// WinForms Keys 枚举值与 Windows VK_* 虚拟键码一一对应，
        /// 仅需剥离修饰键标志位。
        /// </summary>
        /// <param name="key">WinForms Keys 枚举值</param>
        /// <returns>Windows 虚拟键码</returns>
        public static int VirtualKeyToSystem(Keys key)
        {
            return (int)(key & Keys.KeyCode);
        }

        /// <summary>
        /// 将系统定义的虚拟键码转化成 WinForms Keys 枚举。
        /// </summary>
        /// <param name="vk">Windows 虚拟键码</param>
        /// <returns>WinForms Keys 枚举值</returns>
        public static Keys VirtualKeyToApplication(int vk)
        {
            return (Keys)vk;
        }

        #endregion Helpers
    }
}
