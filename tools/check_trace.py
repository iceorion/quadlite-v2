#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
check-trace 门卡：需求 → 设计 → 门卡 → 批次 双向可核对（§7.1 / 附录 C）。

扫描 design-v2.md：
  ① FR-01..36 编号完整唯一；② 每条 FR 的验收指向含设计章节与「已认领」门卡；
  ③ 附录 C 组级范围展开恰好覆盖 1..36；④ 附录 C 门卡组在 §7.1 有定义；
  ⑤ §7.1 通配组须被附录 C 认领（无孤立门卡）；⑥ 附录 C 批次号在 §8.1 存在；
  ⑦ §8.1 不引入无主门卡组。

零第三方依赖。支持 --doc 指向负向对照副本。退出码：0 = GREEN，1 = RED。
"""
import argparse
import re
import sys
from pathlib import Path

if hasattr(sys.stdout, "reconfigure"):
    sys.stdout.reconfigure(encoding="utf-8")

ROOT = Path(__file__).resolve().parents[1]
TOKEN_RE = r"[a-z][a-z0-9*\-]+"


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--doc", default=str(ROOT / "design-v2.md"))
    args = ap.parse_args()

    lines = Path(args.doc).read_text(encoding="utf-8").splitlines()
    errors = []

    def fail(msg):
        errors.append(msg)

    def block(start, end):
        s = next(i for i, l in enumerate(lines) if re.search(start, l))
        e = next((i for i in range(s + 1, len(lines)) if re.search(end, lines[i])), len(lines))
        return lines[s:e]

    def cells(line):
        if not line.startswith("|"):
            return []
        return [c.strip() for c in line.strip().strip("|").split("|")]

    def toks(s):
        return [t for t in re.findall(r"`([^`]+)`", s) if re.fullmatch(TOKEN_RE, t)]

    s21 = block(r"^### 2\.1 ", r"^### 2\.2 ")
    s71 = block(r"^### 7\.1 ", r"^### 7\.2 ")
    s81 = block(r"^### 8\.1 ", r"^### 8\.2 ")
    apC = block(r"^## 附录 C", r"^__NO_SUCH_HEADING__")

    gates71 = set()
    for l in s71:
        c = cells(l)
        if len(c) >= 2:
            gates71.update(toks(c[1]))

    batch_gates = set()
    for l in s81:
        c = cells(l)
        if len(c) >= 3:
            batch_gates.update(toks(c[2]))

    c_rows = [cells(l) for l in apC if l.startswith("|") and "FR-" in " ".join(cells(l))]
    c_groups = set()
    for c in c_rows:
        if len(c) >= 5 and c[1].startswith("FR-"):
            c_groups.update(toks(c[3]))

    def is_gate(t):
        if t.endswith("-*"):
            return t in c_groups
        return t in gates71

    def resolvable(group):
        if group in gates71:
            return True
        if group.endswith("-*"):
            prefix = group[:-2] + "-"
            return any(g.startswith(prefix) for g in gates71)
        return False

    # 1. FR 编号完整性
    fr = {}
    for l in s21:
        c = cells(l)
        if len(c) < 4:
            continue
        m = re.fullmatch(r"FR-(\d{2})", c[1])
        if not m:
            continue
        n = int(m.group(1))
        if n in fr:
            fail("FR-%02d 重复定义" % n)
        fr[n] = c[3]
    if sorted(fr) != list(range(1, 37)):
        fail("FR 编号应为 1..36，实际 %s" % sorted(fr))

    # 2. 每条 FR 的验收指向
    for n, cell in sorted(fr.items()):
        tk = toks(cell)
        if "§" not in cell:
            fail("FR-%02d 验收指向缺设计章节：%s" % (n, cell))
        if not any(is_gate(t) for t in tk):
            fail("FR-%02d 验收指向缺已认领门卡组：%s" % (n, cell))
        for t in tk:
            if t.endswith("-*") and t not in c_groups:
                fail("FR-%02d 引用了附录 C 未认领的门卡组 `%s`" % (n, t))

    # 3. 附录 C 组级覆盖
    covered = []
    for c in c_rows:
        if len(c) >= 5 and c[1].startswith("FR-"):
            for a, b in re.findall(r"FR-(\d{2})\.\.(\d{2})", c[1]):
                covered.extend(range(int(a), int(b) + 1))
    if sorted(covered) != list(range(1, 37)):
        fail("附录 C 组级覆盖异常：%s" % sorted(set(covered)))

    # 4. 附录 C 门卡组 ∈ §7.1
    for g in sorted(c_groups):
        if not resolvable(g):
            fail("附录 C 门卡组 `%s` 在 §7.1 无对应门卡/门卡组" % g)

    # 5. §7.1 通配组须被认领
    for g in sorted(gates71):
        if g.endswith("-*") and g not in c_groups and not any(
            gg.startswith(g[:-2] + "-") for gg in c_groups
        ):
            fail("§7.1 门卡组 `%s` 无附录 C 需求认领" % g)

    # 6. 批次号
    batch_ids = set()
    for l in s81:
        c = cells(l)
        if len(c) >= 2:
            m = re.match(r"^B(\d+)\b", c[0])
            if m:
                batch_ids.add("B" + m.group(1))
    for c in c_rows:
        if len(c) >= 5 and c[1].startswith("FR-"):
            for b in re.findall(r"B\d+", c[4]):
                if b not in batch_ids:
                    fail("附录 C 批次 %s 不在 §8.1 批次表中" % b)

    # 7. §8.1 无主组
    for g in sorted(batch_gates):
        if g.endswith("-*") and g not in c_groups and not resolvable(g):
            fail("§8.1 门卡 `%s` 无附录 C 认领且 §7.1 无定义" % g)

    print("check-trace: FR=%d 条, 附录C组=%d 个, §7.1门卡=%d 项, 批次=%s"
          % (len(fr), len(c_groups), len(gates71), ",".join(sorted(batch_ids))))
    if errors:
        for e in errors:
            print("  RED  " + e)
        print("verdict: RED (%d)" % len(errors))
        return 1
    print("verdict: GREEN")
    return 0


if __name__ == "__main__":
    sys.exit(main())
