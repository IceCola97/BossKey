#define USE_WINEVENT
#define USE_POLLWATCH
//#define SHORT_POLLWATCH
#define HIDE_SELF

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Text;
using Timer = System.Threading.Timer;

namespace BossKey.Models
{
    internal sealed class WindowScanner : IWindowScanner, IDisposable
    {
        #region Fields

        private static readonly int ThisProcessId = WindowsAPI.GetCurrentProcessId();

        private readonly HashSet<nint> _handles = [];
        private readonly Lock _lock = new();
        private volatile int _scanLock = 0;

#if USE_WINEVENT
        private readonly WindowsAPI.WinEventProc _winEventCallback;
        private nint _hookCreate;
        private nint _hookDestroy;
#endif

#if USE_POLLWATCH
        private readonly Timer _scanTimer;
#endif

        private string? _filter;
        private int _disposed = 0;

        #endregion

        #region Events

        public event WindowCreatedEventHandler? WindowCreated;
        public event WindowDestroyedEventHandler? WindowDestroyed;

        private void DispatchWindowCreated(ScannedWindow window)
        {
            try
            {
                WindowCreated?.Invoke(window);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"WindowCreated event handler threw an exception: {ex}");
            }
        }

        private void DispatchWindowDestroyed(ScannedWindow window)
        {
            try
            {
                WindowDestroyed?.Invoke(window);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"WindowDestroyed event handler threw an exception: {ex}");
            }
        }

        #endregion Events

        #region Constructor

        public WindowScanner()
        {
            ScanAll(false);

#if USE_WINEVENT
            // 保存委托引用防止被 GC 回收
            _winEventCallback = OnWinEvent;

            // 监听窗口创建事件
            _hookCreate = WindowsAPI.SetWinEventHook(
                WindowsAPI.WinEventId.ObjectCreate,
                WindowsAPI.WinEventId.ObjectCreate,
                0,
                _winEventCallback,
                0, 0,
                WindowsAPI.WinEventFlags.OutOfContext
             );

            WindowsAPI.AssertLastError();

            // 监听窗口销毁事件
            _hookDestroy = WindowsAPI.SetWinEventHook(
                WindowsAPI.WinEventId.ObjectDestroy,
                WindowsAPI.WinEventId.ObjectDestroy,
                0,
                _winEventCallback,
                0, 0,
                WindowsAPI.WinEventFlags.OutOfContext
             );

            WindowsAPI.AssertLastError();
#endif

#if USE_POLLWATCH
#if SHORT_POLLWATCH
            // 定时全量扫描：每 500ms 执行一次
            _scanTimer = new Timer(_ => ScanAll(true), null, 500, 500);
#else
            // 定时全量扫描：首次 500ms 后执行，之后每 5000ms 执行一次
            _scanTimer = new Timer(_ => ScanAll(true), null, 500, 5000);
#endif
#endif
        }

        ~WindowScanner()
        {
            Dispose();
        }

        #endregion

        #region Scanner

        private void ScanAll(bool triggerEvent)
        {
            if (Interlocked.CompareExchange(ref _scanLock, 1, 0) != 0)
                return;

            var scanResult = new HashSet<nint>();

            // 枚举所有顶层窗口并存入 HashSet
            WindowsAPI.EnumWindows((hWnd, _) =>
            {
                if (ScannerFilter(hWnd))
                {
                    scanResult.Add(hWnd);
                }

                return true;
            }, 0);

            // EnumWindows 可能出现错误，我们要重新校正

            HashSet<nint>? oldSet = null;

            lock (_lock)
            {
                oldSet = [.. _handles];
            }

            oldSet.ExceptWith(scanResult);

            foreach (nint handle in oldSet)
            {
                if (ScannerFilter(handle))
                {
                    scanResult.Add(handle);
                }
            }

            HashSet<nint>? addedSet = null;
            HashSet<nint>? removedSet = null;

            lock (_lock)
            {
                if (triggerEvent)
                {
                    addedSet = [.. scanResult];
                    removedSet = [.. _handles];

                    addedSet.ExceptWith(_handles);
                    removedSet.ExceptWith(scanResult);
                }

                _handles.Clear();
                _handles.UnionWith(scanResult);
            }

            if (removedSet != null)
            {
                foreach (nint handle in removedSet)
                {
                    if (ScannerFilter(handle))
                    {
                        Debugger.Break();
                    }

                    var scannedWindow = ForceGetFromHandle(handle, _filter);
                    DispatchWindowDestroyed(scannedWindow);
                }
            }

            if (addedSet != null)
            {
                foreach (nint handle in addedSet)
                {
                    var scannedWindow = GetFromHandle(handle, _filter);

                    if (scannedWindow is not null)
                        DispatchWindowCreated(scannedWindow);
                }
            }

            Interlocked.Exchange(ref _scanLock, 0);
        }

        #endregion Scanner

        #region WinEvent Callback

#if USE_WINEVENT
        private void OnWinEvent(nint hWinEventHook, uint eventType, nint hWnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime)
        {
            // 只关心窗口对象级别的事件，忽略子对象
            if (idObject != 0 || idChild != 0)
                return;

            var eventId = (WindowsAPI.WinEventId)eventType;

            switch (eventId)
            {
                case WindowsAPI.WinEventId.ObjectCreate:
                    PostAddWindow(hWnd);
                    break;
                case WindowsAPI.WinEventId.ObjectDestroy:
                    HandleRemoveWindow(hWnd);
                    break;
            }
        }

        private void PostAddWindow(nint hWnd, int duration = 300)
        {
            Task.Run(async () =>
            {
                await Task.Delay(duration); // 延迟指定毫秒后处理窗口添加

                HandleAddWindow(hWnd);
            });
        }

        private void HandleAddWindow(nint hWnd)
        {
            if (!ScannerFilter(hWnd))
                return;

            lock (_lock)
            {
                if (!_handles.Add(hWnd))
                    return;
            }

            var scannedWindow = GetFromHandle(hWnd, _filter);

            if (scannedWindow is not null)
                DispatchWindowCreated(scannedWindow);
        }

        private void HandleRemoveWindow(nint hWnd)
        {
            lock (_lock)
            {
                if (!_handles.Remove(hWnd))
                    return;
            }

            var scannedWindow = ForceGetFromHandle(hWnd, _filter);
            DispatchWindowDestroyed(scannedWindow);
        }
#endif

        #endregion

        #region IWindowScanner Implementation

        public int WindowCount
        {
            get
            {
                lock (_lock)
                {
                    return _handles.Count;
                }
            }
        }

        public IEnumerable<ScannedWindow> Windows
        {
            get
            {
                // 快照：保存当前句柄集合和过滤条件
                nint[] handlesSnapshot;
                string? currentFilter;

                lock (_lock)
                {
                    handlesSnapshot = new nint[_handles.Count];
                    _handles.CopyTo(handlesSnapshot);
                    currentFilter = _filter;
                }

                foreach (var hWnd in handlesSnapshot)
                {
                    var scannedWindow = GetFromHandle(hWnd, currentFilter);

                    if (scannedWindow is not null)
                        yield return scannedWindow;
                }
            }
        }

        public string? Filter
        {
            get => _filter;
            set => _filter = value;
        }

        #endregion

        #region Helpers
        private static ScannedWindow ForceGetFromHandle(
            nint hWnd,
            string? filter = null
        )
        {
            var result = GetFromHandle(hWnd, filter);

            if (result is null)
                return new ScannedWindow
                {
                    Handle = hWnd,
                    Title = null,
                    ProcessId = 0
                };

            return result;
        }

        private static ScannedWindow? GetFromHandle(
            nint hWnd,
            string? filter = null
        )
        {
            if (!ScannerFilter(hWnd))
                return null;

            // 获取窗口标题
            string? title = GetWindowTitle(hWnd);

            if (string.IsNullOrEmpty(title))
                return null;

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

            return new ScannedWindow
            {
                Handle = hWnd,
                Title = title,
                ProcessId = (int)pid
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

        private static bool IsEmptyRect(in WindowsAPI.RECT rect)
        {
            return rect.Left >= rect.Right || rect.Top >= rect.Bottom;
        }

        private static bool ScannerFilter(nint hWnd)
        {
            if (hWnd == 0)
                return false;
            // 检查窗口是否为顶级窗口（父窗口必须是 DesktopWindow）
            if (WindowsAPI.GetAncestor(hWnd, WindowsAPI.GetAncestorFlags.Parent) != WindowsAPI.GetDesktopWindow())
                return false;
            // 检查窗口是否仍然存在
            if (!WindowsAPI.IsWindow(hWnd))
                return false;
            // 检查窗口是否可见
            if (!WindowsAPI.IsWindowVisible(hWnd))
                return false;
            if (!WindowsAPI.GetWindowRect(hWnd, out var rect)
                || IsEmptyRect(rect))
                return false;
            // 检查窗口标题是否为空
            if (WindowsAPI.GetWindowTextLength(hWnd) <= 0)
                return false;
            // 检查窗口是否属于当前进程（如果是，则忽略）
            if (WindowsAPI.GetWindowThreadProcessId(hWnd, out uint pid) != 0
                && pid == ThisProcessId)
                return false;

            return true;
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) == 1)
                return;

#if USE_WINEVENT
            if (_hookCreate != 0)
            {
                WindowsAPI.UnhookWinEvent(_hookCreate);
                _hookCreate = 0;
            }

            if (_hookDestroy != 0)
            {
                WindowsAPI.UnhookWinEvent(_hookDestroy);
                _hookDestroy = 0;
            }
#endif

#if USE_POLLWATCH
            _scanTimer.Dispose();
#endif

            GC.SuppressFinalize(this);
        }

        #endregion
    }
}
