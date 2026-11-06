using System;

namespace QuadLite.Core
{
    /// <summary>
    /// 列宽交互（§5.9 / FR-12）：ColumnLayout 为列宽唯一来源；四条边界可拖、双击复位；
    /// 偏好按标签一份，0 = 自动。本类型只产出偏好数组与判定，不计算几何宽度。
    /// </summary>
    public static class ColumnPrefRules
    {
        public const int Auto = 0;
        public const int BoundaryCount = 4;

        public static int[] DefaultPrefs()
        {
            return new int[ColumnLayout.ColumnCount];
        }

        public static bool IsAuto(int pref)
        {
            return pref == Auto;
        }

        public static bool IsValidPrefs(int[] prefs)
        {
            if (prefs == null || prefs.Length != ColumnLayout.ColumnCount) return false;
            for (int i = 0; i < prefs.Length; i++)
            {
                if (prefs[i] < 0) return false;
            }
            return true;
        }

        /// <summary>拖动改宽：只写被拖的那一列，返回新数组，不改入参；非法列 / 负宽拒绝。</summary>
        public static int[] Drag(int[] prefs, int columnIndex, int width)
        {
            if (!IsValidPrefs(prefs)) return null;
            if (columnIndex < 0 || columnIndex >= prefs.Length || width < 0) return null;
            int[] next = (int[])prefs.Clone();
            next[columnIndex] = width;
            return next;
        }

        /// <summary>双击复位：该列回到 0 = 自动。</summary>
        public static int[] ResetColumn(int[] prefs, int columnIndex)
        {
            return Drag(prefs, columnIndex, Auto);
        }

        /// <summary>四条可拖边界：列数 - 1 条内边界 + 1 条尾边界。</summary>
        public static int DraggableBoundaries()
        {
            return BoundaryCount;
        }

        /// <summary>偏好按标签一份：不同标签拿到不同数组引用，互不串改。</summary>
        public static bool PrefsPerTab(int[] forTabA, int[] forTabB)
        {
            if (forTabA == null || forTabB == null) return false;
            return !object.ReferenceEquals(forTabA, forTabB);
        }

        public static int[] CloneForTab(int[] prefs)
        {
            if (!IsValidPrefs(prefs)) return null;
            return (int[])prefs.Clone();
        }

        /// <summary>几何宽度只从 ColumnLayout.TryFor 出，本规则层不另算宽度。</summary>
        public static bool WidthsComeFromSingleSource(int rowWidth, int[] prefs)
        {
            int[] widths;
            if (!ColumnLayout.TryFor(rowWidth, prefs, out widths)) return false;
            return ColumnLayout.IsValid(rowWidth, widths);
        }

        /// <summary>恒等式：Σ列宽 = 行宽 − Gutter。</summary>
        public static int ExpectedSum(int rowWidth)
        {
            return rowWidth - ColumnLayout.Gutter;
        }
    }
}
