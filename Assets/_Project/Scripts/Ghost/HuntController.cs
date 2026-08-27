using System.Collections;
using UnityEngine;
using Residuum.Core;

namespace Residuum.Ghost
{
    /// <summary>
    /// 根据玩家当前理智定期判定是否开始猎杀，并在猎杀结束后执行冷却。
    /// 只负责猎杀时机，具体追击行为由 GhostAI 处理。
    /// </summary>
    public class HuntController : MonoBehaviour
    {
        [Header("依赖")]
        [SerializeField]
        [Tooltip("由 Inspector 注入的 GhostAI；负责执行实际猎杀行为。")]
        private GhostAI _ghostAI;

        [Header("猎杀判定")]
        [SerializeField]
        [Min(0f)]
        [Tooltip("两次猎杀判定之间的间隔秒数。")]
        private float _checkInterval = 25f;

        [SerializeField]
        [Min(0f)]
        [Tooltip("允许开始猎杀的理智阈值；理智低于此值时才计算触发概率。")]
        private float _huntThreshold = 50f;

        [SerializeField]
        [Min(0f)]
        [Tooltip("GhostDefinition 缺失时使用的猎杀持续秒数。")]
        private float _fallbackHuntDuration = 25f;

        [SerializeField]
        [Min(0f)]
        [Tooltip("GhostDefinition 缺失时使用的猎杀冷却秒数。")]
        private float _fallbackHuntCooldown = 25f;

        [Header("运行时调试（只读）")]
        [SerializeField]
        [Tooltip("距离下一次猎杀判定的剩余秒数；由运行时代码更新。")]
        private float _secondsUntilNextCheck;

        [SerializeField]
        [Tooltip("按当前理智计算出的猎杀触发概率；由运行时代码更新。")]
        private float _currentTriggerProbability;

        private float _currentSanity;
        private float _nextCheckTime;
        private float _cooldownEndTime;
        private bool _hasSanityValue;
        private bool _isCoolingDown;
        private Coroutine _huntCheckCoroutine;
        private WaitForSeconds _checkIntervalWait;

        public bool IsHunting { get; private set; }

        private void Awake()
        {
            if (_ghostAI == null)
            {
                Debug.LogError($"[HuntController:{name}] 未注入 GhostAI，HuntController 已禁用。", this);
                enabled = false;
                return;
            }

            CacheCheckIntervalWait();
        }

        private void OnEnable()
        {
            if (_ghostAI == null)
            {
                Debug.LogError($"[HuntController:{name}] 未注入 GhostAI，HuntController 已禁用。", this);
                enabled = false;
                return;
            }

            GameEvents.OnSanityChanged += HandleSanityChanged;
            GameEvents.OnHuntEnd += HandleHuntEnd;
            GameEvents.OnRoundStart += HandleRoundStart;

            CacheCheckIntervalWait();
            StartHuntCheckCoroutine();
        }

        private void Update()
        {
            _currentTriggerProbability = CalculateTriggerProbability();

            if (IsHunting)
            {
                _secondsUntilNextCheck = 0f;
                return;
            }

            if (_isCoolingDown)
            {
                float cooldownRemaining = Mathf.Max(_cooldownEndTime - Time.time, 0f);
                _secondsUntilNextCheck = cooldownRemaining + Mathf.Max(_checkInterval, 0f);
                return;
            }

            _secondsUntilNextCheck = Mathf.Max(_nextCheckTime - Time.time, 0f);
        }

        private void OnDisable()
        {
            GameEvents.OnSanityChanged -= HandleSanityChanged;
            GameEvents.OnHuntEnd -= HandleHuntEnd;
            GameEvents.OnRoundStart -= HandleRoundStart;

            StopHuntCheckCoroutine();
            ResetRuntimeState();
        }

        private void OnDestroy()
        {
            StopHuntCheckCoroutine();
            _checkIntervalWait = null;
            _ghostAI = null;
            ResetRuntimeState();
        }

        /// <summary>无视理智、阈值与冷却，立即开始一次猎杀。</summary>
        [ContextMenu("强制触发猎杀")]
        public void ForceHunt()
        {
            if (_ghostAI == null)
            {
                Debug.LogError($"[HuntController:{name}] ForceHunt 失败：未注入 GhostAI。", this);
                enabled = false;
                return;
            }

            BeginHunt();
        }

        private IEnumerator HuntCheckLoop()
        {
            while (enabled)
            {
                while (IsHunting)
                {
                    _nextCheckTime = 0f;
                    yield return null;
                }

                while (_isCoolingDown)
                {
                    if (Time.time >= _cooldownEndTime)
                    {
                        _isCoolingDown = false;
                        _cooldownEndTime = 0f;
                        break;
                    }

                    yield return null;
                }

                _nextCheckTime = Time.time + Mathf.Max(_checkInterval, 0f);
                yield return _checkIntervalWait;

                if (!IsHunting && !_isCoolingDown)
                {
                    TryStartHunt();
                }
            }
        }

        private void TryStartHunt()
        {
            if (!_hasSanityValue || _currentSanity >= _huntThreshold)
            {
                return;
            }

            float triggerProbability = CalculateTriggerProbability();
            if (Random.value < triggerProbability)
            {
                BeginHunt();
            }
        }

        private void BeginHunt()
        {
            float huntDuration = ResolveHuntDuration();

            IsHunting = true;
            _isCoolingDown = false;
            _cooldownEndTime = 0f;
            _nextCheckTime = 0f;

            GameEvents.RaiseHuntStart(huntDuration);
            _ghostAI.EnterHunt(huntDuration);
        }

        private float ResolveHuntDuration()
        {
            if (_ghostAI.Definition != null)
            {
                return Mathf.Max(_ghostAI.Definition.huntDuration, 0f);
            }

            Debug.LogWarning(
                $"[HuntController:{name}] GhostDefinition 为空，本次猎杀使用 Inspector 兜底时长。",
                this);
            return Mathf.Max(_fallbackHuntDuration, 0f);
        }

        private float ResolveHuntCooldown()
        {
            if (_ghostAI != null && _ghostAI.Definition != null)
            {
                return Mathf.Max(_ghostAI.Definition.huntCooldown, 0f);
            }

            Debug.LogWarning(
                $"[HuntController:{name}] GhostDefinition 为空，本次冷却使用 Inspector 兜底时长。",
                this);
            return Mathf.Max(_fallbackHuntCooldown, 0f);
        }

        private float CalculateTriggerProbability()
        {
            if (!_hasSanityValue || _huntThreshold <= 0f || _currentSanity >= _huntThreshold)
            {
                return 0f;
            }

            return Mathf.Clamp01((_huntThreshold - _currentSanity) / _huntThreshold);
        }

        private void HandleSanityChanged(float sanity)
        {
            _currentSanity = sanity;
            _hasSanityValue = true;
        }

        private void HandleHuntEnd()
        {
            // 回合重置后可能迟到的旧猎杀结束事件不得污染新一局冷却状态。
            if (!IsHunting)
            {
                return;
            }

            IsHunting = false;
            _isCoolingDown = true;
            _cooldownEndTime = Time.time + ResolveHuntCooldown();
            _nextCheckTime = 0f;
        }

        private void HandleRoundStart()
        {
            StopHuntCheckCoroutine();
            ResetRuntimeState();
            CacheCheckIntervalWait();
            StartHuntCheckCoroutine();
        }

        private void StartHuntCheckCoroutine()
        {
            if (!isActiveAndEnabled || _huntCheckCoroutine != null)
            {
                return;
            }

            _huntCheckCoroutine = StartCoroutine(HuntCheckLoop());
        }

        private void StopHuntCheckCoroutine()
        {
            if (_huntCheckCoroutine == null)
            {
                return;
            }

            StopCoroutine(_huntCheckCoroutine);
            _huntCheckCoroutine = null;
        }

        private void CacheCheckIntervalWait()
        {
            _checkIntervalWait = new WaitForSeconds(Mathf.Max(_checkInterval, 0f));
        }

        private void ResetRuntimeState()
        {
            _currentSanity = 0f;
            _nextCheckTime = 0f;
            _cooldownEndTime = 0f;
            _secondsUntilNextCheck = 0f;
            _currentTriggerProbability = 0f;
            _hasSanityValue = false;
            _isCoolingDown = false;
            IsHunting = false;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            _checkInterval = Mathf.Max(_checkInterval, 0f);
            _huntThreshold = Mathf.Max(_huntThreshold, 0f);
            _fallbackHuntDuration = Mathf.Max(_fallbackHuntDuration, 0f);
            _fallbackHuntCooldown = Mathf.Max(_fallbackHuntCooldown, 0f);
        }
#endif
    }
}
