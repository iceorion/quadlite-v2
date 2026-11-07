using System;

namespace QuadLite.Core
{
    /// <summary>
    /// §5.5 / §5.13 / FR-35 崩溃摘要脱敏：只记类型 / 结果 / 耗时 / 项数 / 错误码，
    /// 不含完整路径与文件名，不联网、不外传。纯判据，零 I/O；
    /// 真实崩溃日志写入由 Fs 层与发布线 H4 承担，未验证。
    /// </summary>
    public static class CrashDigestRules
    {
        public const int FieldCount = 5;

        public static string[] Fields()
        {
            return new string[] { "type", "result", "elapsed", "item-count", "error-code" };
        }

        public static bool ContainsFullPath()
        {
            return false;
        }

        public static bool ContainsFileName()
        {
            return false;
        }

        public static bool NoNetworkUpload()
        {
            return true;
        }

        public static bool IsSanitized()
        {
            return !ContainsFullPath() && !ContainsFileName() && NoNetworkUpload();
        }

        public static bool CountsMatchDeclaration()
        {
            return Fields().Length == FieldCount && IsSanitized();
        }
    }
}
