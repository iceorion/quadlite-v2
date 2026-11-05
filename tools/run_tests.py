#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
零依赖单测 / 自测 runner：检查链自身的正样本、负样本与合规扫描。

零第三方依赖；输出每组一行 `组名: ok=True/False` + 末行 `verdict:`；退出码 = 失败数。
负向对照按「不变量」覆盖：故意破坏一条不变量后，对应门卡必须变红。
B1 起增加 check-core-specs 的四类负向对照；C# 算法 runner 在 CI 上编译实跑，
exe 形态 --selftest 随 B7 引入。
"""
import shutil
import subprocess
import sys
import tempfile
from pathlib import Path

if hasattr(sys.stdout, "reconfigure"):
    sys.stdout.reconfigure(encoding="utf-8")

ROOT = Path(__file__).resolve().parents[1]
TOOLS = ROOT / "tools"
PY = sys.executable
results = []


def run(args, cwd=None):
    cp = subprocess.run(
        [PY] + [str(a) for a in args],
        cwd=str(cwd or ROOT),
        stdout=subprocess.PIPE, stderr=subprocess.STDOUT,
        text=True, encoding="utf-8", errors="replace",
    )
    return cp.returncode, cp.stdout or ""


def record(name, ok, detail=""):
    results.append((name, bool(ok), detail))
    print("%s: ok=%s%s" % (name, "True" if ok else "False", ("  # " + detail) if detail else ""))


def script(name, *args):
    return [str(TOOLS / name)] + list(args)


def expect_green(name, args, cwd=None):
    rc, out = run(args, cwd)
    verdict = "verdict: GREEN" in out
    record(name, rc == 0 and verdict, "rc=%d" % rc)
    if rc != 0 or not verdict:
        print("    " + out.strip().replace("\n", "\n    "))


def negative_boundary(fixture, expected_rule):
    """单文件夹具目录：check-boundaries 必须变红且命中预期规则。"""
    with tempfile.TemporaryDirectory() as td:
        dst = Path(td)
        shutil.copy2(fixture, dst / fixture.name)
        rc, out = run(script("check_boundaries.py", "--path", str(dst), "--root", str(ROOT)))
        ok = rc >= 1 and expected_rule in out and "verdict: RED" in out
        record("negative-%s" % expected_rule, ok, "rc=%d, fixture=%s" % (rc, fixture.name))
        if not ok:
            print("    " + out.strip().replace("\n", "\n    "))


def negative_trace():
    """故意删掉 FR-22 的门卡组映射，check-trace 必须变红。"""
    with tempfile.TemporaryDirectory() as td:
        p = Path(td) / "design.md"
        text = (ROOT / "design-v2.md").read_text(encoding="utf-8")
        broken = text.replace("§6.2、§7 `clipboard-*`", "§6.2")
        if broken == text:
            record("negative-trace", False, "负向对照未生效：替换串未命中")
            return
        p.write_text(broken, encoding="utf-8")
        rc, out = run(script("check_trace.py", "--doc", str(p)))
        ok = rc == 1 and "FR-22" in out and "verdict: RED" in out
        record("negative-trace", ok, "rc=%d" % rc)
        if not ok:
            print("    " + out.strip().replace("\n", "\n    "))


def negative_core_specs(label, mutate):
    """临时副本里破坏一条静态契约，check-core-specs 必须变红。"""
    with tempfile.TemporaryDirectory() as td:
        dst = Path(td) / "repo"
        shutil.copytree(ROOT, dst, ignore=shutil.ignore_patterns(
            ".git", "__pycache__", "*.pyc", "*.exe"))
        mutate(dst)
        rc, out = run([str(dst / "tools" / "check_core_specs.py"), "--root", str(dst)], cwd=dst)
        ok = rc >= 1 and "verdict: RED" in out
        record("negative-core-" + label, ok, "rc=%d" % rc)
        if not ok:
            print("    " + out.strip().replace("\n", "\n    "))


def mutate_delete_type(dst):
    (dst / "src" / "QuadLite.Core" / "NaturalSort.cs").unlink()


def mutate_drop_group(dst):
    p = dst / "tests" / "QuadLite.Tests" / "Program.cs"
    text = p.read_text(encoding="utf-8")
    broken = text.replace('Run("sort-*", TestSort);\n', "")
    assert broken != text, "negative control did not apply"
    p.write_text(broken, encoding="utf-8")


def mutate_cs6(dst):
    p = dst / "src" / "QuadLite.Core" / "ColumnLayout.cs"
    p.write_text(p.read_text(encoding="utf-8") +
                 '\n// negative control\npublic static class Bad { public string S() { return $"x"; } }\n',
                 encoding="utf-8")


def mutate_enum_gap(dst):
    p = dst / "src" / "QuadLite.Core" / "ErrorModel.cs"
    text = p.read_text(encoding="utf-8")
    broken = text.replace("SessionCorrupt, EnumFailed, DecodeFailed",
                          "SessionCorrupt, EnumFailed, DecodeFailed, Bogus")
    assert broken != text, "negative control did not apply"
    p.write_text(broken, encoding="utf-8")


def negative_open_source():
    """临时副本里塞一条正面旧口号，§10 扫描必须打回。"""
    with tempfile.TemporaryDirectory() as td:
        dst = Path(td) / "repo"
        shutil.copytree(ROOT, dst, ignore=shutil.ignore_patterns(".git", "__pycache__", "*.pyc"))
        readme = dst / "README.md"
        readme.write_text(
            readme.read_text(encoding="utf-8") + "\n打包之后运行期写入为 0，包目录只有 exe。\n",
            encoding="utf-8",
        )
        rc, out = run([str(dst / "tools" / "check_open_source.py"), "--root", str(dst)], cwd=dst)
        ok = rc == 1 and "旧口号" in out and "verdict: RED" in out
        record("negative-INV-06", ok, "rc=%d" % rc)
        if not ok:
            print("    " + out.strip().replace("\n", "\n    "))


def main():
    print("runner: zero-dependency gate self-test (B0 + B1)")

    expect_green("check-trace-green", script("check_trace.py"))
    expect_green("check-boundaries-src", script("check_boundaries.py"))
    expect_green("check-core-specs", script("check_core_specs.py"))
    expect_green("boundaries-ok-fixtures",
                 script("check_boundaries.py", "--path", str(TOOLS / "fixtures" / "boundaries" / "ok"),
                        "--root", str(ROOT)))

    bad_dir = TOOLS / "fixtures" / "boundaries" / "bad"
    expectations = {
        "B-01_Core_UsesIO.cs": "B-01",
        "B-02_App_UsesIO.cs": "B-02",
        "B-03_App_DirectCopy.cs": "B-03",
        "B-04_App_VerbScan.cs": "B-04",
        "B-05_App_FixedWidth.xaml": "B-05",
        "B-06_App_ProbeRef.cs": "B-06",
        "B-07_Core_DirectMove.cs": "B-07",
        "INV-02_App_Network.cs": "INV-02",
        "INV-05_App_DangerLevel.cs": "INV-05",
    }
    for filename, rule in sorted(expectations.items()):
        negative_boundary(bad_dir / filename, rule)

    negative_trace()
    negative_core_specs("missing-type", mutate_delete_type)
    negative_core_specs("missing-group", mutate_drop_group)
    negative_core_specs("cs6-syntax", mutate_cs6)
    negative_core_specs("enum-gap", mutate_enum_gap)
    negative_open_source()
    expect_green("open-source-scan", script("check_open_source.py"))

    failed = [name for name, ok, _ in results if not ok]
    print("tests=%d, failed=%d" % (len(results), len(failed)))
    if failed:
        print("verdict: RED (%d) -> %s" % (len(failed), ", ".join(failed)))
        return len(failed)
    print("verdict: GREEN")
    return 0


if __name__ == "__main__":
    sys.exit(main())
