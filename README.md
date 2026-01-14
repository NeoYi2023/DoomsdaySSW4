# DoomsdaySSW4 Unity项目

## 项目简介

DoomsdaySSW4是一款回合制挖矿策略游戏，使用Unity 2021.3.40f1开发。

## 快速开始

### 1. 环境要求
- Unity 2021.3.40f1
- TextMeshPro包（会自动提示安装）

### 2. 项目设置
1. 打开Unity Hub
2. 添加项目或创建新项目（选择当前文件夹）
3. 打开项目后，Unity会自动检测并提示安装TextMeshPro

### 3. 快速集成
查看 [QUICK_START.md](QUICK_START.md) 获取5分钟快速集成指南。

## 项目结构

```
DoomsdaySSW4/
├── Assets/
│   ├── Scripts/              # 游戏脚本
│   │   ├── Config/           # 配置管理
│   │   ├── Core/             # 核心系统
│   │   ├── Data/             # 数据结构
│   │   └── UI/               # UI系统
│   ├── Resources/            # 资源文件
│   │   └── Localization/     # 本地化资源
│   ├── Scenes/               # 游戏场景
│   └── Editor/               # 编辑器工具
├── SPEC_DoomsdaySSW4.md      # 项目规格文档
├── UNITY_SETUP_GUIDE.md      # Unity集成详细指南
├── QUICK_START.md            # 快速开始指南
└── TESTING_GUIDE.md          # 测试指南
```

## 核心功能

### 已实现功能
- ✅ 分辨率设置系统（预设/自定义，全屏/窗口）
- ✅ 多语言支持（简体中文、繁体中文、英文）
- ✅ 设置持久化保存
- ✅ 游戏初始化系统

### 待实现功能
- ⏳ 游戏核心玩法（挖矿、任务、债务系统等）
- ⏳ 主菜单界面
- ⏳ 游戏主场景

## 文档

- [项目规格文档](SPEC_DoomsdaySSW4.md) - 完整的项目设计和架构说明
- [Unity集成指南](UNITY_SETUP_GUIDE.md) - 详细的Unity项目设置步骤
- [快速开始](QUICK_START.md) - 5分钟快速集成指南
- [测试指南](TESTING_GUIDE.md) - 功能测试和验证方法
- [设置系统文档](Assets/Scripts/Config/README_SETTINGS.md) - 分辨率与语言设置使用说明

## 开发工具

### Unity编辑器工具
项目提供了以下编辑器工具（在`Tools > DoomsdaySSW4`菜单中）：

1. **Setup Project** - 项目设置助手
   - 检查TextMeshPro安装
   - 检查资源文件
   - 检查脚本文件
   - 配置Player Settings

2. **Create Settings UI** - 自动创建设置界面
   - 自动创建MainMenu场景
   - 自动创建所有UI组件
   - 自动添加SettingsScreen组件

## 使用说明

### 分辨率设置
```csharp
// 设置分辨率
SettingsManager.Instance.SetResolution(1920, 1080, true);

// 获取可用分辨率
Resolution[] resolutions = SettingsManager.Instance.GetAvailableResolutions();
```

### 语言设置
```csharp
// 设置语言
LocalizationManager.Instance.SetLanguage("zh-CN");

// 获取本地化文本
string text = LocalizationManager.Instance.GetLocalizedString("ui.menu.start");
```

### 在UI中使用本地化
1. 添加`LocalizedText`组件到Text或TextMeshPro组件
2. 设置`Localization Key`（如：`ui.menu.start`）
3. 文本会自动根据当前语言更新

## 常见问题

### Q: TextMeshPro未找到错误
**A:** 安装TextMeshPro包：`Window > TextMeshPro > Import TMP Essential Resources`

### Q: 语言资源加载失败
**A:** 检查：
- Resources文件夹是否在Assets根目录
- Localization文件夹名称大小写是否正确
- JSON文件格式是否正确

### Q: UI组件不工作
**A:** 检查：
- SettingsScreen组件的UI引用是否正确连接
- 所有UI组件是否在Canvas下
- EventSystem是否存在

## 开发规范

- 所有脚本使用C#编写
- 遵循Unity命名规范
- 使用单例模式管理全局系统
- 使用事件系统进行组件通信
- 所有UI文本使用本地化系统

## 许可证

[根据项目需要添加许可证信息]

## 联系方式

[根据项目需要添加联系方式]
