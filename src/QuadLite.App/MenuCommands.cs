using System;
using QuadLite.Core;

namespace QuadLite.App
{
    /// <summary>选中 ID 的派发结果（结构化，UI 只消费它）。</summary>
    public enum MenuCommandOutcome { None, InvokeSystem, DispatchOwn, Dropped, ShowHint }

    /// <summary>
    /// B5 菜单命令内核（L4，零 I/O）：把选中 ID / 关闭事件 / 哨兵动作解析成结构化结果。
    /// 不调用 shell、不弹菜单、不写 data 目录；真实 NativeMenuHost 未接入。
    /// </summary>
    public static class MenuCommands
    {
        public static MenuCommandOutcome Dispatch(int selectedId, int maxId, MenuHostMode host)
        {
            if (host == MenuHostMode.Fallback) return MenuCommandOutcome.ShowHint;
            MenuDispatchKind kind = MenuInvokeRules.Classify(selectedId, maxId);
            if (kind == MenuDispatchKind.Dropped) return MenuCommandOutcome.Dropped;
            if (kind == MenuDispatchKind.Own) return MenuCommandOutcome.DispatchOwn;
            return MenuCommandOutcome.InvokeSystem;
        }

        /// <summary>系统项派发时把 ID 转成校验过的偏移量，越界不做任何调用。</summary>
        public static int SystemInvokeOffset(int selectedId, int maxId)
        {
            if (!MenuInvokeRules.InvokeIdInRange(selectedId, maxId)) return -1;
            return MenuInvokeRules.InvokeOffset(selectedId);
        }

        public static int OwnId(int selectedId)
        {
            return MenuModel.IsOwnId(selectedId) ? selectedId : -1;
        }

        /// <summary>关闭事件映射到会话状态；正常关闭清 pending，崩溃残留。</summary>
        public static MenuSessionState CloseState(MenuCloseKind kind)
        {
            return kind == MenuCloseKind.Crashed
                ? MenuSessionState.Crashed : MenuSessionState.Closed;
        }

        public static bool PendingCleared(MenuCloseKind kind)
        {
            return MenuSentinelRules.ClearsPendingOnNormalClose(kind);
        }

        public static SentinelAction OnStartup(bool pendingRemains)
        {
            return MenuSentinelRules.OnStartup(pendingRemains);
        }

        public static MenuHostMode HostAfterAction(SentinelAction action, MenuHostMode current)
        {
            if (action == SentinelAction.Degrade) return MenuHostMode.Fallback;
            if (action == SentinelAction.Retry) return MenuSentinelRules.RetryHostMode();
            return current;
        }

        public static int TtlAfterAction(SentinelAction action, int currentDays)
        {
            if (action == SentinelAction.ExtendTtl)
                return MenuSentinelRules.ExtendedTtlDays(currentDays);
            return currentDays;
        }

        /// <summary>菜单期间需要转发的消息集合，供宿主接线时逐条对照。</summary>
        public static MenuMessage[] MessagesToForward()
        {
            MenuMessage[] all = new MenuMessage[MenuMessageRules.MessageCount];
            for (int i = 0; i < MenuMessageRules.MessageCount; i++)
            {
                all[i] = (MenuMessage)i;
            }
            return all;
        }

        public static bool UsesCapturedSnapshot(MenuTarget captured, MenuTarget dispatchTarget)
        {
            return MenuTargetSnapshotRules.DispatchUsesSameSnapshot(captured, dispatchTarget);
        }
    }
}
