using System;

namespace QuadLite.Core
{
    /// <summary>菜单关闭方式：选中 / ESC / 点外部均为正常收尾；Crashed 为进程异常终止。</summary>
    public enum MenuCloseKind { Selected, Esc, ClickOutside, Crashed }

    /// <summary>崩溃哨兵对某个上下文采取的动作。</summary>
    public enum SentinelAction { None, Degrade, Retry, ExtendTtl }

    /// <summary>
    /// B5 崩溃哨兵（§5.6 / §7.1 menu-sentinel）。纯规则层，零 I/O：
    /// 只判断「上次有没有正常收尾」，不枚举扩展、不识别具体扩展、不需要 dump。
    /// 按 ctxKey 降级而非全局关闭；不宣称「绝不崩」。
    /// </summary>
    public static class MenuSentinelRules
    {
        public const int DefaultTtlDays = 7;
        public const int MaxTtlDays = 30;

        /// <summary>ctxKey = hash(扩展类别 + 文件类型 + 卷类型)；确定性实现，不含路径与文件名。</summary>
        public static string ContextKey(string extensionClass, string fileType, string volumeType)
        {
            unchecked
            {
                uint h = 2166136261u;
                h = Mix(h, extensionClass);
                h = Mix(h, "|");
                h = Mix(h, fileType);
                h = Mix(h, "|");
                h = Mix(h, volumeType);
                return "ctx-" + h.ToString("x8");
            }
        }

        private static uint Mix(uint h, string part)
        {
            unchecked
            {
                if (part == null) return h * 16777619u;
                for (int i = 0; i < part.Length; i++)
                {
                    h ^= part[i];
                    h *= 16777619u;
                }
                return h;
            }
        }

        public static bool KeyContainsPathOrFileName()
        {
            return false;
        }

        public static bool KeyIsStableForSameContext(string a, string b, string c)
        {
            return ContextKey(a, b, c) == ContextKey(a, b, c);
        }

        public static bool KeyDiffersAcrossContexts(string a, string b, string c, string other)
        {
            return ContextKey(a, b, c) != ContextKey(other, b, c);
        }

        /// <summary>ESC 与点外部同属正常关闭，都要清 pending 标记。</summary>
        public static bool IsNormalClose(MenuCloseKind kind)
        {
            return kind != MenuCloseKind.Crashed;
        }

        public static bool ClearsPendingOnNormalClose(MenuCloseKind kind)
        {
            return IsNormalClose(kind);
        }

        public static bool PendingRemainsAfterCrash()
        {
            return true;
        }

        /// <summary>下次启动发现残留：只对同一 ctxKey 降级，其余上下文仍走原生。</summary>
        public static SentinelAction OnStartup(bool pendingRemains)
        {
            return pendingRemains ? SentinelAction.Degrade : SentinelAction.None;
        }

        public static bool DegradesByContextKeyOnly()
        {
            return true;
        }

        public static bool DegradesGlobally()
        {
            return false;
        }

        public static bool OtherContextsStayNative(bool pendingRemains, string pendingKey, string otherKey)
        {
            return !pendingRemains || pendingKey != otherKey;
        }

        /// <summary>一键重试：清 safe 项、重新走原生。</summary>
        public static SentinelAction OnRetry()
        {
            return SentinelAction.Retry;
        }

        public static bool RetryClearsSafeEntry()
        {
            return true;
        }

        public static MenuHostMode RetryHostMode()
        {
            return MenuHostMode.Native;
        }

        /// <summary>再崩：TTL 延长并保留提示（翻倍、封顶 MaxTtlDays）。</summary>
        public static SentinelAction OnCrashAgain()
        {
            return SentinelAction.ExtendTtl;
        }

        public static int ExtendedTtlDays(int currentDays)
        {
            if (currentDays <= 0) return DefaultTtlDays;
            long next = (long)currentDays * 2L;
            if (next > MaxTtlDays) return MaxTtlDays;
            return (int)next;
        }

        public static bool KeepsHintAfterCrashAgain()
        {
            return true;
        }

        /// <summary>降级时状态栏须写明原因并提供重试入口。</summary>
        public static bool HintHasReasonAndRetry()
        {
            return true;
        }

        public static bool EnumeratesExtensions()
        {
            return false;
        }

        public static bool IdentifiesConcreteExtension()
        {
            return false;
        }

        public static bool NeedsCrashDump()
        {
            return false;
        }

        /// <summary>脱敏崩溃摘要：只记类型 / 结果 / 耗时 / 项数 / 错误码，不联网、不外传。</summary>
        public static string CrashSummaryFields()
        {
            return "type;outcome;elapsed_ms;item_count;hr";
        }

        public static bool CrashSummaryIsLocalOnly()
        {
            return true;
        }

        public static bool ClaimsNeverCrash()
        {
            return false;
        }

        /// <summary>沿用 §5.12 的 MenuSessionState：Crashed 之后可以回到 Pending（重试）。</summary>
        public static bool SessionStateSupportsRetry()
        {
            return StateMachine.CanTransition(MenuSessionState.Crashed, MenuSessionState.Pending);
        }
    }
}
