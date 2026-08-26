# 素材许可清单

每导入一个第三方素材，在此加一行。**这是课程评分的加分项，也是避免版权问题的唯一凭证。**

| 素材名 | 用途 | 来源 | 协议 | 导入日期 |
|--------|------|------|------|---------|
| ~~Starter Assets - FirstPerson (URP)~~ | ~~第一人称控制器基座~~ | [Unity Asset Store](https://assetstore.unity.com/packages/essentials/starter-assets-firstperson-updates-in-new-charactercontroller-pa-196525) | Unity Asset Store Free | **已弃用，未导入**（2026/08/21 决定，T01 从零实现） |
| Apartment Kit v4.2 | 地图主素材库（改造为中式老式居民楼） | [Unity Asset Store](https://assetstore.unity.com/packages/3d/environments/apartment-kit-124055) | Unity Asset Store Free | 待导入 |
| | | | | |

---

## 常用来源与协议速查

| 来源 | 协议 | 注意事项 |
|------|------|---------|
| [Poly Haven](https://polyhaven.com/) | **CC0** | 完全免费商用，无需署名，无任何限制。**最推荐** |
| [ambientCG](https://ambientcg.com/) | **CC0** | 同上 |
| [Mixamo](https://www.mixamo.com/) | Adobe 免费 | 可用于商业项目，不得转售模型本身 |
| [Freesound](https://freesound.org/) | 逐条不同 | **必须逐个音效确认**：CC0 可随意用；CC-BY 需署名；CC-NC 禁止商用 |
| [Sonniss GDC Bundle](https://sonniss.com/gameaudiogdc) | 免版税商用 | 可用于商业项目 |
| Unity Asset Store 免费区 | Asset Store EULA | 可用于游戏项目，**不得二次分发原始素材文件** |

## Apartment Kit 使用约定

- **不入库**：325.9 MB，走 LFS 会在数日内耗尽 GitHub 免费额度。三名成员各自从
  Package Manager「My Assets」导入同一版本（v4.2），资源 GUID 一致，引用不会断。
  仓库中需忽略其导入目录。
- **必须转 URP**：商店页标注 URP 不兼容（仅 Built-in）。导入后立即执行
  Edit → Rendering → Materials → Convert Selected Built-in Materials to URP，
  否则全场粉红。
- **碰撞体以灰盒为准**：套件模型作为子物体挂在 `BLK_` 灰盒下，其自带 Collider
  一律删除。碰撞与 NavMesh 只认灰盒那一套。

---

## 红线

- 不得把第三方素材的原始文件单独打包分发（游戏 Build 内嵌是允许的）
- 不得使用 CC-NC（非商业）素材于任何可能商用的场景
- 不得使用来源不明的素材
- 拿不准时，换一个 CC0 的替代品，不要赌
