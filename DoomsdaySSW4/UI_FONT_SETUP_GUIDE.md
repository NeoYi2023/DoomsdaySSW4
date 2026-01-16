# TextMeshPro 中文字体设置指南

## 问题说明

如果看到中文文字显示为方框（□），说明当前使用的 TextMeshPro 字体资源不包含中文字符。

## 解决方案

### 方法一：创建支持中文的 TextMeshPro 字体资源（推荐）

#### 步骤 1：准备中文字体文件
1. 准备一个支持中文的字体文件（.ttf 或 .otf 格式）
   - 推荐字体：思源黑体（Source Han Sans）、微软雅黑、宋体等
   - 可以从系统字体文件夹复制，或从网上下载

#### 步骤 2：导入字体到 Unity
1. 在 Project 窗口中，创建文件夹：`Assets/Fonts`
2. 将字体文件（.ttf/.otf）拖拽到 `Assets/Fonts` 文件夹

#### 步骤 3：创建 TextMeshPro 字体资源
1. 在 Unity 菜单栏选择：`Window > TextMeshPro > Font Asset Creator`
2. 在 Font Asset Creator 窗口中：
   - **Source Font File**: 选择你导入的中文字体文件（.ttf/.otf）
   - **Sampling Point Size**: 设置为 `72`（或更大，如 128，质量更好但文件更大）
   - **Padding**: 设置为 `9`（字符间距）
   - **Packing Method**: 选择 `Fast` 或 `Optimal`
   - **Atlas Resolution**: 选择 `1024 x 1024` 或 `2048 x 2048`（如果字符很多，选择更大的分辨率）

#### 步骤 4：生成字符集
1. 在 **Character Set** 下拉菜单中选择：
   - **ASCII**（基础英文字符）
   - **Extended ASCII**（扩展字符）
   - **Custom Characters**（自定义字符集）
   - **Characters from File**（从文件读取字符）

2. **推荐使用 "Characters from File"**：
   - 创建一个文本文件（如 `chinese_chars.txt`）
   - 将所有需要显示的中文字符放入文件（可以复制游戏中的中文文本）
   - 或者使用常用中文字符集：
     ```
     一二三四五六七八九十百千万年月日时分秒
     任务进度剩余回合债务金钱能源钻头强度范围
     铁金钻石水晶能源核心已挖掘空岩石
     结束回合设置升级选择描述
     ```

3. 点击 **Generate Font Atlas** 按钮
4. 等待生成完成（可能需要几分钟）

#### 步骤 5：保存字体资源
1. 生成完成后，点击 **Save** 或 **Save as...** 按钮
2. 保存到：`Assets/TextMesh Pro/Resources/Fonts & Materials/`
3. 命名为：`ChineseFont SDF`（或你喜欢的名称）

#### 步骤 6：设置为默认字体（可选）
1. 在 Unity 菜单栏选择：`Window > TextMeshPro > Settings`
2. 在 **TextMeshPro Settings** 窗口中：
   - 找到 **Default Font Asset**
   - 将新创建的中文字体资源拖拽到该字段
3. 这样所有新创建的 TextMeshPro 文本都会自动使用中文字体

#### 步骤 7：为现有文本组件设置字体
1. 在 Hierarchy 中选择所有使用 TextMeshProUGUI 的 GameObject
2. 在 Inspector 中，找到 **TextMeshProUGUI** 组件
3. 将 **Font Asset** 字段设置为新创建的中文字体资源
4. 或者批量设置（见方法二）

### 方法二：使用编辑器脚本批量设置字体

创建一个编辑器脚本来批量设置所有 TextMeshPro 文本的字体：

1. 创建文件夹：`Assets/Editor`（如果不存在）
2. 创建脚本：`Assets/Editor/SetChineseFont.cs`
3. 复制以下代码：

```csharp
using UnityEngine;
using UnityEditor;
using TMPro;

public class SetChineseFont : EditorWindow
{
    private TMP_FontAsset chineseFont;

    [MenuItem("Tools/设置中文字体")]
    public static void ShowWindow()
    {
        GetWindow<SetChineseFont>("设置中文字体");
    }

    private void OnGUI()
    {
        GUILayout.Label("批量设置 TextMeshPro 中文字体", EditorStyles.boldLabel);
        
        chineseFont = (TMP_FontAsset)EditorGUILayout.ObjectField(
            "中文字体资源", 
            chineseFont, 
            typeof(TMP_FontAsset), 
            false
        );

        if (GUILayout.Button("应用到场景中所有 TextMeshProUGUI"))
        {
            if (chineseFont == null)
            {
                EditorUtility.DisplayDialog("错误", "请先选择中文字体资源！", "确定");
                return;
            }

            TextMeshProUGUI[] allTexts = FindObjectsOfType<TextMeshProUGUI>(true);
            int count = 0;

            foreach (TextMeshProUGUI text in allTexts)
            {
                text.font = chineseFont;
                count++;
            }

            EditorUtility.DisplayDialog("完成", 
                $"已为 {count} 个 TextMeshProUGUI 组件设置中文字体！", 
                "确定");
        }

        if (GUILayout.Button("应用到选中对象"))
        {
            if (chineseFont == null)
            {
                EditorUtility.DisplayDialog("错误", "请先选择中文字体资源！", "确定");
                return;
            }

            GameObject[] selected = Selection.gameObjects;
            int count = 0;

            foreach (GameObject obj in selected)
            {
                TextMeshProUGUI[] texts = obj.GetComponentsInChildren<TextMeshProUGUI>(true);
                foreach (TextMeshProUGUI text in texts)
                {
                    text.font = chineseFont;
                    count++;
                }
            }

            EditorUtility.DisplayDialog("完成", 
                $"已为选中对象中的 {count} 个 TextMeshProUGUI 组件设置中文字体！", 
                "确定");
        }
    }
}
```

4. 使用方法：
   - 在 Unity 菜单栏选择：`Tools > 设置中文字体`
   - 在弹出窗口中，选择你创建的中文字体资源
   - 点击按钮批量设置

### 方法三：在代码中动态设置字体（运行时）

如果需要运行时动态设置，可以在 `GameScreen.cs` 的 `Start()` 方法中添加：

```csharp
// 加载中文字体资源
TMP_FontAsset chineseFont = Resources.Load<TMP_FontAsset>("Fonts & Materials/ChineseFont SDF");

if (chineseFont != null)
{
    // 为所有文本组件设置字体
    if (taskNameText != null) taskNameText.font = chineseFont;
    if (taskProgressText != null) taskProgressText.font = chineseFont;
    if (remainingTurnsText != null) remainingTurnsText.font = chineseFont;
    if (debtInfoText != null) debtInfoText.font = chineseFont;
    if (moneyText != null) moneyText.font = chineseFont;
    if (energyText != null) energyText.font = chineseFont;
    if (drillInfoText != null) drillInfoText.font = chineseFont;
}
```

## 快速测试

设置完成后：
1. 点击 Play 运行游戏
2. 检查所有中文文本是否正常显示
3. 如果仍有方框，检查：
   - 字体资源是否包含该字符
   - 字体资源是否正确分配给文本组件
   - 字体资源的 Atlas 分辨率是否足够

## 常见问题

### Q: 字体资源文件太大怎么办？
**A:** 
- 减少字符集（只包含游戏中实际使用的字符）
- 降低 Sampling Point Size
- 使用较小的 Atlas Resolution

### Q: 某些字符仍然显示为方框？
**A:** 
- 检查字体文件是否包含该字符
- 在 Font Asset Creator 中重新生成，确保包含该字符
- 使用 "Characters from File" 方式，明确列出所有需要的字符

### Q: 如何更新现有字体资源？
**A:**
1. 在 Project 窗口中选择字体资源
2. 在 Inspector 中点击 **Update Font Asset** 按钮
3. 或者重新生成字体资源

## 推荐字体资源

- **思源黑体（Source Han Sans）**：Google 和 Adobe 联合开发，支持中日韩字符
- **微软雅黑**：Windows 系统自带，支持中文
- **Noto Sans CJK**：Google 开发，支持中日韩字符

下载地址：
- 思源黑体：https://github.com/adobe-fonts/source-han-sans
- Noto Sans：https://www.google.com/get/noto/
