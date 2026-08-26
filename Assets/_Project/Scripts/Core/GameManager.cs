using System.Collections.Generic;
using UnityEngine;
using Residuum.Evidence;

namespace Residuum.Core
{
    /// <summary>
    /// 单局流程编排器。GameManager 是唯一允许直接持有各模块引用的组件。
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-100)]
    public sealed class GameManager : MonoBehaviour
    {
        [Header("回合依赖")]
        [SerializeField]
        [Tooltip("本垂直切片可随机选择的全部鬼种定义；空引用会被跳过。")]
        private Residuum.Ghost.GhostDefinition[] _allGhosts;

        [SerializeField]
        [Tooltip("本局唯一的 GhostAI，由 GameManager 在开局时注入鬼种并传送到鬼房。")]
        private Residuum.Ghost.GhostAI _ghostAI;

        [SerializeField]
        [Tooltip("本局猎杀控制器，用于阻止猎杀期间撤离及触发编辑器强制猎杀。")]
        private Residuum.Ghost.HuntController _huntController;

        [SerializeField]
        [Tooltip("玩家理智组件；结算时读取 Current，未注入时理智按 0 处理。")]
        private Residuum.Player.PlayerSanity _sanity;

        [SerializeField]
        [Tooltip("玩家根节点，用于按间隔检测是否进入出口区域。")]
        private Transform _player;

        [Header("撤离")]
        [SerializeField]
        [Tooltip("玩家进入后尝试撤离的出口触发区域；也可由外部直接调用 RequestEvacuate。")]
        private Collider _exitZone;

        [SerializeField]
        [Min(0f)]
        [Tooltip("两次出口区域检测之间的间隔秒数；设为 0 时每帧检测。")]
        private float _exitCheckInterval = 0.2f;

        [SerializeField]
        [Tooltip("猎杀期间尝试撤离时触发，供 UI 提示连线。")]
        private UnityEngine.Events.UnityEvent _onEvacuateBlocked =
            new UnityEngine.Events.UnityEvent();

        [Header("结算")]
        [SerializeField]
        [Range(0f, 100f)]
        [Tooltip("正确判定且证据充足时，获得 Perfect 所需严格高于的理智值。")]
        private float _perfectSanityThreshold = 30f;

        [SerializeField]
        [Min(0)]
        [Tooltip("正确判定时，获得 Perfect 所需的最少已发现证据数量。")]
        private int _perfectEvidenceCount = 2;

        [SerializeField]
        [Tooltip("玩家被抓时触发，供死亡表现层连线。")]
        private UnityEngine.Events.UnityEvent _onPlayerDied =
            new UnityEngine.Events.UnityEvent();

#if UNITY_EDITOR
        [Header("编辑器调试")]
        [SerializeField]
        [Tooltip("编辑器中强制指定本局鬼种；留空时仍从全部鬼种中随机选择。")]
        private Residuum.Ghost.GhostDefinition _forcedGhost;

        [SerializeField]
        [Tooltip("编辑器中无视理智与冷却并强制触发猎杀的快捷键。")]
        private UnityEngine.InputSystem.Key _forceHuntKey = UnityEngine.InputSystem.Key.F9;

        [SerializeField]
        [Tooltip("编辑器中绕过出口与猎杀门禁，直接按当前判定结果结束本局的快捷键。")]
        private UnityEngine.InputSystem.Key _endRoundKey = UnityEngine.InputSystem.Key.F10;

        [SerializeField]
        [Tooltip("编辑器中一键把三种证据全部记为已发现的快捷键。")]
        private UnityEngine.InputSystem.Key _collectAllEvidenceKey = UnityEngine.InputSystem.Key.F11;
#endif

        private readonly HashSet<EvidenceType> _foundEvidence = new HashSet<EvidenceType>();
        private Residuum.Ghost.GhostDefinition _selectedGhost;
        private Residuum.Ghost.GhostDefinition _submittedGuess;
        private float _roundStartTime;
        private float _nextExitCheckTime;
        private bool _isRoundActive;
        private bool _wasPlayerInsideExit;

        public int FoundCount => _foundEvidence.Count;

        private void OnEnable()
        {
            GameEvents.OnEvidenceFound += HandleEvidenceFound;
            GameEvents.OnPlayerCaught += HandlePlayerCaught;
        }

        private void Start()
        {
            StartRound();
        }

        private void Update()
        {
#if UNITY_EDITOR
            HandleEditorShortcuts();
#endif

            if (!_isRoundActive || Time.time < _nextExitCheckTime)
            {
                return;
            }

            _nextExitCheckTime = Time.time + Mathf.Max(_exitCheckInterval, 0f);
            bool isPlayerInsideExit = IsPlayerInsideExitZone();
            if (isPlayerInsideExit && !_wasPlayerInsideExit)
            {
                RequestEvacuate();
            }

            _wasPlayerInsideExit = isPlayerInsideExit;
        }

        private void OnDisable()
        {
            GameEvents.OnEvidenceFound -= HandleEvidenceFound;
            GameEvents.OnPlayerCaught -= HandlePlayerCaught;
        }

        private void OnDestroy()
        {
            // 防御性退订，避免异常销毁顺序让静态事件保留失效委托。
            GameEvents.OnEvidenceFound -= HandleEvidenceFound;
            GameEvents.OnPlayerCaught -= HandlePlayerCaught;

            _isRoundActive = false;
            _foundEvidence.Clear();
            _selectedGhost = null;
            _submittedGuess = null;
            GameEvents.SetGhostEvidence(null);
            GameEvents.GhostRoomCenter = Vector3.zero;
            GameEvents.GhostRoomRadius = 0f;
        }

        /// <summary>完成全部依赖初始化后开始一局游戏。</summary>
        public void StartRound()
        {
            if (_isRoundActive)
            {
                Debug.LogWarning("[GameManager] 当前回合仍在进行，已忽略重复的 StartRound 调用。", this);
                return;
            }

            if (!ValidateRequiredDependencies())
            {
                AbortRoundStart();
                return;
            }

            Residuum.Ghost.GhostDefinition definition = SelectGhostDefinition();
            if (definition == null)
            {
                AbortRoundStart();
                return;
            }

            _selectedGhost = definition;
            _ghostAI.Definition = definition;
            GameEvents.SetGhostEvidence(definition.evidences);

            Residuum.World.RoomManager roomManager = Residuum.World.RoomManager.Instance;
            roomManager.SelectGhostRoom();
            Residuum.World.RoomVolume ghostRoom = roomManager.GhostRoom;
            if (ghostRoom == null)
            {
                Debug.LogError("[GameManager] RoomManager 未能选出鬼房，已中止开局。", this);
                AbortRoundStart();
                return;
            }

            Collider ghostRoomCollider = ghostRoom.GetComponent<Collider>();
            if (ghostRoomCollider == null)
            {
                Debug.LogError(
                    $"[GameManager] 鬼房“{ghostRoom.RoomName}”缺少 Collider，已中止开局。",
                    ghostRoom);
                AbortRoundStart();
                return;
            }

            GameEvents.GhostRoomCenter = ghostRoomCollider.bounds.center;
            GameEvents.GhostRoomRadius = ghostRoomCollider.bounds.extents.magnitude;
            if (GameEvents.GhostRoomRadius <= 0f)
            {
                Debug.LogError(
                    $"[GameManager] 鬼房“{ghostRoom.RoomName}”的 Collider 范围为零，已中止开局。",
                    ghostRoomCollider);
                AbortRoundStart();
                return;
            }

            Vector3 ghostSpawnPoint = ghostRoom.GetRandomPointInside();
            _ghostAI.WarpTo(ghostSpawnPoint);

            _foundEvidence.Clear();
            _submittedGuess = null;
            _roundStartTime = Time.time;
            _nextExitCheckTime = Time.time + Mathf.Max(_exitCheckInterval, 0f);
            _wasPlayerInsideExit = IsPlayerInsideExitZone();
            _isRoundActive = true;

            Debug.Log(
                $"[GameManager] 回合开始：鬼种“{definition.ghostName}”" +
                $"（{definition.displayNameEN}），鬼房“{ghostRoom.RoomName}”。",
                this);

            // 必须最后广播；此时鬼种、证据、鬼房范围和鬼的位置均已就绪。
            GameEvents.RaiseRoundStart();
        }

        /// <summary>记录笔记本提交的鬼种判定，直到撤离时才参与结算。</summary>
        public void SubmitGuess(Residuum.Ghost.GhostDefinition guess)
        {
            if (!_isRoundActive)
            {
                Debug.LogWarning("[GameManager] 当前没有进行中的回合，已忽略鬼种判定。", this);
                return;
            }

            _submittedGuess = guess;
        }

        /// <summary>由出口检测或外部 UnityEvent 调用，尝试撤离并结算。</summary>
        public void RequestEvacuate()
        {
            if (!_isRoundActive)
            {
                return;
            }

            if (_huntController == null)
            {
                Debug.LogError(
                    "[GameManager] HuntController 引用已丢失，无法安全判定是否允许撤离。",
                    this);
                return;
            }

            if (_huntController.IsHunting)
            {
                _onEvacuateBlocked?.Invoke();
                return;
            }

            EndRound(EvaluateAliveResult());
        }

        private bool ValidateRequiredDependencies()
        {
            bool isValid = true;

            if (_ghostAI == null)
            {
                Debug.LogError("[GameManager] 未在 Inspector 注入 GhostAI，无法开始回合。", this);
                isValid = false;
            }

            if (_huntController == null)
            {
                Debug.LogError("[GameManager] 未在 Inspector 注入 HuntController，无法保证撤离规则。", this);
                isValid = false;
            }

            if (Residuum.World.RoomManager.Instance == null)
            {
                Debug.LogError("[GameManager] 场景中没有可用的 RoomManager.Instance，无法选择鬼房。", this);
                isValid = false;
            }

            if (_player == null)
            {
                Debug.LogWarning(
                    "[GameManager] 未注入玩家 Transform，无法自动检测出口；仍可由外部调用 RequestEvacuate。",
                    this);
            }

            if (_exitZone == null)
            {
                Debug.LogWarning(
                    "[GameManager] 未注入出口 Collider，无法自动检测出口；仍可由外部调用 RequestEvacuate。",
                    this);
            }

            return isValid;
        }

        private Residuum.Ghost.GhostDefinition SelectGhostDefinition()
        {
#if UNITY_EDITOR
            if (_forcedGhost != null)
            {
                return _forcedGhost;
            }
#endif

            if (_allGhosts == null || _allGhosts.Length == 0)
            {
                Debug.LogError("[GameManager] 鬼种列表为空，无法开始回合。", this);
                return null;
            }

            int validGhostCount = 0;
            foreach (Residuum.Ghost.GhostDefinition ghost in _allGhosts)
            {
                if (ghost != null)
                {
                    validGhostCount++;
                }
            }

            if (validGhostCount == 0)
            {
                Debug.LogError("[GameManager] 鬼种列表中没有有效的 GhostDefinition，无法开始回合。", this);
                return null;
            }

            int selectedIndex = Random.Range(0, validGhostCount);
            foreach (Residuum.Ghost.GhostDefinition ghost in _allGhosts)
            {
                if (ghost == null)
                {
                    continue;
                }

                if (selectedIndex == 0)
                {
                    return ghost;
                }

                selectedIndex--;
            }

            return null;
        }

        private void HandleEvidenceFound(EvidenceType evidence)
        {
            if (!_isRoundActive || evidence == EvidenceType.None)
            {
                return;
            }

            _foundEvidence.Add(evidence);
        }

        private void HandlePlayerCaught()
        {
            if (!_isRoundActive)
            {
                return;
            }

            _onPlayerDied?.Invoke();
            EndRound(RoundResult.Died);
        }

        private bool IsPlayerInsideExitZone()
        {
            return _player != null
                && _exitZone != null
                && _exitZone.bounds.Contains(_player.position);
        }

        private RoundResult EvaluateAliveResult()
        {
            bool guessedCorrectly = _selectedGhost != null && _submittedGuess == _selectedGhost;
            if (!guessedCorrectly)
            {
                return RoundResult.Survived;
            }

            float currentSanity = 0f;
            if (_sanity == null)
            {
                Debug.LogWarning(
                    "[GameManager] 未注入 PlayerSanity，结算时按理智 0 处理。",
                    this);
            }
            else
            {
                currentSanity = _sanity.Current;
            }

            if (FoundCount >= _perfectEvidenceCount && currentSanity > _perfectSanityThreshold)
            {
                return RoundResult.Perfect;
            }

            return RoundResult.Success;
        }

        private void EndRound(RoundResult result)
        {
            if (!_isRoundActive)
            {
                return;
            }

            _isRoundActive = false;
            float elapsedSeconds = Mathf.Max(Time.time - _roundStartTime, 0f);

            Debug.Log(
                $"[GameManager] 回合结束：{result}，发现证据 {FoundCount} 项，" +
                $"用时 {elapsedSeconds:F1} 秒。",
                this);

            GameEvents.RaiseRoundEnd(result);
        }

        private void AbortRoundStart()
        {
            _isRoundActive = false;
            _foundEvidence.Clear();
            _selectedGhost = null;
            _submittedGuess = null;

            if (_ghostAI != null)
            {
                _ghostAI.Definition = null;
            }

            GameEvents.SetGhostEvidence(null);
            GameEvents.GhostRoomCenter = Vector3.zero;
            GameEvents.GhostRoomRadius = 0f;
        }

#if UNITY_EDITOR
        private void HandleEditorShortcuts()
        {
            if (!_isRoundActive || UnityEngine.InputSystem.Keyboard.current == null)
            {
                return;
            }

            UnityEngine.InputSystem.Keyboard keyboard = UnityEngine.InputSystem.Keyboard.current;

            if (WasKeyPressed(keyboard, _forceHuntKey))
            {
                if (_huntController == null)
                {
                    Debug.LogError("[GameManager] HuntController 引用已丢失，F9 无法触发猎杀。", this);
                }
                else
                {
                    _huntController.ForceHunt();
                }
            }

            if (WasKeyPressed(keyboard, _collectAllEvidenceKey))
            {
                _foundEvidence.Add(EvidenceType.EMF5);
                _foundEvidence.Add(EvidenceType.UVFingerprint);
                _foundEvidence.Add(EvidenceType.GhostWriting);
            }

            if (WasKeyPressed(keyboard, _endRoundKey))
            {
                EndRound(EvaluateAliveResult());
            }
        }

        private static bool WasKeyPressed(
            UnityEngine.InputSystem.Keyboard keyboard,
            UnityEngine.InputSystem.Key key)
        {
            return key != UnityEngine.InputSystem.Key.None
                && keyboard[key].wasPressedThisFrame;
        }
#endif
    }
}
