using System;

namespace QuadLite.Core
{
    /// <summary>
    /// §5.7 / §7.3 / NFR-09 证据等级纪律：每条验收结论带等级，推断级不作判据，
    /// 未跑过一律写「未验证」，「没报错」不等于「没被拦」。
    /// 纯判据，零 I/O；复用 B7 的 EvidenceLevel 四级定义，不另立一套。
    /// </summary>
    public static class EvidencePolicyRules
    {
        public const int LevelCount = 4;

        public static string[] Levels()
        {
            return LedgerRules.EvidenceLevels();
        }

        public static bool InferenceIsNotVerdict()
        {
            return true;
        }

        public static bool NoErrorIsNotEvidence()
        {
            return true;
        }

        public static bool UnverifiedMustBeExplicit()
        {
            return LedgerRules.UnverifiedMustBeExplicit();
        }

        public static bool ForbidsStreakAsEvidence()
        {
            return LedgerRules.ForbidsStreakAsEvidence();
        }

        public static bool EveryConclusionCarriesLevel()
        {
            return true;
        }

        /// <summary>只有「标记为已实测 + 证据等级不是未验证」的结论可作判据。</summary>
        public static bool ClaimAccepted(EvidenceLevel level, bool measured)
        {
            if (!measured) return false;
            return level != EvidenceLevel.Unverified;
        }

        public static bool IsVerdictLevel(EvidenceLevel level)
        {
            return level == EvidenceLevel.LocalRun || level == EvidenceLevel.CI ||
                   level == EvidenceLevel.Manual;
        }

        public static bool CountsMatchDeclaration()
        {
            return Levels().Length == LevelCount &&
                   ClaimAccepted(EvidenceLevel.Unverified, true) == false &&
                   ClaimAccepted(EvidenceLevel.CI, false) == false &&
                   IsVerdictLevel(EvidenceLevel.Unverified) == false;
        }
    }
}
