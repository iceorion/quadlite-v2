using System;

namespace QuadLite.Core
{
    /// <summary>
    /// 视频缩略图规则（§5.11 / FR-31）：15 种容器走系统图像工厂、独立 worker +
    /// 8 秒超时看门狗、Missing / NotImage 负缓存且不重试。
    /// 纯判据，零 I/O；真实 IShellItemImageFactory 调用与 worker 未接入。
    /// </summary>
    public static class VideoThumbRules
    {
        public const int ContainerCount = 15;
        public const int TimeoutSeconds = 8;

        private static readonly string[] Containers = new string[]
        {
            "mp4", "m4v", "mov", "avi", "wmv", "mkv", "flv", "webm",
            "mpg", "mpeg", "m2ts", "mts", "ts", "3gp", "ogv"
        };

        public static string[] SupportedContainers()
        {
            return (string[])Containers.Clone();
        }

        public static bool IsSupported(string extension)
        {
            string ext = Normalize(extension);
            for (int i = 0; i < Containers.Length; i++)
            {
                if (Containers[i] == ext) return true;
            }
            return false;
        }

        public static bool UsesSystemImageFactory()
        {
            return true;
        }

        public static bool UsesIndependentWorker()
        {
            return true;
        }

        public static bool HasWatchdog()
        {
            return true;
        }

        public static bool TimedOut(int elapsedSeconds)
        {
            return elapsedSeconds >= TimeoutSeconds;
        }

        /// <summary>短 .ts 走视频工厂，但仍受 1 MB 尺寸下限约束。</summary>
        public static bool TsAllowed(long sizeBytes)
        {
            return ThumbWhitelistRules.TsAllowed(sizeBytes);
        }

        public static bool Allows(string extension, long sizeBytes)
        {
            if (!IsSupported(extension)) return false;
            if (Normalize(extension) == "ts") return TsAllowed(sizeBytes);
            return true;
        }

        public static ThumbFailure Classify(bool missing, bool notImage)
        {
            return ThumbCacheRules.FailureOf(missing, notImage, false);
        }

        public static bool NegativeCacheNoRetry(ThumbFailure failure)
        {
            return ThumbCacheRules.NegativeCacheNeverRetries(failure);
        }

        /// <summary>写系统缩略图缓存是唯一显式写入例外，动机与系统文件管理器相同。</summary>
        public static bool WritesSystemCache()
        {
            return true;
        }

        public static bool UsesThirdPartyDecoder()
        {
            return false;
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
