using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using QuadLite.Core;

namespace QuadLite.Fs
{
    /// <summary>目录枚举：分批 + generation 令牌；迟到批次由调用方丢弃，取消后不再发批。</summary>
    public sealed class DirSource : IDirSource
    {
        public const int BatchSize = 128;

        public void Enumerate(PathKey path, int generation, Action<EnumBatch> batch, CancellationToken token)
        {
            if (path == null || batch == null) return;
            string[] entries;
            try
            {
                entries = Directory.GetFileSystemEntries(path.Value);
            }
            catch (Exception)
            {
                batch(new EnumBatch(generation, new List<PathKey>(), true));
                return;
            }

            List<PathKey> page = new List<PathKey>();
            for (int i = 0; i < entries.Length; i++)
            {
                if (token.IsCancellationRequested) return;
                PathKey key;
                if (!PathKey.TryNormalize(entries[i], out key)) continue;
                page.Add(key);
                if (page.Count >= BatchSize)
                {
                    batch(new EnumBatch(generation, page, false));
                    page = new List<PathKey>();
                }
            }
            if (token.IsCancellationRequested) return;
            batch(new EnumBatch(generation, page, true));
        }

        public static bool IsLateBatch(int currentGeneration, int batchGeneration)
        {
            return batchGeneration != currentGeneration;
        }
    }
}
