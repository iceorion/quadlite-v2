using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using QuadLite.Core;

namespace QuadLite.Fs
{
    /// <summary>
    /// R2 真实搬运的静态契约（零 I/O 纯函数，供门卡与产品 --selftest 复用）。
    /// 这里只描述「应当如何执行」，不宣称已经执行过：IsRuntimeVerified 恒为 false。
    /// </summary>
    public static class ShellContractRules
    {
        public static bool RequiresStaThread() { return true; }
        public static bool NewInstancePerOperation() { return true; }
        public static bool UndoRecordAsymmetric(bool isUndoAction) { return isUndoAction; }

        /// <summary>§5.1：删除用 FOF_ALLOWUNDO；永久删除加 FOF_WANTNUKEWARNING；NO_UI 阻止系统模态框。</summary>
        public static uint FlagsFor(OpKind kind, bool isUndoAction)
        {
            uint flags = ShellFlags.NoUi;
            if (kind == OpKind.Delete || kind == OpKind.Recycle) flags |= ShellFlags.AllowUndo;
            if (kind == OpKind.PermanentDelete) flags |= ShellFlags.WantNukeWarning;
            // 撤销动作自身不加 FOFX_ADDUNDORECORD（不对称写入）
            if (isUndoAction && (flags & ShellFlags.AddUndoRecord) != 0)
                flags = flags & ~ShellFlags.AddUndoRecord;
            return flags;
        }

        public static bool IsRuntimeVerified() { return false; }
        public static string DeclarationFile() { return "src/QuadLite.Fs/ShellComInterop.cs"; }
        public static int DeclaredFileOperationMethods() { return ShellIds.FileOperationMethods; }
        public static int DeclaredProgressSinkMethods() { return ShellIds.ProgressSinkMethods; }
        public static int DeclaredShellItemMethods() { return ShellIds.ShellItemMethods; }
    }

    /// <summary>
    /// IShellTransport 的首个真实实现：把 OpRequest 交给系统 IFileOperation。
    ///
    /// 默认 Enabled=false，只有显式 Enable 才会触碰 COM；这样 --selftest 与 CI 的
    /// 无交互桌面不会意外发起真实文件搬运。启用后每次操作新建实例、在独立 STA
    /// 线程执行、flags 实例级、逐项结果经 ProgressSinkBridge 回传、try-finally
    /// 释放全部 RCW。
    ///
    /// 运行期行为（真实 shell 可用性、回调时序、回收站真实路径、Join 超时与
    /// 崩溃哨兵）全部标「未实现 / 未验证」，见 docs/adr/ADR-v2-012.md。
    /// </summary>
    public sealed class ShellTransport : IShellTransport
    {
        private bool enabled;

        public bool Enabled { get { return enabled; } }
        public void Enable() { enabled = true; }

        public OpRecord Execute(OpRequest request, IProgressSink sink, CancellationToken token)
        {
            OpPlan plan;
            if (!OpPlanBuilder.TryBuild(request, out plan))
                return Failure(ErrorClass.AccessDenied);
            if (!enabled)
                return Failure(ErrorClass.ShellUnavailable);
            if (token.IsCancellationRequested)
                return new OpRecord(new List<OpItem>(), 0, true, ErrorClass.Cancelled);

            OpRecord[] result = new OpRecord[1];
            Thread worker = new Thread(delegate()
            {
                result[0] = RunOnSta(plan, sink, token);
            });
            worker.SetApartmentState(ApartmentState.STA);
            worker.IsBackground = true;
            worker.Start();
            worker.Join();
            return result[0] ?? Failure(ErrorClass.ShellUnavailable);
        }

        private static OpRecord Failure(ErrorClass error)
        {
            return new OpRecord(new List<OpItem>(), 0, false, error);
        }

        private static OpRecord RunOnSta(OpPlan plan, IProgressSink sink, CancellationToken token)
        {
            object created = null;
            IFileOperation op = null;
            ProgressSinkBridge bridge = null;
            List<IShellItem> items = new List<IShellItem>();
            uint cookie = 0;
            bool advised = false;
            try
            {
                // 每次操作新建 IFileOperation 实例，flags 为实例级
                created = Activator.CreateInstance(
                    Type.GetTypeFromCLSID(new Guid(ShellIds.FileOperationClsid)));
                op = (IFileOperation)created;
                op.SetOperationFlags(ShellContractRules.FlagsFor(plan.Kind, false));

                bridge = new ProgressSinkBridge(sink, plan.Sources);
                if (op.Advise(bridge, out cookie) < 0)
                    return Failure(ErrorClass.ShellUnavailable);
                advised = true;

                IShellItem destination = null;
                if (plan.Destination != null)
                {
                    destination = ShellItems.Create(plan.Destination.Value);
                    items.Add(destination);
                }
                for (int i = 0; i < plan.Sources.Count; i++)
                {
                    if (token.IsCancellationRequested) break;
                    IShellItem source = ShellItems.Create(plan.Sources[i].Value);
                    items.Add(source);
                    Queue(op, plan, source, destination, i);
                }

                int performHr = op.PerformOperations();
                int aborted = 0;
                op.GetAnyOperationsAborted(out aborted);
                bool cancelled = aborted != 0 || token.IsCancellationRequested;
                if (sink != null) sink.OnDone(cancelled);

                ErrorClass error = ErrorClass.None;
                if (performHr < 0) error = ErrorClass.AccessDenied;
                else if (cancelled) error = ErrorClass.Cancelled;
                return new OpRecord(bridge.Items, bridge.CallbackCount, cancelled, error);
            }
            catch (Exception)
            {
                return Failure(ErrorClass.ShellUnavailable);
            }
            finally
            {
                if (advised)
                {
                    try { op.Unadvise(cookie); } catch (Exception) { }
                }
                for (int i = 0; i < items.Count; i++)
                {
                    try { Marshal.ReleaseComObject(items[i]); } catch (Exception) { }
                }
                if (op != null)
                {
                    try { Marshal.ReleaseComObject(op); } catch (Exception) { }
                }
                else if (created != null)
                {
                    try { Marshal.ReleaseComObject(created); } catch (Exception) { }
                }
            }
        }

        private static void Queue(IFileOperation op, OpPlan plan, IShellItem source,
                                  IShellItem destination, int index)
        {
            string leaf = plan.Sources[index].GetLeafName();
            switch (plan.Kind)
            {
                case OpKind.Copy:
                    op.CopyItem(source, destination, null, null);
                    break;
                case OpKind.Move:
                case OpKind.CutPaste:
                    op.MoveItem(source, destination, null, null);
                    break;
                case OpKind.Delete:
                case OpKind.Recycle:
                case OpKind.PermanentDelete:
                    op.DeleteItem(source, null);
                    break;
                case OpKind.Rename:
                    op.RenameItem(source, leaf, null);
                    break;
                case OpKind.NewFolder:
                    op.NewItem(destination, 0x10, leaf, null, null);
                    break;
            }
        }
    }
}
