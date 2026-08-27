# 残响 · RESIDUUM

第一人称灰盒恐怖解谜原型。Unity 6 · URP · 单机。

---

## ⚠️ 第一次拿到这个仓库？先读这个

**直接从 Unity Hub 添加会打不开，或者打开是空场景。** 这不是仓库坏了，
是有三件事必须先做。完整步骤在这里：

### 👉 [`Docs/10_Windows上手指南.md`](Docs/10_Windows上手指南.md)

三分钟版本：

| # | 必做 | 不做的后果 |
|---|---|---|
| 1 | 装 **Unity `6000.5.8f1`**（Hub 列表里没有，用深链 `unityhub://6000.5.8f1/5cb7df797b7d`） | Hub 能添加项目，但点了进不去 |
| 2 | 装 **Git LFS** 并 `git lfs install`，用 `git clone`（**不要 Download ZIP**） | 3 个贴图/探针文件变成文本指针，导入报错 |
| 3 | 从 Asset Store 免费领取并导入 **Apartment Kit v4.2**，导入后转 URP | Blockout 场景 171 处引用丢失，一片空白或品红 |

---

## 环境

| 项 | 值 |
|---|---|
| Unity | `6000.5.8f1`（revision `5cb7df797b7d`）—— **锁定，不升不降** |
| 渲染管线 | URP 17.5.0 |
| 输入 | Input System 1.20.0 |
| 导航 | AI Navigation 2.0.14 |
| 主场景 | `Assets/_Project/Scenes/Blockout.unity` |

---

## 目录

```
Assets/_Project/
  Scripts/          模块实现（Core / Evidence / Ghost / Player / World / UI）
  Scenes/           Blockout.unity —— 主灰盒场景
  ScriptableObjects/Ghosts/   3 种鬼的数据定义
Docs/               设计文档，见下表
tools/codexctl/     任务调度与静态闸门工具链
```

## 文档索引

| 文档 | 内容 |
|---|---|
| [`01_GDD_残响.md`](Docs/01_GDD_残响.md) | 玩法与全部数值：理智衰减、移动速度、猎杀概率、证据判定 |
| [`02_技术架构.md`](Docs/02_技术架构.md) | 目录结构、四个接口、五条架构铁律 |
| [`03_Codex任务包.md`](Docs/03_Codex任务包.md) | T01–T17 任务原始定义 |
| [`04_七天排期.md`](Docs/04_七天排期.md) | 排期与交付清单 |
| [`05_美术协作规范.md`](Docs/05_美术协作规范.md) | 灰盒契约，美术汇合时用 |
| [`06_审查流程.md`](Docs/06_审查流程.md) | 18 项自审清单 |
| [`09_验收路径.md`](Docs/09_验收路径.md) | 每个模块在 Unity 里怎么点验 |
| [`10_Windows上手指南.md`](Docs/10_Windows上手指南.md) | **新成员上手，Windows 端必读** |
| [`ASSET_LICENSES.md`](Docs/ASSET_LICENSES.md) | 第三方素材许可清单，每导入一个加一行 |

---

## 协作纪律

1. **场景文件同一时间只能一个人改。** `Blockout.unity` 5.7 MB，两人同时改
   必然冲突且无法合并。改之前群里喊一声，推送完再喊一声。
2. **拉取前先提交自己的改动**，不要在工作区脏的时候 `git pull`。
3. **`Library/`、`Temp/`、`Assets/Brick Project Studio/` 永不入库**，
   不要 `git add -f`。
4. 提交前扫一眼 `git status --short`，看到不该有的东西先问再提交。
