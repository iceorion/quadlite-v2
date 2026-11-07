using System;

namespace QuadLite.Core
{
    /// <summary>
    /// §8.3 H1 / NFR-10 高 DPI 四档对照：PerMonitorV2 manifest、100/125/150/200%，
    /// 列宽 / 列头 / 几何断言在四档下都需通过，并配截图装置。
    /// 纯判据，零 I/O；真实 DPI 截图与几何实测未执行，未验证。
    /// </summary>
    public static class DpiMatrixRules
    {
        public const int ScaleCount = 4;
        public const int AssertionCount = 3;

        public static string ManifestMode()
        {
            return "PerMonitorV2";
        }

        public static int[] Scales()
        {
            return new int[] { 100, 125, 150, 200 };
        }

        public static string[] Assertions()
        {
            return new string[] { "column-width", "column-header", "geometry" };
        }

        public static bool RequiresScreenshotHarness()
        {
            return true;
        }

        /// <summary>四档缺一档即不完整（真判定，不是常量自证）。</summary>
        public static bool AllScalesCovered(int[] observed)
        {
            if (observed == null) return false;
            int[] want = Scales();
            for (int i = 0; i < want.Length; i++)
            {
                bool found = false;
                for (int j = 0; j < observed.Length; j++)
                {
                    if (observed[j] == want[i]) { found = true; break; }
                }
                if (!found) return false;
            }
            return true;
        }

        /// <summary>每档 × 每条断言都必须通过；矩阵维度或任一格为假即判红。</summary>
        public static bool GeometryAssertionsPass(bool[,] atScale)
        {
            if (atScale == null) return false;
            if (atScale.GetLength(0) != ScaleCount) return false;
            if (atScale.GetLength(1) != AssertionCount) return false;
            for (int i = 0; i < ScaleCount; i++)
            {
                for (int j = 0; j < AssertionCount; j++)
                {
                    if (!atScale[i, j]) return false;
                }
            }
            return true;
        }

        public static bool CountsMatchDeclaration()
        {
            return Scales().Length == ScaleCount && Assertions().Length == AssertionCount &&
                   AllScalesCovered(Scales()) && GeometryAssertionsPass(new bool[ScaleCount, AssertionCount]) == false;
        }
    }
}
