namespace Residuum.Evidence
{
    /// <summary>
    /// 任何能够产出证据的设备实现此接口。
    /// 实现方在成功采集时必须调用 GameEvents.RaiseEvidenceFound()，
    /// 且同一回合内对同一项证据只触发一次。
    /// </summary>
    public interface IEvidenceSource
    {
        EvidenceType ProvidedEvidence { get; }

        /// <summary>尝试采集证据。成功返回 true 并通过 out 参数给出证据类型。</summary>
        bool TryCollect(out EvidenceType evidence);
    }
}
