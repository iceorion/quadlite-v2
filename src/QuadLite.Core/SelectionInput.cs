using System;

namespace QuadLite.Core
{
    /// <summary>选择输入方式（§5.10 / FR-15）。</summary>
    public enum SelectGesture { Point, CtrlToggle, ShiftRange, SelectAll, Marquee }

    /// <summary>
    /// 选择与多选（§5.10 / FR-15）：点选、Ctrl / Shift 多选、Ctrl+A（不含组头 / 筛掉行 / 禁用项）；
    /// 图标档框选，详细信息档框选列为非目标。选中集只含筛后可见的真实条目。
    /// </summary>
    public static class SelectionInput
    {
        /// <summary>Ctrl+A 排除三类：组头、被筛掉的行、禁用项；复用 B1 的 IsSelectable。</summary>
        public static bool SelectAllIncludes(RowEntry row)
        {
            return SelectionRules.IsSelectable(row);
        }

        public static bool ExcludesThreeClasses()
        {
            PathKey k;
            PathKey.TryNormalize(@"D:\Data\a.txt", out k);
            RowEntry group = new RowEntry(null, true, false, false);
            RowEntry filtered = new RowEntry(k, false, true, false);
            RowEntry disabled = new RowEntry(k, false, false, true);
            return !SelectAllIncludes(group) && !SelectAllIncludes(filtered) &&
                   !SelectAllIncludes(disabled);
        }

        /// <summary>点选：只留一个索引；越界返回空集。</summary>
        public static int[] Point(int visibleCount, int index)
        {
            if (visibleCount <= 0 || index < 0 || index >= visibleCount) return new int[0];
            return new int[] { index };
        }

        /// <summary>Ctrl 多选：切换一个索引，返回新数组，不改入参。</summary>
        public static int[] CtrlToggle(int[] selected, int index, int visibleCount)
        {
            if (visibleCount <= 0 || index < 0 || index >= visibleCount) return CopyOrEmpty(selected);
            int[] src = CopyOrEmpty(selected);
            bool found = false;
            for (int i = 0; i < src.Length; i++)
            {
                if (src[i] == index) { found = true; break; }
            }
            int[] next = new int[found ? src.Length - 1 : src.Length + 1];
            int n = 0;
            for (int i = 0; i < src.Length; i++)
            {
                if (found && src[i] == index) continue;
                next[n++] = src[i];
            }
            if (!found) next[n] = index;
            return next;
        }

        /// <summary>Shift 多选：从锚点到 index 的闭区间，钳在筛后可见行范围内。</summary>
        public static int[] ShiftRange(int anchor, int index, int visibleCount)
        {
            if (visibleCount <= 0 || anchor < 0 || index < 0) return new int[0];
            if (anchor >= visibleCount || index >= visibleCount) return new int[0];
            int lo = anchor < index ? anchor : index;
            int hi = anchor < index ? index : anchor;
            int[] range = new int[hi - lo + 1];
            for (int i = 0; i < range.Length; i++) range[i] = lo + i;
            return range;
        }

        /// <summary>框选只在图标档可用；详细信息档框选列为非目标。</summary>
        public static bool MarqueeAllowed(ViewMode mode)
        {
            return ViewRules.IsIconMode(mode);
        }

        public static bool DetailsMarqueeIsNonTarget()
        {
            return !MarqueeAllowed(ViewMode.Details);
        }

        public static bool IsKnownGesture(SelectGesture gesture)
        {
            return gesture == SelectGesture.Point || gesture == SelectGesture.CtrlToggle ||
                   gesture == SelectGesture.ShiftRange || gesture == SelectGesture.SelectAll ||
                   gesture == SelectGesture.Marquee;
        }

        private static int[] CopyOrEmpty(int[] selected)
        {
            if (selected == null) return new int[0];
            return (int[])selected.Clone();
        }
    }
}
