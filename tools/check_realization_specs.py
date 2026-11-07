#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
R1 静态契约门卡：真实实现第一批（WPF 宿主 exe / 产品 --selftest / 单文件构建链）。

零第三方依赖，退出码 = 红条数。
  python tools/check_realization_specs.py                # 扫仓库
  python tools/check_realization_specs.py --root <目录>  # 负向对照时指向临时副本

本脚本只核对「Host 文件齐备 / 入口与窗口契约 / 产品自测契约 / build.ps1 真实编译 /
CI 真跑 / 单程序集一致性 / 分层纪律 / C# 5 / 规格未改 / 证据文档」这些静态事实。

真实 exe 的编译与 --selftest 运行由 CI 的 real-exe-build / real-exe-selftest
两步实测；本机不执行 C# 编译器，一律标「未验证」。
真实 IFileOperation COM 绑定、NativeMenuHost、图标缩略图渲染、H1–H6 取证与
C4 带杀软 VM 均未实现 / 未验证，M1 与 M3 均不成立（见 docs/adr/ADR-v2-011.md）。
"""
import argparse
import re
import sys
from pathlib import Path

if hasattr(sys.stdout, "reconfigure"):
    sys.stdout.reconfigure(encoding="utf-8")

ROOT = Path(__file__).resolve().parents[1]

HOST_TYPES = {
    "Program.cs": r"\bclass\s+Program\b",
    "MainWindow.cs": r"\bclass\s+MainWindow\b",
    "SelftestRunner.cs": r"\bclass\s+SelftestRunner\b",
}
HOST_ASSETS = ["QuadLite.Host.csproj", "app.manifest"]

ENTRY_REQUIRED = [
    ("STAThread", r"\[\s*STAThread\s*\]"),
    ("int Main(string[] args)", r"public\s+static\s+int\s+Main\s*\(\s*string\[\]\s+args\s*\)"),
    ("CLI contract reuse", r"CliArgs\.Parse\s*\(\s*args\s*\)"),
    ("selftest branch", r"cli\.Mode\s*==\s*CliMode\.SelfTest"),
    ("selftest dispatch", r"return\s+SelftestRunner\.Run\s*\(\s*\)"),
    ("WPF message loop", r"app\.Run\s*\(\s*window\s*\)"),
    ("single-instance first stage", r"new\s+Mutex\s*\("),
    ("diagnostic modes honest", r"未实现\s*/\s*未验证"),
]

WINDOW_REQUIRED = [
    ("WPF Window base", r"class\s+MainWindow\s*:\s*Window\b"),
    ("LayoutMath slots reuse", r"LayoutMath\.Slots\s*\("),
    ("four layout modes", r"LayoutMode\.One[\s\S]{0,220}LayoutMode\.Four"),
    ("honest capability boundary", r"未实现\s*/\s*未验证"),
    ("M1 not claimed", r"M1 MVP 不成立"),
]

SELFTEST_REQUIRED = [
    ("detail lines reuse", r"SelftestState\.DetailLines\s*\(\s*\)"),
    ("group line reuse", r"SelftestReportRules\.GroupLine\s*\("),
    ("verdict line reuse", r"SelftestReportRules\.VerdictLine\s*\("),
    ("exit code reuse", r"SelftestReportRules\.ExitCode\s*\(\s*failures\s*\)"),
    ("report lines emitted", r"Console\.WriteLine\s*\(\s*lines\[i\]\s*\)"),
    ("minimum line assertion", r"SelftestReportRules\.MeetsMinimum\s*\(\s*expectedTotal\s*\)"),
    ("M3 evidence honesty", r"ReleaseVerdict\.EvidenceMissing"),
]

BUILD_REQUIRED = [
    "/target:winexe", "/langversion:5", "/win32manifest:", "/out:",
    "PresentationFramework.dll", "System.Xaml.dll", "System.Core.dll",
    "QuadLite.Host", "Get-FileHash", "exe-size", "sha256", "package-selfcheck",
    "single-file packaging = applied",
]
BUILD_FORBIDDEN = ["NOT IMPLEMENTED (B7)", "B0 skeleton", "packaging: not applied"]

CI_REQUIRED = [
    "real-exe-build", "real-exe-selftest", "--selftest",
    "verdict: GREEN", "Start-Process", "-lt 100", "build.ps1",
]
CI_LEGACY = ["check-release-specs", "csharp-runner", "csharp-app-compile"]

HOST_FORBIDDEN = [
    ("using System.IO", re.compile(r"\busing\s+System\.IO\s*;")),
    ("test double reference", re.compile(r"QuadLite\.(?:Probe|Tests)")),
    ("hand-written COM", re.compile(r"\bComImport\b|\bInterfaceType\b|\[\s*Guid\s*\(")),
    ("verb scan", re.compile(r"\bGetCommandString\b")),
    ("registry / network", re.compile(r"Microsoft\.Win32|WebClient|\bSocket\b")),
    ("profile path", re.compile(r"Environment\.GetFolderPath")),
]

CS6_PLUS = [
    ("string interpolation", re.compile(r'\$"')),
    ("null-conditional", re.compile(r"\?\.")),
    ("nameof", re.compile(r"\bnameof\s*\(")),
    ("out var", re.compile(r"\bout\s+var\b")),
    ("null-coalescing assignment", re.compile(r"\?\?=")),
    ("init-only setter", re.compile(r"\binit\s*;")),
]


def read(path):
    if not path.exists():
        return ""
    return path.read_text(encoding="utf-8", errors="replace")


def ok(msg):
    print("  PASS  " + msg)


def bad(msg, errors):
    errors.append(msg)
    print("  RED   " + msg)


def na(msg):
    print("  N/A   " + msg)


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--root", default=str(ROOT), help="待扫描的仓库根")
    root = Path(ap.parse_args().root).resolve()
    host = root / "src" / "QuadLite.Host"
    errors = []

    texts = {}
    for name, pattern in sorted(HOST_TYPES.items()):
        text = read(host / name)
        texts[name] = text
        if not re.search(pattern, text):
            bad("Host 类型缺失或被破坏：%s" % name, errors)
    for name in HOST_ASSETS:
        if not (host / name).exists():
            bad("Host 工程资产缺失：%s" % name, errors)
    if not errors:
        ok("Host 宿主层齐备（%d 个 .cs + %d 个工程资产）" %
           (len(HOST_TYPES), len(HOST_ASSETS)))

    # 1. 入口契约
    entry = texts.get("Program.cs", "")
    entry_bad = [label for label, rx in ENTRY_REQUIRED if not re.search(rx, entry)]
    if entry_bad:
        bad("进程入口契约被破坏：%s" % ", ".join(entry_bad), errors)
    else:
        ok("进程入口契约（%d 项：STA / CLI / selftest 无窗口分支 / 消息循环 / Mutex）" %
           len(ENTRY_REQUIRED))

    # 2. 最小窗口契约：纯 C#，不引入 XAML 编译任务
    window = texts.get("MainWindow.cs", "")
    window_bad = [label for label, rx in WINDOW_REQUIRED if not re.search(rx, window)]
    xaml = sorted(p.name for p in host.glob("*.xaml")) if host.exists() else []
    if xaml:
        window_bad.append("Host 引入 XAML 文件：%s" % ", ".join(xaml))
    if window_bad:
        bad("最小窗口契约被破坏：%s" % ", ".join(window_bad), errors)
    else:
        ok("最小窗口契约（%d 项，纯 C# 控件树，四档槽位取自 LayoutMath）" %
           len(WINDOW_REQUIRED))

    # 3. 产品 --selftest 契约：真跑断言，不是纸面清单
    st = texts.get("SelftestRunner.cs", "")
    st_bad = [label for label, rx in SELFTEST_REQUIRED if not re.search(rx, st)]
    probes = len(re.findall(r"probe\.Add\s*\(", st))
    if probes < 10:
        st_bad.append("运行时断言组 %d 条（应 ≥10）" % probes)
    if "CliArgs.Parse" not in st or "LayoutMath.IsKnownMode" not in st:
        st_bad.append("未复用 Core 真实契约做断言")
    if st_bad:
        bad("产品 --selftest 契约被破坏：%s" % ", ".join(st_bad), errors)
    else:
        ok("产品 --selftest 契约（%d 项，%d 组运行时断言，复用 Core / App 状态内核）" %
           (len(SELFTEST_REQUIRED), probes))

    # 4. build.ps1：真实编译链，不得残留 B0 骨架占位
    build = read(root / "build.ps1")
    build_bad = [token for token in BUILD_REQUIRED if token not in build]
    build_bad += ["残留 B0 占位：" + token for token in BUILD_FORBIDDEN if token in build]
    if "& $CscPath @cscArgs" not in build:
        build_bad.append("未真正调用编译器")
    if build_bad:
        bad("真实构建链被破坏：%s" % "; ".join(build_bad[:5]), errors)
    else:
        ok("build.ps1 真实单文件构建链（%d 项，四层源码一次编译 + BUILD.txt 七字段）" %
           len(BUILD_REQUIRED))

    # 5. CI 必须真跑 exe，且旧闸门一条不删
    ci = read(root / ".github" / "workflows" / "ci.yml")
    ci_bad = [token for token in CI_REQUIRED if token not in ci]
    ci_bad += ["旧闸门缺失：" + token for token in CI_LEGACY if token not in ci]
    if ci_bad:
        bad("CI 真跑链被破坏：%s" % "; ".join(ci_bad[:5]), errors)
    else:
        ok("CI 真跑链（real-exe-build + real-exe-selftest，%d 项；旧闸门未删）" %
           len(CI_REQUIRED))

    # 6. 单程序集一致性：csproj 与 build.ps1 都把四层源码编进一个 exe
    proj = read(host / "QuadLite.Host.csproj")
    proj_bad = []
    for token in ["OutputType>WinExe", "UseWPF>true", "AssemblyName>QuadLite<",
                  "EnableDefaultCompileItems>false", "ApplicationManifest"]:
        if token not in proj:
            proj_bad.append("csproj 缺 " + token)
    for layer in ["QuadLite.Core", "QuadLite.Fs", "QuadLite.App"]:
        if layer not in proj:
            proj_bad.append("csproj 未编入 " + layer)
    if "ProjectReference" in proj:
        proj_bad.append("csproj 引入旁路程序集引用")
    for layer in ["QuadLite.Core", "QuadLite.Fs", "QuadLite.App", "QuadLite.Host"]:
        if layer not in build:
            proj_bad.append("build.ps1 未编入 " + layer)
    if proj_bad:
        bad("单程序集一致性被破坏：%s" % "; ".join(proj_bad[:5]), errors)
    else:
        ok("单程序集一致性（csproj 与 build.ps1 均把四层源码编进 QuadLite.exe）")

    # 7. Host 分层纪律 + C# 5 语法子集
    host_cs = sorted(p for p in host.glob("*.cs") if p.is_file()) if host.exists() else []
    layer_bad = []
    cs6_bad = []
    for p in host_cs:
        text = read(p)
        for label, rx in HOST_FORBIDDEN:
            if rx.search(text):
                layer_bad.append("%s: %s" % (p.name, label))
        for label, rx in CS6_PLUS:
            if rx.search(text):
                cs6_bad.append("%s: %s" % (p.name, label))
    if layer_bad:
        bad("Host 触犯分层纪律：%s" % "; ".join(layer_bad[:5]), errors)
    else:
        ok("Host 分层纪律（零 System.IO / 零手写 COM / 零 verb 查询 / 零网络，%d 个 .cs）" %
           len(host_cs))
    if cs6_bad:
        bad("C# 5 语法子集被破坏：%s" % "; ".join(cs6_bad[:5]), errors)
    else:
        ok("Host C# 5 语法子集（%d 个 .cs）" % len(host_cs))

    # 8. 规格 v1.2 一字未改；R 系列是规格外的实现批次，不写进规格
    design = read(root / "design-v2.md")
    spec_bad = []
    for token in ["已定稿（v1.2）", "| B8 | 发布线（§8.3） | H1–H6 |", "1/2/3/4"]:
        if token not in design:
            spec_bad.append("规格关键声明被改动：" + token)
    if re.search(r"\|\s*R1\s*\|", design):
        spec_bad.append("实现批次 R1 被擅自写进规格 §8.1")
    if spec_bad:
        bad("规格一致性被破坏：%s" % "; ".join(spec_bad), errors)
    else:
        ok("规格仍为 v1.2 且 §8.1 批次表未改；R 系列为规格外实现批次")

    # 9. 证据文档：ADR / 账本 / 负向对照 / README 都要如实标注
    adr = read(root / "docs" / "adr" / "ADR-v2-011.md")
    ledger = read(root / "docs" / "run-ledger.md")
    controls = read(root / "docs" / "negative-controls.md")
    readme = read(root / "README.md")
    doc_bad = []
    for token in ["沿用", "修订", "作废", "未验证", "M1"]:
        if token not in adr:
            doc_bad.append("ADR-011 缺「%s」" % token)
    if "| R1 |" not in ledger:
        doc_bad.append("run-ledger 缺 R1 行")
    if "R1" not in controls:
        doc_bad.append("negative-controls 缺 R1 不变量")
    if "R1" not in readme or "未验证" not in readme:
        doc_bad.append("README 未登记 R1 与未验证边界")
    if doc_bad:
        bad("证据文档被破坏：%s" % "; ".join(doc_bad[:5]), errors)
    else:
        ok("证据文档齐备（ADR-011 沿用/修订/作废 + 账本 R1 + 负向对照 R1 + README）")

    na("本机不执行 C# 编译器：真实 exe 编译与 --selftest 退出码以 CI 远端实测为准")
    na("真实 IFileOperation COM 绑定 / NativeMenuHost / 图标缩略图渲染 / 拖放 / "
       "H1–H6 取证 / C4 带杀软 VM：未实现或未验证；M1 与 M3 均不成立")

    if errors:
        print("verdict: RED (%d)" % len(errors))
        return len(errors)
    print("verdict: GREEN")
    return 0


if __name__ == "__main__":
    sys.exit(main())
