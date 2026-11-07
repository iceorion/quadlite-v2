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

## B1 登记

| 不变量 | 负向对照用例 | 应变红的门卡 |
|---|---|---|
| L0 类型齐备 | 临时副本删掉 `NaturalSort.cs` | `check-core-specs` |
| runner 门卡组注册齐备 | 临时副本删掉 `Run("sort-*", ...)` | `check-core-specs` |
| C# 5 语法子集 | 临时副本注入字符串插值 | `check-core-specs` |
| 错误分类与规格表一一对应 | 临时副本给 `ErrorClass` 加一项但不同步 `ErrorSpec` 表 | `check-core-specs` |
| 算法判据（路径 / 边界 / 列宽 / 七档 / 排序 / 危险矩阵 / 状态 / 错误 / 结构化结果 / 选中集） | 改实现或改断言后，CI 编译运行 C# runner | `csharp-runner`（14 组 `ok=False`、`verdict: RED`、退出码非 0） |

B1 的 4 类静态负向对照由 `tools/run_tests.py` 在临时副本里实测「变红 → 恢复 → 变绿」；算法层的「改坏必红」由 CI 编译运行 C# runner 承担，本机不执行编译器，故本机只记静态那一半。

## B2 登记

| 不变量 | 负向对照用例 | 应变红的门卡 |
|---|---|---|
| 编排层类型齐备 | 临时副本删掉 `OperationCoordinator.cs` | `check-core-specs` |
| B2 门卡组注册齐备 | 临时副本删掉 `Run("op-*", TestOps);` | `check-core-specs` |
| Fs 适配器齐备 / 端口绑定 | 临时副本删掉 `ShellFileOperator.cs` | `check-fs-specs` |
| 禁止手写 COM vtable（§5.2） | 临时副本注入 `[ComImport]` 声明 | `check-fs-specs` |
| 串行闸门、判级拒绝、预检只读、取消后闸门释放 | 改编排实现或断言后，CI 编译运行 C# runner | `csharp-runner` `op-*` |
| 跨窗格撤销、20 步容量、拿不到真实位置不入栈、永久删除不入栈 | 改撤销栈或断言 | `csharp-runner` `undo-*` |
| 副本 / 新建文件夹命名、非法字符、冲突策略 | 改命名规则或断言 | `csharp-runner` `name-*` |
| 单一订阅去抖、两段式自操作抑制不吞外部改动 | 改抑制 / 节流或断言 | `csharp-runner` `refresh-*` |
| 剪贴板内外部分流、Preferred DropEffect、源刷新条件 | 改分流规则或断言 | `csharp-runner` `clipboard-*` |
| 未知参数拒绝、诊断模式不参与单实例 | 改参数解析 | `csharp-runner` `cli-*` |
| 配置根只认程序同级 `data\`、只读降级 | 改路径判定 | `csharp-runner` `portable-*` |

B2 的 4 类静态负向对照由 `tools/run_tests.py` 在临时副本里实测「变红 → 恢复 → 变绿」；编排层的「改坏必红」由 CI 编译运行 C# runner 承担。本机安全策略禁止执行系统 C# 编译器，故本机只记静态那一半。

## B3 登记

| 不变量 | 负向对照用例 | 应变红的门卡 |
|---|---|---|
| B3 布局 / 导航 / 会话类型齐备 | 临时副本删掉 `LayoutMath.cs` | `check-shell-specs`、`check-core-specs` |
| B3 门卡组注册齐备 | 临时副本删掉 `Run("layout-*", TestLayout);` | `check-shell-specs`、`check-core-specs` |
| 3 窗格主窗格必须 `RowSpan=3` | 临时副本把主窗格落位改成 4 窗格 | `check-shell-specs`、`csharp-runner` `layout-*` |
| App 外壳骨架齐备 | 临时副本删掉 `ShellState.cs` | `check-shell-specs` |
| L4 状态内核零 I/O、不引用测试替身 | 临时副本在 App 层注入 `using System.IO;` | `check-shell-specs`、`check-boundaries` B-02 |
| C# 5 语法子集 | 临时副本注入字符串插值 | `check-shell-specs`、`check-core-specs` |
| 四档槽位 / 3 窗格跨度 / 折叠 0/1 / 24px 吸附 / 相对均分偏移 / 折前恢复 | 改实现或改断言后，CI 编译运行 C# runner | `csharp-runner` `layout-*` / `split-*` |
| 标签改序不改入参、冷启动不跳最左、导航只作用活动窗格、导航清筛选 F5 保留 | 改规则或断言 | `csharp-runner` `tab-*` / `nav-*` |
| 书签三态与坏项隔离、会话坏值归 0 与 `future-version` 拒读 | 改规则或断言 | `csharp-runner` `bookmark-*` / `session-*` |
| 规格与实现一致（布局按规格 v1.2 四档，不得擅自改规格） | 改动 `design-v2.md` 四档声明或待决确认状态 | `check-shell-specs` |

B3 的 6 类静态负向对照由 `tools/run_tests.py` 在临时副本里实测「变红 → 恢复 → 变绿」；C# runner 的「改坏必红」由 CI 编译运行承担。本机安全策略禁止执行系统 C# 编译器，故本机只记静态那一半。

## B4 登记

| 不变量 | 负向对照用例 | 应变红的门卡 |
|---|---|---|
| B4 列表规则类型齐备 | 临时副本删掉 `FilterRules.cs` | `check-list-specs`、`check-core-specs` |
| B4 门卡组注册齐备 | 临时副本删掉 `Run("filter-*", TestFilter);` | `check-list-specs`、`check-core-specs` |
| 目录枚举默认 4096 / 批 | 临时副本把 `DefaultBatchSize` 改成 128 | `check-list-specs` |
| App 列表视图状态内核齐备 | 临时副本删掉 `ListViewState.cs` | `check-list-specs` |
| L4 列表状态内核零 I/O、不引用测试替身 | 临时副本在 `ListViewState.cs` 注入 `using System.IO;` | `check-list-specs`、`check-boundaries` B-02 |
| C# 5 语法子集 | 临时副本注入字符串插值 | `check-list-specs`、`check-core-specs` |
| 四列 / 大小五档 / 同列或跨列与 / 导航清 F5 留 / 不写会话 / 四来源读显示行集 | 改实现或断言后，CI 编译运行 C# runner | `csharp-runner` `filter-*` |
| 组头不进显示行集、图标档不渲染组头、组内不二次排序 | 改规则或断言 | `csharp-runner` `group-*` / `view-*` |
| 四开关 16 组合与三态过滤、不回顶、缩略图开关不重枚举 | 改规则或断言 | `csharp-runner` `toggle-*` |
| 点选 / Ctrl / Shift 多选、框选仅图标档、Ctrl+A 排除三类 | 改规则或断言 | `csharp-runner` `select-*` |
| 四类命中单一来源、状态栏按筛后行计数、13 键快捷键表 | 改规则或断言 | `csharp-runner` `hit-*` / `status-*` / `keys-*` |
| 规格与实现一致（规格 v1.2 与 B4 判据声明不得擅改） | 改动 `design-v2.md` §8.1 的 B4 行或版本状态 | `check-list-specs` |

B4 的 6 类静态负向对照由 `tools/run_tests.py` 在临时副本里实测「变红 → 恢复 → 变绿」；C# runner 的「改坏必红」由 CI 编译运行承担。本机安全策略禁止执行系统 C# 编译器，故本机只记静态那一半。

## B5 登记

| 不变量 | 破坏方式 | 应红门卡 |
|---|---|---|
| B5 菜单规则类型齐备 | 临时副本删掉 `MenuFusionRules.cs` | `check-menu-specs`、`check-core-specs` |
| B5 门卡组注册齐备 | 临时副本删掉 `Run("menu-id-range", TestMenuIdRange);` | `check-menu-specs`、`check-core-specs` |
| 产品路径零 verb 查询 | 临时副本把 `VerbScanCount()` 从 `return 0` 改成 `return 1` | `check-menu-specs` |
| 产品程序集不读 verb | 临时副本在产品源码注入 verb 查询字样 | `check-menu-specs`、`check-open-source` |
| App 菜单状态内核齐备 | 临时副本删掉 `MenuState.cs` | `check-menu-specs` |
| L4 菜单状态内核零 I/O、不引用测试替身 | 临时副本在 `MenuState.cs` 注入 `using System.IO;` | `check-menu-specs`、`check-boundaries` B-02 |
| C# 5 语法子集 | 临时副本注入字符串插值 | `check-menu-specs`、`check-core-specs` |
| 默认系统原生、四步序列、零删项、零菜单树改写 | 改实现或改断言后，CI 编译运行 C# runner | `csharp-runner` `menu-native-default` / `menu-no-verb-scan` |
| `InvokeCommand` 出参 `[1, maxId]`、自有段 ≥ `0x8000` 不串 shell | 改参数规则或断言 | `csharp-runner` `menu-id-range` / `menu-own-id-segment` |
| 源头消重：等价项用系统项、不等价项改名独立分组 | 改消重规则或断言 | `csharp-runner` `menu-no-fusion` |
| `IContextMenu2/3` 五类消息转发、目标快照不可变 | 改转发 / 快照规则或断言 | `csharp-runner` `menu-msg-forward` / `menu-target-snapshot` |
| 崩溃哨兵：ctxKey 降级 / 7 天 TTL / 一键重试 / 再崩翻倍 | 改哨兵规则或断言 | `csharp-runner` `menu-sentinel` |
| 单文件 / 假扩展四态 / HMENU 与 COM try-finally 释放 | 改宿主或释放规则 / 断言 | `csharp-runner` `menu-single-file` / `menu-release` |
| 规格与实现一致（规格 v1.2 与 B5 判据声明不得擅改） | 改动 `design-v2.md` §8.1 的 B5 行或版本状态 | `check-menu-specs` |

B5 的 7 类静态负向对照由 `tools/run_tests.py` 在临时副本里实测「变红 → 恢复 → 变绿」；C# runner 的「改坏必红」由 CI 编译运行承担，本机只记静态那一半。

## B6 登记

| 不变量 | 负向对照用例 | 应变红的门卡 |
|---|---|---|
| B6 图标缩略图规则类型齐备 | 临时副本删掉 `ThumbCacheRules.cs` | `check-icon-specs`、`check-core-specs` |
| B6 门卡组注册齐备 | 临时副本删掉 `Run("thumb-nowrite", TestThumbNoWrite);` | `check-icon-specs`、`check-core-specs` |
| 96 MB / 1500 条双闸常量不漂移 | 临时副本把 `MaxEntries = 1500` 改成 1 | `check-icon-specs` |
| 唯一显式写入例外不扩张 | 临时副本把 `ExceptionCount = 1` 改成 2 | `check-icon-specs` |
| App 图标缩略图状态内核齐备 | 临时副本删掉 `ThumbState.cs` | `check-icon-specs` |
| L4 状态内核零 I/O / C# 5 语法子集 | 注入 `using System.IO;` / C# 6 字符串插值 | `check-icon-specs`、`check-boundaries` B-02 / `check-core-specs` |
| 规格仍 v1.2 四档、B6 判据未擅改 | 改动 `design-v2.md` 的 B6 行或版本状态 | `check-icon-specs` |
| 四级图标通道 / 14 种白名单 / EXIF 八值 / 可见优先队列 / 四段缓存键 / 15 容器 / 开关不重枚举 / 唯一写入例外 | 改规则实现或断言后，CI 编译运行 C# runner | `csharp-runner` `icon-*` / `thumb-*` / `thumb-nowrite` |

B6 的 7 类静态负向对照由 `tools/run_tests.py` 在临时副本里实测「变红 → 恢复 → 变绿」；C# runner 的「改坏必红」由 CI 编译运行承担，本机只记静态那一半。布局已由用户拍板保持规格 v1.2 四档，规格未改（见 `docs/adr/ADR-v2-008.md` D1）。

## B7 登记

| 不变量 | 负向对照用例 | 应变红的门卡 |
|---|---|---|
| B7 自测 / 出包规则类型齐备 | 临时副本删掉 `SelftestReportRules.cs` | `check-selftest-specs`、`check-core-specs` |
| B7 门卡组注册齐备 | 临时副本删掉 `Run("cli-selftest-output", TestCliSelftestOutput);` | `check-selftest-specs`、`check-core-specs` |
| `--selftest` ≥100 行判据不漂移 | 临时副本把 `MinOutputLines = 100` 改成 1 | `check-selftest-specs` |
| `BUILD.txt` 七字段不缩水 | 临时副本把 `FieldCount = 7` 改成 6 | `check-selftest-specs` |
| 门卡总表与 §7.1 逐项一致 | 临时副本从 `Gates()` 删掉 `menu-sentinel` | `check-selftest-specs`（第 5 项集合比对） |
| App 自测状态内核齐备 | 临时副本删掉 `SelftestState.cs` | `check-selftest-specs` |
| L4 状态内核零 I/O / C# 5 语法子集 | 注入 `using System.IO;` / C# 6 字符串插值 | `check-selftest-specs`、`check-boundaries` B-02 / `check-core-specs` |
| 规格仍 v1.2 四档、B7 判据未擅改 | 改动 `design-v2.md` 的 B7 行或版本状态 | `check-selftest-specs` |
| 命令行契约 parse 与声明不分叉 | 改 `CliArgs` 使其绕开 `CliContractRules` | `check-selftest-specs` 第 7 项 + `cli-*` runner |

B7 的 8 类静态负向对照由 `tools/run_tests.py` 在临时副本里实测「变红 → 恢复 → 变绿」；C# runner 的「改坏必红」由 CI 编译运行承担，本机只记静态那一半。B7 修订了 B2 的 `CliArgs.Parse`（补齐 §6.4 参数与诊断豁免），B2 既有 `cli-*` 断言一条未删，见 `docs/adr/ADR-v2-009.md` D3。

## B8 登记

| 不变量 | 负向对照用例 | 应变红的门卡 |
|---|---|---|
| B8 发布线规则类型齐备 | 临时副本删掉 `ReleaseStageRules.cs` | `check-release-specs`、`check-core-specs` |
| B8 门卡组注册齐备 | 临时副本删掉 `Run("release-signing-and-record", TestReleaseSigningAndRecord);` | `check-release-specs`、`check-core-specs` |
| H1–H6 六阶段计数不缩水 | 临时副本把 `StageCount = 6` 改成 5 | `check-release-specs` |
| H3 零写入三段取证不缩水 | 临时副本把 `ZoneCount = 3` 改成 2 | `check-release-specs` |
| H1 高 DPI 四档不缩水 | 临时副本把 `ScaleCount = 4` 改成 3 | `check-release-specs` |
| C4 零误报门槛不可绕过 | 临时副本把 `ZeroFalsePositiveClaimValid` 改成无条件 `return true;` | `check-release-specs`（第 4 / 7 项） |
| §8.3 阶段链与规格原文逐项一致 | 临时副本把 §8.3 的 `└─ H3` 改成 `└─ H2` | `check-release-specs`（第 5 项） |
| App 发布线状态内核齐备 / L4 零 I/O / C# 5 语法 | 删掉 `ReleaseState.cs`；注入 `using System.IO;` / C# 6 字符串插值 | `check-release-specs`、`check-boundaries` B-02 / `check-core-specs` |
| 规格仍 v1.2 四档、§8.1 B8 / §8.3 降级路径未擅改 | 改动 `design-v2.md` 的 B8 行、降级路径或版本状态 | `check-release-specs` |
| 发布线规则层（`release-*` 十组） | 改规则实现或断言后，CI 编译运行 C# runner | `csharp-runner` `release-*` |

B8 的 10 类静态负向对照由 `tools/run_tests.py` 在临时副本里实测「变红 → 恢复 → 变绿」；C# runner 的「改坏必红」由 CI 编译运行承担，本机只记静态那一半。B8 沿用 B7 的四级证据等级、预算表、崩溃摘要五字段与三段取证计数，不另立第二套表；当前六阶段全部为「未验证」，`ReleaseState.CurrentM3Verdict()` 返回 `EvidenceMissing`，M3 不成立（见 `docs/adr/ADR-v2-010.md` D6）。

## 未验证项（如实标注，不用「没报错」冒充通过）

| 项 | 验证方式 | 状态 |
|---|---|---|
| 与资源管理器同手感（NFR-04 / INV-04） | 人工验收项，写入 `CONTRIBUTING.md` | 未验证 |
| 运行期零写入三段取证（H3）、高 DPI 四档（H1）、兼容矩阵（H2） | 发布线 H1–H6 | 未验证 |
| 危险判级矩阵逐格、路径规范化、SafeWalk 四道边界 | B1 C# runner（CI 实测） | 见 `docs/run-ledger.md` B1 行；本机未验证 |
| 会话坏值归一 / `future-version` 拒读 | B3 C# runner（`session-*`） | CI 实测通过（run `42040769639`，本机未验证） |
| 列表视图规则层（筛选 / 分组 / 开关 / 命中 / 状态栏 / 快捷键） | B4 C# runner（`group-*` / `filter-*` / `toggle-*` / `hit-*` / `status-*` / `keys-*`） | CI 实测通过（run `42224901915`，本机未验证） |
| 菜单规则层（`menu-*` 十条） | B5 C# runner（`menu-native-default` … `menu-release`） | CI 实测通过（run `42232312609`，本机未验证） |
| 产品程序集 `GetCommandString` 命中 0 次 | `check-menu-specs` + `check-open-source` 扫描 `src/**/*.cs` | 本机实测通过（72 个 .cs，命中 0 次；负向对照注入后必红） |
| C# 算法 runner 在本机编译运行 | 本机不执行 C# 编译器；CI 远端以 C# 5 编译实跑 | 本机未验证 / CI 实测（见账本 B1 / B2 行） |
| 真实 `IFileOperation` 搬运 / flags / 独立 STA / `ProgressSink` 与 `PostDeleteItem` | 成熟 COM 绑定选定后接入 `IShellTransport`，另立 ADR | **未实现 / 未验证**（B2 偏差，见 `ADR-v2-004`） |
| 系统剪贴板 `CF_HDROP` 互通、`FileSystemWatcher` 实际订阅 | B3 / 发布线接入真实适配器 | **未验证**（B2 只编译 Fs 库，不运行） |
| 冲突 / 进度非模态面板 | B3 外壳（偏差，见 `ADR-v2-005` D6） | **未实现 / 未验证** |
| 危险判级真实绑定 | 外壳注入 `IDangerPolicy` | **未验证**（B2 只跑 Fake） |
| Fs 适配器运行期行为 | CI 编译成 library，不运行 | **未验证**（结构 / 语法可判） |
| 真实 WPF 窗口 / XAML / 消息循环 / 事件绑定 | B3 后续批次 | **未实现 / 未验证**（B3 只交状态内核与可编译骨架） |
| 真实 WPF 列表控件 / 虚拟化面板 / 列头拖宽与筛选 UI / 拖放 / 框选交互 / 图标与缩略图渲染 | B4 后续批次 / B6 | **未实现 / 未验证**（B4 只交规则层与可编译状态内核） |
| 真实 `NativeMenuHost` / `IContextMenu` / `IContextMenu2/3` COM 调用、HMENU 生命周期、`TrackPopupMenuEx` 消息循环 | B5 后续批次 / B7 | **未实现 / 未验证**（成熟 COM 绑定未定，见 `ADR-v2-007` D6） |
| 崩溃哨兵磁盘标记 `data\menu-pending.json` / `menu-safe.json` TTL 过期、自绘兜底 UI 与一键重试按钮 | B5 后续批次 / B6 | **未实现 / 未验证**（只交不可变状态内核与结构化提示串） |
| 自有命令真实菜单项插入（ID ≥ `0x8000`） | B5 后续批次 | **未实现 / 未验证**（只交源头消重纯函数判据） |
| `FakeShellExtension` 归位 `QuadLite.Probe` 项目 | 建 Probe 项目时迁移 | **偏差已登记**（暂驻 `tests/QuadLite.Tests/MenuFakes.cs`，见 `ADR-v2-007` D6） |
| 真实目录枚举（`FindFirstFileExW` + `LARGE_FETCH`、4096 / 批） | Fs 层后续批次 | **未实现 / 未验证**（`DirSource` 当前为 128 / 批，见 `ADR-v2-006` D6） |
| 名称拼音分桶 / 修改日期月历 / 类型动态枚举 | B4 后续批次 | **未实现 / 未验证**（只落地桶键组合逻辑） |
| 可编辑地址栏、标签拖动 UI、此电脑层真实枚举与盘符取图、会话原子写与备份 | B3 后续批次 / Fs 层 | **未实现 / 未验证** |
| WPF 应用 MSBuild 构建、`build.ps1 -SingleFile`、exe 形态 `--selftest` | B7 完成出包 | 未验证（CI 不含 WPF 应用构建） |
| 图标缩略图规则层（`icon-*` / `thumb-*` / `thumb-nowrite` 八组） | B6 C# runner（CI 编译实跑） | CI 实测通过：run `42343839222` / head `c0529da`，`csharp-runner` 步骤 `success` ⇒ 退出码 0 ⇒ 51 组 `ok=True`；本机静态门卡实测绿，见 `docs/run-ledger.md` B6 行 |
| 真实 WIC 解码、`IShellItemImageFactory` 绑定、后台 worker、LRU 介质、视频 8 秒看门狗 | B6 后续批次 / 发布线 | **未实现 / 未验证**（只交纯函数判据，见 `ADR-v2-008` D6） |
| 盘符图标逐像素比对、EXIF 像素级变换、图标与缩略图渲染控件 | B6 后续批次 | **未实现 / 未验证** |
| 运行期零写入三段取证（注册表 / 应用目录 / 系统缓存） | 发布线 H3 | 未验证（源码只给出三个观察区与允许条件） |
| App 图标缩略图状态内核库编译 | CI `csharp-app-compile`（8 个 .cs） | CI 实测通过：run `42343839222` 该步骤 `success`；只编译、不运行；本机不执行 C# 编译器 |
| 真实产品 exe 的 `--selftest` / `--bench` / `--shot` / `--open-test` | 出包批次 / 发布线 | **未实现 / 未验证**（B7 只交 runner exe 的 120 行报告，见 `ADR-v2-009` D6） |
| `BUILD.txt` 七字段真实采集（编译时间 / 提交 / 工作树 / exe 大小 / sha256 / 嵌入资源 / 包自检结果） | 出包批次 | **未实现 / 未验证**（只交字段齐备判据与脏树 Warning 规则） |
| 单文件出包、零安装、免运行时、杀软误报 | 发布线 H1–H6 | **未实现 / 未验证** |
| 崩溃摘要真实写入与脱敏核验、性能预算实测值、高 DPI 四档 | 发布线 H4 / H5 / H1 | **未实现 / 未验证**（预算表四行只报 `unmeasured` + 未验证） |
| 真实 Mutex / `run.lock` / `WM_COPYDATA('QL')` 转发 | Fs / App 宿主后续批次 | **未实现 / 未验证** |
| 自测 / 出包规则层（`cli-*` / `portable-*` / `single-instance-*` 十组） | B7 C# runner（CI 编译实跑） | CI 实测通过：run `42357787519` / head `266ca65`，`csharp-runner` 步骤 `success` ⇒ 退出码 0 ⇒ 61 组 `ok=True`；本机 64 组自测 / 53 条负向对照实测绿 |
| App 自测 / 出包状态内核库编译 | CI `csharp-app-compile`（10 个 .cs） | CI 实测通过：run `42357787519` 该步骤 `success`；只编译、不运行；本机不执行 C# 编译器 |
| H1 高 DPI 四档截图 / PerMonitorV2 实装 / 四档几何实测 | 发布线 H1 | **未实现 / 未验证**（B8 只判定四档矩阵齐备与缺档判红） |
| H2 Win11 VM / LTSC × NTFS / exFAT / SMB / 只读载体八格实测 | 发布线 H2 | **未实现 / 未验证**（B8 只判定八格矩阵缺一格即判红） |
| H3 注册表 / 应用目录 / 系统缓存三段取证 | 发布线 H3 | **未实现 / 未验证**（B8 只判定三段齐备与唯一写入例外） |
| H4 崩溃摘要真实写入 / WER 实际读法、H5 性能四预算实测值 | 发布线 H4 / H5 | **未实现 / 未验证**（B8 只判定五字段脱敏、七列齐全、只报数不判定） |
| H6 免费 OV 证书 / `signtool sign` + `verify /pa` / 指纹时间戳进 `BUILD.txt` | 发布线 H6 | **未实现 / 未验证**（B8 只判定链路三步、未签名 Warning 与不得冒充通过） |
| C4 带杀软 VM / 第二台机器、单文件 vs 多文件对比、零误报结论 | 发布线 C4 | **未实现 / 未验证**；B8 把 `AVcount≠0 + 实际扫描` 写成可判定门槛，C4 缺位尚未在发布记录显式声明 |
| 发布线规则层（`release-*` 十组） | B8 C# runner（CI 编译实跑） | CI 实测通过：run `42361122709` / head `97fd31d`，`csharp-runner` 步骤 `success` ⇒ 退出码 0 ⇒ 71 组 `ok=True`；本机 75 组自测 / 63 条负向对照实测绿 |
| App 发布线状态内核库编译 | CI `csharp-app-compile`（12 个 .cs） | CI 实测通过：run `42361122709` 该步骤 `success`；只编译、不运行；本机不执行 C# 编译器 |
| M3 可发布（H1–H6 逐项有证据；C4 缺位显式写「未做」） | 发布记录判定 | **不成立**：六阶段真实证据全无，`ReleaseState.IsPublishable() == false`（见 `ADR-v2-010` D6） |

## 远端验证（CI）

CI run `42013589562`（head `96a5fa4`，2026-11-05）在远端流水线实跑，job `gates` 结论 `success`，四个门卡步骤逐项 `success`：`check-trace`、`check-boundaries`、`run-tests (includes negative controls)`、`open-source compliance`。该 run 只覆盖四个 Python 门卡，**不包含** MSBuild 构建、出包与 exe 形态 `--selftest`，故相关项维持「未验证」；本机实测与 CI 结果一并记在 `docs/run-ledger.md` 的 B0 行。

B1 起 CI 增加 `check-core-specs` 与 `csharp-runner` 两步：后者在 Windows runner 上以 `/langversion:5 /codepage:65001` 把 L0 与 C# runner 编成单个 exe 并运行，覆盖 14 组算法门卡。**首次实跑已通过**：run `42022736600`（head `dcea585`），六个门卡步骤逐项 `success`，其中 `csharp-runner` 退出码 0 ⇒ 14 组 `ok=True`、`verdict: GREEN`。逐批结论以 `docs/run-ledger.md` 为准；本机仍不执行 C# 编译器，静态检查的绿不等于算法跑通。

B2 起 CI 再增加 `check-fs-specs` 与 `csharp-fs-compile`：后者把 Core + Fs 编成单个 library，**只编译、不运行**，因此只证明结构与语法可编译，不证明任何系统能力可用；`csharp-runner` 扩到 21 组（含 7 组 B2 编排）。**首次实跑已通过**：run `42035381476`（head `1066b0d`），八个门卡步骤逐项 `success`，其中 `csharp-runner` 退出码 0 ⇒ 21 组 `ok=True`、`verdict: GREEN`，`csharp-fs-compile` 编译成功且不运行。逐批结论见 `docs/run-ledger.md`。

B3 起 CI 再增加 `check-shell-specs` 与 `csharp-app-compile`：后者把 Core + App 外壳状态内核编成单个 library，**只编译、不运行**，且不引用呈现框架程序集；`csharp-runner` 扩到 27 组（含 6 组 B3 外壳）。真实 WPF 窗口 / XAML / 消息循环不在 CI 内，相关项维持「未验证」。**首次实跑已通过**：run `42040769639`（head `378432a`），十个门卡步骤逐项 `success`，其中 `csharp-runner` 退出码 0 ⇒ 27 组 `ok=True`、`verdict: GREEN`，`csharp-app-compile` 编译成功且不运行。首个功能提交 `15d0aa9` 的 run `42040381217` 因 `bookmark-*` 断言期望值笔误在 `csharp-runner` 判红，修正提交 `378432a` 后复跑全绿；该次红不作为通过证据。逐批结论见 `docs/run-ledger.md`。

B4 起 CI 再增加 `check-list-specs`（`csharp-runner` 与 `csharp-app-compile` 用目录 glob 自动包含新增的 11 个 Core 与 2 个 App 文件，无需新增编译步骤）：`csharp-runner` 扩到 33 组（含 6 组 B4 列表），`csharp-app-compile` 编译 4 个 App 状态内核 .cs，仍只编译、不运行、不引用呈现框架程序集。真实 WPF 列表控件 / 虚拟化面板 / 拖放不在 CI 内，相关项维持「未验证」。**首次实跑已通过**：run `42224901915`（head `8ed8b5b`），十一个门卡 / 编译步骤逐项 `success`，其中 `check-list-specs` `success`、`csharp-runner` 退出码 0 ⇒ 33 组 `ok=True`、`verdict: GREEN`，`csharp-app-compile` 编译成功且不运行。逐批结论见 `docs/run-ledger.md`。

B5 起 CI 再增加 `check-menu-specs`（`csharp-runner` 与 `csharp-app-compile` 用目录 glob 自动包含新增的 8 个 Core、2 个 App 与 1 个 Tests 文件，无需新增编译步骤）：`csharp-runner` 扩到 43 组（含 10 组 B5 菜单），`csharp-app-compile` 编译 6 个 App 状态内核 .cs，仍只编译、不运行、不引用呈现框架程序集；§10「产品程序集 verb 查询 0 次」由 `check-menu-specs` 与 `check-open-source` 双门卡本机实测。真实原生菜单 COM 宿主 / 崩溃哨兵磁盘标记 / 自绘兜底 UI 不在 CI 内，相关项维持「未验证」。**首次实跑已通过**：run `42232312609`（head `aa63769`），十二个门卡 / 编译步骤逐项 `success`，其中 `check-menu-specs` `success`、`csharp-runner` 退出码 0 ⇒ 43 组 `ok=True`、`verdict: GREEN`，`csharp-app-compile` 编译成功且不运行。逐批结论见 `docs/run-ledger.md`。

B6 起 CI 再增加 `check-icon-specs`：`csharp-runner` 与 `csharp-app-compile` 用目录 glob 自动包含新增的 8 个 Core 与 2 个 App 文件，无需新增编译步骤；`csharp-runner` 扩到 51 组（含 8 组 B6 图标缩略图），`csharp-app-compile` 编译 8 个 App 状态内核 .cs，仍只编译、不运行、不引用呈现框架程序集；`run_tests.py` 扩到 55 组 / 45 条负向对照，本机实测绿，「唯一写入例外」由 `check-boundaries` INV-06 与 `check-icon-specs` 双门卡核对。真实 WIC 解码、系统图像工厂、图标缩略图渲染与三段取证不在 CI 内，相关项维持「未验证」。**首次实跑已通过**：run `42343839222`（head `c0529da`，含 B6 功能提交 `8d90b7a` 与账本回填提交 `c0529da`），十三个门卡 / 编译步骤逐项 `success`，其中 `check-icon-specs` `success`、`csharp-runner` 退出码 0 ⇒ 51 组 `ok=True`、`verdict: GREEN`，`csharp-app-compile`（8 个 .cs）编译成功且不运行；日志端点匿名 403，runner 结论沿用前批「步骤成功即退出码 0」判据。本机不执行 C# 编译器，不把静态绿写成算法跑通。逐批结论见 `docs/run-ledger.md`。

B7 起 CI 再增加 `check-selftest-specs`；`csharp-runner` 的编译命令自本批起同时包含 `src/QuadLite.App/*.cs`（此前只编 Core + Tests），因为 runner 的 120 行报告由 `SelftestState` 装配：`csharp-runner` 扩到 61 组（含 10 组 B7），`csharp-app-compile` 编译 10 个 App 状态内核 .cs，仍只编译、不运行、不引用呈现框架程序集；`run_tests.py` 扩到 64 组 / 53 条负向对照，本机实测绿。门卡总表与 §7.1 的 50 项由 `check-selftest-specs` 逐项比对，删一项即判红；`CliArgs` 复用契约表的三处委托也由静态门卡核对。真实产品 exe 的 `--selftest`、`BUILD.txt` 采集、单文件出包、`--bench` 采样、崩溃日志写入与零写入三段取证不在 CI 内，相关项维持「未验证」。**首次实跑已通过**：run `42357787519`（head `266ca65`，含 B7 功能提交 `864eff1` 与账本回填提交 `266ca65`），十四个门卡 / 编译步骤逐项 `success`，其中 `check-selftest-specs` `success`、`csharp-runner` 退出码 0 ⇒ 61 组 `ok=True`、`verdict: GREEN`，`csharp-app-compile`（10 个 .cs）编译成功且不运行；日志端点匿名 403，runner 结论沿用前批「步骤成功即退出码 0」判据。本机不执行 C# 编译器，不把静态绿写成算法跑通。逐批结论见 `docs/run-ledger.md`。

B8 起 CI 再增加 `check-release-specs`；`csharp-runner` 扩到 71 组（含 10 组 B8 发布线），报告 120 → 130 行，`csharp-app-compile` 编译 12 个 App 状态内核 .cs，仍只编译、不运行、不引用呈现框架程序集；`run_tests.py` 扩到 75 组 / 63 条负向对照，本机实测绿。§8.3 依赖链七节点、DPI 四档与降级链由 `check-release-specs` **解析规格原文逐项比对**，改序或改数值即判红；C4 零误报门槛与 M3 证据闭环另由第 6 / 7 项核对。H1–H6 真实取证、C4 带杀软 VM、OV 签名与产品 exe 出包都不在 CI 内，相关项维持「未验证」，M3 不成立。**首次实跑已通过**：run `42361122709`（head `97fd31d`，含 B8 功能提交 `896a446` 与账本回填提交 `97fd31d`），十五个门卡 / 编译步骤逐项 `success`，其中 `check-release-specs` `success`、`csharp-runner` 退出码 0 ⇒ 71 组 `ok=True`、`verdict: GREEN`、`csharp-app-compile`（12 个 .cs）编译成功且不运行；日志端点匿名 403，runner 结论沿用前批「步骤成功即退出码 0」判据。本机不执行 C# 编译器，不把静态绿写成算法跑通或可发布。逐批结论见 `docs/run-ledger.md`。

## 写入例外清单

> 规范性说明：本节只登记产品运行期允许写入的位置。`tools/check_boundaries.py` 的 INV-06 会核对条目数，此处必须与 `design-v2.md` §5.5 / §6.4 一致；新增例外须同步改规格并重新跑门卡。

- 视频缩略图写入 Windows 系统缩略图缓存（thumbcache），与系统文件管理器同源；首版唯一显式例外。
