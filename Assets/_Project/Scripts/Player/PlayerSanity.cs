using System.Collections;
using Residuum.Core;
using UnityEngine;

namespace Residuum.Player
{
    /// <summary>
    /// 管理玩家本回合理智值，并通过 GameEvents 广播理智变化与阈值事件。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PlayerSanity : MonoBehaviour
    {
        private const float MinimumSanity = 0f;
        private const float MaximumSanity = 100f;
        private const float SanityThresholdStep = 25f;
        private const int MinimumBufferSize = 1;

        [Header("理智数值")]
        [Tooltip("每局开始时的理智值。")]
        [SerializeField] private float _startingSanity = 100f;

        [Tooltip("玩家处于黑暗中时每秒损失的理智值。")]
        [SerializeField] private float _darkDecayRate = 0.12f;

        [Tooltip("玩家处于有灯光房间时每秒损失的理智值。")]
        [SerializeField] private float _litRoomDecayRate = 0.06f;

        [Tooltip("手持且开启手电筒时，环境理智衰减速率乘以此倍率。")]
        [SerializeField] private float _flashlightRateMultiplier = 0.5f;

        [Tooltip("近距离目击一次有视线的鬼事件时扣除的理智值。")]
        [SerializeField] private float _ghostEventSanityLoss = 15f;

        [Tooltip("猎杀期间每秒额外损失的理智值。")]
        [SerializeField] private float _huntAdditionalDecayRate = 0.5f;

        [Tooltip("玩家位于安全区时每秒恢复的理智值。")]
        [SerializeField] private float _safeZoneRecoveryRate = 1f;

        [Tooltip("理智首次跌破此值时，整局广播一次理智危险事件。")]
        [SerializeField] private float _huntThreshold = 50f;

        [Tooltip("累计理智变化达到此值后才广播一次，避免每帧刷事件总线。")]
        [SerializeField] private float _sanityBroadcastThreshold = 0.1f;

        [Header("光照检测")]
        [Tooltip("启用时定期用物理球检测附近已启用的 Light；关闭时完全依赖 SetInLitRoom。")]
        [SerializeField] private bool _useLightProbe = true;

        [Tooltip("两次附近光源检测之间的间隔秒数。")]
        [SerializeField] private float _lightCheckInterval = 0.5f;

        [Tooltip("检测附近已启用 Light 的球形半径，单位：米。")]
        [SerializeField] private float _lightCheckRadius = 8f;

        [Tooltip("参与附近光源检测的物理层。默认检测全部层。")]
        [SerializeField] private LayerMask _lightDetectionLayers = ~0;

        [Tooltip("非分配光源检测使用的 Collider 缓冲区大小；场景密集时可适当调大。")]
        [SerializeField] private int _lightOverlapBufferSize = 32;

        [Header("鬼事件")]
        [Tooltip("鬼事件对理智生效的最大距离，单位：米。")]
        [SerializeField] private float _ghostEventEffectiveDistance = 15f;

        [Tooltip("会遮挡玩家与鬼事件之间视线的物理层。建议只选择墙体和大型障碍物层。")]
        [SerializeField] private LayerMask _ghostEventOcclusionMask = Physics.DefaultRaycastLayers;

        [Header("安全区")]
        [Tooltip("基地安全区的 Collider；留空表示本场景没有安全区。")]
        [SerializeField] private Collider _safeZone;

        private Collider[] _lightOverlapBuffer;
        private WaitForSeconds _lightCheckWait;
        private Coroutine _lightCheckCoroutine;
        private float _lastBroadcastSanity;
        private bool _isInLitRoom;
        private bool _isFlashlightOn;
        private bool _isHuntActive;
        private bool _hasRaisedCriticalThisRound;
        private bool _lightBufferWarningLogged;

        public float Current { get; private set; }

        private void Awake()
        {
            ValidateSettings();
            AllocateLightBuffer();
            CacheLightCheckWait();
            ResetRuntimeState(false);
        }

        private void OnEnable()
        {
            GameEvents.OnGhostEvent += HandleGhostEvent;
            GameEvents.OnHuntStart += HandleHuntStart;
            GameEvents.OnHuntEnd += HandleHuntEnd;
            GameEvents.OnRoundStart += HandleRoundStart;

            StartLightCheckCoroutine();
        }

        private void Update()
        {
            if (IsInsideSafeZone())
            {
                ChangeSanity(_safeZoneRecoveryRate * Time.deltaTime);
                return;
            }

            float environmentDecayRate = _isInLitRoom ? _litRoomDecayRate : _darkDecayRate;
            if (_isFlashlightOn)
            {
                environmentDecayRate *= _flashlightRateMultiplier;
            }

            float totalDecayRate = environmentDecayRate;
            if (_isHuntActive)
            {
                totalDecayRate += _huntAdditionalDecayRate;
            }

            ChangeSanity(-totalDecayRate * Time.deltaTime);
        }

        private void OnDisable()
        {
            GameEvents.OnGhostEvent -= HandleGhostEvent;
            GameEvents.OnHuntStart -= HandleHuntStart;
            GameEvents.OnHuntEnd -= HandleHuntEnd;
            GameEvents.OnRoundStart -= HandleRoundStart;

            StopLightCheckCoroutine();
        }

        private void OnDestroy()
        {
            // 防御性退订，避免异常销毁顺序让静态事件留下失效委托。
            GameEvents.OnGhostEvent -= HandleGhostEvent;
            GameEvents.OnHuntStart -= HandleHuntStart;
            GameEvents.OnHuntEnd -= HandleHuntEnd;
            GameEvents.OnRoundStart -= HandleRoundStart;

            StopLightCheckCoroutine();
            _lightCheckWait = null;
            _lightOverlapBuffer = null;
            _safeZone = null;
        }

        private void OnValidate()
        {
            ValidateSettings();
        }

        /// <summary>由房间或其他 Inspector UnityEvent 连接设置当前是否处于有光环境。</summary>
        public void SetInLitRoom(bool isInLitRoom)
        {
            _isInLitRoom = isInLitRoom;
        }

        /// <summary>由手电筒的 Inspector UnityEvent 连接设置当前开关状态。</summary>
        public void SetFlashlightOn(bool isFlashlightOn)
        {
            _isFlashlightOn = isFlashlightOn;
        }

        private void HandleGhostEvent(Vector3 eventPosition)
        {
            if (Vector3.Distance(transform.position, eventPosition) > _ghostEventEffectiveDistance)
            {
                return;
            }

            bool isOccluded = Physics.Linecast(
                transform.position,
                eventPosition,
                _ghostEventOcclusionMask,
                QueryTriggerInteraction.Ignore);

            if (!isOccluded)
            {
                // 每次事件回调只在通过距离与视线检查后扣除一次。
                ChangeSanity(-_ghostEventSanityLoss);
            }
        }

        private void HandleHuntStart(float _)
        {
            _isHuntActive = true;
        }

        private void HandleHuntEnd()
        {
            _isHuntActive = false;
        }

        private void HandleRoundStart()
        {
            ValidateSettings();
            AllocateLightBuffer();
            CacheLightCheckWait();
            ResetRuntimeState(true);
            RestartLightCheckCoroutine();
        }

        private void ChangeSanity(float delta)
        {
            float previous = Current;
            float next = Mathf.Clamp(previous + delta, MinimumSanity, MaximumSanity);
            if (Mathf.Approximately(previous, next))
            {
                return;
            }

            Current = next;
            BroadcastSanityIfNeeded();
            RaiseDownwardThresholdEvents(previous, Current);
        }

        private void BroadcastSanityIfNeeded()
        {
            if (Mathf.Abs(Current - _lastBroadcastSanity) < _sanityBroadcastThreshold)
            {
                return;
            }

            _lastBroadcastSanity = Current;
            GameEvents.RaiseSanityChanged(Current);
        }

        private void RaiseDownwardThresholdEvents(float previous, float current)
        {
            if (!_hasRaisedCriticalThisRound && previous >= _huntThreshold && current < _huntThreshold)
            {
                _hasRaisedCriticalThisRound = true;
                GameEvents.RaiseSanityCritical();
            }

            // 只根据本次下降方向判断，因此回升越过阈值不会触发，再次下降时可以重触发。
            for (float threshold = MaximumSanity - SanityThresholdStep;
                 threshold > MinimumSanity;
                 threshold -= SanityThresholdStep)
            {
                if (previous >= threshold && current < threshold)
                {
                    GameEvents.RaiseSanityThresholdCrossed(threshold);
                }
            }
        }

        private bool IsInsideSafeZone()
        {
            return _safeZone != null && _safeZone.bounds.Contains(transform.position);
        }

        private IEnumerator LightCheckRoutine()
        {
            while (isActiveAndEnabled && _useLightProbe)
            {
                RefreshLightState();
                yield return _lightCheckWait;
            }

            _lightCheckCoroutine = null;
        }

        private void RefreshLightState()
        {
            _isInLitRoom = false;
            if (_lightOverlapBuffer == null)
            {
                return;
            }

            int hitCount = Physics.OverlapSphereNonAlloc(
                transform.position,
                _lightCheckRadius,
                _lightOverlapBuffer,
                _lightDetectionLayers,
                QueryTriggerInteraction.Collide);

            if (!_lightBufferWarningLogged && hitCount >= _lightOverlapBuffer.Length)
            {
                _lightBufferWarningLogged = true;
                Debug.LogWarning(
                    "PlayerSanity 的非分配光源检测缓冲区已占满，本次检测可能遗漏 Light；请调大缓冲区大小。",
                    this);
            }

            for (int index = 0; index < hitCount; index++)
            {
                Collider candidate = _lightOverlapBuffer[index];
                if (candidate == null)
                {
                    continue;
                }

                // 手持灯属于玩家自身，只提供手电倍率，不能同时被当作房间光源重复减免。
                if (candidate.transform == transform || candidate.transform.IsChildOf(transform))
                {
                    continue;
                }

                Light nearbyLight = FindAssociatedLight(candidate);
                if (nearbyLight != null && nearbyLight.isActiveAndEnabled)
                {
                    _isInLitRoom = true;
                    return;
                }
            }
        }

        private static Light FindAssociatedLight(Collider candidate)
        {
            if (candidate.TryGetComponent(out Light light))
            {
                return light;
            }

            light = candidate.GetComponentInParent<Light>();
            return light != null ? light : candidate.GetComponentInChildren<Light>(true);
        }

        private void StartLightCheckCoroutine()
        {
            if (!isActiveAndEnabled || !_useLightProbe || _lightCheckCoroutine != null)
            {
                return;
            }

            _lightCheckCoroutine = StartCoroutine(LightCheckRoutine());
        }

        private void RestartLightCheckCoroutine()
        {
            StopLightCheckCoroutine();
            StartLightCheckCoroutine();
        }

        private void StopLightCheckCoroutine()
        {
            if (_lightCheckCoroutine == null)
            {
                return;
            }

            StopCoroutine(_lightCheckCoroutine);
            _lightCheckCoroutine = null;
        }

        private void ResetRuntimeState(bool broadcastCurrent)
        {
            Current = Mathf.Clamp(_startingSanity, MinimumSanity, MaximumSanity);
            _lastBroadcastSanity = Current;
            _isInLitRoom = false;
            _isFlashlightOn = false;
            _isHuntActive = false;
            _hasRaisedCriticalThisRound = false;
            _lightBufferWarningLogged = false;

            if (broadcastCurrent)
            {
                GameEvents.RaiseSanityChanged(Current);
            }
        }

        private void AllocateLightBuffer()
        {
            _lightOverlapBuffer = new Collider[_lightOverlapBufferSize];
        }

        private void CacheLightCheckWait()
        {
            _lightCheckWait = new WaitForSeconds(_lightCheckInterval);
        }

        private void ValidateSettings()
        {
            _startingSanity = Mathf.Clamp(_startingSanity, MinimumSanity, MaximumSanity);
            _darkDecayRate = Mathf.Max(MinimumSanity, _darkDecayRate);
            _litRoomDecayRate = Mathf.Max(MinimumSanity, _litRoomDecayRate);
            _flashlightRateMultiplier = Mathf.Max(MinimumSanity, _flashlightRateMultiplier);
            _ghostEventSanityLoss = Mathf.Max(MinimumSanity, _ghostEventSanityLoss);
            _huntAdditionalDecayRate = Mathf.Max(MinimumSanity, _huntAdditionalDecayRate);
            _safeZoneRecoveryRate = Mathf.Max(MinimumSanity, _safeZoneRecoveryRate);
            _huntThreshold = Mathf.Clamp(_huntThreshold, MinimumSanity, MaximumSanity);
            _sanityBroadcastThreshold = Mathf.Max(MinimumSanity, _sanityBroadcastThreshold);
            _lightCheckInterval = Mathf.Max(Mathf.Epsilon, _lightCheckInterval);
            _lightCheckRadius = Mathf.Max(MinimumSanity, _lightCheckRadius);
            _lightOverlapBufferSize = Mathf.Max(MinimumBufferSize, _lightOverlapBufferSize);
            _ghostEventEffectiveDistance = Mathf.Max(MinimumSanity, _ghostEventEffectiveDistance);
        }
    }
}
