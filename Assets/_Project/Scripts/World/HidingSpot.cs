using Residuum.Core;
using UnityEngine;

namespace Residuum.World
{
    [DisallowMultipleComponent]
    public sealed class HidingSpot : MonoBehaviour, IInteractable
    {
        [Tooltip("玩家躲进来后所站的位置与朝向。一般放在柜子内部，朝向柜门")]
        [SerializeField] private Transform _hidePoint;

        [Tooltip("玩家出来后所站的位置与朝向。一般放在柜子正前方一步远")]
        [SerializeField] private Transform _exitPoint;

        [Tooltip("躲进去时的提示文本")]
        [SerializeField] private string _enterPromptText = "[E] 躲进去";

        [Tooltip("出来时的提示文本")]
        [SerializeField] private string _exitPromptText = "[E] 出来";

        public string PromptText => _isOccupied ? _exitPromptText : _enterPromptText;
        public bool CanInteract => _hidePoint != null && _exitPoint != null;

        private GameObject _occupant;
        private bool _isOccupied;

        private void Awake()
        {
            if (_hidePoint == null)
            {
                Debug.LogError("HidingSpot 缺少 _hidePoint 引用，该藏匿点将不可交互。", this);
            }

            if (_exitPoint == null)
            {
                Debug.LogError("HidingSpot 缺少 _exitPoint 引用，该藏匿点将不可交互。", this);
            }
        }

        private void OnEnable()
        {
            GameEvents.OnRoundStart += HandleRoundStart;
            GameEvents.OnRoundEnd += HandleRoundEnd;
            GameEvents.OnPlayerCaught += HandlePlayerCaught;
        }

        private void OnDisable()
        {
            GameEvents.OnRoundStart -= HandleRoundStart;
            GameEvents.OnRoundEnd -= HandleRoundEnd;
            GameEvents.OnPlayerCaught -= HandlePlayerCaught;

            ForceExitOccupant();
        }

        private void OnDestroy()
        {
            // 防御性退订，避免异常的销毁顺序让静态事件留下失效委托。
            GameEvents.OnRoundStart -= HandleRoundStart;
            GameEvents.OnRoundEnd -= HandleRoundEnd;
            GameEvents.OnPlayerCaught -= HandlePlayerCaught;

            ForceExitOccupant();
            _occupant = null;
        }

        public void Interact(GameObject interactor)
        {
            if (!CanInteract)
            {
                return;
            }

            if (_isOccupied)
            {
                TryExitOccupant(interactor, false);
                return;
            }

            TryEnter(interactor);
        }

        private void TryEnter(GameObject interactor)
        {
            if (!TryMoveInteractor(interactor, _hidePoint))
            {
                return;
            }

            _occupant = interactor;
            _isOccupied = true;

            // PlayerController 会在收到事件时记录当前朝向，因此必须在完成传送后最后广播。
            GameEvents.RaiseHidingChanged(true);
        }

        private void TryExitOccupant(GameObject fallbackInteractor, bool forceRelease)
        {
            if (!_isOccupied)
            {
                return;
            }

            GameObject interactor = _occupant != null ? _occupant : fallbackInteractor;
            bool moved = TryMoveInteractor(interactor, _exitPoint);
            if (!moved && !forceRelease)
            {
                return;
            }

            _isOccupied = false;
            _occupant = null;

            // 退出也要在位置与朝向完成后才恢复玩家控制。
            GameEvents.RaiseHidingChanged(false);
        }

        private bool TryMoveInteractor(GameObject interactor, Transform targetPoint)
        {
            if (interactor == null)
            {
                Debug.LogWarning("HidingSpot 没有收到有效的交互玩家，已取消本次交互。", this);
                return false;
            }

            if (targetPoint == null)
            {
                Debug.LogWarning("HidingSpot 的目标点已失效，无法完成玩家传送。", this);
                return false;
            }

            CharacterController characterController =
                interactor.GetComponentInChildren<CharacterController>();
            if (characterController == null)
            {
                Debug.LogWarning(
                    "HidingSpot 无法在交互玩家或其子物体上找到 CharacterController，已取消本次交互。",
                    this);
                return false;
            }

            // CharacterController 启用时会覆盖直接写入的 Transform 位置。
            characterController.enabled = false;
            interactor.transform.position = targetPoint.position;
            interactor.transform.rotation = targetPoint.rotation;
            characterController.enabled = true;
            return true;
        }

        private void ForceExitOccupant()
        {
            TryExitOccupant(null, true);
        }

        private void HandleRoundStart()
        {
            ForceExitOccupant();
        }

        private void HandleRoundEnd(RoundResult result)
        {
            _ = result;
            ForceExitOccupant();
        }

        private void HandlePlayerCaught()
        {
            ForceExitOccupant();
        }
    }
}
