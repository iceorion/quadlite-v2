using System;

namespace QuadLite.Core
{
    /// <summary>
    /// §8.3 / §8.4 M3 发布记录：H1–H6 逐项有证据才算可发布；
    /// C4 缺位时必须在发布记录显式写「C4 未做」，不许用「没报错」冒充「没被拦」。
    /// 纯判据，零 I/O；真实发布记录由发布线产出，本类型只判定其形态。
    /// </summary>
    public static class ReleaseRecordRules
    {
        public const string C4AbsenceText = "C4 未做";

        public static string[] StageIds()
        {
            return ReleaseStageRules.StageIds();
        }

        public static bool M3RequiresAllSix()
        {
            return true;
        }

        public static bool C4AbsenceMustBeWritten()
        {
            return true;
        }

        public static string ReleaseLine(string stage, EvidenceLevel level)
        {
            return "release: stage=" + (stage == null ? "" : stage) +
                   " evidence=" + level.ToString() +
                   " verdict=" + (EvidencePolicyRules.IsVerdictLevel(level) ? "recorded" : "unverified");
        }

        /// <summary>
        /// 六个阶段任一为 Unverified 即不可发布；C4 缺位但未写「C4 未做」单独判一类。
        /// 全部实测且 C4 状态如实声明，才返回 Publishable。
        /// </summary>
        public static ReleaseVerdict Evaluate(EvidenceLevel[] levels, bool c4Done, string c4AbsenceNote)
        {
            if (levels == null || levels.Length != ReleaseStageRules.StageCount)
            {
                return ReleaseVerdict.EvidenceMissing;
            }
            for (int i = 0; i < levels.Length; i++)
            {
                if (!EvidencePolicyRules.IsVerdictLevel(levels[i]))
                {
                    return ReleaseVerdict.EvidenceMissing;
                }
            }
            if (!c4Done && c4AbsenceNote != C4AbsenceText)
            {
                return ReleaseVerdict.C4NotDeclared;
            }
            return ReleaseVerdict.Publishable;
        }

        public static bool CountsMatchDeclaration()
        {
            EvidenceLevel[] missing = new EvidenceLevel[ReleaseStageRules.StageCount];
            for (int i = 0; i < missing.Length; i++) missing[i] = EvidenceLevel.Unverified;
            EvidenceLevel[] measured = new EvidenceLevel[ReleaseStageRules.StageCount];
            for (int i = 0; i < measured.Length; i++) measured[i] = EvidenceLevel.Manual;
            return StageIds().Length == ReleaseStageRules.StageCount &&
                   Evaluate(missing, false, C4AbsenceText) == ReleaseVerdict.EvidenceMissing &&
                   Evaluate(measured, false, "") == ReleaseVerdict.C4NotDeclared &&
                   Evaluate(measured, false, C4AbsenceText) == ReleaseVerdict.Publishable &&
                   Evaluate(measured, true, "") == ReleaseVerdict.Publishable;
        }
    }
}
