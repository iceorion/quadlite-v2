using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace QuadLite.Core
{
    public enum OpKind { Copy, Move, Delete, Recycle, Rename, NewFolder, PermanentDelete, CutPaste }
    public enum CollisionPolicy { Ask, Rename, Overwrite, Skip }

    /// <summary>右键那一刻的不可变快照：派发不得读实时路径。</summary>
    public sealed class MenuTarget
    {
        private readonly IReadOnlyList<PathKey> selection;
        private readonly PathKey directory;
        private readonly int paneId;
        private readonly long capturedAt;

        public MenuTarget(IList<PathKey> selection, PathKey directory, int paneId, long capturedAt)
        {
            List<PathKey> copy = new List<PathKey>();
            if (selection != null)
            {
                for (int i = 0; i < selection.Count; i++) copy.Add(selection[i]);
            }
            this.selection = new ReadOnlyCollection<PathKey>(copy);
            this.directory = directory;
            this.paneId = paneId;
            this.capturedAt = capturedAt;
        }

        public IReadOnlyList<PathKey> Selection { get { return selection; } }
        public PathKey Directory { get { return directory; } }
        public int PaneId { get { return paneId; } }
        public long CapturedAt { get { return capturedAt; } }
    }

    public sealed class OpRequest
    {
        private readonly MenuTarget target;
        private readonly OpKind kind;
        private readonly IReadOnlyList<PathKey> sources;
        private readonly PathKey destination;
        private readonly CollisionPolicy policy;

        public OpRequest(MenuTarget target, OpKind kind, IList<PathKey> sources,
                         PathKey destination, CollisionPolicy policy)
        {
            List<PathKey> copy = new List<PathKey>();
            if (sources != null)
            {
                for (int i = 0; i < sources.Count; i++) copy.Add(sources[i]);
            }
            this.target = target;
            this.kind = kind;
            this.sources = new ReadOnlyCollection<PathKey>(copy);
            this.destination = destination;
            this.policy = policy;
        }

        public MenuTarget Target { get { return target; } }
        public OpKind Kind { get { return kind; } }
        public IReadOnlyList<PathKey> Sources { get { return sources; } }
        public PathKey Destination { get { return destination; } }
        public CollisionPolicy Policy { get { return policy; } }
    }

    public sealed class OpPlan
    {
        private readonly OpKind kind;
        private readonly IReadOnlyList<PathKey> sources;
        private readonly PathKey destination;
        private readonly CollisionPolicy policy;

        public OpPlan(OpKind kind, IReadOnlyList<PathKey> sources, PathKey destination, CollisionPolicy policy)
        {
            List<PathKey> copy = new List<PathKey>();
            for (int i = 0; i < sources.Count; i++) copy.Add(sources[i]);
            this.kind = kind;
            this.sources = new ReadOnlyCollection<PathKey>(copy);
            this.destination = destination;
            this.policy = policy;
        }

        public OpKind Kind { get { return kind; } }
        public IReadOnlyList<PathKey> Sources { get { return sources; } }
        public PathKey Destination { get { return destination; } }
        public CollisionPolicy Policy { get { return policy; } }
        public int StepCount { get { return sources.Count; } }
    }

    public static class OpPlanBuilder
    {
        public static int KindCount { get { return Enum.GetValues(typeof(OpKind)).Length; } }
        public static int PolicyCount { get { return Enum.GetValues(typeof(CollisionPolicy)).Length; } }

        public static bool IsKindKnown(int raw)
        {
            return raw >= 0 && raw < KindCount;
        }

        public static bool IsPolicyKnown(int raw)
        {
            return raw >= 0 && raw < PolicyCount;
        }

        public static bool TryBuild(OpRequest request, out OpPlan plan)
        {
            plan = null;
            if (request == null) return false;
            if (!IsKindKnown((int)request.Kind) || !IsPolicyKnown((int)request.Policy)) return false;
            if (request.Sources == null || request.Sources.Count == 0) return false;

            MenuTarget target = request.Target;
            if (target == null || target.Directory == null || target.CapturedAt <= 0) return false;

            // 快照自洽：请求源集合必须来自同一时刻的目标快照
            if (target.Selection.Count != request.Sources.Count) return false;
            for (int i = 0; i < request.Sources.Count; i++)
            {
                if (request.Sources[i] == null) return false;
                if (!request.Sources[i].Equals(target.Selection[i])) return false;
            }

            // 需要落点的操作必须有目标；删除类不需要
            if (NeedsDestination(request.Kind) && request.Destination == null) return false;

            plan = new OpPlan(request.Kind, request.Sources,
                              request.Destination, request.Policy);
            return true;
        }

        public static bool NeedsDestination(OpKind kind)
        {
            return kind == OpKind.Copy || kind == OpKind.Move || kind == OpKind.CutPaste;
        }
    }
}
