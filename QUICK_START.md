# 快速开始指南

## 5分钟快速集成

### 1. 打开Unity项目
- 如果已有Unity项目，直接打开
- 如果没有，在Unity Hub中创建新项目（Unity 2021.3.40f1），项目位置选择当前文件夹

### 2. 安装TextMeshPro（必须）
在Unity编辑器中：
```
Window > TextMeshPro > Import TMP Essential Resources
```
点击"Import"按钮

### 3. 使用自动化工具
在Unity编辑器中：
```
Tools > DoomsdaySSW4 > Setup Project
```
这会检查所有必要的文件和设置。

### 4. 创建场景（二选一）

**方法A：使用自动化工具**
```
Tools > DoomsdaySSW4 > Create Settings UI
```
这会自动创建MainMenu场景和设置UI结构。

**方法B：手动创建**
按照 `UNITY_SETUP_GUIDE.md` 中的详细步骤手动创建。

### 5. 连接UI组件
1. 在Hierarchy中选择SettingsPanel
2. 在Inspector中找到SettingsScreen组件
3. 将UI组件拖拽到对应字段：
   - ResolutionDropdown → Resolution Dropdown
   - ResolutionModeDropdown → Resolution Mode Dropdown
   - WidthInputField → Width Input Field
   - HeightInputField → Height Input Field
   - FullscreenToggle → Fullscreen Toggle
   - LanguageDropdown → Language Dropdown
   - ApplyButton → Apply Button
   - CancelButton → Cancel Button

### 6. 测试
1. 点击Play按钮
2. 测试分辨率切换和语言切换功能

## 文件检查清单

确保以下文件存在：
- [x] Assets/Scripts/Config/SettingsManager.cs
- [x] Assets/Scripts/Core/GameInitializer.cs
- [x] Assets/Scripts/UI/LocalizationManager.cs
- [x] Assets/Scripts/UI/Components/LocalizedText.cs
- [x] Assets/Scripts/UI/Screens/SettingsScreen.cs
- [x] Assets/Resources/Localization/zh-CN.json
- [x] Assets/Resources/Localization/zh-TW.json
- [x] Assets/Resources/Localization/en-US.json

## 常见问题快速解决

**编译错误：找不到TMPro**
→ 安装TextMeshPro（步骤2）

**语言资源加载失败**
→ 检查Resources/Localization文件夹和JSON文件是否存在

**UI组件不工作**
→ 检查SettingsScreen组件的引用是否正确连接
