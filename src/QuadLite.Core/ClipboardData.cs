using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace QuadLite.Core
{
    /// <summary>
    /// 剪贴板内部表示以系统格式为唯一真相：一组路径 + Preferred DropEffect。
    /// 不另建待办列表，也不把「剪切」记成应用内状态。
    /// </summary>
    public sealed class ClipboardData
    {
        private readonly IReadOnlyList<PathKey> items;
        private readonly DropEffect effect;

        public ClipboardData(IList<PathKey> items, DropEffect effect)
        {
            List<PathKey> copy = new List<PathKey>();
            if (items != null)
            {
                for (int i = 0; i < items.Count; i++) copy.Add(items[i]);
            }
            this.items = new ReadOnlyCollection<PathKey>(copy);
            this.effect = effect;
        }

        public IReadOnlyList<PathKey> Items { get { return items; } }
        public DropEffect Effect { get { return effect; } }
    }

    public static class ClipboardRules
    {
        public const string FormatName = "CF_HDROP";
        public const string EffectName = "Preferred DropEffect";

        public static bool IsValid(ClipboardData data)
        {
            if (data == null || data.Items == null || data.Items.Count == 0) return false;
            if (data.Effect == DropEffect.None) return false;
            for (int i = 0; i < data.Items.Count; i++)
            {
                if (data.Items[i] == null || data.Items[i].Value.Length == 0) return false;
                for (int j = 0; j < i; j++)
                {
                    if (data.Items[i].Equals(data.Items[j])) return false;
                }
            }
            return true;
        }

        /// <summary>窗格之间走编排；只有拖到外部目标才使用系统数据对象。</summary>
        public static bool IsInternalDrop(bool targetIsExternal)
        {
            return !targetIsExternal;
        }

        public static DropEffect EffectFor(bool control, bool shift)
        {
            if (shift) return DropEffect.Move;
            if (control) return DropEffect.Copy;
            return DropEffect.Move;
        }

        /// <summary>对外拖放只有真正被接受才需要刷新源窗格。</summary>
        public static bool NeedsSourceRefresh(bool targetIsExternal, bool dropAccepted, DropEffect effect)
        {
            if (!targetIsExternal || !dropAccepted) return false;
            return effect == DropEffect.Move;
        }

        public static string Describe(ClipboardData data)
        {
            if (data == null) return "";
            return FormatName + "+" + EffectName + " items=" + data.Items.Count.ToString() +
                   " effect=" + data.Effect.ToString();
        }
    }
}
