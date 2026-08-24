# 《残响》Codex 任务包 — 17 个可直接投喂的模块任务

> **使用方法**：仓库根目录的 `AGENTS.md` 已包含架构铁律、目录规范、Unity API 陷阱与 18 项自审清单，
> **Codex 在本仓库工作时会自动读取**，所以正常情况下只需粘贴下面某个任务的 **📋 Prompt** 部分。
>
> 只有在 Codex 读不到 `AGENTS.md` 时（例如网页版未连接仓库、或启动目录不在仓库根），
> 才需要额外粘贴下方的「通用前缀」和 `02_技术架构.md` 的 §2 + §3。
>
> **一次只做一个任务。** 合并任务的代价是调试时无法定位问题。

---

## 通用前缀（每个 prompt 开头都加）

```
你正在为一个 Unity 6 + URP 的第一人称恐怖游戏《残响》编写模块。
（确切的 Unity 与包版本以仓库根目录 AGENTS.md 为准。）

约束：
1. 使用 New Input System，不要用旧版 Input.GetKey。
2. 命名空间必须是 Residuum.<模块名>。
3. 严禁直接引用其他模块的类，所有跨模块通信通过静态事件总线 Residuum.Core.GameEvents（代码见上文）。
4. 所有影响手感与平衡的数值必须用 [SerializeField] private 暴露到 Inspector，
   并加 [Tooltip("中文说明")]，禁止硬编码常量。
5. 订阅 GameEvents 的类必须在 OnEnable 订阅、OnDisable 取消订阅。
6. 只输出我指定的文件，输出完整可编译的代码，不要省略。
7. 关键逻辑加简短中文注释。
```

---

# 阶段 A：基础（Day 1）

## T01 — 玩家控制器扩展

| | |
|---|---|
| **文件** | `_Project/Scripts/Player/PlayerController.cs` |
| **依赖** | 工程自带的 `Assets/InputSystem_Actions.inputactions`（Player map 已含 Move / Look / Sprint / Crouch / Interact / Attack / Previous / Next） |
| **负责** | Codex 生成 → Henry 调手感 |
| **状态** | ✅ 已交付（分支 `codex/t01-player-controller`，637 行）。**遗留：缺 `Cursor.lockState` / `Cursor.visible` 处理，需补。** |

> ⚠️ **本任务已放弃 Starter Assets 方案。** 原因：它的 FirstPersonController 不含头部摆动、
> 体力、蹲伏压缩、猎杀响应中的任何一项，等于全部重写；且它自带一套 InputActions
> 会与工程自带的那份冲突。下方 prompt 已改为直接基于工程自带输入配置从零实现。

**📋 Prompt**
```
写一个 PlayerController.cs（命名空间 Residuum.Player），不依赖任何第三方控制器包。

输入：使用工程已有的 Assets/InputSystem_Actions.inputactions，不要新建 .inputactions
文件。优先用 [SerializeField] private InputActionAsset 注入。需要的 action 都在
Player map 下：Move(Vector2)、Look(Vector2)、Sprint(Button)、Crouch(Button)。

要求：

- 使用 CharacterController 组件做移动
- 移动：WASD 行走(默认2.8 m/s)、Shift 冲刺(4.5 m/s，有体力条：满值5秒，
  耗尽后需3秒恢复才能再冲刺)、Ctrl 蹲下(1.4 m/s，同时把 CharacterController
  height 从 1.8 平滑插值到 1.0，摄像机跟随)
- 视角：鼠标控制，灵敏度可调，垂直角度限制 ±85 度，支持在 Inspector 中开关反转Y轴
- 头部摆动(head bob)：行走和冲刺时摄像机做正弦上下+左右摆动，振幅和频率分别可调，
  蹲下时振幅减半。静止时平滑回中。
- 脚步事件：每走完一个步幅距离(可调，默认2.2米)触发一次 public event
  Action<float> OnFootstep，参数为当前移动速度，供音频模块订阅
- 订阅 GameEvents.OnHuntStart / OnHuntEnd：猎杀期间冲刺速度提升到 5.2 m/s
  且体力消耗减半（肾上腺素）
- 订阅 GameEvents.OnHidingChanged：为 true 时完全禁用移动与视角输入（只保留极小
  范围的视角自由，±30度，模拟在衣柜里张望）

- 鼠标光标：进入播放时 Cursor.lockState = Locked 且 Cursor.visible = false；
  失焦(OnApplicationFocus)与禁用时正确恢复。这一条不能漏——否则视角转到窗口
  边缘就会卡住。
- 摄像机可以用 Cinemachine 3.1.7 的 CinemachineCamera（注意不是 2.x 的
  CinemachineVirtualCamera），或直接控制子物体 Transform，选更简单可靠的一种。

另外输出一份说明：需要在 Inspector 中挂哪些组件、层级结构怎么搭、相机怎么配、
InputActionAsset 怎么连。
```

**✅ 验收**：在 `_Sandbox.unity` 里能走、跑、蹲，头部摆动不晕，体力条逻辑正确，鼠标锁定正常。

---

## T02 — 交互系统与门

| | |
|---|---|
| **文件** | `_Project/Scripts/World/PlayerInteractor.cs`、`_Project/Scripts/World/Door.cs` |
| **依赖** | `IInteractable` 接口已存在 |

**📋 Prompt**
```
写两个文件：

【1】Residuum.World.PlayerInteractor（挂在玩家摄像机上）
- 每帧从摄像机中心发射射线，距离可调(默认2.5米)，LayerMask 可配置
- 命中实现了 Residuum.World.IInteractable 的物体且 CanInteract 为 true 时，
  调用 GameEvents.RaiseInteractPromptChanged(目标的 PromptText)，
  没命中时调用 GameEvents.RaiseInteractPromptChanged(null)
- 按 E 键调用 Interact(gameObject)
- 射线检测做频率限制：每 0.1 秒检测一次而非每帧，减少开销
- 猎杀期间(订阅 GameEvents.OnHuntStart/OnHuntEnd)交互距离缩短到 1.5 米

【2】Residuum.World.Door，实现 IInteractable
- 门有三态：关闭 / 开启 / 正在转动，用协程平滑旋转 Y 轴（角度和时长可调）
- PromptText 根据状态返回 "[E] 开门" 或 "[E] 关门"
- 提供 public void ForceOpen(float speedMultiplier) 供鬼AI调用（鬼开门更快更猛）
- 提供 public bool IsOpen { get; }
- 门上挂 NavMeshObstacle 组件，关闭时 carving=true，开启时 carving=false，
  这样鬼的寻路会正确绕开关闭的门
- 开关门时触发 public event Action<bool> OnDoorStateChanged 供音频订阅
```

**✅ 验收**：走到门前有提示，按 E 平滑开关，NavMesh 上的 AI 能在门开时通过。

---

## T03 — 道具槽系统 + 手电筒

| | |
|---|---|
| **文件** | `_Project/Scripts/Items/ItemSlotSystem.cs`、`_Project/Scripts/Items/Flashlight.cs` |
| **依赖** | `IHoldable` 接口 |

**📋 Prompt**
```
写两个文件：

【1】Residuum.Items.ItemSlotSystem（挂玩家身上）
- 3 个装备槽，用数字键 1/2/3 切换，鼠标滚轮也可循环切换
- 持有一个 IHoldable 数组，切换时调用上一件的 OnUnequip() 和新一件的 OnEquip()
- 手持模型挂在一个可配置的 handAnchor Transform 下，未装备的隐藏
- 鼠标左键调用当前物品的 OnPrimaryUse()
- 触发 public event Action<int, string> OnSlotChanged（槽位索引，物品名）供 HUD 订阅
- 提供 public IHoldable Current { get; }

【2】Residuum.Items.Flashlight，实现 IHoldable
- 控制一个子物体上的 URP Spot Light
- OnPrimaryUse() 切换开关（也响应 F 键，即使未装备也能开关，因为它是主光源）
- 光照参数全部 [SerializeField] 暴露：强度、色温(默认4200K)、射程(默认12米)、
  内外锥角(默认30/45度)
- 电量系统：满电 300 秒(可调)，开启时消耗，电量低于 20% 时随机闪烁频率增加，
  耗尽后关闭并需要 15 秒"冷却"才能再开（模拟拍打手电让它复活）
- 订阅 GameEvents.OnHuntStart：立即进入强制故障状态，用协程做不规则闪烁
  （随机间隔 0.05~0.4 秒），期间无法手动控制
- 订阅 GameEvents.OnHuntEnd：恢复正常
- **互斥**：提供 public void ForceOff(bool locked)，当玩家装备紫外线灯时调用
  ForceOff(true) 强制熄灭并锁定（期间 F 键无效），卸下 UV 时 ForceOff(false) 解锁。
  理由：紫外荧光只在黑暗中可见，这是物理必然。互斥由 ItemSlotSystem 通过
  UnityEvent 连线触发，两个道具之间不直接引用。
- 订阅 GameEvents.OnGhostEvent：如果鬼事件位置在 8 米内，触发 1.5 秒短暂闪烁
- 触发 public event Action<float> OnBatteryChanged（0-1）供 HUD 订阅
```

**✅ 验收**：三个槽能切换，手电筒开关正常，手动触发 `GameEvents.RaiseHuntStart(20f)` 时手电筒疯狂闪烁。

---

# 阶段 B：世界与证据（Day 2–3）

## T04 — 房间系统与鬼房

| | |
|---|---|
| **文件** | `_Project/Scripts/World/RoomVolume.cs`、`_Project/Scripts/World/RoomManager.cs` |

**📋 Prompt**
```
写两个文件：

【1】Residuum.World.RoomVolume（挂在每个房间的 BoxCollider trigger 上）
- 字段：房间名(中文)、房间ID、bool 是否可作为鬼房候选
- 检测玩家进出，触发 public event Action<RoomVolume, bool> OnPlayerPresenceChanged
- 提供 public bool HasPlayer { get; }
- 提供 public Vector3 GetRandomPointInside()，返回房间内地面上的随机点
  （用 NavMesh.SamplePosition 确保点在导航网格上）
- 在 Scene 视图用 OnDrawGizmos 画出房间边界和名字，方便我搭建关卡

【2】Residuum.World.RoomManager（场景单例）
- 启动时自动收集场景中所有 RoomVolume
- public void SelectGhostRoom()：从候选房间中随机选一个作为鬼房
- public RoomVolume GhostRoom { get; }
- public RoomVolume CurrentPlayerRoom { get; }
- 温度模拟：提供 public float GetTemperatureAt(Vector3 pos)，
  基础室温可调(默认12摄氏度)，鬼房内温度线性降到可调的低温(默认-2度)，
  按到鬼房中心的距离插值，影响半径可调
- 触发 public event Action<float> OnPlayerTemperatureChanged，每 0.5 秒更新一次
```

**✅ 验收**：Scene 视图能看到房间框，运行时随机选中一个鬼房（用 Debug.Log 输出），走进去温度读数下降。

---

## T05 — EMF 读数器

| | |
|---|---|
| **文件** | `_Project/Scripts/Evidence/EMFReader.cs` |

**📋 Prompt**
```
写 Residuum.Evidence.EMFReader，实现 Residuum.Items.IHoldable。

- 读数 1~5 级，显示在手持模型上的一个 TextMeshPro 或一排指示灯上（提供两种方式的
  接口，我在 Inspector 里选）
- 订阅 GameEvents.OnGhostInteract(Vector3 位置)：
  收到事件后，若玩家在该位置的检测半径内(可调，默认6米)，则在可调的持续时间内
  (默认8秒)显示读数。读数级别按距离衰减：距离越近级别越高，2米内为最高级
- 关键：最高级是 4 还是 5，取决于当前鬼种是否拥有 EMF5 证据。
  为了不直接引用 Ghost 模块，读一个 [SerializeField] private bool 或
  通过 GameEvents 新增的一个只读静态属性。请采用后者：
  在 GameEvents 中我会加一个 public static bool GhostHasEMF5 { get; set; }，
  由 GameManager 在回合开始时设置。你直接读它。
- 当读数达到 5 时，调用 GameEvents.RaiseEvidenceFound(EvidenceType.EMF5)，
  且同一回合只触发一次
- 音频：每次读数变化触发 public event Action<int> OnReadingChanged，
  蜂鸣频率随级别升高（音频模块负责播放，你只发事件）
- 未装备时不工作（OnUnequip 时停止所有协程）
```

**✅ 验收**：手动调用 `GameEvents.RaiseGhostInteract(玩家附近的点)`，EMF 读数跳动，5 格时输出证据发现日志。

---

## T06 — 紫外线灯与指纹

| | |
|---|---|
| **文件** | `_Project/Scripts/Evidence/UVLight.cs`、`_Project/Scripts/Evidence/Fingerprint.cs` |

**📋 Prompt**
```
写两个文件：

【1】Residuum.Evidence.Fingerprint（挂在门把手、开关、窗台等可留指纹的物体上）
- public void Reveal()：让指纹可见（切换一个子物体的显示，或设置材质 emission）
- 生命周期：被鬼创建后存在可调时长(默认60秒)，然后自动消失
- public bool IsActive { get; }
- 提供 public static Fingerprint SpawnAt(Transform target)，供鬼AI调用
- 指纹本身平时完全不可见（材质 alpha 为 0），只有被 UV 光照到才显现

【2】Residuum.Evidence.UVLight，实现 Residuum.Items.IHoldable
- 一个紫色 URP Spot Light（颜色、强度、锥角可调），OnPrimaryUse 切换开关
- 开启时，每 0.2 秒用 Physics.OverlapSphere（半径可调，默认4米）
  + 视锥角度判断，找出射程内且在光锥内的所有 Fingerprint
- 对每个符合条件的、IsActive 为 true 的指纹调用 Reveal()
- 首次照到有效指纹时，调用 GameEvents.RaiseEvidenceFound(EvidenceType.UVFingerprint)，
  同一回合只触发一次
- 电量系统同手电筒，但满电时长更短(默认180秒)
- **互斥**：OnEquip() 时触发 public UnityEvent<bool> onRequestFlashlightLock(true)，
  OnUnequip() 时触发 (false)。我会在 Inspector 里把它连到 Flashlight.ForceOff。
  设计意图：装备 UV 期间房间全黑，玩家几乎失明，必须停在原地承担风险。
```

**✅ 验收**：在场景里放几个 Fingerprint，用 UV 灯照到时显现并记录证据。

---

## T07 — 鬼影书

| | |
|---|---|
| **文件** | `_Project/Scripts/Evidence/GhostWritingBook.cs` |

**📋 Prompt**
```
写 Residuum.Evidence.GhostWritingBook，同时实现 Residuum.Items.IHoldable
和 Residuum.World.IInteractable。

- 两种状态：被手持 / 已放置在世界中
- OnPrimaryUse()：如果手持，则放置到玩家前方地面（用射线检测地面位置和法线，
  贴地摆放）；放置后从道具槽移除
- 作为 IInteractable：已放置状态下玩家可按 E 捡回
- 放置后开始判定协程：每隔可调间隔(默认30秒)判定一次
  判定条件（三个都满足才成功）：
    a) GameEvents.GhostHasGhostWriting 为 true（同 EMF 的处理方式，
       我会在 GameEvents 中加这个静态属性）
    b) 书本位置在鬼房内（通过一个 [SerializeField] LayerMask 或
       Physics.OverlapSphere 检测带 "GhostRoom" Tag 的 trigger）
    c) 随机数通过成功率检定（可调，默认 30%）
- 成功时：切换书本模型为"已书写"版本（换材质或激活子物体），
  播放一个可配置的粒子/音效事件，调用
  GameEvents.RaiseEvidenceFound(EvidenceType.GhostWriting)，然后停止判定
- 提供 public bool HasWriting { get; }
```

**✅ 验收**：放在鬼房里等待，30 秒后有概率出现书写，证据被记录。

---

## T08 — 证据管理器

| | |
|---|---|
| **文件** | `_Project/Scripts/Evidence/EvidenceManager.cs` |

**📋 Prompt**
```
写 Residuum.Evidence.EvidenceManager（场景单例）。

- 订阅 GameEvents.OnEvidenceFound，把证据存入一个 HashSet<EvidenceType>，去重
- public IReadOnlyCollection<EvidenceType> Found { get; }
- public bool Has(EvidenceType t)
- public int FoundCount { get; }
- 每收集到一项新证据，触发
  public event Action<EvidenceType, int> OnEvidenceRegistered（证据类型，当前总数）
- 提供推理逻辑：
    public GhostGuessResult Deduce(IReadOnlyList<GhostDefinition> allGhosts)
  返回一个结构体，包含：
    - List<GhostDefinition> possibleGhosts —— 所有与已找到证据兼容的鬼
      （兼容 = 已找到的每一项证据都在该鬼的证据列表中）
    - bool isUnique —— possibleGhosts.Count == 1
  注意：这里需要引用 Residuum.Ghost.GhostDefinition，这是唯一允许的跨模块引用，
  因为 GhostDefinition 是纯数据 ScriptableObject 而非行为类。
- 订阅 GameEvents.OnRoundStart 时清空所有已收集证据
```

**✅ 验收**：手动触发两项证据，`Deduce` 返回唯一的鬼种。

---

# 阶段 C：鬼与威胁（Day 4）

## T09 — 鬼 AI 主体

| | |
|---|---|
| **文件** | `_Project/Scripts/Ghost/GhostAI.cs` |
| **依赖** | `GhostDefinition`(SO)、NavMeshSurface 已烘焙 |
| **⚠️** | **本项目最难的一个任务，预留半天，可能需要 2–3 轮迭代** |

**📋 Prompt**
```
写 Residuum.Ghost.GhostAI，使用 NavMeshAgent。

有限状态机，四个状态：Idle / Roam / Interact / Hunt。用 enum + switch 实现，
不要用第三方 FSM 框架。

【Idle】停留在鬼房内的随机点，持续 5~15 秒（可调范围），然后 30% 概率转 Roam，
       70% 概率转 Interact

【Roam】离开鬼房，走到场景中随机一个 RoomVolume 的随机点。为了不直接引用
       World 模块，请通过一个 [SerializeField] private Transform[] roamPoints
       数组接收巡逻点，我在场景里手动摆。到达后停留 3~8 秒，然后返回鬼房转 Idle。
       移动速度用 definition.walkSpeed。
       如果 definition.leavesFootprints 为 true，每走 1.5 米在地面生成一个
       可配置的脚印 Decal 预制体（存在 20 秒后销毁）

【Interact】在当前位置附近寻找带 Residuum.World.IInteractable 的物体
       （用 OverlapSphere + 半径可调），随机选一个：
       - 调用 GameEvents.RaiseGhostInteract(该物体位置)
       - 有可调概率(默认40%)在该物体上生成指纹：请通过一个
         public UnityEvent<Transform> onFingerprintRequest 暴露，
         我在 Inspector 里连到 Fingerprint.SpawnAt，保持模块解耦
       - 如果 definition.massThrowOnHunt 为 true（骚灵），互动频率乘以
         definition.interactFrequency
       完成后转回 Idle

【Hunt】由外部 HuntController 调用 public void EnterHunt(float duration) 进入。
       - 速度切换为 definition.huntSpeed
       - 持续追踪最近的玩家 Transform（通过 [SerializeField] Transform player 指定）
       - 视线检测：每 0.25 秒做一次 Linecast，如果被墙遮挡则记为"丢失视线"
       - 丢失视线超过 3 秒（可调）后，先走向最后已知位置，到达后在附近随机游走
       - 如果 GameEvents 中玩家处于躲藏状态（订阅 OnHidingChanged 记录一个 bool），
         则不能直接锁定玩家，改为在最后已知位置附近游走；但每 5 秒有 15%(可调)
         概率强行走向玩家实际位置（模拟"检查藏匿点"）
       - 如果 definition.canSprintBurst（幽影），每隔 definition.sprintBurstInterval
         秒，把 agent.speed 临时提升 80% 持续 1.5 秒
       - 与玩家距离小于可调值(默认1.2米)且玩家未躲藏时，调用
         GameEvents.RaisePlayerCaught()
       - duration 到期后调用 GameEvents.RaiseHuntEnd() 并回到 Idle

其他：
- public GhostDefinition Definition { get; set; }，由 GameManager 在回合开始时注入
- 平时 Renderer 关闭（不可见），只在 Hunt 状态和 GhostEvent 时开启
- 提供 public void TriggerGhostEvent()：短暂显形 2 秒并调用
  GameEvents.RaiseGhostEvent(transform.position)，然后重新隐形
- 在 OnDrawGizmosSelected 中画出当前状态、目标点、视线射线，方便我调试
```

**✅ 验收**：鬼在灰盒场景里巡逻、互动、能被手动触发猎杀并追到玩家。**这一步不通过不要往下走。**

---

## T10 — 猎杀调度器

| | |
|---|---|
| **文件** | `_Project/Scripts/Ghost/HuntController.cs` |

**📋 Prompt**
```
写 Residuum.Ghost.HuntController。

- 订阅 GameEvents.OnSanityChanged，缓存当前理智值
- 每隔可调间隔(默认25秒)执行一次猎杀判定：
    如果理智 >= 猎杀阈值(可调，默认50)，不猎杀
    否则触发概率 = (阈值 - 当前理智) / 阈值，用 Random.value 判定
- 触发时：
    调用 GameEvents.RaiseHuntStart(definition.huntDuration)
    调用 ghostAI.EnterHunt(duration)
    启动冷却计时，冷却期间(definition.huntCooldown 秒)不再判定
- 提供 public void ForceHunt()，方便我在演示时手动触发（Debug 用）
- 提供 public bool IsHunting { get; }
- 在 Inspector 暴露一个只读的调试显示：距离下次判定还有几秒、当前触发概率
  （用 [SerializeField] private 字段在 Update 中更新即可）
```

**✅ 验收**：理智降到 30% 后，25 秒内有较高概率触发猎杀。

---

## T11 — 躲藏系统

| | |
|---|---|
| **文件** | `_Project/Scripts/World/HidingSpot.cs`、`_Project/Scripts/Player/PlayerHiding.cs` |

> ⚠️ **与 T01 的时序契约（必须遵守）**：`PlayerController.UpdateLook` 每帧无条件写
> `transform.rotation`。因此 `PlayerHiding.Enter()` **必须先完成玩家的位置插值与朝向设置，
> 最后再调用 `GameEvents.RaiseHidingChanged(true)`** —— 顺序反了的话玩家朝向会被
> PlayerController 立刻覆盖，藏匿视角的中心点会取到错误的角度。

**📋 Prompt**
```
写两个文件：

【1】Residuum.World.HidingSpot，实现 IInteractable
- 类型枚举：Closet(衣柜) / UnderBed(床底)
- 有一个 hidePoint Transform 表示玩家躲进去后的摄像机位置
- PromptText 返回 "[E] 躲藏" 或躲藏中的 "[E] 出来"
- Interact 时调用玩家身上的 PlayerHiding —— 但为了解耦，改为：
  触发 public event Action<HidingSpot> OnHideRequested，由 PlayerHiding 订阅场景中
  所有 HidingSpot 的这个事件（或者更简单：Interact(GameObject interactor) 的
  参数就是玩家，直接 interactor.GetComponent<PlayerHiding>()，这个可以接受）
- 衣柜类型有门：躲进去时门关上（协程旋转），出来时打开

【2】Residuum.Player.PlayerHiding（挂玩家身上）
- public void Enter(HidingSpot spot)：
    平滑把玩家移动到 spot 的 hidePoint（0.4 秒插值，可调）
    禁用 CharacterController
    调用 GameEvents.RaiseHidingChanged(true)
- public void Exit()：反向操作，调用 GameEvents.RaiseHidingChanged(false)
- 躲藏期间按 E 或 Esc 退出
- 躲藏期间视角限制在 ±30 度（PlayerController 已处理，你只需发事件）
- 后处理：躲藏时触发 public event Action<bool> OnHidingVisualChanged，
  供后处理模块加 vignette
- public bool IsHiding { get; }
```

**✅ 验收**：猎杀时躲进衣柜，鬼在外面徘徊找不到你。

---

# 阶段 D：状态与 UI（Day 5）

## T12 — 理智系统

| | |
|---|---|
| **文件** | `_Project/Scripts/Player/PlayerSanity.cs` |

**📋 Prompt**
```
写 Residuum.Player.PlayerSanity（挂玩家身上）。

数值全部 [SerializeField] 暴露，默认值如下：
- 起始理智 100
- 黑暗中衰减 0.12 /秒
- 有灯光房间衰减 0.06 /秒
- 手持开启的手电筒时上述速率 ×0.5
- 目击鬼事件 -15（一次性）
- 猎杀期间额外 -0.5 /秒
- 在安全区 +1.0 /秒，上限 100

实现：
- "是否在黑暗中"：通过一个 [SerializeField] private bool useLightProbe 决定检测方式。
  简单方式：接收一个 public void SetInLitRoom(bool) 由外部调用。
  推荐方式：每 0.5 秒用 Physics.OverlapSphere 找附近半径内开启的 Light 组件，
  找到则算有光。请实现推荐方式，并保留简单方式作为覆盖开关。
- 手电筒状态：不要直接引用 Flashlight 类。暴露
  public void SetFlashlightOn(bool)，我在 Inspector 里用 UnityEvent 连接。
- 订阅 GameEvents.OnGhostEvent：位置在可调距离内(默认15米)且有视线时才扣 15 点
- 订阅 GameEvents.OnHuntStart / OnHuntEnd 控制额外衰减
- 安全区：一个 [SerializeField] Collider safeZone，玩家在其中时回复
- 每次变化调用 GameEvents.RaiseSanityChanged(current)
- 首次跌破 50 时调用 GameEvents.RaiseSanityCritical()（只调用一次）
- 理智每跌破 25 的整数倍时，触发
  public event Action<float> OnSanityThresholdCrossed，供音频与后处理模块加强效果
```

**✅ 验收**：在黑暗中站着，理智稳定下降；打开手电减半；跌破 50 有日志。

---

## T13 — 判定笔记本 UI

| | |
|---|---|
| **文件** | `_Project/Scripts/UI/JournalUI.cs` |
| **建议** | **这个任务交给队友做**，UI 最容易并行 |

**📋 Prompt**
```
写 Residuum.UI.JournalUI，使用 Unity UI (uGUI) + TextMeshPro。

Tab 键开关笔记本。打开时暂停鼠标锁定（Cursor.lockState = None）并显示光标，
关闭时恢复。

界面内容：
【左侧】证据清单，三行，每行一个证据名 + 一个状态图标：
   未找到(灰) / 已找到(绿色勾)
   订阅 EvidenceManager.OnEvidenceRegistered 更新
   注意：这里允许直接引用 EvidenceManager，因为 UI 是表现层
【右侧】鬼种推理表，一个 3×4 的表格：
   行 = 三种鬼，列 = 鬼名 + 三项证据
   每格显示该鬼是否拥有该项证据(✓ / —)
   数据来自 GhostDefinition[] 数组，在 Inspector 里赋值
   根据当前已找到的证据自动把不可能的鬼种整行变暗（灰度 + alpha 0.35）
【底部】三个"判定为..."按钮，对应三种鬼。点击后：
   触发 public event Action<GhostDefinition> OnGuessSubmitted
   按钮点击后进入已锁定状态，显示"已提交：XXX"，不可更改

所有文本用中文。请同时输出一段说明，告诉我需要在 Canvas 下建哪些
GameObject 层级结构，以便我手动搭 UI。
```

**✅ 验收**：Tab 打开，证据实时更新，不可能的鬼种自动变暗，能提交判定。

---

## T14 — HUD

| | |
|---|---|
| **文件** | `_Project/Scripts/UI/HUDController.cs` |
| **建议** | 队友做 |

**📋 Prompt**
```
写 Residuum.UI.HUDController，uGUI + TextMeshPro。

显示元素（每个都可在 Inspector 里单独关闭，方便截图演示）：
1. 准星：一个小点，命中可交互物时变成圆圈并在下方显示提示文字
   （订阅 GameEvents.OnInteractPromptChanged）
2. 理智条：订阅 GameEvents.OnSanityChanged。
   颜色随数值渐变：100%=白 → 50%=琥珀 → 0%=深红。低于 25% 时整条以
   1.2 秒周期缓慢脉动
3. 体力条：只在冲刺时和冲刺后 2 秒内淡入显示，其余时间淡出
4. 当前道具：左下角显示槽位 1/2/3 和当前手持物品名，订阅
   ItemSlotSystem.OnSlotChanged
5. 手电筒电量：小图标 + 百分比，订阅 Flashlight.OnBatteryChanged
6. 猎杀警告：订阅 GameEvents.OnHuntStart，屏幕边缘出现红色 vignette 图片
   并快速脉动，OnHuntEnd 时淡出
7. 提示条：public void ShowToast(string msg, float duration)，
   屏幕中上方淡入淡出显示（用于"发现证据：EMF 5级"这类提示）
   自动订阅 GameEvents.OnEvidenceFound 显示对应提示

所有淡入淡出用 CanvasGroup + 协程，不要用第三方 Tween 库。
同样输出 Canvas 层级结构说明。
```

**✅ 验收**：所有元素正确响应事件，猎杀时红边闪烁。

---

## T15 — 回合管理器

| | |
|---|---|
| **文件** | `_Project/Scripts/Core/GameManager.cs` |
| **⚠️** | 这是唯一允许持有全部系统引用的类 |

**📋 Prompt**
```
写 Residuum.Core.GameManager（场景单例，DontDestroyOnLoad 不需要）。

回合流程：
1. StartRound()：
   - 从 [SerializeField] GhostDefinition[] allGhosts 中随机选一种鬼
   - 把选中的 definition 注入 GhostAI
   - 根据 definition.evidences 设置 GameEvents 的三个静态属性：
     GhostHasEMF5 / GhostHasUV / GhostHasGhostWriting
   - 调用 RoomManager.SelectGhostRoom()
   - 把鬼传送到鬼房内的随机点
   - 调用 GameEvents.RaiseRoundStart()
   - 记录回合开始时间
2. 订阅 JournalUI.OnGuessSubmitted 记录玩家的判定（但不立即结算）
3. 玩家走到出口 trigger 时调用 EndRound()：
   - 比对判定与真实鬼种
   - 按以下规则计算 RoundResult：
       Perfect: 判定正确 且 EvidenceManager.FoundCount >= 2 且 理智 > 30
       Success: 判定正确
       Survived: 判定错误
   - 调用 GameEvents.RaiseRoundEnd(result)
4. 订阅 GameEvents.OnPlayerCaught：
   - 立即 GameEvents.RaiseRoundEnd(RoundResult.Died)
   - 触发一个可配置的死亡演出（摄像机缓慢倒下 + 画面渐黑，用协程实现）

另外提供调试功能（用 #if UNITY_EDITOR 包裹）：
- 在 Inspector 中可以强制指定本局的鬼种，而不是随机（演示时非常有用）
- 快捷键 F9 强制触发猎杀，F10 直接结束回合
- 快捷键 F11 一键收集所有证据（演示跳过流程用）

请同时输出需要在 GameEvents.cs 中新增的三个静态属性的代码。
```

**✅ 验收**：完整跑通一局：开始 → 找证据 → 判定 → 撤离 → 结算。

---

## T16 — 结算界面

| | |
|---|---|
| **文件** | `_Project/Scripts/UI/ResultScreen.cs` |
| **建议** | 队友做 |

**📋 Prompt**
```
写 Residuum.UI.ResultScreen，uGUI + TextMeshPro。

订阅 GameEvents.OnRoundEnd(RoundResult)。

显示：
- 大字评级 S / A / C / F，对应 Perfect / Success / Survived / Died，
  带一个从 0.6 倍缩放弹到 1.0 的入场动画（协程 + AnimationCurve，曲线可在
  Inspector 里调）
- 真实鬼种名 + 玩家判定的鬼种名，判定正确时后者显示绿色，错误时红色
- 已收集证据列表 / 共 2 项应有证据
- 剩余理智百分比
- 用时（分:秒）
- "再来一局"按钮（重载当前场景）和"退出"按钮
- 死亡结局(F)时整个面板用红色主题，其余用冷灰蓝主题，两套颜色在 Inspector 里配

面板出现前有 1.5 秒黑屏过渡（可调）。
```

---

## T17 — 音频总监

| | |
|---|---|
| **文件** | `_Project/Scripts/Audio/AudioDirector.cs` |
| **建议** | 队友做；**性价比极高，优先级高于美术** |

**📋 Prompt**
```
写 Residuum.Audio.AudioDirector（场景单例）。

使用 Unity AudioMixer（我会创建一个名为 MasterMixer 的 mixer，含
Ambience / SFX / Hunt 三个 group，并已暴露参数 "AmbienceVolume"、
"LowpassCutoff"、"ReverbLevel"）。

功能：
1. 环境层：循环播放一个可配置的 drone 音效。
   订阅 GameEvents.OnSanityChanged：理智越低，
   - AmbienceVolume 越大（映射曲线用 AnimationCurve 暴露）
   - LowpassCutoff 越低（22000Hz → 800Hz），让声音变闷
   - ReverbLevel 越高
   全部用 AnimationCurve 映射，方便我调
2. 心跳：理智低于阈值(可调，默认40)时开始播放心跳循环，
   播放速率(pitch)随理智降低而升高(1.0 → 1.6)。猎杀期间强制最快
3. 猎杀层：订阅 GameEvents.OnHuntStart，
   - 淡入一段紧张的低频音效
   - 播放一次冲击音(stinger)
   - 环境层音量压低（ducking）
   OnHuntEnd 时反向恢复，淡出时长可调
4. 鬼事件音：订阅 GameEvents.OnGhostEvent(Vector3)，
   在该位置用 AudioSource.PlayClipAtPoint 从一个可配置的音效数组中随机播放一个
5. 脚步：提供 public void PlayFootstep(float speed, Vector3 pos)，
   从两个数组（行走 / 奔跑）中随机选，音量与 speed 相关。
   我会用 UnityEvent 把 PlayerController.OnFootstep 连过来
6. 提供 public void PlayOneShot(AudioClip clip, Vector3 pos, float volume = 1f)
   作为通用接口

所有 AudioClip 引用用数组 + [SerializeField] 暴露，我会自己填素材。
```

---

# 附：给 Codex 的调试 Prompt 模板

代码编译不过或行为不对时，**不要自己硬改**，用这个模板贴回去：

```
以下是你刚才生成的 [文件名]。在 Unity 6 URP 中出现了问题：

【编译错误 / 运行时报错原文】
（完整粘贴 Console 里的报错，包括行号）

【我期望的行为】
...

【实际发生的行为】
...

请只修改必要的部分，输出完整的修正后文件，并用一两句话说明你改了什么、为什么。
```

---

# 任务依赖顺序图

```
T01 玩家控制器 ──┬─→ T02 交互系统 ──→ T03 道具槽+手电
                 │                        │
                 │                        ├─→ T05 EMF
                 │        T04 房间系统 ────┼─→ T06 UV+指纹
                 │              │          └─→ T07 鬼影书
                 │              │                  │
                 │              │          T08 证据管理器
                 │              ↓                  │
                 │        T09 鬼AI ──→ T10 猎杀调度  │
                 │              │           │       │
                 └─→ T11 躲藏 ←─┘           │       │
                        │                   │       │
                   T12 理智系统 ←────────────┘       │
                        │                           │
                        └──→ T15 回合管理器 ←────────┘
                                   │
                        T13 笔记本 / T14 HUD / T16 结算 / T17 音频
                        （可全程并行，交给队友）
```

**关键路径**：T01 → T02 → T04 → T09 → T10 → T15。这条链上的任何延误都会推迟整个项目，优先保这条。

---

*配套文档：`01_GDD_残响.md` / `02_技术架构.md` / `04_七天排期.md`*
