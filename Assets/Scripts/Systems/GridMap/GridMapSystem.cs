using System.Collections.Generic;
using UnityEngine;

namespace WDG
{
    public class GridMapSystem : IGameSystem
    {
        public int Width { get; private set; }
        public int Height { get; private set; }

        private GridCell[,] _cells;
        private List<Vector2Int> _spawnPoints = new List<Vector2Int>();
        private Vector2Int _corePoint;

        public void Initialize()
        {
            // No-op: LoadFromConfig is called separately
        }

        public void Tick(float deltaTime)
        {
            // No-op
        }

        public void Dispose()
        {
            _cells = null;
            _spawnPoints.Clear();
        }

        public void LoadFromConfig(MapConfig config)
        {
            Width = config.width;
            Height = config.height;
            _cells = new GridCell[Width, Height];

            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    _cells[x, y] = new GridCell(new Vector2Int(x, y));
                }
            }

            if (config.blockedCells != null)
            {
                foreach (var pos in config.blockedCells)
                {
                    if (IsInBounds(pos))
                    {
                        _cells[pos.x, pos.y].State = CellState.Blocked;
                    }
                }
            }

            if (config.spawnPoints != null)
            {
                foreach (var pos in config.spawnPoints)
                {
                    if (IsInBounds(pos))
                    {
                        _cells[pos.x, pos.y].State = CellState.SpawnPoint;
                        _spawnPoints.Add(pos);
                    }
                }
            }

            _corePoint = config.corePoint;
            if (IsInBounds(_corePoint))
            {
                _cells[_corePoint.x, _corePoint.y].State = CellState.CorePoint;
            }
        }

        public GridCell GetCell(Vector2Int pos)
        {
            if (!IsInBounds(pos))
                return null;
            return _cells[pos.x, pos.y];
        }

        public bool IsBuildable(Vector2Int pos)
        {
            if (!IsInBounds(pos))
                return false;
            return _cells[pos.x, pos.y].State == CellState.Empty;
        }

        public void SetCellState(Vector2Int pos, CellState state, string occupantId = null)
        {
            if (!IsInBounds(pos))
                return;
            _cells[pos.x, pos.y].State = state;
            _cells[pos.x, pos.y].OccupantId = occupantId;
        }

        public List<Vector2Int> GetSpawnPoints()
        {
            return new List<Vector2Int>(_spawnPoints);
        }

        public Vector2Int GetCorePoint()
        {
            return _corePoint;
        }

        public bool IsInBounds(Vector2Int pos)
        {
            return pos.x >= 0 && pos.x < Width && pos.y >= 0 && pos.y < Height;
        }
    }
}
