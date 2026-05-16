using UnityEngine;

namespace WDG
{
    public struct GameStateChangedEvent
    {
        public GameState PreviousState;
        public GameState NewState;
    }

    public struct WaveStartedEvent
    {
        public int WaveIndex;
    }

    public struct WaveCompletedEvent
    {
        public int WaveIndex;
        public int BonusReward;
    }

    public struct GameOverEvent
    {
        public bool IsVictory;
    }

    public struct BuildingPlacedEvent
    {
        public string BuildingId;
        public Vector2Int Position;
    }

    public struct BuildingRemovedEvent
    {
        public string BuildingId;
        public Vector2Int Position;
    }

    public struct EnemySpawnedEvent
    {
        public string EnemyId;
        public int InstanceId;
        public Vector2 SpawnPosition;
    }

    public struct EnemyKilledEvent
    {
        public int InstanceId;
        public string EnemyId;
        public int ResourceDrop;
    }

    public struct EnemyReachedCoreEvent
    {
        public int InstanceId;
        public string EnemyId;
        public int Damage;
    }

    public struct AttackEvent
    {
        public string AttackerId;
        public int TargetInstanceId;
        public float Damage;
        public Vector2 TargetPosition;
    }

    public struct ResourceChangedEvent
    {
        public int PreviousAmount;
        public int NewAmount;
    }

    public struct RewardSelectedEvent
    {
        public string RewardId;
        public RewardType Type;
    }
}
