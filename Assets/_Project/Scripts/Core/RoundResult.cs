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

    /// <summary>
    /// 单局结算明细。给结算界面用 —— RoundResult 只有一个等级字母，
    /// 拿不到「真凶是谁 / 玩家判了谁」，而那是推理玩法唯一的兑现时刻。
    ///
    /// 【为什么用鬼名字符串而不是 GhostDefinition】不让 Core 反过来依赖
    /// Residuum.Ghost。契约层保持单向依赖，否则 Ghost 与 Core 互相引用，
    /// 后面任何一方的改动都会波及另一方。
    /// </summary>
    public readonly struct RoundSummary
    {
        public readonly RoundResult Result;

        /// <summary>本局真正的鬼种名。异常路径下可能为空字符串。</summary>
        public readonly string ActualGhostName;

        /// <summary>玩家在笔记本里提交的判定名；未提交为 null。</summary>
        public readonly string GuessedGhostName;

        /// <summary>本局采集到的证据项数，0–2。</summary>
        public readonly int FoundEvidenceCount;

        /// <summary>本局耗时（秒）。</summary>
        public readonly float ElapsedSeconds;

        public RoundSummary(
            RoundResult result,
            string actualGhostName,
            string guessedGhostName,
            int foundEvidenceCount,
            float elapsedSeconds)
        {
            Result = result;
            ActualGhostName = actualGhostName;
            GuessedGhostName = guessedGhostName;
            FoundEvidenceCount = foundEvidenceCount;
            ElapsedSeconds = elapsedSeconds;
        }

        /// <summary>玩家是否提交过判定。</summary>
        public bool HasGuess => !string.IsNullOrEmpty(GuessedGhostName);
    }
}
