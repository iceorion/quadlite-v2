#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
B7 静态契约门卡：自测装置 / 门卡总表 / 账本 / BUILD.txt / 出包自检的规则层、
L4 状态内核与命令行契约。

零第三方依赖，退出码 = 红条数。
  python tools/check_selftest_specs.py                 # 扫仓库
  python tools/check_selftest_specs.py --root <目录>   # 负向对照时指向临时副本

本脚本只核对「类型齐备 / 门卡组注册 / 规格常量 / §7.1 门卡总表逐项一致 /
--selftest 报告行数与格式 / 命令行契约复用 / 分层边界 / 规格版本 / C# 5 语法」
这些静态事实。

真实产品 exe 形态的 `--selftest`、BUILD.txt 采集、单文件出包、--bench 采样、
崩溃日志写入与零写入三段取证均未接入，一律标「未实现 / 未验证」
（见 docs/adr/ADR-v2-009.md）。
"""
import argparse
import re
import sys
from pathlib import Path

if hasattr(sys.stdout, "reconfigure"):
    sys.stdout.reconfigure(encoding="utf-8")

ROOT = Path(__file__).resolve().parents[1]

B7_CORE_TYPES = {
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

APP_TYPES = {
    "SelftestState.cs": r"\bclass\s+SelftestState\b",
    "PackageState.cs": r"\bclass\s+PackageState\b",
}

EXPECTED_GROUPS = [
    "cli-contract", "cli-diagnostic-modes", "cli-selftest-output",
    "portable-gate-inventory", "portable-ledger", "portable-build-manifest",
    "portable-package-selfcheck", "portable-budget-table",
    "portable-crash-digest", "single-instance-marker",
]

CS6_PLUS = [
    ("string interpolation", re.compile(r'\$"')),
    ("null-conditional", re.compile(r"\?\.")),
    ("nameof", re.compile(r"\bnameof\s*\(")),
    ("out var", re.compile(r"\bout\s+var\b")),
    ("null-coalescing assignment", re.compile(r"\?\?=")),
    ("init-only setter", re.compile(r"\binit\s*;")),
]

# §6.4 / §7.1 / §7.3 / §8.1 B7 的关键常量与不变量，改为他值即判红。
CORE_INVARIANTS = {
    "CliContractRules.cs": [
        ("nine spec parameters", r"SpecParameterCount\s*=\s*9\b"),
        ("fourteen known parameters", r"ParameterCount\s*=\s*14\b"),
        ("seven diagnostic modes", r"DiagnosticModes\.Length\s*==\s*7\b"),
        ("two release-excluded tools", r"ReleaseExcluded\.Length\s*==\s*2\b"),
    ],
    "SelftestReportRules.cs": [
        ("100 line minimum", r"MinOutputLines\s*=\s*100\b"),
        ("two tail lines", r"TailLineCount\s*=\s*2\b"),
        ("verdict prefix", r'VerdictPrefix\s*=\s*"verdict:\s*"'),
        ("exit code is red count", r"ExitCode\s*\(\s*int\s+redCount\s*\)\s*\{[^}]*return\s+redCount\s*;"),
    ],
    "GateInventoryRules.cs": [
        ("nine gate groups", r"GroupCount\s*=\s*9\b"),
        ("fifty registered gates", r"GateCount\s*=\s*50\b"),
        ("nine batches", r"BatchCount\s*=\s*9\b"),
    ],
    "LedgerRules.cs": [
        ("nine ledger columns", r"RequiredColumnCount\s*=\s*9\b"),
        ("seven char head_sha", r"HeadShaLength\s*=\s*7\b"),
        ("four evidence levels", r"EvidenceLevelCount\s*=\s*4\b"),
        ("streak is not evidence",
         r"ForbidsStreakAsEvidence\s*\(\s*\)\s*\{[^}]*return\s+true\s*;"),
    ],
    "BuildManifestRules.cs": [
        ("seven manifest fields", r"FieldCount\s*=\s*7\b"),
        ("dirty tree warning", r'DirtyTreeWarning\s*=\s*"Warning"'),
    ],
    "PackageSelfcheckRules.cs": [
        ("two package items", r"ContentCount\s*=\s*2\b"),
        ("two excluded tools", r"ExcludedToolCount\s*=\s*2\b"),
    ],
    "BudgetTableRules.cs": [
        ("four budget kinds", r"BudgetCount\s*=\s*4\b"),
        ("seven budget columns", r"ColumnCount\s*=\s*7\b"),
        ("no timing verdict", r"NoThresholdVerdict\s*\(\s*\)\s*\{[^}]*return\s+true\s*;"),
        ("not a CI gate", r"IsCiGate\s*\(\s*\)\s*\{[^}]*return\s+false\s*;"),
    ],
    "CrashDigestRules.cs": [
        ("five digest fields", r"FieldCount\s*=\s*5\b"),
        ("no full path", r"ContainsFullPath\s*\(\s*\)\s*\{[^}]*return\s+false\s*;"),
        ("no file name", r"ContainsFileName\s*\(\s*\)\s*\{[^}]*return\s+false\s*;"),
    ],
    "SingleInstanceRules.cs": [
        ("QL marker", r'Marker\s*=\s*"QL"'),
        ("three mechanisms", r"MechanismCount\s*=\s*3\b"),
        ("only normal mode participates",
         r"ParticipatesInSingleInstance\s*\(\s*CliMode\s+mode\s*\)\s*\{[^}]*return\s+mode\s*==\s*CliMode\.Normal\s*;"),
    ],
}

CLI_CONTRACT_WIRING = [
    ("parse consults contract",
     r"CliContractRules\.IsKnown\(a\)"),
    ("legal parameters delegate",
     r"LegalParameters\s*\(\s*\)\s*\{[^}]*return\s+CliContractRules\.LegalParameters\s*\(\s*\)\s*;"),
    ("single instance delegates",
     r"ParticipatesInSingleInstance\s*\{[^}]*return\s+SingleInstanceRules\.ParticipatesInSingleInstance\s*\(\s*mode\s*\)"),
]

APP_FORBIDDEN = [
    ("using System.IO", re.compile(r"\busing\s+System\.IO\s*;")),
    ("test double reference", re.compile(r"QuadLite\.(?:Probe|Tests)")),
    ("hand-written COM", re.compile(r"\bComImport\b|\bInterfaceType\b|\[\s*Guid\s*\(")),
    ("verb scan", re.compile(r"GetCommandString")),
]


def array_body(text, name):
    m = re.search(r"string\[\]\s+" + re.escape(name) +
                  r"\s*\(\s*\)\s*\{([^}]*)\}", text, re.S)
    if not m:
        return None
    return re.findall(r'"([^"]+)"', m.group(1))


def spec_gate_tokens(design_text):
    """取 design-v2.md §7.1 表格第二列的反引号门卡 token。"""
    m = re.search(r"^### 7\.1 .*?$(.*?)(?=^### 7\.2 )", design_text, re.M | re.S)
    if not m:
        return []
    tokens = []
    for line in m.group(1).splitlines():
        if not line.startswith("|"):
            continue
        cells = [c.strip() for c in line.strip().strip("|").split("|")]
        if len(cells) < 2:
            continue
        tokens.extend(re.findall(r"`([a-z][a-z0-9*\-]+)`", cells[1]))
    return tokens


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

    print("check-selftest-specs: root=%s" % root.name)

    # 1. B7 Core 类型齐备
    missing = []
    for fname, pattern in sorted(B7_CORE_TYPES.items()):
        p = core / fname
        if not p.exists():
            missing.append(fname + " (missing)")
        elif not re.search(pattern, read(p)):
            missing.append(fname + " (type not found)")
    if missing:
        bad("B7 Core 规则层不齐备：%s" % "; ".join(missing))
    else:
        ok("B7 Core 规则层齐备（%d 个文件）" % len(B7_CORE_TYPES))

    # 2. App 自测 / 出包状态内核齐备
    missing_app = []
    for fname, pattern in sorted(APP_TYPES.items()):
        p = app / fname
        if not p.exists():
            missing_app.append(fname + " (missing)")
        elif not re.search(pattern, read(p)):
            missing_app.append(fname + " (type not found)")
    if missing_app:
        bad("App 自测 / 出包状态内核不齐备：%s" % "; ".join(missing_app))
    else:
        ok("App 自测 / 出包状态内核齐备（%d 个文件）" % len(APP_TYPES))

    # 3. runner B7 门卡组注册齐备
    ptext = read(tests / "Program.cs")
    groups = re.findall(r'\bRun\(\s*"([^"]+)"', ptext)
    missing_groups = [g for g in EXPECTED_GROUPS if g not in groups]
    if missing_groups:
        bad("runner 缺少 B7 门卡组：%s" % ", ".join(missing_groups))
    else:
        ok("runner B7 门卡组注册齐备（%d 组）" % len(EXPECTED_GROUPS))

    # 4. 规格关键常量与不变量
    inv_bad = []
    for fname, checks in sorted(CORE_INVARIANTS.items()):
        text = read(core / fname)
        for label, rx in checks:
            if not re.search(rx, text, re.S):
                inv_bad.append("%s: %s" % (fname, label))
    if inv_bad:
        bad("B7 规格常量 / 不变量被破坏：%s" % "; ".join(inv_bad))
    else:
        total = sum(len(v) for v in CORE_INVARIANTS.values())
        ok("B7 规格常量 / 不变量齐备（%d 项）" % total)

    # 5. 门卡总表与 §7.1 逐项一致（不是只比一个数字）
    src_gates = array_body(read(core / "GateInventoryRules.cs"), "Gates")
    spec_gates = spec_gate_tokens(read(root / "design-v2.md"))
    gate_bad = []
    if src_gates is None:
        gate_bad.append("源码门卡表缺失")
    elif spec_gates and (sorted(src_gates) != sorted(spec_gates)):
        only_src = sorted(set(src_gates) - set(spec_gates))
        only_spec = sorted(set(spec_gates) - set(src_gates))
        gate_bad.append("与 §7.1 不一致（源码多 %s；规格多 %s）" % (only_src[:5], only_spec[:5]))
    if len(spec_gates) != 50:
        gate_bad.append("§7.1 门卡 %d 项（应为 50）" % len(spec_gates))
    if gate_bad:
        bad("门卡总表漂移：%s" % "; ".join(gate_bad))
    else:
        ok("门卡总表与 §7.1 逐项一致（%d 项）" % len(spec_gates))

    # 6. --selftest 报告形态：明细行 + 组行 + 末两行，合计 ≥100
    report_bad = []
    if len(groups) != 71:
        report_bad.append("组数 %d（应为 71）" % len(groups))
    if "SelftestState.DetailLines()" not in ptext or "Console.WriteLine(detail[i])" not in ptext:
        report_bad.append("runner 未输出门卡总表明细行")
    if not re.search(r"new\s+SelftestState\(\s*71\s*,\s*failures\s*\)", ptext):
        report_bad.append("runner 未用状态内核汇总组数")
    if "report.VerdictLine()" not in ptext or "report.ExitCode()" not in ptext:
        report_bad.append("runner 末行 verdict / 退出码未委托状态内核")
    gate_rows = 50
    budget_rows = 4
    summary_rows = 3
    tail_rows = 2
    total_lines = gate_rows + budget_rows + summary_rows + len(groups) + tail_rows
    if total_lines < 100:
        report_bad.append("报告仅 %d 行（应 ≥100）" % total_lines)
    if report_bad:
        bad("--selftest 输出形态被破坏：%s" % "; ".join(report_bad))
    else:
        ok("--selftest 报告形态（%d 行 ≥100，末行 verdict，退出码=红条数）" % total_lines)

    # 7. 命令行契约必须真的被复用，而不是另立一张纸面清单
    cli_text = read(core / "CliArgs.cs")
    wiring_bad = [label for label, rx in CLI_CONTRACT_WIRING
                  if not re.search(rx, cli_text, re.S)]
    if wiring_bad:
        bad("CliArgs 未复用命令行契约：%s" % ", ".join(wiring_bad))
    else:
        ok("CliArgs 复用 CliContractRules / SingleInstanceRules（%d 处）" % len(CLI_CONTRACT_WIRING))

    # 8. App 状态内核零 I/O / 无测试替身 / 无手写 COM
    app_files = sorted(p for p in app.glob("*.cs") if p.is_file()) if app.exists() else []
    app_bad = []
    for p in app_files:
        text = read(p)
        for label, rx in APP_FORBIDDEN:
            if rx.search(text):
                app_bad.append("%s: %s" % (p.name, label))
    if app_bad:
        bad("App 状态内核触犯分层边界：%s" % "; ".join(app_bad[:5]))
    else:
        ok("App 状态内核零 I/O / 无测试替身 / 无手写 COM（%d 个 .cs）" % len(app_files))

    # 9. 产品程序集 verb 查询 0 次 / 零手写 COM
    product = sorted(p for p in (root / "src").rglob("*.cs") if p.is_file())
    product_bad = []
    for p in product:
        text = read(p)
        if "GetCommandString" in text:
            product_bad.append("%s: verb scan" % p.relative_to(root).as_posix())
        if re.search(r"\bComImport\b|\bInterfaceType\b", text):
            product_bad.append("%s: hand-written COM" % p.relative_to(root).as_posix())
    if product_bad:
        bad("产品程序集触犯 B7 分层纪律：%s" % "; ".join(product_bad[:5]))
    else:
        ok("产品程序集 verb 查询 0 次 / 零手写 COM（%d 个 .cs）" % len(product))

    # 10. 规格与实现一致 + C# 5 语法子集
    design = read(root / "design-v2.md")
    consistency = []
    if "| B7 | 自测装置、门卡总表、账本、出包包自检" not in design:
        consistency.append("§8.1 B7 判据声明被改动")
    if "已定稿（v1.2）" not in design:
        consistency.append("规格版本不再是 v1.2")
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
    if consistency or cs6_hits:
        if consistency:
            bad("规格一致性被破坏：%s" % "; ".join(consistency))
        if cs6_hits:
            bad("C# 5 语法子集被破坏：%s" % "; ".join(cs6_hits[:5]))
    else:
        ok("规格仍为 v1.2 且 B7 判据未改；C# 5 语法子集（%d 个 .cs）" % len(cs_files))

    print("  N/A   真实产品 exe 的 --selftest / BUILD.txt 采集 / 单文件出包 / "
          "--bench 采样 / 崩溃日志 / 三段取证：未实现 / 未验证")
    if errors:
        for e in errors:
            print("  RED   " + e)
        print("verdict: RED (%d)" % len(errors))
        return len(errors)
    print("verdict: GREEN")
    return 0


if __name__ == "__main__":
    sys.exit(main())
