#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
B2 静态契约门卡：Fs 层适配器与端口绑定。

零第三方依赖，退出码 = 红条数。
  python tools/check_fs_specs.py                 # 扫仓库
  python tools/check_fs_specs.py --root <目录>   # 负向对照时指向临时副本

本脚本只核对「文件齐备 / 端口绑定 / 不手写 COM vtable / shell 调用点隔离 /
C# 5 语法子集」这些静态事实。真实 IFileOperation、系统剪贴板与 watcher 的运行期
行为不在本门卡范围，一律标「未验证」（见 docs/adr/ADR-v2-004.md）。
"""
import argparse
import re
import sys
from pathlib import Path

if hasattr(sys.stdout, "reconfigure"):
    sys.stdout.reconfigure(encoding="utf-8")

ROOT = Path(__file__).resolve().parents[1]

FS_TYPES = {
    "AtomicFile.cs": r"\bclass\s+AtomicFile\b",
    "FsWatchService.cs": r"\bclass\s+FsWatchService\b",
    "DirSource.cs": r"\bclass\s+DirSource\b",
    "FileClipboard.cs": r"\bclass\s+FileClipboard\b",
    "ShellFileOperator.cs": r"\bclass\s+ShellFileOperator\b",
}

PORT_BINDINGS = {
    "ShellFileOperator.cs": r":\s*IFileOperator\b",
    "FsWatchService.cs": r":\s*IFsWatch\b",
    "DirSource.cs": r":\s*IDirSource\b",
    "FileClipboard.cs": r":\s*IClipboard\b",
}

# 规格 §5.2：COM 声明使用成熟绑定，不手写 vtable。
NO_HAND_WRITTEN_COM = re.compile(r"\bComImport\b|\bInterfaceType\b|\[\s*Guid\s*\(")

CS6_PLUS = [
    ("string interpolation", re.compile(r'\$"')),
    ("null-conditional", re.compile(r"\?\.")),
    ("nameof", re.compile(r"\bnameof\s*\(")),
    ("out var", re.compile(r"\bout\s+var\b")),
    ("null-coalescing assignment", re.compile(r"\?\?=")),
    ("init-only setter", re.compile(r"\binit\s*;")),
]


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--root", default=str(ROOT), help="待扫描的仓库根")
    args = ap.parse_args()
    root = Path(args.root).resolve()
    fs = root / "src" / "QuadLite.Fs"
    errors = []

    def bad(msg):
        errors.append(msg)

    def ok(msg):
        print("  PASS  " + msg)

    print("check-fs-specs: root=%s" % root.name)

    files = sorted(p for p in fs.glob("*.cs") if p.is_file()) if fs.exists() else []
    texts = dict((p.name, p.read_text(encoding="utf-8", errors="replace")) for p in files)

    # 1. 适配器齐备
    missing = []
    for fname, pattern in sorted(FS_TYPES.items()):
        if fname not in texts:
            missing.append(fname + " (missing)")
        elif not re.search(pattern, texts[fname]):
            missing.append(fname + " (type not found)")
    if missing:
        bad("Fs 适配器不齐备：%s" % "; ".join(missing))
    else:
        ok("Fs 适配器齐备（%d 个文件）" % len(FS_TYPES))

    # 2. 端口绑定：每个真实适配器都要实现对应 Core 端口
    unbound = []
    for fname, pattern in sorted(PORT_BINDINGS.items()):
        if fname not in texts or not re.search(pattern, texts[fname]):
            unbound.append(fname)
    if unbound:
        bad("端口未绑定：%s" % ", ".join(unbound))
    else:
        ok("端口绑定齐备（%d 个端口）" % len(PORT_BINDINGS))

    # 3. 不手写 COM vtable
    com_hits = []
    for fname, text in sorted(texts.items()):
        m = NO_HAND_WRITTEN_COM.search(text)
        if m:
            com_hits.append("%s: `%s`" % (fname, m.group(0)))
    if com_hits:
        bad("检测到手写 COM 声明：%s" % "; ".join(com_hits))
    else:
        ok("无手写 COM vtable（%d 个 .cs）" % len(files))

    # 4. shell 调用点隔离在注入的 transport 上
    shell = texts.get("ShellFileOperator.cs", "")
    if "IShellTransport" not in shell:
        bad("ShellFileOperator 未把真实 shell 调用隔离到 IShellTransport")
    elif re.search(r"\b(?:interface|class)\s+IFileOperation\b", shell):
        bad("ShellFileOperator 自行声明 IFileOperation 接口布局")
    else:
        ok("shell 调用点隔离到 IShellTransport（真实绑定未验证）")

    # 5. C# 5 语法子集
    cs6_hits = []
    for fname, text in sorted(texts.items()):
        for label, rx in CS6_PLUS:
            m = rx.search(text)
            if m:
                cs6_hits.append("%s: %s" % (fname, label))
    if cs6_hits:
        bad("C# 5 语法子集被破坏：%s" % "; ".join(cs6_hits[:5]))
    else:
        ok("C# 5 语法子集（%d 个 .cs）" % len(files))

    print("  N/A   真实 IFileOperation / 系统剪贴板 / FileSystemWatcher 运行期行为：未验证")
    if errors:
        for e in errors:
            print("  RED   " + e)
        print("verdict: RED (%d)" % len(errors))
        return len(errors)
    print("verdict: GREEN")
    return 0


if __name__ == "__main__":
    sys.exit(main())
