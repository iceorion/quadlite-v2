# QuadLite v2 软件设计说明书（SDD）

| 项 | 内容 |
|---|---|
| 文档标识 | `design-v2.md` |
| 版本 / 日期 | 1.1 / 2026-11-05 |
| 状态 | 已定稿（v1.1）：本文是公开仓首个规格提交；每次提交前须通过 §10 开源合规清单 |
| 变更摘要 | 1.0 → 1.1：需求按 6 个功能域重组为 FR-01..36；新增 §5.8–5.13、§6.4、§8.4、附录 C；修正布局档位、只读降级口径与 S3 提交文件集合；补齐需求追踪映射，使 §2.1 / §7.1 / §8.1 / 附录 C 四方一致 |
| 适用范围 | QuadLite v2 首版（公开、`docs-first` / spec-first 重构） |
| 读者 | 开发者、评审者、贡献者、发布负责人 |
| 权威关系 | 本文是**需求、架构、实现与验收的唯一规范**。项目管理（阶段与 DoD）与过程记录另行维护，不随本文发布 |
| 参考 | 仓库内 `CONTRIBUTING.md`（构建与验收）、`SECURITY.md`（日志与脱敏）、`docs/adr/`（设计决策记录）；外部标准：Windows Shell `IContextMenu` / `IFileOperation` / `IFileOperationProgressSink` 文档 |

> 本文只写**需求、目标、非目标、架构、接口、实现方式、验收判据、实施计划、风险与合规、待决事项**。不含历史方案溯源、取舍推演、修订过程与反思。

---

## 1. 引言

### 1.1 目的与范围

定义 QuadLite v2 首版的功能边界、系统架构、模块职责、外部依赖与可机器验证的验收判据，使实现可以按批次独立交付、独立验证。本文不规定具体代码实现，也不规定项目管理流程。

### 1.2 定义与缩略语

| 术语 | 定义 |
|---|---|
| 窗格 / 活动窗格 | 布局 **1/2/3/4** 中的一个独立目录视图；全局唯一活动窗格，快捷键只作用于它 |
| 门卡 / 门卡组 | `--selftest` 中的命名断言；必须能红（改坏实现即失败）。门卡组按功能域前缀命名，见 §7.1 |
| 负向对照 | 故意破坏被测不变量后，对应门卡必须变红的验证用例；按**不变量**覆盖，不按门卡逐条配 |
| 端口 | 可替换的接口（`IFileOperator` 等），实现由适配器提供，测试由假件提供 |
| shell 契约 | Windows Shell 文档中定义、由资源管理器同样使用的标准接口序列 |
| ctxKey | 崩溃哨兵的上下文键：`hash(扩展类别 + 文件类型 + 卷类型)` |
| OpRequest / OpRecord | 一次文件操作的请求 / 结构化结果：逐项源→真实目标、`hr`、失败原因 |
| MenuTarget | 打开右键那一刻的不可变快照：源集合、目标窗格、目录、选中集 |
| 选中集 | 当前窗格内**筛后可见**的已选真实条目；组头、筛掉的行、禁用项均不计入 |
| 筛选 | 列头 ▾ 对当前行集施加的过滤条件；同列「或」、跨列「与」，不写会话 |
| 显示开关 | 隐藏文件 / 系统文件 / 扩展名 / 缩略图四项视图开关，会话持久化 |
| 追踪矩阵 | 附录 C：需求 → 设计章节 → 门卡 → 批次的双向可核对映射 |
| MVP | 最小可用版本：能启动、能浏览、能搬运、能撤销、能过静态检查的第一个可交付状态（§8.4） |

---

## 2. 需求

### 2.1 功能性需求

按 6 个功能域分组；编号即需求基线，附录 C 给出「需求 → 设计 → 门卡 → 批次」追踪矩阵。

| 组 | ID | 需求 | 验收指向 |
|---|---|---|---|
| A 布局导航 | FR-01 | **1/2/3/4 窗格布局**切换；折叠而非销毁窗格；全局唯一活动窗格 | §5.8、§7 `layout-*` / `split-*` |
| A | FR-02 | 每窗格独立标签组、拖动改序、新建 / 关闭标签；标签各自持有历史 / 行集 / 排序 / 滚动 / 令牌 | §5.8、§7 `tab-*` |
| A | FR-03 | 可编辑地址栏 + 驱动器下拉；路径规范化覆盖长路径 / UNC / `..` 越界 / 设备命名空间 / 引号空白，`PathKey` 统一形式 | §5.8、§7 `path-*` |
| A | FR-04 | 前进 / 后退导航；导航键只作用于活动窗格 | §5.8、§7 `nav-*` |
| A | FR-05 | 「此电脑」层与盘符根浏览，盘符按真实路径取图 | §5.8、§5.11、§7 `nav-*` / `icon-*` |
| A | FR-06 | 书签 / 收藏夹：只收藏目录、失效灰显、失效判定离 UI 线程 | §5.8、§7 `bookmark-*` |
| A | FR-07 | 会话持久化：布局 / 标签 / 路径 / 视图 / 排序 / 列宽 / 显示开关；原子写 + 备份 + 坏值隔离 | §5.8、§6.4、§7 `session-*` |
| A | FR-08 | 分隔条拖动、拖到 0 即折叠、比例以「相对均分偏移」跨会话保持 | §5.8、§7 `split-*` |
| B 列表视图 | FR-09 | 目录枚举分批（默认 4096 / 批）、可取消、`generation` 丢弃迟到批次、虚拟滚动 | §5.9、§7 `enum-*` |
| B | FR-10 | 七档视图 `Details / List / ExtraLarge / Large / Medium / Small / Tiles`；图标档不渲染组头 | §5.9、§7 `view-*` |
| B | FR-11 | 排序（**自然排序**：数字按数值）、列头点击排序、分组依据；分组不二次排序 | §5.9、§7 `sort-*` / `group-*` |
| B | FR-12 | 列头拖动改宽、双击复位、每标签一份偏好（`0=自动`）；`ColumnLayout.For` 为列宽唯一来源 | §5.9、§7 `cols-*` |
| B | FR-13 | 列头筛选：四列 ▾（名称拼音分桶 / 修改日期月历 / 类型动态 / 大小五档），同列「或」跨列「与」；导航即清除、F5 刷新保留、不写会话 | §5.9、§7 `filter-*` |
| B | FR-14 | 显示开关：隐藏文件 / 系统文件 / 扩展名 / 缩略图，会话持久化，切换后原地刷新且保持视口 | §5.9、§7 `toggle-*` |
| B | FR-15 | 选择与多选：点选、Ctrl/Shift 多选、Ctrl+A（不含组头、不含筛掉的行、不含禁用项）、图标档框选 | §5.10、§7 `select-*` |
| B | FR-16 | 状态栏按**筛后可见行**计数、显示选中数与操作结果 / 提示 | §5.10、§7 `status-*` |
| B | FR-17 | 双击判位置、拖入 / 右键共用同一命中测试 | §5.10、§7 `hit-*` |
| C 文件操作 | FR-18 | 复制 / 移动 / 删除 / 重命名 / 新建文件夹 / 剪切粘贴；执行由系统文件操作引擎承担 | §5.1、§7 `op-*` |
| C | FR-19 | 冲突处理：应用预检同名 / 只读 / 占用并给非模态面板；同目录粘贴 ` - 副本`；新建文件夹重名递增；文件名校验（保留名 / 尾随点空格 / 长度） | §5.1、§7 `name-*` |
| C | FR-20 | 跨窗格搬运；内部拖放走应用编排、对外拖放走 shell 数据对象 | §5.1、§7 `op-drop-internal-vs-external` |
| C | FR-21 | 全局撤销栈（20 步、本次会话、不落盘），任一窗格可撤销最近一次；不可撤销如实标注 | §5.4、§7 `op-undo-cross-pane` |
| C | FR-22 | 系统剪贴板互通 `CF_HDROP + Preferred DropEffect`，与资源管理器双向可用 | §6.2、§7 `clipboard-*` |
| C | FR-23 | 危险操作判级与确认；判级只在 `SafetyGuard` 一处，判级矩阵逐格有测试 | §5.1、§7 `safety-*` |
| C | FR-24 | 失败汇总：成败只看 `hr>=0`、分母是回调数、逐项结构化 `OpRecord`，不吞错误 | §5.1、§7 `op-result-structured` |
| C | FR-25 | 目录监视：单一订阅、去抖、自操作抑制、刷新保持视口与选中集 | §5.3、§7 `refresh-*` |
| D 右键菜单 | FR-26 | **默认提供 Windows 系统原生右键菜单**，含本机注册的 shell 扩展项 | §5.2、§7 `menu-*` |
| D | FR-27 | 源头消重：等价命令直接用系统项，不等价命令改名独立分组；零 verb 查询、零删项 | §5.2、§7 `menu-no-fusion` |
| D | FR-28 | 崩溃哨兵：一次异常后按 ctxKey 降级、一键恢复 | §5.6、§7 `menu-sentinel` |
| E 图标缩略图 | FR-29 | 四级图标尺寸通道（16/48/96/256）+ exe 图标两段式 + 盘符真实图标 | §5.11、§7 `icon-*` |
| E | FR-30 | 图片缩略图：WIC 内置解码器白名单、两档自解码、EXIF 方向、可见优先队列、LRU 双闸、开关不重枚举 | §5.11、§7 `thumb-*` |
| E | FR-31 | 视频缩略图走系统图像工厂、独立 worker + 超时看门狗 + 负缓存；写系统缩略图缓存为**唯一显式写入例外** | §5.11、§2.2 NFR-02、§7 `thumb-*` / `thumb-nowrite` |
| F 系统工程 | FR-32 | 单实例 + 命令行契约（§6.4）；自测 / 出图 / 诊断模式不参与单实例 | §5.13、§6.4、§7 `single-instance-*` / `cli-*` |
| F | FR-33 | 便携配置只认 `data\`；不可写时降级为内存会话模式并在状态栏明示 | §5.13、§7 `portable-*` |
| F | FR-34 | 自测装置、门卡总表、账本，`--selftest` 输出可机器判定 | §7、§7.1 `check-boundaries` / `check-trace` |
| F | FR-35 | 崩溃摘要脱敏、单文件出包与 `BUILD.txt` 字段齐备 | §5.5、§5.12、§8.3、§7 `portable-*` / `error-*` |
| F | FR-36 | 会话格式兼容旧版 `schemaVersion 1`；坏值按字段归一而非整份拒读 | §6.4、§7 `session-*` |

### 2.2 非功能性需求

| ID | 需求 | 判据 |
|---|---|---|
| NFR-01 | 单 exe、零安装、零服务、免运行时 | `build.ps1 -SingleFile` 产出 `exe + BUILD.txt` |
| NFR-02 | 零注册表、零联网；`data\` 之外零写入，唯一显式例外为缩略图缓存 | 零写入三段取证；例外清单只含一条 |
| NFR-03 | 配置只认 exe 同级 `data\`，绝不静默回落 `%APPDATA%` | 路径解析单测 |
| NFR-04 | 与资源管理器同手感（菜单、图标、类型名、拖放、快捷键） | 人工验收项写入 `CONTRIBUTING.md` |
| NFR-05 | 分层边界：危险判级只在 `SafetyGuard`；UI 不直接 `File.Copy`；`Core` 不出现 `System.IO` | `check-boundaries` 静态检查 `exit 1` |
| NFR-06 | 可构建、可验证：SDK 风格 csproj，CI 跑单测并输出退出码 | 同一份 `--selftest` 输出在旧/新工程文件下逐行 `diff` 为空 |
| NFR-07 | 公开仓内容全部可公开，无私有资产与环境偶然 | §10 扫描清单 |
| NFR-08 | 不因第三方 shell 扩展导致反复闪退；主程序异常终止后不得无日志无降级 | §5.6 哨兵门卡 |
| NFR-09 | 每条验收结论带证据等级；毫秒/时序指标只报数不判定 | §7.3 |
| NFR-10 | 高 DPI：PerMonitorV2 manifest，100/125/150/200% 四档下列宽、列头判据与几何断言不退化 | 四档对照写进 `CONTRIBUTING.md`；§8.3 H1 |
| NFR-11 | 规模与性能预算：大目录分批枚举 + 虚拟滚动；枚举 / 滚动 / 导航 / 启动四项预算须记「机器 + 环境」，毫秒只报数 | §7.3 预算表；§8.3 H5 |
| NFR-12 | 可访问性与国际化：核心操作键盘可达、焦点可见；菜单文本随系统语言，判据不得依赖中文字符串或固定 verb 文本 | 人工项 + `filter-*` / `menu-no-fusion` 单测 |
| NFR-13 | 线程模型：UI 线程只做呈现；shell COM 在独立 STA；枚举 / 图标 / 缩略图在后台 worker；不允许系统模态框抽走应用消息泵 | §5.12 静态检查 + `op-ui-no-system-modal` |
| NFR-14 | 错误与降级可观测：任何失败都有分类、用户可见文案、是否可重试、是否入撤销栈；禁止静默吞错 | §5.12 错误模型表 + `op-result-structured` |

### 2.3 约束与假设

- C-01 首版技术基线：.NET Framework 4.8 + WPF + C# 5 语法子集；工程文件改 SDK 风格，构建脚本参数化 `FrameworkPathOverride`。
- C-02 独立公开新仓；首个提交必须为规格文档，规格定稿前不写产品代码；不导入既有私有代码库资产。
- C-03 环境解绑不得改变产品行为，须用同一套单测与 `--selftest` 对拍。
- C-04 无第三方库依赖（无 NuGet）；WPF / WIC / Shell COM 均为 Windows 系统 API。
- C-05 一次只改一个功能域，不并行推进两个批次。
- C-06 假设目标系统为 Windows 10 1903+ / Windows 11、x64。系统文件操作接口不可用时，须在**启动时探测**并进入只读模式：禁用全部变更类入口、状态栏明示原因与恢复条件，不得静默失败或给出「操作成功」假象；探测方式与只读模式状态见 §5.12。此条不豁免 FR-18，只是定义其不可用时的降级形态。

---

## 3. 目标与非目标

| 目标 | 非目标（首版不做） |
|---|---|
| 一个公开、可构建、可验证的四窗格文件浏览器 | 功能扩张：内置播放器 / 预览、压缩包浏览、左导航树、全局搜索框、批量重命名、磁盘分析、云同步、深色主题、插件系统 |
| 完整覆盖已验证的核心浏览与搬运功能，并把行为契约写成可机器验证的规格 | 借重构改名或重写已验证的纯函数算法（自然排序、列宽计算、路径规范化、危险判级） |
| 把证据闭环（门卡 / 负向对照 / 单测 / 账本 / 设计决策记录）建立成可公开复现的形式 | 搬运既有私有取证资产（截图 / 取证文本 / 账本原文） |
| 解除环境偶然（硬编码路径、代理端口、构建工具链限制） | 借环境解绑顺手改产品行为 |
| 六条产品不变量固化为机器可检查的约束 | 追求「全绿」式表述；毫秒指标做回归闸门 |
| 默认系统原生右键菜单且不因扩展反复闪退 | 逐项识别 / 枚举 / 改写第三方扩展菜单项 |
| 交互边界明确 | 面包屑 / 地址栏键入定位、详细信息档框选、列头右键（列选择）、标签页右键、分组折叠、跨窗格拖标签 |
| 命名空间与媒体边界明确 | `shell:` 虚拟命名空间、控制面板 / 库 / 回收站视图、UNC 根识别（回落通用图标）；PDF/Office、webp/heic/avif、`<1 MB` 的 `.ts` 缩略图；32 位与 ARM64 |

---

## 4. 总体设计

### 4.1 架构分层

```
L0 领域层（Core · 纯函数 · 零 I/O · 可 100% 单测）
    ColumnLayout / ViewSpec / CellMetrics / MenuModel / PathKey
    SafetyGuard.Decide / SafeWalk / OpRequest / OpPlan / OpRecord / MenuTarget / Selection

L1 编排层（App · 全局单例 OperationCoordinator）
    串行闸门 → 危险判级 → 冲突/空间/只读预检 → 提交执行
    → 结构化 OpRecord → 撤销栈入栈 → 广播刷新到 N 个窗格

L2 端口层（接口 · 可替换 · 可 mock）
    IFileOperator / IClipboard / IShellVerbs / IFsWatch / IDirSource

L3 适配器层（Fs · 系统互操作唯一出入口 · 薄）
    ShellFileOperator (IFileOperation) · ShellOpen (动词)
    FileClipboard (CF_HDROP)            · NativeMenuHost (IContextMenu)
    AtomicFile · FsWatchService

L4 表现层（App · WPF 外壳，只读 Core 结果）
    PaneView / MenuLayer / Hint / SessionStore / ThumbProvider

QuadLite.Probe   测试替身与探针，不进发布包
QuadLite.Tests   零依赖 runner（首版）
```

**依赖方向**：L4 → L1 → L0；L1 只依赖 L2 接口；L3 实现 L2 接口。**L1 不得引用 L3 具体类型**。

### 4.2 边界规则（`tools/check-boundaries.ps1`）

逐条扫描 `src/**/*.cs`，违反即 `exit 1`，并接入 CI 闸门：

| 规则 | 检查 |
|---|---|
| B-01 | `Core` 不出现 `System.IO` / `System.Windows` / `Process` / `Registry` |
| B-02 | `Core` 之外只有 `Fs` 可用 `using System.IO`（Probe / Tests 白名单） |
| B-03 | `App` 不出现 `File.Copy` / `File.Move` / `File.Delete` / `Directory.*` 直调 |
| B-04 | 产品程序集 `GetCommandString` 命中 0 次（诊断工具目录白名单） |
| B-05 | XAML 不写死列宽 / 行高 |
| B-06 | 自测文件中的类型不得被产品文件引用 |
| B-07 | 所有搬运经 `IFileOperator` 端口 |

### 4.3 设计决策清单

> 只记录**当前生效的决定**与其验收判据。备选方案与取舍推演不在本文范围。

| ID | 决定 | 验收判据 | 可逆性 |
|---|---|---|---|
| DD-01 | 首版 net48 + WPF + C# 5 语法子集；SDK 风格 csproj；`FrameworkPathOverride` 参数化 | ① `--selftest` 新旧工程逐行 `diff` 为空；② `build.ps1 -SingleFile` 产出 `exe + BUILD.txt`；③ CI 输出单测退出码 | 高（升级运行时需另立决策） |
| DD-02 | 代码分批迁入公开仓，每批一个提交并附开源审查结果 | §10 扫描清单全过 | 高 |
| DD-03 | 许可证 MIT | 仓库根 `LICENSE` + README 声明；源码无内部版权头 | 高 |
| DD-04 | 代码签名使用免费 OV 证书 | `signtool sign /fd sha256 /tr … /td sha256` + `verify /pa`；指纹与时间戳进 `BUILD.txt` | 高 |
| DD-05 | 默认系统原生右键菜单；薄壳标准序列；源头消重；参数硬校验；崩溃哨兵；自绘层仅兜底 | §7.1 `menu-*` 十条 | 中（详见 §5.2） |
| DD-06 | 文件操作执行层用系统 `IFileOperation`；编排 / 撤销 / 刷新自建；接管冲突与进度 UI | §7.1 `op-*` 八条 | 中（详见 §5.1） |
| DD-07 | 核心浏览功能**沿用已验证的行为契约**（1/2/3/4 布局、七档视图、自然排序、列头筛选、显示开关、书签、快捷键、枚举分批与取消），按规格重新实现，不导入既有私有代码库源码、不借重构改名 | §7.1 功能域门卡组 + 附录 C 追踪矩阵 | 中 |
| DD-08 | 统一状态模型与错误模型：操作闸门、菜单生命周期、失败分类集中定义，UI 只消费结构化结果 | §5.12 三张表 + `op-*` / `menu-*` 门卡 | 高 |

---

## 5. 详细设计

### 5.1 多窗口编排与文件操作（DD-06）

**按层判据** —— 决定每一能力的归属，不作为可选开关：

| 能力 | 归属 |
|---|---|
| 字节搬运、回收站、ACL/ADS/稀疏文件、跨卷、长路径、UAC、占位符 hydration、`SHChangeNotify` | 必须走系统 `IFileOperation` |
| 打开 / 打开方式 / 属性 | 必须走系统动词 |
| 系统剪贴板 `CF_HDROP`（对外互通） | 必须走系统 |
| 多窗口编排、撤销栈、选中集、跨窗格语义、结构化 `OpRecord`、刷新去重、危险判定 | 必须自建 |
| 冲突 / 进度 UI | 自建非模态面板 |
| 内部拖放（窗格间） | 自建编排；仅对外拖放走 shell 数据对象 |
| 剪贴板内部表示 | 以系统格式为准，不另建待办列表 |

**`OperationCoordinator`（L1，全局单例）流程**：

```
OpRequest（含 MenuTarget 快照：源集合 / 目标窗格 / 期望动作）
  → 串行闸门（同一时刻只允许一个操作）
  → SafetyGuard.Decide（Forbidden 拒绝 / Normal 确认 / TypePath 键入名称末段）
  → 预检（同名冲突、空间、只读、占用）
  → IFileOperator.Execute（FOF_SILENT / FOF_NOERRORUI + ProgressSink）
  → 结构化 OpRecord（逐项源→真实目标、hr、失败原因）
  → 撤销栈入栈 / 广播刷新到所有窗格
  → 结果回显（成功 / 取消 / 部分失败，逐项列出，不吞错误）
```

约束：① 同一时刻只允许一个操作；② 任何删除 / 覆盖 / 跨卷移动先过 `SafetyGuard`；③ 执行完必须回报结果；④ 拿不到操作后真实位置的操作不入撤销栈，并在界面说明「本次无法撤销」。

**接管系统对话框**：应用自己预检冲突 / 只读 / 占用并绘制**非模态**进度与冲突面板；`IFileOperation` 只承担搬运，不弹系统进度框 / 冲突框。操作期间按闸门状态置灰变更类入口，导航类只读入口保持可用。这样避免系统模态框抽消息导致「界面可点 → 重复发起同一删除」，并明确多窗口下的焦点归属。

**执行契约**：每次操作新建 `IFileOperation` 实例（flags 为实例级）；在独立 STA 线程执行；删除用 `FOF_ALLOWUNDO`，永久删除加 `FOF_WANTNUKEWARNING`；撤销动作自身不加 `FOFX_ADDUNDORECORD`（不对称写入）；长路径交给 Shell 前转普通路径，超长路径如实失败并报错；撤销栈容量 20、仅本次会话、不落盘；永久删除不入栈并如实标注「不可撤销」。

**拖放分流**：窗格之间走 `OperationCoordinator`，效果由 Ctrl/Shift 修饰键在应用内判定；拖到资源管理器 / 桌面才使用 `CF_HDROP + Preferred DropEffect`，由 `DoDragDrop` 返回值判断是否需要刷新源窗格。两条路径分别可断言。

### 5.2 右键菜单子系统（DD-05）

**默认路径 = 系统原生菜单**。新写 `NativeMenuHost`（目标 ≤400 行），只走 shell 标准四步 + 消息转发：

```
右键 → 取 PIDL / IShellItem → GetUIObjectOf(IContextMenu)
     → CreatePopupMenu → QueryContextMenu(idCmdFirst=1, idCmdLast=0x7FFF, CMF_*)
     → 记录 maxId = 返回值 - 1
     → 自有项以 ID ≥ 0x8000 用分隔线独立成组追加（不与系统项同名）
     → TrackPopupMenuEx（自建 owner 窗口，转发菜单消息给 IContextMenu2/3）
     → 选中：ID ≥ 0x8000 → 自有派发（用打开右键那一刻的 MenuTarget 快照）
              ID <  0x8000 → 校验 ∈ [1, maxId] → InvokeCommand(id - idCmdFirst)
     → DestroyMenu / Release（try-finally，不缓存 IContextMenu 指针）
```

| 项 | 规定 |
|---|---|
| 自有命令 | ID ≥ `0x8000`，与 shell 段 `[1,0x7FFF]` 天然隔离。系统已有且行为等价的（打开 / 打开方式 / 属性）直接用系统项，不实现自有副本；行为不等价的（剪切 / 复制 / 删除）保留应用实现但**改名 + 独立分组**（如「复制到另一窗格…」「删除（可撤销）」）；空白面「当前文件夹属性」按例外保留并改名。菜单中不存在同名重复项 → 结构上不需要识别或删除重复项 |
| 禁止项 | 不调 `GetCommandString`、不调 `DeleteMenu`、不遍历改写菜单树、不实现 verb 识别 / 懒填子菜单 / 800ms 超时 / STA worker 池 / 负缓存 / 菜单指针缓存 |
| 参数纪律 | 产品程序集 `GetCommandString` 0 次；`InvokeCommand` 前校验 `idCmd - idCmdFirst ∈ [0, maxId]`，越界丢弃；COM 声明使用成熟绑定，不手写 vtable；诊断路径如需读 verb，须独立进程 + 先校验 ID 范围 + 超时 + 不进发布包；缓冲区按 `cchMax` 分配，Unicode 长度按字符算；每会话 try-finally 释放 HMENU / COM |
| 读取菜单文本 | 只用纯 Win32 `GetMenuItemInfoW`，不进入扩展代码 |
| 自绘层 | 仅作 L3 降级后的兜底实现，不是默认路径 |

**L3 崩溃哨兵**（§5.6）与 **L4 可选隔离宿主**（§8 可选演进）。

### 5.3 通知与刷新

全局单一 `FsWatchService`：一处订阅文件系统通知，经 `SelfOpGuard` 去重节流后广播给所有窗格，避免 N 个 watcher 各自刷新造成刷新风暴与选中集丢失。自操作抑制分两段（操作进行中 + 结束后 1 秒）且带路径前缀条件，只忽略真正碰过的路径，不吞掉用户在另一窗格同时做的外部改动。时间由调用方注入，抑制逻辑为纯函数、可 100% 单测。

### 5.4 撤销

全局 `UndoStack`（20 步、本次会话、不落盘），跨窗格共享；一批 N 个源项算一步，不逐项撤。撤销动作反向调用 `IFileOperator`。依赖 `IFileOperationProgressSink` 的 `PostDeleteItem` 取得删除项在回收站中的真实路径，拿不到则不入栈。已知限制（首版不重写）：与系统撤销记录并存且互不知情；回收站清空后对应步骤失败并留在栈上带原因；覆盖式复制的撤销只删副本、不恢复被覆盖的原内容——界面须如实说明。

### 5.5 数据与会话格式

| 数据 | 格式 | 规定 |
|---|---|---|
| `data\session.json` | `schemaVersion` 2，含 `view` / `group` / `sortField` / 列宽偏好 | 兼容旧版会话格式（`schemaVersion` 1）可读；越界一律回落默认（列宽负数 / <下限 / >2000 归 0，不是 clamp）；`schemaVersion` > 2 整份拒读（`future-version`） |
| `data\bookmarks.json` | `schemaVersion` + `items[{name,path}]`，上限 64 | 单条坏项只丢该条，不整份拒读 |
| `data\run.lock\<pid>.json` | hwnd + `WM_COPYDATA` 标记 `'QL'` | `--selftest` / `--bench` / `--shot` / `--open-test` 不参与单实例 |
| `data\menu-pending.json` | `{ ctxKey, ts, attempt }` | 见 §5.6 |
| `data\menu-safe.json` | ctxKey 列表，TTL 默认 7 天 | 见 §5.6 |
| `data\QuadLite-crash.log` | 只记类型 / 结果 / 耗时 / 项数 / 错误码 | **不含完整路径与文件名**；格式写入 `SECURITY.md` |
| `dist\single\BUILD.txt` | 编译时间 / 提交 / 工作树 / exe 大小 / sha256 / 嵌入资源 / 包自检结果 | 工作树脏照实写 + Warning |

首版不迁移 `ProfileStartupJit`：无 `%LocalAppData%` 写入，零写入例外清单只剩缩略图缓存一条。冷启动优化若需要，走机器级 `ngen install`，并在文档注明「需管理员、属部署动作，不是运行期写入」。

### 5.6 崩溃哨兵

```
弹菜单前：写 data\menu-pending.json（ctxKey = hash(扩展类别 + 文件类型 + 卷类型), ts, attempt）
菜单正常关闭（含 ESC / 点外部）：删除标记
进程异常终止：标记残留
下次启动：发现残留 →
   ① 该 ctxKey 加入 data\menu-safe.json（TTL 默认 7 天）
   ② 本地留脱敏崩溃摘要（异常码 + 故障模块名，不联网、不外传）
   ③ 该上下文右键直接走自绘菜单，状态栏写明原因 + 「重试系统菜单」按钮
   ④ 点重试 → 清 safe 项、重新走原生；再崩 → TTL 延长并保留提示
```

按 ctxKey 降级而非全局关闭；哨兵只判断「上次有没有正常收尾」，不枚举、不识别扩展、不需要 dump。不宣称「绝不崩」。

### 5.7 证据闭环的公开形态

| 形态 | 公开仓内容 | 判据 |
|---|---|---|
| 门卡 | 命名门卡 + 判据描述文本 + 可重跑脚本 | 改坏实现必须变红 |
| 负向对照 | 生成脚本与期望输出 | 每批至少 1 条实测变红 |
| 单测 | 零依赖 runner，退出码 = 失败数 | 全绿为闸门，但不写「全绿」口号 |
| 账本 | v2 自己的账本，每批一行，**必须记当次 `head_sha`** | 禁止「连跑 N 次全绿」单独作结论 |
| 设计决策记录 | `docs/adr/`，编号 `ADR-v2-0xx` | 每条含「决策 / 理由 / 判据 / 负向对照 / 状态」 |
| 截图 / 取证文本 | **不迁入**；只保留统计结论与可复现脚本 | §10 扫描命中即打回 |

### 5.8 布局、导航与会话（FR-01..08）

**窗格模型**：`PaneState` 每窗格独立持有标签组、导航历史、显示行集、排序 / 分组状态、枚举令牌、滚动锚点、列宽偏好、筛选条件与视图状态。布局切换只改变网格落位与可见性，**不销毁窗格对象**；全局唯一 `ActivePaneId`，所有快捷键与工具栏动作先解析它，不得由「当前获得焦点的控件」推断。

| 项 | 规定 |
|---|---|
| 布局 | `LayoutMode {One,Two,Three,Four}`；3 窗格用 `RowSpan=3` 落位；折叠由分隔条比例 `0/1` 推导，不另存可见性布尔 |
| 分隔条 | 比例按「相对均分偏移」存储，跨会话保持；拖到阈值即折叠；折叠态恢复不重建行集 |
| 导航 | `Navigate(path)` → 路径规范化 → 带 `generation` 的枚举请求 → 前进 / 后退栈按标签独立；**导航即清除筛选**；F5 走 `RefreshInPlace` 保视口与选中集 |
| 地址栏 | 可编辑 + 驱动器下拉；规范化覆盖长路径 / UNC / `..` 越界 / 设备命名空间 / 引号与空白；非法输入不导航并在状态栏说明原因 |
| 此电脑 / 盘符 | 由 `IDirSource` 合成条目；盘符图标按真实路径取，不得回落通用文件夹图标 |
| 书签 | 只收目录；三态 `Ok/Missing/Unknown`，失效判定在线程池；上限 64，单条坏项只丢该条不整份拒读；点击只导航活动窗格 |
| 会话 | 原子写 + `.bak` + `.bad-<时间戳>`；坏值按**字段**归一；`schemaVersion > 2` 整份拒读并回落默认；兼容旧版 `schemaVersion 1` |

**判据**：`layout-*`（四档槽位与跨度、五档窗口钉点）、`split-*`（折叠 / 吸附 / 连线数 / 状态不重建）、`tab-*`（改序不改入参、冷启动不跳最左）、`path-*`、`nav-*`、`bookmark-*`、`session-*`（坏值归一、未来版本拒读）。

### 5.9 列表、视图、列头与筛选（FR-09..14）

**目录枚举**：Win32 批量枚举（`FindFirstFileExW` + `FindExInfoBasic` + `LARGE_FETCH`），默认 4096 条 / 批；每批检查取消令牌并携带 `generation`，切目录后迟到批次一律丢弃；列表用虚拟化面板，不为全部行建立可视对象。

**七档视图**（`ViewSpec` 为唯一来源）：

| 档位 | 图标通道 | 组头 | 缩略图目标 |
|---|---|---|---|
| `Details` | 16 | 渲染 | 0 |
| `List` | 16 | 不渲染 | 0 |
| `ExtraLarge` | 256 | 不渲染 | 256 |
| `Large` | 96 | 不渲染 | 96 |
| `Medium` | 48 | 不渲染 | 48 |
| `Small` | 16 | 不渲染 | 0 |
| `Tiles` | 48 | 不渲染 | 48 |

`TargetPx` 只对需要缩略图的档位返回非零，菜单置灰、解码调度、缓存键三处都问同一个函数，不得各写一份判断。

**排序与分组**：自然排序比较器（数字按数值），列头点击切换升降序；分组先比组序、组内走原比较器，**不做二次排序**；组头只存在于显示列表，不进显示行集。

**列宽**：`ColumnLayout.For` 为唯一来源；`Σ列宽 = 行宽 − 12` 恒等式、名称列保底 120、不产生 `0 < 宽 < 下限` 的窄条；四条边界可拖、双击复位；偏好按标签一份，`0=自动`。

**列头筛选**：四列 ▾ 面板 —— 名称按拼音分桶、修改日期按月历、类型动态枚举、大小五档；同列「或」、跨列「与」。筛选结果写入**显示行集**；状态栏计数、`Ctrl+A`、拖放源、文件操作源一律读显示行集，不读全量表。箭头可见性按全量表判断。导航即清除、F5 刷新保留、**不写会话**。

**显示开关**：隐藏文件 / 系统文件 / 扩展名 / 缩略图四项，会话持久化；切换后原地刷新且保持视口；过滤判定为 `Core` 纯函数。

**判据**：`enum-*`（分批 / 取消 / 迟到批次丢弃）、`view-*`（七档覆盖、组头、双击空白 / 条目）、`sort-*` / `group-*`、`cols-*`（唯一来源、恒等式、偏好按标签）、`filter-*`（四列组合、跨列与、导航清除、F5 保留、计数一致）、`toggle-*`（三态组合、不回顶）。

### 5.10 选择、命中测试、状态栏与快捷键（FR-15..17）

**选中集**：只包含筛后可见的真实条目；组头、被筛掉的行、禁用项均不计入。`Ctrl+A` 走真实全选命令但必须排除上述三类；图标档支持框选，详细信息档框选列为非目标。

**命中测试**：单一 `HitTest(point)` 供双击、拖入、右键共用，返回 `{条目 | 空白面 | 组头 | 列头}`；图标档双击空白面与双击条目行为不同，由同一命中结果分流。

**状态栏**：显示筛后行数、选中数、操作结果与提示；文案一律来自 L1 的结构化结果，UI 不自行拼装业务结论（例如不得由「没有异常」推出「操作成功」）。

**快捷键契约**（NFR-12：核心操作全部键盘可达、焦点可见）：

| 键位 | 动作 | 作用范围 | 操作闸门忙时 |
|---|---|---|---|
| `Ctrl+C` / `Ctrl+X` / `Ctrl+V` | 复制 / 剪切 / 粘贴到系统剪贴板 | 活动窗格选中集 | 禁用并提示 |
| `Ctrl+Z` | 撤销最近一次操作 | 全局 | 禁用且不产生撤销步骤，提示语与操作结果同一句 |
| `Ctrl+A` | 全选（不含组头 / 筛掉行 / 禁用项） | 活动窗格 | 可用 |
| `Ctrl+T` / `Ctrl+Tab` | 新建标签 / 切换标签 | 活动窗格 | 可用 |
| `F2` | 重命名 | 活动窗格选中集 | 禁用并提示 |
| `F5` | 原地刷新（保视口与选中集） | 活动窗格 | 可用 |
| `Delete` / `Shift+Delete` | 删除到回收站 / 永久删除 | 活动窗格选中集 | 禁用并提示 |
| `Ctrl+Shift+N` | 新建文件夹并进入命名 | 活动窗格 | 禁用并提示 |
| `ESC` | 关闭菜单 / 取消当前选择或面板 | 当前上下文 | 可用 |

### 5.11 图标与缩略图（FR-29..31）

| 项 | 规定 |
|---|---|
| 图标通道 | 16 / 48 / 96 / 256 四级；exe 图标两段式（先扩展名通用图，再按真实路径回填）；可见区间用 `VisibleSpan` + `SpanUnit{Items,Pixels}`，避免单位混用；jumbo 返回透明画布时回落 48 档 |
| 盘符 | 每一档按真实路径取图，判据为「与通用文件夹图标逐像素不同」 |
| 图片缩略图 | 单一白名单（WIC 内置解码器全集 14 种，含 `.ts` 的 1 MB 尺寸下限）；EXIF 八值方向；可见优先单线程队列；96 MB + 1500 条双闸 LRU；缓存键四段并含类型隔离 `i|` / `v|`；开关切换不重枚举 |
| 视频缩略图 | 15 种容器走系统 `IShellItemImageFactory`；独立 worker + 8 秒超时看门狗；失败分 `Missing` / `NotImage` 负缓存且不重试 |
| 写入例外 | 视频缩略图写系统缩略图缓存是**唯一显式写入例外**；须按「注册表 / 应用目录 / 系统缓存」三段取证，且只在与系统文件管理器相同动机下写入 |

**有意缺口（不是缺陷，写入公开 README）**：PDF / Office 无缩略图；webp / heic / avif 不解（需第三方解码器，破坏零依赖）；`<1 MB` 的 `.ts` 短片无缩略图；HEVC / AV1 等解不出时回落类型图标并负缓存。

### 5.12 状态模型、错误模型与线程模型（DD-08 / NFR-13 / NFR-14 / C-06）

**状态模型**（每张表是唯一定义，UI 只消费状态，不自行推断）：

| 对象 | 状态 | 转换与约束 |
|---|---|---|
| `OperationGate` | `Idle → Prechecking → Running → Reporting → Idle`；`ReadOnly`、`Cancelled` | 全局唯一；忙时变更类入口置灰，导航类只读入口保持可用；取消须等 `Reporting` 完成后才回 `Idle` |
| `MenuSession` | `None → Pending → Tracking → Closed`；`Crashed` | `Pending` 写 `menu-pending.json`；`Closed` 删标记（ESC / 点外部都算）；残留即 `Crashed` |
| `PaneLoad` | `Idle → Enumerating(gen=n) → Ready`；`Stale` / `Error` | 只接受同代回调；`Stale` 静默丢弃不刷新 |
| `ShellAvailability` | `Probe → Available` / `ReadOnly` | 启动探测一次；`ReadOnly` 下禁用全部变更类入口并说明原因（C-06） |
| `ThumbJob` / `IconJob` | `Queued → Running → Done` / `Failed` | `Failed` 进负缓存，不无限重试 |

**错误模型**（每条失败必须有分类、用户可见文案、可重试性、是否入撤销栈）：

| 分类 | 触发 | 可重试 | 入撤销栈 | 用户可见 |
|---|---|---|---|---|
| `Cancelled` | 用户取消 / ESC | 是 | 否 | 「已取消」 |
| `PartialSuccess` | 部分项 `hr < 0` | 是（逐项） | 仅成功项 | 逐项列出源→目标与失败原因 |
| `AccessDenied` / `SharingViolation` | 权限 / 占用 | 是 | 否 | 原因 + 建议动作 |
| `NameConflict` | 同名 | 是（改名 / 覆盖 / 跳过） | 按结果 | 应用内非模态面板 |
| `PathTooLong` | 超长路径 | 否 | 否 | 如实失败，不静默截断 |
| `PlaceholderHydrationFailed` | 云占位符按需下载失败 | 是 | 否 | 说明「文件未在本地」 |
| `ShellUnavailable` | COM 不可用 / 极老系统 | 否 | 否 | 进入只读模式（C-06） |
| `ClipboardUnavailable` | 剪贴板被占用 | 是 | 否 | 说明不影响内部拖放 |
| `SessionCorrupt` | 会话 / 书签坏值 | 不适用 | 不适用 | 状态栏说明「已回落默认」 |
| `EnumFailed` | 目录枚举失败 | 是 | 否 | 保留旧行集 + 明确错误，不显示空目录 |
| `DecodeFailed` | 缩略图解码失败 | 否 | 否 | 回落类型图标 + 负缓存 |

**线程模型**（禁止系统模态框抽走应用消息泵）：

| 工作 | 线程 | 约束 |
|---|---|---|
| 呈现 / 命中测试 / 状态栏 | UI 线程 | 禁止 I/O 与长时 COM 调用 |
| 目录枚举 | 后台 worker | 回调带 `generation` 与取消令牌 |
| 文件操作 COM | 独立 STA 线程 | 每次新建 `IFileOperation` 实例；自建 `ProgressSink`；不在 UI 线程 `PerformOperations` |
| 原生菜单 COM | 独立 STA 线程 | 自建 owner 窗口并转发菜单消息；不弹系统进度 / 冲突框 |
| 图标 / 缩略图 | 后台 worker + 可见优先队列 | 必须在 UI 线程取的 Shell 图标列表用短时间片，不阻塞渲染 |
| 书签失效判定 | 线程池 | 不占 UI 线程 |

跨线程回 UI 必须携带 `generation` / 令牌并校验；任何后台结果迟到即丢弃，不得覆盖较新的用户状态。

### 5.13 便携、单实例与命令行（FR-32 / FR-33 / FR-35）

- 配置根只认 exe 同级 `data\`，**绝不静默回落**到用户目录；启动时探测可写性，不可写则进入内存会话模式并在状态栏明示；提供 `--data-dir` / `--no-persist` 两个逃生口。
- 单实例：Mutex 判「能不能起」+ `run.lock` 查「交给谁」+ `WM_COPYDATA('QL')` 转发路径；`--selftest` / `--bench` / `--shot` / `--open-test` 等诊断模式不参与单实例。命令行完整契约见 §6.4。
- 零写入例外清单**只有缩略图缓存一条**；崩溃摘要只记类型 / 结果 / 耗时 / 项数 / 错误码，不含完整路径与文件名，格式写入 `SECURITY.md`。

---

## 6. 接口定义

### 6.1 内部端口（L2）

| 端口 | 职责 | 实现 |
|---|---|---|
| `IFileOperator` | 复制 / 移动 / 删除 / 重命名，返回结构化 `OpRecord` | `ShellFileOperator` |
| `IClipboard` | 读 / 写系统剪贴板文件集合 | `FileClipboard` |
| `IShellVerbs` | 打开 / 打开方式 / 属性 | `ShellOpen` |
| `IFsWatch` | 订阅与广播文件系统通知 | `FsWatchService` |
| `IDirSource` | 目录枚举（含「此电脑」合成层） | `Fs` 层实现 |

**端口契约（签名级，L1 仅依赖此层）**：

```
OpRequest   { MenuTarget Target; OpKind Kind; IReadOnlyList<PathKey> Sources;
              PathKey Destination; CollisionPolicy Policy; }
OpRecord    { IReadOnlyList<OpItem> Items; int CallbackCount; bool Cancelled;
              ErrorClass Error; }
OpItem      { PathKey Source; PathKey ActualTarget; int hr; string Reason; }
MenuTarget  { IReadOnlyList<PathKey> Selection; PathKey Directory;
              int PaneId; long CapturedAt; }            // 不可变快照
Selection   { IReadOnlyList<PathKey> VisibleSelected; int VisibleRowCount; }

IFileOperator.Execute(OpRequest, IProgressSink, CancellationToken) -> OpRecord
IClipboard.Read() / Write(CF_HDROP + Preferred DropEffect)
IShellVerbs.Open / OpenWith / Properties(MenuTarget)
IFsWatch.Subscribe(PathKey, Action<FsEvent>) / Unsubscribe(...)
IDirSource.Enumerate(PathKey, int generation, Action<Batch>, CancellationToken)
```

`OpRecord` 的 `CallbackCount` 是失败汇总的唯一分母；`ActualTarget` 为空时该步不入撤销栈（§5.4）。任何端口不得把第三方指针、HMENU 或未校验偏移量跨层传出。

**纪律**：系统侧紧契约——L3 只调 shell 文档中必有的方法、参数全部校验，不把未校验 ID 递给第三方代码；应用内松耦合——L1 不引用 L3 具体类型。

### 6.2 外部系统契约

| 契约 | 调用序列 / 数据 |
|---|---|
| 原生菜单 | `GetUIObjectOf(IContextMenu)` → `QueryContextMenu` → `TrackPopupMenuEx`（含 `IContextMenu2/3::HandleMenuMsg2` 转发）→ `InvokeCommand` |
| 文件操作 | Shell `IFileOperation`：每次新建实例、独立 STA、`SetOperationFlags` → 入队 → `PerformOperations` → `IFileOperationProgressSink` 回调 |
| 剪贴板 | `CF_HDROP` + `Preferred DropEffect`（Copy=1 / Move=2） |
| 动词 | `ShellExecuteEx` / `SHOpenWithDialog` |
| 隔离宿主（可选） | 同一二进制 `QuadLite.exe --shell-menu-host`，命名管道只传纯数据 |

### 6.3 测试替身

`QuadLite.Probe` 提供 `FakeShellExtension`（ok / 越界 / owner-draw / 崩溃）、`FakeFileOperator`、`FakeDirSource`、`FakeFsWatch`、假宿主（`FailFast`）。全部菜单与操作门卡不依赖真机安装扩展、云盘或特定权限状态。系统模态菜单的视觉验收、高 DPI 四档对照、资源管理器双向拖放三项单列人工项，写入 `CONTRIBUTING.md`。

### 6.4 命令行与数据格式

| 参数 | 语义 | 输出 / 退出码 |
|---|---|---|
| `--selftest` | 无人工界面自测，跑全部门卡 | 每组一行 `组名: ok=True/False` + 末行 `verdict:`；退出码 = 红条数 |
| `--bench <dir>` | 枚举 / 滚动 / 导航性能采样 | 只报数，不判定；写入预算表（§7.3） |
| `--shot <path>` | 出图（可带 `--view` / `--select` / `--menu`） | PNG，供人工视觉对照 |
| `--open-test <path>` | 单实例转发路径的测试入口 | 结构化结果 |
| `--thumbs <dir>` / `--dumpmenu <path>` | 诊断工具，**不进发布包** | 文本；读 verb 前必须校验 ID 范围且有超时 |
| `--data-dir <dir>` / `--no-persist` | 便携配置逃生口 | 启动生效；`--no-persist` 不写任何配置 |
| `--shell-menu-host`（可选，B8 后） | 同一二进制的隔离宿主模式 | 命名管道纯数据；主进程监测退出码 |

诊断模式不参与单实例（§5.13）。所有参数解析为纯函数，未知参数拒绝启动并列出合法参数，不静默忽略。

**数据格式**（全部位于 `data\`，JSON 字段为规范的一部分）：

| 文件 | 字段 | 规定 |
|---|---|---|
| `session.json` | `schemaVersion`, `layout`, `splitRatios[]`, `panes[]{tabs[]{path,history,view,group,sortField,colPrefs},activeTab}`, `showHidden`, `showSystem`, `showExt`, `showThumbs` | 兼容 `schemaVersion 1`；数值越界按字段归 0（不是 clamp）；`>2` 整份拒读 `future-version`；单条坏项不整份拒读 |
| `bookmarks.json` | `schemaVersion`, `items[]{name,path}` | 上限 64；单条坏项只丢该条 |
| `run.lock\<pid>.json` | `hwnd`, `marker='QL'` | 单实例转发 |
| `menu-pending.json` | `ctxKey`, `ts`, `attempt` | §5.6 |
| `menu-safe.json` | `ctxKey[]`, TTL 默认 7 天 | §5.6 |
| `QuadLite-crash.log` | 类型 / 结果 / 耗时 / 项数 / 错误码 | 不含完整路径与文件名；格式写入 `SECURITY.md` |
| `BUILD.txt`（出包目录） | 编译时间 / 提交 / 工作树 / exe 大小 / sha256 / 嵌入资源 / 包自检结果 | 工作树脏照实写 + Warning |

---

## 7. 验收与测试

### 7.1 门卡总表

| 组 | 门卡 | 判据 |
|---|---|---|
| 菜单 | `menu-native-default` | 默认路径为系统原生菜单 |
| 菜单 | `menu-no-verb-scan` | 产品路径 `GetCommandString` 0 次 |
| 菜单 | `menu-id-range` | `InvokeCommand` 出参严格落在 `[1, maxId]` |
| 菜单 | `menu-own-id-segment` | 自有段 ≥ `0x8000`，与 shell 段无交集，派发不串 |
| 菜单 | `menu-no-fusion` | 自有项与系统项文本规范化后无同名；产品路径 0 次删项 / 插项洗牌 |
| 菜单 | `menu-msg-forward` | `IContextMenu2/3` 消息转发覆盖 owner-draw 与菜单选择 |
| 菜单 | `menu-target-snapshot` | 派发使用打开右键那一刻的快照，不用实时路径 |
| 菜单 | `menu-sentinel` | 标记残留 → 按 ctxKey 降级 → 一键重试 → 再崩 TTL 延长 |
| 菜单 | `menu-single-file` | 发布包仍为单 exe、零安装、免运行时 |
| 菜单 | `menu-release` | HMENU / COM 每会话 try-finally 释放 |
| 操作 | `op-native-exec` | 所有搬运经 `IFileOperator`；UI/App 层 `File.Copy/Move/Delete`、`Directory.Move` 命中 0 |
| 操作 | `op-coordinator-global` | 四窗格同目录时一次操作只触发一次刷新；抑制器全局一份 |
| 操作 | `op-ui-no-system-modal` | 操作期间无系统进度框 / 冲突框；应用内非模态面板可用；变更类入口置灰 |
| 操作 | `op-result-structured` | 部分成功 / 自动重命名时产出逐项 `OpRecord`（源→真实目标、hr、原因） |
| 操作 | `op-undo-cross-pane` | A 窗格操作、B 窗格可撤销；撤销反向调 `IFileOperator` |
| 操作 | `op-drop-internal-vs-external` | 内部走编排、对外走 `CF_HDROP`，两条路径分别可断言 |
| 操作 | `op-fake-operator` | 全部门卡可用 `FakeFileOperator` 跑通 |
| 操作 | `op-single-fswatch` | 一次批量操作通知去重后广播一次，无刷新风暴 |
| 操作 | `name-*` | 冲突预检、同目录副本命名、新建文件夹重名递增、文件名校验 |
| 操作 | `refresh-*` | 单一订阅 + 去抖 + 自操作抑制；刷新保持视口与选中集 |
| 操作 | `clipboard-*` | 与资源管理器双向 `CF_HDROP` + `Preferred DropEffect` 互通 |
| 布局导航 | `layout-*` / `split-*` / `tab-*` / `path-*` / `nav-*` / `bookmark-*` / `session-*` | 四档布局含 3 窗格 `RowSpan`、折叠不重建、路径规范化、导航只作用活动窗格、书签三态、会话坏值归一与未来版本拒读 |
| 列表视图 | `enum-*` / `view-*` / `sort-*` / `group-*` / `cols-*` / `filter-*` / `toggle-*` | 分批与迟到批次丢弃、七档覆盖、自然排序、组头不进选中集、列宽恒等式、跨列「与」、导航清筛选 F5 保留、四开关不回顶 |
| 选择交互 | `select-*` / `hit-*` / `status-*` / `keys-*` | Ctrl+A 排除三类非条目、双击/拖入/右键共用命中、状态栏按筛后行计数、快捷键表逐键与忙时行为 |
| 图标缩略图 | `icon-*` / `thumb-*` / `thumb-nowrite` | 四级通道、盘符逐像素差异、白名单与 `.ts` 门槛、EXIF 八值、LRU 双闸、切换不重枚举、缓存写入只在允许时机 |
| 便携与命令行 | `portable-*` / `cli-*` / `single-instance-*` | 配置根唯一、不可写降级明示、未知参数拒绝启动、诊断模式不参与单实例 |
| 安全与状态 | `safety-*` / `state-*` / `error-*` | 危险判级矩阵逐格、闸门状态转换、错误分类与「是否入撤销栈」逐类可断言 |
| 通用 | `check-boundaries` / `check-trace` | §4.2 七条规则全绿；附录 C 追踪矩阵中每个 `FR-xx` 都有门卡与批次，且无孤立门卡 |

### 7.2 负向对照

**按不变量覆盖，不按门卡逐条配**：每条不变量至少一条负向对照（故意破坏该不变量，对应门卡必须变红），并在 `docs/negative-controls.md` 登记「不变量 → 负向对照用例 → 应变红的门卡」三列。理由：门卡数量随实现增长，要求「每个门卡一条」会把验收成本推到不可维护，而同一不变量常由多个门卡共同守护。

示例：塞一处 `File.Copy` 直调 → `op-native-exec` 红；每窗格各持一份抑制器 → `op-coordinator-global` 红；故意让系统弹冲突框 → `op-ui-no-system-modal` 红；自有项与系统项同名 → `menu-no-fusion` 红；把 3 窗格落位改成 4 窗格 → `layout-*` 红；让筛选后的行参与 Ctrl+A → `select-*` 与 `status-*` 红；假宿主 `FailFast` → 隔离宿主门卡验证主进程存活。

### 7.3 CI 闸门

静态边界检查（`check-boundaries` + `check-trace`）+ 单测 + `--selftest`。**毫秒 / 时序门卡只报数不判定**，判定交给结构性判据（如 `switchShell < rows/4`、`noReenum=True`）。每个验收结论带证据等级；未跑过的一律写「未验证」。

**性能预算表**（NFR-11）：枚举 / 滚动 / 导航 / 启动四项，每行必须记 `机器 + 系统版本 + 文件系统 + DPI + 负载规模 + 实测值 + 证据等级`；只用于跨版本阅读，不作为 CI 闸门。高 DPI 四档（NFR-10）另出对照记录，几何断言在四档下都需通过。

---

## 8. 实施计划

### 8.1 迁移批次（S4 起）

> 每批 = 一组功能域 + 对应门卡 + 负向对照 + 单测 + 账本一行 + 一条设计决策记录。一次一处。

| 批次 | 交付内容 | 门卡 |
|---|---|---|
| B0 | 工程骨架、六条不变量的静态检查、`check-trace`、CI 跑单测；**补全** S3 已提交的 README / CONTRIBUTING / SECURITY 中的构建与验收章节（LICENSE / CODE_OF_CONDUCT 在 S3 已定稿，不重复创建） | `check-boundaries` 与 `check-trace` 全绿；单测退出码 0 |
| B1 | L0 纯函数层：ColumnLayout / ViewSpec / CellMetrics / MenuModel / PathKey / SafetyGuard / SafeWalk / OpPlan / OpRecord / Selection / 状态与错误模型 | `cols-*` / `path-*` / `enum-*` 断言、自然排序、危险矩阵逐格、SafeWalk 四道边界、`state-*` / `error-*` 枚举完备性 |
| B2 | L2/L3 与编排：AtomicFile / `ShellFileOperator` / 撤销栈 / IDirSource / `OperationCoordinator` / 结构化 `OpRecord` / 单一 `FsWatchService` / 端口可 mock / 接管冲突与进度 UI / CLI 与便携 | `op-*` 八条 + `clipboard-*` / `cli-*` / `portable-*` / `refresh-*` / `name-*` / `safety-*`；失败汇总只看 `hr>=0`、分母=回调数；不对称写入有单测 |
| B3 | L4 外壳：1/2/3/4 布局 / 地址栏 / 导航 / 标签 / 会话 / 分隔条 / 书签 / 此电脑层 | `layout-*` 七条 + 三条负向（含 3 窗格）、`split-*` 九条 + 五条负向、`tab-*` 九条、`path-*` / `nav-*` / `bookmark-*` / `session-*` |
| B4 | 列表 / 七档视图 / 自然排序 / 分组 / 列头拖宽与筛选 / 显示开关 / 选择 / 命中 / 状态栏 / 快捷键 | `view-*` / `sort-*` / `group-*` / `cols-*` / `filter-*` / `toggle-*` / `select-*` / `hit-*` / `status-*` / `keys-*`；筛选后行集与计数/Ctrl+A/拖放源一致 |
| B5 | 系统原生右键菜单：`NativeMenuHost` 薄壳、源头消重、参数硬校验、崩溃哨兵、自绘兜底 | `menu-*` 十条 + 负向对照；`FakeShellExtension` 不依赖真机装扩展 |
| B6 | 图标 / 缩略图 / 缓存 | 16/48/96/256 四级通道、缓存键四段、开关不重枚举；视频写 thumbcache 为唯一显式例外 |
| B7 | 自测装置、门卡总表、账本、出包包自检 | `--selftest` 输出带 `verdict:` 且 ≥100 行；`BUILD.txt` 字段齐；毫秒门卡只报数 |
| B8 | 发布线（§8.3） | H1–H6 |
| B8 之后（可选） | 同 exe 双模式隔离宿主 `QuadLite.exe --shell-menu-host`（同一二进制，单 exe 不破）；仅哨兵触发过或手动开启时启用 | `menu-host-crash`：假宿主 `FailFast` → 主进程存活、弹自绘、哨兵落标记 |

### 8.2 每批 DoD

① 静态边界检查绿；② 该批门卡全绿 + 至少 1 条负向对照实测变红；③ 单测全绿并记入账本，账本 `head_sha` 必须等于当次提交；④ 设计决策记录写清「沿用 / 修订 / 作废」；⑤ §10 开源审查清单通过。

### 8.3 发布线

```
C4（带杀软的 VM / 第二台机器；AVcount≠0 才可验证「零误报」）
 └─ H1（高 DPI：manifest + 截图装置，100/125/150/200% 四档对照）
     └─ H3（零写入三段取证）
         └─ H2（兼容矩阵：Win11 VM / LTSC / NTFS·exFAT·SMB·只读载体）
             └─ H4（崩溃摘要脱敏 + Windows 错误报告读法）
                 └─ H5（性能回归表：必须记「机器 + 环境」，不只记一个数）
                     └─ H6（免费 OV 签名，指纹进 BUILD.txt）
```

C4 缺位时按降级路径先做 H1 → H3 → H2，并在发布记录显式写「C4 未做」，不许用「没报错」冒充「没被拦」。

### 8.4 里程碑、依赖与 MVP

> C-05 规定一次只改一个功能域，故批次**串行推进**；下图只表达逻辑依赖，不表达并行。

```
B0 骨架 ─→ B1 纯函数 ─→ B2 编排/操作 ─→ B3 外壳 ─→ B4 列表/视图
                                                        │
                                                        └→ B5 原生菜单 ─→ B6 图标缩略图 ─→ B7 自测/出包 ─→ B8 发布线
```

| 里程碑 | 包含批次 | 可交付判据 |
|---|---|---|
| M0 规格定稿 | S3 | 公开仓首个提交全是文档，0 行产品代码；§10 清单全过 |
| **M1 MVP** | B0–B5 | 能启动、能浏览（1/2/3/4 窗格 + 七档视图 + 筛选）、能搬运与撤销、能弹系统原生右键菜单、`check-boundaries` 与 `check-trace` 绿、门卡与负向对照覆盖表齐 |
| M2 体验完备 | B6–B7 | 图标与缩略图、自测装置、账本、单文件出包自检 |
| M3 可发布 | B8 | 发布线 H1–H6 逐项有证据；C4 缺位显式写「未做」 |

每批 DoD 见 §8.2；**每批结束时应用必须可启动**（B1 纯函数层以单测 runner 与 `--selftest` 为可运行形态，不要求 UI 可交互），且 `check-trace` 中该批对应 `FR-xx` 的门卡必须已实现或显式标「未验证」。

---

## 9. 风险与缓解

| 风险 | 影响 | 缓解 | 相关章节 |
|---|---|---|---|
| 第三方 shell 扩展在标准序列中异常 | 主进程异常终止 | 薄壳不调可选接口 + 参数硬校验 + 崩溃哨兵按 ctxKey 降级；可选 L4 隔离宿主 | §5.2 / §5.6 |
| 手写 COM 声明布局错误 | 非托管访问违规、无托管异常 | 使用成熟 COM 绑定，不手写 vtable；见 §11 待决事项 | §5.2 / §6.1 |
| 系统文件操作结果不可观测 | 自动重命名 / 部分成功无法结构化 | `ProgressSink` 回调 → `OpRecord`；拿不到真实位置不入撤销栈 | §5.1 / §5.4 |
| 多窗格刷新风暴与选中集丢失 | 状态不一致 | 单一 `FsWatchService` + 两段带前缀的抑制窗口 | §5.3 |
| 系统模态框抽消息导致重复操作 | 重复删除 / 覆盖 | 接管冲突与进度 UI，非模态面板 + 闸门置灰 | §5.1 |
| 系统文件操作不可用（COM 异常 / 极老系统） | 功能缺失 | `IsSupported=false` 时退化为只读并明确提示 | §2.3 C-06 |
| 长路径 / 云占位符等边界 | 丢属性或失败 | 交给 Shell 前转普通路径，超长如实失败并报错；枚举层由 `SafeWalk` 处理 | §5.1 |
| 公开仓带入私有资产 | 合规事故 | §10 每批提交前扫描，命中即打回 | §10 |
| 测试替身与真机行为不一致（假件永远「正常」） | 门卡绿但真机仍出问题 | 每条门卡标「假件可验证 / 需真机」两类；系统模态菜单、DPI、拖放、扩展组合列人工项；不把假件结果写成真机结论 | §6.3 / §7.2 |
| 开源后用户机器 shell 扩展组合无限 | 个别上下文重复异常 | 崩溃哨兵按 ctxKey 自适应降级，不依赖识别具体扩展 | §5.6 |
| 大目录 / 长路径 / 云占位符 | 界面卡顿或误报空目录 | 分批枚举 + 迟到批次丢弃 + `EnumFailed` 保留旧行集并明确报错（§5.12） | §5.9 / §5.12 |
| 会话 / 书签从旧版本读入坏值 | 启动失败或状态错乱 | 按字段归一、单条坏项隔离、`future-version` 拒读并回落默认 | §5.8 / §6.4 |
| 单 exe 内嵌程序集被启发式误判 | 用户下载被拦 | 发布线 C4（带杀软环境）+ 单文件 / 多文件对比；未签名阶段显式写「未验证」 | §8.3 |
| 签名证书不可用或签名链路失败 | 无法交付可信包 | `BUILD.txt` 如实记录未签名 + Warning，不用「未报错」冒充通过 | §8.3 / §5.13 |
| net48 / 系统 API 在未来版本变化 | 运行期能力缺失 | 启动时探测 `ShellAvailability`（§5.12），能力缺失进只读模式并说明 | §2.3 C-06 |

---

## 10. 开源合规清单（S3 与每批提交前执行）

- [ ] `LICENSE`（MIT）、`README.md` 顶部声明、`CONTRIBUTING.md`、`SECURITY.md`、`CODE_OF_CONDUCT.md`
- [ ] 无既有私有资产：截图、取证 `.txt`、`run-ledger.csv` 原文、`BUILD.txt`、任何二进制产物
- [ ] 无环境偶然：内部绝对路径、代理端口、凭据写法、机器名 / 用户名、真实桌面路径
- [ ] 无失效引用与旧口号（「运行期写入为 0」「全绿」「包目录只有 exe」）
- [ ] 每条结论带证据等级；推断级不作判据
- [ ] 第三方资源说明：无 NuGet 依赖；WPF / WIC / Shell COM 均为系统 API；测试夹具现场生成，仓库不放二进制
- [ ] 菜单专项：产品程序集 `GetCommandString` 命中 0 次；`InvokeCommand` 出参过 `[0,maxId]` 断言
- [ ] CI：静态边界检查 + 单测 + `--selftest`（不含毫秒门卡判定）
- [ ] **内部分析记录与历史过程稿整份排除**，不得作为公开仓内容或引用来源；本文不引用任何不随仓发布的文档
- [ ] `check-trace`：附录 C 中每个 `FR-xx` 都有对应门卡与批次，无孤立门卡、无无主需求
- [ ] **S3 文件集合一致**：首个提交只包含下列纯文本文件，无产品代码、无二进制、无取证资产

**S3 首个提交的文件集合**（修正 1.0 版中「仅本文一个文件」与本节清单要求仓库根基础文档并存的矛盾）：

```
design-v2.md            # 规格（本文）
README.md               # 产品定位、能力边界、明确不做清单
LICENSE                 # MIT
CONTRIBUTING.md         # 构建、验收、人工验收项
SECURITY.md             # 日志脱敏、崩溃摘要格式、漏洞上报
CODE_OF_CONDUCT.md
```

`docs/adr/` 由后续批次随第一个设计决策记录创建，不在 S3 提交。

**S3 触发条件**：本文定稿 → 本清单逐条通过 → 上述文件集合作为公开仓首个提交（仓内只有文档，0 行产品代码）。

---

## 11. 待决事项

| ID | 事项 | 不决时的默认执行 |
|---|---|---|
| O-01 | 是否把 shell 互操作统一收进「成熟 COM 绑定 / 原生薄层 SEH」（跨 B2 + B5 一次完成），以统一处理两类非托管访问违规风险 | 各批次分别使用成熟绑定 + 参数硬校验；功能与门卡不受影响 |
| O-02 | 是否启用 L4 同 exe 隔离宿主（B8 之后的可选演进） | 不启用；L1–L3 已覆盖默认路径 |
| O-03 | 是否把剪切 / 复制 / 删除统一改走系统命令并把撤销栈接到 shell 操作上 | 保持 §5.2 的改名分组方案与自建撤销栈 |
| O-04 | 是否彻底抹除公开仓 force push 前的悬空提交对象（删库重建同名公开空仓） | 不执行；当前仓库 0 文件，残留对象不在任何 ref 下 |
| O-05 | 是否保留 **3 窗格布局**（本版按「保留」写进 FR-01 / §5.8 / `layout-*`） | 保留：与已验证行为一致，且 `LayoutMath` 的槽位 / 跨度与单测可复用；若要砍，须同时移入 §3 非目标并删相应判据 |
| O-06 | 列头筛选（FR-13）与显示开关（FR-14）是否全部进首版 | 全部进：二者是「按筛后行集计数 / Ctrl+A / 拖放源」一致性的前提，拆开做会产生半成品状态模型；若缩减须同步删 FR 与门卡 |

---

## 附录 A 术语与编号

需求 `FR-xx`（按 6 个功能域分组：A 布局导航 / B 列表视图 / C 文件操作 / D 右键菜单 / E 图标缩略图 / F 系统工程）、`NFR-xx`、约束 `C-xx`；设计决策 `DD-xx`；门卡按功能域前缀 `menu-*` / `op-*` / `layout-*` / `split-*` / `tab-*` / `path-*` / `nav-*` / `bookmark-*` / `session-*` / `enum-*` / `view-*` / `sort-*` / `group-*` / `cols-*` / `filter-*` / `toggle-*` / `select-*` / `hit-*` / `status-*` / `keys-*` / `icon-*` / `thumb-*` / `safety-*` / `state-*` / `error-*` / `portable-*` / `cli-*`；批次 `B0`–`B8`；里程碑 `M0`–`M3`；发布阶段 `H1`–`H6`；待决事项 `O-xx`；分析记录中的裁定编号不进入本文。

## 附录 B 公开版能力边界措辞

右键菜单：

> QuadLite 右键使用 Windows 系统原生菜单，会加载本机注册的 shell 扩展。QuadLite 只按 shell 标准接口调用，不枚举、不改写菜单项。若某次右键因扩展异常未能正常结束，会在下次启动时对该类文件临时改用内置菜单并说明原因，可一键恢复。

文件操作与撤销：

> 文件的复制、移动、删除由 Windows 系统文件操作引擎执行，因此 ACL、备用数据流、长路径、回收站与云盘占位符的行为与系统文件管理器一致。QuadLite 自己维护跨窗格的操作编排与撤销：撤销仅覆盖本次会话内、且能确定结果位置的操作；永久删除、覆盖式复制与被系统回收站清空的操作不可撤销，界面会如实说明。

便携与写入：

> QuadLite 不写注册表、不联网、不安装服务，配置只保存在程序同级的 `data\` 目录。唯一例外是视频缩略图会写入 Windows 系统缩略图缓存（与系统文件管理器同源）。配置目录不可写时程序以内存模式运行并在状态栏明示，不会静默改写到其他位置。

已知边界：

> 首版不提供全文搜索、面包屑、压缩包浏览、内置预览；PDF / Office 与部分新图像格式没有缩略图；UNC 共享根回落为通用文件夹图标；系统文件操作接口不可用时程序以只读模式运行。

不得写成「不加载第三方扩展」「运行期写入为 0」「绝不崩溃」「与系统文件管理器完全一致」，也不得宣称未验证的兼容性或性能结论。

## 附录 C 需求追踪矩阵

> 用途：保证「需求 → 设计 → 门卡 → 批次」双向可核对。`tools/check-trace.ps1` 扫描本文，任何 `FR-xx` 缺少门卡或批次、任何门卡组无对应需求，均 `exit 1`，接入 CI。

| 组 | 需求 | 设计章节 | 门卡组 | 批次 | 里程碑 |
|---|---|---|---|---|---|
| A 布局导航 | FR-01..08 | §5.8、§6.4 | `layout-*` `split-*` `tab-*` `path-*` `nav-*` `bookmark-*` `session-*` | B3（FR-03/07 部分在 B1/B2） | M1 |
| B 列表视图 | FR-09..17 | §5.9、§5.10 | `enum-*` `view-*` `sort-*` `group-*` `cols-*` `filter-*` `toggle-*` `select-*` `hit-*` `status-*` `keys-*` | B1（纯函数）、B4 | M1 |
| C 文件操作 | FR-18..25 | §5.1、§5.3、§5.4、§5.12 | `op-*` `clipboard-*` `name-*` `safety-*` `refresh-*` `state-*` `error-*` | B1（模型）、B2 | M1 |
| D 右键菜单 | FR-26..28 | §5.2、§5.6 | `menu-*` | B5 | M1 |
| E 图标缩略图 | FR-29..31 | §5.11 | `icon-*` `thumb-*` `thumb-nowrite` | B6 | M2 |
| F 系统工程 | FR-32..36 | §5.5、§5.13、§6.4、§7 | `portable-*` `cli-*` `single-instance-*` `check-boundaries` `check-trace` | B0、B2、B7、B8 | M1 / M2 / M3 |
| 非功能 | NFR-01..14 | §2.2、§4.2、§5.12、§7.3、§8.3、§10 | `check-boundaries`、预算表、四档 DPI 对照、§10 扫描清单 | B0、B7、B8 | M1 / M3 |
| 约束 | C-01..06 | §2.3、§5.12 | §10 扫描清单、`state-*` | 全批次 | 全里程碑 |

**逐条映射规则**：§2.1 每行的「验收指向」列给出该 `FR-xx` 的设计章节与门卡；§8.1 每批的「门卡」列给出该批必须实现的门卡组。二者与本节组级矩阵必须三方一致；新增需求时同步改三处，否则 `check-trace` 失败。明确不做项（§3）不分配门卡，但必须在 `README.md` 中列出。
