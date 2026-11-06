using System;

namespace QuadLite.Core
{
    /// <summary>列头筛选的四列与大小五档（§5.9 / FR-13）。</summary>
    public enum FilterColumn { Name, Date, Type, Size }
    public enum SizeBucket { Empty, Tiny, Small, Medium, Large }

    /// <summary>
    /// 列头筛选（§5.9 / FR-13）：名称按拼音分桶、修改日期按月历、类型动态枚举、大小五档；
    /// 同列「或」、跨列「与」。结果写入显示行集；状态栏计数、Ctrl+A、拖放源、文件操作源
    /// 一律读显示行集，不读全量表。箭头可见性按全量表判断。导航即清除、F5 保留、不写会话。
    /// </summary>
    public static class FilterRules
    {
        public const int ColumnCount = 4;
        public const int BucketCount = 5;

        public static bool IsKnownColumn(FilterColumn column)
        {
            return column == FilterColumn.Name || column == FilterColumn.Date ||
                   column == FilterColumn.Type || column == FilterColumn.Size;
        }

        /// <summary>同列多选是「或」：命中该列任一已选值即通过。</summary>
        public static bool SameColumnIsOr(int rowValue, int[] selectedValues)
        {
            if (selectedValues == null || selectedValues.Length == 0) return true;
            for (int i = 0; i < selectedValues.Length; i++)
            {
                if (selectedValues[i] == rowValue) return true;
            }
            return false;
        }

        /// <summary>跨列是「与」：四列全部通过才进显示行集。</summary>
        public static bool CrossColumnIsAnd(bool nameOk, bool dateOk, bool typeOk, bool sizeOk)
        {
            return nameOk && dateOk && typeOk && sizeOk;
        }

        /// <summary>大小五档分桶（单位字节）；负数归 0 档。</summary>
        public static SizeBucket BucketOf(long bytes)
        {
            if (bytes <= 0) return SizeBucket.Empty;
            if (bytes < 1024L * 1024L) return SizeBucket.Tiny;
            if (bytes < 16L * 1024L * 1024L) return SizeBucket.Small;
            if (bytes < 1024L * 1024L * 1024L) return SizeBucket.Medium;
            return SizeBucket.Large;
        }

        public static string ColumnKey(FilterColumn column, int bucket)
        {
            return ((int)column).ToString() + ":" + bucket.ToString();
        }

        /// <summary>键在同一列内用「或」合并，跨列用「与」合并；键格式 列:桶。</summary>
        public static bool Matches(string[] rowKeys, string[][] selectedByColumn)
        {
            if (rowKeys == null || selectedByColumn == null) return false;
            if (rowKeys.Length != ColumnCount || selectedByColumn.Length != ColumnCount) return false;
            for (int c = 0; c < ColumnCount; c++)
            {
                string[] selected = selectedByColumn[c];
                if (selected == null || selected.Length == 0) continue;
                bool hit = false;
                for (int s = 0; s < selected.Length; s++)
                {
                    if (selected[s] == rowKeys[c]) { hit = true; break; }
                }
                if (!hit) return false;
            }
            return true;
        }

        /// <summary>导航即清除筛选：复用 NavRules 的同一判据，不另写一份。</summary>
        public static bool ClearsOn(NavAction action)
        {
            return NavRules.ClearsFilterOn(action);
        }

        /// <summary>F5 原地刷新保留筛选。</summary>
        public static bool KeepsOn(NavAction action)
        {
            return !NavRules.ClearsFilterOn(action);
        }

        public static bool PersistsToSession()
        {
            return false;
        }

        /// <summary>箭头可见性按全量表判断，不按筛后表判断（筛空时箭头仍在）。</summary>
        public static bool ArrowVisible(int fullTableCount)
        {
            return fullTableCount > 0;
        }

        /// <summary>四个消费方全部指向显示行集：计数相等即一致，不读全量表。</summary>
        public static bool SourcesAllUseDisplayRowSet(int displayedCount, int statusCount,
                                                      int selectAllCount, int dragSourceCount,
                                                      int operationSourceCount)
        {
            if (displayedCount < 0) return false;
            return statusCount == displayedCount && selectAllCount == displayedCount &&
                   dragSourceCount == displayedCount && operationSourceCount == displayedCount;
        }

        public static int SourceCount()
        {
            return 4;
        }
    }
}
