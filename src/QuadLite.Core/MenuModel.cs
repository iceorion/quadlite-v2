using System;
using System.Collections.Generic;

namespace QuadLite.Core
{
    /// <summary>源头消重与自有 ID 段：等价系统项直接用系统项，不等价项改名独立分组；零 verb 枚举。</summary>
    public static class MenuModel
    {
        public const int OwnIdBase = 0x8000;
        public const int MaxOwnId = 0xFFFF;

        private static readonly string[] EquivalentSystemVerbs = new string[]
        {
            "open", "openwith", "properties", "cut", "copy", "delete", "rename", "paste"
        };

        public static bool IsOwnId(int id)
        {
            return id >= OwnIdBase && id <= MaxOwnId;
        }

        public static bool IsSystemId(int id, int maxId)
        {
            return id >= 1 && id < OwnIdBase && id <= maxId;
        }

        /// <summary>InvokeCommand 出参严格落在 [1, maxId]，且只属于系统段或自有段之一。</summary>
        public static bool IsValidInvokeId(int id, int maxId)
        {
            if (id < 1 || id > maxId) return false;
            return IsSystemId(id, maxId) || (IsOwnId(id) && id <= maxId);
        }

        public static bool OwnSegmentDisjoint(int maxId)
        {
            return OwnIdBase > 0 && OwnIdBase <= MaxOwnId && maxId >= 1;
        }

        public static bool IsEquivalentSystemVerb(string name)
        {
            string n = Normalize(name);
            for (int i = 0; i < EquivalentSystemVerbs.Length; i++)
            {
                if (EquivalentSystemVerbs[i] == n) return true;
            }
            return false;
        }

        /// <summary>
        /// 返回真正需要插入的自有项。等价系统项不插入（源头消重）；
        /// 不等价但规范化后同名的项改名后插入独立分组。
        /// </summary>
        public static string[] ResolveOwnVerbs(string[] systemVerbNames, string[] ownVerbNames)
        {
            List<string> sys = new List<string>();
            if (systemVerbNames != null)
            {
                for (int i = 0; i < systemVerbNames.Length; i++) sys.Add(Normalize(systemVerbNames[i]));
            }
            List<string> result = new List<string>();
            if (ownVerbNames == null) return result.ToArray();

            for (int i = 0; i < ownVerbNames.Length; i++)
            {
                string n = Normalize(ownVerbNames[i]);
                if (n.Length == 0) continue;
                if (sys.Contains(n)) continue;  // 等价项：直接用系统项
                string renamed = n;
                int guard = 0;
                while (sys.Contains(renamed) && guard < 8)
                {
                    renamed = n + " (quadlite " + (guard + 1).ToString() + ")";
                    guard++;
                }
                result.Add(renamed);
            }
            return result.ToArray();
        }

        public static bool HasDuplicateNormalized(string[] verbs)
        {
            if (verbs == null) return false;
            List<string> seen = new List<string>();
            for (int i = 0; i < verbs.Length; i++)
            {
                string n = Normalize(verbs[i]);
                if (n.Length == 0) continue;
                if (seen.Contains(n)) return true;
                seen.Add(n);
            }
            return false;
        }

        /// <summary>去引号空白、去 &amp; 助记符、去尾部省略号，统一小写。</summary>
        public static string Normalize(string verb)
        {
            if (verb == null) return "";
            string s = verb.Trim();
            s = s.Replace("&", "");
            while (s.EndsWith("...", StringComparison.Ordinal) || s.EndsWith("…", StringComparison.Ordinal))
            {
                s = s.Substring(0, s.Length - (s.EndsWith("...", StringComparison.Ordinal) ? 3 : 1)).Trim();
            }
            return s.ToLowerInvariant();
        }
    }
}
