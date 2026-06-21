namespace Colorless.Entity.Enemy
{
    using Sirenix.OdinInspector;
    using UnityEngine;
    using VContainer;
    using Colorless.Grid;
    using Colorless.Turn;

    public abstract class EnemyBase : MonoBehaviour, IEntity
    {
        [Inject] protected GridManager _gridManager;
        [Inject] protected TurnManager _turnManager;

        protected Vector2Int _gridPosition;

        [ShowInInspector, ReadOnly, LabelText("Grid Position (Runtime)")]
        public Vector2Int GridPosition => _gridPosition;

        public virtual void Initialize()
        {
            _gridPosition = _gridManager.WorldToGrid(transform.position);
            transform.position = _gridManager.GridToWorld(_gridPosition);

            GridCell cell = _gridManager.GetCell(_gridPosition);
            if (cell != null) cell.Occupant = gameObject;

            _turnManager.RegisterEntity(this);
        }

        public abstract void Act();

        /// <summary>
        /// 自セルに Hazard（SpikeTrap 等）が登録されていたら、自身を退場（非アクティブ化）する。
        /// 各 Enemy の移動直後に呼び出して環境キルを成立させる。
        /// </summary>
        protected bool CheckHazardAndDie()
        {
            GridCell cell = _gridManager.GetCell(_gridPosition);
            if (cell == null || cell.Hazard == null) return false;

            /* 現セルから Occupant を外して退場 */
            if (cell.Occupant == gameObject) cell.Occupant = null;
            gameObject.SetActive(false);
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

        /// <summary>
        /// サブクラス独自の状態をスナップショットに保存する。
        /// 状態を持たない敵は null を返してよい。
        /// </summary>
        public virtual object CaptureState() => null;

        /// <summary>
        /// CaptureState で返した値を渡されて状態を復元する。
        /// </summary>
        public virtual void RestoreState(object state) { }
    }
}
