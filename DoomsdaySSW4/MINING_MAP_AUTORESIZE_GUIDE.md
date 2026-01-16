# 挖矿地图自适应大小设置指南

## 功能说明

MiningMapView组件现在支持自动调整矿石格子大小，使所有格子的总长宽与LeftPanel（或MiningMapContainer）的总长宽保持一致。

## 工作原理

- **自动计算**：根据容器大小、格子数量（9列9行）和间距，自动计算每个格子的最佳大小
- **实时响应**：当容器大小改变时，自动重新计算格子大小
- **灵活配置**：可以选择使用父容器（LeftPanel）或当前容器（MiningMapContainer）的大小

## 设置步骤

### 步骤1: 确保MiningMapContainer填充LeftPanel

1. 在Hierarchy中选中 `MiningMapContainer`
2. 在Inspector的Rect Transform中：
   - 点击左上角的锚点图标
   - 选择 **"stretch-stretch"**（四个角拉伸）
   - 或手动设置：
     - Left: `0`
     - Right: `0`
     - Top: `0`
     - Bottom: `0`

这样MiningMapContainer会完全填充LeftPanel。

### 步骤2: 配置MiningMapView组件

1. 选中 `MiningMapContainer`
2. 在Inspector中找到 `MiningMapView` 组件
3. 在 **自适应设置** 部分：
   - **Auto Resize**: ✅ 勾选（启用自适应大小）
   - **Use Parent Size**: ✅ 勾选（使用LeftPanel的大小，推荐）
   - **Spacing**: `X: 5, Y: 5`（格子之间的间距，可根据需要调整）

### 步骤3: 验证设置

1. 运行游戏
2. 检查挖矿地图是否填满LeftPanel
3. 调整LeftPanel的大小，观察格子是否自动调整

## 配置选项说明

### Auto Resize（自动调整大小）
- **启用**：格子大小会根据容器自动计算
- **禁用**：使用固定的格子大小（默认60x60）

### Use Parent Size（使用父容器大小）
- **启用**：使用LeftPanel的大小进行计算（推荐）
- **禁用**：使用MiningMapContainer自身的大小进行计算

### Spacing（间距）
- **X**: 格子之间的水平间距（像素）
- **Y**: 格子之间的垂直间距（像素）
- 默认值：`5, 5`

## 计算公式

格子大小的计算公式：

```
可用宽度 = 容器宽度 - (左padding + 右padding)
可用高度 = 容器高度 - (上padding + 下padding)

间距总宽度 = 间距X × (列数 - 1)
间距总高度 = 间距Y × (行数 - 1)

格子宽度 = (可用宽度 - 间距总宽度) / 列数
格子高度 = (可用高度 - 间距总高度) / 行数
```

其中：
- 列数 = 9（LAYER_WIDTH）
- 行数 = 9（LAYER_HEIGHT）

## 注意事项

1. **RectTransform设置**：确保MiningMapContainer的RectTransform设置为填充父容器
2. **GridLayoutGroup Padding**：如果GridLayoutGroup设置了padding，会在计算时考虑
3. **容器大小**：确保LeftPanel或MiningMapContainer有明确的大小（不能为0）
4. **运行时调整**：如果容器大小在运行时改变，格子大小会自动重新计算

## 常见问题

### Q: 格子显示不正确或溢出
**A:** 
- 检查MiningMapContainer的RectTransform是否设置为填充父容器
- 检查LeftPanel是否有明确的大小
- 查看Console是否有警告信息

### Q: 格子太小或太大
**A:**
- 调整LeftPanel的大小
- 调整Spacing值（减小间距可以增大格子）
- 检查GridLayoutGroup的Padding设置

### Q: 自适应不工作
**A:**
- 确保Auto Resize已勾选
- 检查MiningMapView组件是否正确添加到MiningMapContainer
- 查看Console是否有错误信息

### Q: 想使用固定大小
**A:**
- 取消勾选Auto Resize
- 格子大小将使用默认的60x60（或手动设置的值）

## 示例配置

### 推荐配置（自适应LeftPanel）
```
MiningMapContainer:
  RectTransform: 填充父容器（stretch-stretch）
  
MiningMapView组件:
  Auto Resize: ✅ 勾选
  Use Parent Size: ✅ 勾选
  Spacing: X=5, Y=5
```

### 固定大小配置
```
MiningMapView组件:
  Auto Resize: ❌ 不勾选
  GridLayoutGroup:
    Cell Size: X=60, Y=60
```

## 技术细节

- **计算时机**：
  - Start()：游戏开始时计算
  - OnRectTransformDimensionsChange()：容器大小改变时重新计算
  - UpdateMap()：更新地图时重新计算（确保大小是最新的）

- **使用的容器**：
  - 如果Use Parent Size启用：使用LeftPanel的RectTransform
  - 如果Use Parent Size禁用：使用MiningMapContainer自身的RectTransform

- **Padding处理**：
  - GridLayoutGroup的padding会在计算时自动考虑
  - 可用空间 = 容器大小 - padding
