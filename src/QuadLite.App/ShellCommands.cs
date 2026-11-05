using System;
using QuadLite.Core;

namespace QuadLite.App
{
    public enum ShellCommandKind { SwitchLayout, SetActivePane, CollapseSplit, RestoreSplit }

    public sealed class ShellCommandResult
    {
        private readonly bool applied;
        private readonly string reason;
        private readonly ShellState state;

        public ShellCommandResult(bool applied, string reason, ShellState state)
        {
            this.applied = applied;
            this.reason = reason;
            this.state = state;
        }

        public bool Applied { get { return applied; } }
        public string Reason { get { return reason; } }
        public ShellState State { get { return state; } }
    }

    /// <summary>
    /// 外壳命令（§5.8）：每条命令返回新 ShellState，不改入参；命令只解析全局唯一活动窗格，
    /// 不由当前焦点控件推断。真实 WPF 事件绑定未接入，见 docs/adr/ADR-v2-005.md。
    /// </summary>
    public static class ShellCommands
    {
        public static bool IsKnown(ShellCommandKind kind)
        {
            return kind == ShellCommandKind.SwitchLayout ||
                   kind == ShellCommandKind.SetActivePane ||
                   kind == ShellCommandKind.CollapseSplit ||
                   kind == ShellCommandKind.RestoreSplit;
        }

        public static ShellCommandResult Apply(ShellState state, ShellCommandKind kind, int arg)
        {
            if (state == null) return new ShellCommandResult(false, "NoState", null);
            if (!IsKnown(kind)) return new ShellCommandResult(false, "UnknownCommand", state);

            if (kind == ShellCommandKind.SwitchLayout)
            {
                if (!LayoutMath.IsKnownMode(arg))
                {
                    return new ShellCommandResult(false, "BadLayout", state);
                }
                return new ShellCommandResult(true, "Ok", state.WithLayout((LayoutMode)arg));
            }

            if (kind == ShellCommandKind.SetActivePane)
            {
                if (arg < 0 || arg >= state.VisiblePaneCount())
                {
                    return new ShellCommandResult(false, "BadPane", state);
                }
                return new ShellCommandResult(true, "Ok", state.WithActivePane(arg));
            }

            if (kind == ShellCommandKind.CollapseSplit)
            {
                return new ShellCommandResult(true, "Ok", state.WithSplit(state.SplitOffset, true));
            }

            // RestoreSplit：折叠态只在比例上表现为 0/1，相对均分偏移一直留在会话状态里，
            // 因此恢复直接使用它，不拿折叠态的 0/1 反推（SplitMath.Restore 由 runner 覆盖）。
            return new ShellCommandResult(true, "Ok", state.WithSplit(state.SplitOffset, false));
        }

        public static bool KeepsPaneObjects(ShellState before, ShellState after)
        {
            if (before == null || after == null) return false;
            return PaneSet.LayoutKeepsObjects(before.Panes(), after.Panes());
        }
    }
}
