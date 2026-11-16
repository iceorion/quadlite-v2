#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
R4 静态契约门卡：系统原生右键菜单 COM 宿主（规格外实现批次）。

零第三方依赖，退出码 = 红条数。
  python tools/check_menu_host_specs.py                 # 扫仓库
  python tools/check_menu_host_specs.py --root <目录>  # 负向对照时指向临时副本

只核对「交付物齐备 / 唯一白名单 COM 声明 / SDK 权威 GUID 与 vtable 顺序 /
产品路径零 verb 调用零删项 / 标准四步顺序 / 参数硬校验 / 自有项独立分组 /
IContextMenu2/3 五类消息转发 / 目标快照 / try-finally 释放 / 独立 STA 与默认关闭 /
Host 右键接线与分层纪律 / 构建 CI 链 / 规格 v1.2 未改 / 产品 selftest / 证据文档 /
诚实标注」这些静态事实。

真实 shell 扩展、TrackPopupMenuEx 模态循环、owner-draw 消息时序、PIDL / HMENU
生命周期、崩溃哨兵与自绘兜底 UI 的运行期行为全部标「未验证」；
宿主默认关闭，CI 只编译单 exe 并跑无窗口 --selftest，不实例化 COM。
M1 / M2 / M3 均不成立，见 docs/adr/ADR-v2-014.md。
"""
import argparse
import re
import sys
from pathlib import Path

import com_policy

if hasattr(sys.stdout, "reconfigure"):
    sys.stdout.reconfigure(encoding="utf-8")

ROOT = Path(__file__).resolve().parents[1]

INTERFACE_BLOCK = re.compile(r"internal\s+interface\s+(\w+)\s*\{([\s\S]*?)\n    \}")
METHOD_NAME = re.compile(r"\bint\s+(\w+)\s*\(")
CONST_ID = re.compile(r'public\s+const\s+string\s+(\w+)\s*=\s*"([0-9a-fA-F-]{36})"')

R4_IDS = [
    "ContextMenuIid", "ContextMenu2Iid", "ContextMenu3Iid", "ShellFolderIid",
]
R4_INTERFACES = ["IContextMenu", "IContextMenu2", "IContextMenu3", "IShellFolder"]

# 标准四步：调用点必须按此顺序出现，且四步都在同一宿主文件里。
FOUR_STEPS = [
    ("GetUIObjectOf", r"parent\.GetUIObjectOf\s*\("),
    ("QueryContextMenu", r"menu\.QueryContextMenu\s*\("),
    ("TrackPopupMenuEx", r"MenuNative\.TrackPopupMenuEx\s*\("),
    ("InvokeCommand", r"menu\.InvokeCommand\s*\("),
]

HOST_REQUIRED = [
    ("menu commands type", r"class\s+MenuCommands\b"),
    ("immutable snapshot", r"new\s+MenuTarget\s*\("),
    ("snapshot rule", r"MenuHostContractRules\.UsesImmutableSnapshot\s*\("),
    ("structured status text", r"StatusRules\.ResultText\s*\("),
    ("native entry", r"host\.Show\s*\("),
]

HOST_FORBIDDEN = [
    ("using System.IO", re.compile(r"\busing\s+System\.IO\s*;")),
    ("registry / network", re.compile(r"Microsoft\.Win32|WebClient|\bSocket\b")),
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
    errors = []

    def ok(msg):
        print("  PASS  " + msg)

    def bad(msg):
        errors.append(msg)
        print("  RED   " + msg)

    print("check-menu-host-specs: root=%s" % root.name)

    decl = root / com_policy.COM_DECLARATION_FILE
    host_impl = root / "src" / "QuadLite.Fs" / "NativeMenuHost.cs"
    ui_impl = root / "src" / "QuadLite.Host" / "MenuCommands.cs"
    pane = root / "src" / "QuadLite.Host" / "PaneView.cs"
    window = root / "src" / "QuadLite.Host" / "MainWindow.cs"
    selftest = root / "src" / "QuadLite.Host" / "SelftestRunner.cs"
    proj = root / "src" / "QuadLite.Host" / "QuadLite.Host.csproj"
    build = root / "build.ps1"
    ci = root / ".github" / "workflows" / "ci.yml"
    design = root / "design-v2.md"
    adr = root / "docs" / "adr" / "ADR-v2-014.md"
    negative = root / "docs" / "negative-controls.md"

    decl_text = read(decl)
    impl = read(host_impl)
    ui = read(ui_impl)
    pane_text = read(pane)
    window_text = read(window)

    # 1. 交付物齐备
    assets = [decl, host_impl, ui_impl, pane, window,
              root / "tools" / "check_menu_host_specs.py",
              root / "tools" / "com_policy.py", adr]
    missing = [p.name for p in assets if not p.exists()]
    if missing:
        bad("R4 交付物缺失：%s" % ", ".join(missing))
    else:
        ok("R4 交付物齐备（Fs 宿主 / Host 接线 / COM 白名单 / 门卡 / 单点口径 / ADR-014）")

    # 2. 分层 COM 口径：全部 .cs 只有唯一白名单允许声明
    all_cs = []
    for base in [root / "src", root / "tests"]:
        if base.exists():
            all_cs += sorted(p for p in base.rglob("*.cs") if p.is_file())
    com_hits = []
    verb_hits = []
    delete_hits = []
    raw_hits = []
    for p in all_cs:
        rel = p.relative_to(root).as_posix()
        text = read(p)
        hit = com_policy.declaration_hit(rel, text)
        if hit:
            com_hits.append("%s: `%s`" % (rel, hit))
        hit = com_policy.verb_hit(rel, text)
        if hit:
            verb_hits.append("%s: `%s`" % (rel, hit))
        if com_policy.MENU_DELETION.search(text):
            delete_hits.append(rel)
        if com_policy.RAW_VTABLE.search(text):
            raw_hits.append(rel)
    if com_hits:
        bad("非白名单文件出现 COM 声明：%s" % "; ".join(com_hits[:4]))
    else:
        ok("唯一白名单 COM 声明口径（%d 个 .cs，%s）" %
           (len(all_cs), com_policy.COM_DECLARATION_FILE))
    if verb_hits or delete_hits or raw_hits:
        bad("触犯 verb 调用 / 删项 / 函数指针禁令：%s" %
            "; ".join((verb_hits + delete_hits + raw_hits)[:5]))
    else:
        ok("产品路径零 verb 调用、零删项、零函数指针式 vtable")

    # 3. SDK 权威 GUID 与四段 vtable 顺序
    ids = dict(CONST_ID.findall(decl_text))
    id_bad = []
    for name in R4_IDS:
        if ids.get(name) != com_policy.EXPECTED_IDS[name]:
            id_bad.append("%s (%s != %s)" %
                          (name, ids.get(name, "missing"), com_policy.EXPECTED_IDS[name]))
    if id_bad:
        bad("R4 COM GUID 与 SDK 不一致：%s" % "; ".join(id_bad))
    else:
        ok("R4 四个 COM 标识符与 Windows SDK 10.0.19041.0 逐字一致")

    blocks = dict((name, body) for name, body in INTERFACE_BLOCK.findall(decl_text))
    order_bad = []
    for name in R4_INTERFACES:
        if name not in blocks:
            order_bad.append(name + " (interface missing)")
            continue
        actual = METHOD_NAME.findall(blocks[name])
        if actual != com_policy.EXPECTED_METHODS[name]:
            order_bad.append("%s (%d methods, order/count drift)" % (name, len(actual)))
    if order_bad:
        bad("R4 COM vtable 顺序被破坏：%s" % "; ".join(order_bad))
    else:
        ok("R4 四段 vtable 顺序（3 / 4 / 5 / 10）与 SDK 一致")

    # 4. 标准四步顺序 + 参数硬校验
    positions = []
    step_bad = []
    for label, rx in FOUR_STEPS:
        m = re.search(rx, impl)
        if m is None:
            step_bad.append(label + " (missing)")
        else:
            positions.append(m.start())
    if len(positions) == len(FOUR_STEPS) and positions != sorted(positions):
        step_bad.append("四步调用顺序被打乱")
    hard_bad = [label for label, rx in [
        ("idCmdFirst=1", r"\(uint\)MenuInvokeRules\.IdCmdFirst"),
        ("idCmdLast=0x7FFF", r"\(uint\)MenuInvokeRules\.IdCmdLast"),
        ("maxId from query", r"MenuInvokeRules\.MaxIdFromQueryResult\s*\("),
        ("invoke id range", r"MenuInvokeRules\.InvokeIdInRange\s*\("),
        ("invoke offset", r"MenuInvokeRules\.InvokeOffset\s*\("),
        ("out of range drop", r"MenuDispatchKind\.Dropped"),
        ("dispatch classify", r"MenuInvokeRules\.Classify\s*\("),
    ] if not re.search(rx, impl)]
    if step_bad or hard_bad:
        bad("标准四步 / 参数硬校验被破坏：%s" % "; ".join((step_bad + hard_bad)[:6]))
    else:
        ok("§5.2 标准四步顺序 + idCmdFirst / maxId / 偏移 / 越界丢弃硬校验")

    # 5. 自有项独立分组 + 目标快照 + 消息转发
    group_bad = []
    if not re.search(r"MenuFusionRules\.Resolve\s*\(", impl):
        group_bad.append("源头消重")
    if not re.search(r"MenuModel\.OwnIdBase", impl):
        group_bad.append("自有 ID 段")
    if "MenuFlags.MfSeparator" not in impl:
        group_bad.append("一条分隔线")
    if re.search(r"MenuNative\.DeleteMenu\s*\(", impl):
        group_bad.append("删项")
    if group_bad:
        bad("自有项独立分组被破坏：%s" % "; ".join(group_bad))
    else:
        ok("自有项 ID ≥ 0x8000、一条分隔线独立成组、零删项 / 零插项洗牌")

    snapshot_bad = []
    if "MenuTarget snapshot" not in impl:
        snapshot_bad.append("快照参数")
    if not re.search(r"MenuTargetSnapshotRules\.DispatchUsesSameSnapshot\s*\(", impl):
        snapshot_bad.append("派发同一份快照")
    if not re.search(r"new\s+MenuTarget\s*\(", ui):
        snapshot_bad.append("UI 侧只取一份快照")
    if snapshot_bad:
        bad("目标快照判据被破坏：%s" % "; ".join(snapshot_bad))
    else:
        ok("派发只认打开右键那一刻的 MenuTarget 不可变快照")

    forward_bad = []
    for token in ["MenuFlags.WmInitMenuPopup", "MenuFlags.WmDrawItem",
                  "MenuFlags.WmMeasureItem", "MenuFlags.WmMenuSelect",
                  "MenuFlags.WmCommand"]:
        if token not in impl:
            forward_bad.append("缺 " + token)
    if "MenuMessageRules.MustForward" not in impl:
        forward_bad.append("未走五类消息判据")
    if "HandleMenuMsg2(" not in impl or "HandleMenuMsg(" not in impl:
        forward_bad.append("IContextMenu2/3 转发缺失")
    if not re.search(r"RegisterClassEx\s*\(", impl) or not re.search(r"CreateWindowEx\s*\(", impl):
        forward_bad.append("自建 owner 窗口")
    if forward_bad:
        bad("IContextMenu2/3 消息转发被破坏：%s" % "; ".join(forward_bad[:6]))
    else:
        ok("自建 owner 窗口转发五类菜单消息（优先 HandleMenuMsg2）")

    # 6. try-finally 释放 + 独立 STA + 默认关闭
    release_bad = []
    if "finally" not in impl:
        release_bad.append("try-finally")
    for token in ["MenuNative.DestroyMenu", "Marshal.ReleaseComObject",
                  "MenuNative.CoTaskMemFree", "MenuReleaseRules"]:
        if token not in impl:
            release_bad.append("缺 " + token)
    if impl.find("MenuNative.DestroyMenu") > impl.find("Marshal.ReleaseComObject"):
        release_bad.append("释放顺序不是 HMENU → COM")
    if not re.search(r"SetApartmentState\s*\(\s*ApartmentState\.STA\s*\)", impl):
        release_bad.append("独立 STA 线程")
    if "if (!enabled)" not in impl:
        release_bad.append("默认关闭")
    if "public void Enable()" not in impl:
        release_bad.append("显式启用入口")
    if ".Enable()" in ui:
        release_bad.append("UI 自行启用宿主")
    if release_bad:
        bad("生命周期 / 线程 / 默认关闭被破坏：%s" % "; ".join(release_bad[:6]))
    else:
        ok("每会话 try-finally（HMENU → COM / PIDL）+ 独立 STA + 默认关闭")

    # 7. Host 右键接线与分层纪律 / C# 5
    wiring_bad = [label for label, rx in HOST_REQUIRED if not re.search(rx, ui)]
    if "MouseRightButtonUp" not in pane_text or "SetMenuSink" not in pane_text:
        wiring_bad.append("PaneView 右键接线")
    if "SetMenuSink(OnContextMenu)" not in window_text:
        wiring_bad.append("MainWindow 未接右键")
    if "OnContextMenu" not in window_text or "menuCommands.Show" not in window_text:
        wiring_bad.append("快照未交给 MenuCommands")
    if "native-menu=" not in window_text or "disabled by default" not in window_text:
        wiring_bad.append("状态栏未明示默认关闭")
    if wiring_bad:
        bad("Host 右键接线被破坏：%s" % "; ".join(wiring_bad))
    else:
        ok("PaneView 右键 → MenuTarget 快照 → MenuCommands → 结构化状态栏文案")

    host_cs = sorted(p for p in (root / "src" / "QuadLite.Host").glob("*.cs")
                     if p.is_file()) if (root / "src" / "QuadLite.Host").exists() else []
    layer_bad = []
    cs6_bad = []
    for p in host_cs:
        rel = p.relative_to(root).as_posix()
        text = read(p)
        if com_policy.verb_hit(rel, text):
            layer_bad.append(p.name + ": verb scan")
        for label, rx in HOST_FORBIDDEN:
            if rx.search(text):
                layer_bad.append("%s: %s" % (p.name, label))
        for label, rx in CS6_PLUS:
            if rx.search(text):
                cs6_bad.append("%s: %s" % (p.name, label))
    if layer_bad:
        bad("Host 触犯分层纪律：%s" % "; ".join(layer_bad[:5]))
    else:
        ok("Host 分层纪律（零 System.IO / 零 verb / 零网络 / 零旧口号，%d 个 .cs）" %
           len(host_cs))
    if cs6_bad:
        bad("C# 5 语法子集被破坏：%s" % "; ".join(cs6_bad[:5]))
    else:
        ok("Host C# 5 语法子集（%d 个 .cs）" % len(host_cs))

    # 8. 构建链与 CI
    chain_bad = []
    proj_text = read(proj)
    build_text = read(build)
    ci_text = read(ci)
    if '<Compile Include="MenuCommands.cs" />' not in proj_text:
        chain_bad.append("csproj 缺 MenuCommands.cs")
    for layer in ["QuadLite.Core", "QuadLite.Fs", "QuadLite.App", "QuadLite.Host"]:
        if layer not in build_text:
            chain_bad.append("build.ps1 缺 " + layer)
    if "check-menu-host-specs" not in ci_text:
        chain_bad.append("CI 缺 check-menu-host-specs")
    for token in ["check-ui-specs", "check-transport-specs", "check-menu-specs",
                  "check-realization-specs", "real-exe-build", "real-exe-selftest"]:
        if token not in ci_text:
            chain_bad.append("旧闸门被删：" + token)
    if chain_bad:
        bad("构建 / CI 链被破坏：%s" % "; ".join(chain_bad[:5]))
    else:
        ok("csproj / build.ps1 四层单 exe + CI 新旧闸门齐全")

    # 9. 规格仍是 v1.2，R 系列不写进 §8.1
    design_text = read(design)
    spec_bad = []
    for token in ["已定稿（v1.2）", "1/2/3/4"]:
        if token not in design_text:
            spec_bad.append("规格关键声明被改动：" + token)
    for token in ["R1", "R2", "R3", "R4"]:
        if re.search(r"\|\s*%s\s*\|" % token, design_text):
            spec_bad.append("实现批次 %s 被擅自写进规格" % token)
    if spec_bad:
        bad("规格一致性被破坏：%s" % "; ".join(spec_bad))
    else:
        ok("规格 v1.2 一字未改；R1–R4 仍是规格外实现批次")

    # 10. 产品 --selftest 的 R4 断言 + 证据文档 + 诚实标注
    st_text = read(selftest)
    r4_groups = re.findall(r'probe\.Add\("(r4-[^"]+)"', st_text)
    evidence_bad = []
    if len(r4_groups) < 8:
        evidence_bad.append("产品 --selftest 的 R4 断言不足 8 组（实为 %d）" % len(r4_groups))
    if not adr.exists():
        evidence_bad.append("ADR-014 缺失")
    if "R4" not in read(negative) or "未验证" not in read(negative):
        evidence_bad.append("负向对照未登记 R4 未验证项")
    for token in ["未验证", "默认关闭", "M1"]:
        if token not in impl:
            evidence_bad.append("宿主源码缺诚实标注：" + token)
    if evidence_bad:
        bad("证据链 / 诚实标注被破坏：%s" % "; ".join(evidence_bad[:5]))
    else:
        ok("产品 --selftest %d 组 R4 断言 + ADR-014 + 负向对照登记 + 未验证标注" %
           len(r4_groups))

    print("  N/A   真实 shell 扩展 / TrackPopupMenuEx 模态循环 / owner-draw 时序 / "
          "PIDL 与 HMENU 生命周期 / 崩溃哨兵 / 自绘兜底 UI：未验证")
    print("  N/A   宿主默认关闭，CI 不开窗口、不枚举目录、不实例化 COM；M1 / M2 / M3 均不成立")
    if errors:
        for e in errors:
            print("  RED   " + e)
        print("verdict: RED (%d)" % len(errors))
        return len(errors)
    print("verdict: GREEN")
    return 0


if __name__ == "__main__":
    sys.exit(main())
