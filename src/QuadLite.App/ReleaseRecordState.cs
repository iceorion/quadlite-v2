using System;
using System.Collections.Generic;
using QuadLite.Core;

namespace QuadLite.App
{
    /// <summary>
    /// B8 发布记录状态内核：把调用方传入的 H1–H6 证据等级与 C4 状态组装成发布记录行，
    /// 并给出 §8.4 M3 判定。零 I/O；不采集真实取证文本、不写文件。
    /// </summary>
    public sealed class ReleaseRecordState
    {
        private readonly EvidenceLevel[] levels;
        private readonly bool c4Done;
        private readonly string c4AbsenceNote;

        public ReleaseRecordState(EvidenceLevel[] levels, bool c4Done, string c4AbsenceNote)
        {
            this.levels = levels;
            this.c4Done = c4Done;
            this.c4AbsenceNote = c4AbsenceNote;
        }

        public ReleaseVerdict Verdict()
        {
            return ReleaseRecordRules.Evaluate(levels, c4Done, c4AbsenceNote);
        }

        public string[] Lines()
        {
            List<string> lines = new List<string>();
            string[] stages = ReleaseRecordRules.StageIds();
            for (int i = 0; i < stages.Length; i++)
            {
                EvidenceLevel level = levels == null || i >= levels.Length
                    ? EvidenceLevel.Unverified : levels[i];
                lines.Add(ReleaseRecordRules.ReleaseLine(stages[i], level));
            }
            lines.Add("release: stage=C4 status=" + (c4Done ? "done" : "absent") +
                      " note=" + (c4AbsenceNote == null ? "" : c4AbsenceNote));
            lines.Add("release: M3=" + Verdict().ToString());
            return lines.ToArray();
        }

        public int LineCount()
        {
            return Lines().Length;
        }

        public bool C4AbsenceDeclared()
        {
            return !c4Done && c4AbsenceNote == ReleaseRecordRules.C4AbsenceText;
        }
    }
}
