using System;
using System.Collections.Generic;
using System.Threading;

namespace QuadLite.Core
{
    /// <summary>判级结论（由注入的判级端口给出，编排层只消费结果）。</summary>
    public enum GuardVerdict { Allow, Confirm, TypePath, Reject }

    public enum FsEventKind { Created, Changed, Renamed, Deleted }

    public enum DropEffect { None, Copy, Move, Link }

    /// <summary>一次文件系统通知；时间由调用方注入，便于纯函数判定。</summary>
    public sealed class FsEvent
    {
        private readonly PathKey path;
        private readonly FsEventKind kind;
        private readonly long atTicks;

        public FsEvent(PathKey path, FsEventKind kind, long atTicks)
        {
            this.path = path;
            this.kind = kind;
            this.atTicks = atTicks;
        }

        public PathKey Path { get { return path; } }
        public FsEventKind Kind { get { return kind; } }
        public long AtTicks { get { return atTicks; } }
    }

    /// <summary>枚举分批；迟到批次由 generation 丢弃。</summary>
    public sealed class EnumBatch
    {
        private readonly int generation;
        private readonly IReadOnlyList<PathKey> items;
        private readonly bool isLast;

        public EnumBatch(int generation, IList<PathKey> items, bool isLast)
        {
            List<PathKey> copy = new List<PathKey>();
            if (items != null)
            {
                for (int i = 0; i < items.Count; i++) copy.Add(items[i]);
            }
            this.generation = generation;
            this.items = new System.Collections.ObjectModel.ReadOnlyCollection<PathKey>(copy);
            this.isLast = isLast;
        }

        public int Generation { get { return generation; } }
        public IReadOnlyList<PathKey> Items { get { return items; } }
        public bool IsLast { get { return isLast; } }
    }

    public sealed class PreflightResult
    {
        private readonly bool hasConflict;
        private readonly bool isReadOnly;
        private readonly bool isLocked;
        private readonly ErrorClass error;

        public PreflightResult(bool hasConflict, bool isReadOnly, bool isLocked, ErrorClass error)
        {
            this.hasConflict = hasConflict;
            this.isReadOnly = isReadOnly;
            this.isLocked = isLocked;
            this.error = error;
        }

        public bool HasConflict { get { return hasConflict; } }
        public bool IsReadOnly { get { return isReadOnly; } }
        public bool IsLocked { get { return isLocked; } }
        public ErrorClass Error { get { return error; } }
        public bool CanProceed { get { return !isReadOnly && !isLocked && error == ErrorClass.None; } }
    }

    /// <summary>逐项回调；CallbackCount 以回调次数为准，不用源集合长度代替。</summary>
    public interface IProgressSink
    {
        void OnItem(int index, int hr, PathKey actualTarget, string reason);
        void OnDone(bool cancelled);
    }

    /// <summary>所有搬运的唯一入口。真实系统实现在 Fs 层，测试用 Fake。</summary>
    public interface IFileOperator
    {
        OpRecord Execute(OpRequest request, IProgressSink sink, CancellationToken token);
    }

    public interface IClipboard
    {
        ClipboardData Read();
        bool Write(ClipboardData data);
    }

    public interface IShellVerbs
    {
        void Open(MenuTarget target);
        void OpenWith(MenuTarget target);
        void Properties(MenuTarget target);
    }

    public interface IFsWatch
    {
        void Subscribe(PathKey path, Action<FsEvent> handler);
        void Unsubscribe(PathKey path, Action<FsEvent> handler);
    }

    public interface IDirSource
    {
        void Enumerate(PathKey path, int generation, Action<EnumBatch> batch, CancellationToken token);
    }

    public interface IDangerPolicy
    {
        GuardVerdict Decide(OpRequest request);
    }

    public interface IPreflight
    {
        PreflightResult Check(OpRequest request);
    }

    public interface IClock
    {
        long NowTicks();
    }

    /// <summary>刷新广播的接收端；全局只有一份，避免刷新风暴。</summary>
    public interface IRefreshSink
    {
        void Broadcast(FsEvent evt);
    }

    /// <summary>真实 shell 搬运的注入点：成熟 COM 绑定接入前不由本层直接调用。</summary>
    public interface IShellTransport
    {
        OpRecord Execute(OpRequest request, IProgressSink sink, CancellationToken token);
    }

    public interface IClipboardTransport
    {
        bool Write(ClipboardData data);
        ClipboardData Read();
    }

    public interface IWatchTransport
    {
        void Subscribe(string path);
        void Unsubscribe(string path);
    }
}
