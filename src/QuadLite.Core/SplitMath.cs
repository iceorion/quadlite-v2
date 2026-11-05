using System;

namespace QuadLite.Core
{
    /// <summary>
    /// 分隔条数学（§5.8 / FR-08）：比例按「相对均分偏移」存储，跨会话保持；
    /// 折叠为 0/1，不另存可见性布尔；24px 吸附；折前比例可恢复。纯数值、零 I/O。
    /// </summary>
    public static class SplitMath
    {
        public const double SnapThresholdPx = 24.0;
        public const double EvenRatio = 0.5;
        public const double MinRatio = 0.0;
        public const double MaxRatio = 1.0;

        /// <summary>存进会话的是相对均分的偏移，不是绝对像素比例。</summary>
        public static double OffsetOf(double ratio)
        {
            return ratio - EvenRatio;
        }

        public static double RatioOf(double offset)
        {
            return EvenRatio + offset;
        }

        public static bool IsOffsetValid(double offset)
        {
            return !double.IsNaN(offset) && offset >= -EvenRatio && offset <= EvenRatio;
        }

        public static bool IsRatioValid(double ratio)
        {
            return !double.IsNaN(ratio) && ratio >= MinRatio && ratio <= MaxRatio;
        }

        /// <summary>拖到 0 或 1 即折叠；折叠态不销毁窗格对象。</summary>
        public static bool IsCollapsed(double ratio)
        {
            return ratio <= MinRatio || ratio >= MaxRatio;
        }

        public static double CollapseTo(bool firstPaneCollapsed)
        {
            return firstPaneCollapsed ? MinRatio : MaxRatio;
        }

        /// <summary>24px 吸附到 0 / 均分 / 1 三个锚点；总尺寸非法返回 -1。</summary>
        public static double Snap(double ratio, double totalPx)
        {
            if (totalPx <= 0.0 || !IsRatioValid(ratio)) return -1.0;
            double threshold = SnapThresholdPx / totalPx;
            if (ratio <= threshold) return MinRatio;
            if (ratio >= MaxRatio - threshold) return MaxRatio;
            if (Math.Abs(ratio - EvenRatio) <= threshold) return EvenRatio;
            return ratio;
        }

        /// <summary>恢复折叠前的比例：用折叠前记录的 offset，不用折叠态的 0/1 反推。</summary>
        public static double Restore(double offsetBeforeCollapse)
        {
            if (!IsOffsetValid(offsetBeforeCollapse)) return EvenRatio;
            return RatioOf(offsetBeforeCollapse);
        }

        /// <summary>连线数：1 窗格 0 条、2 窗格 1 条、3/4 窗格各 2 条（横线贯穿 + 竖线）。</summary>
        public static int ConnectionCount(LayoutMode mode)
        {
            if (mode == LayoutMode.One) return 0;
            if (mode == LayoutMode.Two) return 1;
            if (mode == LayoutMode.Three) return 2;
            if (mode == LayoutMode.Four) return 2;
            return -1;
        }

        /// <summary>折叠只改变比例，不改变窗格对象数量（状态不重建）。</summary>
        public static int PaneCountAfterCollapse(LayoutMode mode)
        {
            return LayoutMath.PaneCount(mode);
        }

        /// <summary>把存下来的 offset 换算成指定宽度下的像素位置。</summary>
        public static double PixelAtWidth(double storedOffset, double widthPx)
        {
            if (!IsOffsetValid(storedOffset) || widthPx <= 0.0) return -1.0;
            return RatioOf(storedOffset) * widthPx;
        }

        /// <summary>由像素位置还原 offset；与存下来的值一致才叫「跨会话保持」。</summary>
        public static double OffsetAtPixel(double pixel, double widthPx)
        {
            if (pixel < 0.0 || widthPx <= 0.0) return -2.0;
            return OffsetOf(pixel / widthPx);
        }

        /// <summary>相对均分偏移在两种窗口尺寸下往返换算后仍是同一个 offset。</summary>
        public static bool OffsetSurvivesResize(double storedOffset, double widthA, double widthB)
        {
            double pa = PixelAtWidth(storedOffset, widthA);
            double pb = PixelAtWidth(storedOffset, widthB);
            if (pa < 0.0 || pb < 0.0) return false;
            double backA = OffsetAtPixel(pa, widthA);
            double backB = OffsetAtPixel(pb, widthB);
            return Math.Abs(backA - storedOffset) < 1e-12 &&
                   Math.Abs(backB - storedOffset) < 1e-12;
        }
    }
}
