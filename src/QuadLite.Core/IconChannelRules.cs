using System;

namespace QuadLite.Core
{
    /// <summary>四级图标通道（§5.11 / FR-29）。数值即像素，与 ViewSpecs.IconPx 同源。</summary>
    public enum IconSize { Small16 = 16, Medium48 = 48, Large96 = 96, Jumbo256 = 256 }

    /// <summary>
    /// 图标通道规则（B6）：四级尺寸、exe 两段式、jumbo 透明画布回落、盘符逐档取图、
    /// 可见区间单位防混用。复用 B1 的 ViewSpecs / CellMetrics，不重复定义已验证行为契约。
    /// 纯数值与字符串判据，零 I/O；真实取图与逐像素比对未接入。
    /// </summary>
    public static class IconChannelRules
    {
        public const int ChannelCount = 4;
        public const int FallbackPx = 48;
        public const int ExeStageCount = 2;

        private static readonly int[] Channels = new int[] { 16, 48, 96, 256 };

        public static int[] Levels()
        {
            return (int[])Channels.Clone();
        }

        public static bool IsKnown(int px)
        {
            for (int i = 0; i < Channels.Length; i++)
            {
                if (Channels[i] == px) return true;
            }
            return false;
        }

        /// <summary>七档视图各自的图标尺寸；Details / List / Small 归 16 档。</summary>
        public static int ForView(ViewMode mode)
        {
            ViewSpec spec = ViewSpecs.For(mode);
            return spec == null ? 0 : spec.IconPx;
        }

        public static IconSize LevelOf(int px)
        {
            if (!IsKnown(px)) return (IconSize)FallbackPx;
            return (IconSize)px;
        }

        /// <summary>jumbo 返回透明画布时回落 48 档，不放大空白图。</summary>
        public static int ResolveJumbo(bool transparentCanvas)
        {
            return transparentCanvas ? FallbackPx : (int)IconSize.Jumbo256;
        }

        public static bool JumboFallsBack(bool transparentCanvas)
        {
            return ResolveJumbo(transparentCanvas) == FallbackPx;
        }

        /// <summary>exe 图标两段式：先扩展名通用图，再按真实路径回填。</summary>
        public static bool IsTwoStageForExe(string extension)
        {
            return Normalize(extension) == "exe";
        }

        public static string[] ExeStages()
        {
            return new string[] { "extension-generic", "real-path" };
        }

        public static bool StageOrderIsGenericThenReal(string[] stages)
        {
            return stages != null && stages.Length == ExeStageCount &&
                   stages[0] == "extension-generic" && stages[1] == "real-path";
        }

        /// <summary>盘符每一档按真实路径取图，判据是「与通用文件夹图标逐像素不同」。</summary>
        public static bool DriveUsesRealPath(bool usesRealPath)
        {
            return usesRealPath;
        }

        public static bool DriveIconDiffersFromGeneric(bool pixelIdentical)
        {
            return !pixelIdentical;
        }

        public static bool DriveValidAt(int px, bool usesRealPath, bool pixelIdentical)
        {
            return IsKnown(px) && DriveUsesRealPath(usesRealPath) &&
                   DriveIconDiffersFromGeneric(pixelIdentical);
        }

        /// <summary>可见区间单位不混用：Items 与 Pixels 不可相交，不悄悄换算。</summary>
        public static bool UnitSafe(VisibleSpan a, VisibleSpan b)
        {
            return CellMetrics.CanIntersect(a, b);
        }

        public static bool MixedUnitRejected(VisibleSpan items, VisibleSpan pixels)
        {
            return items.Unit == SpanUnit.Items && pixels.Unit == SpanUnit.Pixels &&
                   !CellMetrics.CanIntersect(items, pixels);
        }

        public static bool NeedsThumb(ViewMode mode)
        {
            return ViewSpecs.NeedsThumbnail(mode);
        }

        private static string Normalize(string extension)
        {
            if (extension == null) return "";
            string s = extension.Trim();
            if (s.StartsWith(".", StringComparison.Ordinal)) s = s.Substring(1);
            return s.ToLowerInvariant();
        }
    }
}
