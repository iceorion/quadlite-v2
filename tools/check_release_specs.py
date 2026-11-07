#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
B8 静态契约门卡：发布线 C4 + H1–H6 的规则层、L4 状态内核与 §8.3 原文一致性。

零第三方依赖，退出码 = 红条数。
  python tools/check_release_specs.py                 # 扫仓库
  python tools/check_release_specs.py --root <目录>   # 负向对照时指向临时副本

本脚本只核对「类型齐备 / 门卡组注册 / 规格常量 / §8.3 阶段链逐项一致 /
证据等级纪律 / C4 零误报门槛 / 分层边界 / 规格版本 / C# 5 语法」这些静态事实。

H1–H6 的真实取证活动（高 DPI 四档截图、零写入三段取证、兼容矩阵实测、
WER 读法、性能采样、OV 签名）与 C4 带杀软 VM 均未执行，一律标「未实现 / 未验证」
（见 docs/adr/ADR-v2-010.md）。
"""
import argparse
import re
import sys
from pathlib import Path

if hasattr(sys.stdout, "reconfigure"):
    sys.stdout.reconfigure(encoding="utf-8")

ROOT = Path(__file__).resolve().parents[1]

B8_CORE_TYPES = {
    "ReleaseStageRules.cs": r"\bclass\s+ReleaseStageRules\b",
    "EvidencePolicyRules.cs": r"\bclass\s+EvidencePolicyRules\b",
    "DpiMatrixRules.cs": r"\bclass\s+DpiMatrixRules\b",
    "WriteEvidenceRules.cs": r"\bclass\s+WriteEvidenceRules\b",
    "CompatibilityMatrixRules.cs": r"\bclass\s+CompatibilityMatrixRules\b",
    "CrashReportReadingRules.cs": r"\bclass\s+CrashReportReadingRules\b",
    "PerfRegressionRules.cs": r"\bclass\s+PerfRegressionRules\b",
    "CodeSigningRules.cs": r"\bclass\s+CodeSigningRules\b",
    "AvScanRules.cs": r"\bclass\s+AvScanRules\b",
    "ReleaseRecordRules.cs": r"\bclass\s+ReleaseRecordRules\b",
}

APP_TYPES = {
    "ReleaseState.cs": r"\bclass\s+ReleaseState\b",
    "ReleaseRecordState.cs": r"\bclass\s+ReleaseRecordState\b",
}

EXPECTED_GROUPS = [
    "release-stage-order", "release-degraded-path", "release-evidence-policy",
    "release-av-scan", "release-dpi-matrix", "release-write-evidence",
    "release-compat-matrix", "release-crash-reading", "release-perf-regression",
    "release-signing-and-record",
]

CS6_PLUS = [
    ("string interpolation", re.compile(r'\$"')),
    ("null-conditional", re.compile(r"\?\.")),
    ("nameof", re.compile(r"\bnameof\s*\(")),
    ("out var", re.compile(r"\bout\s+var\b")),
    ("null-coalescing assignment", re.compile(r"\?\?=")),
    ("init-only setter", re.compile(r"\binit\s*;")),
]

# §8.3 / NFR-10 / NFR-11 / DD-04 / §8.4 M3 的关键常量与不变量，改为他值即判红。
CORE_INVARIANTS = {
    "ReleaseStageRules.cs": [
        ("six stages", r"StageCount\s*=\s*6\b"),
        ("seven nodes", r"NodeCount\s*=\s*7\b"),
        ("three degraded stages", r"DegradedStageCount\s*=\s*3\b"),
        ("C4 is first", r'DependencyOrder\s*\(\s*\)\s*\{[^}]*new\s+string\[\]\s*\{\s*"C4",\s*"H1",\s*"H3",\s*"H2"'),
        ("degraded path H1/H3/H2", r'DegradedOrder\s*\(\s*\)\s*\{[^}]*new\s+string\[\]\s*\{\s*"H1",\s*"H3",\s*"H2"\s*\}'),
    ],
    "EvidencePolicyRules.cs": [
        ("four evidence levels", r"LevelCount\s*=\s*4\b"),
        ("levels reuse ledger", r"Levels\s*\(\s*\)\s*\{[^}]*return\s+LedgerRules\.EvidenceLevels\s*\(\s*\)"),
        ("unverified rejected", r"ClaimAccepted\s*\([\s\S]{0,220}return\s+level\s*!=\s*EvidenceLevel\.Unverified\s*;"),
    ],
    "DpiMatrixRules.cs": [
        ("four DPI scales", r"ScaleCount\s*=\s*4\b"),
        ("three assertions", r"AssertionCount\s*=\s*3\b"),
        ("PerMonitorV2", r'ManifestMode\s*\(\s*\)\s*\{[^}]*return\s+"PerMonitorV2"'),
        ("100/125/150/200", r"new\s+int\[\]\s*\{\s*100,\s*125,\s*150,\s*200\s*\}"),
        ("all scales coverage loop", r"AllScalesCovered\s*\([\s\S]{0,700}if\s*\(!found\)\s*return\s+false\s*;"),
    ],
    "WriteEvidenceRules.cs": [
        ("three zones", r"ZoneCount\s*=\s*3\b"),
        ("one exception", r"ExceptionCount\s*=\s*1\b"),
        ("registry / app-directory / system-cache",
         r'new\s+string\[\]\s*\{\s*"registry",\s*"app-directory",\s*"system-cache"\s*\}'),
        ("sole exception is thumbnail cache", r'SoleException\s*\(\s*\)\s*\{[^}]*return\s+"thumbnail-cache"'),
    ],
    "CompatibilityMatrixRules.cs": [
        ("two operating systems", r"OsCount\s*=\s*2\b"),
        ("four filesystems", r"FsCount\s*=\s*4\b"),
        ("eight cells", r"CellCount\s*=\s*8\b"),
        ("read-only carrier is a cell", r'"read-only-carrier"'),
    ],
    "CrashReportReadingRules.cs": [
        ("two sources", r"SourceCount\s*=\s*2\b"),
        ("local digest is primary", r'"local-crash-digest",\s*"windows-error-reporting"'),
        ("reuse B7 digest", r"CrashDigestRules\.IsSanitized\s*\(\s*\)"),
    ],
    "PerfRegressionRules.cs": [
        ("four budgets", r"BudgetCount\s*=\s*4\b"),
        ("seven columns", r"ColumnCount\s*=\s*7\b"),
        ("reuse B7 budget table", r"BudgetTableRules\.Columns\s*\(\s*\)"),
        ("not a CI gate", r"IsCiGate\s*\(\s*\)\s*\{[^}]*return\s+BudgetTableRules\.IsCiGate\s*\(\s*\)\s*;"),
    ],
    "CodeSigningRules.cs": [
        ("free OV certificate", r'CertificateClass\s*=\s*"OV"'),
        ("sha256 digest", r'DigestAlgorithm\s*=\s*"sha256"'),
        ("three signing steps", r"StepCount\s*=\s*3\b"),
        ("unsigned cannot claim success", r"SignatureClaimValid\s*\([\s\S]{0,220}if\s*\(!signed\)\s*return\s+false\s*;"),
    ],
    "AvScanRules.cs": [
        ("at least one AV engine", r"MinEngineCount\s*=\s*1\b"),
        ("engine count and scan both required",
         r"ZeroFalsePositiveClaimValid\s*\([^)]*\)\s*\{[^}]*return\s+engineCount\s*>=\s*MinEngineCount\s*&&\s*scanned\s*;"),
    ],
    "ReleaseRecordRules.cs": [
        ("C4 absence wording", r'C4AbsenceText\s*=\s*"C4 未做"'),
        ("six stages required", r"Evaluate\s*\([\s\S]{0,320}levels\.Length\s*!=\s*ReleaseStageRules\.StageCount"),
        ("C4 absence is a separate red", r"return\s+ReleaseVerdict\.C4NotDeclared\s*;"),
        ("all six measured returns publishable", r"return\s+ReleaseVerdict\.Publishable\s*;"),
    ],
}

APP_FORBIDDEN = [
    ("using System.IO", re.compile(r"\busing\s+System\.IO\s*;")),
    ("test double reference", re.compile(r"QuadLite\.(?:Probe|Tests)")),
    ("hand-written COM", re.compile(r"\bComImport\b|\bInterfaceType\b|\[\s*Guid\s*\(")),
    ("verb scan", re.compile(r"GetCommandString")),
]


def array_body(text, name, cast="string"):
    m = re.search(cast + r"\[\]\s+" + re.escape(name) +
                  r"\s*\(\s*\)\s*\{([^}]*)\}", text, re.S)
    if not m:
        return None
    return re.findall(r'"([^"]+)"', m.group(1))


def section83_tokens(design_text):
    """解析 design-v2.md §8.3 代码块，返回 (依赖链节点, DPI 四档, 降级链)。"""
    m = re.search(r"^### 8\.3 .*?$(.*?)(?=^### 8\.4 )", design_text, re.M | re.S)
    if not m:
        return [], [], []
    body = m.group(1)
    code = re.search(r"```(.*?)```", body, re.S)
    chain = []
    dpi = []
    if code:
        first = re.search(r"^\s*(C4|H[1-6])（", code.group(1), re.M)
        if first:
            chain.append(first.group(1))
        chain.extend(re.findall(r"└─\s*(H[1-6])（", code.group(1)))
        dpi = [int(x) for x in re.findall(r"(?<!\d)(100|125|150|200)(?!\d)", code.group(1))]
    degraded = re.findall(r"H[1-6]", " ".join(
        re.findall(r"降级路径先做\s*([^\n，。]+)", body)))
    return chain, dpi, degraded


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

    print("check-release-specs: root=%s" % root.name)

    # 1. B8 Core 类型齐备
    missing = []
    for fname, pattern in sorted(B8_CORE_TYPES.items()):
        p = core / fname
        if not p.exists():
            missing.append(fname + " (missing)")
        elif not re.search(pattern, read(p)):
            missing.append(fname + " (type not found)")
    if missing:
        bad("B8 Core 规则层不齐备：%s" % "; ".join(missing))
    else:
        ok("B8 Core 规则层齐备（%d 个文件）" % len(B8_CORE_TYPES))

    # 2. App 发布线状态内核齐备
    missing_app = []
    for fname, pattern in sorted(APP_TYPES.items()):
        p = app / fname
        if not p.exists():
            missing_app.append(fname + " (missing)")
        elif not re.search(pattern, read(p)):
            missing_app.append(fname + " (type not found)")
    if missing_app:
        bad("App 发布线状态内核不齐备：%s" % "; ".join(missing_app))
    else:
        ok("App 发布线状态内核齐备（%d 个文件）" % len(APP_TYPES))

    # 3. runner B8 门卡组注册齐备
    ptext = read(tests / "Program.cs")
    groups = re.findall(r'\bRun\(\s*"([^"]+)"', ptext)
    missing_groups = [g for g in EXPECTED_GROUPS if g not in groups]
    if missing_groups:
        bad("runner 缺少 B8 门卡组：%s" % ", ".join(missing_groups))
    else:
        ok("runner B8 门卡组注册齐备（%d 组）" % len(EXPECTED_GROUPS))

    # 4. 规格关键常量与不变量
    inv_bad = []
    for fname, checks in sorted(CORE_INVARIANTS.items()):
        text = read(core / fname)
        for label, rx in checks:
            if not re.search(rx, text, re.S):
                inv_bad.append("%s: %s" % (fname, label))
    if inv_bad:
        bad("B8 规格常量 / 不变量被破坏：%s" % "; ".join(inv_bad))
    else:
        total = sum(len(v) for v in CORE_INVARIANTS.values())
        ok("B8 规格常量 / 不变量齐备（%d 项）" % total)

    # 5. §8.3 阶段链 / DPI 四档 / 降级路径逐项比对（不是只比一个数字）
    design = read(root / "design-v2.md")
    spec_chain, spec_dpi, spec_degraded = section83_tokens(design)
    chain_bad = []
    src_chain = array_body(read(core / "ReleaseStageRules.cs"), "DependencyOrder")
    src_degraded = array_body(read(core / "ReleaseStageRules.cs"), "DegradedOrder")
    if spec_chain != ["C4", "H1", "H3", "H2", "H4", "H5", "H6"]:
        chain_bad.append("§8.3 依赖链解析异常：%s" % spec_chain)
    if src_chain is None or sorted(src_chain) != sorted(spec_chain):
        chain_bad.append("依赖链与 §8.3 不一致（源码 %s）" % src_chain)
    elif src_chain != spec_chain:
        chain_bad.append("依赖链顺序与 §8.3 不一致（源码 %s）" % src_chain)
    if spec_dpi != [100, 125, 150, 200]:
        chain_bad.append("§8.3 DPI 四档解析异常：%s" % spec_dpi)
    dpi_text = read(core / "DpiMatrixRules.cs")
    src_dpi = [int(x) for x in re.findall(r"\b(\d{3})\b",
               re.search(r"new\s+int\[\]\s*\{([^}]*)\}", dpi_text).group(1))] \
        if re.search(r"new\s+int\[\]\s*\{([^}]*)\}", dpi_text) else []
    if src_dpi != spec_dpi:
        chain_bad.append("DPI 四档与 §8.3 不一致（源码 %s）" % src_dpi)
    if spec_degraded != ["H1", "H3", "H2"]:
        chain_bad.append("§8.3 降级路径解析异常：%s" % spec_degraded)
    if src_degraded != spec_degraded:
        chain_bad.append("降级路径与 §8.3 不一致（源码 %s）" % src_degraded)
    if chain_bad:
        bad("§8.3 发布线漂移：%s" % "; ".join(chain_bad))
    else:
        ok("§8.3 依赖链 7 节点 / DPI 四档 / 降级链逐项一致（%d 项）" %
           (len(spec_chain) + len(spec_dpi) + len(spec_degraded)))

    # 6. 六阶段证据要求 + M3 当前结论：不得把静态绿写成可发布
    record_bad = []
    record_text = read(core / "ReleaseRecordRules.cs")
    if not re.search(r"levels\.Length\s*!=\s*ReleaseStageRules\.StageCount", record_text):
        record_bad.append("未要求六阶段齐备")
    if "EvidencePolicyRules.IsVerdictLevel(levels[i])" not in record_text:
        record_bad.append("未逐阶段校验证据等级")
    state_text = read(app / "ReleaseState.cs")
    if "EvidenceLevel.Unverified" not in state_text or "CurrentM3Verdict()" not in state_text:
        record_bad.append("状态内核未把当前六阶段标为未验证")
    if "IsPublishable()" not in state_text:
        record_bad.append("状态内核缺少 M3 判定")
    if record_bad:
        bad("M3 证据闭环被破坏：%s" % "; ".join(record_bad))
    else:
        ok("M3 证据闭环（六阶段逐项证据 + C4 缺位声明 + 当前判定为不可发布）")

    # 7. C4 零误报门槛：AVcount≠0 且实际扫过，不许「没报错」冒充「没被拦」
    av_text = read(core / "AvScanRules.cs")
    av_bad = []
    if not re.search(r"return\s+engineCount\s*>=\s*MinEngineCount\s*&&\s*scanned\s*;", av_text):
        av_bad.append("零误报判定可被无引擎或未扫描绕过")
    if "EvidencePolicyRules.NoErrorIsNotEvidence()" not in av_text:
        av_bad.append("未复用「没报错不是证据」纪律")
    if av_bad:
        bad("C4 零误报门槛被破坏：%s" % "; ".join(av_bad))
    else:
        ok("C4 零误报门槛（AVcount 非零 + 实际扫描 + 没报错不作证据）")

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
        bad("产品程序集触犯 B8 分层纪律：%s" % "; ".join(product_bad[:5]))
    else:
        ok("产品程序集 verb 查询 0 次 / 零手写 COM（%d 个 .cs）" % len(product))

    # 10. 规格与实现一致 + C# 5 语法子集
    consistency = []
    if "| B8 | 发布线（§8.3） | H1–H6 |" not in design:
        consistency.append("§8.1 B8 判据声明被改动")
    if "C4 缺位时按降级路径先做 H1 → H3 → H2" not in design:
        consistency.append("§8.3 降级路径声明被改动")
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
        ok("规格仍为 v1.2 且 §8.1 B8 / §8.3 降级路径未改；C# 5 语法子集（%d 个 .cs）" % len(cs_files))

    print("  N/A   H1–H6 真实取证（高 DPI 截图 / 三段取证 / 兼容矩阵 / WER / 性能采样 / OV 签名）"
          "与 C4 带杀软 VM：未实现 / 未验证")
    if errors:
        for e in errors:
            print("  RED   " + e)
        print("verdict: RED (%d)" % len(errors))
        return len(errors)
    print("verdict: GREEN")
    return 0


if __name__ == "__main__":
    sys.exit(main())
