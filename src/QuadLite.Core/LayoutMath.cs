using System;

namespace QuadLite.Core
{
    /// <summary>
    /// 四档布局（规格 v1.2 §5.8）：1/2/3/4 窗格的槽位与跨度唯一来源。
    /// 3x3 逻辑网格中 0/2 行列为内容区、1 行列为分隔条；本类型纯数值、零 I/O，
    /// 不引用任何呈现层类型。可自定义二叉分割树方案仍在讨论中、未拍板，本批按四档实现。
    /// </summary>
    public enum LayoutMode { One = 1, Two = 2, Three = 3, Four = 4 }

    /// <summary>一个窗格在 3x3 逻辑网格中的落位，不可变。</summary>
    public sealed class PaneSlot
    {
        private readonly int paneIndex;
        private readonly int row;
        private readonly int column;
        private readonly int rowSpan;
        private readonly int columnSpan;

        public PaneSlot(int paneIndex, int row, int column, int rowSpan, int columnSpan)
        {
            this.paneIndex = paneIndex;
            this.row = row;
            this.column = column;
            this.rowSpan = rowSpan;
            this.columnSpan = columnSpan;
        }

        public int PaneIndex { get { return paneIndex; } }
        public int Row { get { return row; } }
        public int Column { get { return column; } }
        public int RowSpan { get { return rowSpan; } }
        public int ColumnSpan { get { return columnSpan; } }
    }

    public static class LayoutMath
    {
        public const int MaxPanes = 4;
        public const int GridSize = 3;
        public const int ThreePaneMainSpan = 3;

        private static readonly PaneSlot[] One = new PaneSlot[]
        {
            new PaneSlot(0, 0, 0, 3, 3)
        };

        private static readonly PaneSlot[] Two = new PaneSlot[]
        {
            new PaneSlot(0, 0, 0, 3, 1),
            new PaneSlot(1, 0, 2, 3, 1)
        };

        // 3 窗格：左侧主窗格必须 RowSpan=3；右侧上下两格各占一行。
        // 若把主窗格改成 RowSpan=1，即退化为 4 窗格落位，IsValidSlotTable 判红。
        private static readonly PaneSlot[] Three = new PaneSlot[]
        {
            new PaneSlot(0, 0, 0, 3, 1),
            new PaneSlot(1, 0, 2, 1, 1),
            new PaneSlot(2, 2, 2, 1, 1)
        };

        private static readonly PaneSlot[] Four = new PaneSlot[]
        {
            new PaneSlot(0, 0, 0, 1, 1),
            new PaneSlot(1, 0, 2, 1, 1),
            new PaneSlot(2, 2, 0, 1, 1),
            new PaneSlot(3, 2, 2, 1, 1)
        };

        public static bool IsKnownMode(int raw)
        {
            return raw >= (int)LayoutMode.One && raw <= (int)LayoutMode.Four;
        }

        public static int PaneCount(LayoutMode mode)
        {
            if (!IsKnownMode((int)mode)) return 0;
            return (int)mode;
        }

        /// <summary>返回槽位表副本；调用方改不到内部静态表。</summary>
        public static PaneSlot[] Slots(LayoutMode mode)
        {
            PaneSlot[] table = TableOf(mode);
            if (table == null) return null;
            return (PaneSlot[])table.Clone();
        }

        private static PaneSlot[] TableOf(LayoutMode mode)
        {
            if (mode == LayoutMode.One) return One;
            if (mode == LayoutMode.Two) return Two;
            if (mode == LayoutMode.Three) return Three;
            if (mode == LayoutMode.Four) return Four;
            return null;
        }

        /// <summary>折叠 / 布局切换只改变落位与可见性，不销毁窗格对象。</summary>
        public static bool IsPaneVisible(LayoutMode mode, int paneIndex)
        {
            return paneIndex >= 0 && paneIndex < PaneCount(mode);
        }

        /// <summary>全局唯一活动窗格：只接受显式 paneId，不从当前焦点控件推断；越界回落 0。</summary>
        public static int ResolveActivePane(LayoutMode mode, int requestedPaneId)
        {
            if (requestedPaneId < 0 || requestedPaneId >= PaneCount(mode)) return 0;
            return requestedPaneId;
        }

        /// <summary>五档窗口钉点：0..4 对应 0 / 0.25 / 0.5 / 0.75 / 1，越界返回 -1。</summary>
        public static double AnchorFraction(int anchorStep)
        {
            if (anchorStep < 0 || anchorStep > 4) return -1.0;
            return anchorStep * 0.25;
        }

        /// <summary>
        /// 槽位表判据：数量等于档位、paneIndex 唯一且覆盖 0..n-1、跨度不越 3x3 网格、
        /// 3 窗格恰有一个主窗格 RowSpan=3 且其余为 1。
        /// </summary>
        public static bool IsValidSlotTable(PaneSlot[] slots, LayoutMode mode)
        {
            if (slots == null) return false;
            int count = PaneCount(mode);
            if (count == 0 || slots.Length != count) return false;

            bool[] seen = new bool[count];
            int mainSpans = 0;
            for (int i = 0; i < slots.Length; i++)
            {
                PaneSlot s = slots[i];
                if (s == null) return false;
                int id = s.PaneIndex;
                if (id < 0 || id >= count || seen[id]) return false;
                seen[id] = true;
                if (s.Row < 0 || s.Column < 0) return false;
                if (s.RowSpan < 1 || s.ColumnSpan < 1) return false;
                if (s.Row + s.RowSpan > GridSize) return false;
                if (s.Column + s.ColumnSpan > GridSize) return false;
                if (mode == LayoutMode.Three && s.RowSpan == ThreePaneMainSpan) mainSpans++;
            }
            for (int i = 0; i < count; i++)
            {
                if (!seen[i]) return false;
            }
            if (mode == LayoutMode.Three)
            {
                if (mainSpans != 1) return false;
                PaneSlot[] expected = Three;
                int shortCount = 0;
                for (int i = 0; i < slots.Length; i++)
                {
                    if (slots[i].RowSpan == 1) shortCount++;
                }
                if (shortCount != expected.Length - 1) return false;
            }
            return true;
        }
    }
}
