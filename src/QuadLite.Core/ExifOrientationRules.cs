using System;

namespace QuadLite.Core
{
    /// <summary>EXIF 方向八值（§5.11 / FR-30）。1 为默认，越界一律归一到 1。</summary>
    public enum ExifOrientation
    {
        TopLeft = 1, TopRight = 2, BottomRight = 3, BottomLeft = 4,
        LeftTop = 5, RightTop = 6, RightBottom = 7, LeftBottom = 8
    }

    /// <summary>
    /// EXIF 方向规则（B6）：八值映射到顺时针四分之一转数与水平翻转，
    /// 纯数值判据，零 I/O。真实像素变换与解码器绑定未接入。
    /// </summary>
    public static class ExifOrientationRules
    {
        public const int OrientationCount = 8;
        public const int DefaultOrientation = 1;

        public static bool IsKnown(int raw)
        {
            return raw >= 1 && raw <= OrientationCount;
        }

        public static int Normalize(int raw)
        {
            return IsKnown(raw) ? raw : DefaultOrientation;
        }

        /// <summary>顺时针 90° 的个数：0 / 1 / 2 / 3。</summary>
        public static int QuarterTurns(int raw)
        {
            int v = Normalize(raw);
            if (v == 3 || v == 4) return 2;
            if (v == 5 || v == 6) return 1;
            if (v == 7 || v == 8) return 3;
            return 0;
        }

        public static bool FlipHorizontal(int raw)
        {
            int v = Normalize(raw);
            return v == 2 || v == 4 || v == 5 || v == 7;
        }

        public static bool NeedsRotate(int raw)
        {
            return QuarterTurns(raw) != 0;
        }

        public static bool IsIdentity(int raw)
        {
            return !NeedsRotate(raw) && !FlipHorizontal(raw);
        }

        /// <summary>方向只影响渲染变换，不改变缓存键的路径 / 尺寸 / 时间戳三段。</summary>
        public static bool ChangesCacheKey()
        {
            return false;
        }

        public static int[] AllValues()
        {
            return new int[] { 1, 2, 3, 4, 5, 6, 7, 8 };
        }
    }
}
