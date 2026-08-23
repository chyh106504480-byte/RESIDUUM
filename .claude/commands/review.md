---
description: 终审一个 codexctl 跑完的任务
argument-hint: T02
---

对 `$1` 做终审。你是终审员，不是执行者——**不要替 Codex 重写代码**。

## 读这些

1. `.codexctl/runs/$1/latest/report.md` —— 机器产出的审查包，当索引用
2. `.codexctl/runs/$1/latest/context.json` —— 结构化数据，闸门命中和编译错误在这
3. `.codexctl/runs/$1/latest/diff.patch` —— **自己看代码**
4. `tools/codexctl/tasks/$1.md` 的验收标准 —— 逐条核对 diff 是否真的满足

## 判断

- 闸门与自审冲突 → 以闸门为准
- 自审全 pass 但 `deviations` 非空 → 重点评估那几条自作主张的决策
- 报告写「编译未验证」→ 不是通过。让 Henry 关掉 Unity 后跑 `codexctl compile`
- 「编译通过」只代表 dll 生成了，行为要 Henry 在 Unity 里点
- 契约文件被改动 = 直接打回，不讨论
- Codex 顶回了任务书里的某条指令 → 先查官方文档再下结论

## 输出

按 CLAUDE.md 第 5 节的格式：必修 / 记录 / Henry 需要在 Unity 里验证的。

打回的话，最后附一段可以直接追加给 Codex 的补充指令。
