using System;
using System.Collections.Generic;
using QuadLite.App;
using QuadLite.Core;

namespace QuadLite.Host
{
    /// <summary>
    /// 产品 exe 形态的 --selftest（R1 首次真实接入）：不创建窗口，逐行输出
    /// Core / App 契约的真实明细与一组运行时断言，末行 verdict，退出码 = 红条数。
    /// 报告行数 ≥100（§8.1 B7 判据），毫秒项只报数不判定。
    /// 这里跑的是产品自身可判定的契约，不把测试替身的结果冒充真机取证。
    /// </summary>
    public static class SelftestRunner
    {
        private sealed class Probe
        {
            private readonly List<string> names;
            private readonly List<bool> oks;

            public Probe()
            {
                names = new List<string>();
                oks = new List<bool>();
            }

            public void Add(string name, bool ok)
            {
                names.Add(name);
                oks.Add(ok);
            }

            public int Count { get { return names.Count; } }

            public int Failures()
            {
                int n = 0;
                for (int i = 0; i < oks.Count; i++)
                {
                    if (!oks[i]) n++;
                }
                return n;
            }

            public string Name(int i) { return names[i]; }
            public bool Ok(int i) { return oks[i]; }
        }

        public static int Run()
        {
            List<string> lines = new List<string>();
            lines.Add("QuadLite product --selftest / R1 real host / spec v1.2 unchanged");
            lines.Add("host: single-file exe, zero NuGet, C# 5, net48, WPF message loop");

            // 门卡总表 / 预算表 / 账本 / 包自检 / BUILD.txt 明细（B7 状态内核）。
            string[] detail = SelftestState.DetailLines();
            for (int i = 0; i < detail.Length; i++) lines.Add(detail[i]);

            // 发布线阶段与依赖（B8 状态内核），全部标 evidence-required。
            ReleaseState release = new ReleaseState(true);
            string[] stageLines = release.DetailLines();
            for (int i = 0; i < stageLines.Length; i++) lines.Add(stageLines[i]);

            string[] gateGroups = GateInventoryRules.Groups();
            for (int i = 0; i < gateGroups.Length; i++) lines.Add("gate-group: " + gateGroups[i]);
            string[] batches = GateInventoryRules.BatchIds();
            for (int i = 0; i < batches.Length; i++) lines.Add("batch: " + batches[i] + " status=recorded");
            string[] kinds = BudgetTableRules.Kinds();
            for (int i = 0; i < kinds.Length; i++)
            {
                lines.Add("budget-kind: " + kinds[i] + " measured=unmeasured evidence=未验证");
            }
            string[] mechanisms = SingleInstanceRules.Mechanisms();
            for (int i = 0; i < mechanisms.Length; i++)
            {
                lines.Add("single-instance: " + mechanisms[i] +
                          (i == 0 ? " status=host-wired" : " status=未实现 / 未验证"));
            }
            string[] stages = ReleaseStageRules.StageIds();
            for (int i = 0; i < stages.Length; i++)
            {
                lines.Add("release-stage: " + stages[i] + " evidence=未验证");
            }

            Probe probe = new Probe();
            RunProbes(probe);
            int baseCount = lines.Count;
            int summaryCount = 3;
            int shapeGroupCount = 1;
            int verdictCount = 1;
            int expectedTotal = baseCount + probe.Count + shapeGroupCount + summaryCount + verdictCount;
            probe.Add("host-report-shape",
                      SelftestReportRules.MeetsMinimum(expectedTotal) &&
                      SelftestReportRules.MinOutputLines == 100);
            for (int i = 0; i < probe.Count; i++)
            {
                lines.Add(SelftestReportRules.GroupLine(probe.Name(i), probe.Ok(i)));
            }

            int failures = probe.Failures();
            lines.Add("groups=" + probe.Count.ToString() + " detail=" + detail.Length.ToString() +
                      " stage-lines=" + stageLines.Length.ToString());
            lines.Add("report-lines=" + expectedTotal.ToString() +
                      " min=" + SelftestReportRules.MinOutputLines.ToString());
            lines.Add("evidence: static-contract=CI, shell/DPI/AV/performance/manual=未验证; M1 与 M3 均不成立");
            if (lines.Count + verdictCount != expectedTotal) failures++;
            lines.Add(SelftestReportRules.VerdictLine(failures));

            for (int i = 0; i < lines.Count; i++) Console.WriteLine(lines[i]);
            return SelftestReportRules.ExitCode(failures);
        }

        private static void RunProbes(Probe probe)
        {
            CliParseResult known = CliArgs.Parse(new string[] { "--selftest" });
            probe.Add("host-cli-known", known.Ok && known.Mode == CliMode.SelfTest);

            CliParseResult unknown = CliArgs.Parse(new string[] { "--not-a-real-parameter" });
            probe.Add("host-cli-reject", !unknown.Ok);

            CliParseResult shot = CliArgs.Parse(new string[] { "--shot" });
            probe.Add("host-cli-exempt", shot.Ok && !shot.ParticipatesInSingleInstance);

            bool layoutOk = LayoutMath.IsKnownMode(1) && LayoutMath.IsKnownMode(2) &&
                            LayoutMath.IsKnownMode(3) && LayoutMath.IsKnownMode(4) &&
                            !LayoutMath.IsKnownMode(5) &&
                            LayoutMath.PaneCount(LayoutMode.Four) == 4 &&
                            LayoutMath.PaneCount(LayoutMode.Three) == 3;
            probe.Add("host-layout-four", layoutOk);

            probe.Add("host-single-instance", SingleInstanceRules.CountsMatchDeclaration() &&
                                              SingleInstanceRules.DiagnosticModesBypass());

            probe.Add("host-gate-inventory",
                      GateInventoryRules.Gates().Length == GateInventoryRules.GateCount &&
                      !GateInventoryRules.IsOrphan("menu-no-verb-scan") &&
                      GateInventoryRules.IsOrphan("menu-not-registered"));

            probe.Add("host-selftest-report",
                      SelftestReportRules.MeetsMinimum(120) &&
                      !SelftestReportRules.MeetsMinimum(99) &&
                      SelftestReportRules.IsVerdictLine("verdict: GREEN") &&
                      SelftestReportRules.ExitCode(3) == 3);

            probe.Add("host-manifest",
                      BuildManifestRules.FieldCount == 7 &&
                      BuildManifestRules.CountsMatchDeclaration() &&
                      BuildManifestRules.RequiresWarningWhenDirty());

            ReleaseState release = new ReleaseState(true);
            probe.Add("host-release-evidence",
                      !release.IsPublishable() &&
                      release.CurrentM3Verdict() == ReleaseVerdict.EvidenceMissing);
        }
    }
}
