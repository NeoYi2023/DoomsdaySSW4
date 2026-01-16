# 游戏运行完整检查清单

本文档提供详细的检查步骤，确保游戏可以完整运行。

## 快速检查清单

### ✅ 场景基础设置（必须）

#### Canvas和EventSystem
- [ ] **Canvas GameObject存在**
  - 在Hierarchy中检查是否有Canvas
  - 如果没有：右键Hierarchy > `UI > Canvas`
  
- [ ] **Canvas组件配置**
  - 选中Canvas，在Inspector中检查：
  - Render Mode: `Screen Space - Overlay`
  - Canvas Scaler > UI Scale Mode: `Scale With Screen Size`
  - Reference Resolution: `1920 x 1080`
  - Screen Match Mode: `Match Width Or Height`
  - Match: `0.5`

- [ ] **EventSystem GameObject存在**
  - 在Hierarchy中检查是否有EventSystem
  - 如果没有：右键Hierarchy > `UI > Event System`

- [ ] **CanvasKeeper组件**
  - 选中Canvas
  - 在Inspector中点击 `Add Component`
  - 搜索并添加 `CanvasKeeper` 组件

#### GameInitializer
- [ ] **GameInitializer GameObject存在**
  - 在Hierarchy中检查是否有GameInitializer
  - 如果没有：右键Hierarchy > `Create Empty`，命名为 `GameInitializer`

- [ ] **GameInitializer组件已添加**
  - 选中GameInitializer
  - 在Inspector中点击 `Add Component`
  - 搜索并添加 `GameInitializer` 组件

- [ ] **Initialize On Start已勾选**
  - 在GameInitializer组件的Inspector中
  - 确保 `Initialize On Start` 复选框已勾选

### ✅ UI结构完整性（必须）

#### GameScreen结构
按照以下结构创建UI元素：

```
Canvas
└── GameScreen (GameObject)
    ├── LeftPanel (Panel)
    │   └── MiningMapContainer (GameObject)
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
        ├── EndTurnButton (Button - TextMeshPro)
        └── SettingsButton (Button - TextMeshPro)
```

**创建步骤：**

1. **GameScreen GameObject**
   - 在Canvas下右键 > `Create Empty`
   - 命名为 `GameScreen`
   - 添加组件：`GameScreen`

2. **LeftPanel**
   - 在GameScreen下右键 > `UI > Panel`
   - 命名为 `LeftPanel`

3. **MiningMapContainer**
   - 在LeftPanel下右键 > `Create Empty`
   - 命名为 `MiningMapContainer`
   - 添加组件：`MiningMapView`
   - 添加组件：`Grid Layout Group`
   - 配置GridLayoutGroup：
     - Constraint: `Fixed Column Count`
     - Constraint Count: `9`
     - Cell Size: `X: 60, Y: 60`
     - Spacing: `X: 5, Y: 5`

4. **RightPanel**
   - 在GameScreen下右键 > `UI > Panel`
   - 命名为 `RightPanel`

5. **TaskInfoPanel**
   - 在RightPanel下右键 > `UI > Panel`
   - 命名为 `TaskInfoPanel`
   - 在TaskInfoPanel下创建：
     - `UI > Text - TextMeshPro` → 命名为 `TaskNameText`
     - `UI > Text - TextMeshPro` → 命名为 `TaskProgressText`
     - `UI > Text - TextMeshPro` → 命名为 `RemainingTurnsText`

6. **DebtInfoPanel**
   - 在RightPanel下右键 > `UI > Panel`
   - 命名为 `DebtInfoPanel`
   - 在DebtInfoPanel下创建：
     - `UI > Text - TextMeshPro` → 命名为 `DebtInfoText`

7. **ResourcePanel**
   - 在RightPanel下右键 > `UI > Panel`
   - 命名为 `ResourcePanel`
   - 在ResourcePanel下创建：
     - `UI > Text - TextMeshPro` → 命名为 `MoneyText`
     - `UI > Text - TextMeshPro` → 命名为 `EnergyText`

8. **DrillInfoPanel**
   - 在RightPanel下右键 > `UI > Panel`
   - 命名为 `DrillInfoPanel`
   - 在DrillInfoPanel下创建：
     - `UI > Text - TextMeshPro` → 命名为 `DrillInfoText`

9. **BottomPanel**
   - 在GameScreen下右键 > `UI > Panel`
   - 命名为 `BottomPanel`
   - 在BottomPanel下创建：
     - `UI > Button - TextMeshPro` → 命名为 `EndTurnButton`
     - `UI > Button - TextMeshPro` → 命名为 `SettingsButton`

#### UpgradeSelectionScreen结构
```
Canvas
└── UpgradeSelectionScreen (GameObject)
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

**创建步骤：**

1. **UpgradeSelectionScreen GameObject**
   - 在Canvas下右键 > `Create Empty`
   - 命名为 `UpgradeSelectionScreen`
   - 添加组件：`UpgradeSelectionScreen`

2. **Panel**
   - 在UpgradeSelectionScreen下右键 > `UI > Panel`
   - 命名为 `Panel`

3. **TitleText**
   - 在Panel下右键 > `UI > Text - TextMeshPro`
   - 命名为 `TitleText`

4. **三个选项按钮**
   - 在Panel下创建三个按钮：
     - `UI > Button - TextMeshPro` → 命名为 `Option1Button`
     - `UI > Button - TextMeshPro` → 命名为 `Option2Button`
     - `UI > Button - TextMeshPro` → 命名为 `Option3Button`
   
   每个按钮下创建两个文本：
   - `UI > Text - TextMeshPro` → 命名为 `Option1NameText`（在Option1Button下）
   - `UI > Text - TextMeshPro` → 命名为 `Option1DescText`（在Option1Button下）
   - （同样为Option2和Option3创建）

### ✅ UI组件引用连接（必须）

#### GameScreen组件引用
1. 选中 `GameScreen` GameObject
2. 在Inspector中找到 `GameScreen` 组件
3. 将以下UI元素拖拽到对应字段：
   - **Mining Map Container** → 拖入 `MiningMapContainer`
   - **Task Name Text** → 拖入 `TaskNameText`
   - **Task Progress Text** → 拖入 `TaskProgressText`
   - **Remaining Turns Text** → 拖入 `RemainingTurnsText`
   - **Debt Info Text** → 拖入 `DebtInfoText`
   - **Money Text** → 拖入 `MoneyText`
   - **Energy Text** → 拖入 `EnergyText`
   - **Drill Info Text** → 拖入 `DrillInfoText`
   - **End Turn Button** → 拖入 `EndTurnButton`
   - **Settings Button** → 拖入 `SettingsButton`

#### UpgradeSelectionScreen组件引用
1. 选中 `UpgradeSelectionScreen` GameObject
2. 在Inspector中找到 `UpgradeSelectionScreen` 组件
3. 将以下UI元素拖拽到对应字段：
   - **Panel** → 拖入 `Panel`
   - **Title Text** → 拖入 `TitleText`
   - **Option Buttons** → 展开数组，分别拖入 `Option1Button`, `Option2Button`, `Option3Button`
   - **Option Name Texts** → 展开数组，分别拖入 `Option1NameText`, `Option2NameText`, `Option3NameText`
   - **Option Description Texts** → 展开数组，分别拖入 `Option1DescText`, `Option2DescText`, `Option3DescText`

### ✅ 配置文件检查（必须）

#### 配置文件存在性
检查以下文件是否存在：
- [ ] `Assets/Resources/Configs/TaskConfigs.json`
- [ ] `Assets/Resources/Configs/OreConfigs.json`
- [ ] `Assets/Resources/Configs/DrillConfigs.json`
- [ ] `Assets/Resources/Configs/ShipConfigs.json`
- [ ] `Assets/Resources/Configs/OreSpawnConfigs.json`

#### 配置文件内容验证
打开每个JSON文件，检查：
- [ ] TaskConfigs.json 包含至少一个任务（taskId, taskName等字段）
- [ ] OreConfigs.json 包含至少一种矿石类型
- [ ] DrillConfigs.json 包含 "default_drill" 配置
- [ ] ShipConfigs.json 包含 "default_ship" 配置
- [ ] OreSpawnConfigs.json 包含至少一层配置

### ✅ 字体资源检查（推荐）

- [ ] **中文字体资源已创建**
  - 在Project窗口检查：`Assets/TextMesh Pro/Resources/Fonts & Materials/`
  - 应该有中文字体资源文件（.asset）

- [ ] **字体资源包含中文字符**
  - 选中字体资源
  - 在Inspector中检查 Character Count 是否 > 0

- [ ] **字体已分配或自动加载**
  - 代码中已实现自动加载（GameScreen.TrySetChineseFont）
  - 或手动为每个TextMeshProUGUI分配字体

### ✅ Build Settings（必须）

- [ ] **GameScene已添加到Build Settings**
  1. File > Build Settings
  2. 点击 `Add Open Scenes` 或拖拽GameScene到列表

- [ ] **GameScene设置为启动场景**
  - 在Build Settings中，将GameScene拖到列表最上方

## 验证步骤

### 步骤1: 编译检查
1. 打开Unity编辑器
2. 等待脚本编译完成（查看右下角进度条）
3. 打开Console窗口（`Window > General > Console`）
4. 检查是否有编译错误（红色）
5. 修复所有编译错误

### 步骤2: 场景检查
1. 打开GameScene
2. 在Hierarchy中检查UI结构
3. 确认所有必需GameObject存在
4. 检查组件引用连接（选中GameScreen，查看Inspector）

### 步骤3: 运行测试
1. 点击Play按钮
2. 观察Console日志：
   - 应该看到 "游戏设置已初始化"
   - 应该看到 "开始新游戏..."
   - 应该看到 "游戏初始化完成"
3. 检查UI是否显示：
   - 挖矿地图（9x7网格）
   - 任务信息
   - 资源信息
   - 按钮
4. 测试核心功能：
   - 点击"结束回合"按钮
   - 观察矿石硬度是否减少
   - 观察金钱是否增加

### 步骤4: 功能验证
按照以下清单逐项测试：

- [ ] 游戏启动后，挖矿地图正确显示（9x7网格）
- [ ] 不同矿石显示不同颜色
- [ ] 矿石硬度数字正确显示
- [ ] 点击"结束回合"后，矿石被攻击
- [ ] 矿石硬度归零后，被挖掉并显示为深灰色
- [ ] 挖掉的矿石转化为金钱，债务减少
- [ ] 能源矿石累计能源值
- [ ] 能源达到阈值时，显示三选一升级界面
- [ ] 选择升级后，钻头属性提升
- [ ] 任务进度正确更新
- [ ] 完成任务后，自动领取下一个任务
- [ ] 回合数限制正确工作

## 常见问题快速修复

### 问题1: 游戏启动后没有反应
**检查：**
1. GameInitializer GameObject是否存在？
2. Initialize On Start是否勾选？
3. Console是否有错误？

**修复：**
- 创建GameInitializer GameObject并添加组件
- 勾选 Initialize On Start
- 查看Console错误并修复

### 问题2: UI不显示
**检查：**
1. Canvas是否激活（勾选框）？
2. UI元素是否在Hierarchy中？
3. GameScreen组件引用是否连接？

**修复：**
- 确保Canvas勾选框已勾选
- 检查Hierarchy中UI结构
- 连接GameScreen组件的所有引用

### 问题3: 地图不显示
**检查：**
1. MiningMapContainer是否连接？
2. MiningMapView组件是否存在？
3. GridLayoutGroup是否配置？

**修复：**
- 连接GameScreen.miningMapContainer
- 添加MiningMapView组件
- 配置GridLayoutGroup（9列，60x60）

### 问题4: 点击按钮无反应
**检查：**
1. 按钮引用是否连接？
2. Console是否有错误？
3. GameManager是否初始化？

**修复：**
- 连接endTurnButton到GameScreen
- 查看Console错误
- 检查GameInitializer是否正常工作

### 问题5: 配置加载失败
**检查：**
1. 配置文件路径是否正确？
2. JSON格式是否正确？
3. Console是否有配置错误？

**修复：**
- 确认文件在 `Assets/Resources/Configs/` 目录
- 检查JSON格式（使用JSON验证工具）
- 查看Console中的具体错误信息

## 使用自动化检查工具

可以使用 `Tools > 游戏运行检查` 工具自动检查大部分配置项。

详见：`GAME_RUNTIME_CHECKER.md`
