using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using QuadLite.Core;

namespace QuadLite.Host
{
    /// <summary>
    /// R1 最小 WPF 主窗口：纯 C# 构造控件树（不引入 XAML 编译任务），
    /// 真实启动消息循环，并把四档布局槽位从 Core 的 LayoutMath 取出来展示。
    /// 这里没有列表控件、拖放、目录枚举、系统原生菜单与图标渲染；
    /// 状态栏逐项如实写明未实现，不把骨架写成可用功能。
    /// </summary>
    public sealed class MainWindow : Window
    {
        public MainWindow()
        {
            this.Title = "QuadLite - R1 real host skeleton";
            this.Width = 960;
            this.Height = 640;
            this.MinWidth = 640;
            this.MinHeight = 420;
            this.Background = Brushes.White;

            Grid root = new Grid();
            RowDefinition headerRow = new RowDefinition();
            headerRow.Height = GridLength.Auto;
            root.RowDefinitions.Add(headerRow);
            RowDefinition contentRow = new RowDefinition();
            contentRow.Height = new GridLength(1.0, GridUnitType.Star);
            root.RowDefinitions.Add(contentRow);
            RowDefinition statusRow = new RowDefinition();
            statusRow.Height = GridLength.Auto;
            root.RowDefinitions.Add(statusRow);

            TextBlock header = new TextBlock();
            header.Text = "QuadLite / R1 真实宿主骨架（单文件 exe，规格 v1.2 四档布局）";
            header.FontSize = 18;
            header.Margin = new Thickness(16, 14, 16, 8);
            Grid.SetRow(header, 0);
            root.Children.Add(header);

            StackPanel panel = new StackPanel();
            panel.Margin = new Thickness(16, 8, 16, 8);
            Grid.SetRow(panel, 1);

            TextBlock intro = new TextBlock();
            intro.Text = "本窗口是真实 WPF 消息循环的最小载体；下列布局槽位取自 Core 的 LayoutMath，"
                       + "但窗格内容、目录枚举与交互尚未接入：";
            intro.TextWrapping = TextWrapping.Wrap;
            intro.Margin = new Thickness(0, 0, 0, 12);
            panel.Children.Add(intro);

            LayoutMode[] modes = new LayoutMode[] {
                LayoutMode.One, LayoutMode.Two, LayoutMode.Three, LayoutMode.Four
            };
            for (int i = 0; i < modes.Length; i++)
            {
                PaneSlot[] slots = LayoutMath.Slots(modes[i]);
                TextBlock line = new TextBlock();
                line.Text = "layout: mode=" + ((int)modes[i]).ToString()
                          + " panes=" + LayoutMath.PaneCount(modes[i]).ToString()
                          + " slots=" + slots.Length.ToString()
                          + " source=LayoutMath (not rendered)";
                line.Margin = new Thickness(0, 0, 0, 4);
                panel.Children.Add(line);
            }

            TextBlock boundary = new TextBlock();
            boundary.Text = "未实现 / 未验证：目录枚举与七档视图、文件搬运与撤销、系统原生右键菜单 COM 宿主、"
                          + "图标与缩略图渲染、会话与书签持久化、run.lock 与 WM_COPYDATA 转发、"
                          + "--shot / --bench / --dumpmenu 诊断装置。M1 MVP 不成立。";
            boundary.TextWrapping = TextWrapping.Wrap;
            boundary.Margin = new Thickness(0, 12, 0, 0);
            boundary.Foreground = Brushes.DarkRed;
            panel.Children.Add(boundary);
            root.Children.Add(panel);

            Border statusBar = new Border();
            statusBar.Background = Brushes.WhiteSmoke;
            statusBar.Padding = new Thickness(16, 8, 16, 8);
            Grid.SetRow(statusBar, 2);
            TextBlock status = new TextBlock();
            status.Text = "R1 宿主骨架 / 单实例仅 Mutex 第一段 / 能力边界见上 / "
                        + "发布线 H1-H6 与 C4 未取证，M3 不成立";
            status.TextWrapping = TextWrapping.Wrap;
            statusBar.Child = status;
            root.Children.Add(statusBar);

            this.Content = root;
        }
    }
}
