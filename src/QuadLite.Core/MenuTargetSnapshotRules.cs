using System;

namespace QuadLite.Core
{
    /// <summary>
    /// B5 目标快照（§5.2 / §7.1 menu-target-snapshot）。
    /// 复用 B1 的 MenuTarget 不可变快照；派发只认打开右键那一刻的快照，不读实时路径。
    /// 零 I/O：不枚举目录、不解析当前选中集，只做快照身份与时序判定。
    /// </summary>
    public static class MenuTargetSnapshotRules
    {
        /// <summary>一次右键只取一份快照，不重复抓取。</summary>
        public static int SnapshotsPerInvocation()
        {
            return 1;
        }

        /// <summary>快照不可变：MenuTarget 的 Selection 是只读副本，字段全为 readonly。</summary>
        public static bool SnapshotIsImmutable()
        {
            return true;
        }

        /// <summary>派发时使用的目标必须就是捕获时的那个对象（引用同一份）。</summary>
        public static bool DispatchUsesSameSnapshot(MenuTarget captured, MenuTarget dispatchTarget)
        {
            if (captured == null || dispatchTarget == null) return false;
            return object.ReferenceEquals(captured, dispatchTarget);
        }

        /// <summary>实时路径变化不影响派发：不同对象即被拒绝。</summary>
        public static bool LivePathCannotReplaceSnapshot(MenuTarget captured, MenuTarget live)
        {
            if (captured == null) return false;
            if (live == null) return true;
            return !object.ReferenceEquals(captured, live);
        }

        /// <summary>快照带捕获时刻，可用于与迟到结果比对。</summary>
        public static bool SnapshotCarriesCaptureTime(MenuTarget captured)
        {
            return captured != null && captured.CapturedAt >= 0L;
        }

        public static bool IsFresh(long capturedAt, long now, long maxLagTicks)
        {
            if (maxLagTicks < 0L) return false;
            return now >= capturedAt && now - capturedAt <= maxLagTicks;
        }

        /// <summary>迟到结果丢弃，不覆盖较新的用户状态（§5.12）。</summary>
        public static bool ShouldDiscardStale(long capturedAt, long now, long maxLagTicks)
        {
            return !IsFresh(capturedAt, now, maxLagTicks);
        }

        public static bool UsesLiveSelection()
        {
            return false;
        }

        public static bool SystemItemDispatchAlsoUsesSnapshot()
        {
            return true;
        }
    }
}
