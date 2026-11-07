using System;
using QuadLite.Core;

namespace QuadLite.App
{
    /// <summary>
    /// B7 出包状态内核：把调用方传入的七个字段组装成 BUILD.txt 行，
    /// 并给出包自检结论。零 I/O、不采集真实 sha256 / exe 大小，不引用呈现框架；
    /// 真实出包与字段采集属发布线，未验证。
    /// </summary>
    public sealed class PackageState
    {
        private readonly string[] values;

        public PackageState(string compileTime, string commit, string worktree,
                            string exeSize, string sha256, string embeddedResources,
                            string packageSelfcheck)
        {
            values = new string[] {
                compileTime, commit, worktree, exeSize, sha256,
                embeddedResources, packageSelfcheck
            };
        }

        public string[] BuildLines()
        {
            string[] fields = BuildManifestRules.Fields();
            string[] lines = new string[fields.Length];
            for (int i = 0; i < fields.Length; i++)
            {
                lines[i] = fields[i] + "=" + (values[i] == null ? "" : values[i]);
            }
            return lines;
        }

        public bool ManifestComplete()
        {
            return BuildLines().Length == BuildManifestRules.FieldCount;
        }

        public string DirtyTreeVerdict(bool dirty)
        {
            return BuildManifestRules.DirtyTreeVerdict(dirty);
        }

        public string[] Contents()
        {
            return PackageSelfcheckRules.Contents();
        }

        public bool SelfcheckPassed()
        {
            return PackageSelfcheckRules.SingleExecutable() &&
                   PackageSelfcheckRules.IncludesBuildManifest() &&
                   PackageSelfcheckRules.ExcludesDiagnosticTools() &&
                   PackageSelfcheckRules.NoInstaller() &&
                   PackageSelfcheckRules.NoRuntimeRequired();
        }

        public int WriteEvidenceZoneCount()
        {
            return PackageSelfcheckRules.WriteEvidenceZoneCount();
        }
    }
}
