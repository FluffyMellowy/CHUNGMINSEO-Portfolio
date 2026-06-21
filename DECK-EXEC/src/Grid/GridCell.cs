namespace Colorless.Grid
{
    using UnityEngine;

    public enum TileType
    {
        Floor,
        Wall,
        Hole,
        Exit_Stairs,
        Exit_Door,
        Exit_Button,
        Collapse
    }

    public sealed class GridCell
    {
        public Vector2Int Position { get; private set; }
        public TileType TileType { get; set; }
        public bool IsWalkable => TileType == TileType.Floor
                               || TileType == TileType.Exit_Stairs
                               || (TileType == TileType.Exit_Door && !IsLocked)
                               || TileType == TileType.Exit_Button;
        public bool IsLocked { get; set; } = false;

        /// <summary>
        /// このマスに存在するエンティティ
        /// </summary>
        public GameObject Occupant { get; set; }

        /// <summary>
        /// このマスに重なっている "ハザード"（SpikeTrap 等）。
        /// Occupant とは別管理：ハザードは進入を妨げず、進入した側を殺害する。
        /// SpikeTrap.Initialize がここに自身を登録する。
        /// </summary>
        public GameObject Hazard { get; set; }

        /// <summary>
        /// 可視化用オブジェクト
        /// </summary>
        public GameObject Visual { get; set; }

        public GridCell(Vector2Int position, TileType tileType)
        {
            Position = position;
            TileType = tileType;
            Occupant = null;
        }

        /// <summary>
        /// 進入可能かどうか
        /// </summary>
        public bool CanEnter()
        {
            return IsWalkable && Occupant == null;
        }
    }
}
