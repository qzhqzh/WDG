using UnityEngine;

namespace WDG
{
    public class GridCell
    {
        public Vector2Int Position { get; private set; }
        public CellState State { get; set; }
        public string OccupantId { get; set; }

        public GridCell(Vector2Int position)
        {
            Position = position;
            State = CellState.Empty;
            OccupantId = null;
        }
    }
}
