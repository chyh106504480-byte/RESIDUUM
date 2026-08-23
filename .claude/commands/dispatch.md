---
description: 派活给 Codex 并接住结果
argument-hint: T02
---

跑 `$1`，然后处理结果。

## 前置检查

```bash
git -C ~/RESIDUUM status --short          # 工作区必须干净
ls ~/RESIDUUM/Temp/UnityLockfile          # 存在 = Unity 开着，编译会被跳过
```

Unity 开着的话，先问 Henry 要不要关掉再跑——不关就没有编译验证这一关，
这是 codexctl 存在的主要理由，别轻易放弃。

## 跑

```bash
cd ~/RESIDUUM && python3 tools/codexctl/codexctl.py run $1
```

这一步会跑很久（Codex 实现 + 最多 2 轮打回重修 + Unity 编译 + 自审）。

## 接住结果

- **退出码 0** → 直接进 `/review $1`
- **退出码 1** → 先看是哪一关卡住的：
  - 契约文件被改 → 打回重写，并检查是不是任务包没说清「需要新事件怎么办」
  - 闸门 error → 先确认**不是假阳性**（对着 CLAUDE.md 第 4 节）。是规则的问题就修规则，
    修完必须在干净树上 `gate --all` 复验
  - 编译错误 → 看 `.codexctl/runs/$1/latest/unity-*.log`，
    区分 `_Project` 的错和包层的错，包层的错不是这个任务的责任
  - 打回 2 轮还不过 → 停手，别再喂。多半是任务包本身有歧义，回去改任务包

不管哪种，都跟 Henry 说清楚卡在哪一关、你打算怎么办，再动手。
