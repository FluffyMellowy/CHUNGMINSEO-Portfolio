namespace Colorless.Entity.Enemy
{
    using Sirenix.OdinInspector;
    using UnityEngine;
    using Colorless.Grid;

    /// <summary>
    /// 平常時は静止。毎ターン 4 方向の直線視野でプレイヤーを探索し、
    /// 視野内に見えたらその方向へ最遠まで突進する敵。
    ///
    /// 視野ルール（決定論的）：
    ///   - 自セルから 4 方向（上 / 右 / 下 / 左）の固定順序で 1 セルずつスキャン
    ///   - Wall に当たると視線終了
    ///   - Box / 他敵に当たると視線遮蔽（その方向の先は見えない）
    ///   - Hole は視線を遮らない（突進すると落下する点に注意）
    ///   - 各方向は独立にスキャン。プレイヤーは座標が一意なので最大 1 方向でしか見つからない
    ///
    /// 突進ルール：
    ///   - 視野で見つけた方向へ直進。Wall / Box / 他敵の直前で停止
    ///   - 突進中プレイヤー同セル通過で殺害（その時点で SequenceExecutor が中断）
    ///   - 突進中 Hole 進入 → 落下消滅
    /// </summary>
    public sealed class ChargerEnemy : EnemyBase
    {
        private static readonly Vector2Int[] ScanOrder =
        {
            Vector2Int.up, Vector2Int.right, Vector2Int.down, Vector2Int.left,
        };

        [ShowInInspector, ReadOnly, LabelText("Last Sighted Direction")]
        private Vector2Int? _lastSighted;

        public override void Act()
        {
            if (!gameObject.activeSelf) return;

            _lastSighted = ScanForPlayer();
            if (!_lastSighted.HasValue) return;

            Charge(_lastSighted.Value);
        }

        private Vector2Int? ScanForPlayer()
        {
            foreach (Vector2Int dir in ScanOrder)
            {
                Vector2Int cur = _gridPosition + dir;
                while (true)
                {
                    GridCell cell = _gridManager.GetCell(cur);
                    if (cell == null) break;
                    if (cell.TileType == TileType.Wall) break;

                    if (cell.Occupant != null)
                    {
                        if (cell.Occupant.GetComponent<Player>() != null)
                            return dir;
                        /* Box / 他敵 → 視野遮蔽 */
                        break;
                    }

                    cur += dir;
                }
            }
            return null;
        }

        private void Charge(Vector2Int dir)
        {
            Vector2Int cur = _gridPosition;

            while (true)
            {
                Vector2Int next = cur + dir;
                GridCell cell = _gridManager.GetCell(next);
                if (cell == null) break;
                if (cell.TileType == TileType.Wall) break;

                if (cell.Occupant != null)
                {
                    if (cell.Occupant.GetComponent<Player>() != null)
                    {
                        /* プレイヤー同セルを通過 → 殺害（突進は実質ここで終了） */
                        cur = next;
                        break;
                    }
                    /* Box / 他敵 → 手前で停止 */
                    break;
                }

                cur = next;

                /* 穴に入った瞬間に落下消滅 */
                if (cell.TileType == TileType.Hole)
                {
                    CommitMove(cur);
                    gameObject.SetActive(false);
                    return;
                }
            }

            if (cur != _gridPosition)
            {
                CommitMove(cur);
                /* Hazard 着地でも退場（穴と同様の環境キル） */
                if (gameObject.activeSelf) CheckHazardAndDie();
            }
        }

        private void CommitMove(Vector2Int newPos)
        {
            GridCell current = _gridManager.GetCell(_gridPosition);
            if (current != null && current.Occupant == gameObject) current.Occupant = null;

            _gridPosition = newPos;
            transform.position = _gridManager.GridToWorld(_gridPosition);

            GridCell target = _gridManager.GetCell(newPos);
            if (target != null) target.Occupant = gameObject;
        }
    }
}
