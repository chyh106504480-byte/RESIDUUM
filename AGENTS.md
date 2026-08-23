# AGENTS.md —— 《残响》RESIDUUM 项目指令

> 本文件是 Codex 在本仓库工作时的常驻指令，每个任务都会自动读取。
> 因此**任务描述里不需要再重复架构约定**，只写「这次要做什么」即可。

---

## 项目是什么

Unity 6 + URP 的第一人称写实恐怖取证游戏，对标《恐鬼症》。
玩家进入闹鬼的房子，用有限设备收集证据、判定鬼种、在理智耗尽前撤离。

**当前阶段**：7 天垂直切片。单人，无联机。1 张地图，3 种鬼，3 种证据，4 件道具。

---

## 技术栈（以本仓库实际安装为准，不可更改）

| 项 | 版本 |
|----|------|
| Unity | **6000.5.8f1** |
| 渲染管线 | **URP 17.0.1**（不是 Built-in，不是 HDRP） |
| 输入 | **Input System 1.12.0**（`UnityEngine.InputSystem`）。禁止使用 `Input.GetKey` 等旧版 API |
| 导航 | **AI Navigation 2.0.14**（`Unity.AI.Navigation` 的 `NavMeshSurface`），不是旧版 Navigation 窗口 |
| 相机 | **Cinemachine 3.1.7**（注意：3.x 的 API 与 2.x 差异很大，`CinemachineVirtualCamera` 已改为 `CinemachineCamera`） |
| UI | uGUI + TextMeshPro |

---

## 架构铁律

### 1. 事件总线解耦（最重要）

**任何模块都不得直接引用另一个模块的具体类。** 所有跨模块通信通过静态事件总线
`Residuum.Core.GameEvents`（见 `Assets/_Project/Scripts/Core/GameEvents.cs`）。

唯一例外：`GameManager` 负责初始化，允许持有全部引用。

需要引用其他模块的对象时，改用以下三种方式之一：
- 通过 `GameEvents` 收发事件
- 通过 `[SerializeField]` 字段由 Inspector 注入
- 通过 `public UnityEvent<T>` 由 Inspector 连线

### 2. 契约文件不可修改

以下文件是全项目共同依赖的契约，**任何任务都不得修改**：

```
Assets/_Project/Scripts/Core/GameEvents.cs
Assets/_Project/Scripts/Core/RoundResult.cs
Assets/_Project/Scripts/Evidence/EvidenceType.cs
Assets/_Project/Scripts/Evidence/IEvidenceSource.cs
Assets/_Project/Scripts/World/IInteractable.cs
Assets/_Project/Scripts/Items/IHoldable.cs
Assets/_Project/Scripts/Ghost/GhostDefinition.cs
```

如果任务看起来需要新增事件或修改接口签名，**不要动手改**，在回复中说明需要什么、为什么，由维护者统一添加。

### 3. 鬼是数据，不是代码

三种鬼（怨灵 / 幽影 / 骚灵）**不写三个类**。只有一个 `GhostAI`，
全部行为差异由 `GhostDefinition` ScriptableObject 的数值驱动。

### 4. 一个任务只产出 1–2 个文件

不要顺手创建「相关的」辅助类。任务要什么就给什么。

### 5. 所有可调数值必须暴露到 Inspector

任何影响手感与平衡的数字（速度、时长、概率、距离、角度、音量）
必须写成 `[SerializeField] private` 字段并附 `[Tooltip("中文说明")]`，
**禁止硬编码常量**。设计师需要在 Inspector 里调，不改代码。

---

## 目录与命名

```
Assets/
├── _Project/            ← 所有自写内容，与第三方彻底隔离
│   ├── Scripts/
│   │   ├── Core/        GameManager, GameEvents, RoundResult
│   │   ├── Player/      PlayerController, PlayerInteractor, PlayerSanity, PlayerHiding
│   │   ├── Ghost/       GhostAI, GhostDefinition, HuntController
│   │   ├── Evidence/    EvidenceManager, EMFReader, UVLight, GhostWritingBook, Fingerprint
│   │   ├── Items/       ItemSlotSystem, Flashlight
│   │   ├── World/       Door, RoomVolume, RoomManager, HidingSpot
│   │   ├── UI/          HUDController, JournalUI, ResultScreen
│   │   └── Audio/       AudioDirector
│   ├── Prefabs/  ScriptableObjects/  Scenes/  Art/  Audio/
└── ThirdParty/          ← 所有下载的免费素材，不修改
```

- 命名空间：`Residuum.<模块名>`，与所在文件夹一致
- 类名 / 文件名：`PascalCase`，两者必须完全一致
- 私有字段：`_camelCase`；序列化私有字段：`[SerializeField] private float _walkSpeed;`
- 公开属性：`PascalCase { get; private set; }`
- 事件：`OnXxx`
- 注释：关键逻辑用**中文**注释

---

## Unity 6 API 陷阱（高频错误，务必检查）

| 旧写法（已废弃） | 本项目正确写法 |
|---|---|
| `FindObjectOfType<T>()` | **`FindAnyObjectByType<T>()`** —— `FindFirstObjectByType` 在 6.x 也已弃用（依赖 instance ID 排序），别用 |
| `FindObjectsOfType<T>()` | **`FindObjectsByType<T>()`** 或 `FindObjectsByType<T>(FindObjectsInactive.Include)` —— 带 `FindObjectsSortMode` 的重载已弃用 |
| `rigidbody.velocity` | `rigidbody.linearVelocity` |
| `Input.GetKey` / `GetAxis` / `mousePosition` | Input System (`UnityEngine.InputSystem`) |
| `CinemachineVirtualCamera` | **`CinemachineCamera`**（Cinemachine 3.x 已改名） |
| `CinemachineBrain` 的 2.x 属性写法 | 参考 Cinemachine 3.1 API |
| `particleSystem.startSpeed` | `particleSystem.main.startSpeed` |
| 每帧调用 `Camera.main` | 在 `Awake` 中缓存 |
| 循环内 `new WaitForSeconds(x)` | 缓存为字段复用 |

---

## 交付前自审（必做）

在给出最终代码之前，逐条检查下列 18 项，并在代码之后**以表格形式输出自审结果**
（每项标注 `通过` / `已修正` / `不适用`）。发现问题先修正代码再输出。
不要跳过，也不要笼统地说「已检查」。

**架构合规**
1. 除 `Residuum.Core` 和 `Residuum.Evidence` 的枚举与接口外，是否引用了其他模块的具体类？
2. 命名空间是否为 `Residuum.<指定模块>`？
3. 是否修改了任何契约文件？（绝对禁止）
4. 文件名是否与主类名完全一致？

**生命周期**
5. 每个 `GameEvents` 订阅，是否在 `OnEnable` 订阅、`OnDisable` 取消？是否严格配对？
6. 所有 `StartCoroutine` 启动的协程，是否在 `OnDisable` 中停止？
7. 是否存在 `OnDestroy` 时未清理的引用或计时器？

**Unity 6 API**
8. 是否使用了 `FindObjectOfType` / `FindObjectsOfType`？
9. 是否使用了 `Rigidbody.velocity`？
10. 是否使用了旧版输入 API？
11. 是否使用了 Cinemachine 2.x 的类名（如 `CinemachineVirtualCamera`）？
12. 是否在 `Update` 中重复调用 `Camera.main`？协程中的 `WaitForSeconds` 是否在循环内反复 `new`？

**设计师可调性**
13. 是否存在任何硬编码数值常量（速度、时长、概率、距离、角度）？
14. 每个 `[SerializeField]` 字段是否都有 `[Tooltip("中文说明")]`？
15. 数值默认值是否与下方「核心设计参数」一致？

**健壮性**
16. 所有 `GetComponent` 与引用字段，使用前是否做了 null 检查？关键依赖缺失时是否用 `Debug.LogError` 明确报出？
17. 是否有除零、数组越界等风险？
18. 代码是否完整可编译（无 `...` 省略、无 `TODO` 占位）？

---

## 核心设计参数（写代码时对照）

**3×3 推理表** —— 每种鬼恰好持有 2 项证据，三种组合互不相同：

| 鬼种 | EMF-5 | 紫外线指纹 | 鬼影书写 |
|------|:---:|:---:|:---:|
| 怨灵 Spirit | ✓ | ✓ | — |
| 幽影 Wraith | ✓ | — | ✓ |
| 骚灵 Poltergeist | — | ✓ | ✓ |

**理智**：起始 100；黑暗中 −0.12 %/s；有灯光房间 −0.06 %/s；手持开启手电 ×0.5；
目击鬼事件 −15；猎杀期间额外 −0.5 %/s；安全区 +1.0 %/s。

**猎杀**：理智 < 50 后每 25 秒判定，概率 = `(50 − sanity) / 50`。
持续 20–30 秒（按鬼种），结束后冷却 25 秒。

**玩家移动**：行走 2.8 m/s，冲刺 4.5 m/s（体力 5 秒），蹲下 1.4 m/s。
鬼的猎杀速度必须低于 4.5，否则玩家无法逃脱。

**操作**：WASD 移动 / Shift 冲刺 / Ctrl 蹲下 / E 交互 / F 手电 / 1·2·3·滚轮 切换道具 / 左键 使用 / Tab 笔记本
