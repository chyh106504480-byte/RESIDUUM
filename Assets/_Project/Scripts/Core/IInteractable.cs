using UnityEngine;

namespace Residuum.Core
{
    /// <summary>
    /// 玩家可以用准星瞄准并按 E 交互的物体：门、抽屉、开关、藏匿点、已放置的鬼影书。
    /// PlayerInteractor 通过射线检测找到实现此接口的组件。
    ///
    /// 【为什么放在 Core 而不是 World】它是跨模块契约：World 的门与藏匿点实现它，
    /// Ghost 的 AI 需要在附近搜索它，Items 将来也会用。若留在 Residuum.World，
    /// Ghost 就得 using Residuum.World，那等于给 RoomManager.Instance 这类
    /// 具体类型开了后门，而闸门只查 using 行、查不出你拿它干了什么。
    /// 收进 Core 后无需任何新的跨模块例外。
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
