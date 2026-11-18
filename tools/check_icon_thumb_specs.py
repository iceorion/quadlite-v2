#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
R5 静态契约门卡：图标 / 缩略图取图链路与呈现接线（规格外实现批次）。

零第三方依赖，退出码 = 红条数。
  python tools/check_icon_thumb_specs.py                 # 扫仓库
  python tools/check_icon_thumb_specs.py --root <目录>  # 负向对照时指向临时副本

只核对「交付物齐备 / 唯一白名单 COM 声明 / SDK 权威 GUID 与 vtable 顺序 /
SIIGBF flags 与 SIZE / 四级通道与 WIC 白名单 / EXIF 八值 / 单 worker 与 generation
迟到丢弃 / LRU 双闸与负缓存 / 视频看门狗与唯一写入例外 / 独立 STA 与默认关闭 /
try-finally 释放 / Host 呈现接线与分层纪律 / 构建 CI 链 / 规格 v1.2 未改 /
产品 --selftest / 证据文档 / 诚实标注」这些静态事实。

真实系统图像工厂、HBITMAP 逐像素比对、WIC 解码介质、三段取证、thumbcache 写入
与图标呈现运行期全部标「未验证」；宿主默认关闭，CI 只编译单 exe 并跑无窗口
--selftest，不实例化 COM、不解码、不写缓存。M1 / M2 / M3 均不成立，
见 docs/adr/ADR-v2-015.md。
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

R5_IDS = ["ImageFactoryIid"]
R5_INTERFACES = ["IShellItemImageFactory"]

SIIGBF_FLAGS = [
    ("ResizeToFit", "0x0000"), ("BiggerSizeOk", "0x0001"),
    ("MemoryOnly", "0x0002"), ("IconOnly", "0x0004"),
    ("ThumbnailOnly", "0x0008"), ("InCacheOnly", "0x0010"),
    ("CropToSquare", "0x0020"), ("WideThumbnails", "0x0040"),
    ("IconBackground", "0x0080"), ("ScaleUp", "0x0100"),
]

FS_REQUIRED = [
    ("contract rules type", r"class\s+IconThumbContractRules\b"),
    ("immutable snapshot type", r"class\s+IconThumbRequest\b"),
    ("structured result type", r"class\s+IconThumbResult\b"),
    ("host type", r"class\s+IconThumbHost\b"),
    ("SDK GUID via ShellIds", r"new Guid\(ShellIds\.ImageFactoryIid\)"),
    ("factory creation", r"ImageFactoryNative\.SHCreateItemForImage\s*\("),
    ("GetImage call", r"factory\.GetImage\s*\("),
    ("SIZE by value", r"new\s+Size32\s*\("),
    ("SIIGBF flags", r"ImageFactoryFlags\."),
    ("four channel validation", r"IconChannelRules\.IsKnown\s*\("),
    ("cache key", r"ThumbCacheRules\.Key\s*\("),
    ("type isolation", r"ThumbCacheRules\.PrefixOf\s*\("),
    ("sole write exception", r"ThumbWriteRules\.WriteAllowed\s*\("),
    ("watchdog budget", r"VideoThumbRules\.TimeoutSeconds\s*\*\s*1000"),
    ("bounded join", r"worker\.Join\(budgetMs\)"),
    ("independent STA", r"SetApartmentState\s*\(\s*ApartmentState\.STA\s*\)"),
    ("closed by default", r"if \(!enabled\)"),
    ("explicit enable entry", r"public void Enable\(\)"),
    ("COM release", r"Marshal\.ReleaseComObject\s*\("),
    ("HBITMAP release", r"ImageFactoryNative\.DeleteObject\s*\("),
    ("try-finally", r"\bfinally\b"),
    ("runtime honesty", r"未验证"),
    ("milestone honesty", r"M1 / M2 / M3"),
]

CONTRACT_METHODS = [
    "DeclaredImageFactoryMethods", "FourIconChannels", "JumboFallsBackTo48",
    "ExeTwoStageAndDriveRealPath", "WicWhitelistIntact", "ExifEightValues",
    "SingleWorkerVisibleFirst", "LateResultDiscarded", "LruBothGatesAndIsolation",
    "NegativeCacheNoRetry", "VideoFactoryAndWatchdog", "SoleWriteException",
    "ToggleKeepsRowSet", "RequiresStaThread", "IsRuntimeVerified",
]

HOST_REQUIRED = [
    ("commands type", r"class\s+IconThumbCommands\b"),
    ("default-closed provider", r"if \(!host\.Enabled\)\s*return null\s*;"),
    ("zero-IO app dispatch", r"ThumbCommands\.Request\s*\("),
    ("structured decision", r"ThumbRequestResult decision"),
    ("immutable snapshot", r"new\s+IconThumbRequest\s*\("),
    ("WIC system wrapper", r"BitmapDecoder\.Create\s*\("),
    ("EXIF orientation", r"ExifOrientationRules\.(?:Normalize|QuarterTurns|FlipHorizontal)\s*\("),
    ("two-tier decode", r"IconSize\.(?:Large96|Jumbo256)"),
    ("WPF transform", r"new\s+TransformedBitmap\s*\("),
    ("HBITMAP to BitmapSource", r"Imaging\.CreateBitmapSourceFromHBitmap\s*\("),
    ("explicit bitmap release", r"host\.Release\s*\("),
    ("write exception entry", r"VideoCacheWriteAllowed"),
    ("honest contract", r"ContractIsHonest\s*\("),
]

HOST_PANE_REQUIRED = [
    ("item template", r"list\.ItemTemplate\s*=\s*template\s*;"),
    ("icon provider injection", r"SetIconProvider\s*\("),
    ("icon binding", r'new Binding\("Icon"\)'),
    ("notify property changed", r"\bINotifyPropertyChanged\b"),
    ("background worker", r"new\s+Thread\s*\("),
    ("generation recheck", r"ThumbQueueRules\.ShouldApplyResult\s*\("),
    ("cross-thread freeze", r"icon\.Freeze\s*\("),
]

HOST_WINDOW_REQUIRED = [
    ("provider wired", r"SetIconProvider\(OnIconRequest\)"),
    ("icon request method", r"OnIconRequest\(PathKey key, int generation\)"),
    ("provide call", r"iconCommands\.Provide\s*\("),
    ("status bar field", r"icon-thumb="),
    ("default closed status", r"disabled by default"),
]

HOST_FORBIDDEN = [
    ("using System.IO", re.compile(r"\busing\s+System\.IO\s*;")),
    ("hand-written COM", re.compile(r"\bComImport\b|\bInterfaceType\b|\[\s*Guid\s*\(")),
    ("test double", re.compile(r"QuadLite\.(?:Probe|Tests)")),
    ("registry / network", re.compile(r"Microsoft\.Win32|WebClient|\bSocket\b")),
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


def write_exception_items(root):
    text = read(root / "docs" / "negative-controls.md")
    m = re.search(r"^##\s+写入例外清单\s*$(.*?)(?=^##\s|\Z)", text, re.M | re.S)
    if not m:
        return []
    return [l for l in m.group(1).splitlines() if re.match(r"^\s*-\s+\S", l)]


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--root", default=str(ROOT), help="待扫描的仓库根")
    root = Path(ap.parse_args().root).resolve()
    errors = []

    def ok(msg):
        print("  PASS  " + msg)

    def bad(msg):
        errors.append(msg)

    print("check-icon-thumb-specs: root=%s" % root.name)

    decl = root / com_policy.COM_DECLARATION_FILE
    fs_impl = root / "src" / "QuadLite.Fs" / "IconThumbHost.cs"
    ui_impl = root / "src" / "QuadLite.Host" / "IconThumbCommands.cs"
    pane = root / "src" / "QuadLite.Host" / "PaneView.cs"
    window = root / "src" / "QuadLite.Host" / "MainWindow.cs"
    selftest = root / "src" / "QuadLite.Host" / "SelftestRunner.cs"
    proj = root / "src" / "QuadLite.Host" / "QuadLite.Host.csproj"
    build = root / "build.ps1"
    ci = root / ".github" / "workflows" / "ci.yml"
    design = root / "design-v2.md"
    adr = root / "docs" / "adr" / "ADR-v2-015.md"
    negative = root / "docs" / "negative-controls.md"

    decl_text = read(decl)
    fs = read(fs_impl)
    ui = read(ui_impl)
    pane_text = read(pane)
    window_text = read(window)

    # 1. 交付物齐备
    assets = [decl, fs_impl, ui_impl, pane, window, proj,
              root / "tools" / "check_icon_thumb_specs.py",
              root / "tools" / "com_policy.py", adr]
    missing = [p.name for p in assets if not p.exists()]
    if missing:
        bad("R5 交付物缺失：%s" % ", ".join(missing))
    else:
        ok("R5 交付物齐备（Fs 宿主 / Host 呈现 / COM 白名单 / 门卡 / 单点口径 / ADR-015）")

    # 2. 唯一白名单 COM 声明 + 零 verb / 删项 / 函数指针
    all_cs = []
    for base in [root / "src", root / "tests"]:
        if base.exists():
            all_cs += sorted(p for p in base.rglob("*.cs") if p.is_file())
    com_hits, verb_hits, delete_hits, raw_hits = [], [], [], []
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
        bad("触犯 verb / 删项 / 函数指针禁令：%s" %
            "; ".join((verb_hits + delete_hits + raw_hits)[:5]))
    else:
        ok("产品路径零 verb 调用、零删项、零函数指针式 vtable")

    # 3. SDK 权威 GUID 与全表 vtable 顺序（第三套自动纳入单点口径）
    ids = dict(CONST_ID.findall(decl_text))
    id_bad = []
    for name, expected in sorted(com_policy.EXPECTED_IDS.items()):
        if ids.get(name) != expected:
            id_bad.append("%s (%s != %s)" % (name, ids.get(name, "missing"), expected))
    if id_bad:
        bad("COM GUID 与 SDK 不一致：%s" % "; ".join(id_bad))
    else:
        ok("%d 个 COM 标识符与 Windows SDK 10.0.19041.0 逐字一致（含 R5 新增 1 个）" %
           len(com_policy.EXPECTED_IDS))

    blocks = dict((name, body) for name, body in INTERFACE_BLOCK.findall(decl_text))
    order_bad = []
    for name, expected in sorted(com_policy.EXPECTED_METHODS.items()):
        if name not in blocks:
            order_bad.append(name + " (interface missing)")
            continue
        actual = METHOD_NAME.findall(blocks[name])
        if actual != expected:
            order_bad.append("%s (%d methods, order/count drift)" % (name, len(actual)))
    if order_bad:
        bad("COM vtable 顺序被破坏：%s" % "; ".join(order_bad))
    else:
        ok("%d 段 vtable 与 SDK 一致（R2 三段 + R4 四段 + R5 一段）" %
           len(com_policy.EXPECTED_METHODS))

    # 4. SIIGBF flags / SIZE / Win32 入口
    flags_bad = []
    for label, value in SIIGBF_FLAGS:
        if not re.search(r"public\s+const\s+uint\s+%s\s*=\s*%s\b" % (label, value), decl_text):
            flags_bad.append(label)
    if "struct Size32" not in decl_text:
        flags_bad.append("Size32")
    if "public int cx;" not in decl_text or "public int cy;" not in decl_text:
        flags_bad.append("SIZE cx/cy")
    if 'EntryPoint = "SHCreateItemFromParsingName"' not in decl_text:
        flags_bad.append("SHCreateItemFromParsingName entry")
    if 'DllImport("gdi32.dll")' not in decl_text or "DeleteObject(IntPtr" not in decl_text:
        flags_bad.append("gdi32 DeleteObject")
    if flags_bad:
        bad("R5 SDK flags / SIZE / Win32 入口被破坏：%s" % ", ".join(flags_bad[:6]))
    else:
        ok("SIIGBF 十个值 / SIZE 结构 / 两个 Win32 入口与 SDK 一致")

    # 5. Fs 宿主契约：规则层齐备 + 真实调用链 + 生命周期
    fs_bad = [label for label, rx in FS_REQUIRED if not re.search(rx, fs)]
    contract_bad = [m for m in CONTRACT_METHODS
                    if not re.search(r"public\s+static\s+\w+\s+%s\s*\(" % m, fs)]
    if contract_bad:
        fs_bad.append("契约规则缺失：" + ", ".join(contract_bad[:4]))
    if ".Enable()" in ui and "host.Enable()" in ui:
        fs_bad.append("Host 自行启用宿主")
    finally_pos = fs.find("finally")
    if finally_pos < 0 or fs.find("Marshal.ReleaseComObject") < finally_pos:
        fs_bad.append("COM 释放未写在会话 finally 内")
    if fs_bad:
        bad("Fs 图标缩略图宿主契约被破坏：%s" % "; ".join(fs_bad[:6]))
    else:
        ok("Fs 宿主：%d 项调用 / 生命周期判据 + %d 个零 I/O 契约方法" %
           (len(FS_REQUIRED), len(CONTRACT_METHODS)))

    # 6. Host 呈现接线 / WIC 系统包装 / 分层纪律 / C# 5
    host_bad = [label for label, rx in HOST_REQUIRED if not re.search(rx, ui)]
    if "UsesNuGet" not in ui and "NuGet" not in ui:
        host_bad.append("零第三方解码器声明")
    if host_bad:
        bad("Host 图标命令契约被破坏：%s" % "; ".join(host_bad[:6]))
    else:
        ok("Host 图标命令：零 I/O 派发 + 不可变快照 + WIC 系统包装 + 显式释放（%d 项）" %
           len(HOST_REQUIRED))

    wiring_bad = [label for label, rx in HOST_PANE_REQUIRED if not re.search(rx, pane_text)]
    wiring_bad += [label for label, rx in HOST_WINDOW_REQUIRED
                   if not re.search(rx, window_text)]
    if wiring_bad:
        bad("Host 列表项呈现接线被破坏：%s" % "; ".join(wiring_bad[:6]))
    else:
        ok("PaneView 图标位 / 后台 worker / generation 双侧校验 + MainWindow 注入与状态栏")

    host_cs = sorted(p for p in (root / "src" / "QuadLite.Host").glob("*.cs")
                     if p.is_file()) if (root / "src" / "QuadLite.Host").exists() else []
    layer_bad, cs6_bad = [], []
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
        ok("Host 分层纪律（零 System.IO / 零手写 COM / 零网络 / 零旧口号，%d 个 .cs）" %
           len(host_cs))
    if cs6_bad:
        bad("C# 5 语法子集被破坏：%s" % "; ".join(cs6_bad[:5]))
    else:
        ok("Host C# 5 语法子集（%d 个 .cs）" % len(host_cs))

    # 7. 构建链与 CI
    chain_bad = []
    proj_text, build_text, ci_text = read(proj), read(build), read(ci)
    if '<Compile Include="IconThumbCommands.cs" />' not in proj_text:
        chain_bad.append("csproj 缺 IconThumbCommands.cs")
    for layer in ["QuadLite.Core", "QuadLite.Fs", "QuadLite.App", "QuadLite.Host"]:
        if layer not in build_text:
            chain_bad.append("build.ps1 缺 " + layer)
    if "107 个 .cs" not in build_text:
        chain_bad.append("build.ps1 源码计数未同步 107")
    if "check-icon-thumb-specs" not in ci_text:
        chain_bad.append("CI 缺 check-icon-thumb-specs")
    for token in ["check-menu-host-specs", "check-ui-specs", "check-icon-specs",
                  "check-transport-specs", "real-exe-build", "real-exe-selftest"]:
        if token not in ci_text:
            chain_bad.append("旧闸门被删：" + token)
    if chain_bad:
        bad("构建 / CI 链被破坏：%s" % "; ".join(chain_bad[:6]))
    else:
        ok("csproj / build.ps1 四层 107 个 .cs 单 exe + CI 新旧闸门齐全")

    # 8. 规格仍是 v1.2，R 系列不写进 §8.1
    design_text = read(design)
    spec_bad = []
    for token in ["已定稿（v1.2）", "1/2/3/4"]:
        if token not in design_text:
            spec_bad.append("规格关键声明被改动：" + token)
    for token in ["R1", "R2", "R3", "R4", "R5"]:
        if re.search(r"\|\s*%s\s*\|" % token, design_text):
            spec_bad.append("实现批次 %s 被擅自写进规格" % token)
    if spec_bad:
        bad("规格一致性被破坏：%s" % "; ".join(spec_bad))
    else:
        ok("规格 v1.2 一字未改；R1–R5 仍是规格外实现批次")

    # 9. 产品 --selftest / 证据文档 / 唯一写入例外 / 诚实标注
    st_text = read(selftest)
    r5_groups = re.findall(r'probe\.Add\("(r5-[^"]+)"', st_text)
    evidence_bad = []
    if len(r5_groups) < 8:
        evidence_bad.append("产品 --selftest 的 R5 断言不足 8 组（实为 %d）" % len(r5_groups))
    if not adr.exists():
        evidence_bad.append("ADR-015 缺失")
    neg_text = read(negative)
    if "## R5 登记" not in neg_text or "未验证" not in neg_text:
        evidence_bad.append("负向对照未登记 R5")
    items = write_exception_items(root)
    if len(items) != 1 or "thumbcache" not in " ".join(items).lower():
        evidence_bad.append("写入例外清单不是恰好一条 thumbcache")
    for token in ["未验证", "默认关闭", "M1"]:
        if token not in fs:
            evidence_bad.append("宿主源码缺诚实标注：" + token)
    if evidence_bad:
        bad("证据链 / 诚实标注被破坏：%s" % "; ".join(evidence_bad[:5]))
    else:
        ok("产品 --selftest %d 组 R5 断言 + ADR-015 + R5 负向登记 + "
           "唯一写入例外 + 未验证标注" % len(r5_groups))

    print("  N/A   真实系统图像工厂 / HBITMAP 逐像素比对 / WIC 解码介质 / "
          "thumbcache 三段取证 / 图标呈现：未验证")
    print("  N/A   宿主默认关闭，CI 不开窗口、不枚举目录、不实例化 COM、不写缓存；"
          "M1 / M2 / M3 均不成立")
    if errors:
        for e in errors:
            print("  RED   " + e)
        print("verdict: RED (%d)" % len(errors))
        return len(errors)
    print("verdict: GREEN")
    return 0


if __name__ == "__main__":
    sys.exit(main())
