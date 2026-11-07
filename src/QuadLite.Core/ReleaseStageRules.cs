using System;

namespace QuadLite.Core
{
    /// <summary>§8.3 发布线节点：C4 前置检查 + H1..H6 六个阶段。</summary>
    public enum ReleaseStage { C4, H1, H2, H3, H4, H5, H6 }

    /// <summary>§8.4 M3 可发布判定的三种结果。</summary>
    public enum ReleaseVerdict { Publishable, C4NotDeclared, EvidenceMissing }

    /// <summary>
    /// §8.3 发布线顺序与降级路径。规格依赖链为 C4 → H1 → H3 → H2 → H4 → H5 → H6；
    /// C4 缺位时按降级路径先做 H1 → H3 → H2，并在发布记录显式写「C4 未做」。
    /// 纯判据，零 I/O；真实取证活动由发布线承担，本类型不执行，未验证。
    /// </summary>
    public static class ReleaseStageRules
    {
        public const int StageCount = 6;
        public const int NodeCount = 7;
        public const int DegradedStageCount = 3;

        public static string[] StageIds()
        {
            return new string[] { "H1", "H2", "H3", "H4", "H5", "H6" };
        }

        public static string[] DependencyOrder()
        {
            return new string[] { "C4", "H1", "H3", "H2", "H4", "H5", "H6" };
        }

        public static string[] DegradedOrder()
        {
            return new string[] { "H1", "H3", "H2" };
        }

        public static bool IsNode(string id)
        {
            string[] order = DependencyOrder();
            for (int i = 0; i < order.Length; i++)
            {
                if (order[i] == id) return true;
            }
            return false;
        }

        public static bool IsStage(string id)
        {
            if (id == "C4") return false;
            return IsNode(id);
        }

        /// <summary>按规格依赖链取前置节点；C4 为起点，返回空串。</summary>
        public static string Prerequisite(string id)
        {
            string[] order = DependencyOrder();
            for (int i = 0; i < order.Length; i++)
            {
                if (order[i] == id) return i == 0 ? "" : order[i - 1];
            }
            return null;
        }

        /// <summary>C4 缺位且已显式声明时，H1/H3/H2 走降级链；其余节点仍走完整依赖链。</summary>
        public static string EffectivePrerequisite(string id, bool c4DeclaredAbsent)
        {
            if (!c4DeclaredAbsent) return Prerequisite(id);
            string[] degraded = DegradedOrder();
            for (int i = 0; i < degraded.Length; i++)
            {
                if (degraded[i] == id) return i == 0 ? "" : degraded[i - 1];
            }
            return Prerequisite(id);
        }

        public static bool CanAdvance(string id, string[] completed, bool c4DeclaredAbsent)
        {
            string pre = EffectivePrerequisite(id, c4DeclaredAbsent);
            if (pre == null) return false;
            if (pre == "") return true;
            if (completed == null) return false;
            for (int i = 0; i < completed.Length; i++)
            {
                if (completed[i] == pre) return true;
            }
            return false;
        }

        public static bool CountsMatchDeclaration()
        {
            return StageIds().Length == StageCount &&
                   DependencyOrder().Length == NodeCount &&
                   DegradedOrder().Length == DegradedStageCount &&
                   IsNode("C4") && IsStage("H6") && !IsStage("C4");
        }
    }
}
