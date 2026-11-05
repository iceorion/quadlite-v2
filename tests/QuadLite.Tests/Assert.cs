using System;

namespace QuadLite.Tests
{
    /// <summary>零依赖断言：任一失败即抛，交给 Program 汇总成一条红组。</summary>
    internal static class Check
    {
        public static void True(bool condition, string label)
        {
            if (!condition) throw new Exception("expect true: " + label);
        }

        public static void False(bool condition, string label)
        {
            True(!condition, label);
        }

        public static void Equal(object expected, object actual, string label)
        {
            if (!object.Equals(expected, actual))
            {
                throw new Exception("expect " + Convert.ToString(expected) + " but got " +
                                   Convert.ToString(actual) + " [" + label + "]");
            }
        }

        public static void NotNull(object value, string label)
        {
            if (value == null) throw new Exception("expect non-null: " + label);
        }
    }
}
