using System;

namespace QuadLite.Core
{
    /// <summary>
    /// §8.3 H3 / NFR-02 / §5.11 零写入三段取证：注册表 / 应用目录 / 系统缓存三段分别取证，
    /// 唯一显式写入例外为视频缩略图写系统缩略图缓存，且须与系统文件管理器同源同动机。
    /// 纯判据，零 I/O；真实三段取证由发布线承担，未执行，未验证。
    /// </summary>
    public static class WriteEvidenceRules
    {
        public const int ZoneCount = 3;
        public const int ExceptionCount = 1;

        public static string[] Zones()
        {
            return new string[] { "registry", "app-directory", "system-cache" };
        }

        public static string SoleException()
        {
            return "thumbnail-cache";
        }

        public static bool SameMotivationAsShell()
        {
            return true;
        }

        /// <summary>三段必须齐备，缺一段或出现未声明的段即判红。</summary>
        public static bool ZonesComplete(string[] observed)
        {
            if (observed == null) return false;
            if (observed.Length != ZoneCount) return false;
            string[] want = Zones();
            for (int i = 0; i < want.Length; i++)
            {
                bool found = false;
                for (int j = 0; j < observed.Length; j++)
                {
                    if (observed[j] == want[i]) { found = true; break; }
                }
                if (!found) return false;
            }
            return true;
        }

        /// <summary>例外清单有且只有一条，且必须是缩略图缓存。</summary>
        public static bool OnlyDeclaredException(string[] exceptions)
        {
            if (exceptions == null || exceptions.Length != ExceptionCount) return false;
            return exceptions[0] == SoleException();
        }

        public static bool CountsMatchDeclaration()
        {
            return Zones().Length == ZoneCount &&
                   PackageSelfcheckRules.WriteEvidenceZoneCount() == ZoneCount &&
                   ZonesComplete(Zones()) &&
                   OnlyDeclaredException(new string[] { SoleException() }) &&
                   OnlyDeclaredException(new string[] { "profile-fallback" }) == false;
        }
    }
}
