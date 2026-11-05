using System;
using System.Collections.Generic;

namespace QuadLite.Core
{
    public enum CollisionOutcome { Proceed, Skip, Rename, Overwrite, Ask }

    /// <summary>命名与冲突预检：纯字符串规则，不访问文件系统。</summary>
    public static class NameRules
    {
        private static readonly string[] Reserved = new string[]
        {
            "CON", "PRN", "AUX", "NUL", "COM1", "COM2", "COM3", "COM4", "COM5",
            "COM6", "COM7", "COM8", "COM9", "LPT1", "LPT2", "LPT3", "LPT4",
            "LPT5", "LPT6", "LPT7", "LPT8", "LPT9"
        };

        public static bool HasInvalidChars(string name)
        {
            if (name == null) return true;
            for (int i = 0; i < name.Length; i++)
            {
                char c = name[i];
                if (c < 32) return true;
                if (c == '<' || c == '>' || c == ':' || c == '"' || c == '/' ||
                    c == '\\' || c == '|' || c == '?' || c == '*') return true;
            }
            return false;
        }

        public static bool IsReservedDeviceName(string name)
        {
            if (name == null) return false;
            string stem = name;
            int dot = name.IndexOf('.');
            if (dot >= 0) stem = name.Substring(0, dot);
            stem = stem.Trim().ToUpperInvariant();
            for (int i = 0; i < Reserved.Length; i++)
            {
                if (stem == Reserved[i]) return true;
            }
            return false;
        }

        public static bool IsValidFileName(string name)
        {
            if (string.IsNullOrEmpty(name)) return false;
            if (HasInvalidChars(name)) return false;
            if (name.EndsWith(".", StringComparison.Ordinal)) return false;
            if (name.EndsWith(" ", StringComparison.Ordinal)) return false;
            if (IsReservedDeviceName(name)) return false;
            return true;
        }

        /// <summary>同目录副本命名：基名 - 副本 / - 副本 (2) ...</summary>
        public static string CopyOf(string baseName, IList<string> existing)
        {
            string stem = baseName;
            string ext = "";
            int dot = baseName == null ? -1 : baseName.LastIndexOf('.');
            if (dot > 0)
            {
                stem = baseName.Substring(0, dot);
                ext = baseName.Substring(dot);
            }
            for (int n = 1; n < 10000; n++)
            {
                string suffix = n == 1 ? " - 副本" : (" - 副本 (" + n.ToString() + ")");
                string candidate = stem + suffix + ext;
                if (!Contains(existing, candidate)) return candidate;
            }
            return "";
        }

        /// <summary>新建文件夹重名递增。</summary>
        public static string NewFolder(IList<string> existing)
        {
            if (!Contains(existing, "新建文件夹")) return "新建文件夹";
            for (int n = 2; n < 10000; n++)
            {
                string candidate = "新建文件夹 (" + n.ToString() + ")";
                if (!Contains(existing, candidate)) return candidate;
            }
            return "";
        }

        public static CollisionOutcome Resolve(CollisionPolicy policy, bool conflictExists)
        {
            if (!conflictExists) return CollisionOutcome.Proceed;
            if (policy == CollisionPolicy.Ask) return CollisionOutcome.Ask;
            if (policy == CollisionPolicy.Rename) return CollisionOutcome.Rename;
            if (policy == CollisionPolicy.Overwrite) return CollisionOutcome.Overwrite;
            return CollisionOutcome.Skip;
        }

        public static bool Contains(IList<string> existing, string name)
        {
            if (existing == null) return false;
            for (int i = 0; i < existing.Count; i++)
            {
                if (string.Equals(existing[i], name, StringComparison.OrdinalIgnoreCase)) return true;
            }
            return false;
        }
    }
}
