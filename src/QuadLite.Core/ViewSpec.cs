using System;

namespace QuadLite.Core
{
    /// <summary>七档视图：图标通道 / 组头 / 缩略图目标的唯一来源。</summary>
    public enum ViewMode { Details, List, ExtraLarge, Large, Medium, Small, Tiles }

    public sealed class ViewSpec
    {
        private readonly ViewMode mode;
        private readonly int iconPx;
        private readonly int targetPx;
        private readonly bool groupHeader;

        public ViewSpec(ViewMode mode, int iconPx, int targetPx, bool groupHeader)
        {
            this.mode = mode;
            this.iconPx = iconPx;
            this.targetPx = targetPx;
            this.groupHeader = groupHeader;
        }

        public ViewMode Mode { get { return mode; } }
        public int IconPx { get { return iconPx; } }
        public int TargetPx { get { return targetPx; } }
        public bool RendersGroupHeader { get { return groupHeader; } }
    }

    public static class ViewSpecs
    {
        private static readonly ViewSpec[] All = new ViewSpec[]
        {
            new ViewSpec(ViewMode.Details,    16,   0, true),
            new ViewSpec(ViewMode.List,       16,   0, false),
            new ViewSpec(ViewMode.ExtraLarge,256, 256, false),
            new ViewSpec(ViewMode.Large,      96,  96, false),
            new ViewSpec(ViewMode.Medium,     48,  48, false),
            new ViewSpec(ViewMode.Small,      16,   0, false),
            new ViewSpec(ViewMode.Tiles,      48,  48, false)
        };

        public static int Count { get { return All.Length; } }

        public static ViewSpec For(ViewMode mode)
        {
            int i = (int)mode;
            if (i < 0 || i >= All.Length) return null;
            return All[i];
        }

        /// <summary>菜单置灰、解码调度、缓存键三处都问同一个函数。</summary>
        public static bool NeedsThumbnail(ViewMode mode)
        {
            ViewSpec s = For(mode);
            return s != null && s.TargetPx > 0;
        }

        public static bool IsKnown(int raw)
        {
            return raw >= 0 && raw < All.Length && All[raw].Mode == (ViewMode)raw;
        }

        public static int GroupHeaderCount()
        {
            int n = 0;
            for (int i = 0; i < All.Length; i++)
            {
                if (All[i].RendersGroupHeader) n++;
            }
            return n;
        }
    }
}
