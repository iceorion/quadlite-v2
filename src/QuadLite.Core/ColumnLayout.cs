using System;

namespace QuadLite.Core
{
    /// <summary>列宽唯一来源：Σ列宽 = 行宽 − 12，名称列保底 120，不产生 0 &lt; 宽 &lt; 下限 的窄条。</summary>
    public static class ColumnLayout
    {
        public const int Gutter = 12;
        public const int MinNameWidth = 120;
        public const int MinOtherWidth = 40;
        public const int ColumnCount = 4;
        private static readonly int[] Defaults = new int[] { 260, 130, 110, 90 };

        public static bool TryFor(int rowWidth, int[] prefs, out int[] widths)
        {
            widths = null;
            int minBudget = MinNameWidth + (ColumnCount - 1) * MinOtherWidth;
            if (rowWidth < Gutter + minBudget) return false;

            int budget = rowWidth - Gutter;
            int[] w = new int[ColumnCount];
            for (int i = 0; i < ColumnCount; i++)
            {
                int p = (prefs != null && i < prefs.Length) ? prefs[i] : 0;
                if (p < 0) return false;
                w[i] = p == 0 ? Defaults[i] : p;  // 0 = 自动
            }

            for (int i = 0; i < ColumnCount; i++)
            {
                int min = i == 0 ? MinNameWidth : MinOtherWidth;
                if (w[i] < min) w[i] = min;
            }

            int sum = Sum(w);
            if (sum > budget)
            {
                int excess = sum - budget;
                for (int i = ColumnCount - 1; i >= 0 && excess > 0; i--)
                {
                    int min = i == 0 ? MinNameWidth : MinOtherWidth;
                    int take = Math.Min(excess, w[i] - min);
                    if (take > 0) { w[i] -= take; excess -= take; }
                }
                if (excess > 0) return false;
                sum = budget;
            }
            if (sum < budget) w[0] += budget - sum;

            if (!IsValid(rowWidth, w)) return false;
            widths = w;
            return true;
        }

        public static bool IsValid(int rowWidth, int[] w)
        {
            if (w == null || w.Length != ColumnCount) return false;
            if (Sum(w) != rowWidth - Gutter) return false;
            for (int i = 0; i < ColumnCount; i++)
            {
                int min = i == 0 ? MinNameWidth : MinOtherWidth;
                if (w[i] < min) return false;
            }
            return true;
        }

        private static int Sum(int[] w)
        {
            int s = 0;
            for (int i = 0; i < w.Length; i++) s += w[i];
            return s;
        }
    }
}
