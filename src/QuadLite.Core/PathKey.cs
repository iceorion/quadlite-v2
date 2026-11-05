using System;
using System.Collections.Generic;

namespace QuadLite.Core
{
    /// <summary>路径统一形式：纯字符串规范化，不访问文件系统，不依赖文件路径 API。</summary>
    public enum PathKind { Drive, Unc, LongPath }

    public sealed class PathKey : IEquatable<PathKey>
    {
        public const int MaxPathLength = 32767;
        private const string LongPrefix = @"\\?\";
        private const string DevicePrefix = @"\\.\";

        private readonly string value;
        private readonly string identity;
        private readonly string root;
        private readonly string tail;
        private readonly PathKind kind;

        private PathKey(string value, string root, string tail, PathKind kind)
        {
            this.value = value;
            this.root = root;
            this.tail = tail;
            this.kind = kind;
            this.identity = value.ToLowerInvariant();
        }

        public string Value { get { return value; } }
        public string Identity { get { return identity; } }
        public string Root { get { return root; } }
        public string Tail { get { return tail; } }
        public PathKind Kind { get { return kind; } }

        /// <summary>
        /// 覆盖：引号与空白、/ 与 \ 混用、重复分隔符、. 与 ..、越界根、UNC、长路径前缀、
        /// 设备命名空间。设备命名空间一律拒绝；长路径按字面保留，不做 .. 解析。
        /// </summary>
        public static bool TryNormalize(string raw, out PathKey key)
        {
            key = null;
            if (raw == null) return false;
            string s = TrimOuter(raw);
            if (s.Length == 0 || s.Length > MaxPathLength) return false;
            if (ContainsControl(s)) return false;
            s = s.Replace('/', '\\');
            if (s.StartsWith(DevicePrefix, StringComparison.Ordinal)) return false;

            if (s.StartsWith(LongPrefix, StringComparison.Ordinal))
            {
                string body = Collapse(s.Substring(LongPrefix.Length));
                if (body.Length == 0) return false;
                string v = LongPrefix + body;
                key = new PathKey(v, LongPrefix, body, PathKind.LongPath);
                return true;
            }

            if (s.StartsWith(@"\\", StringComparison.Ordinal))
            {
                string[] seg = s.Substring(2).Split(new char[] { '\\' }, StringSplitOptions.RemoveEmptyEntries);
                if (seg.Length < 2) return false;
                string server = seg[0];
                string share = seg[1];
                if (server == "." || server == ".." || share == "." || share == "..") return false;
                string resolved;
                if (!TryStack(seg, 2, out resolved)) return false;
                string r = @"\\" + server + @"\" + share;
                string v = resolved.Length == 0 ? r : r + @"\" + resolved;
                key = new PathKey(v, r, resolved, PathKind.Unc);
                return true;
            }

            if (s.Length >= 2 && s[1] == ':' && IsDriveLetter(s[0]))
            {
                if (s.Length > 2 && s[2] != '\\') return false;
                char drive = char.ToUpperInvariant(s[0]);
                string body = s.Length > 2 ? s.Substring(3) : "";
                string[] seg = body.Split(new char[] { '\\' }, StringSplitOptions.RemoveEmptyEntries);
                string resolved;
                if (!TryStack(seg, 0, out resolved)) return false;
                string r = drive + @":";
                string v = r + @"\" + resolved;
                key = new PathKey(v, r, resolved, PathKind.Drive);
                return true;
            }

            return false;
        }

        public static bool IsDriveLetter(char c)
        {
            return (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z');
        }

        /// <summary>折叠重复分隔符；用于长路径（按字面，不解析 ..）。</summary>
        public static string Collapse(string body)
        {
            string[] seg = body.Split(new char[] { '\\' }, StringSplitOptions.RemoveEmptyEntries);
            return string.Join(@"\", seg);
        }

        /// <summary>从 start 起解析 . 与 ..；弹空栈即越界根，返回 false。</summary>
        internal static bool TryStack(string[] seg, int start, out string resolved)
        {
            List<string> stack = new List<string>();
            for (int i = start; i < seg.Length; i++)
            {
                string part = seg[i];
                if (part.Length == 0 || part == ".") continue;
                if (part == "..")
                {
                    if (stack.Count == 0) { resolved = null; return false; }
                    stack.RemoveAt(stack.Count - 1);
                    continue;
                }
                stack.Add(part);
            }
            resolved = string.Join(@"\", stack.ToArray());
            return true;
        }

        public bool TryGetParent(out PathKey parent)
        {
            parent = null;
            if (tail.Length == 0) return false;
            int cut = tail.LastIndexOf('\\');
            string parentTail = cut < 0 ? "" : tail.Substring(0, cut);
            if (parentTail == tail) return false;
            string v = kind == PathKind.Drive
                ? root + @"\" + parentTail
                : root + (parentTail.Length == 0 ? "" : @"\" + parentTail);
            if (kind == PathKind.LongPath)
            {
                int slash = value.LastIndexOf('\\');
                if (slash <= LongPrefix.Length) return false;
                v = value.Substring(0, slash);
            }
            return TryNormalize(v, out parent);
        }

        public string GetLeafName()
        {
            string v = value.TrimEnd('\\');
            int slash = v.LastIndexOf('\\');
            return slash < 0 ? v : v.Substring(slash + 1);
        }

        public bool IsUnder(PathKey ancestor)
        {
            if (ancestor == null) return false;
            string prefix = ancestor.identity.TrimEnd('\\') + @"\";
            return identity.StartsWith(prefix, StringComparison.Ordinal);
        }

        public bool Equals(PathKey other)
        {
            if (other == null) return false;
            return kind == other.kind && identity == other.identity;
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as PathKey);
        }

        public override int GetHashCode()
        {
            return identity.GetHashCode() ^ (int)kind;
        }

        public override string ToString()
        {
            return value;
        }

        private static string TrimOuter(string raw)
        {
            string s = raw.Trim();
            if (s.Length >= 2 && ((s[0] == '"' && s[s.Length - 1] == '"') ||
                                  (s[0] == '\'' && s[s.Length - 1] == '\'')))
            {
                s = s.Substring(1, s.Length - 2).Trim();
            }
            return s;
        }

        private static bool ContainsControl(string s)
        {
            for (int i = 0; i < s.Length; i++)
            {
                if (s[i] < ' ') return true;
            }
            return false;
        }
    }
}
