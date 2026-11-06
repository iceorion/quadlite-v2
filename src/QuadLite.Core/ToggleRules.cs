using System;

namespace QuadLite.Core
{
    /// <summary>四项显示开关（§5.9 / FR-14）。</summary>
    public enum DisplayToggle { Hidden, System, Extension, Thumbnail }

    /// <summary>条目的可见性三态：普通 / 隐藏 / 系统（组合自隐藏与系统两个属性）。</summary>
    public enum EntryVisibility { Normal, Hidden, System }

    /// <summary>四项开关的组合状态，零 I/O 值对象。</summary>
    public struct ToggleState
    {
        private readonly bool showHidden;
        private readonly bool showSystem;
        private readonly bool showExtension;
        private readonly bool showThumbnail;

        public ToggleState(bool showHidden, bool showSystem, bool showExtension, bool showThumbnail)
        {
            this.showHidden = showHidden;
            this.showSystem = showSystem;
            this.showExtension = showExtension;
            this.showThumbnail = showThumbnail;
        }

        public bool ShowHidden { get { return showHidden; } }
        public bool ShowSystem { get { return showSystem; } }
        public bool ShowExtension { get { return showExtension; } }
        public bool ShowThumbnail { get { return showThumbnail; } }
    }

    /// <summary>
    /// 显示开关（§5.9 / FR-14）：四项开关会话持久化；切换后原地刷新且保持视口（不回顶）；
    /// 过滤判定为 Core 纯函数。缩略图开关不触发重新枚举。
    /// </summary>
    public static class ToggleRules
    {
        public const int ToggleCount = 4;
        public const int StateCount = 16;   // 2^4 组合

        public static ToggleState Default()
        {
            return new ToggleState(false, false, true, false);
        }

        public static bool IsKnown(DisplayToggle toggle)
        {
            return toggle == DisplayToggle.Hidden || toggle == DisplayToggle.System ||
                   toggle == DisplayToggle.Extension || toggle == DisplayToggle.Thumbnail;
        }

        /// <summary>切换单项开关，返回新状态，不改入参。</summary>
        public static ToggleState Flip(ToggleState state, DisplayToggle toggle)
        {
            if (toggle == DisplayToggle.Hidden)
                return new ToggleState(!state.ShowHidden, state.ShowSystem, state.ShowExtension, state.ShowThumbnail);
            if (toggle == DisplayToggle.System)
                return new ToggleState(state.ShowHidden, !state.ShowSystem, state.ShowExtension, state.ShowThumbnail);
            if (toggle == DisplayToggle.Extension)
                return new ToggleState(state.ShowHidden, state.ShowSystem, !state.ShowExtension, state.ShowThumbnail);
            return new ToggleState(state.ShowHidden, state.ShowSystem, state.ShowExtension, !state.ShowThumbnail);
        }

        /// <summary>三态过滤：普通恒显示；隐藏看开关；系统看开关。</summary>
        public static bool IsVisible(ToggleState state, EntryVisibility visibility)
        {
            if (visibility == EntryVisibility.Normal) return true;
            if (visibility == EntryVisibility.Hidden) return state.ShowHidden;
            return state.ShowSystem;
        }

        public static int CombinationIndex(ToggleState state)
        {
            int n = 0;
            if (state.ShowHidden) n += 1;
            if (state.ShowSystem) n += 2;
            if (state.ShowExtension) n += 4;
            if (state.ShowThumbnail) n += 8;
            return n;
        }

        public static bool IsKnownCombination(int index)
        {
            return index >= 0 && index < StateCount;
        }

        public static bool PersistsToSession()
        {
            return true;
        }

        /// <summary>切换后原地刷新：保持视口锚点与选中集，不回顶。</summary>
        public static int ViewportAnchorAfterToggle(int anchorBefore)
        {
            return anchorBefore < 0 ? 0 : anchorBefore;
        }

        public static bool KeepsViewport(int anchorBefore, int anchorAfter)
        {
            return anchorBefore >= 0 && anchorAfter == anchorBefore;
        }

        /// <summary>缩略图开关只影响渲染，不重新枚举目录。</summary>
        public static bool RequiresReenumeration(DisplayToggle toggle)
        {
            return toggle != DisplayToggle.Thumbnail;
        }
    }
}
