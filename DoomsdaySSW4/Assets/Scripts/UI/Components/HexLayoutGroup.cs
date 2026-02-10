using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// UI 六边形布局组件：在 Canvas 中将子节点（RectTransform）排布为正六边形蜂窝状。
/// 设计目标：作为 GridLayoutGroup 的“六边形版本”，用于 9x9 等规则网格的视觉排布。
/// </summary>
[AddComponentMenu("Layout/Hex Layout Group")]
public class HexLayoutGroup : LayoutGroup
{
    public enum HexOrientation
    {
        PointyTop, // 尖顶六边形
        FlatTop    // 平顶六边形
    }

    public enum HexStaggerAxis
    {
        Row,
        Column
    }

    public enum HexStaggerIndex
    {
        Even,
        Odd
    }

    [SerializeField]
    private Vector2 cellSize = new Vector2(100f, 100f);

    [SerializeField]
    private Vector2 spacing = Vector2.zero;

    [SerializeField]
    private HexOrientation orientation = HexOrientation.FlatTop;

    [SerializeField]
    private HexStaggerAxis staggerAxis = HexStaggerAxis.Row;

    [SerializeField]
    private HexStaggerIndex staggerIndex = HexStaggerIndex.Odd;

    [SerializeField]
    [Min(1)]
    private int constraintCount = 9;

    /// <summary>
    /// 单元格尺寸（包围盒大小）。
    /// </summary>
    public Vector2 CellSize
    {
        get => cellSize;
        set
        {
            if (cellSize == value) return;
            cellSize = value;
            SetDirty();
        }
    }

    /// <summary>
    /// 额外间距。
    /// </summary>
    public Vector2 Spacing
    {
        get => spacing;
        set
        {
            if (spacing == value) return;
            spacing = value;
            SetDirty();
        }
    }

    public HexOrientation Orientation
    {
        get => orientation;
        set
        {
            if (orientation == value) return;
            orientation = value;
            SetDirty();
        }
    }

    public HexStaggerAxis StaggerAxis
    {
        get => staggerAxis;
        set
        {
            if (staggerAxis == value) return;
            staggerAxis = value;
            SetDirty();
        }
    }

    public HexStaggerIndex StaggerIndex
    {
        get => staggerIndex;
        set
        {
            if (staggerIndex == value) return;
            staggerIndex = value;
            SetDirty();
        }
    }

    public int ConstraintCount
    {
        get => Mathf.Max(1, constraintCount);
        set
        {
            value = Mathf.Max(1, value);
            if (constraintCount == value) return;
            constraintCount = value;
            SetDirty();
        }
    }

    public override void CalculateLayoutInputHorizontal()
    {
        base.CalculateLayoutInputHorizontal();

        int childCount = rectChildren.Count;
        int columns, rows;
        GetGridSize(childCount, out columns, out rows);

        Vector2 requiredSize = GetRequiredSize(columns, rows);
        // 使用 LayoutGroup 提供的接口设置宽度信息
        SetLayoutInputForAxis(
            padding.horizontal + requiredSize.x,
            padding.horizontal + requiredSize.x,
            -1,
            0);
    }

    public override void CalculateLayoutInputVertical()
    {
        int childCount = rectChildren.Count;
        int columns, rows;
        GetGridSize(childCount, out columns, out rows);

        Vector2 requiredSize = GetRequiredSize(columns, rows);
        SetLayoutInputForAxis(
            padding.vertical + requiredSize.y,
            padding.vertical + requiredSize.y,
            -1,
            1);
    }

    public override void SetLayoutHorizontal()
    {
        LayoutChildren();
    }

    public override void SetLayoutVertical()
    {
        LayoutChildren();
    }

    private void GetGridSize(int childCount, out int columns, out int rows)
    {
        int constraint = ConstraintCount;
        if (childCount <= 0)
        {
            columns = 0;
            rows = 0;
            return;
        }

        columns = Mathf.Min(constraint, childCount);
        rows = Mathf.CeilToInt(childCount / (float)constraint);
    }

    /// <summary>
    /// 根据列数/行数计算整体蜂窝所需宽高。
    /// </summary>
    private Vector2 GetRequiredSize(int columns, int rows)
    {
        if (columns <= 0 || rows <= 0)
        {
            return Vector2.zero;
        }

        float w = cellSize.x;
        float h = cellSize.y;

        float horizontalStep;
        float verticalStep;

        if (orientation == HexOrientation.FlatTop)
        {
            // 平顶：横向 0.75w，纵向约 0.866h
            horizontalStep = 0.75f * w + spacing.x;
            verticalStep = 0.866f * h + spacing.y;
        }
        else
        {
            // 尖顶：横向 w，纵向 0.75h
            horizontalStep = w + spacing.x;
            verticalStep = 0.75f * h + spacing.y;
        }

        // 近似整体宽高： (cols - 1) * step + w
        float totalWidth = (columns - 1) * horizontalStep + w;
        float totalHeight = (rows - 1) * verticalStep + h;

        return new Vector2(totalWidth, totalHeight);
    }

    private void LayoutChildren()
    {
        int childCount = rectChildren.Count;
        int columns, rows;
        GetGridSize(childCount, out columns, out rows);

        if (columns <= 0 || rows <= 0)
        {
            return;
        }

        Vector2 requiredSize = GetRequiredSize(columns, rows);

        // 使用 LayoutGroup 内置的对齐计算，考虑 padding 与 childAlignment
        float startX = GetStartOffset(0, requiredSize.x);
        float startY = GetStartOffset(1, requiredSize.y);

        float w = cellSize.x;
        float h = cellSize.y;

        float flatHorizontalStep = 0.75f * w + spacing.x;
        float flatVerticalStep = 0.866f * h + spacing.y;
        float pointyHorizontalStep = w + spacing.x;
        float pointyVerticalStep = 0.75f * h + spacing.y;

        for (int i = 0; i < childCount; i++)
        {
            RectTransform child = rectChildren[i];
            if (child == null)
            {
                continue;
            }

            int row = i / ConstraintCount;
            int col = i % ConstraintCount;

            // 对于最后一行可能不足 constraintCount 的情况，仍然按完整列数计算位置，这样视觉上更规整

            float localX;
            float localY;

            if (orientation == HexOrientation.FlatTop)
            {
                ComputeFlatTopPosition(row, col, flatHorizontalStep, flatVerticalStep, out localX, out localY);
            }
            else
            {
                ComputePointyTopPosition(row, col, pointyHorizontalStep, pointyVerticalStep, out localX, out localY);
            }

            float posX = startX + localX;
            float posY = startY + localY;

            SetChildAlongAxis(child, 0, posX, w);
            SetChildAlongAxis(child, 1, posY, h);
        }
    }

    private void ComputeFlatTopPosition(int row, int col, float horizontalStep, float verticalStep, out float x, out float y)
    {
        // 以行优先排布，每行 offset 可能不同
        float rowOffsetX = 0f;
        float colOffsetY = 0f;

        if (staggerAxis == HexStaggerAxis.Row)
        {
            bool isStaggered =
                (staggerIndex == HexStaggerIndex.Even && row % 2 == 0) ||
                (staggerIndex == HexStaggerIndex.Odd && row % 2 != 0);

            if (isStaggered)
            {
                rowOffsetX = 0.5f * cellSize.x;
            }
        }
        else // Column stagger
        {
            bool isStaggered =
                (staggerIndex == HexStaggerIndex.Even && col % 2 == 0) ||
                (staggerIndex == HexStaggerIndex.Odd && col % 2 != 0);

            if (isStaggered)
            {
                colOffsetY = 0.5f * cellSize.y;
            }
        }

        x = col * horizontalStep + rowOffsetX;
        y = row * verticalStep + colOffsetY;
    }

    private void ComputePointyTopPosition(int row, int col, float horizontalStep, float verticalStep, out float x, out float y)
    {
        float rowOffsetX = 0f;
        float colOffsetY = 0f;

        if (staggerAxis == HexStaggerAxis.Column)
        {
            bool isStaggered =
                (staggerIndex == HexStaggerIndex.Even && col % 2 == 0) ||
                (staggerIndex == HexStaggerIndex.Odd && col % 2 != 0);

            if (isStaggered)
            {
                colOffsetY = 0.5f * cellSize.y;
            }
        }
        else // Row stagger
        {
            bool isStaggered =
                (staggerIndex == HexStaggerIndex.Even && row % 2 == 0) ||
                (staggerIndex == HexStaggerIndex.Odd && row % 2 != 0);

            if (isStaggered)
            {
                rowOffsetX = 0.5f * cellSize.x;
            }
        }

        x = col * horizontalStep + rowOffsetX;
        y = row * verticalStep + colOffsetY;
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        SetDirty();
    }

    protected override void OnDisable()
    {
        base.OnDisable();
    }

#if UNITY_EDITOR
    protected override void OnValidate()
    {
        base.OnValidate();
        constraintCount = Mathf.Max(1, constraintCount);
        cellSize.x = Mathf.Max(0.0f, cellSize.x);
        cellSize.y = Mathf.Max(0.0f, cellSize.y);
        SetDirty();
    }
#endif

    protected void SetDirty()
    {
        if (!IsActive())
        {
            return;
        }

        LayoutRebuilder.MarkLayoutForRebuild(rectTransform);
    }
}

