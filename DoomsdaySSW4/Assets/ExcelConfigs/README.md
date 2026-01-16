# Excel配置表转JSON工具使用说明

## 概述

这个工具可以将Excel配置表（或CSV格式）转换为Unity游戏使用的JSON配置文件。

## 文件命名格式

Excel文件必须使用以下命名格式：
```
配置表名_中文备注字段.xlsx
或
配置表名_中文备注字段.csv
```

支持的配置表名：
- `TaskConfigs` - 任务配置表
- `OreConfigs` - 矿石配置表
- `OreSpawnConfigs` - 矿石生成规则配置表
- `DrillConfigs` - 钻头配置表
- `ShipConfigs` - 船只配置表
- `EnergyUpgradeConfigs` - 能源升级配置表

示例文件名：
- `TaskConfigs_任务配置表.xlsx`
- `OreConfigs_矿石配置表.csv`
- `OreSpawnConfigs_矿石生成规则配置表.xlsx`

## Excel文件格式要求

### 1. TaskConfigs_任务配置表.xlsx

| taskId | taskName | taskType | maxTurns | targetDebtAmount | nextTaskId | description |
|--------|----------|----------|----------|------------------|------------|-------------|
| task_001 | 第一个任务 | Basic | 10 | 1000 | task_002 | 在10回合内偿还1000债务 |

**字段说明：**
- `taskId`: 任务ID（字符串）
- `taskName`: 任务名称（字符串）
- `taskType`: 任务类型（Basic 或 Advanced）
- `maxTurns`: 最大回合数（整数）
- `targetDebtAmount`: 目标债务金额（整数）
- `nextTaskId`: 下一个任务ID（字符串，可为空）
- `description`: 任务描述（字符串）

### 2. OreConfigs_矿石配置表.xlsx

| oreId | oreName | oreType | hardness | requiredAttributeKey | requiredAttributeValue | value | isEnergyOre | energyValue | minDepth | maxDepth | spawnProbability |
|-------|---------|---------|----------|----------------------|------------------------|-------|-------------|-------------|----------|----------|------------------|
| iron | 铁矿石 | Common | 10 | | 0 | 50 | false | 0 | 1 | 5 | 0.0 |

**字段说明：**
- `oreId`: 矿石ID（字符串）
- `oreName`: 矿石名称（字符串）
- `oreType`: 矿石类型（Common, Rare, Energy, Special）
- `hardness`: 硬度值（整数）
- `requiredAttributeKey`: 所需属性键（字符串，可为空）
- `requiredAttributeValue`: 所需属性值（整数）
- `value`: 价值（整数）
- `isEnergyOre`: 是否为能源矿石（true/false 或 1/0）
- `energyValue`: 能源值（整数）
- `minDepth`: 最小深度（整数）
- `maxDepth`: 最大深度（整数）
- `spawnProbability`: 生成概率（浮点数）

### 3. OreSpawnConfigs_矿石生成规则配置表.xlsx

| layerDepth | oreId | weight | maxCount | spawnProbability |
|------------|-------|--------|----------|-------------------|
| 1 | iron | 75 | 35 | 0.0 |
| 1 | energy_core | 25 | 8 | 0.0 |
| 2 | iron | 55 | 30 | 0.0 |

**字段说明：**
- `layerDepth`: 层数（整数）
- `oreId`: 矿石ID（字符串）
- `weight`: 权重（整数）
- `maxCount`: 最大数量（整数）
- `spawnProbability`: 生成概率（浮点数）

**注意：** 同一`layerDepth`的多行会被合并为一个配置对象。

### 4. DrillConfigs_钻头配置表.xlsx

| drillId | drillName | miningStrength | miningRangeX | miningRangeY | description |
|---------|-----------|----------------|--------------|--------------|-------------|
| default_drill | 默认钻头 | 10 | 5 | 5 | 默认钻头，攻击值10，范围5x5 |

**字段说明：**
- `drillId`: 钻头ID（字符串）
- `drillName`: 钻头名称（字符串）
- `miningStrength`: 挖掘强度（整数）
- `miningRangeX`: 挖掘范围X（整数）
- `miningRangeY`: 挖掘范围Y（整数）
- `description`: 描述（字符串）

### 5. ShipConfigs_船只配置表.xlsx

| shipId | shipName | initialDebt | description |
|--------|----------|-------------|-------------|
| default_ship | 默认挖矿船 | 3000 | 起始债务3000的默认挖矿船 |

**字段说明：**
- `shipId`: 船只ID（字符串）
- `shipName`: 船只名称（字符串）
- `initialDebt`: 初始债务（整数）
- `description`: 描述（字符串）

### 6. EnergyUpgradeConfigs_能源升级配置表.xlsx

| upgradeId | type | name | description | value | weight | iconPath |
|-----------|------|------|-------------|-------|--------|----------|
| drill_strength_1 | DrillStrength | 挖掘强度提升 I | 钻头攻击值 +5 | 5 | 30 | Icons/Upgrades/drill_strength_1 |

**字段说明：**
- `upgradeId`: 升级ID（字符串）
- `type`: 升级类型（字符串，如 DrillStrength, DrillRange, MiningEfficiency, OreValueBoost, OreDiscovery）
- `name`: 升级名称（字符串）
- `description`: 升级描述（字符串）
- `value`: 升级数值（整数）
- `weight`: 权重（整数，用于随机选择）
- `iconPath`: 图标路径（字符串，相对于Resources目录，如 "Icons/Upgrades/drill_strength_1"，可为空）

### 7. EnergyThresholds_能源阈值配置表.xlsx

| shipId | energyThresholds |
|--------|------------------|
| default_ship | 50,100,150 |
| ship_2 | 60,120,180 |

**字段说明：**
- `shipId`: 船只ID（字符串，必须与ShipConfigs中的shipId匹配）
- `energyThresholds`: 能源阈值数组（字符串，使用逗号分隔的整数，如 "50,100,150"）

**注意：** 
- 每个船只可以有独立的能源阈值配置
- 当能源值达到阈值时，会触发三选一升级选择
- 如果某个船只没有配置，会使用默认阈值 [50, 100, 150]
- `energyThresholds`字段包含逗号，在CSV中必须用引号括起来，如 `"50,100,150"`（工具会自动处理未加引号的情况）

## 使用步骤

### 方法1：使用Excel文件（需要导出为CSV）

1. 在Excel中创建配置表，按照上述格式填写数据
2. 将Excel文件另存为CSV格式（UTF-8编码）
3. 将CSV文件放到 `Assets/ExcelConfigs/` 目录
4. 在Unity Editor菜单栏选择 `Tools -> 配置表 -> Excel转JSON`
5. 工具会自动转换所有CSV文件并保存到 `Assets/Resources/Configs/`

### 方法2：直接使用CSV文件

1. 在Excel中创建配置表，按照上述格式填写数据
2. 将Excel文件另存为CSV格式（UTF-8编码）
3. 将CSV文件重命名为：`配置表名_中文备注字段.csv`
4. 将CSV文件放到 `Assets/ExcelConfigs/` 目录
5. 在Unity Editor菜单栏选择 `Tools -> 配置表 -> Excel转JSON`

## 注意事项

1. **文件编码**：CSV文件必须使用UTF-8编码，否则中文可能显示为乱码
2. **表头要求**：Excel/CSV第一行必须是表头（字段名），字段名必须与C#类属性名完全匹配（区分大小写）
3. **数据类型**：
   - 整数：直接填写数字
   - 浮点数：使用小数点，如 `0.0`
   - 布尔值：填写 `true`/`false` 或 `1`/`0` 或 `yes`/`no`
   - 枚举值：必须与枚举定义匹配（如 `Basic`, `Common`, `Rare` 等）
4. **空值处理**：空单元格会被转换为空字符串或默认值（0, false等）
5. **OreSpawnConfigs特殊处理**：同一`layerDepth`的多行会自动合并为一个配置对象

## 常见问题

### Q: 转换后JSON文件为空
**A:** 检查：
- CSV文件编码是否为UTF-8
- 表头字段名是否与C#类属性名完全匹配
- 数据行是否有格式错误

### Q: 枚举值转换失败
**A:** 检查：
- 枚举值是否与定义匹配（区分大小写）
- 是否有多余的空格

### Q: 布尔值转换错误
**A:** 确保布尔值字段填写为：`true`/`false` 或 `1`/`0` 或 `yes`/`no`

### Q: OreSpawnConfigs分组错误
**A:** 确保`layerDepth`列的值是整数，同一层的所有规则行必须具有相同的`layerDepth`值

### Q: EnergyThresholds转换错误
**A:** 检查：
- `energyThresholds`列的值必须是逗号分隔的整数（如 "50,100,150"）
- `shipId`必须与ShipConfigs中的船只ID匹配
- 确保没有多余的空格

## 输出文件位置

转换后的JSON文件会保存到：
```
Assets/Resources/Configs/
```

文件名格式：
- `TaskConfigs.json`
- `OreConfigs.json`
- `OreSpawnConfigs.json`
- `DrillConfigs.json`
- `ShipConfigs.json`
- `EnergyUpgradeConfigs.json`
- `EnergyThresholds.json`
