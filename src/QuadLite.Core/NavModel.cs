using System;

namespace QuadLite.Core
{
    public enum NavAction { Back, Forward, Navigate, RefreshInPlace }

    /// <summary>
    /// 导航规则（§5.8 / FR-04）：前进 / 后退按标签独立；导航键只作用于活动窗格；
    /// 导航即清除筛选；F5 走 RefreshInPlace 保视口与选中集；迟到批次按 generation 丢弃。
    /// </summary>
    public static class NavRules
    {
        public static bool ClearsFilterOn(NavAction action)
        {
            return action != NavAction.RefreshInPlace;
        }

        public static bool KeepsViewportOn(NavAction action)
        {
            return action == NavAction.RefreshInPlace;
        }

        public static bool KeepsSelectionOn(NavAction action)
        {
            return action == NavAction.RefreshInPlace;
        }

        public static int NextGeneration(int current)
        {
            if (current < 1 || current == int.MaxValue) return 1;
            return current + 1;
        }

        public static bool IsLateBatchDiscarded(int batchGeneration, int currentGeneration)
        {
            return batchGeneration != currentGeneration;
        }

        /// <summary>快捷键只解析全局唯一活动窗格，不向别的窗格派发。</summary>
        public static bool AppliesOnlyToActivePane(int targetPaneId, int activePaneId)
        {
            return targetPaneId == activePaneId;
        }

        public static bool CanGoBack(int index)
        {
            return index > 0;
        }

        public static bool CanGoForward(int index, int count)
        {
            return count > 0 && index >= 0 && index < count - 1;
        }

        public static int BackIndex(int index)
        {
            if (!CanGoBack(index)) return index;
            return index - 1;
        }

        public static int ForwardIndex(int index, int count)
        {
            if (!CanGoForward(index, count)) return index;
            return index + 1;
        }

        /// <summary>迟到批次一律丢弃，不覆盖较新的用户状态。</summary>
        public static bool ShouldApplyBatch(int batchGeneration, int currentGeneration)
        {
            return !IsLateBatchDiscarded(batchGeneration, currentGeneration);
        }
    }
}
