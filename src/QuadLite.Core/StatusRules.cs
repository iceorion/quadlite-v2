using System;

namespace QuadLite.Core
{
    /// <summary>
    /// 状态栏（§5.10 / FR-16）：按筛后可见行计数、显示选中数与操作结果；
    /// 文案一律来自 L1 的结构化结果，UI 不自行拼装业务结论。
    /// </summary>
    public static class StatusRules
    {
        /// <summary>筛后行数、选中数、操作结果三段，缺一不可。</summary>
        public static int FieldCount()
        {
            return 3;
        }

        public static int VisibleCount(Selection selection)
        {
            return SelectionRules.StatusBarCount(selection);
        }

        public static int SelectedCount(Selection selection)
        {
            return selection == null ? 0 : selection.VisibleSelected.Count;
        }

        /// <summary>只有结构化结果才允许产出状态文案；null 结果不出文案。</summary>
        public static bool CanRenderResult(object structuredResult)
        {
            return structuredResult != null;
        }

        public static string ResultText(string structuredText)
        {
            return structuredText == null ? "" : structuredText;
        }

        /// <summary>成功只认显式结构化标记；「没有异常」本身不构成成功。</summary>
        public static bool ClaimsSuccess(bool hasStructuredResult, bool structuredSuccess)
        {
            return hasStructuredResult && structuredSuccess;
        }

        /// <summary>不得由「没有异常」推出「操作成功」：无结构化结果时一律不宣称成功。</summary>
        public static bool RejectsInferredSuccess()
        {
            return !ClaimsSuccess(false, true);
        }

        /// <summary>状态栏计数必须等于筛后显示行集计数，不是全量表计数。</summary>
        public static bool CountMatchesDisplayedSet(int statusCount, int displayedCount)
        {
            return statusCount >= 0 && statusCount == displayedCount;
        }
    }
}
