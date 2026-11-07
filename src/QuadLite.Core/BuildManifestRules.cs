using System;

namespace QuadLite.Core
{
    /// <summary>
    /// §5.5 / §6.4 `BUILD.txt` 字段齐备判据：编译时间 / 提交 / 工作树 /
    /// exe 大小 / sha256 / 嵌入资源 / 包自检结果；工作树脏照实写并附 Warning。
    /// 纯判据，零 I/O；真实出包与字段采集属发布线，未验证。
    /// </summary>
    public static class BuildManifestRules
    {
        public const int FieldCount = 7;
        public const string DirtyTreeWarning = "Warning";

        public static string[] Fields()
        {
            return new string[] {
                "compile-time", "commit", "worktree", "exe-size",
                "sha256", "embedded-resources", "package-selfcheck"
            };
        }

        private static bool Contains(string[] items, string value)
        {
            if (value == null) return false;
            for (int i = 0; i < items.Length; i++)
            {
                if (items[i] == value) return true;
            }
            return false;
        }

        public static bool IsComplete(string[] present)
        {
            if (present == null) return false;
            string[] fields = Fields();
            for (int i = 0; i < fields.Length; i++)
            {
                if (!Contains(present, fields[i])) return false;
            }
            return true;
        }

        public static string DirtyTreeVerdict(bool dirty)
        {
            return dirty ? "dirty + " + DirtyTreeWarning : "clean";
        }

        public static bool RequiresWarningWhenDirty()
        {
            return DirtyTreeVerdict(true).IndexOf(DirtyTreeWarning, StringComparison.Ordinal) >= 0;
        }

        public static bool CountsMatchDeclaration()
        {
            return Fields().Length == FieldCount &&
                   IsComplete(Fields()) &&
                   !IsComplete(new string[] { "commit", "sha256" });
        }
    }
}
