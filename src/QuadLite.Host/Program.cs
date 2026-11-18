using System;
using System.Threading;
using System.Windows;
using QuadLite.Core;
using QuadLite.Fs;

namespace QuadLite.Host
{
    /// <summary>
    /// R1 真实宿主入口：仓库第一次产出可编译、可运行的 WPF 单文件 exe。
    /// 规格 design-v2.md v1.2 未改动；本层只接「进程入口 + CLI 分派 + 最小窗口」，
    /// 浏览 / 搬运 / 原生右键菜单 / 图标缩略图 / 会话持久化仍未实现，如实标注。
    /// </summary>
    public static class Program
    {
        [STAThread]
        public static int Main(string[] args)
        {
            CliParseResult cli = CliArgs.Parse(args);
            if (!cli.Ok)
            {
                Console.WriteLine("QuadLite: " + cli.Error);
                Console.WriteLine("QuadLite: legal parameters: " + CliArgs.LegalParameters());
                return 2;
            }

            // 诊断态不创建窗口：CI 可在无交互桌面上真跑产品 exe 的自测。
            if (cli.Mode == CliMode.SelfTest)
            {
                return SelftestRunner.Run();
            }

            // R6 阶段 0 + 1：--diagnose 接真实证据探针，不创建窗口、不碰用户文件。
            // 阶段 0 只判「shell COM 能否实例化」；阶段 1 在系统 temp 的自建目录内
            // 显式启用 ShellTransport 跑 copy/move/delete，逐项输出结构化结论。
            // 结论由 stdout 如实输出，不预先写成「已验证」，也不冒充 M1 成立。
            if (cli.Mode == CliMode.Diagnose)
            {
                Console.WriteLine("QuadLite: " + ShellComProbe.RunInstantiationProbe());
                Console.WriteLine("QuadLite: " + ShellTransportProbe.Run());
                return 0;
            }

            if (cli.Mode == CliMode.Shot || cli.Mode == CliMode.ShellMenuHost)
            {
                Console.WriteLine("QuadLite: mode=" + cli.Mode.ToString() +
                                  " status=未实现 / 未验证（诊断装置尚未接入真实设备）");
                return 3;
            }

            // §5.13 单实例三段机制：本批只落地 Mutex 第一段；
            // run.lock 与 WM_COPYDATA('QL') 转发仍未实现，不冒充完整单实例。
            bool createdNew;
            using (Mutex mutex = new Mutex(true, "QuadLite.SingleInstance", out createdNew))
            {
                if (!createdNew)
                {
                    Console.WriteLine("QuadLite: another instance is running; " +
                                      "WM_COPYDATA forwarding not implemented (未验证)");
                    return 0;
                }
                Application app = new Application();
                MainWindow window = new MainWindow();
                return app.Run(window);
            }
        }
    }
}
