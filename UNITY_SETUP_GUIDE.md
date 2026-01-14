# Unity项目集成指南

## 快速开始

### 步骤1：创建Unity项目

1. 打开Unity Hub
2. 点击"New Project"
3. 选择Unity版本：**2021.3.40f1**
4. 选择模板：**2D**（或3D，根据游戏需求）
5. 项目名称：**DoomsdaySSW4**
6. 项目位置：**选择当前文件夹** (`e:\Work\Cursor\DoomsdaySSW4\`)
   - 或者创建新文件夹，然后将Assets文件夹内容复制过去

### 步骤2：验证文件结构

确保Unity项目打开后，在Project窗口中能看到以下结构：
```
Assets/
├── Scripts/
│   ├── Config/
│   ├── Core/
│   ├── Data/
│   └── UI/
├── Resources/
│   └── Localization/
└── Scenes/
```

### 步骤3：安装TextMeshPro

1. 在Unity编辑器中，打开菜单：`Window > TextMeshPro > Import TMP Essential Resources`
2. 在弹出的对话框中点击"Import"
3. （可选）导入示例：`Window > TextMeshPro > Import TMP Examples & Extras`

### 步骤4：配置项目设置

1. 打开 `Edit > Project Settings > Player`
2. 设置以下内容：
   - **Company Name**: 你的公司名称
   - **Product Name**: DoomsdaySSW4
   - **Version**: 1.0.0

3. 在 `Resolution and Presentation` 中：
   - 设置默认分辨率
   - 配置全屏模式

### 步骤5：创建场景

按照下面的"场景创建指南"创建MainMenu场景。

## 场景创建指南

### 创建MainMenu场景

1. 在Project窗口中，右键点击`Assets/Scenes`文件夹
2. 选择 `Create > Scene`
3. 命名为 `MainMenu`
4. 双击打开场景

### 设置场景

1. **创建Canvas**
   - 右键Hierarchy > `UI > Canvas`
   - 选中Canvas，在Inspector中：
     - Canvas Scaler > UI Scale Mode: `Scale With Screen Size`
     - Reference Resolution: `1920 x 1080`
     - Screen Match Mode: `Match Width Or Height`
     - Match: `0.5`

2. **创建EventSystem**（如果不存在）
   - Unity通常会自动创建，如果没有：
   - 右键Hierarchy > `UI > Event System`

3. **创建设置面板**
   - 在Canvas下右键 > `UI > Panel`
   - 命名为 `SettingsPanel`
   - 添加组件：`SettingsScreen`（在Inspector中点击Add Component，搜索SettingsScreen）

4. **创建分辨率设置UI**
   - 在SettingsPanel下创建：
     - `UI > Dropdown - TextMeshPro` → 命名为 `ResolutionDropdown`
     - `UI > Dropdown - TextMeshPro` → 命名为 `ResolutionModeDropdown`
     - `UI > Input Field - TextMeshPro` → 命名为 `WidthInputField`
     - `UI > Input Field - TextMeshPro` → 命名为 `HeightInputField`
     - `UI > Toggle` → 命名为 `FullscreenToggle`

5. **创建语言设置UI**
   - `UI > Dropdown - TextMeshPro` → 命名为 `LanguageDropdown`

6. **创建按钮**
   - `UI > Button - TextMeshPro` → 命名为 `ApplyButton`
   - `UI > Button - TextMeshPro` → 命名为 `CancelButton`

7. **连接UI组件到SettingsScreen**
   - 选中SettingsPanel
   - 在Inspector中找到SettingsScreen组件
   - 将创建的UI组件拖拽到对应的字段：
     - Resolution Dropdown → ResolutionDropdown
     - Resolution Mode Dropdown → ResolutionModeDropdown
     - Width Input Field → WidthInputField
     - Height Input Field → HeightInputField
     - Fullscreen Toggle → FullscreenToggle
     - Language Dropdown → LanguageDropdown
     - Apply Button → ApplyButton
     - Cancel Button → CancelButton

8. **创建GameInitializer**
   - 右键Hierarchy > `Create Empty`
   - 命名为 `GameInitializer`
   - 添加组件：`GameInitializer`

9. **保存场景**
   - `Ctrl+S` 或 `File > Save`

## 测试步骤

### 1. 编译检查
- 打开Unity编辑器后，等待脚本编译完成
- 查看Console窗口（`Window > General > Console`）
- 如果有错误，按照错误信息修复

### 2. 运行测试
1. 点击Play按钮
2. 在Game视图中，应该能看到设置界面
3. 测试功能：
   - 切换分辨率
   - 切换全屏模式
   - 切换语言
   - 点击应用按钮

## 常见问题

### Q1: TextMeshPro未找到错误
**解决**：安装TextMeshPro包（见步骤3）

### Q2: Resources文件夹找不到
**解决**：确保文件夹名称是`Resources`（大小写敏感），位于Assets根目录下

### Q3: 单例未初始化
**解决**：确保场景中有GameInitializer对象，或检查GameInitializer.cs中的RuntimeInitializeOnLoadMethod

### Q4: 语言资源加载失败
**解决**：
- 检查JSON文件格式是否正确
- 确保文件在`Assets/Resources/Localization/`目录下
- 检查文件名称大小写：`zh-CN.json`, `zh-TW.json`, `en-US.json`

### Q5: UI组件连接后不工作
**解决**：
- 确保所有UI组件都在Canvas下
- 检查SettingsScreen组件是否正确添加到SettingsPanel
- 检查UI组件的引用是否正确

## 下一步

完成基础设置后，可以：
1. 创建主菜单UI（开始游戏、设置、退出按钮）
2. 创建游戏主场景
3. 添加更多本地化文本
4. 实现游戏核心功能
