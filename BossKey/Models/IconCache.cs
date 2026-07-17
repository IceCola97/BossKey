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
        private static readonly ConcurrentQueue<ManualResetEventSlim> _eventQueue = [];
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
        private static ManualResetEventSlim ObtainEvent(bool initialState = false)
        {
            if (_eventQueue.TryDequeue(out ManualResetEventSlim? evt))
            {
                if (initialState)
                    evt.Set();
                else
                    evt.Reset();

                return evt;
            }
            else
            {
                return new ManualResetEventSlim(initialState);
            }
        }

        /// <summary>
        /// 回收 ManualResetEvent 对象到队列中，以便重用，减少对象创建和销毁的开销
        /// </summary>
        /// <param name="evt"></param>
        private static void RecycleEvent(ManualResetEventSlim evt)
        {
            _eventQueue.Enqueue(evt);
        }

        /// <summary>
        /// 发送消息获取窗口图标的核心方法，支持同步和异步回调
        /// </summary>
        /// <param name="hWnd">目标窗口句柄</param>
        /// <param name="iconSize">请求的图标尺寸（Small / Small2 / Big）</param>
        /// <param name="timeout">SendMessageTimeout 的超时时间（毫秒）</param>
        /// <param name="asyncCallback">
        ///     异步回调：当后台任务完成且调用方已离开同步等待路径时触发。
        ///     如果同步路径已拿到结果，该回调不会被调用。
        /// </param>
        /// <returns>
        ///     如果在 6ms 的同步等待窗口内拿到了结果，返回图标句柄；
        ///     如果超时未拿到，返回 <c>null</c>，调用方需通过异步回调获取结果。
        /// </returns>
        private static nint? SendGetWindowIconCore(
            nint hWnd,
            IconSize iconSize,
            int timeout,
            Action<nint> asyncCallback
        )
        {
            // 从对象池中获取一个 ManualResetEvent，用于同步等待（初始为未信号状态）
            var evt = ObtainEvent(false);
            // syncResult: 后台任务写入，同步路径读取——用于在 6ms 内传递结果
            nint syncResult = 0;
            // evtState 三态状态机:
            //   0 = 初始态，后台任务尚未设置事件 / 同步路径尚未放弃等待
            //   1 = 后台任务已设置事件（evt.Set() 完成）
            //   2 = 同步路径已放弃等待（6ms 超时），或已完成读取
            int evtState = 0;
            // state 三态状态机:
            //   0 = 初始态，同步路径仍在等待结果
            //   1 = 后台任务已完成 SendMessageTimeout 并写入了 syncResult
            //   2 = 同步路径已读取 syncResult 并返回（或已超时放弃）
            int state = 0;

            // 在后台线程池中向目标窗口发送 WM_GETICON 消息，
            // 避免因目标窗口无响应而阻塞当前线程。
            Task.Run(() =>
            {
                // --- 阶段1: 协调 evt 的生命周期 ---
                // 尝试将 evtState 从 0 改为 1，表示"我来负责发信号"。
                // 如果 CAS 失败，说明同步路径已经超时放弃等待了（evtState 已是 2），
                // 此时 evt 不再需要，直接回收。
                if (Interlocked.CompareExchange(ref evtState, 1, 0) != 0)
                {
                    RecycleEvent(evt);
                }
                else
                {
                    // CAS 成功，同步路径还在等待——发出信号唤醒它
                    evt.Set();
                }

                // --- 阶段2: 执行实际的 SendMessageTimeout 调用 ---
                // 向目标窗口发送自定义消息以请求其图标句柄。
                // AbortIfHung: 如果目标窗口无响应则立即中止
                // ErrorOnExit: 如果目标线程已退出则报错
                if (SendMessageTimeout(
                    hWnd,
                    WindowMessage.GetIcon,
                    (nint)iconSize, 0,
                    SendMessageTimeoutFlags.AbortIfHung | SendMessageTimeoutFlags.ErrorOnExit,
                    (uint)timeout,
                    out nint result
                ) == 0)
                {
                    // SendMessageTimeout 返回 0 表示失败（超时或无响应），
                    // 将 result 清零以确保不会返回无效的图标句柄
                    result = 0;
                }

                // 将结果写入共享变量，供同步路径读取
                syncResult = result;

                // --- 阶段3: 决定谁来回调 ---
                // 尝试将 state 从 0 改为 1，表示"后台任务已完成"。
                // 如果 CAS 成功（原值为 0），说明同步路径还在等，由同步路径负责返回结果。
                // 如果 CAS 失败（原值已是 2），说明同步路径已超时放弃，
                // 此时由后台任务负责触发异步回调。
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

            // --- 同步等待路径 ---
            // 等待后台任务完成 evt.Set()，最长等待 6ms。
            // 这是一个极短的等待窗口，目的是在大多数情况下（窗口响应快时）
            // 能够同步拿到结果，避免异步回调的复杂性。
            evt.Wait(6);

            // --- evt 生命周期协同 ---
            // 尝试将 evtState 从 0 改为 2，表示"同步路径已放弃等待"。
            // 如果 CAS 成功（原值为 0），说明后台任务还没执行到 evt.Set()，
            //   此时 evt 不在此处回收——后台任务稍后会通过 CAS 失败分支回收它。
            // 如果 CAS 失败（原值已是 1），说明后台任务已经 Set 过了，
            //   回收 evt，然后自旋等待后台任务将结果写入 syncResult。
            if (Interlocked.CompareExchange(ref evtState, 2, 0) != 0)
            {
                RecycleEvent(evt);

                // 自旋等待后台任务完成 SendMessageTimeout 并写入 syncResult。
                // 相比 Thread.Sleep(0)，SpinWait 不依赖线程调度器行为，
                // 能可靠地在极短时间内（通常几个 CPU 周期后）观察到 state 的变化，
                // 从而最大化快速路径的命中率。
                var spinWait = new SpinWait();
                while (Volatile.Read(ref state) == 0 && spinWait.Count < 1000)
                {
                    spinWait.SpinOnce();
                }
            }

            // --- 结果读取协同 ---
            // 尝试将 state 从 0 改为 2，表示"同步路径已读取结果"。
            // 如果 CAS 成功（原值为 0），说明后台任务还没把结果写入 syncResult，
            //   同步路径超时失败，返回 null，后续由后台任务触发异步回调。
            // 如果 CAS 失败（原值已是 1），说明后台任务已经写入了 syncResult，
            //   同步路径成功拿到结果并返回。
            if (Interlocked.CompareExchange(ref state, 2, 0) != 0)
            {
                return syncResult;
            }

            // 同步路径未能在 6ms 内拿到结果，返回 null 表示需要异步等待
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
