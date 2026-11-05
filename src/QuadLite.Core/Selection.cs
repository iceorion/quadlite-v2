using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace QuadLite.Core
{
    public sealed class RowEntry
    {
        private readonly PathKey key;
        private readonly bool isGroupHeader;
        private readonly bool isFilteredOut;
        private readonly bool isDisabled;

        public RowEntry(PathKey key, bool isGroupHeader, bool isFilteredOut, bool isDisabled)
        {
            this.key = key;
            this.isGroupHeader = isGroupHeader;
            this.isFilteredOut = isFilteredOut;
            this.isDisabled = isDisabled;
        }

        public PathKey Key { get { return key; } }
        public bool IsGroupHeader { get { return isGroupHeader; } }
        public bool IsFilteredOut { get { return isFilteredOut; } }
        public bool IsDisabled { get { return isDisabled; } }
    }

    /// <summary>筛后显示行集：状态栏计数、Ctrl+A、拖放源、文件操作源一律读它。</summary>
    public sealed class Selection
    {
        private readonly IReadOnlyList<PathKey> visibleSelected;
        private readonly int visibleRowCount;

        public Selection(IList<PathKey> visibleSelected, int visibleRowCount)
        {
            List<PathKey> copy = new List<PathKey>();
            if (visibleSelected != null)
            {
                for (int i = 0; i < visibleSelected.Count; i++) copy.Add(visibleSelected[i]);
            }
            this.visibleSelected = new ReadOnlyCollection<PathKey>(copy);
            this.visibleRowCount = visibleRowCount;
        }

        public IReadOnlyList<PathKey> VisibleSelected { get { return visibleSelected; } }
        public int VisibleRowCount { get { return visibleRowCount; } }
    }

    public static class SelectionRules
    {
        /// <summary>组头、被筛掉的行、禁用项均不计入选中集。</summary>
        public static bool IsSelectable(RowEntry row)
        {
            return row != null && row.Key != null && !row.IsGroupHeader &&
                   !row.IsFilteredOut && !row.IsDisabled;
        }

        public static Selection SelectAll(IReadOnlyList<RowEntry> rows)
        {
            List<PathKey> picked = new List<PathKey>();
            int visibleRows = 0;
            if (rows != null)
            {
                for (int i = 0; i < rows.Count; i++)
                {
                    RowEntry row = rows[i];
                    if (row == null || row.IsGroupHeader || row.IsFilteredOut) continue;
                    visibleRows++;  // 禁用项仍是一个显示行，但不进选中集
                    if (IsSelectable(row)) picked.Add(row.Key);
                }
            }
            return new Selection(picked, visibleRows);
        }

        public static int StatusBarCount(Selection selection)
        {
            return selection == null ? 0 : selection.VisibleRowCount;
        }

        public static bool CanEnterOperation(Selection selection)
        {
            return selection != null && selection.VisibleSelected.Count > 0;
        }
    }
}
