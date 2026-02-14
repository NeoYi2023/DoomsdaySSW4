using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// UI 六边形布局组件：在 Canvas 中将子节点（RectTransform）排布为正六边形蜂窝状。
/// 设计目标：作为 GridLayoutGroup 的“六边形版本”，用于规则逻辑网格（例如默认 9x9，可扩展）的视觉排布。
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

    /// <summary>
    /// 布局起点：TopLeft 为左上角（row 0 在上），BottomLeft 为左下角（row 0 在下）。
    /// </summary>
    public enum HexLayoutStartCorner
    {
        TopLeft,
        BottomLeft
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
    private int constraintCountEven = 9;

    [SerializeField]
    [Min(1)]
    private int constraintCountOdd = 9;

    [SerializeField]
    private HexLayoutStartCorner startCorner = HexLayoutStartCorner.TopLeft;

    /// <summary>
    /// 布局起点：TopLeft 左上角，BottomLeft 左下角（row 0 在底部）。
    /// </summary>
    public HexLayoutStartCorner StartCorner
    {
        get => startCorner;
        set
        {
            if (startCorner == value) return;
            startCorner = value;
            SetDirty();
        }
    }

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

    /// <summary>
    /// 偶数行（row 0, 2, 4, …）每行格子数。
    /// </summary>
    public int ConstraintCountEven
    {
        get => Mathf.Max(1, constraintCountEven);
        set
        {
            value = Mathf.Max(1, value);
            if (constraintCountEven == value) return;
            constraintCountEven = value;
            SetDirty();
        }
    }

    /// <summary>
    /// 奇数行（row 1, 3, 5, …）每行格子数。
    /// </summary>
    public int ConstraintCountOdd
    {
        get => Mathf.Max(1, constraintCountOdd);
        set
        {
            value = Mathf.Max(1, value);
            if (constraintCountOdd == value) return;
            constraintCountOdd = value;
            SetDirty();
        }
    }

    /// <summary>
    /// 兼容：返回偶数行与奇数行数量的较大值。
    /// </summary>
    public int ConstraintCount
    {
        get => Mathf.Max(ConstraintCountEven, ConstraintCountOdd);
        set
        {
            value = Mathf.Max(1, value);
            constraintCountEven = value;
            constraintCountOdd = value;
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

    /// <summary>
    /// 根据子节点索引 i 得到所在行、列（行优先，偶数行/奇数行数量可不同）。
    /// </summary>
    private void GetRowColFromIndex(int i, out int row, out int col)
    {
        int countEven = ConstraintCountEven;
        int countOdd = ConstraintCountOdd;
        int index = 0;
        for (row = 0; row < 1000; row++)
        {
            int countThisRow = (row % 2 == 0) ? countEven : countOdd;
            if (i < index + countThisRow)
            {
                col = i - index;
                return;
            }
            index += countThisRow;
        }
        row = 0;
        col = 0;
    }

    /// <summary>
    /// 根据子节点总数计算行数与最大列数（用于尺寸计算）。
    /// </summary>
    private void GetGridSize(int childCount, out int columns, out int rows)
    {
        int countEven = ConstraintCountEven;
        int countOdd = ConstraintCountOdd;
        if (childCount <= 0)
        {
            columns = 0;
            rows = 0;
            return;
        }

        columns = Mathf.Max(countEven, countOdd);
        int index = 0;
        rows = 0;
        for (int r = 0; index < childCount; r++)
        {
            int countThisRow = (r % 2 == 0) ? countEven : countOdd;
            index += countThisRow;
            rows = r + 1;
        }
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

        // 整体宽高 = (cols - 1) * step + cellSize
        float totalWidth = (columns - 1) * horizontalStep + w;
        float totalHeight = (rows - 1) * verticalStep + h;

        // stagger 偏移：奇数行/列会额外偏移 0.5 * step，需加到对应维度
        if (rows > 1)
        {
            if (orientation == HexOrientation.FlatTop && staggerAxis == HexStaggerAxis.Row)
                totalWidth += 0.5f * horizontalStep;
            else if (orientation == HexOrientation.FlatTop && staggerAxis == HexStaggerAxis.Column)
                totalHeight += 0.5f * verticalStep;
            else if (orientation == HexOrientation.PointyTop && staggerAxis == HexStaggerAxis.Row)
                totalWidth += 0.5f * horizontalStep;
            else if (orientation == HexOrientation.PointyTop && staggerAxis == HexStaggerAxis.Column)
                totalHeight += 0.5f * verticalStep;
        }

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

            GetRowColFromIndex(i, out int row, out int col);

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

            if (startCorner == HexLayoutStartCorner.BottomLeft)
            {
                float verticalStep = orientation == HexOrientation.FlatTop ? flatVerticalStep : pointyVerticalStep;
                localY = (rows - 1) * verticalStep - localY;
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
                rowOffsetX = 0.5f * horizontalStep;
            }
        }
        else // Column stagger
        {
            bool isStaggered =
                (staggerIndex == HexStaggerIndex.Even && col % 2 == 0) ||
                (staggerIndex == HexStaggerIndex.Odd && col % 2 != 0);

            if (isStaggered)
            {
                colOffsetY = 0.5f * verticalStep;
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
                colOffsetY = 0.5f * verticalStep;
            }
        }
        else // Row stagger
        {
            bool isStaggered =
                (staggerIndex == HexStaggerIndex.Even && row % 2 == 0) ||
                (staggerIndex == HexStaggerIndex.Odd && row % 2 != 0);

            if (isStaggered)
            {
                rowOffsetX = 0.5f * horizontalStep;
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
        constraintCountEven = Mathf.Max(1, constraintCountEven);
        constraintCountOdd = Mathf.Max(1, constraintCountOdd);
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

