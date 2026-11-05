using System;
using System.IO;

namespace QuadLite.Fs
{
    /// <summary>
    /// 原子写：临时文件 → 替换 → 备份 → 坏值隔离。只写程序同级 data\ 目录（§5.5 / §6.4）。
    /// 本层是真实文件实现，运行期行为在发布线 H3 取证前标「未验证」。
    /// </summary>
    public sealed class AtomicFile
    {
        public const string BackupSuffix = ".bak";
        public const string BadSuffixPrefix = ".bad-";

        public static bool Write(string targetPath, string content)
        {
            if (string.IsNullOrEmpty(targetPath) || content == null) return false;
            string temp = targetPath + ".tmp";
            string backup = targetPath + BackupSuffix;
            try
            {
                File.WriteAllText(temp, content);
                if (File.Exists(targetPath))
                {
                    File.Replace(temp, targetPath, backup);
                }
                else
                {
                    File.Move(temp, targetPath);
                }
                return true;
            }
            catch (Exception)
            {
                Quarantine(targetPath, content);
                return false;
            }
        }

        /// <summary>读失败或未来版本拒读时，坏文件带时间戳另存，不静默覆盖也不整份丢失。</summary>
        public static void Quarantine(string targetPath, string content)
        {
            if (string.IsNullOrEmpty(targetPath)) return;
            try
            {
                string bad = targetPath + BadSuffixPrefix + DateTime.UtcNow.ToString("yyyyMMddHHmmss");
                File.WriteAllText(bad, content == null ? "" : content);
            }
            catch (Exception)
            {
                // 隔离失败不阻断主流程；只读降级由上层明示。
            }
        }

        public static string Read(string targetPath)
        {
            if (string.IsNullOrEmpty(targetPath) || !File.Exists(targetPath)) return "";
            try
            {
                return File.ReadAllText(targetPath);
            }
            catch (Exception)
            {
                return "";
            }
        }
    }
}
