#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
check_kit.py —— Apartment Kit 对账工具

场景 Blockout.unity 里 1756 个 prefab 实例中，有 171 个外部引用指向
Apartment Kit 这个不入库的素材包。包没导入、版本不对、或者路径被改过，
场景打开就是一片空白（只剩线框和名字标签）。

这个脚本比对你本地的 Assets/Brick Project Studio/ 和
tools/kit_manifest.txt（在 macOS 端已验证可用的工程上生成），
直接告诉你缺什么、以及是不是忘了转 URP。

用法（在仓库根目录）:
    python3 tools/check_kit.py
Windows 上通常是:
    python tools\\check_kit.py
"""

import os
import re
import sys

KIT_ROOT = os.path.join("Assets", "Brick Project Studio")
MANIFEST = os.path.join("tools", "kit_manifest.txt")
URP_LIT_GUID = "933532a4fcc9baf4fa0491de14d08ed7"
BUILTIN_SHADER_GUID = "0000000000000000f000000000000000"

GREEN, RED, YELLOW, DIM, RESET = "\033[32m", "\033[31m", "\033[33m", "\033[2m", "\033[0m"
if os.name == "nt" and not os.environ.get("WT_SESSION"):
    GREEN = RED = YELLOW = DIM = RESET = ""


def die(msg, hint=""):
    print(f"\n{RED}✗ {msg}{RESET}")
    if hint:
        print(f"  {hint}")
    sys.exit(1)


def load_manifest(path):
    if not os.path.isfile(path):
        die(f"找不到 {path}", "请在仓库根目录运行本脚本。")
    rows = []
    with open(path, encoding="utf-8") as fh:
        for line in fh:
            line = line.rstrip("\n")
            if not line or line.startswith("#"):
                continue
            guid, _, rel = line.partition("\t")
            rows.append((guid, rel))
    return rows


def scan_local_guids(root):
    """遍历本地 Kit 目录，建立 guid -> 实际路径 的索引。"""
    found = {}
    for dirpath, _, filenames in os.walk(root):
        for name in filenames:
            if not name.endswith(".meta"):
                continue
            full = os.path.join(dirpath, name)
            try:
                with open(full, encoding="utf-8", errors="ignore") as fh:
                    head = fh.read(400)
            except OSError:
                continue
            m = re.search(r"^guid: ([0-9a-f]{32})", head, re.M)
            if m:
                found[m.group(1)] = full[: -len(".meta")]
    return found


def check_urp(root):
    """抽查 Kit 材质指向的 shader，判断 Built-in → URP 转换做了没有。"""
    urp = builtin = other = 0
    for dirpath, _, filenames in os.walk(root):
        for name in filenames:
            if not name.endswith(".mat"):
                continue
            try:
                with open(os.path.join(dirpath, name), encoding="utf-8", errors="ignore") as fh:
                    text = fh.read(2000)
            except OSError:
                continue
            m = re.search(r"m_Shader: \{fileID: -?\d+, guid: ([0-9a-f]{32})", text)
            if not m:
                other += 1
            elif m.group(1) == URP_LIT_GUID:
                urp += 1
            elif m.group(1) == BUILTIN_SHADER_GUID:
                builtin += 1
            else:
                other += 1
    return urp, builtin, other


def main():
    if not os.path.isdir("Assets") or not os.path.isdir("ProjectSettings"):
        die("当前目录不是 Unity 工程根目录",
            "请先 cd 到仓库根目录（就是同时含 Assets 和 ProjectSettings 的那一层）。")

    manifest = load_manifest(MANIFEST)
    print(f"\n对账清单: {len(manifest)} 项（来自 tools/kit_manifest.txt）")

    if not os.path.isdir(KIT_ROOT):
        print(f"\n{RED}✗ 没有找到 {KIT_ROOT}/{RESET}")
        print("""
  Apartment Kit 根本没导入。这就是场景空白的原因。

  怎么办：
    1. 用你自己的 Unity 账号打开
       https://assetstore.unity.com/packages/3d/environments/apartment-kit-124055
    2. 点 "Add to My Assets"（免费）
    3. Unity → Window → Package Manager → 左上角切到 "My Assets"
       → 找到 Apartment Kit → Download → Import（全选，别取消勾选）
    4. 导入路径保持默认，必须落在 Assets/Brick Project Studio/
    5. 导入后做 URP 转换（见 Docs/10_Windows上手指南.md 第 3 步）
    6. 回来重跑本脚本
""")
        sys.exit(1)

    local = scan_local_guids(KIT_ROOT)
    print(f"本地 {KIT_ROOT}/ 扫到 {len(local)} 个带 GUID 的资产")

    missing = [(g, rel) for g, rel in manifest if g not in local]
    moved = [(g, rel, local[g]) for g, rel in manifest
             if g in local and os.path.normpath(local[g]) != os.path.normpath(rel)]

    print()
    if not missing:
        print(f"{GREEN}✓ GUID 对账通过：场景需要的 {len(manifest)} 项全部命中{RESET}")
    else:
        print(f"{RED}✗ GUID 对账失败：缺 {len(missing)} / {len(manifest)} 项{RESET}")
        print(f"\n{DIM}  缺失样例（最多列 15 条）：{RESET}")
        for g, rel in missing[:15]:
            print(f"    {g}  {rel}")
        if len(missing) > 15:
            print(f"    … 还有 {len(missing) - 15} 条")
        if len(missing) == len(manifest):
            print("""
  一项都没命中 = 你导入的包和工程用的不是同一份。
  最可能是 Package Manager 给你下了更新的版本，GUID 全变了。

  怎么办：先删掉 Assets/Brick Project Studio/，然后核对 .unitypackage 指纹：
""")
        else:
            print("""
  部分命中 = 导入时取消勾选了一些内容，或者事后删过文件。
  怎么办：Package Manager → My Assets → Apartment Kit → Import，
  这次全选，不要取消任何勾选。
""")
        print("""    Windows 的 Asset Store 缓存在:
      C:\\Users\\<你的用户名>\\AppData\\Roaming\\Unity\\Asset Store-5.x\\Brick Project Studio\\3D ModelsEnvironments\\Apartment Kit.unitypackage

    在 PowerShell 里对指纹:
      Get-FileHash "$env:APPDATA\\Unity\\Asset Store-5.x\\Brick Project Studio\\3D ModelsEnvironments\\Apartment Kit.unitypackage" -Algorithm SHA256

    应当是（与 macOS 端工程一致的那一份）:
      size   = 341734961
      sha256 = A984753A958350390EC074C6C56146FA68E3826A0E39463C85B398C9DBB5496A

    对不上就说明版本不同，找 Henry 要正确的那一份。""")

    if moved:
        print(f"\n{YELLOW}⚠ {len(moved)} 项 GUID 对得上但路径不同（包被挪过位置）{RESET}")
        for g, rel, actual in moved[:5]:
            print(f"    期望 {rel}\n    实际 {actual}")
        print("  GUID 一致的话场景引用不会断，但建议还是放回默认路径。")

    urp, builtin, other = check_urp(KIT_ROOT)
    total = urp + builtin + other
    print(f"\nURP 转换检查：{total} 个材质中 URP/Lit {urp} 个，Built-in {builtin} 个，其它 {other} 个")
    if builtin > 10:
        print(f"{RED}✗ 绝大多数材质还是 Built-in —— 场景会满屏品红{RESET}")
        print("""
  怎么办：Window → Rendering → Render Pipeline Converter
          → 选 Built-in to URP → 只勾 Material Upgrade
            （不要勾 Rendering Settings，那会改动工程设置）
          → Initialize Converters → Convert Assets
""")
    elif builtin > 0:
        print(f"{GREEN}✓ URP 转换已做{RESET}（剩 {builtin} 个 Built-in 属正常，"
              "是转换器处理不了的专有 Shader，灰盒阶段可以放着）")
    else:
        print(f"{GREEN}✓ URP 转换已做{RESET}")

    ok = not missing and builtin <= 10
    print()
    if ok:
        print(f"{GREEN}结论：素材包没问题。打开 Assets/_Project/Scenes/Blockout.unity "
              f"应该能看到完整房间。{RESET}")
        print(f"{DIM}      如果还是空的，把 Unity Console 的红色报错截图发群里。{RESET}\n")
        sys.exit(0)
    else:
        print(f"{RED}结论：按上面的提示修，修完重跑本脚本。{RESET}\n")
        sys.exit(1)


if __name__ == "__main__":
    main()
