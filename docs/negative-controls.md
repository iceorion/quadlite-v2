# 负向对照登记

> 按「不变量」覆盖，不按门卡逐条配（§7.2）：每条不变量至少一条负向对照，故意破坏该不变量后对应门卡必须变红。
> B0 的负向对照由 `tools/run_tests.py` 无人工界面执行，逐条结果见 `docs/run-ledger.md` 的 B0 行；下表登记「不变量 → 用例 → 应变红的门卡」三列。

## B0 登记

| 不变量 | 负向对照用例 | 应变红的门卡 |
|---|---|---|
| L0 领域层零 I/O | `bad/B-01_Core_UsesIO.cs` 在 Core 引入 `System.IO` | `B-01` |
| 只有 Fs 可用 `using System.IO` | `bad/B-02_App_UsesIO.cs` 在 App 引入 `System.IO` | `B-02` |
| UI 不直接搬运文件 | `bad/B-03_App_DirectCopy.cs` 直调 `File.Copy` | `B-03`、`B-07` |
| 产品程序集不读 verb | `bad/B-04_App_VerbScan.cs` 出现 `GetCommandString` | `B-04` |
| XAML 不写死列宽 / 行高 | `bad/B-05_App_FixedWidth.xaml` 写死 `Width` | `B-05` |
| 产品不引用测试替身类型 | `bad/B-06_App_ProbeRef.cs` 引用 `QuadLite.Probe` | `B-06` |
| 所有搬运经 `IFileOperator` 端口 | `bad/B-07_Core_DirectMove.cs` 直调 `File.Move` | `B-07` |
| 零联网 / 零注册表 / 零系统配置目录写入 | `bad/INV-02_App_Network.cs` 出现联网 API | `INV-02` |
| 危险判级只在 `SafetyGuard` 一处 | `bad/INV-05_App_DangerLevel.cs` 在别处判级 | `INV-05` |
| 需求 ↔ 门卡双向可追踪 | 临时副本删掉 FR-22 的 `clipboard-*` 映射 | `check-trace` |
| 公开仓不带入失效口号与私有资产 | 临时副本写入正面旧口号 | `§10` 扫描 / `INV-06` |

正样本对照：`tools/fixtures/boundaries/ok/` 三个文件必须保持门卡绿。夹具全部是可公开的文本源码，不是既有私有资产。

## 未验证项（如实标注，不用「没报错」冒充通过）

| 项 | 验证方式 | 状态 |
|---|---|---|
| 与资源管理器同手感（NFR-04 / INV-04） | 人工验收项，写入 `CONTRIBUTING.md` | 未验证 |
| 运行期零写入三段取证（H3）、高 DPI 四档（H1）、兼容矩阵（H2） | 发布线 H1–H6 | 未验证 |
| 危险判级矩阵逐格、路径解析、会话坏值归一 | B1 / B2 单测 | 未验证 |
| 产品程序集 `GetCommandString` 命中 0 次 | B5 门卡实测 | 未验证 |
| MSBuild 构建、`build.ps1 -SingleFile`、exe 形态 `--selftest` | B1 引入源码后接入 CI，B7 完成出包 | 未验证（B0 未实跑，CI 亦未包含 .NET 构建） |

## 远端验证（CI）

CI run `42013589562`（head `96a5fa4`，2026-11-05）在远端流水线实跑，job `gates` 结论 `success`，四个门卡步骤逐项 `success`：`check-trace`、`check-boundaries`、`run-tests (includes negative controls)`、`open-source compliance`。该 run 只覆盖四个 Python 门卡，**不包含** MSBuild 构建、出包与 exe 形态 `--selftest`，故上表三项维持「未验证」；本机实测与 CI 结果一并记在 `docs/run-ledger.md` 的 B0 行。

## 写入例外清单

> 规范性说明：本节只登记产品运行期允许写入的位置。`tools/check_boundaries.py` 的 INV-06 会核对条目数，此处必须与 `design-v2.md` §5.5 / §6.4 一致；新增例外须同步改规格并重新跑门卡。

- 视频缩略图写入 Windows 系统缩略图缓存（thumbcache），与系统文件管理器同源；首版唯一显式例外。
