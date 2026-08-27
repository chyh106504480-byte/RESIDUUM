using UnityEngine;
using System.Collections.Generic;
using TMPro;
using UnityEngine.UI;
using Residuum.Core;
using Residuum.Evidence;

namespace Residuum.UI
{
    [System.Serializable]
    public class GhostGuessEvent :
        UnityEngine.Events.UnityEvent<Residuum.Ghost.GhostDefinition>
    {
    }

    /// <summary>
    /// 笔记本界面：显示证据状态、鬼种推理表，并通过 UnityEvent 提交玩家判定。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class JournalUI : MonoBehaviour
    {
        private const int EvidenceCount = 3;
        private const int EvidenceColumnsPerGhost = 3;

        private static readonly EvidenceType[] EvidenceTypes =
        {
            EvidenceType.EMF5,
            EvidenceType.UVFingerprint,
            EvidenceType.GhostWriting
        };

        [Header("开关与依赖")]
        [Tooltip("笔记本的界面根对象。该对象必须与 JournalUI 所在对象分开，以便关闭后仍能检测 Tab。")]
        [SerializeField] private GameObject _journalRoot;

        [Tooltip("打开或关闭笔记本的按键。")]
        [SerializeField] private UnityEngine.InputSystem.Key _toggleKey =
            UnityEngine.InputSystem.Key.Tab;

        [Tooltip("拖入玩家身上的 PlayerController 组件。笔记本打开时会禁用它，" +
                 "从而同时停掉移动、视角与光标锁定；关闭时恢复")]
        [SerializeField] private MonoBehaviour _playerControllerBehaviour;

        [Tooltip("场景里的 EvidenceManager")]
        [SerializeField] private Residuum.Evidence.EvidenceManager _evidenceManager;

        [Tooltip("三种鬼的定义资产，用于绘制推理表。顺序即表格行序")]
        [SerializeField] private Residuum.Ghost.GhostDefinition[] _allGhosts;

        [Header("证据清单")]
        [Tooltip("三项证据的文字行，顺序固定为 EMF-5、紫外线指纹、鬼影书写。")]
        [SerializeField] private TextMeshProUGUI[] _evidenceLabels;

        [Tooltip("三项证据的排除按钮，顺序必须与证据文字行一致。")]
        [SerializeField] private Button[] _evidenceRuleButtons;

        [Tooltip("已发现证据显示在名称前的标记。")]
        [SerializeField] private string _foundPrefix = "✓ ";

        [Tooltip("已排除证据显示在名称前的标记。")]
        [SerializeField] private string _ruledPrefix = "✗ ";

        [Tooltip("状态未知的证据显示在名称前的占位。")]
        [SerializeField] private string _unknownPrefix = "  ";

        [Tooltip("已发现证据的文字颜色。")]
        [SerializeField] private Color _foundColor = Color.white;

        [Tooltip("已排除证据的文字颜色。")]
        [SerializeField] private Color _ruledColor = new Color(0.55f, 0.2f, 0.2f, 1f);

        [Tooltip("状态未知的证据文字颜色。")]
        [SerializeField] private Color _unknownColor = new Color(0.45f, 0.45f, 0.45f, 1f);

        [Header("鬼种推理表")]
        [Tooltip("每种鬼的名称文字，顺序必须与鬼种定义数组一致。")]
        [SerializeField] private TextMeshProUGUI[] _ghostNameLabels;

        [Tooltip("鬼种的证据格，按行优先排列；每行依次为 EMF-5、紫外线指纹、鬼影书写。")]
        [SerializeField] private TextMeshProUGUI[] _ghostEvidenceCells;

        [Tooltip("推理表中表示该鬼持有某项证据的文字。")]
        [SerializeField] private string _hasEvidenceMark = "✓";

        [Tooltip("推理表中表示该鬼不持有某项证据的文字。")]
        [SerializeField] private string _missingEvidenceMark = "—";

        [Tooltip("已被排除出候选列表的整行颜色乘数。")]
        [SerializeField] private Color _ruledOutRowTint = new Color(1f, 1f, 1f, 0.35f);

        [Header("判定提交")]
        [Tooltip("每种鬼的判定按钮，顺序必须与鬼种定义数组一致。")]
        [SerializeField] private Button[] _guessButtons;

        [Tooltip("玩家当前选择的鬼种整行颜色乘数。")]
        [SerializeField] private Color _selectedGuessTint = new Color(0.35f, 0.85f, 0.4f, 1f);

        [Tooltip("在 Inspector 里连到 GameManager.SubmitGuess")]
        public GhostGuessEvent onGuessSubmitted = new GhostGuessEvent();

        private UnityEngine.Events.UnityAction[] _evidenceRuleActions;
        private UnityEngine.Events.UnityAction[] _guessActions;
        private Color[] _ghostNameBaseColors;
        private Color[] _ghostEvidenceBaseColors;
        private Residuum.Ghost.GhostDefinition _selectedGuess;
        private bool _buttonListenersAttached;
        private bool _isJournalOpen;
        private bool _isHunting;
        private bool _isRoundActive = true;

        private void Awake()
        {
            if (!ValidateReferences())
            {
                if (_journalRoot != null &&
                    !transform.IsChildOf(_journalRoot.transform))
                {
                    _journalRoot.SetActive(false);
                }

                enabled = false;
                return;
            }

            CacheBaseColors();
            BuildButtonActions();

            _journalRoot.SetActive(false);
            _isJournalOpen = false;
        }

        private void OnEnable()
        {
            GameEvents.OnHuntStart += HandleHuntStart;
            GameEvents.OnHuntEnd += HandleHuntEnd;
            GameEvents.OnRoundStart += HandleRoundStart;
            GameEvents.OnRoundEnd += HandleRoundEnd;
            GameEvents.OnEvidenceFound += HandleEvidenceFound;
            _evidenceManager.onCandidatesChanged?.AddListener(HandleCandidatesChanged);

            AddButtonListeners();
        }

        private void Update()
        {
            UnityEngine.InputSystem.Keyboard keyboard =
                UnityEngine.InputSystem.Keyboard.current;

            if (keyboard == null ||
                _toggleKey == UnityEngine.InputSystem.Key.None ||
                !keyboard[_toggleKey].wasPressedThisFrame)
            {
                return;
            }

            if (_isJournalOpen)
            {
                SetJournalOpen(false);
                return;
            }

            if (!_isHunting && _isRoundActive)
            {
                SetJournalOpen(true);
            }
        }

        private void OnDisable()
        {
            UnsubscribeFromEvents();
            RemoveButtonListeners();

            if (_isJournalOpen)
            {
                SetJournalOpen(false);
            }
        }

        private void OnDestroy()
        {
            // 防御性清理，避免异常销毁顺序让静态事件或按钮保留失效委托。
            UnsubscribeFromEvents();
            RemoveButtonListeners();

            if (_isJournalOpen)
            {
                SetJournalOpen(false);
            }

            _selectedGuess = null;
            _evidenceRuleActions = null;
            _guessActions = null;
            _ghostNameBaseColors = null;
            _ghostEvidenceBaseColors = null;
        }

        private void HandleHuntStart(float duration)
        {
            _isHunting = true;

            if (_isJournalOpen)
            {
                SetJournalOpen(false);
            }
        }

        private void HandleHuntEnd()
        {
            _isHunting = false;
        }

        private void HandleRoundStart()
        {
            _isRoundActive = true;
            _isHunting = false;
            _selectedGuess = null;
            RefreshAllIfOpen();
        }

        private void HandleRoundEnd(RoundResult result)
        {
            _isRoundActive = false;

            if (_isJournalOpen)
            {
                SetJournalOpen(false);
            }
        }

        private void HandleEvidenceFound(EvidenceType type)
        {
            RefreshAllIfOpen();
        }

        private void HandleCandidatesChanged()
        {
            RefreshAllIfOpen();
        }

        private void HandleEvidenceRuleClicked(int evidenceIndex)
        {
            if (evidenceIndex < 0 || evidenceIndex >= EvidenceTypes.Length)
            {
                return;
            }

            EvidenceType type = EvidenceTypes[evidenceIndex];
            if (_evidenceManager.IsFound(type))
            {
                return;
            }

            if (_evidenceManager.IsRuled(type))
            {
                _evidenceManager.ClearRuled(type);
            }
            else
            {
                _evidenceManager.MarkEvidenceRuled(type);
            }
        }

        private void HandleGuessClicked(int ghostIndex)
        {
            if (ghostIndex < 0 || ghostIndex >= _allGhosts.Length)
            {
                return;
            }

            _selectedGuess = _allGhosts[ghostIndex];

            if (_isJournalOpen)
            {
                RefreshGhostRowColors();
            }

            onGuessSubmitted?.Invoke(_selectedGuess);
        }

        private void SetJournalOpen(bool shouldOpen)
        {
            if (_isJournalOpen == shouldOpen)
            {
                return;
            }

            _isJournalOpen = shouldOpen;
            _journalRoot.SetActive(shouldOpen);

            if (_playerControllerBehaviour != null)
            {
                _playerControllerBehaviour.enabled = !shouldOpen;
            }

            if (shouldOpen)
            {
                RefreshAll();
            }
        }

        private void RefreshAllIfOpen()
        {
            if (_isJournalOpen)
            {
                RefreshAll();
            }
        }

        private void RefreshAll()
        {
            RefreshEvidenceList();
            RefreshGhostTable();
        }

        private void RefreshEvidenceList()
        {
            for (int i = 0; i < EvidenceTypes.Length; i++)
            {
                EvidenceType type = EvidenceTypes[i];
                TextMeshProUGUI label = _evidenceLabels[i];

                if (_evidenceManager.IsFound(type))
                {
                    label.text = _foundPrefix + GetEvidenceDisplayName(type);
                    label.color = _foundColor;
                }
                else if (_evidenceManager.IsRuled(type))
                {
                    label.text = _ruledPrefix + GetEvidenceDisplayName(type);
                    label.color = _ruledColor;
                }
                else
                {
                    label.text = _unknownPrefix + GetEvidenceDisplayName(type);
                    label.color = _unknownColor;
                }
            }
        }

        private void RefreshGhostTable()
        {
            for (int ghostIndex = 0; ghostIndex < _allGhosts.Length; ghostIndex++)
            {
                Residuum.Ghost.GhostDefinition definition = _allGhosts[ghostIndex];
                _ghostNameLabels[ghostIndex].text = definition.ghostName;

                int rowStart = ghostIndex * EvidenceColumnsPerGhost;
                for (int evidenceIndex = 0;
                     evidenceIndex < EvidenceColumnsPerGhost;
                     evidenceIndex++)
                {
                    _ghostEvidenceCells[rowStart + evidenceIndex].text =
                        definition.HasEvidence(EvidenceTypes[evidenceIndex])
                            ? _hasEvidenceMark
                            : _missingEvidenceMark;
                }
            }

            RefreshGhostRowColors();
        }

        private void RefreshGhostRowColors()
        {
            for (int ghostIndex = 0; ghostIndex < _allGhosts.Length; ghostIndex++)
            {
                Residuum.Ghost.GhostDefinition definition = _allGhosts[ghostIndex];
                Color rowTint = Color.white;

                if (!IsCandidate(definition))
                {
                    rowTint *= _ruledOutRowTint;
                }

                if (definition == _selectedGuess)
                {
                    rowTint *= _selectedGuessTint;
                }

                _ghostNameLabels[ghostIndex].color =
                    _ghostNameBaseColors[ghostIndex] * rowTint;

                int rowStart = ghostIndex * EvidenceColumnsPerGhost;
                for (int evidenceIndex = 0;
                     evidenceIndex < EvidenceColumnsPerGhost;
                     evidenceIndex++)
                {
                    int cellIndex = rowStart + evidenceIndex;
                    _ghostEvidenceCells[cellIndex].color =
                        _ghostEvidenceBaseColors[cellIndex] * rowTint;
                }
            }
        }

        private bool IsCandidate(Residuum.Ghost.GhostDefinition definition)
        {
            IReadOnlyList<Residuum.Ghost.GhostDefinition> candidates =
                _evidenceManager.Candidates;

            for (int i = 0; i < candidates.Count; i++)
            {
                if (candidates[i] == definition)
                {
                    return true;
                }
            }

            return false;
        }

        private void CacheBaseColors()
        {
            _ghostNameBaseColors = new Color[_ghostNameLabels.Length];
            for (int i = 0; i < _ghostNameLabels.Length; i++)
            {
                _ghostNameBaseColors[i] = _ghostNameLabels[i].color;
            }

            _ghostEvidenceBaseColors = new Color[_ghostEvidenceCells.Length];
            for (int i = 0; i < _ghostEvidenceCells.Length; i++)
            {
                _ghostEvidenceBaseColors[i] = _ghostEvidenceCells[i].color;
            }
        }

        private void BuildButtonActions()
        {
            _evidenceRuleActions =
                new UnityEngine.Events.UnityAction[_evidenceRuleButtons.Length];
            for (int i = 0; i < _evidenceRuleButtons.Length; i++)
            {
                int capturedIndex = i;
                _evidenceRuleActions[i] =
                    () => HandleEvidenceRuleClicked(capturedIndex);
            }

            _guessActions = new UnityEngine.Events.UnityAction[_guessButtons.Length];
            for (int i = 0; i < _guessButtons.Length; i++)
            {
                int capturedIndex = i;
                _guessActions[i] = () => HandleGuessClicked(capturedIndex);
            }
        }

        private void AddButtonListeners()
        {
            if (_buttonListenersAttached)
            {
                return;
            }

            for (int i = 0; i < _evidenceRuleButtons.Length; i++)
            {
                _evidenceRuleButtons[i].onClick.AddListener(_evidenceRuleActions[i]);
            }

            for (int i = 0; i < _guessButtons.Length; i++)
            {
                _guessButtons[i].onClick.AddListener(_guessActions[i]);
            }

            _buttonListenersAttached = true;
        }

        private void RemoveButtonListeners()
        {
            if (!_buttonListenersAttached)
            {
                return;
            }

            for (int i = 0; i < _evidenceRuleButtons.Length; i++)
            {
                if (_evidenceRuleButtons[i] != null)
                {
                    _evidenceRuleButtons[i].onClick.RemoveListener(
                        _evidenceRuleActions[i]);
                }
            }

            for (int i = 0; i < _guessButtons.Length; i++)
            {
                if (_guessButtons[i] != null)
                {
                    _guessButtons[i].onClick.RemoveListener(_guessActions[i]);
                }
            }

            _buttonListenersAttached = false;
        }

        private void UnsubscribeFromEvents()
        {
            GameEvents.OnHuntStart -= HandleHuntStart;
            GameEvents.OnHuntEnd -= HandleHuntEnd;
            GameEvents.OnRoundStart -= HandleRoundStart;
            GameEvents.OnRoundEnd -= HandleRoundEnd;
            GameEvents.OnEvidenceFound -= HandleEvidenceFound;

            if (_evidenceManager != null)
            {
                _evidenceManager.onCandidatesChanged?.RemoveListener(
                    HandleCandidatesChanged);
            }
        }

        private bool ValidateReferences()
        {
            if (_evidenceManager == null)
            {
                return DisableWithError(
                    "[JournalUI] _evidenceManager 未注入。组件已禁用。");
            }

            if (_journalRoot == null)
            {
                return DisableWithError(
                    "[JournalUI] _journalRoot 未注入。组件已禁用。");
            }

            if (transform.IsChildOf(_journalRoot.transform))
            {
                return DisableWithError(
                    "[JournalUI] _journalRoot 不能是 JournalUI 所在对象或其父对象，否则关闭界面后无法再检测 Tab。组件已禁用。");
            }

            if (_playerControllerBehaviour == this)
            {
                return DisableWithError(
                    "[JournalUI] _playerControllerBehaviour 不能引用 JournalUI 自身。组件已禁用。");
            }

            int ghostCount = _allGhosts == null ? 0 : _allGhosts.Length;
            if (ghostCount == 0)
            {
                return DisableWithError(
                    "[JournalUI] _allGhosts 不能为空：期望至少 1，实际 0。组件已禁用。");
            }

            if (!ValidateArrayLength(
                    _evidenceLabels,
                    EvidenceCount,
                    nameof(_evidenceLabels)))
            {
                return false;
            }

            if (!ValidateArrayLength(
                    _evidenceRuleButtons,
                    EvidenceCount,
                    nameof(_evidenceRuleButtons)))
            {
                return false;
            }

            if (!ValidateArrayLength(
                    _ghostNameLabels,
                    ghostCount,
                    nameof(_ghostNameLabels)))
            {
                return false;
            }

            if (!ValidateArrayLength(
                    _guessButtons,
                    ghostCount,
                    nameof(_guessButtons)))
            {
                return false;
            }

            int expectedCellCount = ghostCount * EvidenceColumnsPerGhost;
            if (!ValidateArrayLength(
                    _ghostEvidenceCells,
                    expectedCellCount,
                    nameof(_ghostEvidenceCells)))
            {
                return false;
            }

            for (int i = 0; i < ghostCount; i++)
            {
                if (_allGhosts[i] == null)
                {
                    return DisableWithError(
                        $"[JournalUI] _allGhosts[{i}] 是空引用。组件已禁用。");
                }

                if (_ghostNameLabels[i] == null)
                {
                    return DisableWithError(
                        $"[JournalUI] _ghostNameLabels[{i}] 是空引用。组件已禁用。");
                }

                if (_guessButtons[i] == null)
                {
                    return DisableWithError(
                        $"[JournalUI] _guessButtons[{i}] 是空引用。组件已禁用。");
                }
            }

            for (int i = 0; i < EvidenceCount; i++)
            {
                if (_evidenceLabels[i] == null)
                {
                    return DisableWithError(
                        $"[JournalUI] _evidenceLabels[{i}] 是空引用。组件已禁用。");
                }

                if (_evidenceRuleButtons[i] == null)
                {
                    return DisableWithError(
                        $"[JournalUI] _evidenceRuleButtons[{i}] 是空引用。组件已禁用。");
                }
            }

            for (int i = 0; i < _ghostEvidenceCells.Length; i++)
            {
                if (_ghostEvidenceCells[i] == null)
                {
                    return DisableWithError(
                        $"[JournalUI] _ghostEvidenceCells[{i}] 是空引用。组件已禁用。");
                }
            }

            return true;
        }

        private bool ValidateArrayLength<T>(T[] array, int expectedLength, string fieldName)
        {
            int actualLength = array == null ? 0 : array.Length;
            if (actualLength == expectedLength)
            {
                return true;
            }

            return DisableWithError(
                $"[JournalUI] {fieldName} 长度错误：期望 {expectedLength}，实际 {actualLength}。组件已禁用。");
        }

        private bool DisableWithError(string message)
        {
            Debug.LogError(message, this);
            return false;
        }

        private static string GetEvidenceDisplayName(EvidenceType type)
        {
            switch (type)
            {
                case EvidenceType.EMF5:
                    return "EMF-5";
                case EvidenceType.UVFingerprint:
                    return "紫外线指纹";
                case EvidenceType.GhostWriting:
                    return "鬼影书写";
                default:
                    return type.ToString();
            }
        }
    }
}
