using UnityEngine;
using Residuum.Evidence;

namespace Residuum.Ghost
{
    /// <summary>
    /// 鬼种定义 —— 纯数据资产。
    ///
    /// 【设计意图】三种鬼不写三个类。GhostAI 只有一个，行为差异全部由本资产的数值驱动。
    /// 好处：总设计师在 Inspector 里调平衡，不需要改代码，也不需要再麻烦 Codex。
    ///
    /// 创建方式：Project 窗口右键 → Create → Residuum → Ghost Definition
    /// 本项目需要创建三个资产：Spirit / Wraith / Poltergeist，
    /// 存放于 Assets/_Project/ScriptableObjects/Ghosts/
    /// </summary>
    [CreateAssetMenu(fileName = "GhostDef_", menuName = "Residuum/Ghost Definition", order = 0)]
    public class GhostDefinition : ScriptableObject
    {
        [Header("身份")]
        [Tooltip("显示在笔记本与结算界面的中文名")]
        public string ghostName = "怨灵";

        [Tooltip("英文名，用于日志与 UI 副标题")]
        public string displayNameEN = "Spirit";

        [TextArea(2, 5)]
        [Tooltip("笔记本中的鬼种描述，用于给玩家行为上的提示")]
        public string journalDescription;

        [Header("证据组合")]
        [Tooltip("必须恰好 2 项。三种鬼的组合必须互不相同，否则推理表失去唯一性。")]
        public EvidenceType[] evidences = new EvidenceType[2];

        [Header("移动")]
        [Tooltip("非猎杀状态下的巡逻速度 (m/s)")]
        public float walkSpeed = 1.6f;

        [Tooltip("猎杀状态下的追击速度 (m/s)。玩家行走 2.8、冲刺 4.5，所以此值应低于 4.5。")]
        public float huntSpeed = 1.7f;

        [Tooltip("是否在地板上留下脚印。幽影为 false —— 这是玩家可用的辅助判据。")]
        public bool leavesFootprints = true;

        [Header("猎杀")]
        [Tooltip("单次猎杀持续秒数")]
        public float huntDuration = 25f;

        [Tooltip("猎杀结束后的冷却秒数，期间必定不再触发")]
        public float huntCooldown = 25f;

        [Tooltip("对玩家理智扣减速率的倍率。骚灵为 1.5。")]
        public float sanityDrainMultiplier = 1f;

        [Header("特殊行为")]
        [Tooltip("追击中是否周期性加速冲刺。幽影为 true。")]
        public bool canSprintBurst = false;

        [Tooltip("加速冲刺的触发间隔（秒），仅在 canSprintBurst 为 true 时生效")]
        public float sprintBurstInterval = 6f;

        [Tooltip("猎杀开始瞬间是否抛飞周围所有物品。骚灵为 true。")]
        public bool massThrowOnHunt = false;

        [Tooltip("与场景物体互动的频率倍率。骚灵为 2.0。")]
        public float interactFrequency = 1f;

        /// <summary>该鬼种是否拥有指定证据。</summary>
        public bool HasEvidence(EvidenceType type)
        {
            if (evidences == null) return false;
            for (int i = 0; i < evidences.Length; i++)
                if (evidences[i] == type) return true;
            return false;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (evidences == null || evidences.Length != 2)
            {
                Debug.LogWarning(
                    $"[GhostDefinition:{name}] 证据数量必须恰好为 2。" +
                    "当前数量会破坏 3×3 推理表「任意两项组合唯一确定一种鬼」的性质。", this);
                return;
            }

            if (evidences[0] == evidences[1])
                Debug.LogWarning($"[GhostDefinition:{name}] 两项证据重复了。", this);

            if (evidences[0] == EvidenceType.None || evidences[1] == EvidenceType.None)
                Debug.LogWarning($"[GhostDefinition:{name}] 证据不能为 None。", this);

            if (huntSpeed >= 4.5f)
                Debug.LogWarning(
                    $"[GhostDefinition:{name}] huntSpeed 达到或超过玩家冲刺速度 4.5，" +
                    "玩家将无法逃脱，只能靠躲藏。这是有意的吗？", this);
        }
#endif
    }
}
