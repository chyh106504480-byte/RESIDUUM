---
description: 往契约里加事件/接口成员（只有你能做这件事）
argument-hint: OnDoorStateChanged(IInteractable door, bool isOpen)
---

Codex 申请了新契约：`$ARGUMENTS`

## 先别急着加

事件总线膨胀是慢性病，加进去就很难删。逐条问：

1. **现有事件能不能表达？** 列出 `GameEvents.cs` 里所有现有事件，逐个比对
2. **这真的是跨模块通信吗？** 同模块内部用不着走总线，那是 Codex 偷懒
3. **参数能不能更少？** 往最小里定。传 `Vector3` 够用就不要传整个组件引用
4. **谁订阅、谁触发？** 说不清就是设计还没想清楚，先问 Henry

判断是"不该加"的话，写清理由，让 Codex 用现有事件改实现。

## 要加的话

1. 改 `Assets/_Project/Scripts/Core/GameEvents.cs`：
   - `public static event Action<...> OnXxx;`
   - `public static void RaiseXxx(...) => OnXxx?.Invoke(...);`
   - **加进回合开始的静态重置**——漏了就会跨局残留，这是最难查的一类 bug
2. 中文注释写明：谁触发、谁订阅、什么时机
3. **单独一条提交**，`feat(contract): 新增 OnXxx 事件`，不要混进模块提交
4. 更新 `AGENTS.md` 里的事件清单（如果那里列了）
5. 告诉 Codex 契约已就位，让它 resume 继续

## 别做的

- 不要顺手重构 `GameEvents.cs` 的其它部分
- 不要因为"以后可能用得上"预先加事件
- 不要改已有事件的签名——那会让所有已过审的模块静默失效
