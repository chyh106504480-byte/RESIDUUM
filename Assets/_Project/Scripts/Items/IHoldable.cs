namespace Residuum.Items
{
    /// <summary>
    /// 可装备到道具槽的手持物品：手电筒、EMF 读数器、紫外线灯、鬼影书。
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
