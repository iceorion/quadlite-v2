#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
B5 静态契约门卡：系统原生右键菜单的零 I/O 规则层、App 菜单状态内核与菜单专项合规。

零第三方依赖，退出码 = 红条数。
  python tools/check_menu_specs.py                 # 扫仓库
  python tools/check_menu_specs.py --root <目录>   # 负向对照时指向临时副本

本脚本只核对「B5 Core 类型齐备 / App 状态内核齐备 / 门卡组注册 / 规格关键常量 /
源头消重与参数纪律 / 产品程序集 verb 查询 0 次 / FakeShellExtension 四态 /
分层边界 / C# 5 语法 / 规格版本一致」这些静态事实。

真实 NativeMenuHost、IContextMenu / IContextMenu2/3 COM 调用、TrackPopupMenuEx
消息循环、HMENU 生命周期、崩溃哨兵的磁盘标记与自绘兜底 UI 均未接入，
一律标「未实现 / 未验证」（见 docs/adr/ADR-v2-007.md）。
"""
import argparse
import re
import sys
from pathlib import Path

if hasattr(sys.stdout, "reconfigure"):
    sys.stdout.reconfigure(encoding="utf-8")

ROOT = Path(__file__).resolve().parents[1]

B5_CORE_TYPES = {
    "MenuHostRules.cs": r"\bclass\s+MenuHostRules\b",
    "MenuInvokeRules.cs": r"\bclass\s+MenuInvokeRules\b",
    "MenuFusionRules.cs": r"\bclass\s+MenuFusionRules\b",
    "MenuMessageRules.cs": r"\bclass\s+MenuMessageRules\b",
    "MenuTargetSnapshotRules.cs": r"\bclass\s+MenuTargetSnapshotRules\b",
    "MenuSentinelRules.cs": r"\bclass\s+MenuSentinelRules\b",
    "MenuReleaseRules.cs": r"\bclass\s+MenuReleaseRules\b",
    "MenuProbeRules.cs": r"\bclass\s+MenuProbeRules\b",
}

APP_TYPES = {
    "MenuState.cs": r"\bclass\s+MenuState\b",
    "MenuCommands.cs": r"\bclass\s+MenuCommands\b",
}

EXPECTED_GROUPS = [
    "menu-native-default", "menu-no-verb-scan", "menu-id-range",
    "menu-own-id-segment", "menu-no-fusion", "menu-msg-forward",
    "menu-target-snapshot", "menu-sentinel", "menu-single-file", "menu-release",
]

CS6_PLUS = [
    ("string interpolation", re.compile(r'\$"')),
    ("null-conditional", re.compile(r"\?\.")),
    ("nameof", re.compile(r"\bnameof\s*\(")),
    ("out var", re.compile(r"\bout\s+var\b")),
    ("null-coalescing assignment", re.compile(r"\?\?=")),
    ("init-only setter", re.compile(r"\binit\s*;")),
]

# 规格 §5.2 / §5.6 / §7.1 的关键常量与不变量，改为他值即判红（防止实现静默漂移）。
CORE_INVARIANTS = {
    "MenuHostRules.cs": [
        ("four standard steps", r"StandardStepCount\s*=\s*4\b"),
        ("zero verb queries", r"VerbScanCount\s*\(\s*\)\s*\{[^}]*return\s+0\s*;"),
        ("zero item deletion", r"DeleteItemCount\s*\(\s*\)\s*\{[^}]*return\s+0\s*;"),
        ("zero menu-tree rewrite", r"MenuTreeRewriteCount\s*\(\s*\)\s*\{[^}]*return\s+0\s*;"),
    ],
    "MenuInvokeRules.cs": [
        ("idCmdFirst is one", r"IdCmdFirst\s*=\s*1\b"),
        ("idCmdLast is shell ceiling", r"IdCmdLast\s*=\s*0x7FFF\b"),
        ("no hand-written vtable", r"DeclaresRawVtable\s*\(\s*\)\s*\{[^}]*return\s+false\s*;"),
    ],
    "MenuFusionRules.cs": [
        ("zero delete-menu calls", r"DeleteMenuCount\s*\(\s*\)\s*\{[^}]*return\s+0\s*;"),
        ("zero insert shuffle", r"InsertShuffleCount\s*\(\s*\)\s*\{[^}]*return\s+0\s*;"),
        ("one own-group separator", r"OwnGroupSeparatorCount\s*=\s*1\b"),
    ],
    "MenuMessageRules.cs": [
        ("five forwarded messages", r"MessageCount\s*=\s*5\b"),
        ("handler is HandleMenuMsg2", r'return\s+"HandleMenuMsg2"\s*;'),
    ],
    "MenuSentinelRules.cs": [
        ("TTL default seven days", r"DefaultTtlDays\s*=\s*7\b"),
        ("TTL cap thirty days", r"MaxTtlDays\s*=\s*30\b"),
        ("no global degrade", r"DegradesGlobally\s*\(\s*\)\s*\{[^}]*return\s+false\s*;"),
        ("no extension enumeration", r"EnumeratesExtensions\s*\(\s*\)\s*\{[^}]*return\s+false\s*;"),
    ],
    "MenuReleaseRules.cs": [
        ("two resources per session", r"ResourcesPerSession\s*=\s*2\b"),
        ("try-finally release", r"UsesTryFinally\s*\(\s*\)\s*\{[^}]*return\s+true\s*;"),
    ],
    "MenuProbeRules.cs": [
        ("four probe behaviors", r"BehaviorCount\s*=\s*4\b"),
        ("no real extension installed", r"RequiresRealExtensionInstalled\s*\(\s*\)\s*\{[^}]*return\s+false\s*;"),
    ],
    "MenuTargetSnapshotRules.cs": [
        ("one snapshot per invocation", r"SnapshotsPerInvocation\s*\(\s*\)\s*\{[^}]*return\s+1\s*;"),
        ("no live selection read", r"UsesLiveSelection\s*\(\s*\)\s*\{[^}]*return\s+false\s*;"),
    ],
}

PRODUCT_FORBIDDEN = [
    ("test double reference", re.compile(r"QuadLite\.(?:Probe|Tests)")),
    ("hand-written COM", re.compile(r"\bComImport\b|\bInterfaceType\b|\[\s*Guid\s*\(")),
    ("verb scan call", re.compile(r"\bGetCommandString\b")),
    ("menu item deletion", re.compile(r"\bDeleteMenu\s*\(")),
]

APP_FORBIDDEN = [
    ("using System.IO", re.compile(r"\busing\s+System\.IO\s*;")),
    ("test double reference", re.compile(r"QuadLite\.(?:Probe|Tests)")),
    ("hand-written COM", re.compile(r"\bComImport\b|\bInterfaceType\b|\[\s*Guid\s*\(")),
    ("verb scan call", re.compile(r"\bGetCommandString\b")),
]

PROBE_BEHAVIORS = ["Ok", "OutOfRange", "OwnerDraw", "Crash"]


def enum_body(text, name):
    m = re.search(r"\benum\s+" + re.escape(name) + r"\s*\{([^}]*)\}", text, re.S)
    if not m:
        return None
    return [x.strip() for x in re.sub(r"//[^\n]*", "", m.group(1)).split(",") if x.strip()]


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

    def read(path):
        return path.read_text(encoding="utf-8", errors="replace") if path.exists() else ""

    print("check-menu-specs: root=%s" % root.name)

    # 1. B5 Core 类型齐备
    missing = []
    for fname, pattern in sorted(B5_CORE_TYPES.items()):
        p = core / fname
        if not p.exists():
            missing.append(fname + " (missing)")
        elif not re.search(pattern, read(p)):
            missing.append(fname + " (type not found)")
    if missing:
        bad("B5 Core 类型不齐备：%s" % "; ".join(missing))
    else:
        ok("B5 Core 规则层齐备（%d 个文件）" % len(B5_CORE_TYPES))

    # 2. App 菜单状态内核齐备
    missing_app = []
    for fname, pattern in sorted(APP_TYPES.items()):
        p = app / fname
        if not p.exists():
            missing_app.append(fname + " (missing)")
        elif not re.search(pattern, read(p)):
            missing_app.append(fname + " (type not found)")
    if missing_app:
        bad("App 菜单状态内核不齐备：%s" % "; ".join(missing_app))
    else:
        ok("App 菜单状态内核齐备（%d 个文件）" % len(APP_TYPES))

    # 3. runner B5 门卡组注册齐备
    ptext = read(tests / "Program.cs")
    groups = re.findall(r'\bRun\(\s*"([^"]+)"', ptext)
    missing_groups = [g for g in EXPECTED_GROUPS if g not in groups]
    if missing_groups:
        bad("runner 缺少 B5 门卡组：%s" % ", ".join(missing_groups))
    else:
        ok("runner B5 门卡组注册齐备（%d 组）" % len(EXPECTED_GROUPS))

    # 4. 规格关键常量与不变量
    inv_bad = []
    for fname, checks in sorted(CORE_INVARIANTS.items()):
        text = read(core / fname)
        for label, rx in checks:
            if not re.search(rx, text, re.S):
                inv_bad.append("%s: %s" % (fname, label))
    if inv_bad:
        bad("B5 规格常量 / 不变量被破坏：%s" % "; ".join(inv_bad))
    else:
        total = sum(len(v) for v in CORE_INVARIANTS.values())
        ok("B5 规格常量 / 不变量齐备（%d 项）" % total)

    # 5. 菜单专项合规：产品程序集不读 verb、不删项、不手写 COM、不引用替身
    #    分层边界中的 System.IO 由 check-boundaries（B-01 / B-02）单独判定，
    #    Fs 适配器按设计允许 System.IO，不在此门卡重复拦截。
    product = sorted(p for p in (root / "src").rglob("*.cs") if p.is_file())
    violations = []
    for p in product:
        text = read(p)
        for label, rx in PRODUCT_FORBIDDEN:
            if rx.search(text):
                violations.append("%s: %s" % (p.relative_to(root).as_posix(), label))
    if violations:
        bad("产品程序集触犯菜单专项纪律：%s" % "; ".join(violations))
    else:
        ok("产品程序集 verb 查询 0 次 / 零删项 / 零手写 COM（%d 个 .cs 实测）" % len(product))

    # 6. App 菜单状态内核零 I/O / 无测试替身 / 无手写 COM / 无 verb 查询
    app_files = sorted(p for p in app.glob("*.cs") if p.is_file()) if app.exists() else []
    app_bad = []
    for p in app_files:
        text = read(p)
        for label, rx in APP_FORBIDDEN:
            if rx.search(text):
                app_bad.append("%s: %s" % (p.name, label))
    if app_bad:
        bad("App 菜单状态内核触犯分层边界：%s" % "; ".join(app_bad))
    else:
        ok("App 状态内核零 I/O / 无测试替身 / 无手写 COM（%d 个 .cs）" % len(app_files))

    # 7. FakeShellExtension 四态齐备：替身在 Tests 层，四态由 Core 枚举定义、runner 逐态覆盖
    fakes = read(tests / "MenuFakes.cs")
    probe_enum = enum_body(read(core / "MenuProbeRules.cs"), "ProbeBehavior")
    matrix = [b for b in PROBE_BEHAVIORS
              if not re.search(r"ProbeBehavior\." + b + r"\b", ptext)]
    if "class FakeShellExtension" not in fakes:
        bad("FakeShellExtension 替身缺失")
    elif probe_enum is None or probe_enum != PROBE_BEHAVIORS:
        bad("ProbeBehavior 四态枚举不齐：%s" % probe_enum)
    elif matrix:
        bad("runner 四态矩阵未逐态覆盖：%s" % ", ".join(matrix))
    else:
        ok("FakeShellExtension 四态齐备（ok / 越界 / owner-draw / 崩溃，runner 逐态覆盖）")

    # 8. App 命令枚举完备性（B5 新增，check-core-specs 不扫 App 层）
    outcome = enum_body(read(app / "MenuCommands.cs"), "MenuCommandOutcome")
    if outcome is None or len(outcome) != 5:
        bad("MenuCommandOutcome 枚举应为 5 项，实为 %s" %
            ("缺失" if outcome is None else len(outcome)))
    else:
        ok("MenuCommandOutcome 枚举完备（5 项）")

    # 9. 规格与实现一致：设计文档仍是 v1.2，B5 判据声明未被擅自改动
    design = read(root / "design-v2.md")
    if "| B5 | 系统原生右键菜单" not in design or "`menu-*` 十条" not in design:
        bad("规格 §8.1 的 B5 判据声明被改动，B5 实现需同步")
    elif "已定稿（v1.2）" not in design:
        bad("规格版本不再是 v1.2，B5 实现需同步复核")
    else:
        ok("规格仍为 v1.2 且 B5 判据声明未改（布局方案讨论状态不由本批改动）")

    # 10. C# 5 语法子集（Core + App + Tests 全扫）
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

    print("  N/A   真实 NativeMenuHost / IContextMenu COM / TrackPopupMenuEx / HMENU / "
          "崩溃哨兵磁盘标记 / 自绘兜底 UI：未实现 / 未验证")
    if errors:
        for e in errors:
            print("  RED   " + e)
        print("verdict: RED (%d)" % len(errors))
        return len(errors)
    print("verdict: GREEN")
    return 0


if __name__ == "__main__":
    sys.exit(main())
