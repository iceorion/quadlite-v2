using System;

namespace QuadLite.Core
{
    /// <summary>命中结果四类（§5.10 / FR-17）。</summary>
    public enum HitKind { Item, Blank, GroupHeader, ColumnHeader }

    /// <summary>
    /// 命中测试（§5.10 / FR-17）：单一 HitTest 供双击、拖入、右键共用；
    /// 图标档双击空白面与双击条目行为不同，由同一命中结果分流。
    /// </summary>
    public static class HitTestRules
    {
        public const int KindCount = 4;

        public static bool IsKnown(HitKind kind)
        {
            return kind == HitKind.Item || kind == HitKind.Blank ||
                   kind == HitKind.GroupHeader || kind == HitKind.ColumnHeader;
        }

        /// <summary>三个消费方拿到的必须是同一个命中结果。</summary>
        public static bool SharedBy(HitKind doubleClick, HitKind dragDrop, HitKind contextMenu)
        {
            return doubleClick == dragDrop && dragDrop == contextMenu;
        }

        /// <summary>双击行为分流：条目打开，图标档空白面清选，其余为空操作。</summary>
        public static int DoubleClickAction(ViewMode mode, HitKind hit)
        {
            if (hit == HitKind.Item) return 1;                                  // open item
            if (hit == HitKind.Blank && ViewRules.IsIconMode(mode)) return 2;  // clear selection
            return 0;                                                           // no action
        }

        public static bool BlankDiffersFromItem(ViewMode mode)
        {
            return DoubleClickAction(mode, HitKind.Blank) != DoubleClickAction(mode, HitKind.Item);
        }

        public static bool DetailsBlankIsNoAction()
        {
            return DoubleClickAction(ViewMode.Details, HitKind.Blank) == 0;
        }

        /// <summary>列头 / 组头不进条目命中，不能当作可搬运目标。</summary>
        public static bool IsItemTarget(HitKind kind)
        {
            return kind == HitKind.Item;
        }
    }
}
