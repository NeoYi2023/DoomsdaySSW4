using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using System.Text;

/// <summary>
/// 游戏运行检查工具：自动检查场景配置和UI连接
/// </summary>
public class GameRuntimeChecker : EditorWindow
{
    private Vector2 scrollPosition;
    private List<string> issues = new List<string>();
    private List<string> warnings = new List<string>();
    private List<string> successes = new List<string>();

    [MenuItem("Tools/游戏运行检查")]
    public static void ShowWindow()
    {
        GetWindow<GameRuntimeChecker>("游戏运行检查");
    }

    private void OnGUI()
    {
        GUILayout.Label("游戏运行完整检查工具", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        if (GUILayout.Button("开始检查", GUILayout.Height(30)))
        {
            RunCheck();
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("检查结果", EditorStyles.boldLabel);

        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        // 显示成功项
        if (successes.Count > 0)
        {
            EditorGUILayout.LabelField($"✓ 通过 ({successes.Count})", EditorStyles.boldLabel);
            foreach (string success in successes)
            {
                EditorGUILayout.LabelField($"  ✓ {success}", EditorStyles.wordWrappedLabel);
            }
            EditorGUILayout.Space();
        }

        // 显示警告
        if (warnings.Count > 0)
        {
            EditorGUILayout.LabelField($"⚠ 警告 ({warnings.Count})", EditorStyles.boldLabel);
            foreach (string warning in warnings)
            {
                EditorGUILayout.HelpBox(warning, MessageType.Warning);
            }
            EditorGUILayout.Space();
        }

        // 显示问题
        if (issues.Count > 0)
        {
            EditorGUILayout.LabelField($"❌ 问题 ({issues.Count})", EditorStyles.boldLabel);
            foreach (string issue in issues)
            {
                EditorGUILayout.HelpBox(issue, MessageType.Error);
            }
        }

        EditorGUILayout.EndScrollView();

        EditorGUILayout.Space();

        // 总结
        if (issues.Count == 0 && warnings.Count == 0)
        {
            EditorGUILayout.HelpBox("✓ 所有检查项通过！游戏应该可以正常运行。", MessageType.Info);
        }
        else if (issues.Count == 0)
        {
            EditorGUILayout.HelpBox("⚠ 有一些警告，但不影响基本运行。", MessageType.Warning);
        }
        else
        {
            EditorGUILayout.HelpBox($"❌ 发现 {issues.Count} 个问题需要修复。请查看上方详细信息。", MessageType.Error);
        }
    }

    private void RunCheck()
    {
        issues.Clear();
        warnings.Clear();
        successes.Clear();

        // 检查场景
        CheckScene();

        // 检查UI结构
        CheckUIStructure();

        // 检查UI引用
        CheckUIReferences();

        // 检查配置文件
        CheckConfigFiles();

        // 检查字体资源
        CheckFontResources();

        // 检查Build Settings
        CheckBuildSettings();
    }

    private void CheckScene()
    {
        Scene currentScene = SceneManager.GetActiveScene();
        if (currentScene == null || !currentScene.isLoaded)
        {
            issues.Add("当前没有加载的场景");
            return;
        }

        GameObject[] rootObjects = currentScene.GetRootGameObjects();

        // 检查Canvas
        Canvas canvas = FindComponentInScene<Canvas>(rootObjects);
        if (canvas == null)
        {
            issues.Add("场景中没有Canvas GameObject");
        }
        else
        {
            successes.Add("Canvas存在");
            
            // 检查Canvas配置
            CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
            if (scaler == null)
            {
                issues.Add("Canvas缺少CanvasScaler组件");
            }
            else
            {
                if (scaler.uiScaleMode != CanvasScaler.ScaleMode.ScaleWithScreenSize)
                {
                    warnings.Add("Canvas Scaler的UI Scale Mode不是Scale With Screen Size");
                }
                if (scaler.referenceResolution != new Vector2(1920, 1080))
                {
                    warnings.Add("Canvas Scaler的Reference Resolution不是1920x1080");
                }
            }

            // 检查CanvasKeeper
            if (canvas.GetComponent<CanvasKeeper>() == null)
            {
                warnings.Add("Canvas缺少CanvasKeeper组件（建议添加以确保Canvas保持激活）");
            }
            else
            {
                successes.Add("CanvasKeeper组件已添加");
            }
        }

        // 检查EventSystem
        EventSystem eventSystem = FindComponentInScene<EventSystem>(rootObjects);
        if (eventSystem == null)
        {
            issues.Add("场景中没有EventSystem GameObject");
        }
        else
        {
            successes.Add("EventSystem存在");
        }

        // 检查GameInitializer
        GameInitializer initializer = FindComponentInScene<GameInitializer>(rootObjects);
        if (initializer == null)
        {
            issues.Add("场景中没有GameInitializer GameObject（必须添加才能启动游戏）");
        }
        else
        {
            successes.Add("GameInitializer存在");
            
            SerializedObject so = new SerializedObject(initializer);
            SerializedProperty prop = so.FindProperty("initializeOnStart");
            if (prop != null && !prop.boolValue)
            {
                warnings.Add("GameInitializer的Initialize On Start未勾选");
            }
        }
    }

    private void CheckUIStructure()
    {
        GameScreen gameScreen = FindObjectOfType<GameScreen>();
        if (gameScreen == null)
        {
            issues.Add("场景中没有GameScreen GameObject");
            return;
        }

        successes.Add("GameScreen GameObject存在");

        // 检查GameScreen的子对象
        Transform gameScreenTransform = gameScreen.transform;
        
        // 检查LeftPanel
        Transform leftPanel = gameScreenTransform.Find("LeftPanel");
        if (leftPanel == null)
        {
            issues.Add("GameScreen下缺少LeftPanel");
        }
        else
        {
            successes.Add("LeftPanel存在");
            
            // 检查MiningMapContainer
            Transform miningContainer = leftPanel.Find("MiningMapContainer");
            if (miningContainer == null)
            {
                issues.Add("LeftPanel下缺少MiningMapContainer");
            }
            else
            {
                successes.Add("MiningMapContainer存在");
                
                if (miningContainer.GetComponent<MiningMapView>() == null)
                {
                    issues.Add("MiningMapContainer缺少MiningMapView组件");
                }
                
                if (miningContainer.GetComponent<GridLayoutGroup>() == null)
                {
                    issues.Add("MiningMapContainer缺少GridLayoutGroup组件");
                }
            }
        }

        // 检查RightPanel
        Transform rightPanel = gameScreenTransform.Find("RightPanel");
        if (rightPanel == null)
        {
            issues.Add("GameScreen下缺少RightPanel");
        }
        else
        {
            successes.Add("RightPanel存在");
            
            // 检查各个Panel
            CheckPanelChild(rightPanel, "TaskInfoPanel", new[] { "TaskNameText", "TaskProgressText", "RemainingTurnsText" });
            CheckPanelChild(rightPanel, "DebtInfoPanel", new[] { "DebtInfoText" });
            CheckPanelChild(rightPanel, "ResourcePanel", new[] { "MoneyText", "EnergyText" });
            CheckPanelChild(rightPanel, "DrillInfoPanel", new[] { "DrillInfoText" });
        }

        // 检查BottomPanel
        Transform bottomPanel = gameScreenTransform.Find("BottomPanel");
        if (bottomPanel == null)
        {
            issues.Add("GameScreen下缺少BottomPanel");
        }
        else
        {
            successes.Add("BottomPanel存在");
            CheckPanelChild(bottomPanel, null, new[] { "EndTurnButton", "SettingsButton" });
        }

        // 检查UpgradeSelectionScreen
        UpgradeSelectionScreen upgradeScreen = FindObjectOfType<UpgradeSelectionScreen>();
        if (upgradeScreen == null)
        {
            warnings.Add("场景中没有UpgradeSelectionScreen GameObject（升级界面）");
        }
        else
        {
            successes.Add("UpgradeSelectionScreen存在");
        }
    }

    private void CheckPanelChild(Transform parent, string panelName, string[] childNames)
    {
        Transform panel = panelName != null ? parent.Find(panelName) : parent;
        
        if (panelName != null && panel == null)
        {
            issues.Add($"{parent.name}下缺少{panelName}");
            return;
        }

        if (panelName != null)
        {
            successes.Add($"{panelName}存在");
        }

        foreach (string childName in childNames)
        {
            Transform child = panel.Find(childName);
            if (child == null)
            {
                issues.Add($"{panel.name}下缺少{childName}");
            }
            else
            {
                successes.Add($"{childName}存在");
            }
        }
    }

    private void CheckUIReferences()
    {
        GameScreen gameScreen = FindObjectOfType<GameScreen>();
        if (gameScreen == null) return;

        SerializedObject so = new SerializedObject(gameScreen);
        
        // 检查GameScreen的引用
        CheckSerializedReference(so, "miningMapContainer", "Mining Map Container");
        CheckSerializedReference(so, "taskNameText", "Task Name Text");
        CheckSerializedReference(so, "taskProgressText", "Task Progress Text");
        CheckSerializedReference(so, "remainingTurnsText", "Remaining Turns Text");
        CheckSerializedReference(so, "debtInfoText", "Debt Info Text");
        CheckSerializedReference(so, "moneyText", "Money Text");
        CheckSerializedReference(so, "energyText", "Energy Text");
        CheckSerializedReference(so, "drillInfoText", "Drill Info Text");
        CheckSerializedReference(so, "endTurnButton", "End Turn Button");
        CheckSerializedReference(so, "settingsButton", "Settings Button");

        // 检查UpgradeSelectionScreen
        UpgradeSelectionScreen upgradeScreen = FindObjectOfType<UpgradeSelectionScreen>();
        if (upgradeScreen != null)
        {
            SerializedObject upgradeSo = new SerializedObject(upgradeScreen);
            CheckSerializedReference(upgradeSo, "panel", "Panel");
            CheckSerializedReference(upgradeSo, "titleText", "Title Text");
        }
    }

    private void CheckSerializedReference(SerializedObject so, string propertyName, string displayName)
    {
        SerializedProperty prop = so.FindProperty(propertyName);
        if (prop == null) return;

        if (prop.propertyType == SerializedPropertyType.ObjectReference)
        {
            if (prop.objectReferenceValue == null)
            {
                issues.Add($"GameScreen的{displayName}未连接");
            }
            else
            {
                successes.Add($"{displayName}已连接");
            }
        }
        else if (prop.propertyType == SerializedPropertyType.ArraySize)
        {
            // 数组类型
            int arraySize = prop.arraySize;
            if (arraySize == 0)
            {
                warnings.Add($"{displayName}数组为空");
            }
        }
    }

    private void CheckConfigFiles()
    {
        string[] configFiles = {
            "Configs/TaskConfigs",
            "Configs/OreConfigs",
            "Configs/DrillConfigs",
            "Configs/ShipConfigs",
            "Configs/OreSpawnConfigs"
        };

        foreach (string configPath in configFiles)
        {
            TextAsset config = Resources.Load<TextAsset>(configPath);
            if (config == null)
            {
                issues.Add($"配置文件不存在: {configPath}.json");
            }
            else
            {
                successes.Add($"配置文件存在: {configPath}.json");
                
                // 简单验证JSON格式
                if (string.IsNullOrEmpty(config.text) || config.text.Trim().Length == 0)
                {
                    issues.Add($"配置文件为空: {configPath}.json");
                }
            }
        }
    }

    private void CheckFontResources()
    {
        // 检查是否有中文字体资源
        TMP_FontAsset[] fonts = Resources.LoadAll<TMP_FontAsset>("Fonts & Materials");
        bool hasChineseFont = false;
        
        foreach (TMP_FontAsset font in fonts)
        {
            if (font != null && font.name.ToLower().Contains("chinese") || 
                font.name.ToLower().Contains("yahei") || 
                font.name.ToLower().Contains("微软"))
            {
                hasChineseFont = true;
                if (font.characterLookupTable != null && font.characterLookupTable.Count > 0)
                {
                    successes.Add($"找到中文字体资源: {font.name} ({font.characterLookupTable.Count}个字符)");
                }
                else
                {
                    warnings.Add($"中文字体资源 {font.name} 的字符查找表为空，需要重新生成");
                }
                break;
            }
        }

        if (!hasChineseFont)
        {
            warnings.Add("未找到中文字体资源（代码会自动尝试加载，但建议手动创建）");
        }
    }

    private void CheckBuildSettings()
    {
        Scene currentScene = SceneManager.GetActiveScene();
        if (currentScene == null) return;

        EditorBuildSettingsScene[] scenes = EditorBuildSettings.scenes;
        bool sceneInBuild = false;
        bool sceneIsFirst = false;

        for (int i = 0; i < scenes.Length; i++)
        {
            if (scenes[i].path == currentScene.path)
            {
                sceneInBuild = true;
                if (i == 0)
                {
                    sceneIsFirst = true;
                }
                break;
            }
        }

        if (!sceneInBuild)
        {
            warnings.Add("当前场景未添加到Build Settings");
        }
        else
        {
            successes.Add("场景已添加到Build Settings");
            
            if (!sceneIsFirst)
            {
                warnings.Add("当前场景不是启动场景（建议拖到Build Settings列表最上方）");
            }
            else
            {
                successes.Add("场景是启动场景");
            }
        }
    }

    private T FindComponentInScene<T>(GameObject[] rootObjects) where T : Component
    {
        foreach (GameObject obj in rootObjects)
        {
            T component = obj.GetComponentInChildren<T>(true);
            if (component != null)
                return component;
        }
        return null;
    }
}
