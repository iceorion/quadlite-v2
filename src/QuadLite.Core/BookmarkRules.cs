using System;
using System.Collections.Generic;

namespace QuadLite.Core
{
    public enum BookmarkState { Ok, Missing, Unknown }

    public sealed class BookmarkItem
    {
        private readonly string name;
        private readonly PathKey path;
        private readonly bool isDirectory;

        public BookmarkItem(string name, PathKey path, bool isDirectory)
        {
            this.name = name;
            this.path = path;
            this.isDirectory = isDirectory;
        }

        public string Name { get { return name; } }
        public PathKey Path { get { return path; } }
        public bool IsDirectory { get { return isDirectory; } }
    }

    /// <summary>
    /// 书签规则（§5.8 / FR-06 / §6.4）：只收目录；三态 Ok/Missing/Unknown，失效判定
    /// 离 UI 线程故未探测一律 Unknown；上限 64；单条坏项只丢该条，不整份拒读；
    /// 点击只导航活动窗格。纯内存、零 I/O。
    /// </summary>
    public static class BookmarkRules
    {
        public const int MaxItems = 64;

        public static bool CanAdd(BookmarkItem item)
        {
            if (item == null) return false;
            if (string.IsNullOrEmpty(item.Name)) return false;
            if (item.Path == null) return false;
            return item.IsDirectory;
        }

        public static int Cap(int count)
        {
            if (count < 0) return 0;
            return count > MaxItems ? MaxItems : count;
        }

        /// <summary>未探测完成前不得由 UI 自行推断为存在或失效。</summary>
        public static BookmarkState StateFor(bool probeDone, bool exists)
        {
            if (!probeDone) return BookmarkState.Unknown;
            return exists ? BookmarkState.Ok : BookmarkState.Missing;
        }

        /// <summary>坏项隔离：过滤后仍保留其余条目，不整份拒读。</summary>
        public static BookmarkItem[] Sanitize(BookmarkItem[] items)
        {
            List<BookmarkItem> kept = new List<BookmarkItem>();
            if (items == null) return kept.ToArray();
            for (int i = 0; i < items.Length && kept.Count < MaxItems; i++)
            {
                if (CanAdd(items[i])) kept.Add(items[i]);
            }
            return kept.ToArray();
        }

        public static int RemainingAfterDrop(int total, int badIndex)
        {
            if (total <= 0 || badIndex < 0 || badIndex >= total) return -1;
            return total - 1;
        }

        public static bool BadItemIsolated(int total, int badIndex)
        {
            return RemainingAfterDrop(total, badIndex) == total - 1;
        }

        public static int TargetPane(int activePaneId)
        {
            return activePaneId;
        }

        public static bool IsClickTargetValid(int targetPaneId, int activePaneId)
        {
            return targetPaneId == activePaneId;
        }
    }
}
