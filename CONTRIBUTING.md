# 贡献指南

感谢参与 QuadLite。本项目采用 docs-first / spec-first 路线：规格先行，代码按批次迁入。

## 当前阶段

**B0 已完成**：工程骨架、检查脚本、夹具、CI 与证据闭环（设计决策记录 / 负向对照登记 / 运行账本）均已落地，根目录三个文档的构建与验收章节已补全。**B1 已完成**：`src/QuadLite.Core/` 13 个 L0 纯函数类型与 `tests/QuadLite.Tests/` 的 C# 零依赖 runner。**B2 已完成**：Core 新增 10 个编排 / 端口类型、Fs 新增 5 个适配器、C# runner 扩到 21 组；`src/QuadLite.App/` 仍为空，**B3 起**才进入 WPF 外壳。`LICENSE` 与 `CODE_OF_CONDUCT.md` 已定稿，不重复创建。

偏差与未验证项见 `docs/adr/ADR-v2-001.md`（B0）、`ADR-v2-002.md`（B1）、`ADR-v2-004.md`（B2）：本机不执行 C# 编译器，算法门卡由 CI 远端编译实跑；真实 `IFileOperation` 搬运、系统剪贴板互通、watcher 行为与冲突 / 进度 UI 均未实现或未验证；WPF 应用构建与出包标「未验证」，不冒充通过。

在任何批次开工前，请先读 `design-v2.md`：它是需求、架构、接口与验收判据的唯一权威文档。

## 技术基线

| 项 | 规定 |
|---|---|
| 运行时 | .NET Framework 4.8 + WPF + C# 5 语法子集 |
| 工程文件 | SDK 风格 csproj；`FrameworkPathOverride` 参数化，不绑定单机工具链路径 |
| 依赖 | 无 NuGet；WPF / WIC / Windows Shell COM 均为系统 API |
| 出包 | `build.ps1 -SingleFile` 产出单 exe + `BUILD.txt` |

> 检查链用 Python 3 标准库（零第三方依赖），本机与 CI 均可重跑并做负向对照；构建与出包仍是 `build.ps1`（Windows 专属）。门卡名 `check-boundaries` / `check-trace` 不变。

## 构建与验收（B0 起可用）

| 入口 | 用途 | 判定 |
|---|---|---|
| `python tools/check_trace.py` | 需求追踪矩阵（§2.1 / §7.1 / §8.1 / 附录 C 四方一致） | `verdict: GREEN`；缺门卡、缺批次或孤立门卡即 `exit 1` |
| `python tools/check_boundaries.py` | §4.2 的 B-01..B-07 + 六条不变量可静态项 | `verdict: GREEN`；违反即 `exit 1` |
| `python tools/check_core_specs.py` | B1/B2 静态契约：Core 类型齐备 / C# 5 语法子集 / 枚举完备性 / 21 组门卡注册 / Core 零 I/O | `verdict: GREEN`；任一缺失即 `exit 1` |
| `python tools/check_fs_specs.py` | B2 静态契约：Fs 适配器齐备 / 端口绑定 / 禁止手写 COM vtable / shell 调用点隔离 / C# 5 语法 | `verdict: GREEN`；任一违反即 `exit 1` |
| `python tools/run_tests.py` | 零依赖 runner：25 组（正样本 + 19 条负向对照 + 合规扫描） | 每组一行 `ok=True/False`，末行 `verdict:`；退出码 = 失败数 |
| `python tools/check_open_source.py` | §10 开源合规全文扫描 | `verdict: GREEN`；命中即打回 |
| C# runner（CI 编译） | 21 组门卡（14 组 L0 + 7 组 B2 编排），系统 .NET Framework 编译器 `/langversion:5` | 每组一行 `ok=True/False` + 末行 `verdict:`；退出码 = 红条数；**本机未执行编译器，标「未验证（CI 远端实测）」** |
| Fs 库编译（CI） | Core + Fs 编成 library，不运行 | 编译成功即过；**运行期行为未验证** |
| `build.ps1 -SingleFile` | 单文件出包 | 产出 exe + `BUILD.txt`，字段齐备；**B0 未实跑（未验证）** |
| exe 形态 `--selftest` | 无人工界面自测，跑全部门卡 | 每组一行 `组名: ok=True/False` + 末行 `verdict:`；退出码 = 红条数；**随 B7 接入（未验证）** |
| `.github/workflows/ci.yml` | 六道 Python 门卡 + C# runner 编译运行 + Fs 库编译 | B0 首次实跑已通过（run `42013589562` / `96a5fa4`）；B1 首次实跑已通过（run `42022736600` / `dcea585`，六步全 `success`）；B2 新增 `check-fs-specs` 与 `csharp-fs-compile`，逐批结论见 `docs/run-ledger.md`；CI 不含 WPF 应用构建与出包，那两项仍为「未验证」；毫秒 / 时序门卡只报数不判定 |

门卡必须「改坏必红」：故意破坏对应不变量时门卡要变红，登记表见 `docs/negative-controls.md`。负向对照按**不变量**覆盖，不按门卡逐条配。夹具在 `tools/fixtures/boundaries/`，`ok/` 保持绿、`bad/` 每条触发指定规则。

## 目录结构

```
Directory.Build.props     共享构建属性（net48 / C# 5 / FrameworkPathOverride）
build.ps1                 构建与出包入口
src/QuadLite.Core/        L0 领域层 + B2 编排层（23 个纯函数 / 端口类型，零 I/O）
src/QuadLite.Fs/          L3 适配器层（B2 已落地 5 个实现，真实 shell 调用点待接入）
src/QuadLite.App/         L4 WPF 外壳（B3 起）
tests/QuadLite.Tests/     C# 零依赖 runner（21 组门卡 + Fake 端口）
tools/                    检查链、夹具、合规扫描、静态契约核对
.github/workflows/        CI 闸门
docs/adr/                 设计决策记录
docs/negative-controls.md 负向对照与写入例外清单
docs/run-ledger.md        运行账本
```

## 每批 DoD

① 静态边界检查绿；② 该批门卡全绿 + 至少 1 条负向对照实测变红；③ 单测全绿并记入账本，账本 `head_sha` 必须等于当次提交；④ 设计决策记录写清「沿用 / 修订 / 作废」；⑤ `design-v2.md` §10 开源合规清单通过。

一次只改一个功能域（C-05）。每条验收结论带证据等级，未跑过的一律写「未验证」；追求「全绿」式表述、用毫秒指标做回归闸门都在非目标之列。

## 人工验收项

以下三项无法完全自动化，需在记录中写明环境与结论：

- **NFR-04 同手感**：菜单、图标、类型名、拖放、快捷键与 Windows 资源管理器对照。
- **NFR-10 高 DPI**：100 / 125 / 150 / 200% 四档对照，列宽、列头判据与几何断言不退化。
- **§6.3 三项**：系统模态菜单的视觉验收、四档 DPI 对照、与资源管理器双向拖放。
- **NFR-11 性能预算**：枚举 / 滚动 / 导航 / 启动四项，每行记「机器 + 系统版本 + 文件系统 + DPI + 负载规模 + 实测值 + 证据等级」；只用于跨版本阅读，不作为 CI 闸门。

判据不得依赖中文字符串或固定 verb 文本（NFR-12）。

## 设计决策记录

`docs/adr/`，编号 `ADR-v2-0xx`，每条含「决策 / 理由 / 判据 / 负向对照 / 状态」，并标清「沿用 / 修订 / 作废」。已生效：`ADR-v2-001.md`（B0）、`ADR-v2-002.md`（B1）、`ADR-v2-004.md`（B2）。`ADR-v2-003` 编号曾用于一次被撤回的布局改版，跳过不再复用。新增需求必须同步更新 `design-v2.md` 附录 C 并通过 `check-trace`。

## 提交前开源审查

按 `design-v2.md` §10 逐条检查。不得带入：截图、取证文本、账本原文、`BUILD.txt`、任何二进制产物、内部绝对路径、代理端口、凭据、机器名 / 用户名、真实桌面路径；不得引用不随仓发布的分析记录或历史过程稿；不得写「运行期写入为 0」「全绿」「包目录只有 exe」「绝不崩溃」「与系统文件管理器完全一致」等失效引用或过度宣称。

## 报告问题

功能缺陷、规格疑问走公开 Issue，并附可复核的复现步骤。安全漏洞走 `SECURITY.md` 的私密渠道，不要在公开 Issue 贴细节、日志原文或环境信息。
