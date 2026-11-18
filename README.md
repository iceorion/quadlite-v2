# QuadLite

一个面向 Windows 的便携多窗格文件浏览器**规格项目**。仓库按 docs-first / spec-first 路线推进：规格定稿 → B0 工程骨架与检查链 → B1 L0 纯函数层 → B2 编排与适配器层 → B3 外壳状态内核 → B4 列表视图规则层 → B5 系统原生右键菜单规则层 → B6 图标 / 缩略图规则层 → B7 自测 / 门卡总表 / 出包自检规则层 → **B8 发布线 C4 + H1–H6 规则层** → **R1 真实实现第一批（单文件 WPF 宿主 / 产品 `--selftest` / 真实构建链）** → **R2 真实 `IFileOperation` 托管 COM 绑定与 Fs transport** → **R3 浏览 / 搬运 UI 真实接线** → **R4 系统原生菜单 COM 宿主** → **R5 图标 / 缩略图取图链路与呈现接线**。R1 起 CI 已能编译出真实 `QuadLite.exe` 并运行它的 `--selftest`；R2 起真实搬运代码路径已编进同一个 exe；R3 起窗口按 `LayoutMath.Slots` 四档落位、用 `IDirSource` 枚举目录、`ListBox` 呈现条目，并把复制 / 移动 / 删除接到 R2 transport。R4 起 Fs 层 `NativeMenuHost` 按 §5.2 标准四步接上 `IContextMenu` / `IContextMenu2/3` 与 owner 窗口消息转发，Host 右键把不可变 `MenuTarget` 快照接入（SDK 逐字对应声明，产品路径零 verb 查询 / 零删项）。R5 起 Fs 层 `IconThumbHost` 接上 `IShellItemImageFactory` 与四级图标通道，Host 列表项模板带图标位、图片两档自解码走 WPF 内置 WIC 系统包装（不新增手写 COM vtable）。transport、菜单宿主与图标宿主**都默认关闭**、CI 不开窗口、不枚举目录、不实例化 COM、不写 thumbcache，真实交互、原生菜单、系统图像工厂与图标呈现运行期完全未验证；拖放 / 崩溃哨兵 / 自绘兜底 / LRU 介质 / 三段取证仍未实现；**没有发布版本，M1 / M2 / M3 均不成立**（本机可用 `build.ps1` 自行编译单 exe 测试）。

- 许可证：MIT（见 `LICENSE`）
- 规范：`design-v2.md`（需求、架构、接口与验收判据的唯一权威文档）
- 参与方式：见 `CONTRIBUTING.md`；安全问题见 `SECURITY.md`；行为准则见 `CODE_OF_CONDUCT.md`

## 当前状态

| 项 | 状态 |
|---|---|
| 规格 | `design-v2.md` v1.2 已定稿（B0 修订检查链脚本名、确认三项待决） |
| 工程骨架 | B0 已完成：SDK 风格 csproj、构建脚本、检查链、CI、证据闭环 |
| 代码 | `src/QuadLite.Core/` 78 个零 I/O 类型（B1 的 13 个 L0 + B2 的 10 个编排 / 端口 + B3 的 9 个布局 / 导航 / 会话 + B4 的 11 个列表视图规则 + B5 的 8 个菜单规则 + B6 的 8 个图标缩略图规则 + B7 的 9 个自测 / 出包规则 + B8 的 10 个发布线规则）；`src/QuadLite.Fs/` 9 个 .cs（B2 的 5 个适配器 + R2 的托管 COM 绑定与 transport + R4 的原生菜单 COM 宿主 + R5 的图标缩略图宿主）；`src/QuadLite.App/` 12 个状态类型（无窗口 / XAML）；`src/QuadLite.Host/` 8 个类型（R1 的进程入口 / 最小窗口 / 产品自测 + R3 的注入端口与 UI 判据 / 真实浏览窗格 / 搬运命令接线 + R4 的右键菜单命令 + R5 的图标命令，R1 起由 CI 编译成单 exe，四层合计 107 个 .cs） |
| C# runner | 71 组门卡（14 组 L0 + 7 组 B2 + 6 组 B3 + 6 组 B4 + 10 组 B5 + 8 组 B6 + 10 组 B7 + 10 组 B8），CI 上以 C# 5 编译实跑，B7 起输出门卡总表报告（B8 起 130 行）；B1（run `42022736600` / `dcea585`）、B2（run `42035381476` / `1066b0d`）、B3（run `42040769639` / `378432a`，十步全 `success`）、B4（run `42224901915` / `8ed8b5b`，十一步全 `success`）、B5（run `42232312609` / `aa63769`，十二步全 `success`）、B6（run `42343839222` / `c0529da`，十三步全 `success`）与 B7（run `42357787519` / `266ca65`，十四步全 `success`）均已通过；B8（run `42361122709` / `97fd31d`，十五步全 `success`）亦已通过，含 `check-release-specs`、71 组 runner 与 12 文件 App 库编译；本机只跑 Python 静态门卡，逐批结论见 `docs/run-ledger.md` |
| 布局方案 | 规格 v1.2 的 1/2/3/4 四档；用户已拍板**保持四档**，自定义二叉分割树方案不采纳，规格一字未改（见 `docs/adr/ADR-v2-008.md`；撤回的改版见 `ADR-v2-005.md`） |
| 文件操作能力 | **仍不能搬运文件**：R2 起 `IFileOperation` 托管 COM 声明与 `IShellTransport` 真实实现已编译进 exe（SDK 逐字对应，见 `docs/adr/ADR-v2-012.md`）；R3 起工具栏「复制 / 移动 / 删除」已把请求经 `OperationCoordinator` 接到该 transport（见 `ADR-v2-013.md`），但 transport 默认关闭、命令返回「未执行 / 未验证」，CI 只编译不运行，真实 shell 行为标「未验证」；§5.2「成熟绑定」的偏差已如实登记；R4 起系统原生菜单宿主按同一纪律默认关闭，右键只回「菜单未执行 / 未验证」（见 `ADR-v2-014.md` D5）；R5 起图标 / 缩略图宿主按同一纪律默认关闭，列表项只显示文字占位（见 `ADR-v2-015.md` D6） |
| 可下载版本 | 无。R1 起 CI 用系统 C# 编译器把四层源码一次编成一个 `QuadLite.exe`（零 NuGet、零合并工具、PerMonitorV2 manifest），并实跑产品 exe 的 `--selftest`；R5 起四层共 107 个 .cs，窗口已有四档真实落位 / 目录枚举 / 列表控件 / 搬运命令，Fs 层已接上按 §5.2 标准四步实现的系统原生菜单宿主与 §5.11 的 `IShellItemImageFactory` 图标宿主（都默认关闭），但 CI 不开窗口、不枚举目录、不实例化 COM、不解码、不写 thumbcache，这些运行期行为全部未验证；拖放、七档视图与筛选、单实例后两段、崩溃哨兵与自绘兜底、LRU 介质与三段取证仍未实现 / 未验证；MSBuild 出包路径、真实 shell 行为与 H1–H6 / C4 取证也未验证。既有 CI 仍编译并运行单测 runner、把 Fs 适配器与 App 状态内核分别编成 library（不运行） |

规格中的能力描述是**首版的设计目标与验收判据**，不是已发布功能的宣称。里程碑与 MVP 定义见 `design-v2.md` §8.4。

## B0 工程骨架与检查链

B0 只交付骨架与可机器验证的闸门，不含产品功能代码：

| 路径 | 内容 |
|---|---|
| `Directory.Build.props` / `src/*.csproj` | net48 + WPF + C# 5 语法子集，SDK 风格工程文件，`FrameworkPathOverride` 参数化 |
| `build.ps1` | 构建与出包入口（B0 为骨架；R1 起改为真实单文件编译链，由 CI `real-exe-build` 实跑产出 `QuadLite.exe` + `BUILD.txt`，`real-exe-selftest` 实跑产品 `--selftest`；MSBuild 出包路径仍标「未验证」） |
| `tools/` | 零第三方依赖的 Python 3 检查脚本（B0 三个 + B1 `check-core-specs` + B2 `check-fs-specs` + B3 `check-shell-specs` + B4 `check-list-specs` + B5 `check-menu-specs` + B6 `check-icon-specs` + B7 `check-selftest-specs` + B8 `check-release-specs` + R1 `check-realization-specs` + R2 `check-transport-specs` + R3 `check-ui-specs` + R4 `check-menu-host-specs` + R5 `check-icon-thumb-specs`，共 16 个 `check_*.py`）+ 正 / 负样本夹具 |
| `.github/workflows/ci.yml` | 需求追踪 / 静态边界 / 静态契约（Core、Fs、外壳、列表、菜单、图标缩略图、自测出包、发布线）/ 检查链自测 / 开源合规 / C# runner（B7 起同时编译 App 状态内核）/ Fs 与 App 库编译；B0–B6 首次实跑均已通过（B6 为 run `42343839222` / head `c0529da`，十三步全 `success`）；B7（run `42357787519` / head `266ca65`，十四步全 `success`）首次实跑亦已通过；B8（run `42361122709` / head `97fd31d`，十五步全 `success`）首次实跑亦已通过，`check-release-specs`、71 组 runner 与 12 文件 App 库编译均 `success`；R1 起再加 `check-realization-specs`、`real-exe-build` 与 `real-exe-selftest` 三步，R2 起再加 `check-transport-specs`，R3 起再加 `check-ui-specs`（run `43717485240` / head `0836109`，二十个门卡 / 编译步骤全 `success`），R4 起再加 `check-menu-host-specs`（run `43874542767` / head `fd11116`，二十一个门卡 / 编译步骤全 `success`），R5 起再加 `check-icon-thumb-specs`（共二十二个门卡 / 编译步骤，push 后的 CI 结论待回填）；**R1 首次实跑已通过**：run `42376298215`（push head `a84b646`），十八个门卡 / 编译步骤逐项 `success`，含真实单 exe 编译与产品 `--selftest`（≥100 行 / `verdict: GREEN` / 退出码 0 / `BUILD.txt` 七字段） |
| `docs/adr/` `docs/negative-controls.md` `docs/run-ledger.md` | 设计决策记录、负向对照登记、运行账本 |

本地复核（无需安装任何依赖）：

```
python tools/check_trace.py
python tools/check_boundaries.py
python tools/check_core_specs.py
python tools/check_fs_specs.py
python tools/check_shell_specs.py
python tools/check_list_specs.py
python tools/check_menu_specs.py
python tools/check_icon_specs.py
python tools/check_selftest_specs.py
python tools/check_release_specs.py
python tools/check_realization_specs.py
python tools/check_transport_specs.py
python tools/check_ui_specs.py
python tools/check_menu_host_specs.py
python tools/check_icon_thumb_specs.py
python tools/run_tests.py
python tools/check_open_source.py
```

`run_tests.py` 跑正样本、145 条负向对照（故意破坏后门卡必须变红，含 13 条 R1 真实宿主契约、12 条 R2 托管 COM 契约、15 条 R3 浏览 / 搬运 UI 契约、19 条 R4 原生菜单宿主契约与 23 条 R5 图标缩略图契约）与合规扫描，共 162 组，末行输出 `verdict:`，退出码 = 失败数。B0 的四个 Python 门卡首次实跑已通过（run `42013589562`，head `96a5fa4`，结论 `success`）；B1 起 CI 另加 `check-core-specs` 与 C# runner 编译运行，B2 起再加 `check-fs-specs` 与 Fs 适配器库编译，B3 起再加 `check-shell-specs` 与 App 外壳状态内核库编译，B4 起再加 `check-list-specs`，B5 起再加 `check-menu-specs`，B6 起再加 `check-icon-specs`（本机与 CI 均实测绿，run `42343839222` / head `c0529da`），B7 起再加 `check-selftest-specs`（本机与 CI 均实测绿，run `42357787519` / head `266ca65`），B8 起再加 `check-release-specs`（本机与 CI 均实测绿，run `42361122709` / head `97fd31d`）。R1 起真实单文件 exe 的编译与产品 exe 形态 `--selftest` 已由 CI 实跑（见下节），R2 起托管 COM 绑定与 transport 已编入 Fs 层并由 `check-transport-specs` 静态判定；R3 起 Host 窗口的四档落位 / 目录枚举 / 列表控件 / 搬运命令由 `check-ui-specs` 静态判定并编进单 exe；R4 起 SDK 逐字对应的菜单 COM 声明、标准四步、owner 窗口五类消息转发、参数硬校验、目标快照与 try-finally 释放由 `check-menu-host-specs` 静态判定并编进同一个 exe；R5 起第三套 COM 声明（`IShellItemImageFactory` 一槽 vtable）、`SIIGBF` 十个值、四级通道 / WIC 白名单 / EXIF / LRU 双闸 / 负缓存 / 8 秒看门狗 / 唯一写入例外、独立 STA 与 `generation` 丢弃由 `check-icon-thumb-specs` 静态判定并编进同一个 exe，但 CI 不开窗口、不枚举目录、不实例化 COM、不解码、不写 thumbcache，运行期完全未验证；拖放、七档视图与筛选、崩溃哨兵与自绘兜底、LRU 介质与三段取证、MSBuild 出包路径与 H1–H6 / C4 取证仍未实现或未验证，见 `docs/adr/ADR-v2-011.md`、`ADR-v2-012.md`、`ADR-v2-013.md`、`ADR-v2-014.md` 与 `ADR-v2-015.md`。

## B1 L0 纯函数层

`src/QuadLite.Core/` 的 13 个类型全部是纯函数、零 I/O，按 `design-v2.md` §5 的行为契约落地：路径规范化与 SafeWalk 四道边界、列宽恒等式、七档视图、自然排序、危险判级矩阵、自有菜单项的源头消重与 ID 段、操作请求 / 结构化结果、筛后选中集、状态与错误模型。

`tests/QuadLite.Tests/` 是零第三方依赖的 C# runner：14 组门卡逐行输出 `ok=True/False`，末行 `verdict:`，退出码 = 红条数。CI 在 Windows runner 上用系统 .NET Framework 编译器以 `/langversion:5` 编译并运行它，把「C# 5 语法子集」也变成机器可判的约束；首次实跑已通过（run `42022736600`，14 组全 `ok=True`）。本机不执行 C# 编译器，这一层如实标「未验证（CI 远端实测）」，见 `docs/adr/ADR-v2-002.md`。

本机可复核的静态契约（零第三方依赖、无需编译器）：

```
python tools/check_core_specs.py
python tools/run_tests.py
```

`check_core_specs.py` 核对类型齐备、C# 5 语法子集、枚举完备性、runner 门卡组注册与 Core 零 I/O；`run_tests.py` 含 4 类针对它的负向对照，故意破坏必须变红。

## B2 编排与适配器层

`src/QuadLite.Core/` 新增 10 个零 I/O 类型，落地 `design-v2.md` §5.1 / §5.3 / §5.4 / §5.12 的编排内核：端口契约、`OperationCoordinator`（串行闸门 → 判级 → 预检 → 端口执行 → 结构化结果 → 撤销入栈 → 一次刷新广播）、20 步跨窗格撤销栈、副本命名与冲突策略、两段式自操作抑制、单一订阅去抖、剪贴板数据模型、命令行解析、便携配置根、结果分类。全部跑在注入的 Fake 端口上，不依赖真实 shell。

`src/QuadLite.Fs/` 有 5 个适配器：原子写、单一 watcher、目录分批枚举、剪贴板编解码、搬运端口实现。**真实 `IFileOperation` 调用点隔离在 `IShellTransport`**：§5.2 禁止手写 COM vtable，成熟绑定尚未选定，所以本批不声明接口布局、不调用真实 shell；CI 只把 Fs 层编译成 library 而不运行。因此 B2 交付的是「编排可验证 + 端口契约冻结」，**不等于能搬文件**。偏差与未验证项完整登记在 `docs/adr/ADR-v2-004.md`。

本机可复核的静态契约：

```
python tools/check_fs_specs.py
python tools/run_tests.py
```

`check_fs_specs.py` 核对适配器齐备、端口绑定、禁止手写 COM、shell 调用点隔离与 C# 5 语法子集；`run_tests.py` 含 4 类 B2 负向对照（删编排类型 / 删门卡组 / 删适配器 / 注入手写 COM），逐条实测变红。CI 首次实跑已通过（run `42035381476`，八个步骤全 `success`）：21 组 C# 门卡全 `ok=True`，Fs 适配器库编译成功。本机不执行 C# 编译器，该层只记 CI 远端实测。

## B3 外壳状态内核

B3 按 `design-v2.md` §8.1 的既定范围推进，**布局按 v1.2 的 1/2/3/4 四档实现**；用户已拍板保持规格 v1.2 的四档，自定义分割树方案不采纳，规格一字未改（见 `docs/adr/ADR-v2-008.md`）。

`src/QuadLite.Core/` 新增 9 个零 I/O 类型：四档槽位与跨度（含 3 窗格 `RowSpan=3`）、分隔条相对均分偏移 / 折叠 0/1 / 24px 吸附 / 折前恢复、窗格状态容器、标签组改序不改入参、导航前进后退与清筛选、地址栏输入规范化、书签三态与坏项隔离、会话坏值归一与 `future-version` 拒读、此电脑层条目编排。

`src/QuadLite.App/` 首次有代码：`ShellState` / `ShellCommands` 只读 Core 结果装配外壳状态，**不含窗口、XAML、消息循环与事件绑定**；CI 把它们与 Core 一起编成 library（只编译、不运行）。因此 B3 交付的是「外壳的可判定内核 + 可编译骨架」，**不等于应用可启动**，也不等于 MVP 可交付。

本机可复核的静态契约：

```
python tools/check_shell_specs.py
python tools/run_tests.py
```

`check_shell_specs.py` 核对 B3 类型齐备、App 骨架齐备、门卡组注册、规格与实现一致（防止擅自改动布局方案）、四档枚举与 3 窗格跨度、折叠 / 吸附常量、`future-version` 判据、App 层零 I/O 与 C# 5 语法；`run_tests.py` 含 6 类 B3 负向对照，逐条实测变红。C# runner 扩到 27 组，CI run `42040769639`（head `378432a`）十步全 `success`：27 组 `ok=True`、`verdict: GREEN`，`csharp-app-compile` 库编译通过（不运行）。首个功能提交 `15d0aa9` 的 `csharp-runner` 因一处断言期望值笔误判红，修正提交 `378432a` 后复跑全绿，过程见 `docs/adr/ADR-v2-005.md`。

## B4 列表视图规则层

B4 按 `design-v2.md` §8.1 的既定范围推进，**规格一字未改**：FR-09..17 与十个门卡组在 S3 就已在附录 C 登记，`check-trace` 天然绿。B1 已交付列表视图的 L0 骨架（七档视图、列宽恒等式、自然排序、Ctrl+A 排除三类），B4 不重复定义，只在其上补齐规格里尚未可判定的规则。

`src/QuadLite.Core/` 新增 11 个零 I/O 类型：4096 / 批与迟到批次丢弃、图标档不渲染组头、列头点击切换升降序与组头不进显示行集、列宽拖宽 / 双击复位 / 偏好按标签、四列筛选（同列「或」跨列「与」、导航清 F5 留、不写会话、状态栏 / Ctrl+A / 拖放源 / 操作源四来源一律读显示行集）、四显示开关 16 组合、点选 / Ctrl / Shift 多选与框选、四类命中单一来源、状态栏结构化文案、13 键快捷键表。

`src/QuadLite.App/` 新增 `ListViewState` / `ListViewCommands`：只读 Core 结果装配列表视图状态与快捷键派发，**不含窗口、XAML、列表控件、虚拟化面板、拖放与事件绑定**；CI 把它们与 Core 一起编成 library（只编译、不运行）。因此 B4 交付的是「列表视图的可判定内核 + 可编译骨架」，**不等于应用可启动**，也不等于 MVP 可交付。偏差与未验证项完整登记在 `docs/adr/ADR-v2-006.md`：真实目录枚举（4096 / 批）、列头筛选 UI、拼音分桶与动态类型枚举、真实拖放与框选、图标与缩略图渲染均未实现。

本机可复核的静态契约：

```
python tools/check_list_specs.py
python tools/run_tests.py
```

`check_list_specs.py` 核对 B4 类型齐备、App 状态内核齐备、门卡组注册、规格关键常量（4096 / 四列 / 五档 / 13 键）与不变量、规格与实现一致、App 层零 I/O 与 C# 5 语法；`run_tests.py` 含 6 类 B4 负向对照，逐条实测变红。C# runner 扩到 33 组（6 个新增组 + 5 个既有组补强断言），CI run `42224901915`（head `8ed8b5b`）十一步全 `success`：33 组 `ok=True`、`verdict: GREEN`，`check-list-specs` 与 `csharp-app-compile` 均 `success`。布局已拍板保持 v1.2 四档；B4 只关心单窗格内的行集与交互，不受窗格数量与落位模型影响（见 `ADR-v2-005` D7）。

## B5 系统原生右键菜单规则层

B5 按 `design-v2.md` §8.1 的既定范围推进，**规格一字未改**：§7.1 的 `menu-*` 十条与 `FakeShellExtension` 四态在 S3 就已在附录 C 登记，`check-trace` 天然绿。v1 的崩溃根因是自造融合层逐项读 verb；B5 的默认路径是系统原生菜单四步（`GetUIObjectOf` → `QueryContextMenu` → `TrackPopupMenuEx` → `InvokeCommand`），零 verb 查询、零删项、零菜单树改写。

`src/QuadLite.Core/` 新增 8 个零 I/O 类型：宿主默认路径与禁止项、`InvokeCommand` 出参硬校验与自有段派发分流、源头消重（等价命令直接用系统项，不等价命令改名独立分组）、`IContextMenu2/3` 消息转发、打开右键那一刻的不可变 `MenuTarget` 快照、崩溃哨兵（ctxKey 降级 / 7 天 TTL / 一键重试 / 再崩翻倍）、HMENU 与 COM 的 try-finally 释放、假 shell 扩展四态。

有一处语义纠偏：B1 的 `MenuModel.IsValidInvokeId` 是合并系统段与自有段的 L0 通用判据，而 §5.2 的 `InvokeCommand` 范围只约束系统段，自有段 ID ≥ `0x8000` 走应用内派发。B5 **不改 B1**，另建 `MenuInvokeRules.InvokeIdInRange()` 只判系统段，`menu-own-id-segment` 门卡组对此有专条断言（见 `docs/adr/ADR-v2-007.md` D3）。

`src/QuadLite.App/` 新增 `MenuState` / `MenuCommands`：只读 Core 结果装配宿主模式、会话态、ctxKey、safeListed、pending、TTL、maxId 与派发结果，**不含窗口、XAML、COM 声明、消息循环与事件绑定**。因此 B5 交付的是「原生右键菜单的可判定内核 + 可编译骨架」，**不等于原生菜单已可用，也不宣称不再崩溃**。真实 `NativeMenuHost` / `IContextMenu` COM 绑定、`data\menu-pending.json` 磁盘标记、自绘兜底 UI 与一键重试按钮均未实现，完整偏差见 `docs/adr/ADR-v2-007.md` D6。

本机可复核的静态契约：

```
python tools/check_menu_specs.py
python tools/run_tests.py
```

`check_menu_specs.py` 核对 B5 Core 类型齐备、App 状态内核齐备、门卡组注册、22 项规格常量与不变量、产品程序集 verb 查询 0 次（72 个 .cs 实测）、App 分层边界、`FakeShellExtension` 四态、规格与实现一致、C# 5 语法；`run_tests.py` 含 7 类 B5 负向对照，逐条实测变红。C# runner 扩到 43 组，CI run `42232312609`（head `aa63769`）十二步全 `success`：43 组 `ok=True`、`verdict: GREEN`，`check-menu-specs` 与 `csharp-app-compile` 均 `success`。布局已拍板保持 v1.2 四档；B5 只关心一次右键会话，不受窗格数量与落位模型影响（见 `ADR-v2-005` D7）。

## B6 图标 / 缩略图规则层

B6 按 `design-v2.md` §8.1 的既定范围推进，**规格一字未改**：FR-29..31 与 `icon-*` / `thumb-*` / `thumb-nowrite` 三个门卡组在 S3 就已在附录 C 登记，`check-trace` 天然绿。开工前用户明确拍板**保持 v1.2 四档布局**，自定义二叉分割树方案不采纳；B6 只关心单窗格内的图标档位与行集渲染，不受窗格数量与落位模型影响（见 `docs/adr/ADR-v2-008.md` D1）。

B1 / B4 已交付的 L0 骨架直接复用，不重复定义：七档视图的 16 / 48 / 96 / 256 图标像素取自 `ViewSpecs`，可见区间单位防混用取自 `CellMetrics.VisibleSpan`，作业状态机取自 `StateMachine.CanTransition(JobState)`，迟到批次丢弃取自 `NavRules` / `EnumBatchRules`，缩略图开关不重枚举取自 `ToggleRules.RequiresReenumeration`。

`src/QuadLite.Core/` 新增 8 个零 I/O 类型：四级图标通道与 exe 两段式 / jumbo 透明画布回落 48 / 盘符逐档取图、WIC 内置解码器 14 种白名单与 `.ts` 1 MB 下限、EXIF 八值方向、可见优先单线程队列与 generation 丢弃、缓存键四段与 `i|` / `v|` 隔离 + 96 MB / 1500 条双闸 + 负缓存不重试、15 种视频容器与 8 秒看门狗、开关切换不重枚举、唯一显式写入例外与三段取证判据。

`src/QuadLite.App/` 新增 `ThumbState` / `ThumbCommands`：只读 Core 结果装配尺寸档、作业状态、失败分类、缓存双闸、generation、写入许可与结构化提示，**不含窗口、XAML、列表控件、WIC / Shell 绑定与事件绑定**。因此 B6 交付的是「图标缩略图的可判定内核 + 可编译骨架」，**不等于图标与缩略图已渲染**。真实 WIC 解码、`IShellItemImageFactory` 调用、后台 worker、LRU 介质、盘符逐像素比对、三段取证与缩略图控件均未实现，完整偏差见 `docs/adr/ADR-v2-008.md` D6。

本机可复核的静态契约：

```
python tools/check_icon_specs.py
python tools/run_tests.py
```

`check_icon_specs.py` 核对 B6 Core 类型齐备、App 状态内核齐备、门卡组注册、26 项规格常量与不变量、WIC 14 种 / 视频 15 种数量、唯一写入例外（源码判据 + 文档清单各一条）、App 分层边界、产品程序集零手写 COM、规格仍 v1.2、C# 5 语法；`run_tests.py` 含 7 类 B6 负向对照，逐条实测变红。C# runner 扩到 51 组（新增 8 组，均落在附录 C 已登记的三个门卡组内），`run_tests.py` 扩到 55 组 / 45 条负向对照，本机实测绿；**CI 首次实跑已通过**：run `42343839222`（head `c0529da`），`check-icon-specs`、51 组 `csharp-runner` 与 8 个 App 状态内核库编译三步均 `success`，`csharp-runner` 退出码 0 ⇒ 51 组 `ok=True`、`verdict: GREEN`。本机不执行 C# 编译器，该层以 CI 远端实测为准；日志端点匿名 403，未读取日志原文。

## B7 自测装置 / 门卡总表 / 出包自检规则层

B7 按 §8.1 既定范围推进，**规格一字未改**，布局沿用 v1.2 四档（见 `docs/adr/ADR-v2-009.md` D1）。Core 新增 9 个零 I/O 规则类型：§6.4 命令行契约（14 个已知参数）、`--selftest` 报告形态（≥100 行 / 末行 `verdict:` / 退出码 = 红条数）、门卡总表（§7.1 的 50 项）、账本自检（9 列 / 7 位 `head_sha` / 四级证据等级）、`BUILD.txt` 七字段与脏树 Warning、单文件出包自检、四项性能预算表（七字段、只报数不判定）、崩溃摘要脱敏五字段、单实例三段机制与 `QL` 标记。App 新增 `SelftestState` / `PackageState` 只读装配。

**本批修订了 B2 的一处真实漂移**：`CliArgs.Parse` 原先只认 4 个参数、且 `--shot` 未豁免单实例；现改为复用 `CliContractRules` 与 `SingleInstanceRules`，补齐 §6.4 主表九项及修饰项，B2 的 `cli-*` 断言一条未删。详见 ADR-009 D3。

C# runner 51 → **61 组**，并真实输出 120 行报告（门卡总表 50 + 预算表 4 + 摘要 3 + 组结果 61 + 尾部 2），满足 §8.1 B7 的「≥100 行」判据；CI 的 `csharp-runner` 自本批起同时编译 App 状态内核，`csharp-app-compile` 最小文件数 8 → 10。新增 `tools/check_selftest_specs.py`（10 项检查、29 项规格常量），其中第 5 项**解析 §7.1 原文逐项比对**门卡总表，不是只比一个数字；`run_tests.py` 扩到 64 组 / 53 条负向对照，8 条 B7 负向逐条实测变红。

**偏差照实登记**：B7 交付的仍是「可判定内核 + runner 报告形态」，真实产品 exe 的 `--selftest`、`BUILD.txt` 采集、单文件出包、`--bench` 采样、崩溃日志写入与零写入三段取证均未实现或未验证；§8.4 的 M2 判据未达成。**CI 首次实跑已通过**：run `42357787519`（head `266ca65`，含 B7 功能提交 `864eff1` 与账本回填提交 `266ca65`），十四个门卡 / 编译步骤逐项 `success`，其中 `check-selftest-specs` `success`、`csharp-runner` 退出码 0 ⇒ 61 组 `ok=True`、`verdict: GREEN`，`csharp-app-compile`（10 个 .cs）编译成功且不运行。本机不执行 C# 编译器，该层以 CI 远端实测为准；日志端点匿名 403，未读取日志原文。

## B8 发布线 C4 + H1–H6 规则层

B8 按 §8.1 / §8.3 既定范围推进，**规格一字未改**，布局沿用 v1.2 四档（见 `docs/adr/ADR-v2-010.md` D1）。Core 新增 10 个零 I/O 规则类型：§8.3 七节点依赖链（C4 → H1 → H3 → H2 → H4 → H5 → H6，注意 H3 在 H2 之前）与降级链、证据等级纪律、H1 高 DPI 四档矩阵、H3 零写入三段取证、H2 兼容矩阵 2×4 八格、H4 崩溃摘要脱敏与 WER 读法、H5 性能回归七列、H6 OV 签名三步与指纹入库、C4 零误报门槛、M3 发布记录判定。App 新增 `ReleaseState` / `ReleaseRecordState` 只读装配。B7 的四级证据等级、预算表、崩溃摘要五字段、三段取证计数一律复用，不另立第二套表。

C# runner 61 → **71 组**，报告 120 → **130 行**；`csharp-app-compile` 最小文件数 10 → 12。新增 `tools/check_release_specs.py`（10 项检查、38 项规格常量），其中第 5 项**解析 §8.3 原文**逐项比对依赖链七节点、DPI 四档与降级链（含顺序），不是只比一个数字；`run_tests.py` 扩到 75 组 / 63 条负向对照，10 条 B8 负向逐条实测变红，含「把 C4 判定改成无条件 `return true`」与「把 §8.3 的 `└─ H3` 改成 `└─ H2`」两类针对证据纪律和规格漂移的对照。

**偏差照实登记**：§8.4 的 M3 判据是「H1–H6 逐项有证据；C4 缺位显式写『未做』」，而本批交付的是发布线的**可判定契约**，六阶段真实取证、C4 带杀软 VM、OV 签名与发布记录一项都没有。`ReleaseState.CurrentM3Verdict()` 明确返回 `EvidenceMissing`、`IsPublishable() == false`——**M3 不成立**，不把静态门卡全绿写成可发布。B8 时点真实产品 exe 与 WPF 应用仍未接入，详见 ADR-010 D6（R1 已补上最小宿主，见下节）。**CI 首次实跑已通过**：run `42361122709`（head `97fd31d`，含 B8 功能提交 `896a446` 与账本回填提交 `97fd31d`），十五个门卡 / 编译步骤逐项 `success`，其中 `check-release-specs` `success`、`csharp-runner` 退出码 0 ⇒ 71 组 `ok=True`、`verdict: GREEN`、`csharp-app-compile`（12 个 .cs）编译成功且不运行。本机不执行 C# 编译器，该层以 CI 远端实测为准；日志端点匿名 403，未读取日志原文。

## R1 真实实现第一批（规格外实现批次）

B0–B8 全部是规则层与状态内核；用户拍板「开始接真实实现」后启用 **R 系列**（Realization，规格 `design-v2.md` v1.2 一字未改、不写进 §8.1 批次表）。R1 只解决「可运行载体」：

| 项 | 交付 |
|---|---|
| `src/QuadLite.Host/` | R1 交付时的 `Program.cs`（STAThread 入口 / `CliArgs` 分派 / 单实例 Mutex 第一段 / `Application.Run`）、`MainWindow.cs`（纯 C# 最小 WPF 窗口，四档槽位取自 `LayoutMath`，状态栏逐项如实写未实现）、`SelftestRunner.cs`（产品 `--selftest` 复用 B7/B8 状态内核 + 10 组运行时断言）、csproj + PerMonitorV2 manifest；`MainWindow.cs` 已在 R3 重写为四档真实落位 + 列表控件 + 工具栏 |
| `build.ps1` | 从 B0 骨架改为真实单文件构建：系统 C# 编译器以 `/target:winexe /langversion:5` 把四层源码一次编成 `QuadLite.exe`（R1 时 98 个 .cs，R3 时 103 个，R4 时 105 个），显式引用 WPF 四个程序集，采集 `BUILD.txt` 七字段（真实大小 / SHA256 / 脏树 Warning） |
| CI 三步 | `check-realization-specs`（10 项静态契约）、`real-exe-build`（真编译 + 七字段校验）、`real-exe-selftest`（运行 exe `--selftest`，断言 ≥100 行 / 末行 `verdict: GREEN` / 退出码 0） |
| 门卡与负向对照 | `tools/check_realization_specs.py`；`run_tests.py` 在 R1 时扩到 89 组 / 76 条负向（13 条 R1 对照逐条实测变红） |

**偏差照实登记**：R1 交付的是「能编译、能启动、能自测的真实 exe 骨架」，不是 MVP。R1 交付时窗口没有目录枚举与列表控件，不能浏览、不能搬运、不能弹系统原生菜单、不能渲染图标；`IShellTransport` 仍无真实实现，单实例只落地 Mutex 第一段；`--shot` / `--bench` / `--dumpmenu` 诊断装置只输出「未实现 / 未验证」；MSBuild 出包路径、真实 shell 行为、H1–H6 与 C4 全部未验证。本机不执行 C# 编译器，真实编译与 `--selftest` 结论以 CI 远端实测为准。`ReleaseState.IsPublishable() == false`，**M1 / M2 / M3 均不成立**，完整偏差见 `docs/adr/ADR-v2-011.md`。

**CI 首次实跑已通过**：run `42376298215`（push head `a84b646`），十八个门卡 / 编译步骤逐项 `success`，其中 `check-realization-specs` / `real-exe-build` / `real-exe-selftest` 三步全绿 ⇒ 真实单 exe 编译成功、`BUILD.txt` 七字段齐、产品 `--selftest` 报告 ≥100 行且 `verdict: GREEN`、退出码 0。本机不执行 C# 编译器，该结论只记 CI 远端实测；日志端点未读取原文。

## R2 真实 `IFileOperation` 托管 COM 绑定（规格外实现批次）

R2 只解决「真实搬运的代码路径」：`src/QuadLite.Fs/ShellComInterop.cs`（唯一允许 COM 声明的白名单文件，四个 GUID 与 21 / 16 / 5 段 vtable 顺序取自本机 Windows SDK 10.0.19041.0 头文件）与 `ShellTransport.cs`（`IShellTransport` 的真实实现：独立 STA、每次新建实例、flags 实例级、进度回调回传 `OpItem`、try-finally 释放 RCW、默认关闭）。`tools/com_policy.py` 单点定义分层口径，`tools/check_transport_specs.py` 做静态判定，`run_tests.py` 扩到 102 组 / 88 条负向对照，CI 新增 `check-transport-specs` 一步。

**偏差照实登记**：§5.2 要求「COM 声明使用成熟绑定，不手写 vtable」，零 NuGet + net48 下 .NET Framework 没有公开托管的 `IFileOperation` 绑定，本批改用 SDK 逐字对应的托管声明，这是明确偏差，规格一字未改。CI 只编译、不实例化 COM、不调用 `PerformOperations`；真实 shell 行为、回收站真实路径、STA 线程 Join 超时与崩溃哨兵全部「未验证」。R2 交付时窗口仍无浏览 / 搬运 UI，transport 默认关闭，**M1 / M2 / M3 均不成立**，完整偏差见 `docs/adr/ADR-v2-012.md`。

**CI 首次实跑已通过**：run `43713387488`（push head `bbbe0e5`），十九个门卡 / 编译步骤逐项 `success`，其中 `check-transport-specs`、`csharp-fs-compile`、`real-exe-build`、`real-exe-selftest` 全绿 ⇒ 托管 COM 声明编译通过、四层 100 个 .cs 一次编成单 exe、产品 `--selftest` 报告 ≥100 行且 `verdict: GREEN`、退出码 0。本机不执行 C# 编译器，该结论只记 CI 远端实测；CI 不实例化 COM、不调用 `PerformOperations`。

## R3 浏览 / 搬运 UI 真实接线（规格外实现批次）

R3 把 R2 的 transport 第一次挂到用户可触发的操作上，范围按 `ADR-013` D1 收敛为「浏览 + 搬运」：

| 项 | 交付 |
|---|---|
| 四档真实落位 | `MainWindow` 用 `LayoutMath.Slots` 把四个 `PaneView` 放进 3×3 `Grid`，按 `LayoutMath.IsPaneVisible` 切可见性；布局切换不销毁窗格对象 |
| 目录枚举 | `PaneView` 经 `IDirSource`（Fs 层 `DirSource`）在后台 worker 枚举，`Dispatcher` 回 UI 线程，`NavRules.NextGeneration` 后在 worker 与 UI 两侧各丢一次迟到批次 |
| 列表与状态栏 | 真实 WPF `ListBox` 呈现条目，双击导航、地址栏走 `AddressRules.TryParse`，选中集与行数走 `Selection` / `StatusRules` |
| 搬运命令 | 工具栏「复制 / 移动 / 删除」→ `OpRequest`（不可变快照）→ `OperationCoordinator` → `ShellFileOperator` → `ShellTransport`；状态文案只从结构化结果经 `StatusRules.ResultText` 产出 |
| 注入端口 | `HostPolicies.cs` 提供零 I/O 的 `IClock` / `IDangerPolicy` / `IPreflight` / `IRefreshSink` 与 `UiContractRules` 判据（运行期恒为「未验证」） |
| 门卡与负向对照 | `tools/check_ui_specs.py`（12 组静态判据）；`run_tests.py` 扩到 118 组 / 103 条负向对照（15 条 R3 逐条实测变红）；CI 新增 `check-ui-specs` 一步；`build.ps1` 源码计数 100 → 103；产品 `--selftest` 增 6 组 R3 断言 |

**偏差照实登记**：transport 仍默认 `Enabled = false`，R3 **不提供 UI 启用入口**；关闭时命令明确返回「搬运未执行 / 未验证」，绝不把「没有异常」写成成功。CI 与本机都不打开窗口、不枚举目录、不实例化 COM，真实键鼠交互、异步枚举时序、四档实际渲染、真实 `IFileOperation` 行为全部「未验证」。R3 时点系统原生菜单 COM 宿主仍未实现（R4 已接线但默认关闭，见下节）；拖放、图标与缩略图渲染、七档视图与筛选、会话与书签、单实例后两段、崩溃哨兵、MSBuild 出包路径与 H1–H6 / C4 取证仍未实现或未验证。`ReleaseState.IsPublishable() == false`，**M1 / M2 / M3 均不成立**，完整偏差见 `docs/adr/ADR-v2-013.md`。

**CI 首次实跑判红并已修复**：功能提交 `d8ba545` 的 run `43716871530` 在 `real-exe-build` 判红（`real-exe-selftest` 跳过），根因是产品 `--selftest` 里三个 `out` 变量放在 `&&` 短路链后直接使用，触发 C# 5 的 CS0165；Python 门卡与 Fs / App 库编译均 `success`，因为都不编 Host 层。修复提交 `0836109` 显式初始化变量，并让 CI 编译步骤把 `error CS` 行以 annotation 暴露，复跑 run `43717485240`（head `0836109`）结论 `success`，二十个门卡 / 编译步骤逐项 `success`，其中 `check-ui-specs` / `real-exe-build` / `real-exe-selftest` 全绿 ⇒ 四层 103 个 .cs 一次编成单 exe、`BUILD.txt` 七字段齐、产品 `--selftest` 报告 ≥100 行且 `verdict: GREEN`、退出码 0（含 6 组 R3 契约断言）。首次判红不作为通过证据。本机不执行 C# 编译器，真实编译与 `--selftest` 结论只记 CI 远端实测；CI 不开窗口、不枚举目录、不实例化 COM。

后续顺序（拆分理由见 ADR-013 D1）：**R4** 系统原生菜单 COM 宿主、**R5** 图标与缩略图取图链路（均见下两节，已接线但默认关闭）。顺序较 ADR-011 D2 的口头提议进一步细分，先做载体、再做彼此正交的 COM 绑定；R5 实测把「WIC 手写 COM」收敛为 WPF 内置系统包装（见 `ADR-015` D2）。

## R4 系统原生菜单 COM 宿主（规格外实现批次）

R4 按 `ADR-014` D1 只做「系统原生右键菜单的真实代码路径」，规格 `design-v2.md` v1.2 一字未改、四档不变：

| 项 | 交付 |
|---|---|
| 第二套 COM 声明 | `src/QuadLite.Fs/ShellComInterop.cs` 扩为唯一白名单，新增 `IContextMenu` / `IContextMenu2` / `IContextMenu3` / `IShellFolder` 的 SDK 10.0.19041.0 权威 GUID 与 3 / 4 / 5 / 10 段 vtable 顺序，以及 `CreatePopupMenu` / `TrackPopupMenuEx` 等 Win32 入口 |
| 菜单宿主 | `src/QuadLite.Fs/NativeMenuHost.cs`：独立 STA 线程执行标准四步（`GetUIObjectOf` → `QueryContextMenu` → `TrackPopupMenuEx` → `InvokeCommand`），自建 owner 窗口转发五类菜单消息（优先 `HandleMenuMsg2`），`try-finally` 先释放 HMENU 再释放 COM / PIDL |
| Host 右键接线 | `MenuCommands.cs` + `PaneView.MouseRightButtonUp` + `MainWindow.SetMenuSink`：把打开右键那一刻的不可变 `MenuTarget` 快照交给宿主，结果只经 `StatusRules.ResultText` 产出状态栏文案 |
| 判据与口径 | `tools/com_policy.py` 收口 verb 判据（白名单允许 SDK vtable 声明、任何调用形态判红）；`tools/check_menu_host_specs.py` 16 组静态判据；CI 新增 `check-menu-host-specs` 一步（共二十一个门卡 / 编译步骤）；产品 `--selftest` 增 8 组 R4 契约断言 |
| 门卡与负向对照 | `run_tests.py` 扩到 138 组 / 122 条负向对照（19 条 R4 逐条实测变红）；`build.ps1` 源码计数 103 → 105；`QuadLite.Host.csproj` 纳入 `MenuCommands.cs` |

**偏差照实登记**：`NativeMenuHost.Enabled` 默认 `false`，Host 层不提供启用入口，右键只回「菜单未执行：原生菜单宿主默认关闭（未启用 / 未验证）」——这是与规格 §5.2「默认路径 = 系统原生菜单」的**明确偏差**（`ADR-014` D5），理由：L3 崩溃哨兵与自绘兜底 UI 未实现，第三方 shell 扩展在标准序列中异常会直接带走主进程；H1–H6 真机取证与 C4 也未开始。§5.2「成熟绑定」偏差继续沿用 ADR-012 的登记。CI 与本机都不实例化 COM、不弹菜单，真实 shell 扩展、PIDL / HMENU 生命周期、owner-draw 消息时序与模态循环全部「未验证」；纯 Win32 菜单文本读取、崩溃哨兵磁盘标记、隔离宿主与自绘兜底仍未实现。`ReleaseState.IsPublishable() == false`，**M1 / M2 / M3 均不成立**，完整偏差见 `docs/adr/ADR-v2-014.md`。

**CI 首次实跑已通过**：run `43874542767`（push head `fd11116`，含 R4 功能提交 `cfed91a` 与账本回填提交 `fd11116`），二十一个门卡 / 编译步骤逐项 `success`，其中 `check-menu-host-specs` / `csharp-fs-compile` / `real-exe-build` / `real-exe-selftest` 全绿 ⇒ 第二套托管 COM 声明与菜单宿主编译通过、四层 105 个 .cs 一次编成单 exe 且 `BUILD.txt` 七字段齐、产品 `--selftest` 报告 ≥100 行、`verdict: GREEN`、退出码 0（含 8 组 R4 契约断言）。本机 15 个 `check_*.py` 门卡与 `run_tests.py` 实测绿（138 组 / 0 失败、122 条负向逐条变红），但不执行 C# 编译器；按 jobs API 的 step `conclusion` 记录，日志端点匿名 403 未读原文。CI 仍不开窗口、不实例化 COM，菜单运行期未验证。

## R5 图标 / 缩略图取图链路与呈现接线（规格外实现批次）

R5 按 `ADR-015` D1 只做「图标 / 缩略图取图与呈现的真实代码路径」，规格 `design-v2.md` v1.2 一字未改、四档不变：

| 项 | 交付 |
|---|---|
| 第三套 COM 声明 | `src/QuadLite.Fs/ShellComInterop.cs` 扩为三套共用的唯一白名单，新增 `IShellItemImageFactory`（SDK 10.0.19041.0 `um/ShObjIdl_core.h` 8693 行 GUID `bcc18b79-ba16-442f-80c4-8a59c30c463b`、IUnknown 之后仅 `GetImage` 一槽）、`SIIGBF_*` 十个值、`SIZE` 结构与 `gdi32!DeleteObject` |
| 取图宿主 | `src/QuadLite.Fs/IconThumbHost.cs`：独立 STA 后台 worker、`SHCreateItemFromParsingName` → `GetImage(SIZE, SIIGBF)`、`IconChannelRules.IsKnown` 四级档硬校验、`ThumbCacheRules.Key` 四段键与 `i|` / `v|` 隔离、`VideoThumbRules.TimeoutSeconds × 1000` 的 `Join` 看门狗、`try-finally` 释放 COM / HBITMAP、默认关闭 |
| Host 呈现接线 | `IconThumbCommands.cs` + `PaneView` 列表项模板 / 后台取图 / `ThumbQueueRules.ShouldApplyResult` 二次校验 + `MainWindow` 注入与状态栏：App `ThumbCommands.Request` 零 I/O 派发、不可变 `IconThumbRequest` 快照、HBITMAP → `BitmapSource`、WPF 内置 WIC 系统包装做 96 / 256 两档自解码与 EXIF 八值方向 |
| 判据与口径 | `tools/check_icon_thumb_specs.py` 16 组静态判据（14 项 PASS + 2 项 N/A，含「`Provide` 不绕过默认关闭开关」）；`tools/com_policy.py` 的 GUID / vtable 全表扩到 R5 一段，`check-transport-specs` 遍历全表自动覆盖；CI 新增 `check-icon-thumb-specs` 一步（共二十二个门卡 / 编译步骤）；产品 `--selftest` 增 8 组 R5 契约断言 |
| 门卡与负向对照 | `run_tests.py` 扩到 162 组 / 145 条负向对照（23 条 R5 逐条实测变红）；`build.ps1` 源码计数 105 → 107；`QuadLite.Host.csproj` 纳入 `IconThumbCommands.cs` |

**偏差照实登记**：`IconThumbHost.Enabled` 默认 `false`，Host 层不提供启用入口，列表项只显示文字占位、状态栏写 `icon-thumb=wired but disabled by default / 未验证`——这是与规格 §5.11「默认按视图档渲染图标 / 缩略图」的**明确偏差**（`ADR-015` D6），理由与 R4 一致：L3 崩溃哨兵、HBITMAP / thumbcache 生命周期真机取证、H1–H6 与 C4 均未完成。ADR-013 D1 预期 R5 需要「WIC + `IShellItemImageFactory`」两套手写 COM，实测收敛为只加后一套：图片两档自解码走 WPF 内置 WIC 托管包装（零 NuGet、零第三方解码器、不手写 `IWIC*` vtable），这是 §5.2「成熟绑定」偏差的部分消解，登记在 `ADR-015` D2；Host 通过 `Uri` 使用该包装而未走 Fs 端口，分层收敛登记在 D7。真实系统图像工厂、HBITMAP 逐像素比对、WIC 解码介质、LRU 淘汰、thumbcache 三段取证与图标呈现全部「未验证」。`ReleaseState.IsPublishable() == false`，**M1 / M2 / M3 均不成立**，完整偏差见 `docs/adr/ADR-v2-015.md`。

**本机实测**：16 个 `check_*.py` 门卡与 `run_tests.py`（共 17 条本地复核命令）全绿（162 组 / 0 失败、145 条负向逐条变红）、`design-v2.md` diff 为空、`build.ps1` 保留 UTF-8 BOM、四层 107 个 `.cs`。本机不执行 C# 编译器。**CI 结论待回填**：四层 107 个 .cs 的真编译与产品 `--selftest`（≥100 行 / `verdict: GREEN` / 退出码 0 / 8 组 R5 断言）以 push 后的 `gates` job 按步骤 `success` 为准，结论回填 `docs/run-ledger.md` R5 行与 `ADR-015`。CI 仍不开窗口、不枚举目录、不实例化 COM、不解码、不写 thumbcache。

## 首版设计目标（摘要）

- 1/2/3/4 窗格布局，每窗格独立标签、历史与视图偏好；便携配置只认程序同级 `data\`。
- 右键默认使用 **Windows 系统原生菜单**，会加载本机注册的 shell 扩展。QuadLite 只按 shell 标准接口调用，不枚举、不改写菜单项。若某次右键因扩展异常未能正常结束，会在下次启动时对该类文件临时改用内置菜单并说明原因，可一键恢复。
- 文件的复制、移动、删除由 **Windows 系统文件操作引擎**执行，因此 ACL、备用数据流、长路径、回收站与云盘占位符的行为与系统文件管理器一致。QuadLite 自己维护跨窗格的操作编排与撤销：撤销仅覆盖本次会话内、且能确定结果位置的操作；永久删除、覆盖式复制与被系统回收站清空的操作不可撤销，界面会如实说明。
- 七档视图、自然排序、列头筛选、显示开关、书签、会话持久化、图标与缩略图；高 DPI 四档对照、单 exe 免运行时。
- 不写注册表、不联网、不安装服务，配置只保存在程序同级的 `data\` 目录。唯一例外是视频缩略图会写入 Windows 系统缩略图缓存（与系统文件管理器同源）。配置目录不可写时程序以内存模式运行并在状态栏明示，不会静默改写到其他位置。

## 明确不做（首版非目标）

完整清单与判据见 `design-v2.md` §3；此处按附录 C 的约定公开列出。

- **功能扩张**：内置播放器 / 预览、压缩包浏览、左导航树、全局搜索框、批量重命名、磁盘分析、云同步、深色主题、插件系统。
- **交互边界**：面包屑 / 地址栏键入定位、详细信息档框选、列头右键（列选择）、标签页右键、分组折叠、跨窗格拖标签。
- **命名空间与媒体边界**：`shell:` 虚拟命名空间、控制面板 / 库 / 回收站视图、UNC 根识别（回落通用图标）；PDF / Office、webp / heic / avif、`<1 MB` 的 `.ts` 缩略图；32 位与 ARM64。
- **已知边界**：首版不提供全文搜索、内置预览；PDF / Office 与部分新图像格式没有缩略图；系统文件操作接口不可用时程序以只读模式运行（`design-v2.md` C-06）。
- **不逐项识别 / 枚举 / 改写第三方 shell 扩展菜单项**；不搬运任何私有取证资产。

## 文档导航

| 文档 | 内容 |
|---|---|
| `design-v2.md` | 需求（FR / NFR）、架构、接口、验收判据、实施批次、风险与合规、待决事项 |
| `CONTRIBUTING.md` | 构建、验收、人工验收项、提交纪律 |
| `SECURITY.md` | 日志脱敏、崩溃摘要格式、漏洞上报 |
| `CODE_OF_CONDUCT.md` | 社区行为准则 |
| `docs/adr/ADR-v2-001.md` | B0 设计决策记录（沿用 / 修订 / 作废） |
| `docs/adr/ADR-v2-002.md` | B1 设计决策记录（L0 契约、证据分层、C# runner 远端实跑） |
| `docs/adr/ADR-v2-004.md` | B2 设计决策记录（三层证据、端口冻结、COM 绑定推迟、偏差登记） |
| `docs/adr/ADR-v2-005.md` | B3 设计决策记录（四档布局、状态内核、WPF 窗口推迟、偏差登记） |
| `docs/adr/ADR-v2-006.md` | B4 设计决策记录（列表视图规则层、状态内核、真实控件推迟、偏差登记） |
| `docs/adr/ADR-v2-007.md` | B5 设计决策记录（原生右键菜单规则层、ID 段语义纠偏、真实 COM 宿主推迟、偏差登记） |
| `docs/adr/ADR-v2-008.md` | B6 设计决策记录（布局拍板保持四档、图标缩略图规则层、真实渲染推迟、偏差登记） |
| `docs/adr/ADR-v2-009.md` | B7 设计决策记录（自测装置、命令行契约修订、门卡总表逐项比对、真实出包推迟、偏差登记） |
| `docs/adr/ADR-v2-010.md` | B8 设计决策记录（发布线阶段链、证据等级纪律、C4 零误报门槛、M3 不成立、偏差登记） |
| `docs/adr/ADR-v2-011.md` | R1 设计决策记录（真实宿主层、单文件构建链、顺序调整、沿用 / 修订 / 作废、M1–M3 仍不成立、偏差登记） |
| `docs/adr/ADR-v2-012.md` | R2 设计决策记录（SDK 权威 COM 声明、§5.2 偏差、分层口径、运行期未验证、M1–M3 仍不成立） |
| `docs/adr/ADR-v2-013.md` | R3 设计决策记录（范围收敛与 R4/R5 顺序、四档落位与目录枚举、搬运接线但默认关闭、运行期未验证、M1–M3 仍不成立） |
| `docs/adr/ADR-v2-014.md` | R4 设计决策记录（SDK 逐字对应声明、标准四步与消息转发、verb 口径单点、默认关闭即 §5.2 偏差、运行期未验证、M1–M3 仍不成立） |
| `docs/adr/ADR-v2-015.md` | R5 设计决策记录（第三套 COM 收敛为 `IShellItemImageFactory` 一槽、WIC 改走 WPF 内置系统包装、四级通道 / LRU / 看门狗 / 唯一写入例外、默认关闭即 §5.11 偏差、运行期未验证、M1–M3 仍不成立） |
| `docs/negative-controls.md` | 不变量 → 负向对照用例 → 应红门卡；写入例外清单；未验证项 |
| `docs/run-ledger.md` | 每批一行，记功能提交 `head_sha` |
| `LICENSE` | MIT 许可证 |

`tools/`、`docs/adr/`、负向对照登记与账本已在 B0 交付；`data\` 下的运行时文件与 `docs/` 中随实现增长的批次记录按 §8.1 逐步落地，不是失效引用。

## 第三方资源

无 NuGet 依赖。WPF、WIC 与 Windows Shell COM 均为系统 API；测试夹具现场生成，仓库不放二进制产物。

## 路线

规格定稿（S3）→ B0 骨架与检查链 → B1 L0 纯函数 → B2 编排 / 适配器 → B3 外壳状态内核 → B4 列表视图规则层 → B5 系统原生右键菜单规则层 → B6 图标 / 缩略图规则层 → B7 自测 / 出包自检规则层 → B8 发布线 C4 + H1–H6 规则层 → **R1 真实实现第一批（单文件 WPF 宿主 / 产品 `--selftest` / 真实构建链）** → **R2 真实 IFileOperation 托管 COM 绑定（已编译、运行期未验证）** → **R3 浏览 / 搬运 UI 接线（已编译、运行期未验证）** → **R4 原生菜单 COM 宿主（已接线、默认关闭、运行期未验证）** → **R5 图标缩略图取图链路（已接线、默认关闭、运行期未验证）** →（一次一个功能域）门卡 → 负向对照 → 单测 → 账本 → 设计决策记录。每批提交前执行 `design-v2.md` §10 开源合规清单。
