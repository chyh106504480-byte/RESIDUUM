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

        /// <summary>
        /// 结算明细。**在 OnRoundEnd 之后触发**，供结算界面展示真凶与玩家判定。
        /// 只关心成败等级的模块订阅 OnRoundEnd 即可，不必订阅这个。
        ///
        /// 【顺序为什么是「之后」】判定笔记本在 OnRoundEnd 里会关闭自己并
        /// 重新启用 PlayerController（连带把光标锁回去）。结算面板要禁用
        /// PlayerController 才能让玩家点到按钮，必须排在笔记本后面执行，
        /// 否则会被笔记本的还原动作覆盖，玩家点不到「再来一局」。
        /// </summary>
        public static event Action<RoundSummary> OnRoundSummary;

        /// <summary>
        /// 玩家在出口门上按了交互键，请求撤离。**这不是撤离本身**——
        /// 订阅方（确认弹窗）负责问「确定要走吗」，玩家点确认后才调
        /// GameManager.RequestEvacuate()。
        ///
        /// 【为什么不让出口门直接调 RequestEvacuate】那样玩家碰一下门就结束整局，
        /// 没有反悔余地。撤离是不可逆的，必须有一次确认。
        /// </summary>
        public static event Action OnEvacuatePromptRequested;

        /// <summary>
        /// 全场停电状态变化。true 表示灯全灭了，玩家需要自己去开关处重新打开。
        ///
        /// 【谁来广播 false】LightManager 定期检查它管辖的灯里还有没有亮着的，
        /// 玩家在任意一个开关上把灯打开就算停电结束。这样 LightSwitch 不需要
        /// 认识 LightManager。
        /// </summary>
        public static event Action<bool> OnBlackoutChanged;

        // ─────────────────────────────────────────────
        //  理智
        // ─────────────────────────────────────────────
        /// <summary>参数：当前理智值，范围 0–100</summary>
        public static event Action<float> OnSanityChanged;
        /// <summary>首次跌破猎杀阈值时触发一次</summary>
        public static event Action OnSanityCritical;

        /// <summary>
        /// 外部对理智施加一次性惩罚。参数：扣除的理智点数，0–100 标度上的绝对值，
        /// 传正数表示扣减。
        ///
        /// 【为什么由触发方决定数值】OnGhostEvent 那种「事件在别处、数值配在
        /// PlayerSanity 上」的写法，每多一条规则就要往 PlayerSanity 加一个字段。
        /// 停电扣理智只是第一条，后面还会有更多，所以数值跟着触发方走。
        /// </summary>
        public static event Action<float> OnSanityPenalty;

        /// <summary>
        /// 理智每跌破 25 的整数倍时触发一次，参数为跨越的那个阈值（75 / 50 / 25）。
        /// 音频（T17）与后处理据此逐级加强效果——心跳更响、画面更扭曲。
        ///
        /// 与 OnSanityCritical 的区别：那个只在首次跌破猎杀阈值时发一次，
        /// 用于触发「危险状态」这一个开关；这个是连续分级的强度信号。
        /// 理智回升后再次跌破同一阈值会再次触发。
        /// </summary>
        public static event Action<float> OnSanityThresholdCrossed;

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

        /// <summary>
        /// 视角控制是否被界面接管。参数 true 表示界面正在使用鼠标，玩家不应转头。
        ///
        /// 【和 OnHidingChanged 的区别】躲藏会连移动一起停掉，这个只停视角——
        /// 玩家翻笔记本的时候照样能走动、照样会被鬼摸到，打开界面不是安全屋。
        ///
        /// 【谁管光标】只有 PlayerController。它收到这个事件后按自己的
        /// _lockCursorOnStart 决定解锁与恢复。界面一方**不要**自己写
        /// Cursor.lockState 或 Cursor.visible，两处同时写必然打架。
        /// </summary>
        public static event Action<bool> OnLookSuspendedChanged;

        // ─────────────────────────────────────────────
        //  玩家交互
        // ─────────────────────────────────────────────
        /// <summary>
        /// 准星当前指向的可交互目标发生变化时触发。
        /// 参数：要显示的提示文案（例如 "[E] 开门"）；null 表示当前没有目标，UI 侧应隐藏提示。
        ///
        /// 【为什么传 string 而不是 IInteractable】IInteractable 属于 Residuum.World，
        /// 若在这里传接口，Core 就反向依赖了 World，破坏分层。提示文案是 UI 唯一需要的信息，
        /// 由 PlayerInteractor 从目标的 PromptText 取出后广播即可。
        /// </summary>
        public static event Action<string> OnInteractPromptChanged;

        // ─────────────────────────────────────────────
        //  环境与道具读数
        // ─────────────────────────────────────────────
        /// <summary>
        /// 玩家所处位置的温度读数变化，单位摄氏度。由 RoomManager 按固定间隔广播，HUD 订阅。
        /// 温度是氛围与鬼房提示，不是证据 —— 切片只有 EMF / 紫外线指纹 / 鬼影书写三项。
        /// </summary>
        public static event Action<float> OnPlayerTemperatureChanged;

        /// <summary>
        /// 当前装备槽变化。参数：槽位索引 0–2；物品中文名（空槽为 null）。HUD 订阅。
        /// </summary>
        public static event Action<int, string> OnSlotChanged;

        /// <summary>
        /// 手电筒电量变化，参数为归一化电量 0–1。HUD 订阅。
        /// </summary>
        public static event Action<float> OnBatteryChanged;

        /// <summary>
        /// EMF 读数器的读数等级变化。音频（T17）按等级提高蜂鸣频率，HUD 也可订阅。
        ///
        /// 参数取值：**0 = 无读数（静默）**，1–5 = 读数等级。
        /// 归零同样会广播，订阅方据此结束蜂鸣与显示——不广播 0 的话，
        /// 读数结束后音频和 UI 会停在最后一级下不来。
        ///
        /// 只在等级真正变化时触发，不是每次刷新都发。
        ///
        /// 【与 3×3 推理表的关系】能否到达 5 级由 GhostHasEMF5 决定：
        /// 该鬼种不持有 EMF5 证据时读数封顶 4，这是推理表唯一性的保证，不可绕过。
        /// </summary>
        public static event Action<int> OnEMFReadingChanged;

        // ─────────────────────────────────────────────
        //  本回合鬼种的证据配置
        //  由 GameManager 在回合开始时写入，各证据道具只读。
        //  这样道具模块不需要引用 Ghost 模块即可知道自己该不该出证据。
        // ─────────────────────────────────────────────
        public static bool GhostHasEMF5 { get; set; }
        public static bool GhostHasUVFingerprint { get; set; }
        public static bool GhostHasGhostWriting { get; set; }

        /// <summary>
        /// 本回合鬼房的世界中心与半径。由 GameManager 在回合开始时写入，各模块只读。
        ///
        /// 【为什么放在这里】鬼影书（Residuum.Evidence）要判断自己是否被放在鬼房内，
        /// 但鬼房信息在 Residuum.World.RoomManager —— 铁律不放行跨模块引用具体类。
        /// 与 GhostHasEMF5 等同理，由 GameManager 统一写入这里，下游只读。
        ///
        /// 半径 ≤ 0 表示本回合鬼房尚未设定，读取方应视为「判定不通过」。
        /// </summary>
        public static Vector3 GhostRoomCenter { get; set; }
        public static float GhostRoomRadius { get; set; }

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
        public static void RaiseRoundSummary(RoundSummary s)  => OnRoundSummary?.Invoke(s);
        public static void RaiseEvacuatePromptRequested()     => OnEvacuatePromptRequested?.Invoke();
        public static void RaiseSanityChanged(float v)        => OnSanityChanged?.Invoke(v);
        public static void RaiseSanityCritical()              => OnSanityCritical?.Invoke();
        public static void RaiseSanityPenalty(float points)   => OnSanityPenalty?.Invoke(points);
        public static void RaiseBlackoutChanged(bool b)       => OnBlackoutChanged?.Invoke(b);
        public static void RaiseSanityThresholdCrossed(float threshold) => OnSanityThresholdCrossed?.Invoke(threshold);
        public static void RaiseHuntStart(float duration)     => OnHuntStart?.Invoke(duration);
        public static void RaiseHuntEnd()                     => OnHuntEnd?.Invoke();
        public static void RaisePlayerCaught()                => OnPlayerCaught?.Invoke();
        public static void RaiseEvidenceFound(EvidenceType e) => OnEvidenceFound?.Invoke(e);
        public static void RaiseGhostInteract(Vector3 pos)    => OnGhostInteract?.Invoke(pos);
        public static void RaiseGhostEvent(Vector3 pos)       => OnGhostEvent?.Invoke(pos);
        public static void RaiseHidingChanged(bool hiding)    => OnHidingChanged?.Invoke(hiding);
        public static void RaiseLookSuspendedChanged(bool suspended) => OnLookSuspendedChanged?.Invoke(suspended);
        public static void RaiseInteractPromptChanged(string prompt) => OnInteractPromptChanged?.Invoke(prompt);
        public static void RaisePlayerTemperatureChanged(float celsius) => OnPlayerTemperatureChanged?.Invoke(celsius);
        public static void RaiseSlotChanged(int slot, string itemName)  => OnSlotChanged?.Invoke(slot, itemName);
        public static void RaiseBatteryChanged(float normalized)        => OnBatteryChanged?.Invoke(normalized);
        public static void RaiseEMFReadingChanged(int level)            => OnEMFReadingChanged?.Invoke(level);

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
            OnRoundSummary = null;
            OnEvacuatePromptRequested = null;
            OnSanityChanged = null;
            OnSanityCritical = null;
            OnSanityPenalty = null;
            OnBlackoutChanged = null;
            OnSanityThresholdCrossed = null;
            OnHuntStart = null;
            OnHuntEnd = null;
            OnPlayerCaught = null;
            OnEvidenceFound = null;
            OnGhostInteract = null;
            OnGhostEvent = null;
            OnHidingChanged = null;
            OnLookSuspendedChanged = null;
            OnInteractPromptChanged = null;
            OnPlayerTemperatureChanged = null;
            OnSlotChanged = null;
            OnBatteryChanged = null;
            OnEMFReadingChanged = null;

            GhostHasEMF5 = false;
            GhostHasUVFingerprint = false;
            GhostHasGhostWriting = false;
            GhostRoomCenter = Vector3.zero;
            GhostRoomRadius = 0f;
        }
    }
}
