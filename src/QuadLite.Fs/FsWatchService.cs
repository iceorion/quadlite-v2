using System;
using System.IO;
using QuadLite.Core;

namespace QuadLite.Fs
{
    /// <summary>
    /// 全局单一订阅：一处 watcher，经去抖 + 自操作抑制后广播给所有窗格。
    /// 实际订阅与通知由系统提供，本批只冻结结构与纯函数判定，运行期行为标「未验证」。
    /// </summary>
    public sealed class FsWatchService : IFsWatch, IDisposable
    {
        private readonly IRefreshSink sink;
        private readonly IClock clock;
        private readonly SelfOpGuard guard;
        private readonly WatchThrottle throttle = new WatchThrottle();
        private FileSystemWatcher watcher;
        private string subscribedPath;
        private int subscriberCount;

        public FsWatchService(IRefreshSink sink, IClock clock, SelfOpGuard guard)
        {
            this.sink = sink;
            this.clock = clock;
            this.guard = guard;
        }

        public int WatcherCount { get { return watcher == null ? 0 : 1; } }
        public int SubscriberCount { get { return subscriberCount; } }
        public int BroadcastCount { get { return throttle.BroadcastCount; } }
        public string SubscribedPath { get { return subscribedPath; } }

        public void Subscribe(PathKey path, Action<FsEvent> handler)
        {
            if (path == null || path.Value.Length == 0) return;
            subscriberCount++;
            if (watcher == null)
            {
                subscribedPath = path.Value;
                throttle.Register(subscribedPath, Now());
                watcher = new FileSystemWatcher(subscribedPath);
                watcher.IncludeSubdirectories = false;
                watcher.Changed += OnChanged;
                watcher.Created += OnChanged;
                watcher.Deleted += OnChanged;
                watcher.EnableRaisingEvents = true;
            }
        }

        public void Unsubscribe(PathKey path, Action<FsEvent> handler)
        {
            if (subscriberCount > 0) subscriberCount--;
            if (subscriberCount > 0) return;
            DisposeWatcher();
            if (subscribedPath != null) throttle.Unregister(subscribedPath);
            subscribedPath = null;
        }

        /// <summary>一次批量操作合并成一次广播；被自操作抑制的通知不吞外部改动。</summary>
        public void OnExternalEvent(FsEvent evt)
        {
            if (evt == null || evt.Path == null) return;
            if (guard != null && guard.ShouldSuppress(evt.Path, evt.AtTicks)) return;
            if (!throttle.ShouldBroadcast(evt)) return;
            if (sink != null) sink.Broadcast(evt);
            throttle.RecordBroadcast();
        }

        private void OnChanged(object sender, FileSystemEventArgs e)
        {
            PathKey key;
            if (!PathKey.TryNormalize(e.FullPath, out key)) return;
            OnExternalEvent(new FsEvent(key, FsEventKind.Changed, Now()));
        }

        private long Now()
        {
            return clock == null ? 1L : clock.NowTicks();
        }

        private void DisposeWatcher()
        {
            if (watcher == null) return;
            watcher.EnableRaisingEvents = false;
            watcher.Changed -= OnChanged;
            watcher.Created -= OnChanged;
            watcher.Deleted -= OnChanged;
            watcher.Dispose();
            watcher = null;
        }

        public void Dispose()
        {
            DisposeWatcher();
        }
    }
}
