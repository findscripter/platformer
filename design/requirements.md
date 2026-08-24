# 游戏需求整理

## 项目背景
- **GameJam 项目**：腾讯 GameJam
- **当前状态**：已有 Unity 6 (6000.5.0f1) 2D 平台游戏基础框架
- **美术资源**：已提供 Figma 设计稿和美术素材

## 美术资源清单

### Figma 设计稿
- URL: `https://www.figma.com/design/CNF4mNVHgV96ZH92QMurPZ/...`
- 内容：场景美术、角色设计、UI元件

### 已提供素材（E:\腾讯gamejam\美术资源）
1. **元件 pt.1.zip**（已解压）
   - `dialogues.png` - 对话框元件
   - `DR 元件.png` - DR相关元件
   - `Mist FX.png` / `Mist FX2.png` - 雾气特效
   - `Wave FX.png` - 波浪特效
   - `场景参考.png` - 场景参考图

2. **关键帧.rar**（待解压）
   - 角色动画关键帧资源

## 技术要求

### 美术集成流程
根据对话记录，需要：

1. **Figma → Unity 流程**
   - 从 Figma 导出 PNG 切图（一张透明 PNG 对应一个元件）
   - 导入 Unity 后转换为 Sprite
   - 启用卡片设计平台，拖拽进场景

2. **AI 辅助需求**
   - 使用 AI 分类场景/UI，自动归类占位
   - 场景美术素材直接铺放到场景
   - 关卡搭建后期摆放细节流程

3. **素材规范**
   - 格式：透明 PNG
   - 命名：按 Figma 图层命名
   - 组织：场景素材 vs UI 元件分开

## 当前游戏架构（已有代码）

### 核心系统
- ✅ **状态机**：GameStateMachine（MainMenu, Playing, Paused, Dialogue 等）
- ✅ **对话系统**：节点式对话（TextNode, ChoiceNode, EventNode）
- ✅ **交互框架**：IInteractable（NPC, 门, 开关, 场景切换）
- ✅ **存档系统**：JSON 存档（场景、对话进度、收集品）
- ✅ **事件系统**：EventCenter 类型化事件总线

### 已有功能
- ✅ 玩家控制器（Rigidbody2D + 自定义地面检测 + 跳跃缓冲）
- ✅ 动画状态控制器（锁定移动的动画）
- ✅ 收集品追踪系统
- ✅ 场景过渡管理器
- ✅ 关卡目标/死亡机制

## 待整合需求

### 阶段 1：美术资源导入
- [ ] 解压并整理关键帧动画资源
- [ ] 从 Figma 导出完整素材集
- [ ] 按场景/UI 分类组织素材
- [ ] 导入 Unity 并配置 Sprite 设置

### 阶段 2：场景美术替换
- [ ] 替换占位符场景元素为美术资源
- [ ] 搭建视觉层次（背景/中景/前景）
- [ ] 添加特效层（雾气/波浪等）
- [ ] 调整 Cinemachine 相机边界

### 阶段 3：角色动画集成
- [ ] 配置关键帧 Sprite Sheet
- [ ] 创建 Animator Controller 动画状态机
- [ ] 替换现有 PlayerAnimationStateController 逻辑
- [ ] 测试动画过渡和触发

### 阶段 4：UI 美化
- [ ] 替换对话框 UI（使用 dialogues.png）
- [ ] 设计主菜单界面
- [ ] 设计暂停菜单
- [ ] 添加 HUD 元素

## 技术栈（已配置）
- **引擎**: Unity 6000.5.0f1
- **语言**: C#
- **渲染**: URP 17.5.0
- **物理**: Physics2D
- **输入**: Unity Input System 1.19.0
- **相机**: Cinemachine 3.1.6

## 下一步行动
1. 解压 `关键帧.rar` 查看动画资源
2. 访问 Figma 设计稿导出完整素材集
3. 创建美术资源导入计划（Sprite 配置、Atlas 打包）
4. 制定场景美术替换优先级
