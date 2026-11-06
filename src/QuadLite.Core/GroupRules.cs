using System;

namespace QuadLite.Core
{
    /// <summary>
    /// 分组（§5.9 / FR-11）：分组先比组序、组内走原比较器，不做二次排序；
    /// 组头只存在于显示列表，不进显示行集；图标档不渲染组头。
    /// </summary>
    public static class GroupRules
    {
        /// <summary>组头永不进显示行集，因此不参与选中集、计数、拖放源与操作源。</summary>
        public static bool HeaderEntersRowSet()
        {
            return false;
        }

        public static bool HeaderAppearsInDisplayList()
        {
            return true;
        }

        public static bool RendersHeader(ViewMode mode)
        {
            return ViewRules.RendersGroupHeader(mode);
        }

        /// <summary>分组视图只在详细信息档渲染组头；图标档一律不渲染。</summary>
        public static bool IconModeRendersHeader(ViewMode mode)
        {
            return ViewRules.IsIconMode(mode) && RendersHeader(mode);
        }

        /// <summary>两级比较：组序不同直接定序；组序相同返回原比较器结果，不重新排序。</summary>
        public static int Compare(int groupOrderA, int groupOrderB, int innerComparison)
        {
            if (groupOrderA != groupOrderB)
                return groupOrderA < groupOrderB ? -1 : 1;
            return innerComparison;
        }

        /// <summary>组内使用同一个比较器：传入的比较结果原样返回即为不二次排序。</summary>
        public static bool NoSecondSort(int innerComparison)
        {
            return Compare(1, 1, innerComparison) == innerComparison;
        }

        public static bool GroupHeaderIsNotSelectable()
        {
            return !HeaderEntersRowSet();
        }

        /// <summary>显示列表 = 显示行集 + 组头；组头数不污染行集计数。</summary>
        public static int DisplayListCount(int rowCount, int headerCount)
        {
            if (rowCount < 0 || headerCount < 0) return 0;
            return rowCount + headerCount;
        }
    }
}
