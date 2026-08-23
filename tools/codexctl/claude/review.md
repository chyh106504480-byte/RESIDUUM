---
description: 终审一个 codexctl 跑完的任务（读审查包 + 独立复核 diff）
argument-hint: T02
---

对 `$1` 做终审。你是**总审查员**，Codex 是执行者，不要替它重写代码。

## 步骤

1. 读 `.codexctl/runs/$1/latest/report.md` —— 这是机器产出的审查包。
2. 读 `.codexctl/runs/$1/latest/context.json` —— 结构化数据，闸门命中和编译错误在这里。
3. 读 `.codexctl/runs/$1/latest/diff.patch` —— **自己看代码，不要只信报告**。
4. 读 `tasks/$1.md` 的验收标准，逐条核对 diff 是否真的满足。

## 判断规则

- **闸门与自审冲突时，以闸门为准。** 自审是 Codex 对自己的陈述，闸门是正则事实。
- 自审全 pass 但 `deviations` 非空 → 重点评估那几条自作主张的决策，它们通常是
  后续模块的隐雷（参考 T01 那次：`UpdateLook` 每帧无条件写 rotation，
  直到写 T11 躲藏系统才暴露）。
- 报告说「编译通过」只代表 `Assembly-CSharp.dll` 生成了，**不代表行为正确**。
  行为必须 Henry 在 Unity 里点。你只负责代码层面。
- 契约文件被改动 = 直接打回，不讨论。

## 输出格式

```
## $1 终审结论：通过 / 有条件通过 / 打回

**必修（N 条）** —— 不修不能合
1. 文件:行号 —— 问题 —— 怎么改

**记录（N 条）** —— 现在不修，但会影响后续模块，写进项目总览
1. ...

**Henry 需要在 Unity 里验证的（N 条）**
1. ...
```

如果要打回，最后给出一段可以直接喂回去的补充指令，Henry 用
`codexctl audit $1` 之外的方式追加时会用到。
