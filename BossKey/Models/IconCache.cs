#define FULL_ASYNC

using BossKey.Utils;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using static BossKey.Models.WindowsAPI;
using Timer = System.Threading.Timer;

namespace BossKey.Models
{
    internal static class IconCache
    {
        private static readonly int DEFAULT_TIMEOUT = 3000;
        private static readonly int DEFAULT_UPDATE_INTERVAL = 5000;
        private static readonly int TRIGGERED_UPDATE_INTERVAL = 500;

        private static readonly ConcurrentDictionary<nint, nint> _icons = [];
        private static readonly ConcurrentQueue<ManualResetEvent> _eventQueue = [];
        private static readonly Timer _timer;

        public static event WindowIconUpdatedHandler? WindowIconUpdated;

        static IconCache()
        {
            _timer = new Timer(PollUpdateIconCaches, null, DEFAULT_UPDATE_INTERVAL, DEFAULT_UPDATE_INTERVAL);
        }

        /// <summary>
        /// 定时轮询更新图标缓存
        /// </summary>
        /// <param name="state"></param>
        private static void PollUpdateIconCaches(object? state)
        {
            var icons = _icons.ToArray();

            foreach (var (hWnd, hOldIcon) in icons)
            {
                if (!IsWindow(hWnd))
                {
                    _icons.TryRemove(hWnd, out _);
                    continue;
                }

                UpdateCachedIcon(hWnd, hOldIcon);
            }
        }

        /// <summary>
        /// 获取一个 ManualResetEvent 对象，如果队列中有可用的对象，则从队列中取出并重置状态，否则创建一个新的对象
        /// </summary>
        /// <param name="initialState"></param>
        /// <returns></returns>
        private static ManualResetEvent ObtainEvent(bool initialState = false)
        {
            if (_eventQueue.TryDequeue(out ManualResetEvent? evt))
            {
                if (initialState)
                    evt.Set();
                else
                    evt.Reset();

                return evt;
            }
            else
            {
                return new ManualResetEvent(initialState);
            }
        }

        /// <summary>
        /// 回收 ManualResetEvent 对象到队列中，以便重用，减少对象创建和销毁的开销
        /// </summary>
        /// <param name="evt"></param>
        private static void RecycleEvent(ManualResetEvent evt)
        {
            _eventQueue.Enqueue(evt);
        }

        /// <summary>
        /// 发送消息获取窗口图标的核心方法，支持同步和异步回调
        /// </summary>
        /// <param name="hWnd"></param>
        /// <param name="iconSize"></param>
        /// <param name="timeout"></param>
        /// <param name="asyncCallback"></param>
        /// <returns></returns>
        private static nint? SendGetWindowIconCore(
            nint hWnd,
            IconSize iconSize,
            int timeout,
            Action<nint> asyncCallback
        )
        {
            var evt = ObtainEvent(false);
            nint syncResult = 0;
            int evtState = 0;
            int state = 0;

            Task.Run(() =>
            {
                if (Interlocked.CompareExchange(ref evtState, 1, 0) != 0)
                {
                    RecycleEvent(evt);
                }
                else
                {
                    evt.Set();
                }

                if (SendMessageTimeout(
                    hWnd,
                    WindowMessage.GetIcon,
                    (nint)iconSize, 0,
                    SendMessageTimeoutFlags.AbortIfHung | SendMessageTimeoutFlags.ErrorOnExit,
                    (uint)timeout,
                    out nint result
                ) == 0)
                {
                    result = 0;
                }

                syncResult = result;

                if (Interlocked.CompareExchange(ref state, 1, 0) != 0)
                {
                    try
                    {
                        asyncCallback?.Invoke(result);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Error in SendGetWindowIconCore async callback: {ex.Message}");
                    }
                }
            });

            evt.WaitOne(6);

            if (Interlocked.CompareExchange(ref evtState, 2, 0) != 0)
            {
                RecycleEvent(evt);
                Thread.Sleep(0);
            }

            if (Interlocked.CompareExchange(ref state, 2, 0) != 0)
            {
                return syncResult;
            }

            return null;
        }

        /// <summary>
        /// 异步发送消息获取窗口图标，尝试不同的图标大小，直到获取到有效的图标或者超时<br/>
        /// 使用异步确保可以尝试每一种图标获取方式，并在获取到图标后立即返回结果
        /// </summary>
        /// <param name="hWnd"></param>
        /// <param name="timeout"></param>
        /// <returns></returns>
        private static async Task<nint> AsyncSendGetWindowIcon(
            nint hWnd,
            int timeout
        )
        {
            var tsc = new TaskCompletionSource<nint>();
            nint? icon = SendGetWindowIconCore(hWnd, IconSize.Small, timeout, tsc.SetResult);
            nint result = 0;

            if (!icon.HasValue)
                icon = await tsc.Task;

            result = icon.Value;

            if (result != 0)
                return result;

            tsc = new TaskCompletionSource<nint>();
            icon = SendGetWindowIconCore(hWnd, IconSize.Small2, timeout, tsc.SetResult);

            if (!icon.HasValue)
                icon = await tsc.Task;

            result = icon.Value;

            if (result != 0)
                return result;

            tsc = new TaskCompletionSource<nint>();
            icon = SendGetWindowIconCore(hWnd, IconSize.Big, timeout, tsc.SetResult);

            if (!icon.HasValue)
                icon = await tsc.Task;

            result = icon.Value;

            if (result != 0)
                return result;

            if (result == 0)
                result = GetClassLong(hWnd, ClassLongIndex.HIconSm);
            if (result == 0)
                result = GetClassLong(hWnd, ClassLongIndex.HIcon);

            return result;
        }

        /// <summary>
        /// 发送消息获取窗口图标，尝试不同的图标大小，支持同步和异步回调
        /// </summary>
        /// <param name="hWnd"></param>
        /// <param name="timeout"></param>
        /// <param name="asyncCallback"></param>
        /// <returns></returns>
        private static nint? SendGetWindowIcon(
            nint hWnd,
            int timeout,
            Action<nint> asyncCallback
        )
        {
            var task = AsyncSendGetWindowIcon(hWnd, timeout);

            if (task.IsCompleted)
                return task.Result;

            task.ContinueWith(t =>
            {
                try
                {
                    asyncCallback?.Invoke(t.Result);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error in SendGetWindowIcon async callback: {ex.Message}");
                }
            });

            return null;
        }

        /// <summary>
        /// 发送消息获取窗口图标，并在异步回调中处理结果
        /// </summary>
        /// <param name="hWnd"></param>
        /// <param name="timeout"></param>
        /// <param name="asyncCallback"></param>
        private static void SendGetWindowIconCallback(
            nint hWnd,
            int timeout,
            Action<nint> asyncCallback
        )
        {
            nint? result = SendGetWindowIcon(hWnd, timeout, asyncCallback);

            if (result.HasValue)
            {
                try
                {
                    asyncCallback?.Invoke(result.Value);

                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error in SendGetWindowIconCallback async callback: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// 分发图标更新事件
        /// </summary>
        /// <param name="hWnd"></param>
        /// <param name="icon"></param>
        private static void DispatchIconUpdate(nint hWnd, nint icon)
        {
            try
            {
                WindowIconUpdated?.Invoke(hWnd, icon);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in DispatchIconUpdate dispatching icon update: {ex.Message}");
            }
        }

        /// <summary>
        /// 更新缓存中的图标，如果图标发生变化，则触发图标更新事件
        /// </summary>
        /// <param name="hWnd"></param>
        /// <param name="cachedIcon"></param>
        private static void UpdateCachedIcon(nint hWnd, nint cachedIcon)
        {
            SendGetWindowIconCallback(hWnd, DEFAULT_TIMEOUT, (result) =>
            {
                if (result == cachedIcon)
                {
                    return;
                }

                _icons[hWnd] = result;
                DispatchIconUpdate(hWnd, result);
            });
        }

        /// <summary>
        /// 获取窗口图标，不使用缓存，直接发送消息获取图标，并在异步回调中更新缓存和触发图标更新事件
        /// </summary>
        /// <param name="hWnd"></param>
        /// <returns></returns>
        private static nint GetWindowIconNoCache(nint hWnd)
        {
            nint? result = SendGetWindowIcon(hWnd, DEFAULT_TIMEOUT, (result) =>
            {
                _icons[hWnd] = result;
                DispatchIconUpdate(hWnd, result);
            });

            if (result.HasValue)
            {
                _icons[hWnd] = result.Value;
                DispatchIconUpdate(hWnd, result.Value);
                return result.Value;
            }

            // 暂时给一个占位值，等待回调异步更新
            return 0;
        }

        private static string BuildWindowIconSite(nint hWnd)
        {
            return $"window-icon-{hWnd}";
        }

        /// <summary>
        /// 触发图标缓存更新，使用防抖机制，避免频繁更新
        /// </summary>
        /// <param name="hWnd"></param>
        /// <param name="hOldIcon"></param>
        private static void TriggerUpdateIconCache(nint hWnd, nint hOldIcon)
        {
            LazyCall.Debounce(
                BuildWindowIconSite(hWnd),
                TRIGGERED_UPDATE_INTERVAL,
                () => UpdateCachedIcon(hWnd, hOldIcon)
            );
        }

        /// <summary>
        /// 获取窗口图标，优先使用缓存，如果缓存不存在，则发送消息获取图标，并在异步回调中更新缓存和触发图标更新事件
        /// </summary>
        /// <param name="hWnd"></param>
        /// <returns></returns>
        public static nint GetWindowIcon(nint hWnd)
        {
            if (!IsWindow(hWnd))
            {
                return 0;
            }

            if (_icons.TryGetValue(hWnd, out nint cachedIcon))
            {
                TriggerUpdateIconCache(hWnd, cachedIcon);
                return cachedIcon;
            }

            return GetWindowIconNoCache(hWnd);
        }
    }

    internal delegate void WindowIconUpdatedHandler(nint hWnd, nint hIcon);
}
