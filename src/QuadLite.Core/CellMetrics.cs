using System;

namespace QuadLite.Core
{
    /// <summary>可见区间单位显式化，避免 Items / Pixels 混用。</summary>
    public enum SpanUnit { Items, Pixels }

    public struct VisibleSpan
    {
        private readonly SpanUnit unit;
        private readonly int start;
        private readonly int count;

        public VisibleSpan(SpanUnit unit, int start, int count)
        {
            this.unit = unit;
            this.start = start;
            this.count = count;
        }

        public SpanUnit Unit { get { return unit; } }
        public int Start { get { return start; } }
        public int Count { get { return count; } }
    }

    public static class CellMetrics
    {
        public const int Padding = 8;

        public static int CellSize(ViewMode mode)
        {
            ViewSpec s = ViewSpecs.For(mode);
            return s == null ? 0 : s.IconPx + Padding * 2;
        }

        /// <summary>单位不同的区间不可相交，返回 false 而不是悄悄换算。</summary>
        public static bool CanIntersect(VisibleSpan a, VisibleSpan b)
        {
            return a.Unit == b.Unit;
        }

        public static bool Within(VisibleSpan span, int total)
        {
            if (span.Start < 0 || span.Count < 0 || total < 0) return false;
            return span.Start + span.Count <= total;
        }

        /// <summary>只在 Pixels 单位下做几何换算；Items 单位返回 -1。</summary>
        public static int FirstVisibleIndex(VisibleSpan span, int cellPx, int viewportPx)
        {
            if (span.Unit != SpanUnit.Pixels || cellPx <= 0 || viewportPx <= 0) return -1;
            if (!Within(span, span.Start + viewportPx)) return -1;
            return span.Start / cellPx;
        }
    }
}
