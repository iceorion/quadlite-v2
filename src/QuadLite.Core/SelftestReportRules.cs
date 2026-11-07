using System;

namespace QuadLite.Core
{
    /// <summary>
    /// §8.1 B7 / §6.4 `--selftest` 输出形态：每组一行、末行 verdict、
    /// 输出 ≥100 行、退出码 = 红条数；毫秒 / 时序门卡只报数不判定（§7.3 / NFR-09）。
    /// 纯判据，零 I/O；真实产品 exe 形态的 --selftest 由出包后的发布线承担，未验证。
    /// </summary>
    public static class SelftestReportRules
    {
        public const int MinOutputLines = 100;
        public const int TailLineCount = 2;
        public const string VerdictPrefix = "verdict: ";

        public static string GroupLine(string group, bool ok)
        {
            return group + ": ok=" + (ok ? "True" : "False");
        }

        public static bool IsVerdictLine(string line)
        {
            return line != null && line.StartsWith(VerdictPrefix, StringComparison.Ordinal);
        }

        public static string VerdictLine(int redCount)
        {
            if (redCount == 0) return VerdictPrefix + "GREEN";
            return VerdictPrefix + "RED (" + redCount.ToString() + ")";
        }

        public static int ExitCode(int redCount)
        {
            return redCount;
        }

        /// <summary>报告 = 门卡总表明细 + 组结果 + 预算表 + 账本 / 包自检 + 尾部两行。</summary>
        public static int LineCount(int groupCount, int gateRows, int budgetRows, int summaryRows)
        {
            return gateRows + budgetRows + summaryRows + groupCount + TailLineCount;
        }

        public static bool MeetsMinimum(int lines)
        {
            return lines >= MinOutputLines;
        }

        public static bool MillisecondsAreReportOnly()
        {
            return true;
        }

        public static bool NoTimingVerdict()
        {
            return true;
        }

        public static string[] ReportSections()
        {
            return new string[] {
                "gate-inventory", "budget-table", "ledger", "package", "groups", "verdict"
            };
        }
    }
}
