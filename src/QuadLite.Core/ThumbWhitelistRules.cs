using System;

namespace QuadLite.Core
{
    /// <summary>
    /// 图片缩略图白名单（§5.11 / FR-30）：单一白名单，WIC 内置解码器全集 14 种扩展名，
    /// 零第三方解码器。PDF / Office、webp / heic / avif 属规格写明的有意缺口，不是缺陷。
    /// `.ts` 走系统视频工厂，另有 1 MB 尺寸下限（见 VideoThumbRules）。
    /// 纯字符串 / 数值判据，零 I/O；真实 WIC 解码调用未接入。
    /// </summary>
    public static class ThumbWhitelistRules
    {
        public const int WhitelistCount = 14;
        public const long TsMinBytes = 1048576L;

        private static readonly string[] Whitelist = new string[]
        {
            "bmp", "dib", "gif", "ico", "jpg", "jpeg", "jpe",
            "png", "tif", "tiff", "dds", "hdp", "jxr", "wdp"
        };

        private static readonly string[] IntentionalGaps = new string[]
        {
            "pdf", "doc", "docx", "xls", "xlsx", "ppt", "pptx",
            "webp", "heic", "avif"
        };

        public static string[] Extensions()
        {
            return (string[])Whitelist.Clone();
        }

        public static string[] Gaps()
        {
            return (string[])IntentionalGaps.Clone();
        }

        public static bool IsWhitelisted(string extension)
        {
            string ext = Normalize(extension);
            for (int i = 0; i < Whitelist.Length; i++)
            {
                if (Whitelist[i] == ext) return true;
            }
            return false;
        }

        public static bool IsIntentionalGap(string extension)
        {
            string ext = Normalize(extension);
            for (int i = 0; i < IntentionalGaps.Length; i++)
            {
                if (IntentionalGaps[i] == ext) return true;
            }
            return false;
        }

        /// <summary>短 .ts 无缩略图：1 MB 是规格写明的下限，不是性能启发式。</summary>
        public static bool NeedsSizeGate(string extension)
        {
            return Normalize(extension) == "ts";
        }

        public static bool SizeGatePassed(long sizeBytes)
        {
            return sizeBytes >= TsMinBytes;
        }

        public static bool TsAllowed(long sizeBytes)
        {
            return SizeGatePassed(sizeBytes);
        }

        /// <summary>白名单命中且通过尺寸门槛；有意缺口一律回落类型图标。</summary>
        public static bool Allows(string extension, long sizeBytes)
        {
            if (!IsWhitelisted(extension)) return false;
            if (NeedsSizeGate(extension)) return SizeGatePassed(sizeBytes);
            return true;
        }

        public static bool UsesThirdPartyDecoder()
        {
            return false;
        }

        public static bool UsesNuGet()
        {
            return false;
        }

        public static bool BlankRejected(string extension)
        {
            return string.IsNullOrEmpty(extension) || Normalize(extension) == "";
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
