namespace Residuum.Evidence
{
    /// <summary>
    /// 三项证据。数量与 3×3 推理表强绑定 —— 增删任何一项都会破坏
    /// 「每种鬼恰好持有 2 项、任意两项组合唯一确定一种鬼」的数学性质。
    /// 若要扩展到 4 项证据，必须同时把鬼种扩展到 C(4,2)=6 种。
    /// </summary>
    public enum EvidenceType
    {
        None = 0,
        /// <summary>EMF 读数器达到 5 级</summary>
        EMF5 = 1,
        /// <summary>紫外线灯照出的指纹</summary>
        UVFingerprint = 2,
        /// <summary>鬼影书上出现的字迹</summary>
        GhostWriting = 3
    }
}
