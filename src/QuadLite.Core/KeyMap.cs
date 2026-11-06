using System;

namespace QuadLite.Core
{
    /// <summary>快捷键动作（§5.10 快捷键表）。</summary>
    public enum KeyAction
    {
        Copy, Cut, Paste, Undo, SelectAll, NewTab, NextTab, Rename, Refresh,
        Delete, PermanentDelete, NewFolder, Escape
    }

    /// <summary>作用范围。</summary>
    public enum KeyScope { ActivePane, Global, Context }

    /// <summary>操作闸门忙时的行为。</summary>
    public enum BusyBehavior { Enabled, DisabledWithHint, DisabledNoUndoStep }

    /// <summary>
    /// 快捷键契约（§5.10 / NFR-12）：核心操作全部键盘可达；表格是唯一来源，
    /// 逐键给出作用范围与忙时行为，UI 不另写一份判断。
    /// </summary>
    public static class KeyMap
    {
        public const int ActionCount = 13;

        public static bool IsKnown(KeyAction action)
        {
            int i = (int)action;
            return i >= 0 && i < ActionCount;
        }

        public static KeyScope ScopeOf(KeyAction action)
        {
            if (action == KeyAction.Undo) return KeyScope.Global;
            if (action == KeyAction.Escape) return KeyScope.Context;
            return KeyScope.ActivePane;
        }

        /// <summary>忙时：变更类禁用并提示；Ctrl+Z 禁用且不产生撤销步骤；只读类可用。</summary>
        public static BusyBehavior BusyBehaviorOf(KeyAction action)
        {
            if (action == KeyAction.Undo) return BusyBehavior.DisabledNoUndoStep;
            if (action == KeyAction.SelectAll || action == KeyAction.NewTab ||
                action == KeyAction.NextTab || action == KeyAction.Refresh ||
                action == KeyAction.Escape) return BusyBehavior.Enabled;
            return BusyBehavior.DisabledWithHint;
        }

        public static bool IsEnabledWhileBusy(KeyAction action)
        {
            return BusyBehaviorOf(action) == BusyBehavior.Enabled;
        }

        public static bool UndoCreatesNoStepWhileBusy(KeyAction action)
        {
            return action == KeyAction.Undo &&
                   BusyBehaviorOf(action) == BusyBehavior.DisabledNoUndoStep;
        }

        public static bool IsReadOnlyAction(KeyAction action)
        {
            return action == KeyAction.SelectAll || action == KeyAction.Refresh ||
                   action == KeyAction.NextTab || action == KeyAction.Escape ||
                   action == KeyAction.NewTab;
        }

        /// <summary>每个动作都有作用域与忙时行为，表不缺项。</summary>
        public static bool IsTableComplete()
        {
            for (int i = 0; i < ActionCount; i++)
            {
                KeyAction action = (KeyAction)i;
                if (!IsKnown(action)) return false;
                if (ScopeOf(action) == KeyScope.ActivePane &&
                    BusyBehaviorOf(action) == BusyBehavior.Enabled &&
                    !IsReadOnlyAction(action)) return false;
            }
            return true;
        }

        public static int EnabledWhileBusyCount()
        {
            int n = 0;
            for (int i = 0; i < ActionCount; i++)
            {
                if (IsEnabledWhileBusy((KeyAction)i)) n++;
            }
            return n;
        }
    }
}
