using System;
using System.Collections.Generic;
using System.Threading;
using QuadLite.Core;

namespace QuadLite.Fs
{
    /// <summary>
    /// 搬运端口的 Fs 层实现：只做请求校验、快照一致性与结构化结果回传。
    /// 真实的 IFileOperation 调用（新实例 / 独立 STA / flags / ProgressSink）由注入的
    /// IShellTransport 承担。规格 §5.2 禁止手写 COM vtable，故本批不声明接口布局；
    /// 成熟绑定选定前，该调用点标「未实现 / 未验证」，不冒充能搬运文件。
    /// </summary>
    public sealed class ShellFileOperator : IFileOperator
    {
        private readonly IShellTransport transport;
        private int instanceCount;

        public ShellFileOperator(IShellTransport transport)
        {
            this.transport = transport;
        }

        public int InstanceCount { get { return instanceCount; } }

        public OpRecord Execute(OpRequest request, IProgressSink sink, CancellationToken token)
        {
            OpPlan plan;
            if (!OpPlanBuilder.TryBuild(request, out plan))
            {
                return new OpRecord(new List<OpItem>(), 0, false, ErrorClass.AccessDenied);
            }
            if (transport == null)
            {
                return new OpRecord(new List<OpItem>(), 0, false, ErrorClass.ShellUnavailable);
            }
            instanceCount++;  // 每次操作新建执行实例，flags 为实例级
            return transport.Execute(request, sink, token);
        }

        /// <summary>删除用 FOF_ALLOWUNDO；撤销动作自身不加 FOFX_ADDUNDORECORD。</summary>
        public static bool NeedsAllowUndo(OpKind kind)
        {
            return kind == OpKind.Delete || kind == OpKind.Recycle;
        }

        public static bool IsUndoRecordAsymmetric(bool isUndoAction)
        {
            return isUndoAction;
        }
    }
}
