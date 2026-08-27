# 10 · Windows 端上手指南

**给谁看**：拿到 GitHub 仓库、在 Windows 上第一次打开《残响》的成员。
**结论先行**：仓库本身没有问题，Unity Hub 一定能识别这个工程。打不开 / 打开是空的，
只可能是下面三件事之一没做对。照这份文档从头走一遍，不要跳步。

---

## 为什么说仓库没问题

以下几项已在 macOS 端逐条核对过，都是正常的（列在这里是为了你不用再怀疑仓库）：

- `ProjectSettings/ProjectVersion.txt` 已入库 —— Unity Hub 靠它识别项目版本
- `Packages/manifest.json` 与 `packages-lock.json` 已入库，没有任何 `file:` 本地路径依赖
- `Assets/` 下 137 个入库文件，`.meta` 一个不缺（含文件夹 meta）
- 没有符号链接、没有大小写重名、没有 Windows 非法字符（`: * ? " < > |`）的文件名
- 最长路径 85 字符，远低于 Windows 260 限制
- 本地 `main` 与 `origin/main` 完全同步

所以：**你遇到的问题在你这一侧的环境，不在代码里。**

---

## 第 0 步 · 装对三样东西

### 1. Git for Windows + Git LFS

从 <https://git-scm.com/download/win> 装 Git。安装向导里勾上 Git LFS（默认勾选）。
装完开一个 PowerShell 确认：

```bash
git --version && git lfs version
```

两条都要有输出。然后**必须**执行一次（每台机器只需一次）：

```bash
git lfs install
```

**没装 LFS 的后果**：仓库里 3 个文件（`Assets/TutorialInfo/Icons/URP.png`、
两个反射探针 `.exr`）会变成 130 字节左右的文本文件，Unity 导入时报错。

### 2. Unity Hub

<https://unity.com/download>

### 3. Unity 编辑器 —— 必须是 `6000.5.8f1`，一个字都不能差

这是最常见的「Hub 里能添加、点了进不去」的原因：**Hub 的 Installs 列表默认只列
LTS 和推荐版，不会列出 6000.5.8f1**，所以你在 Hub 里手动找是找不到的，必须用深链。

装好 Unity Hub 之后，在浏览器地址栏粘贴这个链接并回车，Hub 会自动接管：

```
unityhub://6000.5.8f1/5cb7df797b7d
```

深链不生效的话，用直链下载安装器（4.06 GB，已验证可下载）：

```
https://download.unity3d.com/download_unity/5cb7df797b7d/Windows64EditorInstaller/UnitySetup64-6000.5.8f1.exe
```

安装时**模块只需要勾** `Microsoft Visual Studio Community` 或 `Visual Studio Editor`
（二选一，用于写 C#）。Android / iOS / WebGL 全都不要勾，工程不需要，能省十几个 GB。

> **不要用别的版本"凑合打开"。** Unity Hub 允许你用更高版本打开旧工程，但那会
> 不可逆地升级 `ProjectSettings` 和场景文件，一提交就把所有人炸掉。工程锁定
> 6000.5.8f1，不降 LTS，也不升。

---

## 第 1 步 · 克隆仓库

### 不要用 GitHub 网页的「Download ZIP」

ZIP 包里的 LFS 文件是指针文本，不是真文件，而且没有 `.git` 目录，你没法拉更新、
没法提交。**必须用 git clone。**

先做两条全局配置（每台机器一次）：

```bash
git config --global core.longpaths true
```

```bash
git config --global core.autocrlf true
```

然后克隆。**路径要求**（很重要）：

- 全英文路径，不要有中文、空格
- **不要放在 OneDrive / 百度网盘 / 坚果云等同步目录里** —— 同步软件会锁住
  `Library/` 里的文件，Unity 导入会随机失败
- 不要放在桌面（桌面在中文用户名下就是中文路径）
- 建议直接放盘符根附近，例如 `D:\Dev\`

```bash
cd /d D:\Dev && git clone https://github.com/chyh106504480-byte/RESIDUUM.git
```

克隆完确认 LFS 文件是真文件而不是指针：

```bash
cd D:\Dev\RESIDUUM && git lfs ls-files
```

应该列出 3 个文件。如果这条命令输出为空，说明 `git lfs install` 没生效，
补跑一次然后 `git lfs pull`。

---

## 第 2 步 · Unity Hub 添加项目

1. Unity Hub → `Projects` → 右上角 `Add` → `Add project from disk`
2. 选 **`D:\Dev\RESIDUUM`** 这一层 —— 就是**直接包含 `Assets` 和 `ProjectSettings`
   两个文件夹的那一层**。不要选它的父目录，也不要选进 `Assets` 里面。
3. 添加后看项目那一行右侧的 **Editor Version** 列：
   - 显示 `6000.5.8f1` 且是普通颜色 → 正常，点项目名打开
   - 显示黄色感叹号 / 带下载图标 / 点了没反应 → **版本没装，回第 0 步第 3 条**

**首次打开要 15–40 分钟**（Unity 要把 `Assets` 全量导入、生成 `Library/`）。
进度条走到一半像卡死是正常的，**不要中途关掉**，关掉会留下半截 `Library/`，
下次打开更慢甚至报错。真要重来就把 `Library/` 整个删掉重新打开。

---

## 第 3 步 · 导入 Apartment Kit（不做这步，场景是空的）

**这是「工程打开了，但 Blockout 场景里什么都没有 / 一片品红」的唯一原因，
也是最容易被误认为"项目坏了"的一步。**

### 先确认你是不是撞上了这个

打开 `Blockout.unity` 之后，如果 Scene 视图里是这样：

- 看不到任何墙、地板、家具，只有几个**青色/白色线框盒子**
- 空中飘着 `Int_Apt_01_Floor_01 (45)` 之类的**名字标签**，但对应位置什么都没有
- Hierarchy 里对象都在、层级完整，但图标是断裂的预制体图标
- 可能还有一两个**品红色**的物体

那就是这一步没做（或者没做对）。**工程本身完全正常。**

原因是硬的：`Blockout.unity` 里有 **1756 个预制体实例**，其中 **171 个外部引用**
指向 Apartment Kit。工程自带的可见几何体只有 41 个 MeshRenderer。
换句话说，**没有这个包，场景里 95% 的东西都渲染不出来**，剩下的就是你看到的那几个线框。

### 为什么不干脆把这个包也提交到仓库

它 325.9 MB，走 Git LFS 几天就会耗尽 GitHub 免费额度；而且 Unity Asset Store
的条款不允许把素材原始文件单独打包分发。所以**约定不入库，每人各自导入**。
资源 GUID 是包自带的，只要三个人导入的是**同一版本**，场景引用就能对上。

### 怎么导

1. 用**你自己的 Unity 账号**登录，打开
   <https://assetstore.unity.com/packages/3d/environments/apartment-kit-124055>
2. 这是**免费**资源，点 `Add to My Assets`（加入我的资源）
3. 回到 Unity 编辑器 → `Window` → `Package Manager` → 左上角下拉切到 `My Assets`
   → 找到 `Apartment Kit` → `Download` → `Import`
4. **版本必须是 v4.2**，和其他人保持一致
5. 导入路径保持默认，最终必须落在 **`Assets/Brick Project Studio/`**，
   这个目录已在 `.gitignore` 里排除，不会被你误提交。**不要改名、不要挪位置**，
   改了 GUID 对得上但路径对不上，场景照样报丢失。

### 导入后必须转 URP，否则满屏品红

商店页标注该包只兼容 Built-in 渲染管线，工程用的是 URP。因为包不入库，
**每个人在自己机器上都要各转一次**：

`Window` → `Rendering` → `Render Pipeline Converter` → 选 `Built-in to URP`
→ **只勾 `Material Upgrade`**（千万不要勾 `Rendering Settings`，那会改动工程设置）
→ `Initialize Converters` → `Convert Assets`

转完仍有零星品红是正常的（个别 Built-in 专有 Shader 转换器处理不了），
手动把 Shader 改成 `Universal Render Pipeline/Lit` 即可，灰盒阶段可以先放着。

转换改的是包内材质资产，**不可逆**。转坏了直接删掉 `Assets/Brick Project Studio/`
重新导入，反正不在仓库里。

详见 [`ASSET_LICENSES.md`](ASSET_LICENSES.md)。

### 做完跑一次对账，不要靠肉眼判断

仓库里有一个零依赖的对账脚本，直接告诉你缺什么、以及 URP 转了没有。
在**仓库根目录**运行：

```bash
python tools\check_kit.py
```

（Mac / Linux 上是 `python3 tools/check_kit.py`。Windows 没装 Python 的话，
在 Microsoft Store 搜 "Python 3" 一键安装，或去 <https://www.python.org/downloads/windows/>。）

它会比对你本地的 `Assets/Brick Project Studio/` 和
[`tools/kit_manifest.txt`](../tools/kit_manifest.txt)（在已验证可用的 macOS 端工程上生成的
171 项 GUID 清单），可能的结果：

| 输出 | 含义 | 下一步 |
|---|---|---|
| `✓ GUID 对账通过` + `✓ URP 转换已做` | 素材包没问题 | 场景还是空的就截 Console 报错发群里 |
| `✗ 没有找到 Assets/Brick Project Studio/` | 压根没导入 | 回到本节开头重做 |
| `✗ GUID 对账失败：缺 171 / 171` | 导入的**版本不对**，GUID 全不一样 | 见下 |
| `✗ 缺 N / 171`（N < 171） | 导入时取消勾选过内容 | Import 时全选，别取消任何勾 |
| `✗ 绝大多数材质还是 Built-in` | 忘了转 URP | 跑上面的 Render Pipeline Converter |

### 版本必须完全一致，这是 GUID 对不上的根因

Package Manager 的 `My Assets` **默认给你下最新版**。Apartment Kit 换版本时
资源 GUID 可能整体变掉，那样 171 项一个都对不上，场景照样空白。

脚本报「缺 171/171」时，先删掉 `Assets/Brick Project Studio/`，再核对
`.unitypackage` 的指纹。Windows 上在 PowerShell 里跑：

```bash
Get-FileHash "$env:APPDATA\Unity\Asset Store-5.x\Brick Project Studio\3D ModelsEnvironments\Apartment Kit.unitypackage" -Algorithm SHA256
```

必须是与工程一致的这一份：

```
size   = 341734961  （325.9 MB）
sha256 = A984753A958350390EC074C6C56146FA68E3826A0E39463C85B398C9DBB5496A
```

对不上就是版本不同，找 Henry 要正确的那一份，不要将就用手上这个。

---

## 第 4 步 · 验证你真的装好了

依次确认这四条，全过才算上手完成：

1. Unity 标题栏版本号是 `6000.5.8f1`
2. `python tools\check_kit.py` 输出 **`✓ GUID 对账通过`** 和 **`✓ URP 转换已做`**
3. 打开 `Assets/_Project/Scenes/Blockout.unity`，Console 里**没有红色报错**
   （黄色警告可以忽略）
4. Scene 视图里看得见完整的房间陈设 —— 有墙、有地板、有家具，
   不是一片空白线框，也不是一片品红
5. 按 Play，能用 WASD 走动、鼠标转视角

---

## 常见现象对照表

| 现象 | 原因 | 解法 |
|---|---|---|
| Hub 里项目版本号是黄的 / 点了没反应 | `6000.5.8f1` 没装 | 第 0 步第 3 条的深链 |
| Hub 说「不是有效的 Unity 项目」 | 选错了文件夹层级 | 选包含 `Assets` 和 `ProjectSettings` 的那一层 |
| 打开后进 Safe Mode / 一堆 CS 报错 | 首次导入没跑完就关了 | 删掉 `Library/` 重新打开 |
| 场景只剩线框和飘在空中的名字标签 | Apartment Kit 没导入 | 第 3 步，跑 `check_kit.py` |
| 场景全是品红 | Apartment Kit 导入了但没转 URP | 第 3 步的 Render Pipeline Converter |
| 导入了 Kit 但场景还是空的 | 版本不对，GUID 全变了 | 第 3 步末尾的指纹核对 |
| Console 报某个 `.png` / `.exr` 无法导入 | Git LFS 没装，文件是指针 | `git lfs install` 然后 `git lfs pull` |
| 导入时报路径过长 | 仓库放得太深 | 挪到 `D:\Dev\RESIDUUM`，并 `core.longpaths true` |
| Package Manager 一直卡在 Resolving | 网络访问 Unity registry 慢 | 挂代理，或等；不要中途关编辑器 |

---

## 日常协作纪律（比上手更重要，请读完）

### 1. 场景文件同一时间只能一个人改

`Assets/_Project/Scenes/Blockout.unity` 现在 **5.7 MB**。Unity 场景是 YAML，
两个人同时改**必然冲突，而且几乎无法手工合并**——合错了就是整个关卡报废。

约定：**要改场景先在群里喊一声，改完提交推送再喊一声。** 没拿到"令牌"就不要动场景。
改脚本、改预制体、改 ScriptableObject 不受此限制。

### 2. 拉取之前先提交自己的改动

```bash
git status --short
```

有改动先 `git add` + `git commit`，再 `git pull`。**不要在有未提交改动时 pull**，
Unity 的二进制/YAML 资产在 stash 里来回滚很容易出事。

### 3. 这些本地文件不入库，看到它们别慌

`Library/`、`Temp/`、`Logs/`、`obj/`、`UserSettings/`、`*.csproj`、`*.sln`、
`Assets/Brick Project Studio/` 全都在 `.gitignore` 里。它们是每台机器各自生成的，
**永远不要用 `git add -f` 强加进去**。

### 4. 提交前扫一眼你到底提交了什么

```bash
git status --short && git diff --cached --stat
```

看到 `Library/` 或 `Brick Project Studio` 出现在里面，立刻停下来问。

---

## 还是打不开怎么办

把下面这条命令的完整输出发到群里，加上 **Unity Console 的红色报错截图**：

```bash
git -C D:\Dev\RESIDUUM log --oneline -3 && git -C D:\Dev\RESIDUUM status --short && git lfs version && git lfs ls-files && python D:\Dev\RESIDUUM\tools\check_kit.py
```

有这些信息才好判断，光说"打不开"定位不了。
