# 需求文档 — 万代国 Phase 0 核心原型

## 简介

本文档定义"万代国"（多纪元文明防守肉鸽）Phase 0 核心原型的开发需求。Phase 0 的唯一目标是**跑通单局最小循环**，验证"经营构筑型塔防肉鸽"的核心玩法是否成立。

核心原型需要完成：地图网格、建筑放置、敌人寻路、波次刷怪、核心生命值、资源消耗、波后奖励三选一。

技术栈：Unity 6.3 LTS + C#  
视角：俯视或轻斜俯视 2.5D 战略视角  
范围：单人模式，10 波 + 2 Boss，村落级与城镇级

## 术语表

- **Game_Core**：游戏核心系统，负责管理整局游戏的生命周期与状态流转
- **Grid_Map**：网格地图系统，管理游戏地图的网格数据、可建造区域与路径信息
- **Building_System**：建筑系统，负责建筑的放置、升级、移除与效果计算
- **Enemy_System**：敌人系统，负责敌人的生成、寻路、移动与行为逻辑
- **Wave_System**：波次系统，负责管理波次推进、敌人生成配置与波次状态
- **Combat_System**：战斗结算系统，负责伤害计算、目标选择与战斗效果处理
- **Resource_System**：资源系统，负责资源的获取、消耗与余额管理
- **Reward_System**：奖励系统，负责波后奖励的生成与玩家选择处理
- **Pathfinding_Module**：寻路模块，负责计算敌人从出生点到目标点的最优路径
- **Config_System**：配置系统，负责加载和管理所有游戏数据配置
- **Core_Health**：核心生命值，代表玩家文明核心的存活状态
- **Building**：建筑实体，玩家放置在网格上的防御或功能性设施
- **Enemy**：敌人实体，沿路径向玩家核心移动的威胁单位
- **Wave**：波次，一轮敌人进攻的完整周期
- **Reward_Card**：奖励卡，波后三选一中的单个可选奖励项

## 需求

### 需求 1：网格地图系统

**用户故事：** 作为玩家，我希望在一个清晰的网格地图上进行游戏，以便我能直观地规划防线布局和理解敌人行进路径。

#### 验收标准

1. THE Grid_Map SHALL 以矩形网格形式呈现游戏地图，最小支持 20x15 的网格尺寸
2. THE Grid_Map SHALL 为每个网格单元维护状态信息，包括：可建造、不可建造、已占用、路径节点
3. WHEN 游戏局开始时，THE Grid_Map SHALL 根据关卡配置数据生成地图布局，包含至少一个敌人出生点和一个核心防守点
4. THE Grid_Map SHALL 明确标识敌人出生点与核心防守点的位置，使玩家能够识别防守方向
5. WHEN 建筑被放置或移除时，THE Grid_Map SHALL 在同一帧内更新对应网格单元的状态
6. THE Grid_Map SHALL 从外部配置文件加载地图布局数据，地图定义不硬编码在程序逻辑中

### 需求 2：建筑放置系统

**用户故事：** 作为玩家，我希望能在准备阶段将防御建筑放置到网格上，以便我能构建防线抵御敌人进攻。

#### 验收标准

1. WHILE 处于准备阶段，THE Building_System SHALL 允许玩家在可建造的网格单元上放置建筑
2. WHEN 玩家尝试在不可建造或已占用的网格单元上放置建筑时，THE Building_System SHALL 拒绝该操作并向玩家提供视觉反馈
3. WHEN 玩家放置建筑时，THE Building_System SHALL 扣除对应的资源消耗，资源不足时拒绝放置
4. WHEN 建筑被成功放置后，THE Building_System SHALL 通知 Grid_Map 更新网格状态，并通知 Pathfinding_Module 重新计算路径
5. IF 放置建筑会导致敌人出生点到核心防守点之间不存在有效路径，THEN THE Building_System SHALL 拒绝该放置操作
6. THE Building_System SHALL 支持至少 4 种不同类型的防御建筑，每种建筑具有独立的攻击范围、伤害值、攻击间隔与资源消耗配置
7. THE Building_System SHALL 从 ScriptableObject 配置中读取建筑属性定义，建筑属性不硬编码在程序中

### 需求 3：敌人寻路系统

**用户故事：** 作为玩家，我希望敌人能沿合理路径向我的核心移动，以便我能根据敌人行进方向策略性地布置防线。

#### 验收标准

1. THE Pathfinding_Module SHALL 为每个敌人计算从其出生点到核心防守点的最短有效路径
2. WHEN Grid_Map 的可通行状态发生变化时，THE Pathfinding_Module SHALL 重新计算所有受影响敌人的路径
3. THE Enemy_System SHALL 使敌人沿 Pathfinding_Module 计算出的路径持续移动，移动速度由敌人配置定义
4. IF 不存在从出生点到核心防守点的有效路径，THEN THE Pathfinding_Module SHALL 返回路径不可达状态
5. THE Pathfinding_Module SHALL 在 50 个敌人同时存在的情况下，单次路径计算耗时不超过 16 毫秒（保证 60 FPS）

### 需求 4：波次刷怪系统

**用户故事：** 作为玩家，我希望敌人以波次形式进攻，以便我能在波次间隙进行建设和策略调整。

#### 验收标准

1. THE Wave_System SHALL 按照波次配置数据依序推进波次，Phase 0 支持 10 个普通波次和 2 个 Boss 波次
2. WHEN 一个波次开始时，THE Wave_System SHALL 根据该波次的配置数据生成对应类型和数量的敌人
3. WHEN 当前波次的所有敌人被消灭或到达核心后，THE Wave_System SHALL 将游戏状态切换为准备阶段
4. THE Wave_System SHALL 支持在单个波次内分批次生成敌人，每批次之间存在可配置的时间间隔
5. WHEN 第 5 波和第 10 波开始时，THE Wave_System SHALL 生成 Boss 级敌人，Boss 具有独立的行为配置
6. THE Wave_System SHALL 从外部配置文件加载波次定义，包括每波的敌人类型、数量、生成间隔与生成位置

### 需求 5：核心生命值系统

**用户故事：** 作为玩家，我希望有一个明确的胜负条件，以便我能理解防守的紧迫性并做出合理决策。

#### 验收标准

1. THE Game_Core SHALL 维护一个核心生命值，初始值由配置定义
2. WHEN 敌人到达核心防守点时，THE Game_Core SHALL 扣除该敌人对应的伤害值，并移除该敌人
3. IF 核心生命值降至零或以下，THEN THE Game_Core SHALL 立即结束当前游戏局并显示失败结果
4. WHEN 所有波次完成且核心生命值大于零时，THE Game_Core SHALL 结束当前游戏局并显示胜利结果
5. THE Game_Core SHALL 在游戏界面中持续显示当前核心生命值与最大生命值

### 需求 6：资源系统

**用户故事：** 作为玩家，我希望通过消灭敌人获取资源，以便我能用资源建造更多防御设施来应对后续波次。

#### 验收标准

1. THE Resource_System SHALL 管理至少一种基础资源类型，玩家开局获得由配置定义的初始资源量
2. WHEN 敌人被消灭时，THE Resource_System SHALL 增加该敌人配置中定义的资源掉落量
3. WHEN 玩家放置建筑时，THE Resource_System SHALL 扣除该建筑配置中定义的资源消耗量
4. IF 玩家当前资源不足以支付建筑消耗，THEN THE Resource_System SHALL 阻止该建筑的放置操作
5. THE Resource_System SHALL 在游戏界面中实时显示玩家当前资源余额
6. WHEN 每个波次结束时，THE Resource_System SHALL 根据波次配置给予玩家额外的波次结算资源奖励

### 需求 7：波后奖励三选一系统

**用户故事：** 作为玩家，我希望在每波结束后从三个随机奖励中选择一个，以便我能根据当前局势构筑独特的防御策略。

#### 验收标准

1. WHEN 一个波次结束且进入准备阶段时，THE Reward_System SHALL 从奖励池中随机抽取三张不重复的 Reward_Card 供玩家选择
2. THE Reward_System SHALL 支持至少以下奖励类型：新建筑蓝图解锁、资源奖励、已有建筑属性强化
3. WHEN 玩家选择一张 Reward_Card 后，THE Reward_System SHALL 立即应用该奖励效果并关闭选择界面
4. THE Reward_System SHALL 确保同一次三选一中不出现重复的奖励项
5. THE Reward_System SHALL 从外部配置加载奖励池定义，包括每个奖励的类型、效果参数与权重
6. WHILE 奖励选择界面处于打开状态，THE Game_Core SHALL 暂停波次推进计时

### 需求 8：战斗结算系统

**用户故事：** 作为玩家，我希望建筑能自动攻击范围内的敌人，以便我能专注于策略布局而非逐个操控攻击。

#### 验收标准

1. THE Combat_System SHALL 使每个防御建筑按照其配置的攻击间隔自动选择攻击范围内的敌人进行攻击
2. WHEN 建筑攻击命中敌人时，THE Combat_System SHALL 扣除敌人生命值，扣除量等于建筑配置的伤害值
3. WHEN 敌人生命值降至零或以下时，THE Combat_System SHALL 移除该敌人并触发资源掉落
4. THE Combat_System SHALL 支持至少两种目标选择策略：最近优先、生命值最低优先
5. THE Combat_System SHALL 为每次攻击提供基础视觉反馈（弹道或攻击特效）

### 需求 9：游戏流程管理

**用户故事：** 作为玩家，我希望游戏有清晰的阶段流转，以便我能理解当前处于什么状态并做出相应操作。

#### 验收标准

1. THE Game_Core SHALL 管理以下游戏状态的流转：初始化 → 准备阶段 → 战斗阶段 → 结算阶段 → 奖励选择 → 准备阶段（循环）→ 游戏结束
2. WHEN 游戏状态发生切换时，THE Game_Core SHALL 通知所有相关系统执行对应的状态转换逻辑
3. WHILE 处于准备阶段，THE Game_Core SHALL 允许玩家进行建筑放置操作，并在玩家确认后切换到战斗阶段
4. WHILE 处于战斗阶段，THE Game_Core SHALL 禁止玩家进行建筑放置操作
5. THE Game_Core SHALL 在界面中显示当前波次编号与游戏阶段状态

### 需求 10：配置驱动架构

**用户故事：** 作为开发者，我希望所有核心游戏数据通过配置文件定义，以便我能快速迭代平衡性调整而无需修改代码。

#### 验收标准

1. THE Config_System SHALL 支持从 ScriptableObject 加载建筑定义，包括：名称、攻击力、攻击范围、攻击间隔、资源消耗、占用网格大小
2. THE Config_System SHALL 支持从 ScriptableObject 加载敌人定义，包括：名称、生命值、移动速度、对核心伤害值、资源掉落量
3. THE Config_System SHALL 支持从 ScriptableObject 加载波次定义，包括：波次编号、敌人类型列表、每种敌人数量、生成间隔、是否为 Boss 波
4. THE Config_System SHALL 支持从 ScriptableObject 加载奖励池定义，包括：奖励类型、效果参数、抽取权重
5. THE Config_System SHALL 支持从 ScriptableObject 加载地图布局定义，包括：网格尺寸、可建造区域、敌人出生点、核心防守点位置
6. WHEN 配置数据存在格式错误或缺失必要字段时，THE Config_System SHALL 在 Unity Console 中输出明确的错误信息并阻止游戏启动

### 需求 11：表现层与逻辑层分离

**用户故事：** 作为开发者，我希望游戏逻辑与视觉表现解耦，以便我能独立测试核心逻辑并为后续多人同步预留架构空间。

#### 验收标准

1. THE Game_Core SHALL 将战斗逻辑、资源计算、状态管理实现为不依赖 Unity MonoBehaviour 生命周期的纯 C# 类
2. THE Game_Core SHALL 通过事件或消息机制将逻辑层状态变化通知表现层，表现层不直接修改逻辑层数据
3. THE Building_System SHALL 将建筑的逻辑数据（攻击力、范围、冷却）与视觉表现（模型、动画、特效）分离为独立组件
4. THE Enemy_System SHALL 将敌人的逻辑状态（位置、生命值、路径）与视觉表现（模型、动画）分离为独立组件
5. THE Game_Core SHALL 支持在无渲染环境下运行核心游戏逻辑（用于单元测试）
