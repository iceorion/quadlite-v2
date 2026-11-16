using System;
using System.Collections.Generic;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using QuadLite.Core;

namespace QuadLite.Host
{
    /// <summary>
    /// R3 真实浏览窗格：WPF ListBox 呈现 IDirSource 枚举出的条目，切目录时 generation
    /// 前进并丢弃迟到批次，双击进入子路径，状态栏计数读筛后显示行集。
    ///
    /// 本类型不直接引用 System.IO：目录读取一律经 Fs 层 IDirSource 注入；枚举在后台
    /// worker 上跑，批次经 Dispatcher 回 UI 线程。R4 起右键把打开菜单那一刻的
    /// 选中集 / 目录快照交给 MenuCommands（宿主默认关闭、运行期未验证）；
    /// 拖放、图标与缩略图渲染仍未实现 / 未验证，见 docs/adr/ADR-v2-013.md、
    /// docs/adr/ADR-v2-014.md。
    /// </summary>
    public sealed class PaneView : Border
    {
        private sealed class Entry
        {
            private readonly PathKey key;

            public Entry(PathKey key) { this.key = key; }

            public PathKey Key { get { return key; } }

            public override string ToString() { return key.GetLeafName(); }
        }

        private readonly int paneId;
        private readonly PaneState state;
        private readonly IDirSource source;
        private readonly ListBox list;
        private readonly TextBlock caption;
        private CancellationTokenSource cancel;
        private Action<string> onStatus;
        private Action<IList<PathKey>, PathKey, int> menuSink;

        public PaneView(int paneId, PaneState state, IDirSource source)
        {
            this.paneId = paneId;
            this.state = state;
            this.source = source;
            this.BorderBrush = Brushes.LightGray;
            this.BorderThickness = new Thickness(1.0);
            this.Margin = new Thickness(3.0);
            this.Background = Brushes.White;

            DockPanel dock = new DockPanel();
            caption = new TextBlock();
            caption.Text = "pane " + paneId.ToString() + " / 未加载";
            caption.Margin = new Thickness(6.0, 4.0, 6.0, 4.0);
            caption.Padding = new Thickness(2.0);
            caption.Background = Brushes.WhiteSmoke;
            DockPanel.SetDock(caption, Dock.Top);
            dock.Children.Add(caption);

            list = new ListBox();
            list.SelectionMode = SelectionMode.Extended;
            list.Margin = new Thickness(2.0);
            list.MouseDoubleClick += OnDoubleClick;
            list.MouseRightButtonUp += OnRightClick;
            dock.Children.Add(list);

            this.Child = dock;
        }

        public int PaneId { get { return paneId; } }
        public PaneState State { get { return state; } }
        public PathKey Directory { get { return state.Path; } }
        public int DisplayedCount { get { return list.Items.Count; } }

        public void SetStatusSink(Action<string> sink) { onStatus = sink; }

        /// <summary>右键回调：由 MainWindow 把快照交给 R4 菜单宿主，本类不碰 COM。</summary>
        public void SetMenuSink(Action<IList<PathKey>, PathKey, int> sink) { menuSink = sink; }

        /// <summary>导航：generation 前进、旧枚举取消、迟到批次丢弃。</summary>
        public void Navigate(PathKey path)
        {
            if (path == null || source == null) return;
            StopEnumeration();
            int generation = NavRules.NextGeneration(state.Generation);
            state.SetPath(path, generation);
            list.Items.Clear();
            caption.Text = "pane " + paneId.ToString() + " / " + path.Value;

            CancellationTokenSource cts = new CancellationTokenSource();
            cancel = cts;
            CancellationToken token = cts.Token;
            IDirSource dir = this.source;
            PaneView self = this;
            PathKey target = path;
            Thread worker = new Thread(delegate()
            {
                try
                {
                    dir.Enumerate(target, generation, self.OnBatch, token);
                }
                catch (Exception)
                {
                    // 枚举失败不冒充成功：结构化结果由状态栏如实显示。
                }
            });
            worker.IsBackground = true;
            worker.Start();
            Report();
        }

        /// <summary>F5 原地刷新：保视口与选中集，只推进 generation。</summary>
        public void RefreshInPlace()
        {
            if (state.Path == null) return;
            StopEnumeration();
            int generation = NavRules.NextGeneration(state.Generation);
            state.RefreshInPlace(generation);
            list.Items.Clear();
            CancellationTokenSource cts = new CancellationTokenSource();
            cancel = cts;
            IDirSource dir = this.source;
            PaneView self = this;
            PathKey target = state.Path;
            CancellationToken token = cts.Token;
            Thread worker = new Thread(delegate()
            {
                try { dir.Enumerate(target, generation, self.OnBatch, token); }
                catch (Exception) { }
            });
            worker.IsBackground = true;
            worker.Start();
            Report();
        }

        public bool GoUp()
        {
            PathKey parent = null;
            if (state.Path == null || !state.Path.TryGetParent(out parent)) return false;
            Navigate(parent);
            return true;
        }

        /// <summary>选中集只包含列表里的真实条目；组头 / 禁用项 / 筛掉的行不进集合。</summary>
        public IList<PathKey> Selected()
        {
            List<PathKey> keys = new List<PathKey>();
            for (int i = 0; i < list.SelectedItems.Count; i++)
            {
                Entry entry = list.SelectedItems[i] as Entry;
                if (entry != null) keys.Add(entry.Key);
            }
            return keys;
        }

        private void StopEnumeration()
        {
            if (cancel != null)
            {
                try { cancel.Cancel(); } catch (Exception) { }
                cancel = null;
            }
        }

        private void OnBatch(EnumBatch batch)
        {
            if (batch == null) return;
            // 迟到批次在 worker 侧就丢弃，不占用 UI 线程。
            if (EnumBatchRules.IsLateBatch(batch.Generation, state.Generation)) return;
            Dispatcher.BeginInvoke(new Action(delegate()
            {
                // 回到 UI 线程再判一次，防止排队期间又切了目录。
                if (EnumBatchRules.IsLateBatch(batch.Generation, state.Generation)) return;
                for (int i = 0; i < batch.Items.Count; i++)
                {
                    if (batch.Items[i] != null) list.Items.Add(new Entry(batch.Items[i]));
                }
                if (batch.IsLast) state.SetLoadState(PaneLoadState.Ready);
                Report();
            }));
        }

        private void OnDoubleClick(object sender, MouseButtonEventArgs e)
        {
            Entry entry = list.SelectedItem as Entry;
            if (entry == null) return;
            // 不读文件系统判断目录 / 文件：交给 IDirSource 枚举，文件会得到空批次。
            Navigate(entry.Key);
        }

        /// <summary>
        /// R4：右键那一刻取一份选中集；空白面用当前目录自身作目标。
        /// 不读实时路径、不自行构造菜单，快照交给 MenuCommands 处理。
        /// </summary>
        private void OnRightClick(object sender, MouseButtonEventArgs e)
        {
            if (menuSink == null) return;
            IList<PathKey> keys = Selected();
            if (keys.Count == 0 && state.Path != null)
            {
                List<PathKey> blank = new List<PathKey>();
                blank.Add(state.Path);
                keys = blank;
            }
            menuSink(keys, state.Path, paneId);
        }

        private void Report()
        {
            int displayed = list.Items.Count;
            Selection selection = new Selection(Selected(), displayed);
            int visible = StatusRules.VisibleCount(selection);
            bool consistent = StatusRules.CountMatchesDisplayedSet(visible, displayed) &&
                              UiContractRules.StatusFieldCount() == 3;
            if (onStatus != null)
            {
                onStatus("pane " + paneId.ToString() +
                         " rows=" + displayed.ToString() +
                         " selected=" + StatusRules.SelectedCount(selection).ToString() +
                         " counts=" + (consistent ? "consistent" : "inconsistent") +
                         " load=" + state.LoadState.ToString());
            }
        }
    }
}
