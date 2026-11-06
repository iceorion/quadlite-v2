using System;

namespace QuadLite.Core
{
    /// <summary>
    /// 缩略图队列规则（§5.11 / §5.12 / FR-30）：可见优先、单线程 worker、
    /// 迟到结果按 generation 丢弃、ThumbJob / IconJob 状态机复用 B1 的 StateMachine。
    /// 纯判据，零 I/O；真实 worker 与回 UI 的令牌校验未接入。
    /// </summary>
    public static class ThumbQueueRules
    {
        public const int WorkerCount = 1;

        public static bool IsSingleThreaded()
        {
            return WorkerCount == 1;
        }

        public static bool UsesBackgroundWorker()
        {
            return true;
        }

        /// <summary>可见优先：可见项优先级恒高于屏外项，选中项再高一档。</summary>
        public static int PriorityOf(bool visible, bool selected)
        {
            int p = visible ? 2 : 1;
            return selected ? p + 1 : p;
        }

        public static bool VisibleFirst(bool visibleA, bool visibleB)
        {
            return PriorityOf(visibleA, false) > PriorityOf(visibleB, false);
        }

        /// <summary>可见优先，同级按入队序号，先来先做。</summary>
        public static bool ComesFirst(bool visibleA, int seqA, bool visibleB, int seqB)
        {
            int pA = PriorityOf(visibleA, false);
            int pB = PriorityOf(visibleB, false);
            if (pA != pB) return pA > pB;
            return seqA < seqB;
        }

        public static bool ShouldApplyResult(int batchGeneration, int currentGeneration, bool cancelled)
        {
            return EnumBatchRules.ShouldApplyBatch(batchGeneration, currentGeneration, cancelled);
        }

        public static bool LateResultDiscarded(int batchGeneration, int currentGeneration)
        {
            return NavRules.IsLateBatchDiscarded(batchGeneration, currentGeneration);
        }

        public static bool CanTransition(JobState from, JobState to)
        {
            return StateMachine.CanTransition(from, to);
        }

        /// <summary>Failed 进负缓存，不允许回到 Queued 无限重试。</summary>
        public static bool FailedNoRetry(JobState from, JobState to)
        {
            return from == JobState.Failed && !CanTransition(from, to);
        }

        public static bool UiThreadOnlyRenders()
        {
            return true;
        }

        public static bool AllowsLongComCallOnUiThread()
        {
            return false;
        }
    }
}
