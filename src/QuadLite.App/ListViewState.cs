using System;
using QuadLite.Core;

namespace QuadLite.App
{
    /// <summary>
    /// L4 列表视图状态（§5.9 / §5.10）：只读 Core 的视图 / 排序 / 筛选 / 开关规则结果，
    /// 装配成表现层可消费的不可变状态。本批交付的是列表视图的可判定内核：
    /// 真实 WPF 列表控件、虚拟化面板、拖放与事件绑定尚未接入，偏差登记在
    /// docs/adr/ADR-v2-006.md。本类型零 I/O，不引用呈现框架程序集。
    /// </summary>
    public sealed class ListViewState
    {
        private readonly ViewMode view;
        private readonly SortSpec sort;
        private readonly ToggleState toggles;
        private readonly int displayedRows;
        private readonly int selectedRows;

        private ListViewState(ViewMode view, SortSpec sort, ToggleState toggles,
                              int displayedRows, int selectedRows)
        {
            this.view = view;
            this.sort = sort;
            this.toggles = toggles;
            this.displayedRows = displayedRows;
            this.selectedRows = selectedRows;
        }

        public static ListViewState Default()
        {
            return new ListViewState(ViewMode.Details, SortRules.Default(),
                                     ToggleRules.Default(), 0, 0);
        }

        public ViewMode View { get { return view; } }
        public SortSpec Sort { get { return sort; } }
        public int DisplayedRows { get { return displayedRows; } }
        public int SelectedRows { get { return selectedRows; } }

        public bool RendersGroupHeader()
        {
            return GroupRules.RendersHeader(view);
        }

        public bool NeedsThumbnail()
        {
            return ViewRules.NeedsThumbnail(view);
        }

        public bool MarqueeAllowed()
        {
            return SelectionInput.MarqueeAllowed(view);
        }

        public bool IsVisible(EntryVisibility visibility)
        {
            return ToggleRules.IsVisible(toggles, visibility);
        }

        public int StatusVisibleCount()
        {
            return displayedRows;
        }

        public bool StatusCountMatchesDisplayedSet(int statusCount)
        {
            return StatusRules.CountMatchesDisplayedSet(statusCount, displayedRows);
        }

        /// <summary>四来源一致性：状态栏 / Ctrl+A / 拖放源 / 操作源都等于显示行集。</summary>
        public bool SourcesAgree(int statusCount, int selectAllCount,
                                 int dragSourceCount, int operationSourceCount)
        {
            return FilterRules.SourcesAllUseDisplayRowSet(displayedRows, statusCount,
                                                          selectAllCount, dragSourceCount,
                                                          operationSourceCount);
        }

        public ListViewState WithView(ViewMode mode)
        {
            if (!ViewRules.IsKnownMode((int)mode)) return this;
            return new ListViewState(mode, sort, toggles, displayedRows, selectedRows);
        }

        public ListViewState WithSort(SortField field)
        {
            return new ListViewState(view, SortRules.OnHeaderClick(sort, field),
                                     toggles, displayedRows, selectedRows);
        }

        public ListViewState WithToggle(DisplayToggle toggle)
        {
            if (!ToggleRules.IsKnown(toggle)) return this;
            return new ListViewState(view, sort, ToggleRules.Flip(toggles, toggle),
                                     displayedRows, selectedRows);
        }

        public ListViewState WithRowSet(int displayed, int selected)
        {
            if (displayed < 0 || selected < 0 || selected > displayed) return this;
            return new ListViewState(view, sort, toggles, displayed, selected);
        }
    }
}
