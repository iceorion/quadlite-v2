using System;

namespace QuadLite.Core
{
    /// <summary>每条失败必须有分类、用户可见文案、可重试性、是否入撤销栈。</summary>
    public enum ErrorClass
    {
        None, Cancelled, PartialSuccess, AccessDenied, SharingViolation, NameConflict,
        PathTooLong, PlaceholderHydrationFailed, ShellUnavailable, ClipboardUnavailable,
        SessionCorrupt, EnumFailed, DecodeFailed
    }

    public sealed class ErrorSpec
    {
        private readonly bool? retryable;
        private readonly bool? entersUndoStack;
        private readonly string userText;

        public ErrorSpec(bool? retryable, bool? entersUndoStack, string userText)
        {
            this.retryable = retryable;
            this.entersUndoStack = entersUndoStack;
            this.userText = userText;
        }

        /// <summary>null = 不适用 / 按逐项结果决定。</summary>
        public bool? Retryable { get { return retryable; } }
        public bool? EntersUndoStack { get { return entersUndoStack; } }
        public string UserText { get { return userText; } }
    }

    public static class ErrorModel
    {
        private static readonly ErrorSpec[] Specs = new ErrorSpec[]
        {
            new ErrorSpec(null, null, ""),
            new ErrorSpec(true, false, "已取消"),
            new ErrorSpec(true, null, "逐项列出源→目标与失败原因"),
            new ErrorSpec(true, false, "原因 + 建议动作"),
            new ErrorSpec(true, false, "原因 + 建议动作"),
            new ErrorSpec(true, null, "应用内非模态面板"),
            new ErrorSpec(false, false, "如实失败，不静默截断"),
            new ErrorSpec(true, false, "说明「文件未在本地」"),
            new ErrorSpec(false, false, "进入只读模式"),
            new ErrorSpec(true, false, "说明不影响内部拖放"),
            new ErrorSpec(null, null, "状态栏说明「已回落默认」"),
            new ErrorSpec(true, false, "保留旧行集 + 明确错误，不显示空目录"),
            new ErrorSpec(false, false, "回落类型图标 + 负缓存")
        };

        public static int Count { get { return Specs.Length; } }

        public static ErrorSpec For(ErrorClass error)
        {
            int i = (int)error;
            if (i < 0 || i >= Specs.Length) return null;
            return Specs[i];
        }

        /// <summary>枚举完备性：新增分类而不同步规格表即判红。</summary>
        public static bool IsComplete()
        {
            if (Enum.GetValues(typeof(ErrorClass)).Length != Specs.Length) return false;
            for (int i = 0; i < Specs.Length; i++)
            {
                if (Specs[i] == null || Specs[i].UserText == null) return false;
            }
            return true;
        }

        public static bool IsKnown(int raw)
        {
            return raw >= 0 && raw < Specs.Length;
        }
    }
}
