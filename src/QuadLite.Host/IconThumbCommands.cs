using System;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using QuadLite.App;
using QuadLite.Core;
using QuadLite.Fs;

namespace QuadLite.Host
{
    /// <summary>
    /// R5：把图标 / 缩略图取图链路接到列表项呈现上。Host 层只做四件事：
    /// 用 App 的 ThumbCommands 做零 I/O 派发、装配不可变 IconThumbRequest 快照、
    /// 把 Fs 返回的 HBITMAP 转成 BitmapSource、按 EXIF 方向用系统 WIC 包装做两档自解码。
    ///
    /// 本层不写 System.IO、不写 COM 声明、不手写 vtable、不自行启用宿主。
    /// 图片解码走 WPF 内置的 WIC 托管包装（零 NuGet、零第三方解码器），不新增 COM 声明；
    /// 这是相对 ADR-013 D1「第三套 WIC COM」预期的收敛，登记在 ADR-015 D2。
    /// IconThumbHost 默认关闭，未显式 Enable 时 Provide 只返回 null（图片自解码分支也一并跳过，
    /// 列表项显示占位）；
    /// 真实系统图像工厂、HBITMAP 呈现与 thumbcache 写入运行期完全未验证。
    /// M1 / M2 / M3 均不成立，见 docs/adr/ADR-v2-015.md。
    /// </summary>
    public sealed class IconThumbCommands
    {
        private readonly IconThumbHost host = new IconThumbHost();

        public bool Enabled { get { return host.Enabled; } }

        /// <summary>零 I/O 派发：白名单 / 尺寸门槛 / 视图档 / 写入许可一律问 App 状态内核。</summary>
        public static ThumbRequestResult Decide(string extension, long sizeBytes, ViewMode mode,
                                                bool sameMotiveAsExplorer)
        {
            ToggleState toggles = new ToggleState(true, true, true, true);
            return ThumbCommands.Request(extension, sizeBytes, toggles, mode,
                                         sameMotiveAsExplorer);
        }

        /// <summary>写系统缩略图缓存是唯一显式例外，且只在相同动机下允许。</summary>
        public static bool VideoCacheWriteAllowed(bool sameMotiveAsExplorer)
        {
            return ThumbWriteRules.WriteAllowed(WriteZone.SystemCache, sameMotiveAsExplorer) &&
                   ThumbWriteRules.ExceptionCount == 1 &&
                   ThumbWriteRules.NeedsThreeZoneEvidence();
        }

        /// <summary>
        /// 可见条目的唯一取图入口：请求那一刻装配一份不可变快照，
        /// Fs 层在独立 STA 后台 worker 上取图；关闭时立即返回 null，
        /// 既不实例化 COM，也不让 WPF WIC 系统包装读文件（默认关闭纪律）。
        /// </summary>
        public BitmapSource Provide(PathKey path, string extension, long sizeBytes,
                                    ViewMode mode, int generation, long stamp,
                                    bool sameMotiveAsExplorer)
        {
            if (path == null) return null;
            // 未启用时连图片自解码也不做：WPF WIC 包装读文件同样是真实 I/O。
            if (!host.Enabled) return null;
            ThumbRequestResult decision = Decide(extension, sizeBytes, mode,
                                                 sameMotiveAsExplorer);
            int px = IconChannelRules.ForView(mode);
            if (!IconChannelRules.IsKnown(px)) px = IconChannelRules.FallbackPx;
            bool video = VideoThumbRules.IsSupported(extension) &&
                         VideoThumbRules.Allows(extension, sizeBytes);
            bool image = ThumbWhitelistRules.IsWhitelisted(extension) &&
                         ThumbWhitelistRules.Allows(extension, sizeBytes);
            if (image && decision.Accepted) return DecodeImage(path.Value, px,
                ExifOrientationRules.DefaultOrientation);
            IconThumbRequest snapshot = new IconThumbRequest(path, extension, sizeBytes, px,
                video, decision.Accepted, sameMotiveAsExplorer,
                ExifOrientationRules.DefaultOrientation, generation, stamp);
            IconThumbResult result = host.Request(snapshot);
            if (result == null || !result.Executed || result.HBitmap == IntPtr.Zero) return null;
            try
            {
                return Imaging.CreateBitmapSourceFromHBitmap(result.HBitmap, IntPtr.Zero,
                    new Int32Rect(0, 0, result.Pixels, result.Pixels),
                    BitmapSizeOptions.FromEmptyOptions());
            }
            catch (Exception)
            {
                return null;
            }
            finally
            {
                host.Release(result.HBitmap);
            }
        }

        /// <summary>
        /// 图片两档自解码（96 / 256）+ EXIF 八值方向：走 WPF 内置的 WIC 托管包装，
        /// 不引入第三方解码器，也不手写 IWIC* vtable。失败返回 null 交回落类型图标。
        /// </summary>
        public static BitmapSource DecodeImage(string path, int targetPx, int rawOrientation)
        {
            if (string.IsNullOrEmpty(path)) return null;
            try
            {
                BitmapDecoder decoder = BitmapDecoder.Create(new Uri(path),
                    BitmapCreateOptions.IgnoreColorProfile, BitmapCacheOption.OnLoad);
                if (decoder.Frames.Count == 0) return null;
                BitmapSource source = decoder.Frames[0];
                int orientation = ExifOrientationRules.Normalize(rawOrientation);
                int turns = ExifOrientationRules.QuarterTurns(orientation);
                if (turns != 0)
                {
                    source = new TransformedBitmap(source,
                        new RotateTransform(turns * 90.0));
                }
                if (ExifOrientationRules.FlipHorizontal(orientation))
                {
                    source = new TransformedBitmap(source, new ScaleTransform(-1.0, 1.0));
                }
                int tier = targetPx >= (int)IconSize.Jumbo256
                    ? (int)IconSize.Jumbo256 : (int)IconSize.Large96;
                double width = source.PixelWidth;
                if (width > 0.0)
                {
                    double scale = ((double)tier) / width;
                    source = new TransformedBitmap(source, new ScaleTransform(scale, scale));
                }
                return source;
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>产品 --selftest 可复用的零 I/O 判据，不创建窗口、不实例化 COM。</summary>
        public static bool ContractIsHonest()
        {
            return !new IconThumbHost().Enabled &&
                   !IconThumbContractRules.IsRuntimeVerified() &&
                   IconThumbContractRules.SoleWriteException() &&
                   IconThumbContractRules.RequiresStaThread();
        }
    }
}
