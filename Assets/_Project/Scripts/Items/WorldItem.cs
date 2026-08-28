using Residuum.Core;
using UnityEngine;

namespace Residuum.Items
{
    /// <summary>
    /// 放置在场景中的可拾取道具，将拾取请求交给玩家的物品栏处理。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class WorldItem : MonoBehaviour, IInteractable
    {
        [Tooltip("这件道具对应哪个编号槽。勾上「是手电筒」时本字段忽略")]
        [Range(0, 2)]
        [SerializeField] private int _slotIndex;

        [Tooltip("勾上表示这是手电筒，走独立的拾取路径")]
        [SerializeField] private bool _isFlashlight;

        [Tooltip("道具的中文名，用于交互提示")]
        [SerializeField] private string _itemName = "道具";

        [Tooltip("提示文本模板，{0} 会被替换成道具名")]
        [SerializeField] private string _promptFormat = "[E] 捡起 {0}";

        public string PromptText => string.Format(_promptFormat, _itemName);

        public bool CanInteract => true;

        private void Awake()
        {
            if (!_isFlashlight && (_slotIndex < 0 || _slotIndex > 2))
            {
                Debug.LogError($"WorldItem「{gameObject.name}」的编号槽必须在 0 到 2 之间。", this);
            }

            if (GetComponent<Collider>() == null)
            {
                Debug.LogWarning($"WorldItem「{gameObject.name}」没有 Collider，准星无法命中，无法拾取。", this);
            }
        }

        public void Interact(GameObject interactor)
        {
            if (interactor == null)
            {
                Debug.LogWarning($"WorldItem「{gameObject.name}」交互失败：交互者为空，无法找到物品栏。", this);
                return;
            }

            ItemSlotSystem itemSlotSystem = interactor.GetComponentInChildren<ItemSlotSystem>();
            if (itemSlotSystem == null)
            {
                Debug.LogWarning($"WorldItem「{gameObject.name}」交互失败：在交互者「{interactor.name}」身上找不到 ItemSlotSystem 物品栏。", this);
                return;
            }

            if (_isFlashlight)
            {
                itemSlotSystem.TryPickUpFlashlight();
                return;
            }

            itemSlotSystem.TryPickUpSlot(_slotIndex);
        }
    }
}
