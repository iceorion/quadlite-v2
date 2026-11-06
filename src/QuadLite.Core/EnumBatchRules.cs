using System;

namespace QuadLite.Core
{
    /// <summary>
    /// 目录枚举分批（§5.9 / FR-09）：默认 4096 条 / 批、可取消、generation 丢弃迟到批次、
    /// 虚拟滚动只物化可见行。本类型是纯函数判据层，不做任何真实目录读取。
    /// </summary>
    public static class EnumBatchRules
    {
        public const int DefaultBatchSize = 4096;

        public static bool IsValidBatchSize(int size)
        {
            return size > 0;
        }

        public static int BatchCount(int total, int batchSize)
        {
            if (total <= 0 || !IsValidBatchSize(batchSize)) return 0;
            return (total + batchSize - 1) / batchSize;
        }

        public static int BatchStart(int index, int batchSize)
        {
            if (index < 0 || !IsValidBatchSize(batchSize)) return -1;
            return index * batchSize;
        }

        public static int BatchLength(int index, int total, int batchSize)
        {
            if (index < 0 || total <= 0 || !IsValidBatchSize(batchSize)) return 0;
            int start = BatchStart(index, batchSize);
            if (start < 0 || start >= total) return 0;
            int remain = total - start;
            return remain < batchSize ? remain : batchSize;
        }

        /// <summary>切目录后 generation 前进，迟到批次一律丢弃；复用 NavRules 同一判据。</summary>
        public static bool IsLateBatch(int batchGeneration, int currentGeneration)
        {
            return NavRules.IsLateBatchDiscarded(batchGeneration, currentGeneration);
        }

        public static bool ShouldApplyBatch(int batchGeneration, int currentGeneration, bool cancelled)
        {
            return !cancelled && NavRules.ShouldApplyBatch(batchGeneration, currentGeneration);
        }

        /// <summary>取消后不再发后续批次。</summary>
        public static bool StopsAfterCancel(bool cancelled)
        {
            return cancelled;
        }

        /// <summary>虚拟滚动：物化行数 = min(视口行数, 总行数)，不为全部行建立可视对象。</summary>
        public static int RealizedRows(int total, int viewportRows)
        {
            if (total <= 0 || viewportRows <= 0) return 0;
            return total < viewportRows ? total : viewportRows;
        }

        public static bool IsVirtualized(int total, int viewportRows)
        {
            return total > 0 && viewportRows > 0 && RealizedRows(total, viewportRows) < total;
        }
    }
}
