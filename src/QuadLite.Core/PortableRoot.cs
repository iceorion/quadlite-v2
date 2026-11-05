using System;

namespace QuadLite.Core
{
    /// <summary>
    /// 配置根唯一：只认程序同级的 data\ 目录；不可写时明示只读降级。
    /// 这里只做字符串判定，实际读写与权限探测由 Fs 层承担。
    /// </summary>
    public static class PortableRoot
    {
        public const string ConfigDirectoryName = "data";

        public static bool IsAllowedRelative(string leaf)
        {
            if (string.IsNullOrEmpty(leaf)) return false;
            if (leaf.StartsWith("\\", StringComparison.Ordinal) ||
                leaf.StartsWith("/", StringComparison.Ordinal)) return false;
            if (leaf.IndexOf(":", StringComparison.Ordinal) >= 0) return false;
            if (leaf.IndexOf("..", StringComparison.Ordinal) >= 0) return false;
            if (leaf.IndexOf("\\", StringComparison.Ordinal) >= 0) return false;
            if (leaf.IndexOf("/", StringComparison.Ordinal) >= 0) return false;
            if (!NameRules.IsValidFileName(leaf)) return false;
            return true;
        }

        public static string Resolve(string programDirectory, string leaf)
        {
            if (string.IsNullOrEmpty(programDirectory)) return "";
            if (!IsAllowedRelative(leaf)) return "";
            string sep = programDirectory.EndsWith("\\", StringComparison.Ordinal) ? "" : "\\";
            return programDirectory + sep + ConfigDirectoryName + "\\" + leaf;
        }

        public static bool IsUnderConfigRoot(string fullPath, string programDirectory)
        {
            if (string.IsNullOrEmpty(fullPath) || string.IsNullOrEmpty(programDirectory)) return false;
            string prefix = programDirectory + "\\" + ConfigDirectoryName + "\\";
            return fullPath.StartsWith(prefix, StringComparison.OrdinalIgnoreCase);
        }

        public static bool IsReadOnlyFallback(bool canWriteConfigDirectory)
        {
            return !canWriteConfigDirectory;
        }
    }
}
