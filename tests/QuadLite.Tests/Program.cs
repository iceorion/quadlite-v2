using System;
using System.Collections.Generic;
using QuadLite.Core;
using QuadLite.App;

namespace QuadLite.Tests
{
    /// <summary>
    /// C# 零依赖 runner：无第三方库，每组一行 `组名: ok=True/False`，末行 verdict，退出码 = 红条数。
    /// 编译约束：C# 5 语法子集、net48 运行时、只引用 mscorlib / System.dll / System.Core.dll。
    /// </summary>
    public static class Program
    {
        private static int failures;

        public static int Main(string[] args)
        {
            // B7 自测装置：门卡总表 / 预算表 / 账本 / 包自检明细行先行输出，
            // 与后面的组结果和末两行合计 ≥100 行（§8.1 B7）。
            string[] detail = SelftestState.DetailLines();
            for (int i = 0; i < detail.Length; i++)
            {
                Console.WriteLine(detail[i]);
            }

            Run("path-*", TestPath);
            Run("safewalk-*", TestSafeWalk);
            Run("cols-*", TestColumns);
            Run("view-*", TestView);
            Run("cell-metrics", TestCellMetrics);
            Run("menu-model", TestMenuModel);
            Run("safety-*", TestSafety);
            Run("enum-*", TestEnums);
            Run("sort-*", TestSort);
            Run("state-*", TestState);
            Run("error-*", TestError);
            Run("op-record", TestOpRecord);
            Run("op-plan", TestOpPlan);
            Run("select-*", TestSelection);
            Run("op-*", TestOps);
            Run("undo-*", TestUndo);
            Run("name-*", TestNames);
            Run("refresh-*", TestRefresh);
            Run("clipboard-*", TestClipboard);
            Run("cli-*", TestCli);
            Run("portable-*", TestPortable);
            Run("layout-*", TestLayout);
            Run("split-*", TestSplit);
            Run("tab-*", TestTabs);
            Run("nav-*", TestNav);
            Run("bookmark-*", TestBookmarks);
            Run("session-*", TestSession);
            Run("group-*", TestGroup);
            Run("filter-*", TestFilter);
            Run("toggle-*", TestToggle);
            Run("hit-*", TestHit);
            Run("status-*", TestStatus);
            Run("keys-*", TestKeys);
            Run("menu-native-default", TestMenuNativeDefault);
            Run("menu-no-verb-scan", TestMenuNoVerbScan);
            Run("menu-id-range", TestMenuIdRange);
            Run("menu-own-id-segment", TestMenuOwnIdSegment);
            Run("menu-no-fusion", TestMenuNoFusion);
            Run("menu-msg-forward", TestMenuMsgForward);
            Run("menu-target-snapshot", TestMenuTargetSnapshot);
            Run("menu-sentinel", TestMenuSentinel);
            Run("menu-single-file", TestMenuSingleFile);
            Run("menu-release", TestMenuRelease);
            Run("icon-*", TestIcons);
            Run("thumb-whitelist", TestThumbWhitelist);
            Run("thumb-exif", TestThumbExif);
            Run("thumb-queue", TestThumbQueue);
            Run("thumb-cache", TestThumbCache);
            Run("thumb-video", TestThumbVideo);
            Run("thumb-toggle", TestThumbToggle);
            Run("thumb-nowrite", TestThumbNoWrite);
            Run("cli-contract", TestCliContract);
            Run("cli-diagnostic-modes", TestCliDiagnosticModes);
            Run("cli-selftest-output", TestCliSelftestOutput);
            Run("portable-gate-inventory", TestGateInventory);
            Run("portable-ledger", TestLedger);
            Run("portable-build-manifest", TestBuildManifest);
            Run("portable-package-selfcheck", TestPackageSelfcheck);
            Run("portable-budget-table", TestBudgetTable);
            Run("portable-crash-digest", TestCrashDigest);
            Run("single-instance-marker", TestSingleInstanceMarker);

            SelftestState report = new SelftestState(61, failures);
            Console.WriteLine("groups=" + report.GroupCount.ToString() +
                              ", failed=" + failures.ToString() +
                              ", lines=" + report.LineCount().ToString());
            Console.WriteLine(report.VerdictLine());
            return report.ExitCode();
        }

        private static void Run(string name, Func<bool> test)
        {
            bool ok;
            try
            {
                ok = test();
            }
            catch (Exception ex)
            {
                ok = false;
                Console.WriteLine("  " + name + " threw: " + ex.Message);
            }
            Console.WriteLine(name + ": ok=" + (ok ? "True" : "False"));
            if (!ok) failures++;
        }

        private static bool TestPath()
        {
            PathKey k;
            Check.True(PathKey.TryNormalize("  \"D:\\Data\\a\\..\\b\"  ", out k), "quoted + whitespace");
            Check.Equal(@"D:\Data\b", k.Value, "dot segments");

            Check.True(PathKey.TryNormalize("d:/data//sub/", out k), "slash mix");
            Check.Equal(@"D:\data\sub", k.Value, "collapsed separators");
            Check.Equal(PathKind.Drive, k.Kind, "drive kind");

            Check.False(PathKey.TryNormalize(@"D:\..\oops", out k), "escape above root");
            Check.False(PathKey.TryNormalize(@"\\.\PhysicalDrive0", out k), "device namespace rejected");
            Check.False(PathKey.TryNormalize("", out k), "empty rejected");
            Check.False(PathKey.TryNormalize("relative\\path", out k), "relative rejected");

            Check.True(PathKey.TryNormalize(@"\\server\share\a\..\b", out k), "UNC");
            Check.Equal(@"\\server\share\b", k.Value, "UNC dot segments");
            Check.Equal(PathKind.Unc, k.Kind, "UNC kind");
            Check.False(PathKey.TryNormalize(@"\\server\..\share", out k), "UNC root escape");

            Check.True(PathKey.TryNormalize(@"\\?\D:\Data\a\..\b", out k), "long path");
            Check.True(k.Value.StartsWith(@"\\?\", StringComparison.Ordinal), "long prefix kept");
            Check.True(k.Value.IndexOf(@"..", StringComparison.Ordinal) >= 0, "long path is literal");
            Check.Equal(PathKind.LongPath, k.Kind, "long kind");

            PathKey a, b;
            PathKey.TryNormalize(@"D:\Data\A", out a);
            PathKey.TryNormalize(@"d:\data\a", out b);
            Check.True(a.Equals(b), "case-insensitive identity");
            Check.Equal(a.GetHashCode(), b.GetHashCode(), "hash follows identity");

            PathKey leaf, parent;
            PathKey.TryNormalize(@"D:\Data\sub\file.txt", out leaf);
            Check.Equal("file.txt", leaf.GetLeafName(), "leaf name");
            Check.True(leaf.TryGetParent(out parent), "parent exists");
            Check.Equal(@"D:\Data\sub", parent.Value, "parent value");
            PathKey root;
            PathKey.TryNormalize(@"D:\", out root);
            Check.False(root.TryGetParent(out parent), "volume root has no parent");

            PathKey ancestor, child;
            PathKey.TryNormalize(@"D:\Data", out ancestor);
            PathKey.TryNormalize(@"D:\Data\sub", out child);
            Check.True(child.IsUnder(ancestor), "is under");
            Check.False(ancestor.IsUnder(child), "not reversed");

            // B3 地址栏：规范化统一走 PathKey，非法输入给结构化原因码且不导航
            PathKey addr;
            AddressReject why;
            Check.True(AddressRules.TryParse("  \"D:\\Data\\a\\..\\b\"  ", out addr, out why), "address quoted + dot segments");
            Check.Equal(@"D:\Data\b", addr.Value, "address normalized");
            Check.Equal(AddressReject.None, why, "no reject");
            Check.True(AddressRules.TryParse(@"\\server\share\x", out addr, out why), "address UNC");
            Check.True(AddressRules.TryParse(@"\\?\D:\long", out addr, out why), "address long path");
            Check.False(AddressRules.TryParse(@"\\.\PhysicalDrive0", out addr, out why), "device namespace refused");
            Check.Equal(AddressReject.DeviceNamespace, why, "device namespace reason");
            Check.False(AddressRules.TryParse(@"D:\..\oops", out addr, out why), "root escape refused");
            Check.Equal(AddressReject.EscapeAboveRoot, why, "root escape reason");
            Check.False(AddressRules.TryParse("relative\\path", out addr, out why), "relative refused");
            Check.Equal(AddressReject.Relative, why, "relative reason");
            Check.False(AddressRules.ShouldNavigate(AddressReject.Relative), "bad input does not navigate");
            Check.Equal("Relative", AddressRules.RejectText(AddressReject.Relative), "structured reason text");
            Check.True(AddressRules.IsDriveItem(@"C:\"), "drive item");
            Check.False(AddressRules.IsDriveItem(@"C:\Data"), "non-root is not a drive item");
            string[] drives = AddressRules.DriveDropdown(new string[] { @"C:\", @"c:\", @"D:\", "bad" });
            Check.Equal(2, drives.Length, "drive dropdown deduped");
            Check.Equal(@"C:\", drives[0], "drive dropdown normalized");
            return true;
        }

        private static bool TestSafeWalk()
        {
            PathKey root, joined;
            PathKey.TryNormalize(@"D:\Data", out root);
            WalkReject reject;

            Check.True(SafeWalk.TryJoin(root, @"a\..\b", out joined, out reject), "join");
            Check.Equal(@"D:\Data\b", joined.Value, "joined value");
            Check.Equal((int)WalkReject.None, (int)reject, "no reject");

            // 边界 1：空段 / 非法字符
            Check.False(SafeWalk.TryJoin(root, @"a\<bad>", out joined, out reject), "invalid segment");
            Check.Equal((int)WalkReject.EmptySegment, (int)reject, "boundary 1");
            Check.False(SafeWalk.TryJoin(root, "   ", out joined, out reject), "blank child");
            Check.Equal((int)WalkReject.EmptySegment, (int)reject, "boundary 1 blank");

            // 边界 2：.. 越界根
            Check.False(SafeWalk.TryJoin(root, @"..\..\oops", out joined, out reject), "escape root");
            Check.Equal((int)WalkReject.EscapeRoot, (int)reject, "boundary 2");

            // 边界 3：绝对路径注入
            Check.False(SafeWalk.TryJoin(root, @"E:\Injected", out joined, out reject), "absolute injection");
            Check.Equal((int)WalkReject.AbsoluteInjection, (int)reject, "boundary 3");
            Check.False(SafeWalk.TryJoin(root, @"\leading", out joined, out reject), "leading slash");
            Check.Equal((int)WalkReject.AbsoluteInjection, (int)reject, "boundary 3 slash");

            // 边界 4：长度超限
            string huge = new string('x', PathKey.MaxPathLength + 1);
            Check.False(SafeWalk.TryJoin(root, huge, out joined, out reject), "too long");
            Check.Equal((int)WalkReject.TooLong, (int)reject, "boundary 4");
            return true;
        }

        private static bool TestColumns()
        {
            int[] w;
            Check.True(ColumnLayout.TryFor(1000, null, out w), "auto widths");
            Check.True(ColumnLayout.IsValid(1000, w), "sum identity");
            Check.True(w[0] >= ColumnLayout.MinNameWidth, "name floor");

            Check.True(ColumnLayout.TryFor(1000, new int[] { 0, 0, 0, 0 }, out w), "0 = auto");
            Check.True(ColumnLayout.IsValid(1000, w), "auto identity");

            Check.True(ColumnLayout.TryFor(252, null, out w), "minimum row width");
            Check.True(ColumnLayout.IsValid(252, w), "minimum identity");
            Check.Equal(240, w[0] + w[1] + w[2] + w[3], "row width - gutter");

            Check.False(ColumnLayout.TryFor(251, null, out w), "narrower than minimum");
            Check.False(ColumnLayout.TryFor(1000, new int[] { 1, -1, 0, 0 }, out w), "negative preference");
            Check.False(ColumnLayout.IsValid(1000, new int[] { 10, 10, 10, 10 }), "narrow strips rejected");
            Check.False(ColumnLayout.IsValid(1000, new int[] { 100, 100, 100 }), "column count");

            // B4 cols-*：拖宽 / 双击复位 / 偏好按标签 / 列宽唯一来源
            Check.Equal(0, ColumnPrefRules.Auto, "0 = auto");
            Check.Equal(4, ColumnPrefRules.BoundaryCount, "four boundaries");
            int[] prefs = ColumnPrefRules.DefaultPrefs();
            Check.Equal(4, prefs.Length, "one pref per column");
            Check.True(ColumnPrefRules.IsAuto(prefs[0]), "default is auto");
            int[] dragged = ColumnPrefRules.Drag(prefs, 1, 200);
            Check.Equal(200, dragged[1], "drag writes dragged column");
            Check.Equal(0, prefs[1], "drag does not mutate input");
            int[] reset = ColumnPrefRules.ResetColumn(dragged, 1);
            Check.Equal(0, reset[1], "double click resets to auto");
            Check.Equal(4, ColumnPrefRules.DraggableBoundaries(), "four draggable boundaries");
            Check.True(ColumnPrefRules.Drag(prefs, 5, 100) == null, "bad column refused");
            Check.True(ColumnPrefRules.PrefsPerTab(ColumnPrefRules.CloneForTab(prefs),
                                                   ColumnPrefRules.CloneForTab(prefs)), "prefs per tab");
            Check.True(ColumnPrefRules.WidthsComeFromSingleSource(1000, prefs), "ColumnLayout is single source");
            Check.Equal(988, ColumnPrefRules.ExpectedSum(1000), "sum = row - gutter");
            Check.False(ColumnPrefRules.IsValidPrefs(new int[] { 0, 0, 0 }), "wrong pref length");
            return true;
        }

        private static bool TestView()
        {
            Check.Equal(7, ViewSpecs.Count, "seven modes");
            for (int i = 0; i < ViewSpecs.Count; i++)
            {
                Check.True(ViewSpecs.IsKnown(i), "mode " + i.ToString());
                Check.NotNull(ViewSpecs.For((ViewMode)i), "spec " + i.ToString());
            }
            Check.False(ViewSpecs.IsKnown(7), "unknown mode");
            Check.Equal(1, ViewSpecs.GroupHeaderCount(), "only Details renders group header");
            Check.True(ViewSpecs.For(ViewMode.Details).RendersGroupHeader, "Details group header");
            Check.False(ViewSpecs.For(ViewMode.List).RendersGroupHeader, "List no group header");
            Check.Equal(256, ViewSpecs.For(ViewMode.ExtraLarge).TargetPx, "extra large target");
            Check.Equal(0, ViewSpecs.For(ViewMode.Details).TargetPx, "details no thumbnail");
            Check.True(ViewSpecs.NeedsThumbnail(ViewMode.Tiles), "single source for thumbnails");
            Check.False(ViewSpecs.NeedsThumbnail(ViewMode.Small), "small no thumbnail");

            // B4 view-*：组头只在详细信息档、图标档不渲染组头、缩略图目标唯一来源
            Check.True(ViewRules.RendersGroupHeader(ViewMode.Details), "details renders group header");
            Check.False(ViewRules.RendersGroupHeader(ViewMode.Tiles), "tiles no group header");
            Check.True(ViewRules.IsIconMode(ViewMode.ExtraLarge), "extra large is icon mode");
            Check.False(ViewRules.IsIconMode(ViewMode.Details), "details not icon mode");
            Check.Equal(96, ViewRules.ThumbnailTarget(ViewMode.Large), "large target 96");
            Check.True(ViewRules.NeedsThumbnail(ViewMode.Tiles), "tiles needs thumbnail");
            Check.False(ViewRules.NeedsThumbnail(ViewMode.List), "list no thumbnail");
            Check.True(ViewRules.OnlyDetailsRendersGroupHeader(), "exactly one mode renders header");
            Check.Equal(4, ViewRules.IconModeCount(), "four icon modes");
            Check.True(ViewRules.IsKnownMode(6), "last mode known");
            Check.False(ViewRules.IsKnownMode(7), "mode out of range");
            Check.True(ViewRules.GroupHeaderIsDetailsOnly(ViewMode.Details), "details header ok");
            Check.True(ViewRules.GroupHeaderIsDetailsOnly(ViewMode.Small), "small no header ok");
            return true;
        }

        private static bool TestCellMetrics()
        {
            VisibleSpan items = new VisibleSpan(SpanUnit.Items, 0, 10);
            VisibleSpan pixels = new VisibleSpan(SpanUnit.Pixels, 0, 100);
            Check.False(CellMetrics.CanIntersect(items, pixels), "unit mix rejected");
            Check.True(CellMetrics.CanIntersect(items, new VisibleSpan(SpanUnit.Items, 0, 5)), "same unit");
            Check.True(CellMetrics.Within(items, 10), "within total");
            Check.False(CellMetrics.Within(new VisibleSpan(SpanUnit.Items, 8, 5), 10), "overflow");
            Check.Equal(-1, CellMetrics.FirstVisibleIndex(items, 16, 100), "items unit not converted");
            Check.Equal(0, CellMetrics.FirstVisibleIndex(pixels, 16, 100), "pixels conversion");
            Check.Equal(32, CellMetrics.CellSize(ViewMode.List), "cell size from ViewSpecs");
            return true;
        }

        private static bool TestMenuModel()
        {
            Check.True(MenuModel.IsOwnId(0x8000), "own id base");
            Check.False(MenuModel.IsOwnId(1), "system id not own");
            Check.True(MenuModel.IsValidInvokeId(1, 100), "lowest valid id");
            Check.False(MenuModel.IsValidInvokeId(0, 100), "zero id rejected");
            Check.False(MenuModel.IsValidInvokeId(101, 100), "id above maxId rejected");
            Check.True(MenuModel.IsValidInvokeId(0x8000, 0x9000), "own id within maxId");
            Check.False(MenuModel.IsValidInvokeId(0x8000, 100), "own id outside maxId rejected");
            Check.True(MenuModel.OwnSegmentDisjoint(100), "segments disjoint");
            Check.True(MenuModel.IsEquivalentSystemVerb("&Copy..."), "normalized equivalent verb");

            string[] system = new string[] { "&Copy", "Paste...", "Properties" };
            string[] own = new string[] { "copy", "用记事本打开", "Paste" };
            string[] resolved = MenuModel.ResolveOwnVerbs(system, own);
            Check.Equal(1, resolved.Length, "source-level dedup");
            Check.Equal("用记事本打开", resolved[0], "independent group survives");
            Check.False(MenuModel.HasDuplicateNormalized(resolved), "no duplicate normalized names");
            Check.True(MenuModel.HasDuplicateNormalized(new string[] { "A", "a", "&A" }), "duplicate detected");
            Check.False(MenuModel.HasDuplicateNormalized(new string[] { "A", "B" }), "distinct names");
            return true;
        }

        private static bool TestSafety()
        {
            Check.True(SafetyGuard.IsMatrixComplete(), "matrix complete");
            Check.Equal(OpPlanBuilder.KindCount, SafetyGuard.RowCount, "one row per kind");
            Check.Equal(4, SafetyGuard.ColumnCount, "four target classes");
            for (int k = 0; k < SafetyGuard.RowCount; k++)
            {
                for (int t = 0; t < SafetyGuard.ColumnCount; t++)
                {
                    DangerLevel level = SafetyGuard.Cell(k, t);
                    Check.True(SafetyGuard.IsKnownLevel(level), "cell " + k.ToString() + "/" + t.ToString());
                    if (t == (int)GuardTarget.SystemPath)
                        Check.True(level == DangerLevel.Forbidden, "system path cell " + k.ToString());
                    if (t == (int)GuardTarget.Normal)
                    {
                        DangerLevel expect = ((OpKind)k == OpKind.PermanentDelete)
                            ? DangerLevel.TypePath : DangerLevel.Normal;
                        Check.True(level == expect, "normal target cell " + k.ToString());
                    }
                }
            }
            Check.True(SafetyGuard.Decide(OpKind.Delete, GuardTarget.Normal, new GuardFacts(true))
                       == DangerLevel.Forbidden, "read-only precondition wins");
            Check.True(SafetyGuard.Decide((OpKind)99, GuardTarget.Normal, null)
                       == DangerLevel.Forbidden, "unknown kind forbidden");
            Check.True(SafetyGuard.Decide(OpKind.Copy, (GuardTarget)99, null)
                       == DangerLevel.Forbidden, "unknown target forbidden");
            return true;
        }

        private static bool TestEnums()
        {
            Check.True(StateMachine.IsEnumComplete(), "state enums complete");
            Check.True(ErrorModel.IsComplete(), "error enums complete");
            Check.Equal(13, ErrorModel.Count, "error class count");
            Check.Equal(8, OpPlanBuilder.KindCount, "op kind count");
            Check.Equal(4, OpPlanBuilder.PolicyCount, "collision policy count");
            Check.Equal(7, ViewSpecs.Count, "view mode count");
            for (int i = 0; i < ErrorModel.Count; i++)
            {
                Check.NotNull(ErrorModel.For((ErrorClass)i), "error spec " + i.ToString());
            }
            Check.True(OpPlanBuilder.IsKindKnown(7), "last kind known");
            Check.False(OpPlanBuilder.IsKindKnown(8), "kind out of range");

            // B4 enum-*：4096/批、可取消、迟到批次丢弃、虚拟滚动
            Check.Equal(4096, EnumBatchRules.DefaultBatchSize, "4096 per batch");
            Check.False(EnumBatchRules.IsValidBatchSize(0), "zero batch size refused");
            Check.Equal(3, EnumBatchRules.BatchCount(10000, 4096), "ceil batch count");
            Check.Equal(0, EnumBatchRules.BatchCount(0, 4096), "empty dir no batch");
            Check.Equal(8192, EnumBatchRules.BatchStart(2, 4096), "batch start");
            Check.Equal(4096, EnumBatchRules.BatchLength(0, 10000, 4096), "full batch length");
            Check.Equal(1808, EnumBatchRules.BatchLength(2, 10000, 4096), "tail batch length");
            Check.True(EnumBatchRules.IsLateBatch(3, 4), "stale generation detected");
            Check.False(EnumBatchRules.ShouldApplyBatch(3, 4, false), "stale batch dropped");
            Check.False(EnumBatchRules.ShouldApplyBatch(4, 4, true), "cancelled batch dropped");
            Check.True(EnumBatchRules.ShouldApplyBatch(4, 4, false), "current batch applied");
            Check.True(EnumBatchRules.StopsAfterCancel(true), "cancel stops batches");
            Check.Equal(30, EnumBatchRules.RealizedRows(10000, 30), "only viewport rows realized");
            Check.Equal(10, EnumBatchRules.RealizedRows(10, 30), "small dir fully realized");
            Check.True(EnumBatchRules.IsVirtualized(10000, 30), "large dir virtualized");
            Check.False(EnumBatchRules.IsVirtualized(10, 30), "small dir not virtualized");
            return true;
        }

        private static bool TestSort()
        {
            string[,] pairs = new string[,]
            {
                { "file2.txt", "file10.txt" },
                { "a2", "a10" },
                { "a01", "a1" },
                { "A", "a" },
                { "item9", "item10" },
                { "v1.2", "v1.10" },
                { "x", "x0" }
            };
            for (int i = 0; i < pairs.GetLength(0); i++)
            {
                Check.True(NaturalComparer.Instance.Compare(pairs[i, 0], pairs[i, 1]) < 0,
                           "order pair " + i.ToString());
                Check.True(NaturalComparer.Instance.Compare(pairs[i, 1], pairs[i, 0]) > 0,
                           "reverse pair " + i.ToString());
            }
            Check.Equal(0, NaturalComparer.Instance.Compare("same.txt", "same.txt"), "equal");
            Check.True(NaturalComparer.Instance.Compare("a999999999999999999999999",
                                                       "a1000000000000000000000000") < 0,
                       "very long digit run");

            List<string> rows = new List<string>(new string[]
            {
                "file10.txt", "a10", "file2.txt", "a2"
            });
            rows.Sort(NaturalComparer.Instance);
            Check.Equal("a2", rows[0], "sorted 0");
            Check.Equal("a10", rows[1], "sorted 1");
            Check.Equal("file2.txt", rows[2], "sorted 2");
            Check.Equal("file10.txt", rows[3], "sorted 3");

            // B4 sort-*：列头点击切换升降序、换列回升序、名称列走自然排序
            Check.Equal(4, SortRules.FieldCount, "four sort fields");
            SortSpec sdef = SortRules.Default();
            Check.Equal(SortField.Name, sdef.Field, "default sort by name");
            Check.Equal(SortDirection.Ascending, sdef.Direction, "default ascending");
            SortSpec sflip = SortRules.OnHeaderClick(sdef, SortField.Name);
            Check.Equal(SortDirection.Descending, sflip.Direction, "same column flips to descending");
            SortSpec sback = SortRules.OnHeaderClick(sflip, SortField.Name);
            Check.Equal(SortDirection.Ascending, sback.Direction, "same column flips back");
            SortSpec snew = SortRules.OnHeaderClick(sflip, SortField.Date);
            Check.Equal(SortField.Date, snew.Field, "new column selected");
            Check.Equal(SortDirection.Ascending, snew.Direction, "new column resets to ascending");
            Check.True(SortRules.UsesNaturalComparer(SortField.Name), "name uses natural comparer");
            Check.False(SortRules.UsesNaturalComparer(SortField.Size), "size not natural comparer");
            Check.True(SortRules.CompareNamesWithDirection("a2", "a10", SortDirection.Ascending) < 0, "asc natural");
            Check.True(SortRules.CompareNamesWithDirection("a2", "a10", SortDirection.Descending) > 0, "desc natural");
            Check.Equal(-3, SortRules.ApplyDirection(3, SortDirection.Descending), "direction flips sign");
            return true;
        }

        private static bool TestState()
        {
            Check.True(StateMachine.CanTransition(OperationGateState.Idle, OperationGateState.Prechecking), "idle to precheck");
            Check.False(StateMachine.CanTransition(OperationGateState.Idle, OperationGateState.Running), "no skipping");
            Check.True(StateMachine.CanTransition(OperationGateState.Running, OperationGateState.Cancelled), "cancel while running");
            Check.False(StateMachine.CanTransition(OperationGateState.Cancelled, OperationGateState.Idle), "cancel waits for Reporting");
            Check.True(StateMachine.CanTransition(OperationGateState.Cancelled, OperationGateState.Reporting), "cancel to reporting");
            Check.True(StateMachine.IsChangeBlocked(OperationGateState.Running), "busy blocks changes");
            Check.False(StateMachine.IsChangeBlocked(OperationGateState.Idle), "idle allows changes");
            Check.True(StateMachine.IsNavigationAllowed(OperationGateState.Running), "navigation stays available");
            Check.True(StateMachine.IsStaleDiscarded(PaneLoadState.Stale), "stale silently dropped");
            Check.False(StateMachine.IsStaleDiscarded(PaneLoadState.Ready), "ready not dropped");
            Check.False(StateMachine.CanTransition(JobState.Failed, JobState.Queued), "negative cache no retry");
            Check.True(StateMachine.CanTransition(MenuSessionState.Pending, MenuSessionState.Crashed), "sentinel crash");
            Check.True(StateMachine.CanTransition(MenuSessionState.Crashed, MenuSessionState.Pending), "sentinel retry");
            Check.True(StateMachine.CanTransition(ShellAvailabilityState.Probe, ShellAvailabilityState.ReadOnly), "shell read-only degrade");
            return true;
        }

        private static bool TestError()
        {
            Check.True(ErrorModel.IsComplete(), "every class has a spec");
            ErrorSpec tooLong = ErrorModel.For(ErrorClass.PathTooLong);
            Check.True(tooLong.Retryable.HasValue && !tooLong.Retryable.Value, "not retryable");
            Check.True(tooLong.EntersUndoStack.HasValue && !tooLong.EntersUndoStack.Value, "no undo step");
            Check.True(tooLong.UserText.Length > 0, "user visible text");
            ErrorSpec partial = ErrorModel.For(ErrorClass.PartialSuccess);
            Check.False(partial.EntersUndoStack.HasValue, "undo decided per item");
            Check.True(ErrorModel.For(ErrorClass.Cancelled).Retryable.HasValue &&
                       ErrorModel.For(ErrorClass.Cancelled).Retryable.Value, "cancelled retryable");
            Check.Equal("", ErrorModel.For(ErrorClass.None).UserText, "none has no text");
            Check.True(ErrorModel.IsKnown(12), "last class known");
            Check.False(ErrorModel.IsKnown(13), "class out of range");
            return true;
        }

        private static bool TestOpRecord()
        {
            PathKey src, dst;
            PathKey.TryNormalize(@"D:\Data\a.txt", out src);
            PathKey.TryNormalize(@"D:\Backup\a.txt", out dst);
            OpItem ok = new OpItem(src, dst, 0, "");
            OpItem fail = new OpItem(src, null, -2147024891, "access denied");
            OpRecord record = new OpRecord(new OpItem[] { ok, fail }, 3, false, ErrorClass.PartialSuccess);

            Check.True(OpRecordRules.IsSuccess(ok), "hr >= 0 is success");
            Check.False(OpRecordRules.IsSuccess(fail), "hr < 0 is failure");
            Check.Equal(1, OpRecordRules.SuccessCount(record), "success count");
            Check.Equal(3, OpRecordRules.Denominator(record), "callback count is the only denominator");
            Check.True(OpRecordRules.CanUndo(ok), "known target enters undo");
            Check.False(OpRecordRules.CanUndo(fail), "unknown target does not enter undo");
            Check.True(OpRecordRules.IsConsistent(record), "consistent record");
            string summary = OpRecordRules.Summarize(record);
            Check.True(summary.IndexOf("failed", StringComparison.Ordinal) >= 0, "failure not swallowed");
            Check.True(summary.IndexOf("success=1/3", StringComparison.Ordinal) >= 0, "denominator explicit");
            Check.True(OpRecordRules.IsConsistent(
                new OpRecord(new OpItem[0], 0, true, ErrorClass.Cancelled)), "cancelled record");
            Check.False(OpRecordRules.IsConsistent(
                new OpRecord(new OpItem[0], 0, true, ErrorClass.None)), "cancelled flag must match class");
            return true;
        }

        private static bool TestOpPlan()
        {
            PathKey dir, src, dst;
            PathKey.TryNormalize(@"D:\Data", out dir);
            PathKey.TryNormalize(@"D:\Data\a.txt", out src);
            PathKey.TryNormalize(@"D:\Backup", out dst);
            MenuTarget target = new MenuTarget(new PathKey[] { src }, dir, 1, 123456789L);
            OpPlan plan;

            OpRequest req = new OpRequest(target, OpKind.Copy, new PathKey[] { src },
                                          dst, CollisionPolicy.Ask);
            Check.True(OpPlanBuilder.TryBuild(req, out plan), "valid request");
            Check.Equal(1, plan.StepCount, "one step per source");
            Check.Equal(OpKind.Copy, plan.Kind, "kind preserved");

            Check.False(OpPlanBuilder.TryBuild(
                new OpRequest(target, OpKind.Copy, new PathKey[] { src }, null, CollisionPolicy.Ask),
                out plan), "copy needs destination");
            Check.False(OpPlanBuilder.TryBuild(
                new OpRequest(target, OpKind.Copy, new PathKey[0], dst, CollisionPolicy.Ask),
                out plan), "empty sources");
            Check.False(OpPlanBuilder.TryBuild(
                new OpRequest(target, (OpKind)99, new PathKey[] { src }, dst, CollisionPolicy.Ask),
                out plan), "unknown kind");
            Check.False(OpPlanBuilder.TryBuild(
                new OpRequest(target, OpKind.Copy, new PathKey[] { src }, dst, (CollisionPolicy)99),
                out plan), "unknown policy");
            Check.True(OpPlanBuilder.NeedsDestination(OpKind.Copy), "copy needs target");
            Check.False(OpPlanBuilder.NeedsDestination(OpKind.Delete), "delete needs no target");
            return true;
        }

        private static bool TestSelection()
        {
            PathKey k1, k2, k3;
            PathKey.TryNormalize(@"D:\Data\one.txt", out k1);
            PathKey.TryNormalize(@"D:\Data\two.txt", out k2);
            PathKey.TryNormalize(@"D:\Data\three.txt", out k3);
            RowEntry[] rows = new RowEntry[]
            {
                new RowEntry(null, true, false, false),   // group header
                new RowEntry(k1, false, false, false),    // selectable
                new RowEntry(k2, false, true, false),     // filtered out
                new RowEntry(k3, false, false, true)      // disabled
            };
            Selection selection = SelectionRules.SelectAll(rows);
            Check.Equal(1, selection.VisibleSelected.Count, "Ctrl+A excludes three classes");
            Check.True(k1.Equals(selection.VisibleSelected[0]), "only selectable row picked");
            Check.Equal(2, SelectionRules.StatusBarCount(selection), "status bar counts visible rows");
            Check.True(SelectionRules.CanEnterOperation(selection), "operation source is visible set");
            Check.False(SelectionRules.IsSelectable(rows[0]), "group header not selectable");
            Check.False(SelectionRules.IsSelectable(rows[2]), "filtered row not selectable");
            Check.False(SelectionRules.IsSelectable(rows[3]), "disabled row not selectable");

            // B4 select-*：点选 / Ctrl / Shift 多选、框选仅图标档、Ctrl+A 排除三类
            Check.True(SelectionInput.ExcludesThreeClasses(), "Ctrl+A excludes three classes");
            int[] pt = SelectionInput.Point(5, 2);
            Check.Equal(1, pt.Length, "point selects one");
            Check.Equal(2, pt[0], "point index");
            Check.Equal(0, SelectionInput.Point(5, 9).Length, "point out of range empty");
            int[] c1 = SelectionInput.CtrlToggle(new int[0], 3, 5);
            Check.Equal(1, c1.Length, "ctrl adds");
            Check.Equal(3, c1[0], "ctrl added index");
            int[] c2 = SelectionInput.CtrlToggle(c1, 3, 5);
            Check.Equal(0, c2.Length, "ctrl toggles off");
            int[] c3 = SelectionInput.CtrlToggle(new int[] { 1, 2 }, 4, 5);
            Check.Equal(3, c3.Length, "ctrl appends");
            Check.Equal(4, c3[2], "ctrl appended index");
            int[] sr = SelectionInput.ShiftRange(1, 4, 5);
            Check.Equal(4, sr.Length, "shift range length");
            Check.Equal(1, sr[0], "shift range start");
            Check.Equal(4, sr[3], "shift range end");
            Check.Equal(4, SelectionInput.ShiftRange(4, 1, 5).Length, "shift range reversed");
            Check.Equal(0, SelectionInput.ShiftRange(1, 9, 5).Length, "shift out of range empty");
            Check.True(SelectionInput.MarqueeAllowed(ViewMode.Large), "icon mode marquee");
            Check.False(SelectionInput.MarqueeAllowed(ViewMode.Details), "details no marquee");
            Check.True(SelectionInput.DetailsMarqueeIsNonTarget(), "details marquee is non-target");
            return true;
        }

        private static OpRequest MakeRequest(out PathKey s1, out PathKey s2, out PathKey dest)
        {
            PathKey dir;
            PathKey.TryNormalize(@"D:\A", out dir);
            PathKey.TryNormalize(@"D:\A\one.txt", out s1);
            PathKey.TryNormalize(@"D:\A\two.txt", out s2);
            PathKey.TryNormalize(@"D:\B", out dest);
            List<PathKey> srcs = new List<PathKey>();
            srcs.Add(s1);
            srcs.Add(s2);
            MenuTarget target = new MenuTarget(srcs, dir, 0, 100L);
            return new OpRequest(target, OpKind.Copy, srcs, dest, CollisionPolicy.Ask);
        }

        private static bool TestOps()
        {
            PathKey s1, s2, dest, t1;
            OpRequest req = MakeRequest(out s1, out s2, out dest);
            PathKey.TryNormalize(@"D:\B\one.txt", out t1);

            FakeClock clock = new FakeClock(1000L);
            FakeFileOperator op = new FakeFileOperator();
            op.AddSuccess(s1, t1);
            op.AddFailure(s2, -2147024891, "denied");
            FakeDangerPolicy danger = new FakeDangerPolicy();
            FakePreflight pf = new FakePreflight();
            FakeRefreshSink sink = new FakeRefreshSink();
            OperationCoordinator c = new OperationCoordinator(op, danger, pf, sink, clock);

            Check.False(c.IsChangeBlocked(), "idle gate open");
            Check.True(c.IsNavigationAllowed(), "navigation allowed while busy");
            Check.True(StateMachine.IsChangeBlocked(OperationGateState.Running), "busy gate blocks changes");

            CoordinatorResult r = c.Execute(req);
            Check.True(r.Accepted, "operation accepted");
            Check.Equal(1, op.ExecuteCount, "single execution on fake port");
            Check.Equal(2, r.CallbackCount, "callback count is the denominator");
            Check.Equal(2, r.Record.Items.Count, "structured per-item result");
            Check.Equal(ErrorClass.AccessDenied, r.Error, "partial failure classified by hr");
            Check.True(ResultClassifier.IsPartialSuccess(r.Record), "partial success not swallowed");
            Check.True(r.UndoPushed, "successful item enters undo stack");
            Check.Equal(1, r.RefreshCount, "one broadcast for the whole batch");
            Check.Equal(1, sink.Count, "single refresh sink notification");
            Check.Equal(1, c.BroadcastCount, "no refresh storm");
            Check.Equal(OperationGateState.Idle, r.Gate, "gate returns to idle");
            Check.Equal(1, c.ExecuteCount, "coordinator counted one operation");
            Check.Equal(1, danger.DecideCount, "danger policy consulted once");
            Check.Equal(1, pf.CheckCount, "preflight consulted once");
            Check.True(OpRecordRules.Summarize(r.Record).IndexOf("two.txt") >= 0, "failure listed per item");
            Check.False(OpRecordRules.CanUndo(r.Record.Items[1]), "failed item cannot be undone");

            FakeDangerPolicy reject = new FakeDangerPolicy();
            reject.Verdict = GuardVerdict.Reject;
            FakeFileOperator op2 = new FakeFileOperator();
            OperationCoordinator c2 = new OperationCoordinator(op2, reject, new FakePreflight(), new FakeRefreshSink(), clock);
            CoordinatorResult r2 = c2.Execute(req);
            Check.False(r2.Accepted, "rejected by danger policy");
            Check.Equal(0, op2.ExecuteCount, "no execution on reject");
            Check.Equal(OperationGateState.Idle, c2.State, "gate released after reject");

            FakePreflight ro = new FakePreflight();
            ro.IsReadOnly = true;
            OperationCoordinator c3 = new OperationCoordinator(new FakeFileOperator(), new FakeDangerPolicy(), ro, new FakeRefreshSink(), clock);
            CoordinatorResult r3 = c3.Execute(req);
            Check.False(r3.Accepted, "readonly preflight rejects");
            Check.Equal(ErrorClass.ShellUnavailable, r3.Error, "readonly maps to shell unavailable");
            Check.Equal(OperationGateState.Idle, c3.State, "gate released after preflight");

            FakeFileOperator canc = new FakeFileOperator();
            canc.AddSuccess(s1, t1);
            canc.SetOutcome(true, ErrorClass.Cancelled);
            OperationCoordinator c4 = new OperationCoordinator(canc, new FakeDangerPolicy(), new FakePreflight(), new FakeRefreshSink(), clock);
            CoordinatorResult r4 = c4.Execute(req);
            Check.True(r4.Accepted, "cancel is a reported result");
            Check.Equal(ErrorClass.Cancelled, r4.Error, "cancelled classified");
            Check.False(r4.UndoPushed, "cancelled does not enter undo stack");
            Check.Equal(OperationGateState.Idle, c4.State, "cancel releases gate");
            return true;
        }

        private static bool TestUndo()
        {
            PathKey s1, s2, s3, d1, d2, dest;
            OpRequest req = MakeRequest(out s1, out s2, out dest);
            PathKey.TryNormalize(@"D:\A\three.txt", out s3);
            PathKey.TryNormalize(@"D:\B\one.txt", out d1);
            PathKey.TryNormalize(@"D:\B\two.txt", out d2);

            List<OpItem> items = new List<OpItem>();
            items.Add(new OpItem(s1, d1, 0, ""));
            items.Add(new OpItem(s2, d2, 0, ""));
            items.Add(new OpItem(s3, null, -1, "unknown target"));
            OpRecord rec = new OpRecord(items, 3, false, ErrorClass.PartialSuccess);

            Check.Equal(2, OpRecordRules.SuccessCount(rec), "two successful items");
            Check.Equal(3, OpRecordRules.Denominator(rec), "callback count is denominator");
            Check.False(UndoRules.CanPush(items[2], OpKind.Copy), "unknown target cannot be undone");
            Check.False(UndoRules.CanPush(items[0], OpKind.PermanentDelete), "permanent delete cannot be undone");
            Check.True(UndoRules.CanPushRecord(rec, OpKind.Copy), "record has undoable items");

            UndoStack stack = new UndoStack();
            Check.True(stack.Push(rec, OpKind.Copy, 1L), "push accepted");
            Check.Equal(1, stack.Count, "one batch counts as one step");
            Check.Equal(2, stack.Peek().ItemCount, "only undoable items kept");
            for (int i = 0; i < 25; i++) stack.Push(rec, OpKind.Copy, 1L);
            Check.Equal(20, stack.Count, "capacity 20, session only");
            Check.NotNull(stack.Pop(), "pop returns a step");
            Check.Equal(19, stack.Count, "pop removes one step");

            FakeFileOperator op = new FakeFileOperator();
            OperationCoordinator c = new OperationCoordinator(op, new FakeDangerPolicy(), new FakePreflight(), new FakeRefreshSink(), new FakeClock(1L));
            Check.True(c.UndoProbe.Push(rec, OpKind.Copy, 1L), "shared stack accepts step");
            Check.True(c.UndoLast(), "undo executes reverse call");
            Check.Equal(1, op.ExecuteCount, "undo calls the same port in reverse");
            Check.Equal(0, c.UndoCount, "step consumed after undo");
            Check.False(c.UndoLast(), "empty stack cannot undo");

            OpRequest otherPane = MakeRequest(out s1, out s2, out dest);
            Check.Equal(0, otherPane.Target.PaneId, "snapshot is captured per invocation");
            Check.Equal(20, UndoRules.Capacity, "capacity matches the spec");
            return true;
        }

        private static bool TestNames()
        {
            Check.True(NameRules.IsValidFileName("a.txt"), "plain name valid");
            Check.False(NameRules.IsValidFileName(""), "empty invalid");
            Check.False(NameRules.IsValidFileName("a/b"), "separator invalid");
            Check.False(NameRules.IsValidFileName("a<b"), "angle bracket invalid");
            Check.False(NameRules.IsValidFileName("CON"), "device name invalid");
            Check.False(NameRules.IsValidFileName("a."), "trailing dot invalid");
            Check.False(NameRules.IsValidFileName("a "), "trailing space invalid");
            Check.True(NameRules.HasInvalidChars("a|b"), "pipe detected");

            List<string> existing = new List<string>();
            Check.Equal("a - 副本.txt", NameRules.CopyOf("a.txt", existing), "copy suffix");
            existing.Add("a - 副本.txt");
            Check.Equal("a - 副本 (2).txt", NameRules.CopyOf("a.txt", existing), "copy suffix increments");

            List<string> folders = new List<string>();
            Check.Equal("新建文件夹", NameRules.NewFolder(folders), "first new folder");
            folders.Add("新建文件夹");
            Check.Equal("新建文件夹 (2)", NameRules.NewFolder(folders), "new folder increments");

            Check.Equal(CollisionOutcome.Proceed, NameRules.Resolve(CollisionPolicy.Ask, false), "no conflict proceeds");
            Check.Equal(CollisionOutcome.Ask, NameRules.Resolve(CollisionPolicy.Ask, true), "ask on conflict");
            Check.Equal(CollisionOutcome.Rename, NameRules.Resolve(CollisionPolicy.Rename, true), "rename on conflict");
            Check.Equal(CollisionOutcome.Overwrite, NameRules.Resolve(CollisionPolicy.Overwrite, true), "overwrite on conflict");
            Check.Equal(CollisionOutcome.Skip, NameRules.Resolve(CollisionPolicy.Skip, true), "skip on conflict");
            return true;
        }

        private static bool TestRefresh()
        {
            WatchThrottle throttle = new WatchThrottle();
            Check.True(throttle.Register(@"D:\A", 0L), "first subscription");
            Check.False(throttle.Register(@"D:\A", 0L), "same path subscribes once");
            Check.Equal(1, throttle.SubscriptionCount, "single watcher");
            Check.True(WatchThrottle.IsSingleWatcher(1), "one watcher is enough");
            Check.False(WatchThrottle.IsSingleWatcher(2), "two watchers would storm");

            PathKey p1, p2;
            PathKey.TryNormalize(@"D:\A\one.txt", out p1);
            PathKey.TryNormalize(@"D:\B\two.txt", out p2);
            Check.True(throttle.ShouldBroadcast(new FsEvent(p1, FsEventKind.Changed, 100L)), "first event passes");
            Check.False(throttle.ShouldBroadcast(new FsEvent(p1, FsEventKind.Changed, 100L)), "same path inside debounce suppressed");
            Check.True(throttle.ShouldBroadcast(new FsEvent(p1, FsEventKind.Changed, 100L + WatchThrottle.DebounceTicks + 1)), "after debounce passes");
            Check.True(throttle.ShouldBroadcast(new FsEvent(p2, FsEventKind.Created, 100L)), "other path not swallowed");
            throttle.RecordBroadcast();
            Check.Equal(1, throttle.BroadcastCount, "one broadcast recorded");

            PathKey root, touched, external;
            PathKey.TryNormalize(@"D:\A", out root);
            PathKey.TryNormalize(@"D:\A\one.txt", out touched);
            PathKey.TryNormalize(@"D:\B\other.txt", out external);
            SelfOpGuard guard = new SelfOpGuard();
            guard.Begin(root);
            guard.Touch(touched);
            Check.True(guard.InOperation, "in operation");
            Check.True(guard.ShouldSuppress(touched, 1L), "touched path suppressed");
            Check.False(guard.ShouldSuppress(external, 1L), "external change not swallowed");
            guard.End(500L);
            Check.True(guard.ShouldSuppress(touched, 500L + SelfOpGuard.TrailTicks), "trailing second still suppressed");
            Check.False(guard.ShouldSuppress(touched, 500L + SelfOpGuard.TrailTicks + 1L), "trail expires");
            Check.True(SelfOpGuard.IsTrailExpired(500L, 500L + SelfOpGuard.TrailTicks + 1L), "trail expiry is a pure function");
            return true;
        }

        private static bool TestClipboard()
        {
            PathKey p1, p2;
            PathKey.TryNormalize(@"D:\A\one.txt", out p1);
            PathKey.TryNormalize(@"D:\A\two.txt", out p2);
            List<PathKey> items = new List<PathKey>();
            items.Add(p1);
            items.Add(p2);
            ClipboardData data = new ClipboardData(items, DropEffect.Copy);
            Check.True(ClipboardRules.IsValid(data), "two distinct paths valid");
            Check.Equal(2, data.Items.Count, "immutable copy kept");
            Check.Equal("CF_HDROP", ClipboardRules.FormatName, "system format name");
            Check.True(ClipboardRules.Describe(data).IndexOf("Preferred DropEffect") >= 0, "effect exposed");

            List<PathKey> dup = new List<PathKey>();
            dup.Add(p1);
            dup.Add(p1);
            Check.False(ClipboardRules.IsValid(new ClipboardData(dup, DropEffect.Copy)), "duplicates rejected");
            Check.False(ClipboardRules.IsValid(new ClipboardData(items, DropEffect.None)), "no effect rejected");
            Check.False(ClipboardRules.IsValid(new ClipboardData(new List<PathKey>(), DropEffect.Copy)), "empty rejected");

            Check.True(ClipboardRules.IsInternalDrop(false), "pane to pane goes through coordinator");
            Check.False(ClipboardRules.IsInternalDrop(true), "desktop uses system data object");
            Check.Equal(DropEffect.Move, ClipboardRules.EffectFor(false, true), "shift means move");
            Check.Equal(DropEffect.Copy, ClipboardRules.EffectFor(true, false), "ctrl means copy");
            Check.True(ClipboardRules.NeedsSourceRefresh(true, true, DropEffect.Move), "accepted external move refreshes source");
            Check.False(ClipboardRules.NeedsSourceRefresh(true, true, DropEffect.Copy), "external copy leaves source");
            Check.False(ClipboardRules.NeedsSourceRefresh(true, false, DropEffect.Move), "rejected drop leaves source");
            Check.False(ClipboardRules.NeedsSourceRefresh(false, true, DropEffect.Move), "internal drop handled by coordinator");
            return true;
        }

        private static bool TestCli()
        {
            CliParseResult empty = CliArgs.Parse(new string[0]);
            Check.True(empty.Ok, "no args is valid");
            Check.Equal(CliMode.Normal, empty.Mode, "normal mode");
            Check.True(empty.ParticipatesInSingleInstance, "normal mode uses single instance");

            CliParseResult self = CliArgs.Parse(new string[] { "--selftest" });
            Check.Equal(CliMode.SelfTest, self.Mode, "selftest mode");
            Check.False(self.ParticipatesInSingleInstance, "selftest bypasses single instance");

            CliParseResult diag = CliArgs.Parse(new string[] { "--diagnose" });
            Check.Equal(CliMode.Diagnose, diag.Mode, "diagnose mode");
            Check.False(diag.ParticipatesInSingleInstance, "diagnose bypasses single instance");

            CliParseResult shot = CliArgs.Parse(new string[] { "--shot", "--split", "layout=3,colTop=0" });
            Check.True(shot.Ok, "shot with split valid");
            Check.Equal(CliMode.Shot, shot.Mode, "shot mode");
            Check.Equal("layout=3,colTop=0", shot.ShotSplit, "split payload parsed");

            Check.False(CliArgs.Parse(new string[] { "--split", "x" }).Ok, "split without shot rejected");
            Check.False(CliArgs.Parse(new string[] { "--bogus" }).Ok, "unknown parameter rejected");
            Check.True(CliArgs.LegalParameters().IndexOf("--selftest") >= 0, "legal parameters listed");
            return true;
        }

        private static bool TestPortable()
        {
            Check.True(PortableRoot.IsAllowedRelative("session.json"), "plain leaf allowed");
            Check.False(PortableRoot.IsAllowedRelative("..\\escape"), "parent traversal rejected");
            Check.False(PortableRoot.IsAllowedRelative("C:\\absolute"), "absolute rejected");
            Check.False(PortableRoot.IsAllowedRelative("sub\\leaf"), "nested leaf rejected");
            Check.False(PortableRoot.IsAllowedRelative(""), "empty rejected");
            Check.False(PortableRoot.IsAllowedRelative("CON"), "device name rejected");
            Check.Equal(@"D:\App\data\session.json", PortableRoot.Resolve(@"D:\App", "session.json"), "config root is program local data");
            Check.Equal("", PortableRoot.Resolve(@"D:\App", "..\\escape"), "bad leaf has no path");
            Check.True(PortableRoot.IsUnderConfigRoot(@"D:\App\data\session.json", @"D:\App"), "root check passes");
            Check.False(PortableRoot.IsUnderConfigRoot(@"D:\Else\session.json", @"D:\App"), "outside root rejected");
            Check.True(PortableRoot.IsReadOnlyFallback(false), "unwritable config degrades to readonly");
            Check.False(PortableRoot.IsReadOnlyFallback(true), "writable config stays normal");
            return true;
        }

        private static bool TestLayout()
        {
            Check.Equal(1, LayoutMath.PaneCount(LayoutMode.One), "one pane");
            Check.Equal(2, LayoutMath.PaneCount(LayoutMode.Two), "two panes");
            Check.Equal(3, LayoutMath.PaneCount(LayoutMode.Three), "three panes");
            Check.Equal(4, LayoutMath.PaneCount(LayoutMode.Four), "four panes");

            Check.True(LayoutMath.IsValidSlotTable(LayoutMath.Slots(LayoutMode.One), LayoutMode.One), "1-pane slot table");
            Check.True(LayoutMath.IsValidSlotTable(LayoutMath.Slots(LayoutMode.Two), LayoutMode.Two), "2-pane slot table");
            Check.True(LayoutMath.IsValidSlotTable(LayoutMath.Slots(LayoutMode.Three), LayoutMode.Three), "3-pane slot table");
            Check.True(LayoutMath.IsValidSlotTable(LayoutMath.Slots(LayoutMode.Four), LayoutMode.Four), "4-pane slot table");

            PaneSlot[] three = LayoutMath.Slots(LayoutMode.Three);
            Check.Equal(3, three[0].RowSpan, "3-pane main pane RowSpan=3");
            Check.Equal(1, three[1].RowSpan, "3-pane upper right");
            Check.Equal(1, three[2].RowSpan, "3-pane lower right");

            Check.Equal(0.0, LayoutMath.AnchorFraction(0), "anchor 0");
            Check.Equal(0.25, LayoutMath.AnchorFraction(1), "anchor 1");
            Check.Equal(0.5, LayoutMath.AnchorFraction(2), "anchor 2");
            Check.Equal(0.75, LayoutMath.AnchorFraction(3), "anchor 3");
            Check.Equal(1.0, LayoutMath.AnchorFraction(4), "anchor 4");
            Check.Equal(-1.0, LayoutMath.AnchorFraction(5), "anchor out of range");

            Check.Equal(2, LayoutMath.ResolveActivePane(LayoutMode.Four, 2), "explicit active pane");
            Check.Equal(0, LayoutMath.ResolveActivePane(LayoutMode.Two, 3), "out-of-range active falls back");

            PaneState[] panes = PaneSet.Create();
            Check.True(PaneSet.IsComplete(panes), "complete pane set");
            PaneState[] after = PaneSet.SwitchLayout(panes, LayoutMode.Two, LayoutMode.Four);
            Check.True(PaneSet.LayoutKeepsObjects(panes, after), "layout switch keeps pane objects");
            Check.Equal(4, after.Length, "four objects always allocated");

            Check.True(LayoutMath.IsPaneVisible(LayoutMode.Three, 2), "pane 2 visible in 3-pane");
            Check.False(LayoutMath.IsPaneVisible(LayoutMode.Three, 3), "pane 3 hidden in 3-pane");

            // 三条负向：3 窗格退化成 4 窗格落位 / 重复 paneIndex / 未知档位
            PaneSlot[] broken = new PaneSlot[]
            {
                new PaneSlot(0, 0, 0, 1, 1), new PaneSlot(1, 0, 2, 1, 1), new PaneSlot(2, 2, 2, 1, 1)
            };
            Check.False(LayoutMath.IsValidSlotTable(broken, LayoutMode.Three), "3-pane must not degrade to 4-pane landing");
            PaneSlot[] dup = new PaneSlot[]
            {
                new PaneSlot(0, 0, 0, 3, 1), new PaneSlot(0, 0, 2, 3, 1)
            };
            Check.False(LayoutMath.IsValidSlotTable(dup, LayoutMode.Two), "duplicate pane id rejected");
            Check.False(LayoutMath.IsKnownMode(5), "unknown layout rejected");
            Check.Equal(0, LayoutMath.PaneCount((LayoutMode)5), "unknown layout has no panes");
            return true;
        }

        private static bool TestSplit()
        {
            Check.Equal(0.5, SplitMath.EvenRatio, "even ratio");
            Check.Equal(24.0, SplitMath.SnapThresholdPx, "24px snap threshold");
            Check.Equal(0.25, SplitMath.OffsetOf(0.75), "offset stored relative to even split");
            Check.Equal(0.75, SplitMath.RatioOf(0.25), "ratio from offset");
            Check.True(SplitMath.IsOffsetValid(0.25), "valid offset");
            Check.False(SplitMath.IsOffsetValid(0.6), "offset beyond half rejected");

            Check.Equal(0.0, SplitMath.CollapseTo(true), "collapse to 0");
            Check.Equal(1.0, SplitMath.CollapseTo(false), "collapse to 1");
            Check.True(SplitMath.IsCollapsed(0.0), "0 is collapsed");
            Check.True(SplitMath.IsCollapsed(1.0), "1 is collapsed");
            Check.False(SplitMath.IsCollapsed(0.4), "middle ratio not collapsed");

            Check.Equal(0.0, SplitMath.Snap(0.01, 1000.0), "snap near 0");
            Check.Equal(1.0, SplitMath.Snap(0.99, 1000.0), "snap near 1");
            Check.Equal(0.5, SplitMath.Snap(0.51, 1000.0), "snap near even split");
            Check.Equal(0.35, SplitMath.Snap(0.35, 1000.0), "no snap away from anchors");

            Check.Equal(0.75, SplitMath.Restore(0.25), "restore pre-collapse ratio");
            Check.Equal(0.5, SplitMath.Restore(0.9), "bad offset falls back to even");

            Check.Equal(0, SplitMath.ConnectionCount(LayoutMode.One), "1-pane connections");
            Check.Equal(1, SplitMath.ConnectionCount(LayoutMode.Two), "2-pane connections");
            Check.Equal(2, SplitMath.ConnectionCount(LayoutMode.Three), "3-pane connections");
            Check.Equal(2, SplitMath.ConnectionCount(LayoutMode.Four), "4-pane connections");
            Check.Equal(3, SplitMath.PaneCountAfterCollapse(LayoutMode.Three), "collapse keeps pane objects");
            Check.True(SplitMath.OffsetSurvivesResize(0.2, 800.0, 1600.0), "offset survives resize");

            // 五条负向：比例越界 / 吸附输入非法 / offset 越界 / 非法 offset 不可持久化
            Check.False(SplitMath.IsRatioValid(-0.1), "negative ratio rejected");
            Check.False(SplitMath.IsRatioValid(1.1), "ratio above 1 rejected");
            Check.Equal(-1.0, SplitMath.Snap(1.2, 1000.0), "invalid snap input returns sentinel");
            Check.False(SplitMath.IsOffsetValid(-0.75), "offset below -0.5 rejected");
            Check.False(SplitMath.OffsetSurvivesResize(0.9, 800.0, 1600.0), "invalid offset cannot be persisted");
            return true;
        }

        private static bool TestTabs()
        {
            int[] ids = new int[] { 10, 20, 30, 40 };
            int[] moved = TabRules.Reorder(ids, 0, 2);
            Check.NotNull(moved, "reorder produced new array");
            Check.Equal(20, moved[0], "reorder step 1");
            Check.Equal(30, moved[1], "reorder step 2");
            Check.Equal(10, moved[2], "moved id lands at target");
            Check.Equal(40, moved[3], "tail unchanged");
            Check.Equal(10, ids[0], "input array not mutated");
            Check.Equal(30, TabRules.ActiveAfterReorder(30, moved), "active follows id");
            Check.Equal(30, TabRules.ColdStartActive(30, ids), "cold start does not jump to leftmost");
            Check.True(TabRules.ColdStartKeepsActive(30, ids), "cold start keeps active");

            int[] added = TabRules.Add(ids, 50);
            Check.Equal(5, added.Length, "add tab");
            Check.Equal(50, added[4], "new tab appended");
            Check.Equal(4, ids.Length, "add does not mutate input");
            int[] removed = TabRules.RemoveAt(ids, 1);
            Check.Equal(3, removed.Length, "close tab");
            Check.Equal(30, removed[1], "close shifts tail");
            Check.Equal(30, TabRules.ActiveAfterClose(30, 1, removed), "closing other tab keeps active");
            Check.Equal(10, TabRules.ActiveAfterClose(20, 0, removed), "closing active at head falls back to head");
            Check.Equal(-1, TabRules.ActiveAfterClose(99, 0, new int[0]), "no remaining tab");

            Check.True(TabRules.AreIdsUnique(ids), "ids unique");
            Check.False(TabRules.AreIdsUnique(new int[] { 1, 1 }), "duplicate ids rejected");
            Check.False(TabRules.IsReorderValid(ids, -1, 2), "bad source index rejected");
            Check.True(TabRules.HasAtLeastOne(removed), "at least one tab");
            Check.True(TabRules.AreHistoriesIndependent(new TabInfo[]
            {
                new TabInfo(10, null, 3, ViewMode.Details), new TabInfo(20, null, 0, ViewMode.List)
            }), "histories independent");
            return true;
        }

        private static bool TestNav()
        {
            Check.True(NavRules.ClearsFilterOn(NavAction.Navigate), "navigate clears filter");
            Check.True(NavRules.ClearsFilterOn(NavAction.Back), "back clears filter");
            Check.True(NavRules.ClearsFilterOn(NavAction.Forward), "forward clears filter");
            Check.False(NavRules.ClearsFilterOn(NavAction.RefreshInPlace), "F5 keeps filter");
            Check.True(NavRules.KeepsViewportOn(NavAction.RefreshInPlace), "F5 keeps viewport");
            Check.True(NavRules.KeepsSelectionOn(NavAction.RefreshInPlace), "F5 keeps selection");
            Check.False(NavRules.KeepsViewportOn(NavAction.Navigate), "navigate resets viewport");
            Check.Equal(6, NavRules.NextGeneration(5), "generation advances");
            Check.Equal(1, NavRules.NextGeneration(int.MaxValue), "generation wraps");
            Check.True(NavRules.IsLateBatchDiscarded(4, 5), "late batch discarded");
            Check.False(NavRules.ShouldApplyBatch(4, 5), "late batch not applied");
            Check.True(NavRules.AppliesOnlyToActivePane(2, 2), "active pane only");
            Check.False(NavRules.AppliesOnlyToActivePane(1, 2), "other pane not targeted");
            Check.True(NavRules.CanGoBack(2), "can go back");
            Check.False(NavRules.CanGoBack(0), "cannot go back at head");
            Check.True(NavRules.CanGoForward(1, 4), "can go forward");
            Check.False(NavRules.CanGoForward(3, 4), "cannot go forward at tail");
            Check.Equal(1, NavRules.BackIndex(2), "back index");
            Check.Equal(2, NavRules.ForwardIndex(1, 4), "forward index");

            ThisPcEntry[] entries = ThisPc.Compose(new string[] { @"C:\", @"D:\", @"C:\", "bad" });
            Check.Equal(3, entries.Length, "root + two unique drives");
            Check.True(ThisPc.HasRoot(entries), "one synthetic root");
            Check.Equal(2, ThisPc.CountOf(entries, ThisPcKind.Drive), "two drives");
            Check.True(ThisPc.NeedsRealPathIcon(entries[1]), "drive uses real path icon");
            Check.False(ThisPc.GenericIconFallbackAllowed(entries[1]), "drive must not fall back to generic icon");
            Check.True(ThisPc.GenericIconFallbackAllowed(entries[0]), "root may use generic icon");
            return true;
        }

        private static bool TestBookmarks()
        {
            PathKey dir, file;
            PathKey.TryNormalize(@"D:\Data", out dir);
            PathKey.TryNormalize(@"D:\Data\a.txt", out file);
            BookmarkItem good = new BookmarkItem("data", dir, true);
            BookmarkItem notDir = new BookmarkItem("file", file, false);
            BookmarkItem noName = new BookmarkItem("", dir, true);
            Check.True(BookmarkRules.CanAdd(good), "directory accepted");
            Check.False(BookmarkRules.CanAdd(notDir), "file rejected");
            Check.False(BookmarkRules.CanAdd(noName), "empty name rejected");
            Check.False(BookmarkRules.CanAdd(null), "null item rejected");
            Check.Equal(BookmarkState.Unknown, BookmarkRules.StateFor(false, true), "unprobed is unknown");
            Check.Equal(BookmarkState.Ok, BookmarkRules.StateFor(true, true), "probed existing is ok");
            Check.Equal(BookmarkState.Missing, BookmarkRules.StateFor(true, false), "probed missing is missing");

            BookmarkItem[] mixed = new BookmarkItem[] { good, notDir, good, noName };
            BookmarkItem[] kept = BookmarkRules.Sanitize(mixed);
            Check.Equal(2, kept.Length, "bad items dropped, good ones kept");
            Check.Equal(4, mixed.Length, "input not mutated");
            Check.Equal(64, BookmarkRules.MaxItems, "limit 64");
            Check.Equal(64, BookmarkRules.Cap(200), "cap at 64");
            Check.Equal(3, BookmarkRules.RemainingAfterDrop(4, 1), "drop one item leaves three");
            Check.True(BookmarkRules.BadItemIsolated(4, 1), "bad item isolated");
            Check.Equal(2, BookmarkRules.TargetPane(2), "click targets active pane only");
            Check.True(BookmarkRules.IsClickTargetValid(2, 2), "active pane click valid");
            Check.False(BookmarkRules.IsClickTargetValid(1, 2), "other pane click rejected");

            BookmarkItem[] many = new BookmarkItem[70];
            for (int i = 0; i < many.Length; i++) many[i] = good;
            Check.Equal(64, BookmarkRules.Sanitize(many).Length, "64 cap enforced");
            return true;
        }

        private static bool TestSession()
        {
            Check.Equal(2, SessionRules.CurrentSchema, "current schema is 2");
            Check.True(SessionRules.IsSchemaSupported(1), "v1 supported");
            Check.True(SessionRules.IsSchemaSupported(2), "v2 supported");
            Check.False(SessionRules.IsSchemaSupported(3), "v3 not supported");
            Check.Equal(SessionReject.FutureVersion, SessionRules.RejectFor(3), "future version reject");
            Check.True(SessionRules.FutureVersionRejectsWholeFile(SessionRules.RejectFor(3)), "future version rejects whole file");
            Check.Equal(SessionReject.BadSchema, SessionRules.RejectFor(0), "bad schema");
            Check.Equal(SessionReject.None, SessionRules.RejectFor(2), "current schema accepted");
            Check.True(SessionRules.IsV1Compatible(1), "v1 compatible");
            Check.Equal(0, SessionRules.NormalizeInt(999, 0, 4), "out-of-range integer goes to 0 not clamp");
            Check.Equal(3, SessionRules.NormalizeInt(3, 0, 4), "in-range integer unchanged");
            Check.Equal(LayoutMode.One, SessionRules.NormalizeLayout(99), "bad layout normalizes to one");
            Check.Equal(LayoutMode.Three, SessionRules.NormalizeLayout(3), "known layout kept");
            Check.Equal(0.0, SessionRules.NormalizeRatio(1.5), "bad ratio goes to 0");
            Check.Equal(0.4, SessionRules.NormalizeRatio(0.4), "valid ratio kept");
            Check.True(SessionRules.NormalizeBool(1), "bool 1 true");
            Check.False(SessionRules.NormalizeBool(2), "bool 2 false");
            Check.Equal(2, SessionRules.RemainingAfterBadItem(3, 1), "bad field isolated");
            Check.True(SessionRules.BadItemIsolated(3, 1), "not whole file rejected");
            Check.Equal(8, SessionRules.RequiredFields().Length, "session fields complete");
            return true;
        }

        private static bool TestGroup()
        {
            // 分组（§5.9）：组头不进显示行集、图标档不渲染组头、组内不二次排序
            Check.False(GroupRules.HeaderEntersRowSet(), "header never in row set");
            Check.True(GroupRules.HeaderAppearsInDisplayList(), "header lives in display list");
            Check.True(GroupRules.RendersHeader(ViewMode.Details), "details renders header");
            Check.False(GroupRules.RendersHeader(ViewMode.Large), "icon mode no header");
            Check.False(GroupRules.IconModeRendersHeader(ViewMode.ExtraLarge), "icon never renders header");
            Check.Equal(-1, GroupRules.Compare(0, 1, 5), "group order wins over inner");
            Check.Equal(5, GroupRules.Compare(1, 1, 5), "same group keeps inner comparison");
            Check.True(GroupRules.NoSecondSort(-3), "inner comparison unchanged");
            Check.True(GroupRules.GroupHeaderIsNotSelectable(), "header not selectable");
            Check.Equal(13, GroupRules.DisplayListCount(10, 3), "display list = rows + headers");
            Check.Equal(0, GroupRules.DisplayListCount(-1, 3), "negative rows rejected");
            return true;
        }

        private static bool TestFilter()
        {
            // 列头筛选（§5.9）：四列 / 大小五档 / 同列或 / 跨列与 / 导航清 F5 留 / 不写会话
            Check.Equal(4, FilterRules.ColumnCount, "four filter columns");
            Check.Equal(5, FilterRules.BucketCount, "five size buckets");
            Check.True(FilterRules.IsKnownColumn(FilterColumn.Name), "name column known");
            Check.True(FilterRules.SameColumnIsOr(2, new int[] { 1, 2, 3 }), "same column OR hit");
            Check.False(FilterRules.SameColumnIsOr(9, new int[] { 1, 2, 3 }), "same column OR miss");
            Check.True(FilterRules.SameColumnIsOr(2, new int[0]), "empty selection no constraint");
            Check.True(FilterRules.CrossColumnIsAnd(true, true, true, true), "cross column AND pass");
            Check.False(FilterRules.CrossColumnIsAnd(true, false, true, true), "cross column AND fail");
            Check.Equal(SizeBucket.Empty, FilterRules.BucketOf(0), "zero is empty bucket");
            Check.Equal(SizeBucket.Tiny, FilterRules.BucketOf(1024L), "tiny bucket");
            Check.Equal(SizeBucket.Small, FilterRules.BucketOf(2L * 1024L * 1024L), "small bucket");
            Check.Equal(SizeBucket.Medium, FilterRules.BucketOf(100L * 1024L * 1024L), "medium bucket");
            Check.Equal(SizeBucket.Large, FilterRules.BucketOf(2L * 1024L * 1024L * 1024L), "large bucket");
            Check.Equal("0:3", FilterRules.ColumnKey(FilterColumn.Name, 3), "column key format");

            string[][] sel = new string[4][];
            sel[0] = new string[] { "0:1", "0:2" };   // 名称列：两桶「或」
            sel[1] = null;
            sel[2] = null;
            sel[3] = new string[] { "3:4" };          // 大小列：Large
            string[] rowA = new string[] { "0:2", "1:x", "2:y", "3:4" };
            Check.True(FilterRules.Matches(rowA, sel), "name OR + size AND pass");
            string[] rowB = new string[] { "0:9", "1:x", "2:y", "3:4" };
            Check.False(FilterRules.Matches(rowB, sel), "name column miss rejects row");
            string[] rowC = new string[] { "0:2", "1:x", "2:y", "3:0" };
            Check.False(FilterRules.Matches(rowC, sel), "size column miss rejects row");

            Check.True(FilterRules.ClearsOn(NavAction.Navigate), "navigate clears filter");
            Check.True(FilterRules.KeepsOn(NavAction.RefreshInPlace), "F5 keeps filter");
            Check.False(FilterRules.ClearsOn(NavAction.RefreshInPlace), "F5 does not clear");
            Check.False(FilterRules.PersistsToSession(), "filter not written to session");
            Check.True(FilterRules.ArrowVisible(10), "arrow from non-empty full table");
            Check.False(FilterRules.ArrowVisible(0), "empty table no arrow");
            Check.True(FilterRules.SourcesAllUseDisplayRowSet(7, 7, 7, 7, 7), "four sources agree");
            Check.False(FilterRules.SourcesAllUseDisplayRowSet(7, 10, 7, 7, 7), "status reading full table rejected");
            Check.Equal(4, FilterRules.SourceCount(), "four consumers of display row set");
            return true;
        }

        private static bool TestToggle()
        {
            // 显示开关（§5.9）：四开关 16 组合、三态过滤、不回顶、会话持久化、缩略图不重枚举
            Check.Equal(4, ToggleRules.ToggleCount, "four toggles");
            Check.Equal(16, ToggleRules.StateCount, "sixteen combinations");
            ToggleState def = ToggleRules.Default();
            Check.False(def.ShowHidden, "hidden off by default");
            Check.False(def.ShowSystem, "system off by default");
            Check.True(def.ShowExtension, "extension on by default");
            Check.False(def.ShowThumbnail, "thumbnail off by default");
            Check.True(ToggleRules.IsVisible(def, EntryVisibility.Normal), "normal always visible");
            Check.False(ToggleRules.IsVisible(def, EntryVisibility.Hidden), "hidden hidden by default");
            Check.False(ToggleRules.IsVisible(def, EntryVisibility.System), "system hidden by default");
            ToggleState flipped = ToggleRules.Flip(def, DisplayToggle.Hidden);
            Check.True(flipped.ShowHidden, "flipped hidden on");
            Check.True(ToggleRules.IsVisible(flipped, EntryVisibility.Hidden), "hidden now visible");
            Check.False(def.ShowHidden, "flip does not mutate input");
            Check.Equal(4, ToggleRules.CombinationIndex(def), "extension bit only");
            Check.Equal(15, ToggleRules.CombinationIndex(new ToggleState(true, true, true, true)), "all-on index");
            Check.True(ToggleRules.IsKnownCombination(15), "last combination known");
            Check.False(ToggleRules.IsKnownCombination(16), "combination out of range");
            Check.True(ToggleRules.IsKnown(DisplayToggle.Hidden), "known toggle");
            Check.True(ToggleRules.PersistsToSession(), "toggles persist to session");
            Check.Equal(7, ToggleRules.ViewportAnchorAfterToggle(7), "viewport anchor preserved");
            Check.True(ToggleRules.KeepsViewport(7, 7), "toggle keeps viewport");
            Check.False(ToggleRules.KeepsViewport(7, 0), "scroll to top rejected");
            Check.False(ToggleRules.RequiresReenumeration(DisplayToggle.Thumbnail), "thumbnail toggle no reenum");
            Check.True(ToggleRules.RequiresReenumeration(DisplayToggle.Hidden), "hidden toggle reenumerates");
            return true;
        }

        private static bool TestHit()
        {
            // 命中测试（§5.10）：四类结果、三消费方共用、图标档空白与条目分流
            Check.Equal(4, HitTestRules.KindCount, "four hit kinds");
            Check.True(HitTestRules.IsKnown(HitKind.Item), "item known");
            Check.True(HitTestRules.SharedBy(HitKind.Item, HitKind.Item, HitKind.Item), "shared hit");
            Check.False(HitTestRules.SharedBy(HitKind.Item, HitKind.Blank, HitKind.Item), "inconsistent hit rejected");
            Check.Equal(1, HitTestRules.DoubleClickAction(ViewMode.Large, HitKind.Item), "double click item opens");
            Check.Equal(2, HitTestRules.DoubleClickAction(ViewMode.Large, HitKind.Blank), "icon blank clears selection");
            Check.Equal(0, HitTestRules.DoubleClickAction(ViewMode.Details, HitKind.Blank), "details blank no action");
            Check.True(HitTestRules.BlankDiffersFromItem(ViewMode.Large), "icon blank differs from item");
            Check.True(HitTestRules.DetailsBlankIsNoAction(), "details blank is no action");
            Check.True(HitTestRules.IsItemTarget(HitKind.Item), "item is target");
            Check.False(HitTestRules.IsItemTarget(HitKind.GroupHeader), "group header not target");
            Check.False(HitTestRules.IsItemTarget(HitKind.ColumnHeader), "column header not target");
            return true;
        }

        private static bool TestStatus()
        {
            // 状态栏（§5.10）：按筛后可见行计数、选中数、文案来自结构化结果
            Check.Equal(3, StatusRules.FieldCount(), "three status fields");
            PathKey k1, k2;
            PathKey.TryNormalize(@"D:\Data\a.txt", out k1);
            PathKey.TryNormalize(@"D:\Data\b.txt", out k2);
            List<PathKey> picked = new List<PathKey>();
            picked.Add(k1);
            picked.Add(k2);
            Selection sel = new Selection(picked, 5);
            Check.Equal(5, StatusRules.VisibleCount(sel), "visible row count from selection");
            Check.Equal(2, StatusRules.SelectedCount(sel), "selected count");
            Check.Equal(0, StatusRules.VisibleCount(null), "null selection zero");
            Check.True(StatusRules.CanRenderResult(new object()), "structured result renders");
            Check.False(StatusRules.CanRenderResult(null), "null result rejected");
            Check.Equal("", StatusRules.ResultText(null), "null text empty");
            Check.Equal("ok", StatusRules.ResultText("ok"), "text from structured result");
            Check.True(StatusRules.ClaimsSuccess(true, true), "explicit success");
            Check.False(StatusRules.ClaimsSuccess(false, true), "no structured result no success claim");
            Check.True(StatusRules.RejectsInferredSuccess(), "no success inferred from no-exception");
            Check.True(StatusRules.CountMatchesDisplayedSet(5, 5), "count matches displayed set");
            Check.False(StatusRules.CountMatchesDisplayedSet(5, 9), "full-table count rejected");
            return true;
        }

        private static bool TestKeys()
        {
            // 快捷键（§5.10）：13 键、作用范围、忙时行为、表逐键完备
            Check.Equal(13, KeyMap.ActionCount, "thirteen actions");
            Check.True(KeyMap.IsKnown(KeyAction.Copy), "copy known");
            Check.False(KeyMap.IsKnown((KeyAction)13), "action out of range");
            Check.Equal(KeyScope.Global, KeyMap.ScopeOf(KeyAction.Undo), "undo is global");
            Check.Equal(KeyScope.Context, KeyMap.ScopeOf(KeyAction.Escape), "escape is context");
            Check.Equal(KeyScope.ActivePane, KeyMap.ScopeOf(KeyAction.Copy), "copy is active pane");
            Check.Equal(BusyBehavior.DisabledWithHint, KeyMap.BusyBehaviorOf(KeyAction.Copy), "copy busy disabled with hint");
            Check.Equal(BusyBehavior.DisabledNoUndoStep, KeyMap.BusyBehaviorOf(KeyAction.Undo), "undo busy no step");
            Check.Equal(BusyBehavior.Enabled, KeyMap.BusyBehaviorOf(KeyAction.Refresh), "F5 busy enabled");
            Check.True(KeyMap.IsEnabledWhileBusy(KeyAction.Refresh), "F5 available while busy");
            Check.False(KeyMap.IsEnabledWhileBusy(KeyAction.Delete), "delete disabled while busy");
            Check.True(KeyMap.UndoCreatesNoStepWhileBusy(KeyAction.Undo), "undo creates no step while busy");
            Check.False(KeyMap.UndoCreatesNoStepWhileBusy(KeyAction.Copy), "non-undo not this rule");
            Check.True(KeyMap.IsTableComplete(), "key table complete");
            Check.Equal(5, KeyMap.EnabledWhileBusyCount(), "five read-only actions stay enabled");
            Check.True(KeyMap.IsReadOnlyAction(KeyAction.SelectAll), "select all is read-only");
            Check.False(KeyMap.IsReadOnlyAction(KeyAction.Delete), "delete is not read-only");
            return true;
        }

        // ---- B5 系统原生右键菜单：§7.1 menu-* 十条门卡 ----

        private static bool TestMenuNativeDefault()
        {
            // menu-native-default：默认路径系统原生；自绘层只在哨兵降级后兜底
            Check.Equal(MenuHostMode.Native, MenuHostRules.DefaultMode(), "native is default");
            Check.True(MenuHostRules.FallbackIsDegradeOnly(MenuHostMode.Native, false), "native always valid");
            Check.True(MenuHostRules.FallbackIsDegradeOnly(MenuHostMode.Fallback, true), "fallback allowed after degrade");
            Check.False(MenuHostRules.FallbackIsDegradeOnly(MenuHostMode.Fallback, false), "fallback not a default");
            Check.True(MenuHostRules.CanRestoreNative(), "one-click restore exists");
            Check.Equal(4, MenuHostRules.StandardStepCount, "four standard steps");
            Check.True(MenuHostRules.UsesStandardSequence(MenuHostRules.Steps()), "standard four-step sequence");
            Check.False(MenuHostRules.UsesStandardSequence(new string[] { "A", "B", "C", "D" }), "unknown sequence rejected");
            Check.False(MenuHostRules.UsesStandardSequence(null), "null sequence rejected");
            Check.Equal(0, MenuHostRules.VerbScanCount(), "zero verb queries");
            Check.Equal(0, MenuHostRules.DeleteItemCount(), "zero item deletion");
            Check.Equal(0, MenuHostRules.MenuTreeRewriteCount(), "zero menu-tree rewrite");
            Check.True(MenuHostRules.ReadsItemTextViaPlainWin32(), "item text via plain Win32 only");
            Check.True(MenuHostRules.ForbidsLazySubmenu(), "lazy submenu forbidden");
            Check.True(MenuHostRules.ForbidsFixedTimeout(), "fixed timeout forbidden");
            Check.True(MenuHostRules.ForbidsStaWorkerPool(), "STA worker pool forbidden");
            Check.True(MenuHostRules.ForbidsNegativeCache(), "negative cache forbidden");
            Check.True(MenuHostRules.ForbidsMenuPointerCache(), "menu pointer cache forbidden");
            Check.True(MenuHostRules.UsesMatureComBindingOnly(), "mature COM binding only");
            return true;
        }

        private static bool TestMenuNoVerbScan()
        {
            // menu-no-verb-scan：产品路径 0 次命令字符串查询；诊断路径四项前置条件
            Check.Equal(0, MenuHostRules.VerbScanCount(), "product path never queries verbs");
            Check.True(MenuInvokeRules.DiagnosticReadAllowed(true, true, true, false), "diagnostic allowed with all guards");
            Check.False(MenuInvokeRules.DiagnosticReadAllowed(false, true, true, false), "same process rejected");
            Check.False(MenuInvokeRules.DiagnosticReadAllowed(true, false, true, false), "unchecked id rejected");
            Check.False(MenuInvokeRules.DiagnosticReadAllowed(true, true, false, false), "no timeout rejected");
            Check.False(MenuInvokeRules.DiagnosticReadAllowed(true, true, true, true), "release package rejected");
            Check.True(MenuInvokeRules.UsesMatureComBinding(), "mature binding only");
            Check.False(MenuInvokeRules.DeclaresRawVtable(), "no hand-written vtable");
            Check.Equal(0, new FakeShellExtension(ProbeBehavior.Ok, 8).ObservedVerbs().Length, "fake observes no verbs");
            Check.False(MenuProbeRules.RequiresRealExtensionInstalled(), "no real extension needed");
            Check.False(MenuProbeRules.RequiresCloudOrSpecialPermission(), "no cloud or special permission needed");
            return true;
        }

        private static bool TestMenuIdRange()
        {
            // menu-id-range：InvokeCommand 出参严格落在 [1, maxId]，偏移量 ∈ [0, maxId]
            Check.Equal(1, MenuInvokeRules.IdCmdFirst, "idCmdFirst is one");
            Check.Equal(0x7FFF, MenuInvokeRules.IdCmdLast, "idCmdLast is shell ceiling");
            Check.Equal(10, MenuInvokeRules.MaxIdFromQueryResult(11), "maxId from query result");
            Check.Equal(0, MenuInvokeRules.MaxIdFromQueryResult(0), "empty menu maxId");
            Check.Equal(0, MenuInvokeRules.MaxIdFromQueryResult(-3), "negative result clamped");
            Check.Equal(0, MenuInvokeRules.InvokeOffset(1), "first offset zero");
            Check.Equal(9, MenuInvokeRules.InvokeOffset(10), "last offset nine");
            Check.True(MenuInvokeRules.OffsetInRange(0, 10), "offset low bound");
            Check.True(MenuInvokeRules.OffsetInRange(10, 10), "offset high bound");
            Check.False(MenuInvokeRules.OffsetInRange(11, 10), "offset above maxId dropped");
            Check.False(MenuInvokeRules.OffsetInRange(-1, 10), "negative offset dropped");
            Check.True(MenuInvokeRules.InvokeIdInRange(1, 10), "lowest system id valid");
            Check.True(MenuInvokeRules.InvokeIdInRange(10, 10), "maxId valid");
            Check.False(MenuInvokeRules.InvokeIdInRange(11, 10), "id above maxId dropped");
            Check.False(MenuInvokeRules.InvokeIdInRange(0, 10), "zero id dropped");
            Check.True(MenuInvokeRules.ShouldDrop(11, 10), "out of range discarded");
            Check.False(MenuInvokeRules.ShouldDrop(1, 10), "valid id kept");
            Check.Equal(64, MenuInvokeRules.BufferChars(64), "buffer sized by cchMax");
            Check.Equal(0, MenuInvokeRules.BufferChars(-1), "negative cchMax rejected");
            Check.True(MenuInvokeRules.UnicodeLengthIsByCharacter(), "unicode length by character");
            return true;
        }

        private static bool TestMenuOwnIdSegment()
        {
            // menu-own-id-segment：自有段 ≥ 0x8000，与 shell 段无交集，派发不串
            Check.Equal(0x8000, MenuModel.OwnIdBase, "own base is 0x8000");
            Check.True(MenuModel.IsOwnId(0x8000), "own base is own");
            Check.False(MenuModel.IsOwnId(0x7FFF), "shell ceiling is not own");
            Check.True(MenuInvokeRules.OwnSegmentDisjoint(), "segments disjoint");
            Check.True(MenuFusionRules.OwnGroupStartsAboveShellCeiling(), "own group above shell ceiling");
            Check.Equal(MenuDispatchKind.System, MenuInvokeRules.Classify(1, 10), "system id to shell");
            Check.Equal(MenuDispatchKind.Own, MenuInvokeRules.Classify(0x8000, 10), "own id to app dispatch");
            Check.Equal(MenuDispatchKind.Dropped, MenuInvokeRules.Classify(11, 10), "out of range dropped");
            Check.Equal(MenuDispatchKind.Dropped, MenuInvokeRules.Classify(0, 10), "zero dropped");
            Check.True(MenuInvokeRules.OwnNeverLeaksToSystem(0x8000, 10), "own id never invokes shell");
            Check.False(MenuInvokeRules.ShouldDrop(0x8000, 10), "own id not dropped by maxId");
            Check.False(MenuInvokeRules.InvokeIdInRange(0x8000, 10), "own id never passed to InvokeCommand");
            Check.Equal(MenuDispatchKind.Own, MenuInvokeRules.Classify(0x8000, 0), "own id valid even with empty shell menu");
            return true;
        }

        private static bool TestMenuNoFusion()
        {
            // menu-no-fusion：源头消重，等价项用系统项，不等价项改名独立分组
            Check.Equal(0, MenuFusionRules.DeleteMenuCount(), "zero delete-item calls");
            Check.Equal(0, MenuFusionRules.InsertShuffleCount(), "zero insert shuffle");
            Check.True(MenuFusionRules.OwnItemsAreIndependentGroup(), "own items in independent group");
            Check.Equal(1, MenuFusionRules.OwnGroupSeparatorCount, "one separator");
            string[] system = new string[] { "&Copy", "Paste...", "Properties" };
            string[] own = MenuFusionRules.Resolve(system, new string[] { "copy", "用记事本打开", "Paste" });
            Check.Equal(1, own.Length, "equivalent items removed at source");
            Check.Equal("用记事本打开", own[0], "non-equivalent item survives");
            Check.True(MenuFusionRules.NoDuplicateAfterFusion(system,
                new string[] { "copy", "用记事本打开", "Paste" }), "no duplicate after fusion");
            Check.True(MenuFusionRules.EquivalentUsesSystemItem(
                new string[] { "Copy", "Paste" }, "copy"), "equivalent uses system item");
            Check.True(MenuFusionRules.NonEquivalentKeptAsIndependentItem(
                new string[] { "Copy", "Paste" }, "复制到另一窗格…"), "renamed item independent");
            Check.True(MenuFusionRules.BlankAreaExceptionKeptAndRenamed(
                new string[] { "Properties" }, "当前文件夹属性"), "blank-area exception renamed");
            return true;
        }

        private static bool TestMenuMsgForward()
        {
            // menu-msg-forward：IContextMenu2/3 转发覆盖 owner-draw 与菜单选择
            Check.Equal(5, MenuMessageRules.MessageCount, "five forwarded messages");
            Check.Equal(5, MenuMessageRules.ForwardedCount(), "all five forwarded");
            Check.True(MenuMessageRules.MustForward(MenuMessage.DrawItem), "draw item forwarded");
            Check.True(MenuMessageRules.MustForward(MenuMessage.MeasureItem), "measure item forwarded");
            Check.False(MenuMessageRules.MustForward((MenuMessage)5), "unknown message not forwarded");
            Check.False(MenuMessageRules.MustForwardUnknown(0x0117), "raw unknown code not forwarded");
            Check.Equal("HandleMenuMsg2", MenuMessageRules.Handler(MenuMessage.MenuSelect), "handler is HandleMenuMsg2");
            Check.True(MenuMessageRules.CoversOwnerDraw(), "owner-draw covered");
            Check.True(MenuMessageRules.CoversMenuSelection(), "menu selection covered");
            Check.True(MenuMessageRules.CoversCommand(), "command covered");
            Check.True(MenuMessageRules.UsesOwnerWindow(), "owner window used");
            Check.True(MenuMessageRules.ForwardsToContextMenu2Or3(), "forwards to IContextMenu2/3");
            Check.True(MenuMessageRules.ForwardsOnlyDuringTracking(), "forward only while tracking");
            return true;
        }

        private static bool TestMenuTargetSnapshot()
        {
            // menu-target-snapshot：派发只用打开右键那一刻的 MenuTarget 快照
            PathKey dir, liveDir;
            PathKey.TryNormalize(@"D:\Data", out dir);
            PathKey.TryNormalize(@"D:\Other", out liveDir);
            List<PathKey> sel = new List<PathKey>();
            sel.Add(dir);
            MenuTarget captured = new MenuTarget(sel, dir, 2, 100L);
            MenuTarget live = new MenuTarget(sel, liveDir, 2, 900L);
            Check.Equal(1, MenuTargetSnapshotRules.SnapshotsPerInvocation(), "one snapshot per invocation");
            Check.True(MenuTargetSnapshotRules.SnapshotIsImmutable(), "snapshot immutable");
            Check.True(MenuTargetSnapshotRules.DispatchUsesSameSnapshot(captured, captured), "same snapshot accepted");
            Check.False(MenuTargetSnapshotRules.DispatchUsesSameSnapshot(captured, live), "different snapshot rejected");
            Check.True(MenuTargetSnapshotRules.LivePathCannotReplaceSnapshot(captured, live), "live path cannot replace snapshot");
            Check.True(MenuTargetSnapshotRules.LivePathCannotReplaceSnapshot(captured, null), "null live keeps snapshot");
            Check.True(MenuTargetSnapshotRules.SnapshotCarriesCaptureTime(captured), "snapshot carries capture time");
            Check.True(MenuTargetSnapshotRules.IsFresh(100L, 150L, 100L), "fresh result kept");
            Check.False(MenuTargetSnapshotRules.IsFresh(100L, 250L, 100L), "stale result rejected");
            Check.False(MenuTargetSnapshotRules.IsFresh(200L, 150L, 100L), "future capture rejected");
            Check.True(MenuTargetSnapshotRules.ShouldDiscardStale(100L, 250L, 100L), "stale discarded");
            Check.False(MenuTargetSnapshotRules.UsesLiveSelection(), "no live selection read");
            Check.True(MenuTargetSnapshotRules.SystemItemDispatchAlsoUsesSnapshot(), "system dispatch uses snapshot too");
            return true;
        }

        private static bool TestMenuSentinel()
        {
            // menu-sentinel：标记残留 → 按 ctxKey 降级 → 一键重试 → 再崩 TTL 延长
            Check.Equal(7, MenuSentinelRules.DefaultTtlDays, "TTL default seven days");
            Check.Equal(30, MenuSentinelRules.MaxTtlDays, "TTL cap thirty days");
            string k = MenuSentinelRules.ContextKey("ext-class", "file-type", "vol-type");
            Check.True(MenuSentinelRules.KeyIsStableForSameContext("ext-class", "file-type", "vol-type"), "ctxKey stable");
            Check.True(MenuSentinelRules.KeyDiffersAcrossContexts("a", "b", "c", "z"), "ctxKey differs per context");
            Check.False(MenuSentinelRules.KeyContainsPathOrFileName(), "ctxKey has no path or file name");
            Check.True(MenuSentinelRules.IsNormalClose(MenuCloseKind.Selected), "selected is normal close");
            Check.True(MenuSentinelRules.IsNormalClose(MenuCloseKind.Esc), "ESC is normal close");
            Check.True(MenuSentinelRules.IsNormalClose(MenuCloseKind.ClickOutside), "outside click is normal close");
            Check.False(MenuSentinelRules.IsNormalClose(MenuCloseKind.Crashed), "crash is not normal close");
            Check.True(MenuSentinelRules.ClearsPendingOnNormalClose(MenuCloseKind.Esc), "ESC clears pending");
            Check.False(MenuSentinelRules.ClearsPendingOnNormalClose(MenuCloseKind.Crashed), "crash leaves pending");
            Check.True(MenuSentinelRules.PendingRemainsAfterCrash(), "pending remains after crash");
            Check.Equal(SentinelAction.Degrade, MenuSentinelRules.OnStartup(true), "residual degrades");
            Check.Equal(SentinelAction.None, MenuSentinelRules.OnStartup(false), "clean startup stays native");
            Check.True(MenuSentinelRules.DegradesByContextKeyOnly(), "degrade by ctxKey only");
            Check.False(MenuSentinelRules.DegradesGlobally(), "no global shutdown");
            Check.True(MenuSentinelRules.OtherContextsStayNative(true, "k1", "k2"), "other contexts stay native");
            Check.False(MenuSentinelRules.OtherContextsStayNative(true, "k1", "k1"), "same context degrades");
            Check.Equal(SentinelAction.Retry, MenuSentinelRules.OnRetry(), "retry action");
            Check.True(MenuSentinelRules.RetryClearsSafeEntry(), "retry clears safe entry");
            Check.Equal(MenuHostMode.Native, MenuSentinelRules.RetryHostMode(), "retry uses native");
            Check.Equal(SentinelAction.ExtendTtl, MenuSentinelRules.OnCrashAgain(), "crash again extends TTL");
            Check.Equal(14, MenuSentinelRules.ExtendedTtlDays(7), "TTL 7 to 14");
            Check.Equal(28, MenuSentinelRules.ExtendedTtlDays(14), "TTL 14 to 28");
            Check.Equal(30, MenuSentinelRules.ExtendedTtlDays(28), "TTL capped at 30");
            Check.Equal(7, MenuSentinelRules.ExtendedTtlDays(0), "invalid TTL falls back to default");
            Check.True(MenuSentinelRules.KeepsHintAfterCrashAgain(), "hint kept after second crash");
            Check.True(MenuSentinelRules.HintHasReasonAndRetry(), "hint has reason and retry");
            Check.False(MenuSentinelRules.EnumeratesExtensions(), "extensions not enumerated");
            Check.False(MenuSentinelRules.IdentifiesConcreteExtension(), "no concrete extension identified");
            Check.False(MenuSentinelRules.NeedsCrashDump(), "no dump needed");
            Check.Equal("type;outcome;elapsed_ms;item_count;hr", MenuSentinelRules.CrashSummaryFields(), "redacted crash fields");
            Check.True(MenuSentinelRules.CrashSummaryIsLocalOnly(), "crash summary local only");
            Check.False(MenuSentinelRules.ClaimsNeverCrash(), "never-crash not claimed");
            Check.True(MenuSentinelRules.SessionStateSupportsRetry(), "Crashed can return to Pending");
            return true;
        }

        private static bool TestMenuSingleFile()
        {
            // menu-single-file：单 exe、零安装、免运行时；隔离宿主为可选演进
            Check.False(MenuHostRules.IntroducesSecondProcess(), "no second process in default path");
            Check.False(MenuHostRules.RequiresInstallationOrRuntime(), "no install or runtime");
            Check.True(MenuHostRules.OptionalHostIsOptIn(), "isolated host is opt-in");
            Check.Equal(MenuHostMode.Native, MenuHostRules.DefaultMode(), "default still native");
            // FakeShellExtension 四态覆盖，不依赖真机装扩展
            Check.Equal(4, MenuProbeRules.BehaviorCount, "four probe behaviors");
            Check.True(MenuProbeRules.CoversAllBehaviors(new ProbeBehavior[]
            {
                ProbeBehavior.Ok, ProbeBehavior.OutOfRange, ProbeBehavior.OwnerDraw, ProbeBehavior.Crash
            }), "four-state matrix covered");
            Check.False(MenuProbeRules.CoversAllBehaviors(new ProbeBehavior[]
            {
                ProbeBehavior.Ok, ProbeBehavior.OutOfRange, ProbeBehavior.OwnerDraw
            }), "incomplete matrix rejected");
            FakeShellExtension ok = new FakeShellExtension(ProbeBehavior.Ok, 10);
            Check.Equal(11, ok.QueryContextMenu(), "fake query result");
            Check.Equal(10, ok.MaxId(), "fake maxId");
            Check.Equal(1, ok.SelectedId(), "fake selected id");
            Check.True(ok.Invoke(1), "valid system id invokes");
            Check.Equal(1, ok.InvokeCount, "one invocation");
            Check.False(ok.Invoke(11), "out-of-range id never invokes");
            Check.Equal(1, ok.InvokeCount, "invocation count unchanged");
            Check.True(ok.ClosesNormally(), "ok closes normally");
            Check.False(ok.DependsOnRealMachine(), "fake independent of real machine");
            FakeShellExtension bad = new FakeShellExtension(ProbeBehavior.OutOfRange, 10);
            Check.Equal(4106, bad.SelectedId(), "out-of-range id");
            Check.True(MenuProbeRules.ShouldDrop(ProbeBehavior.OutOfRange, 10), "out-of-range dropped");
            Check.False(bad.Invoke(bad.SelectedId()), "out-of-range never invokes shell");
            Check.Equal(0, bad.InvokeCount, "no shell invocation on bad id");
            FakeShellExtension draw = new FakeShellExtension(ProbeBehavior.OwnerDraw, 10);
            Check.True(draw.NeedsMessageForward(), "owner-draw needs message forward");
            Check.Equal(MenuModel.OwnIdBase, draw.SelectedId(), "owner-draw uses own segment");
            Check.False(draw.Invoke(draw.SelectedId()), "own id never invokes shell");
            FakeShellExtension crash = new FakeShellExtension(ProbeBehavior.Crash, 10);
            Check.False(crash.ClosesNormally(), "crash does not close normally");
            Check.Equal(MenuCloseKind.Crashed, crash.Close(), "crash close kind");
            Check.False(crash.DependsOnCloudOrPermission(), "fake independent of cloud");
            return true;
        }

        private static bool TestMenuRelease()
        {
            // menu-release：HMENU / COM 每会话 try-finally 释放
            Check.Equal(2, MenuReleaseRules.ResourcesPerSession, "two resources per session");
            Check.True(MenuReleaseRules.UsesTryFinally(), "try-finally used");
            Check.True(MenuReleaseRules.ReleasesOnAllExitPaths(MenuCloseKind.Selected), "release on select");
            Check.True(MenuReleaseRules.ReleasesOnAllExitPaths(MenuCloseKind.Esc), "release on ESC");
            Check.True(MenuReleaseRules.ReleasesOnAllExitPaths(MenuCloseKind.ClickOutside), "release on outside click");
            Check.True(MenuReleaseRules.ReleasesOnAllExitPaths(MenuCloseKind.Crashed), "release on crash");
            Check.Equal(0, MenuReleaseRules.ReleaseCallCount(0), "no session no release");
            Check.Equal(2, MenuReleaseRules.ReleaseCallCount(1), "one session two releases");
            Check.Equal(6, MenuReleaseRules.ReleaseCallCount(3), "three sessions six releases");
            Check.True(MenuReleaseRules.ReleasesMenuHandle(), "HMENU released");
            Check.True(MenuReleaseRules.ReleasesComInterface(), "COM interface released");
            Check.False(MenuReleaseRules.CachesMenuPointerAcrossSessions(), "no menu pointer cache");
            Check.False(MenuReleaseRules.CachesComPointerAcrossSessions(), "no COM pointer cache");
            Check.True(MenuReleaseRules.NewInstancePerInvocation(), "new instance per invocation");
            Check.True(MenuReleaseRules.DestroyMenuCalled(), "DestroyMenu called");
            Check.True(MenuReleaseRules.OrderIsMenuThenCom(MenuReleaseRules.ReleaseOrder()), "menu then COM order");
            Check.False(MenuReleaseRules.OrderIsMenuThenCom(new string[] { "COM", "HMENU" }), "wrong order rejected");
            Check.False(MenuReleaseRules.OrderIsMenuThenCom(null), "null order rejected");
            return true;
        }

        private static bool TestIcons()
        {
            // icon-*：四级通道 / exe 两段式 / jumbo 透明回落 / 盘符逐档取图 / 可见区间单位
            Check.Equal(4, IconChannelRules.ChannelCount, "four icon channels");
            Check.Equal(4, IconChannelRules.Levels().Length, "four declared levels");
            Check.True(IconChannelRules.IsKnown(16), "16 px known");
            Check.True(IconChannelRules.IsKnown(48), "48 px known");
            Check.True(IconChannelRules.IsKnown(96), "96 px known");
            Check.True(IconChannelRules.IsKnown(256), "256 px known");
            Check.False(IconChannelRules.IsKnown(32), "unknown px rejected");
            Check.Equal(256, IconChannelRules.ForView(ViewMode.ExtraLarge), "extra large channel");
            Check.Equal(96, IconChannelRules.ForView(ViewMode.Large), "large channel");
            Check.Equal(48, IconChannelRules.ForView(ViewMode.Medium), "medium channel");
            Check.Equal(16, IconChannelRules.ForView(ViewMode.Details), "details channel");
            Check.Equal(48, IconChannelRules.ResolveJumbo(true), "transparent jumbo falls back");
            Check.Equal(256, IconChannelRules.ResolveJumbo(false), "opaque jumbo stays 256");
            Check.True(IconChannelRules.JumboFallsBack(true), "transparent triggers fallback");
            Check.Equal(2, IconChannelRules.ExeStageCount, "exe icon two stages");
            Check.True(IconChannelRules.IsTwoStageForExe(".EXE"), "exe extension normalized");
            Check.False(IconChannelRules.IsTwoStageForExe("png"), "non-exe single stage");
            Check.Equal(2, IconChannelRules.ExeStages().Length, "two stage names");
            Check.True(IconChannelRules.StageOrderIsGenericThenReal(IconChannelRules.ExeStages()),
                       "generic then real path");
            Check.False(IconChannelRules.StageOrderIsGenericThenReal(
                            new string[] { "real-path", "extension-generic" }), "wrong order rejected");
            Check.True(IconChannelRules.DriveValidAt(48, true, false), "drive differs per level");
            Check.False(IconChannelRules.DriveValidAt(48, true, true), "generic icon rejected");
            Check.False(IconChannelRules.DriveValidAt(32, true, false), "unknown level rejected");
            VisibleSpan items = new VisibleSpan(SpanUnit.Items, 0, 10);
            VisibleSpan pixels = new VisibleSpan(SpanUnit.Pixels, 0, 100);
            Check.True(IconChannelRules.UnitSafe(items, new VisibleSpan(SpanUnit.Items, 0, 5)),
                       "same unit safe");
            Check.True(IconChannelRules.MixedUnitRejected(items, pixels), "mixed units rejected");
            Check.True(IconChannelRules.NeedsThumb(ViewMode.ExtraLarge), "jumbo needs thumb");
            Check.False(IconChannelRules.NeedsThumb(ViewMode.Details), "details needs no thumb");
            return true;
        }

        private static bool TestThumbWhitelist()
        {
            // thumb-whitelist：WIC 内置 14 种、有意缺口、.ts 1 MB 下限、零第三方解码器
            Check.Equal(14, ThumbWhitelistRules.WhitelistCount, "fourteen WIC extensions");
            Check.Equal(14, ThumbWhitelistRules.Extensions().Length, "whitelist length");
            Check.True(ThumbWhitelistRules.IsWhitelisted(".PNG"), "leading dot normalized");
            Check.True(ThumbWhitelistRules.IsWhitelisted("jpg"), "jpeg family allowed");
            Check.False(ThumbWhitelistRules.IsWhitelisted("webp"), "webp needs third party decoder");
            Check.True(ThumbWhitelistRules.IsIntentionalGap("heic"), "heic gap declared");
            Check.True(ThumbWhitelistRules.IsIntentionalGap("pdf"), "pdf gap declared");
            Check.False(ThumbWhitelistRules.IsIntentionalGap("png"), "png is not a gap");
            Check.Equal(1048576L, ThumbWhitelistRules.TsMinBytes, "one mebibyte gate");
            Check.False(ThumbWhitelistRules.SizeGatePassed(1048575L), "below gate rejected");
            Check.True(ThumbWhitelistRules.SizeGatePassed(1048576L), "at gate accepted");
            Check.True(ThumbWhitelistRules.NeedsSizeGate(".TS"), "ts size gate");
            Check.False(ThumbWhitelistRules.NeedsSizeGate("png"), "no gate for images");
            Check.True(ThumbWhitelistRules.TsAllowed(1048576L), "long ts allowed");
            Check.False(ThumbWhitelistRules.TsAllowed(999999L), "short ts has no thumb");
            Check.True(ThumbWhitelistRules.Allows("bmp", 1L), "whitelisted image allowed");
            Check.False(ThumbWhitelistRules.Allows("pdf", 99999999L), "office/pdf rejected");
            Check.False(ThumbWhitelistRules.UsesThirdPartyDecoder(), "zero third party decoder");
            Check.False(ThumbWhitelistRules.UsesNuGet(), "zero NuGet dependency");
            Check.True(ThumbWhitelistRules.BlankRejected("  "), "blank rejected");
            Check.True(ThumbWhitelistRules.BlankRejected(null), "null rejected");
            return true;
        }

        private static bool TestThumbExif()
        {
            // thumb-exif：EXIF 八值方向、越界归一、旋转与翻转判据
            Check.Equal(8, ExifOrientationRules.OrientationCount, "eight orientations");
            Check.Equal(8, ExifOrientationRules.AllValues().Length, "eight values enumerated");
            Check.True(ExifOrientationRules.IsKnown(1), "value 1 known");
            Check.True(ExifOrientationRules.IsKnown(8), "value 8 known");
            Check.False(ExifOrientationRules.IsKnown(0), "value 0 rejected");
            Check.False(ExifOrientationRules.IsKnown(9), "value 9 rejected");
            Check.Equal(1, ExifOrientationRules.Normalize(0), "low out of range falls back");
            Check.Equal(1, ExifOrientationRules.Normalize(99), "high out of range falls back");
            Check.Equal(0, ExifOrientationRules.QuarterTurns(1), "top-left no rotation");
            Check.Equal(2, ExifOrientationRules.QuarterTurns(3), "bottom-right half turn");
            Check.Equal(1, ExifOrientationRules.QuarterTurns(6), "right-top quarter turn");
            Check.Equal(3, ExifOrientationRules.QuarterTurns(8), "left-bottom three quarters");
            Check.True(ExifOrientationRules.FlipHorizontal(2), "top-right mirrored");
            Check.True(ExifOrientationRules.FlipHorizontal(5), "left-top mirrored");
            Check.False(ExifOrientationRules.FlipHorizontal(1), "identity not mirrored");
            Check.True(ExifOrientationRules.IsIdentity(1), "identity detected");
            Check.False(ExifOrientationRules.IsIdentity(6), "rotated not identity");
            Check.False(ExifOrientationRules.ChangesCacheKey(), "orientation does not change key");
            return true;
        }

        private static bool TestThumbQueue()
        {
            // thumb-queue：可见优先单线程、generation 迟到丢弃、JobState 状态机
            Check.Equal(1, ThumbQueueRules.WorkerCount, "single worker");
            Check.True(ThumbQueueRules.IsSingleThreaded(), "single threaded");
            Check.True(ThumbQueueRules.UsesBackgroundWorker(), "background worker");
            Check.True(ThumbQueueRules.VisibleFirst(true, false), "visible first");
            Check.False(ThumbQueueRules.VisibleFirst(false, true), "offscreen not first");
            Check.Equal(3, ThumbQueueRules.PriorityOf(true, true), "visible and selected highest");
            Check.Equal(1, ThumbQueueRules.PriorityOf(false, false), "offscreen lowest");
            Check.True(ThumbQueueRules.ComesFirst(true, 10, false, 1), "visible beats seq");
            Check.True(ThumbQueueRules.ComesFirst(true, 2, true, 5), "same priority by seq");
            Check.False(ThumbQueueRules.ComesFirst(true, 5, true, 2), "later seq loses");
            Check.True(ThumbQueueRules.ShouldApplyResult(3, 3, false), "same generation applied");
            Check.False(ThumbQueueRules.ShouldApplyResult(2, 3, false), "late result dropped");
            Check.False(ThumbQueueRules.ShouldApplyResult(3, 3, true), "cancelled result dropped");
            Check.True(ThumbQueueRules.LateResultDiscarded(2, 3), "late generation detected");
            Check.True(ThumbQueueRules.CanTransition(JobState.Queued, JobState.Running), "queued to running");
            Check.True(ThumbQueueRules.CanTransition(JobState.Running, JobState.Failed), "running to failed");
            Check.False(ThumbQueueRules.CanTransition(JobState.Running, JobState.Queued), "no backward transition");
            Check.True(ThumbQueueRules.FailedNoRetry(JobState.Failed, JobState.Queued), "failed no retry");
            Check.True(ThumbQueueRules.UiThreadOnlyRenders(), "UI thread renders only");
            Check.False(ThumbQueueRules.AllowsLongComCallOnUiThread(), "no long COM on UI thread");
            return true;
        }

        private static bool TestThumbCache()
        {
            // thumb-cache：四段键、i|/v| 隔离、96 MB + 1500 条双闸、负缓存不重试
            Check.Equal(4, ThumbCacheRules.KeyPartCount, "four key parts");
            Check.Equal(4, ThumbCacheRules.KeyParts().Length, "key parts enumerated");
            Check.Equal(1500, ThumbCacheRules.MaxEntries, "entry gate");
            Check.Equal(96L * 1024L * 1024L, ThumbCacheRules.MaxBytes, "byte gate");
            string imageKey = ThumbCacheRules.Key(ThumbCacheRules.ImagePrefix, @"d:\a.png", 96, 7L);
            string videoKey = ThumbCacheRules.Key(ThumbCacheRules.VideoPrefix, @"d:\a.mp4", 96, 7L);
            Check.True(ThumbCacheRules.IsTypeIsolated(imageKey), "image key isolated");
            Check.True(ThumbCacheRules.IsTypeIsolated(videoKey), "video key isolated");
            Check.False(ThumbCacheRules.IsTypeIsolated("x|unknown"), "unknown prefix rejected");
            Check.True(ThumbCacheRules.AreIsolated(imageKey, videoKey), "image and video disjoint");
            Check.False(ThumbCacheRules.AreIsolated(imageKey, imageKey), "same key not a pair");
            Check.True(ThumbCacheRules.WithinEntryGate(1500), "at entry gate");
            Check.False(ThumbCacheRules.WithinEntryGate(1501), "over entry gate");
            Check.True(ThumbCacheRules.WithinByteGate(100663296L), "at byte gate");
            Check.False(ThumbCacheRules.WithinByteGate(100663297L), "over byte gate");
            Check.True(ThumbCacheRules.WithinBothGates(10, 1024L), "both gates green");
            Check.True(ThumbCacheRules.NeedsEviction(1501, 0L), "entry gate triggers eviction");
            Check.True(ThumbCacheRules.NeedsEviction(10, 100663297L), "byte gate triggers eviction");
            Check.Equal(1, ThumbCacheRules.EvictCount(1501), "one surplus entry");
            Check.Equal(0, ThumbCacheRules.EvictCount(1500), "no eviction at cap");
            Check.Equal(ThumbFailure.Missing, ThumbCacheRules.FailureOf(true, false, false), "missing classification");
            Check.Equal(ThumbFailure.NotImage, ThumbCacheRules.FailureOf(false, true, false), "not-image classification");
            Check.Equal(ThumbFailure.DecodeFailed, ThumbCacheRules.FailureOf(false, false, true), "decode classification");
            Check.True(ThumbCacheRules.IsNegativeCache(ThumbFailure.NotImage), "negative cache detected");
            Check.False(ThumbCacheRules.RetryAllowed(ThumbFailure.DecodeFailed), "decode failure not retried");
            Check.True(ThumbCacheRules.FallsBackToTypeIcon(ThumbFailure.Missing), "fallback to type icon");
            Check.True(ThumbCacheRules.NegativeCacheNeverRetries(ThumbFailure.Missing), "negative cache never retries");
            return true;
        }

        private static bool TestThumbVideo()
        {
            // thumb-video：15 容器、系统图像工厂、独立 worker + 8 秒看门狗、负缓存
            Check.Equal(15, VideoThumbRules.ContainerCount, "fifteen containers");
            Check.Equal(15, VideoThumbRules.SupportedContainers().Length, "container list length");
            Check.True(VideoThumbRules.IsSupported(".MP4"), "mp4 supported");
            Check.True(VideoThumbRules.IsSupported("ts"), "ts supported with size gate");
            Check.False(VideoThumbRules.IsSupported("pdf"), "pdf not a container");
            Check.True(VideoThumbRules.UsesSystemImageFactory(), "system image factory");
            Check.True(VideoThumbRules.UsesIndependentWorker(), "independent worker");
            Check.True(VideoThumbRules.HasWatchdog(), "watchdog present");
            Check.Equal(8, VideoThumbRules.TimeoutSeconds, "eight second timeout");
            Check.False(VideoThumbRules.TimedOut(7), "below timeout");
            Check.True(VideoThumbRules.TimedOut(8), "at timeout");
            Check.False(VideoThumbRules.TsAllowed(999999L), "short ts rejected");
            Check.True(VideoThumbRules.TsAllowed(1048576L), "long ts accepted");
            Check.False(VideoThumbRules.Allows("ts", 999999L), "short ts no thumb");
            Check.True(VideoThumbRules.Allows("mp4", 1L), "mp4 no size gate");
            Check.Equal(ThumbFailure.NotImage, VideoThumbRules.Classify(false, true), "not-image classified");
            Check.True(VideoThumbRules.NegativeCacheNoRetry(ThumbFailure.Missing), "missing not retried");
            Check.True(VideoThumbRules.WritesSystemCache(), "system thumbcache write declared");
            Check.False(VideoThumbRules.UsesThirdPartyDecoder(), "no third party decoder");
            return true;
        }

        private static bool TestThumbToggle()
        {
            // thumb-toggle：开关切换不重枚举、保持行集 / 视口 / 选中集
            Check.True(ThumbToggleRules.NoReenumOnThumbToggle(), "no reenumeration");
            Check.False(ThumbToggleRules.ReenumeratesOn(DisplayToggle.Thumbnail), "thumbnail off enum path");
            Check.True(ThumbToggleRules.ReenumeratesOn(DisplayToggle.Hidden), "hidden still reenumerates");
            ToggleState off = ToggleRules.Default();
            Check.False(ThumbToggleRules.Enabled(off), "default off");
            ToggleState on = ThumbToggleRules.Flip(off);
            Check.True(ThumbToggleRules.Enabled(on), "flipped on");
            Check.False(ThumbToggleRules.NeedsThumbForView(off, ViewMode.ExtraLarge), "off needs no thumb");
            Check.True(ThumbToggleRules.NeedsThumbForView(on, ViewMode.ExtraLarge), "on in jumbo needs thumb");
            Check.False(ThumbToggleRules.NeedsThumbForView(on, ViewMode.Details), "details never needs thumb");
            Check.True(ThumbToggleRules.KeepsViewport(7, 7), "viewport kept");
            Check.False(ThumbToggleRules.KeepsViewport(7, 0), "viewport jump rejected");
            Check.True(ThumbToggleRules.KeepsRowSet(100, 100), "row set kept");
            Check.False(ThumbToggleRules.KeepsRowSet(100, 99), "row set change rejected");
            Check.True(ThumbToggleRules.KeepsSelection(3, 3), "selection kept");
            Check.True(ThumbToggleRules.PersistsToSession(), "toggle persisted");
            Check.True(ThumbToggleRules.ReusesCacheWhenReenabled(), "cache reused");
            Check.False(ThumbToggleRules.QueuesNewJobOnToggle(), "no new job on toggle");
            return true;
        }

        private static bool TestThumbNoWrite()
        {
            // thumb-nowrite：唯一显式写入例外、三段取证、动机相同才允许写入
            Check.Equal(1, ThumbWriteRules.ExceptionCount, "exactly one write exception");
            Check.True(ThumbWriteRules.IsSoleException("thumbcache"), "thumbcache is sole exception");
            Check.False(ThumbWriteRules.IsSoleException("settings"), "settings not an exception");
            Check.Equal(3, ThumbWriteRules.EvidenceZoneCount, "three evidence zones");
            Check.Equal(3, ThumbWriteRules.EvidenceZones().Length, "zones enumerated");
            Check.True(ThumbWriteRules.NeedsThreeZoneEvidence(), "three-zone evidence required");
            Check.True(ThumbWriteRules.WriteAllowed(WriteZone.SystemCache, true), "cache write with same motive");
            Check.False(ThumbWriteRules.WriteAllowed(WriteZone.SystemCache, false), "motive differs rejected");
            Check.False(ThumbWriteRules.WriteAllowed(WriteZone.Settings, true), "settings write rejected");
            Check.False(ThumbWriteRules.WriteAllowed(WriteZone.ApplicationFolder, true), "app folder write rejected");
            Check.True(ThumbWriteRules.ZeroSettingsWrites(), "zero settings writes");
            Check.True(ThumbWriteRules.ZeroAppDirectoryWrites(), "zero app directory writes");
            Check.True(ThumbWriteRules.ZeroWritesWithoutSameMotive(), "no write without same motive");
            Check.True(ThumbWriteRules.NoPersistWritesNothing(), "no-persist writes nothing");
            Check.True(ThumbWriteRules.ConfigRootOnlyData(), "config root only data");
            Check.False(ThumbWriteRules.SilentFallbackToUserProfile(), "no silent profile fallback");
            return true;
        }

        private static bool TestCliContract()
        {
            Check.Equal(9, CliContractRules.SpecParameterCount, "spec table has nine parameters");
            Check.Equal(14, CliContractRules.ParameterCount, "fourteen known parameters");
            Check.Equal(14, CliContractRules.Parameters().Length, "parameter list length");
            Check.True(CliContractRules.CountsMatchDeclaration(), "counts match declaration");
            Check.True(CliContractRules.IsKnown("--selftest"), "selftest known");
            Check.True(CliContractRules.IsKnown("--data-dir"), "data-dir known");
            Check.False(CliContractRules.IsKnown("--bogus"), "unknown parameter rejected");
            Check.True(CliContractRules.IsDiagnostic("--selftest"), "selftest is diagnostic");
            Check.True(CliContractRules.IsDiagnostic("--bench"), "bench is diagnostic");
            Check.False(CliContractRules.RequiresValue("--selftest"), "selftest takes no value");
            Check.True(CliContractRules.RequiresValue("--bench"), "bench needs a value");
            Check.True(CliContractRules.RequiresValue("--data-dir"), "data-dir needs a value");
            Check.True(CliContractRules.AllowsOptionalValue("--shot"), "shot value is optional");
            Check.False(CliContractRules.AllowsOptionalValue("--bench"), "bench value is mandatory");
            Check.True(CliContractRules.IsShotModifier("--view"), "view is a shot modifier");
            Check.True(CliContractRules.IsExcludedFromRelease("--thumbs"), "thumbs excluded from package");
            Check.False(CliContractRules.IsExcludedFromRelease("--selftest"), "selftest stays in package");
            Check.True(CliContractRules.LegalParameters().IndexOf("--selftest") >= 0, "legal list contains selftest");
            Check.True(CliContractRules.LegalParameters().IndexOf("--data-dir") >= 0, "legal list contains data-dir");

            Check.True(CliArgs.Parse(new string[] { "--bench", "dir" }).Ok, "bench with value accepted");
            Check.True(CliArgs.Parse(new string[] { "--open-test", "target" }).Ok, "open-test accepted");
            Check.True(CliArgs.Parse(new string[] { "--thumbs", "dir" }).Ok, "thumbs accepted");
            Check.True(CliArgs.Parse(new string[] { "--dumpmenu", "menu" }).Ok, "dumpmenu accepted");
            Check.True(CliArgs.Parse(new string[] { "--data-dir", "dir", "--no-persist" }).Ok, "portable escapes accepted");
            Check.True(CliArgs.Parse(new string[] { "--shot", "out.png" }).Ok, "shot with path accepted");
            Check.True(CliArgs.Parse(new string[] { "--shot", "--view", "details" }).Ok, "shot with modifier accepted");
            Check.False(CliArgs.Parse(new string[] { "--bench" }).Ok, "bench without value rejected");
            Check.False(CliArgs.Parse(new string[] { "--view", "details" }).Ok, "modifier without shot rejected");
            Check.False(CliArgs.Parse(new string[] { "--bogus" }).Ok, "unknown parameter still rejected");
            Check.Equal(CliMode.Diagnose, CliArgs.Parse(new string[] { "--bench", "dir" }).Mode, "bench uses diagnose mode");
            Check.Equal(CliMode.Normal, CliArgs.Parse(new string[] { "--data-dir", "dir" }).Mode, "data-dir keeps normal mode");
            return true;
        }

        private static bool TestCliDiagnosticModes()
        {
            Check.True(SingleInstanceRules.DiagnosticModesBypass(), "diagnostic modes bypass single instance");
            Check.True(CliArgs.Parse(new string[0]).ParticipatesInSingleInstance, "normal mode participates");
            Check.False(CliArgs.Parse(new string[] { "--selftest" }).ParticipatesInSingleInstance, "selftest bypasses");
            Check.False(CliArgs.Parse(new string[] { "--bench", "dir" }).ParticipatesInSingleInstance, "bench bypasses");
            Check.False(CliArgs.Parse(new string[] { "--shot", "out.png" }).ParticipatesInSingleInstance, "shot bypasses");
            Check.False(CliArgs.Parse(new string[] { "--open-test", "target" }).ParticipatesInSingleInstance, "open-test bypasses");
            Check.False(CliArgs.Parse(new string[] { "--thumbs", "dir" }).ParticipatesInSingleInstance, "thumbs bypasses");
            Check.False(CliArgs.Parse(new string[] { "--diagnose" }).ParticipatesInSingleInstance, "diagnose bypasses");
            Check.False(CliArgs.Parse(new string[] { "--shell-menu-host" }).ParticipatesInSingleInstance, "host mode bypasses");
            Check.True(CliArgs.Parse(new string[] { "--no-persist" }).ParticipatesInSingleInstance, "no-persist still participates");
            Check.Equal(2, CliContractRules.ExcludedFromRelease().Length, "two diagnostic tools excluded");
            Check.True(CliContractRules.IsExcludedFromRelease("--dumpmenu"), "dumpmenu not in release package");
            return true;
        }

        private static bool TestCliSelftestOutput()
        {
            Check.Equal(100, SelftestReportRules.MinOutputLines, "minimum 100 lines");
            Check.Equal(120, SelftestReportRules.LineCount(61, 50, 4, 3), "report line count");
            Check.True(SelftestReportRules.MeetsMinimum(120), "120 lines is enough");
            Check.False(SelftestReportRules.MeetsMinimum(99), "99 lines is not enough");
            Check.True(SelftestReportRules.IsVerdictLine("verdict: GREEN"), "verdict line recognised");
            Check.False(SelftestReportRules.IsVerdictLine("groups=61"), "group line is not verdict");
            Check.Equal("verdict: GREEN", SelftestReportRules.VerdictLine(0), "green verdict");
            Check.Equal("verdict: RED (2)", SelftestReportRules.VerdictLine(2), "red verdict");
            Check.Equal(3, SelftestReportRules.ExitCode(3), "exit code equals red count");
            Check.Equal("cli-*: ok=True", SelftestReportRules.GroupLine("cli-*", true), "group line format");
            Check.True(SelftestReportRules.MillisecondsAreReportOnly(), "milliseconds report only");
            Check.True(SelftestReportRules.NoTimingVerdict(), "no timing verdict");
            Check.Equal(6, SelftestReportRules.ReportSections().Length, "six report sections");

            SelftestState clean = new SelftestState(61, 0);
            Check.Equal(57, SelftestState.DetailLineCount(), "detail rows");
            Check.Equal(120, clean.LineCount(), "state line count");
            Check.True(clean.MeetsMinimumLines(), "state output meets minimum");
            Check.Equal("verdict: GREEN", clean.VerdictLine(), "state green verdict");
            SelftestState red = new SelftestState(61, 2);
            Check.Equal("verdict: RED (2)", red.VerdictLine(), "state red verdict");
            Check.Equal(2, red.ExitCode(), "state exit code");
            return true;
        }

        private static bool TestGateInventory()
        {
            Check.Equal(9, GateInventoryRules.GroupCount, "nine gate groups");
            Check.Equal(50, GateInventoryRules.GateCount, "fifty registered gates");
            Check.Equal(9, GateInventoryRules.BatchCount, "nine batches");
            Check.Equal(50, GateInventoryRules.Gates().Length, "gate list length");
            Check.Equal(9, GateInventoryRules.Groups().Length, "group list length");
            Check.Equal(9, GateInventoryRules.BatchIds().Length, "batch list length");
            Check.True(GateInventoryRules.CountsMatchDeclaration(), "counts match declaration");
            Check.True(GateInventoryRules.IsRegistered("menu-sentinel"), "menu sentinel registered");
            Check.True(GateInventoryRules.IsOrphan("bogus-*"), "unknown gate is orphan");
            Check.Equal("B5", GateInventoryRules.BatchFor("menu-sentinel"), "menu gates belong to B5");
            Check.Equal("B6", GateInventoryRules.BatchFor("icon-*"), "icon gates belong to B6");
            Check.Equal("B0", GateInventoryRules.BatchFor("check-trace"), "trace gate belongs to B0");
            Check.Equal("B2/B7", GateInventoryRules.BatchFor("cli-*"), "cli gates belong to B2/B7");
            string[] gates = GateInventoryRules.Gates();
            for (int i = 0; i < gates.Length; i++)
            {
                Check.True(GateInventoryRules.BatchFor(gates[i]).Length > 0, "gate has an owner batch");
            }
            return true;
        }

        private static bool TestLedger()
        {
            Check.Equal(9, LedgerRules.RequiredColumnCount, "nine ledger columns");
            Check.Equal(9, LedgerRules.Columns().Length, "column list length");
            Check.Equal(7, LedgerRules.HeadShaLength, "short head_sha length");
            Check.Equal(4, LedgerRules.EvidenceLevelCount, "four evidence levels");
            Check.Equal(4, LedgerRules.EvidenceLevels().Length, "evidence level list length");
            Check.Equal(3, (int)EvidenceLevel.Unverified, "unverified is a first-class level");
            Check.True(LedgerRules.IsEvidenceLevel("实测"), "local run is evidence");
            Check.True(LedgerRules.IsEvidenceLevel("未验证"), "unverified is explicit");
            Check.False(LedgerRules.IsEvidenceLevel("推断"), "inference is not evidence");
            Check.True(LedgerRules.IsHeadSha("8d90b7a"), "valid head_sha");
            Check.False(LedgerRules.IsHeadSha("8d90b7"), "too short rejected");
            Check.False(LedgerRules.IsHeadSha("xyz1234"), "non hex rejected");
            Check.True(LedgerRules.RequiresHeadSha(), "head_sha mandatory");
            Check.True(LedgerRules.OneRowPerBatch(), "one row per batch");
            Check.True(LedgerRules.ForbidsStreakAsEvidence(), "streak is not evidence");
            Check.True(LedgerRules.UnverifiedMustBeExplicit(), "unverified must be written down");
            Check.Equal(9, LedgerRules.BatchIds().Length, "B0..B8 rows");
            Check.True(LedgerRules.CountsMatchDeclaration(), "counts match declaration");
            return true;
        }

        private static bool TestBuildManifest()
        {
            Check.Equal(7, BuildManifestRules.FieldCount, "seven BUILD.txt fields");
            Check.Equal(7, BuildManifestRules.Fields().Length, "field list length");
            Check.Equal("Warning", BuildManifestRules.DirtyTreeWarning, "dirty tree warning token");
            Check.True(BuildManifestRules.IsComplete(BuildManifestRules.Fields()), "all fields present");
            Check.False(BuildManifestRules.IsComplete(new string[] { "commit", "sha256" }), "partial manifest rejected");
            Check.False(BuildManifestRules.IsComplete(null), "null manifest rejected");
            Check.True(BuildManifestRules.DirtyTreeVerdict(true).IndexOf("Warning") >= 0, "dirty tree writes Warning");
            Check.Equal("clean", BuildManifestRules.DirtyTreeVerdict(false), "clean tree stays clean");
            Check.True(BuildManifestRules.RequiresWarningWhenDirty(), "warning required when dirty");
            Check.True(BuildManifestRules.CountsMatchDeclaration(), "counts match declaration");

            PackageState state = new PackageState("time", "commit", "clean", "size",
                                                 "hash", "resources", "passed");
            Check.Equal(7, state.BuildLines().Length, "build lines emitted");
            Check.True(state.ManifestComplete(), "state manifest complete");
            return true;
        }

        private static bool TestPackageSelfcheck()
        {
            Check.Equal(2, PackageSelfcheckRules.ContentCount, "exe plus BUILD.txt");
            Check.Equal(2, PackageSelfcheckRules.Contents().Length, "content list length");
            Check.True(PackageSelfcheckRules.SingleExecutable(), "single exe package");
            Check.True(PackageSelfcheckRules.IncludesBuildManifest(), "BUILD.txt included");
            Check.True(PackageSelfcheckRules.ExcludesDiagnosticTools(), "diagnostic tools excluded");
            Check.True(PackageSelfcheckRules.NoInstaller(), "no installer");
            Check.True(PackageSelfcheckRules.NoRuntimeRequired(), "no runtime required");
            Check.Equal(2, PackageSelfcheckRules.ExcludedToolCount, "two excluded tools");
            Check.Equal(3, PackageSelfcheckRules.WriteEvidenceZoneCount(), "three write evidence zones");
            Check.True(PackageSelfcheckRules.SanitizedCrashDigest(), "crash digest sanitized");

            PackageState state = new PackageState("time", "commit", "clean", "size",
                                                 "hash", "resources", "passed");
            Check.True(state.SelfcheckPassed(), "package selfcheck passed");
            Check.Equal(2, state.Contents().Length, "state contents");
            Check.Equal(3, state.WriteEvidenceZoneCount(), "state evidence zones");
            return true;
        }

        private static bool TestBudgetTable()
        {
            Check.Equal(4, BudgetTableRules.BudgetCount, "four budget kinds");
            Check.Equal(7, BudgetTableRules.ColumnCount, "seven budget columns");
            Check.Equal(4, BudgetTableRules.Kinds().Length, "kind list length");
            Check.Equal(7, BudgetTableRules.Columns().Length, "column list length");
            Check.Equal(3, (int)BudgetKind.Startup, "startup is fourth kind");
            Check.True(BudgetTableRules.OnlyReportsNumbers(), "budget only reports numbers");
            Check.True(BudgetTableRules.NoThresholdVerdict(), "no threshold verdict");
            Check.False(BudgetTableRules.IsCiGate(), "budget is not a CI gate");
            string line = BudgetTableRules.ReportLine("enumeration", "unmeasured", "未验证");
            Check.True(line.IndexOf("report-only") >= 0, "report line marked report-only");
            Check.False(line.IndexOf("verdict") >= 0, "report line has no verdict");
            Check.True(BudgetTableRules.CountsMatchDeclaration(), "counts match declaration");
            return true;
        }

        private static bool TestCrashDigest()
        {
            Check.Equal(5, CrashDigestRules.FieldCount, "five digest fields");
            Check.Equal(5, CrashDigestRules.Fields().Length, "field list length");
            Check.False(CrashDigestRules.ContainsFullPath(), "no full path");
            Check.False(CrashDigestRules.ContainsFileName(), "no file name");
            Check.True(CrashDigestRules.NoNetworkUpload(), "no network upload");
            Check.True(CrashDigestRules.IsSanitized(), "digest sanitized");
            Check.True(CrashDigestRules.CountsMatchDeclaration(), "counts match declaration");
            return true;
        }

        private static bool TestSingleInstanceMarker()
        {
            Check.Equal("QL", SingleInstanceRules.Marker, "WM_COPYDATA marker");
            Check.True(SingleInstanceRules.MarkerValid("QL"), "valid marker");
            Check.False(SingleInstanceRules.MarkerValid("QX"), "invalid marker rejected");
            Check.Equal(3, SingleInstanceRules.MechanismCount, "three mechanisms");
            Check.Equal(3, SingleInstanceRules.Mechanisms().Length, "mechanism list length");
            Check.True(SingleInstanceRules.ParticipatesInSingleInstance(CliMode.Normal), "normal mode participates");
            Check.False(SingleInstanceRules.ParticipatesInSingleInstance(CliMode.SelfTest), "selftest bypasses");
            Check.False(SingleInstanceRules.ParticipatesInSingleInstance(CliMode.Shot), "shot bypasses");
            Check.True(SingleInstanceRules.DiagnosticModesBypass(), "all diagnostic modes bypass");
            Check.True(SingleInstanceRules.CountsMatchDeclaration(), "counts match declaration");
            return true;
        }
    }
}
