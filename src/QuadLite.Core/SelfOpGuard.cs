using System;
using System.Collections.Generic;

namespace QuadLite.Core
{
    /// <summary>
    /// 自操作抑制两段式：操作进行中 + 结束后 1 秒，且只忽略真正碰过的路径前缀。
    /// 时间由调用方注入，判定是纯函数，可 100% 单测；不吞掉另一窗格同时发生的外部改动。
    /// </summary>
    public sealed class SelfOpGuard
    {
        public const long TrailTicks = 10000000L;  // 1 秒，单位与 IClock 一致

        private readonly List<PathKey> touched = new List<PathKey>();
        private bool inOperation;
        private long endTicks;
        private bool hasEnded;

        public bool InOperation { get { return inOperation; } }
        public int TouchedCount { get { return touched.Count; } }

        public void Begin(PathKey root)
        {
            inOperation = true;
            hasEnded = false;
            endTicks = 0;
            touched.Clear();
            if (root != null) touched.Add(root);
        }

        public void Touch(PathKey path)
        {
            if (path == null) return;
            for (int i = 0; i < touched.Count; i++)
            {
                if (touched[i].Equals(path)) return;
            }
            touched.Add(path);
        }

        public void End(long nowTicks)
        {
            inOperation = false;
            endTicks = nowTicks;
            hasEnded = true;
        }

        public bool ShouldSuppress(PathKey path, long nowTicks)
        {
            if (path == null) return false;
            if (!inOperation && !(hasEnded && nowTicks - endTicks <= TrailTicks)) return false;
            for (int i = 0; i < touched.Count; i++)
            {
                if (path.Equals(touched[i]) || path.IsUnder(touched[i])) return true;
            }
            return false;
        }

        /// <summary>结束后超过尾窗即恢复放行，避免永久吞掉通知。</summary>
        public static bool IsTrailExpired(long endTicks, long nowTicks)
        {
            return hasEndedFor(endTicks) && nowTicks - endTicks > TrailTicks;
        }

        private static bool hasEndedFor(long endTicks)
        {
            return endTicks > 0;
        }
    }
}
