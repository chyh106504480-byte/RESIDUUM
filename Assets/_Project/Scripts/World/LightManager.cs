using System.Collections;
using Residuum.Core;
using UnityEngine;

namespace Residuum.World
{
    /// <summary>
    /// 管理场景灯光在鬼现身与猎杀期间的效果，以及回合中的随机停电。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class LightManager : MonoBehaviour
    {
        [Tooltip("本管理器管辖的全部灯光。留空时不工作并在 Awake 报错")]
        [SerializeField] private Light[] _managedLights;

        [Header("猎杀闪烁")]
        [Tooltip("猎杀期间灯光闪烁的间隔秒数")]
        [SerializeField] private float _flickerInterval = 0.12f;

        [Tooltip("每次闪烁时，一盏灯保持亮着的概率")]
        [SerializeField, Range(0f, 1f)] private float _flickerOnChance = 0.45f;

        [Tooltip("猎杀开始时直接全场熄灭而不是闪烁的概率")]
        [Range(0f, 1f)]
        [SerializeField] private float _huntBlackoutChance = 0.3f;

        [Header("鬼现身")]
        [Tooltip("鬼现身时灯光变红并闪烁的持续秒数。应与 GhostAI 的显形秒数接近")]
        [Min(0f)]
        [SerializeField] private float _manifestEffectDuration = 2f;

        [Tooltip("鬼现身时的灯光颜色")]
        [SerializeField] private Color _manifestColor = new Color(0.85f, 0.1f, 0.1f, 1f);

        [Tooltip("鬼现身时灯光闪烁的间隔秒数")]
        [Min(0f)]
        [SerializeField] private float _manifestFlickerInterval = 0.1f;

        [Tooltip("每次闪烁时一盏灯保持亮着的概率")]
        [Range(0f, 1f)]
        [SerializeField] private float _manifestFlickerOnChance = 0.5f;

        [Header("随机停电")]
        [Tooltip("两次停电判定之间的间隔秒数")]
        [SerializeField] private float _blackoutCheckInterval = 45f;

        [Tooltip("每次判定触发停电的概率")]
        [SerializeField, Range(0f, 1f)] private float _blackoutChance = 0.35f;

        [Tooltip("停电时一次性扣除的理智点数")]
        [SerializeField] private float _blackoutSanityPenalty = 10f;

        [Tooltip("检查玩家是否已经把灯重新打开的间隔秒数")]
        [SerializeField] private float _blackoutRecoveryCheckInterval = 1f;

        private bool[] _lightStatesBeforeFlicker;
        private bool[] _lightStatesBeforeManifest;
        private Color[] _lightColorsBeforeManifest;
        private bool _hasValidManagedLight;
        private bool _isBlackout;
        private bool _isHuntActive;
        private bool _isRoundActive;
        private Coroutine _flickerCoroutine;
        private Coroutine _manifestCoroutine;
        private Coroutine _blackoutCheckCoroutine;
        private Coroutine _blackoutRecoveryCoroutine;
        private WaitForSeconds _flickerWait;
        private WaitForSeconds _manifestFlickerWait;
        private WaitForSeconds _blackoutCheckWait;
        private WaitForSeconds _blackoutRecoveryCheckWait;

        private void Awake()
        {
            _hasValidManagedLight = HasValidManagedLight();
            if (!_hasValidManagedLight)
            {
                Debug.LogError(
                    $"LightManager：物体“{name}”未配置任何有效的 Light，本管理器不会工作。",
                    this);
            }

            CacheWaitInstructions();
        }

        private void OnEnable()
        {
            GameEvents.OnHuntStart += HandleHuntStart;
            GameEvents.OnHuntEnd += HandleHuntEnd;
            GameEvents.OnGhostEvent += HandleGhostEvent;
            GameEvents.OnRoundStart += HandleRoundStart;
            GameEvents.OnRoundEnd += HandleRoundEnd;
        }

        private void OnDisable()
        {
            GameEvents.OnHuntStart -= HandleHuntStart;
            GameEvents.OnHuntEnd -= HandleHuntEnd;
            GameEvents.OnGhostEvent -= HandleGhostEvent;
            GameEvents.OnRoundStart -= HandleRoundStart;
            GameEvents.OnRoundEnd -= HandleRoundEnd;

            _isRoundActive = false;
            _isHuntActive = false;
            StopManifestEffect(true);
            StopFlicker(true);
            StopBlackoutCheck();
            StopBlackoutRecoveryCheck();
        }

        private void OnDestroy()
        {
            // 防御性退订，避免异常销毁顺序让静态事件留下失效委托。
            GameEvents.OnHuntStart -= HandleHuntStart;
            GameEvents.OnHuntEnd -= HandleHuntEnd;
            GameEvents.OnGhostEvent -= HandleGhostEvent;
            GameEvents.OnRoundStart -= HandleRoundStart;
            GameEvents.OnRoundEnd -= HandleRoundEnd;

            StopManifestEffect(true);
            StopFlicker(true);
            StopBlackoutCheck();
            StopBlackoutRecoveryCheck();

            _lightStatesBeforeFlicker = null;
            _lightStatesBeforeManifest = null;
            _lightColorsBeforeManifest = null;
            _flickerWait = null;
            _manifestFlickerWait = null;
            _blackoutCheckWait = null;
            _blackoutRecoveryCheckWait = null;
            _managedLights = null;
        }

        private void HandleHuntStart(float duration)
        {
            _ = duration;

            if (!_isRoundActive || !_hasValidManagedLight)
            {
                return;
            }

            _isHuntActive = true;
            if (_isBlackout)
            {
                return;
            }

            // 猎杀效果优先于鬼现身：先还原最原始的颜色与开关快照。
            StopManifestEffect(true);
            if (RollChance(_huntBlackoutChance))
            {
                BeginBlackout(false);
                return;
            }

            StartFlicker();
        }

        private void HandleHuntEnd()
        {
            _isHuntActive = false;
            StopFlicker(true);
        }

        private void HandleGhostEvent(Vector3 position)
        {
            _ = position;

            if (!_isRoundActive || !_hasValidManagedLight || _isBlackout || _isHuntActive)
            {
                return;
            }

            StartManifestEffect();
        }

        private void HandleRoundStart()
        {
            _isRoundActive = false;
            _isHuntActive = false;
            StopManifestEffect(true);
            StopFlicker(true);
            StopBlackoutCheck();
            StopBlackoutRecoveryCheck();

            bool wasBlackout = _isBlackout;
            _isBlackout = false;
            _lightStatesBeforeFlicker = null;
            _lightStatesBeforeManifest = null;
            _lightColorsBeforeManifest = null;
            CacheWaitInstructions();

            if (wasBlackout)
            {
                GameEvents.RaiseBlackoutChanged(false);
            }

            _isRoundActive = true;
            StartBlackoutCheck();
        }

        private void HandleRoundEnd(RoundResult result)
        {
            _ = result;

            _isRoundActive = false;
            _isHuntActive = false;
            StopManifestEffect(true);
            StopFlicker(true);
            StopBlackoutCheck();
            StopBlackoutRecoveryCheck();
        }

        private void StartFlicker()
        {
            if (!isActiveAndEnabled || !_isRoundActive || !_isHuntActive ||
                _isBlackout || !_hasValidManagedLight || _flickerCoroutine != null)
            {
                return;
            }

            CaptureLightStates();
            _flickerCoroutine = StartCoroutine(FlickerRoutine());
        }

        private void StartManifestEffect()
        {
            if (!isActiveAndEnabled || !_isRoundActive || _isBlackout || _isHuntActive ||
                !_hasValidManagedLight || _manifestEffectDuration <= 0f)
            {
                return;
            }

            if (_manifestCoroutine == null)
            {
                CaptureManifestLightStates();
            }
            else
            {
                // 重叠现身只刷新持续时间，不覆盖已保存的原始快照。
                StopCoroutine(_manifestCoroutine);
                _manifestCoroutine = null;
            }

            _manifestCoroutine = StartCoroutine(ManifestFlickerRoutine());
        }

        private IEnumerator ManifestFlickerRoutine()
        {
            float effectEndTime = Time.time + Mathf.Max(_manifestEffectDuration, 0f);
            while (_isRoundActive && !_isBlackout && !_isHuntActive &&
                   Time.time < effectEndTime)
            {
                for (int index = 0; index < _managedLights.Length; index++)
                {
                    Light managedLight = _managedLights[index];
                    if (managedLight != null)
                    {
                        managedLight.color = _manifestColor;
                        managedLight.enabled = RollChance(_manifestFlickerOnChance);
                    }
                }

                yield return _manifestFlickerWait;
            }

            _manifestCoroutine = null;
            RestoreManifestLightStates();
            ClearManifestLightStates();
        }

        private void StopManifestEffect(bool restoreLightStates)
        {
            if (_manifestCoroutine != null)
            {
                StopCoroutine(_manifestCoroutine);
                _manifestCoroutine = null;
            }

            if (restoreLightStates)
            {
                RestoreManifestLightStates();
            }

            ClearManifestLightStates();
        }

        private void CaptureManifestLightStates()
        {
            _lightStatesBeforeManifest = new bool[_managedLights.Length];
            _lightColorsBeforeManifest = new Color[_managedLights.Length];
            for (int index = 0; index < _managedLights.Length; index++)
            {
                Light managedLight = _managedLights[index];
                if (managedLight != null)
                {
                    _lightStatesBeforeManifest[index] = managedLight.enabled;
                    _lightColorsBeforeManifest[index] = managedLight.color;
                }
            }
        }

        private void RestoreManifestLightStates()
        {
            if (_lightStatesBeforeManifest == null || _lightColorsBeforeManifest == null ||
                _managedLights == null)
            {
                return;
            }

            int stateCount = Mathf.Min(
                Mathf.Min(_lightStatesBeforeManifest.Length, _lightColorsBeforeManifest.Length),
                _managedLights.Length);
            for (int index = 0; index < stateCount; index++)
            {
                Light managedLight = _managedLights[index];
                if (managedLight != null)
                {
                    managedLight.color = _lightColorsBeforeManifest[index];
                    managedLight.enabled = _lightStatesBeforeManifest[index];
                }
            }
        }

        private void ClearManifestLightStates()
        {
            _lightStatesBeforeManifest = null;
            _lightColorsBeforeManifest = null;
        }

        private IEnumerator FlickerRoutine()
        {
            while (_isRoundActive && _isHuntActive && !_isBlackout)
            {
                for (int index = 0; index < _managedLights.Length; index++)
                {
                    Light managedLight = _managedLights[index];
                    if (managedLight != null)
                    {
                        managedLight.enabled = RollChance(_flickerOnChance);
                    }
                }

                yield return _flickerWait;
            }

            _flickerCoroutine = null;
        }

        private void StopFlicker(bool restoreLightStates)
        {
            if (_flickerCoroutine != null)
            {
                StopCoroutine(_flickerCoroutine);
                _flickerCoroutine = null;
            }

            if (restoreLightStates)
            {
                RestoreLightStates();
            }

            _lightStatesBeforeFlicker = null;
        }

        private void CaptureLightStates()
        {
            _lightStatesBeforeFlicker = new bool[_managedLights.Length];
            for (int index = 0; index < _managedLights.Length; index++)
            {
                Light managedLight = _managedLights[index];
                if (managedLight != null)
                {
                    _lightStatesBeforeFlicker[index] = managedLight.enabled;
                }
            }
        }

        private void RestoreLightStates()
        {
            if (_lightStatesBeforeFlicker == null || _managedLights == null)
            {
                return;
            }

            int stateCount = Mathf.Min(_lightStatesBeforeFlicker.Length, _managedLights.Length);
            for (int index = 0; index < stateCount; index++)
            {
                Light managedLight = _managedLights[index];
                if (managedLight != null)
                {
                    managedLight.enabled = _lightStatesBeforeFlicker[index];
                }
            }
        }

        private void StartBlackoutCheck()
        {
            if (!isActiveAndEnabled || !_isRoundActive || _isBlackout ||
                !_hasValidManagedLight || _blackoutCheckCoroutine != null)
            {
                return;
            }

            _blackoutCheckCoroutine = StartCoroutine(BlackoutCheckRoutine());
        }

        private IEnumerator BlackoutCheckRoutine()
        {
            while (_isRoundActive && !_isBlackout)
            {
                yield return _blackoutCheckWait;

                if (!_isRoundActive || _isBlackout)
                {
                    break;
                }

                if (RollChance(_blackoutChance))
                {
                    _blackoutCheckCoroutine = null;
                    BeginBlackout(true);
                    yield break;
                }
            }

            _blackoutCheckCoroutine = null;
        }

        private void StopBlackoutCheck()
        {
            if (_blackoutCheckCoroutine == null)
            {
                return;
            }

            StopCoroutine(_blackoutCheckCoroutine);
            _blackoutCheckCoroutine = null;
        }

        private void BeginBlackout(bool applySanityPenalty)
        {
            if (!_isRoundActive || _isBlackout || !_hasValidManagedLight)
            {
                return;
            }

            // 停电优先级最高：先还原所有临时效果，再统一关灯。
            StopManifestEffect(true);
            StopFlicker(true);
            _isBlackout = true;
            SetAllManagedLights(false);

            if (applySanityPenalty)
            {
                GameEvents.RaiseSanityPenalty(_blackoutSanityPenalty);
            }

            GameEvents.RaiseBlackoutChanged(true);
            StartBlackoutRecoveryCheck();
        }

        private void StartBlackoutRecoveryCheck()
        {
            if (!isActiveAndEnabled || !_isRoundActive || !_isBlackout ||
                !_hasValidManagedLight || _blackoutRecoveryCoroutine != null)
            {
                return;
            }

            _blackoutRecoveryCoroutine = StartCoroutine(BlackoutRecoveryRoutine());
        }

        private IEnumerator BlackoutRecoveryRoutine()
        {
            while (_isRoundActive && _isBlackout)
            {
                yield return _blackoutRecoveryCheckWait;

                if (!_isRoundActive || !_isBlackout || !HasEnabledManagedLight())
                {
                    continue;
                }

                _isBlackout = false;
                _blackoutRecoveryCoroutine = null;
                GameEvents.RaiseBlackoutChanged(false);

                if (_isHuntActive)
                {
                    StartFlicker();
                }

                StartBlackoutCheck();
                yield break;
            }

            _blackoutRecoveryCoroutine = null;
        }

        private void StopBlackoutRecoveryCheck()
        {
            if (_blackoutRecoveryCoroutine == null)
            {
                return;
            }

            StopCoroutine(_blackoutRecoveryCoroutine);
            _blackoutRecoveryCoroutine = null;
        }

        private void SetAllManagedLights(bool isEnabled)
        {
            if (_managedLights == null)
            {
                return;
            }

            for (int index = 0; index < _managedLights.Length; index++)
            {
                Light managedLight = _managedLights[index];
                if (managedLight != null)
                {
                    managedLight.enabled = isEnabled;
                }
            }
        }

        private bool HasEnabledManagedLight()
        {
            if (_managedLights == null)
            {
                return false;
            }

            for (int index = 0; index < _managedLights.Length; index++)
            {
                Light managedLight = _managedLights[index];
                if (managedLight != null && managedLight.enabled)
                {
                    return true;
                }
            }

            return false;
        }

        private bool HasValidManagedLight()
        {
            if (_managedLights == null || _managedLights.Length == 0)
            {
                return false;
            }

            for (int index = 0; index < _managedLights.Length; index++)
            {
                if (_managedLights[index] != null)
                {
                    return true;
                }
            }

            return false;
        }

        private void CacheWaitInstructions()
        {
            _flickerWait = new WaitForSeconds(Mathf.Max(_flickerInterval, 0f));
            _manifestFlickerWait =
                new WaitForSeconds(Mathf.Max(_manifestFlickerInterval, 0f));
            _blackoutCheckWait = new WaitForSeconds(Mathf.Max(_blackoutCheckInterval, 0f));
            _blackoutRecoveryCheckWait =
                new WaitForSeconds(Mathf.Max(_blackoutRecoveryCheckInterval, 0f));
        }

        private static bool RollChance(float chance)
        {
            if (chance <= 0f)
            {
                return false;
            }

            return chance >= 1f || Random.value < chance;
        }
    }
}
