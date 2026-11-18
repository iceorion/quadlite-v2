# 贡献指南

感谢参与 QuadLite。本项目采用 docs-first / spec-first 路线：规格先行，代码按批次迁入。

## 当前阶段

**B0 已完成**：工程骨架、检查脚本、夹具、CI 与证据闭环（设计决策记录 / 负向对照登记 / 运行账本）均已落地，根目录三个文档的构建与验收章节已补全。**B1 已完成**：`src/QuadLite.Core/` 13 个 L0 纯函数类型与 `tests/QuadLite.Tests/` 的 C# 零依赖 runner。**B2 已完成**：Core 新增 10 个编排 / 端口类型、Fs 新增 5 个适配器、C# runner 扩到 21 组。**B3 已完成**：Core 新增 9 个布局 / 导航 / 会话类型、`src/QuadLite.App/` 新增 2 个外壳状态类型、C# runner 扩到 27 组。**B4 已完成**：Core 新增 11 个列表视图规则类型、App 新增 2 个列表视图状态类型、C# runner 扩到 33 组。**B5 已完成**：Core 新增 8 个菜单规则类型、App 新增 2 个菜单状态类型、tests 新增 `FakeShellExtension` 四态替身、C# runner 扩到 43 组；**B6 已完成**：Core 新增 8 个图标缩略图规则类型、App 新增 2 个图标缩略图状态类型、C# runner 扩到 51 组、`run_tests.py` 扩到 55 组 / 45 条负向对照；**B7 已完成**：Core 新增 9 个自测 / 出包规则类型、App 新增 2 个状态内核、修订 B2 命令行契约、`tools/check_selftest_specs.py` 落地、C# runner 扩到 61 组、`run_tests.py` 扩到 64 组 / 53 条负向对照；**B8 已完成**：Core 新增 10 个发布线规则类型、App 新增 2 个状态内核、`tools/check_release_specs.py` 落地、C# runner 扩到 71 组、`run_tests.py` 扩到 75 组 / 63 条负向对照。**R1 已完成**（规格外实现批次，`design-v2.md` 一字未改）：新增 `src/QuadLite.Host/` 真实宿主层（进程入口 / 最小 WPF 窗口 / 产品 `--selftest`）、`tools/check_realization_specs.py`；`build.ps1` 改为真实单文件构建；CI 新增三步真编译并运行 exe；`run_tests.py` 在 R1 时扩到 89 组 / 76 条负向对照。**R2 已完成**（规格外实现批次，`design-v2.md` 一字未改）：新增 `src/QuadLite.Fs/ShellComInterop.cs` / `ShellTransport.cs`、`tools/com_policy.py` / `check_transport_specs.py`、CI `check-transport-specs` 一步，`run_tests.py` 在 R2 时扩到 102 组 / 88 条负向对照。**R3 已完成**（规格外实现批次，`design-v2.md` 一字未改）：新增 `src/QuadLite.Host/HostPolicies.cs` / `PaneView.cs` / `BrowseCommands.cs`、重写 `MainWindow.cs`、`tools/check_ui_specs.py`、CI `check-ui-specs` 一步，`run_tests.py` 扩到 118 组 / 103 条负向对照，`build.ps1` 源码计数 100 → 103。**R4 已完成**（规格外实现批次，`design-v2.md` 一字未改）：新增 `src/QuadLite.Fs/NativeMenuHost.cs`、`src/QuadLite.Host/MenuCommands.cs`、`tools/check_menu_host_specs.py`、`docs/adr/ADR-v2-014.md`，扩写 `ShellComInterop.cs` 的第二套 COM 声明、`PaneView` / `MainWindow` 右键接线、`QuadLite.Host.csproj`、`build.ps1`（103 → 105）、`SelftestRunner.cs`（8 组 R4 断言）、CI `check-menu-host-specs` 一步，`run_tests.py` 扩到 138 组 / 122 条负向对照。当前窗口按 `LayoutMath.Slots` 四档落位、用 `IDirSource` 枚举目录、`ListBox` 呈现条目，并把复制 / 移动 / 删除经 `OperationCoordinator` 接到 R2 transport；R4 起 Fs 层 `NativeMenuHost` 按 §5.2 标准四步接上 `IContextMenu` / `IContextMenu2/3` 与 owner 窗口五类消息转发，Host 右键接入不可变 `MenuTarget` 快照。**R5 已完成**（规格外实现批次，`design-v2.md` 一字未改）：新增 `src/QuadLite.Fs/IconThumbHost.cs`、`src/QuadLite.Host/IconThumbCommands.cs`、`tools/check_icon_thumb_specs.py`、`docs/adr/ADR-v2-015.md`，扩写 `ShellComInterop.cs` 的第三套 COM 声明（`IShellItemImageFactory` 一槽 vtable / `SIIGBF` 十个值）、`PaneView` 列表项图标位与后台取图、`MainWindow` provider 注入与状态栏、`QuadLite.Host.csproj`、`build.ps1`（105 → 107）、`SelftestRunner.cs`（8 组 R5 断言）、CI `check-icon-thumb-specs` 一步，`run_tests.py` 扩到 162 组 / 145 条负向对照。R5 起 Fs 层 `IconThumbHost` 按 §5.11 接上系统图像工厂取图链路，Host 列表项模板带图标位、图片两档自解码走 WPF 内置 WIC 系统包装（零手写 `IWIC*` vtable）；`Provide` 在未启用时连图片自解码也不做，只返回 `null`。transport、菜单宿主与图标宿主**都默认关闭**（命令 / 右键 / 列表项只回「未执行 / 文字占位 / 未验证」，偏差登记在 ADR-013 / ADR-014 D5 / ADR-015 D6）。**R6 已完成**（规格外实现批次，`design-v2.md` 一字未改）：新增 Fs 层 `ShellComProbe.cs`（阶段 0 COM 实例化探针，独立 STA 内 `CoInitializeEx` → `Activator.CreateInstance(CLSID_FileOperation)` → 释放，不碰文件）与 `ShellTransportProbe.cs`（阶段 1 在系统 temp 自建目录内显式 Enable `ShellTransport`，跑 Copy / Move / Recycle 三类操作并输出结构化结论，结束即清理）;`--diagnose` 分支接两个探针，CI 新增 record-only 的 `real-exe-probe-com` 与 `upload-artifact`，产出 exe 供本机交互桌面真机验证；三宿主仍默认关闭、`--diagnose` 不属 Normal 路径，本机 16 门卡与 `run_tests.py`（162 组）全绿、四层 109 个 `.cs`，**CI 实跑与真机结论待回填**，见 `ADR-v2-016`。拖放、七档视图与筛选、单实例后两段、崩溃哨兵与自绘兜底、LRU 介质与三段取证仍未接入；CI 不开窗口、不枚举目录、不实例化 COM、不解码、不写 thumbcache，**M1 / M2 / M3 均不成立**。`LICENSE` 与 `CODE_OF_CONDUCT.md` 已定稿，不重复创建。

偏差与未验证项见 `docs/adr/ADR-v2-001.md`（B0）、`ADR-v2-002.md`（B1）、`ADR-v2-004.md`（B2）、`ADR-v2-005.md`（B3）、`ADR-v2-006.md`（B4）、`ADR-v2-007.md`（B5）、`ADR-v2-008.md`（B6）、`ADR-v2-009.md`（B7）、`ADR-v2-010.md`（B8）、`ADR-v2-011.md`（R1）、`ADR-v2-012.md`（R2）、`ADR-v2-013.md`（R3）、`ADR-v2-014.md`（R4）、`ADR-v2-015.md`（R5）：本机不执行 C# 编译器，算法门卡由 CI 远端编译实跑；真实 `IFileOperation` 搬运、系统剪贴板互通、watcher 行为、冲突 / 进度 UI、真实键鼠交互与四档实际渲染、WIC 解码介质与图标呈现运行期未验证（R5 骨架已接线但默认关闭）；R4 起系统原生菜单 COM 宿主已接线但默认关闭、运行期完全未验证，与规格 §5.2「默认路径 = 系统原生菜单」的偏差登记在 ADR-014 D5；R5 起图标 / 缩略图取图链路与列表项图标位已接线但默认关闭、运行期完全未验证，与规格 §5.11「默认按视图档渲染」的偏差登记在 ADR-015 D6；R1 起真实产品 exe `--selftest` 与 `BUILD.txt` 采集已由 CI 实跑，但 `--bench` 采样、崩溃日志写入、零写入三段取证与 MSBuild 出包路径仍未实现或未验证；B8 的 H1–H6 真实取证（高 DPI 四档 / 三段取证 / 兼容矩阵 / WER / 性能采样 / OV 签名）与 C4 带杀软 VM 全部未执行，§8.4 的 M3 判据未达成；R1 起真实单文件 exe 的编译与产品 exe `--selftest` 由 CI 远端实测，MSBuild 出包路径、真实 shell 行为与 H1–H6 取证仍标「未验证」，不冒充通过。**布局已由用户拍板保持规格 v1.2 四档**，自定义分割树方案不采纳，规格一字未改；B3 按四档实现，B4 / B5 / B6 与布局无关。

在任何批次开工前，请先读 `design-v2.md`：它是需求、架构、接口与验收判据的唯一权威文档。

## 技术基线

| 项 | 规定 |
|---|---|
| 运行时 | .NET Framework 4.8 + WPF + C# 5 语法子集 |
| 工程文件 | SDK 风格 csproj；`FrameworkPathOverride` 参数化，不绑定单机工具链路径 |
| 依赖 | 无 NuGet；WPF / WIC / Windows Shell COM 均为系统 API |
| 出包 | `build.ps1`（R1 起真实编译）把四层源码一次编成单个 `QuadLite.exe` + 采集 `BUILD.txt` 七字段；零 NuGet、零合并工具、零 XAML 编译任务 |

> 检查链用 Python 3 标准库（零第三方依赖），本机与 CI 均可重跑并做负向对照；构建与出包仍是 `build.ps1`（Windows 专属）。门卡名 `check-boundaries` / `check-trace` 不变。

## 构建与验收（B0 起可用）

| 入口 | 用途 | 判定 |
|---|---|---|
| `python tools/check_trace.py` | 需求追踪矩阵（§2.1 / §7.1 / §8.1 / 附录 C 四方一致） | `verdict: GREEN`；缺门卡、缺批次或孤立门卡即 `exit 1` |
| `python tools/check_boundaries.py` | §4.2 的 B-01..B-07 + 六条不变量可静态项 | `verdict: GREEN`；违反即 `exit 1` |
| `python tools/check_core_specs.py` | B1/B2/B3/B4/B5/B6/B7/B8 静态契约：Core 类型齐备（78 个）/ C# 5 语法子集 / 枚举完备性（51 个）/ 71 组门卡注册 / Core 零 I/O | `verdict: GREEN`；任一缺失即 `exit 1` |
| `python tools/check_fs_specs.py` | B2 静态契约：Fs 适配器齐备 / 端口绑定 / 禁止手写 COM vtable / shell 调用点隔离 / C# 5 语法 | `verdict: GREEN`；任一违反即 `exit 1` |
| `python tools/check_shell_specs.py` | B3 静态契约：外壳类型齐备 / App 骨架 / 门卡组注册 / 规格与实现一致 / 四档与 3 窗格 / 折叠吸附 / `future-version` / App 零 I/O / C# 5 语法 | `verdict: GREEN`；任一违反即 `exit 1` |
| `python tools/check_list_specs.py` | B4 静态契约：列表规则类型齐备 / App 状态内核 / 门卡组注册 / 规格关键常量与不变量（4096 / 四列 / 五档 / 13 键）/ 规格与实现一致 / App 零 I/O / C# 5 语法 | `verdict: GREEN`；任一违反即 `exit 1` |
| `python tools/check_menu_specs.py` | B5 静态契约：菜单规则类型齐备 / App 状态内核 / 门卡组注册 / 22 项规格常量与不变量 / 产品程序集 verb 查询 0 次 / App 零 I/O / 假扩展四态 / 规格与实现一致 / C# 5 语法 | `verdict: GREEN`；任一违反即 `exit 1` |
| `python tools/check_icon_specs.py` | B6 静态契约：图标缩略图规则类型齐备 / App 状态内核 / 门卡组注册 / 26 项规格常量与不变量 / WIC 14 种与视频 15 种 / 唯一写入例外 / App 零 I/O / 规格与实现一致 / C# 5 语法 | `verdict: GREEN`；任一违反即 `exit 1` |
| `python tools/check_selftest_specs.py` | B7 静态契约：9 个 Core 类型 / 2 个 App 状态内核 / 10 组门卡注册 / 29 项规格常量 / §7.1 门卡总表 50 项逐项比对 / ≥100 行报告形态 / `CliArgs` 复用契约表 / 产品程序集 verb 查询 0 次 / 规格仍 v1.2 / C# 5 语法 | `verdict: GREEN`；任一违反即 `exit 1` |
| `python tools/check_release_specs.py` | B8 静态契约：10 个 Core 类型 / 2 个 App 状态内核 / 10 组门卡注册 / 38 项规格常量 / §8.3 依赖链 7 节点 + DPI 四档 + 降级链逐项比对 / M3 证据闭环 / C4 零误报门槛 / 产品程序集 verb 查询 0 次 / 规格仍 v1.2 / C# 5 语法 | `verdict: GREEN`；任一违反即 `exit 1` |
| `python tools/check_realization_specs.py` | R1 静态契约：Host 三类型 + 工程资产齐备 / 入口与窗口契约 / 产品自测 10 组运行时断言 / `build.ps1` 真实编译链 / CI 真跑 / 单程序集一致性 / 分层纪律 / C# 5 语法 / 规格仍 v1.2 / ADR 与账本齐备 | `verdict: GREEN`；任一违反即 `exit 1` |
| `python tools/check_transport_specs.py` | R2 静态契约：单一 COM 声明白名单 / 分层零声明 / SDK 权威 GUID / 21 / 16 / 5 段 vtable 顺序 / flags / 独立 STA 与生命周期 / 默认关闭 / 构建链与 CI / 规格未改 / 证据文档 / C# 5 / 诚实标注 | `verdict: GREEN`；任一违反即 `exit 1` |
| `python tools/check_ui_specs.py` | R3 静态契约：交付物齐备 / 真实 ListBox 与 IDirSource / 双侧迟到批次丢弃 / 四档真实落位 / 搬运命令接线 / 默认关闭且不冒充成功 / R2 分层 COM 口径未破坏 / Host 分层纪律与 C# 5 / 单程序集一致性 / CI 与旧闸门 / 规格未改 / 证据文档 / 产品 selftest / 诚实标注 | `verdict: GREEN`；任一违反即 `exit 1` |
| `python tools/check_menu_host_specs.py` | R4 静态契约：交付物齐备 / 唯一白名单 COM 声明 / SDK 权威 GUID 与 3 / 4 / 5 / 10 段 vtable / 零 verb 调用零删项零函数指针 / 标准四步顺序 / 参数硬校验 / 自有 ID 段与一条分隔线 / `IContextMenu2/3` 五类消息转发 / 目标快照 / try-finally 与独立 STA / Host 接线与分层 C# 5 / 构建 CI 链 / 规格未改 / 产品 selftest 与证据文档 / 诚实标注 | `verdict: GREEN`；任一违反即 `exit 1` |
| `python tools/check_icon_thumb_specs.py` | R5 静态契约：交付物齐备 / 唯一白名单 COM 声明 / SDK 权威 GUID 与一槽 vtable / `SIIGBF` 十个值与 `SIZE` / 四级通道与 WIC 白名单 / EXIF 八值 / 单 worker 与 generation 丢弃 / LRU 双闸与负缓存 / 8 秒看门狗与唯一写入例外 / 独立 STA 与默认关闭（含 `Provide` 不绕过开关）/ try-finally 释放 / Host 呈现接线与分层 C# 5 / 构建 CI 链 / 规格未改 / 产品 selftest 与证据文档 / 诚实标注 | `verdict: GREEN`；任一违反即 `exit 1` |
| `python tools/run_tests.py` | 零依赖 runner：162 组（正样本 + 145 条负向对照 + 合规扫描，含 13 条 R1、12 条 R2、15 条 R3、19 条 R4 与 23 条 R5 负向） | 每组一行 `ok=True/False`，末行 `verdict:`；退出码 = 失败数 |
| `python tools/check_open_source.py` | §10 开源合规全文扫描 | `verdict: GREEN`；命中即打回 |
| C# runner（CI 编译） | 71 组门卡（14 组 L0 + 7 组 B2 编排 + 6 组 B3 外壳 + 6 组 B4 列表 + 10 组 B5 菜单 + 8 组 B6 图标缩略图 + 10 组 B7 自测出包 + 10 组 B8 发布线），系统 .NET Framework 编译器 `/langversion:5`；B7 起编译命令同时包含 App 状态内核并输出 ≥100 行报告（B8 起 130 行） | 每组一行 `ok=True/False` + 末行 `verdict:`；退出码 = 红条数；**本机未执行编译器，标「未验证（CI 远端实测）」**；B6 首次实跑已通过（run `42343839222` / `c0529da`，51 组）；B7 首次实跑亦已通过（run `42357787519` / `266ca65`，61 组，十四步全 `success`）；B8 首次实跑亦已通过（run `42361122709` / `97fd31d`，71 组，十五步全 `success`） |
| Fs 库编译（CI） | Core + Fs 编成 library，不运行 | 编译成功即过；**运行期行为未验证** |
| App 库编译（CI） | Core + App 外壳 / 列表视图 / 菜单 / 图标缩略图 / 自测出包 / 发布线状态内核（12 个 .cs）编成 library，不引用呈现框架程序集、不运行 | 编译成功即过；**本步骤只编 App 库，真实 WPF 窗口 / 列表控件由 `real-exe-build` 构建；R4 原生菜单与 R5 图标 / 缩略图取图宿主均已接线但默认关闭、运行期未验证** |
| `build.ps1`（CI 实跑） | R1 真实单文件构建：系统 C# 编译器以 `/target:winexe /langversion:5` 把 Core / Fs / App / Host 四层 107 个 .cs 一次编成 `QuadLite.exe`，显式引用 WPF 四个程序集 + PerMonitorV2 manifest，并采集 `BUILD.txt` 七字段（含真实大小与 SHA256） | 编译成功 + exe 存在 + 七字段齐；**CI 远端实测**，本机不执行编译器，标「未验证（本机）」 |
| exe 形态 `--selftest`（CI 实跑） | R1 产品 exe 无窗口自测：门卡总表明细 + R1 十组 Core 契约断言 + R2 四组 COM 契约 + R3 六组浏览 / 搬运 UI 契约 + R4 八组原生菜单宿主契约 + R5 八组图标缩略图宿主契约；不创建窗口、不枚举目录、不实例化 COM、不解码 | ≥100 行、末行 `verdict: GREEN`、退出码 0；**CI 远端实测**；`--bench` / `--shot` / `--dumpmenu` 诊断装置仍未实现 / 未验证 |
| `QuadLite.Host.csproj`（IDE 入口） | 同样的四层源码单程序集声明，供 IDE 打开 | MSBuild 出包路径**尚未实跑，未验证** |
| `.github/workflows/ci.yml` | 十七道 Python 门卡 + C# runner 编译运行 + Fs / App 库编译 + R1 真实 exe 构建与 `--selftest` 实跑 + R2 托管 COM 契约核对 + R3 浏览 / 搬运 UI 契约核对 + R4 原生菜单宿主契约核对 + R5 图标缩略图宿主契约核对 | B0（run `42013589562` / `96a5fa4`）、B1（run `42022736600` / `dcea585`）、B2（run `42035381476` / `1066b0d`，八步全 `success`）、B3（run `42040769639` / `378432a`，十步全 `success`）、B4（run `42224901915` / `8ed8b5b`，十一步全 `success`）、B5（run `42232312609` / `aa63769`，十二步全 `success`）与 B6（run `42343839222` / `c0529da`，十三步全 `success`）首次实跑均已通过；B7（run `42357787519` / `266ca65`，十四步全 `success`）首次实跑亦已通过，含 `check-selftest-specs`、61 组 runner 与 10 文件 App 库编译；B8（run `42361122709` / `97fd31d`，十五步全 `success`）首次实跑亦已通过，含 `check-release-specs`、71 组 runner 与 12 文件 App 库编译，逐批结论见 `docs/run-ledger.md`；R1 起 CI 新增 `check-realization-specs` / `real-exe-build` / `real-exe-selftest` 三步，真实编译并运行产品 exe 的 `--selftest`（run `42376298215` / head `a84b646`，十八个门卡 / 编译步骤全 `success`）；R2 起 CI 新增 `check-transport-specs`，真实编译含托管 COM 声明的 Fs 层与单 exe（run `43713387488` / head `bbbe0e5`，十九个门卡 / 编译步骤全 `success`），但只编译不实例化 COM；R3 起 CI 新增 `check-ui-specs`（run `43717485240` / head `0836109`，二十个门卡 / 编译步骤全 `success`；首次功能提交 `d8ba545` 的 run `43716871530` 因 C# 5 CS0165 在 `real-exe-build` 判红，由修复提交 `0836109` 修正后复跑全绿，该次红不作为通过证据），单 exe 已含四档落位 / 目录枚举 / 列表控件 / 搬运命令，但 CI 不开窗口、不枚举目录、不实例化 COM；R4 起 CI 新增 `check-menu-host-specs`（run `43874542767` / head `fd11116`，二十一个门卡 / 编译步骤全 `success`），单 exe 已含标准四步菜单宿主但 CI 不实例化 COM；R5 起 CI 新增 `check-icon-thumb-specs`（共二十二个门卡 / 编译步骤），单 exe 已含第三套 COM 声明与图标呈现接线，但 CI 不实例化 COM、不解码、不写 thumbcache；三个宿主都默认关闭，真实系统图像工厂 / 图标呈现、七档视图与筛选、崩溃哨兵 / 自绘兜底、真实 shell 行为与 H1–H6 真实取证仍为「未验证」；R5 的 CI 首次实跑已通过（run `44318088019` / head `61a9ef4`，`gates` job 与二十二个门卡 / 编译步骤逐项 `success`，含 `real-exe-build` 编译 107 个 .cs 与 `real-exe-selftest` 8 组 R5 断言，按 jobs API 的 step `conclusion` 记录，日志端点匿名 403 未读原文）；毫秒 / 时序门卡只报数不判定 |

门卡必须「改坏必红」：故意破坏对应不变量时门卡要变红，登记表见 `docs/negative-controls.md`。负向对照按**不变量**覆盖，不按门卡逐条配。夹具在 `tools/fixtures/boundaries/`，`ok/` 保持绿、`bad/` 每条触发指定规则。

## 目录结构

```
Directory.Build.props     共享构建属性（net48 / C# 5 / FrameworkPathOverride）
build.ps1                 构建与出包入口
src/QuadLite.Core/        L0 领域层 + B2 编排层 + B3 外壳内核 + B4 列表规则层 + B5 菜单规则层 + B6 图标缩略图规则层 + B7 自测 / 出包规则层 + B8 发布线规则层（78 个纯函数 / 端口类型，零 I/O；B7 修订 CliArgs）
src/QuadLite.Fs/          L3 适配器层（B2 的 5 个适配器 + R2 的托管 COM 绑定与 transport + R4 的原生菜单 COM 宿主 + R5 的图标 / 缩略图取图宿主，共 9 个 .cs；运行期未验证）
src/QuadLite.App/         L4 状态内核（B3 外壳 2 个 + B4 列表视图 2 个 + B5 菜单 2 个 + B6 图标缩略图 2 个 + B7 自测 / 出包 2 个 + B8 发布线 2 个纯逻辑类型；窗口 / XAML / 列表控件 / COM 宿主 / 渲染不在本层，由 Host 层承担）
src/QuadLite.Host/        R1 真实宿主层（Program / MainWindow / SelftestRunner + csproj + app.manifest）+ R3 浏览 / 搬运 UI（HostPolicies / PaneView / BrowseCommands）+ R4 右键菜单接线（MenuCommands）+ R5 图标命令（IconThumbCommands；MainWindow 已重写为四档落位 + 列表控件 + 工具栏 + 状态栏）；四层 107 个 .cs 一次编成单 exe
tests/QuadLite.Tests/     C# 零依赖 runner（71 组门卡 + 130 行门卡总表报告 + Fake 端口 + FakeShellExtension）
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

`docs/adr/`，编号 `ADR-v2-0xx`，每条含「决策 / 理由 / 判据 / 负向对照 / 状态」，并标清「沿用 / 修订 / 作废」。已生效：`ADR-v2-001.md`（B0）、`ADR-v2-002.md`（B1）、`ADR-v2-004.md`（B2）、`ADR-v2-005.md`（B3）、`ADR-v2-006.md`（B4）、`ADR-v2-007.md`（B5）、`ADR-v2-008.md`（B6）、`ADR-v2-009.md`（B7）、`ADR-v2-010.md`（B8）、`ADR-v2-011.md`（R1）、`ADR-v2-012.md`（R2）、`ADR-v2-013.md`（R3）、`ADR-v2-014.md`（R4）、`ADR-v2-015.md`（R5）、`ADR-v2-016.md`（R6）。`ADR-v2-003` 编号曾用于一次被撤回的布局改版，跳过不再复用；用户已拍板保持规格 v1.2 四档，该改版不翻案。新增需求必须同步更新 `design-v2.md` 附录 C 并通过 `check-trace`；若将来重开布局决策，仍须同步 `check_shell_specs.py` / `check_list_specs.py` / `check_menu_specs.py` / `check_icon_specs.py` / `check_selftest_specs.py` / `check_release_specs.py` 的「规格与实现一致」门卡。

## 提交前开源审查

按 `design-v2.md` §10 逐条检查。不得带入：截图、取证文本、账本原文、`BUILD.txt`、任何二进制产物、内部绝对路径、代理端口、凭据、机器名 / 用户名、真实桌面路径；不得引用不随仓发布的分析记录或历史过程稿；不得写「运行期写入为 0」「全绿」「包目录只有 exe」「绝不崩溃」「与系统文件管理器完全一致」等失效引用或过度宣称。

## 报告问题

功能缺陷、规格疑问走公开 Issue，并附可复核的复现步骤。安全漏洞走 `SECURITY.md` 的私密渠道，不要在公开 Issue 贴细节、日志原文或环境信息。
