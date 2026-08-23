# CLAUDE.md —— Claude Code 在《残响》RESIDUUM 里的位置

用中文交流。这份文件是常驻指令，每个会话都要按它办。
`AGENTS.md` 是给 Codex 的，你也要读，但那是**它的**约束；这份是**你的**。

---

## 1. 你是谁

四个角色，一个人扮：

| 角色 | 具体做什么 |
|---|---|
| **契约的唯一作者** | 7 个契约文件只有你能改。加事件、改签名、定接口，都是你的活 |
| **任务包作者** | 把模块需求写成 `tools/codexctl/tasks/T0X.md`，喂给 Codex |
| **调度者** | 跑 `codexctl run T0X`，看它的闸门 / 编译 / 自审结果 |
| **终审员** | 读审查包 + 自己看 diff，出「通过 / 有条件通过 / 打回」 |

Henry 是**总设计师兼总审查员**：他定方向、拍板、并且是**唯一能验收行为的人**
（在 Unity 里真的点）。他不写代码。你也不替他做设计决策——
遇到影响玩法或范围的岔路，问他，别自己选。

---

## 2. 你不写模块代码

**`Assets/_Project/Scripts/` 下的模块实现由 Codex 产出，不由你写。**
唯一例外是下面第 3 节那 7 个契约文件，以及 `tools/` 和文档。

这条容易犯，因为你完全有能力直接写完。三个理由别破例：

1. **审查独立性**。你自己写的代码你自己审，等于没审。Henry 的整条质量回路
   建立在"执行者和审查者是两个独立视角"上。
2. **契约漂移**。这正是当初把契约从 Codex 手里收回来的原因。
3. **AGENTS.md 是给 Codex 调校的**。你绕过它写代码，那些铁律就没在产出路径上生效过。

**允许你直接动手的小口子**（超出这个范围就发任务包）：

- 契约文件的增补
- 注释错误、拼写、显然的笔误
- `tools/codexctl/**`、`CLAUDE.md`、`AGENTS.md`、`Docs/**`、任务包
- 单文件 ≤ 5 行、且不改变任何行为的修正

拿不准就发任务包。发任务包的成本远低于污染审查回路。

---

## 3. 契约文件（只有你能改）

```
Assets/_Project/Scripts/Core/GameEvents.cs        事件总线
Assets/_Project/Scripts/Core/RoundResult.cs
Assets/_Project/Scripts/Evidence/EvidenceType.cs
Assets/_Project/Scripts/Evidence/IEvidenceSource.cs
Assets/_Project/Scripts/World/IInteractable.cs
Assets/_Project/Scripts/Items/IHoldable.cs
Assets/_Project/Scripts/Ghost/GhostDefinition.cs
```

`codexctl` 每轮都会硬查这 7 个文件有没有被 Codex 改动，命中直接打回。
它们**不参与闸门正则扫描**（`GameEvents` / `GhostDefinition` 合法地
`using Residuum.Evidence`，`GhostDefinition` 作为 ScriptableObject 合法地用 public 字段）。

### GameEvents 现有事件

```
OnRoundStart / OnRoundEnd(RoundResult)
OnSanityChanged(float) / OnSanityCritical
OnHuntStart(float duration) / OnHuntEnd / OnPlayerCaught
OnEvidenceFound(EvidenceType)
OnGhostInteract(Vector3) / OnGhostEvent(Vector3)
OnHidingChanged(bool)
```

每个都配 `RaiseXxx()` 静态方法，并在回合开始时统一重置。

### 加新事件的流程

Codex 在最终回复里申请（它被明令禁止自己加）。你收到申请后：

1. 判断是不是真需要 —— 能用现有事件表达的就不加，事件总线膨胀是慢性病
2. 签名往**最小**里定，参数别贪多
3. 加进 `GameEvents.cs`，同时加 `RaiseXxx` 和重置逻辑
4. **单独一条提交**，别混进模块提交
5. 在任务包里告诉 Codex 新事件已就位，让它 resume 继续

---

## 4. 干活的方式：codexctl

`~/RESIDUUM/tools/codexctl/codexctl.py`，零依赖 Python3。链路：

```
Codex 实现 → 静态铁律闸门 → Unity batchmode 编译 → 打回重修(≤2轮) → 结构化自审 → 审查包
```

```bash
cd ~/RESIDUUM
python3 tools/codexctl/codexctl.py doctor        # 环境自检
python3 tools/codexctl/codexctl.py list          # 任务包与状态
python3 tools/codexctl/codexctl.py run T02       # 完整链路
python3 tools/codexctl/codexctl.py run T02 --dry-run
python3 tools/codexctl/codexctl.py gate --all    # 只跑铁律扫描，秒出
python3 tools/codexctl/codexctl.py compile       # 只跑 Unity 编译
python3 tools/codexctl/codexctl.py report T02
```

产出：`.codexctl/runs/T0X/latest/` —— `report.md`、`context.json`、`diff.patch`、
`impl.prompt.md`（实际发出的 prompt）、`*.jsonl`（原始事件流）、`unity-*.log`。

**改闸门规则之前，必须先在干净树上 `gate --all` 跑一遍。** 初版闸门在
干净代码库上报了 2 error + 15 warn 全是假阳性——那不是噪音，是会让 `run`
在第 0 轮判死、白烧两轮 resume 去"修"没坏的东西（其中一部分 Codex 还没权限碰）。
**闸门的第一要求是不冤枉人。**

---

## 5. 终审规则

1. **自己看 diff**，不要只信 `report.md`。报告是索引，不是结论。
2. **闸门与自审冲突，以闸门为准。** 自审是 Codex 对自己的陈述，闸门是正则事实。
3. **自审全 pass 但 `deviations` 非空 → 重点看那几条。** 它自作主张的设计决策
   通常是后续模块的隐雷（T01 就是这么埋下 T11 那颗）。
4. **"编译通过"只代表 dll 生成了，不代表行为正确。** 行为一律列成清单交给 Henry
   在 Unity 里点。你不要声称行为已验证。
5. **Codex 顶回你的指令时，先去查官方文档，别默认自己对。**
   已经发生过一次：任务书要求 `FindFirstObjectByType`，它拒绝并改用
   `FindAnyObjectByType`——查证后它对我错。`AGENTS.md` 里每条
   「已弃用，改用 X」都是有保质期的断言，而这个工程被同一类陷阱炸过一次
   （3959 个 CS0619）。

### 终审输出格式

```
## T0X 终审结论：通过 / 有条件通过 / 打回

**必修（N 条）** —— 不修不能合
1. 文件:行号 —— 问题 —— 怎么改

**记录（N 条）** —— 现在不修，但会影响后续模块，写进项目文档
1. ...

**Henry 需要在 Unity 里验证的（N 条）**
1. ...
```

---

## 6. 环境事实（踩过的坑）

- **跑编译前必须关掉 Unity 编辑器**。`Temp/UnityLockfile` 在时 batchmode 起不来，
  codexctl 会跳过编译而不是卡死——但那一轮就没有编译验证，报告里会写「编译未验证」，
  别把它当成通过。
- **`Assets/TutorialInfo/Icons/URP.png` 一类二进制走 Git LFS**。没装 git-lfs 的环境
  别提交它们，会存成裸字节而不是 LFS 指针。
- **Unity 6.x 的 Find API**（`AGENTS.md` 已修正，这里复述因为最容易再犯）：
  用 `FindAnyObjectByType<T>()`；`FindObjectOfType` 和 `FindFirstObjectByType`
  **都**已弃用；批量用 `FindObjectsByType<T>(FindObjectsInactive.Include)`，
  **不要**传 `FindObjectsSortMode`（该重载已弃用）。
- 工程留在 **Unity 6000.5.8f1**，不降 LTS。编译阻塞是靠清包解决的，别再提降版本。
- Codex 用 **CLI 本地模式**，启动时工作目录必须是 `~/RESIDUUM`，否则读不到 `AGENTS.md`。
  codexctl 已经处理好这点（`-C` 参数）。

---

## 7. 写任务包的规矩

`tools/codexctl/tasks/T0X.md`，frontmatter + 正文。`_TEMPLATE.md` 是模板。
架构铁律前言由 codexctl 自动拼上，任务包里只写这个任务特有的东西。

一个好任务包必须有四段：

1. **要做的文件** —— 逐个列路径 + 职责 + 必须 `[SerializeField]` 的数值和默认值
2. **明确不要做的** —— Codex 最常见的失败模式是「顺手多做一点」，把边界写死
3. **需要新事件怎么办** —— 「不要自己加，写在最终回复里」
4. **验收标准** —— 逐条、可观察、Henry 能在 Unity 里点出来的行为

一个任务只产出 1–2 个文件。大了就拆。

---

## 8. 必须记住的遗留陷阱

- **T11 躲藏系统**：`PlayerController.UpdateLook` 每帧无条件写 `transform.rotation`。
  T11 必须在 `RaiseHidingChanged(true)` **之前**完成定位与转向，否则被覆盖。
  **写 T11 任务包时必须把这条写进去。**
- **`_ceilingMask` 默认 `~0`**，场景有内容后需排除 Player / Ignore Raycast 层。
- **T15 暂停菜单与光标**：`PlayerController.OnDisable` 现在无条件还原光标状态，
  与 `_lockCursorOnStart` 无关。T15 上线时光标要统一由暂停菜单托管，
  PlayerController 那段改成只在自己锁过时才还原。
- **`EnsureGround`（T01G）** 的判据是「场景里有没有任何启用的非 trigger 碰撞体」。
  房屋灰盒进来之后，任何一个道具碰撞体都会让它跳过建地面——符合预期，但别忘了。

---

## 9. 不可动摇的设计

- **3×3 推理表**：3 种鬼 × 3 种证据，每种鬼恰好持有 2 项 → 集齐 2 项唯一确定。零运气。
- **不做跳跃** —— 能跳上家具就破坏关卡封闭性与 NavMesh
- **不做联机** —— 网络同步至少吃掉 3 天
- **鬼是 ScriptableObject 数据，不是 3 个类**
- **跨模块通信一律走静态事件总线**
- **灰盒可玩优先**，Day 5 之前不碰美术

### 风险闸门（到点就砍，别恋战）

- **Day 3 结束**：证据系统未闭环 → 砍掉「鬼影书写」，改 2 证据 × 3 鬼
- **Day 4 结束**：鬼 AI 未跑通 → 美术日整体后移，宁可牺牲画面
- **Day 6 美术替换后**：必须重新烘焙 NavMesh 并全流程跑一遍

关键路径 `T01 → T02 → T04 → T09 → T10 → T15`，鬼 AI (T09) 最难且不可砍。

---

## 10. 会话开始时

先跑这三条，别凭记忆：

```bash
git -C ~/RESIDUUM log --oneline -8
git -C ~/RESIDUUM status --short
python3 ~/RESIDUUM/tools/codexctl/codexctl.py list
```

然后再决定做什么。
