# codexctl

RESIDUUM 项目的 **Codex CLI 中转控制器**。

一条命令跑完：`Codex 实现 → 静态铁律闸门 → Unity batchmode 编译 → 打回重修 →
结构化自审 → 生成给 Claude 的审查包`。

## 它解决什么

现在的链路是 `Codex 写 → Codex 自审 → Henry 在 Unity 验 → Claude 终审`，
有三个洞：

| 洞 | codexctl 怎么补 |
|---|---|
| **Codex 编译不了 C#**，自审是唯一自动关卡 | 本地起 Unity batchmode，把真实 `CS####` 错误喂回同一个 codex thread 让它自己修 |
| **自审是自然语言，Codex 说「合规」你没法验** | `--output-schema` 强制成 18 项结构化 JSON，同时用正则独立跑一遍铁律，两边对不上以正则为准 |
| **契约文件全靠 Codex 自觉不改** | 每轮 diff 后对着 7 个契约文件的清单硬查，改了直接打回 |

## 安装

```bash
# 放哪都行，建议 ~/RESIDUUM/Tools/codexctl
mkdir -p ~/RESIDUUM/Tools && cp -r codexctl ~/RESIDUUM/Tools/
cd ~/RESIDUUM/Tools/codexctl
chmod +x codexctl.py
ln -sf "$PWD/codexctl.py" /usr/local/bin/codexctl   # 可选

# 改配置里的 project_root
$EDITOR codexctl.config.json
```

只依赖 Python 3.9+（macOS 自带的就行），零第三方包。

## 第一次跑

```bash
codexctl doctor    # 环境自检：codex 在不在、登录没、Unity 在哪、契约文件全不全
codexctl probe     # 真跑一次 codex（只读沙箱），验证 exec / resume / --output-schema
                   # 三种调用在你这个 codex 版本上参数顺序是对的
codexctl list      # 看任务包
```

`probe` 很重要 —— codex CLI 的参数约定改过几次（比如 `-a/--ask-for-approval`
是全局标志，必须放在 `exec` **之前**，放后面直接报错）。probe 通过再跑 run。

## 日常

```bash
codexctl run T01F              # 完整链路
codexctl run T02 --dry-run     # 只打印会执行的命令
codexctl gate --all            # 只跑铁律扫描，秒出
codexctl compile               # 只跑 Unity 编译（先关掉 Unity 编辑器！）
codexctl report T02            # 打印最近一次审查包
codexctl audit T02             # 对已有 thread 重跑结构化自审
```

跑完在 Claude Code 里：

```
/review T02
```

## 产出在哪

```
~/RESIDUUM/.codexctl/runs/T02/latest/
├── report.md          ← 给 Claude 看的审查包（人也能看）
├── context.json       ← 同样内容的结构化版本
├── diff.patch         ← 本次任务的完整 diff
├── impl.jsonl         ← codex 实现回合的原始事件流
├── impl.prompt.md     ← 实际发出去的 prompt（含架构铁律前言）
├── repair1.*          ← 第 1 轮打回重修
├── audit.json         ← 18 项结构化自审结果
├── audit.schema.json  ← 喂给 --output-schema 的 schema
└── unity-0.log        ← Unity batchmode 完整日志
```

`.codexctl/` 会自动加进 `.gitignore`。

## 任务包

`tasks/*.md`，frontmatter + Markdown 正文：

```markdown
---
id: T02
title: 交互系统与门
branch: codex/t02-interaction
depends: T01
unity: true
---

## 目标
...
```

正文会自动拼上架构铁律前言（`PREAMBLE`）：事件总线、7 个契约文件不许改、
Unity 6 API 禁令、SerializeField 要求。写任务包时只写这个任务特有的东西。

`_TEMPLATE.md` 是模板，下划线开头的文件不会被当成任务。

## 配置要点

```jsonc
{
  "project_root": "~/RESIDUUM",
  "codex": {
    "sandbox": "workspace-write",     // 让它能改文件
    "approval": "never",              // 非交互模式下 on-request 会被静默降级成 never，直接写死
    "use_subscription_auth": true,    // 屏蔽 OPENAI_API_KEY，走 ChatGPT 订阅额度而不是 API 计费
    "model": null                     // 换模型会让 prompt cache 失效约六成，别频繁换
  },
  "unity": {
    "editor_path": null,              // null = 按 ProjectVersion.txt 从 Hub 里自动找
    "project_scope_prefix": "Assets/_Project"   // 用来把「你的错」和「包的错」分开
  },
  "repair": { "max_attempts": 2 },
  "contract_files": [ /* 7 个 */ ],
  "gate_rules":     [ /* 正则 + 级别 + 说明 */ ],
  "audit_checklist":[ /* 18 项，改这里 schema 自动跟着变 */ ]
}
```

`audit_checklist` 请换成 `06_审查流程.md` 里那份真实的 18 项 —— 现在这份是
按项目总览推出来的近似版。

## 已知限制

- **Unity 编辑器开着这个工程时，batchmode 起不来**（`Temp/UnityLockfile`）。
  codexctl 会检测到并跳过编译而不是卡死，但那一轮就没有编译验证了。
- 编译通过 ≠ 行为正确。行为必须 Henry 在 Unity 里点。
- `resume` 会累积上下文，输入 token 每轮大致翻倍；`max_attempts` 别调太高。
- 打回重修只喂「机器能证明的错」（契约、闸门、编译）。设计层面的问题
  仍然由 Claude 终审给出，人来决定要不要再发一轮。
