namespace Colorless.Entity
{
    using Sirenix.OdinInspector;
    using UnityEngine;
    using VContainer;
    using Colorless.Grid;

    public sealed class Box : MonoBehaviour
    {
        [Inject] private GridManager _gridManager;

        private Vector2Int _gridPosition;

        [ShowInInspector, ReadOnly, LabelText("Grid Position (Runtime)")]
        public Vector2Int GridPosition => _gridPosition;

        public void Initialize()
        {
            _gridPosition = _gridManager.WorldToGrid(transform.position);
            transform.position = _gridManager.GridToWorld(_gridPosition);

            GridCell cell = _gridManager.GetCell(_gridPosition);
            if (cell != null) cell.Occupant = gameObject;
        }

        /// <summary>
        /// 指定方向に押されたとき。穴に落ちたら SetActive(false) で非表示にし、
        /// ロールバックで復元できるようにする。
        /// </summary>
        public bool TryPush(Vector2Int direction, System.Action onComplete = null)
        {
            Vector2Int newPos = _gridPosition + direction;
            GridCell targetCell = _gridManager.GetCell(newPos);
            if (targetCell == null) return false;

            /* 穴に落ちる → 非表示化（破壊しない、ロールバック復元用） */
            if (targetCell.TileType == TileType.Hole)
            {
                GridCell currentCell = _gridManager.GetCell(_gridPosition);
                if (currentCell != null && currentCell.Occupant == gameObject)
                    currentCell.Occupant = null;
                gameObject.SetActive(false);
                onComplete?.Invoke();
                return true;
            }

            if (!targetCell.CanEnter()) return false;

            /* 現在のセルから解除 */
            GridCell current = _gridManager.GetCell(_gridPosition);
            if (current != null && current.Occupant == gameObject) current.Occupant = null;

            /* 即時移動 */
            _gridPosition = newPos;
            transform.position = _gridManager.GridToWorld(newPos);
            targetCell.Occupant = gameObject;

            onComplete?.Invoke();
            return true;
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
