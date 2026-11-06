using System;

namespace QuadLite.Core
{
    /// <summary>假 shell 扩展的四种行为（§6.3）：正常 / 越界 / owner-draw / 崩溃。</summary>
    public enum ProbeBehavior { Ok, OutOfRange, OwnerDraw, Crash }

    /// <summary>
    /// B5 测试替身规则（§6.3 / §8.1 B5 门卡列）。纯规则层，零 I/O：
    /// 全部菜单门卡不依赖真机安装扩展、云盘或特定权限状态。
    /// </summary>
    public static class MenuProbeRules
    {
        public const int BehaviorCount = 4;

        public static bool RequiresRealExtensionInstalled()
        {
            return false;
        }

        public static bool RequiresCloudOrSpecialPermission()
        {
            return false;
        }

        /// <summary>模拟 QueryContextMenu 返回值（= maxId + 1）。</summary>
        public static int QueryResult(int systemItemCount)
        {
            if (systemItemCount <= 0) return 1;
            return systemItemCount + 1;
        }

        /// <summary>模拟用户选中的 ID：越界用例返回一个必定被丢弃的 ID。</summary>
        public static int SelectedId(ProbeBehavior behavior, int maxId)
        {
            if (behavior == ProbeBehavior.OutOfRange) return maxId + 4096;
            if (behavior == ProbeBehavior.OwnerDraw) return MenuModel.OwnIdBase;
            return MenuInvokeRules.IdCmdFirst;
        }

        public static bool ShouldDrop(ProbeBehavior behavior, int maxId)
        {
            return MenuInvokeRules.ShouldDrop(SelectedId(behavior, maxId), maxId);
        }

        public static bool NeedsMessageForward(ProbeBehavior behavior)
        {
            return behavior == ProbeBehavior.OwnerDraw;
        }

        public static bool ClosesNormally(ProbeBehavior behavior)
        {
            return behavior != ProbeBehavior.Crash;
        }

        public static MenuCloseKind CloseKind(ProbeBehavior behavior)
        {
            return behavior == ProbeBehavior.Crash
                ? MenuCloseKind.Crashed : MenuCloseKind.Selected;
        }

        /// <summary>四态齐备才能覆盖 §7.1 的全部菜单门卡。</summary>
        public static bool CoversAllBehaviors(ProbeBehavior[] behaviors)
        {
            if (behaviors == null) return false;
            bool[] seen = new bool[BehaviorCount];
            for (int i = 0; i < behaviors.Length; i++)
            {
                int k = (int)behaviors[i];
                if (k < 0 || k >= BehaviorCount) return false;
                seen[k] = true;
            }
            for (int i = 0; i < BehaviorCount; i++)
            {
                if (!seen[i]) return false;
            }
            return true;
        }

        public static bool[] DefaultMatrix()
        {
            return new bool[] { true, true, true, true };
        }
    }
}
