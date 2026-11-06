using System;

namespace QuadLite.Core
{
    /// <summary>
    /// 缩略图显示开关规则（§5.11 / §5.9 / FR-30）：切换开关不重枚举、原地刷新保持
    /// 行集 / 视口 / 选中集。复用 B4 的 ToggleRules 与 B1 的 ViewSpecs，不重复定义。
    /// 纯判据，零 I/O；真实 UI 事件绑定未接入。
    /// </summary>
    public static class ThumbToggleRules
    {
        public static bool NoReenumOnThumbToggle()
        {
            return !ToggleRules.RequiresReenumeration(DisplayToggle.Thumbnail);
        }

        public static bool ReenumeratesOn(DisplayToggle toggle)
        {
            return ToggleRules.RequiresReenumeration(toggle);
        }

        public static ToggleState Flip(ToggleState state)
        {
            return ToggleRules.Flip(state, DisplayToggle.Thumbnail);
        }

        public static bool Enabled(ToggleState state)
        {
            return state.ShowThumbnail;
        }

        public static bool NeedsThumbForView(ToggleState state, ViewMode mode)
        {
            return Enabled(state) && ViewSpecs.NeedsThumbnail(mode);
        }

        public static bool KeepsViewport(int anchorBefore, int anchorAfter)
        {
            return ToggleRules.KeepsViewport(anchorBefore, anchorAfter);
        }

        public static bool KeepsRowSet(int rowsBefore, int rowsAfter)
        {
            return rowsBefore >= 0 && rowsAfter == rowsBefore;
        }

        public static bool KeepsSelection(int selectedBefore, int selectedAfter)
        {
            return selectedBefore >= 0 && selectedAfter == selectedBefore;
        }

        public static bool PersistsToSession()
        {
            return ToggleRules.PersistsToSession();
        }

        /// <summary>关开关不销毁已解码缓存，重新打开复用同一份键。</summary>
        public static bool ReusesCacheWhenReenabled()
        {
            return true;
        }

        public static bool QueuesNewJobOnToggle()
        {
            return false;
        }
    }
}
