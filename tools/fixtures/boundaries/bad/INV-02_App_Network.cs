namespace QuadLite.App
{
    // 负向对照：产品源码出现联网 API，违反「零联网」不变量，应令 INV-02 变红。
    internal sealed class BadNetwork
    {
        private void Run()
        {
            var client = new System.Net.WebClient();
        }
    }
}
