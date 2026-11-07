using System;

namespace QuadLite.Core
{
    /// <summary>
    /// §8.3 C4 前置检查：带杀软的 VM / 第二台机器，AVcount≠0 才可验证「零误报」；
    /// 须做单文件 / 多文件对比；未签名阶段显式写「未验证」。
    /// 纯判据，零 I/O；「没报错」不等于「没被拦」——这是本类型最关键的判定。
    /// </summary>
    public static class AvScanRules
    {
        public const int MinEngineCount = 1;

        public static bool RequiresSecondMachineOrVm()
        {
            return true;
        }

        public static bool RequiresSingleVsMultiFileComparison()
        {
            return true;
        }

        public static bool UnsignedPhaseExplicitlyUnverified()
        {
            return true;
        }

        public static bool NoErrorIsNotZeroDetection()
        {
            return EvidencePolicyRules.NoErrorIsNotEvidence();
        }

        /// <summary>引擎数为 0 或根本没扫过，都不得宣称零误报。</summary>
        public static bool ZeroFalsePositiveClaimValid(int engineCount, bool scanned)
        {
            return engineCount >= MinEngineCount && scanned;
        }

        public static bool CountsMatchDeclaration()
        {
            return MinEngineCount == 1 &&
                   ZeroFalsePositiveClaimValid(0, true) == false &&
                   ZeroFalsePositiveClaimValid(1, false) == false &&
                   ZeroFalsePositiveClaimValid(1, true) == true &&
                   NoErrorIsNotZeroDetection();
        }
    }
}
