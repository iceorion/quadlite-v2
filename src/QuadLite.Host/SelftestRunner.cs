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

            // R2：托管 COM 绑定只做可判定的静态契约断言，绝不实例化 shell 对象。
            probe.Add("r2-com-id-contract",
                      QuadLite.Fs.ShellContractRules.DeclaredFileOperationMethods() == 21 &&
                      QuadLite.Fs.ShellContractRules.DeclaredProgressSinkMethods() == 16 &&
                      QuadLite.Fs.ShellContractRules.DeclaredShellItemMethods() == 5 &&
                      !QuadLite.Fs.ShellContractRules.IsRuntimeVerified());

            probe.Add("r2-sta-contract",
                      QuadLite.Fs.ShellContractRules.RequiresStaThread() &&
                      QuadLite.Fs.ShellContractRules.NewInstancePerOperation() &&
                      QuadLite.Fs.ShellContractRules.UndoRecordAsymmetric(true) &&
                      !QuadLite.Fs.ShellContractRules.UndoRecordAsymmetric(false));

            probe.Add("r2-flags-contract",
                      (QuadLite.Fs.ShellContractRules.FlagsFor(OpKind.Delete, false) & 0x0040u) != 0 &&
                      (QuadLite.Fs.ShellContractRules.FlagsFor(OpKind.PermanentDelete, false) & 0x4000u) != 0 &&
                      (QuadLite.Fs.ShellContractRules.FlagsFor(OpKind.Copy, false) & 0x20000000u) == 0);

            probe.Add("r2-transport-closed-by-default",
                      !new QuadLite.Fs.ShellTransport().Enabled);

            // R3：浏览 / 搬运 UI 的可判定契约。全部纯内存：不创建窗口、不枚举目录、
            // 不实例化 COM、不调用 PerformOperations。
            probe.Add("r3-slots-complete",
                      UiContractRules.SlotsComplete() && UiContractRules.PaneObjectsPersist());

            probe.Add("r3-late-batch-discarded",
                      UiContractRules.LateBatchDiscarded(7, 6) &&
                      !UiContractRules.LateBatchDiscarded(7, 7));

            probe.Add("r3-transport-honest",
                      UiContractRules.TransportHonest(false,
                          "搬运未执行：transport 默认关闭（未验证）") &&
                      !UiContractRules.TransportHonest(true, "搬运完成"));

            probe.Add("r3-no-inferred-success",
                      UiContractRules.NoInferredSuccess() &&
                      UiContractRules.StatusFieldCount() == 3);

            PathKey dir = null;
            PathKey file = null;
            PathKey dest = null;
            bool parsed = PathKey.TryNormalize("C:\\src", out dir) &&
                          PathKey.TryNormalize("C:\\src\\a.txt", out file) &&
                          PathKey.TryNormalize("C:\\dst", out dest);
            List<PathKey> selection = new List<PathKey>();
            if (parsed && file != null) selection.Add(file);
            OpRequest copyRequest = null;
            OpRequest badRequest = null;
            bool copyOk = false;
            bool badOk = false;
            if (parsed)
            {
                copyOk = BrowseCommands.TryBuildRequest(OpKind.Copy, selection, dir, dest, 0,
                                                        out copyRequest);
                badOk = BrowseCommands.TryBuildRequest(OpKind.Copy, selection, dir, null, 0,
                                                      out badRequest);
            }
            probe.Add("r3-request-hard-validation",
                      parsed && copyOk && !badOk && copyRequest != null && badRequest != null);

            probe.Add("r3-runtime-unverified",
                      !UiContractRules.IsRuntimeVerified() &&
                      !new BrowseCommands().TransportEnabled &&
                      UiContractRules.BoundaryText() == "未实现 / 未验证");

            // R4：原生菜单宿主的可判定契约。全部纯内存：不创建窗口、不枚举目录、
            // 不实例化 COM、不调用 TrackPopupMenuEx / InvokeCommand。
            probe.Add("r4-standard-four-steps",
                      MenuCommands.ContractIsHonest() &&
                      QuadLite.Fs.MenuHostContractRules.StandardSequenceDeclared() &&
                      QuadLite.Fs.MenuHostContractRules.RequiresStaThread());

            probe.Add("r4-sdk-vtable-sizes",
                      QuadLite.Fs.MenuHostContractRules.DeclaredContextMenuMethods() == 3 &&
                      QuadLite.Fs.MenuHostContractRules.DeclaredContextMenu2Methods() == 4 &&
                      QuadLite.Fs.MenuHostContractRules.DeclaredContextMenu3Methods() == 5 &&
                      QuadLite.Fs.MenuHostContractRules.DeclaredShellFolderMethods() == 10);

            probe.Add("r4-no-verb-no-deletion",
                      QuadLite.Fs.MenuHostContractRules.VerbsNeverQueried() &&
                      MenuHostRules.VerbScanCount() == 0 &&
                      MenuHostRules.MenuTreeRewriteCount() == 0);

            probe.Add("r4-id-range-and-own-segment",
                      MenuInvokeRules.InvokeIdInRange(1, 7) &&
                      MenuInvokeRules.ShouldDrop(0x7FFF, 7) &&
                      !MenuInvokeRules.ShouldDrop(0x8001, 7) &&
                      QuadLite.Fs.MenuHostContractRules.OwnSegmentDisjoint());

            probe.Add("r4-message-forwarding",
                      QuadLite.Fs.MenuHostContractRules.ForwardsAllMessages() &&
                      MenuMessageRules.ForwardsOnlyDuringTracking() &&
                      MenuMessageRules.ForwardsToContextMenu2Or3());

            probe.Add("r4-release-rules",
                      QuadLite.Fs.MenuHostContractRules.ReleasesBothResources() &&
                      MenuReleaseRules.ReleaseCallCount(2) == 4);

            probe.Add("r4-target-snapshot",
                      QuadLite.Fs.MenuHostContractRules.UsesImmutableSnapshot() &&
                      MenuTargetSnapshotRules.SystemItemDispatchAlsoUsesSnapshot());

            probe.Add("r4-runtime-unverified",
                      !QuadLite.Fs.MenuHostContractRules.IsRuntimeVerified() &&
                      !new QuadLite.Fs.NativeMenuHost().Enabled &&
                      !new MenuCommands().Enabled);
        }
    }
}
