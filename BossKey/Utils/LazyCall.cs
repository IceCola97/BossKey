using System;
using System.Collections.Generic;
using System.Threading;
using Timer = System.Threading.Timer;

namespace BossKey.Utils
{
    internal static class LazyCall
    {
        // 节流：记录每个 site 的最后执行时间
        private static readonly Dictionary<string, DateTime> _throttleLastTimes = new();
        private static readonly object _throttleLock = new();

        // 防抖：为每个 site 存储一个定时器
        private static readonly Dictionary<string, Timer> _debounceTimers = new();
        private static readonly object _debounceLock = new();

        /// <summary>
        /// 节流方法：在指定时间（ticks 毫秒）内只执行一次。
        /// 如果当前 site 的冷却期未过，则直接返回不做任何操作；
        /// 否则立即执行 action 并开始新的冷却计时。
        /// </summary>
        /// <param name="site">标识调用来源的字符串，相同 site 共享冷却计时</param>
        /// <param name="ticks">冷却时间，单位毫秒</param>
        /// <param name="action">要执行的操作</param>
        public static void Throttle(string site, int ticks, Action action)
        {
            var now = DateTime.Now;

            lock (_throttleLock)
            {
                if (_throttleLastTimes.TryGetValue(site, out var lastTime) &&
                    (now - lastTime).TotalMilliseconds < ticks)
                {
                    return; // 冷却期内，忽略此次调用
                }
                _throttleLastTimes[site] = now;
            }

            action();
        }

        /// <summary>
        /// 防抖方法：延迟执行 action，如果在等待期间再次调用相同 site，
        /// 则取消前一次调用，重新按新的 ticks 开始计时，最终只执行最后一次 action。
        /// </summary>
        /// <param name="site">标识调用来源的字符串，相同 site 共享防抖计时</param>
        /// <param name="ticks">延迟时间，单位毫秒</param>
        /// <param name="action">要执行的操作</param>
        public static void Debounce(string site, int ticks, Action action)
        {
            lock (_debounceLock)
            {
                // 取消上一次的定时器
                if (_debounceTimers.TryGetValue(site, out var existingTimer))
                {
                    existingTimer.Dispose();
                }

                // 创建新的一次性定时器
                Timer? timer = null;
                timer = new Timer(_ =>
                {
                    try
                    {
                        action();
                    }
                    finally
                    {
                        lock (_debounceLock)
                        {
                            _debounceTimers.Remove(site);
                            timer?.Dispose();
                        }
                    }
                }, null, ticks, Timeout.Infinite);

                _debounceTimers[site] = timer;
            }
        }

        /// <summary>
        /// 取消指定 site 的节流计时器，使其立即可以再次执行 action。
        /// </summary>
        /// <param name="site"></param>
        public static void ResetThrottle(string site)
        {
            lock (_throttleLock)
            {
                _throttleLastTimes.Remove(site);
            }
        }

        /// <summary>
        /// 取消指定 site 的防抖计时器，如果存在的话。
        /// </summary>
        /// <param name="site"></param>
        public static void CancelDebounce(string site)
        {
            lock (_debounceLock)
            {
                if (_debounceTimers.TryGetValue(site, out var existingTimer))
                {
                    existingTimer.Dispose();
                    _debounceTimers.Remove(site);
                }
            }
        }
    }
}
