using System;

namespace QuadLite.Core
{
    /// <summary>SafeWalk 的四道边界；每条边界在 runner 里单独一格验证。</summary>
    public enum WalkReject { None, EmptySegment, EscapeRoot, AbsoluteInjection, TooLong }

    /// <summary>相对路径拼接：纯字符串，最终规范化仍交给 PathKey 单一来源。</summary>
    public static class SafeWalk
    {
        public const int MaxSegments = 256;

        public static bool TryJoin(PathKey root, string child, out PathKey joined, out WalkReject reject)
        {
            joined = null;
            reject = WalkReject.None;
            if (root == null) return false;
            if (child == null || child.Trim().Length == 0)
            {
                reject = WalkReject.EmptySegment;
                return false;
            }

            string c = child.Replace('/', '\\');

            // 边界 3：绝对路径 / 设备命名空间注入
            if (c.StartsWith(@"\\", StringComparison.Ordinal) ||
                c.StartsWith(@"\", StringComparison.Ordinal) ||
                c.IndexOf(':') >= 0 ||
                c.StartsWith(@"\\.\", StringComparison.Ordinal))
            {
                reject = WalkReject.AbsoluteInjection;
                return false;
            }

            string[] seg = c.Split(new char[] { '\\' }, StringSplitOptions.RemoveEmptyEntries);
            if (seg.Length == 0 || seg.Length > MaxSegments)
            {
                reject = seg.Length == 0 ? WalkReject.EmptySegment : WalkReject.TooLong;
                return false;
            }

            // 边界 1：空段 / 控制字符 / 非法文件名字符；边界 2：.. 越界根
            int baseDepth = root.Kind == PathKind.LongPath ? 0 : CountSegments(root.Tail);
            int depth = baseDepth;
            for (int i = 0; i < seg.Length; i++)
            {
                string part = seg[i];
                if (part.Trim().Length == 0 || HasInvalidChar(part))
                {
                    reject = WalkReject.EmptySegment;
                    return false;
                }
                if (part == ".") continue;
                if (part == "..")
                {
                    if (root.Kind == PathKind.LongPath)
                    {
                        reject = WalkReject.EscapeRoot;
                        return false;
                    }
                    depth--;
                    if (depth < 0)
                    {
                        reject = WalkReject.EscapeRoot;
                        return false;
                    }
                    continue;
                }
                depth++;
            }

            // 边界 4：总长度上限
            string candidate = root.Value.TrimEnd('\\') + @"\" + c;
            if (candidate.Length > PathKey.MaxPathLength)
            {
                reject = WalkReject.TooLong;
                return false;
            }
            if (!PathKey.TryNormalize(candidate, out joined))
            {
                reject = WalkReject.TooLong;
                return false;
            }
            return true;
        }

        public static int CountSegments(string tail)
        {
            if (string.IsNullOrEmpty(tail)) return 0;
            return tail.Split(new char[] { '\\' }, StringSplitOptions.RemoveEmptyEntries).Length;
        }

        private static bool HasInvalidChar(string part)
        {
            for (int i = 0; i < part.Length; i++)
            {
                char ch = part[i];
                if (ch < ' ') return true;
                if (ch == '<' || ch == '>' || ch == ':' || ch == '"' ||
                    ch == '|' || ch == '*' || ch == '?') return true;
            }
            return false;
        }
    }
}
