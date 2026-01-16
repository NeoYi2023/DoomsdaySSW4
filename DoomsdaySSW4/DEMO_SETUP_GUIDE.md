# Demo设置指南

## 快速开始

### 1. 在Unity中打开项目
- 打开Unity Hub
- 打开项目：`E:\Work\Cursor\DoomsdaySSW4\DoomsdaySSW4`

### 2. 创建游戏场景

1. 在Project窗口中，右键点击 `Assets/Scenes` 文件夹
2. 选择 `Create > Scene`
3. 命名为 `GameScene`
4. 双击打开场景

### 3. 设置场景UI结构

#### 3.1 创建Canvas
1. 右键Hierarchy > `UI > Canvas`
2. 选中Canvas，在Inspector中设置：
   - Canvas Scaler > UI Scale Mode: `Scale With Screen Size`
   - Reference Resolution: `1920 x 1080`
   - Screen Match Mode: `Match Width Or Height`
   - Match: `0.5`

#### 3.2 创建EventSystem（如果不存在）
- Unity通常会自动创建，如果没有：
- 右键Hierarchy > `UI > Event System`

#### 3.3 创建游戏主界面结构

在Canvas下创建以下结构：

```
Canvas
├── GameScreen (GameObject，添加GameScreen组件)
│   ├── LeftPanel (Panel)
│   │   └── MiningMapContainer (GameObject，添加MiningMapView组件)
│   │       └── (MiningMapView会自动创建9x9网格)
│   ├── RightPanel (Panel)
│   │   ├── TaskInfoPanel (Panel)
│   │   │   ├── TaskNameText (TextMeshProUGUI)
│   │   │   ├── TaskProgressText (TextMeshProUGUI)
│   │   │   └── RemainingTurnsText (TextMeshProUGUI)
│   │   ├── DebtInfoPanel (Panel)
│   │   │   └── DebtInfoText (TextMeshProUGUI)
│   │   ├── ResourcePanel (Panel)
│   │   │   ├── MoneyText (TextMeshProUGUI)
│   │   │   └── EnergyText (TextMeshProUGUI)
│   │   └── DrillInfoPanel (Panel)
│   │       └── DrillInfoText (TextMeshProUGUI)
│   └── BottomPanel (Panel)
│       ├── EndTurnButton (Button - TextMeshPro)
│       └── SettingsButton (Button - TextMeshPro)
└── UpgradeSelectionScreen (GameObject，添加UpgradeSelectionScreen组件)
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

### 4. 连接UI组件

#### 4.1 GameScreen组件
选中GameScreen GameObject，在Inspector中找到GameScreen组件，将UI组件拖拽到对应字段：
- Mining Map Container → MiningMapContainer
- Task Name Text → TaskNameText
- Task Progress Text → TaskProgressText
- Remaining Turns Text → RemainingTurnsText
- Debt Info Text → DebtInfoText
- Money Text → MoneyText
- Energy Text → EnergyText
- Drill Info Text → DrillInfoText
- End Turn Button → EndTurnButton
- Settings Button → SettingsButton

#### 4.2 MiningMapView组件
选中MiningMapContainer，在Inspector中：

1. **设置RectTransform填充父容器**：
   - 在Rect Transform中，点击左上角的锚点图标
   - 选择"stretch-stretch"（四个角拉伸）
   - 或手动设置：Left=0, Right=0, Top=0, Bottom=0
   - 这样MiningMapContainer会填充整个LeftPanel

2. **配置MiningMapView组件**：
   - 找到MiningMapView组件
   - Auto Resize: 勾选（启用自适应大小）
   - Use Parent Size: 勾选（使用LeftPanel的大小）
   - Spacing: X=5, Y=5（格子间距）
   - Grid Layout会自动配置（9列，格子大小会根据LeftPanel自动计算）

#### 4.3 UpgradeSelectionScreen组件
选中UpgradeSelectionScreen GameObject，在Inspector中找到UpgradeSelectionScreen组件，连接：
- Panel → Panel
- Title Text → TitleText
- Option Buttons → 三个按钮数组
- Option Name Texts → 三个名称文本数组
- Option Description Texts → 三个描述文本数组

### 5. 添加GameInitializer

1. 在场景中创建空GameObject，命名为 `GameInitializer`
2. 添加 `GameInitializer` 组件
3. 确保 `Initialize On Start` 勾选

### 6. 设置场景为启动场景

1. File > Build Settings
2. 将GameScene添加到Scenes
3. 将GameScene拖到最上面（设为启动场景）

### 7. 运行测试

1. 点击Play按钮
2. 游戏应该自动初始化并显示挖矿地图
3. 点击"结束回合"按钮，观察：
   - 矿石被攻击（硬度减少）
   - 矿石被挖掉后，金钱增加
   - 债务减少
   - 回合数增加
   - 任务进度更新

## 功能验证清单

- [ ] 游戏启动后，挖矿地图正确显示（9x9网格）
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
- [ ] 任务失败时显示失败提示
- [ ] 完成所有任务后显示胜利提示

## 常见问题

### Q: 地图不显示
**A:** 检查：
- MiningMapContainer是否有GridLayoutGroup组件
- GameScreen组件的Mining Map Container字段是否正确连接
- MiningManager是否正确初始化

### Q: 点击结束回合没有反应
**A:** 检查：
- EndTurnButton是否正确连接到GameScreen组件
- GameManager是否正确初始化
- Console是否有错误信息

### Q: 升级界面不显示
**A:** 检查：
- UpgradeSelectionScreen的Panel是否正确连接
- EnergyUpgradeManager是否正确累计能源
- 能源阈值是否正确设置（默认50, 100, 150）

### Q: 矿石不生成
**A:** 检查：
- ConfigManager是否正确加载配置
- OreSpawnConfigs.json格式是否正确
- Console是否有配置加载错误

## 下一步优化

Demo运行正常后，可以考虑：
1. 添加挖矿动画效果
2. 优化UI布局和视觉效果
3. 添加音效
4. 实现存档系统
5. 添加更多任务和矿石类型
