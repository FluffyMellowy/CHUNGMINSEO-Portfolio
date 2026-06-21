namespace Colorless.Entity.Enemy
{
    using Sirenix.OdinInspector;
    using UnityEngine;
    using Colorless.Grid;

    /// <summary>
    /// 一方向に沿って往復するパトロール敵。Axis で水平 / 垂直を切替える。
    ///   - 毎ターン facing 方向に 1 セル進む
    ///   - 進路が壁／箱／他敵／グリッド境界で塞がっている時：方向を反転し、そのターンは静止する
    ///   - 穴に進入すると落下消滅する（環境キルの対象）
    ///   - プレイヤーと同セルに進入したら殺害（Occupant 上書きで SequenceExecutor が検知）
    /// </summary>
    public sealed class WandererEnemy : EnemyBase
    {
        public enum Axis { Horizontal, Vertical }

        [Title("Patrol")]
        [SerializeField] private Axis _axis = Axis.Horizontal;
        [InfoBox("初期方向：Horizontal なら右、Vertical なら上")]
        [SerializeField] private bool _startsPositive = true;

        [ShowInInspector, ReadOnly, LabelText("Moving Positive (Runtime)")]
        private bool _isMovingPositive = true;

        private void Awake()
        {
            _isMovingPositive = _startsPositive;
        }

        public override void Act()
        {
            if (!gameObject.activeSelf) return;

            Vector2Int dir = CurrentDirection();
            Vector2Int targetPos = _gridPosition + dir;
            GridCell targetCell = _gridManager.GetCell(targetPos);

            if (IsBlocked(targetCell))
            {
                /* 塞がり → 方向反転、このターンは静止 */
                _isMovingPositive = !_isMovingPositive;
                return;
            }

            MoveInto(targetPos, targetCell);

            /* 進入先が Hazard（SpikeTrap）なら落下と同様に退場 */
            if (gameObject.activeSelf) CheckHazardAndDie();
        }

        private Vector2Int CurrentDirection()
        {
            return _axis switch
            {
                Axis.Horizontal => _isMovingPositive ? Vector2Int.right : Vector2Int.left,
                Axis.Vertical => _isMovingPositive ? Vector2Int.up : Vector2Int.down,
                _ => Vector2Int.right,
            };
        }

        /// <summary>
        /// 移動先が塞がっているかを判定する。
        ///   - 範囲外、Wall：塞がり
        ///   - Hole：塞がりではない（落下のため進入する）
        ///   - Occupant あり：Player なら突進可、それ以外（敵 / 箱）は塞がり
        /// </summary>
        private static bool IsBlocked(GridCell cell)
        {
            if (cell == null) return true;
            if (cell.TileType == TileType.Wall) return true;
            if (cell.Occupant != null)
            {
                Player asPlayer = cell.Occupant.GetComponent<Player>();
                if (asPlayer == null) return true;
            }
            return false;
        }

        private void MoveInto(Vector2Int newPos, GridCell targetCell)
        {
            /* 現セルから Occupant を外す */
            GridCell current = _gridManager.GetCell(_gridPosition);
            if (current != null && current.Occupant == gameObject) current.Occupant = null;

            _gridPosition = newPos;
            transform.position = _gridManager.GridToWorld(_gridPosition);

            /* 穴落下：Occupant に登録せず、自身を非アクティブ化して退場 */
            if (targetCell.TileType == TileType.Hole)
            {
                gameObject.SetActive(false);
                return;
            }

            /* 通常進入 or プレイヤー突進：Occupant を上書きする
               （プレイヤー突進時は Player の Occupant を上書きするが、SequenceExecutor 側で
                 "プレイヤーと敵が同セル" を死亡として検知する） */
            targetCell.Occupant = gameObject;
        }

        public override object CaptureState() => _isMovingPositive;

        public override void RestoreState(object state)
        {
            if (state is bool b) _isMovingPositive = b;
        }
    }
}
