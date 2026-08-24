using System.Collections;
using Residuum.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Residuum.Items
{
    [DisallowMultipleComponent]
    public sealed class Flashlight : MonoBehaviour, IHoldable
    {
        private const string PlayerActionMapName = "Player";
        private const string FlashlightActionTemplateName = "Previous";
        private const string FlashlightBindingPath = "<Keyboard>/f";

        [Header("依赖")]
        [Tooltip("子物体上的 URP Spot Light。")]
        [SerializeField] private Light _spotLight;

        [Tooltip("工程现有的 Assets/InputSystem_Actions.inputactions。脚本只创建运行时 Action 副本，不修改输入资产。")]
        [SerializeField] private InputActionAsset _inputActions;

        [Header("光照")]
        [Tooltip("手电筒 Spot Light 的光照强度。")]
        [SerializeField] private float _intensity = 8f;

        [Tooltip("手电筒色温，单位：开尔文。")]
        [SerializeField] private float _colorTemperature = 4200f;

        [Tooltip("手电筒照射距离，单位：米。")]
        [SerializeField] private float _range = 12f;

        [Tooltip("Spot Light 内锥角，单位：度。")]
        [SerializeField] private float _innerSpotAngle = 30f;

        [Tooltip("Spot Light 外锥角，单位：度。")]
        [SerializeField] private float _outerSpotAngle = 45f;

        [Header("电量")]
        [Tooltip("从满电持续开启到耗尽的秒数。")]
        [SerializeField] private float _fullBatterySeconds = 300f;

        [Tooltip("归一化电量相对上次广播至少变化此值时，才向 HUD 广播。")]
        [SerializeField] private float _batteryBroadcastThreshold = 0.01f;

        [Tooltip("电量低于此归一化值时开始随机闪烁。")]
        [SerializeField] private float _lowBatteryThreshold = 0.2f;

        [Tooltip("电量耗尽后的强制冷却秒数；冷却结束后内置电池恢复满电。")]
        [SerializeField] private float _depletedCooldownSeconds = 15f;

        [Header("低电量闪烁")]
        [Tooltip("电量接近耗尽时，两次低电量闪烁之间的最短间隔，单位：秒。")]
        [SerializeField] private float _lowBatteryMinimumInterval = 0.08f;

        [Tooltip("电量刚低于阈值时，两次低电量闪烁之间的最长间隔，单位：秒。")]
        [SerializeField] private float _lowBatteryMaximumInterval = 0.8f;

        [Tooltip("低电量闪烁每次熄灭的时长，单位：秒。")]
        [SerializeField] private float _lowBatteryOffDuration = 0.05f;

        [Tooltip("低电量随机间隔的最小倍率。")]
        [SerializeField] private float _lowBatteryRandomMinimumMultiplier = 0.75f;

        [Tooltip("低电量随机间隔的最大倍率。")]
        [SerializeField] private float _lowBatteryRandomMaximumMultiplier = 1.25f;

        [Header("强制故障")]
        [Tooltip("猎杀与近距离鬼事件闪烁的最短随机间隔，单位：秒。")]
        [SerializeField] private float _forcedFlickerMinimumInterval = 0.05f;

        [Tooltip("猎杀与近距离鬼事件闪烁的最长随机间隔，单位：秒。")]
        [SerializeField] private float _forcedFlickerMaximumInterval = 0.4f;

        [Tooltip("预先缓存的随机闪烁等待对象数量；越大时长变化越丰富。")]
        [SerializeField] private int _flickerWaitCacheSize = 16;

        [Tooltip("鬼事件距离玩家小于此值时触发短暂闪烁，单位：米。")]
        [SerializeField] private float _ghostEventDistance = 8f;

        [Tooltip("近距离鬼事件触发的短暂闪烁时长，单位：秒。")]
        [SerializeField] private float _ghostEventFlickerDuration = 1.5f;

        public string ItemName => "手电筒";

        private InputAction _flashlightAction;
        private WaitForSeconds[] _flickerWaitCache;
        private WaitForSeconds _cooldownWait;
        private Coroutine _forcedFlickerCoroutine;
        private Coroutine _cooldownCoroutine;

        private float _batterySecondsRemaining;
        private float _lastBroadcastBattery = float.NegativeInfinity;
        private float _lowBatteryFlickerTimer;
        private bool _isInitialized;
        private bool _isRequestedOn;
        private bool _isLowBatteryPulseOff;
        private bool _isCoolingDown;
        private bool _isHuntActive;
        private bool _isExternallyLocked;

        private void Awake()
        {
            ValidateSettings();

            if (_spotLight == null)
            {
                Debug.LogError("Flashlight 未指定子物体上的 URP Spot Light，组件已禁用。", this);
                enabled = false;
                return;
            }

            if (!TryInitializeInput())
            {
                enabled = false;
                return;
            }

            ApplyLightSettings();
            BuildWaitCache();
            _batterySecondsRemaining = _fullBatterySeconds;
            SetLightEnabled(false);
            _isInitialized = true;
        }

        private void OnEnable()
        {
            if (!_isInitialized)
            {
                return;
            }

            GameEvents.OnHuntStart += HandleHuntStart;
            GameEvents.OnHuntEnd += HandleHuntEnd;
            GameEvents.OnGhostEvent += HandleGhostEvent;

            _flashlightAction.performed += HandleFlashlightPerformed;
            _flashlightAction.Enable();
            BroadcastBatteryIfNeeded(true);

            if (_isCoolingDown)
            {
                _cooldownCoroutine = StartCoroutine(CooldownRoutine());
            }
            else if (_isHuntActive)
            {
                StartForcedFlicker(false);
            }
            else
            {
                ApplyRequestedLightState();
            }
        }

        private void OnDisable()
        {
            GameEvents.OnHuntStart -= HandleHuntStart;
            GameEvents.OnHuntEnd -= HandleHuntEnd;
            GameEvents.OnGhostEvent -= HandleGhostEvent;

            if (_flashlightAction != null)
            {
                _flashlightAction.performed -= HandleFlashlightPerformed;
                _flashlightAction.Disable();
            }

            StopRunningCoroutines();
            ResetLowBatteryFlicker();
            SetLightEnabled(false);
        }

        private void OnDestroy()
        {
            GameEvents.OnHuntStart -= HandleHuntStart;
            GameEvents.OnHuntEnd -= HandleHuntEnd;
            GameEvents.OnGhostEvent -= HandleGhostEvent;

            if (_flashlightAction != null)
            {
                _flashlightAction.performed -= HandleFlashlightPerformed;
                _flashlightAction.Disable();
                _flashlightAction.Dispose();
                _flashlightAction = null;
            }

            StopRunningCoroutines();
            _flickerWaitCache = null;
            _cooldownWait = null;
        }

        private void Update()
        {
            if (!_isInitialized || _spotLight == null)
            {
                return;
            }

            DrainBattery(Time.deltaTime);

            if (!_isHuntActive && _forcedFlickerCoroutine == null)
            {
                UpdateLowBatteryFlicker(Time.deltaTime);
            }
        }

        public void OnEquip()
        {
            // 模型显隐由 ItemSlotSystem 负责；控制器保持激活，确保未装备时 F 键仍可使用。
        }

        public void OnUnequip()
        {
            // 手电筒是主光源，收起模型不改变开关状态，也不停止事件响应。
        }

        public void OnPrimaryUse()
        {
            ToggleLight();
        }

        public void ForceOff(bool locked)
        {
            _isExternallyLocked = locked;
            _isRequestedOn = false;
            StopForcedFlicker();
            ResetLowBatteryFlicker();
            SetLightEnabled(false);

            if (!locked && _isHuntActive && !_isCoolingDown && _batterySecondsRemaining > 0f)
            {
                StartForcedFlicker(false);
            }
        }

        private void HandleFlashlightPerformed(InputAction.CallbackContext context)
        {
            _ = context;
            ToggleLight();
        }

        private void ToggleLight()
        {
            if (!_isInitialized || _isExternallyLocked || _isCoolingDown || _isHuntActive
                || _forcedFlickerCoroutine != null || _batterySecondsRemaining <= 0f)
            {
                return;
            }

            _isRequestedOn = !_isRequestedOn;
            ResetLowBatteryFlicker();
            ApplyRequestedLightState();
        }

        private void HandleHuntStart(float duration)
        {
            _ = duration;
            _isHuntActive = true;
            ResetLowBatteryFlicker();
            StartForcedFlicker(false);
        }

        private void HandleHuntEnd()
        {
            _isHuntActive = false;
            StopForcedFlicker();
            ResetLowBatteryFlicker();
            ApplyRequestedLightState();
        }

        private void HandleGhostEvent(Vector3 ghostPosition)
        {
            if (!_isInitialized || _isHuntActive || _isExternallyLocked || _isCoolingDown
                || _batterySecondsRemaining <= 0f)
            {
                return;
            }

            float maximumDistanceSquared = _ghostEventDistance * _ghostEventDistance;
            if ((ghostPosition - transform.position).sqrMagnitude > maximumDistanceSquared)
            {
                return;
            }

            ResetLowBatteryFlicker();
            StartForcedFlicker(true);
        }

        private void StartForcedFlicker(bool hasDurationLimit)
        {
            StopForcedFlicker();

            if (_spotLight == null || _isExternallyLocked || _isCoolingDown
                || _batterySecondsRemaining <= 0f
                || (hasDurationLimit && _ghostEventFlickerDuration <= 0f)
                || _flickerWaitCache == null || _flickerWaitCache.Length == 0)
            {
                SetLightEnabled(false);
                ApplyRequestedLightState();
                return;
            }

            SetLightEnabled(false);
            _forcedFlickerCoroutine = StartCoroutine(ForcedFlickerRoutine(hasDurationLimit));
        }

        private IEnumerator ForcedFlickerRoutine(bool hasDurationLimit)
        {
            float elapsed = 0f;
            bool flickerState = false;

            while (!hasDurationLimit || elapsed < _ghostEventFlickerDuration)
            {
                if (_isExternallyLocked || _isCoolingDown || _batterySecondsRemaining <= 0f)
                {
                    break;
                }

                flickerState = !flickerState;
                SetLightEnabled(flickerState);

                WaitForSeconds wait = GetRandomFlickerWait(out float waitDuration);
                if (wait == null)
                {
                    break;
                }

                yield return wait;
                elapsed += waitDuration;

                if (!hasDurationLimit && !_isHuntActive)
                {
                    break;
                }
            }

            _forcedFlickerCoroutine = null;
            ApplyRequestedLightState();
        }

        private WaitForSeconds GetRandomFlickerWait(out float duration)
        {
            duration = 0f;
            if (_flickerWaitCache == null || _flickerWaitCache.Length == 0)
            {
                return null;
            }

            int index = Random.Range(0, _flickerWaitCache.Length);
            float interpolation = _flickerWaitCache.Length > 1
                ? (float)index / (_flickerWaitCache.Length - 1)
                : 0f;
            duration = Mathf.Lerp(_forcedFlickerMinimumInterval, _forcedFlickerMaximumInterval, interpolation);
            return _flickerWaitCache[index];
        }

        private void StopForcedFlicker()
        {
            if (_forcedFlickerCoroutine != null)
            {
                StopCoroutine(_forcedFlickerCoroutine);
                _forcedFlickerCoroutine = null;
            }
        }

        private void UpdateLowBatteryFlicker(float deltaTime)
        {
            if (!_isRequestedOn || _isExternallyLocked || _isCoolingDown || _batterySecondsRemaining <= 0f)
            {
                ResetLowBatteryFlicker();
                ApplyRequestedLightState();
                return;
            }

            float normalizedBattery = GetNormalizedBattery();
            if (normalizedBattery >= _lowBatteryThreshold)
            {
                ResetLowBatteryFlicker();
                SetLightEnabled(true);
                return;
            }

            _lowBatteryFlickerTimer -= deltaTime;
            if (_lowBatteryFlickerTimer > 0f)
            {
                return;
            }

            _isLowBatteryPulseOff = !_isLowBatteryPulseOff;
            SetLightEnabled(!_isLowBatteryPulseOff);

            if (_isLowBatteryPulseOff)
            {
                _lowBatteryFlickerTimer = _lowBatteryOffDuration;
                return;
            }

            float thresholdRatio = _lowBatteryThreshold > Mathf.Epsilon
                ? Mathf.Clamp01(normalizedBattery / _lowBatteryThreshold)
                : 0f;
            float baseInterval = Mathf.Lerp(
                _lowBatteryMinimumInterval,
                _lowBatteryMaximumInterval,
                thresholdRatio);
            float randomMultiplier = Random.Range(
                _lowBatteryRandomMinimumMultiplier,
                _lowBatteryRandomMaximumMultiplier);
            _lowBatteryFlickerTimer = baseInterval * randomMultiplier;
        }

        private void ResetLowBatteryFlicker()
        {
            _lowBatteryFlickerTimer = 0f;
            _isLowBatteryPulseOff = false;
        }

        private void DrainBattery(float deltaTime)
        {
            if (!_spotLight.enabled || _isCoolingDown || _batterySecondsRemaining <= 0f)
            {
                return;
            }

            _batterySecondsRemaining = Mathf.Max(0f, _batterySecondsRemaining - deltaTime);
            BroadcastBatteryIfNeeded(false);

            if (_batterySecondsRemaining <= 0f)
            {
                BeginDepletedCooldown();
            }
        }

        private void BeginDepletedCooldown()
        {
            _batterySecondsRemaining = 0f;
            _isRequestedOn = false;
            _isCoolingDown = true;
            StopForcedFlicker();
            ResetLowBatteryFlicker();
            SetLightEnabled(false);
            BroadcastBatteryIfNeeded(true);

            if (_cooldownCoroutine != null)
            {
                StopCoroutine(_cooldownCoroutine);
            }

            _cooldownCoroutine = StartCoroutine(CooldownRoutine());
        }

        private IEnumerator CooldownRoutine()
        {
            yield return _cooldownWait;

            _batterySecondsRemaining = _fullBatterySeconds;
            _isCoolingDown = false;
            _cooldownCoroutine = null;
            BroadcastBatteryIfNeeded(true);

            if (_isHuntActive)
            {
                StartForcedFlicker(false);
            }
            else
            {
                ApplyRequestedLightState();
            }
        }

        private void StopRunningCoroutines()
        {
            StopForcedFlicker();

            if (_cooldownCoroutine != null)
            {
                StopCoroutine(_cooldownCoroutine);
                _cooldownCoroutine = null;
            }
        }

        private void ApplyRequestedLightState()
        {
            bool canTurnOn = _isInitialized
                && !_isExternallyLocked
                && !_isCoolingDown
                && !_isHuntActive
                && _forcedFlickerCoroutine == null
                && _batterySecondsRemaining > 0f;
            SetLightEnabled(canTurnOn && _isRequestedOn);
        }

        private void SetLightEnabled(bool isEnabled)
        {
            if (_spotLight != null)
            {
                _spotLight.enabled = isEnabled;
            }
        }

        private float GetNormalizedBattery()
        {
            return _fullBatterySeconds > Mathf.Epsilon
                ? Mathf.Clamp01(_batterySecondsRemaining / _fullBatterySeconds)
                : 0f;
        }

        private void BroadcastBatteryIfNeeded(bool force)
        {
            float normalizedBattery = GetNormalizedBattery();
            if (!force && Mathf.Abs(normalizedBattery - _lastBroadcastBattery) < _batteryBroadcastThreshold)
            {
                return;
            }

            _lastBroadcastBattery = normalizedBattery;
            GameEvents.RaiseBatteryChanged(normalizedBattery);
        }

        private bool TryInitializeInput()
        {
            if (_inputActions == null)
            {
                Debug.LogError("Flashlight 未注入工程现有的 InputSystem_Actions InputActionAsset。", this);
                return false;
            }

            InputActionMap playerMap = _inputActions.FindActionMap(PlayerActionMapName, false);
            InputAction template = playerMap?.FindAction(FlashlightActionTemplateName, false);
            if (template == null || template.bindings.Count == 0)
            {
                Debug.LogError("InputActionAsset 必须包含带绑定的 Player/Previous Action，供 F 键运行时 Action 使用。", this);
                return false;
            }

            _flashlightAction = template.Clone();
            for (int bindingIndex = 0; bindingIndex < _flashlightAction.bindings.Count; bindingIndex++)
            {
                _flashlightAction.ApplyBindingOverride(bindingIndex, string.Empty);
            }

            _flashlightAction.ApplyBindingOverride(0, FlashlightBindingPath);
            return true;
        }

        private void ApplyLightSettings()
        {
            _spotLight.type = LightType.Spot;
            _spotLight.intensity = _intensity;
            _spotLight.useColorTemperature = true;
            _spotLight.colorTemperature = _colorTemperature;
            _spotLight.range = _range;
            _spotLight.innerSpotAngle = _innerSpotAngle;
            _spotLight.spotAngle = _outerSpotAngle;
        }

        private void BuildWaitCache()
        {
            _flickerWaitCache = new WaitForSeconds[_flickerWaitCacheSize];
            for (int index = 0; index < _flickerWaitCache.Length; index++)
            {
                float interpolation = _flickerWaitCache.Length > 1
                    ? (float)index / (_flickerWaitCache.Length - 1)
                    : 0f;
                float duration = Mathf.Lerp(
                    _forcedFlickerMinimumInterval,
                    _forcedFlickerMaximumInterval,
                    interpolation);
                _flickerWaitCache[index] = new WaitForSeconds(duration);
            }

            _cooldownWait = new WaitForSeconds(_depletedCooldownSeconds);
        }

        private void ValidateSettings()
        {
            _intensity = Mathf.Max(0f, _intensity);
            _colorTemperature = Mathf.Max(Mathf.Epsilon, _colorTemperature);
            _range = Mathf.Max(0f, _range);
            _outerSpotAngle = Mathf.Max(Mathf.Epsilon, _outerSpotAngle);
            _innerSpotAngle = Mathf.Clamp(_innerSpotAngle, 0f, _outerSpotAngle);
            _fullBatterySeconds = Mathf.Max(Mathf.Epsilon, _fullBatterySeconds);
            _batteryBroadcastThreshold = Mathf.Clamp01(_batteryBroadcastThreshold);
            _lowBatteryThreshold = Mathf.Clamp01(_lowBatteryThreshold);
            _depletedCooldownSeconds = Mathf.Max(0f, _depletedCooldownSeconds);
            _lowBatteryMinimumInterval = Mathf.Max(Mathf.Epsilon, _lowBatteryMinimumInterval);
            _lowBatteryMaximumInterval = Mathf.Max(_lowBatteryMinimumInterval, _lowBatteryMaximumInterval);
            _lowBatteryOffDuration = Mathf.Max(Mathf.Epsilon, _lowBatteryOffDuration);
            _lowBatteryRandomMinimumMultiplier = Mathf.Max(Mathf.Epsilon, _lowBatteryRandomMinimumMultiplier);
            _lowBatteryRandomMaximumMultiplier = Mathf.Max(
                _lowBatteryRandomMinimumMultiplier,
                _lowBatteryRandomMaximumMultiplier);
            _forcedFlickerMinimumInterval = Mathf.Max(Mathf.Epsilon, _forcedFlickerMinimumInterval);
            _forcedFlickerMaximumInterval = Mathf.Max(
                _forcedFlickerMinimumInterval,
                _forcedFlickerMaximumInterval);
            _flickerWaitCacheSize = Mathf.Max(1, _flickerWaitCacheSize);
            _ghostEventDistance = Mathf.Max(0f, _ghostEventDistance);
            _ghostEventFlickerDuration = Mathf.Max(0f, _ghostEventFlickerDuration);
        }
    }
}
