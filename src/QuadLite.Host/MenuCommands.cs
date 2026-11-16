using System;
using System.Collections.Generic;
using QuadLite.Core;
using QuadLite.Fs;

namespace QuadLite.Host
{
    /// <summary>
    /// R4：把系统原生菜单宿主接到用户可触发的右键上，Host 层只做三件事：
    /// 捕获打开右键那一刻的不可变 MenuTarget 快照、把自有项标签交给 Fs 层、
    /// 把结构化结果经 StatusRules.ResultText 转成状态栏文案。
    ///
    /// 本层不写 COM 声明、不引入 Win32 入口、不读文件系统、不自行启用宿主。
    /// NativeMenuHost 默认关闭，未显式 Enable 时只回「未执行 / 未验证」；
    /// 真实 shell 扩展与消息循环运行期未取证，M1 / M2 / M3 仍不成立。
    /// 见 docs/adr/ADR-v2-014.md。
    /// </summary>
    public sealed class MenuCommands
    {
        private readonly NativeMenuHost host = new NativeMenuHost();

        public bool Enabled { get { return host.Enabled; } }

        /// <summary>自有项标签由调用方传入：不等价命令改名、独立分组，不与系统项同名。</summary>
        public static string[] OwnLabels()
        {
            return new string[]
            {
                "复制到另一窗格…",
                "移动到另一窗格…",
                "删除（可撤销）",
                "当前文件夹属性（QuadLite）"
            };
        }

        /// <summary>唯一的右键入口。快照在 UI 线程捕获，COM 会话由 Fs 层在独立 STA 跑。</summary>
        public string Show(IList<PathKey> selection, PathKey directory, int paneId)
        {
            if (selection == null || selection.Count == 0 || directory == null)
            {
                return StatusRules.ResultText("菜单未执行：选择集或目录快照为空（未验证）");
            }
            long capturedAt = new HostClock().NowTicks();
            MenuTarget snapshot = new MenuTarget(selection, directory, paneId, capturedAt);
            MenuHostResult result = host.Show(snapshot, OwnLabels());
            if (result == null)
            {
                return StatusRules.ResultText("菜单未执行：宿主未返回结构化结果（未验证）");
            }
            return StatusRules.ResultText(result.Text);
        }

        /// <summary>产品 --selftest 可复用的零 I/O 判据，不创建窗口、不实例化 COM。</summary>
        public static bool ContractIsHonest()
        {
            return !new NativeMenuHost().Enabled &&
                   !MenuHostContractRules.IsRuntimeVerified() &&
                   MenuHostContractRules.StandardSequenceDeclared() &&
                   MenuHostContractRules.UsesImmutableSnapshot();
        }
    }
}
