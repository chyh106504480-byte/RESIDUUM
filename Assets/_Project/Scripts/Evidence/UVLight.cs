using System.Collections;
using Residuum.Core;
using UnityEngine;

namespace Residuum.Evidence
{
    [DisallowMultipleComponent]
    public sealed class UVLight : MonoBehaviour, IHoldable
    {
        private const string DisplayName = "紫外线灯";
        private const float MaximumSpotAngle = 179f;
        private const int MinimumBufferSize = 1;

        [Header("依赖")]
        [Tooltip("子物体上的紫色 URP Spot Light。")]
        [SerializeField] private Light _spotLight;

        [Header("光照")]
        [Tooltip("紫外线灯的灯光颜色。")]
        [SerializeField] private Color _lightColor = new Color(0.35f, 0f, 1f, 1f);

        [Tooltip("满电时紫外线 Spot Light 的光照强度。低电量时会从此强度逐步变暗。")]
        [SerializeField] private float _lightIntensity = 5f;

        [Tooltip("紫外线 Spot Light 的完整外锥角，单位：度。指纹检测使用相同锥角。")]
        [SerializeField] private float _coneAngle = 45f;

        [Header("指纹检测")]
        [Tooltip("紫外线灯检测指纹的最大半径，单位：米。")]
        [SerializeField] private float _detectionRadius = 4f;

        [Tooltip("两次指纹检测之间的间隔，单位：秒。")]
        [SerializeField] private float _detectionInterval = 0.2f;

        [Tooltip("参与指纹检测的物理层。默认检测全部层。")]
        [SerializeField] private LayerMask _detectionLayers = ~0;

        [Tooltip("非分配物理检测使用的 Collider 缓冲区大小。场景密集时可适当调大。")]
        [SerializeField] private int _overlapBufferSize = 32;

        [Header("电量")]
        [Tooltip("紫外线灯从满电持续开启到耗尽的秒数。")]
        [SerializeField] private float _fullBatterySeconds = 180f;

        [Tooltip("电量低于此归一化值时，紫外线灯开始随剩余电量逐步变暗。")]
        [SerializeField] private float _lowBatteryThreshold = 0.2f;

        [Header("手电互斥")]
        [Tooltip("装备时传 true、卸下时传 false；请在 Inspector 中连接到 Flashlight.ForceOff。")]
        public UnityEngine.Events.UnityEvent<bool> onRequestFlashlightLock =
            new UnityEngine.Events.UnityEvent<bool>();

        public string ItemName => DisplayName;

        private Collider[] _overlapBuffer;
        private WaitForSeconds _detectionWait;
        private Coroutine _detectionCoroutine;
        private float _batterySecondsRemaining;
        private bool _isInitialized;
        private bool _isEquipped;
        private bool _isOn;
        private bool _evidenceReportedThisRound;
        private bool _bufferWarningLogged;

        private void Awake()
        {
            ValidateSettings();

            if (_spotLight == null)
            {
                Debug.LogError("UVLight 未指定子物体上的紫色 URP Spot Light，组件已禁用。", this);
                enabled = false;
                return;
            }

            _batterySecondsRemaining = _fullBatterySeconds;
            ApplyLightSettings();
            _overlapBuffer = new Collider[_overlapBufferSize];
            _detectionWait = new WaitForSeconds(_detectionInterval);
            SetLightEnabled(false);
            _isInitialized = true;
        }

        private void OnEnable()
        {
            if (!_isInitialized)
            {
                return;
            }

            GameEvents.OnRoundStart += HandleRoundStart;
        }

        private void OnDisable()
        {
            GameEvents.OnRoundStart -= HandleRoundStart;

            if (_isEquipped)
            {
                onRequestFlashlightLock?.Invoke(false);
            }

            _isEquipped = false;
            TurnOff();
        }

        private void OnDestroy()
        {
            GameEvents.OnRoundStart -= HandleRoundStart;

            if (_isEquipped)
            {
                onRequestFlashlightLock?.Invoke(false);
            }

            _isEquipped = false;
            StopAllCoroutines();
            _detectionCoroutine = null;
            SetLightEnabled(false);
            _overlapBuffer = null;
            _detectionWait = null;
        }

        private void OnValidate()
        {
            ValidateSettings();

            if (_spotLight != null)
            {
                ApplyLightSettings();
            }
        }

        private void Update()
        {
            if (!_isInitialized || !_isEquipped || !_isOn)
            {
                return;
            }

            _batterySecondsRemaining = Mathf.Max(
                0f,
                _batterySecondsRemaining - Time.deltaTime);
            ApplyCurrentIntensity();

            if (_batterySecondsRemaining <= 0f)
            {
                TurnOff();
            }
        }

        public void OnEquip()
        {
            _isEquipped = true;
            TurnOff();

            // 装备即锁死普通手电，保证玩家在打开 UV 前后都处于预期的黑暗风险中。
            onRequestFlashlightLock?.Invoke(true);
        }

        public void OnUnequip()
        {
            _isEquipped = false;
            TurnOff();
            onRequestFlashlightLock?.Invoke(false);
        }

        public void OnPrimaryUse()
        {
            if (!_isInitialized || !_isEquipped)
            {
                return;
            }

            if (_isOn)
            {
                TurnOff();
                return;
            }

            if (_batterySecondsRemaining <= 0f)
            {
                return;
            }

            TurnOn();
        }

        private void HandleRoundStart()
        {
            _evidenceReportedThisRound = false;
        }

        private void TurnOn()
        {
            StopDetection();
            _isOn = true;
            ApplyCurrentIntensity();
            SetLightEnabled(true);
            _detectionCoroutine = StartCoroutine(DetectionRoutine());
        }

        private void TurnOff()
        {
            _isOn = false;
            StopDetection();
            SetLightEnabled(false);
        }

        private IEnumerator DetectionRoutine()
        {
            while (_isEquipped && _isOn && _batterySecondsRemaining > 0f)
            {
                DetectFingerprints();
                yield return _detectionWait;
            }

            _detectionCoroutine = null;
        }

        private void StopDetection()
        {
            StopAllCoroutines();
            _detectionCoroutine = null;
        }

        private void DetectFingerprints()
        {
            if (_spotLight == null || _overlapBuffer == null)
            {
                return;
            }

            Transform lightTransform = _spotLight.transform;
            int hitCount = Physics.OverlapSphereNonAlloc(
                lightTransform.position,
                _detectionRadius,
                _overlapBuffer,
                _detectionLayers,
                QueryTriggerInteraction.Collide);

            if (!_bufferWarningLogged && hitCount >= _overlapBuffer.Length)
            {
                _bufferWarningLogged = true;
                Debug.LogWarning(
                    "UVLight 的非分配检测缓冲区已占满，本次检测可能遗漏 Collider；请调大缓冲区大小。",
                    this);
            }

            for (int index = 0; index < hitCount; index++)
            {
                Collider candidate = _overlapBuffer[index];
                if (candidate == null)
                {
                    continue;
                }

                Fingerprint fingerprint = FindFingerprint(candidate);
                if (fingerprint == null || !fingerprint.IsActive
                    || !IsInsideLightCone(fingerprint.transform.position, lightTransform))
                {
                    continue;
                }

                fingerprint.Reveal();
                ReportEvidenceOncePerRound();
            }
        }

        private static Fingerprint FindFingerprint(Collider candidate)
        {
            Fingerprint fingerprint = candidate.GetComponent<Fingerprint>();
            if (fingerprint != null)
            {
                return fingerprint;
            }

            fingerprint = candidate.GetComponentInParent<Fingerprint>();
            return fingerprint != null
                ? fingerprint
                : candidate.GetComponentInChildren<Fingerprint>(true);
        }

        private bool IsInsideLightCone(Vector3 targetPosition, Transform lightTransform)
        {
            Vector3 toTarget = targetPosition - lightTransform.position;
            if (toTarget.sqrMagnitude <= Mathf.Epsilon)
            {
                return true;
            }

            float halfAngleRadians = _coneAngle * Mathf.Deg2Rad * 0.5f;
            float minimumDot = Mathf.Cos(halfAngleRadians);
            float targetDot = Vector3.Dot(lightTransform.forward, toTarget.normalized);
            return targetDot >= minimumDot;
        }

        private void ReportEvidenceOncePerRound()
        {
            if (_evidenceReportedThisRound)
            {
                return;
            }

            // 先锁定标志，避免事件监听方重入时重复上报。
            _evidenceReportedThisRound = true;
            Debug.Log("UVLight 发现紫外线指纹证据。", this);
            GameEvents.RaiseEvidenceFound(EvidenceType.UVFingerprint);
        }

        private void ApplyLightSettings()
        {
            _spotLight.type = LightType.Spot;
            _spotLight.color = _lightColor;
            _spotLight.range = _detectionRadius;
            _spotLight.spotAngle = _coneAngle;
            ApplyCurrentIntensity();
        }

        private void ApplyCurrentIntensity()
        {
            if (_spotLight == null)
            {
                return;
            }

            float normalizedBattery = GetNormalizedBattery();
            float intensityMultiplier = normalizedBattery >= _lowBatteryThreshold
                || _lowBatteryThreshold <= Mathf.Epsilon
                ? 1f
                : Mathf.Clamp01(normalizedBattery / _lowBatteryThreshold);

            _spotLight.intensity = _lightIntensity * intensityMultiplier;
        }

        private float GetNormalizedBattery()
        {
            return _fullBatterySeconds > Mathf.Epsilon
                ? Mathf.Clamp01(_batterySecondsRemaining / _fullBatterySeconds)
                : 0f;
        }

        private void SetLightEnabled(bool isEnabled)
        {
            if (_spotLight != null)
            {
                _spotLight.enabled = isEnabled;
            }
        }

        private void ValidateSettings()
        {
            _lightIntensity = Mathf.Max(0f, _lightIntensity);
            _coneAngle = Mathf.Clamp(_coneAngle, Mathf.Epsilon, MaximumSpotAngle);
            _detectionRadius = Mathf.Max(Mathf.Epsilon, _detectionRadius);
            _detectionInterval = Mathf.Max(Mathf.Epsilon, _detectionInterval);
            _overlapBufferSize = Mathf.Max(MinimumBufferSize, _overlapBufferSize);
            _fullBatterySeconds = Mathf.Max(Mathf.Epsilon, _fullBatterySeconds);
            _lowBatteryThreshold = Mathf.Clamp01(_lowBatteryThreshold);
        }
    }
}
