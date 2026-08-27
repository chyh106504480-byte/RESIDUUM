using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Residuum.Core;
using Residuum.Evidence;

namespace Residuum.UI
{
    /// <summary>
    /// 仅通过 GameEvents 接收状态并刷新 HUD，不持有任何玩法模块的具体引用。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class HUDController : MonoBehaviour
    {
        private const int EvidenceCount = 3;

        [Header("显示开关")]
        [Tooltip("是否显示准星与交互提示。")]
        [SerializeField] private bool _showCrosshairAndPrompt = true;
        [Tooltip("是否显示理智条。")]
        [SerializeField] private bool _showSanity = true;
        [Tooltip("是否显示证据清单。")]
        [SerializeField] private bool _showEvidence = true;
        [Tooltip("是否显示当前道具与电量。")]
        [SerializeField] private bool _showItemAndBattery = true;
        [Tooltip("是否显示温度读数。")]
        [SerializeField] private bool _showTemperature = true;
        [Tooltip("是否显示猎杀暗角。")]
        [SerializeField] private bool _showHuntVignette = true;

        [Header("准星与交互提示")]
        [Tooltip("常驻准星图形。")]
        [SerializeField] private Graphic _crosshair;
        [Tooltip("准星下方的交互提示文字。")]
        [SerializeField] private TextMeshProUGUI _promptLabel;
        [Tooltip("没有可交互目标时的准星颜色。")]
        [SerializeField] private Color _crosshairNormalColor = Color.white;
        [Tooltip("指向可交互目标时的准星高亮颜色。")]
        [SerializeField] private Color _crosshairHighlightColor = new Color(1f, 0.75f, 0.2f, 1f);

        [Header("理智条")]
        [Tooltip("Filled 类型的理智条填充图片。")]
        [SerializeField] private Image _sanityFill;
        [Tooltip("理智满值，用于把当前理智换算为填充比例。")]
        [Min(0.01f)]
        [SerializeField] private float _maximumSanity = 100f;
        [Tooltip("理智颜色渐变的中间值。")]
        [Min(0f)]
        [SerializeField] private float _sanityColorMidpoint = 50f;
        [Tooltip("理智满值时的颜色。")]
        [SerializeField] private Color _sanityHighColor = Color.white;
        [Tooltip("理智处于中间值时的琥珀色。")]
        [SerializeField] private Color _sanityMiddleColor = new Color(1f, 0.55f, 0.08f, 1f);
        [Tooltip("理智归零时的深红色。")]
        [SerializeField] private Color _sanityLowColor = new Color(0.45f, 0.02f, 0.02f, 1f);
        [Tooltip("理智低于此值时开始透明度脉动。")]
        [Min(0f)]
        [SerializeField] private float _sanityPulseThreshold = 25f;
        [Tooltip("低理智透明度脉动完成一次循环所需的秒数。")]
        [Min(0.01f)]
        [SerializeField] private float _sanityPulsePeriod = 1.2f;
        [Tooltip("低理智脉动时的最低透明度比例。")]
        [Range(0f, 1f)]
        [SerializeField] private float _sanityPulseMinimumAlpha = 0.45f;

        [Header("证据清单")]
        [Tooltip("三项证据的文字行，顺序为 EMF-5、紫外线指纹、鬼影书写。")]
        [SerializeField] private TextMeshProUGUI[] _evidenceLabels = new TextMeshProUGUI[EvidenceCount];
        [Tooltip("三项证据的显示名称，顺序必须与证据文字行一致。")]
        [SerializeField] private string[] _evidenceNames = { "EMF-5", "紫外线指纹", "鬼影书写" };
        [Tooltip("已发现证据显示在名称前的标记。")]
        [SerializeField] private string _evidenceFoundPrefix = "✓ ";
        [Tooltip("尚未发现证据时的文字颜色。")]
        [SerializeField] private Color _evidenceHiddenColor = new Color(0.45f, 0.45f, 0.45f, 1f);
        [Tooltip("已经发现证据时的文字颜色。")]
        [SerializeField] private Color _evidenceFoundColor = Color.white;

        [Header("当前道具与电量")]
        [Tooltip("当前槽位号与物品名文字。")]
        [SerializeField] private TextMeshProUGUI _itemLabel;
        [Tooltip("手电电量条的填充图片。")]
        [SerializeField] private Image _batteryFill;
        [Tooltip("把事件中的零基槽位索引转换为玩家看到的槽位号时所加的数值。")]
        [SerializeField] private int _slotDisplayOffset = 1;
        [Tooltip("空槽位显示的物品名。")]
        [SerializeField] private string _emptyItemName = "空";
        [Tooltip("最后一次收到电量更新后，电量条保持显示的秒数。")]
        [Min(0f)]
        [SerializeField] private float _batteryHideDelay = 2f;
        [Tooltip("电量条从可见淡出到隐藏所需的秒数。")]
        [Min(0f)]
        [SerializeField] private float _batteryFadeDuration = 0.35f;

        [Header("温度读数")]
        [Tooltip("玩家当前位置的摄氏温度文字。")]
        [SerializeField] private TextMeshProUGUI _temperatureLabel;
        [Tooltip("温度低于此摄氏度时切换为冷色。")]
        [SerializeField] private float _coldTemperatureThreshold = 2f;
        [Tooltip("普通温度时的文字颜色。")]
        [SerializeField] private Color _temperatureNormalColor = Color.white;
        [Tooltip("接近鬼房的低温提示颜色。")]
        [SerializeField] private Color _temperatureColdColor = new Color(0.35f, 0.8f, 1f, 1f);

        [Header("猎杀视觉")]
        [Tooltip("覆盖画面的猎杀暗角 UI 图形。")]
        [SerializeField] private Graphic _huntVignette;
        [Tooltip("猎杀暗角的最高透明度。")]
        [Range(0f, 1f)]
        [SerializeField] private float _huntVignetteIntensity = 0.65f;
        [Tooltip("猎杀暗角淡入或淡出所需的秒数。")]
        [Min(0f)]
        [SerializeField] private float _huntFadeDuration = 0.4f;
        [Tooltip("猎杀暗角完成一次脉动所需的秒数。")]
        [Min(0.01f)]
        [SerializeField] private float _huntPulsePeriod = 0.8f;
        [Tooltip("猎杀暗角脉动时相对于最高强度的最低比例。")]
        [Range(0f, 1f)]
        [SerializeField] private float _huntPulseMinimumMultiplier = 0.6f;

        private readonly HashSet<string> _warnedFields = new HashSet<string>();
        private bool[] _evidenceFound;
        private WaitForSeconds _batteryHideWait;
        private Coroutine _batteryFadeCoroutine;
        private Color _batteryBaseColor = Color.white;
        private Color _huntVignetteBaseColor = Color.white;
        private float _currentSanity;
        private float _currentBattery;
        private float _currentTemperature;
        private float _huntFadeAmount;
        private int _currentSlotIndex;
        private string _currentPrompt;
        private string _currentItemName;
        private bool _hasSlotUpdate;
        private bool _hasBatteryUpdate;
        private bool _hasTemperatureUpdate;
        private bool _isHunting;
        private bool _roundActive = true;

        private void Awake()
        {
            _evidenceFound = new bool[EvidenceCount];
            _currentSanity = _maximumSanity;
            _batteryHideWait = new WaitForSeconds(Mathf.Max(0f, _batteryHideDelay));

            if (_batteryFill != null)
            {
                _batteryBaseColor = _batteryFill.color;
            }

            if (_huntVignette != null)
            {
                _huntVignetteBaseColor = _huntVignette.color;
                SetGraphicAlpha(_huntVignette, 0f);
                _huntVignette.enabled = false;
            }
        }

        private void OnEnable()
        {
            GameEvents.OnSanityChanged += HandleSanityChanged;
            GameEvents.OnEvidenceFound += HandleEvidenceFound;
            GameEvents.OnInteractPromptChanged += HandleInteractPromptChanged;
            GameEvents.OnSlotChanged += HandleSlotChanged;
            GameEvents.OnBatteryChanged += HandleBatteryChanged;
            GameEvents.OnPlayerTemperatureChanged += HandleTemperatureChanged;
            GameEvents.OnHuntStart += HandleHuntStart;
            GameEvents.OnHuntEnd += HandleHuntEnd;
            GameEvents.OnRoundStart += HandleRoundStart;
            GameEvents.OnRoundEnd += HandleRoundEnd;

            RefreshAll();
        }

        private void OnDisable()
        {
            GameEvents.OnSanityChanged -= HandleSanityChanged;
            GameEvents.OnEvidenceFound -= HandleEvidenceFound;
            GameEvents.OnInteractPromptChanged -= HandleInteractPromptChanged;
            GameEvents.OnSlotChanged -= HandleSlotChanged;
            GameEvents.OnBatteryChanged -= HandleBatteryChanged;
            GameEvents.OnPlayerTemperatureChanged -= HandleTemperatureChanged;
            GameEvents.OnHuntStart -= HandleHuntStart;
            GameEvents.OnHuntEnd -= HandleHuntEnd;
            GameEvents.OnRoundStart -= HandleRoundStart;
            GameEvents.OnRoundEnd -= HandleRoundEnd;

            StopAllCoroutines();
            _batteryFadeCoroutine = null;
            HideAll();
        }

        private void OnDestroy()
        {
            StopAllCoroutines();
            _batteryFadeCoroutine = null;
            _batteryHideWait = null;
            _warnedFields.Clear();
        }

        private void Update()
        {
            if (!_roundActive)
            {
                return;
            }

            UpdateSanityPulse();
            UpdateHuntVignette();
        }

        private void HandleSanityChanged(float sanity)
        {
            _currentSanity = Mathf.Clamp(sanity, 0f, Mathf.Max(_maximumSanity, Mathf.Epsilon));
            RefreshSanity();
        }

        private void HandleEvidenceFound(EvidenceType evidenceType)
        {
            int index = GetEvidenceIndex(evidenceType);
            if (index < 0 || index >= EvidenceCount)
            {
                return;
            }

            _evidenceFound[index] = true;
            RefreshEvidenceLabel(index);
        }

        private void HandleInteractPromptChanged(string prompt)
        {
            _currentPrompt = prompt;
            RefreshCrosshairAndPrompt();
        }

        private void HandleSlotChanged(int slotIndex, string itemName)
        {
            _currentSlotIndex = slotIndex;
            _currentItemName = itemName;
            _hasSlotUpdate = true;

            // 切换槽位后按“已收到电量更新且当前物品非空”的契约重新判断显示状态。
            StopBatteryFade();
            RefreshItemLabel();
            RefreshBattery();
        }

        private void HandleBatteryChanged(float normalizedBattery)
        {
            _currentBattery = Mathf.Clamp01(normalizedBattery);
            _hasBatteryUpdate = true;
            RefreshBattery();
        }

        private void HandleTemperatureChanged(float celsius)
        {
            _currentTemperature = celsius;
            _hasTemperatureUpdate = true;
            RefreshTemperature();
        }

        private void HandleHuntStart(float duration)
        {
            _ = duration;
            _isHunting = true;
            RefreshHuntVignette();
        }

        private void HandleHuntEnd()
        {
            _isHunting = false;
            RefreshHuntVignette();
        }

        private void HandleRoundStart()
        {
            _roundActive = true;
            _currentSanity = _maximumSanity;
            _currentPrompt = null;
            _currentItemName = null;
            _hasSlotUpdate = false;
            _hasBatteryUpdate = false;
            _hasTemperatureUpdate = false;
            _isHunting = false;
            _huntFadeAmount = 0f;
            ResetEvidence();
            StopBatteryFade();
            RefreshAll();
        }

        private void HandleRoundEnd(RoundResult result)
        {
            _ = result;
            _roundActive = false;
            _isHunting = false;
            _huntFadeAmount = 0f;
            StopBatteryFade();
            HideAll();
        }

        private void RefreshAll()
        {
            if (!_roundActive)
            {
                HideAll();
                return;
            }

            RefreshCrosshairAndPrompt();
            RefreshSanity();
            RefreshEvidence();
            RefreshItemLabel();
            RefreshBattery();
            RefreshTemperature();
            RefreshHuntVignette();
        }

        private void RefreshCrosshairAndPrompt()
        {
            if (!_roundActive || !_showCrosshairAndPrompt)
            {
                SetGraphicEnabled(_crosshair, false);
                SetGraphicEnabled(_promptLabel, false);
                return;
            }

            bool hasPrompt = !string.IsNullOrEmpty(_currentPrompt);
            if (_crosshair == null)
            {
                WarnMissingOnce(nameof(_crosshair));
            }
            else
            {
                _crosshair.enabled = true;
                _crosshair.color = hasPrompt ? _crosshairHighlightColor : _crosshairNormalColor;
            }

            if (!hasPrompt)
            {
                SetGraphicEnabled(_promptLabel, false);
                return;
            }

            if (_promptLabel == null)
            {
                WarnMissingOnce(nameof(_promptLabel));
                return;
            }

            _promptLabel.text = _currentPrompt;
            _promptLabel.enabled = true;
        }

        private void RefreshSanity()
        {
            if (!_roundActive || !_showSanity)
            {
                SetGraphicEnabled(_sanityFill, false);
                return;
            }

            if (_sanityFill == null)
            {
                WarnMissingOnce(nameof(_sanityFill));
                return;
            }

            _sanityFill.enabled = true;
            _sanityFill.fillAmount = Mathf.Clamp01(_currentSanity / Mathf.Max(_maximumSanity, Mathf.Epsilon));
            ApplySanityColor(1f);
        }

        private void UpdateSanityPulse()
        {
            if (!_showSanity || _sanityFill == null || !_sanityFill.enabled)
            {
                return;
            }

            float alphaMultiplier = 1f;
            if (_currentSanity < _sanityPulseThreshold)
            {
                float pulse = GetPulse01(_sanityPulsePeriod);
                alphaMultiplier = Mathf.Lerp(_sanityPulseMinimumAlpha, 1f, pulse);
            }

            ApplySanityColor(alphaMultiplier);
        }

        private void ApplySanityColor(float alphaMultiplier)
        {
            float maximum = Mathf.Max(_maximumSanity, Mathf.Epsilon);
            float normalized = Mathf.Clamp01(_currentSanity / maximum);
            float midpoint = Mathf.Clamp01(_sanityColorMidpoint / maximum);
            Color color;

            if (normalized >= midpoint)
            {
                float upperBlend = Mathf.InverseLerp(midpoint, 1f, normalized);
                color = Color.Lerp(_sanityMiddleColor, _sanityHighColor, upperBlend);
            }
            else
            {
                float lowerBlend = Mathf.InverseLerp(0f, midpoint, normalized);
                color = Color.Lerp(_sanityLowColor, _sanityMiddleColor, lowerBlend);
            }

            color.a *= Mathf.Clamp01(alphaMultiplier);
            _sanityFill.color = color;
        }

        private void RefreshEvidence()
        {
            for (int index = 0; index < EvidenceCount; index++)
            {
                RefreshEvidenceLabel(index);
            }
        }

        private void RefreshEvidenceLabel(int index)
        {
            TextMeshProUGUI label = GetEvidenceLabel(index);
            if (!_roundActive || !_showEvidence)
            {
                SetGraphicEnabled(label, false);
                return;
            }

            if (label == null)
            {
                WarnMissingOnce($"{nameof(_evidenceLabels)}[{index}]");
                return;
            }

            bool found = _evidenceFound[index];
            string evidenceName = GetEvidenceName(index);
            label.text = found ? _evidenceFoundPrefix + evidenceName : evidenceName;
            label.color = found ? _evidenceFoundColor : _evidenceHiddenColor;
            label.enabled = true;
        }

        private TextMeshProUGUI GetEvidenceLabel(int index)
        {
            if (_evidenceLabels == null || index < 0 || index >= _evidenceLabels.Length)
            {
                return null;
            }

            return _evidenceLabels[index];
        }

        private string GetEvidenceName(int index)
        {
            if (_evidenceNames != null && index >= 0 && index < _evidenceNames.Length
                && !string.IsNullOrEmpty(_evidenceNames[index]))
            {
                return _evidenceNames[index];
            }

            WarnMissingOnce($"{nameof(_evidenceNames)}[{index}]");
            switch (index)
            {
                case 0:
                    return "EMF-5";
                case 1:
                    return "紫外线指纹";
                case 2:
                    return "鬼影书写";
                default:
                    return string.Empty;
            }
        }

        private void RefreshItemLabel()
        {
            if (!_roundActive || !_showItemAndBattery || !_hasSlotUpdate)
            {
                SetGraphicEnabled(_itemLabel, false);
                return;
            }

            if (_itemLabel == null)
            {
                WarnMissingOnce(nameof(_itemLabel));
                return;
            }

            string displayName = string.IsNullOrEmpty(_currentItemName) ? _emptyItemName : _currentItemName;
            _itemLabel.text = $"槽位 {_currentSlotIndex + _slotDisplayOffset} · {displayName}";
            _itemLabel.enabled = true;
        }

        private void RefreshBattery()
        {
            StopBatteryFade();

            if (!_roundActive || !_showItemAndBattery || !_hasBatteryUpdate
                || string.IsNullOrEmpty(_currentItemName))
            {
                HideBatteryFill();
                return;
            }

            if (_batteryFill == null)
            {
                WarnMissingOnce(nameof(_batteryFill));
                return;
            }

            _batteryFill.fillAmount = _currentBattery;
            _batteryFill.color = _batteryBaseColor;
            _batteryFill.enabled = true;
            _batteryFadeCoroutine = StartCoroutine(FadeBatteryAfterDelay());
        }

        private IEnumerator FadeBatteryAfterDelay()
        {
            yield return _batteryHideWait;

            float elapsed = 0f;
            while (elapsed < _batteryFadeDuration)
            {
                elapsed += Time.deltaTime;
                float fade = 1f - Mathf.Clamp01(elapsed / Mathf.Max(_batteryFadeDuration, Mathf.Epsilon));
                SetGraphicAlpha(_batteryFill, _batteryBaseColor.a * fade);
                yield return null;
            }

            _hasBatteryUpdate = false;
            HideBatteryFill();
            _batteryFadeCoroutine = null;
        }

        private void StopBatteryFade()
        {
            if (_batteryFadeCoroutine == null)
            {
                return;
            }

            StopCoroutine(_batteryFadeCoroutine);
            _batteryFadeCoroutine = null;
        }

        private void HideBatteryFill()
        {
            if (_batteryFill == null)
            {
                return;
            }

            _batteryFill.color = _batteryBaseColor;
            _batteryFill.enabled = false;
        }

        private void RefreshTemperature()
        {
            if (!_roundActive || !_showTemperature || !_hasTemperatureUpdate)
            {
                SetGraphicEnabled(_temperatureLabel, false);
                return;
            }

            if (_temperatureLabel == null)
            {
                WarnMissingOnce(nameof(_temperatureLabel));
                return;
            }

            _temperatureLabel.text = _currentTemperature.ToString("F1") + " °C";
            _temperatureLabel.color = _currentTemperature < _coldTemperatureThreshold
                ? _temperatureColdColor
                : _temperatureNormalColor;
            _temperatureLabel.enabled = true;
        }

        private void RefreshHuntVignette()
        {
            if (!_roundActive || !_showHuntVignette)
            {
                SetGraphicEnabled(_huntVignette, false);
                return;
            }

            if (!_isHunting && _huntFadeAmount <= 0f)
            {
                SetGraphicEnabled(_huntVignette, false);
                return;
            }

            if (_huntVignette == null)
            {
                WarnMissingOnce(nameof(_huntVignette));
                return;
            }

            _huntVignette.enabled = true;
            ApplyHuntVignetteAlpha();
        }

        private void UpdateHuntVignette()
        {
            if (!_showHuntVignette)
            {
                SetGraphicEnabled(_huntVignette, false);
                return;
            }

            float target = _isHunting ? 1f : 0f;
            if (_huntFadeDuration <= 0f)
            {
                _huntFadeAmount = target;
            }
            else
            {
                _huntFadeAmount = Mathf.MoveTowards(
                    _huntFadeAmount,
                    target,
                    Time.deltaTime / _huntFadeDuration);
            }

            RefreshHuntVignette();
        }

        private void ApplyHuntVignetteAlpha()
        {
            float pulseMultiplier = 1f;
            if (_isHunting)
            {
                pulseMultiplier = Mathf.Lerp(
                    _huntPulseMinimumMultiplier,
                    1f,
                    GetPulse01(_huntPulsePeriod));
            }

            float alpha = _huntVignetteIntensity * _huntFadeAmount * pulseMultiplier;
            SetGraphicAlpha(_huntVignette, Mathf.Clamp01(alpha));
        }

        private float GetPulse01(float period)
        {
            float safePeriod = Mathf.Max(period, Mathf.Epsilon);
            float angle = Time.time * (Mathf.PI * 2f) / safePeriod;
            return (Mathf.Sin(angle) + 1f) * 0.5f;
        }

        private void ResetEvidence()
        {
            for (int index = 0; index < EvidenceCount; index++)
            {
                _evidenceFound[index] = false;
            }
        }

        private int GetEvidenceIndex(EvidenceType evidenceType)
        {
            switch (evidenceType)
            {
                case EvidenceType.EMF5:
                    return 0;
                case EvidenceType.UVFingerprint:
                    return 1;
                case EvidenceType.GhostWriting:
                    return 2;
                default:
                    return -1;
            }
        }

        private void HideAll()
        {
            SetGraphicEnabled(_crosshair, false);
            SetGraphicEnabled(_promptLabel, false);
            SetGraphicEnabled(_sanityFill, false);
            SetGraphicEnabled(_itemLabel, false);
            HideBatteryFill();
            SetGraphicEnabled(_temperatureLabel, false);
            SetGraphicEnabled(_huntVignette, false);

            for (int index = 0; index < EvidenceCount; index++)
            {
                SetGraphicEnabled(GetEvidenceLabel(index), false);
            }
        }

        private void SetGraphicEnabled(Graphic graphic, bool isEnabled)
        {
            if (graphic != null)
            {
                graphic.enabled = isEnabled;
            }
        }

        private void SetGraphicAlpha(Graphic graphic, float alpha)
        {
            if (graphic == null)
            {
                return;
            }

            Color color = graphic == _huntVignette ? _huntVignetteBaseColor : graphic.color;
            color.a = alpha;
            graphic.color = color;
        }

        private void WarnMissingOnce(string fieldName)
        {
            if (!_warnedFields.Add(fieldName))
            {
                return;
            }

            Debug.LogWarning($"{nameof(HUDController)}：{fieldName} 未在 Inspector 中赋值，对应显示将保持隐藏。", this);
        }
    }
}
