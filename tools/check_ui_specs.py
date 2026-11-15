#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
R3 静态契约门卡：浏览 / 搬运 UI 真实接线（规格外实现批次）。

零第三方依赖，退出码 = 红条数。
  python tools/check_ui_specs.py                 # 扫仓库
  python tools/check_ui_specs.py --root <目录>  # 负向对照时指向临时副本

只核对「交付物齐备 / 真实列表控件 / IDirSource 与迟到批次丢弃 / LayoutMath 四档
真实落位 / 搬运命令接线到 R2 transport / 默认关闭且不冒充成功 / Host 分层纪律与
C# 5 / 单程序集一致性 / CI 与旧闸门 / 规格 v1.2 未改 / 证据文档 / 产品 selftest /
诚实标注」这些静态事实。

真实键鼠交互、真实目录内容、拖放、系统原生菜单 COM 宿主、图标与缩略图渲染、
真实 IFileOperation 运行期行为全部标「未验证」；CI 只编译单 exe 并跑 --selftest，
不开窗口、不实例化 COM。M1 / M2 / M3 均不成立，见 docs/adr/ADR-v2-013.md。
"""
import argparse
import re
import sys
from pathlib import Path

import com_policy

if hasattr(sys.stdout, "reconfigure"):
    sys.stdout.reconfigure(encoding="utf-8")

ROOT = Path(__file__).resolve().parents[1]

UI_FILES = ["HostPolicies.cs", "PaneView.cs", "BrowseCommands.cs"]

PANE_REQUIRED = [
    ("WPF control", r"class\s+PaneView\s*:\s*Border\b"),
    ("real list control", r"new\s+ListBox\s*\("),
    ("IDirSource port", r"\bIDirSource\b"),
    ("worker enumeration", r"dir\.Enumerate\s*\("),
    ("UI thread marshalling", r"Dispatcher\.BeginInvoke\s*\("),
    ("late batch discard (worker)", r"EnumBatchRules\.IsLateBatch\s*\("),
    ("generation advance", r"NavRules\.NextGeneration\s*\("),
    ("cancellation", r"\bcancel\.Cancel\s*\(\s*\)"),
    ("selection from displayed rows", r"\bSelected\s*\(\s*\)"),
    ("status bar rule reuse", r"StatusRules\.(?:VisibleCount|SelectedCount|CountMatchesDisplayedSet)\s*\("),
]

WINDOW_REQUIRED = [
    ("slot source", r"LayoutMath\.Slots\s*\("),
    ("grid placement", r"Grid\.SetRow\s*\("),
    ("grid column placement", r"Grid\.SetColumn\s*\("),
    ("row span", r"Grid\.SetRowSpan\s*\("),
    ("column span", r"Grid\.SetColumnSpan\s*\("),
    ("visibility by mode", r"LayoutMath\.IsPaneVisible\s*\("),
    ("four layout modes", r"LayoutMode\.One[\s\S]{0,220}LayoutMode\.Four"),
    ("address validation", r"AddressRules\.TryParse\s*\("),
    ("real panes", r"new\s+PaneView\s*\("),
    ("status bar", r"status\.Text\s*="),
]

COMMANDS_REQUIRED = [
    ("R2 transport", r"new\s+ShellTransport\s*\(\s*\)"),
    ("Fs port isolation", r"new\s+ShellFileOperator\s*\("),
    ("coordinator", r"new\s+OperationCoordinator\s*\("),
    ("immutable snapshot", r"new\s+MenuTarget\s*\("),
    ("structured request", r"new\s+OpRequest\s*\("),
    ("hard validation", r"OpPlanBuilder\.TryBuild\s*\("),
    ("single execution entry", r"coordinator\.Execute\s*\("),
    ("structured status text", r"StatusRules\.ResultText\s*\("),
    ("closed by default", r"if\s*\(\s*!transport\.Enabled\s*\)"),
    ("honest refusal", r"搬运未执行"),
    ("no inferred success", r"result\.Error\s*!=\s*ErrorClass\.None"),
]

HOST_FORBIDDEN = [
    ("using System.IO", re.compile(r"\busing\s+System\.IO\s*;")),
    ("verb scan", re.compile(r"\bGetCommandString\b")),
    ("registry / network", re.compile(r"Microsoft\.Win32|WebClient|\bSocket\b")),
    ("profile path", re.compile(r"Environment\.GetFolderPath")),
    ("test double", re.compile(r"QuadLite\.(?:Probe|Tests)")),
    ("old slogan", re.compile(r"与系统文件管理器完全一致")),
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


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--root", default=str(ROOT), help="待扫描的仓库根")
    root = Path(ap.parse_args().root).resolve()
    host = root / "src" / "QuadLite.Host"
    errors = []

    def ok(msg):
        print("  PASS  " + msg)

    def bad(msg):
        errors.append(msg)
        print("  RED   " + msg)

    def na(msg):
        print("  N/A   " + msg)

    print("check-ui-specs: root=%s" % root.name)

    # 1. R3 交付物齐备
    assets = [host / name for name in UI_FILES] + [
        root / "tools" / "check_ui_specs.py",
        root / "docs" / "adr" / "ADR-v2-013.md",
    ]
    missing = [p.name for p in assets if not p.exists()]
    if missing:
        bad("R3 交付物缺失：%s" % ", ".join(missing))
    else:
        ok("R3 交付物齐备（Host 3 个 .cs + 门卡 + ADR-013）")

    pane = read(host / "PaneView.cs")
    window = read(host / "MainWindow.cs")
    commands = read(host / "BrowseCommands.cs")
    policies = read(host / "HostPolicies.cs")

    # 2. 真实列表控件 + 3. 端口与迟到批次
    pane_bad = [label for label, rx in PANE_REQUIRED if not re.search(rx, pane)]
    if len(re.findall(r"EnumBatchRules\.IsLateBatch\s*\(", pane)) < 2:
        pane_bad.append("迟到批次未在 worker 与 UI 两侧各判一次")
    if pane_bad:
        bad("真实浏览窗格契约被破坏：%s" % ", ".join(pane_bad[:6]))
    else:
        ok("真实 ListBox + IDirSource 枚举 + 双侧迟到批次丢弃（%d 项）" % len(PANE_REQUIRED))

    # 4. 四档真实落位
    window_bad = [label for label, rx in WINDOW_REQUIRED if not re.search(rx, window)]
    xaml = sorted(p.name for p in host.glob("*.xaml")) if host.exists() else []
    if xaml:
        window_bad.append("Host 引入 XAML 文件")
    if window_bad:
        bad("四档真实落位被破坏：%s" % ", ".join(window_bad[:6]))
    else:
        ok("LayoutMath.Slots 四档落位 + 地址校验 + 状态栏（%d 项）" % len(WINDOW_REQUIRED))

    # 5. 搬运命令接线
    cmd_bad = [label for label, rx in COMMANDS_REQUIRED if not re.search(rx, commands)]
    for token in ["未验证", "ShellTransport", "IFileOperation"]:
        if token not in commands:
            cmd_bad.append("缺「%s」" % token)
    if cmd_bad:
        bad("搬运命令接线被破坏：%s" % ", ".join(cmd_bad[:6]))
    else:
        ok("OpRequest → Coordinator → ShellFileOperator → ShellTransport（%d 项）" %
           len(COMMANDS_REQUIRED))

    # 6. transport 默认关闭 + 不冒充成功（跨文件交叉核对）
    transport = read(root / "src" / "QuadLite.Fs" / "ShellTransport.cs")
    honesty_bad = []
    if "if (!enabled)" not in transport:
        honesty_bad.append("R2 transport 不再是默认关闭")
    if "disabled by default" not in window:
        honesty_bad.append("窗口状态栏未明示 transport 默认关闭")
    if "搬运未执行" not in commands or "未验证" not in commands:
        honesty_bad.append("关闭时未返回「未执行 / 未验证」文案")
    if "Enable()" in commands:
        honesty_bad.append("UI 自行启用真实搬运，违反 R2 默认关闭纪律")
    if honesty_bad:
        bad("默认关闭 / 诚实标注被破坏：%s" % "; ".join(honesty_bad))
    else:
        ok("transport 默认关闭且 UI 明示，绝不把「没有异常」写成成功")

    # 7. 分层 COM 口径（R2 白名单之外零声明）+ Host 纪律 + C# 5
    all_cs = []
    for base in [root / "src", root / "tests"]:
        if base.exists():
            all_cs += sorted(p for p in base.rglob("*.cs") if p.is_file())
    com_hits = []
    for p in all_cs:
        hit = com_policy.declaration_hit(p.relative_to(root).as_posix(), read(p))
        if hit:
            com_hits.append("%s: `%s`" % (p.name, hit))
    if com_hits:
        bad("非白名单文件出现 COM 声明：%s" % "; ".join(com_hits[:4]))
    else:
        ok("R2 分层 COM 口径未被 R3 破坏（%d 个 .cs）" % len(all_cs))

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
        bad("Host 触犯分层纪律：%s" % "; ".join(layer_bad[:5]))
    else:
        ok("Host 分层纪律（零 System.IO / 零 verb / 零网络 / 零旧口号，%d 个 .cs）" % len(host_cs))
    if cs6_bad:
        bad("C# 5 语法子集被破坏：%s" % "; ".join(cs6_bad[:5]))
    else:
        ok("Host C# 5 语法子集（%d 个 .cs）" % len(host_cs))

    # 8. 单程序集一致性：三个新类型必须在 csproj 与 build.ps1 编译集合内
    proj = read(host / "QuadLite.Host.csproj")
    build = read(root / "build.ps1")
    ci = read(root / ".github" / "workflows" / "ci.yml")
    chain_bad = []
    for name in UI_FILES:
        if '<Compile Include="%s" />' % name not in proj:
            chain_bad.append("csproj 缺 " + name)
    for layer in ["QuadLite.Core", "QuadLite.Fs", "QuadLite.App", "QuadLite.Host"]:
        if layer not in build:
            chain_bad.append("build.ps1 缺 " + layer)
    if "check-ui-specs" not in ci:
        chain_bad.append("CI 缺 check-ui-specs")
    for token in ["check-transport-specs", "check-realization-specs",
                  "real-exe-build", "real-exe-selftest"]:
        if token not in ci:
            chain_bad.append("旧闸门被删：" + token)
    if chain_bad:
        bad("构建 / CI 链被破坏：%s" % "; ".join(chain_bad[:5]))
    else:
        ok("csproj 三新类型 + build.ps1 四层单 exe + CI 新旧闸门齐全")

    # 9. 规格仍 v1.2，R 系列不写进 §8.1
    design = read(root / "design-v2.md")
    spec_bad = []
    for token in ["已定稿（v1.2）", "1/2/3/4"]:
        if token not in design:
            spec_bad.append("规格关键声明被改动：" + token)
    for token in ["R1", "R2", "R3"]:
        if re.search(r"\|\s*%s\s*\|" % token, design):
            spec_bad.append("实现批次 %s 被擅自写进规格 §8.1" % token)
    if spec_bad:
        bad("规格一致性被破坏：%s" % "; ".join(spec_bad))
    else:
        ok("规格 v1.2 一字未改；R1 / R2 / R3 仍是规格外实现批次")

    # 10. 证据文档如实标注
    adr = read(root / "docs" / "adr" / "ADR-v2-013.md")
    ledger = read(root / "docs" / "run-ledger.md")
    controls = read(root / "docs" / "negative-controls.md")
    readme = read(root / "README.md")
    doc_bad = []
    for token in ["沿用", "修订", "作废", "未验证", "M1", "R4"]:
        if token not in adr:
            doc_bad.append("ADR-013 缺「%s」" % token)
    if "| R3 |" not in ledger:
        doc_bad.append("run-ledger 缺 R3 行")
    if "R3" not in controls:
        doc_bad.append("negative-controls 缺 R3 不变量")
    if "R3" not in readme or "未验证" not in readme:
        doc_bad.append("README 未登记 R3 与未验证边界")
    if doc_bad:
        bad("证据文档被破坏：%s" % "; ".join(doc_bad[:5]))
    else:
        ok("证据文档齐备（ADR-013 + 账本 R3 + 负向对照 R3 + README）")

    # 11. 产品 --selftest 纳入 R3 契约断言
    st = read(host / "SelftestRunner.cs")
    r3_probes = len(re.findall(r'probe\.Add\s*\(\s*"r3-', st))
    if r3_probes < 4:
        bad("产品 --selftest 缺 R3 契约断言（%d 组，应 ≥4）" % r3_probes)
    else:
        ok("产品 --selftest 接入 %d 组 R3 运行时契约断言" % r3_probes)

    # 12. 诚实标注：源码与判据都不宣称「已验证」
    honesty = []
    for name in UI_FILES:
        if "未验证" not in read(host / name):
            honesty.append(name + " 未标注未验证")
    if not re.search(r"IsRuntimeVerified\s*\(\s*\)\s*\{\s*return\s+false;\s*\}", policies):
        honesty.append("UiContractRules 未声明运行期未验证")
    if "M1 MVP 不成立" not in window:
        honesty.append("窗口未写明 M1 不成立")
    if honesty:
        bad("诚实标注被破坏：%s" % "; ".join(honesty))
    else:
        ok("源码逐项如实标注「未验证」，M1 不成立，运行期判据恒 false")

    na("真实键鼠交互 / 目录内容 / 拖放 / 原生菜单 COM 宿主 / 图标缩略图渲染 / "
       "IFileOperation 运行期行为：未验证，需 H1–H6 真机取证")
    na("R3 只把「可编译、可静态判定的浏览与搬运链路」接入证据闭环；M1 / M2 / M3 均不成立")

    if errors:
        print("verdict: RED (%d)" % len(errors))
        return len(errors)
    print("verdict: GREEN")
    return 0


if __name__ == "__main__":
    sys.exit(main())
