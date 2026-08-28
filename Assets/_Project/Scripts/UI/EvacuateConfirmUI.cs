using Residuum.Core;
using UnityEngine;

namespace Residuum.UI
{
    /// <summary>
    /// 撤离前的二次确认界面。界面层级与按钮文字由 Inspector 搭建和注入。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class EvacuateConfirmUI : MonoBehaviour
    {
        [Header("界面引用")]
        [Tooltip("撤离确认面板根节点；开局隐藏，且不能是本组件所在对象或其祖先。")]
        [SerializeField] private GameObject _panelRoot;

        [Tooltip("显示撤离确认文案的文本。")]
        [SerializeField] private TMPro.TextMeshProUGUI _messageLabel;

        [Tooltip("确认撤离按钮。")]
        [SerializeField] private UnityEngine.UI.Button _confirmButton;

        [Tooltip("取消撤离按钮。")]
        [SerializeField] private UnityEngine.UI.Button _cancelButton;

        [Header("确认文案")]
        [Tooltip("撤离确认框中显示的提示文案。")]
        [SerializeField] private string _message = "确定要撤离吗？离开后本局结束。";

        [Header("确认事件")]
        [Tooltip("在 Inspector 里连到 GameManager.RequestEvacuate")]
        public UnityEngine.Events.UnityEvent onEvacuateConfirmed =
            new UnityEngine.Events.UnityEvent();

        private bool _isPromptOpen;
        private bool _buttonListenersAttached;

        private void Awake()
        {
            if (!ValidateReferences())
            {
                enabled = false;
                return;
            }

            _messageLabel.text = _message;
            _panelRoot.SetActive(false);
            _isPromptOpen = false;
        }

        private void OnEnable()
        {
            GameEvents.OnEvacuatePromptRequested += HandlePromptRequested;
            GameEvents.OnHuntStart += HandleHuntStart;
            GameEvents.OnRoundStart += HandleRoundStart;
            GameEvents.OnRoundEnd += HandleRoundEnd;

            AddButtonListeners();
        }

        private void OnDisable()
        {
            UnsubscribeFromEvents();
            RemoveButtonListeners();

            if (_isPromptOpen)
            {
                HidePrompt();
            }
        }

        private void OnDestroy()
        {
            // 防御性清理，避免异常销毁顺序让静态事件或按钮保留失效委托。
            UnsubscribeFromEvents();
            RemoveButtonListeners();

            if (_isPromptOpen)
            {
                HidePrompt();
            }
        }

        private void HandlePromptRequested()
        {
            if (_isPromptOpen)
            {
                return;
            }

            _messageLabel.text = _message;
            _panelRoot.SetActive(true);
            _isPromptOpen = true;
            GameEvents.RaiseLookSuspendedChanged(true);
        }

        private void HandleConfirmClicked()
        {
            HidePrompt();
            onEvacuateConfirmed?.Invoke();
        }

        private void HandleCancelClicked()
        {
            HidePrompt();
        }

        private void HandleHuntStart(float duration)
        {
            HidePrompt();
        }

        private void HandleRoundStart()
        {
            HidePrompt();
        }

        private void HandleRoundEnd(RoundResult result)
        {
            HidePrompt();
        }

        private void HidePrompt()
        {
            if (!_isPromptOpen)
            {
                return;
            }

            _isPromptOpen = false;

            if (_panelRoot != null)
            {
                _panelRoot.SetActive(false);
            }

            GameEvents.RaiseLookSuspendedChanged(false);
        }

        private void AddButtonListeners()
        {
            if (_buttonListenersAttached)
            {
                return;
            }

            _confirmButton.onClick.AddListener(HandleConfirmClicked);
            _cancelButton.onClick.AddListener(HandleCancelClicked);
            _buttonListenersAttached = true;
        }

        private void RemoveButtonListeners()
        {
            if (!_buttonListenersAttached)
            {
                return;
            }

            if (_confirmButton != null)
            {
                _confirmButton.onClick.RemoveListener(HandleConfirmClicked);
            }

            if (_cancelButton != null)
            {
                _cancelButton.onClick.RemoveListener(HandleCancelClicked);
            }

            _buttonListenersAttached = false;
        }

        private void UnsubscribeFromEvents()
        {
            GameEvents.OnEvacuatePromptRequested -= HandlePromptRequested;
            GameEvents.OnHuntStart -= HandleHuntStart;
            GameEvents.OnRoundStart -= HandleRoundStart;
            GameEvents.OnRoundEnd -= HandleRoundEnd;
        }

        private bool ValidateReferences()
        {
            if (_panelRoot == null)
            {
                return DisableWithError(
                    "[EvacuateConfirmUI] _panelRoot 未注入。组件已禁用。");
            }

            if (transform.IsChildOf(_panelRoot.transform))
            {
                return DisableWithError(
                    "[EvacuateConfirmUI] _panelRoot 不能是本组件所在的 GameObject 或其祖先，否则隐藏弹窗后组件无法继续接收事件。组件已禁用。");
            }

            if (_messageLabel == null)
            {
                return DisableWithError(
                    "[EvacuateConfirmUI] _messageLabel 未注入。组件已禁用。");
            }

            if (_confirmButton == null)
            {
                return DisableWithError(
                    "[EvacuateConfirmUI] _confirmButton 未注入。组件已禁用。");
            }

            if (_cancelButton == null)
            {
                return DisableWithError(
                    "[EvacuateConfirmUI] _cancelButton 未注入。组件已禁用。");
            }

            return true;
        }

        private bool DisableWithError(string message)
        {
            Debug.LogError(message, this);
            return false;
        }
    }
}
