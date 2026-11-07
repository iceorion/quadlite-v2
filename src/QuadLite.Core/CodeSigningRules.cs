using System;

namespace QuadLite.Core
{
    /// <summary>
    /// §8.3 H6 / DD-04 / §9 签名链路：免费 OV 证书、sha256 摘要、RFC3161 时间戳，
    /// sign → timestamp → verify 三步齐全，指纹与时间戳进 BUILD.txt；
    /// 未签名或签名链路失败时 BUILD.txt 如实记录并带 Warning，不用「未报错」冒充通过。
    /// 纯判据，零 I/O；真实 signtool 调用与证书获取未执行，未验证。
    /// </summary>
    public static class CodeSigningRules
    {
        public const string CertificateClass = "OV";
        public const string DigestAlgorithm = "sha256";
        public const int StepCount = 3;

        public static string[] Steps()
        {
            return new string[] { "sign", "timestamp", "verify" };
        }

        public static string UnsignedVerdict()
        {
            return BuildManifestRules.DirtyTreeWarning;
        }

        public static bool FingerprintGoesToBuildManifest()
        {
            return true;
        }

        public static bool UnsignedMustBeRecorded()
        {
            return true;
        }

        /// <summary>未签名、未验签、指纹未入库三种情况都不得宣称签名通过。</summary>
        public static bool SignatureClaimValid(bool signed, bool verified, bool fingerprintRecorded)
        {
            if (!signed) return false;
            if (!verified) return false;
            return fingerprintRecorded;
        }

        public static bool CountsMatchDeclaration()
        {
            return Steps().Length == StepCount && CertificateClass == "OV" &&
                   DigestAlgorithm == "sha256" &&
                   SignatureClaimValid(false, true, true) == false &&
                   SignatureClaimValid(true, false, true) == false &&
                   SignatureClaimValid(true, true, false) == false &&
                   SignatureClaimValid(true, true, true) == true;
        }
    }
}
