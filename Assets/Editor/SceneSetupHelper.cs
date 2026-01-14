using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

/// <summary>
/// 场景设置助手：自动创建设置界面UI结构
/// </summary>
public class SceneSetupHelper
{
    [MenuItem("Tools/DoomsdaySSW4/Create Settings UI")]
    public static void CreateSettingsUI()
    {
        // 检查是否有Canvas
        Canvas canvas = Object.FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            EditorUtility.DisplayDialog("错误", "场景中没有Canvas。请先创建Canvas。", "确定");
            return;
        }

        // 创建设置面板
        GameObject settingsPanel = CreateSettingsPanel(canvas.transform);
        
        // 创建分辨率设置UI
        CreateResolutionSettings(settingsPanel.transform);
        
        // 创建语言设置UI
        CreateLanguageSettings(settingsPanel.transform);
        
        // 创建按钮
        CreateButtons(settingsPanel.transform);

        // 添加SettingsScreen组件
        SettingsScreen settingsScreen = settingsPanel.GetComponent<SettingsScreen>();
        if (settingsScreen == null)
        {
            settingsScreen = settingsPanel.AddComponent<SettingsScreen>();
        }

        EditorUtility.DisplayDialog("完成", "设置UI已创建。\n\n请手动连接SettingsScreen组件的UI引用。", "确定");
    }

    private static GameObject CreateSettingsPanel(Transform parent)
    {
        GameObject panel = new GameObject("SettingsPanel");
        panel.transform.SetParent(parent, false);
        
        Image image = panel.AddComponent<Image>();
        image.color = new Color(0.2f, 0.2f, 0.2f, 0.9f);
        
        RectTransform rectTransform = panel.GetComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.sizeDelta = new Vector2(800, 600);
        rectTransform.anchoredPosition = Vector2.zero;

        return panel;
    }

    private static void CreateResolutionSettings(Transform parent)
    {
        // 创建标题
        CreateLabel(parent, "分辨率设置", new Vector2(0, 250), 24);

        // Resolution Dropdown
        GameObject resolutionDropdown = CreateDropdown(parent, "ResolutionDropdown", new Vector2(-200, 200));
        
        // Resolution Mode Dropdown
        GameObject modeDropdown = CreateDropdown(parent, "ResolutionModeDropdown", new Vector2(-200, 150));
        
        // Width InputField
        GameObject widthInput = CreateInputField(parent, "WidthInputField", new Vector2(-200, 100), "宽度");
        
        // Height InputField
        GameObject heightInput = CreateInputField(parent, "HeightInputField", new Vector2(-200, 50), "高度");
        
        // Fullscreen Toggle
        GameObject fullscreenToggle = CreateToggle(parent, "FullscreenToggle", new Vector2(-200, 0), "全屏模式");
    }

    private static void CreateLanguageSettings(Transform parent)
    {
        // 创建标题
        CreateLabel(parent, "语言设置", new Vector2(0, -100), 24);
        
        // Language Dropdown
        GameObject languageDropdown = CreateDropdown(parent, "LanguageDropdown", new Vector2(0, -150));
    }

    private static void CreateButtons(Transform parent)
    {
        // Apply Button
        GameObject applyButton = CreateButton(parent, "ApplyButton", new Vector2(-100, -250), "应用");
        
        // Cancel Button
        GameObject cancelButton = CreateButton(parent, "CancelButton", new Vector2(100, -250), "取消");
    }

    private static GameObject CreateLabel(Transform parent, string text, Vector2 position, int fontSize)
    {
        GameObject label = new GameObject("Label_" + text);
        label.transform.SetParent(parent, false);
        
        TextMeshProUGUI tmp = label.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.alignment = TextAlignmentOptions.Center;
        
        RectTransform rectTransform = label.GetComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.sizeDelta = new Vector2(200, 30);
        rectTransform.anchoredPosition = position;

        return label;
    }

    private static GameObject CreateDropdown(Transform parent, string name, Vector2 position)
    {
        GameObject dropdown = new GameObject(name);
        dropdown.transform.SetParent(parent, false);
        
        TMP_Dropdown tmpDropdown = dropdown.AddComponent<TMP_Dropdown>();
        
        RectTransform rectTransform = dropdown.GetComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.sizeDelta = new Vector2(200, 30);
        rectTransform.anchoredPosition = position;

        // 创建Label子对象
        GameObject label = new GameObject("Label");
        label.transform.SetParent(dropdown.transform, false);
        TextMeshProUGUI labelText = label.AddComponent<TextMeshProUGUI>();
        labelText.text = "选项";
        labelText.fontSize = 14;
        
        RectTransform labelRect = label.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.sizeDelta = Vector2.zero;
        labelRect.offsetMin = new Vector2(10, 6);
        labelRect.offsetMax = new Vector2(-25, -7);

        tmpDropdown.captionText = labelText;

        return dropdown;
    }

    private static GameObject CreateInputField(Transform parent, string name, Vector2 position, string placeholder)
    {
        GameObject inputField = new GameObject(name);
        inputField.transform.SetParent(parent, false);
        
        TMP_InputField tmpInput = inputField.AddComponent<TMP_InputField>();
        
        RectTransform rectTransform = inputField.GetComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.sizeDelta = new Vector2(150, 30);
        rectTransform.anchoredPosition = position;

        // 创建Text Area
        GameObject textArea = new GameObject("Text Area");
        textArea.transform.SetParent(inputField.transform, false);
        RectTransform textAreaRect = textArea.AddComponent<RectTransform>();
        textAreaRect.anchorMin = Vector2.zero;
        textAreaRect.anchorMax = Vector2.one;
        textAreaRect.sizeDelta = Vector2.zero;
        textAreaRect.offsetMin = new Vector2(10, 0);
        textAreaRect.offsetMax = new Vector2(-10, 0);

        // 创建Text
        GameObject text = new GameObject("Text");
        text.transform.SetParent(textArea.transform, false);
        TextMeshProUGUI textComponent = text.AddComponent<TextMeshProUGUI>();
        textComponent.text = "";
        textComponent.fontSize = 14;
        
        RectTransform textRect = text.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        tmpInput.textViewport = textAreaRect;
        tmpInput.textComponent = textComponent;

        // 创建Placeholder
        GameObject placeholderObj = new GameObject("Placeholder");
        placeholderObj.transform.SetParent(textArea.transform, false);
        TextMeshProUGUI placeholderText = placeholderObj.AddComponent<TextMeshProUGUI>();
        placeholderText.text = placeholder;
        placeholderText.fontSize = 14;
        placeholderText.color = new Color(0.5f, 0.5f, 0.5f, 0.5f);
        
        RectTransform placeholderRect = placeholderObj.GetComponent<RectTransform>();
        placeholderRect.anchorMin = Vector2.zero;
        placeholderRect.anchorMax = Vector2.one;
        placeholderRect.sizeDelta = Vector2.zero;
        placeholderRect.offsetMin = Vector2.zero;
        placeholderRect.offsetMax = Vector2.zero;

        tmpInput.placeholder = placeholderText;

        return inputField;
    }

    private static GameObject CreateToggle(Transform parent, string name, Vector2 position, string label)
    {
        GameObject toggle = new GameObject(name);
        toggle.transform.SetParent(parent, false);
        
        Toggle toggleComponent = toggle.AddComponent<Toggle>();
        
        RectTransform rectTransform = toggle.GetComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.sizeDelta = new Vector2(200, 20);
        rectTransform.anchoredPosition = position;

        // 创建Background
        GameObject background = new GameObject("Background");
        background.transform.SetParent(toggle.transform, false);
        Image bgImage = background.AddComponent<Image>();
        bgImage.color = new Color(0.2f, 0.2f, 0.2f, 1f);
        
        RectTransform bgRect = background.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.sizeDelta = Vector2.zero;
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;

        // 创建Checkmark
        GameObject checkmark = new GameObject("Checkmark");
        checkmark.transform.SetParent(background.transform, false);
        Image checkmarkImage = checkmark.AddComponent<Image>();
        checkmarkImage.color = Color.white;
        
        RectTransform checkmarkRect = checkmark.GetComponent<RectTransform>();
        checkmarkRect.anchorMin = new Vector2(0.2f, 0.2f);
        checkmarkRect.anchorMax = new Vector2(0.8f, 0.8f);
        checkmarkRect.sizeDelta = Vector2.zero;
        checkmarkRect.offsetMin = Vector2.zero;
        checkmarkRect.offsetMax = Vector2.zero;

        toggleComponent.graphic = checkmarkImage;
        toggleComponent.targetGraphic = bgImage;

        // 创建Label
        GameObject labelObj = new GameObject("Label");
        labelObj.transform.SetParent(toggle.transform, false);
        TextMeshProUGUI labelText = labelObj.AddComponent<TextMeshProUGUI>();
        labelText.text = label;
        labelText.fontSize = 14;
        
        RectTransform labelRect = labelObj.GetComponent<RectTransform>();
        labelRect.anchorMin = new Vector2(0, 0);
        labelRect.anchorMax = new Vector2(1, 1);
        labelRect.sizeDelta = Vector2.zero;
        labelRect.offsetMin = new Vector2(25, 0);
        labelRect.offsetMax = new Vector2(0, 0);

        return toggle;
    }

    private static GameObject CreateButton(Transform parent, string name, Vector2 position, string text)
    {
        GameObject button = new GameObject(name);
        button.transform.SetParent(parent, false);
        
        Image buttonImage = button.AddComponent<Image>();
        buttonImage.color = new Color(0.2f, 0.6f, 0.9f, 1f);
        
        Button buttonComponent = button.AddComponent<Button>();
        
        RectTransform rectTransform = button.GetComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.sizeDelta = new Vector2(150, 40);
        rectTransform.anchoredPosition = position;

        // 创建Text
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(button.transform, false);
        TextMeshProUGUI textComponent = textObj.AddComponent<TextMeshProUGUI>();
        textComponent.text = text;
        textComponent.fontSize = 16;
        textComponent.alignment = TextAlignmentOptions.Center;
        textComponent.color = Color.white;
        
        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        return button;
    }
}
