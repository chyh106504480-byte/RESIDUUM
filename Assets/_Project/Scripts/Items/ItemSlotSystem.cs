using Residuum.Core;
using UnityEngine;

namespace Residuum.Items
{
    [DisallowMultipleComponent]
    public sealed class ItemSlotSystem : MonoBehaviour
    {
        private const int SlotCount = 3;
        private const int FirstSlotIndex = 0;
        private const int SecondSlotIndex = 1;
        private const int ThirdSlotIndex = 2;
        private const int NoSlotIndex = -1;
        private const string PlayerActionMapName = "Player";
        private const string UiActionMapName = "UI";
        private const string SlotOneActionName = "Slot1";
        private const string SlotTwoActionName = "Slot2";
        private const string SlotThreeActionName = "Slot3";
        private const string AttackActionName = "Attack";
        private const string ScrollWheelActionName = "ScrollWheel";

        [Header("依赖")]
        [Tooltip("三个道具槽中的控制组件，长度必须为 3；组件需实现 IHoldable。")]
        [SerializeField] private MonoBehaviour[] _slots = new MonoBehaviour[SlotCount];

        [Tooltip("三个槽位各自的手持模型，长度必须为 3。模型会挂到手持模型挂点，并用 SetActive 切换显隐；请勿把常驻道具控制组件放在这些模型上。")]
        [SerializeField] private GameObject[] _heldModels = new GameObject[SlotCount];

        [Tooltip("玩家相机下的手持模型挂点。")]
        [SerializeField] private Transform _handAnchor;

        [Tooltip("工程现有的 Assets/InputSystem_Actions.inputactions。脚本直接使用其中定义的 Action。")]
        [SerializeField] private UnityEngine.InputSystem.InputActionAsset _inputActions;

        [Header("世界道具")]
        [Tooltip("三个编号槽各自在世界中的道具对象，长度必须为 3。拾取时隐藏，丢弃时移到玩家前方再显示。")]
        [SerializeField] private GameObject[] _worldItems = new GameObject[SlotCount];

        [Header("手电筒")]
        [Tooltip("手电筒的 IHoldable 控制组件，独立于三个编号槽")]
        [SerializeField] private MonoBehaviour _flashlightSlot;

        [Tooltip("手电筒的手持模型")]
        [SerializeField] private GameObject _flashlightModel;

        [Tooltip("回合开始时是否自动把手电拿在手上")]
        [SerializeField] private bool _equipFlashlightOnRoundStart = true;

        [Tooltip("装备/收起手电筒的按键")]
        [SerializeField] private UnityEngine.InputSystem.Key _flashlightKey
            = UnityEngine.InputSystem.Key.T;

        [Header("丢弃")]
        [Tooltip("丢弃当前手持道具的按键")]
        [SerializeField] private UnityEngine.InputSystem.Key _dropKey
            = UnityEngine.InputSystem.Key.G;

        [Tooltip("丢弃时道具出现在玩家前方多远，单位：米")]
        [Min(0.3f)]
        [SerializeField] private float _dropDistance = 1.2f;

        [Tooltip("丢弃时道具出现的高度偏移，单位：米")]
        [SerializeField] private float _dropHeightOffset = -0.3f;

        [Tooltip("丢弃时给道具的初速度倍率。0 表示原地松手，正数表示往前抛")]
        [Min(0f)]
        [SerializeField] private float _dropForwardSpeed = 2f;

        [Header("切换")]
        [Tooltip("鼠标滚轮输入超过此绝对值时才切换槽位，用于过滤设备噪声。")]
        [SerializeField] private float _scrollInputThreshold = 0.01f;

        public IHoldable Current { get; private set; }

        private readonly IHoldable[] _holdables = new IHoldable[SlotCount];
        private readonly bool[] _hasSlotItem = new bool[SlotCount];
        private readonly Vector3[] _worldItemInitialPositions = new Vector3[SlotCount];
        private readonly Quaternion[] _worldItemInitialRotations = new Quaternion[SlotCount];
        private readonly bool[] _hasWorldItemInitialTransform = new bool[SlotCount];

        private IHoldable _flashlightHoldable;
        private UnityEngine.InputSystem.InputAction _slotOneAction;
        private UnityEngine.InputSystem.InputAction _slotTwoAction;
        private UnityEngine.InputSystem.InputAction _slotThreeAction;
        private UnityEngine.InputSystem.InputAction _scrollAction;
        private UnityEngine.InputSystem.InputAction _primaryUseAction;
        private int _currentSlotIndex = NoSlotIndex;
        private int _lastNumberedSlotIndex = NoSlotIndex;
        private bool _hasFlashlight;
        private bool _isFlashlightEquipped;
        private bool _isInitialized;
        private bool _slotBroadcastPending;
        private bool _slotOneActionEnabledHere;
        private bool _slotTwoActionEnabledHere;
        private bool _slotThreeActionEnabledHere;
        private bool _scrollActionEnabledHere;
        private bool _primaryUseActionEnabledHere;

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
            CacheWorldItemInitialTransforms();
            ResetInventory();

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
            GameEvents.OnRoundStart += HandleRoundStart;
            _slotBroadcastPending = true;
        }

        private void OnDisable()
        {
            GameEvents.OnRoundStart -= HandleRoundStart;
            UnsubscribeInputActions();
            DisableInputActions();
            UnequipCurrent();
        }

        private void OnDestroy()
        {
            GameEvents.OnRoundStart -= HandleRoundStart;
            UnsubscribeInputActions();
            DisableInputActions();

            _slotOneAction = null;
            _slotTwoAction = null;
            _slotThreeAction = null;
            _scrollAction = null;
            _primaryUseAction = null;
            _flashlightHoldable = null;
            Current = null;
        }

        private void Update()
        {
            UnityEngine.InputSystem.Keyboard keyboard = UnityEngine.InputSystem.Keyboard.current;
            if (keyboard == null)
            {
                return;
            }

            if (WasKeyPressed(keyboard, _flashlightKey))
            {
                ToggleFlashlight();
            }

            if (WasKeyPressed(keyboard, _dropKey))
            {
                DropCurrent();
            }
        }

        private void LateUpdate()
        {
            if (!_slotBroadcastPending)
            {
                return;
            }

            _slotBroadcastPending = false;
            GameEvents.RaiseSlotChanged(_currentSlotIndex, Current?.ItemName);
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

            if (_flashlightSlot == null)
            {
                _flashlightHoldable = null;
                return;
            }

            _flashlightHoldable = _flashlightSlot as IHoldable;
            if (_flashlightHoldable == null)
            {
                Debug.LogError(
                    $"ItemSlotSystem 的手电筒组件 {_flashlightSlot.GetType().Name} 未实现 IHoldable，手电筒将无法拾取。",
                    _flashlightSlot);
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
                        Debug.LogWarning($"ItemSlotSystem 第 {index + 1} 槽已有道具但未指定手持模型：这是灰盒阶段的预期状态，待美术资源到位后再配置即可。", this);
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

            PrepareHeldModel(_flashlightModel, "手电筒");
        }

        private void PrepareHeldModel(GameObject heldModel, string itemDescription)
        {
            if (heldModel == null)
            {
                Debug.LogWarning($"ItemSlotSystem 未指定{itemDescription}手持模型：这是灰盒阶段的预期状态，待美术资源到位后再配置即可。", this);
                return;
            }

            if (heldModel.transform == _handAnchor)
            {
                Debug.LogError($"ItemSlotSystem 不能把 Hand Anchor 自身作为{itemDescription}手持模型。", this);
                return;
            }

            if (heldModel.transform.parent != _handAnchor)
            {
                heldModel.transform.SetParent(_handAnchor, false);
            }

            heldModel.SetActive(false);
        }

        private void CacheWorldItemInitialTransforms()
        {
            for (int index = 0; index < SlotCount; index++)
            {
                GameObject worldItem = GetWorldItem(index);
                if (worldItem == null)
                {
                    continue;
                }

                Transform worldItemTransform = worldItem.transform;
                _worldItemInitialPositions[index] = worldItemTransform.position;
                _worldItemInitialRotations[index] = worldItemTransform.rotation;
                _hasWorldItemInitialTransform[index] = true;
            }

        }

        private bool TryInitializeInput()
        {
            if (_inputActions == null)
            {
                Debug.LogError("ItemSlotSystem 未注入工程现有的 InputSystem_Actions InputActionAsset。", this);
                return false;
            }

            _slotOneAction = _inputActions.FindAction($"{PlayerActionMapName}/{SlotOneActionName}", false);
            _slotTwoAction = _inputActions.FindAction($"{PlayerActionMapName}/{SlotTwoActionName}", false);
            _slotThreeAction = _inputActions.FindAction($"{PlayerActionMapName}/{SlotThreeActionName}", false);
            _primaryUseAction = _inputActions.FindAction($"{PlayerActionMapName}/{AttackActionName}", false);
            _scrollAction = _inputActions.FindAction($"{UiActionMapName}/{ScrollWheelActionName}", false);

            bool hasSlotOneAction = ValidateInputAction(_slotOneAction, $"{PlayerActionMapName}/{SlotOneActionName}");
            bool hasSlotTwoAction = ValidateInputAction(_slotTwoAction, $"{PlayerActionMapName}/{SlotTwoActionName}");
            bool hasSlotThreeAction = ValidateInputAction(_slotThreeAction, $"{PlayerActionMapName}/{SlotThreeActionName}");
            bool hasPrimaryUseAction = ValidateInputAction(_primaryUseAction, $"{PlayerActionMapName}/{AttackActionName}");
            bool hasScrollAction = ValidateInputAction(_scrollAction, $"{UiActionMapName}/{ScrollWheelActionName}");

            return hasSlotOneAction && hasSlotTwoAction && hasSlotThreeAction
                && hasPrimaryUseAction && hasScrollAction;
        }

        private bool ValidateInputAction(UnityEngine.InputSystem.InputAction action, string actionPath)
        {
            if (action != null)
            {
                return true;
            }

            Debug.LogError($"ItemSlotSystem 在 InputActionAsset 中找不到 {actionPath} Action。", this);
            return false;
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
            EnableInputActionIfNeeded(_slotOneAction, ref _slotOneActionEnabledHere);
            EnableInputActionIfNeeded(_slotTwoAction, ref _slotTwoActionEnabledHere);
            EnableInputActionIfNeeded(_slotThreeAction, ref _slotThreeActionEnabledHere);
            EnableInputActionIfNeeded(_scrollAction, ref _scrollActionEnabledHere);
            EnableInputActionIfNeeded(_primaryUseAction, ref _primaryUseActionEnabledHere);
        }

        private void DisableInputActions()
        {
            DisableInputActionIfOwned(_slotOneAction, ref _slotOneActionEnabledHere);
            DisableInputActionIfOwned(_slotTwoAction, ref _slotTwoActionEnabledHere);
            DisableInputActionIfOwned(_slotThreeAction, ref _slotThreeActionEnabledHere);
            DisableInputActionIfOwned(_scrollAction, ref _scrollActionEnabledHere);
            DisableInputActionIfOwned(_primaryUseAction, ref _primaryUseActionEnabledHere);
        }

        private static void EnableInputActionIfNeeded(
            UnityEngine.InputSystem.InputAction action,
            ref bool actionEnabledHere)
        {
            actionEnabledHere = action != null && !action.enabled;
            if (actionEnabledHere)
            {
                action.Enable();
            }
        }

        private static void DisableInputActionIfOwned(
            UnityEngine.InputSystem.InputAction action,
            ref bool actionEnabledHere)
        {
            if (actionEnabledHere && action != null)
            {
                action.Disable();
            }

            actionEnabledHere = false;
        }

        private void HandleSlotOnePerformed(UnityEngine.InputSystem.InputAction.CallbackContext context)
        {
            _ = context;
            SwitchToSlot(FirstSlotIndex);
        }

        private void HandleSlotTwoPerformed(UnityEngine.InputSystem.InputAction.CallbackContext context)
        {
            _ = context;
            SwitchToSlot(SecondSlotIndex);
        }

        private void HandleSlotThreePerformed(UnityEngine.InputSystem.InputAction.CallbackContext context)
        {
            _ = context;
            SwitchToSlot(ThirdSlotIndex);
        }

        private void HandleScrollPerformed(UnityEngine.InputSystem.InputAction.CallbackContext context)
        {
            float scrollY = context.ReadValue<Vector2>().y;
            if (Mathf.Abs(scrollY) <= _scrollInputThreshold)
            {
                return;
            }

            int direction = scrollY > 0f ? -1 : 1;
            int startIndex = _currentSlotIndex >= FirstSlotIndex ? _currentSlotIndex : FirstSlotIndex;
            int nextIndex = FindNextHeldSlot(startIndex, direction);
            if (nextIndex != NoSlotIndex)
            {
                SwitchToSlot(nextIndex);
            }
        }

        private void HandlePrimaryUsePerformed(UnityEngine.InputSystem.InputAction.CallbackContext context)
        {
            _ = context;
            Current?.OnPrimaryUse();
        }

        private void SwitchToSlot(int slotIndex)
        {
            if (!CanEquipSlot(slotIndex) || (!_isFlashlightEquipped && slotIndex == _currentSlotIndex))
            {
                return;
            }

            UnequipCurrent();
            _currentSlotIndex = slotIndex;
            Current = _holdables[slotIndex];
            _lastNumberedSlotIndex = slotIndex;
            SetHeldModelActive(_currentSlotIndex, true);
            Current.OnEquip();

            GameEvents.RaiseSlotChanged(_currentSlotIndex, Current?.ItemName);
        }

        /// <summary>拾取第 index 个编号槽的道具。已持有或索引越界返回 false。</summary>
        public bool TryPickUpSlot(int index)
        {
            if (index < FirstSlotIndex || index >= SlotCount || _hasSlotItem[index])
            {
                return false;
            }

            if (_holdables[index] == null)
            {
                Debug.LogError($"ItemSlotSystem 第 {index + 1} 槽没有有效的 IHoldable，无法拾取。", this);
                return false;
            }

            GameObject worldItem = GetWorldItem(index);
            if (worldItem == null)
            {
                Debug.LogError($"ItemSlotSystem 第 {index + 1} 槽未指定世界道具对象，无法拾取。", this);
                return false;
            }

            FreezeWorldItemPhysics(worldItem);
            worldItem.SetActive(false);
            _hasSlotItem[index] = true;
            SwitchToSlot(index);
            return true;
        }

        /// <summary>兼容场景中遗留的手电筒 WorldItem；手电筒为开局基础装备。</summary>
        public bool TryPickUpFlashlight()
        {
            return false;
        }

        private void ToggleFlashlight()
        {
            if (!_hasFlashlight || _flashlightHoldable == null)
            {
                return;
            }

            if (!_isFlashlightEquipped)
            {
                EquipFlashlight();
                return;
            }

            UnequipCurrent();
            if (CanEquipSlot(_lastNumberedSlotIndex))
            {
                SwitchToSlot(_lastNumberedSlotIndex);
                return;
            }

            BroadcastEmptySlot();
        }

        private void EquipFlashlight()
        {
            if (!_hasFlashlight || _flashlightHoldable == null || _isFlashlightEquipped)
            {
                return;
            }

            UnequipCurrent();
            Current = _flashlightHoldable;
            _isFlashlightEquipped = true;
            SetFlashlightModelActive(true);
            Current.OnEquip();
            GameEvents.RaiseSlotChanged(NoSlotIndex, Current.ItemName);
        }

        private void DropCurrent()
        {
            if (Current == null)
            {
                return;
            }

            if (_isFlashlightEquipped)
            {
                DropFlashlight();
                return;
            }

            DropNumberedSlot(_currentSlotIndex);
        }

        private void DropNumberedSlot(int slotIndex)
        {
            if (!CanEquipSlot(slotIndex))
            {
                return;
            }

            GameObject worldItem = GetWorldItem(slotIndex);
            if (worldItem == null)
            {
                Debug.LogError($"ItemSlotSystem 第 {slotIndex + 1} 槽未指定世界道具对象，无法丢弃。", this);
                return;
            }

            UnequipCurrent();
            _hasSlotItem[slotIndex] = false;
            PlaceWorldItemForDrop(worldItem);
            GameEvents.RaiseSlotChanged(slotIndex, null);
        }

        private void DropFlashlight()
        {
        }

        private void PlaceWorldItemForDrop(GameObject worldItem)
        {
            Transform worldItemTransform = worldItem.transform;
            worldItemTransform.position = transform.position
                + transform.forward * _dropDistance
                + Vector3.up * _dropHeightOffset;
            worldItem.SetActive(true);

            Rigidbody worldItemRigidbody = worldItem.GetComponent<Rigidbody>();
            if (worldItemRigidbody != null)
            {
                worldItemRigidbody.isKinematic = false;
                worldItemRigidbody.linearVelocity = transform.forward * _dropForwardSpeed;
            }
        }

        private void HandleRoundStart()
        {
            ResetInventory();

            if (_equipFlashlightOnRoundStart)
            {
                EquipFlashlight();
            }
        }

        private void ResetInventory()
        {
            UnequipCurrent();

            for (int index = 0; index < SlotCount; index++)
            {
                _hasSlotItem[index] = false;
                RestoreWorldItem(index);
            }

            _hasFlashlight = _flashlightHoldable != null;
            _lastNumberedSlotIndex = NoSlotIndex;
            _slotBroadcastPending = true;
        }

        private void RestoreWorldItem(int slotIndex)
        {
            GameObject worldItem = GetWorldItem(slotIndex);
            if (worldItem == null)
            {
                return;
            }

            FreezeWorldItemPhysics(worldItem);

            if (_hasWorldItemInitialTransform[slotIndex])
            {
                worldItem.transform.SetPositionAndRotation(
                    _worldItemInitialPositions[slotIndex],
                    _worldItemInitialRotations[slotIndex]);
            }

            worldItem.SetActive(true);
        }

        private int FindNextHeldSlot(int startIndex, int direction)
        {
            for (int offset = 1; offset <= SlotCount; offset++)
            {
                int candidateIndex = (startIndex + direction * offset + SlotCount) % SlotCount;
                if (CanEquipSlot(candidateIndex))
                {
                    return candidateIndex;
                }
            }

            return NoSlotIndex;
        }

        private bool CanEquipSlot(int slotIndex)
        {
            return slotIndex >= FirstSlotIndex
                && slotIndex < SlotCount
                && _hasSlotItem[slotIndex]
                && _holdables[slotIndex] != null;
        }

        private void UnequipCurrent()
        {
            if (Current != null)
            {
                Current.OnUnequip();
            }

            if (_isFlashlightEquipped)
            {
                SetFlashlightModelActive(false);
            }
            else
            {
                SetHeldModelActive(_currentSlotIndex, false);
            }

            Current = null;
            _currentSlotIndex = NoSlotIndex;
            _isFlashlightEquipped = false;
        }

        private void BroadcastEmptySlot()
        {
            GameEvents.RaiseSlotChanged(NoSlotIndex, null);
        }

        private static bool WasKeyPressed(
            UnityEngine.InputSystem.Keyboard keyboard,
            UnityEngine.InputSystem.Key key)
        {
            return key != UnityEngine.InputSystem.Key.None
                && keyboard[key].wasPressedThisFrame;
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

        private void SetFlashlightModelActive(bool isActive)
        {
            if (_flashlightModel != null && _flashlightModel.transform != _handAnchor)
            {
                _flashlightModel.SetActive(isActive);
            }
        }

        private GameObject GetWorldItem(int slotIndex)
        {
            if (_worldItems == null || slotIndex < FirstSlotIndex || slotIndex >= _worldItems.Length)
            {
                return null;
            }

            return _worldItems[slotIndex];
        }

        private static void FreezeWorldItemPhysics(GameObject worldItem)
        {
            Rigidbody worldItemRigidbody = worldItem.GetComponent<Rigidbody>();
            if (worldItemRigidbody == null)
            {
                return;
            }

            worldItemRigidbody.isKinematic = true;
            worldItemRigidbody.linearVelocity = Vector3.zero;
            worldItemRigidbody.angularVelocity = Vector3.zero;
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

            if (_worldItems == null || _worldItems.Length != SlotCount)
            {
                Debug.LogError($"ItemSlotSystem 的 World Items 长度必须为 {SlotCount}；缺失位置将无法拾取或丢弃。", this);
            }

            if (_scrollInputThreshold < 0f)
            {
                Debug.LogWarning("ItemSlotSystem 的滚轮输入阈值不能为负数，已按 0 处理。", this);
                _scrollInputThreshold = 0f;
            }

            if (_dropForwardSpeed < 0f)
            {
                Debug.LogWarning("ItemSlotSystem 的丢弃初速度倍率不能为负数，已按 0 处理。", this);
                _dropForwardSpeed = 0f;
            }
        }
    }
}
