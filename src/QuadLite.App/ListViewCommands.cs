using System;
using QuadLite.Core;

namespace QuadLite.App
{
    /// <summary>列表视图命令的结构化结果（§5.10）：只描述「做什么」，不执行任何 I/O。</summary>
    public sealed class ListViewCommandResult
    {
        private readonly bool accepted;
        private readonly KeyAction action;
        private readonly KeyScope scope;
        private readonly string reason;

        public ListViewCommandResult(bool accepted, KeyAction action, KeyScope scope, string reason)
        {
            this.accepted = accepted;
            this.action = action;
            this.scope = scope;
            this.reason = reason;
        }

        public bool Accepted { get { return accepted; } }
        public KeyAction Action { get { return action; } }
        public KeyScope Scope { get { return scope; } }
        public string Reason { get { return reason; } }
    }

    /// <summary>
    /// 快捷键派发（§5.10）：先解析全局唯一活动窗格的作用域，再按操作闸门忙时行为判定；
    /// 只读 Core 的 KeyMap，不碰焦点控件、不执行搬运。真实事件绑定尚未接入，
    /// 见 docs/adr/ADR-v2-006.md。
    /// </summary>
    public static class ListViewCommands
    {
        public static ListViewCommandResult Dispatch(KeyAction action, bool gateBusy,
                                                     int targetPaneId, int activePaneId)
        {
            if (!KeyMap.IsKnown(action))
                return new ListViewCommandResult(false, action, KeyScope.ActivePane, "UnknownAction");
            if (!NavRules.AppliesOnlyToActivePane(targetPaneId, activePaneId))
                return new ListViewCommandResult(false, action, KeyMap.ScopeOf(action), "WrongPane");
            if (gateBusy && !KeyMap.IsEnabledWhileBusy(action))
                return new ListViewCommandResult(false, action, KeyMap.ScopeOf(action), "Busy");
            return new ListViewCommandResult(true, action, KeyMap.ScopeOf(action), "Ok");
        }

        /// <summary>双击 / 拖入 / 右键共用同一命中结果，分流到不同动作码。</summary>
        public static int ResolveInteraction(ViewMode mode, HitKind hit, out string reason)
        {
            bool shared = HitTestRules.SharedBy(hit, hit, hit);
            if (!shared || !HitTestRules.IsKnown(hit))
            {
                reason = "InvalidHit";
                return 0;
            }
            reason = "Ok";
            return HitTestRules.DoubleClickAction(mode, hit);
        }
    }
}
