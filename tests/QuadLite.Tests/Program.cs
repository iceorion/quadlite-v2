using System;
using System.Collections.Generic;
using QuadLite.Core;

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

            Console.WriteLine("groups=21, failed=" + failures.ToString());
            if (failures == 0)
            {
                Console.WriteLine("verdict: GREEN");
            }
            else
            {
                Console.WriteLine("verdict: RED (" + failures.ToString() + ")");
            }
            return failures;
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
    }
}
