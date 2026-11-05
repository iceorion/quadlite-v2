using System;

namespace QuadLite.Core
{
    public enum OperationGateState { Idle, Prechecking, Running, Reporting, ReadOnly, Cancelled }
    public enum MenuSessionState { None, Pending, Tracking, Closed, Crashed }
    public enum PaneLoadState { Idle, Enumerating, Ready, Stale, Error }
    public enum ShellAvailabilityState { Probe, Available, ReadOnly }
    public enum JobState { Queued, Running, Done, Failed }

    /// <summary>每张状态表是唯一定义，UI 只消费状态，不自行推断。</summary>
    public static class StateMachine
    {
        public static bool CanTransition(OperationGateState from, OperationGateState to)
        {
            if (from == OperationGateState.Idle)
                return to == OperationGateState.Prechecking || to == OperationGateState.ReadOnly;
            if (from == OperationGateState.Prechecking)
                return to == OperationGateState.Running || to == OperationGateState.ReadOnly;
            if (from == OperationGateState.Running)
                return to == OperationGateState.Reporting || to == OperationGateState.Cancelled ||
                       to == OperationGateState.ReadOnly;
            if (from == OperationGateState.Reporting)
                return to == OperationGateState.Idle || to == OperationGateState.ReadOnly;
            if (from == OperationGateState.Cancelled)
                return to == OperationGateState.Reporting || to == OperationGateState.ReadOnly;
            if (from == OperationGateState.ReadOnly) return to == OperationGateState.Idle;
            return false;
        }

        public static bool CanTransition(MenuSessionState from, MenuSessionState to)
        {
            if (from == MenuSessionState.None) return to == MenuSessionState.Pending;
            if (from == MenuSessionState.Pending)
                return to == MenuSessionState.Tracking || to == MenuSessionState.Crashed;
            if (from == MenuSessionState.Tracking) return to == MenuSessionState.Closed;
            if (from == MenuSessionState.Closed) return to == MenuSessionState.None;
            if (from == MenuSessionState.Crashed) return to == MenuSessionState.Pending;
            return false;
        }

        public static bool CanTransition(PaneLoadState from, PaneLoadState to)
        {
            if (from == PaneLoadState.Idle)
                return to == PaneLoadState.Enumerating || to == PaneLoadState.Error;
            if (from == PaneLoadState.Enumerating)
                return to == PaneLoadState.Ready || to == PaneLoadState.Stale ||
                       to == PaneLoadState.Error;
            if (from == PaneLoadState.Ready)
                return to == PaneLoadState.Idle || to == PaneLoadState.Enumerating;
            if (from == PaneLoadState.Stale || from == PaneLoadState.Error)
                return to == PaneLoadState.Idle;
            return false;
        }

        public static bool CanTransition(ShellAvailabilityState from, ShellAvailabilityState to)
        {
            if (from == ShellAvailabilityState.Probe)
                return to == ShellAvailabilityState.Available || to == ShellAvailabilityState.ReadOnly;
            if (from == ShellAvailabilityState.ReadOnly) return to == ShellAvailabilityState.Probe;
            return false;
        }

        public static bool CanTransition(JobState from, JobState to)
        {
            if (from == JobState.Queued) return to == JobState.Running;
            if (from == JobState.Running) return to == JobState.Done || to == JobState.Failed;
            return false;  // Failed 进负缓存，不无限重试
        }

        /// <summary>忙时变更类入口置灰；ReadOnly 下同样禁用（C-06）。</summary>
        public static bool IsChangeBlocked(OperationGateState state)
        {
            return state != OperationGateState.Idle;
        }

        /// <summary>导航类只读入口在忙时仍可用，只读模式下也可浏览。</summary>
        public static bool IsNavigationAllowed(OperationGateState state)
        {
            return Enum.IsDefined(typeof(OperationGateState), state);
        }

        public static bool IsStaleDiscarded(PaneLoadState state)
        {
            return state == PaneLoadState.Stale;
        }

        /// <summary>五张状态表的枚举完备性：新增枚举值而不同步本表即判红。</summary>
        public static bool IsEnumComplete()
        {
            return Covered(new int[] { 0, 1, 2, 3, 4, 5 }, typeof(OperationGateState)) &&
                   Covered(new int[] { 0, 1, 2, 3, 4 }, typeof(MenuSessionState)) &&
                   Covered(new int[] { 0, 1, 2, 3, 4 }, typeof(PaneLoadState)) &&
                   Covered(new int[] { 0, 1, 2 }, typeof(ShellAvailabilityState)) &&
                   Covered(new int[] { 0, 1, 2, 3 }, typeof(JobState));
        }

        private static bool Covered(int[] declared, Type enumType)
        {
            Array values = Enum.GetValues(enumType);
            if (values.Length != declared.Length) return false;
            for (int i = 0; i < declared.Length; i++)
            {
                bool found = false;
                for (int j = 0; j < values.Length; j++)
                {
                    if (Convert.ToInt32(values.GetValue(j)) == declared[i]) { found = true; break; }
                }
                if (!found) return false;
            }
            return true;
        }
    }
}
