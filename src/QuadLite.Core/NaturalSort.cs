using System;
using System.Collections.Generic;

namespace QuadLite.Core
{
    /// <summary>自然排序：数字按数值比较，非数字按忽略大小写比较，差异用 Ordinal 兜底保证确定性。</summary>
    public sealed class NaturalComparer : IComparer<string>
    {
        public static readonly NaturalComparer Instance = new NaturalComparer();

        public int Compare(string a, string b)
        {
            if (a == null && b == null) return 0;
            if (a == null) return -1;
            if (b == null) return 1;

            int i = 0, j = 0;
            while (i < a.Length && j < b.Length)
            {
                bool da = IsDigit(a[i]);
                bool db = IsDigit(b[j]);
                if (da && db)
                {
                    int si = i, sj = j;
                    while (i < a.Length && IsDigit(a[i])) i++;
                    while (j < b.Length && IsDigit(b[j])) j++;
                    int la = i - si, lb = j - sj;
                    long va, vb;
                    bool pa = long.TryParse(a.Substring(si, la), out va);
                    bool pb = long.TryParse(b.Substring(sj, lb), out vb);
                    if (pa && pb)
                    {
                        if (va != vb) return va < vb ? -1 : 1;
                    }
                    else
                    {
                        string na = TrimZeros(a.Substring(si, la));
                        string nb = TrimZeros(b.Substring(sj, lb));
                        if (na.Length != nb.Length) return na.Length < nb.Length ? -1 : 1;
                        int c = string.CompareOrdinal(na, nb);
                        if (c != 0) return c;
                    }
                    // 数值相等时，前导零更多（数字串更长）者排前，保持与 v1 已验证行为一致
                    if (la != lb) return la > lb ? -1 : 1;
                }
                else
                {
                    char ca = char.ToUpperInvariant(a[i]);
                    char cb = char.ToUpperInvariant(b[j]);
                    if (ca != cb) return ca < cb ? -1 : 1;
                    i++;
                    j++;
                }
            }
            if (i < a.Length) return 1;
            if (j < b.Length) return -1;
            return string.CompareOrdinal(a, b);
        }

        private static bool IsDigit(char c)
        {
            return c >= '0' && c <= '9';
        }

        private static string TrimZeros(string s)
        {
            int k = 0;
            while (k < s.Length - 1 && s[k] == '0') k++;
            return s.Substring(k);
        }
    }
}
