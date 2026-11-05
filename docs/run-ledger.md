# 运行账本

> 每批一行，`head_sha` 必须是该批**功能交付提交**的 SHA（§8.2 DoD ③）。
> SHA 自指的处理：一个提交无法在同一提交里写下自己的 SHA，因此账本行在该批功能提交之后的**回填提交**中写入；回填提交只改本文件，不产生新批次行，也不改动功能提交内容。
> 复核方式：`git cat-file -e <head_sha>` 且 `git show -s --format=%s <head_sha>` 能对应到该批提交信息。
> 证据等级：实测（本机 runner 已跑）/ CI（远端流水线）/ 人工（需人工验收）/ 未验证。禁止用「连跑 N 次全绿」单独作结论。

| 批次 | head_sha | check-boundaries | check-trace | run_tests | 负向对照 | §10 扫描 | 证据等级 | 备注 |
|---|---|---|---|---|---|---|---|---|
| B0 | `9f5f0e0` | 绿 | 绿 | 绿（15 组 / 0 失败） | 11 条全部变红 | 绿 | 实测 + CI | 工程骨架、检查链、CI、证据闭环；0 行产品代码；本机四门卡实测绿，CI run `42013589562`（head `96a5fa4`）四步全 `success`；CI 不含 .NET 构建，构建与出包仍未验证，见 `docs/adr/ADR-v2-001.md` |
| B1 | `dcea585` | 绿 | 绿 | 绿（20 组 / 0 失败） | 15 条全部变红（含 4 条 B1 静态契约） | 绿 | 本机实测 + CI | L0 纯函数层 13 个类型 + C# runner 14 组门卡 + `check-core-specs`；本机五道门卡实测绿；CI run `42022736600`（head `dcea585`）六步全 `success`，含 `csharp-runner` 编译 + 运行（退出码 0 ⇒ 14 组 `ok=True`、`verdict: GREEN`）；本机不执行 C# 编译器，该层本机未验证；WPF 应用构建 / 出包 / exe `--selftest` 仍未验证，见 `docs/adr/ADR-v2-002.md` |
| B2 | `1066b0d` | 绿 | 绿 | 绿（25 组 / 0 失败） | 19 条全部变红（含 4 条 B2 静态契约） | 绿 | 本机实测 + CI | Core 新增 10 个编排 / 端口类型 + Fs 新增 5 个适配器 + `check-fs-specs` + C# runner 21 组；本机六道门卡实测绿；CI run `42035381476`（head `1066b0d`）八步全 `success`，含 `csharp-runner`（21 组 `ok=True`、`verdict: GREEN`）与 `csharp-fs-compile`（库编译通过，不运行）；本机不执行 C# 编译器，该层本机未验证；真实 `IFileOperation` / 系统剪贴板 / watcher / 冲突进度 UI 未实现或未验证，见 `docs/adr/ADR-v2-004.md` |
| B3 | 待回填 | 绿 | 绿 | 绿（32 组 / 0 失败） | 25 条全部变红（含 6 条 B3 静态契约） | 绿 | 本机实测；C# runner 与 App 库编译结论待 CI 首次实跑 | Core 新增 9 个外壳内核类型 + App 新增 2 个外壳状态类型 + `check-shell-specs` + C# runner 27 组；`head_sha` 与 CI run 结论由回填提交写入；布局按 v1.2 四档、规格未改；真实 WPF 窗口 / XAML / 消息循环、系统原生菜单、冲突进度面板、会话原子写、真实文件搬运未实现或未验证，见 `docs/adr/ADR-v2-005.md` |

> 后续批次沿用同一行格式；每批结束时须记当次 `head_sha`，未跑过的一律写「未验证」。
