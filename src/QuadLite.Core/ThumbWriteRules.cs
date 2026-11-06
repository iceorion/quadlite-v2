using System;

namespace QuadLite.Core
{
    /// <summary>零写入三段取证的三个观察区（§5.11 / §2.2 NFR-02）。</summary>
    public enum WriteZone { Settings, ApplicationFolder, SystemCache }

    /// <summary>
    /// 写入例外规则（§5.11 / §5.13 / `thumb-nowrite`）：视频缩略图写系统缩略图缓存是
    /// 首版唯一显式写入例外，须按「注册表 / 应用目录 / 系统缓存」三段取证，
    /// 且只在与系统文件管理器相同动机下写入。
    /// 纯判据，零 I/O；真实三段取证由发布线 H3 承担，未验证。
    /// </summary>
    public static class ThumbWriteRules
    {
        public const int ExceptionCount = 1;
        public const int EvidenceZoneCount = 3;

        public static string SoleException()
        {
            return "thumbcache";
        }

        public static bool IsSoleException(string zone)
        {
            return zone != null && zone.ToLowerInvariant() == SoleException();
        }

        public static string[] EvidenceZones()
        {
            return new string[] { "registry", "app-directory", "system-cache" };
        }

        public static bool NeedsThreeZoneEvidence()
        {
            return EvidenceZones().Length == EvidenceZoneCount;
        }

        /// <summary>只有系统缓存区、且动机与系统文件管理器相同时才允许写入。</summary>
        public static bool WriteAllowed(WriteZone zone, bool sameMotiveAsExplorer)
        {
            return zone == WriteZone.SystemCache && sameMotiveAsExplorer;
        }

        public static bool ZeroSettingsWrites()
        {
            return !WriteAllowed(WriteZone.Settings, true);
        }

        public static bool ZeroAppDirectoryWrites()
        {
            return !WriteAllowed(WriteZone.ApplicationFolder, true);
        }

        public static bool ZeroWritesWithoutSameMotive()
        {
            return !WriteAllowed(WriteZone.SystemCache, false);
        }

        public static bool NoPersistWritesNothing()
        {
            return true;
        }

        public static bool ConfigRootOnlyData()
        {
            return true;
        }

        public static bool SilentFallbackToUserProfile()
        {
            return false;
        }
    }
}
