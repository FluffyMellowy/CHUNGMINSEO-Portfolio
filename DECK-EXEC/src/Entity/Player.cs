namespace Colorless.Entity
{
    using Sirenix.OdinInspector;
    using UnityEngine;
    using VContainer;
    using Colorless.Grid;

    public sealed class Player : MonoBehaviour
    {
        [Inject] private GridManager _gridManager;

        private Vector2Int _gridPosition;
        private Vector2Int _facing = Vector2Int.down;

        [ShowInInspector, ReadOnly, LabelText("Grid Position (Runtime)")]
        public Vector2Int GridPosition => _gridPosition;

        /// <summary>
        /// 現在向いている方向。Move 系カードで更新される。
        /// Dash・PushBox・Attack などのカードはこの方向を参照する。
        /// </summary>
        [ShowInInspector, ReadOnly, LabelText("Facing (Runtime)")]
        public Vector2Int Facing => _facing;

        public void Initialize()
        {
            _gridPosition = _gridManager.WorldToGrid(transform.position);
            transform.position = _gridManager.GridToWorld(_gridPosition);

            GridCell cell = _gridManager.GetCell(_gridPosition);
            if (cell != null) cell.Occupant = gameObject;
        }

        /// <summary>
        /// 指定セル位置へ移動する。Occupant登録/解除はここで処理。
        /// 移動可否判定は ICardEffect 側が行う。
        /// </summary>
        public void ExecuteMove(Vector2Int newPos)
        {
            GridCell currentCell = _gridManager.GetCell(_gridPosition);
            if (currentCell != null) currentCell.Occupant = null;

            _gridPosition = newPos;
            transform.position = _gridManager.GridToWorld(_gridPosition);

            GridCell targetCell = _gridManager.GetCell(_gridPosition);
            if (targetCell != null) targetCell.Occupant = gameObject;
        }

        /// <summary>
        /// facing を更新する。Move カード実行時に必ず呼ばれる
        /// （実際に移動したかどうかに関わらず "意図" として方向は更新される）。
        /// </summary>
        public void SetFacing(Vector2Int dir)
        {
            if (dir == Vector2Int.zero) return;
            _facing = dir;
        }

        /// <summary>
        /// スナップショット復元用：直接位置を書き換える。
        /// Occupant 整合はスナップショット側で一括管理する。
        /// </summary>
        public void SetGridPosition(Vector2Int pos)
        {
            _gridPosition = pos;
            transform.position = _gridManager.GridToWorld(pos);
        }
    }
}
