using System;
using System.Collections.Generic;
using System.Threading;

namespace QuadLite.Core
{
    public sealed class CoordinatorResult
    {
        private readonly bool accepted;
        private readonly string reason;
        private readonly OpRecord record;
        private readonly GuardVerdict guard;
        private readonly PreflightResult preflight;
        private readonly bool undoPushed;
        private readonly int refreshCount;
        private readonly OperationGateState gate;
        private readonly ErrorClass error;
        private readonly int callbackCount;

        public CoordinatorResult(bool accepted, string reason, OpRecord record, GuardVerdict guard,
                                 PreflightResult preflight, bool undoPushed, int refreshCount,
                                 OperationGateState gate, ErrorClass error, int callbackCount)
        {
            this.accepted = accepted;
            this.reason = reason;
            this.record = record;
            this.guard = guard;
            this.preflight = preflight;
            this.undoPushed = undoPushed;
            this.refreshCount = refreshCount;
            this.gate = gate;
            this.error = error;
            this.callbackCount = callbackCount;
        }

        public bool Accepted { get { return accepted; } }
        public string Reason { get { return reason; } }
        public OpRecord Record { get { return record; } }
        public GuardVerdict Guard { get { return guard; } }
        public PreflightResult Preflight { get { return preflight; } }
        public bool UndoPushed { get { return undoPushed; } }
        public int RefreshCount { get { return refreshCount; } }
        public OperationGateState Gate { get { return gate; } }
        public ErrorClass Error { get { return error; } }
        public int CallbackCount { get { return callbackCount; } }
    }

    /// <summary>
    /// L1 全局编排：串行闸门、判级、预检、端口执行、结构化结果、撤销入栈、一次刷新广播。
    /// 自身零 I/O；所有外部能力由端口注入，因此可用 Fake 跑通全部门卡。
    /// </summary>
    public sealed class OperationCoordinator
    {
        private readonly IFileOperator fileOperator;
        private readonly IDangerPolicy dangerPolicy;
        private readonly IPreflight preflight;
        private readonly IRefreshSink refreshSink;
        private readonly IClock clock;
        private readonly UndoStack undoStack = new UndoStack();
        private readonly SelfOpGuard selfOpGuard = new SelfOpGuard();
        private readonly WatchThrottle throttle = new WatchThrottle();

        private OperationGateState state = OperationGateState.Idle;
        private int executeCount;

        public OperationCoordinator(IFileOperator fileOperator, IDangerPolicy dangerPolicy,
                                    IPreflight preflight, IRefreshSink refreshSink, IClock clock)
        {
            this.fileOperator = fileOperator;
            this.dangerPolicy = dangerPolicy;
            this.preflight = preflight;
            this.refreshSink = refreshSink;
            this.clock = clock;
        }

        public OperationGateState State { get { return state; } }
        public int ExecuteCount { get { return executeCount; } }
        public int UndoCount { get { return undoStack.Count; } }
        public int BroadcastCount { get { return throttle.BroadcastCount; } }
        public SelfOpGuard GuardProbe { get { return selfOpGuard; } }
        public WatchThrottle ThrottleProbe { get { return throttle; } }
        public UndoStack UndoProbe { get { return undoStack; } }

        public bool IsChangeBlocked()
        {
            return StateMachine.IsChangeBlocked(state);
        }

        public bool IsNavigationAllowed()
        {
            return StateMachine.IsNavigationAllowed(state);
        }

        public CoordinatorResult Execute(OpRequest request)
        {
            if (request == null) return Rejected("请求为空", ErrorClass.ShellUnavailable);
            if (StateMachine.IsChangeBlocked(state)) return Rejected("操作闸门忙", ErrorClass.None);
            if (fileOperator == null) return Rejected("搬运端口不可用", ErrorClass.ShellUnavailable);

            if (!MoveTo(OperationGateState.Prechecking))
            {
                return Rejected("状态转换失败", ErrorClass.ShellUnavailable);
            }

            GuardVerdict verdict = dangerPolicy == null
                ? GuardVerdict.Reject : dangerPolicy.Decide(request);
            if (verdict == GuardVerdict.Reject)
            {
                return Finish(null, verdict, null, false, 0, "判级拒绝", ErrorClass.AccessDenied, 0);
            }

            PreflightResult pf = preflight == null ? null : preflight.Check(request);
            if (pf != null && !pf.CanProceed)
            {
                return Finish(null, verdict, pf, false, 0, "预检未通过",
                              ResultClassifier.Classify(null, pf), 0);
            }

            selfOpGuard.Begin(request.Target == null ? null : request.Target.Directory);
            for (int i = 0; i < request.Sources.Count; i++) selfOpGuard.Touch(request.Sources[i]);

            if (!MoveTo(OperationGateState.Running))
            {
                return Finish(null, verdict, pf, false, 0, "状态转换失败", ErrorClass.ShellUnavailable, 0);
            }

            CallbackCounter counter = new CallbackCounter();
            OpRecord record;
            try
            {
                record = fileOperator.Execute(request, counter, CancellationToken.None);
            }
            catch (Exception)
            {
                selfOpGuard.End(Now());
                return Finish(null, verdict, pf, false, 0, "执行异常", ErrorClass.ShellUnavailable, 0);
            }
            executeCount++;
            selfOpGuard.End(Now());

            ErrorClass error = ResultClassifier.Classify(record, pf);
            bool undoPushed = undoStack.Push(record, request.Kind, Now());

            int refreshCount = 0;
            if (refreshSink != null && record != null && record.Items.Count > 0)
            {
                FsEvent evt = new FsEvent(request.Target == null ? null : request.Target.Directory,
                                          FsEventKind.Changed, Now());
                if (throttle.ShouldBroadcast(evt))
                {
                    refreshSink.Broadcast(evt);
                    throttle.RecordBroadcast();
                    refreshCount = 1;
                }
            }

            if (record != null && record.Cancelled)
            {
                MoveTo(OperationGateState.Cancelled);
                MoveTo(OperationGateState.Reporting);
            }
            else
            {
                MoveTo(OperationGateState.Reporting);
            }
            MoveTo(OperationGateState.Idle);

            return new CoordinatorResult(true, "", record, verdict, pf, undoPushed,
                                         refreshCount, state, error, counter.Count);
        }

        /// <summary>撤销反向调同一端口；一批算一步，跨窗格共享同一栈。</summary>
        public bool UndoLast()
        {
            UndoStep step = undoStack.Peek();
            if (step == null || fileOperator == null) return false;
            if (StateMachine.IsChangeBlocked(state)) return false;

            List<PathKey> sources = new List<PathKey>();
            for (int i = 0; i < step.Items.Count; i++)
            {
                if (step.Items[i].ActualTarget != null) sources.Add(step.Items[i].ActualTarget);
            }
            if (sources.Count == 0) return false;

            PathKey destination = step.Items[0].Source == null
                ? null : step.Items[0].Source;
            if (destination == null) return false;

            MenuTarget snapshot = new MenuTarget(sources, destination, 0, Now());
            OpRequest reverse = new OpRequest(snapshot, OpKind.Move, sources,
                                              destination, CollisionPolicy.Ask);
            CallbackCounter counter = new CallbackCounter();
            fileOperator.Execute(reverse, counter, CancellationToken.None);
            undoStack.Pop();
            return true;
        }

        private CoordinatorResult Rejected(string reason, ErrorClass error)
        {
            return new CoordinatorResult(false, reason, null, GuardVerdict.Reject, null,
                                         false, 0, state, error, 0);
        }

        private CoordinatorResult Finish(OpRecord record, GuardVerdict verdict,
                                         PreflightResult pf, bool undoPushed, int refreshCount,
                                         string reason, ErrorClass error, int callbackCount)
        {
            // 拒绝 / 异常也必须走完状态机回到 Idle，否则闸门会永久卡在忙态。
            if (state == OperationGateState.Prechecking)
            {
                MoveTo(OperationGateState.Running);
                MoveTo(OperationGateState.Cancelled);
                MoveTo(OperationGateState.Reporting);
            }
            else if (state == OperationGateState.Running)
            {
                MoveTo(OperationGateState.Reporting);
            }
            MoveTo(OperationGateState.Idle);
            return new CoordinatorResult(false, reason, record, verdict, pf, undoPushed,
                                         refreshCount, state, error, callbackCount);
        }

        private bool MoveTo(OperationGateState next)
        {
            if (!StateMachine.CanTransition(state, next)) return false;
            state = next;
            return true;
        }

        private long Now()
        {
            return clock == null ? 1L : clock.NowTicks();
        }

        private sealed class CallbackCounter : IProgressSink
        {
            private int count;

            public int Count { get { return count; } }
            public void OnItem(int index, int hr, PathKey actualTarget, string reason) { count++; }
            public void OnDone(bool cancelled) { }
        }
    }
}
