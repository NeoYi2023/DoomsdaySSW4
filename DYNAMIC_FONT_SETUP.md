# 动态中文字体设置指南

## 概述

已实现**方案A：动态字体加载**，这是最省内存的中文字体加载方式。动态字体会按需生成字符，只包含实际使用的字符，初始内存占用最小。

## 已完成的更改

1. ✅ 创建了 `DynamicChineseFontLoader.cs` - 动态字体加载器
2. ✅ 更新了 `GameInitializer.cs` - 集成字体初始化
3. ✅ 更新了 `GameScreen.cs` - 注释掉旧的静态字体加载代码

## 使用步骤

### 步骤 1：准备中文字体文件

1. 在 Unity 项目中创建文件夹：`Assets/Resources/Fonts/`
2. 将中文字体文件（.ttf 或 .otf）放入该文件夹
   - **推荐字体**：
     - 微软雅黑（YaHei.ttf）- Windows 系统自带
     - 思源黑体（SourceHanSans.ttf）- 开源字体
     - Noto Sans CJK - Google 字体
   
   - **字体文件位置**：
     - Windows: `C:\Windows\Fonts\` 可以找到系统字体
     - 或者从网上下载字体文件

### 步骤 2：配置字体路径

1. 在 Unity 编辑器中，找到场景中的 `GameInitializer` GameObject
2. 在 Inspector 中：
   - 确保 `Initialize Font On Start` 已勾选
   - 或者手动创建 `DynamicChineseFontLoader` GameObject：
     - 添加 `DynamicChineseFontLoader` 组件
     - 设置 `Source Font Path` 为字体文件名（不含扩展名）
       - 例如：如果字体文件是 `YaHei.ttf`，路径设置为 `Fonts/YaHei`
       - 例如：如果字体文件是 `SourceHanSans.ttf`，路径设置为 `Fonts/SourceHanSans`

### 步骤 3：调整内存优化参数（可选）

如果需要进一步节省内存，可以在 `DynamicChineseFontLoader` 组件中调整：

- **Sampling Point Size**: `64` → `48`（节省约 30% 内存）
- **Atlas Width/Height**: `512` → `256`（节省约 75% 内存）

**注意**：降低这些参数可能会略微影响字体质量，但通常不明显。

### 步骤 4：运行游戏

1. 点击 Play 运行游戏
2. 查看 Console 输出，应该看到：
   ```
   动态中文字体加载器已初始化
   开始创建动态字体资源，源字体: [字体名称]
   动态字体资源创建成功: [字体名称] Dynamic SDF
   初始内存占用: 约 X.XX MB
   已为 X 个 TextMeshProUGUI 组件设置动态字体
   已设置动态字体为 TextMeshPro 默认字体
   ```

## 工作原理

### 动态字体模式

- **初始状态**：字体资源创建时，纹理图集是空的（或只包含基础字符）
- **按需生成**：当文本显示新字符时，Unity 会自动将该字符生成到纹理图集
- **内存增长**：内存会随着使用的字符数量逐渐增长
- **优势**：只包含实际使用的字符，不会浪费内存

### 内存占用对比

| 方式 | 初始内存 | 最终内存（使用100个中文字符） |
|------|---------|---------------------------|
| 动态字体（方案A） | ~0.5-1 MB | ~3-8 MB |
| 静态字体（最小字符集） | ~2-5 MB | ~2-5 MB |
| 静态字体（常用字符集） | ~10-30 MB | ~10-30 MB |
| 静态字体（完整字符集） | ~50-200 MB | ~50-200 MB |

## 验证字体是否正常工作

1. **检查 Console 日志**：
   - 应该看到 "动态字体资源创建成功"
   - 不应该看到字体加载错误

2. **检查游戏中的中文文本**：
   - 所有中文文本应该正常显示
   - 不应该显示为方框（□）

3. **检查内存占用**：
   - 可以在代码中调用 `DynamicChineseFontLoader.GetMemoryInfo()` 查看内存信息

## 常见问题

### Q: 字体文件找不到？

**A:** 检查以下几点：
1. 字体文件是否放在 `Assets/Resources/Fonts/` 目录下
2. `Source Font Path` 是否正确（不含扩展名，例如 `Fonts/YaHei` 而不是 `Fonts/YaHei.ttf`）
3. 字体文件是否已正确导入 Unity（检查 Inspector 中的导入设置）

### Q: 某些字符显示为方框？

**A:** 这是正常的，动态字体会在首次显示字符时自动生成。如果持续显示为方框：
1. 检查源字体文件是否包含该字符
2. 检查字体文件是否正确加载
3. 查看 Console 是否有错误信息

### Q: 首次显示字符有延迟？

**A:** 这是动态字体的正常行为。首次显示新字符时，Unity 需要生成字符到纹理图集，可能有短暂延迟（通常 < 100ms）。这是为了节省内存的权衡。

### Q: 如何查看当前字体内存占用？

**A:** 可以在代码中调用：
```csharp
DynamicChineseFontLoader loader = FindObjectOfType<DynamicChineseFontLoader>();
if (loader != null)
{
    Debug.Log(loader.GetMemoryInfo());
}
```

### Q: 可以同时使用多个字体吗？

**A:** 可以。动态字体加载器会自动应用到所有 TextMeshProUGUI 组件，但你可以为特定文本组件设置不同的字体。

## 性能优化建议

1. **预生成常用字符**（高级用法）：
   - 可以在游戏启动时预先生成常用字符
   - 通过显示包含所有常用字符的隐藏文本来实现

2. **使用 Fallback 字体链**：
   - 主字体（英文+数字，很小）+ 备用中文字体（动态）
   - 可以进一步优化内存占用

3. **监控内存使用**：
   - 定期检查 `GetMemoryInfo()` 的输出
   - 如果内存增长过快，考虑使用静态字体（方案B）

## 回退到静态字体

如果需要回退到静态字体加载方式：

1. 在 `GameInitializer` 中取消勾选 `Initialize Font On Start`
2. 在 `GameScreen.cs` 中取消注释 `TrySetChineseFont()` 方法
3. 按照 `UI_FONT_SETUP_GUIDE.md` 创建静态字体资源

## 技术支持

如果遇到问题，请检查：
1. Console 日志中的错误信息
2. 字体文件路径是否正确
3. Unity 版本是否支持 TextMeshPro（需要 Unity 2018.1+）
