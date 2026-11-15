using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using QuadLite.Core;
using QuadLite.Fs;

namespace QuadLite.Host
{
    /// <summary>
    /// R3 主窗口：四档布局按 LayoutMath.Slots 真实落位，四个 PaneView 用 IDirSource
    /// 枚举真实目录，工具栏把复制 / 移动 / 删除接到 OperationCoordinator 与 R2 的
    /// ShellTransport（默认关闭，未启用时明确写「未执行 / 未验证」）。
    ///
    /// 这里仍是「可编译、可静态判定的浏览与搬运骨架」，不是 MVP：
    /// 拖放、系统原生右键菜单 COM 宿主、图标与缩略图渲染、七档视图与筛选、
    /// 会话与书签持久化、run.lock 与 WM_COPYDATA 转发、--shot / --bench 诊断装置
    /// 均未实现 / 未验证；真实键鼠交互与真实 shell 搬运运行期未取证。M1 MVP 不成立。
    /// </summary>
    public sealed class MainWindow : Window
    {
        private readonly Grid paneGrid;
        private readonly PaneView[] panes;
        private readonly TextBlock status;
        private readonly TextBox address;
        private readonly BrowseCommands commands;
        private LayoutMode layout = LayoutMode.Four;
        private int activePaneId = 0;

        public MainWindow()
        {
            this.Title = "QuadLite - R3 real browsing host (spec v1.2, four layouts)";
            this.Width = 1100;
            this.Height = 720;
            this.MinWidth = 720;
            this.MinHeight = 480;
            this.Background = Brushes.White;

            commands = new BrowseCommands(null);
            IDirSource source = new DirSource();
            PaneState[] states = PaneSet.Create();
            panes = new PaneView[LayoutMath.MaxPanes];
            for (int i = 0; i < panes.Length; i++)
            {
                panes[i] = new PaneView(i, states[i], source);
                int id = i;
                panes[i].SetStatusSink(delegate(string text) { OnPaneStatus(id, text); });
            }

            Grid root = new Grid();
            root.RowDefinitions.Add(AutoRow());
            root.RowDefinitions.Add(StarRow());
            root.RowDefinitions.Add(AutoRow());

            StackPanel toolbar = new StackPanel();
            toolbar.Orientation = Orientation.Horizontal;
            toolbar.Margin = new Thickness(8.0, 8.0, 8.0, 4.0);
            Grid.SetRow(toolbar, 0);

            LayoutMode[] modes = new LayoutMode[] {
                LayoutMode.One, LayoutMode.Two, LayoutMode.Three, LayoutMode.Four
            };
            for (int i = 0; i < modes.Length; i++)
            {
                LayoutMode mode = modes[i];
                Button b = MakeButton(((int)mode).ToString() + " 窗格",
                    delegate(object sender, RoutedEventArgs e) { SetLayout(mode); });
                toolbar.Children.Add(b);
            }
            toolbar.Children.Add(MakeButton("上级", delegate(object s, RoutedEventArgs e)
            {
                if (!panes[activePaneId].GoUp()) UpdateStatus("已在根目录，无法返回上级");
            }));
            toolbar.Children.Add(MakeButton("刷新", delegate(object s, RoutedEventArgs e)
            {
                panes[activePaneId].RefreshInPlace();
            }));
            toolbar.Children.Add(MakeButton("复制", delegate(object s, RoutedEventArgs e)
            {
                RunCommand(OpKind.Copy);
            }));
            toolbar.Children.Add(MakeButton("移动", delegate(object s, RoutedEventArgs e)
            {
                RunCommand(OpKind.Move);
            }));
            toolbar.Children.Add(MakeButton("删除", delegate(object s, RoutedEventArgs e)
            {
                RunCommand(OpKind.Delete);
            }));
            root.Children.Add(toolbar);

            address = new TextBox();
            address.Width = 380.0;
            address.Margin = new Thickness(6.0, 0.0, 6.0, 0.0);
            address.VerticalAlignment = VerticalAlignment.Center;
            toolbar.Children.Add(address);
            toolbar.Children.Add(MakeButton("转到", delegate(object s, RoutedEventArgs e)
            {
                GoToAddress();
            }));

            paneGrid = new Grid();
            Grid.SetRow(paneGrid, 1);
            paneGrid.Margin = new Thickness(8.0, 4.0, 8.0, 4.0);
            for (int i = 0; i < LayoutMath.GridSize; i++)
            {
                RowDefinition row = new RowDefinition();
                row.Height = i == 1 ? new GridLength(4.0) : new GridLength(1.0, GridUnitType.Star);
                paneGrid.RowDefinitions.Add(row);
                ColumnDefinition col = new ColumnDefinition();
                col.Width = i == 1 ? new GridLength(4.0) : new GridLength(1.0, GridUnitType.Star);
                paneGrid.ColumnDefinitions.Add(col);
            }
            for (int i = 0; i < panes.Length; i++) paneGrid.Children.Add(panes[i]);
            root.Children.Add(paneGrid);

            Border statusBar = new Border();
            statusBar.Background = Brushes.WhiteSmoke;
            statusBar.Padding = new Thickness(10.0, 6.0, 10.0, 6.0);
            Grid.SetRow(statusBar, 2);
            status = new TextBlock();
            status.TextWrapping = TextWrapping.Wrap;
            statusBar.Child = status;
            root.Children.Add(statusBar);

            this.Content = root;
            ApplyLayout();

            PathKey start;
            if (PathKey.TryNormalize(Environment.CurrentDirectory, out start))
            {
                address.Text = start.Value;
                panes[0].Navigate(start);
            }
            else
            {
                UpdateStatus("起始路径无法规范化，请在地址栏输入绝对路径");
            }
        }

        private static RowDefinition AutoRow()
        {
            RowDefinition row = new RowDefinition();
            row.Height = GridLength.Auto;
            return row;
        }

        private static RowDefinition StarRow()
        {
            RowDefinition row = new RowDefinition();
            row.Height = new GridLength(1.0, GridUnitType.Star);
            return row;
        }

        private static Button MakeButton(string label, RoutedEventHandler handler)
        {
            Button b = new Button();
            b.Content = label;
            b.Margin = new Thickness(0.0, 0.0, 6.0, 0.0);
            b.Padding = new Thickness(8.0, 3.0, 8.0, 3.0);
            b.Click += handler;
            return b;
        }

        private void SetLayout(LayoutMode mode)
        {
            if (!LayoutMath.IsKnownMode((int)mode)) return;
            layout = mode;
            ApplyLayout();
        }

        private void ApplyLayout()
        {
            PaneSlot[] slots = LayoutMath.Slots(layout);
            for (int i = 0; i < panes.Length; i++)
            {
                panes[i].Visibility = LayoutMath.IsPaneVisible(layout, i)
                    ? Visibility.Visible : Visibility.Collapsed;
            }
            for (int i = 0; i < slots.Length; i++)
            {
                PaneSlot slot = slots[i];
                PaneView view = panes[slot.PaneIndex];
                Grid.SetRow(view, slot.Row);
                Grid.SetColumn(view, slot.Column);
                Grid.SetRowSpan(view, slot.RowSpan);
                Grid.SetColumnSpan(view, slot.ColumnSpan);
            }
            activePaneId = LayoutMath.ResolveActivePane(layout, activePaneId);
            UpdateStatus("layout=" + ((int)layout).ToString() +
                         " panes=" + LayoutMath.PaneCount(layout).ToString() +
                         " active=" + activePaneId.ToString());
        }

        private void GoToAddress()
        {
            PathKey key;
            AddressReject reject;
            if (!AddressRules.TryParse(address.Text, out key, out reject) ||
                !AddressRules.ShouldNavigate(reject))
            {
                UpdateStatus("地址未通过校验：" + AddressRules.RejectText(reject));
                return;
            }
            panes[activePaneId].Navigate(key);
        }

        private void RunCommand(OpKind kind)
        {
            PaneView active = panes[activePaneId];
            IList<PathKey> selection = active.Selected();
            PathKey destination = null;
            if (OpPlanBuilder.NeedsDestination(kind))
            {
                destination = OtherPanePath(activePaneId);
                if (destination == null)
                {
                    UpdateStatus("搬运未执行：需要第二个可见窗格作为目标（未验证）");
                    return;
                }
            }
            UpdateStatus(commands.Execute(kind, selection, active.Directory,
                                          destination, activePaneId));
        }

        private PathKey OtherPanePath(int excludingPaneId)
        {
            for (int i = 0; i < panes.Length; i++)
            {
                if (i == excludingPaneId || !LayoutMath.IsPaneVisible(layout, i)) continue;
                if (panes[i].Directory != null) return panes[i].Directory;
            }
            return null;
        }

        private void OnPaneStatus(int paneId, string text)
        {
            if (paneId != activePaneId) return;
            UpdateStatus(text);
        }

        private void UpdateStatus(string text)
        {
            status.Text = text +
                " | transport=" + (commands.TransportEnabled ? "enabled" : "disabled by default / 未验证") +
                " | 未实现 / 未验证：拖放、系统原生菜单 COM 宿主、图标缩略图渲染、" +
                "七档视图与筛选、会话与书签、run.lock 与 WM_COPYDATA 转发、" +
                "--shot / --bench 诊断装置；真实交互与 shell 运行期未取证；" +
                "发布线 H1-H6 与 C4 未取证，M1 MVP 不成立";
        }
    }
}
