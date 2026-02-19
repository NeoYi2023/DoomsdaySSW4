using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 挖矿地图静态格子生成器：在 MiningMapContainer 下创建 MapGridRoot 及静态 MiningMapCell 子节点（row-major），与 PlatformGrid 排版一致。
/// 根据当前逻辑网格尺寸（MiningManager.LAYER_WIDTH × MiningManager.LAYER_HEIGHT）生成对应数量的逻辑格子，美术可在此基础上手动补充外圈装饰格子。
/// </summary>
public static class MiningMapGridEditor
{
    private const string MapGridRootName = "MapGridRoot";

    [MenuItem("Tools/DoomsdaySSW4/Create Mining Map Static Grid (MapGridRoot + logic cells)")]
    public static void CreateMiningMapStaticGrid()
    {
        MiningMapView miningMapView = Object.FindObjectOfType<MiningMapView>(true);
        if (miningMapView == null)
        {
            EditorUtility.DisplayDialog("未找到 MiningMapView", "请在场景中先放置挂有 MiningMapView 的 MiningMapContainer。", "确定");
            return;
        }

        Transform container = miningMapView.transform;
        Transform existing = container.Find(MapGridRootName);
        if (existing != null)
        {
            if (!EditorUtility.DisplayDialog("已存在 MapGridRoot", "MapGridRoot 已存在，是否删除并重新生成？", "重新生成", "取消"))
                return;
            Undo.DestroyObjectImmediate(existing.gameObject);
        }

        // 创建 MapGridRoot
        GameObject rootGo = new GameObject(MapGridRootName);
        Undo.RegisterCreatedObjectUndo(rootGo, "Create MapGridRoot");
        rootGo.transform.SetParent(container, false);

        RectTransform rootRect = rootGo.AddComponent<RectTransform>();
        rootRect.anchorMin = new Vector2(0.5f, 0.5f);
        rootRect.anchorMax = new Vector2(0.5f, 0.5f);
        rootRect.pivot = new Vector2(0.5f, 0.5f);
        rootRect.anchoredPosition = Vector2.zero;
        rootRect.sizeDelta = new Vector2(900, 900);

        HexLayoutGroup hexLayout = rootGo.AddComponent<HexLayoutGroup>();
        hexLayout.Orientation = HexLayoutGroup.HexOrientation.FlatTop;
        hexLayout.StaggerAxis = HexLayoutGroup.HexStaggerAxis.Row;
        hexLayout.StaggerIndex = HexLayoutGroup.HexStaggerIndex.Odd;
        // 逻辑网格宽度由 MiningManager.LAYER_WIDTH 决定（默认 9）；多余格子可由美术补充为外圈装饰格子
        hexLayout.ConstraintCountEven = MiningManager.LAYER_WIDTH;
        hexLayout.ConstraintCountOdd = MiningManager.LAYER_WIDTH;
        hexLayout.CellSize = new Vector2(51, 51);
        hexLayout.Spacing = new Vector2(2, 2);
        hexLayout.StartCorner = HexLayoutGroup.HexLayoutStartCorner.BottomLeft;

        int logicWidth = MiningManager.LAYER_WIDTH;
        int logicHeight = MiningManager.LAYER_HEIGHT;

        // 创建逻辑格子，row-major: (0,0)..(logicWidth-1,0),(0,1)..(logicWidth-1,logicHeight-1)
        for (int row = 0; row < logicHeight; row++)
        {
            for (int col = 0; col < logicWidth; col++)
            {
                int x = col;
                int y = row;
                GameObject cellGo = new GameObject($"MiningMapCell_{x}_{y}");
                Undo.RegisterCreatedObjectUndo(cellGo, "Create MiningMapCell");
                cellGo.transform.SetParent(rootGo.transform, false);

                RectTransform cellRect = cellGo.AddComponent<RectTransform>();
                cellRect.anchorMin = new Vector2(0, 0);
                cellRect.anchorMax = new Vector2(0, 0);
                cellRect.pivot = new Vector2(0.5f, 0.5f);
                cellRect.anchoredPosition = Vector2.zero;
                cellRect.sizeDelta = Vector2.zero;

                Image img = cellGo.AddComponent<Image>();
                img.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);

                MiningMapCell cell = cellGo.AddComponent<MiningMapCell>();
                cell.x = x;
                cell.y = y;

                // 子节点：硬度文本
                GameObject textGo = new GameObject("HardnessText");
                Undo.RegisterCreatedObjectUndo(textGo, "Create HardnessText");
                textGo.transform.SetParent(cellGo.transform, false);
                RectTransform textRect = textGo.AddComponent<RectTransform>();
                textRect.anchorMin = Vector2.zero;
                textRect.anchorMax = Vector2.one;
                textRect.offsetMin = Vector2.zero;
                textRect.offsetMax = Vector2.zero;
                TextMeshProUGUI tmp = textGo.AddComponent<TextMeshProUGUI>();
                tmp.text = "";
                tmp.fontSize = 14;
                tmp.alignment = TextAlignmentOptions.Center;
                tmp.color = Color.white;
            }
        }

        // 绑定 MiningMapView.mapGridRoot 并启用 useStaticCells
        SerializedObject so = new SerializedObject(miningMapView);
        SerializedProperty mapGridRootProp = so.FindProperty("mapGridRoot");
        SerializedProperty useStaticCellsProp = so.FindProperty("useStaticCells");
        if (mapGridRootProp != null)
            mapGridRootProp.objectReferenceValue = rootRect;
        if (useStaticCellsProp != null)
            useStaticCellsProp.boolValue = true;
        so.ApplyModifiedPropertiesWithoutUndo();

        int logicCount = logicWidth * logicHeight;
        EditorUtility.DisplayDialog("完成", $"已创建 MapGridRoot 及 {logicCount} 个逻辑 MiningMapCell 子节点（row-major），并已绑定 MiningMapView.mapGridRoot、启用 useStaticCells。\n\n可在 MapGridRoot 上调整 HexLayoutGroup 的 CellSize/Spacing 与 PlatformGrid 的 GridRoot 一致，然后按需在外围补充装饰格子以满足美术需求。", "确定");
    }
}
