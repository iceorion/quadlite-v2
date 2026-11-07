using System;
using System.Collections.Generic;
using QuadLite.Core;

namespace QuadLite.App
{
    /// <summary>
    /// B7 自测状态内核：把 Core 的门卡总表 / 预算表 / 账本 / 包自检判据组装成
    /// 只读报告明细行，并给出总行数、末行 verdict 与退出码。
    /// 零 I/O、不引用呈现框架；真实产品 exe 的 --selftest 入口未接入，未验证。
    /// </summary>
    public sealed class SelftestState
    {
        private const int SummaryLineCount = 3;
        private readonly int groupCount;
        private readonly int redCount;

        public SelftestState(int groupCount, int redCount)
        {
            this.groupCount = groupCount;
            this.redCount = redCount;
        }

        public int GroupCount { get { return groupCount; } }
        public int RedCount { get { return redCount; } }

        public static string[] DetailLines()
        {
            List<string> lines = new List<string>();
            string[] gates = GateInventoryRules.Gates();
            for (int i = 0; i < gates.Length; i++)
            {
                lines.Add("gate: " + gates[i] +
                          " batch=" + GateInventoryRules.BatchFor(gates[i]) +
                          " status=registered");
            }
            string[] kinds = BudgetTableRules.Kinds();
            for (int i = 0; i < kinds.Length; i++)
            {
                lines.Add("budget: " + BudgetTableRules.ReportLine(kinds[i], "unmeasured", "未验证"));
            }
            lines.Add("ledger: batches=" + LedgerRules.BatchIds().Length.ToString() +
                      " head_sha=required evidence-required");
            lines.Add("package: contents=" + PackageSelfcheckRules.ContentCount.ToString() +
                      " excluded-tools=" + PackageSelfcheckRules.ExcludedToolCount.ToString());
            lines.Add("manifest: fields=" + BuildManifestRules.FieldCount.ToString() +
                      " dirty-tree=Warning");
            return lines.ToArray();
        }

        public static int DetailLineCount()
        {
            return DetailLines().Length;
        }

        public int LineCount()
        {
            return SelftestReportRules.LineCount(
                groupCount, GateInventoryRules.GateCount,
                BudgetTableRules.BudgetCount, SummaryLineCount);
        }

        public bool MeetsMinimumLines()
        {
            return SelftestReportRules.MeetsMinimum(LineCount()) &&
                   LineCount() == groupCount + DetailLineCount() + SelftestReportRules.TailLineCount;
        }

        public string GroupLine(string group, bool ok)
        {
            return SelftestReportRules.GroupLine(group, ok);
        }

        public string VerdictLine()
        {
            return SelftestReportRules.VerdictLine(redCount);
        }

        public int ExitCode()
        {
            return SelftestReportRules.ExitCode(redCount);
        }

        public string[] Sections()
        {
            return SelftestReportRules.ReportSections();
        }
    }
}
