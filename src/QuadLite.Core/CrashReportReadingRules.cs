using System;

namespace QuadLite.Core
{
    /// <summary>
    /// §8.3 H4 崩溃摘要脱敏与 Windows 错误报告读法：本地崩溃摘要为第一来源，
    /// WER 只作辅助；摘要五字段不含完整路径与文件名，格式见 SECURITY.md。
    /// 纯判据，零 I/O；复用 B7 的 CrashDigestRules，不另立字段表。
    /// </summary>
    public static class CrashReportReadingRules
    {
        public const int SourceCount = 2;

        public static string[] Sources()
        {
            return new string[] { "local-crash-digest", "windows-error-reporting" };
        }

        public static bool DigestIsSanitized()
        {
            return CrashDigestRules.IsSanitized();
        }

        public static bool WerOnlyAsSecondarySource()
        {
            return true;
        }

        public static bool NoNetworkUpload()
        {
            return CrashDigestRules.NoNetworkUpload();
        }

        /// <summary>五字段必须齐备，且不得引入任何额外字段（脱敏边界）。</summary>
        public static bool DigestFieldsComplete(string[] observed)
        {
            if (observed == null || observed.Length != CrashDigestRules.FieldCount) return false;
            string[] want = CrashDigestRules.Fields();
            for (int i = 0; i < want.Length; i++)
            {
                if (observed[i] != want[i]) return false;
            }
            return true;
        }

        public static bool CountsMatchDeclaration()
        {
            return Sources().Length == SourceCount && DigestIsSanitized() &&
                   DigestFieldsComplete(CrashDigestRules.Fields()) &&
                   DigestFieldsComplete(new string[] { "a", "b" }) == false;
        }
    }
}
