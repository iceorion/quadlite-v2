using System;

namespace QuadLite.Core
{
    /// <summary>排序字段与方向（§5.9 / FR-11）。</summary>
    public enum SortField { Name, Date, Type, Size }
    public enum SortDirection { Ascending, Descending }

    /// <summary>当前排序规格：不可变值对象，列头点击返回新规格而不是改入参。</summary>
    public struct SortSpec
    {
        private readonly SortField field;
        private readonly SortDirection direction;

        public SortSpec(SortField field, SortDirection direction)
        {
            this.field = field;
            this.direction = direction;
        }

        public SortField Field { get { return field; } }
        public SortDirection Direction { get { return direction; } }
    }

    /// <summary>
    /// 列头点击排序（§5.9 / FR-11）：同列点击切换升降序，换列回到新列升序；
    /// 名称列走自然排序比较器，其余列沿用同一比较器的方向翻转，不另写一套排序。
    /// </summary>
    public static class SortRules
    {
        public const int FieldCount = 4;

        public static SortSpec Default()
        {
            return new SortSpec(SortField.Name, SortDirection.Ascending);
        }

        public static bool IsKnownField(SortField field)
        {
            return field == SortField.Name || field == SortField.Date ||
                   field == SortField.Type || field == SortField.Size;
        }

        public static SortDirection Flip(SortDirection direction)
        {
            return direction == SortDirection.Ascending
                ? SortDirection.Descending : SortDirection.Ascending;
        }

        /// <summary>列头点击：同列翻方向，换列回到该列升序。</summary>
        public static SortSpec OnHeaderClick(SortSpec current, SortField clicked)
        {
            if (!IsKnownField(clicked)) return current;
            if (current.Field == clicked)
                return new SortSpec(clicked, Flip(current.Direction));
            return new SortSpec(clicked, SortDirection.Ascending);
        }

        /// <summary>只有名称列使用自然排序比较器。</summary>
        public static bool UsesNaturalComparer(SortField field)
        {
            return field == SortField.Name;
        }

        public static int CompareNames(string a, string b)
        {
            return NaturalComparer.Instance.Compare(a, b);
        }

        /// <summary>降序只翻转比较结果，比较器本身不变。</summary>
        public static int ApplyDirection(int comparison, SortDirection direction)
        {
            return direction == SortDirection.Descending ? -comparison : comparison;
        }

        public static int CompareNamesWithDirection(string a, string b, SortDirection direction)
        {
            return ApplyDirection(CompareNames(a, b), direction);
        }
    }
}
