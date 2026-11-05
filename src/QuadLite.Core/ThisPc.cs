using System;
using System.Collections.Generic;

namespace QuadLite.Core
{
    public enum ThisPcKind { Root, Drive, Folder }

    public sealed class ThisPcEntry
    {
        private readonly ThisPcKind kind;
        private readonly PathKey path;
        private readonly string label;

        public ThisPcEntry(ThisPcKind kind, PathKey path, string label)
        {
            this.kind = kind;
            this.path = path;
            this.label = label;
        }

        public ThisPcKind Kind { get { return kind; } }
        public PathKey Path { get { return path; } }
        public string Label { get { return label; } }
    }

    /// <summary>
    /// 「此电脑」层（§5.8 / FR-05）：条目由 IDirSource 合成后交给本类型整理，
    /// 这里只做纯逻辑编排，不读盘、不取图标；盘符按真实路径取图，不得回落通用文件夹图标。
    /// </summary>
    public static class ThisPc
    {
        public static ThisPcEntry[] Compose(string[] driveRoots)
        {
            List<ThisPcEntry> entries = new List<ThisPcEntry>();
            entries.Add(new ThisPcEntry(ThisPcKind.Root, null, "This PC"));
            if (driveRoots == null) return entries.ToArray();
            for (int i = 0; i < driveRoots.Length; i++)
            {
                if (!AddressRules.IsDriveItem(driveRoots[i])) continue;
                PathKey k;
                if (!PathKey.TryNormalize(driveRoots[i], out k)) continue;
                bool seen = false;
                for (int j = 0; j < entries.Count; j++)
                {
                    if (entries[j].Path != null && entries[j].Path.Equals(k)) { seen = true; break; }
                }
                if (!seen) entries.Add(new ThisPcEntry(ThisPcKind.Drive, k, k.Value));
            }
            return entries.ToArray();
        }

        /// <summary>盘符必须按真实路径取图标；其余条目可用通用图标。</summary>
        public static bool NeedsRealPathIcon(ThisPcEntry entry)
        {
            return entry != null && entry.Kind == ThisPcKind.Drive && entry.Path != null;
        }

        public static bool GenericIconFallbackAllowed(ThisPcEntry entry)
        {
            return entry != null && entry.Kind != ThisPcKind.Drive;
        }

        public static int CountOf(ThisPcEntry[] entries, ThisPcKind kind)
        {
            if (entries == null) return 0;
            int n = 0;
            for (int i = 0; i < entries.Length; i++)
            {
                if (entries[i] != null && entries[i].Kind == kind) n++;
            }
            return n;
        }

        public static bool HasRoot(ThisPcEntry[] entries)
        {
            return CountOf(entries, ThisPcKind.Root) == 1;
        }
    }
}
