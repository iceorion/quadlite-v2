namespace QuadLite.Core
{
    // 负向对照：搬运未经 IFileOperator 端口，应令 B-07 变红。
    public static class BadDirectMove
    {
        public static void Run()
        {
            System.IO.File.Move("a", "b");
        }
    }
}
