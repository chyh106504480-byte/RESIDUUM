namespace Residuum.Core
{
    /// <summary>
    /// 单局结算结果。评级映射：
    ///   Perfect  → S   判定正确 + 集齐 2 项证据 + 理智 > 30%
    ///   Success  → A   判定正确
    ///   Survived → C   判定错误但活着离开
    ///   Died     → F   被猎杀致死
    /// </summary>
    public enum RoundResult
    {
        Perfect = 0,
        Success = 1,
        Survived = 2,
        Died = 3
    }
}
