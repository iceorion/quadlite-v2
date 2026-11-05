#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
B3 静态契约门卡：L4 外壳状态内核与布局 / 导航 / 标签 / 会话纯函数层。

零第三方依赖，退出码 = 红条数。
  python tools/check_shell_specs.py                 # 扫仓库
  python tools/check_shell_specs.py --root <目录>   # 负向对照时指向临时副本

本脚本只核对「B3 类型齐备 / App 外壳骨架齐备 / 门卡组注册 / 规格与实现一致 /
四档布局与 3 窗格跨度 / 折叠与吸附常量 / future-version 判定 / 分层边界 / C# 5 语法」
这些静态事实。真实 WPF 窗口、控件、消息循环、系统原生菜单与冲突 / 进度面板不在本门卡
范围，一律标「未实现 / 未验证」（见 docs/adr/ADR-v2-005.md）。
"""
import argparse
import re
import sys
from pathlib import Path

if hasattr(sys.stdout, "reconfigure"):
    sys.stdout.reconfigure(encoding="utf-8")

ROOT = Path(__file__).resolve().parents[1]

B3_CORE_TYPES = {
    "LayoutMath.cs": r"\bclass\s+LayoutMath\b",
    "SplitMath.cs": r"\bclass\s+SplitMath\b",
    "PaneState.cs": r"\bclass\s+PaneSet\b",
    "TabModel.cs": r"\bclass\s+TabRules\b",
    "NavModel.cs": r"\bclass\s+NavRules\b",
    "AddressRules.cs": r"\bclass\s+AddressRules\b",
    "BookmarkRules.cs": r"\bclass\s+BookmarkRules\b",
    "SessionRules.cs": r"\bclass\s+SessionRules\b",
    "ThisPc.cs": r"\bclass\s+ThisPc\b",
}

APP_TYPES = {
    "ShellState.cs": r"\bclass\s+ShellState\b",
    "ShellCommands.cs": r"\bclass\s+ShellCommands\b",
}

EXPECTED_GROUPS = ["layout-*", "split-*", "tab-*", "nav-*", "bookmark-*", "session-*"]

CS6_PLUS = [
    ("string interpolation", re.compile(r'\$"')),
    ("null-conditional", re.compile(r"\?\.")),
    ("nameof", re.compile(r"\bnameof\s*\(")),
    ("out var", re.compile(r"\bout\s+var\b")),
    ("null-coalescing assignment", re.compile(r"\?\?=")),
    ("init-only setter", re.compile(r"\binit\s*;")),
]

# 3 窗格主窗格必须 RowSpan=3；改成 1 即退化为 4 窗格落位，layout-* 必须判红。
THREE_PANE_MAIN_SLOT = re.compile(r"new\s+PaneSlot\(\s*0\s*,\s*0\s*,\s*0\s*,\s*3\s*,\s*1\s*\)")

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

    print("check-shell-specs: root=%s" % root.name)

    def read(path):
        return path.read_text(encoding="utf-8", errors="replace") if path.exists() else ""

    # 1. B3 Core 类型齐备
    missing = []
    for fname, pattern in sorted(B3_CORE_TYPES.items()):
        p = core / fname
        if not p.exists():
            missing.append(fname + " (missing)")
        elif not re.search(pattern, read(p)):
            missing.append(fname + " (type not found)")
    if missing:
        bad("B3 Core 类型不齐备：%s" % "; ".join(missing))
    else:
        ok("B3 Core 类型齐备（%d 个文件）" % len(B3_CORE_TYPES))

    # 2. App 外壳骨架齐备
    missing_app = []
    for fname, pattern in sorted(APP_TYPES.items()):
        p = app / fname
        if not p.exists():
            missing_app.append(fname + " (missing)")
        elif not re.search(pattern, read(p)):
            missing_app.append(fname + " (type not found)")
    if missing_app:
        bad("App 外壳骨架不齐备：%s" % "; ".join(missing_app))
    else:
        ok("App 外壳骨架齐备（%d 个文件）" % len(APP_TYPES))

    # 3. runner 门卡组注册齐备
    program = tests / "Program.cs"
    ptext = read(program)
    groups = re.findall(r'\bRun\(\s*"([^"]+)"', ptext)
    missing_groups = [g for g in EXPECTED_GROUPS if g not in groups]
    if missing_groups:
        bad("runner 缺少 B3 门卡组：%s" % ", ".join(missing_groups))
    else:
        ok("runner B3 门卡组注册齐备（%d 组）" % len(EXPECTED_GROUPS))

    # 4. 规格与实现一致：规格仍为 v1.2 四档，且 3 窗格按默认保留
    design = read(root / "design-v2.md")
    if "LayoutMode {One,Two,Three,Four}" not in design:
        bad("规格中的四档 LayoutMode 声明被改动，B3 实现需同步")
    elif "已确认按默认执行" not in design:
        bad("规格待决项确认状态被改动，B3 实现需同步")
    else:
        ok("规格仍为 v1.2 四档布局（布局方案讨论状态不由本批改动）")

    # 5. 四档枚举与 3 窗格跨度
    layout = read(core / "LayoutMath.cs")
    if not re.search(r"\benum\s+LayoutMode\s*\{[^}]*One\s*=\s*1[^}]*Four\s*=\s*4", layout, re.S):
        bad("LayoutMode 四档枚举不完整")
    elif not THREE_PANE_MAIN_SLOT.search(layout):
        bad("3 窗格主窗格未按 RowSpan=3 落位（或退化成 4 窗格落位）")
    else:
        ok("四档枚举与 3 窗格 RowSpan=3 落位")

    # 6. 折叠 0/1 与 24px 吸附
    split = read(core / "SplitMath.cs")
    if "SnapThresholdPx = 24.0" not in split or "CollapseTo" not in split:
        bad("折叠 / 24px 吸附常量缺失")
    else:
        ok("折叠 0/1 与 24px 吸附常量齐备")

    # 7. future-version 拒读
    session = read(core / "SessionRules.cs")
    if "CurrentSchema = 2" not in session or "FutureVersion" not in session:
        bad("会话 future-version 判据缺失")
    else:
        ok("会话 future-version / schemaVersion 判据齐备")

    # 8. App 层分层边界（本批骨架零 I/O、不引用测试替身、不手写 COM）
    app_files = sorted(p for p in app.glob("*.cs") if p.is_file()) if app.exists() else []
    violations = []
    for p in app_files:
        text = read(p)
        for label, rx in APP_FORBIDDEN:
            if rx.search(text):
                violations.append("%s: %s" % (p.name, label))
    if violations:
        bad("App 外壳骨架触犯分层边界：%s" % "; ".join(violations))
    else:
        ok("App 外壳骨架零 I/O / 无测试替身 / 无手写 COM（%d 个 .cs）" % len(app_files))

    # 9. C# 5 语法子集（Core + App + Tests 全扫）
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

    print("  N/A   真实 WPF 窗口 / XAML / 消息循环 / 系统原生菜单 / 冲突与进度面板：未实现 / 未验证")
    if errors:
        for e in errors:
            print("  RED   " + e)
        print("verdict: RED (%d)" % len(errors))
        return len(errors)
    print("verdict: GREEN")
    return 0


if __name__ == "__main__":
    sys.exit(main())
