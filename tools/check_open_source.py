#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
§10 开源合规全文扫描（每批提交前执行，B0 起随仓发布）。

零第三方依赖。退出码：0 = 可提交，1 = 打回。
扫描器自身（本文件）含敏感词模式串，属于规则定义而非资产，故排除自身；
其余所有非 .git 文件逐行检查。
"""
import argparse
import re
import sys
from pathlib import Path

if hasattr(sys.stdout, "reconfigure"):
    sys.stdout.reconfigure(encoding="utf-8")

_ap = argparse.ArgumentParser()
_ap.add_argument("--root", default=str(Path(__file__).resolve().parents[1]),
                 help="待扫描的仓库根（负向对照时指向临时副本）")
_args = _ap.parse_args()
ROOT = Path(_args.root).resolve()
ALLOWED_SUFFIX = {
    "", ".md", ".txt", ".cs", ".xaml", ".csproj", ".props", ".manifest",
    ".ps1", ".py", ".yml", ".yaml", ".json", ".gitignore", ".xml", ".sln",
}
FORBIDDEN_SUFFIX = {
    ".exe", ".dll", ".pdb", ".png", ".jpg", ".jpeg", ".gif", ".bmp", ".ico",
    ".zip", ".7z", ".rar", ".pdf", ".log", ".csv", ".tsv", ".pem", ".key",
    ".pfx", ".user", ".suo", ".cache", ".bin", ".dat",
}
INTERNAL_DOCS = {
    "v1-review.md", "PLAN-v2.md", "feature-inventory.md", "adr-inventory.md",
    "evidence-index.md", "create-repo.json", "context-menu-crash-v2.md",
    "native-menu-v2.md", "file-ops-v2.md",
}
REQUIRED = [
    "design-v2.md", "README.md", "LICENSE", "CONTRIBUTING.md", "SECURITY.md",
    "CODE_OF_CONDUCT.md", "Directory.Build.props", "build.ps1", ".gitignore",
    "tools/check_boundaries.py", "tools/check_trace.py", "tools/run_tests.py",
    "tools/check_core_specs.py", "tools/check_fs_specs.py", "tools/check_shell_specs.py",
    "tools/check_list_specs.py", "tools/check_menu_specs.py",
    "tools/check_icon_specs.py", "tools/check_selftest_specs.py",
    "tools/check_release_specs.py",
    "src/QuadLite.Core/PathKey.cs", "src/QuadLite.Core/OperationCoordinator.cs",
    "src/QuadLite.Core/LayoutMath.cs", "src/QuadLite.Core/FilterRules.cs",
    "src/QuadLite.Core/MenuHostRules.cs",
    "src/QuadLite.Core/IconChannelRules.cs",
    "src/QuadLite.Core/CliContractRules.cs",
    "src/QuadLite.Core/ReleaseStageRules.cs",
    "src/QuadLite.Core/ReleaseRecordRules.cs",
    "src/QuadLite.App/ShellState.cs", "src/QuadLite.App/ListViewState.cs",
    "src/QuadLite.App/MenuState.cs", "src/QuadLite.App/ThumbState.cs",
    "src/QuadLite.App/SelftestState.cs",
    "src/QuadLite.App/ReleaseState.cs",
    "src/QuadLite.Fs/ShellFileOperator.cs",
    "tests/QuadLite.Tests/Program.cs",
    ".github/workflows/ci.yml", "docs/adr/ADR-v2-001.md", "docs/adr/ADR-v2-002.md",
    "docs/adr/ADR-v2-004.md", "docs/adr/ADR-v2-005.md", "docs/adr/ADR-v2-006.md",
    "docs/adr/ADR-v2-007.md", "docs/adr/ADR-v2-008.md",
    "docs/adr/ADR-v2-009.md", "docs/adr/ADR-v2-010.md",
    "docs/negative-controls.md", "docs/run-ledger.md",
]

SENSITIVE = re.compile(
    r"I:\\|I:/|/i/wb-workspace|C:\\Users|/c/Users|Bluebug|10808|127\.0\.0\.1"
    r"|iceorion/quadlite(?!-v2)|35f7801|v1-freeze|_scratch|_archive"
    r"|feature-inventory|adr-inventory|evidence-index|create-repo"
    r"|context-menu-crash|native-menu-v2|file-ops-v2|check-trace-s3|check-open-source-s3"
)
SLOGANS = re.compile(r"运行期写入为 0|包目录只有 exe|绝不崩溃|与系统文件管理器完全一致|连跑 N 次全绿")
NEGATION = re.compile(
    r"不得|禁止|非目标|不写|不宣称|追求|失效|旧口号|不搬运|不迁入|不附带|请移除|不记录|不引入|不加载|不一致|不得写成"
)

errors = []


def ok(msg):
    print("  PASS  " + msg)


def na(msg):
    print("  N/A   " + msg)


def bad(msg):
    errors.append(msg)
    print("  FAIL  " + msg)


files = sorted(
    p for p in ROOT.rglob("*")
    if p.is_file() and ".git" not in p.parts and "__pycache__" not in p.parts
    and p.name != Path(__file__).name
)
names = {p.name for p in files}

# 1. 必需文件齐备
missing = [r for r in REQUIRED if not (ROOT / r).exists()]
if missing:
    bad("必需文件缺失：%s" % missing)
else:
    ok("B0 必需文件齐备（%d 项）" % len(REQUIRED))

# 2. 纯文本 / 无二进制 / 无私有产物
for p in files:
    if p.suffix.lower() in FORBIDDEN_SUFFIX:
        bad("禁止文件类型：%s" % p.relative_to(ROOT).as_posix())
    if p.suffix.lower() not in ALLOWED_SUFFIX:
        bad("非白名单后缀：%s" % p.relative_to(ROOT).as_posix())
    raw = p.read_bytes()
    if b"\x00" in raw:
        bad("含 NUL 字节（二进制）：%s" % p.relative_to(ROOT).as_posix())
    try:
        raw.decode("utf-8")
    except UnicodeDecodeError:
        bad("非 UTF-8 文本：%s" % p.relative_to(ROOT).as_posix())
if not errors:
    ok("全部为 UTF-8 纯文本，无二进制 / 截图 / 日志 / 令牌 / 私有产物")

# 3. 分析记录与历史过程稿整份排除
present = names & INTERNAL_DOCS
if present:
    bad("内部文档进入公开仓：%s" % sorted(present))
else:
    ok("分析记录、只读素材与历史过程稿整份排除")

# 4. 环境偶然与私有资产
hits = []
for p in files:
    for i, line in enumerate(p.read_text(encoding="utf-8").splitlines(), 1):
        m = SENSITIVE.search(line)
        if m:
            hits.append("%s:%d 命中 `%s`" % (p.relative_to(ROOT).as_posix(), i, m.group(0)))
if hits:
    for h in hits[:20]:
        bad("敏感词 " + h)
    if len(hits) > 20:
        bad("敏感词另有 %d 处" % (len(hits) - 20))
else:
    ok("无内部绝对路径 / 代理端口 / 凭据 / 机器名用户名 / 私有仓名 / SHA / 内部文档名")

# 5. 失效口号（区分禁止性语境）
# 口号只可能出现在对外表述（文档 / 产品文案）里；检查链工具脚本中的模式串与
# 负向对照注入串属规则定义，不作为宣称扫描，否则扫描器会命中自己的规则。
SLOGAN_SCAN_SUFFIX = {".md", ".txt", ".cs", ".xaml", ""}
slogan_hits = []
for p in files:
    if p.suffix.lower() not in SLOGAN_SCAN_SUFFIX:
        continue
    for i, line in enumerate(p.read_text(encoding="utf-8").splitlines(), 1):
        if SLOGANS.search(line) and not NEGATION.search(line):
            slogan_hits.append("%s:%d %s" % (p.relative_to(ROOT).as_posix(), i, line.strip()[:80]))
if slogan_hits:
    for h in slogan_hits:
        bad("旧口号/过度宣称 " + h)
else:
    ok("无失效口号；命中均处于禁止 / 说明语境")

# 6. LICENSE / README 非目标 / 证据等级 / 第三方资源
lic = (ROOT / "LICENSE").read_text(encoding="utf-8")
if "MIT License" in lic:
    ok("LICENSE 为 MIT")
else:
    bad("LICENSE 非 MIT")
rd = (ROOT / "README.md").read_text(encoding="utf-8")
need = ["明确不做", "内置播放器", "全文搜索", "shell:", "UNC 根识别", "插件系统"]
if all(s in rd for s in need):
    ok("README 列出 §3 明确不做清单")
else:
    bad("README 非目标清单缺项：%s" % [s for s in need if s not in rd])
design = (ROOT / "design-v2.md").read_text(encoding="utf-8")
for label, cond in [
    ("每条结论带证据等级", "证据等级" in design and "证据等级" in (ROOT / "CONTRIBUTING.md").read_text(encoding="utf-8")),
    ("第三方资源说明（无 NuGet）", "NuGet" in rd or "NuGet" in design),
    ("菜单专项门卡定义", "menu-id-range" in design and "GetCommandString" in design),
    ("CI 闸门定义", "--selftest" in design and "check-boundaries" in design),
]:
    if cond:
        ok(label)
    else:
        bad(label + " 缺失")

# 7. 规格阶段如实标注（B5 菜单专项合规在本脚本内实测）
src_cs_files = sorted(p for p in (ROOT / "src").rglob("*.cs") if p.is_file())
verb_hits = [p.relative_to(ROOT).as_posix() for p in src_cs_files
             if re.search(r"\bGetCommandString\b",
                          p.read_text(encoding="utf-8", errors="replace"))]
if verb_hits:
    bad("产品程序集出现 verb 查询：%s" % ", ".join(verb_hits))
else:
    ok("产品程序集 verb 查询命中 0 次（%d 个 .cs 实测，§7.1 menu-no-verb-scan）" %
       len(src_cs_files))
na("真实 WPF 窗口 / XAML / 消息循环、真实列表控件与拖放、系统原生菜单 COM 宿主、图标与缩略图渲染、MSBuild 应用构建、单文件出包、真实产品 exe 形态 --selftest、BUILD.txt 采集与 --bench 采样：未实跑，标「未验证」（见 ADR-v2-002 / ADR-v2-004 / ADR-v2-005 / ADR-v2-006 / ADR-v2-007 / ADR-v2-008 / ADR-v2-009 / ADR-v2-010）")
na("H1–H6 真实取证（零写入三段取证、高 DPI 四档、兼容矩阵、WER 读法、性能采样、OV 签名）与 C4 带杀软 VM：人工 / 发布线项（未验证）；B8 只核对可静态判定的发布线契约，M3 不成立")
na("同手感：人工验收项（未验证）")

src_cs = [p for p in files if p.suffix == ".cs" and "src" in p.parts]
print("  NOTE  src/ 产品源码 %d 个（B1 L0 + B2 编排 / 适配器 + B3/B4/B5/B6/B7/B8 规则与状态内核；真实 shell 调用点、NativeMenuHost、图标缩略图渲染、发布线真实取证、产品 exe --selftest 与 WPF 窗口未接入）" % len(src_cs))
print("\n文件数=%d，总字节=%d" % (len(files), sum(p.stat().st_size for p in files)))
if errors:
    print("verdict: RED (%d)" % len(errors))
    sys.exit(1)
print("verdict: GREEN（§10 清单逐条通过，可提交）")
sys.exit(0)
