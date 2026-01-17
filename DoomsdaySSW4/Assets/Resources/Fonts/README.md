# 字体文件目录

## 说明

此目录用于存放中文字体文件（.ttf 或 .otf 格式），供动态字体加载系统使用。

## 使用步骤

### 1. 准备字体文件

推荐使用以下字体：
- **华文细黑**（STXIHEI.ttf）- 系统自带中文字体，支持中文显示
- **微软雅黑**（YaHei.ttf）- Windows 系统自带，位于 `C:\Windows\Fonts\msyh.ttc` 或 `msyhbd.ttc`
- **思源黑体**（SourceHanSans.ttf）- 开源字体，可从 Google Fonts 下载
- **Noto Sans CJK** - Google 字体，支持中日韩字符

### 2. 放置字体文件

将字体文件（.ttf 或 .otf）复制到此目录：
- 例如：`STXIHEI.ttf`（华文细黑，当前默认字体）
- 例如：`YaHei.ttf`
- 例如：`SourceHanSans.ttf`

**注意**：Unity 会自动识别此目录下的字体文件。

### 3. 配置字体路径

在 Unity 编辑器中：
1. 找到场景中的 `GameInitializer` GameObject
2. 在 Inspector 中确保 `Initialize Font On Start` 已勾选
3. 或者手动创建 `DynamicChineseFontLoader` GameObject：
   - 添加 `DynamicChineseFontLoader` 组件
   - 设置 `Source Font Path` 为字体文件名（不含扩展名和路径）
     - 如果文件是 `STXIHEI.ttf`，路径设置为：`Fonts/STXIHEI`（当前默认）
     - 如果文件是 `YaHei.ttf`，路径设置为：`Fonts/YaHei`
     - 如果文件是 `SourceHanSans.ttf`，路径设置为：`Fonts/SourceHanSans`

### 4. 验证

运行游戏后，查看 Console 输出：
- 如果看到 "动态字体资源创建成功"，说明字体加载成功
- 如果看到 "无法加载源字体文件"，请检查：
  - 字体文件是否在此目录下
  - `Source Font Path` 配置是否正确（应该是 `Fonts/字体文件名`，不含扩展名）

## 文件结构示例

```
Assets/Resources/Fonts/
├── STXIHEI.ttf        (华文细黑字体文件，当前默认)
├── YaHei.ttf          (微软雅黑字体文件)
├── SourceHanSans.ttf  (思源黑体字体文件)
└── README.md          (本说明文件)
```

## 注意事项

1. 字体文件必须放在 `Assets/Resources/Fonts/` 目录下
2. `Source Font Path` 配置必须包含 `Fonts/` 前缀
3. 路径中不包含文件扩展名（.ttf 或 .otf）
4. 字体文件大小可能较大（几MB到几十MB），这是正常的
