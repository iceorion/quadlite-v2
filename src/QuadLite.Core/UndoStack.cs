using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace QuadLite.Core
{
    /// <summary>一批 N 个源项算一步；跨窗格共享，仅本次会话，不落盘。</summary>
    public sealed class UndoStep
    {
        private readonly IReadOnlyList<OpItem> items;
        private readonly OpKind kind;
        private readonly long capturedAt;

        public UndoStep(IList<OpItem> items, OpKind kind, long capturedAt)
        {
            List<OpItem> copy = new List<OpItem>();
            if (items != null)
            {
                for (int i = 0; i < items.Count; i++) copy.Add(items[i]);
            }
            this.items = new ReadOnlyCollection<OpItem>(copy);
            this.kind = kind;
            this.capturedAt = capturedAt;
        }

        public IReadOnlyList<OpItem> Items { get { return items; } }
        public OpKind Kind { get { return kind; } }
        public long CapturedAt { get { return capturedAt; } }
        public int ItemCount { get { return items.Count; } }
    }

    public static class UndoRules
    {
        public const int Capacity = 20;

        /// <summary>拿不到操作后真实位置、以及永久删除，都不入栈。</summary>
        public static bool CanPush(OpItem item, OpKind kind)
        {
            if (!OpRecordRules.CanUndo(item)) return false;
            if (kind == OpKind.PermanentDelete) return false;
            return true;
        }

        public static bool CanPushRecord(OpRecord record, OpKind kind)
        {
            if (record == null || record.Cancelled) return false;
            if (!OpRecordRules.IsConsistent(record)) return false;
            int n = 0;
            for (int i = 0; i < record.Items.Count; i++)
            {
                if (CanPush(record.Items[i], kind)) n++;
            }
            return n > 0;
        }

        /// <summary>覆盖式复制的撤销只删副本，界面须如实说明。</summary>
        public static bool RestoresOverwrittenContent(OpKind kind, CollisionPolicy policy)
        {
            return false;
        }
    }

    public sealed class UndoStack
    {
        private readonly List<UndoStep> steps = new List<UndoStep>();

        public int Count { get { return steps.Count; } }
        public int Capacity { get { return UndoRules.Capacity; } }

        public bool Push(OpRecord record, OpKind kind, long capturedAt)
        {
            if (!UndoRules.CanPushRecord(record, kind)) return false;
            List<OpItem> kept = new List<OpItem>();
            for (int i = 0; i < record.Items.Count; i++)
            {
                if (UndoRules.CanPush(record.Items[i], kind)) kept.Add(record.Items[i]);
            }
            steps.Add(new UndoStep(kept, kind, capturedAt));
            while (steps.Count > UndoRules.Capacity) steps.RemoveAt(0);
            return true;
        }

        public UndoStep Pop()
        {
            if (steps.Count == 0) return null;
            UndoStep last = steps[steps.Count - 1];
            steps.RemoveAt(steps.Count - 1);
            return last;
        }

        public UndoStep Peek()
        {
            return steps.Count == 0 ? null : steps[steps.Count - 1];
        }

        public void Clear()
        {
            steps.Clear();
        }
    }
}
