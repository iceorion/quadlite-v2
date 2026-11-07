using System;
using System.Collections.Generic;
using QuadLite.Core;

namespace QuadLite.App
{
    /// <summary>
    /// B8 发布线状态内核：把 §8.3 的阶段顺序、前置依赖、降级路径与每阶段证据要求
    /// 组装成只读明细行，并给出当前 M3 判定。
    /// 零 I/O、不引用呈现框架；真实 DPI / 杀软 / 兼容矩阵 / 签名取证未执行，未验证。
    /// </summary>
    public sealed class ReleaseState
    {
        private readonly bool c4DeclaredAbsent;

        public ReleaseState(bool c4DeclaredAbsent)
        {
            this.c4DeclaredAbsent = c4DeclaredAbsent;
        }

        public bool C4DeclaredAbsent { get { return c4DeclaredAbsent; } }

        public string[] StageIds()
        {
            return ReleaseStageRules.StageIds();
        }

        public string[] DependencyOrder()
        {
            return c4DeclaredAbsent ? ReleaseStageRules.DegradedOrder()
                                    : ReleaseStageRules.DependencyOrder();
        }

        public string[] DetailLines()
        {
            List<string> lines = new List<string>();
            string[] order = ReleaseStageRules.DependencyOrder();
            for (int i = 0; i < order.Length; i++)
            {
                string id = order[i];
                string pre = ReleaseStageRules.EffectivePrerequisite(id, c4DeclaredAbsent);
                if (pre == null) pre = "unknown";
                lines.Add("release-stage: " + id +
                          " prerequisite=" + (pre == "" ? "none" : pre) +
                          " evidence-required");
            }
            string[] degraded = ReleaseStageRules.DegradedOrder();
            for (int i = 0; i < degraded.Length; i++)
            {
                lines.Add("release-degraded: " + degraded[i]);
            }
            lines.Add("release-c4: " + (c4DeclaredAbsent ? ReleaseRecordRules.C4AbsenceText : "required"));
            return lines.ToArray();
        }

        public int DetailLineCount()
        {
            return DetailLines().Length;
        }

        /// <summary>
        /// 当前项目的真实状态：六个阶段全部未验证，C4 缺位未声明。
        /// 返回 EvidenceMissing，明确 M3 不成立，不把静态门卡绿写成可发布。
        /// </summary>
        public ReleaseVerdict CurrentM3Verdict()
        {
            EvidenceLevel[] levels = new EvidenceLevel[ReleaseStageRules.StageCount];
            for (int i = 0; i < levels.Length; i++) levels[i] = EvidenceLevel.Unverified;
            return ReleaseRecordRules.Evaluate(levels, false, "");
        }

        public bool IsPublishable()
        {
            return CurrentM3Verdict() == ReleaseVerdict.Publishable;
        }
    }
}
