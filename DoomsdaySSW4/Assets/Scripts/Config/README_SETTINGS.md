# 分辨率与语言设置功能使用说明

## 概述

本系统提供了完整的游戏分辨率设置和UI多语言支持功能。

## 功能特性

### 分辨率设置
- 支持预设分辨率选项（1920x1080, 1680x1050, 1600x900等）
- 支持自定义分辨率输入
- 支持全屏/窗口模式切换
- 自动检测系统支持的分辨率
- 设置自动保存和加载

### 语言设置
- 支持简体中文（zh-CN）
- 支持繁体中文（zh-TW）
- 支持英文（en-US）
- 运行时动态切换语言
- 语言设置自动保存和加载

## 使用方法

### 1. 初始化系统

在游戏启动时，系统会自动初始化。也可以手动初始化：

```csharp
// 初始化设置管理器（会自动加载保存的设置）
SettingsManager.Instance.LoadResolutionSettings();

// 初始化本地化管理器（会自动加载保存的语言设置）
LocalizationManager.Instance.SetLanguage("zh-CN");
```

### 2. 使用设置管理器

#### 设置分辨率
```csharp
SettingsManager.Instance.SetResolution(1920, 1080, true);
```

#### 获取可用分辨率
```csharp
Resolution[] resolutions = SettingsManager.Instance.GetAvailableResolutions();
```

#### 切换全屏模式
```csharp
SettingsManager.Instance.ToggleFullscreen();
```

### 3. 使用本地化管理器

#### 设置语言
```csharp
LocalizationManager.Instance.SetLanguage("zh-CN");
```

#### 获取本地化文本
```csharp
string text = LocalizationManager.Instance.GetLocalizedString("ui.menu.start");
```

#### 获取格式化文本
```csharp
string text = LocalizationManager.Instance.GetLocalizedString("ui.message.score", 1000);
```

### 4. 在UI中使用本地化文本

#### 方法1：使用LocalizedText组件
1. 在Unity编辑器中，选择需要本地化的Text或TextMeshProUGUI组件所在的GameObject
2. 添加`LocalizedText`组件
3. 在Inspector中设置`Localization Key`（如：`ui.menu.start`）
4. 文本会自动根据当前语言更新

#### 方法2：在代码中获取
```csharp
Text textComponent = GetComponent<Text>();
textComponent.text = LocalizationManager.Instance.GetLocalizedString("ui.menu.start");
```

### 5. 使用设置界面

1. 在场景中创建设置界面GameObject
2. 添加`SettingsScreen`组件
3. 在Inspector中配置以下UI组件：
   - `Resolution Dropdown`: 分辨率下拉菜单
   - `Resolution Mode Dropdown`: 分辨率模式下拉菜单（预设/自定义）
   - `Width Input Field`: 宽度输入框（自定义模式）
   - `Height Input Field`: 高度输入框（自定义模式）
   - `Fullscreen Toggle`: 全屏切换开关
   - `Language Dropdown`: 语言下拉菜单
   - `Apply Button`: 应用按钮
   - `Cancel Button`: 取消按钮

## 文件结构

```
Assets/
├── Scripts/
│   ├── Config/
│   │   └── SettingsManager.cs          # 分辨率设置管理器
│   ├── UI/
│   │   ├── LocalizationManager.cs      # 本地化管理器
│   │   ├── Screens/
│   │   │   └── SettingsScreen.cs       # 设置界面
│   │   └── Components/
│   │       └── LocalizedText.cs       # 本地化文本组件
│   ├── Data/
│   │   └── Models/
│   │       └── LocalizationData.cs     # 本地化数据结构
│   └── Core/
│       └── GameInitializer.cs          # 游戏初始化器
└── Resources/
    └── Localization/
        ├── zh-CN.json                  # 简体中文资源
        ├── zh-TW.json                  # 繁体中文资源
        └── en-US.json                  # 英文资源
```

## 添加新的本地化文本

1. 编辑对应的语言资源文件（如`zh-CN.json`）
2. 在`entries`数组中添加新的条目：
```json
{
  "key": "ui.new.key",
  "value": "新文本"
}
```
3. 在其他语言文件中添加对应的翻译
4. 在代码中使用：
```csharp
string text = LocalizationManager.Instance.GetLocalizedString("ui.new.key");
```

## 注意事项

1. **分辨率验证**：系统会自动验证分辨率是否在系统支持范围内
2. **语言资源加载**：语言资源文件必须放在`Assets/Resources/Localization/`目录下
3. **设置持久化**：设置会自动保存到PlayerPrefs，游戏重启后自动加载
4. **语言切换**：切换语言后，所有使用`LocalizedText`组件的文本会自动更新
5. **单例模式**：`SettingsManager`和`LocalizationManager`都是单例，使用`Instance`属性访问

## 扩展功能

### 添加新的预设分辨率

在`SettingsManager.cs`中的`_presetResolutions`列表中添加：

```csharp
private readonly List<Resolution> _presetResolutions = new List<Resolution>
{
    // ... 现有分辨率
    new Resolution { width = 2560, height = 1440 }, // 添加新分辨率
};
```

### 添加新的语言支持

1. 在`LocalizationManager.cs`的`_supportedLanguages`列表中添加语言代码
2. 创建对应的语言资源文件（如`ja-JP.json`）
3. 在设置界面的语言下拉菜单中会自动显示新语言
