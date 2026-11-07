using System;

namespace QuadLite.Core
{
    /// <summary>
    /// §7.1 门卡总表的可判定副本：9 个门卡组、50 个登记门卡、批次归属与孤立检查。
    /// 纯字符串表，零 I/O；`check-trace` 仍以 design-v2.md 为唯一权威，本表只供
    /// 自测装置生成明细行与静态契约门卡核对数量。
    /// </summary>
    public static class GateInventoryRules
    {
        public const int GroupCount = 9;
        public const int GateCount = 50;
        public const int BatchCount = 9;

        public static string[] Groups()
        {
            return new string[] {
                "菜单", "操作", "布局导航", "列表视图", "选择交互",
                "图标缩略图", "便携与命令行", "安全与状态", "通用"
            };
        }

        /// <summary>与 design-v2.md §7.1 第二列逐项对应，顺序一致。</summary>
        public static string[] Gates()
        {
            return new string[] {
                "menu-native-default", "menu-no-verb-scan", "menu-id-range",
                "menu-own-id-segment", "menu-no-fusion", "menu-msg-forward",
                "menu-target-snapshot", "menu-sentinel", "menu-single-file", "menu-release",
                "op-native-exec", "op-coordinator-global", "op-ui-no-system-modal",
                "op-result-structured", "op-undo-cross-pane", "op-drop-internal-vs-external",
                "op-fake-operator", "op-single-fswatch", "name-*", "refresh-*", "clipboard-*",
                "layout-*", "split-*", "tab-*", "path-*", "nav-*", "bookmark-*", "session-*",
                "enum-*", "view-*", "sort-*", "group-*", "cols-*", "filter-*", "toggle-*",
                "select-*", "hit-*", "status-*", "keys-*",
                "icon-*", "thumb-*", "thumb-nowrite",
                "portable-*", "cli-*", "single-instance-*",
                "safety-*", "state-*", "error-*",
                "check-boundaries", "check-trace"
            };
        }

        public static string[] BatchIds()
        {
            return new string[] { "B0", "B1", "B2", "B3", "B4", "B5", "B6", "B7", "B8" };
        }

        private static bool Contains(string[] items, string value)
        {
            if (value == null) return false;
            for (int i = 0; i < items.Length; i++)
            {
                if (items[i] == value) return true;
            }
            return false;
        }

        public static bool IsRegistered(string gate)
        {
            return Contains(Gates(), gate);
        }

        public static bool IsOrphan(string gate)
        {
            return !IsRegistered(gate);
        }

        /// <summary>门卡组 → 批次；未登记者返回空串（视为无主需求）。</summary>
        public static string BatchFor(string gate)
        {
            if (!IsRegistered(gate)) return "";
            if (gate.StartsWith("menu-", StringComparison.Ordinal)) return "B5";
            if (gate.StartsWith("icon-", StringComparison.Ordinal) ||
                gate.StartsWith("thumb", StringComparison.Ordinal)) return "B6";
            if (gate.StartsWith("layout-", StringComparison.Ordinal) ||
                gate.StartsWith("split-", StringComparison.Ordinal) ||
                gate.StartsWith("tab-", StringComparison.Ordinal) ||
                gate.StartsWith("bookmark-", StringComparison.Ordinal) ||
                gate.StartsWith("session-", StringComparison.Ordinal)) return "B3";
            if (gate.StartsWith("view-", StringComparison.Ordinal) ||
                gate.StartsWith("group-", StringComparison.Ordinal) ||
                gate.StartsWith("sort-", StringComparison.Ordinal) ||
                gate.StartsWith("filter-", StringComparison.Ordinal) ||
                gate.StartsWith("toggle-", StringComparison.Ordinal) ||
                gate.StartsWith("select-", StringComparison.Ordinal) ||
                gate.StartsWith("hit-", StringComparison.Ordinal) ||
                gate.StartsWith("status-", StringComparison.Ordinal) ||
                gate.StartsWith("keys-", StringComparison.Ordinal)) return "B4";
            if (gate.StartsWith("op-", StringComparison.Ordinal) ||
                gate.StartsWith("portable-", StringComparison.Ordinal) ||
                gate.StartsWith("cli-", StringComparison.Ordinal) ||
                gate.StartsWith("single-instance-", StringComparison.Ordinal)) return "B2/B7";
            if (gate == "check-boundaries" || gate == "check-trace") return "B0";
            return "B1";
        }

        public static bool CountsMatchDeclaration()
        {
            return Groups().Length == GroupCount &&
                   Gates().Length == GateCount &&
                   BatchIds().Length == BatchCount;
        }
    }
}
