using UnityEngine;

namespace WDG
{
    public class GameCore : IGameSystem
    {
        public GameState CurrentState { get; private set; }
        public int CurrentWaveIndex { get; private set; }
        public int CoreHealth { get; private set; }
        public int MaxCoreHealth { get; private set; }

        private ConfigSystem _configSystem;
        private GridMapSystem _gridMapSystem;
        private PathfindingModule _pathfindingModule;
        private ResourceSystem _resourceSystem;
        private BuildingSystem _buildingSystem;
        private EnemySystem _enemySystem;
        private WaveSystem _waveSystem;
        private CombatSystem _combatSystem;
        private RewardSystem _rewardSystem;

        public void Initialize()
        {
            CurrentState = GameState.Initializing;

            _configSystem = ServiceLocator.Get<ConfigSystem>();

            // Create and register all sub-systems
            _gridMapSystem = new GridMapSystem();
            ServiceLocator.Register<GridMapSystem>(_gridMapSystem);

            _pathfindingModule = new PathfindingModule();
            ServiceLocator.Register<PathfindingModule>(_pathfindingModule);

            _resourceSystem = new ResourceSystem();
            ServiceLocator.Register<ResourceSystem>(_resourceSystem);

            _buildingSystem = new BuildingSystem();
            ServiceLocator.Register<BuildingSystem>(_buildingSystem);

            _enemySystem = new EnemySystem();
            ServiceLocator.Register<EnemySystem>(_enemySystem);

            _waveSystem = new WaveSystem();
            ServiceLocator.Register<WaveSystem>(_waveSystem);

            _combatSystem = new CombatSystem();
            ServiceLocator.Register<CombatSystem>(_combatSystem);

            _rewardSystem = new RewardSystem();
            ServiceLocator.Register<RewardSystem>(_rewardSystem);

            // Load map from config
            var mapConfig = _configSystem.MapConfig;
            _gridMapSystem.LoadFromConfig(mapConfig);

            // Initialize core health
            MaxCoreHealth = mapConfig.coreMaxHealth;
            CoreHealth = MaxCoreHealth;

            // Initialize resource system with starting gold
            _resourceSystem.Initialize(mapConfig.startingGold);

            // Initialize all systems
            _gridMapSystem.Initialize();
            _resourceSystem.Initialize();
            _buildingSystem.Initialize();
            _enemySystem.Initialize();
            _waveSystem.Initialize();
            _combatSystem.Initialize();
            _rewardSystem.Initialize();

            // Set total waves
            _waveSystem.TotalWaves = _configSystem.WaveConfigs.Count;

            // Transition to Preparation
            TransitionTo(GameState.Preparation);
        }

        public void Tick(float deltaTime)
        {
            switch (CurrentState)
            {
                case GameState.Combat:
                    _waveSystem.Tick(deltaTime);
                    _enemySystem.Tick(deltaTime);
                    _combatSystem.Tick(deltaTime);

                    // Check if wave completed
                    if (!_waveSystem.IsWaveActive && _enemySystem.ActiveEnemyCount == 0)
                    {
                        TransitionTo(GameState.Settlement);
                    }
                    break;

                case GameState.Settlement:
                    HandleSettlement();
                    break;

                case GameState.Preparation:
                case GameState.RewardSelection:
                case GameState.Victory:
                case GameState.Defeat:
                case GameState.Initializing:
                    // No automatic tick behavior for these states
                    break;
            }
        }

        public void Dispose()
        {
            _gridMapSystem?.Dispose();
            _resourceSystem?.Dispose();
            _buildingSystem?.Dispose();
            _enemySystem?.Dispose();
            _waveSystem?.Dispose();
            _combatSystem?.Dispose();
            _rewardSystem?.Dispose();
        }

        public void TransitionTo(GameState newState)
        {
            var previousState = CurrentState;
            CurrentState = newState;

            EventBus.Publish(new GameStateChangedEvent
            {
                PreviousState = previousState,
                NewState = newState
            });

            // Entry logic for new state
            switch (newState)
            {
                case GameState.Victory:
                    EventBus.Publish(new GameOverEvent { IsVictory = true });
                    break;
                case GameState.Defeat:
                    EventBus.Publish(new GameOverEvent { IsVictory = false });
                    break;
            }
        }

        public void StartNextWave()
        {
            CurrentWaveIndex++;

            var waveConfigs = _configSystem.WaveConfigs;
            WaveConfig waveConfig = null;
            foreach (var config in waveConfigs)
            {
                if (config.waveIndex == CurrentWaveIndex)
                {
                    waveConfig = config;
                    break;
                }
            }

            if (waveConfig != null)
            {
                _waveSystem.StartWave(waveConfig);
                TransitionTo(GameState.Combat);
            }
        }

        public void ApplyCoreHealthDamage(int damage)
        {
            CoreHealth -= damage;

            if (CoreHealth <= 0)
            {
                CoreHealth = 0;
                TransitionTo(GameState.Defeat);
            }
        }

        private void HandleSettlement()
        {
            // Add wave completion bonus
            var waveConfigs = _configSystem.WaveConfigs;
            WaveConfig completedWave = null;
            foreach (var config in waveConfigs)
            {
                if (config.waveIndex == CurrentWaveIndex)
                {
                    completedWave = config;
                    break;
                }
            }

            if (completedWave != null)
            {
                _resourceSystem.AddWaveBonus(completedWave.completionBonus);
            }

            // Check if this was the last wave
            if (CurrentWaveIndex >= _waveSystem.TotalWaves)
            {
                TransitionTo(GameState.Victory);
            }
            else
            {
                TransitionTo(GameState.RewardSelection);
            }
        }
    }
}
