#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
零依赖单测 / 自测 runner：检查链自身的正样本、负样本与合规扫描。

零第三方依赖；输出每组一行 `组名: ok=True/False` + 末行 `verdict:`；退出码 = 失败数。
负向对照按「不变量」覆盖：故意破坏一条不变量后，对应门卡必须变红。
B1 起增加 check-core-specs 的四类负向对照；B2 起增加 check-fs-specs 正向 +
两类 B2 编排 / 两类 Fs 适配器的负向对照；B3 起增加 check-shell-specs 正向 +
六类外壳静态契约负向对照；B4 起增加 check-list-specs 正向 +
六类列表视图静态契约负向对照；B5 起增加 check-menu-specs 正向 +
七类系统原生菜单静态契约负向对照；B6 起增加 check-icon-specs 正向 +
七类图标缩略图静态契约负向对照；B7 起增加 check-selftest-specs 正向 +
八类自测 / 出包静态契约负向对照。C# 算法 runner 在 CI 上编译实跑，
runner exe 自 B7 起输出 ≥100 行门卡总表报告；真实产品 exe 形态 --selftest 仍未接入。
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


# ---- B2 负向对照 ----
def negative_fs_specs(label, mutate):
    """临时副本里破坏一条 Fs 静态契约，check-fs-specs 必须变红。"""
    with tempfile.TemporaryDirectory() as td:
        dst = Path(td) / "repo"
        shutil.copytree(ROOT, dst, ignore=shutil.ignore_patterns(
            ".git", "__pycache__", "*.pyc", "*.exe"))
        mutate(dst)
        rc, out = run([str(dst / "tools" / "check_fs_specs.py"), "--root", str(dst)], cwd=dst)
        ok = rc >= 1 and "verdict: RED" in out
        record("negative-fs-" + label, ok, "rc=%d" % rc)
        if not ok:
            print("    " + out.strip().replace("\n", "\n    "))


def mutate_delete_coordinator(dst):
    (dst / "src" / "QuadLite.Core" / "OperationCoordinator.cs").unlink()


def mutate_drop_b2_group(dst):
    p = dst / "tests" / "QuadLite.Tests" / "Program.cs"
    text = p.read_text(encoding="utf-8")
    broken = text.replace('Run("op-*", TestOps);\n', "")
    assert broken != text, "negative control did not apply"
    p.write_text(broken, encoding="utf-8")


def mutate_delete_shell_operator(dst):
    (dst / "src" / "QuadLite.Fs" / "ShellFileOperator.cs").unlink()


def mutate_inject_comimport(dst):
    p = dst / "src" / "QuadLite.Fs" / "ShellFileOperator.cs"
    p.write_text(p.read_text(encoding="utf-8") +
                 "\n// negative control: [ComImport] interface IRaw {}\n",
                 encoding="utf-8")


# ---- B3 负向对照 ----
def negative_shell_specs(label, mutate):
    """临时副本里破坏一条外壳静态契约，check-shell-specs 必须变红。"""
    with tempfile.TemporaryDirectory() as td:
        dst = Path(td) / "repo"
        shutil.copytree(ROOT, dst, ignore=shutil.ignore_patterns(
            ".git", "__pycache__", "*.pyc", "*.exe"))
        mutate(dst)
        rc, out = run([str(dst / "tools" / "check_shell_specs.py"), "--root", str(dst)], cwd=dst)
        ok = rc >= 1 and "verdict: RED" in out
        record("negative-shell-" + label, ok, "rc=%d" % rc)
        if not ok:
            print("    " + out.strip().replace("\n", "\n    "))


def mutate_delete_layout_math(dst):
    (dst / "src" / "QuadLite.Core" / "LayoutMath.cs").unlink()


def mutate_drop_layout_group(dst):
    p = dst / "tests" / "QuadLite.Tests" / "Program.cs"
    text = p.read_text(encoding="utf-8")
    broken = text.replace('Run("layout-*", TestLayout);\n', "")
    assert broken != text, "negative control did not apply"
    p.write_text(broken, encoding="utf-8")


def mutate_break_three_pane(dst):
    p = dst / "src" / "QuadLite.Core" / "LayoutMath.cs"
    text = p.read_text(encoding="utf-8")
    broken = text.replace("new PaneSlot(0, 0, 0, 3, 1)", "new PaneSlot(0, 0, 0, 1, 1)")
    assert broken != text, "negative control did not apply"
    p.write_text(broken, encoding="utf-8")


def mutate_delete_app_skeleton(dst):
    (dst / "src" / "QuadLite.App" / "ShellState.cs").unlink()


def mutate_app_io(dst):
    p = dst / "src" / "QuadLite.App" / "ShellState.cs"
    p.write_text("using System.IO;  // negative control\n" +
                 p.read_text(encoding="utf-8"), encoding="utf-8")


def mutate_cs6_shell(dst):
    p = dst / "src" / "QuadLite.App" / "ShellCommands.cs"
    p.write_text(p.read_text(encoding="utf-8") +
                 '\n// negative control\npublic static class Bad { public string S() { return $"x"; } }\n',
                 encoding="utf-8")


# ---- B4 负向对照 ----
def negative_list_specs(label, mutate):
    """临时副本里破坏一条列表视图静态契约，check-list-specs 必须变红。"""
    with tempfile.TemporaryDirectory() as td:
        dst = Path(td) / "repo"
        shutil.copytree(ROOT, dst, ignore=shutil.ignore_patterns(
            ".git", "__pycache__", "*.pyc", "*.exe"))
        mutate(dst)
        rc, out = run([str(dst / "tools" / "check_list_specs.py"), "--root", str(dst)], cwd=dst)
        ok = rc >= 1 and "verdict: RED" in out
        record("negative-list-" + label, ok, "rc=%d" % rc)
        if not ok:
            print("    " + out.strip().replace("\n", "\n    "))


def mutate_delete_filter_rules(dst):
    (dst / "src" / "QuadLite.Core" / "FilterRules.cs").unlink()


def mutate_drop_filter_group(dst):
    p = dst / "tests" / "QuadLite.Tests" / "Program.cs"
    text = p.read_text(encoding="utf-8")
    broken = text.replace('Run("filter-*", TestFilter);\n', "")
    assert broken != text, "negative control did not apply"
    p.write_text(broken, encoding="utf-8")


def mutate_break_batch_size(dst):
    p = dst / "src" / "QuadLite.Core" / "EnumBatchRules.cs"
    text = p.read_text(encoding="utf-8")
    broken = text.replace("DefaultBatchSize = 4096", "DefaultBatchSize = 128")
    assert broken != text, "negative control did not apply"
    p.write_text(broken, encoding="utf-8")


def mutate_delete_list_state(dst):
    (dst / "src" / "QuadLite.App" / "ListViewState.cs").unlink()


def mutate_list_app_io(dst):
    p = dst / "src" / "QuadLite.App" / "ListViewState.cs"
    p.write_text("using System.IO;  // negative control\n" +
                 p.read_text(encoding="utf-8"), encoding="utf-8")


def mutate_cs6_list(dst):
    p = dst / "src" / "QuadLite.App" / "ListViewCommands.cs"
    p.write_text(p.read_text(encoding="utf-8") +
                 '\n// negative control\npublic static class Bad2 { public string S() { return $"x"; } }\n',
                 encoding="utf-8")


# ---- B5 负向对照 ----
def negative_menu_specs(label, mutate):
    """临时副本里破坏一条菜单静态契约，check-menu-specs 必须变红。"""
    with tempfile.TemporaryDirectory() as td:
        dst = Path(td) / "repo"
        shutil.copytree(ROOT, dst, ignore=shutil.ignore_patterns(
            ".git", "__pycache__", "*.pyc", "*.exe"))
        mutate(dst)
        rc, out = run([str(dst / "tools" / "check_menu_specs.py"), "--root", str(dst)], cwd=dst)
        ok = rc >= 1 and "verdict: RED" in out
        record("negative-menu-" + label, ok, "rc=%d" % rc)
        if not ok:
            print("    " + out.strip().replace("\n", "\n    "))


def mutate_delete_fusion_rules(dst):
    (dst / "src" / "QuadLite.Core" / "MenuFusionRules.cs").unlink()


def mutate_drop_menu_group(dst):
    p = dst / "tests" / "QuadLite.Tests" / "Program.cs"
    text = p.read_text(encoding="utf-8")
    broken = text.replace('Run("menu-id-range", TestMenuIdRange);\n', "")
    assert broken != text, "negative control did not apply"
    p.write_text(broken, encoding="utf-8")


def mutate_verb_scan_drift(dst):
    p = dst / "src" / "QuadLite.Core" / "MenuHostRules.cs"
    text = p.read_text(encoding="utf-8")
    old = "public static int VerbScanCount()\n        {\n            return 0;\n        }"
    new = "public static int VerbScanCount()\n        {\n            return 1;\n        }"
    broken = text.replace(old, new)
    assert broken != text, "negative control did not apply"
    p.write_text(broken, encoding="utf-8")


def mutate_inject_product_verb_scan(dst):
    p = dst / "src" / "QuadLite.Core" / "MenuHostRules.cs"
    p.write_text(p.read_text(encoding="utf-8") +
                 "\n// negative control: GetCommandString must never appear in product code\n",
                 encoding="utf-8")


def mutate_delete_menu_state(dst):
    (dst / "src" / "QuadLite.App" / "MenuState.cs").unlink()


def mutate_menu_app_io(dst):
    p = dst / "src" / "QuadLite.App" / "MenuState.cs"
    p.write_text("using System.IO;  // negative control\n" +
                 p.read_text(encoding="utf-8"), encoding="utf-8")


def mutate_cs6_menu(dst):
    p = dst / "src" / "QuadLite.App" / "MenuCommands.cs"
    p.write_text(p.read_text(encoding="utf-8") +
                 '\n// negative control\npublic static class Bad3 { public string S() { return $"x"; } }\n',
                 encoding="utf-8")


# ---- B6 负向对照 ----
def negative_icon_specs(label, mutate):
    """临时副本里破坏一条图标缩略图静态契约，check-icon-specs 必须变红。"""
    with tempfile.TemporaryDirectory() as td:
        dst = Path(td) / "repo"
        shutil.copytree(ROOT, dst, ignore=shutil.ignore_patterns(
            ".git", "__pycache__", "*.pyc", "*.exe"))
        mutate(dst)
        rc, out = run([str(dst / "tools" / "check_icon_specs.py"), "--root", str(dst)], cwd=dst)
        ok = rc >= 1 and "verdict: RED" in out
        record("negative-icon-" + label, ok, "rc=%d" % rc)
        if not ok:
            print("    " + out.strip().replace("\n", "\n    "))


def mutate_delete_cache_rules(dst):
    (dst / "src" / "QuadLite.Core" / "ThumbCacheRules.cs").unlink()


def mutate_drop_b6_group(dst):
    p = dst / "tests" / "QuadLite.Tests" / "Program.cs"
    text = p.read_text(encoding="utf-8")
    broken = text.replace('Run("thumb-nowrite", TestThumbNoWrite);\n', "")
    assert broken != text, "negative control did not apply"
    p.write_text(broken, encoding="utf-8")


def mutate_cache_gate_drift(dst):
    p = dst / "src" / "QuadLite.Core" / "ThumbCacheRules.cs"
    text = p.read_text(encoding="utf-8")
    broken = text.replace("MaxEntries = 1500", "MaxEntries = 1")
    assert broken != text, "negative control did not apply"
    p.write_text(broken, encoding="utf-8")


def mutate_write_exception_drift(dst):
    p = dst / "src" / "QuadLite.Core" / "ThumbWriteRules.cs"
    text = p.read_text(encoding="utf-8")
    broken = text.replace("ExceptionCount = 1", "ExceptionCount = 2")
    assert broken != text, "negative control did not apply"
    p.write_text(broken, encoding="utf-8")


def mutate_delete_thumb_state(dst):
    (dst / "src" / "QuadLite.App" / "ThumbState.cs").unlink()


def mutate_thumb_app_io(dst):
    p = dst / "src" / "QuadLite.App" / "ThumbState.cs"
    p.write_text("using System.IO;  // negative control\n" +
                 p.read_text(encoding="utf-8"), encoding="utf-8")


def mutate_cs6_icon(dst):
    p = dst / "src" / "QuadLite.App" / "ThumbCommands.cs"
    p.write_text(p.read_text(encoding="utf-8") +
                 '\n// negative control\npublic static class Bad4 { public string S() { return $"x"; } }\n',
                 encoding="utf-8")


# ---- B7 负向对照 ----
def negative_selftest_specs(label, mutate):
    """临时副本里破坏一条自测 / 出包静态契约，check-selftest-specs 必须变红。"""
    with tempfile.TemporaryDirectory() as td:
        dst = Path(td) / "repo"
        shutil.copytree(ROOT, dst, ignore=shutil.ignore_patterns(
            ".git", "__pycache__", "*.pyc", "*.exe"))
        mutate(dst)
        rc, out = run([str(dst / "tools" / "check_selftest_specs.py"), "--root", str(dst)], cwd=dst)
        ok = rc >= 1 and "verdict: RED" in out
        record("negative-selftest-" + label, ok, "rc=%d" % rc)
        if not ok:
            print("    " + out.strip().replace("\n", "\n    "))


def mutate_delete_report_rules(dst):
    (dst / "src" / "QuadLite.Core" / "SelftestReportRules.cs").unlink()


def mutate_drop_b7_group(dst):
    p = dst / "tests" / "QuadLite.Tests" / "Program.cs"
    text = p.read_text(encoding="utf-8")
    broken = text.replace('Run("cli-selftest-output", TestCliSelftestOutput);\n', "")
    assert broken != text, "negative control did not apply"
    p.write_text(broken, encoding="utf-8")


def mutate_min_lines_drift(dst):
    p = dst / "src" / "QuadLite.Core" / "SelftestReportRules.cs"
    text = p.read_text(encoding="utf-8")
    broken = text.replace("MinOutputLines = 100", "MinOutputLines = 1")
    assert broken != text, "negative control did not apply"
    p.write_text(broken, encoding="utf-8")


def mutate_manifest_field_drift(dst):
    p = dst / "src" / "QuadLite.Core" / "BuildManifestRules.cs"
    text = p.read_text(encoding="utf-8")
    broken = text.replace("FieldCount = 7", "FieldCount = 6")
    assert broken != text, "negative control did not apply"
    p.write_text(broken, encoding="utf-8")


def mutate_gate_inventory_drift(dst):
    p = dst / "src" / "QuadLite.Core" / "GateInventoryRules.cs"
    text = p.read_text(encoding="utf-8")
    broken = text.replace('"menu-sentinel", ', "", 1)
    assert broken != text, "negative control did not apply"
    p.write_text(broken, encoding="utf-8")


def mutate_delete_selftest_state(dst):
    (dst / "src" / "QuadLite.App" / "SelftestState.cs").unlink()


def mutate_b7_app_io(dst):
    p = dst / "src" / "QuadLite.App" / "PackageState.cs"
    p.write_text("using System.IO;  // negative control\n" +
                 p.read_text(encoding="utf-8"), encoding="utf-8")


def mutate_cs6_selftest(dst):
    p = dst / "src" / "QuadLite.App" / "SelftestState.cs"
    p.write_text(p.read_text(encoding="utf-8") +
                 '\n// negative control\npublic static class Bad5 { public string S() { return $"x"; } }\n',
                 encoding="utf-8")


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
    print("runner: zero-dependency gate self-test (B0 + B1 + B2 + B3 + B4 + B5 + B6 + B7)")

    expect_green("check-trace-green", script("check_trace.py"))
    expect_green("check-boundaries-src", script("check_boundaries.py"))
    expect_green("check-core-specs", script("check_core_specs.py"))
    expect_green("check-fs-specs", script("check_fs_specs.py"))
    expect_green("check-shell-specs", script("check_shell_specs.py"))
    expect_green("check-list-specs", script("check_list_specs.py"))
    expect_green("check-menu-specs", script("check_menu_specs.py"))
    expect_green("check-icon-specs", script("check_icon_specs.py"))
    expect_green("check-selftest-specs", script("check_selftest_specs.py"))
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
    negative_core_specs("missing-b2-type", mutate_delete_coordinator)
    negative_core_specs("missing-b2-group", mutate_drop_b2_group)
    negative_fs_specs("missing-adapter", mutate_delete_shell_operator)
    negative_fs_specs("hand-written-com", mutate_inject_comimport)
    negative_shell_specs("missing-layout", mutate_delete_layout_math)
    negative_shell_specs("missing-b3-group", mutate_drop_layout_group)
    negative_shell_specs("three-pane-degraded", mutate_break_three_pane)
    negative_shell_specs("missing-app-skeleton", mutate_delete_app_skeleton)
    negative_shell_specs("app-io", mutate_app_io)
    negative_shell_specs("cs6-syntax", mutate_cs6_shell)
    negative_list_specs("missing-filter", mutate_delete_filter_rules)
    negative_list_specs("missing-b4-group", mutate_drop_filter_group)
    negative_list_specs("batch-size-drift", mutate_break_batch_size)
    negative_list_specs("missing-list-state", mutate_delete_list_state)
    negative_list_specs("list-app-io", mutate_list_app_io)
    negative_list_specs("cs6-syntax", mutate_cs6_list)
    negative_menu_specs("missing-fusion", mutate_delete_fusion_rules)
    negative_menu_specs("missing-b5-group", mutate_drop_menu_group)
    negative_menu_specs("verb-scan-drift", mutate_verb_scan_drift)
    negative_menu_specs("product-verb-scan", mutate_inject_product_verb_scan)
    negative_menu_specs("missing-menu-state", mutate_delete_menu_state)
    negative_menu_specs("menu-app-io", mutate_menu_app_io)
    negative_menu_specs("cs6-syntax", mutate_cs6_menu)
    negative_icon_specs("missing-cache-rules", mutate_delete_cache_rules)
    negative_icon_specs("missing-b6-group", mutate_drop_b6_group)
    negative_icon_specs("cache-gate-drift", mutate_cache_gate_drift)
    negative_icon_specs("write-exception-drift", mutate_write_exception_drift)
    negative_icon_specs("missing-thumb-state", mutate_delete_thumb_state)
    negative_icon_specs("thumb-app-io", mutate_thumb_app_io)
    negative_icon_specs("cs6-syntax", mutate_cs6_icon)
    negative_selftest_specs("missing-report-rules", mutate_delete_report_rules)
    negative_selftest_specs("missing-b7-group", mutate_drop_b7_group)
    negative_selftest_specs("min-lines-drift", mutate_min_lines_drift)
    negative_selftest_specs("manifest-field-drift", mutate_manifest_field_drift)
    negative_selftest_specs("gate-inventory-drift", mutate_gate_inventory_drift)
    negative_selftest_specs("missing-selftest-state", mutate_delete_selftest_state)
    negative_selftest_specs("app-io", mutate_b7_app_io)
    negative_selftest_specs("cs6-syntax", mutate_cs6_selftest)
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
