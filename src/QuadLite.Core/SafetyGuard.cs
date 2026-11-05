using System;

namespace QuadLite.Core
{
    /// <summary>危险判级只在 SafetyGuard 一处；矩阵逐格有测试（见零依赖 runner）。</summary>
    public enum DangerLevel { Forbidden, Normal, TypePath }

    public enum GuardTarget { SystemPath, VolumeRoot, ProtectedVolume, Normal }

    public sealed class GuardFacts
    {
        private readonly bool isReadOnly;

        public GuardFacts(bool isReadOnly)
        {
            this.isReadOnly = isReadOnly;
        }

        public bool IsReadOnly { get { return isReadOnly; } }
    }

    public static class SafetyGuard
    {
        private const int TargetColumns = 4;
        private static readonly DangerLevel[,] Matrix = BuildMatrix();

        public static int RowCount { get { return Matrix.GetLength(0); } }
        public static int ColumnCount { get { return Matrix.GetLength(1); } }

        public static bool IsKnownLevel(DangerLevel level)
        {
            return Enum.IsDefined(typeof(DangerLevel), level);
        }

        public static DangerLevel Cell(int row, int col)
        {
            return Matrix[row, col];
        }

        /// <summary>只读降级是系统性前置守卫，优先于矩阵（C-06）。</summary>
        public static DangerLevel Decide(OpKind kind, GuardTarget target, GuardFacts facts)
        {
            if (facts != null && facts.IsReadOnly) return DangerLevel.Forbidden;
            if (!OpPlanBuilder.IsKindKnown((int)kind)) return DangerLevel.Forbidden;
            int col = (int)target;
            if (col < 0 || col >= TargetColumns) return DangerLevel.Forbidden;
            return Matrix[(int)kind, col];
        }

        public static bool IsMatrixComplete()
        {
            if (RowCount != OpPlanBuilder.KindCount || ColumnCount != TargetColumns) return false;
            if (Enum.GetValues(typeof(DangerLevel)).Length != 3) return false;
            for (int r = 0; r < RowCount; r++)
            {
                for (int c = 0; c < ColumnCount; c++)
                {
                    if (!IsKnownLevel(Matrix[r, c])) return false;
                }
            }
            return true;
        }

        private static DangerLevel[,] BuildMatrix()
        {
            DangerLevel[,] m = new DangerLevel[OpPlanBuilder.KindCount, TargetColumns];
            for (int k = 0; k < OpPlanBuilder.KindCount; k++)
            {
                for (int t = 0; t < TargetColumns; t++) m[k, t] = DangerLevel.Normal;
                m[k, (int)GuardTarget.SystemPath] = DangerLevel.Forbidden;
                m[k, (int)GuardTarget.VolumeRoot] = IsDeleteKind((OpKind)k)
                    ? DangerLevel.Forbidden : DangerLevel.TypePath;
                m[k, (int)GuardTarget.ProtectedVolume] =
                    ((OpKind)k == OpKind.PermanentDelete || (OpKind)k == OpKind.CutPaste)
                    ? DangerLevel.TypePath : DangerLevel.Normal;
                m[k, (int)GuardTarget.Normal] =
                    ((OpKind)k == OpKind.PermanentDelete)
                    ? DangerLevel.TypePath : DangerLevel.Normal;
            }
            return m;
        }

        private static bool IsDeleteKind(OpKind kind)
        {
            return kind == OpKind.Delete || kind == OpKind.Recycle ||
                   kind == OpKind.PermanentDelete || kind == OpKind.CutPaste;
        }
    }
}
