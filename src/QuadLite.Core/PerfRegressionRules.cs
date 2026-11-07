using System;

namespace QuadLite.Core
{
    /// <summary>
    /// §8.3 H5 / §7.3 / NFR-11 性能回归表：枚举 / 滚动 / 导航 / 启动四项，
    /// 每行必须记「机器 + 系统版本 + 文件系统 + DPI + 负载规模 + 实测值 + 证据等级」，
    /// 只用于跨版本阅读，毫秒只报数、不判定、不进 CI 闸门。
    /// 纯判据，零 I/O；复用 B7 的 BudgetTableRules，不另立预算表。
    /// </summary>
    public static class PerfRegressionRules
    {
        public const int BudgetCount = 4;
        public const int ColumnCount = 7;

        public static string[] Columns()
        {
            return BudgetTableRules.Columns();
        }

        public static string[] Kinds()
        {
            return BudgetTableRules.Kinds();
        }

        public static bool RequiresMachineAndEnvironment()
        {
            return true;
        }

        public static bool SingleNumberIsInsufficient()
        {
            return true;
        }

        public static bool OnlyReportsNumbers()
        {
            return BudgetTableRules.OnlyReportsNumbers();
        }

        public static bool IsCiGate()
        {
            return BudgetTableRules.IsCiGate();
        }

        /// <summary>七列任一为空即不合格；只有实测值一列不算一行回归记录。</summary>
        public static bool RowComplete(string[] row)
        {
            if (row == null || row.Length != ColumnCount) return false;
            for (int i = 0; i < row.Length; i++)
            {
                if (string.IsNullOrEmpty(row[i])) return false;
                if (row[i] == "unmeasured") return false;
            }
            return true;
        }

        public static bool CountsMatchDeclaration()
        {
            return Columns().Length == ColumnCount && Kinds().Length == BudgetCount &&
                   IsCiGate() == false && RowComplete(new string[ColumnCount]) == false;
        }
    }
}
