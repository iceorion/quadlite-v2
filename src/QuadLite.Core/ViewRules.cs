using System;

namespace QuadLite.Core
{
    /// <summary>
    /// 七档视图补充规则（§5.9 / FR-10）：图标档不渲染组头，缩略图目标只有 ViewSpecs 一个来源，
    /// 组头只在详细信息档出现。本类型是纯函数，不触碰任何控件。
    /// </summary>
    public static class ViewRules
    {
        public static bool RendersGroupHeader(ViewMode mode)
        {
            ViewSpec s = ViewSpecs.For(mode);
            return s != null && s.RendersGroupHeader;
        }

        /// <summary>需要缩略图的档位即图标档（TargetPx 非零），判定只走 ViewSpecs。</summary>
        public static bool IsIconMode(ViewMode mode)
        {
            ViewSpec s = ViewSpecs.For(mode);
            return s != null && s.TargetPx > 0;
        }

        public static int ThumbnailTarget(ViewMode mode)
        {
            ViewSpec s = ViewSpecs.For(mode);
            return s == null ? 0 : s.TargetPx;
        }

        /// <summary>菜单置灰、解码调度、缓存键三处共用的唯一判断，不另写一份。</summary>
        public static bool NeedsThumbnail(ViewMode mode)
        {
            return ViewSpecs.NeedsThumbnail(mode);
        }

        public static bool OnlyDetailsRendersGroupHeader()
        {
            if (ViewSpecs.GroupHeaderCount() != 1) return false;
            ViewSpec d = ViewSpecs.For(ViewMode.Details);
            return d != null && d.RendersGroupHeader;
        }

        public static int IconModeCount()
        {
            int n = 0;
            for (int i = 0; i < ViewSpecs.Count; i++)
            {
                if (NeedsThumbnail((ViewMode)i)) n++;
            }
            return n;
        }

        public static bool IsKnownMode(int raw)
        {
            return ViewSpecs.IsKnown(raw);
        }

        /// <summary>组头渲染与图标档互斥：任何图标档都不该渲染组头。</summary>
        public static bool GroupHeaderIsDetailsOnly(ViewMode mode)
        {
            return RendersGroupHeader(mode) == (mode == ViewMode.Details);
        }
    }
}
