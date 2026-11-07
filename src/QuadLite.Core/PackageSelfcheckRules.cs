using System;

namespace QuadLite.Core
{
    /// <summary>
    /// §8.1 B7 / NFR-01 出包包自检：单 exe + BUILD.txt、零安装、免运行时；
    /// 诊断工具 --thumbs / --dumpmenu 不进发布包；零写入三段取证沿用 B6 唯一例外。
    /// 纯判据，零 I/O；真实打包、exe 大小与 sha256 由发布线采集，未验证。
    /// </summary>
    public static class PackageSelfcheckRules
    {
        public const int ContentCount = 2;
        public const int ExcludedToolCount = 2;

        public static string[] Contents()
        {
            return new string[] { "QuadLite.exe", "BUILD.txt" };
        }

        public static string[] ExcludedDiagnosticTools()
        {
            return CliContractRules.ExcludedFromRelease();
        }

        public static bool SingleExecutable()
        {
            return Contents().Length == ContentCount;
        }

        public static bool NoInstaller()
        {
            return true;
        }

        public static bool NoRuntimeRequired()
        {
            return true;
        }

        public static bool IncludesBuildManifest()
        {
            string[] contents = Contents();
            for (int i = 0; i < contents.Length; i++)
            {
                if (contents[i] == "BUILD.txt") return true;
            }
            return false;
        }

        public static bool ExcludesDiagnosticTools()
        {
            string[] tools = ExcludedDiagnosticTools();
            if (tools.Length != ExcludedToolCount) return false;
            for (int i = 0; i < tools.Length; i++)
            {
                if (!CliContractRules.IsExcludedFromRelease(tools[i])) return false;
            }
            return true;
        }

        public static int WriteEvidenceZoneCount()
        {
            return ThumbWriteRules.EvidenceZoneCount;
        }

        public static bool SanitizedCrashDigest()
        {
            return !CrashDigestRules.ContainsFullPath() &&
                   !CrashDigestRules.ContainsFileName();
        }
    }
}
