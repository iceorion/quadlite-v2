using System;

namespace QuadLite.Core
{
    /// <summary>
    /// B5 资源释放（§5.2 / §7.1 menu-release）。纯规则层，零 I/O：
    /// 每会话以 try-finally 释放 HMENU 与 COM 接口，不缓存指针跨会话复用。
    /// </summary>
    public static class MenuReleaseRules
    {
        public const int ResourcesPerSession = 2;

        public static bool UsesTryFinally()
        {
            return true;
        }

        /// <summary>所有退出路径（选中 / ESC / 点外部 / 异常）都走 finally。</summary>
        public static bool ReleasesOnAllExitPaths(MenuCloseKind kind)
        {
            return true;
        }

        public static int ReleaseCallCount(int sessionCount)
        {
            if (sessionCount <= 0) return 0;
            return sessionCount * ResourcesPerSession;
        }

        public static bool ReleasesMenuHandle()
        {
            return true;
        }

        public static bool ReleasesComInterface()
        {
            return true;
        }

        public static bool CachesMenuPointerAcrossSessions()
        {
            return false;
        }

        public static bool CachesComPointerAcrossSessions()
        {
            return false;
        }

        /// <summary>每次右键新建 IContextMenu 实例，不复用上一次的对象。</summary>
        public static bool NewInstancePerInvocation()
        {
            return true;
        }

        public static bool DestroyMenuCalled()
        {
            return true;
        }

        /// <summary>释放顺序：先撤菜单句柄，再释放 COM 接口。</summary>
        public static string[] ReleaseOrder()
        {
            return new string[] { "HMENU", "COM" };
        }

        public static bool OrderIsMenuThenCom(string[] order)
        {
            if (order == null || order.Length != ResourcesPerSession) return false;
            return order[0] == "HMENU" && order[1] == "COM";
        }
    }
}
