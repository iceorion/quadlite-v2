using System;
using System.Collections.Generic;

namespace QuadLite.Core
{
    public enum AddressReject
    {
        None, Empty, DeviceNamespace, EscapeAboveRoot, Relative, TooLong, ControlChar
    }

    /// <summary>
    /// 地址栏输入（§5.8 / FR-03）：可编辑 + 驱动器下拉，规范化统一交给 PathKey；
    /// 非法输入不导航，并返回结构化原因码供状态栏显示。纯字符串、零 I/O。
    /// </summary>
    public static class AddressRules
    {
        public static bool TryParse(string raw, out PathKey key, out AddressReject reject)
        {
            key = null;
            reject = AddressReject.None;
            if (raw == null) { reject = AddressReject.Empty; return false; }

            string s = StripOuterQuotes(raw.Trim());
            if (s.Length == 0) { reject = AddressReject.Empty; return false; }
            if (s.Length > PathKey.MaxPathLength) { reject = AddressReject.TooLong; return false; }
            for (int i = 0; i < s.Length; i++)
            {
                if (s[i] < ' ') { reject = AddressReject.ControlChar; return false; }
            }

            string norm = s.Replace('/', '\\');
            if (norm.StartsWith(@"\\.\", StringComparison.Ordinal))
            {
                reject = AddressReject.DeviceNamespace;
                return false;
            }

            if (PathKey.TryNormalize(raw, out key)) return true;

            key = null;
            bool driveRoot = norm.Length >= 3 && PathKey.IsDriveLetter(norm[0]) &&
                             norm[1] == ':' && norm[2] == '\\';
            bool uncRoot = norm.StartsWith(@"\\", StringComparison.Ordinal);
            if (!driveRoot && !uncRoot) { reject = AddressReject.Relative; return false; }
            reject = AddressReject.EscapeAboveRoot;
            return false;
        }

        public static bool ShouldNavigate(AddressReject reject)
        {
            return reject == AddressReject.None;
        }

        /// <summary>状态栏原因：结构化枚举名，不拼中文文案、不含输入原文。</summary>
        public static string RejectText(AddressReject reject)
        {
            return reject.ToString();
        }

        public static bool IsDriveItem(string raw)
        {
            PathKey k;
            if (!PathKey.TryNormalize(raw, out k)) return false;
            return k.Kind == PathKind.Drive && k.Value.Length == 3 && k.Value[1] == ':';
        }

        /// <summary>驱动器下拉只保留合法盘符根，按规范化形式去重，不改入参。</summary>
        public static string[] DriveDropdown(string[] drives)
        {
            List<string> result = new List<string>();
            if (drives == null) return result.ToArray();
            for (int i = 0; i < drives.Length; i++)
            {
                if (!IsDriveItem(drives[i])) continue;
                PathKey k;
                if (!PathKey.TryNormalize(drives[i], out k)) continue;
                bool seen = false;
                for (int j = 0; j < result.Count; j++)
                {
                    if (result[j] == k.Value) { seen = true; break; }
                }
                if (!seen) result.Add(k.Value);
            }
            return result.ToArray();
        }

        private static string StripOuterQuotes(string s)
        {
            if (s.Length >= 2 && ((s[0] == '"' && s[s.Length - 1] == '"') ||
                                  (s[0] == '\'' && s[s.Length - 1] == '\'')))
            {
                return s.Substring(1, s.Length - 2).Trim();
            }
            return s;
        }
    }
}
