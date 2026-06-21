namespace Colorless.Entity.Enemy
{
    using UnityEngine;
    using Colorless.Grid;

    /// <summary>
    /// 移動しない固定殺害トラップ。
    /// Occupant としては登録せず（進入を妨げない）、GridCell.Hazard としてのみ自身を置く。
    /// プレイヤー・敵・箱がこのセルへ進入すると死亡・破棄される（環境キル）。
    ///
    /// 死亡判定の責務は侵入側にある：
    ///   - 敵：EnemyBase.CheckHazardAndDie を移動直後に呼ぶ
    ///   - プレイヤー：SequenceExecutor の死亡判定がハザード参照を見る
    ///   - 箱：PushBoxEffect の進入後判定（別タスクで対応予定）
    /// </summary>
    public sealed class SpikeTrap : EnemyBase
    {
        public override void Initialize()
        {
            /* 親 EnemyBase.Initialize は Occupant に自身を登録するが、ハザードでは Occupant にしない。
               重複ロジックを避けるため、独自に初期化処理を行う。 */
            _gridPosition = _gridManager.WorldToGrid(transform.position);
            transform.position = _gridManager.GridToWorld(_gridPosition);

            GridCell cell = _gridManager.GetCell(_gridPosition);
            if (cell != null) cell.Hazard = gameObject;

            _turnManager.RegisterEntity(this);
        }

        /// <summary>
        /// SpikeTrap は能動的に行動しない。
        /// </summary>
        public override void Act()
        {
            /* no-op */
        }
    }
}
