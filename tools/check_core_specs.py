#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
B1/B2 静态契约门卡：Core 纯逻辑层（L0 + 编排）与 C# 零依赖 runner 的结构核对。

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
}

EXPECTED_GROUPS = [
    "path-*", "safewalk-*", "cols-*", "view-*", "cell-metrics", "menu-model",
    "safety-*", "enum-*", "sort-*", "state-*", "error-*", "op-record",
    "op-plan", "select-*",
    # B2 新增门卡组
    "op-*", "undo-*", "name-*", "refresh-*", "clipboard-*", "cli-*", "portable-*",
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
        if "verdict:" not in ptext or "return failures" not in ptext:
            bad("runner 缺少 verdict 输出或退出码 = 红条数")
        else:
            ok("runner 输出 verdict / 退出码 = 红条数")

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
