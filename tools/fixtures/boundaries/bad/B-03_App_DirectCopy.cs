namespace QuadLite.App
{
    // 负向对照：UI 层直调 File.Copy，应令 B-03（及 B-07）变红。
    internal sealed class BadDirectCopy
    {
        private void Run()
        {
            System.IO.File.Copy("a", "b");
        }
    }
}
