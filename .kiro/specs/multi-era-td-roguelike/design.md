# 技术设计文档 -- 万代国 Phase 0 核心原型

## 1. 概述

本文档定义 Phase 0 核心原型的技术架构、模块划分、类设计与数据流。目标是以最小可行范围跑通单局循环：网格地图、建筑放置、敌人寻路、波次刷怪、核心生命值、资源消耗、波后奖励三选一。

- **技术栈**：Unity 6.3 LTS + C#
- **视角**：俯视 2.5D 战略视角
- **范围**：单人模式，10 波 + 2 Boss，村落级与城镇级
- **架构原则**：表现层与逻辑层分离、配置驱动、事件通信、可测试

---

## 2. 系统架构总览

### 2.1 分层架构

```
+------------------------------------------------------+
|                  Presentation Layer                    |
|  (MonoBehaviour, UI, VFX, Animation, Camera)         |
+------------------------------------------------------+
|                  Bridge / Event Bus                    |
|  (GameEvent, EventChannel, View Binding)             |
+------------------------------------------------------+
|                   Logic Layer (Pure C#)               |
|  (GameCore, Systems, Models, Services)               |
+------------------------------------------------------+
|                   Data Layer                           |
|  (ScriptableObject Configs, Runtime State)           |
+------------------------------------------------------+
```

### 2.2 核心设计决策

1. **逻辑层为纯 C# 类**：不依赖 MonoBehaviour 生命周期，可在无渲染环境下单元测试
2. **事件驱动通信**：逻辑层通过事件总线通知表现层，表现层不直接修改逻辑数据
3. **配置驱动**：建筑、敌人、波次、奖励、地图均通过 ScriptableObject 定义
4. **Service Locator 模式**：系统间通过 ServiceLocator 获取依赖，便于测试时替换 Mock
5. **状态机管理流程**：GameCore 使用有限状态机管理游戏阶段流转

---

## 3. 模块划分

### 3.1 模块依赖关系

```
GameCore (状态机, 生命周期管理)
  |
  +-- WaveSystem (波次推进, 刷怪调度)
  |     |
  |     +-- EnemySystem (敌人生成, 行为, 生命周期)
  |           |
  |           +-- PathfindingModule (A* 寻路)
  |
  +-- BuildingSystem (建筑放置, 升级, 移除)
  |     |
  |     +-- GridMapSystem (网格状态管理)
  |     |
  |     +-- CombatSystem (攻击结算, 目标选择)
  |
  +-- ResourceSystem (资源管理)
  |
  +-- RewardSystem (波后三选一)
  |
  +-- ConfigSystem (ScriptableObject 加载与校验)
  |
  +-- EventBus (全局事件通信)
```

### 3.2 模块职责

| 模块 | 职责 | 输入 | 输出 |
|------|------|------|------|
| GameCore | 游戏状态流转、系统初始化与销毁 | 玩家操作、系统事件 | 状态切换事件 |
| GridMapSystem | 网格数据管理、可建造性查询 | 地图配置、建筑事件 | 网格状态变更事件 |
| BuildingSystem | 建筑生命周期管理 | 玩家放置指令、配置 | 建筑创建/销毁事件 |
| EnemySystem | 敌人生成与生命周期 | 波次配置、寻路结果 | 敌人状态事件 |
| PathfindingModule | A* 路径计算 | 网格数据、起终点 | 路径节点列表 |
| WaveSystem | 波次推进调度 | 波次配置、时间 | 波次开始/结束事件 |
| CombatSystem | 伤害计算、目标选择 | 建筑数据、敌人数据 | 伤害事件、击杀事件 |
| ResourceSystem | 资源余额管理 | 消耗/获取请求 | 余额变更事件 |
| RewardSystem | 奖励抽取与应用 | 奖励池配置、玩家选择 | 奖励应用事件 |
| ConfigSystem | 配置加载与校验 | ScriptableObject 资产 | 类型安全配置数据 |
| EventBus | 发布/订阅事件通信 | 任意事件 | 事件分发 |

---

## 4. 类设计

### 4.1 核心接口与基类

```csharp
// 系统接口
public interface IGameSystem
{
    void Initialize();
    void Tick(float deltaTime);
    void Dispose();
}

// 服务定位器
public static class ServiceLocator
{
    void Register<T>(T instance);
    T Get<T>();
    void Clear();
}

// 事件总线
public class EventBus
{
    void Publish<T>(T evt);
    void Subscribe<T>(Action<T> handler);
    void Unsubscribe<T>(Action<T> handler);
}
```

### 4.2 GameCore -- 状态机

```csharp
public enum GameState
{
    Initializing,
    Preparation,
    Combat,
    Settlement,
    RewardSelection,
    Victory,
    Defeat
}

public class GameCore : IGameSystem
{
    GameState CurrentState { get; }
    int CurrentWave { get; }
    int CoreHealth { get; }
    int MaxCoreHealth { get; }

    void TransitionTo(GameState newState);
    void StartNextWave();
    void ApplyCoreHealthDamage(int damage);
}
```

### 4.3 GridMapSystem

```csharp
public enum CellState
{
    Empty,        // 可建造
    Blocked,      // 不可建造(地形)
    Occupied,     // 已有建筑
    Path,         // 路径节点
    SpawnPoint,   // 敌人出生点
    CorePoint     // 核心防守点
}

public class GridCell
{
    Vector2Int Position;
    CellState State;
    string OccupantId;  // 建筑ID(如有)
}

public class GridMapSystem : IGameSystem
{
    int Width { get; }
    int Height { get; }
    GridCell GetCell(Vector2Int pos);
    bool IsBuildable(Vector2Int pos);
    void SetCellState(Vector2Int pos, CellState state);
    List<Vector2Int> GetSpawnPoints();
    Vector2Int GetCorePoint();
    void LoadFromConfig(MapConfig config);
}
```

### 4.4 PathfindingModule

```csharp
public class PathfindingModule
{
    // A* 实现
    List<Vector2Int> FindPath(Vector2Int start, Vector2Int end, GridMapSystem grid);
    bool HasValidPath(Vector2Int start, Vector2Int end, GridMapSystem grid);
}
```

### 4.5 BuildingSystem

```csharp
public class BuildingInstance
{
    string Id;
    string ConfigId;
    Vector2Int GridPosition;
    float AttackCooldownRemaining;
}

public class BuildingSystem : IGameSystem
{
    bool CanPlace(string buildingConfigId, Vector2Int position);
    BuildingInstance Place(string buildingConfigId, Vector2Int position);
    void Remove(string buildingId);
    List<BuildingInstance> GetAllBuildings();
}
```

### 4.6 EnemySystem

```csharp
public class EnemyInstance
{
    string Id;
    string ConfigId;
    float CurrentHealth;
    float MaxHealth;
    float MoveSpeed;
    int PathIndex;
    List<Vector2Int> Path;
    Vector2 WorldPosition;
}

public class EnemySystem : IGameSystem
{
    EnemyInstance SpawnEnemy(string configId, Vector2Int spawnPoint);
    void RemoveEnemy(string enemyId);
    List<EnemyInstance> GetActiveEnemies();
    int ActiveEnemyCount { get; }
}
```

### 4.7 WaveSystem

```csharp
public class WaveSystem : IGameSystem
{
    int CurrentWaveIndex { get; }
    int TotalWaves { get; }
    bool IsWaveActive { get; }
    void StartWave(int waveIndex);
    void Tick(float deltaTime);  // 处理批次生成计时
}
```

### 4.8 CombatSystem

```csharp
public enum TargetingStrategy
{
    Nearest,
    LowestHealth
}

public class CombatSystem : IGameSystem
{
    void Tick(float deltaTime);  // 遍历建筑, 执行攻击逻辑
    void ApplyDamage(string enemyId, float damage);
}
```

### 4.9 ResourceSystem

```csharp
public class ResourceSystem : IGameSystem
{
    int CurrentGold { get; }
    bool CanAfford(int amount);
    void Add(int amount);
    bool TrySpend(int amount);
}
```

### 4.10 RewardSystem

```csharp
public enum RewardType
{
    NewBuilding,
    ResourceBonus,
    BuildingUpgrade
}

public class RewardCard
{
    string Id;
    RewardType Type;
    string DisplayName;
    string Description;
    Dictionary<string, object> Parameters;
}

public class RewardSystem : IGameSystem
{
    List<RewardCard> DrawRewards(int count = 3);
    void ApplyReward(RewardCard card);
}
```

---

## 5. 数据流

### 5.1 单波次完整数据流

```
[准备阶段]
  玩家操作 → BuildingSystem.CanPlace() → GridMapSystem 查询
                                        → ResourceSystem 查询
  确认放置 → BuildingSystem.Place()
           → GridMapSystem.SetCellState(Occupied)
           → PathfindingModule 重新计算路径
           → ResourceSystem.TrySpend()
           → EventBus.Publish(BuildingPlacedEvent)

  玩家确认开始 → GameCore.StartNextWave()
              → GameCore.TransitionTo(Combat)

[战斗阶段]
  WaveSystem.Tick()
    → 根据时间间隔生成敌人批次
    → EnemySystem.SpawnEnemy()
    → PathfindingModule.FindPath()
    → EventBus.Publish(EnemySpawnedEvent)

  EnemySystem.Tick()
    → 敌人沿路径移动
    → 到达核心 → GameCore.ApplyCoreHealthDamage()
              → EnemySystem.RemoveEnemy()
              → EventBus.Publish(EnemyReachedCoreEvent)

  CombatSystem.Tick()
    → 遍历建筑, 检查冷却
    → 选择范围内目标(TargetingStrategy)
    → ApplyDamage()
    → 敌人死亡 → ResourceSystem.Add(掉落)
              → EnemySystem.RemoveEnemy()
              → EventBus.Publish(EnemyKilledEvent)

  波次结束条件:
    WaveSystem 检测所有敌人已生成且 ActiveEnemyCount == 0
    → EventBus.Publish(WaveCompletedEvent)
    → GameCore.TransitionTo(Settlement)

[结算阶段]
  ResourceSystem.Add(波次结算奖励)
  → GameCore.TransitionTo(RewardSelection)

[奖励选择]
  RewardSystem.DrawRewards(3)
  → UI 展示三张卡
  → 玩家选择 → RewardSystem.ApplyReward()
  → GameCore.TransitionTo(Preparation)
```

### 5.2 核心事件定义

```csharp
// 游戏流程事件
public struct GameStateChangedEvent { GameState OldState; GameState NewState; }
public struct WaveStartedEvent { int WaveIndex; bool IsBossWave; }
public struct WaveCompletedEvent { int WaveIndex; }
public struct GameOverEvent { bool IsVictory; }

// 建筑事件
public struct BuildingPlacedEvent { BuildingInstance Building; }
public struct BuildingRemovedEvent { string BuildingId; Vector2Int Position; }

// 敌人事件
public struct EnemySpawnedEvent { EnemyInstance Enemy; }
public struct EnemyKilledEvent { string EnemyId; Vector2 Position; int ResourceDrop; }
public struct EnemyReachedCoreEvent { string EnemyId; int Damage; }

// 战斗事件
public struct AttackEvent { string BuildingId; string EnemyId; float Damage; }

// 资源事件
public struct ResourceChangedEvent { int OldAmount; int NewAmount; }

// 奖励事件
public struct RewardSelectedEvent { RewardCard Card; }
```

---

## 6. 配置数据模型 (ScriptableObject)

### 6.1 BuildingConfig

```csharp
[CreateAssetMenu(menuName = "WDG/BuildingConfig")]
public class BuildingConfig : ScriptableObject
{
    public string buildingId;
    public string displayName;
    public string description;
    public int cost;
    public float attackDamage;
    public float attackRange;
    public float attackInterval;
    public TargetingStrategy targetingStrategy;
    public Vector2Int gridSize;  // 通常 1x1
    public GameObject prefab;    // 表现层引用
}
```

### 6.2 EnemyConfig

```csharp
[CreateAssetMenu(menuName = "WDG/EnemyConfig")]
public class EnemyConfig : ScriptableObject
{
    public string enemyId;
    public string displayName;
    public float maxHealth;
    public float moveSpeed;
    public int coreDamage;
    public int resourceDrop;
    public bool isBoss;
    public GameObject prefab;
}
```

### 6.3 WaveConfig

```csharp
[CreateAssetMenu(menuName = "WDG/WaveConfig")]
public class WaveConfig : ScriptableObject
{
    public int waveIndex;
    public bool isBossWave;
    public List<WaveSpawnGroup> spawnGroups;
    public int completionBonus;
}

[System.Serializable]
public class WaveSpawnGroup
{
    public EnemyConfig enemyConfig;
    public int count;
    public float spawnInterval;
    public float groupDelay;  // 该批次延迟时间
    public int spawnPointIndex;
}
```

### 6.4 MapConfig

```csharp
[CreateAssetMenu(menuName = "WDG/MapConfig")]
public class MapConfig : ScriptableObject
{
    public int width;
    public int height;
    public List<Vector2Int> blockedCells;
    public List<Vector2Int> spawnPoints;
    public Vector2Int corePoint;
    public int startingGold;
    public int coreMaxHealth;
}
```

### 6.5 RewardPoolConfig

```csharp
[CreateAssetMenu(menuName = "WDG/RewardPoolConfig")]
public class RewardPoolConfig : ScriptableObject
{
    public List<RewardDefinition> rewards;
}

[System.Serializable]
public class RewardDefinition
{
    public string rewardId;
    public RewardType type;
    public string displayName;
    public string description;
    public int weight;
    // 根据类型使用不同参数
    public BuildingConfig unlockBuilding;  // NewBuilding 类型
    public int resourceAmount;             // ResourceBonus 类型
    public string targetBuildingId;        // BuildingUpgrade 类型
    public float upgradeMultiplier;        // BuildingUpgrade 类型
}
```

---

## 7. 目录结构

```
Assets/
  Scripts/
    Core/
      GameCore.cs              -- 游戏主状态机
      ServiceLocator.cs        -- 服务定位器
      EventBus.cs              -- 事件总线
      IGameSystem.cs           -- 系统接口
      GameEvents.cs            -- 所有事件定义
    Systems/
      GridMap/
        GridMapSystem.cs       -- 网格逻辑
        GridCell.cs            -- 网格单元数据
      Building/
        BuildingSystem.cs      -- 建筑系统逻辑
        BuildingInstance.cs    -- 建筑运行时实例
      Enemy/
        EnemySystem.cs         -- 敌人系统逻辑
        EnemyInstance.cs       -- 敌人运行时实例
      Pathfinding/
        PathfindingModule.cs   -- A* 寻路
      Wave/
        WaveSystem.cs          -- 波次管理
      Combat/
        CombatSystem.cs        -- 战斗结算
      Resource/
        ResourceSystem.cs      -- 资源管理
      Reward/
        RewardSystem.cs        -- 奖励系统
        RewardCard.cs          -- 奖励卡数据
    Config/
      ConfigSystem.cs          -- 配置加载校验
      BuildingConfig.cs        -- 建筑配置 SO
      EnemyConfig.cs           -- 敌人配置 SO
      WaveConfig.cs            -- 波次配置 SO
      MapConfig.cs             -- 地图配置 SO
      RewardPoolConfig.cs      -- 奖励池配置 SO
      GameBalanceConfig.cs     -- 全局平衡参数 SO
    Presentation/
      GameBootstrap.cs         -- 入口 MonoBehaviour
      Views/
        GridView.cs            -- 地图表现
        BuildingView.cs        -- 建筑表现
        EnemyView.cs           -- 敌人表现
        ProjectileView.cs      -- 弹道表现
      UI/
        GameHUD.cs             -- 主战斗 HUD
        WaveInfoPanel.cs       -- 波次信息面板
        RewardSelectionUI.cs   -- 奖励选择界面
        BuildingPanel.cs       -- 建筑选择栏
        GameOverPanel.cs       -- 结算界面
    Enums/
      GameState.cs
      CellState.cs
      TargetingStrategy.cs
      RewardType.cs
  ScriptableObjects/
    Buildings/                 -- 建筑配置资产(运行时在编辑器中创建)
    Enemies/                   -- 敌人配置资产
    Waves/                     -- 波次配置资产
    Maps/                      -- 地图配置资产
    Rewards/                   -- 奖励池配置资产
  Scenes/
    MainGame.unity             -- 主游戏场景
    MainMenu.unity             -- 主菜单(可后置)
  Prefabs/
    Buildings/
    Enemies/
    VFX/
  Art/
    Placeholder/               -- 占位素材
  Tests/
    EditMode/
      GameCoreTests.cs
      GridMapTests.cs
      PathfindingTests.cs
      CombatSystemTests.cs
      ResourceSystemTests.cs
```

---

## 8. 关键算法

### 8.1 A* 寻路

- 使用标准 A* 算法
- 启发函数：曼哈顿距离
- 邻居：上下左右四方向（不允许对角移动，简化策略）
- 路径缓存：当网格状态变化时清除缓存并重新计算
- 性能目标：50 个敌人同时存在时，单次计算 < 16ms

### 8.2 建筑放置路径验证

1. 临时将目标格子标记为 Occupied
2. 对所有 SpawnPoint 执行 HasValidPath() 检查
3. 若任一路径不可达，回滚格子状态并拒绝放置
4. 若全部可达，确认放置

### 8.3 战斗目标选择

```
foreach building in activeBuildings:
    if cooldown > 0: continue
    candidates = enemies.Where(e => Distance(building, e) <= range)
    if candidates.empty: continue
    target = strategy switch:
        Nearest     -> candidates.MinBy(distance)
        LowestHealth -> candidates.MinBy(health)
    ApplyDamage(target, building.damage)
    ResetCooldown(building)
```

---

## 9. 多人扩展预留

虽然 Phase 0 仅实现单人，但架构需为多人预留：

1. **逻辑层无副作用**：所有状态变更通过明确方法调用，便于后续加入 Command 模式做同步
2. **确定性执行**：战斗计算使用定点数或确定性排序，避免浮点不确定性（后续优化）
3. **玩家 ID 隔离**：ResourceSystem、BuildingSystem 等可后续扩展为按 PlayerId 隔离数据
4. **事件可序列化**：所有事件结构体可序列化为网络消息

---

## 10. 测试策略

### 10.1 单元测试（Edit Mode）

- GameCore 状态流转
- GridMapSystem 网格操作
- PathfindingModule 路径计算
- CombatSystem 伤害计算
- ResourceSystem 余额管理
- BuildingSystem 放置验证

### 10.2 集成测试（Play Mode，后续）

- 完整波次流程
- 建筑放置到攻击到击杀的完整链路
- 奖励选择到效果应用

---

## 11. 性能约束

| 指标 | 目标值 |
|------|--------|
| 帧率 | 60 FPS |
| 同屏敌人 | 50+ |
| 寻路计算 | < 16ms / 次 |
| 地图尺寸 | 20x15 起步 |
| 内存占用 | < 500MB |

---

## 12. 技术风险与缓解

| 风险 | 影响 | 缓解措施 |
|------|------|----------|
| 大量敌人寻路性能 | 卡顿 | 路径缓存、分帧计算、路径共享 |
| 配置热更新 | 迭代效率 | ScriptableObject 可在 Editor 中即时修改 |
| 状态同步复杂度(未来) | 多人延迟 | 逻辑层确定性设计、Command 模式预留 |
| 建筑组合过于复杂 | 平衡性 | Phase 0 限制 4 种建筑，逐步扩展 |
