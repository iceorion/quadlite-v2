using System;

namespace QuadLite.Core
{
    /// <summary>
    /// §6.4 命令行契约表：合法参数、取值要求、诊断模式与发布包排除项。
    /// 纯字符串判定，零 I/O；CliArgs.Parse 复用本表，避免「列出的合法参数」与
    /// 「实际接受的参数」两处漂移。
    /// </summary>
    public static class CliContractRules
    {
        public const int SpecParameterCount = 9;
        public const int ParameterCount = 14;

        private static readonly string[] SpecParameters = new string[] {
            "--selftest", "--bench", "--shot", "--open-test", "--thumbs",
            "--dumpmenu", "--data-dir", "--no-persist", "--shell-menu-host"
        };

        private static readonly string[] Flags = new string[] {
            "--selftest", "--no-persist", "--shell-menu-host", "--diagnose"
        };

        private static readonly string[] ValueParameters = new string[] {
            "--bench", "--open-test", "--thumbs", "--dumpmenu", "--data-dir",
            "--view", "--select", "--menu", "--split"
        };

        private static readonly string[] OptionalValueParameters = new string[] { "--shot" };

        private static readonly string[] DiagnosticModes = new string[] {
            "--selftest", "--bench", "--shot", "--open-test", "--thumbs",
            "--dumpmenu", "--diagnose"
        };

        private static readonly string[] ShotModifiers = new string[] {
            "--view", "--select", "--menu", "--split"
        };

        private static readonly string[] ReleaseExcluded = new string[] {
            "--thumbs", "--dumpmenu"
        };

        private static bool Contains(string[] items, string value)
        {
            if (value == null) return false;
            for (int i = 0; i < items.Length; i++)
            {
                if (items[i] == value) return true;
            }
            return false;
        }

        /// <summary>§6.4 主表九项。</summary>
        public static string[] SpecParameterList()
        {
            return (string[])SpecParameters.Clone();
        }

        /// <summary>全部已知参数：主表九项 + --shot 三个修饰项 + B2 兼容两项。</summary>
        public static string[] Parameters()
        {
            return new string[] {
                "--selftest", "--bench", "--shot", "--open-test", "--thumbs",
                "--dumpmenu", "--data-dir", "--no-persist", "--shell-menu-host",
                "--view", "--select", "--menu", "--diagnose", "--split"
            };
        }

        public static bool IsKnown(string name)
        {
            return Contains(Parameters(), name);
        }

        public static bool RequiresValue(string name)
        {
            return Contains(ValueParameters, name);
        }

        /// <summary>--shot 的值可选：B2 的 --shot --split 形态与 §6.4 的 --shot &lt;path&gt; 都合法。</summary>
        public static bool AllowsOptionalValue(string name)
        {
            return Contains(OptionalValueParameters, name);
        }

        public static bool IsDiagnostic(string name)
        {
            return Contains(DiagnosticModes, name);
        }

        public static bool IsShotModifier(string name)
        {
            return Contains(ShotModifiers, name);
        }

        public static bool IsExcludedFromRelease(string name)
        {
            return Contains(ReleaseExcluded, name);
        }

        public static string[] ExcludedFromRelease()
        {
            return (string[])ReleaseExcluded.Clone();
        }

        public static string LegalParameters()
        {
            string[] all = Parameters();
            string text = "";
            for (int i = 0; i < all.Length; i++)
            {
                if (i > 0) text += " | ";
                text += all[i];
                if (RequiresValue(all[i])) text += " <value>";
            }
            return text;
        }

        public static bool CountsMatchDeclaration()
        {
            return Parameters().Length == ParameterCount &&
                   SpecParameterList().Length == SpecParameterCount &&
                   DiagnosticModes.Length == 7 &&
                   ReleaseExcluded.Length == 2;
        }
    }
}
