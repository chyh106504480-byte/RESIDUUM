namespace Residuum.Core
{
    /// <summary>
    /// 可装备到道具槽的手持物品：手电筒、EMF 读数器、紫外线灯、鬼影书。
    ///
    /// 【为什么放在 Core 而不是 Items】它是跨模块契约：Items 的手电实现它，
    /// Evidence 的 EMF / 紫外线灯 / 鬼影书也实现它，UI 要读 ItemName。
    /// 若留在 Residuum.Items，证据模块就得 using Residuum.Items，
    /// 而铁律只放行 Core 与 Evidence。收进 Core 后无需任何跨模块例外。
    /// 与 IInteractable 同理。
    ///
    /// 生命周期约定：
    ///   OnEquip()   —— 显示手持模型，开始工作
    ///   OnUnequip() —— 隐藏模型，停止所有协程与检测（重要：不停会造成隐形耗电与误采集）
    ///   OnPrimaryUse() —— 鼠标左键
    /// </summary>
    public interface IHoldable
    {
        /// <summary>显示在 HUD 上的中文物品名</summary>
        string ItemName { get; }

        void OnEquip();
        void OnUnequip();
        void OnPrimaryUse();
    }
}
