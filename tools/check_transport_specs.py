#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
R2 静态契约门卡：真实 IFileOperation 托管 COM 绑定与 Fs transport。

零第三方依赖，退出码 = 红条数。
  python tools/check_transport_specs.py                 # 扫仓库
  python tools/check_transport_specs.py --root <目录>   # 负向对照时指向临时副本

只核对「单一声明文件 / 分层零声明 / SDK 权威 GUID / vtable 顺序 / flags /
独立 STA 与生命周期 / 参数硬校验 / 撤销不对称 / 默认关闭 / 构建链与 CI /
规格未改 / 证据文档 / C# 5 / 诚实标注」这些静态事实。

真实 shell 搬运的运行期行为（COM 可用性、回调时序、回收站真实路径、
崩溃哨兵、Join 超时）一律标「未验证」；CI 只编译 Fs 层与单 exe，不调用
PerformOperations。§5.2「成熟绑定」的偏差登记在 docs/adr/ADR-v2-012.md。
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

CS6_PLUS = [
    ("string interpolation", re.compile(r'\$"')),
    ("null-conditional", re.compile(r"\?\.")),
    ("nameof", re.compile(r"\bnameof\s*\(")),
    ("out var", re.compile(r"\bout\s+var\b")),
    ("null-coalescing assignment", re.compile(r"\?\?=")),
    ("init-only setter", re.compile(r"\binit\s*;")),
]

TRANSPORT_REQUIRED = [
    ("interface binding", r"class\s+ShellTransport\s*:\s*IShellTransport\b"),
    ("STA thread", r"SetApartmentState\s*\(\s*ApartmentState\.STA\s*\)"),
    ("worker thread", r"new\s+Thread\s*\("),
    ("join", r"worker\.Join\s*\(\s*\)"),
    ("new instance per operation", r"Activator\.CreateInstance\s*\("),
    ("CLSID lookup", r"Type\.GetTypeFromCLSID\s*\("),
    ("instance-level flags", r"SetOperationFlags\s*\("),
    ("advise sink", r"op\.Advise\s*\(\s*bridge\s*,\s*out\s+cookie\s*\)"),
    ("unadvise sink", r"op\.Unadvise\s*\(\s*cookie\s*\)"),
    ("perform operations", r"op\.PerformOperations\s*\(\s*\)"),
    ("abort query", r"GetAnyOperationsAborted\s*\("),
    ("release COM", r"Marshal\.ReleaseComObject\s*\("),
    ("try-finally", r"finally\s*\{"),
    ("parameter hard check", r"OpPlanBuilder\.TryBuild\s*\("),
    ("disabled by default", r"if\s*\(\s*!enabled\s*\)"),
    ("explicit enable", r"public\s+void\s+Enable\s*\(\s*\)"),
    ("undo asymmetry rule", r"UndoRecordAsymmetric\s*\("),
    ("undo flag stripped", r"~\s*ShellFlags\.AddUndoRecord"),
]

FLAG_CONSTANTS = [
    ("FOF_ALLOWUNDO", r"AllowUndo\s*=\s*0x0040"),
    ("FOF_WANTNUKEWARNING", r"WantNukeWarning\s*=\s*0x4000"),
    ("FOFX_ADDUNDORECORD", r"AddUndoRecord\s*=\s*0x20000000"),
    ("FOF_NO_UI composition", r"NoUi\s*=\s*Silent\s*\|\s*NoConfirmation\s*\|\s*NoErrorUi\s*\|\s*NoConfirmMkdir"),
    ("SIGDN_FILESYSPATH", r"SigdnFilesysPath\s*=\s*0x80058000"),
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

    def na(msg):
        print("  N/A   " + msg)

    print("check-transport-specs: root=%s" % root.name)

    decl = root / com_policy.COM_DECLARATION_FILE
    transport = root / "src" / "QuadLite.Fs" / "ShellTransport.cs"
    decl_text = read(decl)
    transport_text = read(transport)

    # 1. R2 交付物齐备
    missing = [p for p in [decl, transport, root / "tools" / "com_policy.py",
                           root / "tools" / "check_transport_specs.py",
                           root / "docs" / "adr" / "ADR-v2-012.md"] if not p.exists()]
    if missing:
        bad("R2 交付物缺失：%s" % ", ".join(p.name for p in missing))
    else:
        ok("R2 交付物齐备（COM 声明 / transport / 单点口径 / 门卡 / ADR-012）")

    # 2. 分层 COM 口径：只有白名单文件允许托管 COM 声明
    scan_roots = [root / "src", root / "tests"]
    all_cs = []
    for base in scan_roots:
        if base.exists():
            all_cs += sorted(p for p in base.rglob("*.cs") if p.is_file())
    com_hits = []
    raw_hits = []
    verb_hits = []
    for p in all_cs:
        rel = p.relative_to(root).as_posix()
        text = read(p)
        hit = com_policy.declaration_hit(rel, text)
        if hit:
            com_hits.append("%s: `%s`" % (rel, hit))
        if com_policy.RAW_VTABLE.search(text):
            raw_hits.append(rel + ": raw vtable")
        if com_policy.VERB_SCAN.search(text) or com_policy.MENU_DELETION.search(text):
            verb_hits.append(rel)
    if com_hits:
        bad("非白名单文件出现 COM 声明：%s" % "; ".join(com_hits[:5]))
    else:
        ok("分层 COM 口径（%d 个 .cs，声明仅集中在 %s）" %
           (len(all_cs), com_policy.COM_DECLARATION_FILE))
    if raw_hits or verb_hits:
        bad("触犯函数指针 / verb 查询 / 删项禁令：%s" %
            "; ".join((raw_hits + verb_hits)[:5]))
    else:
        ok("零函数指针式 vtable、零 verb 查询、零菜单删项")
    if not com_policy.HAND_WRITTEN_COM.search(decl_text):
        bad("白名单文件未声明托管 COM 接口")
    else:
        ok("白名单文件确有托管 COM 声明（真实绑定不再为空）")

    # 3. SDK 权威 GUID
    ids = dict(CONST_ID.findall(decl_text))
    id_bad = []
    for name, expected in sorted(com_policy.EXPECTED_IDS.items()):
        if ids.get(name) != expected:
            id_bad.append("%s (%s != %s)" % (name, ids.get(name, "missing"), expected))
    if id_bad:
        bad("COM GUID 与 SDK 头文件不一致：%s" % "; ".join(id_bad))
    else:
        ok("四个 COM 标识符与 Windows SDK 10.0.19041.0 逐字一致")

    # 4. vtable 方法顺序
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
        ok("三段 vtable 方法顺序（21 / 16 / 5）与 SDK 一致")

    # 5. flags 常量
    flag_bad = [label for label, rx in FLAG_CONSTANTS if not re.search(rx, decl_text)]
    if flag_bad:
        bad("SDK flags 常量缺失或被改：%s" % ", ".join(flag_bad))
    else:
        ok("SDK flags 常量齐备（%d 项）" % len(FLAG_CONSTANTS))

    # 6. 独立 STA / 生命周期 / 参数硬校验 / 默认关闭
    tr_bad = [label for label, rx in TRANSPORT_REQUIRED if not re.search(rx, transport_text)]
    if "index >= sources.Count" not in decl_text:
        tr_bad.append("回调越界硬校验")
    if "IsRuntimeVerified" not in transport_text:
        tr_bad.append("运行期未验证判据")
    if tr_bad:
        bad("真实 transport 契约被破坏：%s" % ", ".join(tr_bad[:6]))
    else:
        ok("独立 STA / 每次新实例 / flags / sink / 释放 / 硬校验 / 默认关闭（%d 项）" %
           len(TRANSPORT_REQUIRED))

    # 7. 端口仍由 ShellFileOperator 隔离，编排层零改动
    op = read(root / "src" / "QuadLite.Fs" / "ShellFileOperator.cs")
    if "IShellTransport" not in op or re.search(r"interface\s+IFileOperation\b", op):
        bad("ShellFileOperator 未保持 transport 注入点隔离")
    else:
        ok("真实调用点仍隔离在 IShellTransport（B2 端口契约未改）")

    # 8. 构建链与 CI 纳入 R2
    build = read(root / "build.ps1")
    ci = read(root / ".github" / "workflows" / "ci.yml")
    chain_bad = []
    if "QuadLite.Fs" not in build:
        chain_bad.append("build.ps1 未编入 Fs 层")
    if "check-transport-specs" not in ci:
        chain_bad.append("CI 缺 check-transport-specs")
    if "csharp-fs-compile" not in ci or "real-exe-build" not in ci:
        chain_bad.append("CI 编译链被删")
    if chain_bad:
        bad("构建 / CI 链被破坏：%s" % "; ".join(chain_bad))
    else:
        ok("build.ps1 四层单 exe + CI 三步编译链纳入 R2")

    # 9. 规格仍为 v1.2，R 系列不写进 §8.1
    design = read(root / "design-v2.md")
    spec_bad = []
    if "已定稿（v1.2）" not in design or "1/2/3/4" not in design:
        spec_bad.append("规格关键声明被改动")
    if re.search(r"\|\s*R2\s*\|", design) or re.search(r"\|\s*R1\s*\|", design):
        spec_bad.append("实现批次被擅自写进规格 §8.1")
    if spec_bad:
        bad("规格一致性被破坏：%s" % "; ".join(spec_bad))
    else:
        ok("规格 v1.2 一字未改；R1 / R2 仍是规格外实现批次")

    # 10. 证据文档如实标注
    adr = read(root / "docs" / "adr" / "ADR-v2-012.md")
    ledger = read(root / "docs" / "run-ledger.md")
    readme = read(root / "README.md")
    controls = read(root / "docs" / "negative-controls.md")
    doc_bad = []
    for token in ["偏差", "未验证", "M1", "沿用", "修订"]:
        if token not in adr:
            doc_bad.append("ADR-012 缺「%s」" % token)
    if "| R2 |" not in ledger:
        doc_bad.append("run-ledger 缺 R2 行")
    if "R2" not in readme or "R2" not in controls:
        doc_bad.append("README / negative-controls 未登记 R2")
    if doc_bad:
        bad("证据文档被破坏：%s" % "; ".join(doc_bad))
    else:
        ok("证据文档齐备（ADR-012 偏差登记 + 账本 R2 行 + README / 负向对照）")

    # 11. C# 5 语法子集（R2 两个新文件）
    cs6_bad = []
    for p in [decl, transport]:
        text = read(p)
        for label, rx in CS6_PLUS:
            if rx.search(text):
                cs6_bad.append("%s: %s" % (p.name, label))
    if cs6_bad:
        bad("C# 5 语法子集被破坏：%s" % "; ".join(cs6_bad))
    else:
        ok("R2 源码 C# 5 语法子集（2 个 .cs）")

    # 12. 诚实标注：运行期不得宣称已验证
    if "未验证" not in decl_text or "未验证" not in transport_text:
        bad("R2 源码未如实标注运行期「未验证」")
    else:
        ok("运行期行为如实标注「未验证」（CI 只编译不调用）")

    na("真实 IFileOperation 搬运 / ProgressSink 回调 / 回收站真实路径 / 独立 STA 运行期 / "
       "崩溃哨兵 / Join 超时：未验证，需 H1–H6 真机取证")
    na("§5.2「成熟绑定」偏差：零 NuGet + net48 下改用 SDK 逐字对应的托管声明，见 ADR-012")
    na("M1 / M2 / M3 仍不成立：R3 起窗口已有浏览 / 搬运 UI 接线，但 transport 默认关闭、"
       "CI 不实例化 COM，真实交互与 shell 运行期未验证；ReleaseState.IsPublishable() == false")

    if errors:
        print("verdict: RED (%d)" % len(errors))
        return len(errors)
    print("verdict: GREEN")
    return 0


if __name__ == "__main__":
    sys.exit(main())
