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
            get { return mode != CliMode.Diagnose && mode != CliMode.SelfTest; }
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
                    else
                    {
                        return Reject("未知参数: " + a);
                    }
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
            return "--selftest | --shot --split <layout=...,colTop=...> | --diagnose | --shell-menu-host";
        }
    }
}
