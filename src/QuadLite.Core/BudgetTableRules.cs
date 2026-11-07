using System;

namespace QuadLite.Core
{
    /// <summary>§7.3 / NFR-11 四项性能预算。</summary>
    public enum BudgetKind { Enumeration, Scroll, Navigation, Startup }

    /// <summary>
    /// 性能预算表判据：每行必须记「机器 + 系统版本 + 文件系统 + DPI + 负载规模 +
    /// 实测值 + 证据等级」七项；毫秒只报数、不判定、不进 CI 闸门（NFR-09）。
    /// 纯判据，零 I/O；真实采样由 --bench 与发布线 H5 承担，未验证。
    /// </summary>
    public static class BudgetTableRules
    {
        public const int BudgetCount = 4;
        public const int ColumnCount = 7;

        public static string[] Kinds()
        {
            return new string[] { "enumeration", "scroll", "navigation", "startup" };
        }

        public static string[] Columns()
        {
            return new string[] {
                "machine", "os-version", "filesystem", "dpi",
                "load-scale", "measured-value", "evidence-level"
            };
        }

        public static bool OnlyReportsNumbers()
        {
            return true;
        }

        public static bool NoThresholdVerdict()
        {
            return true;
        }

        public static bool IsCiGate()
        {
            return false;
        }

        public static string ReportLine(string kind, string measuredValue, string evidence)
        {
            return kind + ": measured=" + measuredValue + ", evidence=" + evidence +
                   " (report-only)";
        }

        public static bool CountsMatchDeclaration()
        {
            return Kinds().Length == BudgetCount && Columns().Length == ColumnCount;
        }
    }
}
