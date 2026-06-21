namespace Colorless.Entity.Enemy
{
    using System.Collections.Generic;
    using Sirenix.OdinInspector;
    using UnityEngine;
    using VContainer;
    using Colorless.Grid;

    /// <summary>
    /// 毎ターン BFS で到達可能なプレイヤー位置までの最短経路を計算し、その 1 セル先へ進む追跡敵。
    ///
    /// 探索ルール（決定論的）：
    ///   - 自セルからプレイヤーセルに向かって BFS。展開順は up → right → down → left の固定順
    ///   - 通行可能セル：Floor / Exit_* / Hole（落下覚悟で通る）
    ///   - 通行不可：Wall、Box / 他敵がいる Occupant
    ///   - 経路同点（同距離）の場合、BFS 展開順により最初に出現した経路を選ぶ
    ///   - 経路が存在しない（完全遮断）→ そのターン静止
    ///
    /// 移動結果：
    ///   - 選んだ次セルへ 1 セル進む
    ///   - Hole に進入 → 落下消滅
    ///   - プレイヤー同セル進入 → 殺害
    /// </summary>
    public sealed class ChaserEnemy : EnemyBase
    {
        private static readonly Vector2Int[] ExpandOrder =
        {
            Vector2Int.up, Vector2Int.right, Vector2Int.down, Vector2Int.left,
        };

        [Inject] private Player _player;

        [ShowInInspector, ReadOnly, LabelText("Last Path Step")]
        private Vector2Int? _lastStep;

        public override void Act()
        {
            if (!gameObject.activeSelf) return;
            if (_player == null) return;

            Vector2Int? step = FindFirstStepBFS(_player.GridPosition);
            _lastStep = step;
            if (!step.HasValue) return;

            MoveInto(step.Value);

            if (gameObject.activeSelf) CheckHazardAndDie();
        }

        /// <summary>
        /// BFS で最短経路を求め、その最初の 1 セルを返す。経路無し → null。
        /// </summary>
        private Vector2Int? FindFirstStepBFS(Vector2Int target)
        {
            if (target == _gridPosition) return null;

            Queue<Vector2Int> queue = new();
            /* parent[cell] = そのセルへ最初に辿り着いたときの親セル */
            Dictionary<Vector2Int, Vector2Int> parent = new();
            queue.Enqueue(_gridPosition);
            parent[_gridPosition] = _gridPosition; /* root を自分自身でマーク（"visited" 用） */

            while (queue.Count > 0)
            {
                Vector2Int cur = queue.Dequeue();
                if (cur == target)
                    return BacktrackFirstStep(parent, target);

                foreach (Vector2Int dir in ExpandOrder)
                {
                    Vector2Int next = cur + dir;
                    if (parent.ContainsKey(next)) continue;
                    if (!IsTraversable(next, target)) continue;
                    parent[next] = cur;
                    queue.Enqueue(next);
                }
            }

            return null;
        }

        /// <summary>
        /// BFS で見つけた target セルから親を辿り、_gridPosition の直後の "最初の 1 歩" を返す。
        /// </summary>
        private Vector2Int BacktrackFirstStep(Dictionary<Vector2Int, Vector2Int> parent, Vector2Int target)
        {
            Vector2Int step = target;
            while (parent.TryGetValue(step, out Vector2Int prev) && prev != _gridPosition)
                step = prev;
            return step;
        }

        /// <summary>
        /// BFS の通行判定。target セル自身は到達点なので、Occupant がプレイヤーであっても通す。
        /// </summary>
        private bool IsTraversable(Vector2Int pos, Vector2Int target)
        {
            GridCell cell = _gridManager.GetCell(pos);
            if (cell == null) return false;
            if (cell.TileType == TileType.Wall) return false;

            if (cell.Occupant != null)
            {
                /* 目的地のプレイヤーは通る */
                if (pos == target && cell.Occupant.GetComponent<Player>() != null) return true;
                /* Box / 他敵 → 通行不可 */
                return false;
            }
            return true;
        }

        private void MoveInto(Vector2Int newPos)
        {
            GridCell current = _gridManager.GetCell(_gridPosition);
            if (current != null && current.Occupant == gameObject) current.Occupant = null;

            _gridPosition = newPos;
            transform.position = _gridManager.GridToWorld(_gridPosition);

            GridCell target = _gridManager.GetCell(newPos);
            if (target == null) return;

            /* 穴落下 */
            if (target.TileType == TileType.Hole)
            {
                gameObject.SetActive(false);
                return;
            }

            target.Occupant = gameObject;
        }
    }
}
