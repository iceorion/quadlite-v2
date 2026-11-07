using System;
using System.Collections.Generic;

namespace QuadLite.Core
{
    public enum CliMode { Normal, SelfTest, Shot, Diagnose, ShellMenuHost }

    public sealed class CliParseResult
    {
        private readonly bool ok;
        private readonly string error;
        private readonly CliMode mode;
        private readonly string shotSplit;

        public CliParseResult(bool ok, string error, CliMode mode, string shotSplit)
        {
            this.ok = ok;
            this.error = error;
            this.mode = mode;
            this.shotSplit = shotSplit;
        }

        public bool Ok { get { return ok; } }
        public string Error { get { return error; } }
        public CliMode Mode { get { return mode; } }
        public string ShotSplit { get { return shotSplit; } }
        public bool ParticipatesInSingleInstance
        {
            get { return SingleInstanceRules.ParticipatesInSingleInstance(mode); }
        }
    }

    /// <summary>参数解析为纯函数：未知参数拒绝启动，不静默忽略。</summary>
    public static class CliArgs
    {
        public static CliParseResult Parse(string[] args)
        {
            CliMode mode = CliMode.Normal;
            string shotSplit = "";
            bool sawShot = false;
            if (args != null)
            {
                for (int i = 0; i < args.Length; i++)
                {
                    string a = args[i] == null ? "" : args[i];
                    if (!a.StartsWith("--", StringComparison.Ordinal) ||
                        !CliContractRules.IsKnown(a))
                    {
                        return Reject("未知参数: " + a);
                    }

                    if (a == "--selftest")
                    {
                        mode = CliMode.SelfTest;
                    }
                    else if (a == "--diagnose")
                    {
                        mode = CliMode.Diagnose;
                    }
                    else if (a == "--shell-menu-host")
                    {
                        mode = CliMode.ShellMenuHost;
                    }
                    else if (a == "--shot")
                    {
                        sawShot = true;
                        mode = CliMode.Shot;
                        // §6.4 的 --shot <path> 与 B2 的 --shot --split 都合法。
                        if (i + 1 < args.Length && args[i + 1] != null &&
                            !args[i + 1].StartsWith("--", StringComparison.Ordinal))
                        {
                            i++;
                        }
                    }
                    else if (a == "--split")
                    {
                        if (!sawShot || i + 1 >= args.Length)
                        {
                            return Reject("--split 需要 --shot 且后接参数");
                        }
                        i++;
                        shotSplit = args[i] == null ? "" : args[i];
                    }
                    else if (CliContractRules.IsShotModifier(a))
                    {
                        if (!sawShot || i + 1 >= args.Length)
                        {
                            return Reject(a + " 需要 --shot 且后接参数");
                        }
                        i++;
                    }
                    else if (a == "--bench" || a == "--open-test" ||
                             a == "--thumbs" || a == "--dumpmenu")
                    {
                        if (i + 1 >= args.Length)
                        {
                            return Reject(a + " 需要参数");
                        }
                        i++;
                        mode = CliMode.Diagnose;
                    }
                    else if (a == "--data-dir")
                    {
                        if (i + 1 >= args.Length)
                        {
                            return Reject("--data-dir 需要参数");
                        }
                        i++;
                    }
                    // --no-persist 不改模式，由便携层消费。
                }
            }
            return new CliParseResult(true, "", mode, shotSplit);
        }

        private static CliParseResult Reject(string reason)
        {
            return new CliParseResult(false, reason, CliMode.Normal, "");
        }

        public static string LegalParameters()
        {
            return CliContractRules.LegalParameters();
        }
    }
}
