using System.Collections;
using Residuum.Core;
using TMPro;
using UnityEngine;

namespace Residuum.Evidence
{
    [DisallowMultipleComponent]
    public sealed class EMFReader : MonoBehaviour, IHoldable
    {
        private const string DisplayName = "EMF 读数器";
        private const int IdleReading = 0;
        private const int MinimumActiveReading = 1;
        private const int MaximumEvidenceReading = 5;
        private const int MaximumNonEvidenceReading = 4;
        private const float MinimumPositiveSetting = 0.01f;

        private static readonly int EmissionColorProperty = Shader.PropertyToID("_EmissionColor");

        [Header("检测")]
        [Tooltip("鬼与物体互动时的检测半径，单位：米。")]
        [SerializeField] private float _detectionRadius = 6f;

        [Tooltip("一次有效互动产生的读数持续时长，单位：秒。")]
        [SerializeField] private float _readingDuration = 8f;

        [Tooltip("设备与互动位置在此距离以内时给出本回合允许的最高级读数，单位：米。")]
        [SerializeField] private float _highestLevelDistance = 2f;

        [Tooltip("持续读数期间重新计算距离与等级的间隔，单位：秒。")]
        [SerializeField] private float _readingRefreshInterval = 0.25f;

        [Header("手持模型显示（均可为空）")]
        [Tooltip("手持模型上的读数文字；留空时不显示文字。")]
        [SerializeField] private TextMeshPro _readingLabel;

        [Tooltip("按数组顺序代表各级读数的指示灯 Renderer；材质需预先启用 Emission，留空时不显示指示灯。")]
        [SerializeField] private Renderer[] _indicatorLights;

        public string ItemName => DisplayName;
        public int Reading { get; private set; }

        private MaterialPropertyBlock _materialPropertyBlock;
        private Color[] _indicatorOnEmissionColors;
        private WaitForSeconds _refreshWait;
        private Coroutine _readingCoroutine;
        private Vector3 _interactionPosition;
        private float _readingEndTime;
        private bool _isEquipped;
        private bool _evidenceReportedThisRound;
        private bool _missingDisplayWarningLogged;

        private void Awake()
        {
            ValidateSettings();
            _refreshWait = new WaitForSeconds(_readingRefreshInterval);
            _materialPropertyBlock = new MaterialPropertyBlock();
            CacheIndicatorEmissionColors();
            UpdateDisplay();
            WarnIfDisplayMissing();
        }

        private void OnEnable()
        {
            GameEvents.OnGhostInteract += HandleGhostInteract;
            GameEvents.OnRoundStart += HandleRoundStart;
        }

        private void OnDisable()
        {
            GameEvents.OnGhostInteract -= HandleGhostInteract;
            GameEvents.OnRoundStart -= HandleRoundStart;
            _isEquipped = false;
            StopReading();
        }

        private void OnDestroy()
        {
            // 防御性退订，避免禁用流程被外部销毁顺序打断时留下静态委托。
            GameEvents.OnGhostInteract -= HandleGhostInteract;
            GameEvents.OnRoundStart -= HandleRoundStart;
            StopAllCoroutines();
            _readingCoroutine = null;
            _refreshWait = null;
            _materialPropertyBlock = null;
            _indicatorOnEmissionColors = null;
        }

        private void OnValidate()
        {
            ValidateSettings();
        }

        public void OnEquip()
        {
            _isEquipped = true;
            WarnIfDisplayMissing();
            UpdateDisplay();
        }

        public void OnUnequip()
        {
            _isEquipped = false;
            StopReading();
        }

        public void OnPrimaryUse()
        {
            // EMF 读数器是被动设备，主使用键不触发额外行为。
        }

        private void HandleGhostInteract(Vector3 interactionPosition)
        {
            if (!_isEquipped)
            {
                return;
            }

            float distance = Vector3.Distance(transform.position, interactionPosition);
            if (distance > _detectionRadius)
            {
                return;
            }

            _interactionPosition = interactionPosition;
            _readingEndTime = Time.time + _readingDuration;
            RefreshReading();

            if (_readingCoroutine == null)
            {
                _readingCoroutine = StartCoroutine(ReadingRoutine());
            }
        }

        private void HandleRoundStart()
        {
            _evidenceReportedThisRound = false;
            StopReading();
        }

        private IEnumerator ReadingRoutine()
        {
            while (Time.time < _readingEndTime)
            {
                RefreshReading();
                yield return _refreshWait;
            }

            _readingCoroutine = null;
            SetReading(IdleReading);
        }

        private void RefreshReading()
        {
            float distance = Vector3.Distance(transform.position, _interactionPosition);
            SetReading(CalculateReading(distance));
        }

        private int CalculateReading(float distance)
        {
            int maximumReading = GameEvents.GhostHasEMF5
                ? MaximumEvidenceReading
                : MaximumNonEvidenceReading;

            if (distance <= _highestLevelDistance)
            {
                return maximumReading;
            }

            float proximity = Mathf.InverseLerp(_detectionRadius, _highestLevelDistance, distance);
            int additionalLevels = Mathf.FloorToInt(
                proximity * (maximumReading - MinimumActiveReading));

            return Mathf.Clamp(
                MinimumActiveReading + additionalLevels,
                MinimumActiveReading,
                maximumReading);
        }

        private void SetReading(int level)
        {
            if (Reading == level)
            {
                return;
            }

            bool shouldReportEvidence = level == MaximumEvidenceReading
                && GameEvents.GhostHasEMF5
                && !_evidenceReportedThisRound;

            if (shouldReportEvidence)
            {
                // 先锁定本回合标志，避免事件监听方重入时重复上报。
                _evidenceReportedThisRound = true;
            }

            Reading = level;
            UpdateDisplay();
            GameEvents.RaiseEMFReadingChanged(level);

            if (shouldReportEvidence)
            {
                GameEvents.RaiseEvidenceFound(EvidenceType.EMF5);
            }
        }

        private void StopReading()
        {
            StopAllCoroutines();
            _readingCoroutine = null;
            SetReading(IdleReading);
        }

        private void UpdateDisplay()
        {
            if (_readingLabel != null)
            {
                _readingLabel.text = Reading.ToString();
            }

            if (_indicatorLights == null || _materialPropertyBlock == null)
            {
                return;
            }

            for (int index = 0; index < _indicatorLights.Length; index++)
            {
                Renderer indicator = _indicatorLights[index];
                if (indicator == null)
                {
                    continue;
                }

                Color emissionColor = index < Reading
                    ? GetIndicatorOnEmissionColor(index)
                    : Color.black;

                indicator.GetPropertyBlock(_materialPropertyBlock);
                _materialPropertyBlock.SetColor(EmissionColorProperty, emissionColor);
                indicator.SetPropertyBlock(_materialPropertyBlock);
                _materialPropertyBlock.Clear();
            }
        }

        private void CacheIndicatorEmissionColors()
        {
            if (_indicatorLights == null)
            {
                _indicatorOnEmissionColors = null;
                return;
            }

            _indicatorOnEmissionColors = new Color[_indicatorLights.Length];
            for (int index = 0; index < _indicatorLights.Length; index++)
            {
                Renderer indicator = _indicatorLights[index];
                Material sharedMaterial = indicator != null ? indicator.sharedMaterial : null;

                Color emissionColor = sharedMaterial != null
                    && sharedMaterial.HasProperty(EmissionColorProperty)
                    ? sharedMaterial.GetColor(EmissionColorProperty)
                    : Color.white;

                _indicatorOnEmissionColors[index] = emissionColor.maxColorComponent > Mathf.Epsilon
                    ? emissionColor
                    : Color.white;
            }
        }

        private Color GetIndicatorOnEmissionColor(int index)
        {
            if (_indicatorOnEmissionColors == null || index >= _indicatorOnEmissionColors.Length)
            {
                return Color.white;
            }

            return _indicatorOnEmissionColors[index];
        }

        private void WarnIfDisplayMissing()
        {
            if (_missingDisplayWarningLogged || _readingLabel != null || HasIndicatorLight())
            {
                return;
            }

            _missingDisplayWarningLogged = true;
            Debug.LogWarning(
                "EMFReader 未指定 TextMeshPro 读数或指示灯 Renderer，将只广播读数事件。",
                this);
        }

        private bool HasIndicatorLight()
        {
            if (_indicatorLights == null)
            {
                return false;
            }

            foreach (Renderer indicator in _indicatorLights)
            {
                if (indicator != null)
                {
                    return true;
                }
            }

            return false;
        }

        private void ValidateSettings()
        {
            _detectionRadius = Mathf.Max(_detectionRadius, MinimumPositiveSetting);
            _readingDuration = Mathf.Max(_readingDuration, MinimumPositiveSetting);
            _highestLevelDistance = Mathf.Clamp(
                _highestLevelDistance,
                MinimumPositiveSetting,
                _detectionRadius);
            _readingRefreshInterval = Mathf.Max(
                _readingRefreshInterval,
                MinimumPositiveSetting);
        }
    }
}
