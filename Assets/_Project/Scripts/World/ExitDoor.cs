using Residuum.Core;
using UnityEngine;

namespace Residuum.World
{
    /// <summary>
    /// 撤离出口的交互入口。只发出撤离确认请求，不直接结束回合。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ExitDoor : MonoBehaviour, IInteractable
    {
        [Tooltip("平时的交互提示")]
        [SerializeField] private string _promptText = "[E] 撤离";

        [Tooltip("猎杀期间的提示，此时不能撤离")]
        [SerializeField] private string _huntingPromptText = "猎杀期间无法撤离";

        public string PromptText => _isHunting ? _huntingPromptText : _promptText;
        public bool CanInteract => !_hasDoorConflict && !_isHunting;

        private bool _isHunting;
        private bool _hasDoorConflict;

        private void Awake()
        {
            if (GetComponent<Door>() == null)
            {
                return;
            }

            _hasDoorConflict = true;
            Debug.LogError(
                "[ExitDoor] Door 与 ExitDoor 不能挂在同一个 GameObject 上，否则交互目标不确定。ExitDoor 已禁止交互。",
                this);
        }

        private void OnEnable()
        {
            GameEvents.OnHuntStart += HandleHuntStart;
            GameEvents.OnHuntEnd += HandleHuntEnd;
            GameEvents.OnRoundStart += HandleRoundStart;
        }

        private void OnDisable()
        {
            UnsubscribeFromEvents();
        }

        private void OnDestroy()
        {
            // 防御性退订，避免异常销毁顺序让静态事件保留失效委托。
            UnsubscribeFromEvents();
            _isHunting = false;
        }

        public void Interact(GameObject interactor)
        {
            GameEvents.RaiseEvacuatePromptRequested();
        }

        private void HandleHuntStart(float duration)
        {
            _isHunting = true;
        }

        private void HandleHuntEnd()
        {
            _isHunting = false;
        }

        private void HandleRoundStart()
        {
            _isHunting = false;
        }

        private void UnsubscribeFromEvents()
        {
            GameEvents.OnHuntStart -= HandleHuntStart;
            GameEvents.OnHuntEnd -= HandleHuntEnd;
            GameEvents.OnRoundStart -= HandleRoundStart;
        }
    }
}
