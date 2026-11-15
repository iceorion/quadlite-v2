using System;
using System.Collections.Generic;
using QuadLite.Core;
using QuadLite.Fs;

namespace QuadLite.Host
{
    /// <summary>
    /// R3：把 R2 的 IShellTransport 真正挂到用户可触发的命令上。
    ///
    /// 调用链固定为：OpRequest（右键那一刻的不可变快照） → OperationCoordinator
    /// → ShellFileOperator → ShellTransport → 系统 IFileOperation。UI 不自建路径、
    /// 不直接调 COM、不自行拼装业务结论；状态文案一律走 StatusRules.ResultText。
    ///
    /// transport 默认关闭（R2 纪律）：未显式 Enable 时命令直接返回「搬运未执行 /
    /// 未验证」，绝不把「没有异常」写成成功。真实 shell 运行期行为、回收站路径、
    /// 回调时序与崩溃哨兵全部未验证，见 docs/adr/ADR-v2-013.md。
    /// </summary>
    public sealed class BrowseCommands
    {
        private readonly ShellTransport transport;
        private readonly ShellFileOperator fileOperator;
        private readonly OperationCoordinator coordinator;

        public BrowseCommands() : this(null) { }

        public BrowseCommands(Action<FsEvent> onRefresh)
        {
            transport = new ShellTransport();
            fileOperator = new ShellFileOperator(transport);
            coordinator = new OperationCoordinator(fileOperator,
                new HostDangerPolicy(), new HostPreflight(),
                new HostRefreshSink(onRefresh), new HostClock());
        }

        public ShellTransport Transport { get { return transport; } }
        public bool TransportEnabled { get { return transport.Enabled; } }
        public int ExecuteCount { get { return coordinator.ExecuteCount; } }

        /// <summary>
        /// 请求构造与硬校验：源集合必须与同一时刻的目标快照逐项一致，
        /// 需要落点的操作必须有目标目录。纯内存，供产品 --selftest 直接复用。
        /// </summary>
        public static bool TryBuildRequest(OpKind kind, IList<PathKey> selection,
                                           PathKey directory, PathKey destination,
                                           int paneId, out OpRequest request)
        {
            request = null;
            if (selection == null || selection.Count == 0 || directory == null) return false;
            long capturedAt = new HostClock().NowTicks();
            MenuTarget target = new MenuTarget(selection, directory, paneId, capturedAt);
            request = new OpRequest(target, kind, selection, destination, CollisionPolicy.Ask);
            OpPlan plan;
            return OpPlanBuilder.TryBuild(request, out plan);
        }

        /// <summary>
        /// 唯一的搬运入口。默认关闭时立刻返回明确文案；启用后也只把结构化结果
        /// 转成状态栏文本，不对同手感作任何正面宣称。
        /// </summary>
        public string Execute(OpKind kind, IList<PathKey> selection, PathKey directory,
                              PathKey destination, int paneId)
        {
            if (!transport.Enabled)
            {
                return StatusRules.ResultText(
                    "搬运未执行：真实 IFileOperation transport 默认关闭（未启用 / 未验证）");
            }

            OpRequest request;
            if (!TryBuildRequest(kind, selection, directory, destination, paneId, out request))
            {
                return StatusRules.ResultText(
                    "搬运未执行：选择集为空或目标快照不自洽（未验证）");
            }

            CoordinatorResult result = coordinator.Execute(request);
            if (result == null || !result.Accepted)
            {
                return StatusRules.ResultText(
                    "搬运未执行：编排层未接受请求（未验证）");
            }
            if (result.Record == null || result.Error != ErrorClass.None)
            {
                string detail = result.Record == null ? "空结构化结果" : result.Error.ToString();
                return StatusRules.ResultText(
                    "搬运未成功：" + detail + "（运行期未验证）");
            }
            return StatusRules.ResultText(
                "搬运完成：items=" + result.Record.Items.Count.ToString() + "（运行期未验证）");
        }
    }
}
