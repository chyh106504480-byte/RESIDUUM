using System;
using UnityEngine;
using Residuum.Evidence;

namespace Residuum.Core
{
    /// <summary>
    /// 全局事件总线 —— 本项目的架构契约。
    ///
    /// 【铁律】所有跨模块通信必须通过这里。任何模块都不允许直接持有另一个模块的引用
    ///         （唯一例外是 GameManager，它负责初始化，允许持有全部引用）。
    ///
    /// 【铁律】本文件由总设计师维护。Codex 生成的任何模块都不得修改本文件。
    ///         需要新增事件时，由总设计师统一添加。
    ///
    /// 【订阅方注意】务必在 OnEnable 订阅、OnDisable 取消订阅，否则场景重载后会留下
    ///               指向已销毁对象的委托，触发 MissingReferenceException。
    /// </summary>
    public static class GameEvents
    {
        // ─────────────────────────────────────────────
        //  回合流程
        // ─────────────────────────────────────────────
        public static event Action OnRoundStart;
        public static event Action<RoundResult> OnRoundEnd;

        // ─────────────────────────────────────────────
        //  理智
        // ─────────────────────────────────────────────
        /// <summary>参数：当前理智值，范围 0–100</summary>
        public static event Action<float> OnSanityChanged;
        /// <summary>首次跌破猎杀阈值时触发一次</summary>
        public static event Action OnSanityCritical;

        // ─────────────────────────────────────────────
        //  猎杀
        // ─────────────────────────────────────────────
        /// <summary>参数：本次猎杀持续秒数</summary>
        public static event Action<float> OnHuntStart;
        public static event Action OnHuntEnd;
        public static event Action OnPlayerCaught;

        // ─────────────────────────────────────────────
        //  证据
        // ─────────────────────────────────────────────
        public static event Action<EvidenceType> OnEvidenceFound;

        // ─────────────────────────────────────────────
        //  鬼的活动
        // ─────────────────────────────────────────────
        /// <summary>鬼与场景物体互动的世界坐标。EMF 读数器订阅此事件。</summary>
        public static event Action<Vector3> OnGhostInteract;
        /// <summary>鬼显形 / 惊吓事件的世界坐标。理智系统与音频订阅此事件。</summary>
        public static event Action<Vector3> OnGhostEvent;

        // ─────────────────────────────────────────────
        //  玩家状态
        // ─────────────────────────────────────────────
        /// <summary>参数：true = 已进入藏匿点</summary>
        public static event Action<bool> OnHidingChanged;

        // ─────────────────────────────────────────────
        //  本回合鬼种的证据配置
        //  由 GameManager 在回合开始时写入，各证据道具只读。
        //  这样道具模块不需要引用 Ghost 模块即可知道自己该不该出证据。
        // ─────────────────────────────────────────────
        public static bool GhostHasEMF5 { get; set; }
        public static bool GhostHasUVFingerprint { get; set; }
        public static bool GhostHasGhostWriting { get; set; }

        /// <summary>供 GameManager 一次性写入本回合配置。</summary>
        public static void SetGhostEvidence(EvidenceType[] evidences)
        {
            GhostHasEMF5 = false;
            GhostHasUVFingerprint = false;
            GhostHasGhostWriting = false;

            if (evidences == null) return;

            foreach (var e in evidences)
            {
                switch (e)
                {
                    case EvidenceType.EMF5:          GhostHasEMF5 = true; break;
                    case EvidenceType.UVFingerprint: GhostHasUVFingerprint = true; break;
                    case EvidenceType.GhostWriting:  GhostHasGhostWriting = true; break;
                }
            }
        }

        // ─────────────────────────────────────────────
        //  触发器 —— 只允许对应的负责模块调用
        // ─────────────────────────────────────────────
        public static void RaiseRoundStart()                  => OnRoundStart?.Invoke();
        public static void RaiseRoundEnd(RoundResult r)       => OnRoundEnd?.Invoke(r);
        public static void RaiseSanityChanged(float v)        => OnSanityChanged?.Invoke(v);
        public static void RaiseSanityCritical()              => OnSanityCritical?.Invoke();
        public static void RaiseHuntStart(float duration)     => OnHuntStart?.Invoke(duration);
        public static void RaiseHuntEnd()                     => OnHuntEnd?.Invoke();
        public static void RaisePlayerCaught()                => OnPlayerCaught?.Invoke();
        public static void RaiseEvidenceFound(EvidenceType e) => OnEvidenceFound?.Invoke(e);
        public static void RaiseGhostInteract(Vector3 pos)    => OnGhostInteract?.Invoke(pos);
        public static void RaiseGhostEvent(Vector3 pos)       => OnGhostEvent?.Invoke(pos);
        public static void RaiseHidingChanged(bool hiding)    => OnHidingChanged?.Invoke(hiding);

        // ─────────────────────────────────────────────
        //  静态状态重置
        //
        //  【为什么需要这个】Unity 默认开启 Enter Play Mode Options（关闭 Domain Reload）
        //  以加快进入播放模式的速度。此时静态字段不会在重新播放时归零，上一次运行遗留的
        //  订阅会指向已销毁的对象，表现为「明明没报错但事件触发后一片空引用」。
        //  这个坑在 Editor 里排查起来极其耗时，所以在这里一次性堵死。
        // ─────────────────────────────────────────────
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticState()
        {
            OnRoundStart = null;
            OnRoundEnd = null;
            OnSanityChanged = null;
            OnSanityCritical = null;
            OnHuntStart = null;
            OnHuntEnd = null;
            OnPlayerCaught = null;
            OnEvidenceFound = null;
            OnGhostInteract = null;
            OnGhostEvent = null;
            OnHidingChanged = null;

            GhostHasEMF5 = false;
            GhostHasUVFingerprint = false;
            GhostHasGhostWriting = false;
        }
    }
}
