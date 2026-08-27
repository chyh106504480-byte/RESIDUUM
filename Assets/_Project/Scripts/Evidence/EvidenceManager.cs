using System.Collections.Generic;
using UnityEngine;
using Residuum.Core;

namespace Residuum.Evidence
{
    /// <summary>
    /// 根据已发现及已排除的证据，维护当前仍可能的鬼种列表。
    /// </summary>
    public class EvidenceManager : MonoBehaviour
    {
        private const int RequiredEvidenceCount = 2;

        [Tooltip("本局可供推理的全部鬼种定义；每种鬼必须恰好配置两项互不重复的有效证据")]
        [SerializeField] private Residuum.Ghost.GhostDefinition[] _allGhosts;

        [Tooltip("候选鬼种列表发生变化时触发，供笔记本等界面在 Inspector 中绑定刷新")]
        public UnityEngine.Events.UnityEvent onCandidatesChanged = new UnityEngine.Events.UnityEvent();

        private readonly HashSet<EvidenceType> _foundEvidence = new HashSet<EvidenceType>();
        private readonly HashSet<EvidenceType> _ruledEvidence = new HashSet<EvidenceType>();
        private readonly HashSet<EvidenceType> _ruleConflictWarnings = new HashSet<EvidenceType>();
        private readonly List<Residuum.Ghost.GhostDefinition> _candidates =
            new List<Residuum.Ghost.GhostDefinition>();

        public IReadOnlyList<Residuum.Ghost.GhostDefinition> Candidates => _candidates;
        public int FoundCount => _foundEvidence.Count;
        public bool IsConclusive => _candidates.Count == 1;

        /// <summary>该项证据是否已被实际采集到。</summary>
        public bool IsFound(EvidenceType type)
        {
            return IsValidEvidence(type) && _foundEvidence.Contains(type);
        }

        /// <summary>
        /// 该项证据是否被玩家手动标记为「确认不存在」。
        /// 已发现的证据永远返回 false。
        /// </summary>
        public bool IsRuled(EvidenceType type)
        {
            return IsValidEvidence(type) &&
                   !_foundEvidence.Contains(type) &&
                   _ruledEvidence.Contains(type);
        }

        /// <summary>
        /// 候选鬼种为空——玩家的手动排除与实际发现互相矛盾。
        /// 界面应提示玩家撤销某项排除。
        /// </summary>
        public bool HasContradiction => _candidates.Count == 0;

        private void Awake()
        {
            if (!ValidateGhostDefinitions())
            {
                enabled = false;
                return;
            }

            RebuildCandidates();
        }

        private void OnEnable()
        {
            GameEvents.OnEvidenceFound += HandleEvidenceFound;
            GameEvents.OnRoundStart += HandleRoundStart;
        }

        private void OnDisable()
        {
            GameEvents.OnEvidenceFound -= HandleEvidenceFound;
            GameEvents.OnRoundStart -= HandleRoundStart;
        }

        /// <summary>
        /// 标记一项证据已确认不存在。已经实际发现的证据不能被排除。
        /// </summary>
        public void MarkEvidenceRuled(EvidenceType type)
        {
            if (!IsValidEvidence(type))
            {
                Debug.LogWarning($"[EvidenceManager] 无法排除无效证据类型：{type}。", this);
                return;
            }

            if (_foundEvidence.Contains(type))
            {
                if (_ruleConflictWarnings.Add(type))
                {
                    Debug.LogWarning(
                        $"[EvidenceManager] 证据 {type} 已被发现，不能同时标记为已排除。",
                        this);
                }

                return;
            }

            if (_ruledEvidence.Add(type))
                RebuildCandidates();
        }

        /// <summary>
        /// 撤销一项手动排除标记。
        /// </summary>
        public void ClearRuled(EvidenceType type)
        {
            if (!IsValidEvidence(type))
            {
                Debug.LogWarning($"[EvidenceManager] 无法撤销无效证据类型：{type}。", this);
                return;
            }

            if (_ruledEvidence.Remove(type))
                RebuildCandidates();
        }

        private void HandleEvidenceFound(EvidenceType type)
        {
            if (!IsValidEvidence(type))
            {
                Debug.LogWarning($"[EvidenceManager] 忽略无效的已发现证据类型：{type}。", this);
                return;
            }

            if (!_foundEvidence.Add(type))
                return;

            // 实际发现的证据优先于玩家此前在笔记本中的手动排除。
            _ruledEvidence.Remove(type);
            _ruleConflictWarnings.Remove(type);
            RebuildCandidates();
        }

        private void HandleRoundStart()
        {
            _foundEvidence.Clear();
            _ruledEvidence.Clear();
            _ruleConflictWarnings.Clear();
            RebuildCandidates();
        }

        private void RebuildCandidates()
        {
            if (_allGhosts == null)
                return;

            var nextCandidates = new List<Residuum.Ghost.GhostDefinition>(_allGhosts.Length);

            for (int i = 0; i < _allGhosts.Length; i++)
            {
                Residuum.Ghost.GhostDefinition definition = _allGhosts[i];
                if (MatchesCurrentEvidence(definition))
                    nextCandidates.Add(definition);
            }

            if (CandidateListsMatch(nextCandidates))
                return;

            _candidates.Clear();
            _candidates.AddRange(nextCandidates);
            onCandidatesChanged?.Invoke();
        }

        private bool MatchesCurrentEvidence(Residuum.Ghost.GhostDefinition definition)
        {
            foreach (EvidenceType found in _foundEvidence)
            {
                if (!definition.HasEvidence(found))
                    return false;
            }

            foreach (EvidenceType ruled in _ruledEvidence)
            {
                if (definition.HasEvidence(ruled))
                    return false;
            }

            return true;
        }

        private bool CandidateListsMatch(
            IReadOnlyList<Residuum.Ghost.GhostDefinition> nextCandidates)
        {
            if (_candidates.Count != nextCandidates.Count)
                return false;

            for (int i = 0; i < _candidates.Count; i++)
            {
                if (_candidates[i] != nextCandidates[i])
                    return false;
            }

            return true;
        }

        private bool ValidateGhostDefinitions()
        {
            if (_allGhosts == null || _allGhosts.Length == 0)
            {
                Debug.LogError(
                    "[EvidenceManager] _allGhosts 未配置。至少需要注入一项鬼种定义才能进行推理。",
                    this);
                return false;
            }

            for (int i = 0; i < _allGhosts.Length; i++)
            {
                Residuum.Ghost.GhostDefinition definition = _allGhosts[i];
                if (definition == null)
                {
                    Debug.LogError($"[EvidenceManager] _allGhosts 的索引 {i} 是空引用。", this);
                    return false;
                }

                EvidenceType[] evidences = definition.evidences;
                if (evidences == null || evidences.Length != RequiredEvidenceCount)
                {
                    int actualCount = evidences == null ? 0 : evidences.Length;
                    Debug.LogError(
                        $"[EvidenceManager] 鬼种资产“{definition.name}”必须恰好配置 " +
                        $"{RequiredEvidenceCount} 项证据，当前为 {actualCount} 项。",
                        definition);
                    return false;
                }

                if (!IsValidEvidence(evidences[0]) || !IsValidEvidence(evidences[1]))
                {
                    Debug.LogError(
                        $"[EvidenceManager] 鬼种资产“{definition.name}”包含 None 或未定义的证据类型。",
                        definition);
                    return false;
                }

                if (evidences[0] == evidences[1])
                {
                    Debug.LogError(
                        $"[EvidenceManager] 鬼种资产“{definition.name}”的两项证据重复：" +
                        $"{evidences[0]}。",
                        definition);
                    return false;
                }
            }

            for (int firstIndex = 0; firstIndex < _allGhosts.Length; firstIndex++)
            {
                for (int secondIndex = firstIndex + 1;
                     secondIndex < _allGhosts.Length;
                     secondIndex++)
                {
                    Residuum.Ghost.GhostDefinition first = _allGhosts[firstIndex];
                    Residuum.Ghost.GhostDefinition second = _allGhosts[secondIndex];

                    if (!HaveSameEvidenceCombination(first, second))
                        continue;

                    Debug.LogError(
                        $"[EvidenceManager] 鬼种资产“{first.name}”与“{second.name}”的证据组合相同，" +
                        "无法保证推理表唯一性。",
                        second);
                    return false;
                }
            }

            return true;
        }

        private static bool HaveSameEvidenceCombination(
            Residuum.Ghost.GhostDefinition first,
            Residuum.Ghost.GhostDefinition second)
        {
            return second.HasEvidence(first.evidences[0]) &&
                   second.HasEvidence(first.evidences[1]);
        }

        private static bool IsValidEvidence(EvidenceType type)
        {
            return type == EvidenceType.EMF5 ||
                   type == EvidenceType.UVFingerprint ||
                   type == EvidenceType.GhostWriting;
        }
    }
}
