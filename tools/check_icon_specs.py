#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
B6 静态契约门卡：图标 / 缩略图 / 缓存的零 I/O 规则层、L4 状态内核与唯一写入例外。

零第三方依赖，退出码 = 红条数。
  python tools/check_icon_specs.py                 # 扫仓库
  python tools/check_icon_specs.py --root <目录>   # 负向对照时指向临时副本

本脚本只核对「B6 Core 类型齐备 / App 状态内核齐备 / 门卡组注册 / 规格关键常量 /
WIC 白名单与视频容器数 / 唯一写入例外 / 分层边界 / 规格版本一致 / C# 5 语法」
这些静态事实。

真实 WIC 解码、IShellItemImageFactory 调用、后台 worker、LRU 介质与三段取证
均未接入，一律标「未实现 / 未验证」（见 docs/adr/ADR-v2-008.md）。
"""
import argparse
import re
import sys
from pathlib import Path

if hasattr(sys.stdout, "reconfigure"):
    sys.stdout.reconfigure(encoding="utf-8")

ROOT = Path(__file__).resolve().parents[1]

B6_CORE_TYPES = {
    "IconChannelRules.cs": r"\bclass\s+IconChannelRules\b",
    "ThumbWhitelistRules.cs": r"\bclass\s+ThumbWhitelistRules\b",
    "ExifOrientationRules.cs": r"\bclass\s+ExifOrientationRules\b",
    "ThumbQueueRules.cs": r"\bclass\s+ThumbQueueRules\b",
    "ThumbCacheRules.cs": r"\bclass\s+ThumbCacheRules\b",
    "VideoThumbRules.cs": r"\bclass\s+VideoThumbRules\b",
    "ThumbToggleRules.cs": r"\bclass\s+ThumbToggleRules\b",
    "ThumbWriteRules.cs": r"\bclass\s+ThumbWriteRules\b",
}

APP_TYPES = {
    "ThumbState.cs": r"\bclass\s+ThumbState\b",
    "ThumbCommands.cs": r"\bclass\s+ThumbCommands\b",
}

EXPECTED_GROUPS = [
    "icon-*", "thumb-whitelist", "thumb-exif", "thumb-queue",
    "thumb-cache", "thumb-video", "thumb-toggle", "thumb-nowrite",
]

CS6_PLUS = [
    ("string interpolation", re.compile(r'\$"')),
    ("null-conditional", re.compile(r"\?\.")),
    ("nameof", re.compile(r"\bnameof\s*\(")),
    ("out var", re.compile(r"\bout\s+var\b")),
    ("null-coalescing assignment", re.compile(r"\?\?=")),
    ("init-only setter", re.compile(r"\binit\s*;")),
]

# 规格 §5.11 / §5.12 / §7.1 的关键常量与不变量，改为他值即判红（防止实现静默漂移）。
CORE_INVARIANTS = {
    "IconChannelRules.cs": [
        ("four icon channels", r"ChannelCount\s*=\s*4\b"),
        ("jumbo fallback 48 px", r"FallbackPx\s*=\s*48\b"),
        ("exe two stages", r"ExeStageCount\s*=\s*2\b"),
    ],
    "ThumbWhitelistRules.cs": [
        ("WIC whitelist 14", r"WhitelistCount\s*=\s*14\b"),
        ("ts gate 1 MiB", r"TsMinBytes\s*=\s*1048576L\b"),
        ("no third party decoder", r"UsesThirdPartyDecoder\s*\(\s*\)\s*\{[^}]*return\s+false\s*;"),
    ],
    "ExifOrientationRules.cs": [
        ("eight orientations", r"OrientationCount\s*=\s*8\b"),
        ("default orientation 1", r"DefaultOrientation\s*=\s*1\b"),
    ],
    "ThumbQueueRules.cs": [
        ("single worker", r"WorkerCount\s*=\s*1\b"),
        ("background worker", r"UsesBackgroundWorker\s*\(\s*\)\s*\{[^}]*return\s+true\s*;"),
        ("no long COM on UI thread", r"AllowsLongComCallOnUiThread\s*\(\s*\)\s*\{[^}]*return\s+false\s*;"),
    ],
    "ThumbCacheRules.cs": [
        ("four key parts", r"KeyPartCount\s*=\s*4\b"),
        ("1500 entry gate", r"MaxEntries\s*=\s*1500\b"),
        ("96 MiB byte gate", r"MaxBytes\s*=\s*96L\s*\*\s*1024L\s*\*\s*1024L\b"),
        ("image type prefix", r'ImagePrefix\s*=\s*"i\|"'),
        ("video type prefix", r'VideoPrefix\s*=\s*"v\|"'),
    ],
    "VideoThumbRules.cs": [
        ("fifteen containers", r"ContainerCount\s*=\s*15\b"),
        ("eight second watchdog", r"TimeoutSeconds\s*=\s*8\b"),
        ("system image factory", r"UsesSystemImageFactory\s*\(\s*\)\s*\{[^}]*return\s+true\s*;"),
        ("no third party decoder", r"UsesThirdPartyDecoder\s*\(\s*\)\s*\{[^}]*return\s+false\s*;"),
    ],
    "ThumbToggleRules.cs": [
        ("no reenum on thumb toggle",
         r"NoReenumOnThumbToggle\s*\(\s*\)\s*\{[^}]*return\s+!\s*ToggleRules\.RequiresReenumeration\s*\(\s*DisplayToggle\.Thumbnail\s*\)\s*;"),
        ("cache reused on reenable", r"ReusesCacheWhenReenabled\s*\(\s*\)\s*\{[^}]*return\s+true\s*;"),
    ],
    "ThumbWriteRules.cs": [
        ("exactly one exception", r"ExceptionCount\s*=\s*1\b"),
        ("three evidence zones", r"EvidenceZoneCount\s*=\s*3\b"),
        ("sole exception is thumbcache", r'SoleException\s*\(\s*\)\s*\{[^}]*return\s+"thumbcache"\s*;'),
        ("no silent profile fallback",
         r"SilentFallbackToUserProfile\s*\(\s*\)\s*\{[^}]*return\s+false\s*;"),
    ],
}

APP_FORBIDDEN = [
    ("using System.IO", re.compile(r"\busing\s+System\.IO\s*;")),
    ("test double reference", re.compile(r"QuadLite\.(?:Probe|Tests)")),
    ("hand-written COM", re.compile(r"\bComImport\b|\bInterfaceType\b|\[\s*Guid\s*\(")),
    ("verb scan", re.compile(r"GetCommandString")),
]

PRODUCT_FORBIDDEN = [
    ("hand-written COM", re.compile(r"\bComImport\b|\bInterfaceType\b|\[\s*Guid\s*\(")),
    ("test double reference", re.compile(r"QuadLite\.(?:Probe|Tests)")),
]


def enum_body(text, name):
    m = re.search(r"\benum\s+" + re.escape(name) + r"\s*\{([^}]*)\}", text, re.S)
    if not m:
        return None
    return [x.strip() for x in re.sub(r"//[^\n]*", "", m.group(1)).split(",") if x.strip()]


def array_body(text, name):
    m = re.search(r"string\[\]\s+" + re.escape(name) + r"\s*=\s*new\s+string\[\]\s*\{([^}]*)\}",
                  text, re.S)
    if not m:
        return None
    return re.findall(r'"([^"]+)"', m.group(1))


def write_exception_items(root):
    text = (root / "docs" / "negative-controls.md").read_text(encoding="utf-8", errors="replace")
    m = re.search(r"^##\s+写入例外清单\s*$(.*?)(?=^##\s|\Z)", text, re.M | re.S)
    if not m:
        return []
    return [l for l in m.group(1).splitlines() if re.match(r"^\s*-\s+\S", l)]


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

    print("check-icon-specs: root=%s" % root.name)

    # 1. B6 Core 类型齐备
    missing = []
    for fname, pattern in sorted(B6_CORE_TYPES.items()):
        p = core / fname
        if not p.exists():
            missing.append(fname + " (missing)")
        elif not re.search(pattern, read(p)):
            missing.append(fname + " (type not found)")
    if missing:
        bad("B6 Core 类型不齐备：%s" % "; ".join(missing))
    else:
        ok("B6 Core 规则层齐备（%d 个文件）" % len(B6_CORE_TYPES))

    # 2. App 图标 / 缩略图状态内核齐备
    missing_app = []
    for fname, pattern in sorted(APP_TYPES.items()):
        p = app / fname
        if not p.exists():
            missing_app.append(fname + " (missing)")
        elif not re.search(pattern, read(p)):
            missing_app.append(fname + " (type not found)")
    if missing_app:
        bad("App 图标 / 缩略图状态内核不齐备：%s" % "; ".join(missing_app))
    else:
        ok("App 图标 / 缩略图状态内核齐备（%d 个文件）" % len(APP_TYPES))

    # 3. runner B6 门卡组注册齐备
    ptext = read(tests / "Program.cs")
    groups = re.findall(r'\bRun\(\s*"([^"]+)"', ptext)
    missing_groups = [g for g in EXPECTED_GROUPS if g not in groups]
    if missing_groups:
        bad("runner 缺少 B6 门卡组：%s" % ", ".join(missing_groups))
    else:
        ok("runner B6 门卡组注册齐备（%d 组）" % len(EXPECTED_GROUPS))

    # 4. 规格关键常量与不变量
    inv_bad = []
    for fname, checks in sorted(CORE_INVARIANTS.items()):
        text = read(core / fname)
        for label, rx in checks:
            if not re.search(rx, text, re.S):
                inv_bad.append("%s: %s" % (fname, label))
    if inv_bad:
        bad("B6 规格常量 / 不变量被破坏：%s" % "; ".join(inv_bad))
    else:
        total = sum(len(v) for v in CORE_INVARIANTS.values())
        ok("B6 规格常量 / 不变量齐备（%d 项）" % total)

    # 5. WIC 白名单 14 种 / 视频容器 15 种：数组元素数须等于声明常量
    whitelist = array_body(read(core / "ThumbWhitelistRules.cs"), "Whitelist")
    containers = array_body(read(core / "VideoThumbRules.cs"), "Containers")
    write_text = read(core / "ThumbWriteRules.cs")
    zones_match = re.search(r"EvidenceZones\s*\(\s*\)\s*\{([^}]*)\}", write_text, re.S)
    zones = re.findall(r'"([^"]+)"', zones_match.group(1)) if zones_match else []
    count_bad = []
    if whitelist is None or len(whitelist) != 14:
        count_bad.append("WIC 白名单 %s 项（应为 14）" % ("缺失" if whitelist is None else len(whitelist)))
    if containers is None or len(containers) != 15:
        count_bad.append("视频容器 %s 项（应为 15）" % ("缺失" if containers is None else len(containers)))
    if len(zones) != 3:
        count_bad.append("取证区间 %d 个（应为 3）" % len(zones))
    if count_bad:
        bad("B6 枚举清单数量不符：%s" % "; ".join(count_bad))
    else:
        ok("WIC 白名单 14 种 / 视频容器 15 种 / 取证三段齐备")

    # 6. 唯一写入例外：源码常量 + 文档例外清单恰好一条
    items = write_exception_items(root)
    write_bad = []
    if len(items) != 1 or "thumbcache" not in " ".join(items).lower():
        write_bad.append("写入例外清单应恰好 1 条且为 thumbcache，实为 %d 条" % len(items))
    if not re.search(r'WriteAllowed\s*\(\s*WriteZone\s+zone\s*,\s*bool\s+sameMotiveAsExplorer\s*\)',
                     read(core / "ThumbWriteRules.cs")):
        write_bad.append("写入允许判据缺少「相同动机」条件")
    if write_bad:
        bad("唯一写入例外被破坏：%s" % "; ".join(write_bad))
    else:
        ok("唯一写入例外 thumbcache（源码判据 + 文档清单各一条）")

    # 7. App 状态内核零 I/O / 无测试替身 / 无手写 COM
    app_files = sorted(p for p in app.glob("*.cs") if p.is_file()) if app.exists() else []
    app_bad = []
    for p in app_files:
        text = read(p)
        for label, rx in APP_FORBIDDEN:
            if rx.search(text):
                app_bad.append("%s: %s" % (p.name, label))
    if app_bad:
        bad("App 状态内核触犯分层边界：%s" % "; ".join(app_bad))
    else:
        ok("App 状态内核零 I/O / 无测试替身 / 无手写 COM（%d 个 .cs）" % len(app_files))

    # 8. 产品程序集不手写 COM、不引用测试替身
    product = sorted(p for p in (root / "src").rglob("*.cs") if p.is_file())
    product_bad = []
    for p in product:
        text = read(p)
        for label, rx in PRODUCT_FORBIDDEN:
            if rx.search(text):
                product_bad.append("%s: %s" % (p.relative_to(root).as_posix(), label))
    if product_bad:
        bad("产品程序集触犯 B6 分层纪律：%s" % "; ".join(product_bad[:5]))
    else:
        ok("产品程序集零手写 COM / 零测试替身引用（%d 个 .cs）" % len(product))

    # 9. 规格与实现一致：设计文档仍是 v1.2，B6 判据声明未被擅自改动
    design = read(root / "design-v2.md")
    if "| B6 | 图标 / 缩略图 / 缓存" not in design or "`thumb-nowrite`" not in design:
        bad("规格 §8.1 / §7.1 的 B6 判据声明被改动，B6 实现需同步")
    elif "已定稿（v1.2）" not in design:
        bad("规格版本不再是 v1.2，B6 实现需同步复核")
    else:
        ok("规格仍为 v1.2 且 B6 判据声明未改（布局按四档，不由本批改动）")

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

    print("  N/A   真实 WIC 解码 / 系统图像工厂 / 后台 worker / LRU 介质 / "
          "三段取证 / 图标与缩略图渲染：未实现 / 未验证")
    if errors:
        for e in errors:
            print("  RED   " + e)
        print("verdict: RED (%d)" % len(errors))
        return len(errors)
    print("verdict: GREEN")
    return 0


if __name__ == "__main__":
    sys.exit(main())
