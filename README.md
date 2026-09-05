# 梦境拾荒者的旅程

腾讯 GameJam 2D 平台跳跃。从主菜单走进梦境引导，抽四张山海经塔罗，带着牌面效果跑完 A/B 关卡，最后进入回响空间。

Unity `6000.5.0f1` · URP · Input System · Cinemachine

## 画面

### 主菜单

![主菜单](docs/screenshots/01_menu.png)

### 梦境引导

![梦境空间](docs/screenshots/02_dream.png)

![写下梦境](docs/screenshots/03_write.png)

### 塔罗抽牌

22 张牌背扇形摊开，点 4 张对应关卡节点 T1–T4，正逆位独立随机。

![塔罗抽牌](docs/screenshots/04_tarot.png)

### 关卡

白盒关卡按施工图生成：场景 A → 传送门 → 场景 B → END。

![Gameplay](docs/screenshots/05_gameplay.png)

### 回响空间

通关后淡入独立梦境回响，收束这一局旅程。

![回响空间](docs/screenshots/06_echo.png)

## 怎么玩

| 操作 | 键位 |
| --- | --- |
| 移动 | A / D 或 方向键 |
| 跳跃 | Space / W |
| 交互（塔罗节点、传送门等） | E / F |
| 攻击 | J / 鼠标左键 |
| 暂停 | Esc |

流程：开始游戏 → 引导写梦 → 抽 4 张塔罗 → 跑关（T1 T2 → 下一区域 → T3 T4 → END）→ 回响空间。

## 打开工程

1. 用 Unity Hub 打开本仓库，编辑器版本 **6000.5.0f1**
2. 从 `Boot` 场景进入 Play Mode（编辑器会自动先加载 Boot）
3. 主菜单点「开始游戏」
