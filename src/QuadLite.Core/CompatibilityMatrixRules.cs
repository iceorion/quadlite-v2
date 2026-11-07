using System;

namespace QuadLite.Core
{
    /// <summary>
    /// §8.3 H2 兼容矩阵：Win11 VM / LTSC 两种系统 × NTFS / exFAT / SMB / 只读载体四种介质，
    /// 每格都要有独立证据；只读载体按 §5.13 进入内存会话模式而非静默回落用户目录。
    /// 纯判据，零 I/O；真实兼容矩阵实测未执行，未验证。
    /// </summary>
    public static class CompatibilityMatrixRules
    {
        public const int OsCount = 2;
        public const int FsCount = 4;
        public const int CellCount = 8;

        public static string[] OperatingSystems()
        {
            return new string[] { "win11-vm", "ltsc" };
        }

        public static string[] FileSystems()
        {
            return new string[] { "ntfs", "exfat", "smb", "read-only-carrier" };
        }

        public static bool ReadOnlyCarrierUsesMemorySession()
        {
            return true;
        }

        public static bool CellInRange(int os, int fs)
        {
            return os >= 0 && os < OsCount && fs >= 0 && fs < FsCount;
        }

        /// <summary>2×4 全部八个格子都必须有实测结论，任一格缺证据即不完整。</summary>
        public static bool MatrixComplete(bool[,] measured)
        {
            if (measured == null) return false;
            if (measured.GetLength(0) != OsCount || measured.GetLength(1) != FsCount) return false;
            for (int i = 0; i < OsCount; i++)
            {
                for (int j = 0; j < FsCount; j++)
                {
                    if (!measured[i, j]) return false;
                }
            }
            return true;
        }

        public static bool CountsMatchDeclaration()
        {
            return OperatingSystems().Length == OsCount && FileSystems().Length == FsCount &&
                   OsCount * FsCount == CellCount && CellInRange(1, 3) && !CellInRange(2, 0) &&
                   MatrixComplete(new bool[OsCount, FsCount]) == false;
        }
    }
}
