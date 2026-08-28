using System;
using Residuum.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Residuum.Player
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CharacterController))]
    public sealed class PlayerController : MonoBehaviour
    {
        private const string PlayerActionMapName = "Player";
        private const string MoveActionName = "Move";
        private const string LookActionName = "Look";
        private const string SprintActionName = "Sprint";
        private const string CrouchActionName = "Crouch";

        [Header("依赖")]
        [Tooltip("工程现有的 Assets/InputSystem_Actions.inputactions。请勿创建新的 Input Action Asset。")]
        [SerializeField] private InputActionAsset _inputActions;

        [Tooltip("玩家头部的相机枢轴子物体。脚本直接控制该 Transform 的位置与俯仰角。")]
        [SerializeField] private Transform _cameraPivot;

        [Header("移动")]
        [Tooltip("正常行走速度，单位：米/秒。")]
        [SerializeField] private float _walkSpeed = 2f;

        [Tooltip("正常状态下的冲刺速度，单位：米/秒。")]
        [SerializeField] private float _sprintSpeed = 3.5f;

        [Tooltip("猎杀期间肾上腺素加成后的冲刺速度，单位：米/秒。")]
        [SerializeField] private float _huntSprintSpeed = 3.5f;

        [Tooltip("蹲伏移动速度，单位：米/秒。")]
        [SerializeField] private float _crouchSpeed = 1.4f;

        [Tooltip("重力加速度，单位：米/秒²，应使用负值。")]
        [SerializeField] private float _gravity = -20f;

        [Tooltip("接地时保持的轻微向下速度，防止 CharacterController 离地抖动。")]
        [SerializeField] private float _groundedVerticalVelocity = -2f;

        [Tooltip("判定玩家正在有效移动的最小水平速度，避免微小碰撞位移触发摆动和脚步。")]
        [SerializeField] private float _movingSpeedThreshold = 0.05f;

        [Header("体力")]
        [Tooltip("满体力可连续冲刺的秒数。体力数值以可用冲刺秒数表示。")]
        [SerializeField] private float _staminaDuration = 4.2f;

        [Tooltip("体力从完全耗尽恢复到满值所需秒数。")]
        [SerializeField] private float _staminaRecoveryDuration = 3.5f;

        [Tooltip("猎杀期间的体力消耗倍率；0.5 表示消耗速率减半。")]
        [SerializeField] private float _huntStaminaDrainMultiplier = 0.5f;

        [Header("蹲伏")]
        [Tooltip("站立时 CharacterController 的高度。")]
        [SerializeField] private float _standingHeight = 1.8f;

        [Tooltip("蹲伏时 CharacterController 的高度。")]
        [SerializeField] private float _crouchingHeight = 1f;

        [Tooltip("站立时 CharacterController 的中心点；默认让胶囊底部位于玩家原点。")]
        [SerializeField] private Vector3 _standingCenter = new Vector3(0f, 0.9f, 0f);

        [Tooltip("站立与蹲伏高度平滑过渡的响应时间，单位：秒。")]
        [SerializeField] private float _crouchTransitionTime = 0.12f;

        [Tooltip("判定起身过渡完成的高度误差，单位：米。")]
        [SerializeField] private float _crouchCompletionTolerance = 0.01f;

        [Tooltip("起身 SphereCast 使用的碰撞层。建议排除 Player 层。")]
        [SerializeField] private LayerMask _ceilingMask = ~0;

        [Tooltip("起身检测额外向上延伸的安全距离，单位：米。")]
        [SerializeField] private float _ceilingCheckPadding = 0.05f;

        [Header("视角")]
        [Tooltip("鼠标视角灵敏度，单位：每个输入增量对应的旋转度数。")]
        [SerializeField] private float _mouseLookSensitivity = 0.1f;

        [Tooltip("手柄视角转速，单位：度/秒。")]
        [SerializeField] private float _gamepadLookSpeed = 150f;

        [Tooltip("进入播放模式时是否锁定鼠标并隐藏光标；调试时可关闭。")]
        [SerializeField] private bool _lockCursorOnStart = true;

        [Tooltip("是否反转视角的 Y 轴。")]
        [SerializeField] private bool _invertY;

        [Tooltip("正常状态下向上看的最大俯仰角，单位：度。")]
        [SerializeField] private float _maximumLookUpAngle = 85f;

        [Tooltip("正常状态下向下看的最大俯仰角，单位：度。")]
        [SerializeField] private float _maximumLookDownAngle = 85f;

        [Tooltip("藏匿状态下相对进入藏匿点朝向的最大水平转角，单位：度。")]
        [SerializeField] private float _hidingHorizontalAngle = 30f;

        [Tooltip("藏匿状态下相对进入藏匿点俯仰角的最大垂直转角，单位：度。")]
        [SerializeField] private float _hidingVerticalAngle = 20f;

        [Header("头部摆动")]
        [Tooltip("行走时头部上下摆动振幅，单位：米。")]
        [SerializeField] private float _walkBobVerticalAmplitude = 0.035f;

        [Tooltip("行走时头部左右摆动振幅，单位：米。")]
        [SerializeField] private float _walkBobHorizontalAmplitude = 0.025f;

        [Tooltip("行走时头部摆动频率，单位：周期/秒。")]
        [SerializeField] private float _walkBobFrequency = 1.8f;

        [Tooltip("冲刺时头部上下摆动振幅，单位：米。")]
        [SerializeField] private float _sprintBobVerticalAmplitude = 0.055f;

        [Tooltip("冲刺时头部左右摆动振幅，单位：米。")]
        [SerializeField] private float _sprintBobHorizontalAmplitude = 0.04f;

        [Tooltip("冲刺时头部摆动频率，单位：周期/秒。")]
        [SerializeField] private float _sprintBobFrequency = 2.6f;

        [Tooltip("蹲伏移动时相对于行走摆动振幅的倍率。")]
        [SerializeField] private float _crouchBobAmplitudeMultiplier = 0.5f;

        [Tooltip("左右摆动相对上下摆动的频率倍率。")]
        [SerializeField] private float _horizontalBobFrequencyMultiplier = 0.5f;

        [Tooltip("摆动进入目标或静止回中时的平滑速度。")]
        [SerializeField] private float _bobSmoothingSpeed = 12f;

        [Header("脚步")]
        [Tooltip("正常行走每次触发脚步事件所需的水平移动距离，单位：米。")]
        [SerializeField] private float _footstepStrideDistance = 2.2f;

        [Tooltip("蹲伏时步幅相对于正常步幅的倍率。")]
        [SerializeField] private float _crouchStrideMultiplier = 1.6f;

        public event Action<float> OnFootstep;

        public float CurrentStamina { get; private set; }
        public float CurrentStaminaNormalized => _staminaDuration > Mathf.Epsilon
            ? CurrentStamina / _staminaDuration
            : 0f;
        public float CurrentHorizontalSpeed { get; private set; }
        public bool IsSprinting { get; private set; }
        public bool IsCrouched { get; private set; }
        public bool IsGrounded { get; private set; }

        private readonly RaycastHit[] _ceilingHits = new RaycastHit[16];

        private CharacterController _characterController;
        private InputAction _moveAction;
        private InputAction _lookAction;
        private InputAction _sprintAction;
        private InputAction _crouchAction;

        private bool _moveActionEnabledHere;
        private bool _lookActionEnabledHere;
        private bool _sprintActionEnabledHere;
        private bool _crouchActionEnabledHere;
        private bool _isInitialized;
        private bool _isHuntActive;
        private bool _isHiding;
        private bool _isLookSuspended;
        private bool _sprintLockedUntilFull;
        private bool _isTryingToStand;
        private bool _standRequested;
        private bool _crouchActionSubscribed;

        private float _verticalVelocity;
        private float _yaw;
        private float _pitch;
        private float _hidingYawCenter;
        private float _hidingPitchCenter;
        private float _heightSmoothVelocity;
        private float _bobPhase;
        private float _footstepDistanceAccumulator;

        private Vector3 _standingCameraLocalPosition;
        private Vector3 _bobOffset;
        private Quaternion _cameraInitialLocalRotation;

        private void Awake()
        {
            _characterController = GetComponent<CharacterController>();
            if (_characterController == null)
            {
                Debug.LogError("PlayerController 缺少必需的 CharacterController 组件。", this);
                enabled = false;
                return;
            }

            if (_cameraPivot == null)
            {
                Debug.LogError("PlayerController 未指定 Camera Pivot，无法控制视角与头部摆动。", this);
                enabled = false;
                return;
            }

            if (!TryInitializeInput())
            {
                enabled = false;
                return;
            }

            _characterController.height = _standingHeight;
            _characterController.center = _standingCenter;
            _standingCameraLocalPosition = _cameraPivot.localPosition;
            _cameraInitialLocalRotation = _cameraPivot.localRotation;
            _yaw = transform.eulerAngles.y;
            CurrentStamina = Mathf.Max(0f, _staminaDuration);
            _isInitialized = true;
        }

        private void OnEnable()
        {
            if (_lockCursorOnStart)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }

            GameEvents.OnHuntStart += HandleHuntStart;
            GameEvents.OnHuntEnd += HandleHuntEnd;
            GameEvents.OnHidingChanged += HandleHidingChanged;
            GameEvents.OnLookSuspendedChanged += HandleLookSuspendedChanged;

            if (_crouchAction != null)
            {
                _crouchAction.performed += HandleCrouchPerformed;
                _crouchActionSubscribed = true;
            }

            if (_isInitialized)
            {
                EnableInputActions();
            }
        }

        private void OnDisable()
        {
            GameEvents.OnHuntStart -= HandleHuntStart;
            GameEvents.OnHuntEnd -= HandleHuntEnd;
            GameEvents.OnHidingChanged -= HandleHidingChanged;
            GameEvents.OnLookSuspendedChanged -= HandleLookSuspendedChanged;

            if (_crouchActionSubscribed && _crouchAction != null)
            {
                _crouchAction.performed -= HandleCrouchPerformed;
            }

            _crouchActionSubscribed = false;

            DisableInputActions();
            IsSprinting = false;
            _isLookSuspended = false;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        private void OnDestroy()
        {
            OnFootstep = null;
        }

        private void Update()
        {
            if (!_isInitialized)
            {
                return;
            }

            float deltaTime = Time.deltaTime;
            if (!_isLookSuspended)
            {
                UpdateLook(deltaTime);
            }

            UpdateGroundedState();
            UpdateCrouch(deltaTime);

            Vector2 moveInput = _isHiding ? Vector2.zero : _moveAction.ReadValue<Vector2>();
            bool hasMoveInput = moveInput.sqrMagnitude > Mathf.Epsilon;
            UpdateStamina(hasMoveInput, deltaTime);
            MoveCharacter(moveInput, deltaTime);
            UpdateHeadBob(deltaTime);
        }

        private bool TryInitializeInput()
        {
            if (_inputActions == null)
            {
                Debug.LogError("PlayerController 未注入工程现有的 InputSystem_Actions InputActionAsset。", this);
                return false;
            }

            InputActionMap playerMap = _inputActions.FindActionMap(PlayerActionMapName, false);
            if (playerMap == null)
            {
                Debug.LogError($"InputActionAsset 中找不到名为 {PlayerActionMapName} 的 Action Map。", this);
                return false;
            }

            _moveAction = playerMap.FindAction(MoveActionName, false);
            _lookAction = playerMap.FindAction(LookActionName, false);
            _sprintAction = playerMap.FindAction(SprintActionName, false);
            _crouchAction = playerMap.FindAction(CrouchActionName, false);

            if (_moveAction == null || _lookAction == null || _sprintAction == null || _crouchAction == null)
            {
                Debug.LogError("Player Action Map 必须包含 Move、Look、Sprint、Crouch 四个 Action。", this);
                return false;
            }

            return true;
        }

        private void EnableInputActions()
        {
            EnableActionIfNeeded(_moveAction, ref _moveActionEnabledHere);
            EnableActionIfNeeded(_lookAction, ref _lookActionEnabledHere);
            EnableActionIfNeeded(_sprintAction, ref _sprintActionEnabledHere);
            EnableActionIfNeeded(_crouchAction, ref _crouchActionEnabledHere);
        }

        private static void EnableActionIfNeeded(InputAction action, ref bool enabledHere)
        {
            enabledHere = action != null && !action.enabled;
            if (enabledHere)
            {
                action.Enable();
            }
        }

        private void DisableInputActions()
        {
            DisableActionIfOwned(_moveAction, ref _moveActionEnabledHere);
            DisableActionIfOwned(_lookAction, ref _lookActionEnabledHere);
            DisableActionIfOwned(_sprintAction, ref _sprintActionEnabledHere);
            DisableActionIfOwned(_crouchAction, ref _crouchActionEnabledHere);
        }

        private static void DisableActionIfOwned(InputAction action, ref bool enabledHere)
        {
            if (enabledHere && action != null)
            {
                action.Disable();
            }

            enabledHere = false;
        }

        private void UpdateLook(float deltaTime)
        {
            Vector2 lookInput = _lookAction.ReadValue<Vector2>();
            bool isPointerDelta = _lookAction.activeControl?.device is Pointer;
            float lookMultiplier = isPointerDelta
                ? _mouseLookSensitivity
                : _gamepadLookSpeed * deltaTime;

            float yawDelta = lookInput.x * lookMultiplier;
            float pitchDirection = _invertY ? 1f : -1f;
            float pitchDelta = lookInput.y * lookMultiplier * pitchDirection;

            if (_isHiding)
            {
                _yaw = Mathf.Clamp(
                    _yaw + yawDelta,
                    _hidingYawCenter - _hidingHorizontalAngle,
                    _hidingYawCenter + _hidingHorizontalAngle);
                _pitch = Mathf.Clamp(
                    _pitch + pitchDelta,
                    _hidingPitchCenter - _hidingVerticalAngle,
                    _hidingPitchCenter + _hidingVerticalAngle);
            }
            else
            {
                _yaw += yawDelta;
                _pitch = Mathf.Clamp(
                    _pitch + pitchDelta,
                    -_maximumLookUpAngle,
                    _maximumLookDownAngle);
            }

            transform.rotation = Quaternion.Euler(0f, _yaw, 0f);
            _cameraPivot.localRotation = _cameraInitialLocalRotation * Quaternion.Euler(_pitch, 0f, 0f);
        }

        private void UpdateGroundedState()
        {
            IsGrounded = _characterController.isGrounded;
            if (IsGrounded && _verticalVelocity < 0f)
            {
                _verticalVelocity = _groundedVerticalVelocity;
            }
        }

        private void HandleCrouchPerformed(InputAction.CallbackContext context)
        {
            if (_isHiding)
            {
                return;
            }

            if (IsCrouched)
            {
                _standRequested = !_standRequested;
                if (!_standRequested)
                {
                    _isTryingToStand = false;
                    _heightSmoothVelocity = Mathf.Min(_heightSmoothVelocity, 0f);
                }

                return;
            }

            IsCrouched = true;
            _standRequested = false;
            _isTryingToStand = false;
            _heightSmoothVelocity = Mathf.Min(_heightSmoothVelocity, 0f);
        }

        private void UpdateCrouch(float deltaTime)
        {
            if (_isHiding)
            {
                if (_isTryingToStand)
                {
                    _heightSmoothVelocity = Mathf.Min(_heightSmoothVelocity, 0f);
                }

                _standRequested = false;
                _isTryingToStand = false;
            }
            else if (IsCrouched)
            {
                if (_standRequested)
                {
                    // 起身不是一次性判定：过渡全程每帧检查，途中进入低顶区域会立即停止增高。
                    bool canContinueStanding = CanStandUp();
                    if (_isTryingToStand && !canContinueStanding)
                    {
                        _heightSmoothVelocity = Mathf.Min(_heightSmoothVelocity, 0f);
                    }

                    _isTryingToStand = canContinueStanding;
                }
                else
                {
                    _isTryingToStand = false;
                }
            }

            float targetHeight = IsCrouched && !_isTryingToStand
                ? _crouchingHeight
                : _standingHeight;
            float smoothTime = Mathf.Max(Mathf.Epsilon, _crouchTransitionTime);
            _characterController.height = Mathf.SmoothDamp(
                _characterController.height,
                targetHeight,
                ref _heightSmoothVelocity,
                smoothTime,
                Mathf.Infinity,
                deltaTime);

            // center 与 height 使用同一结果推导，确保胶囊体底部在过渡全程保持稳定。
            Vector3 synchronizedCenter = _standingCenter;
            synchronizedCenter.y -= (_standingHeight - _characterController.height) * 0.5f;
            _characterController.center = synchronizedCenter;

            float completionTolerance = Mathf.Max(Mathf.Epsilon, _crouchCompletionTolerance);
            if (_isTryingToStand
                && Mathf.Abs(_characterController.height - _standingHeight) <= completionTolerance)
            {
                _characterController.height = _standingHeight;
                _characterController.center = _standingCenter;
                IsCrouched = false;
                _standRequested = false;
                _isTryingToStand = false;
            }
        }

        private bool CanStandUp()
        {
            float castRadius = Mathf.Max(Mathf.Epsilon, _characterController.radius - _characterController.skinWidth);
            float currentTopY = _characterController.center.y
                + _characterController.height * 0.5f
                - castRadius;
            float standingTopY = _standingCenter.y
                + _standingHeight * 0.5f
                - castRadius;
            float localCastDistance = standingTopY - currentTopY;

            if (localCastDistance <= Mathf.Epsilon)
            {
                return true;
            }

            Vector3 castOrigin = transform.TransformPoint(
                new Vector3(_characterController.center.x, currentTopY, _characterController.center.z));
            Vector3 standingTop = transform.TransformPoint(
                new Vector3(_standingCenter.x, standingTopY, _standingCenter.z));
            Vector3 castVector = standingTop - castOrigin;
            float castDistance = castVector.magnitude + Mathf.Max(0f, _ceilingCheckPadding);
            Vector3 castDirection = castVector.sqrMagnitude > Mathf.Epsilon
                ? castVector.normalized
                : transform.up;
            float worldRadius = castRadius * Mathf.Max(
                Mathf.Abs(transform.lossyScale.x),
                Mathf.Abs(transform.lossyScale.z));
            int hitCount = Physics.SphereCastNonAlloc(
                castOrigin,
                worldRadius,
                castDirection,
                _ceilingHits,
                castDistance,
                _ceilingMask,
                QueryTriggerInteraction.Ignore);

            // 命中数组装满时保守地禁止起身，避免漏掉后续真实遮挡物。
            if (hitCount >= _ceilingHits.Length)
            {
                return false;
            }

            for (int hitIndex = 0; hitIndex < hitCount; hitIndex++)
            {
                Collider hitCollider = _ceilingHits[hitIndex].collider;
                if (hitCollider != null && !hitCollider.transform.IsChildOf(transform))
                {
                    return false;
                }
            }

            return true;
        }

        private void UpdateStamina(bool hasMoveInput, float deltaTime)
        {
            bool sprintRequested = !_isHiding
                && !IsCrouched
                && IsGrounded
                && hasMoveInput
                && _sprintAction.IsPressed();
            bool hasStamina = CurrentStamina > Mathf.Epsilon;
            IsSprinting = sprintRequested && hasStamina && !_sprintLockedUntilFull;

            if (IsSprinting)
            {
                float drainMultiplier = _isHuntActive
                    ? Mathf.Max(0f, _huntStaminaDrainMultiplier)
                    : 1f;
                CurrentStamina = Mathf.Max(0f, CurrentStamina - deltaTime * drainMultiplier);
                if (CurrentStamina <= Mathf.Epsilon)
                {
                    CurrentStamina = 0f;
                    if (!_sprintLockedUntilFull) GameEvents.RaiseStaminaDepleted();
                    _sprintLockedUntilFull = true;
                }

                return;
            }

            float recoveryDuration = Mathf.Max(Mathf.Epsilon, _staminaRecoveryDuration);
            float recoveryRate = Mathf.Max(0f, _staminaDuration) / recoveryDuration;
            CurrentStamina = Mathf.MoveTowards(
                CurrentStamina,
                Mathf.Max(0f, _staminaDuration),
                recoveryRate * deltaTime);

            if (_sprintLockedUntilFull
                && CurrentStamina >= Mathf.Max(0f, _staminaDuration) - Mathf.Epsilon)
            {
                CurrentStamina = Mathf.Max(0f, _staminaDuration);
                _sprintLockedUntilFull = false;
            }
        }

        private void MoveCharacter(Vector2 moveInput, float deltaTime)
        {
            Vector3 localMove = new Vector3(moveInput.x, 0f, moveInput.y);
            if (localMove.sqrMagnitude > 1f)
            {
                localMove.Normalize();
            }

            Vector3 worldMove = transform.right * localMove.x + transform.forward * localMove.z;
            float movementSpeed = IsCrouched
                ? _crouchSpeed
                : IsSprinting
                    ? (_isHuntActive ? _huntSprintSpeed : _sprintSpeed)
                    : _walkSpeed;

            if (!IsGrounded)
            {
                _verticalVelocity += _gravity * deltaTime;
            }

            Vector3 positionBeforeMove = transform.position;
            Vector3 velocity = worldMove * movementSpeed + Vector3.up * _verticalVelocity;
            _characterController.Move(velocity * deltaTime);
            IsGrounded = _characterController.isGrounded;

            Vector3 actualDisplacement = Vector3.ProjectOnPlane(
                transform.position - positionBeforeMove,
                Vector3.up);
            float horizontalDistance = actualDisplacement.magnitude;
            CurrentHorizontalSpeed = deltaTime > Mathf.Epsilon
                ? horizontalDistance / deltaTime
                : 0f;

            UpdateFootsteps(horizontalDistance);
        }

        private void UpdateFootsteps(float horizontalDistance)
        {
            if (!IsGrounded || CurrentHorizontalSpeed <= _movingSpeedThreshold)
            {
                return;
            }

            _footstepDistanceAccumulator += horizontalDistance;
            float strideMultiplier = IsCrouched ? _crouchStrideMultiplier : 1f;
            float currentStride = Mathf.Max(
                Mathf.Epsilon,
                _footstepStrideDistance * strideMultiplier);

            while (_footstepDistanceAccumulator >= currentStride)
            {
                _footstepDistanceAccumulator -= currentStride;
                OnFootstep?.Invoke(CurrentHorizontalSpeed);
            }
        }

        private void UpdateHeadBob(float deltaTime)
        {
            bool shouldBob = IsGrounded && CurrentHorizontalSpeed > _movingSpeedThreshold;
            Vector3 targetBobOffset = Vector3.zero;

            if (shouldBob)
            {
                float verticalAmplitude = IsSprinting
                    ? _sprintBobVerticalAmplitude
                    : _walkBobVerticalAmplitude;
                float horizontalAmplitude = IsSprinting
                    ? _sprintBobHorizontalAmplitude
                    : _walkBobHorizontalAmplitude;
                float frequency = IsSprinting ? _sprintBobFrequency : _walkBobFrequency;

                if (IsCrouched)
                {
                    verticalAmplitude *= _crouchBobAmplitudeMultiplier;
                    horizontalAmplitude *= _crouchBobAmplitudeMultiplier;
                }

                _bobPhase += deltaTime * frequency * Mathf.PI * 2f;
                targetBobOffset.y = Mathf.Sin(_bobPhase) * verticalAmplitude;
                targetBobOffset.x = Mathf.Sin(_bobPhase * _horizontalBobFrequencyMultiplier)
                    * horizontalAmplitude;
            }

            float smoothingFactor = 1f - Mathf.Exp(-Mathf.Max(0f, _bobSmoothingSpeed) * deltaTime);
            _bobOffset = Vector3.Lerp(_bobOffset, targetBobOffset, smoothingFactor);

            // 相机基准高度直接跟随平滑后的胶囊高度，再叠加头部摆动。
            Vector3 cameraBasePosition = _standingCameraLocalPosition;
            cameraBasePosition.y -= _standingHeight - _characterController.height;
            _cameraPivot.localPosition = cameraBasePosition + _bobOffset;
        }

        private void HandleHuntStart(float huntDuration)
        {
            _isHuntActive = true;
        }

        private void HandleHuntEnd()
        {
            _isHuntActive = false;
        }

        private void HandleLookSuspendedChanged(bool isSuspended)
        {
            _isLookSuspended = isSuspended;

            if (isSuspended)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                return;
            }

            if (_lockCursorOnStart)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }

        private void HandleHidingChanged(bool isHiding)
        {
            if (_isHiding == isHiding)
            {
                return;
            }

            if (isHiding)
            {
                // 隐藏模块可能先把玩家对齐到藏匿点；以实际姿态作为受限视角中心。
                _yaw = transform.eulerAngles.y;
                Quaternion relativeCameraRotation = Quaternion.Inverse(_cameraInitialLocalRotation)
                    * _cameraPivot.localRotation;
                _pitch = Mathf.DeltaAngle(0f, relativeCameraRotation.eulerAngles.x);
                _hidingYawCenter = _yaw;
                _hidingPitchCenter = _pitch;
            }

            _isHiding = isHiding;
            IsSprinting = false;
        }
    }
}
