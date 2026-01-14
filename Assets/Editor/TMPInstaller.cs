using UnityEngine;
using UnityEditor;

/// <summary>
/// TextMeshPro安装检查器
/// </summary>
[InitializeOnLoad]
public class TMPInstaller
{
    static TMPInstaller()
    {
        // 检查TextMeshPro是否已安装
        if (!IsTextMeshProInstalled())
        {
            EditorApplication.delayCall += ShowTMPInstallDialog;
        }
    }

    private static bool IsTextMeshProInstalled()
    {
        try
        {
            System.Type tmpType = System.Type.GetType("TMPro.TextMeshProUGUI, Unity.TextMeshPro");
            return tmpType != null;
        }
        catch
        {
            return false;
        }
    }

    private static void ShowTMPInstallDialog()
    {
        if (EditorUtility.DisplayDialog(
            "TextMeshPro 未安装",
            "检测到TextMeshPro未安装。\n\n代码中使用了TextMeshPro组件，需要安装才能正常工作。\n\n是否现在打开安装窗口？",
            "打开安装窗口",
            "稍后"))
        {
            // 尝试打开TextMeshPro导入窗口
            EditorApplication.ExecuteMenuItem("Window/TextMeshPro/Import TMP Essential Resources");
        }
    }
}
