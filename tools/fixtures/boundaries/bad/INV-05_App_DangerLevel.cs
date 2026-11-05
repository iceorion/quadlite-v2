namespace QuadLite.App
{
    // 负向对照：危险判级出现在 SafetyGuard 之外，违反单点判级，应令 INV-05 变红。
    internal sealed class BadDangerLevel
    {
        private int Decide()
        {
            return (int)DangerLevel.High;
        }
    }
}
