using System.Collections;
using Residuum.Core;
using UnityEngine;

namespace Residuum.World
{
    /// <summary>
    /// 管理场景灯光在猎杀期间的闪烁，以及回合中的随机停电。
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
        private bool _hasValidManagedLight;
        private bool _isBlackout;
        private bool _isHuntActive;
        private bool _isRoundActive;
        private Coroutine _flickerCoroutine;
        private Coroutine _blackoutCheckCoroutine;
        private Coroutine _blackoutRecoveryCoroutine;
        private WaitForSeconds _flickerWait;
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
            GameEvents.OnRoundStart += HandleRoundStart;
            GameEvents.OnRoundEnd += HandleRoundEnd;
        }

        private void OnDisable()
        {
            GameEvents.OnHuntStart -= HandleHuntStart;
            GameEvents.OnHuntEnd -= HandleHuntEnd;
            GameEvents.OnRoundStart -= HandleRoundStart;
            GameEvents.OnRoundEnd -= HandleRoundEnd;

            _isRoundActive = false;
            _isHuntActive = false;
            StopFlicker(true);
            StopBlackoutCheck();
            StopBlackoutRecoveryCheck();
        }

        private void OnDestroy()
        {
            // 防御性退订，避免异常销毁顺序让静态事件留下失效委托。
            GameEvents.OnHuntStart -= HandleHuntStart;
            GameEvents.OnHuntEnd -= HandleHuntEnd;
            GameEvents.OnRoundStart -= HandleRoundStart;
            GameEvents.OnRoundEnd -= HandleRoundEnd;

            StopFlicker(true);
            StopBlackoutCheck();
            StopBlackoutRecoveryCheck();

            _lightStatesBeforeFlicker = null;
            _flickerWait = null;
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

            StartFlicker();
        }

        private void HandleHuntEnd()
        {
            _isHuntActive = false;
            StopFlicker(true);
        }

        private void HandleRoundStart()
        {
            _isRoundActive = false;
            _isHuntActive = false;
            StopFlicker(true);
            StopBlackoutCheck();
            StopBlackoutRecoveryCheck();

            bool wasBlackout = _isBlackout;
            _isBlackout = false;
            _lightStatesBeforeFlicker = null;
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
                    BeginBlackout();
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

        private void BeginBlackout()
        {
            if (!_isRoundActive || _isBlackout || !_hasValidManagedLight)
            {
                return;
            }

            // 停电是真实状态变化：先结束临时闪烁，再统一关灯。
            StopFlicker(true);
            _isBlackout = true;
            SetAllManagedLights(false);

            GameEvents.RaiseSanityPenalty(_blackoutSanityPenalty);
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
