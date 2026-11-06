using System;
using QuadLite.Core;

namespace QuadLite.Tests
{
    /// <summary>
    /// B5 测试替身（§6.3 / §8.1 B5）：ok / 越界 / owner-draw / 崩溃 四态假 shell 扩展。
    /// 不依赖真机安装扩展、云盘或特定权限状态；不触碰任何系统 API。
    /// 注：规格把替身归在 QuadLite.Probe，本仓尚未建 Probe 项目，暂驻 Tests 层（见 ADR-v2-007 D8）。
    /// </summary>
    internal sealed class FakeShellExtension
    {
        private readonly ProbeBehavior behavior;
        private readonly int systemItemCount;

        public int InvokeCount;

        public FakeShellExtension(ProbeBehavior behavior, int systemItemCount)
        {
            this.behavior = behavior;
            this.systemItemCount = systemItemCount;
        }

        public ProbeBehavior Behavior { get { return behavior; } }

        /// <summary>模拟 QueryContextMenu 返回值，maxId = 返回值 - 1。</summary>
        public int QueryContextMenu()
        {
            return MenuProbeRules.QueryResult(systemItemCount);
        }

        public int MaxId()
        {
            return MenuInvokeRules.MaxIdFromQueryResult(QueryContextMenu());
        }

        public int SelectedId()
        {
            return MenuProbeRules.SelectedId(behavior, MaxId());
        }

        /// <summary>模拟 InvokeCommand：只有合法系统段 ID 才递给 shell，越界与自有段都不调用。</summary>
        public bool Invoke(int selectedId)
        {
            if (MenuInvokeRules.Classify(selectedId, MaxId()) != MenuDispatchKind.System) return false;
            InvokeCount++;
            return true;
        }

        public bool NeedsMessageForward()
        {
            return MenuProbeRules.NeedsMessageForward(behavior);
        }

        public MenuCloseKind Close()
        {
            return MenuProbeRules.CloseKind(behavior);
        }

        public bool ClosesNormally()
        {
            return MenuProbeRules.ClosesNormally(behavior);
        }

        /// <summary>假扩展不读取任何命令字符串：门卡 menu-no-verb-scan 据此保持 0 次。</summary>
        public string[] ObservedVerbs()
        {
            return new string[0];
        }

        public bool DependsOnRealMachine()
        {
            return MenuProbeRules.RequiresRealExtensionInstalled();
        }

        public bool DependsOnCloudOrPermission()
        {
            return MenuProbeRules.RequiresCloudOrSpecialPermission();
        }
    }
}
