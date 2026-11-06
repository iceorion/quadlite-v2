# QuadLite

一个面向 Windows 的便携多窗格文件浏览器**规格项目**。仓库按 docs-first / spec-first 路线推进：规格定稿 → B0 工程骨架与检查链 → B1 L0 纯函数层 → B2 编排与适配器层 → B3 外壳状态内核 → **B4 列表视图规则层**。目前没有可运行的 WPF 应用，也没有可下载版本。

- 许可证：MIT（见 `LICENSE`）
- 规范：`design-v2.md`（需求、架构、接口与验收判据的唯一权威文档）
- 参与方式：见 `CONTRIBUTING.md`；安全问题见 `SECURITY.md`；行为准则见 `CODE_OF_CONDUCT.md`

## 当前状态

| 项 | 状态 |
|---|---|
| 规格 | `design-v2.md` v1.2 已定稿（B0 修订检查链脚本名、确认三项待决） |
| 工程骨架 | B0 已完成：SDK 风格 csproj、构建脚本、检查链、CI、证据闭环 |
| 代码 | `src/QuadLite.Core/` 43 个零 I/O 类型（B1 的 13 个 L0 + B2 的 10 个编排 / 端口 + B3 的 9 个布局 / 导航 / 会话 + B4 的 11 个列表视图规则）；`src/QuadLite.Fs/` 5 个适配器；`src/QuadLite.App/` 4 个状态类型（无窗口 / XAML） |
| C# runner | 33 组门卡（14 组 L0 + 7 组 B2 + 6 组 B3 + 6 组 B4），CI 上以 C# 5 编译实跑；B1（run `42022736600` / `dcea585`）、B2（run `42035381476` / `1066b0d`）与 B3（run `42040769639` / `378432a`，十步全 `success`）首次实跑均已通过，B4 首次实跑待回填，逐批结论见 `docs/run-ledger.md` |
| 布局方案 | 规格 v1.2 的 1/2/3/4 四档；用户提出的自定义分割线拓扑仍在讨论中、未拍板，规格未改（见 `docs/adr/ADR-v2-005.md`） |
| 文件操作能力 | **尚不能搬运文件**：真实 `IFileOperation` 调用点隔离在注入端口，成熟 COM 绑定待接入，标「未实现 / 未验证」，见 `docs/adr/ADR-v2-004.md` |
| 可下载版本 | 无；真实 WPF 窗口 / XAML / 消息循环未实现，应用构建与单文件出包尚未实跑（未验证）；CI 编译并运行单测 runner、另把 Fs 适配器与 App 外壳状态内核分别编译成 library（不运行） |

规格中的能力描述是**首版的设计目标与验收判据**，不是已发布功能的宣称。里程碑与 MVP 定义见 `design-v2.md` §8.4。

## B0 工程骨架与检查链

B0 只交付骨架与可机器验证的闸门，不含产品功能代码：

| 路径 | 内容 |
|---|---|
| `Directory.Build.props` / `src/*.csproj` | net48 + WPF + C# 5 语法子集，SDK 风格工程文件，`FrameworkPathOverride` 参数化 |
| `build.ps1` | 构建与出包入口（B0 未实跑，标「未验证」） |
| `tools/` | 零第三方依赖的 Python 3 检查脚本（B0 四个 + B1 `check-core-specs` + B2 `check-fs-specs` + B3 `check-shell-specs` + B4 `check-list-specs`，共八个）+ 正 / 负样本夹具 |
| `.github/workflows/ci.yml` | 需求追踪 / 静态边界 / 静态契约（Core、Fs、外壳、列表）/ 检查链自测 / 开源合规 / C# runner / Fs 与 App 库编译；B0、B1、B2、B3（run `42040769639`）首次实跑均已通过，B4 首次实跑待回填 |
| `docs/adr/` `docs/negative-controls.md` `docs/run-ledger.md` | 设计决策记录、负向对照登记、运行账本 |

本地复核（无需安装任何依赖）：

```
python tools/check_trace.py
python tools/check_boundaries.py
python tools/check_core_specs.py
python tools/check_fs_specs.py
python tools/check_shell_specs.py
python tools/check_list_specs.py
python tools/run_tests.py
python tools/check_open_source.py
```

`run_tests.py` 跑正样本、31 条负向对照（故意破坏后门卡必须变红）与合规扫描，共 39 组，末行输出 `verdict:`，退出码 = 失败数。B0 的四个 Python 门卡首次实跑已通过（run `42013589562`，head `96a5fa4`，结论 `success`）；B1 起 CI 另加 `check-core-specs` 与 C# runner 编译运行，B2 起再加 `check-fs-specs` 与 Fs 适配器库编译，B3 起再加 `check-shell-specs` 与 App 外壳状态内核库编译，B4 起再加 `check-list-specs`。真实 WPF 窗口、列表控件、拖放、应用构建、单文件出包与 exe 形态 `--selftest` 仍未实跑，结论记「未验证」，见 `docs/adr/ADR-v2-002.md`、`ADR-v2-004.md`、`ADR-v2-005.md` 与 `ADR-v2-006.md`。

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

B3 按 `design-v2.md` §8.1 的既定范围推进，**布局按 v1.2 的 1/2/3/4 四档实现**；用户提出的自定义分割线拓扑仍在讨论中、未拍板，规格一字未改（见 `docs/adr/ADR-v2-005.md`）。

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

`check_list_specs.py` 核对 B4 类型齐备、App 状态内核齐备、门卡组注册、规格关键常量（4096 / 四列 / 五档 / 13 键）与不变量、规格与实现一致、App 层零 I/O 与 C# 5 语法；`run_tests.py` 含 6 类 B4 负向对照，逐条实测变红。C# runner 扩到 33 组（6 个新增组 + 5 个既有组补强断言），CI 首次实跑待回填。布局决策仍在讨论中、未拍板；B4 只关心单窗格内的行集与交互，不受窗格数量与落位模型影响（见 `ADR-v2-005` D7）。

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
| `docs/negative-controls.md` | 不变量 → 负向对照用例 → 应红门卡；写入例外清单；未验证项 |
| `docs/run-ledger.md` | 每批一行，记功能提交 `head_sha` |
| `LICENSE` | MIT 许可证 |

`tools/`、`docs/adr/`、负向对照登记与账本已在 B0 交付；`data\` 下的运行时文件与 `docs/` 中随实现增长的批次记录按 §8.1 逐步落地，不是失效引用。

## 第三方资源

无 NuGet 依赖。WPF、WIC 与 Windows Shell COM 均为系统 API；测试夹具现场生成，仓库不放二进制产物。

## 路线

规格定稿（S3）→ B0 骨架与检查链 → B1 L0 纯函数 → B2 编排 / 适配器 → B3 外壳状态内核 → B4 列表视图规则层 →（B5 起）按批次一次一个功能域推进：门卡 → 负向对照 → 单测 → 账本 → 设计决策记录。每批提交前执行 `design-v2.md` §10 开源合规清单。
