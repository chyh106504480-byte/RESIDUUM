using Residuum.Core;
using UnityEngine;

namespace Residuum.World
{
    [DisallowMultipleComponent]
    public sealed class PlayerInteractor : MonoBehaviour
    {
        private const string PlayerActionMapName = "Player";
        private const string InteractActionName = "Interact";

        [Header("输入依赖")]
        [Tooltip("工程现有的 InputSystem_Actions.inputactions。请勿创建新的 Input Action Asset。")]
        [SerializeField] private UnityEngine.InputSystem.InputActionAsset _inputActions;

        [Header("交互检测")]
        [Tooltip("正常状态下从相机中心发射射线的最大交互距离，单位：米。")]
        [SerializeField] private float _interactionDistance = 2.5f;

        [Tooltip("猎杀期间从相机中心发射射线的最大交互距离，单位：米。")]
        [SerializeField] private float _huntInteractionDistance = 1.5f;

        [Tooltip("两次准星目标检测之间的间隔，单位：秒。设为 0 将在每帧检测。")]
        [SerializeField] private float _detectionInterval = 0.1f;

        [Tooltip("可被准星射线检测到的物体层。")]
        [SerializeField] private LayerMask _interactionMask = Physics.DefaultRaycastLayers;

        private UnityEngine.InputSystem.InputAction _interactAction;
        private Collider _lastHitCollider;
        private IInteractable _cachedInteractable;
        private IInteractable _currentInteractable;
        private float _activeInteractionDistance;
        private float _detectionTimer;
        private bool _interactActionEnabledHere;
        private bool _hasBroadcastPrompt;
        private string _lastBroadcastPrompt;

        private void Awake()
        {
            ValidateSettings();
            _activeInteractionDistance = _interactionDistance;

            if (!TryInitializeInput())
            {
                enabled = false;
            }
        }

        private void OnEnable()
        {
            GameEvents.OnHuntStart += HandleHuntStart;
            GameEvents.OnHuntEnd += HandleHuntEnd;

            if (_interactAction != null)
            {
                _interactAction.performed += HandleInteractPerformed;
                EnableInteractActionIfNeeded();
            }
        }

        private void OnDisable()
        {
            GameEvents.OnHuntStart -= HandleHuntStart;
            GameEvents.OnHuntEnd -= HandleHuntEnd;

            if (_interactAction != null)
            {
                _interactAction.performed -= HandleInteractPerformed;
                DisableInteractActionIfOwned();
            }

            ClearCurrentTarget();
            _detectionTimer = 0f;
        }

        private void OnDestroy()
        {
            if (_interactAction != null)
            {
                _interactAction.performed -= HandleInteractPerformed;
            }

            _interactAction = null;
            _lastHitCollider = null;
            _cachedInteractable = null;
            _currentInteractable = null;
            _lastBroadcastPrompt = null;
            _hasBroadcastPrompt = false;
        }

        private void Update()
        {
            if (_detectionTimer > 0f)
            {
                _detectionTimer -= Time.deltaTime;
                return;
            }

            _detectionTimer = _detectionInterval;
            DetectInteractable();
        }

        private void DetectInteractable()
        {
            if (!Physics.Raycast(
                    transform.position,
                    transform.forward,
                    out RaycastHit hit,
                    _activeInteractionDistance,
                    _interactionMask,
                    QueryTriggerInteraction.Collide))
            {
                _lastHitCollider = null;
                _cachedInteractable = null;
                SetCurrentInteractable(null);
                return;
            }

            if (_lastHitCollider != hit.collider)
            {
                _lastHitCollider = hit.collider;
                _cachedInteractable = hit.collider != null
                    ? hit.collider.GetComponentInParent<IInteractable>()
                    : null;
            }

            IInteractable nextInteractable = IsInteractableAlive(_cachedInteractable)
                ? _cachedInteractable
                : null;

            SetCurrentInteractable(nextInteractable);
        }

        private void HandleInteractPerformed(UnityEngine.InputSystem.InputAction.CallbackContext context)
        {
            _ = context;

            if (!IsInteractableAlive(_currentInteractable) || !_currentInteractable.CanInteract)
            {
                return;
            }

            _currentInteractable.Interact(gameObject);
        }

        private void HandleHuntStart(float duration)
        {
            _ = duration;
            _activeInteractionDistance = _huntInteractionDistance;
            _detectionTimer = 0f;
        }

        private void HandleHuntEnd()
        {
            _activeInteractionDistance = _interactionDistance;
            _detectionTimer = 0f;
        }

        private bool TryInitializeInput()
        {
            if (_inputActions == null)
            {
                Debug.LogError("PlayerInteractor 未注入工程现有的 InputSystem_Actions InputActionAsset。", this);
                return false;
            }

            UnityEngine.InputSystem.InputActionMap playerMap =
                _inputActions.FindActionMap(PlayerActionMapName, false);
            if (playerMap == null)
            {
                Debug.LogError($"InputActionAsset 中找不到名为 {PlayerActionMapName} 的 Action Map。", this);
                return false;
            }

            _interactAction = playerMap.FindAction(InteractActionName, false);
            if (_interactAction == null)
            {
                Debug.LogError("Player Action Map 必须包含 Interact Action。", this);
                return false;
            }

            return true;
        }

        private void EnableInteractActionIfNeeded()
        {
            _interactActionEnabledHere = !_interactAction.enabled;
            if (_interactActionEnabledHere)
            {
                _interactAction.Enable();
            }
        }

        private void DisableInteractActionIfOwned()
        {
            if (_interactActionEnabledHere && _interactAction != null)
            {
                _interactAction.Disable();
            }

            _interactActionEnabledHere = false;
        }

        private void SetCurrentInteractable(IInteractable nextInteractable)
        {
            string nextPrompt = IsInteractableAlive(nextInteractable)
                ? nextInteractable.PromptText
                : null;
            bool targetChanged = !object.ReferenceEquals(_currentInteractable, nextInteractable);
            bool promptChanged = !_hasBroadcastPrompt || nextPrompt != _lastBroadcastPrompt;

            _currentInteractable = nextInteractable;
            if (!targetChanged && !promptChanged)
            {
                return;
            }

            _lastBroadcastPrompt = nextPrompt;
            _hasBroadcastPrompt = true;
            GameEvents.RaiseInteractPromptChanged(nextPrompt);
        }

        private void ClearCurrentTarget()
        {
            _lastHitCollider = null;
            _cachedInteractable = null;
            SetCurrentInteractable(null);
        }

        private void ValidateSettings()
        {
            if (_interactionDistance < 0f)
            {
                Debug.LogWarning("PlayerInteractor 的正常交互距离不能为负数，已按 0 处理。", this);
                _interactionDistance = 0f;
            }

            if (_huntInteractionDistance < 0f)
            {
                Debug.LogWarning("PlayerInteractor 的猎杀交互距离不能为负数，已按 0 处理。", this);
                _huntInteractionDistance = 0f;
            }

            if (_detectionInterval < 0f)
            {
                Debug.LogWarning("PlayerInteractor 的检测间隔不能为负数，已按 0 处理。", this);
                _detectionInterval = 0f;
            }
        }

        private static bool IsInteractableAlive(IInteractable interactable)
        {
            if (interactable == null)
            {
                return false;
            }

            return !(interactable is Object unityObject) || unityObject != null;
        }
    }
}
