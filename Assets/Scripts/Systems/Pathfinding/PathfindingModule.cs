using System.Collections.Generic;
using UnityEngine;

namespace WDG
{
    public class PathfindingModule
    {
        private static readonly Vector2Int[] Directions = new Vector2Int[]
        {
            Vector2Int.up,
            Vector2Int.down,
            Vector2Int.left,
            Vector2Int.right
        };

        public List<Vector2Int> FindPath(Vector2Int start, Vector2Int end, GridMapSystem grid)
        {
            if (!grid.IsInBounds(start) || !grid.IsInBounds(end))
                return new List<Vector2Int>();

            var openSet = new MinHeap();
            var closedSet = new HashSet<Vector2Int>();
            var cameFrom = new Dictionary<Vector2Int, Vector2Int>();
            var gScore = new Dictionary<Vector2Int, int>();
            var fScore = new Dictionary<Vector2Int, int>();

            gScore[start] = 0;
            fScore[start] = ManhattanDistance(start, end);
            openSet.Add(new PathNode(start, fScore[start]));

            while (openSet.Count > 0)
            {
                var current = openSet.RemoveMin();

                if (current.Position == end)
                {
                    return ReconstructPath(cameFrom, current.Position);
                }

                closedSet.Add(current.Position);

                foreach (var dir in Directions)
                {
                    var neighbor = current.Position + dir;

                    if (!grid.IsInBounds(neighbor))
                        continue;

                    if (closedSet.Contains(neighbor))
                        continue;

                    if (!IsWalkable(neighbor, grid))
                        continue;

                    int tentativeG = gScore[current.Position] + 1;

                    if (!gScore.ContainsKey(neighbor) || tentativeG < gScore[neighbor])
                    {
                        cameFrom[neighbor] = current.Position;
                        gScore[neighbor] = tentativeG;
                        fScore[neighbor] = tentativeG + ManhattanDistance(neighbor, end);
                        openSet.Add(new PathNode(neighbor, fScore[neighbor]));
                    }
                }
            }

            return new List<Vector2Int>();
        }

        public bool HasValidPath(Vector2Int start, Vector2Int end, GridMapSystem grid)
        {
            var path = FindPath(start, end, grid);
            return path.Count > 0;
        }

        private bool IsWalkable(Vector2Int pos, GridMapSystem grid)
        {
            var cell = grid.GetCell(pos);
            if (cell == null) return false;
            return cell.State == CellState.Empty
                || cell.State == CellState.Path
                || cell.State == CellState.SpawnPoint
                || cell.State == CellState.CorePoint;
        }

        private int ManhattanDistance(Vector2Int a, Vector2Int b)
        {
            return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
        }

        private List<Vector2Int> ReconstructPath(Dictionary<Vector2Int, Vector2Int> cameFrom, Vector2Int current)
        {
            var path = new List<Vector2Int>();
            path.Add(current);
            while (cameFrom.ContainsKey(current))
            {
                current = cameFrom[current];
                path.Add(current);
            }
            path.Reverse();
            return path;
        }

        private struct PathNode
        {
            public Vector2Int Position;
            public int FScore;

            public PathNode(Vector2Int position, int fScore)
            {
                Position = position;
                FScore = fScore;
            }
        }

        private class MinHeap
        {
            private readonly List<PathNode> _data = new List<PathNode>();

            public int Count => _data.Count;

            public void Add(PathNode node)
            {
                _data.Add(node);
                HeapifyUp(_data.Count - 1);
            }

            public PathNode RemoveMin()
            {
                var min = _data[0];
                int lastIndex = _data.Count - 1;
                _data[0] = _data[lastIndex];
                _data.RemoveAt(lastIndex);
                if (_data.Count > 0)
                {
                    HeapifyDown(0);
                }
                return min;
            }

            private void HeapifyUp(int index)
            {
                while (index > 0)
                {
                    int parent = (index - 1) / 2;
                    if (_data[index].FScore < _data[parent].FScore)
                    {
                        Swap(index, parent);
                        index = parent;
                    }
                    else
                    {
                        break;
                    }
                }
            }

            private void HeapifyDown(int index)
            {
                int count = _data.Count;
                while (true)
                {
                    int left = 2 * index + 1;
                    int right = 2 * index + 2;
                    int smallest = index;

                    if (left < count && _data[left].FScore < _data[smallest].FScore)
                        smallest = left;
                    if (right < count && _data[right].FScore < _data[smallest].FScore)
                        smallest = right;

                    if (smallest != index)
                    {
                        Swap(index, smallest);
                        index = smallest;
                    }
                    else
                    {
                        break;
                    }
                }
            }

            private void Swap(int a, int b)
            {
                var temp = _data[a];
                _data[a] = _data[b];
                _data[b] = temp;
            }
        }
    }
}
