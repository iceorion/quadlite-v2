using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace QuadLite.Core
{
    /// <summary>逐项结果：ActualTarget 为空表示拿不到操作后真实位置，该步不入撤销栈。</summary>
    public sealed class OpItem
    {
        private readonly PathKey source;
        private readonly PathKey actualTarget;
        private readonly int hr;
        private readonly string reason;

        public OpItem(PathKey source, PathKey actualTarget, int hr, string reason)
        {
            this.source = source;
            this.actualTarget = actualTarget;
            this.hr = hr;
            this.reason = reason;
        }

        public PathKey Source { get { return source; } }
        public PathKey ActualTarget { get { return actualTarget; } }
        public int Hr { get { return hr; } }
        public string Reason { get { return reason; } }
    }

    /// <summary>CallbackCount 是失败汇总的唯一分母；逐项结果不吞错误。</summary>
    public sealed class OpRecord
    {
        private readonly IReadOnlyList<OpItem> items;
        private readonly int callbackCount;
        private readonly bool cancelled;
        private readonly ErrorClass error;

        public OpRecord(IList<OpItem> items, int callbackCount, bool cancelled, ErrorClass error)
        {
            List<OpItem> copy = new List<OpItem>();
            if (items != null)
            {
                for (int i = 0; i < items.Count; i++) copy.Add(items[i]);
            }
            this.items = new ReadOnlyCollection<OpItem>(copy);
            this.callbackCount = callbackCount;
            this.cancelled = cancelled;
            this.error = error;
        }

        public IReadOnlyList<OpItem> Items { get { return items; } }
        public int CallbackCount { get { return callbackCount; } }
        public bool Cancelled { get { return cancelled; } }
        public ErrorClass Error { get { return error; } }
    }

    public static class OpRecordRules
    {
        public static bool IsSuccess(OpItem item)
        {
            return item != null && item.Hr >= 0;  // 成败只看 hr >= 0
        }

        public static int SuccessCount(OpRecord record)
        {
            if (record == null) return 0;
            int n = 0;
            for (int i = 0; i < record.Items.Count; i++)
            {
                if (IsSuccess(record.Items[i])) n++;
            }
            return n;
        }

        public static int Denominator(OpRecord record)
        {
            return record == null ? 0 : record.CallbackCount;
        }

        public static bool CanUndo(OpItem item)
        {
            return IsSuccess(item) && item.ActualTarget != null;
        }

        public static bool IsConsistent(OpRecord record)
        {
            if (record == null || record.Items == null) return false;
            if (record.CallbackCount < 0) return false;
            if (record.Cancelled && record.Error != ErrorClass.Cancelled) return false;
            for (int i = 0; i < record.Items.Count; i++)
            {
                if (record.Items[i] == null || record.Items[i].Source == null) return false;
            }
            return true;
        }

        /// <summary>逐项列出源→真实目标与失败原因，不把部分成功说成整体成功。</summary>
        public static string Summarize(OpRecord record)
        {
            if (record == null) return "";
            List<string> lines = new List<string>();
            for (int i = 0; i < record.Items.Count; i++)
            {
                OpItem it = record.Items[i];
                string target = it.ActualTarget == null ? "(unknown)" : it.ActualTarget.Value;
                string verdict = IsSuccess(it) ? "ok" : "failed";
                string reason = string.IsNullOrEmpty(it.Reason) ? "" : (" reason=" + it.Reason);
                lines.Add(it.Source.Value + " -> " + target + " " + verdict + " hr=" +
                          it.Hr.ToString() + reason);
            }
            lines.Add("success=" + SuccessCount(record).ToString() + "/" +
                      Denominator(record).ToString() + " cancelled=" +
                      (record.Cancelled ? "True" : "False"));
            return string.Join("; ", lines.ToArray());
        }
    }
}
