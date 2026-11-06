using System;

namespace QuadLite.Core
{
    /// <summary>菜单宿主路径：默认系统原生；自绘层只在哨兵降级后作为兜底。</summary>
    public enum MenuHostMode { Native, Fallback }

    /// <summary>
    /// B5 菜单宿主规则（§5.2 / §7.1 menu-native-default、menu-no-verb-scan、menu-single-file）。
    /// 零 I/O：只给判据，不持有 HMENU、不调用 shell、不写 data 目录。
    /// </summary>
    public static class MenuHostRules
    {
        public const int StandardStepCount = 4;

        private static readonly string[] StandardSteps = new string[]
        {
            "GetUIObjectOf", "QueryContextMenu", "TrackPopupMenuEx", "InvokeCommand"
        };

        public static MenuHostMode DefaultMode()
        {
            return MenuHostMode.Native;
        }

        /// <summary>自绘层只在降级态启用，且必须可一键恢复到原生。</summary>
        public static bool FallbackIsDegradeOnly(MenuHostMode mode, bool sentinelTriggered)
        {
            if (mode == MenuHostMode.Native) return true;
            return sentinelTriggered;
        }

        public static bool CanRestoreNative()
        {
            return true;
        }

        public static string[] Steps()
        {
            string[] copy = new string[StandardSteps.Length];
            Array.Copy(StandardSteps, copy, StandardSteps.Length);
            return copy;
        }

        public static bool UsesStandardSequence(string[] actual)
        {
            if (actual == null || actual.Length != StandardStepCount) return false;
            for (int i = 0; i < StandardStepCount; i++)
            {
                if (actual[i] != StandardSteps[i]) return false;
            }
            return true;
        }

        /// <summary>产品路径的命令字符串查询次数恒为 0（§7.1 menu-no-verb-scan）。</summary>
        public static int VerbScanCount()
        {
            return 0;
        }

        public static int DeleteItemCount()
        {
            return 0;
        }

        public static int MenuTreeRewriteCount()
        {
            return 0;
        }

        public static bool ReadsItemTextViaPlainWin32()
        {
            return true;
        }

        public static bool ForbidsLazySubmenu()
        {
            return true;
        }

        public static bool ForbidsFixedTimeout()
        {
            return true;
        }

        public static bool ForbidsStaWorkerPool()
        {
            return true;
        }

        public static bool ForbidsNegativeCache()
        {
            return true;
        }

        public static bool ForbidsMenuPointerCache()
        {
            return true;
        }

        public static bool UsesMatureComBindingOnly()
        {
            return true;
        }

        /// <summary>默认路径不引入第二个进程 / 第二个可执行文件（menu-single-file）。</summary>
        public static bool IntroducesSecondProcess()
        {
            return false;
        }

        public static bool RequiresInstallationOrRuntime()
        {
            return false;
        }

        public static bool OptionalHostIsOptIn()
        {
            return true;
        }
    }
}
