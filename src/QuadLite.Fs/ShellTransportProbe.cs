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
            string srcFile = Path.Combine(srcDir, "r6-copy.txt");
            string copiedFile = Path.Combine(dstDir, "r6-copy.txt");
            List<string> lines = new List<string>();

            try
            {
                Directory.CreateDirectory(srcDir);
                Directory.CreateDirectory(dstDir);
                File.WriteAllText(srcFile, "quadlite-r6-probe", System.Text.Encoding.UTF8);

                PathKey srcKey;
                PathKey dstKey;
                PathKey copiedKey;
                if (!PathKey.TryNormalize(srcFile, out srcKey) ||
                    !PathKey.TryNormalize(dstDir, out dstKey) ||
                    !PathKey.TryNormalize(copiedFile, out copiedKey))
                {
                    lines.Add("probe-transport: path-normalize-failed");
                    return string.Join(" | ", lines.ToArray());
                }

                ShellTransport transport = new ShellTransport();
                transport.Enable();

                List<PathKey> one = new List<PathKey>();
                one.Add(srcKey);
                MenuTarget snapshot = new MenuTarget(one, srcKey, 0,
                                                     DateTime.UtcNow.Ticks);

                OpRecord copy = RunOne(transport, snapshot, OpKind.Copy, one, dstKey);
                bool copyExists = File.Exists(copiedFile);
                lines.Add("copy: error=" + copy.Error + " callbacks=" + copy.CallbackCount +
                          " items=" + copy.Items.Count + " target-exists=" +
                          (copyExists ? "true" : "false"));

                if (copyExists)
                {
                    List<PathKey> moved = new List<PathKey>();
                    moved.Add(copiedKey);
                    OpRecord move = RunOne(transport, snapshot, OpKind.Move, moved, srcKey);
                    bool moveExists = File.Exists(Path.Combine(srcDir, "r6-copy.txt"));
                    lines.Add("move: error=" + move.Error + " callbacks=" + move.CallbackCount +
                              " items=" + move.Items.Count + " target-exists=" +
                              (moveExists ? "true" : "false"));
                }
                else
                {
                    lines.Add("move: skipped (copy did not produce target)");
                }

                List<PathKey> toDelete = new List<PathKey>();
                PathKey deleteKey;
                if (PathKey.TryNormalize(Path.Combine(srcDir, "r6-copy.txt"), out deleteKey))
                    toDelete.Add(deleteKey);
                if (File.Exists(Path.Combine(srcDir, "r6-copy.txt")))
                {
                    OpRecord del = RunOne(transport, snapshot, OpKind.Recycle, toDelete, null);
                    bool gone = !File.Exists(Path.Combine(srcDir, "r6-copy.txt"));
                    lines.Add("delete: error=" + del.Error + " callbacks=" + del.CallbackCount +
                              " items=" + del.Items.Count + " source-removed=" +
                              (gone ? "true" : "false"));
                }
                else
                {
                    lines.Add("delete: skipped (no source to recycle)");
                }

                lines.Insert(0, "probe-transport: enabled=true evidence=真机实测(运行态)");
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

        private static OpRecord RunOne(ShellTransport transport, MenuTarget snapshot,
                                       OpKind kind, IList<PathKey> sources, PathKey destination)
        {
            OpRequest request = new OpRequest(snapshot, kind, sources, destination,
                                              CollisionPolicy.Overwrite);
            Sink sink = new Sink();
            return transport.Execute(request, sink, CancellationToken.None);
        }
    }
}
