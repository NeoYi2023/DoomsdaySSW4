# 游戏运行问题修复指南

本文档提供针对每个常见问题的详细修复步骤。

## 问题分类

### 🔴 严重问题（阻止游戏运行）

#### 问题1: 场景中没有GameInitializer

**症状：**
- 游戏启动后没有任何反应
- Console中没有"游戏设置已初始化"日志

**修复步骤：**
1. 在Hierarchy窗口中，右键点击空白处
2. 选择 `Create Empty`
3. 命名为 `GameInitializer`
4. 选中GameInitializer
5. 在Inspector中点击 `Add Component`
6. 搜索 `GameInitializer` 并添加
7. 确保 `Initialize On Start` 复选框已勾选

**验证：**
- 运行游戏，Console应该显示初始化日志

---

#### 问题2: 场景中没有Canvas

**症状：**
- UI完全不显示
- Hierarchy中没有Canvas

**修复步骤：**
1. 在Hierarchy窗口中，右键点击空白处
2. 选择 `UI > Canvas`
3. Unity会自动创建Canvas和EventSystem
4. 选中Canvas，在Inspector中配置：
   - Render Mode: `Screen Space - Overlay`
   - 添加CanvasScaler组件（如果没有）：
     - UI Scale Mode: `Scale With Screen Size`
     - Reference Resolution: `X: 1920, Y: 1080`
     - Screen Match Mode: `Match Width Or Height`
     - Match: `0.5`

**验证：**
- Hierarchy中应该看到Canvas和EventSystem

---

#### 问题3: GameScreen组件引用未连接

**症状：**
- UI显示但内容为空
- Console可能有NullReferenceException

**修复步骤：**
1. 在Hierarchy中找到 `GameScreen` GameObject
2. 选中GameScreen
3. 在Inspector中找到 `GameScreen` 组件
4. 将Hierarchy中的UI元素拖拽到对应字段：
   - **Mining Map Container** → 拖入 `MiningMapContainer`（在LeftPanel下）
   - **Task Name Text** → 拖入 `TaskNameText`（在TaskInfoPanel下）
   - **Task Progress Text** → 拖入 `TaskProgressText`（在TaskInfoPanel下）
   - **Remaining Turns Text** → 拖入 `RemainingTurnsText`（在TaskInfoPanel下）
   - **Debt Info Text** → 拖入 `DebtInfoText`（在DebtInfoPanel下）
   - **Money Text** → 拖入 `MoneyText`（在ResourcePanel下）
   - **Energy Text** → 拖入 `EnergyText`（在ResourcePanel下）
   - **Drill Info Text** → 拖入 `DrillInfoText`（在DrillInfoPanel下）
   - **End Turn Button** → 拖入 `EndTurnButton`（在BottomPanel下）
   - **Settings Button** → 拖入 `SettingsButton`（在BottomPanel下）

**验证：**
- 所有字段都不应该显示 "None (Transform)" 或 "None (TextMeshProUGUI)"

---

#### 问题4: 配置文件缺失或格式错误

**症状：**
- Console显示配置加载错误
- 游戏初始化失败

**修复步骤：**

1. **检查文件是否存在**
   - 打开Project窗口
   - 导航到 `Assets/Resources/Configs/`
   - 确认以下文件存在：
     - TaskConfigs.json
     - OreConfigs.json
     - DrillConfigs.json
     - ShipConfigs.json
     - OreSpawnConfigs.json

2. **检查JSON格式**
   - 打开每个JSON文件
   - 使用在线JSON验证工具验证格式
   - 确保：
     - 所有字符串用双引号
     - 最后一个元素后没有逗号
     - 大括号和方括号匹配

3. **检查必需字段**
   - TaskConfigs.json: 必须包含至少一个任务，每个任务有 taskId, taskName, taskType, maxTurns, targetDebtAmount
   - DrillConfigs.json: 必须包含 "default_drill" 配置
   - ShipConfigs.json: 必须包含 "default_ship" 配置

**验证：**
- 运行游戏，Console应该显示配置加载成功

---

### 🟡 警告问题（影响功能完整性）

#### 问题5: UI结构不完整

**症状：**
- 某些UI元素不显示
- 功能不完整

**修复步骤：**

按照 `GAME_RUNTIME_CHECKLIST.md` 中的UI结构创建所有必需元素：

1. **创建GameScreen结构**
   ```
   Canvas
   └── GameScreen (GameObject + GameScreen组件)
       ├── LeftPanel (Panel)
       │   └── MiningMapContainer (GameObject + MiningMapView + GridLayoutGroup)
       ├── RightPanel (Panel)
       │   ├── TaskInfoPanel (Panel)
       │   │   ├── TaskNameText (TextMeshProUGUI)
       │   │   ├── TaskProgressText (TextMeshProUGUI)
       │   │   └── RemainingTurnsText (TextMeshProUGUI)
       │   ├── DebtInfoPanel (Panel)
       │   │   └── DebtInfoText (TextMeshProUGUI)
       │   ├── ResourcePanel (Panel)
       │   │   ├── MoneyText (TextMeshProUGUI)
       │   │   └── EnergyText (TextMeshProUGUI)
       │   └── DrillInfoPanel (Panel)
       │       └── DrillInfoText (TextMeshProUGUI)
       └── BottomPanel (Panel)
           ├── EndTurnButton (Button)
           └── SettingsButton (Button)
   ```

2. **创建UpgradeSelectionScreen结构**
   ```
   Canvas
   └── UpgradeSelectionScreen (GameObject + UpgradeSelectionScreen组件)
       └── Panel (Panel)
           ├── TitleText (TextMeshProUGUI)
           ├── Option1Button (Button)
           │   ├── Option1NameText (TextMeshProUGUI)
           │   └── Option1DescText (TextMeshProUGUI)
           ├── Option2Button (Button)
           │   ├── Option2NameText (TextMeshProUGUI)
           │   └── Option2DescText (TextMeshProUGUI)
           └── Option3Button (Button)
               ├── Option3NameText (TextMeshProUGUI)
               └── Option3DescText (TextMeshProUGUI)
   ```

**详细创建步骤：**
- 参考 `GAME_RUNTIME_CHECKLIST.md` 中的"UI结构完整性"部分

---

#### 问题6: MiningMapContainer缺少GridLayoutGroup

**症状：**
- 挖矿地图不显示或显示不正确

**修复步骤：**
1. 选中 `MiningMapContainer` GameObject
2. 在Inspector中点击 `Add Component`
3. 搜索 `Grid Layout Group` 并添加
4. 配置GridLayoutGroup：
   - Constraint: `Fixed Column Count`
   - Constraint Count: `9`
   - Cell Size: `X: 60, Y: 60`
   - Spacing: `X: 5, Y: 5`

**验证：**
- MiningMapView应该能正确显示9x7网格

---

#### 问题7: 中文字体资源缺失

**症状：**
- 中文文字显示为方框（□）

**修复步骤：**
1. **创建中文字体资源**
   - 参考 `UI_FONT_SETUP_GUIDE.md`
   - 或使用 `Tools > 设置中文字体` 工具

2. **自动加载（已实现）**
   - 代码已实现自动加载字体
   - 确保字体资源在 `Assets/TextMesh Pro/Resources/Fonts & Materials/` 目录
   - 命名为包含 "Chinese"、"YaHei" 或 "微软" 的名称

**验证：**
- 运行游戏，中文应该正常显示

---

#### 问题8: CanvasKeeper组件缺失

**症状：**
- Canvas在运行时被禁用
- UI突然消失

**修复步骤：**
1. 选中Canvas GameObject
2. 在Inspector中点击 `Add Component`
3. 搜索 `CanvasKeeper` 并添加

**验证：**
- 运行游戏，Canvas应该始终保持激活

---

### 🟢 优化问题（不影响基本运行）

#### 问题9: Build Settings未配置

**症状：**
- 构建游戏时可能有问题

**修复步骤：**
1. File > Build Settings
2. 点击 `Add Open Scenes` 将当前场景添加到列表
3. 将GameScene拖到列表最上方（设为启动场景）

**验证：**
- Build Settings中GameScene应该在列表最上方

---

#### 问题10: Canvas Scaler配置不正确

**症状：**
- UI在不同分辨率下显示不正确

**修复步骤：**
1. 选中Canvas
2. 在Inspector中找到CanvasScaler组件
3. 配置：
   - UI Scale Mode: `Scale With Screen Size`
   - Reference Resolution: `X: 1920, Y: 1080`
   - Screen Match Mode: `Match Width Or Height`
   - Match: `0.5`

**验证：**
- 在不同分辨率下测试UI显示

---

## 快速修复流程

### 如果游戏完全无法运行：

1. **检查GameInitializer**
   - 是否存在？
   - Initialize On Start是否勾选？

2. **检查Canvas**
   - 是否存在？
   - 是否激活？

3. **检查Console错误**
   - 打开Console窗口
   - 查看红色错误信息
   - 根据错误信息修复

### 如果UI不显示：

1. **检查Canvas**
   - 勾选框是否勾选？
   - CanvasKeeper是否添加？

2. **检查UI结构**
   - 使用 `Tools > 游戏运行检查` 工具
   - 或手动检查Hierarchy结构

3. **检查组件引用**
   - GameScreen组件的所有字段是否连接？

### 如果功能不完整：

1. **使用自动化检查工具**
   - `Tools > 游戏运行检查`
   - 查看报告的问题和警告

2. **逐项修复**
   - 按照本指南修复每个问题

3. **验证修复**
   - 运行游戏测试功能

## 使用自动化工具

### 运行检查工具

1. 在Unity菜单栏选择：`Tools > 游戏运行检查`
2. 点击 `开始检查` 按钮
3. 查看检查结果：
   - ✓ 绿色：通过
   - ⚠ 黄色：警告（不影响基本运行）
   - ❌ 红色：问题（需要修复）

### 根据报告修复

1. 优先修复 ❌ 红色问题
2. 然后处理 ⚠ 黄色警告
3. 验证所有检查项通过

## 验证修复

修复后，按以下步骤验证：

1. **编译检查**
   - 等待脚本编译完成
   - 确认没有编译错误

2. **运行测试**
   - 点击Play按钮
   - 观察Console日志
   - 检查UI显示

3. **功能测试**
   - 点击"结束回合"按钮
   - 观察游戏逻辑是否正常
   - 测试各个功能模块

4. **再次运行检查工具**
   - 确认所有问题已修复

## 获取帮助

如果按照本指南修复后仍有问题：

1. 查看Console中的具体错误信息
2. 使用 `Tools > 游戏运行检查` 工具获取详细报告
3. 参考 `GAME_RUNTIME_CHECKLIST.md` 进行完整检查
4. 检查 `DEMO_SETUP_GUIDE.md` 中的常见问题部分
