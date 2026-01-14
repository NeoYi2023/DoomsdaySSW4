using UnityEngine;
using UnityEditor;
using System.IO;

/// <summary>
/// Unity编辑器工具：帮助快速设置项目
/// </summary>
public class ProjectSetupHelper : EditorWindow
{
    [MenuItem("Tools/DoomsdaySSW4/Setup Project")]
    public static void ShowWindow()
    {
        GetWindow<ProjectSetupHelper>("Project Setup Helper");
    }

    private void OnGUI()
    {
        GUILayout.Label("DoomsdaySSW4 项目设置助手", EditorStyles.boldLabel);
        GUILayout.Space(10);

        // 检查TextMeshPro
        GUILayout.Label("1. TextMeshPro 检查", EditorStyles.boldLabel);
        if (IsTextMeshProInstalled())
        {
            EditorGUILayout.HelpBox("TextMeshPro 已安装", MessageType.Info);
        }
        else
        {
            EditorGUILayout.HelpBox("TextMeshPro 未安装。请通过 Window > TextMeshPro > Import TMP Essential Resources 安装", MessageType.Warning);
        }

        GUILayout.Space(10);

        // 检查资源文件
        GUILayout.Label("2. 资源文件检查", EditorStyles.boldLabel);
        CheckResourceFiles();

        GUILayout.Space(10);

        // 检查脚本文件
        GUILayout.Label("3. 脚本文件检查", EditorStyles.boldLabel);
        CheckScriptFiles();

        GUILayout.Space(10);

        // 项目设置
        GUILayout.Label("4. 项目设置", EditorStyles.boldLabel);
        if (GUILayout.Button("配置Player Settings"))
        {
            ConfigurePlayerSettings();
        }

        GUILayout.Space(10);

        // 创建场景
        GUILayout.Label("5. 场景创建", EditorStyles.boldLabel);
        if (GUILayout.Button("创建MainMenu场景结构"))
        {
            CreateMainMenuScene();
        }
    }

    private bool IsTextMeshProInstalled()
    {
        return typeof(TMPro.TextMeshProUGUI) != null;
    }

    private void CheckResourceFiles()
    {
        string[] requiredFiles = {
            "Assets/Resources/Localization/zh-CN.json",
            "Assets/Resources/Localization/zh-TW.json",
            "Assets/Resources/Localization/en-US.json"
        };

        foreach (string file in requiredFiles)
        {
            if (File.Exists(file))
            {
                EditorGUILayout.HelpBox($"✓ {file}", MessageType.None);
            }
            else
            {
                EditorGUILayout.HelpBox($"✗ {file} 缺失", MessageType.Error);
            }
        }
    }

    private void CheckScriptFiles()
    {
        string[] requiredScripts = {
            "Assets/Scripts/Config/SettingsManager.cs",
            "Assets/Scripts/Core/GameInitializer.cs",
            "Assets/Scripts/Data/Models/LocalizationData.cs",
            "Assets/Scripts/UI/LocalizationManager.cs",
            "Assets/Scripts/UI/Components/LocalizedText.cs",
            "Assets/Scripts/UI/Screens/SettingsScreen.cs"
        };

        foreach (string script in requiredScripts)
        {
            if (File.Exists(script))
            {
                EditorGUILayout.HelpBox($"✓ {script}", MessageType.None);
            }
            else
            {
                EditorGUILayout.HelpBox($"✗ {script} 缺失", MessageType.Error);
            }
        }
    }

    private void ConfigurePlayerSettings()
    {
        PlayerSettings.companyName = "DoomsdaySSW4";
        PlayerSettings.productName = "DoomsdaySSW4";
        PlayerSettings.bundleVersion = "1.0.0";
        
        EditorUtility.DisplayDialog("设置完成", "Player Settings已配置", "确定");
    }

    private void CreateMainMenuScene()
    {
        // 检查Scenes文件夹是否存在
        if (!Directory.Exists("Assets/Scenes"))
        {
            Directory.CreateDirectory("Assets/Scenes");
            AssetDatabase.Refresh();
        }

        // 创建场景
        UnityEngine.SceneManagement.Scene newScene = UnityEditor.SceneManagement.EditorSceneManager.NewScene(
            UnityEditor.SceneManagement.NewSceneSetup.DefaultGameObjects);

        // 创建Canvas
        GameObject canvas = new GameObject("Canvas");
        Canvas canvasComponent = canvas.AddComponent<Canvas>();
        canvasComponent.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.AddComponent<UnityEngine.UI.CanvasScaler>();
        canvas.AddComponent<UnityEngine.UI.GraphicRaycaster>();

        // 创建EventSystem
        GameObject eventSystem = new GameObject("EventSystem");
        eventSystem.AddComponent<UnityEngine.EventSystems.EventSystem>();
        eventSystem.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();

        // 保存场景
        string scenePath = "Assets/Scenes/MainMenu.unity";
        UnityEditor.SceneManagement.EditorSceneManager.SaveScene(newScene, scenePath);
        
        EditorUtility.DisplayDialog("场景创建", $"MainMenu场景已创建在 {scenePath}\n\n接下来请手动：\n1. 创建设置面板和UI组件\n2. 添加SettingsScreen组件\n3. 连接UI组件引用", "确定");
    }
}
