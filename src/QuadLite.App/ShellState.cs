using System;
using QuadLite.Core;

namespace QuadLite.App
{
    /// <summary>
    /// L4 外壳状态（§4.1 / §5.8）：只读 Core 的布局 / 导航 / 会话结果，装配成表现层状态。
    /// 本批交付的是外壳状态内核：真实 WPF 窗口、XAML 控件与消息循环尚未接入，
    /// 偏差登记在 docs/adr/ADR-v2-005.md。本类型零 I/O，不引用呈现框架程序集。
    /// </summary>
    public sealed class ShellState
    {
        private readonly LayoutMode layout;
        private readonly int activePaneId;
        private readonly PaneState[] panes;
        private readonly double splitOffset;
        private readonly bool splitCollapsed;

        private ShellState(LayoutMode layout, int activePaneId, PaneState[] panes,
                           double splitOffset, bool splitCollapsed)
        {
            this.layout = layout;
            this.activePaneId = activePaneId;
            this.panes = panes;
            this.splitOffset = splitOffset;
            this.splitCollapsed = splitCollapsed;
        }

        public static ShellState Default()
        {
            return new ShellState(LayoutMode.Two, 0, PaneSet.Create(), 0.0, false);
        }

        public LayoutMode Layout { get { return layout; } }
        public int ActivePaneId { get { return activePaneId; } }
        public double SplitOffset { get { return splitOffset; } }
        public bool IsSplitCollapsed { get { return splitCollapsed; } }

        /// <summary>返回同一组窗格对象引用，便于 UI 复用视图而不重建行集。</summary>
        public PaneState[] Panes()
        {
            return panes;
        }

        public PaneSlot[] Slots()
        {
            return LayoutMath.Slots(layout);
        }

        public bool IsPaneVisible(int paneId)
        {
            return LayoutMath.IsPaneVisible(layout, paneId);
        }

        public int VisiblePaneCount()
        {
            return LayoutMath.PaneCount(layout);
        }

        /// <summary>布局切换不销毁窗格对象：新状态复用同一数组引用。</summary>
        public ShellState WithLayout(LayoutMode mode)
        {
            if (!LayoutMath.IsKnownMode((int)mode)) return this;
            PaneState[] kept = PaneSet.SwitchLayout(panes, layout, mode);
            if (kept == null) return this;
            int active = LayoutMath.ResolveActivePane(mode, activePaneId);
            return new ShellState(mode, active, kept, splitOffset, splitCollapsed);
        }

        public ShellState WithActivePane(int paneId)
        {
            int active = LayoutMath.ResolveActivePane(layout, paneId);
            return new ShellState(layout, active, panes, splitOffset, splitCollapsed);
        }

        /// <summary>分隔条只改比例 / 折叠标记，不碰窗格对象，也不另存可见性布尔。</summary>
        public ShellState WithSplit(double offset, bool collapsed)
        {
            if (!SplitMath.IsOffsetValid(offset)) return this;
            double ratio = collapsed ? SplitMath.CollapseTo(true) : SplitMath.RatioOf(offset);
            bool nowCollapsed = SplitMath.IsCollapsed(ratio);
            return new ShellState(layout, activePaneId, panes, offset, nowCollapsed);
        }

        public PaneState ActivePane()
        {
            int id = LayoutMath.ResolveActivePane(layout, activePaneId);
            return panes[id];
        }
    }
}
