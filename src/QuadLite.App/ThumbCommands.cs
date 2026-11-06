using System;
using QuadLite.Core;

namespace QuadLite.App
{
    /// <summary>缩略图请求的结构化结果（§5.11）：只描述「做什么」，不执行任何 I/O。</summary>
    public sealed class ThumbRequestResult
    {
        private readonly bool accepted;
        private readonly string reason;
        private readonly string cachePrefix;
        private readonly bool needsReenumeration;
        private readonly bool needsSystemCacheWrite;

        public ThumbRequestResult(bool accepted, string reason, string cachePrefix,
                                  bool needsReenumeration, bool needsSystemCacheWrite)
        {
            this.accepted = accepted;
            this.reason = reason;
            this.cachePrefix = cachePrefix;
            this.needsReenumeration = needsReenumeration;
            this.needsSystemCacheWrite = needsSystemCacheWrite;
        }

        public bool Accepted { get { return accepted; } }
        public string Reason { get { return reason; } }
        public string CachePrefix { get { return cachePrefix; } }
        public bool NeedsReenumeration { get { return needsReenumeration; } }
        public bool NeedsSystemCacheWrite { get { return needsSystemCacheWrite; } }
    }

    /// <summary>
    /// 缩略图命令派发（B6）：只读 Core 的白名单 / 视频容器 / 缓存 / 写入例外判据，
    /// 装配结构化结果；不碰 WIC、不碰 Shell、不碰磁盘。真实调用点未接入，
    /// 见 docs/adr/ADR-v2-008.md。
    /// </summary>
    public static class ThumbCommands
    {
        public static ThumbRequestResult Request(string extension, long sizeBytes,
                                                ToggleState toggles, ViewMode mode,
                                                bool sameMotiveAsExplorer)
        {
            if (!ThumbToggleRules.NeedsThumbForView(toggles, mode))
                return new ThumbRequestResult(false, "ThumbnailsOffOrViewWithoutThumbs",
                                              "", false, false);

            bool image = ThumbWhitelistRules.IsWhitelisted(extension);
            bool video = VideoThumbRules.IsSupported(extension);
            if (image)
            {
                if (!ThumbWhitelistRules.Allows(extension, sizeBytes))
                    return new ThumbRequestResult(false, "SizeGateRejected", "", false, false);
                return new ThumbRequestResult(true, "Ok",
                                              ThumbCacheRules.PrefixOf(false), false, false);
            }
            if (video)
            {
                if (!VideoThumbRules.Allows(extension, sizeBytes))
                    return new ThumbRequestResult(false, "SizeGateRejected", "", false, false);
                bool write = ThumbWriteRules.WriteAllowed(WriteZone.SystemCache,
                                                          sameMotiveAsExplorer);
                return new ThumbRequestResult(true, "Ok",
                                              ThumbCacheRules.PrefixOf(true), false, write);
            }
            return new ThumbRequestResult(false, "IntentionalGapOrUnsupported", "", false, false);
        }

        /// <summary>切换缩略图开关：不重枚举、不新建 job、generation 保持。</summary>
        public static ThumbRequestResult OnToggle(ToggleState before, ViewMode mode,
                                                  int generationBefore, int generationAfter)
        {
            ToggleState after = ThumbToggleRules.Flip(before);
            bool noReenum = ThumbToggleRules.NoReenumOnThumbToggle();
            bool generationKept = generationBefore == generationAfter;
            if (!noReenum || !generationKept)
                return new ThumbRequestResult(false, "ReenumerationDetected", "", true, false);
            return new ThumbRequestResult(ThumbToggleRules.NeedsThumbForView(after, mode),
                                          "Ok", "", false, false);
        }

        /// <summary>迟到结果：只接受同代回调，由 Core 判据判定，不在此另造规则。</summary>
        public static bool ShouldApplyResult(int batchGeneration, int currentGeneration,
                                             bool cancelled)
        {
            return ThumbQueueRules.ShouldApplyResult(batchGeneration, currentGeneration, cancelled);
        }
    }
}
