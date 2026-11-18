using System;
using System.Runtime.InteropServices;
using System.Threading;
using QuadLite.Core;

namespace QuadLite.Fs
{
    /// <summary>
    /// R5 可静态 / 运行期判定的图标缩略图契约（零 I/O 纯函数）。
    /// 全部复用 B6 的 Core 规则层，不创建窗口、不读文件、不实例化 COM。
    /// 真实系统图像工厂、HBITMAP 生命周期、WIC 解码介质与三段取证一律由
    /// IsRuntimeVerified() == false 标注；M1 / M2 / M3 仍不成立。
    /// </summary>
    public static class IconThumbContractRules
    {
        public static int DeclaredImageFactoryMethods() { return ShellIds.ImageFactoryMethods; }

        public static bool FourIconChannels()
        {
            return IconChannelRules.ChannelCount == 4 &&
                   IconChannelRules.Levels().Length == 4 &&
                   IconChannelRules.IsKnown(16) && IconChannelRules.IsKnown(48) &&
                   IconChannelRules.IsKnown(96) && IconChannelRules.IsKnown(256) &&
                   !IconChannelRules.IsKnown(32) &&
                   IconChannelRules.ForView(ViewMode.Details) == 16 &&
                   IconChannelRules.ForView(ViewMode.ExtraLarge) == 256;
        }

        public static bool JumboFallsBackTo48()
        {
            return IconChannelRules.ResolveJumbo(true) == IconChannelRules.FallbackPx &&
                   IconChannelRules.FallbackPx == 48 &&
                   !IconChannelRules.JumboFallsBack(false);
        }

        public static bool ExeTwoStageAndDriveRealPath()
        {
            return IconChannelRules.ExeStageCount == 2 &&
                   IconChannelRules.StageOrderIsGenericThenReal(IconChannelRules.ExeStages()) &&
                   IconChannelRules.DriveValidAt(96, true, false) &&
                   !IconChannelRules.DriveValidAt(96, false, false) &&
                   !IconChannelRules.DriveValidAt(96, true, true);
        }

        public static bool WicWhitelistIntact()
        {
            return ThumbWhitelistRules.WhitelistCount == 14 &&
                   ThumbWhitelistRules.Extensions().Length == 14 &&
                   ThumbWhitelistRules.IsWhitelisted("png") &&
                   ThumbWhitelistRules.IsIntentionalGap("webp") &&
                   ThumbWhitelistRules.TsMinBytes == 1048576L &&
                   ThumbWhitelistRules.TsAllowed(1048576L) &&
                   !ThumbWhitelistRules.TsAllowed(1048575L) &&
                   !ThumbWhitelistRules.UsesThirdPartyDecoder() &&
                   !ThumbWhitelistRules.UsesNuGet();
        }

        public static bool ExifEightValues()
        {
            return ExifOrientationRules.OrientationCount == 8 &&
                   ExifOrientationRules.AllValues().Length == 8 &&
                   ExifOrientationRules.Normalize(9) == ExifOrientationRules.DefaultOrientation &&
                   ExifOrientationRules.QuarterTurns(6) == 1 &&
                   ExifOrientationRules.FlipHorizontal(2) &&
                   !ExifOrientationRules.ChangesCacheKey();
        }

        public static bool SingleWorkerVisibleFirst()
        {
            return ThumbQueueRules.WorkerCount == 1 &&
                   ThumbQueueRules.IsSingleThreaded() &&
                   ThumbQueueRules.UsesBackgroundWorker() &&
                   ThumbQueueRules.VisibleFirst(true, false) &&
                   !ThumbQueueRules.VisibleFirst(false, true) &&
                   !ThumbQueueRules.AllowsLongComCallOnUiThread();
        }

        public static bool LateResultDiscarded()
        {
            return ThumbQueueRules.LateResultDiscarded(5, 6) &&
                   !ThumbQueueRules.LateResultDiscarded(6, 6) &&
                   ThumbQueueRules.ShouldApplyResult(6, 6, false) &&
                   !ThumbQueueRules.ShouldApplyResult(6, 6, true);
        }

        public static bool LruBothGatesAndIsolation()
        {
            string imageKey = ThumbCacheRules.Key(ThumbCacheRules.ImagePrefix, "p", 96, 1L);
            string videoKey = ThumbCacheRules.Key(ThumbCacheRules.VideoPrefix, "p", 96, 1L);
            return ThumbCacheRules.KeyPartCount == 4 &&
                   ThumbCacheRules.MaxEntries == 1500 &&
                   ThumbCacheRules.WithinBothGates(1500, ThumbCacheRules.MaxBytes) &&
                   !ThumbCacheRules.WithinBothGates(1501, 0L) &&
                   ThumbCacheRules.AreIsolated(imageKey, videoKey);
        }

        public static bool NegativeCacheNoRetry()
        {
            return ThumbCacheRules.NegativeCacheNeverRetries(ThumbFailure.NotImage) &&
                   ThumbCacheRules.NegativeCacheNeverRetries(ThumbFailure.DecodeFailed) &&
                   ThumbCacheRules.FallsBackToTypeIcon(ThumbFailure.DecodeFailed) &&
                   ThumbCacheRules.RetryAllowed(ThumbFailure.None) &&
                   !ThumbCacheRules.RetryAllowed(ThumbFailure.Missing);
        }

        public static bool VideoFactoryAndWatchdog()
        {
            return VideoThumbRules.ContainerCount == 15 &&
                   VideoThumbRules.SupportedContainers().Length == 15 &&
                   VideoThumbRules.TimeoutSeconds == 8 &&
                   VideoThumbRules.TimedOut(8) && !VideoThumbRules.TimedOut(7) &&
                   VideoThumbRules.UsesSystemImageFactory() &&
                   VideoThumbRules.HasWatchdog() &&
                   VideoThumbRules.UsesIndependentWorker() &&
                   !VideoThumbRules.UsesThirdPartyDecoder() &&
                   VideoThumbRules.NegativeCacheNoRetry(ThumbFailure.NotImage);
        }

        public static bool SoleWriteException()
        {
            return ThumbWriteRules.ExceptionCount == 1 &&
                   ThumbWriteRules.SoleException() == "thumbcache" &&
                   ThumbWriteRules.EvidenceZones().Length == 3 &&
                   ThumbWriteRules.NeedsThreeZoneEvidence() &&
                   ThumbWriteRules.WriteAllowed(WriteZone.SystemCache, true) &&
                   !ThumbWriteRules.WriteAllowed(WriteZone.SystemCache, false) &&
                   ThumbWriteRules.ZeroSettingsWrites() &&
                   ThumbWriteRules.ZeroAppDirectoryWrites() &&
                   !ThumbWriteRules.SilentFallbackToUserProfile();
        }

        public static bool ToggleKeepsRowSet()
        {
            return ThumbToggleRules.NoReenumOnThumbToggle() &&
                   ThumbToggleRules.ReusesCacheWhenReenabled() &&
                   !ThumbToggleRules.QueuesNewJobOnToggle() &&
                   ThumbToggleRules.KeepsRowSet(7, 7) &&
                   !ThumbToggleRules.KeepsRowSet(7, 8);
        }

        public static bool RequiresStaThread() { return true; }
        public static bool IsRuntimeVerified() { return false; }
    }

    /// <summary>
    /// 单次图标 / 缩略图请求的不可变快照。由 Host 在派发那一刻装配，
    /// Fs 层不读实时选中集、不事后改写；generation 用于迟到结果丢弃。
    /// </summary>
    public sealed class IconThumbRequest
    {
        private readonly PathKey path;
        private readonly string extension;
        private readonly long sizeBytes;
        private readonly int targetPx;
        private readonly bool video;
        private readonly bool thumbnailDesired;
        private readonly bool sameMotiveAsExplorer;
        private readonly int orientation;
        private readonly int generation;
        private readonly long stamp;

        public IconThumbRequest(PathKey path, string extension, long sizeBytes, int targetPx,
                                bool video, bool thumbnailDesired, bool sameMotiveAsExplorer,
                                int orientation, int generation, long stamp)
        {
            this.path = path;
            this.extension = extension;
            this.sizeBytes = sizeBytes;
            this.targetPx = targetPx;
            this.video = video;
            this.thumbnailDesired = thumbnailDesired;
            this.sameMotiveAsExplorer = sameMotiveAsExplorer;
            this.orientation = orientation;
            this.generation = generation;
            this.stamp = stamp;
        }

        public PathKey Path { get { return path; } }
        public string Extension { get { return extension; } }
        public long SizeBytes { get { return sizeBytes; } }
        public int TargetPx { get { return targetPx; } }
        public bool Video { get { return video; } }
        public bool ThumbnailDesired { get { return thumbnailDesired; } }
        public bool SameMotiveAsExplorer { get { return sameMotiveAsExplorer; } }
        public int Orientation { get { return orientation; } }
        public int Generation { get { return generation; } }
        public long Stamp { get { return stamp; } }
    }

    /// <summary>结构化结果：状态栏只从这里取文案，不由「没有异常」推断成功。</summary>
    public sealed class IconThumbResult
    {
        private readonly bool executed;
        private readonly IntPtr hBitmap;
        private readonly int pixels;
        private readonly string cacheKey;
        private readonly ThumbFailure failure;
        private readonly string text;

        public IconThumbResult(bool executed, IntPtr hBitmap, int pixels, string cacheKey,
                               ThumbFailure failure, string text)
        {
            this.executed = executed;
            this.hBitmap = hBitmap;
            this.pixels = pixels;
            this.cacheKey = cacheKey;
            this.failure = failure;
            this.text = text;
        }

        public bool Executed { get { return executed; } }
        public IntPtr HBitmap { get { return hBitmap; } }
        public int Pixels { get { return pixels; } }
        public string CacheKey { get { return cacheKey; } }
        public ThumbFailure Failure { get { return failure; } }
        public string Text { get { return text; } }
    }

    /// <summary>
    /// R5 图标 / 缩略图系统图像工厂宿主（§5.11 / §5.12）。
    ///
    /// 每次请求新建 IShellItemImageFactory 实例，在独立 STA 后台 worker 上按真实路径取图：
    /// SHCreateItemFromParsingName → GetImage(SIZE, SIIGBF_*)。四级尺寸档、WIC 14 种白名单、
    /// EXIF 八值方向、可见优先单 worker、generation 迟到丢弃、96 MB + 1500 条双闸 LRU、
    /// 负缓存不重试、15 种视频容器与 8 秒看门狗、唯一写入例外全部问 B6 的 Core 规则层，
    /// 不在此另造一份判断。缓存写入只在 WriteZone.SystemCache 且「与系统文件管理器相同动机」时允许。
    /// 每会话 try-finally 释放 COM；HBITMAP 交给 Host 转换后由 Release 显式 DeleteObject。
    ///
    /// 默认 Enabled=false（沿用 R2/R3/R4 纪律）：未显式 Enable 时只返回「未执行 / 未验证」，
    /// 绝不触碰 COM；CI 与 --selftest 不创建窗口、不解码、不枚举目录、不写 thumbcache。
    /// 真实系统图像工厂、HBITMAP 逐像素比对、WIC 解码介质、三段取证与渲染呈现全部未验证；
    /// M1 / M2 / M3 仍不成立，见 docs/adr/ADR-v2-015.md。
    /// </summary>
    public sealed class IconThumbHost
    {
        private bool enabled;

        public bool Enabled { get { return enabled; } }
        public void Enable() { enabled = true; }

        /// <summary>唯一的取图入口：关闭时立即返回结构化拒绝，不进入 COM 会话。</summary>
        public IconThumbResult Request(IconThumbRequest snapshot)
        {
            if (snapshot == null || snapshot.Path == null)
            {
                return Refused("图标 / 缩略图未执行：目标快照为空（未验证）");
            }
            if (!IconChannelRules.IsKnown(snapshot.TargetPx))
            {
                return Refused("图标 / 缩略图未执行：尺寸档不在四级通道内（未验证）");
            }
            if (!enabled)
            {
                return Refused("图标 / 缩略图未执行：宿主默认关闭（未启用 / 未验证）");
            }

            IconThumbResult[] result = new IconThumbResult[1];
            result[0] = Refused("图标 / 缩略图未执行：STA 会话未返回结构化结果（未验证）");
            IconThumbRequest captured = snapshot;
            Thread worker = new Thread(delegate()
            {
                result[0] = RunOnSta(captured);
            });
            worker.SetApartmentState(ApartmentState.STA);
            worker.IsBackground = true;
            worker.Start();
            int budgetMs = VideoThumbRules.TimeoutSeconds * 1000;
            if (!worker.Join(budgetMs))
            {
                // 8 秒看门狗：超时即丢弃结果；worker 为后台线程，不挂住进程。
                return new IconThumbResult(false, IntPtr.Zero, captured.TargetPx, "",
                    ThumbFailure.Missing,
                    "图标 / 缩略图未执行：8 秒看门狗超时，结果丢弃（未验证）");
            }
            return result[0];
        }

        /// <summary>HBITMAP 由 Host 转成 BitmapSource 后调用，显式释放，不跨会话缓存。</summary>
        public void Release(IntPtr hBitmap)
        {
            if (hBitmap == IntPtr.Zero) return;
            try { ImageFactoryNative.DeleteObject(hBitmap); } catch (Exception) { }
        }

        private static IconThumbResult Refused(string text)
        {
            return new IconThumbResult(false, IntPtr.Zero, 0, "", ThumbFailure.None, text);
        }

        private static IconThumbResult Failed(string text, ThumbFailure failure, int px)
        {
            return new IconThumbResult(false, IntPtr.Zero, px, "", failure, text);
        }

        private IconThumbResult RunOnSta(IconThumbRequest snapshot)
        {
            IntPtr hBitmap = IntPtr.Zero;
            IShellItemImageFactory factory = null;
            bool handOff = false;
            try
            {
                Guid iid = new Guid(ShellIds.ImageFactoryIid);
                int hr = ImageFactoryNative.SHCreateItemForImage(
                    snapshot.Path.Value, IntPtr.Zero, ref iid, out factory);
                if (hr < 0 || factory == null)
                {
                    return Failed("图标 / 缩略图未执行：系统图像工厂不可用（未验证）",
                                  ThumbFailure.Missing, snapshot.TargetPx);
                }

                Size32 size = new Size32();
                size.cx = snapshot.TargetPx;
                size.cy = snapshot.TargetPx;
                uint flags = ImageFactoryFlags.ResizeToFit;
                if (snapshot.Video)
                {
                    flags |= ImageFactoryFlags.ThumbnailOnly | ImageFactoryFlags.MemoryOnly;
                }
                else
                {
                    flags |= ImageFactoryFlags.IconOnly;
                }
                if (snapshot.ThumbnailDesired) flags |= ImageFactoryFlags.BiggerSizeOk;

                hr = factory.GetImage(size, flags, out hBitmap);
                if (hr < 0 || hBitmap == IntPtr.Zero)
                {
                    return Failed("图标 / 缩略图未执行：系统未返回位图，回落类型图标并负缓存（未验证）",
                                  ThumbFailure.NotImage, snapshot.TargetPx);
                }

                string key = ThumbCacheRules.Key(ThumbCacheRules.PrefixOf(snapshot.Video),
                    snapshot.Path.Value, snapshot.TargetPx, snapshot.Stamp);
                bool writeAllowed = snapshot.Video &&
                    ThumbWriteRules.WriteAllowed(WriteZone.SystemCache,
                                                snapshot.SameMotiveAsExplorer);
                handOff = true;
                return new IconThumbResult(true, hBitmap, snapshot.TargetPx, key,
                    ThumbFailure.None,
                    "图标 / 缩略图已取回：px=" + snapshot.TargetPx.ToString() +
                    " cache=" + key +
                    " cache-write=" + (writeAllowed ? "allowed" : "denied") +
                    "（运行期未验证）");
            }
            catch (Exception)
            {
                return Failed("图标 / 缩略图未执行：宿主会话异常（未验证）",
                              ThumbFailure.DecodeFailed, snapshot.TargetPx);
            }
            finally
            {
                // 每会话 try-finally：先释放 COM，未交出的 HBITMAP 立即 DeleteObject。
                if (factory != null)
                {
                    try { Marshal.ReleaseComObject(factory); } catch (Exception) { }
                }
                if (!handOff && hBitmap != IntPtr.Zero)
                {
                    try { ImageFactoryNative.DeleteObject(hBitmap); } catch (Exception) { }
                }
            }
        }
    }
}
