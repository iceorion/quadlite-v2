using System;

namespace QuadLite.Core
{
    /// <summary>缓存键四段（§5.11 / FR-30）：类型 / 路径 / 尺寸 / 时间戳。</summary>
    public enum CacheKeyPart { Kind, Path, Size, Stamp }

    /// <summary>缩略图失败分类（§5.12 错误模型）：DecodeFailed 回落类型图标且不重试。</summary>
    public enum ThumbFailure { None, Missing, NotImage, DecodeFailed }

    /// <summary>
    /// 缩略图缓存规则（B6）：四段缓存键、图片 / 视频类型隔离前缀、96 MB + 1500 条双闸 LRU、
    /// 负缓存不重试。纯字符串 / 数值判据，零 I/O；真实 LRU 与缓存介质未接入。
    /// </summary>
    public static class ThumbCacheRules
    {
        public const int KeyPartCount = 4;
        public const int MaxEntries = 1500;
        public const long MaxBytes = 96L * 1024L * 1024L;
        public const string ImagePrefix = "i|";
        public const string VideoPrefix = "v|";

        public static string[] KeyParts()
        {
            return new string[] { "kind", "path", "size", "stamp" };
        }

        /// <summary>四段定长键；类型段在最前，图片 / 视频天然不互相覆盖。</summary>
        public static string Key(string kind, string path, int sizePx, long stamp)
        {
            return kind + "|" + path + "|" + Convert.ToString(sizePx) + "|" + Convert.ToString(stamp);
        }

        public static string PrefixOf(bool video)
        {
            return video ? VideoPrefix : ImagePrefix;
        }

        public static bool IsTypeIsolated(string key)
        {
            return key != null &&
                   (key.StartsWith(ImagePrefix, StringComparison.Ordinal) ||
                    key.StartsWith(VideoPrefix, StringComparison.Ordinal));
        }

        public static bool AreIsolated(string imageKey, string videoKey)
        {
            return imageKey != null && videoKey != null &&
                   imageKey.StartsWith(ImagePrefix, StringComparison.Ordinal) &&
                   videoKey.StartsWith(VideoPrefix, StringComparison.Ordinal) &&
                   imageKey != videoKey;
        }

        public static bool WithinEntryGate(int entries)
        {
            return entries >= 0 && entries <= MaxEntries;
        }

        public static bool WithinByteGate(long bytes)
        {
            return bytes >= 0 && bytes <= MaxBytes;
        }

        public static bool WithinBothGates(int entries, long bytes)
        {
            return WithinEntryGate(entries) && WithinByteGate(bytes);
        }

        public static bool NeedsEviction(int entries, long bytes)
        {
            return !WithinBothGates(entries, bytes);
        }

        public static int EvictCount(int entries)
        {
            return entries > MaxEntries ? entries - MaxEntries : 0;
        }

        public static ThumbFailure FailureOf(bool missing, bool notImage, bool decodeFailed)
        {
            if (missing) return ThumbFailure.Missing;
            if (notImage) return ThumbFailure.NotImage;
            if (decodeFailed) return ThumbFailure.DecodeFailed;
            return ThumbFailure.None;
        }

        public static bool IsNegativeCache(ThumbFailure failure)
        {
            return failure != ThumbFailure.None;
        }

        public static bool RetryAllowed(ThumbFailure failure)
        {
            return failure == ThumbFailure.None;
        }

        public static bool FallsBackToTypeIcon(ThumbFailure failure)
        {
            return IsNegativeCache(failure);
        }

        public static bool NegativeCacheNeverRetries(ThumbFailure failure)
        {
            return IsNegativeCache(failure) && !RetryAllowed(failure);
        }
    }
}
