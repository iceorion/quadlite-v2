using System;

namespace QuadLite.Core
{
    /// <summary>
    /// 窗格状态容器（§5.8）：每窗格独立持有标签组、导航历史、显示行集、排序 / 分组状态、
    /// 枚举令牌、滚动锚点、列宽偏好、筛选条件与视图状态。布局切换只改变落位与可见性，
    /// 不销毁 PaneState 对象；本类型为纯内存状态，零 I/O。
    /// </summary>
    public sealed class PaneState
    {
        private readonly int id;
        private PathKey path;
        private int generation;
        private int scrollAnchor;
        private int historyCount;
        private ViewMode view;
        private int[] colPrefs;
        private bool filtered;
        private PaneLoadState loadState;

        public PaneState(int id)
        {
            this.id = id;
            this.path = null;
            this.generation = 1;
            this.scrollAnchor = 0;
            this.historyCount = 0;
            this.view = ViewMode.Details;
            this.colPrefs = new int[] { 0, 0, 0, 0 };
            this.filtered = false;
            this.loadState = PaneLoadState.Idle;
        }

        public int Id { get { return id; } }
        public PathKey Path { get { return path; } }
        public int Generation { get { return generation; } }
        public int ScrollAnchor { get { return scrollAnchor; } }
        public int HistoryCount { get { return historyCount; } }
        public ViewMode View { get { return view; } }
        public PaneLoadState LoadState { get { return loadState; } }
        public bool IsFiltered { get { return filtered; } }

        public int[] ColumnPrefs()
        {
            return (int[])colPrefs.Clone();
        }

        public void SetPath(PathKey newPath, int newGeneration)
        {
            path = newPath;
            generation = newGeneration;
            filtered = false;  // 导航即清除筛选
            loadState = PaneLoadState.Enumerating;
        }

        public void SetScrollAnchor(int anchor)
        {
            scrollAnchor = anchor;
        }

        public void SetHistoryCount(int count)
        {
            historyCount = count;
        }

        public void SetView(ViewMode mode)
        {
            view = mode;
        }

        public void SetColumnPrefs(int[] prefs)
        {
            if (prefs == null) return;
            colPrefs = (int[])prefs.Clone();
        }

        public void SetFiltered(bool value)
        {
            filtered = value;
        }

        /// <summary>F5 原地刷新：保视口与选中集，只推进 generation 并保留筛选。</summary>
        public void RefreshInPlace(int newGeneration)
        {
            generation = newGeneration;
            loadState = PaneLoadState.Enumerating;
        }

        public void SetLoadState(PaneLoadState state)
        {
            loadState = state;
        }
    }

    public static class PaneSet
    {
        /// <summary>固定 4 个窗格对象；四档布局都复用这 4 个引用。</summary>
        public static PaneState[] Create()
        {
            PaneState[] panes = new PaneState[LayoutMath.MaxPanes];
            for (int i = 0; i < panes.Length; i++) panes[i] = new PaneState(i);
            return panes;
        }

        public static bool IsComplete(PaneState[] panes)
        {
            if (panes == null || panes.Length != LayoutMath.MaxPanes) return false;
            for (int i = 0; i < panes.Length; i++)
            {
                if (panes[i] == null || panes[i].Id != i) return false;
            }
            return true;
        }

        /// <summary>布局切换不销毁窗格对象：前后数组逐项引用相同。</summary>
        public static bool LayoutKeepsObjects(PaneState[] before, PaneState[] after)
        {
            if (before == null || after == null) return false;
            if (before.Length != after.Length || before.Length != LayoutMath.MaxPanes) return false;
            for (int i = 0; i < before.Length; i++)
            {
                if (!object.ReferenceEquals(before[i], after[i])) return false;
            }
            return true;
        }

        public static PaneState Active(PaneState[] panes, int activePaneId)
        {
            if (!IsComplete(panes)) return null;
            int id = LayoutMath.ResolveActivePane(LayoutMode.Four, activePaneId);
            return panes[id];
        }

        /// <summary>切换布局：只改可见性推导，返回同一组对象引用。</summary>
        public static PaneState[] SwitchLayout(PaneState[] panes, LayoutMode from, LayoutMode to)
        {
            if (!IsComplete(panes)) return null;
            if (LayoutMath.PaneCount(from) == 0 || LayoutMath.PaneCount(to) == 0) return null;
            return panes;
        }
    }
}
