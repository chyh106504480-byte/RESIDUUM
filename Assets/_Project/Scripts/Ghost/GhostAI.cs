using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;
using Residuum.Core;

namespace Residuum.Ghost
{
    public enum GhostState
    {
        Idle,
        Roam,
        Interact,
        Hunt
    }

    /// <summary>
    /// 单一鬼 AI。鬼种差异只读取 GhostDefinition，不为不同鬼种派生行为类。
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent))]
    public class GhostAI : MonoBehaviour
    {
        private const int SightSampleCount = 3;

        [Header("场景引用")]
        [SerializeField]
        [Tooltip("玩家根节点，由场景或 GameManager 注入。")]
        private Transform _player;

        [SerializeField]
        [Tooltip("鬼房中心；未指定时使用本组件 Awake 时的位置。")]
        private Transform _ghostRoomCenter;

        [SerializeField]
        [Tooltip("人在场景中手工摆放的巡逻点。")]
        private Transform[] _roamPoints;

        [SerializeField]
        [Tooltip("玩家视线采样点，依次连接头、胸、脚；为空时使用高度偏移回退。")]
        private Transform[] _playerSightPoints;

        [SerializeField]
        [Tooltip("鬼的全部渲染器；平时关闭，猎杀和鬼事件期间开启。")]
        private Renderer[] _renderers;

        [SerializeField]
        [Tooltip("脚印预制体；仅 leavesFootprints 为 true 的鬼会生成。")]
        private GameObject _footprintPrefab;

        [Header("Idle")]
        [SerializeField]
        [Min(0f)]
        [Tooltip("Idle 最短停留秒数，默认 5 秒。")]
        private float _idleDurationMin = 5f;

        [SerializeField]
        [Min(0f)]
        [Tooltip("Idle 最长停留秒数，默认 15 秒。")]
        private float _idleDurationMax = 15f;

        [SerializeField]
        [Range(0f, 1f)]
        [Tooltip("基础转入 Roam 的权重，默认 30%；互动权重会再乘鬼种互动频率。")]
        private float _roamChance = 0.3f;

        [SerializeField]
        [Min(0f)]
        [Tooltip("鬼房中心周围选择 Idle 随机点的半径。")]
        private float _ghostRoomRadius = 2.5f;

        [Header("Roam")]
        [SerializeField]
        [Min(0f)]
        [Tooltip("到达巡逻点后的最短停留秒数，默认 3 秒。")]
        private float _roamWaitMin = 3f;

        [SerializeField]
        [Min(0f)]
        [Tooltip("到达巡逻点后的最长停留秒数，默认 8 秒。")]
        private float _roamWaitMax = 8f;

        [SerializeField]
        [Min(0f)]
        [Tooltip("随机点映射到 NavMesh 时允许搜索的半径。")]
        private float _navMeshSampleRadius = 2f;

        [SerializeField]
        [Min(1)]
        [Tooltip("为鬼房与搜索游走点尝试 NavMesh 采样的次数。")]
        private int _randomPointSampleAttempts = 8;

        [SerializeField]
        [Min(0f)]
        [Tooltip("Roam 中每走多少米生成一个脚印，默认 1.5 米。")]
        private float _footprintSpacing = 1.5f;

        [SerializeField]
        [Min(0f)]
        [Tooltip("脚印存活秒数，默认 20 秒。")]
        private float _footprintLifetime = 20f;

        [SerializeField]
        [Min(0f)]
        [Tooltip("脚印向下探测的起点高度。")]
        private float _footprintRayOriginHeight = 0.5f;

        [SerializeField]
        [Min(0f)]
        [Tooltip("脚印向下探测地面的距离。")]
        private float _footprintGroundCheckDistance = 2f;

        [SerializeField]
        [Tooltip("脚印地面检测使用的层。")]
        private LayerMask _footprintGroundMask = ~0;

        [SerializeField]
        [Min(0f)]
        [Tooltip("脚印沿地面法线抬高的距离，用于避免闪烁。")]
        private float _footprintSurfaceOffset = 0.01f;

        [Header("Interact")]
        [SerializeField]
        [Min(0f)]
        [Tooltip("搜索 IInteractable 的半径，默认 2.5 米。")]
        private float _interactRadius = 2.5f;

        [SerializeField]
        [Tooltip("搜索 IInteractable 时包含的物理层。")]
        private LayerMask _interactableMask = ~0;

        [SerializeField]
        [Range(0f, 1f)]
        [Tooltip("互动目标请求生成指纹的概率，默认 40%。")]
        private float _fingerprintChance = 0.4f;

        [SerializeField]
        [Min(0f)]
        [Tooltip("massThrowOnHunt 鬼种的额外互动权重倍率；默认 1，不实现抛物。")]
        private float _massThrowInteractionFrequencyMultiplier = 1f;

        [Tooltip("指纹生成请求；在 Inspector 中连接到 Fingerprint.SpawnAt。")]
        public UnityEvent<Transform> onFingerprintRequest = new UnityEvent<Transform>();

        [Header("Hunt")]
        [SerializeField]
        [Min(0f)]
        [Tooltip("视线检测间隔秒数，默认 0.25 秒。")]
        private float _sightCheckInterval = 0.25f;

        [SerializeField]
        [Min(0f)]
        [Tooltip("丢失视线后保留最后已知位置的秒数，默认 3 秒。")]
        private float _lostSightMemoryDuration = 3f;

        [SerializeField]
        [Tooltip("阻挡鬼视线的层。玩家层不应包含在内。")]
        private LayerMask _sightBlockMask;

        [SerializeField]
        [Tooltip("鬼发出视线射线的本地高度。")]
        private float _ghostSightOriginHeight = 1.4f;

        [SerializeField]
        [Tooltip("未连接视线采样点时，玩家中部采样点的高度偏移。")]
        private float _fallbackSightMiddleHeight = 0.9f;

        [SerializeField]
        [Tooltip("未连接视线采样点时，玩家上部采样点的高度偏移。")]
        private float _fallbackSightUpperHeight = 1.6f;

        [SerializeField]
        [Min(0f)]
        [Tooltip("最后已知位置附近的随机搜索半径。")]
        private float _huntSearchRadius = 3f;

        [SerializeField]
        [Min(0f)]
        [Tooltip("玩家躲藏时检查藏匿点的判定间隔，默认 5 秒。")]
        private float _hidingCheckInterval = 5f;

        [SerializeField]
        [Range(0f, 1f)]
        [Tooltip("每次判定强行检查玩家实际位置的概率，默认 15%。")]
        private float _hidingCheckChance = 0.15f;

        [SerializeField]
        [Min(0f)]
        [Tooltip("幽影冲刺时 huntSpeed 的倍率，默认 1.8。")]
        private float _sprintBurstMultiplier = 1.8f;

        [SerializeField]
        [Min(0f)]
        [Tooltip("幽影单次加速持续秒数，默认 1.5 秒。")]
        private float _sprintBurstDuration = 1.5f;

        [SerializeField]
        [Min(0f)]
        [Tooltip("未躲藏玩家在此距离内会被抓住，默认 1.2 米。")]
        private float _catchDistance = 1.2f;

        [Header("导航")]
        [SerializeField]
        [Min(0f)]
        [Tooltip("移动目标变化超过此距离时立即更新路径，默认 0.5 米。")]
        private float _destinationChangeThreshold = 0.5f;

        [SerializeField]
        [Min(0f)]
        [Tooltip("追踪移动目标时重新下发路径的最短间隔，默认 0.2 秒。")]
        private float _destinationUpdateInterval = 0.2f;

        [SerializeField]
        [Min(0f)]
        [Tooltip("在 NavMeshAgent stoppingDistance 之外追加的到达容差。")]
        private float _arrivalTolerance = 0.15f;

        [SerializeField]
        [Min(0f)]
        [Tooltip("NavMesh 警告重复输出的最短间隔，防止错误配置刷屏。")]
        private float _navMeshWarningInterval = 2f;

        [Header("显形与调试")]
        [SerializeField]
        [Min(0f)]
        [Tooltip("GhostEvent 的显形秒数，默认 2 秒。")]
        private float _ghostEventVisibleDuration = 2f;

        [SerializeField]
        [Min(0f)]
        [Tooltip("Scene 视图中当前状态文字的高度。")]
        private float _gizmoLabelHeight = 2.2f;

        [SerializeField]
        [Min(0f)]
        [Tooltip("Scene 视图中当前目标点球体的半径。")]
        private float _gizmoTargetRadius = 0.2f;

        private readonly List<Transform> _interactionCandidates = new List<Transform>();
        private readonly List<GameObject> _spawnedFootprints = new List<GameObject>();
        private readonly Vector3[] _lastSightTargets = new Vector3[SightSampleCount];
        private readonly bool[] _lastSightBlocked = new bool[SightSampleCount];

        private NavMeshAgent _agent;
        private Vector3 _initialGhostRoomCenter;
        private Vector3 _currentTarget;
        private Vector3 _lastRequestedDestination;
        private Vector3 _lastMovementPosition;
        private Vector3 _lastKnownPlayerPosition;
        private Vector3 _hidingCheckTarget;

        private float _stateTimerEnd;
        private float _huntEndTime;
        private float _nextSightCheckTime;
        private float _lastSeenTime;
        private float _nextHidingCheckTime;
        private float _nextDestinationUpdateTime;
        private float _nextNavMeshWarningTime;
        private float _distanceSinceFootprint;

        private bool _initialized;
        private bool _idleWaiting;
        private bool _roamWaiting;
        private bool _hasCurrentTarget;
        private bool _hasRequestedDestination;
        private bool _hasLineOfSight;
        private bool _hasLastKnownPlayerPosition;
        private bool _reachedLastKnownPosition;
        private bool _huntSearchTargetActive;
        private bool _checkingHidingSpot;
        private bool _isPlayerHiding;
        private bool _playerCaughtRaised;
        private bool _huntVisible;
        private bool _ghostEventVisible;
        private int _lastSightSampleCount;

        private Coroutine _sprintBurstCoroutine;
        private Coroutine _ghostEventCoroutine;
        private WaitForSeconds _sprintBurstIntervalWait;
        private WaitForSeconds _sprintBurstDurationWait;
        private WaitForSeconds _sprintBurstCooldownWait;
        private WaitForSeconds _ghostEventVisibleWait;

        public GhostState State { get; private set; }

        [SerializeField]
        [Tooltip("本回合鬼种定义；可在 Inspector 预先指定用于单独测试，正式回合由 GameManager 在开始时注入并覆盖。")]
        private GhostDefinition _definition;

        public GhostDefinition Definition
        {
            get => _definition;
            set => _definition = value;
        }

        private void Awake()
        {
            _agent = GetComponent<NavMeshAgent>();
            _initialGhostRoomCenter = transform.position;

            if (_agent == null)
            {
                Debug.LogError($"[GhostAI:{name}] 缺少 NavMeshAgent，GhostAI 已禁用。", this);
                enabled = false;
                return;
            }

            CacheLocalWaits();
            SetVisible(false);
        }

        private void OnEnable()
        {
            GameEvents.OnHidingChanged += HandleHidingChanged;

            if (_initialized && Definition != null && _agent != null)
            {
                EnterState(GhostState.Idle, true);
            }
        }

        private void Start()
        {
            if (Definition == null)
            {
                Debug.LogError(
                    $"[GhostAI:{name}] GhostDefinition 尚未由 GameManager 注入，GhostAI 已禁用。",
                    this);
                enabled = false;
                return;
            }

            if (_player == null)
            {
                Debug.LogError($"[GhostAI:{name}] 未注入玩家 Transform，Hunt 无法追击。", this);
            }

            if (_roamPoints == null || _roamPoints.Length == 0)
            {
                Debug.LogError($"[GhostAI:{name}] 未配置任何 Roam Point，Roam 会回退到 Idle。", this);
            }

            if (_renderers == null || _renderers.Length == 0)
            {
                Debug.LogError($"[GhostAI:{name}] 未注入 Renderer，显形状态不会有可见模型。", this);
            }

            if (Definition.leavesFootprints && _footprintPrefab == null)
            {
                Debug.LogError($"[GhostAI:{name}] 当前鬼种需要脚印，但未注入脚印预制体。", this);
            }

            CacheDefinitionWaits();
            _initialized = true;
            EnterState(GhostState.Idle, true);
        }

        private void Update()
        {
            if (!_initialized || Definition == null || _agent == null)
            {
                return;
            }

            switch (State)
            {
                case GhostState.Idle:
                    UpdateIdle();
                    break;
                case GhostState.Roam:
                    UpdateRoam();
                    break;
                case GhostState.Interact:
                    UpdateInteract();
                    break;
                case GhostState.Hunt:
                    UpdateHunt();
                    break;
            }
        }

        private void OnDisable()
        {
            GameEvents.OnHidingChanged -= HandleHidingChanged;
            StopAllCoroutines();
            _sprintBurstCoroutine = null;
            _ghostEventCoroutine = null;
            _huntVisible = false;
            _ghostEventVisible = false;
            SetVisible(false);
            ClearSpawnedFootprints();
            ClearPathAndStop();
            State = GhostState.Idle;
        }

        private void OnDestroy()
        {
            StopAllCoroutines();
            ClearSpawnedFootprints();
            _interactionCandidates.Clear();
            Definition = null;
        }

        /// <summary>由 HuntController 调用，开始一段指定时长的猎杀。</summary>
        public void EnterHunt(float duration)
        {
            if (Definition == null)
            {
                Debug.LogError(
                    $"[GhostAI:{name}] 无法进入 Hunt：GhostDefinition 尚未注入。GhostAI 已禁用。",
                    this);
                enabled = false;
                return;
            }

            if (_player == null)
            {
                Debug.LogError($"[GhostAI:{name}] 无法进入 Hunt：未注入玩家 Transform。", this);
                return;
            }

            float resolvedDuration = duration > 0f ? duration : Definition.huntDuration;
            _huntEndTime = Time.time + Mathf.Max(resolvedDuration, 0f);
            EnterState(GhostState.Hunt, true);
        }

        /// <summary>通过 NavMeshAgent 同步传送位置，供回合初始化调用。</summary>
        public void WarpTo(Vector3 position)
        {
            if (_agent == null)
            {
                Debug.LogError($"[GhostAI:{name}] Warp 失败：缺少 NavMeshAgent。", this);
                return;
            }

            if (!_agent.enabled || !_agent.gameObject.activeInHierarchy)
            {
                Debug.LogWarning($"[GhostAI:{name}] Warp 失败：NavMeshAgent 当前未启用。", this);
                return;
            }

            if (!_agent.Warp(position))
            {
                Debug.LogWarning($"[GhostAI:{name}] Warp 失败，目标坐标无法映射到 NavMesh：{position}。", this);
                return;
            }

            if (_ghostRoomCenter == null)
            {
                _initialGhostRoomCenter = position;
            }

            _currentTarget = position;
            _hasCurrentTarget = false;
            _hasRequestedDestination = false;
            _lastMovementPosition = position;
            _distanceSinceFootprint = 0f;
        }

        /// <summary>触发短暂显形和跨模块鬼事件。</summary>
        public void TriggerGhostEvent()
        {
            GameEvents.RaiseGhostEvent(transform.position);

            if (_ghostEventCoroutine != null)
            {
                StopCoroutine(_ghostEventCoroutine);
            }

            _ghostEventCoroutine = StartCoroutine(GhostEventVisibilityRoutine());
        }

        private void EnterState(GhostState nextState, bool force = false)
        {
            if (!force && State == nextState)
            {
                return;
            }

            ExitCurrentState();
            State = nextState;
            _hasCurrentTarget = false;
            _hasRequestedDestination = false;

            switch (State)
            {
                case GhostState.Idle:
                    BeginIdle();
                    break;
                case GhostState.Roam:
                    BeginRoam();
                    break;
                case GhostState.Interact:
                    BeginInteract();
                    break;
                case GhostState.Hunt:
                    BeginHunt();
                    break;
            }
        }

        private void ExitCurrentState()
        {
            if (State != GhostState.Hunt)
            {
                return;
            }

            if (_sprintBurstCoroutine != null)
            {
                StopCoroutine(_sprintBurstCoroutine);
                _sprintBurstCoroutine = null;
            }

            _huntVisible = false;
            RefreshVisibility();

            if (_agent != null && Definition != null)
            {
                _agent.speed = Definition.walkSpeed;
            }
        }

        private void BeginIdle()
        {
            _agent.speed = Definition.walkSpeed;
            _idleWaiting = false;
            ClearPathAndStop();

            Vector3 center = _ghostRoomCenter != null
                ? _ghostRoomCenter.position
                : _initialGhostRoomCenter;

            if (!TryGetRandomNavMeshPoint(center, _ghostRoomRadius, out _currentTarget))
            {
                _currentTarget = center;
                Debug.LogWarning(
                    $"[GhostAI:{name}] Idle 无法在鬼房附近采样 NavMesh 点，将重试鬼房中心：{center}。",
                    this);
            }

            _hasCurrentTarget = true;
        }

        private void UpdateIdle()
        {
            if (!_idleWaiting)
            {
                TrySetDestination(_currentTarget, false);

                if (!HasReachedDestination())
                {
                    return;
                }

                ClearPathAndStop();
                _idleWaiting = true;
                _stateTimerEnd = Time.time + RandomRangeOrdered(_idleDurationMin, _idleDurationMax);
                return;
            }

            if (Time.time < _stateTimerEnd)
            {
                return;
            }

            float roamWeight = Mathf.Clamp01(_roamChance);
            float interactionWeight = (1f - roamWeight) * Mathf.Max(Definition.interactFrequency, 0f);

            // 本轮不实现 massThrowOnHunt；该标签只允许影响骚灵已有的互动倾向。
            if (Definition.massThrowOnHunt)
            {
                interactionWeight *= Mathf.Max(_massThrowInteractionFrequencyMultiplier, 0f);
            }

            float totalWeight = roamWeight + interactionWeight;
            if (totalWeight <= Mathf.Epsilon)
            {
                EnterState(GhostState.Idle, true);
                return;
            }

            bool shouldRoam = Random.value < roamWeight / totalWeight;
            EnterState(shouldRoam ? GhostState.Roam : GhostState.Interact);
        }

        private void BeginRoam()
        {
            _agent.speed = Definition.walkSpeed;
            _roamWaiting = false;
            ClearPathAndStop();

            if (!TryChooseRoamPoint(out _currentTarget))
            {
                Debug.LogWarning($"[GhostAI:{name}] Roam 没有可用巡逻点，返回 Idle。", this);
                EnterState(GhostState.Idle, true);
                return;
            }

            _hasCurrentTarget = true;
            _lastMovementPosition = transform.position;
            _distanceSinceFootprint = 0f;
        }

        private void UpdateRoam()
        {
            if (!_roamWaiting)
            {
                TrySetDestination(_currentTarget, false);
                UpdateFootprints();

                if (!HasReachedDestination())
                {
                    return;
                }

                ClearPathAndStop();
                _roamWaiting = true;
                _stateTimerEnd = Time.time + RandomRangeOrdered(_roamWaitMin, _roamWaitMax);
                return;
            }

            if (Time.time >= _stateTimerEnd)
            {
                EnterState(GhostState.Idle);
            }
        }

        private void BeginInteract()
        {
            _agent.speed = Definition.walkSpeed;
            ClearPathAndStop();
        }

        private void UpdateInteract()
        {
            _interactionCandidates.Clear();
            Collider[] nearbyColliders = Physics.OverlapSphere(
                transform.position,
                Mathf.Max(_interactRadius, 0f),
                _interactableMask,
                QueryTriggerInteraction.Collide);

            for (int i = 0; i < nearbyColliders.Length; i++)
            {
                Collider nearbyCollider = nearbyColliders[i];
                if (nearbyCollider == null)
                {
                    continue;
                }

                IInteractable interactable = nearbyCollider.GetComponentInParent<IInteractable>();
                Component interactableComponent = interactable as Component;
                if (interactable == null || interactableComponent == null || !interactable.CanInteract)
                {
                    continue;
                }

                Transform targetTransform = interactableComponent.transform;
                if (!_interactionCandidates.Contains(targetTransform))
                {
                    _interactionCandidates.Add(targetTransform);
                }
            }

            if (_interactionCandidates.Count > 0)
            {
                Transform target = _interactionCandidates[Random.Range(0, _interactionCandidates.Count)];
                GameEvents.RaiseGhostInteract(target.position);

                if (Random.value < Mathf.Clamp01(_fingerprintChance))
                {
                    onFingerprintRequest?.Invoke(target);
                }
            }

            EnterState(GhostState.Idle);
        }

        private void BeginHunt()
        {
            _agent.speed = Definition.huntSpeed;
            _huntVisible = true;
            RefreshVisibility();
            _playerCaughtRaised = false;
            _hasLineOfSight = false;
            _reachedLastKnownPosition = false;
            _huntSearchTargetActive = false;
            _checkingHidingSpot = false;
            _lastSightSampleCount = 0;
            _nextSightCheckTime = Time.time;
            _lastSeenTime = Time.time;
            _nextHidingCheckTime = Time.time + Mathf.Max(_hidingCheckInterval, 0f);

            if (_player != null)
            {
                _lastKnownPlayerPosition = _player.position;
                _hasLastKnownPlayerPosition = true;
            }

            CacheDefinitionWaits();
            if (Definition.canSprintBurst)
            {
                _sprintBurstCoroutine = StartCoroutine(SprintBurstRoutine());
            }
        }

        private void UpdateHunt()
        {
            if (Time.time >= _huntEndTime)
            {
                FinishHunt();
                return;
            }

            if (_player == null)
            {
                return;
            }

            if (_playerCaughtRaised)
            {
                return;
            }

            if (_isPlayerHiding)
            {
                UpdateHiddenPlayerSearch();
            }
            else
            {
                if (Time.time >= _nextSightCheckTime)
                {
                    _nextSightCheckTime = Time.time + Mathf.Max(_sightCheckInterval, 0f);
                    _hasLineOfSight = EvaluatePlayerLineOfSight();

                    if (_hasLineOfSight)
                    {
                        _lastSeenTime = Time.time;
                        _lastKnownPlayerPosition = _player.position;
                        _hasLastKnownPlayerPosition = true;
                        _reachedLastKnownPosition = false;
                        _huntSearchTargetActive = false;
                        _checkingHidingSpot = false;
                    }
                }

                if (_hasLineOfSight)
                {
                    TrySetDestination(_player.position, true);
                }
                else
                {
                    UpdateLostPlayerSearch();
                }
            }

            if (!_playerCaughtRaised
                && !_isPlayerHiding
                && Vector3.Distance(transform.position, _player.position) <= Mathf.Max(_catchDistance, 0f))
            {
                _playerCaughtRaised = true;
                ClearPathAndStop();
                GameEvents.RaisePlayerCaught();
            }
        }

        private void UpdateLostPlayerSearch()
        {
            if (!_hasLastKnownPlayerPosition)
            {
                return;
            }

            if (!_reachedLastKnownPosition)
            {
                _currentTarget = _lastKnownPlayerPosition;
                _hasCurrentTarget = true;
                TrySetDestination(_currentTarget, false);

                if (HasReachedDestination()
                    && Time.time - _lastSeenTime > Mathf.Max(_lostSightMemoryDuration, 0f))
                {
                    _reachedLastKnownPosition = true;
                    _huntSearchTargetActive = false;
                }
                return;
            }

            SearchAround(_lastKnownPlayerPosition);
        }

        private void UpdateHiddenPlayerSearch()
        {
            _hasLineOfSight = false;

            if (_checkingHidingSpot)
            {
                TrySetDestination(_hidingCheckTarget, false);
                if (HasReachedDestination())
                {
                    _checkingHidingSpot = false;
                    _lastKnownPlayerPosition = _hidingCheckTarget;
                    _hasLastKnownPlayerPosition = true;
                    _reachedLastKnownPosition = true;
                    _huntSearchTargetActive = false;
                }
                return;
            }

            if (Time.time >= _nextHidingCheckTime)
            {
                _nextHidingCheckTime = Time.time + Mathf.Max(_hidingCheckInterval, 0f);
                if (Random.value < Mathf.Clamp01(_hidingCheckChance))
                {
                    _hidingCheckTarget = _player.position;
                    _checkingHidingSpot = true;
                    _huntSearchTargetActive = false;
                    TrySetDestination(_hidingCheckTarget, false);
                    return;
                }
            }

            Vector3 searchCenter = _hasLastKnownPlayerPosition
                ? _lastKnownPlayerPosition
                : transform.position;
            SearchAround(searchCenter);
        }

        private void SearchAround(Vector3 center)
        {
            if (!_huntSearchTargetActive)
            {
                if (!TryGetRandomNavMeshPoint(center, _huntSearchRadius, out _currentTarget))
                {
                    _currentTarget = center;
                }

                _hasCurrentTarget = true;
                _huntSearchTargetActive = true;
                _hasRequestedDestination = false;
            }

            TrySetDestination(_currentTarget, false);
            if (HasReachedDestination())
            {
                _huntSearchTargetActive = false;
            }
        }

        private bool EvaluatePlayerLineOfSight()
        {
            if (_player == null || _isPlayerHiding)
            {
                _lastSightSampleCount = 0;
                return false;
            }

            Vector3 origin = transform.position + Vector3.up * _ghostSightOriginHeight;
            _lastSightSampleCount = 0;
            bool anyVisible = false;

            if (_playerSightPoints != null && _playerSightPoints.Length > 0)
            {
                for (int i = 0; i < _playerSightPoints.Length && _lastSightSampleCount < SightSampleCount; i++)
                {
                    Transform sightPoint = _playerSightPoints[i];
                    if (sightPoint == null)
                    {
                        continue;
                    }

                    anyVisible |= EvaluateSightRay(origin, sightPoint.position, _lastSightSampleCount);
                    _lastSightSampleCount++;
                }
            }

            if (_lastSightSampleCount > 0)
            {
                return anyVisible;
            }

            Vector3 playerBase = _player.position;
            anyVisible |= EvaluateSightRay(
                origin,
                playerBase + Vector3.up * _fallbackSightUpperHeight,
                _lastSightSampleCount++);
            anyVisible |= EvaluateSightRay(
                origin,
                playerBase + Vector3.up * _fallbackSightMiddleHeight,
                _lastSightSampleCount++);
            anyVisible |= EvaluateSightRay(origin, playerBase, _lastSightSampleCount++);
            return anyVisible;
        }

        private bool EvaluateSightRay(Vector3 origin, Vector3 target, int sampleIndex)
        {
            bool blocked = Physics.Linecast(
                origin,
                target,
                _sightBlockMask,
                QueryTriggerInteraction.Ignore);

            _lastSightTargets[sampleIndex] = target;
            _lastSightBlocked[sampleIndex] = blocked;
            return !blocked;
        }

        private void FinishHunt()
        {
            GameEvents.RaiseHuntEnd();
            EnterState(GhostState.Idle);
        }

        private IEnumerator SprintBurstRoutine()
        {
            yield return _sprintBurstIntervalWait;

            while (State == GhostState.Hunt)
            {
                _agent.speed = Definition.huntSpeed * Mathf.Max(_sprintBurstMultiplier, 0f);
                yield return _sprintBurstDurationWait;

                if (State == GhostState.Hunt)
                {
                    _agent.speed = Definition.huntSpeed;
                }

                yield return _sprintBurstCooldownWait;
            }

            _sprintBurstCoroutine = null;
        }

        private IEnumerator GhostEventVisibilityRoutine()
        {
            _ghostEventVisible = true;
            RefreshVisibility();
            yield return _ghostEventVisibleWait;
            _ghostEventVisible = false;
            RefreshVisibility();
            _ghostEventCoroutine = null;
        }

        private bool TrySetDestination(Vector3 target, bool refreshAtInterval)
        {
            _currentTarget = target;
            _hasCurrentTarget = true;

            float threshold = Mathf.Max(_destinationChangeThreshold, 0f);
            bool targetChanged = !_hasRequestedDestination
                || (_lastRequestedDestination - target).sqrMagnitude > threshold * threshold;
            bool intervalElapsed = refreshAtInterval && Time.time >= _nextDestinationUpdateTime;

            if (_hasRequestedDestination && !targetChanged && !intervalElapsed)
            {
                return true;
            }

            if (!_hasRequestedDestination && Time.time < _nextDestinationUpdateTime)
            {
                return false;
            }

            float retryInterval = Mathf.Max(_destinationUpdateInterval, 0f);
            _nextDestinationUpdateTime = Time.time + retryInterval;

            if (!_agent.enabled || !_agent.isOnNavMesh)
            {
                WarnNavMeshIssue(
                    $"状态 {State} 下 NavMeshAgent 不在 NavMesh 上，暂不下发目标 {target}，下一次 tick 重试。");
                _hasRequestedDestination = false;
                return false;
            }

            if (_agent.pathPending)
            {
                return false;
            }

            _agent.isStopped = false;
            bool accepted = _agent.SetDestination(target);
            if (!accepted)
            {
                Debug.LogWarning(
                    $"[GhostAI:{name}] SetDestination 返回 false；状态 {State}，目标坐标 {target} 无法映射到 NavMesh。",
                    this);
                _hasRequestedDestination = false;
                return false;
            }

            _lastRequestedDestination = target;
            _hasRequestedDestination = true;
            return true;
        }

        private bool HasReachedDestination()
        {
            if (_agent == null
                || !_agent.enabled
                || !_hasRequestedDestination
                || !_agent.isOnNavMesh
                || _agent.pathPending)
            {
                return false;
            }

            float arrivalDistance = _agent.stoppingDistance + Mathf.Max(_arrivalTolerance, 0f);
            return !float.IsInfinity(_agent.remainingDistance)
                && _agent.remainingDistance <= arrivalDistance;
        }

        private void ClearPathAndStop()
        {
            _hasRequestedDestination = false;
            _nextDestinationUpdateTime = 0f;

            if (_agent == null || !_agent.enabled || !_agent.isOnNavMesh)
            {
                return;
            }

            _agent.isStopped = true;
            _agent.ResetPath();
        }

        private bool TryGetRandomNavMeshPoint(Vector3 center, float radius, out Vector3 point)
        {
            float safeRadius = Mathf.Max(radius, 0f);
            float sampleRadius = Mathf.Max(_navMeshSampleRadius, 0f);
            int attempts = Mathf.Max(_randomPointSampleAttempts, 1);

            for (int i = 0; i < attempts; i++)
            {
                Vector2 offset = Random.insideUnitCircle * safeRadius;
                Vector3 candidate = center + new Vector3(offset.x, 0f, offset.y);

                if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, sampleRadius, _agent.areaMask))
                {
                    point = hit.position;
                    return true;
                }
            }

            point = center;
            return false;
        }

        private bool TryChooseRoamPoint(out Vector3 point)
        {
            if (_roamPoints == null || _roamPoints.Length == 0)
            {
                point = transform.position;
                return false;
            }

            int startIndex = Random.Range(0, _roamPoints.Length);
            for (int offset = 0; offset < _roamPoints.Length; offset++)
            {
                int index = (startIndex + offset) % _roamPoints.Length;
                if (_roamPoints[index] != null)
                {
                    point = _roamPoints[index].position;
                    return true;
                }
            }

            point = transform.position;
            return false;
        }

        private void UpdateFootprints()
        {
            Vector3 currentPosition = transform.position;
            _distanceSinceFootprint += Vector3.Distance(_lastMovementPosition, currentPosition);
            _lastMovementPosition = currentPosition;

            if (!Definition.leavesFootprints
                || _footprintPrefab == null
                || _footprintSpacing <= Mathf.Epsilon
                || _distanceSinceFootprint < _footprintSpacing)
            {
                return;
            }

            _distanceSinceFootprint = 0f;
            Vector3 rayOrigin = currentPosition + Vector3.up * _footprintRayOriginHeight;
            if (!Physics.Raycast(
                    rayOrigin,
                    Vector3.down,
                    out RaycastHit hit,
                    Mathf.Max(_footprintGroundCheckDistance, 0f),
                    _footprintGroundMask,
                    QueryTriggerInteraction.Ignore))
            {
                return;
            }

            Quaternion rotation = Quaternion.FromToRotation(Vector3.up, hit.normal) * transform.rotation;
            Vector3 spawnPosition = hit.point + hit.normal * _footprintSurfaceOffset;
            GameObject footprint = Instantiate(_footprintPrefab, spawnPosition, rotation);
            _spawnedFootprints.Add(footprint);
            Destroy(footprint, Mathf.Max(_footprintLifetime, 0f));

            for (int i = _spawnedFootprints.Count - 1; i >= 0; i--)
            {
                if (_spawnedFootprints[i] == null)
                {
                    _spawnedFootprints.RemoveAt(i);
                }
            }
        }

        private void ClearSpawnedFootprints()
        {
            for (int i = _spawnedFootprints.Count - 1; i >= 0; i--)
            {
                if (_spawnedFootprints[i] != null)
                {
                    Destroy(_spawnedFootprints[i]);
                }
            }

            _spawnedFootprints.Clear();
        }

        private void HandleHidingChanged(bool isHiding)
        {
            _isPlayerHiding = isHiding;
            _hasLineOfSight = false;
            _lastSightSampleCount = 0;

            if (isHiding)
            {
                _checkingHidingSpot = false;
                _huntSearchTargetActive = false;
                _nextHidingCheckTime = Time.time + Mathf.Max(_hidingCheckInterval, 0f);
            }
            else
            {
                _reachedLastKnownPosition = false;
                _nextSightCheckTime = Time.time;
            }
        }

        private void CacheLocalWaits()
        {
            _ghostEventVisibleWait = new WaitForSeconds(Mathf.Max(_ghostEventVisibleDuration, 0f));
            _sprintBurstDurationWait = new WaitForSeconds(Mathf.Max(_sprintBurstDuration, 0f));
        }

        private void CacheDefinitionWaits()
        {
            CacheLocalWaits();
            float interval = Mathf.Max(Definition.sprintBurstInterval, 0f);
            float duration = Mathf.Max(_sprintBurstDuration, 0f);
            _sprintBurstIntervalWait = new WaitForSeconds(interval);
            _sprintBurstCooldownWait = new WaitForSeconds(Mathf.Max(interval - duration, 0f));
        }

        private void WarnNavMeshIssue(string message)
        {
            if (Time.time < _nextNavMeshWarningTime)
            {
                return;
            }

            _nextNavMeshWarningTime = Time.time + Mathf.Max(_navMeshWarningInterval, 0f);
            Debug.LogWarning($"[GhostAI:{name}] {message}", this);
        }

        private float RandomRangeOrdered(float first, float second)
        {
            float minimum = Mathf.Max(Mathf.Min(first, second), 0f);
            float maximum = Mathf.Max(Mathf.Max(first, second), minimum);
            return Random.Range(minimum, maximum);
        }

        private void RefreshVisibility()
        {
            SetVisible(_huntVisible || _ghostEventVisible);
        }

        private void SetVisible(bool visible)
        {
            if (_renderers == null)
            {
                return;
            }

            for (int i = 0; i < _renderers.Length; i++)
            {
                if (_renderers[i] != null)
                {
                    _renderers[i].enabled = visible;
                }
            }
        }

        private void OnValidate()
        {
            _roamChance = Mathf.Clamp01(_roamChance);
            _fingerprintChance = Mathf.Clamp01(_fingerprintChance);
            _hidingCheckChance = Mathf.Clamp01(_hidingCheckChance);
            _randomPointSampleAttempts = Mathf.Max(_randomPointSampleAttempts, 1);

            if (Application.isPlaying)
            {
                CacheLocalWaits();
                if (Definition != null)
                {
                    CacheDefinitionWaits();
                }
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (_hasCurrentTarget)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawLine(transform.position, _currentTarget);
                Gizmos.DrawWireSphere(_currentTarget, _gizmoTargetRadius);
            }

            Vector3 sightOrigin = transform.position + Vector3.up * _ghostSightOriginHeight;
            for (int i = 0; i < _lastSightSampleCount; i++)
            {
                Gizmos.color = _lastSightBlocked[i] ? Color.red : Color.green;
                Gizmos.DrawLine(sightOrigin, _lastSightTargets[i]);
            }

#if UNITY_EDITOR
            UnityEditor.Handles.Label(
                transform.position + Vector3.up * _gizmoLabelHeight,
                $"Ghost: {State}");
#endif
        }
    }
}
