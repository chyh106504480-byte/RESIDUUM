using Residuum.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Residuum.Items
{
    [DisallowMultipleComponent]
    public sealed class ItemSlotSystem : MonoBehaviour
    {
        private const int SlotCount = 3;
        private const int FirstSlotIndex = 0;
        private const int SecondSlotIndex = 1;
        private const int ThirdSlotIndex = 2;
        private const string PlayerActionMapName = "Player";
        private const string UiActionMapName = "UI";
        private const string PreviousActionName = "Previous";
        private const string NextActionName = "Next";
        private const string AttackActionName = "Attack";
        private const string ScrollWheelActionName = "ScrollWheel";
        private const string FirstSlotBindingPath = "<Keyboard>/1";
        private const string SecondSlotBindingPath = "<Keyboard>/2";
        private const string ThirdSlotBindingPath = "<Keyboard>/3";
        private const string PrimaryUseBindingPath = "<Mouse>/leftButton";
        private const string ScrollBindingPath = "<Mouse>/scroll";

        [Header("依赖")]
        [Tooltip("三个道具槽中的控制组件，长度必须为 3；组件需实现 IHoldable。")]
        [SerializeField] private MonoBehaviour[] _slots = new MonoBehaviour[SlotCount];

        [Tooltip("三个槽位各自的手持模型，长度必须为 3。模型会挂到手持模型挂点，并用 SetActive 切换显隐；请勿把常驻道具控制组件放在这些模型上。")]
        [SerializeField] private GameObject[] _heldModels = new GameObject[SlotCount];

        [Tooltip("玩家相机下的手持模型挂点。")]
        [SerializeField] private Transform _handAnchor;

        [Tooltip("工程现有的 Assets/InputSystem_Actions.inputactions。脚本只创建运行时 Action 副本，不修改输入资产。")]
        [SerializeField] private InputActionAsset _inputActions;

        [Header("切换")]
        [Tooltip("鼠标滚轮输入超过此绝对值时才切换槽位，用于过滤设备噪声。")]
        [SerializeField] private float _scrollInputThreshold = 0.01f;

        public IHoldable Current { get; private set; }

        private readonly IHoldable[] _holdables = new IHoldable[SlotCount];

        private InputAction _slotOneAction;
        private InputAction _slotTwoAction;
        private InputAction _slotThreeAction;
        private InputAction _scrollAction;
        private InputAction _primaryUseAction;
        private int _currentSlotIndex = -1;
        private bool _isInitialized;

        private void Awake()
        {
            ValidateSettings();

            if (_handAnchor == null)
            {
                Debug.LogError("ItemSlotSystem 未指定手持模型挂点 Hand Anchor，组件已禁用。", this);
                enabled = false;
                return;
            }

            CacheSlots();
            PrepareHeldModels();

            if (!TryInitializeInput())
            {
                enabled = false;
                return;
            }

            _isInitialized = true;
        }

        private void OnEnable()
        {
            if (!_isInitialized)
            {
                return;
            }

            SubscribeInputActions();
            EnableInputActions();
            SwitchToSlot(FirstSlotIndex);
        }

        private void OnDisable()
        {
            UnsubscribeInputActions();
            DisableInputActions();

            if (Current != null)
            {
                Current.OnUnequip();
            }

            SetHeldModelActive(_currentSlotIndex, false);
            Current = null;
            _currentSlotIndex = -1;
        }

        private void OnDestroy()
        {
            UnsubscribeInputActions();
            DisposeInputActions();
            Current = null;
        }

        private void CacheSlots()
        {
            for (int index = 0; index < SlotCount; index++)
            {
                MonoBehaviour slot = _slots != null && index < _slots.Length
                    ? _slots[index]
                    : null;

                if (slot == null)
                {
                    _holdables[index] = null;
                    continue;
                }

                _holdables[index] = slot as IHoldable;
                if (_holdables[index] == null)
                {
                    Debug.LogError(
                        $"ItemSlotSystem 第 {index + 1} 槽的组件 {slot.GetType().Name} 未实现 IHoldable，该槽将按空槽处理。",
                        slot);
                }
            }
        }

        private void PrepareHeldModels()
        {
            for (int index = 0; index < SlotCount; index++)
            {
                GameObject heldModel = GetHeldModel(index);
                if (heldModel == null)
                {
                    if (_holdables[index] != null)
                    {
                        Debug.LogError($"ItemSlotSystem 第 {index + 1} 槽已有道具，但未指定手持模型。", this);
                    }

                    continue;
                }

                if (heldModel.transform == _handAnchor)
                {
                    Debug.LogError($"ItemSlotSystem 第 {index + 1} 槽不能把 Hand Anchor 自身作为手持模型。", this);
                    continue;
                }

                if (heldModel.transform.parent != _handAnchor)
                {
                    heldModel.transform.SetParent(_handAnchor, false);
                }

                heldModel.SetActive(false);
            }
        }

        private bool TryInitializeInput()
        {
            if (_inputActions == null)
            {
                Debug.LogError("ItemSlotSystem 未注入工程现有的 InputSystem_Actions InputActionAsset。", this);
                return false;
            }

            InputActionMap playerMap = _inputActions.FindActionMap(PlayerActionMapName, false);
            InputActionMap uiMap = _inputActions.FindActionMap(UiActionMapName, false);
            if (playerMap == null || uiMap == null)
            {
                Debug.LogError("InputActionAsset 必须包含 Player 与 UI 两个 Action Map。", this);
                return false;
            }

            InputAction previousTemplate = playerMap.FindAction(PreviousActionName, false);
            InputAction nextTemplate = playerMap.FindAction(NextActionName, false);
            InputAction attackTemplate = playerMap.FindAction(AttackActionName, false);
            InputAction scrollTemplate = uiMap.FindAction(ScrollWheelActionName, false);
            if (previousTemplate == null || nextTemplate == null || attackTemplate == null || scrollTemplate == null)
            {
                Debug.LogError("InputActionAsset 必须包含 Player/Previous、Player/Next、Player/Attack 与 UI/ScrollWheel Action。", this);
                return false;
            }

            _slotOneAction = CloneWithSingleBinding(previousTemplate, FirstSlotBindingPath);
            _slotTwoAction = CloneWithSingleBinding(nextTemplate, SecondSlotBindingPath);
            _slotThreeAction = CloneWithSingleBinding(previousTemplate, ThirdSlotBindingPath);
            _scrollAction = CloneWithSingleBinding(scrollTemplate, ScrollBindingPath);
            _primaryUseAction = CloneWithSingleBinding(attackTemplate, PrimaryUseBindingPath);

            if (_slotOneAction == null || _slotTwoAction == null || _slotThreeAction == null
                || _scrollAction == null || _primaryUseAction == null)
            {
                Debug.LogError("ItemSlotSystem 无法从现有输入资产建立运行时 Action。", this);
                DisposeInputActions();
                return false;
            }

            return true;
        }

        private static InputAction CloneWithSingleBinding(InputAction template, string bindingPath)
        {
            if (template == null || template.bindings.Count == 0)
            {
                return null;
            }

            InputAction action = template.Clone();
            for (int bindingIndex = 0; bindingIndex < action.bindings.Count; bindingIndex++)
            {
                action.ApplyBindingOverride(bindingIndex, string.Empty);
            }

            action.ApplyBindingOverride(0, bindingPath);
            return action;
        }

        private void SubscribeInputActions()
        {
            _slotOneAction.performed += HandleSlotOnePerformed;
            _slotTwoAction.performed += HandleSlotTwoPerformed;
            _slotThreeAction.performed += HandleSlotThreePerformed;
            _scrollAction.performed += HandleScrollPerformed;
            _primaryUseAction.performed += HandlePrimaryUsePerformed;
        }

        private void UnsubscribeInputActions()
        {
            if (_slotOneAction != null)
            {
                _slotOneAction.performed -= HandleSlotOnePerformed;
            }

            if (_slotTwoAction != null)
            {
                _slotTwoAction.performed -= HandleSlotTwoPerformed;
            }

            if (_slotThreeAction != null)
            {
                _slotThreeAction.performed -= HandleSlotThreePerformed;
            }

            if (_scrollAction != null)
            {
                _scrollAction.performed -= HandleScrollPerformed;
            }

            if (_primaryUseAction != null)
            {
                _primaryUseAction.performed -= HandlePrimaryUsePerformed;
            }
        }

        private void EnableInputActions()
        {
            _slotOneAction.Enable();
            _slotTwoAction.Enable();
            _slotThreeAction.Enable();
            _scrollAction.Enable();
            _primaryUseAction.Enable();
        }

        private void DisableInputActions()
        {
            _slotOneAction?.Disable();
            _slotTwoAction?.Disable();
            _slotThreeAction?.Disable();
            _scrollAction?.Disable();
            _primaryUseAction?.Disable();
        }

        private void DisposeInputActions()
        {
            _slotOneAction?.Dispose();
            _slotTwoAction?.Dispose();
            _slotThreeAction?.Dispose();
            _scrollAction?.Dispose();
            _primaryUseAction?.Dispose();

            _slotOneAction = null;
            _slotTwoAction = null;
            _slotThreeAction = null;
            _scrollAction = null;
            _primaryUseAction = null;
        }

        private void HandleSlotOnePerformed(InputAction.CallbackContext context)
        {
            _ = context;
            SwitchToSlot(FirstSlotIndex);
        }

        private void HandleSlotTwoPerformed(InputAction.CallbackContext context)
        {
            _ = context;
            SwitchToSlot(SecondSlotIndex);
        }

        private void HandleSlotThreePerformed(InputAction.CallbackContext context)
        {
            _ = context;
            SwitchToSlot(ThirdSlotIndex);
        }

        private void HandleScrollPerformed(InputAction.CallbackContext context)
        {
            float scrollY = context.ReadValue<Vector2>().y;
            if (Mathf.Abs(scrollY) <= _scrollInputThreshold)
            {
                return;
            }

            int direction = scrollY > 0f ? -1 : 1;
            int startIndex = _currentSlotIndex >= FirstSlotIndex ? _currentSlotIndex : FirstSlotIndex;
            int nextIndex = (startIndex + direction + SlotCount) % SlotCount;
            SwitchToSlot(nextIndex);
        }

        private void HandlePrimaryUsePerformed(InputAction.CallbackContext context)
        {
            _ = context;
            Current?.OnPrimaryUse();
        }

        private void SwitchToSlot(int slotIndex)
        {
            if (slotIndex < FirstSlotIndex || slotIndex >= SlotCount || slotIndex == _currentSlotIndex)
            {
                return;
            }

            if (Current != null)
            {
                Current.OnUnequip();
            }

            SetHeldModelActive(_currentSlotIndex, false);

            _currentSlotIndex = slotIndex;
            Current = _holdables[slotIndex];
            SetHeldModelActive(_currentSlotIndex, Current != null);

            if (Current != null)
            {
                Current.OnEquip();
            }

            GameEvents.RaiseSlotChanged(_currentSlotIndex, Current?.ItemName);
        }

        private void SetHeldModelActive(int slotIndex, bool isActive)
        {
            GameObject heldModel = GetHeldModel(slotIndex);
            if (heldModel != null && heldModel.transform != _handAnchor)
            {
                heldModel.SetActive(isActive);
            }
        }

        private GameObject GetHeldModel(int slotIndex)
        {
            if (_heldModels == null || slotIndex < FirstSlotIndex || slotIndex >= _heldModels.Length)
            {
                return null;
            }

            return _heldModels[slotIndex];
        }

        private void ValidateSettings()
        {
            if (_slots == null || _slots.Length != SlotCount)
            {
                Debug.LogError($"ItemSlotSystem 的 Slots 长度必须为 {SlotCount}；缺失位置将按空槽处理。", this);
            }

            if (_heldModels == null || _heldModels.Length != SlotCount)
            {
                Debug.LogError($"ItemSlotSystem 的 Held Models 长度必须为 {SlotCount}；缺失位置将不显示模型。", this);
            }

            if (_scrollInputThreshold < 0f)
            {
                Debug.LogWarning("ItemSlotSystem 的滚轮输入阈值不能为负数，已按 0 处理。", this);
                _scrollInputThreshold = 0f;
            }
        }
    }
}
