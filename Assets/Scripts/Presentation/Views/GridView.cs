using System.Collections.Generic;
using UnityEngine;

namespace WDG
{
    public class GridView : MonoBehaviour
    {
        [SerializeField] private GameObject cellPrefab;
        [SerializeField] private Material buildableMaterial;
        [SerializeField] private Material blockedMaterial;
        [SerializeField] private Material pathMaterial;
        [SerializeField] private Material spawnMaterial;
        [SerializeField] private Material coreMaterial;

        private const float CellSize = 1f;
        private Dictionary<Vector2Int, GameObject> _cellObjects = new Dictionary<Vector2Int, GameObject>();
        private GridMapSystem _gridMapSystem;
        private bool _highlightActive;

        private void OnEnable()
        {
            EventBus.Subscribe<GameStateChangedEvent>(OnGameStateChanged);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<GameStateChangedEvent>(OnGameStateChanged);
        }

        private void Start()
        {
            _gridMapSystem = ServiceLocator.Get<GridMapSystem>();
            RefreshGrid();
        }

        public void RefreshGrid()
        {
            ClearGrid();

            if (_gridMapSystem == null)
                return;

            for (int x = 0; x < _gridMapSystem.Width; x++)
            {
                for (int y = 0; y < _gridMapSystem.Height; y++)
                {
                    var pos = new Vector2Int(x, y);
                    var cell = _gridMapSystem.GetCell(pos);
                    if (cell == null)
                        continue;

                    var worldPos = GridToWorld(pos);
                    GameObject cellGO = null;

                    if (cellPrefab != null)
                    {
                        cellGO = Instantiate(cellPrefab, worldPos, Quaternion.identity, transform);
                    }
                    else
                    {
                        cellGO = GameObject.CreatePrimitive(PrimitiveType.Cube);
                        cellGO.transform.SetParent(transform);
                        cellGO.transform.position = worldPos;
                        cellGO.transform.localScale = new Vector3(CellSize * 0.95f, 0.1f, CellSize * 0.95f);
                    }

                    cellGO.name = $"Cell_{x}_{y}";
                    ApplyCellMaterial(cellGO, cell.State);
                    _cellObjects[pos] = cellGO;
                }
            }
        }

        public void HighlightBuildableCells()
        {
            _highlightActive = true;
            if (_gridMapSystem == null)
                return;

            for (int x = 0; x < _gridMapSystem.Width; x++)
            {
                for (int y = 0; y < _gridMapSystem.Height; y++)
                {
                    var pos = new Vector2Int(x, y);
                    if (_gridMapSystem.IsBuildable(pos) && _cellObjects.ContainsKey(pos))
                    {
                        ApplyCellMaterial(_cellObjects[pos], CellState.Empty, true);
                    }
                }
            }
        }

        public void ClearHighlights()
        {
            _highlightActive = false;
            if (_gridMapSystem == null)
                return;

            for (int x = 0; x < _gridMapSystem.Width; x++)
            {
                for (int y = 0; y < _gridMapSystem.Height; y++)
                {
                    var pos = new Vector2Int(x, y);
                    var cell = _gridMapSystem.GetCell(pos);
                    if (cell != null && _cellObjects.ContainsKey(pos))
                    {
                        ApplyCellMaterial(_cellObjects[pos], cell.State);
                    }
                }
            }
        }

        public void UpdateCell(Vector2Int pos)
        {
            if (_gridMapSystem == null)
                return;

            var cell = _gridMapSystem.GetCell(pos);
            if (cell == null || !_cellObjects.ContainsKey(pos))
                return;

            ApplyCellMaterial(_cellObjects[pos], cell.State);
        }

        private void OnGameStateChanged(GameStateChangedEvent evt)
        {
            if (evt.NewState == GameState.Preparation)
            {
                HighlightBuildableCells();
            }
            else
            {
                ClearHighlights();
            }
        }

        private void ApplyCellMaterial(GameObject cellGO, CellState state, bool highlight = false)
        {
            var renderer = cellGO.GetComponent<Renderer>();
            if (renderer == null)
                return;

            Material mat = null;

            if (highlight && buildableMaterial != null)
            {
                mat = buildableMaterial;
            }
            else
            {
                switch (state)
                {
                    case CellState.Blocked:
                        mat = blockedMaterial;
                        break;
                    case CellState.SpawnPoint:
                        mat = spawnMaterial;
                        break;
                    case CellState.CorePoint:
                        mat = coreMaterial;
                        break;
                    case CellState.Occupied:
                        mat = blockedMaterial;
                        break;
                    default:
                        mat = pathMaterial;
                        break;
                }
            }

            if (mat != null)
            {
                renderer.material = mat;
            }
        }

        private void ClearGrid()
        {
            foreach (var kvp in _cellObjects)
            {
                if (kvp.Value != null)
                {
                    Destroy(kvp.Value);
                }
            }
            _cellObjects.Clear();
        }

        private Vector3 GridToWorld(Vector2Int gridPos)
        {
            return new Vector3(
                gridPos.x * CellSize + CellSize / 2f,
                0f,
                gridPos.y * CellSize + CellSize / 2f
            );
        }
    }
}
