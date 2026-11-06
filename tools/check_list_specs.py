#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
B4 静态契约门卡：列表 / 视图功能域的零 I/O 规则层与 L4 列表视图状态内核。

零第三方依赖，退出码 = 红条数。
  python tools/check_list_specs.py                 # 扫仓库
  python tools/check_list_specs.py --root <目录>   # 负向对照时指向临时副本

本脚本只核对「B4 Core 类型齐备 / App 状态内核齐备 / 门卡组注册 / 规格关键常量 /
筛选与选中集不变量 / 规格与实现一致 / App 零 I/O / C# 5 语法」这些静态事实。
真实 WPF 列表控件、虚拟化面板、拖放、图标渲染与列头 UI 不在本门卡范围，
一律标「未实现 / 未验证」（见 docs/adr/ADR-v2-006.md）。
"""
import argparse
import re
import sys
from pathlib import Path

if hasattr(sys.stdout, "reconfigure"):
    sys.stdout.reconfigure(encoding="utf-8")

ROOT = Path(__file__).resolve().parents[1]

B4_CORE_TYPES = {
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
}

APP_TYPES = {
    "ListViewState.cs": r"\bclass\s+ListViewState\b",
    "ListViewCommands.cs": r"\bclass\s+ListViewCommands\b",
}

EXPECTED_GROUPS = ["group-*", "filter-*", "toggle-*", "hit-*", "status-*", "keys-*"]

CS6_PLUS = [
    ("string interpolation", re.compile(r'\$"')),
    ("null-conditional", re.compile(r"\?\.")),
    ("nameof", re.compile(r"\bnameof\s*\(")),
    ("out var", re.compile(r"\bout\s+var\b")),
    ("null-coalescing assignment", re.compile(r"\?\?=")),
    ("init-only setter", re.compile(r"\binit\s*;")),
]

# 规格 §5.9 / §5.10 的关键常量与不变量，改为他值即判红（防止实现静默漂移）。
CORE_INVARIANTS = {
    "EnumBatchRules.cs": [
        ("batch size 4096", r"DefaultBatchSize\s*=\s*4096\b"),
    ],
    "FilterRules.cs": [
        ("four filter columns", r"ColumnCount\s*=\s*4\b"),
        ("five size buckets", r"BucketCount\s*=\s*5\b"),
        ("no session persistence", r"PersistsToSession\s*\(\s*\)\s*\{[^}]*return\s+false\s*;"),
    ],
    "ToggleRules.cs": [
        ("four display toggles", r"ToggleCount\s*=\s*4\b"),
        ("session persistence", r"PersistsToSession\s*\(\s*\)\s*\{[^}]*return\s+true\s*;"),
    ],
    "GroupRules.cs": [
        ("header never in row set", r"HeaderEntersRowSet\s*\(\s*\)\s*\{[^}]*return\s+false\s*;"),
    ],
    "HitTestRules.cs": [
        ("four hit kinds", r"KindCount\s*=\s*4\b"),
    ],
    "KeyMap.cs": [
        ("thirteen key actions", r"ActionCount\s*=\s*13\b"),
    ],
}

APP_FORBIDDEN = [
    ("using System.IO", re.compile(r"\busing\s+System\.IO\s*;")),
    ("test double reference", re.compile(r"QuadLite\.(?:Probe|Tests)")),
    ("hand-written COM", re.compile(r"\bComImport\b|\bInterfaceType\b|\[\s*Guid\s*\(")),
    ("verb scan", re.compile(r"GetCommandString")),
]


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--root", default=str(ROOT), help="待扫描的仓库根")
    args = ap.parse_args()
    root = Path(args.root).resolve()
    core = root / "src" / "QuadLite.Core"
    app = root / "src" / "QuadLite.App"
    tests = root / "tests" / "QuadLite.Tests"
    errors = []

    def bad(msg):
        errors.append(msg)

    def ok(msg):
        print("  PASS  " + msg)

    print("check-list-specs: root=%s" % root.name)

    def read(path):
        return path.read_text(encoding="utf-8", errors="replace") if path.exists() else ""

    # 1. B4 Core 类型齐备
    missing = []
    for fname, pattern in sorted(B4_CORE_TYPES.items()):
        p = core / fname
        if not p.exists():
            missing.append(fname + " (missing)")
        elif not re.search(pattern, read(p)):
            missing.append(fname + " (type not found)")
    if missing:
        bad("B4 Core 类型不齐备：%s" % "; ".join(missing))
    else:
        ok("B4 Core 类型齐备（%d 个文件）" % len(B4_CORE_TYPES))

    # 2. App 列表视图状态内核齐备
    missing_app = []
    for fname, pattern in sorted(APP_TYPES.items()):
        p = app / fname
        if not p.exists():
            missing_app.append(fname + " (missing)")
        elif not re.search(pattern, read(p)):
            missing_app.append(fname + " (type not found)")
    if missing_app:
        bad("App 列表视图状态内核不齐备：%s" % "; ".join(missing_app))
    else:
        ok("App 列表视图状态内核齐备（%d 个文件）" % len(APP_TYPES))

    # 3. runner B4 门卡组注册齐备
    program = tests / "Program.cs"
    ptext = read(program)
    groups = re.findall(r'\bRun\(\s*"([^"]+)"', ptext)
    missing_groups = [g for g in EXPECTED_GROUPS if g not in groups]
    if missing_groups:
        bad("runner 缺少 B4 门卡组：%s" % ", ".join(missing_groups))
    else:
        ok("runner B4 门卡组注册齐备（%d 组）" % len(EXPECTED_GROUPS))

    # 4. 规格关键常量与不变量
    inv_bad = []
    for fname, checks in sorted(CORE_INVARIANTS.items()):
        text = read(core / fname)
        for label, rx in checks:
            if not re.search(rx, text, re.S):
                inv_bad.append("%s: %s" % (fname, label))
    if inv_bad:
        bad("B4 规格常量 / 不变量被破坏：%s" % "; ".join(inv_bad))
    else:
        total = sum(len(v) for v in CORE_INVARIANTS.values())
        ok("B4 规格常量 / 不变量齐备（%d 项）" % total)

    # 5. 规格与实现一致：设计文档仍是 v1.2，B4 判据声明未被擅自改动
    design = read(root / "design-v2.md")
    if "| B4 | 列表" not in design or "`view-*` / `sort-*` / `group-*`" not in design:
        bad("规格 §8.1 的 B4 门卡声明被改动，B4 实现需同步")
    elif "已定稿（v1.2）" not in design:
        bad("规格版本不再是 v1.2，B4 实现需同步复核")
    else:
        ok("规格仍为 v1.2 且 B4 判据声明未改（布局方案讨论状态不由本批改动）")

    # 6. App 层分层边界
    app_files = sorted(p for p in app.glob("*.cs") if p.is_file()) if app.exists() else []
    violations = []
    for p in app_files:
        text = read(p)
        for label, rx in APP_FORBIDDEN:
            if rx.search(text):
                violations.append("%s: %s" % (p.name, label))
    if violations:
        bad("App 列表视图状态内核触犯分层边界：%s" % "; ".join(violations))
    else:
        ok("App 状态内核零 I/O / 无测试替身 / 无手写 COM（%d 个 .cs）" % len(app_files))

    # 7. C# 5 语法子集（B4 Core + App + Tests 全扫）
    cs_files = []
    for d in (core, app, tests):
        if d.exists():
            cs_files.extend(sorted(p for p in d.glob("*.cs") if p.is_file()))
    cs6_hits = []
    for p in cs_files:
        text = read(p)
        for label, rx in CS6_PLUS:
            if rx.search(text):
                cs6_hits.append("%s: %s" % (p.name, label))
    if cs6_hits:
        bad("C# 5 语法子集被破坏：%s" % "; ".join(cs6_hits[:5]))
    else:
        ok("C# 5 语法子集（%d 个 .cs）" % len(cs_files))

    print("  N/A   真实 WPF 列表控件 / 虚拟化面板 / 拖放 / 图标渲染 / 列头 UI：未实现 / 未验证")
    if errors:
        for e in errors:
            print("  RED   " + e)
        print("verdict: RED (%d)" % len(errors))
        return len(errors)
    print("verdict: GREEN")
    return 0


if __name__ == "__main__":
    sys.exit(main())
