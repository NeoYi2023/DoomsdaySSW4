using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 三选一升级界面：显示升级选项供玩家选择
/// </summary>
public class UpgradeSelectionScreen : MonoBehaviour
{
    [Header("UI组件")]
    [SerializeField] private GameObject panel;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private Button[] optionButtons = new Button[3];
    [SerializeField] private TextMeshProUGUI[] optionNameTexts = new TextMeshProUGUI[3];
    [SerializeField] private TextMeshProUGUI[] optionDescriptionTexts = new TextMeshProUGUI[3];
    [SerializeField] private UnityEngine.UI.Image[] optionIcons = new UnityEngine.UI.Image[3]; // 升级选项图标
    [SerializeField] private TextMeshProUGUI[] optionButtonTexts = new TextMeshProUGUI[3]; // 按钮上的文本组件

    private GameManager _gameManager;
    private List<EnergyUpgradeOption> _currentOptions;

    private void Awake()
    {
        _gameManager = GameManager.Instance;

        // 注意：不在Awake中禁用panel，因为此时panel可能还未在Inspector中配置
        // 如果panel在Inspector中已配置，保持其初始状态
        // 如果panel未配置，会在Start中自动查找
    }

    private void Start()
    {
        // 如果panel未设置，尝试自动查找
        // 注意：只查找当前GameObject的直接子对象，避免找到其他GameObject下的Panel
        if (panel == null)
        {
            Canvas selfCanvas = GetComponent<Canvas>();
            bool isCanvas = selfCanvas != null;
            Transform panelTransform = null;

            if (isCanvas)
            {
                // 当前在Canvas上：查找场景中名为"UpgradeSelectionScreen"的GameObject下的Panel
                GameObject upgradeScreenGO = GameObject.Find("UpgradeSelectionScreen");
                if (upgradeScreenGO != null)
                {
                    for (int i = 0; i < upgradeScreenGO.transform.childCount; i++)
                    {
                        Transform child = upgradeScreenGO.transform.GetChild(i);
                        if (child.name == "Panel")
                        {
                            panelTransform = child;
                            break;
                        }
                    }
                }
            }
            else
            {
                // 优先查找名为"Panel"的直接子对象（这是正确的UI结构）
                for (int i = 0; i < transform.childCount; i++)
                {
                    Transform child = transform.GetChild(i);
                    if (child.name == "Panel")
                    {
                        panelTransform = child;
                        break;
                    }
                }
            }

            if (panelTransform != null)
            {
                panel = panelTransform.gameObject;
            }
            else if (!isCanvas)
            {
                // 如果找不到名为"Panel"的直接子对象，尝试查找子对象中的Image（可能是BackgroundImage，但不推荐）
                // 但只查找直接子对象，避免找到深层嵌套的Image
                UnityEngine.UI.Image imageComponent = null;
                for (int i = 0; i < transform.childCount; i++)
                {
                    Transform child = transform.GetChild(i);
                    UnityEngine.UI.Image img = child.GetComponent<UnityEngine.UI.Image>();
                    if (img != null)
                    {
                        imageComponent = img;
                        break;
                    }
                }

                if (imageComponent != null)
                {
                    panel = imageComponent.gameObject;
                    Debug.LogWarning($"UpgradeSelectionScreen: 未找到名为'Panel'的直接子对象，使用找到的Image组件 '{panel.name}' 作为panel。建议在Inspector中正确设置Panel字段。");
                }
            }
            else
            {
                // Canvas上找不到正确Panel，避免错误使用BackgroundImage
                Debug.LogError("UpgradeSelectionScreen: Canvas上未找到UpgradeSelectionScreen/Panel。请将UpgradeSelectionScreen组件挂在正确的GameObject上，并确保其下有Panel。");
            }
        }

        // 确保Panel在初始化时是禁用的（只有在ShowUpgradeOptions时才激活）
        // 但如果panel是Canvas，则不禁用它（因为Canvas是其他UI的根）
        if (panel != null)
        {
            Canvas panelCanvas = panel.GetComponent<Canvas>();
            bool panelIsCanvas = panelCanvas != null;
            bool panelIsBackground = panel.name == "BackgroundImage";
            
            if (!panelIsCanvas && !panelIsBackground)
            {
                panel.SetActive(false);
            }
            else if (panelIsBackground)
            {
                // 保持BackgroundImage始终激活
            }
            else
            {
                Debug.LogWarning("UpgradeSelectionScreen: panel被识别为Canvas，不会在初始化时禁用它。请确保UpgradeSelectionScreen组件附加在Canvas的子对象上，而不是Canvas本身。");
            }
        }

        // 自动查找选项按钮和文本组件（如果Inspector中未配置）
        AutoFindOptionComponents();

        // 设置按钮事件
        for (int i = 0; i < optionButtons.Length; i++)
        {
            int index = i; // 闭包变量
            if (optionButtons[i] != null)
            {
                // 清除之前可能存在的监听器（避免重复添加）
                optionButtons[i].onClick.RemoveAllListeners();
                // 使用实例方法引用，确保绑定到当前实例
                optionButtons[i].onClick.AddListener(() => this.OnOptionSelected(index));
            }
        }
        
        // 应用动态字体
        ApplyDynamicFont();
    }

    /// <summary>
    /// 自动查找选项组件（备用方案，如果Inspector中未配置）
    /// </summary>
    private void AutoFindOptionComponents()
    {
        // 查找选项按钮
        for (int i = 0; i < optionButtons.Length; i++)
        {
            if (optionButtons[i] == null)
            {
                string buttonName = $"Option{i + 1}Button";
                Transform buttonTransform = transform.FindDeepChild(buttonName);
                if (buttonTransform != null)
                {
                    optionButtons[i] = buttonTransform.GetComponent<Button>();
                    if (optionButtons[i] != null)
                    {
                        Debug.Log($"自动找到按钮: {buttonName}");
                    }
                }
            }
        }

        // 查找选项名称文本
        for (int i = 0; i < optionNameTexts.Length; i++)
        {
            if (optionNameTexts[i] == null)
            {
                string textName = $"Option{i + 1}NameText";
                Transform textTransform = transform.FindDeepChild(textName);
                if (textTransform != null)
                {
                    optionNameTexts[i] = textTransform.GetComponent<TextMeshProUGUI>();
                    if (optionNameTexts[i] != null)
                    {
                        Debug.Log($"自动找到名称文本: {textName}");
                    }
                }
            }
        }

        // 查找选项描述文本
        for (int i = 0; i < optionDescriptionTexts.Length; i++)
        {
            if (optionDescriptionTexts[i] == null)
            {
                string textName = $"Option{i + 1}DescText";
                Transform textTransform = transform.FindDeepChild(textName);
                if (textTransform != null)
                {
                    optionDescriptionTexts[i] = textTransform.GetComponent<TextMeshProUGUI>();
                    if (optionDescriptionTexts[i] != null)
                    {
                        Debug.Log($"自动找到描述文本: {textName}");
                    }
                }
            }
        }

        // 查找选项图标
        for (int i = 0; i < optionIcons.Length; i++)
        {
            if (optionIcons[i] == null && optionButtons[i] != null)
            {
                // 尝试在按钮的子对象中查找Image组件
                optionIcons[i] = optionButtons[i].GetComponentInChildren<UnityEngine.UI.Image>(true);
                if (optionIcons[i] != null)
                {
                    Debug.Log($"自动找到图标: Option{i + 1}Button的Image组件");
                }
            }
        }

        // 查找或创建按钮文本组件
        for (int i = 0; i < optionButtonTexts.Length; i++)
        {
            if (optionButtonTexts[i] == null && optionButtons[i] != null)
            {
                // 优先查找按钮子对象中已存在的 TextMeshProUGUI 组件
                TextMeshProUGUI existingText = optionButtons[i].GetComponentInChildren<TextMeshProUGUI>(true);
                if (existingText != null)
                {
                    optionButtonTexts[i] = existingText;
                    Debug.Log($"自动找到按钮文本: Option{i + 1}Button的TextMeshProUGUI组件");
                }
                else
                {
                    // 如果不存在，创建新的文本组件作为按钮的子对象
                    GameObject textObj = new GameObject("ButtonText");
                    textObj.transform.SetParent(optionButtons[i].transform, false);
                    
                    // 确保文本组件在按钮子对象列表的最后，这样会渲染在最前面（不被Image遮挡）
                    textObj.transform.SetAsLastSibling();
                    
                    RectTransform textRect = textObj.AddComponent<RectTransform>();
                    // 设置锚点和轴心为居中，确保文本在按钮中心
                    textRect.anchorMin = new Vector2(0.5f, 0.5f);
                    textRect.anchorMax = new Vector2(0.5f, 0.5f);
                    textRect.pivot = new Vector2(0.5f, 0.5f);
                    textRect.anchoredPosition = Vector2.zero; // 强制设置为(0, 0)
                    textRect.sizeDelta = new Vector2(160, 50); // 设置合适的尺寸
                    
                    TextMeshProUGUI text = textObj.AddComponent<TextMeshProUGUI>();
                    text.text = "选择";
                    text.fontSize = 18;
                    text.alignment = TextAlignmentOptions.Center;
                    text.color = Color.white;
                    // 确保文本组件可以接收射线（用于交互）
                    CanvasGroup canvasGroup = textObj.GetComponent<CanvasGroup>();
                    if (canvasGroup == null)
                    {
                        canvasGroup = textObj.AddComponent<CanvasGroup>();
                    }
                    canvasGroup.blocksRaycasts = false; // 不阻挡射线，让按钮可以点击
                    
                    optionButtonTexts[i] = text;
                    Debug.Log($"自动创建按钮文本: Option{i + 1}Button");
                }
            }
            else
            {
            }
        }

    }

    /// <summary>
    /// 显示升级选项
    /// </summary>
    public void ShowUpgradeOptions(List<EnergyUpgradeOption> options)
    {
        if (options == null || options.Count == 0)
        {
            Debug.LogWarning("没有可用的升级选项");
            return;
        }

        _currentOptions = new List<EnergyUpgradeOption>(options); // 创建副本，避免引用问题

        // 再次尝试自动查找组件（以防在Start之后UI结构发生变化）
        AutoFindOptionComponents();
        
        // 重新绑定按钮事件（确保绑定到当前实例）
        for (int i = 0; i < optionButtons.Length && i < options.Count; i++)
        {
            if (optionButtons[i] != null)
            {
                int index = i; // 闭包变量
                optionButtons[i].onClick.RemoveAllListeners();
                optionButtons[i].onClick.AddListener(() => this.OnOptionSelected(index));
            }
        }

        // 如果panel仍未设置，或者在运行时发现panel是BackgroundImage或错误的Panel，尝试查找真正的Panel
        // 注意：只查找直接子对象，避免找到其他GameObject下的Panel
        // 特殊情况：如果当前GameObject是Canvas，应该查找场景中名为"UpgradeSelectionScreen"的GameObject下的Panel
        if (panel == null || (panel != null && (panel.name == "BackgroundImage" || panel.name == "LeftPanel" || panel.name == "RightPanel")))
        {
            Canvas canvasComponent = GetComponent<Canvas>();
            bool isCanvas = canvasComponent != null;
            
            Transform panelTransform = null;
            
            if (isCanvas)
            {
                // 如果当前GameObject是Canvas，查找场景中名为"UpgradeSelectionScreen"的GameObject
                GameObject upgradeScreenGO = GameObject.Find("UpgradeSelectionScreen");
                if (upgradeScreenGO != null)
                {
                    // 查找UpgradeSelectionScreen GameObject下的Panel（直接子对象）
                    for (int i = 0; i < upgradeScreenGO.transform.childCount; i++)
                    {
                        Transform child = upgradeScreenGO.transform.GetChild(i);
                        if (child.name == "Panel")
                        {
                            panelTransform = child;
                            break;
                        }
                    }
                }
            }
            else
            {
                // 优先查找名为"Panel"的直接子对象
                for (int i = 0; i < transform.childCount; i++)
                {
                    Transform child = transform.GetChild(i);
                    if (child.name == "Panel")
                    {
                        panelTransform = child;
                        break;
                    }
                }
            }
            
            if (panelTransform != null)
            {
                panel = panelTransform.gameObject;
            }
            else if (panel != null && panel.name == "BackgroundImage" && !isCanvas)
            {
                // 如果panel是BackgroundImage且当前GameObject不是Canvas，尝试查找它的父对象中的Panel（直接子对象）
                Transform parent = panel.transform.parent;
                if (parent != null)
                {
                    for (int i = 0; i < parent.childCount; i++)
                    {
                        Transform child = parent.GetChild(i);
                        if (child.name == "Panel")
                        {
                            panel = child.gameObject;
                            break;
                        }
                    }
                }
            }
            
            // 如果仍然没有找到Panel，使用原来的逻辑（但只查找直接子对象）
            if (panel == null || panel.name == "BackgroundImage" || panel.name == "LeftPanel" || panel.name == "RightPanel")
            {
                // 如果找不到名为"Panel"的对象，尝试查找直接子对象中的Image
                UnityEngine.UI.Image imageComponent = null;
                for (int i = 0; i < transform.childCount; i++)
                {
                    Transform child = transform.GetChild(i);
                    UnityEngine.UI.Image img = child.GetComponent<UnityEngine.UI.Image>();
                    if (img != null && img.gameObject.name != "BackgroundImage")
                    {
                        imageComponent = img;
                        break;
                    }
                }
                
                if (imageComponent != null)
                {
                    panel = imageComponent.gameObject;
                    Debug.LogWarning($"UpgradeSelectionScreen: 运行时未找到名为'Panel'的直接子对象，使用找到的Image组件 '{panel.name}' 作为panel。");
                }
                else if (panel == null || panel.name == "LeftPanel" || panel.name == "RightPanel")
                {
                    // 使用当前GameObject作为panel（但发出警告）
                    panel = gameObject;
                    Debug.LogError($"UpgradeSelectionScreen: 运行时无法找到正确的Panel！当前gameObject是 '{gameObject.name}'。请确保UpgradeSelectionScreen组件附加在正确的GameObject上，并且该GameObject下有一个名为'Panel'的直接子对象。");
                }
            }
        }

        // 显示面板
        if (panel != null)
        {
            // 确保整个层级都是激活的（从根到Panel）
            Transform currentTransform = panel.transform;
            while (currentTransform != null)
            {
                if (!currentTransform.gameObject.activeSelf)
                {
                    currentTransform.gameObject.SetActive(true);
                }
                currentTransform = currentTransform.parent;
            }

            // 确保GameObject本身是激活的（但只在panel不是gameObject本身时）
            if (panel != gameObject && !gameObject.activeSelf)
            {
                gameObject.SetActive(true);
            }

            // 强制激活Panel
            panel.SetActive(true);
        }
        else
        {
            // 最后的备用方案：使用当前GameObject
            panel = gameObject;
            gameObject.SetActive(true);
        }

        // 更新标题
        if (titleText != null)
        {
            titleText.text = "选择升级";
        }

        // 更新选项显示
        for (int i = 0; i < optionButtons.Length && i < options.Count; i++)
        {
            EnergyUpgradeOption option = options[i];

            if (optionNameTexts[i] != null)
            {
                // 确保文本组件的父对象也是激活的
                Transform textTransform = optionNameTexts[i].transform;
                while (textTransform != null && textTransform != panel.transform)
                {
                    if (!textTransform.gameObject.activeSelf)
                    {
                        textTransform.gameObject.SetActive(true);
                    }
                    textTransform = textTransform.parent;
                }
                optionNameTexts[i].text = option.name;
                optionNameTexts[i].gameObject.SetActive(true);
            }

            if (optionDescriptionTexts[i] != null)
            {
                // 确保文本组件的父对象也是激活的
                Transform textTransform = optionDescriptionTexts[i].transform;
                while (textTransform != null && textTransform != panel.transform)
                {
                    if (!textTransform.gameObject.activeSelf)
                    {
                        textTransform.gameObject.SetActive(true);
                    }
                    textTransform = textTransform.parent;
                }
                optionDescriptionTexts[i].text = option.description;
                optionDescriptionTexts[i].gameObject.SetActive(true);
            }

            // 加载并显示图标
            if (optionIcons[i] != null)
            {
                LoadOptionIcon(optionIcons[i], option.iconPath);
            }

            if (optionButtons[i] != null)
            {
                // 确保按钮的整个层级都是激活的（从按钮到Panel）
                Transform buttonTransform = optionButtons[i].transform;
                while (buttonTransform != null && buttonTransform != panel.transform)
                {
                    if (!buttonTransform.gameObject.activeSelf)
                    {
                        buttonTransform.gameObject.SetActive(true);
                    }
                    buttonTransform = buttonTransform.parent;
                }
                
                optionButtons[i].gameObject.SetActive(true);
                // 确保按钮可以交互
                optionButtons[i].interactable = true;
            }

            // 设置按钮文本
            if (optionButtonTexts[i] != null)
            {
                optionButtonTexts[i].text = "选择";
                optionButtonTexts[i].gameObject.SetActive(true);
                // 强制设置文本颜色为白色，确保可见
                optionButtonTexts[i].color = Color.white;
                // 确保文本组件在按钮子对象列表的最后，这样会渲染在最前面
                optionButtonTexts[i].transform.SetAsLastSibling();
                // 修复RectTransform位置，确保文本在按钮中心
                RectTransform textRect = optionButtonTexts[i].rectTransform;
                textRect.anchorMin = new Vector2(0.5f, 0.5f);
                textRect.anchorMax = new Vector2(0.5f, 0.5f);
                textRect.pivot = new Vector2(0.5f, 0.5f);
                textRect.anchoredPosition = Vector2.zero;
                // 如果尺寸为0，设置默认尺寸
                if (textRect.sizeDelta.x <= 0 || textRect.sizeDelta.y <= 0)
                {
                    textRect.sizeDelta = new Vector2(160, 50);
                }
                // 应用动态字体
                FontHelper.ApplyFontToText(optionButtonTexts[i]);
            }
            else
            {
            }
        }

        // 隐藏多余的按钮
        for (int i = options.Count; i < optionButtons.Length; i++)
        {
            if (optionButtons[i] != null)
            {
                optionButtons[i].gameObject.SetActive(false);
            }
        }

        // 暂停游戏
        if (_gameManager != null)
        {
            _gameManager.PauseGame();
        }
    }

    /// <summary>
    /// 选择升级选项
    /// </summary>
    private void OnOptionSelected(int index)
    {
        if (_currentOptions == null || index < 0 || index >= _currentOptions.Count)
        {
            Debug.LogWarning($"无效的选项索引: {index}");
            return;
        }

        EnergyUpgradeOption selectedOption = _currentOptions[index];

        // 应用升级
        if (_gameManager != null)
        {
            _gameManager.ApplyUpgradeSelection(selectedOption);
        }
        else
        {
            Debug.LogError("GameManager为null，无法应用升级");
        }

        // 隐藏面板（但永远不禁用Canvas）
        // 如果panel是BackgroundImage或错误的Panel（如LeftPanel），尝试找到真正的Panel
        // 特殊情况：如果当前GameObject是Canvas，应该查找场景中名为"UpgradeSelectionScreen"的GameObject下的Panel
        GameObject actualPanel = panel;
        Canvas canvasComponent = GetComponent<Canvas>();
        bool isCanvas = canvasComponent != null;
        
        if (panel != null && (panel.name == "BackgroundImage" || panel.name == "LeftPanel" || panel.name == "RightPanel"))
        {
            Transform panelTransform = null;
            
            if (isCanvas)
            {
                // 如果当前GameObject是Canvas，查找场景中名为"UpgradeSelectionScreen"的GameObject
                GameObject upgradeScreenGO = GameObject.Find("UpgradeSelectionScreen");
                if (upgradeScreenGO != null)
                {
                    // 查找UpgradeSelectionScreen GameObject下的Panel（直接子对象）
                    for (int i = 0; i < upgradeScreenGO.transform.childCount; i++)
                    {
                        Transform child = upgradeScreenGO.transform.GetChild(i);
                        if (child.name == "Panel")
                        {
                            panelTransform = child;
                            break;
                        }
                    }
                }
            }
            else
            {
                // 尝试查找名为"Panel"的直接子对象
                for (int i = 0; i < transform.childCount; i++)
                {
                    Transform child = transform.GetChild(i);
                    if (child.name == "Panel")
                    {
                        panelTransform = child;
                        break;
                    }
                }
            }
            
            if (panelTransform != null)
            {
                actualPanel = panelTransform.gameObject;
            }
            else if (panel.name == "BackgroundImage" && !isCanvas)
            {
                // 尝试在BackgroundImage的父对象中查找Panel（直接子对象）
                Transform parent = panel.transform.parent;
                if (parent != null)
                {
                    for (int i = 0; i < parent.childCount; i++)
                    {
                        Transform child = parent.GetChild(i);
                        if (child.name == "Panel")
                        {
                            actualPanel = child.gameObject;
                            break;
                        }
                    }
                }
            }
        }
        
        if (actualPanel != null)
        {
            actualPanel.SetActive(false);
            
            // BackgroundImage始终保持激活，不在此处禁用
            if (panel != null && panel.name == "BackgroundImage" && panel != actualPanel)
            {
                // BackgroundImage保持激活，不做任何操作
            }
            
            // 如果actualPanel不是gameObject本身，且gameObject不是Canvas，才考虑禁用gameObject
            // 但永远不要禁用Canvas，因为Canvas是其他UI的根
            if (actualPanel != gameObject && !isCanvas)
            {
                // 检查是否有其他UpgradeSelectionScreen实例，如果有，只禁用当前实例
                UpgradeSelectionScreen[] allInstances = FindObjectsOfType<UpgradeSelectionScreen>();
                if (allInstances.Length > 1)
                {
                    // 有多个实例，只禁用当前实例的gameObject（但前提是它不是Canvas）
                    gameObject.SetActive(false);
                }
            }
        }
        else
        {
            // panel为null，但如果gameObject是Canvas，则不禁用它
            if (!isCanvas)
            {
                gameObject.SetActive(false);
            }
        }

        // 恢复游戏
        if (_gameManager != null)
        {
            _gameManager.ResumeGame();
        }

        Debug.Log($"选择了升级: {selectedOption.name}");
    }

    /// <summary>
    /// 加载升级选项图标
    /// </summary>
    private void LoadOptionIcon(UnityEngine.UI.Image image, string iconPath)
    {
        if (image == null)
        {
            return;
        }

        // 如果图标路径为空，隐藏图标
        if (string.IsNullOrEmpty(iconPath))
        {
            image.gameObject.SetActive(false);
            return;
        }

        // 从Resources目录加载图标
        Sprite iconSprite = Resources.Load<Sprite>(iconPath);
        
        if (iconSprite != null)
        {
            image.sprite = iconSprite;
            image.gameObject.SetActive(true);
        }
        else
        {
            Debug.LogWarning($"无法加载升级图标: {iconPath}");
            image.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// 应用动态字体到所有文本组件
    /// </summary>
    private void ApplyDynamicFont()
    {
        FontHelper.ApplyFontToGameObject(gameObject);
    }
}

/// <summary>
/// Transform扩展方法：深度查找子对象
/// </summary>
public static class TransformExtensions
{
    public static Transform FindDeepChild(this Transform parent, string name)
    {
        // 先尝试直接查找
        Transform result = parent.Find(name);
        if (result != null)
            return result;

        // 递归查找所有子对象
        foreach (Transform child in parent)
        {
            result = child.FindDeepChild(name);
            if (result != null)
                return result;
        }

        return null;
    }
}
