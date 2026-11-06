using System;
using QuadLite.Core;

namespace QuadLite.App
{
    /// <summary>
    /// B6 图标 / 缩略图状态内核（L4，零 I/O）：只读 Core 规则结果装配状态。
    /// 不含窗口、XAML、列表控件、WIC / Shell 绑定与任何事件绑定；
    /// 真实图标与缩略图渲染由后续批次在注入端口后接入，见 docs/adr/ADR-v2-008.md。
    /// </summary>
    public sealed class ThumbState
    {
        private readonly IconSize iconSize;
        private readonly JobState job;
        private readonly ThumbFailure failure;
        private readonly bool showThumbs;
        private readonly int cacheEntries;
        private readonly long cacheBytes;
        private readonly int visibleQueued;
        private readonly int generation;
        private readonly bool cacheWriteAllowed;

        private ThumbState(IconSize iconSize, JobState job, ThumbFailure failure, bool showThumbs,
                           int cacheEntries, long cacheBytes, int visibleQueued,
                           int generation, bool cacheWriteAllowed)
        {
            this.iconSize = iconSize;
            this.job = job;
            this.failure = failure;
            this.showThumbs = showThumbs;
            this.cacheEntries = cacheEntries;
            this.cacheBytes = cacheBytes;
            this.visibleQueued = visibleQueued;
            this.generation = generation;
            this.cacheWriteAllowed = cacheWriteAllowed;
        }

        public static ThumbState Default()
        {
            return new ThumbState(IconSize.Small16, JobState.Queued, ThumbFailure.None,
                                  false, 0, 0L, 0, 1, false);
        }

        public IconSize IconSize { get { return iconSize; } }
        public JobState Job { get { return job; } }
        public ThumbFailure Failure { get { return failure; } }
        public bool ShowThumbs { get { return showThumbs; } }
        public int CacheEntries { get { return cacheEntries; } }
        public long CacheBytes { get { return cacheBytes; } }
        public int VisibleQueued { get { return visibleQueued; } }
        public int Generation { get { return generation; } }
        public bool CacheWriteAllowed { get { return cacheWriteAllowed; } }

        public ThumbState WithView(ViewMode mode)
        {
            int px = IconChannelRules.ForView(mode);
            IconSize size = IconChannelRules.IsKnown(px)
                ? IconChannelRules.LevelOf(px) : IconSize.Small16;
            return new ThumbState(size, job, failure, showThumbs, cacheEntries, cacheBytes,
                                  visibleQueued, generation, cacheWriteAllowed);
        }

        public ThumbState WithJob(JobState next)
        {
            return new ThumbState(iconSize, next, failure, showThumbs, cacheEntries, cacheBytes,
                                  visibleQueued, generation, cacheWriteAllowed);
        }

        public ThumbState WithFailure(ThumbFailure next)
        {
            return new ThumbState(iconSize, next == ThumbFailure.None ? job : JobState.Failed,
                                  next, showThumbs, cacheEntries, cacheBytes,
                                  visibleQueued, generation, cacheWriteAllowed);
        }

        public ThumbState WithToggle(ToggleState toggles)
        {
            return new ThumbState(iconSize, job, failure, toggles.ShowThumbnail,
                                  cacheEntries, cacheBytes, visibleQueued, generation,
                                  cacheWriteAllowed);
        }

        public ThumbState WithCache(int entries, long bytes)
        {
            return new ThumbState(iconSize, job, failure, showThumbs, entries, bytes,
                                  visibleQueued, generation, cacheWriteAllowed);
        }

        /// <summary>切目录 / 换视图前进 generation；迟到结果由 Core 判据丢弃。</summary>
        public ThumbState WithNextGeneration()
        {
            return new ThumbState(iconSize, JobState.Queued, ThumbFailure.None, showThumbs,
                                  cacheEntries, cacheBytes, 0,
                                  NavRules.NextGeneration(generation), cacheWriteAllowed);
        }

        public ThumbState WithVisibleQueued(int count)
        {
            int safe = count < 0 ? 0 : count;
            return new ThumbState(iconSize, job, failure, showThumbs, cacheEntries, cacheBytes,
                                  safe, generation, cacheWriteAllowed);
        }

        public ThumbState WithCacheWrite(bool sameMotiveAsExplorer)
        {
            bool allowed = ThumbWriteRules.WriteAllowed(WriteZone.SystemCache, sameMotiveAsExplorer);
            return new ThumbState(iconSize, job, failure, showThumbs, cacheEntries, cacheBytes,
                                  visibleQueued, generation, allowed);
        }

        public bool NeedsFallbackIcon()
        {
            return ThumbCacheRules.FallsBackToTypeIcon(failure);
        }

        public bool WithinCacheGates()
        {
            return ThumbCacheRules.WithinBothGates(cacheEntries, cacheBytes);
        }

        public bool NeedsEviction()
        {
            return ThumbCacheRules.NeedsEviction(cacheEntries, cacheBytes);
        }

        public bool NeedsThumbnailFor(ViewMode mode)
        {
            ToggleState toggles = new ToggleState(false, false, true, showThumbs);
            return ThumbToggleRules.NeedsThumbForView(toggles, mode);
        }

        /// <summary>结构化状态栏文案：失败必须带原因码，不由「没有异常」推断成功。</summary>
        public string HintText()
        {
            if (failure == ThumbFailure.None) return "";
            return "thumb-fallback;reason=" + failure.ToString().ToLowerInvariant() +
                   ";action=type-icon";
        }

        public bool HintMentionsReasonAndFallback()
        {
            string h = HintText();
            return h.IndexOf("reason=", StringComparison.Ordinal) >= 0 &&
                   h.IndexOf("action=type-icon", StringComparison.Ordinal) >= 0;
        }
    }
}
