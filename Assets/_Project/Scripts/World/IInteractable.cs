using UnityEngine;

namespace Residuum.World
{
    /// <summary>
    /// 玩家可以用准星瞄准并按 E 交互的物体：门、抽屉、开关、藏匿点、已放置的鬼影书。
    /// PlayerInteractor 通过射线检测找到实现此接口的组件。
    /// </summary>
    public interface IInteractable
    {
        /// <summary>显示在准星下方的提示文字，例如 "[E] 开门"。返回 null 表示不显示提示。</summary>
        string PromptText { get; }

        /// <summary>当前是否可交互。为 false 时准星不高亮、按 E 无反应。</summary>
        bool CanInteract { get; }

        /// <summary>执行交互。interactor 为发起交互的玩家 GameObject。</summary>
        void Interact(GameObject interactor);
    }
}
