using System;
using System.Collections.Generic;

namespace QuadLite.Core
{
    /// <summary>
    /// 全局单一订阅 + 去抖：同一路径只订阅一次，一次批量操作合并成一次广播。
    /// 不按窗格各起一个 watcher，避免刷新风暴与选中集丢失。
    /// </summary>
    public sealed class WatchThrottle
    {
        public const long DebounceTicks = 2000000L;  // 200ms

        private readonly Dictionary<string, long> lastSeen = new Dictionary<string, long>();
        private int broadcastCount;
        private int subscriptionCount;

        public int BroadcastCount { get { return broadcastCount; } }
        public int SubscriptionCount { get { return subscriptionCount; } }

        public bool Register(string path, long nowTicks)
        {
            if (string.IsNullOrEmpty(path)) return false;
            string key = path.ToUpperInvariant();
            if (!lastSeen.ContainsKey(key))
            {
                subscriptionCount++;
                lastSeen[key] = 0;
                return true;
            }
            return false;
        }

        public bool Unregister(string path)
        {
            if (string.IsNullOrEmpty(path)) return false;
            string key = path.ToUpperInvariant();
            if (!lastSeen.ContainsKey(key)) return false;
            lastSeen.Remove(key);
            subscriptionCount--;
            return true;
        }

        /// <summary>同路径在去抖窗内只放行一次；不同路径不互相吞掉。</summary>
        public bool ShouldBroadcast(FsEvent evt)
        {
            if (evt == null || evt.Path == null) return false;
            string key = evt.Path.Value.ToUpperInvariant();
            long prev = lastSeen.ContainsKey(key) ? lastSeen[key] : 0;
            if (prev > 0 && evt.AtTicks - prev <= DebounceTicks) return false;
            lastSeen[key] = evt.AtTicks;
            return true;
        }

        public void RecordBroadcast()
        {
            broadcastCount++;
        }

        public static bool IsSingleWatcher(int watcherCount)
        {
            return watcherCount == 1;
        }
    }
}
