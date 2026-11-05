using System;

namespace QuadLite.Core
{
    /// <summary>一个标签；各标签独立持有历史 / 行集 / 排序 / 滚动 / 令牌（§5.8 / FR-02）。</summary>
    public sealed class TabInfo
    {
        private readonly int id;
        private readonly PathKey path;
        private readonly int historyCount;
        private readonly ViewMode view;

        public TabInfo(int id, PathKey path, int historyCount, ViewMode view)
        {
            this.id = id;
            this.path = path;
            this.historyCount = historyCount;
            this.view = view;
        }

        public int Id { get { return id; } }
        public PathKey Path { get { return path; } }
        public int HistoryCount { get { return historyCount; } }
        public ViewMode View { get { return view; } }
    }

    /// <summary>
    /// 标签组规则（FR-02）：拖动改序返回新数组、不改入参；冷启动从会话读回 activeTab，
    /// 不强制跳到最左；每标签独立历史 / 视图。纯内存、零 I/O。
    /// </summary>
    public static class TabRules
    {
        public static bool AreIdsUnique(int[] ids)
        {
            if (ids == null || ids.Length == 0) return false;
            for (int i = 0; i < ids.Length; i++)
            {
                for (int j = i + 1; j < ids.Length; j++)
                {
                    if (ids[i] == ids[j]) return false;
                }
            }
            return true;
        }

        public static bool IsReorderValid(int[] ids, int from, int to)
        {
            if (ids == null || ids.Length == 0) return false;
            return from >= 0 && from < ids.Length && to >= 0 && to < ids.Length;
        }

        /// <summary>改序：返回新数组，入参保持不变。</summary>
        public static int[] Reorder(int[] ids, int from, int to)
        {
            if (!AreIdsUnique(ids) || !IsReorderValid(ids, from, to)) return null;
            int[] copy = (int[])ids.Clone();
            int moved = copy[from];
            if (from < to)
            {
                for (int i = from; i < to; i++) copy[i] = copy[i + 1];
            }
            else if (from > to)
            {
                for (int i = from; i > to; i--) copy[i] = copy[i - 1];
            }
            copy[to] = moved;
            return copy;
        }

        public static int[] Add(int[] ids, int newId)
        {
            if (ids == null) return null;
            for (int i = 0; i < ids.Length; i++)
            {
                if (ids[i] == newId) return null;
            }
            int[] result = new int[ids.Length + 1];
            for (int i = 0; i < ids.Length; i++) result[i] = ids[i];
            result[ids.Length] = newId;
            return result;
        }

        public static int[] RemoveAt(int[] ids, int index)
        {
            if (ids == null || index < 0 || index >= ids.Length) return null;
            if (ids.Length == 1) return null;  // 至少保留一个标签
            int[] result = new int[ids.Length - 1];
            int k = 0;
            for (int i = 0; i < ids.Length; i++)
            {
                if (i == index) continue;
                result[k] = ids[i];
                k++;
            }
            return result;
        }

        /// <summary>改序后活动标签跟随同一个 id，不被换成新的最左项。</summary>
        public static int ActiveAfterReorder(int activeId, int[] after)
        {
            if (after == null || !AreIdsUnique(after)) return -1;
            for (int i = 0; i < after.Length; i++)
            {
                if (after[i] == activeId) return activeId;
            }
            return after[0];
        }

        /// <summary>关闭的是活动标签时回落到相邻项；关闭别的标签时活动项不变。</summary>
        public static int ActiveAfterClose(int activeId, int closedIndex, int[] remaining)
        {
            if (remaining == null || remaining.Length == 0) return -1;
            for (int i = 0; i < remaining.Length; i++)
            {
                if (remaining[i] == activeId) return activeId;
            }
            int fallback = closedIndex < remaining.Length ? closedIndex : remaining.Length - 1;
            if (fallback < 0) fallback = 0;
            return remaining[fallback];
        }

        /// <summary>冷启动不跳最左：会话里读回的 activeTab 只要仍存在就原样保留。</summary>
        public static int ColdStartActive(int activeId, int[] ids)
        {
            if (ids == null || !AreIdsUnique(ids) || activeId < 0) return 0;
            for (int i = 0; i < ids.Length; i++)
            {
                if (ids[i] == activeId) return activeId;
            }
            return ids[0];
        }

        public static bool ColdStartKeepsActive(int activeId, int[] ids)
        {
            if (ids == null || activeId < 0) return false;
            return ColdStartActive(activeId, ids) == activeId;
        }

        public static bool HasAtLeastOne(int[] ids)
        {
            return ids != null && ids.Length >= 1;
        }

        public static bool AreHistoriesIndependent(TabInfo[] tabs)
        {
            if (tabs == null) return false;
            for (int i = 0; i < tabs.Length; i++)
            {
                if (tabs[i] == null) return false;
                for (int j = i + 1; j < tabs.Length; j++)
                {
                    if (tabs[j] == null || tabs[i].Id == tabs[j].Id) return false;
                }
            }
            return true;
        }
    }
}
