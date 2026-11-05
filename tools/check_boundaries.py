#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
check-boundaries 门卡：§4.2 的 B-01..B-07 分层边界 + §3 六条产品不变量（INV-01..06）。

零第三方依赖（只用 Python 3 标准库），本机与 CI 均可重跑。
  python tools/check_boundaries.py                     # 扫仓库 src/ 与仓库文档
  python tools/check_boundaries.py --path <夹具目录>   # 供 run_tests.py 做自测 / 负向对照

退出码 = 红条数；N/A 项（须人工或发布线验证）不判红，但会如实输出「未验证」。
"""
import argparse
import re
import sys
from pathlib import Path

if hasattr(sys.stdout, "reconfigure"):
    sys.stdout.reconfigure(encoding="utf-8")

ROOT = Path(__file__).resolve().parents[1]
TEXT_SUFFIX = {".cs", ".xaml", ".csproj", ".props", ".manifest"}
DOCS = ["design-v2.md", "README.md", "CONTRIBUTING.md", "SECURITY.md", "CODE_OF_CONDUCT.md"]

SLOGANS = re.compile(r"运行期写入为 0|包目录只有 exe|绝不崩溃|与系统文件管理器完全一致")
NEGATION = re.compile(
    r"不得|禁止|非目标|不写|不宣称|追求|失效|旧口号|不搬运|不迁入|不附带|请移除|不记录|不引入|不加载|不一致|不得写成"
)

PATTERNS = {
    "B-01": re.compile(r"System\.IO|System\.Windows|\bProcess\b|Microsoft\.Win32\.Registry|\bRegistryKey\b"),
    "B-02": re.compile(r"using\s+System\.IO"),
    "B-03": re.compile(r"\b(?:File\.(?:Copy|Move|Delete)|Directory\.[A-Za-z]+)\b"),
    "B-04": re.compile(r"GetCommandString"),
    "B-05": re.compile(r"(?:Width|Height)\s*=\s*\"\d"),
    "B-06": re.compile(r"QuadLite\.(?:Probe|Tests)"),
    "B-07": re.compile(r"\b(?:File\.(?:Copy|Move|Delete)|Directory\.(?:Move|Delete|CreateDirectory))\b"),
    "INV-01": re.compile(r"ServiceBase|System\.ServiceProcess|ServiceInstaller|System\.Configuration\.Install"),
    "INV-02": re.compile(r"Microsoft\.Win32|RegistryKey|WebClient|HttpClient|WebRequest|HttpWebRequest|\bSocket\b|%APPDATA%"),
    "INV-03": re.compile(r"SpecialFolder\.(?:ApplicationData|LocalApplicationData)|%APPDATA%|Environment\.GetFolderPath"),
    "INV-05": re.compile(r"SafetyGuard\.(?:Decide|Classify)|\bDangerLevel\b"),
}


def layer_of(path, base):
    """按目录或文件名前缀判定分层；夹具用 Core_/Fs_/App_ 前缀。"""
    rel = path.relative_to(base)
    parts = [p.lower() for p in rel.parts]
    name = rel.name.lower()
    if "quadlite.core" in parts or "core" in parts or "core_" in name:
        return "core"
    if "quadlite.fs" in parts or "fs" in parts or "fs_" in name:
        return "fs"
    if "quadlite.app" in parts or "app" in parts or "app_" in name:
        return "app"
    if any(k in parts for k in ("quadlite.probe", "quadlite.tests", "probe", "tests", "fixtures")):
        return "test"
    return "other"


def source_files(root):
    out = []
    for p in sorted(root.rglob("*")):
        if not p.is_file() or ".git" in p.parts or "__pycache__" in p.parts:
            continue
        if p.suffix.lower() in TEXT_SUFFIX:
            out.append(p)
    return out


def rule_applies(rule, layer, suffix, name):
    if rule == "B-01":
        return layer == "core" and suffix == ".cs"
    if rule == "B-02":
        return layer in ("app", "other") and suffix == ".cs"
    if rule == "B-03":
        return layer == "app" and suffix == ".cs"
    if rule == "B-04":
        return layer != "test" and suffix == ".cs"
    if rule == "B-05":
        return suffix == ".xaml"
    if rule == "B-06":
        return layer != "test" and suffix == ".cs"
    if rule == "B-07":
        return layer not in ("fs", "test") and suffix == ".cs"
    if rule in ("INV-01", "INV-02", "INV-03", "INV-05"):
        return layer != "test" and suffix == ".cs"
    return False


def check_source(root):
    hits = {k: [] for k in PATTERNS}
    files = source_files(root)
    for p in files:
        layer = layer_of(p, root)
        suffix = p.suffix.lower()
        text = p.read_text(encoding="utf-8", errors="replace")
        for rule, rx in PATTERNS.items():
            if not rule_applies(rule, layer, suffix, p.name):
                continue
            if rule == "INV-05" and "safetyguard" in p.name.lower():
                continue  # 判级只允许在 SafetyGuard 一处
            m = rx.search(text)
            if m:
                hits[rule].append("%s: `%s`" % (p.relative_to(root).as_posix(), m.group(0)))
    return hits, len(files)


def check_docs(root):
    """INV-06：旧口号正面命中 0；写入例外清单只含 thumbcache 一条。"""
    problems = []
    for name in DOCS:
        p = root / name
        if not p.exists():
            continue
        for i, line in enumerate(p.read_text(encoding="utf-8").splitlines(), 1):
            if SLOGANS.search(line) and not NEGATION.search(line):
                problems.append("%s:%d 旧口号正面命中" % (name, i))
    nc = root / "docs" / "negative-controls.md"
    if not nc.exists():
        return problems, "N/A 未验证（docs/negative-controls.md 尚未创建）"
    text = nc.read_text(encoding="utf-8")
    m = re.search(r"^##\s+写入例外清单\s*$(.*?)(?=^##\s|\Z)", text, re.M | re.S)
    if not m:
        return problems, "N/A 未验证（写入例外清单章节缺失）"
    items = [l for l in m.group(1).splitlines() if re.match(r"^\s*-\s+\S", l)]
    if len(items) != 1 or "thumbcache" not in " ".join(items).lower():
        problems.append("写入例外清单应恰好 1 条且为 thumbcache，实际 %d 条" % len(items))
    return problems, "ok"


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--path", default=str(ROOT / "src"), help="源码扫描根目录（夹具模式）")
    ap.add_argument("--root", default=str(ROOT), help="仓库根（文档检查用）")
    args = ap.parse_args()

    src_root = Path(args.path).resolve()
    repo_root = Path(args.root).resolve()
    hits, nfiles = check_source(src_root)
    doc_problems, inv06_state = check_docs(repo_root)

    reds = 0
    print("check-boundaries: source files=%d, rules=7, invariants=6" % nfiles)
    for rule in ["B-01", "B-02", "B-03", "B-04", "B-05", "B-06", "B-07"]:
        if hits[rule]:
            reds += 1
            print("  RED   %s: %s" % (rule, "; ".join(hits[rule][:3])))
        else:
            print("  PASS  %s: ok=True" % rule)
    for rule in ["INV-01", "INV-02", "INV-03", "INV-05"]:
        if hits[rule]:
            reds += 1
            print("  RED   %s: %s" % (rule, "; ".join(hits[rule][:3])))
        else:
            print("  PASS  %s: ok=True" % rule)
    print("  N/A   INV-04: 未验证（与资源管理器同手感，人工项 NFR-04）")
    print("  N/A   INV-01/02/03 运行期写入取证：未验证（发布线 H3，不在静态检查范围）")
    print("  N/A   INV-05 危险判级矩阵逐格：未验证（B1/B2 单测）")
    if doc_problems:
        reds += 1
        for d in doc_problems:
            print("  RED   INV-06: %s" % d)
    else:
        print("  PASS  INV-06: ok=True（%s）" % inv06_state)
    if nfiles == 0:
        print("  NOTE  src/ 下无产品源码（B0 骨架），规则由 tools/fixtures 负向对照覆盖")

    if reds:
        print("verdict: RED (%d)" % reds)
        return reds
    print("verdict: GREEN")
    return 0


if __name__ == "__main__":
    sys.exit(main())
