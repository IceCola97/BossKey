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
        private readonly HashSet<nint> _shownHandles = [];
        private readonly Lock _lock = new();
        private volatile int _scanLock = 0;

#if USE_WINEVENT
        private readonly WindowsAPI.WinEventProc _winEventCallback;
        private nint _hookWatch;
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
        public event WindowShownEventHandler? WindowShown;
        public event WindowHiddenEventHandler? WindowHidden;

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

        private void DispatchWindowShown(ScannedWindow scannedWindow)
        {
            try
            {
                WindowShown?.Invoke(scannedWindow);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"WindowShown event handler threw an exception: {ex}");
            }
        }

        private void DispatchWindowHidden(ScannedWindow scannedWindow)
        {
            try
            {
                WindowHidden?.Invoke(scannedWindow);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"WindowHidden event handler threw an exception: {ex}");
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

            // 监听窗口相关事件
            _hookWatch = WindowsAPI.SetWinEventHook(
                WindowsAPI.WinEventId.ObjectCreate,
                WindowsAPI.WinEventId.ObjectHide,
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
            var visibleSet = new HashSet<nint>();

            // 枚举所有顶层窗口并存入 HashSet
            WindowsAPI.EnumWindows((hWnd, _) =>
            {
                if (ScannerFilter(hWnd))
                {
                    scanResult.Add(hWnd);

                    if (WindowsAPI.IsWindowVisible(hWnd))
                    {
                        visibleSet.Add(hWnd);
                    }
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

                    if (WindowsAPI.IsWindowVisible(handle))
                    {
                        visibleSet.Add(handle);
                    }
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

                _shownHandles.Clear();
                _shownHandles.UnionWith(visibleSet);
            }

            var currentFilter = FilterInternal;

            // 计算创建与销毁集合
            if (removedSet != null)
            {
                foreach (nint handle in removedSet)
                {
                    var scannedWindow = ForceGetFromHandle(handle, currentFilter);
                    DispatchWindowDestroyed(scannedWindow);
                }
            }

            if (addedSet != null)
            {
                foreach (nint handle in addedSet)
                {
                    var scannedWindow = GetFromHandle(handle, currentFilter);

                    if (scannedWindow is not null)
                        DispatchWindowCreated(scannedWindow);
                }
            }

            // 计算可见性变化集合
            if (triggerEvent)
            {
                HashSet<nint> showSet = [.. visibleSet];
                HashSet<nint> hideSet = [.. scanResult];

                hideSet.ExceptWith(showSet);

                if (addedSet != null)
                {
                    showSet.ExceptWith(addedSet);
                    hideSet.ExceptWith(addedSet);
                }

                if (removedSet != null)
                {
                    showSet.ExceptWith(removedSet);
                    hideSet.ExceptWith(removedSet);
                }

                foreach (nint handle in showSet)
                {
                    var scannedWindow = GetFromHandle(handle, currentFilter);

                    if (scannedWindow is not null)
                        DispatchWindowShown(scannedWindow);
                }

                foreach (nint handle in hideSet)
                {
                    var scannedWindow = GetFromHandle(handle, currentFilter);

                    if (scannedWindow is not null)
                        DispatchWindowHidden(scannedWindow);
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
                case WindowsAPI.WinEventId.ObjectShow:
                    HandleShowWindow(hWnd);
                    break;
                case WindowsAPI.WinEventId.ObjectHide:
                    HandleHideWindow(hWnd);
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

            var scannedWindow = GetFromHandle(hWnd, FilterInternal);

            if (scannedWindow is not null)
            {
                if (scannedWindow.Visible)
                {
                    lock (_lock)
                    {
                        _shownHandles.Add(hWnd);
                    }
                }

                DispatchWindowCreated(scannedWindow);
            }
        }

        private void HandleRemoveWindow(nint hWnd)
        {
            lock (_lock)
            {
                if (!_handles.Remove(hWnd))
                    return;

                _shownHandles.Remove(hWnd);
            }

            var scannedWindow = ForceGetFromHandle(hWnd, FilterInternal);
            DispatchWindowDestroyed(scannedWindow);
        }

        private void HandleShowWindow(nint hWnd)
        {
            lock (_lock)
            {
                if (!_handles.Contains(hWnd))
                    return;

                _shownHandles.Add(hWnd);
            }

            var scannedWindow = GetFromHandle(hWnd, FilterInternal);

            if (scannedWindow is not null)
                DispatchWindowShown(scannedWindow);
        }

        private void HandleHideWindow(nint hWnd)
        {
            lock (_lock)
            {
                if (!_handles.Contains(hWnd))
                    return;

                _shownHandles.Remove(hWnd);
            }

            var scannedWindow = GetFromHandle(hWnd, FilterInternal);

            if (scannedWindow is not null)
                DispatchWindowHidden(scannedWindow);
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
                    currentFilter = FilterInternal;
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

        private string? FilterInternal
            => string.IsNullOrEmpty(_filter) ? null : _filter;

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
                    ProcessId = 0,
                    Visible = false,
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

        private static bool IsEmptyRect(in WindowsAPI.RECT rect)
        {
            return rect.Left >= rect.Right || rect.Top >= rect.Bottom;
        }

        private static bool IsValidWindow(nint hWnd)
        {
            var style = (WindowsAPI.WindowStyle)WindowsAPI.GetWindowLong(hWnd, WindowsAPI.WindowLongIndex.Style);
            var exStyle = (WindowsAPI.WindowExStyle)WindowsAPI.GetWindowLong(hWnd, WindowsAPI.WindowLongIndex.ExStyle);
            return (style & (
                    WindowsAPI.WindowStyle.Child
                    | WindowsAPI.WindowStyle.Disabled
                )) == 0
                && (exStyle & (
                    WindowsAPI.WindowExStyle.NoActivate
                )) == 0;
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
            if (!WindowsAPI.GetWindowRect(hWnd, out var rect)
                || IsEmptyRect(rect))
                return false;
            // 检查窗口标题是否为空
            if (WindowsAPI.GetWindowTextLength(hWnd) <= 0)
                return false;
            // 检查窗口是否有效
            if (!IsValidWindow(hWnd))
                return false;

#if HIDE_SELF
            // 检查窗口是否属于当前进程（如果是，则忽略）
            if (WindowsAPI.GetWindowThreadProcessId(hWnd, out uint pid) != 0
                && pid == ThisProcessId)
                return false;
#endif

            return true;
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) == 1)
                return;

#if USE_WINEVENT
            if (_hookWatch != 0)
            {
                WindowsAPI.UnhookWinEvent(_hookWatch);
                _hookWatch = 0;
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
