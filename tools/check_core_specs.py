#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
B1/B2/B3/B4/B5/B6/B7 静态契约门卡：Core 纯逻辑层（L0 + 编排 + 布局 + 列表 + 菜单 + 图标缩略图 + 自测 / 出包）与 C# 零依赖 runner 的结构核对。

零第三方依赖。退出码 = 红条数。
  python tools/check_core_specs.py                 # 扫仓库
  python tools/check_core_specs.py --root <目录>   # 负向对照时指向临时副本

运行期的算法正确性由 tests/QuadLite.Tests 的 C# runner 承担（CI 上以 C# 5 编译实跑）；
本脚本只保证「源码齐备 / 语法子集 / 枚举完备 / 门卡组注册」这四类改坏必红的静态事实。
"""
import argparse
import re
import sys
from pathlib import Path

if hasattr(sys.stdout, "reconfigure"):
    sys.stdout.reconfigure(encoding="utf-8")

ROOT = Path(__file__).resolve().parents[1]

CORE_TYPES = {
    "PathKey.cs": r"\bclass\s+PathKey\b",
    "SafeWalk.cs": r"\bclass\s+SafeWalk\b",
    "ColumnLayout.cs": r"\bclass\s+ColumnLayout\b",
    "ViewSpec.cs": r"\bclass\s+ViewSpecs\b",
    "CellMetrics.cs": r"\bclass\s+CellMetrics\b",
    "MenuModel.cs": r"\bclass\s+MenuModel\b",
    "SafetyGuard.cs": r"\bclass\s+SafetyGuard\b",
    "OpPlan.cs": r"\bclass\s+OpPlanBuilder\b",
    "OpRecord.cs": r"\bclass\s+OpRecordRules\b",
    "Selection.cs": r"\bclass\s+SelectionRules\b",
    "StateModel.cs": r"\bclass\s+StateMachine\b",
    "ErrorModel.cs": r"\bclass\s+ErrorModel\b",
    "NaturalSort.cs": r"\bclass\s+NaturalComparer\b",
    # B2：编排层与端口契约（全部零 I/O）
    "Ports.cs": r"\binterface\s+IFileOperator\b",
    "ClipboardData.cs": r"\bclass\s+ClipboardData\b",
    "NamePolicy.cs": r"\bclass\s+NameRules\b",
    "UndoStack.cs": r"\bclass\s+UndoStack\b",
    "SelfOpGuard.cs": r"\bclass\s+SelfOpGuard\b",
    "FsWatchThrottle.cs": r"\bclass\s+WatchThrottle\b",
    "CliArgs.cs": r"\bclass\s+CliArgs\b",
    "PortableRoot.cs": r"\bclass\s+PortableRoot\b",
    "OperationCoordinator.cs": r"\bclass\s+OperationCoordinator\b",
    "ResultClassifier.cs": r"\bclass\s+ResultClassifier\b",
    # B3：布局 / 导航 / 标签 / 会话纯函数层（全部零 I/O）
    "LayoutMath.cs": r"\bclass\s+LayoutMath\b",
    "SplitMath.cs": r"\bclass\s+SplitMath\b",
    "PaneState.cs": r"\bclass\s+PaneSet\b",
    "TabModel.cs": r"\bclass\s+TabRules\b",
    "NavModel.cs": r"\bclass\s+NavRules\b",
    "AddressRules.cs": r"\bclass\s+AddressRules\b",
    "BookmarkRules.cs": r"\bclass\s+BookmarkRules\b",
    "SessionRules.cs": r"\bclass\s+SessionRules\b",
    "ThisPc.cs": r"\bclass\s+ThisPc\b",
    # B4：列表 / 视图功能域规则层（全部零 I/O）
    "ViewRules.cs": r"\bclass\s+ViewRules\b",
    "SortRules.cs": r"\bclass\s+SortRules\b",
    "GroupRules.cs": r"\bclass\s+GroupRules\b",
    "ColumnPrefRules.cs": r"\bclass\s+ColumnPrefRules\b",
    "FilterRules.cs": r"\bclass\s+FilterRules\b",
    "ToggleRules.cs": r"\bclass\s+ToggleRules\b",
    "SelectionInput.cs": r"\bclass\s+SelectionInput\b",
    "EnumBatchRules.cs": r"\bclass\s+EnumBatchRules\b",
    "HitTestRules.cs": r"\bclass\s+HitTestRules\b",
    "StatusRules.cs": r"\bclass\s+StatusRules\b",
    "KeyMap.cs": r"\bclass\s+KeyMap\b",
    # B5：系统原生右键菜单规则层（全部零 I/O）
    "MenuHostRules.cs": r"\bclass\s+MenuHostRules\b",
    "MenuInvokeRules.cs": r"\bclass\s+MenuInvokeRules\b",
    "MenuFusionRules.cs": r"\bclass\s+MenuFusionRules\b",
    "MenuMessageRules.cs": r"\bclass\s+MenuMessageRules\b",
    "MenuTargetSnapshotRules.cs": r"\bclass\s+MenuTargetSnapshotRules\b",
    "MenuSentinelRules.cs": r"\bclass\s+MenuSentinelRules\b",
    "MenuReleaseRules.cs": r"\bclass\s+MenuReleaseRules\b",
    "MenuProbeRules.cs": r"\bclass\s+MenuProbeRules\b",
    # B6：图标 / 缩略图 / 缓存规则层（全部零 I/O）
    "IconChannelRules.cs": r"\bclass\s+IconChannelRules\b",
    "ThumbWhitelistRules.cs": r"\bclass\s+ThumbWhitelistRules\b",
    "ExifOrientationRules.cs": r"\bclass\s+ExifOrientationRules\b",
    "ThumbQueueRules.cs": r"\bclass\s+ThumbQueueRules\b",
    "ThumbCacheRules.cs": r"\bclass\s+ThumbCacheRules\b",
    "VideoThumbRules.cs": r"\bclass\s+VideoThumbRules\b",
    "ThumbToggleRules.cs": r"\bclass\s+ThumbToggleRules\b",
    "ThumbWriteRules.cs": r"\bclass\s+ThumbWriteRules\b",
    # B7：自测装置 / 门卡总表 / 账本 / 出包自检 / 命令行契约（全部零 I/O）
    "CliContractRules.cs": r"\bclass\s+CliContractRules\b",
    "SelftestReportRules.cs": r"\bclass\s+SelftestReportRules\b",
    "GateInventoryRules.cs": r"\bclass\s+GateInventoryRules\b",
    "LedgerRules.cs": r"\bclass\s+LedgerRules\b",
    "BuildManifestRules.cs": r"\bclass\s+BuildManifestRules\b",
    "PackageSelfcheckRules.cs": r"\bclass\s+PackageSelfcheckRules\b",
    "BudgetTableRules.cs": r"\bclass\s+BudgetTableRules\b",
    "CrashDigestRules.cs": r"\bclass\s+CrashDigestRules\b",
    "SingleInstanceRules.cs": r"\bclass\s+SingleInstanceRules\b",
}

EXPECTED_GROUPS = [
    "path-*", "safewalk-*", "cols-*", "view-*", "cell-metrics", "menu-model",
    "safety-*", "enum-*", "sort-*", "state-*", "error-*", "op-record",
    "op-plan", "select-*",
    # B2 新增门卡组
    "op-*", "undo-*", "name-*", "refresh-*", "clipboard-*", "cli-*", "portable-*",
    # B3 新增门卡组
    "layout-*", "split-*", "tab-*", "nav-*", "bookmark-*", "session-*",
    # B4 新增门卡组
    "group-*", "filter-*", "toggle-*", "hit-*", "status-*", "keys-*",
    # B5 新增门卡组（§7.1 menu-* 十条）
    "menu-native-default", "menu-no-verb-scan", "menu-id-range",
    "menu-own-id-segment", "menu-no-fusion", "menu-msg-forward",
    "menu-target-snapshot", "menu-sentinel", "menu-single-file", "menu-release",
    # B6 新增门卡组（附录 C 的 icon-* / thumb-* / thumb-nowrite 三个门卡组展开）
    "icon-*", "thumb-whitelist", "thumb-exif", "thumb-queue",
    "thumb-cache", "thumb-video", "thumb-toggle", "thumb-nowrite",
    # B7 新增门卡组（附录 C F 组的 cli-* / portable-* / single-instance-* 展开）
    "cli-contract", "cli-diagnostic-modes", "cli-selftest-output",
    "portable-gate-inventory", "portable-ledger", "portable-build-manifest",
    "portable-package-selfcheck", "portable-budget-table",
    "portable-crash-digest", "single-instance-marker",
]

# C# 5 语法子集：出现这些构造即判红（DD-01 / NFR-06）
CS6_PLUS = [
    ("string interpolation", re.compile(r'\$"')),
    ("null-conditional", re.compile(r"\?\.")),
    ("nameof", re.compile(r"\bnameof\s*\(")),
    ("out var", re.compile(r"\bout\s+var\b")),
    ("null-coalescing assignment", re.compile(r"\?\?=")),
    ("init-only setter", re.compile(r"\binit\s*;")),
]

ENUM_EXPECT = {
    "ErrorClass": 13,
    "OpKind": 8,
    "CollisionPolicy": 4,
    "ViewMode": 7,
    "DangerLevel": 3,
    "GuardTarget": 4,
    "WalkReject": 5,
    "SpanUnit": 2,
    "PathKind": 3,
    "OperationGateState": 6,
    "MenuSessionState": 5,
    "PaneLoadState": 5,
    "ShellAvailabilityState": 3,
    "JobState": 4,
    # B2 新增枚举
    "GuardVerdict": 4,
    "FsEventKind": 4,
    "DropEffect": 4,
    "CollisionOutcome": 5,
    "CliMode": 5,
    # B3 新增枚举
    "LayoutMode": 4,
    "NavAction": 4,
    "AddressReject": 7,
    "BookmarkState": 3,
    "SessionReject": 4,
    "ThisPcKind": 3,
    # B4 新增枚举
    "SortField": 4,
    "SortDirection": 2,
    "FilterColumn": 4,
    "SizeBucket": 5,
    "DisplayToggle": 4,
    "EntryVisibility": 3,
    "SelectGesture": 5,
    "HitKind": 4,
    "KeyAction": 13,
    "KeyScope": 3,
    "BusyBehavior": 3,
    # B5 新增枚举（Core 层）
    "MenuHostMode": 2,
    "MenuDispatchKind": 3,
    "MenuMessage": 5,
    "ProbeBehavior": 4,
    "MenuCloseKind": 4,
    "SentinelAction": 4,
    # B6 新增枚举（Core 层）
    "IconSize": 4,
    "ExifOrientation": 8,
    "CacheKeyPart": 4,
    "ThumbFailure": 4,
    "WriteZone": 3,
    # B7 新增枚举（Core 层）
    "EvidenceLevel": 4,
    "BudgetKind": 4,
}


def cs_files(root):
    out = []
    for base in ("src/QuadLite.Core", "tests/QuadLite.Tests"):
        d = root / base
        if not d.exists():
            continue
        out.extend(sorted(p for p in d.glob("*.cs") if p.is_file()))
    return out


def enum_members(text, name):
    m = re.search(r"\benum\s+" + re.escape(name) + r"\s*\{([^}]*)\}", text, re.S)
    if not m:
        return None
    body = re.sub(r"//[^\n]*", "", m.group(1))
    return [x.strip() for x in body.split(",") if x.strip()]


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--root", default=str(ROOT), help="待扫描的仓库根")
    args = ap.parse_args()
    root = Path(args.root).resolve()
    errors = []

    def bad(msg):
        errors.append(msg)

    def ok(msg):
        print("  PASS  " + msg)

    print("check-core-specs: root=%s" % root.name)

    # 1. 类型齐备：L0 与 B2 编排层类型各在一个文件里
    core = root / "src" / "QuadLite.Core"
    missing_types = []
    for fname, pattern in sorted(CORE_TYPES.items()):
        p = core / fname
        if not p.exists():
            missing_types.append(fname + " (missing)")
        elif not re.search(pattern, p.read_text(encoding="utf-8", errors="replace")):
            missing_types.append(fname + " (type not found)")
    if missing_types:
        bad("L0 类型不齐备：%s" % "; ".join(missing_types))
    else:
        ok("Core 类型齐备（%d 个文件）" % len(CORE_TYPES))

    files = cs_files(root)
    if not files:
        bad("未找到任何 .cs 源码")
    all_text = "\n".join(p.read_text(encoding="utf-8", errors="replace") for p in files)

    # 2. C# 5 语法子集
    cs6_hits = []
    for p in files:
        text = p.read_text(encoding="utf-8", errors="replace")
        for label, rx in CS6_PLUS:
            m = rx.search(text)
            if m:
                cs6_hits.append("%s: %s (`%s`)" % (p.name, label, m.group(0)))
    if cs6_hits:
        bad("C# 5 语法子集被破坏：%s" % "; ".join(cs6_hits[:5]))
    else:
        ok("C# 5 语法子集（%d 个 .cs）" % len(files))

    # 3. 枚举完备性：声明数量与规格表 / 状态表一致
    enum_bad = []
    for name, expected in sorted(ENUM_EXPECT.items()):
        members = enum_members(all_text, name)
        if members is None:
            enum_bad.append(name + " (not declared)")
        elif len(members) != expected:
            enum_bad.append("%s (%d != %d)" % (name, len(members), expected))
    error_model = root / "src" / "QuadLite.Core" / "ErrorModel.cs"
    if error_model.exists():
        spec_count = len(re.findall(r"new\s+ErrorSpec\s*\(",
                                    error_model.read_text(encoding="utf-8", errors="replace")))
        ec = enum_members(all_text, "ErrorClass")
        if ec is None or spec_count != len(ec):
            enum_bad.append("ErrorClass 规格表 %d 条 != 枚举 %s 项" %
                            (spec_count, "?" if ec is None else len(ec)))
    if enum_bad:
        bad("枚举完备性：%s" % "; ".join(enum_bad))
    else:
        ok("枚举完备性（%d 个枚举 + ErrorClass 规格表）" % len(ENUM_EXPECT))

    # 4. runner 门卡组注册齐备
    program = root / "tests" / "QuadLite.Tests" / "Program.cs"
    if not program.exists():
        bad("C# runner Program.cs 缺失")
    else:
        ptext = program.read_text(encoding="utf-8", errors="replace")
        groups = re.findall(r'\bRun\(\s*"([^"]+)"', ptext)
        missing_groups = [g for g in EXPECTED_GROUPS if g not in groups]
        if missing_groups:
            bad("runner 门卡组缺失：%s" % ", ".join(missing_groups))
        elif len(groups) != len(EXPECTED_GROUPS):
            bad("runner 门卡组数量 %d != %d" % (len(groups), len(EXPECTED_GROUPS)))
        else:
            ok("runner 门卡组注册齐备（%d 组）" % len(groups))
        has_verdict = "verdict:" in ptext or "report.VerdictLine()" in ptext
        has_exit_code = "return failures" in ptext or "report.ExitCode()" in ptext
        if not has_verdict or not has_exit_code:
            bad("runner 缺少 verdict 输出或退出码 = 红条数")
        else:
            ok("runner 输出 verdict / 退出码 = 红条数（B7 起委托 SelftestState）")

    # 5. Core 零 I/O（与 check-boundaries 同源，给出更明确的定位）
    io_hits = []
    for p in files:
        if p.parent.name != "QuadLite.Core":
            continue
        for i, line in enumerate(p.read_text(encoding="utf-8", errors="replace").splitlines(), 1):
            if re.search(r"\busing\s+System\.IO\s*;", line):
                io_hits.append("%s:%d" % (p.name, i))
    if io_hits:
        bad("Core 出现 System.IO：%s" % ", ".join(io_hits))
    else:
        ok("Core 零 I/O 引用")

    if errors:
        for e in errors:
            print("  RED   " + e)
        print("verdict: RED (%d)" % len(errors))
        return len(errors)
    print("verdict: GREEN")
    return 0


if __name__ == "__main__":
    sys.exit(main())
