using System;
using QuadLite.Core;

namespace QuadLite.App
{
    /// <summary>
    /// B5 菜单会话状态内核（L4，零 I/O）：只读 Core 规则结果装配状态。
    /// 不含窗口、XAML、消息循环、HMENU、COM 声明与任何事件绑定；
    /// 真实系统原生菜单由后续批次在注入端口后接入，此处不冒充可运行应用。
    /// </summary>
    public sealed class MenuState
    {
        private readonly MenuHostMode host;
        private readonly MenuSessionState session;
        private readonly string contextKey;
        private readonly bool safeListed;
        private readonly bool pending;
        private readonly int ttlDays;
        private readonly int maxId;
        private readonly int snapshots;

        private MenuState(MenuHostMode host, MenuSessionState session, string contextKey,
                          bool safeListed, bool pending, int ttlDays, int maxId, int snapshots)
        {
            this.host = host;
            this.session = session;
            this.contextKey = contextKey;
            this.safeListed = safeListed;
            this.pending = pending;
            this.ttlDays = ttlDays;
            this.maxId = maxId;
            this.snapshots = snapshots;
        }

        public static MenuState Default()
        {
            return new MenuState(MenuHostRules.DefaultMode(), MenuSessionState.None,
                                 "", false, false, MenuSentinelRules.DefaultTtlDays, 0, 0);
        }

        public MenuHostMode Host { get { return host; } }
        public MenuSessionState Session { get { return session; } }
        public string ContextKey { get { return contextKey; } }
        public bool SafeListed { get { return safeListed; } }
        public bool Pending { get { return pending; } }
        public int TtlDays { get { return ttlDays; } }
        public int MaxId { get { return maxId; } }
        public int Snapshots { get { return snapshots; } }

        public bool IsNative() { return host == MenuHostMode.Native; }
        public bool IsFallback() { return host == MenuHostMode.Fallback; }
        public bool NeedsHint() { return IsFallback(); }
        public bool NeedsRetryButton() { return IsFallback(); }
        public bool UsesNativeByDefault() { return MenuHostRules.DefaultMode() == MenuHostMode.Native; }

        public MenuState WithSession(MenuSessionState state)
        {
            return new MenuState(host, state, contextKey, safeListed, pending, ttlDays, maxId, snapshots);
        }

        public MenuState WithHost(MenuHostMode mode)
        {
            return new MenuState(mode, session, contextKey, safeListed, pending, ttlDays, maxId, snapshots);
        }

        public MenuState WithContext(string extensionClass, string fileType, string volumeType)
        {
            string key = MenuSentinelRules.ContextKey(extensionClass, fileType, volumeType);
            return new MenuState(host, session, key, safeListed, pending, ttlDays, maxId, snapshots);
        }

        /// <summary>打开右键：写 pending、取一份目标快照、按 probe 结果记录 maxId。</summary>
        public MenuState OnOpen(int queryResult)
        {
            int newMax = MenuInvokeRules.MaxIdFromQueryResult(queryResult);
            return new MenuState(host, MenuSessionState.Pending, contextKey, safeListed,
                                 true, ttlDays, newMax,
                                 snapshots + MenuTargetSnapshotRules.SnapshotsPerInvocation());
        }

        public MenuState OnTracking()
        {
            return WithSession(MenuSessionState.Tracking);
        }

        public MenuState OnClose(MenuCloseKind kind)
        {
            MenuSessionState next = kind == MenuCloseKind.Crashed
                ? MenuSessionState.Crashed : MenuSessionState.Closed;
            bool clear = MenuSentinelRules.ClearsPendingOnNormalClose(kind);
            return new MenuState(host, next, contextKey, safeListed,
                                 !clear, ttlDays, maxId, snapshots);
        }

        public MenuState OnStartupDegrade(bool pendingRemains)
        {
            SentinelAction action = MenuSentinelRules.OnStartup(pendingRemains);
            if (action != SentinelAction.Degrade) return this;
            return new MenuState(MenuHostMode.Fallback, session, contextKey,
                                 true, false, ttlDays, maxId, snapshots);
        }

        public MenuState OnRetry()
        {
            if (MenuSentinelRules.OnRetry() != SentinelAction.Retry) return this;
            return new MenuState(MenuSentinelRules.RetryHostMode(), MenuSessionState.Pending,
                                 contextKey, false, true, ttlDays, maxId, snapshots);
        }

        public MenuState OnCrashAgain()
        {
            return new MenuState(MenuHostMode.Fallback, MenuSessionState.Crashed, contextKey,
                                 true, true, MenuSentinelRules.ExtendedTtlDays(ttlDays),
                                 maxId, snapshots);
        }

        /// <summary>结构化结果：状态栏文案只从这里取，不由「没有异常」推断成功。</summary>
        public string HintText()
        {
            if (!IsFallback()) return "";
            return "native-menu-degraded;reason=pending-residual;action=retry";
        }

        public bool HintMentionsReasonAndRetry()
        {
            string h = HintText();
            return h.IndexOf("reason=", StringComparison.Ordinal) >= 0 &&
                   h.IndexOf("action=retry", StringComparison.Ordinal) >= 0;
        }
    }
}
