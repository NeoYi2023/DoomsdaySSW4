using UnityEngine;
using UnityEditor;
using TMPro;

/// <summary>
/// 批量设置 TextMeshPro 中文字体的编辑器工具
/// </summary>
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
        EditorGUILayout.Space();
        
        EditorGUILayout.HelpBox(
            "使用说明：\n" +
            "1. 先创建支持中文的 TextMeshPro 字体资源（参考 UI_FONT_SETUP_GUIDE.md）\n" +
            "2. 在此窗口中选择字体资源\n" +
            "3. 点击按钮批量应用到所有 TextMeshProUGUI 组件",
            MessageType.Info
        );
        
        EditorGUILayout.Space();
        
        chineseFont = (TMP_FontAsset)EditorGUILayout.ObjectField(
            "中文字体资源", 
            chineseFont, 
            typeof(TMP_FontAsset), 
            false
        );

        EditorGUILayout.Space();

        GUI.enabled = chineseFont != null;
        
        if (GUILayout.Button("应用到场景中所有 TextMeshProUGUI", GUILayout.Height(30)))
        {
            ApplyToAllInScene();
        }

        if (GUILayout.Button("应用到选中对象及其子对象", GUILayout.Height(30)))
        {
            ApplyToSelected();
        }

        GUI.enabled = true;
    }

    private void ApplyToAllInScene()
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
            Undo.RecordObject(text, "设置中文字体");
            text.font = chineseFont;
            count++;
        }

        if (count > 0)
        {
            EditorUtility.DisplayDialog("完成", 
                $"已为 {count} 个 TextMeshProUGUI 组件设置中文字体！\n\n" +
                "请检查场景中的文本显示是否正常。", 
                "确定");
        }
        else
        {
            EditorUtility.DisplayDialog("提示", 
                "场景中没有找到 TextMeshProUGUI 组件。", 
                "确定");
        }
    }

    private void ApplyToSelected()
    {
        if (chineseFont == null)
        {
            EditorUtility.DisplayDialog("错误", "请先选择中文字体资源！", "确定");
            return;
        }

        GameObject[] selected = Selection.gameObjects;
        
        if (selected.Length == 0)
        {
            EditorUtility.DisplayDialog("提示", "请先在 Hierarchy 中选择 GameObject！", "确定");
            return;
        }

        int count = 0;

        foreach (GameObject obj in selected)
        {
            TextMeshProUGUI[] texts = obj.GetComponentsInChildren<TextMeshProUGUI>(true);
            foreach (TextMeshProUGUI text in texts)
            {
                Undo.RecordObject(text, "设置中文字体");
                text.font = chineseFont;
                count++;
            }
        }

        EditorUtility.DisplayDialog("完成", 
            $"已为选中对象中的 {count} 个 TextMeshProUGUI 组件设置中文字体！", 
            "确定");
    }
}
