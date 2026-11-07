using System;

namespace QuadLite.Core
{
    /// <summary>§5.7 每条结论必须带的证据等级；推断级不作判据。</summary>
    public enum EvidenceLevel { LocalRun, CI, Manual, Unverified }

    /// <summary>
    /// §5.7 / §8.2 DoD ③ 账本自检规则：每批一行、head_sha 指向功能提交、
    /// 证据等级齐备、未跑过一律写「未验证」，禁止用连跑次数当结论。
    /// 纯判据，零 I/O；真实账本文件由 docs/run-ledger.md 人工维护，本类型只判定格式。
    /// </summary>
    public static class LedgerRules
    {
        public const int RequiredColumnCount = 9;
        public const int HeadShaLength = 7;
        public const int EvidenceLevelCount = 4;

        public static string[] Columns()
        {
            return new string[] {
                "批次", "head_sha", "check-boundaries", "check-trace", "run_tests",
                "负向对照", "§10 扫描", "证据等级", "备注"
            };
        }

        public static string[] BatchIds()
        {
            return GateInventoryRules.BatchIds();
        }

        public static string[] EvidenceLevels()
        {
            return new string[] { "实测", "CI", "人工", "未验证" };
        }

        public static bool IsEvidenceLevel(string value)
        {
            string[] levels = EvidenceLevels();
            for (int i = 0; i < levels.Length; i++)
            {
                if (levels[i] == value) return true;
            }
            return false;
        }

        public static bool IsHeadSha(string value)
        {
            if (value == null || value.Length != HeadShaLength) return false;
            for (int i = 0; i < value.Length; i++)
            {
                char c = value[i];
                bool hex = (c >= '0' && c <= '9') || (c >= 'a' && c <= 'f');
                if (!hex) return false;
            }
            return true;
        }

        public static bool RequiresHeadSha()
        {
            return true;
        }

        public static bool OneRowPerBatch()
        {
            return true;
        }

        public static bool ForbidsStreakAsEvidence()
        {
            return true;
        }

        public static bool UnverifiedMustBeExplicit()
        {
            return true;
        }

        public static bool CountsMatchDeclaration()
        {
            return EvidenceLevels().Length == EvidenceLevelCount &&
                   Columns().Length == RequiredColumnCount;
        }
    }
}
