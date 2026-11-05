using System;
using System.Collections.Generic;
using System.Threading;
using QuadLite.Core;

namespace QuadLite.Tests
{
    /// <summary>全部 B2 门卡都跑在注入的 Fake 端口上，不依赖真实 shell / 文件系统。</summary>
    internal sealed class FakeClock : IClock
    {
        private long now;

        public FakeClock(long start) { now = start; }
        public long NowTicks() { return now; }
        public void Advance(long delta) { now += delta; }
        public void Set(long value) { now = value; }
    }

    internal sealed class FakeFileOperator : IFileOperator
    {
        private readonly List<OpItem> items = new List<OpItem>();
        private int callbacks;
        private bool cancelled;
        private ErrorClass error = ErrorClass.None;

        public int ExecuteCount;
        public OpRequest LastRequest;

        public void AddSuccess(PathKey source, PathKey target)
        {
            items.Add(new OpItem(source, target, 0, ""));
        }

        public void AddFailure(PathKey source, int hr, string reason)
        {
            items.Add(new OpItem(source, null, hr, reason));
        }

        public void SetOutcome(bool isCancelled, ErrorClass errorClass)
        {
            cancelled = isCancelled;
            error = errorClass;
        }

        public OpRecord Execute(OpRequest request, IProgressSink sink, CancellationToken token)
        {
            ExecuteCount++;
            LastRequest = request;
            if (sink != null)
            {
                for (int i = 0; i < items.Count; i++)
                {
                    sink.OnItem(i, items[i].Hr, items[i].ActualTarget, items[i].Reason);
                    callbacks++;
                }
                sink.OnDone(cancelled);
            }
            return new OpRecord(items, callbacks, cancelled, error);
        }
    }

    internal sealed class FakeDangerPolicy : IDangerPolicy
    {
        public GuardVerdict Verdict = GuardVerdict.Allow;
        public int DecideCount;

        public GuardVerdict Decide(OpRequest request)
        {
            DecideCount++;
            return Verdict;
        }
    }

    internal sealed class FakePreflight : IPreflight
    {
        public bool HasConflict;
        public bool IsReadOnly;
        public bool IsLocked;
        public ErrorClass Error = ErrorClass.None;
        public int CheckCount;

        public PreflightResult Check(OpRequest request)
        {
            CheckCount++;
            return new PreflightResult(HasConflict, IsReadOnly, IsLocked, Error);
        }
    }

    internal sealed class FakeRefreshSink : IRefreshSink
    {
        public int Count;
        public FsEvent Last;

        public void Broadcast(FsEvent evt)
        {
            Count++;
            Last = evt;
        }
    }
}
