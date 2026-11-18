using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using QuadLite.Core;

namespace QuadLite.Fs
{
    /// <summary>
    /// R6 阶段 1：transport 真机证据探针。
    ///
    /// 只在显式 --diagnose 时由产品入口调用；不参与 Normal 路径，不创建窗口，
    /// 不碰用户文件：所有读写都发生在系统 temp 下、带唯一前缀的自建目录内，
    /// 结束即清理。显式 Enable 一个全新的 ShellTransport，跑 copy → move →
    /// delete(回收站) 三种 OpKind，输出逐项结构化结论（hr / 真实目标 / 存在性），
    /// 不写日志原文、不冒充「已验证」。
    ///
    /// 本类不引入任何 COM 声明（复用 ShellComInterop.cs 的白名单声明），
    /// 只是 IShellTransport 的一个真机调用点。C# 5 语法子集。
    /// </summary>
    public sealed class ShellTransportProbe
    {
        private sealed class Sink : IProgressSink
        {
            public int Callbacks;
            public int LastHr;

            public void OnItem(int index, int hr, PathKey actualTarget, string reason)
            {
                Callbacks++;
                LastHr = hr;
            }

            public void OnDone(bool cancelled) { }
        }

        public static string Run()
        {
            string root = Path.Combine(Path.GetTempPath(),
                                       "quadlite-r6-" + Guid.NewGuid().ToString("N"));
            string srcDir = Path.Combine(root, "src");
            string dstDir = Path.Combine(root, "dst");
            string movedDir = Path.Combine(root, "moved");
            string srcFile = Path.Combine(srcDir, "r6-copy.txt");
            string copiedFile = Path.Combine(dstDir, "r6-copy.txt");
            string movedFile = Path.Combine(movedDir, "r6-copy.txt");
            List<string> lines = new List<string>();

            try
            {
                Directory.CreateDirectory(srcDir);
                Directory.CreateDirectory(dstDir);
                Directory.CreateDirectory(movedDir);
                File.WriteAllText(srcFile, "quadlite-r6-probe", System.Text.Encoding.UTF8);

                PathKey srcKey;
                PathKey dstKey;
                PathKey copiedKey;
                PathKey movedDirKey;
                if (!PathKey.TryNormalize(srcFile, out srcKey) ||
                    !PathKey.TryNormalize(dstDir, out dstKey) ||
                    !PathKey.TryNormalize(copiedFile, out copiedKey) ||
                    !PathKey.TryNormalize(movedDir, out movedDirKey))
                {
                    lines.Add("probe-transport: path-normalize-failed");
                    return string.Join(" | ", lines.ToArray());
                }

                ShellTransport transport = new ShellTransport();
                transport.Enable();

                List<PathKey> one = new List<PathKey>();
                one.Add(srcKey);

                OpRecord copy = RunOne(transport, OpKind.Copy, one, dstKey);
                bool copyExists = File.Exists(copiedFile);
                lines.Add("copy: error=" + copy.Error + " callbacks=" + copy.CallbackCount +
                          " items=" + copy.Items.Count + " target-exists=" +
                          (copyExists ? "true" : "false"));

                if (copyExists)
                {
                    // IFileOperation::MoveItem 的目标必须是「文件夹」，不是文件路径。
                    // 用独立的空 movedDir，避免与 srcDir 的同名原始文件冲突造成假 hr。
                    List<PathKey> moved = new List<PathKey>();
                    moved.Add(copiedKey);
                    OpRecord move = RunOne(transport, OpKind.Move, moved, movedDirKey);
                    bool moveExists = File.Exists(movedFile);
                    bool copyGone = !File.Exists(copiedFile);
                    lines.Add("move: error=" + move.Error + " callbacks=" + move.CallbackCount +
                              " items=" + move.Items.Count + " target-exists=" +
                              (moveExists ? "true" : "false") + " source-gone=" +
                              (copyGone ? "true" : "false"));
                }
                else
                {
                    lines.Add("move: skipped (copy did not produce target)");
                }

                List<PathKey> toDelete = new List<PathKey>();
                PathKey deleteKey;
                if (PathKey.TryNormalize(srcFile, out deleteKey))
                    toDelete.Add(deleteKey);
                if (File.Exists(srcFile))
                {
                    OpRecord del = RunOne(transport, OpKind.Recycle, toDelete, null);
                    bool gone = !File.Exists(srcFile);
                    lines.Add("delete: error=" + del.Error + " callbacks=" + del.CallbackCount +
                              " items=" + del.Items.Count + " source-removed=" +
                              (gone ? "true" : "false"));
                }
                else
                {
                    lines.Add("delete: skipped (no source to recycle)");
                }

                lines.Insert(0, "probe-transport: enabled=true evidence=runtime-live");
                return string.Join(" | ", lines.ToArray());
            }
            catch (Exception ex)
            {
                lines.Add("probe-transport: exception=" + ex.GetType().Name);
                return string.Join(" | ", lines.ToArray());
            }
            finally
            {
                try { Directory.Delete(root, true); }
                catch (Exception) { }
            }
        }

        /// <summary>
        /// 每个 OpKind 都必须用「以自身源集合为 Selection」的快照：OpPlanBuilder 要求
        /// Sources == Target.Selection，否则 TryBuild 拒绝（表现为 0 回调的 AccessDenied）。
        /// 早先三个操作共用一个 copy 快照，导致 move 源（copiedKey）与快照源不一致而被拒；
        /// 这是探针构造缺陷，不是 ShellTransport / COM 的问题。
        /// </summary>
        private static OpRecord RunOne(ShellTransport transport, OpKind kind,
                                       IList<PathKey> sources, PathKey destination)
        {
            MenuTarget snapshot = new MenuTarget(sources, sources[0], 1,
                                                 DateTime.UtcNow.Ticks);
            OpRequest request = new OpRequest(snapshot, kind, sources, destination,
                                              CollisionPolicy.Overwrite);
            Sink sink = new Sink();
            return transport.Execute(request, sink, CancellationToken.None);
        }
    }
}
