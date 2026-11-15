using System;
using QuadLite.Core;

namespace QuadLite.Host
{
    /// <summary>
    /// R3：Host 层为 OperationCoordinator 提供的四个注入端口实现。
    /// 全部零 I/O：不访问文件系统、注册表、网络与配置目录；判级 / 预检 / 时钟 /
    /// 刷新广播都只做内存计算。真实 shell 可用性由 Fs 层 transport 决定，
    /// 本层不据此推断「操作成功」。
    /// </summary>
    public sealed class HostClock : IClock
    {
        public long NowTicks() { return DateTime.UtcNow.Ticks; }
    }

    /// <summary>判级端口：永久删除一律拒绝，其余交确认；不放行未知请求。</summary>
    public sealed class HostDangerPolicy : IDangerPolicy
    {
        public GuardVerdict Decide(OpRequest request)
        {
            if (request == null) return GuardVerdict.Reject;
            if (request.Kind == OpKind.PermanentDelete) return GuardVerdict.Reject;
            return GuardVerdict.Confirm;
        }
    }

    /// <summary>预检端口：本批不做盘符 / 只读探测，如实返回「无冲突且无错误」的内存结论。</summary>
    public sealed class HostPreflight : IPreflight
    {
        public PreflightResult Check(OpRequest request)
        {
            return new PreflightResult(false, false, false, ErrorClass.None);
        }
    }

    /// <summary>刷新广播接收端：把 FsEvent 转交 UI 回调，全局只有一份，不自行发起枚举。</summary>
    public sealed class HostRefreshSink : IRefreshSink
    {
        private readonly Action<FsEvent> handler;

        public HostRefreshSink(Action<FsEvent> handler)
        {
            this.handler = handler;
        }

        public void Broadcast(FsEvent evt)
        {
            if (handler != null) handler(evt);
        }
    }

    /// <summary>
    /// R3 可静态 / 运行期判定的 UI 契约判据（零 I/O 纯函数）。
    /// 产品 --selftest 直接复用这里，不创建窗口、不实例化 COM、不做真实目录读取。
    /// 运行期交互（键鼠、拖放、真实目录内容、原生菜单、图标渲染）一律标「未验证」。
    /// </summary>
    public static class UiContractRules
    {
        /// <summary>四档槽位表数量、跨度与主窗格判据全部问 LayoutMath 唯一来源。</summary>
        public static bool SlotsComplete()
        {
            for (int raw = 1; raw <= 4; raw++)
            {
                LayoutMode mode = (LayoutMode)raw;
                PaneSlot[] slots = LayoutMath.Slots(mode);
                if (slots == null || slots.Length != LayoutMath.PaneCount(mode)) return false;
                if (!LayoutMath.IsValidSlotTable(slots, mode)) return false;
            }
            return true;
        }

        /// <summary>布局切换只改落位与可见性，不销毁窗格对象。</summary>
        public static bool PaneObjectsPersist()
        {
            PaneState[] panes = PaneSet.Create();
            PaneState[] after = PaneSet.SwitchLayout(panes, LayoutMode.One, LayoutMode.Four);
            return PaneSet.IsComplete(panes) && PaneSet.LayoutKeepsObjects(panes, after);
        }

        /// <summary>切目录后 generation 前进，迟到批次必须丢弃。</summary>
        public static bool LateBatchDiscarded(int currentGeneration, int lateGeneration)
        {
            return EnumBatchRules.IsLateBatch(lateGeneration, currentGeneration) &&
                   EnumBatchRules.ShouldApplyBatch(currentGeneration, currentGeneration, false) &&
                   !EnumBatchRules.ShouldApplyBatch(lateGeneration, currentGeneration, false);
        }

        /// <summary>transport 默认关闭时，UI 文案必须同时含「未执行」与「未验证」，不冒充成功。</summary>
        public static bool TransportHonest(bool enabled, string message)
        {
            return !enabled && message != null &&
                   message.IndexOf("未执行") >= 0 && message.IndexOf("未验证") >= 0;
        }

        /// <summary>状态栏文案只认结构化结果，「没有异常」不构成成功。</summary>
        public static bool NoInferredSuccess()
        {
            return StatusRules.RejectsInferredSuccess() &&
                   !StatusRules.ClaimsSuccess(false, true) &&
                   StatusRules.CanRenderResult(new object()) &&
                   !StatusRules.CanRenderResult(null);
        }

        public static int StatusFieldCount()
        {
            return StatusRules.FieldCount();
        }

        /// <summary>R3 的真实交互行为从未在本机 / CI 取证，恒为 false。</summary>
        public static bool IsRuntimeVerified() { return false; }

        public static string BoundaryText()
        {
            return "未实现 / 未验证";
        }
    }
}
